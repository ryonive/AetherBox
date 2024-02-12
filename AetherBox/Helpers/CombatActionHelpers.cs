using AetherBox.Helpers.EasyCombat;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace AetherBox.Helpers;

internal class CombatActionHelpers
{
    public string Name => _action.Name;

    /// <summary> ID of this action. </summary>
    private uint ID { get; }

    /// <summary> The adjusted Id of this action. </summary>
    private uint AdjustedID { get; }

    /// <summary> The key of sorting this action. </summary>
    private uint SortKey { get; }

    protected readonly Action _action;

    public float AnimationLockTime => Common.AnimationLockTime?.TryGetValue(AdjustedID, out var time) ?? false ? time : 0.6f;

    private byte CoolDownGroup { get; }

    private unsafe RecastDetail* CoolDownDetail => ActionManager.Instance()->GetRecastGroupDetail(CoolDownGroup - 1);

    private unsafe float RecastTimeElapsedRaw => CoolDownDetail == null ? 0 : CoolDownDetail->Elapsed;

    private unsafe float RecastTime => CoolDownDetail == null ? 0 : CoolDownDetail->Total;

    private float RecastTimeRemain => RecastTime - RecastTimeElapsedRaw;

    public float RemainingRecastTime => RecastTimeRemain;
}

