using System.Numerics;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using AetherBox.Helpers.EasyCombat;
using ECommons.Automation;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.GeneratedSheets2;
using Dalamud.Game.ClientState.Objects.SubKinds;
using static AetherBox.Helpers.BossMod.ActorCastEvent;
using ECommons;
using ECommons.DalamudServices.Legacy;

namespace AetherBox.Features.Actions;
#pragma warning disable S1104 // Fields should not have public accessibility
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable S4487 // Unread "private" fields should be removed
public class AutoBusyOnTeleport : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Auto Busy on TP", "", 3, null)]

        public bool AutoBusy;

        [FeatureConfigOption("Remove after TP", "", 3, null)]
        public bool AutoBusyOff;

        [FeatureConfigOption("Auto trade request", "", 3, null)]
        public bool  AutoTeleportInterupt;
    }

    private bool busyOn = false;

    private IGameObject? player;

    private uint? playerObjectID;
    private Dalamud.Game.ClientState.Objects.Types.IGameObject? master;

    public override string Name => "Auto Busy On Teleport";

    public override string Description => "Fuck you";

    public override FeatureType FeatureType => FeatureType.Actions;

    public Configs? Config { get; private set; }
    public float WeaponElapsed { get; internal set; }
    public float WeaponRemain { get; internal set; }
    public float WeaponTotal { get; internal set; }
    public unsafe float ActionRemain => *(float*)((IntPtr)ActionManager.Instance() + 0x8);
    public float NextAbilityToNextGCD => WeaponRemain - ActionRemain;
    public float CastingTotal { get; internal set; }
    internal bool InPvP => GameMain.IsInPvPArea() || GameMain.IsInPvPInstance();
    public bool IsInSanctuary => GameMain.IsInSanctuary();
    internal float CombatTimeRaw { get; set; }
    private uint? lastTradeTargetId = null;
    private string tradeTargetName = "";

    protected unsafe override DrawConfigDelegate DrawConfigTree => delegate (ref bool hasChanged)
    {
        float tableWidth = ImGui.GetContentRegionAvail().X -10;
        float rowHeight = 30.0f; // Adjust this value as needed for the height of each row
        Vector2 tableSize = new Vector2(tableWidth, rowHeight * 2); // 2 rows
        if (ImGui.BeginTable("AutoBusy header options", 2, ImGuiTableFlags.SizingStretchProp, tableSize))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, ImGui.GetContentRegionAvail().X - 10);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, ImGui.GetContentRegionAvail().X - 10);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (ImGui.Checkbox("Auto Busy on TP", ref Config.AutoBusy))
            {
                hasChanged = true;
            }
            ImGuiHelper.HelpMarker("Set OnlineStatus to Busy when using teleport.");

            ImGui.TableNextColumn();

            if (ImGui.Checkbox("Remove after TP", ref Config.AutoBusyOff))
            {
                hasChanged = true;
            }
            ImGuiHelper.HelpMarker("Set online status back to online after teleporting.");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
/*
            if (ImGui.Checkbox("Auto trade request", ref Config.AutoTeleportInterupt))
            {
                hasChanged = true;
            }
            ImGuiHelper.HelpMarker("You know what this does\n(Range is 5y)");

            ImGui.TableNextColumn();
            if (Config.AutoTeleportInterupt)
            {
                ImGui.Text("Trade Target:");
                ImGui.SameLine();
                ImGui.InputText("##TradeTarget", ref tradeTargetName, 50);
            }
            else
            {
                ImGui.Text(tradeTargetName);
            }
*/
            ImGui.EndTable();
/*
            try
            {
                Dalamud.Game.ClientState.Objects.Types.GameObject lastMaster;
                Dalamud.Game.ClientState.Objects.Types.GameObject target;
                string str;

                lastMaster = Svc.Targets.PreviousTarget;
                var masterObjectID = Svc.Targets?.Target?.ObjectId;
                if (master == null || lastMaster == null)
                {
                    PrintModuleMessage($"Master is null!");
                }
                else if (master)
                {
                    master = Svc.Targets?.Target;
                    masterObjectID = Svc.Targets?.Target?.ObjectId;
                    PrintModuleMessage($"Master is set to {master?.Name}");
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Debug($"{ex}");
            }
*/
        }
        ImGuiHelper.SeperatorWithSpacing();

        if (ImGui.BeginTable("AutoBusy Debug", 2, ImGuiTableFlags.SizingStretchProp, tableSize))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, ImGui.GetContentRegionAvail().X - 10);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, ImGui.GetContentRegionAvail().X - 10);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (Svc.ClientState.LocalPlayer != null)
            {
                ImGuiHelper.AddTableRow("BusyOn: ", Config.AutoBusy.ToString());
                var isBusy = Helpers.Player.Object.OnlineStatus.Id == 12;
                ImGuiHelper.AddTableRow("IsBusy: ", isBusy);

                ImGui.TableNextColumn();

                ImGuiHelper.AddBooleanTableRow("IsCasting Teleport: ", CharacterFunctions.IsCasting(Svc.ClientState.LocalPlayer, (uint)ActionID.Teleport));
                ImGui.TableNextColumn();
                //ImGui.Text($"{lastTradeTargetId.ToString()}");
            }
            ImGui.EndTable();
        }
        ImGuiHelper.SeperatorWithSpacing();
    };

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.Framework.Update += CheckForTeleport;
        Svc.Condition.ConditionChange += CheckForChange;
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        Svc.Framework.Update -= CheckForTeleport;
        Svc.Condition.ConditionChange -= CheckForChange;
        base.Disable();
    }

    private unsafe void CheckForTeleport(IFramework framework)
    {
        var localPlayer = Helpers.Player.Object;
        if (localPlayer == null || localPlayer.Struct()->Character.InCombat)  // if localplayer is null or enganged in combat we return
        {
            return;
        }

        if (Config.AutoTeleportInterupt)
        {
            if (!string.IsNullOrEmpty(tradeTargetName) && tradeTargetName != Svc.Targets.Target?.Name.ToString())
            {
                Svc.Targets.Target = GameObjectHelper.Players.FirstOrDefault(chara =>
                    chara is IBattleChara battleChara &&
                    battleChara.IsCasting &&
                    battleChara.CastActionId == (uint)ActionID.Teleport &&
                    chara.DistanceToPlayerCenter() < 5 &&
                    chara.Name.ToString() == tradeTargetName &&
                    chara.EntityId!= lastTradeTargetId &&
                    Svc.ClientState.LocalPlayer != null);

                if (Svc.Targets.Target != null)
                {
                    Chat.Instance.SendMessage("/e /trade");
                    PrintModuleMessage(tradeTargetName + " is trying to teleport. Let's stop that!");
                    lastTradeTargetId = Svc.Targets.Target.EntityId; // Update the last trade target's object ID
                }
            }
        }
    }

    private unsafe void CheckForChange(ConditionFlag flag, bool value)
    {
        var isBusy = Helpers.Player.Object.OnlineStatus.Id == 12;
        if (Config.AutoBusy)
        {
            if (Config.AutoBusy && Svc.ClientState.LocalPlayer.IsCasting((uint)ActionID.Teleport) && !isBusy)
            {
                Chat.Instance.SendMessage("/busy on");
                busyOn = false;
            }
        }

        if (Config.AutoBusyOff)
        {
            // Check if you are not casting teleport and were previously in the "busy" state
            if (flag == ConditionFlag.Casting && !Helpers.Player.Object.IsCasting((uint)ActionID.Teleport) && isBusy)
            {
                Chat.Instance.SendMessage("/busy off");
                busyOn = false;
            }

            // Check if you are between areas and already in the "busy" state
            if (flag == ConditionFlag.BetweenAreas && isBusy)
            {
                Chat.Instance.SendMessage("/busy off");
                busyOn = false;
            }
        }
    }
}
