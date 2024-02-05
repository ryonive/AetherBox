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

namespace AetherBox.Features.Actions;

public class AutoBusyOnTeleport : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Auto Busy on TP", "", 3, null)]
        public bool AutoBusy;

        [FeatureConfigOption("Remove after TP", "", 3, null)]
        public bool AutoBusyOff;

    }
    private bool busyOn = false;

    private Dalamud.Game.ClientState.Objects.Types.GameObject? player;

    private uint? playerObjectID;

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

            ImGui.EndTable();
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
                ImGuiHelper.AddTableRow("IsBusy: ", Svc.ClientState.LocalPlayer.Struct()->Character.CharacterData.OnlineStatus.ToString());

                ImGui.TableNextColumn();

                ImGuiHelper.AddBooleanTableRow("IsCasting Teleport: ", CharacterFunctions.IsCasting(Svc.ClientState.LocalPlayer, (uint)ActionID.Teleport));
                ImGuiHelper.AddTableRow("RemainCastTime: ", WeaponRemain.ToString($"{WeaponRemain}s"));
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
        Svc.Log.Information($"[{Name}] subscribed to event: [CheckForTeleport]'");
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        Svc.Framework.Update -= CheckForTeleport;
        Svc.Condition.ConditionChange -= CheckForChange;
        Svc.Log.Information($"[{Name}] unsubscribed from event: [CheckForTeleport]'");
        base.Disable();
    }

    private unsafe void CheckForTeleport(IFramework framework)
    {
        var localPlayer = Player.Object;
        var instance = ActionManager.Instance();
        var castTotal = localPlayer.TotalCastTime;
        var weaponTotal = instance->GetRecastTime(ActionType.Action, 11);
        if (castTotal > 0) castTotal += 0.1f;
        if (localPlayer.IsCasting) weaponTotal = Math.Max(castTotal, weaponTotal);
        WeaponElapsed = instance->GetRecastTimeElapsed(ActionType.Action, 11);
        WeaponRemain = WeaponElapsed == 0 ? localPlayer.TotalCastTime - localPlayer.CurrentCastTime : Math.Max(weaponTotal - WeaponElapsed, localPlayer.TotalCastTime - localPlayer.CurrentCastTime);

        //Casting time.
        if (WeaponElapsed < 0.3) CastingTotal = castTotal;
        if (weaponTotal > 0 && WeaponElapsed > 0.2) WeaponTotal = weaponTotal;

        if (Svc.ClientState.LocalPlayer == null || Svc.ClientState.LocalPlayer.Struct()->Character.InCombat)
        {

            return;
        }

        if (Config.AutoBusy && CharacterFunctions.IsCasting(Svc.ClientState.LocalPlayer, (uint)ActionID.Teleport) && !busyOn)
        {
            TaskManager.Enqueue(() =>
            {
                Chat.Instance.SendMessage("/busy on");
                busyOn = true;
            });
        }
        else if (Config.AutoBusyOff && !CharacterFunctions.IsCasting(Svc.ClientState.LocalPlayer, (uint)ActionID.Teleport) && busyOn)
        {
            TaskManager.Enqueue(() =>
            {
                Chat.Instance.SendMessage("/busy off");
                busyOn = false;
            });
        }
    }

    private unsafe void CheckForChange(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.BetweenAreas)
        {
            if (busyOn) 
            {
                TaskManager.Enqueue(() =>
                {
                    Chat.Instance.SendMessage("/busy off");
                    busyOn = false;
                });
            }
        }
    }

}
