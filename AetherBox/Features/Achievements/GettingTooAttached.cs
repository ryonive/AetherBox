using System;
using System.Linq;
using System.Numerics;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using AetherBox.UI;
using ClickLib.Clicks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Dalamud.Memory;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Features.Achievements;

public class GettingTooAttached : Feature
{
	public class Configs : FeatureConfig
	{
		[FeatureConfigOption("Times to loop", "", 1, null, IntMin = 0, IntMax = 10000, EditorSize = 300)]
		public int numberOfLoops = 10000;
	}

	private Overlays overlay;

	private float height;

	internal bool active;

	public override string Name => "Getting Too Attached";

	public override string Description => "Adds a button to the materia melding window to loop melding for the Getting Too Attached achievements.";

	public override FeatureType FeatureType => FeatureType.Achievements;

	public Configs Config { get; private set; }

	public override bool UseAutoConfig => false;

	public override void Enable()
	{
		Config = LoadConfig<Configs>() ?? new Configs();
		overlay = new Overlays(this);
		Svc.Toasts.ErrorToast += CheckForErrors;
		Common.OnAddonSetup += ConfirmMateriaDialog;
		Common.OnAddonSetup += ConfirmRetrievalDialog;
		base.Enable();
	}

	public override void Disable()
	{
		SaveConfig(Config);
		Plugin.WindowSystem.RemoveWindow(overlay);
		Svc.Toasts.ErrorToast -= CheckForErrors;
		Common.OnAddonSetup -= ConfirmMateriaDialog;
		Common.OnAddonSetup -= ConfirmRetrievalDialog;
		base.Disable();
	}

	public unsafe override void Draw()
	{
		if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var addon) || addon->UldManager.NodeListCount <= 1 || !addon->UldManager.NodeList[1]->IsVisible)
		{
			return;
		}
		AtkResNode* node;
		node = addon->UldManager.NodeList[1];
		if (!node->IsVisible)
		{
			return;
		}
		AtkResNodeHelper.GetNodePosition(node);
		ImGuiHelpers.ForceNextWindowMainViewport();
		ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(addon->X, (float)addon->Y - height));
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
		ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(10f, 10f));
		ImGui.Begin($"###LoopMelding{node->NodeID}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoNavFocus);
		if (ImGui.Button((!active) ? "Getting Too Attached###StartLooping" : "Looping. Click to abort.###AbortLoop"))
		{
			if (!active)
			{
				active = true;
				TaskManager.Enqueue((System.Action)YesAlready.DisableIfNeeded, (string)null);
				TaskManager.Enqueue((System.Action)TryGettingTooAttached, (string)null);
			}
			else
			{
				CancelLoop();
			}
		}
		ImGui.SameLine();
		ImGui.PushItemWidth(150f);
		if (ImGui.SliderInt("Loops", ref Config.numberOfLoops, 0, 10000))
		{
			SaveConfig(Config);
		}
		height = ImGui.GetWindowSize().Y;
		ImGui.End();
		ImGui.PopStyleVar(2);
	}

	private void CancelLoop()
	{
		active = false;
		TaskManager.Abort();
		TaskManager.Enqueue((System.Action)YesAlready.EnableIfNeeded, (string)null);
	}

	private void CheckForErrors(ref SeString message, ref bool isHandled)
	{
		string msg;
		msg = message.ExtractText();
		if (new int[2] { 7701, 7707 }.Any((int x) => msg == Svc.Data.GetExcelSheet<LogMessage>().FirstOrDefault((LogMessage y) => y.RowId == x)?.Text.ExtractText()))
		{
			PrintModuleMessage("Error while melding. Aborting Tasks.");
			CancelLoop();
		}
	}

	private static bool IsBusy()
	{
		if (!Svc.Condition[ConditionFlag.MeldingMateria] && !Svc.Condition[ConditionFlag.Occupied39])
		{
			return !Svc.Condition[ConditionFlag.NormalConditions];
		}
		return true;
	}

	private void TryGettingTooAttached()
	{
		if (Config.numberOfLoops > 0)
		{
			TaskManager.Enqueue((Func<bool?>)SelectItem, "Selecting Item");
			TaskManager.Enqueue((Func<bool?>)SelectMateria, "Selecting Materia");
			TaskManager.Enqueue((Func<bool?>)SelectItem, "Selecting Item");
			TaskManager.Enqueue(() => ActivateContextMenu(), "Opening Context Menu");
			TaskManager.Enqueue(() => RetrieveMateriaContextMenu(), "Activating Retrieve Materia Context Entry");
			TaskManager.Enqueue(delegate
			{
				Config.numberOfLoops--;
			});
			TaskManager.Enqueue((System.Action)TryGettingTooAttached, "Repeat Loop");
		}
		else
		{
			TaskManager.Enqueue(() => active = false);
			TaskManager.Enqueue((System.Action)YesAlready.EnableIfNeeded, (string)null);
		}
	}

	private unsafe bool? SelectItem()
	{
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var addon) && !IsBusy() && !AreDialogsOpen())
		{
			if (addon->UldManager.NodeList[16]->IsVisible)
			{
				CancelLoop();
				PrintModuleMessage("Unable to continue. No gear in inventory");
				return false;
			}
			Callback.Fire(addon, false, 1, 0, 1, 0);
			return addon->AtkValues[287].Int != -1;
		}
		return false;
	}

	public static bool AreDialogsOpen()
	{
		if (Svc.GameGui.GetAddonByName("MateriaAttachDialog") != IntPtr.Zero)
		{
			return Svc.GameGui.GetAddonByName("MateriaRetrieveDialog") != IntPtr.Zero;
		}
		return false;
	}

	public unsafe bool? SelectMateria()
	{
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var addon) && !AreDialogsOpen())
		{
			if (addon->UldManager.NodeList[6]->IsVisible)
			{
				CancelLoop();
				PrintModuleMessage("Unable to continue. No materia to meld.");
				return false;
			}
			if (MemoryHelper.ReadSeStringNullTerminated(new IntPtr(addon->AtkValues[289].String)).ToString().Split(new char[2] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)[2] == "0")
			{
				CancelLoop();
				PrintModuleMessage("Unable to continue. First listed materia has too high ilvl requirements.");
				return false;
			}
			Callback.Fire(addon, false, 2, 0, 1, 0);
			AtkUnitBase* attachDialog;
			return GenericHelpers.TryGetAddonByName<AtkUnitBase>("MateriaAttachDialog", out attachDialog) && attachDialog->IsVisible && Svc.Condition[ConditionFlag.MeldingMateria];
		}
		return false;
	}

	public unsafe void ConfirmMateriaDialog(SetupAddonArgs obj)
	{
		if (obj.AddonName != "MateriaAttachDialog" || !active)
		{
			return;
		}
		if (obj.Addon->AtkValues[50].Type != 0)
		{
			CancelLoop();
			PrintModuleMessage("Unable to continue. This gear is requires overmelding.");
			return;
		}
		TaskManager.EnqueueImmediate(() => Svc.Condition[ConditionFlag.MeldingMateria]);
		TaskManager.EnqueueImmediate(delegate
		{
			Callback.Fire(obj.Addon, true, 0, 0, 0);
		});
	}

	public unsafe bool ActivateContextMenu()
	{
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var addon) && !Svc.Condition[ConditionFlag.MeldingMateria])
		{
			Callback.Fire(addon, false, 4, 0, 1, 0);
			if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ContextMenu", out var contextMenu))
			{
				return contextMenu->IsVisible;
			}
			return false;
		}
		return false;
	}

	private unsafe static bool RetrieveMateriaContextMenu()
	{
		if (!Svc.Condition[ConditionFlag.Occupied39])
		{
			Callback.Fire((AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu"), true, 0, 1, 0u, 0, 0);
		}
		return !Svc.Condition[ConditionFlag.Occupied39];
	}

	public unsafe void ConfirmRetrievalDialog(SetupAddonArgs obj)
	{
		if (!(obj.AddonName != "MateriaRetrieveDialog") && active)
		{
			ClickMateriaRetrieveDialog.Using((nint)obj.Addon).Begin();
		}
	}
}
