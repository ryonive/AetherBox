using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AetherBox.Features;
using AetherBox.Features.UI;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using AetherBox.UI;
using ClickLib.Clicks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Features.UI;

public class WorkshopTurnin : Feature
{
	public class Configs : FeatureConfig
	{
		[FeatureConfigOption("Times to loop", "", 1, null, IntMin = 0, IntMax = 100, EditorSize = 300)]
		public int partsToBuild = 1;
	}

	public readonly struct PartIngredient
	{
		public Item Ingredient { get; }

		public uint RequiredLevelToTurnIn { get; }

		public ClassJob RequiredJobToTurnIn { get; }

		public uint AmountInInventory { get; }

		public uint AmountPerTurnIn { get; }

		public uint TotalRequiredAmount { get; }

		public uint TurnedInSoFar { get; }

		public uint TotalTimesToTurnIn { get; }

		public PartIngredient(Item ingredient, uint reqLevel, ClassJob reqJob, uint inventory, uint perturn, uint total, uint timesSoFar, uint timesTotal)
		{
			Ingredient = ingredient;
			RequiredLevelToTurnIn = reqLevel;
			RequiredJobToTurnIn = reqJob;
			AmountInInventory = inventory;
			AmountPerTurnIn = perturn;
			TotalRequiredAmount = total;
			TurnedInSoFar = timesSoFar;
			TotalTimesToTurnIn = timesTotal;
		}
	}

	private Overlays overlay;

	private float height;

	internal static bool active = false;

	internal bool phaseActive;

	internal bool projectActive;

	internal bool partLoopActive;

	private static readonly string[] SkipCutsceneStr = new string[6] { "Skip cutscene?", "要跳过这段过场动画吗？", "要跳過這段過場動畫嗎？", "Videosequenz überspringen?", "Passer la scène cinématique ?", "このカットシーンをスキップしますか？" };

	private static readonly string[] ContributeMaterialsStr = new string[4] { "Contribute materials.", "Materialien abliefern", "Fournir des matériaux", "素材を納品する" };

	private static readonly string[] AdvancePhaseStr = new string[4] { "Advance to the next phase of production.", "Arbeitsschritt ausführen", "Faire progresser un projet de con", "作業工程を進捗させる" };

	private static readonly string[] CompleteConstructionStr = new string[4] { "Complete the construction", "Herstellung", "Terminer la con", "を完成させる" };

	private static readonly string[] CollectProductStr = new string[4] { "Collect finished product.", "Produkt entgegennehmen", "Récupérer un projet terminé", "アイテムを受け取る" };

	private static readonly string[] LeaveWorkshopStr = new string[4] { "Nothing.", "Nichts", "Annuler", "やめる" };

	private static readonly string[] ConfirmContributionStr = new string[4] { "to the company project?", "schaftsprojekt bereitstellen?", "pour le projet de con", "カンパニー製作設備に納品します。" };

	private static readonly string[] ConfirmProductRetrievalStr = new string[4] { "Retrieve", "entnehmen", "Récupérer", "を回収します。" };

	internal static string[] PanelName = new string[1] { "Fabrication Station" };

	public override string Name => "FC Workshop Hand-In";

	public override string Description => "Adds buttons to auto hand-in the current phase and the entire project to the workshop menu.";

	public override FeatureType FeatureType => FeatureType.UI;

	public Configs Config { get; private set; }

	public override void Enable()
	{
		Config = LoadConfig<Configs>() ?? new Configs();
		overlay = new Overlays(this);
		Svc.Framework.Update += Tick;
		base.Enable();
	}

	public override void Disable()
	{
		SaveConfig(Config);
		Plugin.Ws.RemoveWindow(overlay);
		Svc.Framework.Update -= Tick;
		base.Disable();
	}

	public unsafe override void Draw()
	{
		if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon) || addon->UldManager.NodeListCount <= 1 || addon->UldManager.NodeListCount < 38 || !addon->UldManager.NodeList[1]->IsVisible)
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
		ImGui.Begin($"###LoopButtons{node->NodeID}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoNavFocus);
		if (active && !phaseActive)
		{
			ImGui.BeginDisabled();
		}
		if (ImGui.Button((!phaseActive) ? "Phase Turn In###StartPhaseLooping" : "Turning in... Click to Abort###AbortPhaseLoop"))
		{
			if (!phaseActive)
			{
				phaseActive = true;
				TurnInPhase();
			}
			else
			{
				EndLoop("User cancelled");
			}
		}
		if (active && !phaseActive)
		{
			ImGui.EndDisabled();
		}
		ImGui.SameLine();
		if (active && !projectActive)
		{
			ImGui.BeginDisabled();
		}
		if (ImGui.Button((!projectActive) ? "Project Turn In###StartProjectLooping" : "Turning in... Click to Abort###AbortProjectLoop"))
		{
			if (!projectActive)
			{
				projectActive = true;
				TurnInProject();
			}
			else
			{
				EndLoop("User cancelled");
			}
		}
		if (active && !projectActive)
		{
			ImGui.EndDisabled();
		}
		active = phaseActive || projectActive || partLoopActive;
		height = ImGui.GetWindowSize().Y;
		ImGui.End();
		ImGui.PopStyleVar(2);
	}

	private static void Tick(IFramework framework)
	{
		TextAdvanceManager.Tick();
		YesAlready.Tick();
	}

	private bool MustEndLoop(bool condition, string message)
	{
		if (condition)
		{
			PrintModuleMessage(message);
			EndLoop(message);
		}
		return condition;
	}

	private bool EndLoop(string msg)
	{
		Svc.Log.Debug("Cancelling... Reason: " + msg);
		active = false;
		phaseActive = false;
		projectActive = false;
		partLoopActive = false;
		TaskManager.Abort();
		return true;
	}

	private unsafe bool TurnInPhase()
	{
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon) && addon->AtkValues[12].Type != 0)
		{
			List<PartIngredient> requiredIngredients;
			requiredIngredients = GetRequiredItems();
			List<PartIngredient> list;
			list = requiredIngredients;
			if (list != null && list.Count == 0)
			{
				Svc.Log.Debug("req is 0");
				return true;
			}
			bool flag;
			flag = MustEndLoop(!IsSufficientlyLeveled(requiredIngredients), "Not high enough level to turn in items");
			if (!flag)
			{
				uint id;
				id = Svc.ClientState.LocalPlayer.ClassJob.Id;
				bool condition;
				condition = ((id < 8 || id > 15) ? true : false);
				flag = MustEndLoop(condition, "Must be a DoH to turn in items.");
			}
			if (flag)
			{
				return true;
			}
			foreach (PartIngredient ingredient in requiredIngredients)
			{
				if (ingredient.AmountPerTurnIn > ingredient.AmountInInventory)
				{
					continue;
				}
				for (uint i = ingredient.TurnedInSoFar; i < ingredient.TotalTimesToTurnIn; i++)
				{
					TaskManager.EnqueueImmediate(() => ClickItem(requiredIngredients.IndexOf(ingredient), ingredient.AmountPerTurnIn), $"{"ClickItem"} {ingredient.Ingredient.Name}");
					TaskManager.DelayNextImmediate(300);
					TaskManager.EnqueueImmediate(() => ConfirmHQTrade(), 200, "ConfirmHQTrade");
					TaskManager.DelayNextImmediate(300);
					TaskManager.EnqueueImmediate(() => ConfirmContribution(), "ConfirmContribution");
				}
			}
			bool hasMorePhases;
			hasMorePhases = addon->AtkValues[6].UInt != addon->AtkValues[7].UInt - 1;
			TaskManager.EnqueueImmediate((!hasMorePhases) ? new Func<bool?>(CompleteConstruction) : new Func<bool?>(AdvancePhase));
			TaskManager.EnqueueImmediate((Func<bool?>)WaitForCutscene, "WaitForCutscene");
			TaskManager.EnqueueImmediate((Func<bool?>)PressEsc, "PressEsc");
			TaskManager.EnqueueImmediate((Func<bool?>)ConfirmSkip, "ConfirmSkip");
			if (phaseActive)
			{
				TaskManager.Enqueue(() => EndLoop("Finished TurnInPhase"));
			}
			return true;
		}
		return false;
	}

	private unsafe bool TurnInProject()
	{
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon))
		{
			for (uint i = addon->AtkValues[6].UInt; i < addon->AtkValues[7].UInt; i++)
			{
				bool num;
				num = i != addon->AtkValues[7].UInt - 1;
				TaskManager.Enqueue(() => TurnInPhase(), $"{"TurnInPhase"} {i}");
				TaskManager.Enqueue((Func<bool?>)InteractWithFabricationPanel, "InteractWithFabricationPanel");
				if (num)
				{
					TaskManager.Enqueue((Func<bool?>)ContributeMaterials, "ContributeMaterials");
					continue;
				}
				TaskManager.Enqueue((Func<bool?>)CollectProduct, "CollectProduct");
				TaskManager.Enqueue(() => ConfirmProductRetrieval(), "ConfirmProductRetrieval");
				TaskManager.Enqueue((Func<bool?>)LeaveWorkshop, "LeaveWorkshop");
			}
			if (projectActive)
			{
				TaskManager.Enqueue(() => EndLoop("Finished TurnInProject"));
			}
			return true;
		}
		EndLoop("Failed to find SubmarinePartsMenu");
		return true;
	}

	private unsafe List<PartIngredient> GetRequiredItems()
	{
		if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon))
		{
			EndLoop("Failed to find SubmarinePartsMenu");
			return null;
		}
		return (from i in Enumerable.Range(0, 12)
			where addon->AtkValues[36 + i].Type != (FFXIVClientStructs.FFXIV.Component.GUI.ValueType)0
			select new PartIngredient(Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).GetRow(addon->AtkValues[12 + i].UInt), addon->AtkValues[144 + i].UInt, Svc.Data.GetExcelSheet<ClassJob>(Svc.ClientState.ClientLanguage).FirstOrDefault((ClassJob x) => x.RowId == addon->AtkValues[48 + i].Int - 62000), addon->AtkValues[72 + i].UInt, addon->AtkValues[60 + i].UInt, addon->AtkValues[60 + i].UInt * addon->AtkValues[120 + i].UInt, addon->AtkValues[108 + i].UInt, addon->AtkValues[120 + i].UInt)).ToList();
	}

	private unsafe static bool IsSufficientlyLeveled(List<PartIngredient> requiredIngredients)
	{
		foreach (PartIngredient i in requiredIngredients)
		{
			if (PlayerState.Instance()->ClassJobLevelArray[i.RequiredJobToTurnIn.ExpArrayIndex] < i.RequiredLevelToTurnIn)
			{
				return false;
			}
		}
		return true;
	}

	private unsafe bool ClickItem(int positionInList, uint turnInAmount)
	{
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("Request", out var requestAddon) && requestAddon->IsVisible)
		{
			return false;
		}
		if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon))
		{
			Callback.Fire(addon, false, 0, (uint)positionInList, turnInAmount);
			if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("Request", out var rAddon))
			{
				return rAddon->IsVisible;
			}
			return false;
		}
		return false;
	}

	private unsafe static bool ConfirmContribution()
	{
		return ConfirmContributionStr.Any(delegate(string str)
		{
			AtkUnitBase* specificYesno;
			specificYesno = BaseFeature.GetSpecificYesno((string s) => s.ContainsAny(StringComparison.OrdinalIgnoreCase, str));
			if (specificYesno != null)
			{
				ClickSelectYesNo.Using((nint)specificYesno).Yes();
				return true;
			}
			return false;
		});
	}

	private unsafe static bool ConfirmHQTrade()
	{
		AtkUnitBase* x;
		x = BaseFeature.GetSpecificYesno(Svc.Data.GetExcelSheet<Addon>().GetRow(102434u).Text);
		if (x != null)
		{
			ClickSelectYesNo.Using((nint)x).Yes();
			return true;
		}
		return false;
	}

	private unsafe static bool ConfirmProductRetrieval()
	{
		return ConfirmProductRetrievalStr.Any(delegate(string str)
		{
			AtkUnitBase* specificYesno;
			specificYesno = BaseFeature.GetSpecificYesno((string s) => s.ContainsAny(StringComparison.OrdinalIgnoreCase, str));
			if (specificYesno != null)
			{
				ClickSelectYesNo.Using((nint)specificYesno).Yes();
				return true;
			}
			return false;
		});
	}

	private static bool? ContributeMaterials()
	{
		return ContributeMaterialsStr.Any((string str) => BaseFeature.TrySelectSpecificEntry(str, () => BaseFeature.GenericThrottle && EzThrottler.Throttle("WorkshopTurnin.ContributeMaterials", 1000)));
	}

	private static bool? AdvancePhase()
	{
		return AdvancePhaseStr.Any((string str) => BaseFeature.TrySelectSpecificEntry(str, () => BaseFeature.GenericThrottle && EzThrottler.Throttle("WorkshopTurnin.AdvancePhase", 1000)));
	}

	private static bool? CompleteConstruction()
	{
		return CompleteConstructionStr.Any((string str) => BaseFeature.TrySelectSpecificEntry(str, () => BaseFeature.GenericThrottle && EzThrottler.Throttle("WorkshopTurnin.CompleteConstruction", 1000)));
	}

	private static bool? CollectProduct()
	{
		return CollectProductStr.Any((string str) => BaseFeature.TrySelectSpecificEntry(str, () => BaseFeature.GenericThrottle && EzThrottler.Throttle("WorkshopTurnin.CollectProduct", 1000)));
	}

	private static bool? LeaveWorkshop()
	{
		return LeaveWorkshopStr.Any((string str) => BaseFeature.TrySelectSpecificEntry(str, () => BaseFeature.GenericThrottle && EzThrottler.Throttle("WorkshopTurnin.CollectProduct", 1000)));
	}

	private static bool? WaitForCutscene()
	{
		return Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Svc.Condition[ConditionFlag.WatchingCutscene78];
	}

	private unsafe static bool? PressEsc()
	{
		nint nLoading;
		nLoading = Svc.GameGui.GetAddonByName("NowLoading");
		if (nLoading != IntPtr.Zero)
		{
			AtkUnitBase* nowLoading;
			nowLoading = (AtkUnitBase*)nLoading;
			if (!nowLoading->IsVisible && WindowsKeypress.SendKeypress(Keys.Escape))
			{
				return true;
			}
		}
		return false;
	}

	private unsafe static bool? ConfirmSkip()
	{
		nint addon;
		addon = Svc.GameGui.GetAddonByName("SelectString");
		if (addon == IntPtr.Zero)
		{
			return false;
		}
		AddonSelectString* selectStrAddon;
		selectStrAddon = (AddonSelectString*)addon;
		if (!GenericHelpers.IsAddonReady(&selectStrAddon->AtkUnitBase))
		{
			return false;
		}
		if (!SkipCutsceneStr.Contains(selectStrAddon->AtkUnitBase.UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText.ToString()))
		{
			return false;
		}
		if (EzThrottler.Throttle("WorkshopTurnin.ConfirmSkip"))
		{
			Svc.Log.Debug("Selecting cutscene skipping");
			ClickSelectString.Using(addon).SelectItem(0);
			return true;
		}
		return false;
	}

	internal static bool TryGetNearestFabricationPanel(out GameObject obj)
	{
		return Svc.Objects.TryGetFirst((GameObject x) => x.Name.ToString().EqualsAny<string>(PanelName) && x.IsTargetable, out obj);
	}

	internal unsafe static bool? InteractWithFabricationPanel()
	{
		if (BaseFeature.IsLoading())
		{
			return false;
		}
		if (TryGetNearestFabricationPanel(out var obj))
		{
			if (Svc.Targets.Target?.Address == obj.Address)
			{
				if (BaseFeature.GenericThrottle && EzThrottler.Throttle("WorkshopTurnin.InteractWithFabricationPanel", 2000))
				{
					TargetSystem.Instance()->InteractWithObject(obj.Struct(), checkLineOfSight: false);
					return true;
				}
			}
			else if (obj.IsTargetable && BaseFeature.GenericThrottle)
			{
				Svc.Targets.Target = obj;
			}
		}
		return false;
	}
}
