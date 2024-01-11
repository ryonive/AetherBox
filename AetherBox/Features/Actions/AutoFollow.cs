using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using AetherBox.Features;
using AetherBox.Features.Actions;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Components;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;

namespace AetherBox.Features.Actions;

public class AutoFollow : Feature
{
	public class Configs : FeatureConfig
	{
		[FeatureConfigOption("Distance to Keep", "", 1, null)]
		public int distanceToKeep = 3;

		[FeatureConfigOption("Don't follow if further than this (yalms)", "", 2, null, IntMin = 0, IntMax = 30, EditorSize = 300, HelpText = "Set to 0 to disable")]
		public int disableIfFurtherThan;

		[FeatureConfigOption("Function only in duty", "", 3, null)]
		public bool OnlyInDuty;

		[FeatureConfigOption("Change master on chat message", "", 4, null, IntMin = 0, IntMax = 30, EditorSize = 300, HelpText = "If a party chat message contains \"autofollow\", the current master will be switched to them.")]
		public bool changeMasterOnChat;

		[FeatureConfigOption("Mount & Fly (Experimental)", "", 5, null)]
		public bool MountAndFly = true;
	}

	private readonly List<string> registeredCommands = new List<string>();

	private readonly OverrideMovement movement = new OverrideMovement();

	private Dalamud.Game.ClientState.Objects.Types.GameObject? master;

	private uint? masterObjectID;

	public override string Name => "Auto Follow";

	public override string Description => "True Auto Follow. Trigger with /autofollow while targeting someone. Use it with no target to wipe the current master.";

	public override FeatureType FeatureType => FeatureType.Actions;

	public Configs Config { get; private set; }

	protected override DrawConfigDelegate DrawConfigTree => delegate(ref bool hasChanged)
	{
		if (ImGui.Checkbox("Function Only in Duty", ref Config.OnlyInDuty))
		{
			hasChanged = true;
		}
		if (ImGui.Checkbox("Change master on chat message", ref Config.changeMasterOnChat))
		{
			hasChanged = true;
		}
		ImGuiComponents.HelpMarker("If a party chat message contains \"autofollow\", the current master will be switched to them.");
		if (ImGui.Checkbox("Mount & Fly", ref Config.MountAndFly))
		{
			hasChanged = true;
		}
		ImGui.PushItemWidth(300f);
		if (ImGui.SliderInt("Distance to Keep (yalms)", ref Config.distanceToKeep, 0, 30))
		{
			hasChanged = true;
		}
		ImGui.PushItemWidth(300f);
		if (ImGui.SliderInt("Disable if Further Than (yalms)", ref Config.disableIfFurtherThan, 0, 300))
		{
			hasChanged = true;
		}
		ImGui.Text($"Current Master: {((master != null) ? master.Name : ((SeString)"null"))}");
		if (Svc.ClientState.LocalPlayer == null)
		{
			ImGui.Text("Your Position: x: null, y: null, z: null");
		}
		else
		{
			ImGui.Text($"Your Position: x: {Svc.ClientState.LocalPlayer.Position.X}, y: {Svc.ClientState.LocalPlayer.Position.Y}, z: {Svc.ClientState.LocalPlayer.Position.Z}");
		}
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 3);
		defaultInterpolatedStringHandler.AppendLiteral("Master Position: x: ");
		defaultInterpolatedStringHandler.AppendFormatted((master != null) ? ((object)master.Position.X) : "null");
		defaultInterpolatedStringHandler.AppendLiteral(", y: ");
		defaultInterpolatedStringHandler.AppendFormatted((master != null) ? ((object)master.Position.Y) : "null");
		defaultInterpolatedStringHandler.AppendLiteral(", z: ");
		defaultInterpolatedStringHandler.AppendFormatted((master != null) ? ((object)master.Position.Z) : "null");
		ImGui.Text(defaultInterpolatedStringHandler.ToStringAndClear());
		defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
		defaultInterpolatedStringHandler.AppendLiteral("Distance to Master: ");
		defaultInterpolatedStringHandler.AppendFormatted((master != null && Svc.ClientState.LocalPlayer != null) ? ((object)Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position)) : "null");
		ImGui.Text(defaultInterpolatedStringHandler.ToStringAndClear());
		if (ImGui.Button("Set"))
		{
			SetMaster();
		}
		ImGui.SameLine();
		if (ImGui.Button("Clear"))
		{
			ClearMaster();
		}
	};

	public string Command { get; set; } = "/autofollow";


	protected void OnCommand(List<string> args)
	{
		try
		{
			if (Svc.Targets.Target != null)
			{
				SetMaster();
			}
			else
			{
				ClearMaster();
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	protected virtual void OnCommandInternal(string _, string args)
	{
		args = args.ToLower();
		OnCommand(args.Split(' ').ToList());
	}

	public override void Enable()
	{
		Config = LoadConfig<Configs>() ?? new Configs();
		if (Svc.Commands.Commands.ContainsKey(Command))
		{
			Svc.Log.Error("Command '" + Command + "' is already registered.");
		}
		else
		{
			Svc.Commands.AddHandler(Command, new CommandInfo(OnCommandInternal)
			{
				HelpMessage = "",
				ShowInHelp = false
			});
			registeredCommands.Add(Command);
		}
		Svc.Framework.Update += Follow;
		Svc.Chat.ChatMessage += OnChatMessage;
		base.Enable();
	}

	public override void Disable()
	{
		SaveConfig(Config);
		foreach (string c in registeredCommands)
		{
			Svc.Commands.RemoveHandler(c);
		}
		registeredCommands.Clear();
		Svc.Framework.Update -= Follow;
		Svc.Chat.ChatMessage -= OnChatMessage;
		base.Disable();
	}

	private void SetMaster()
	{
		try
		{
			master = Svc.Targets.Target;
			masterObjectID = Svc.Targets.Target.ObjectId;
		}
		catch
		{
		}
	}

	private void ClearMaster()
	{
		master = null;
		masterObjectID = null;
	}

	private unsafe void Follow(IFramework framework)
	{
		master = Svc.Objects.FirstOrDefault((Dalamud.Game.ClientState.Objects.Types.GameObject x) => x.ObjectId == masterObjectID);
		if (master == null)
		{
			movement.Enabled = false;
			return;
		}
		if (Config.disableIfFurtherThan > 0 && Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) > (float)Config.disableIfFurtherThan)
		{
			movement.Enabled = false;
			return;
		}
		if (Config.OnlyInDuty && GameMain.Instance()->CurrentContentFinderConditionId == 0)
		{
			movement.Enabled = false;
			return;
		}
		if (Svc.Condition[ConditionFlag.InFlight])
		{
			TaskManager.Abort();
		}
		if (master.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
		{
			if (((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)master.Address)->IsMounted() && CanMount())
			{
				movement.Enabled = false;
				ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9u, 3758096384uL, 0u, 0u, 0u, null);
				return;
			}
			if (Config.MountAndFly && ((Structs.Character*)master.Address)->IsFlying != 0 && !Svc.Condition[ConditionFlag.InFlight] && Svc.Condition[ConditionFlag.Mounted])
			{
				movement.Enabled = false;
				TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2u, 3758096384uL, 0u, 0u, 0u, null));
				TaskManager.DelayNext(50);
				TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2u, 3758096384uL, 0u, 0u, 0u, null));
				return;
			}
			if (!((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)master.Address)->IsMounted() && Svc.Condition[ConditionFlag.Mounted])
			{
				movement.Enabled = false;
				((FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)master.Address)->GetStatusManager->RemoveStatus(10, 0);
				ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9u, 3758096384uL, 0u, 0u, 0u, null);
				return;
			}
		}
		if (Vector3.Distance(Svc.ClientState.LocalPlayer.Position, master.Position) <= (float)Config.distanceToKeep)
		{
			movement.Enabled = false;
			return;
		}
		movement.Enabled = true;
		movement.DesiredPosition = master.Position;
	}

	private static bool CanMount()
	{
		if (!Svc.Condition[ConditionFlag.Mounted] && !Svc.Condition[ConditionFlag.Mounting] && !Svc.Condition[ConditionFlag.InCombat])
		{
			return !Svc.Condition[ConditionFlag.Casting];
		}
		return false;
	}

	private unsafe void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
	{
		if (type != XivChatType.Party)
		{
			return;
		}
		PlayerPayload player;
		player = sender.Payloads.SingleOrDefault((Payload x) => x is PlayerPayload) as PlayerPayload;
		if (!message.TextValue.ToLowerInvariant().Contains("autofollow", StringComparison.CurrentCultureIgnoreCase))
		{
			return;
		}
		foreach (Dalamud.Game.ClientState.Objects.Types.GameObject actor in Svc.Objects)
		{
			if (!(actor == null))
			{
				Svc.Log.Info($"{actor.Name.TextValue} == {player.PlayerName} {actor.Name.TextValue.ToLowerInvariant().Equals(player.PlayerName)}");
				if (actor.Name.TextValue.Equals(player.PlayerName) && ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actor.Address)->GetIsTargetable())
				{
					Svc.Targets.Target = actor;
					SetMaster();
				}
			}
		}
	}
}
