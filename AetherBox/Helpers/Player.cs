using ECommons.GameHelpers;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using System.Numerics;
using DGameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using Dalamud.Game;
using Lumina.Excel;
using PlayerController = AetherBox.Helpers.Structs.PlayerController;

#nullable disable

namespace AetherBox.Helpers;

public unsafe static class Player
{
    private delegate void SetPosition(float x, float y, float z);
    private static SetPosition _setPosition = null!;

    public static IPlayerCharacter Object => Svc.ClientState.LocalPlayer;
    public static bool Available => Svc.ClientState.LocalPlayer != null;
    public static bool Interactable => Available && Object.IsTargetable;
    public static bool Occupied => IsOccupied();
    public static ulong CID => Svc.ClientState.LocalContentId;
    public static StatusList Status => Svc.ClientState.LocalPlayer.StatusList;
    public static string Name => Svc.ClientState.LocalPlayer?.Name.ToString();
    public static string NameWithWorld => GetNameWithWorld(Svc.ClientState.LocalPlayer);
    public static int Level => Svc.ClientState.LocalPlayer?.Level ?? 0;
    public static bool IsInHomeWorld => Svc.ClientState.LocalPlayer.HomeWorld.Id == Svc.ClientState.LocalPlayer.CurrentWorld.Id;
    public static string HomeWorld => Svc.ClientState.LocalPlayer?.HomeWorld.GameData.Name.ToString();
    public static string CurrentWorld => Svc.ClientState.LocalPlayer?.CurrentWorld.GameData.Name.ToString();
    public static string HomeDataCenter => GetRow<World>(Svc.ClientState.LocalPlayer.HomeWorld.Id).DataCenter.Value.Name.ToString();
    public static string CurrentDataCenter => GetRow<World>(Svc.ClientState.LocalPlayer.CurrentWorld.Id).DataCenter.Value.Name.ToString();
    public static Character* Character => (Character*)Svc.ClientState.LocalPlayer.Address;
    public static BattleChara* BattleChara => (BattleChara*)Svc.ClientState.LocalPlayer.Address;
    public static CSGameObject* GameObject => (CSGameObject*)Svc.ClientState.LocalPlayer.Address;

    public static PlayerController* Controller => (PlayerController*)Svc.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 3C 01 75 1E 48 8D 0D");
    public static uint Territory => Svc.ClientState.TerritoryType;
    public static bool InDuty => GameMain.Instance()->CurrentContentFinderConditionId != 0;
    public static bool HasPenalty => FFXIVClientStructs.FFXIV.Client.Game.UI.InstanceContent.Instance()->GetPenaltyRemainingInMinutes(0) > 0;
    public static Vector3 Position { get => Object.Position; set => GameObject->SetPosition(value.X, value.Y, value.Z); }
    public static float Speed { get => Controller->MoveControllerWalk.BaseMovementSpeed; set => Debug.SetSpeed(6 * value); }
    public static bool IsMoving => AgentMap.Instance()->IsPlayerMoving == 1;
    public static DGameObject Target { get => Svc.Targets.Target; set => Svc.Targets.Target = value; }
    public static bool IsTargetLocked => *(byte*)((nint)TargetSystem.Instance() + 309) == 1;
    public static bool IsCasting => Object.IsCasting;

    public static Job Job => GetJob(Svc.ClientState.LocalPlayer);
    public static ECommons.ExcelServices.GrandCompany GrandCompany => (ECommons.ExcelServices.GrandCompany)PlayerState.Instance()->GrandCompany;
    public static FlagMapMarker MapFlag => AgentMap.Instance()->FlagMapMarker;
    public static bool OnIsland => MJIManager.Instance()->IsPlayerInSanctuary == 1;

    public static unsafe Camera* Camera => CameraManager.Instance()->GetActiveCamera();
    public static unsafe CameraEx* CameraEx => (CameraEx*)CameraManager.Instance()->GetActiveCamera();

    public static List<MapMarkerData> QuestLocations => FFXIVClientStructs.FFXIV.Client.Game.UI.Map.Instance()->QuestMarkers.ToArray().SelectMany(i => i.MarkerData.ToList()).ToList();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetNameWithWorld(this IPlayerCharacter pc) => pc == null ? null : (pc.Name.ToString() + "@" + pc.HomeWorld.GameData.Name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Job GetJob(this IPlayerCharacter pc) => (Job)pc.ClassJob.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNear(this IPlayerCharacter pc, CSGameObject obj, float distance = 3) => pc.Distance(obj) < distance;
    public static bool IsNear(this IPlayerCharacter pc, DGameObject obj, float distance = 3) => pc.Distance(obj) < distance;
    public static bool IsNear(this IPlayerCharacter pc, Vector3 pos, float distance = 3) => pc.Distance(pos) < distance;
    public static float Distance(this IPlayerCharacter pc, CSGameObject obj) => Vector3.Distance(pc.Position, obj.Position);
    public static float Distance(this IPlayerCharacter pc, DGameObject obj) => Vector3.Distance(pc.Position, obj.Position);
    public static float Distance(this IPlayerCharacter pc, Vector3 pos) => Vector3.Distance(pc.Position, pos);

    private static int EquipAttemptLoops = 0;
    public static void Equip(uint itemID)
    {
        var pos = Inventory.GetItemLocationInInventory(itemID, Inventory.Equippable);
        if (pos == null)
        {
            DuoLog.Error($"Failed to find item {GetRow<Item>(itemID)?.Name} (ID: {itemID}) in inventory");
            return;
        }

        var agentId = Inventory.Armory.Contains(pos.Value.inv) ? AgentId.ArmouryBoard : AgentId.Inventory;
        var addonId = AgentModule.Instance()->GetAgentByInternalId(agentId)->GetAddonId();
        var ctx = AgentInventoryContext.Instance();
        ctx->OpenForItemSlot(pos.Value.inv, pos.Value.slot, addonId);

        var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu");
        if (contextMenu != null)
        {
            for (var i = 0; i < contextMenu->AtkValuesCount; i++)
            {
                var firstEntryIsEquip = ctx->EventIds[i] == 25; // i'th entry will fire eventid 7+i; eventid 25 is 'equip'
                if (firstEntryIsEquip)
                {
                    Svc.Log.Debug($"Equipping item #{itemID} from {pos.Value.inv} @ {pos.Value.slot}, index {i}");
                    Callback.Fire(contextMenu, true, 0, i - 7, 0, 0, 0); // p2=-1 is close, p2=0 is exec first command
                }
            }
            Callback.Fire(contextMenu, true, 0, -1, 0, 0, 0);
            EquipAttemptLoops++;

            if (EquipAttemptLoops >= 5)
            {
                DuoLog.Error($"Equip option not found after 5 attempts. Aborting.");
                return;
            }
        }
    }

    public static bool IsOccupied()
    {
        return Svc.Condition[ConditionFlag.Occupied]
               || Svc.Condition[ConditionFlag.Occupied30]
               || Svc.Condition[ConditionFlag.Occupied33]
               || Svc.Condition[ConditionFlag.Occupied38]
               || Svc.Condition[ConditionFlag.Occupied39]
               || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
               || Svc.Condition[ConditionFlag.OccupiedInEvent]
               || Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
               || Svc.Condition[ConditionFlag.OccupiedSummoningBell]
               || Svc.Condition[ConditionFlag.WatchingCutscene]
               || Svc.Condition[ConditionFlag.WatchingCutscene78]
               || Svc.Condition[ConditionFlag.BetweenAreas]
               || Svc.Condition[ConditionFlag.BetweenAreas51]
               || Svc.Condition[ConditionFlag.InThatPosition]
               || Svc.Condition[ConditionFlag.TradeOpen]
               || Svc.Condition[ConditionFlag.Crafting]
               || Svc.Condition[ConditionFlag.Crafting40]
               || Svc.Condition[ConditionFlag.PreparingToCraft]
               || Svc.Condition[ConditionFlag.InThatPosition]
               || Svc.Condition[ConditionFlag.Unconscious]
               || Svc.Condition[ConditionFlag.MeldingMateria]
               || Svc.Condition[ConditionFlag.Gathering]
               || Svc.Condition[ConditionFlag.OperatingSiegeMachine]
               || Svc.Condition[ConditionFlag.CarryingItem]
               || Svc.Condition[ConditionFlag.CarryingObject]
               || Svc.Condition[ConditionFlag.BeingMoved]
               || Svc.Condition[ConditionFlag.Mounted2]
               || Svc.Condition[ConditionFlag.Mounting]
               || Svc.Condition[ConditionFlag.Mounting71]
               || Svc.Condition[ConditionFlag.ParticipatingInCustomMatch]
               || Svc.Condition[ConditionFlag.PlayingLordOfVerminion]
               || Svc.Condition[ConditionFlag.ChocoboRacing]
               || Svc.Condition[ConditionFlag.PlayingMiniGame]
               || Svc.Condition[ConditionFlag.Performing]
               || Svc.Condition[ConditionFlag.PreparingToCraft]
               || Svc.Condition[ConditionFlag.Fishing]
               || Svc.Condition[ConditionFlag.Transformed]
               || Svc.Condition[ConditionFlag.UsingHousingFunctions]
               || Svc.ClientState.LocalPlayer?.IsTargetable != true;
    }

    public static ExcelSheet<T> GetSheet<T>(ClientLanguage? language = null) where T : ExcelRow
        => Svc.Data.GetExcelSheet<T>(language ?? Svc.ClientState.ClientLanguage)!;

    public static T? GetRow<T>(uint rowId, uint subRowId = uint.MaxValue, ClientLanguage? language = null) where T : ExcelRow
        => GetSheet<T>(language).GetRow(rowId, subRowId);
}
