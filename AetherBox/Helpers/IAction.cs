using System.ComponentModel;

namespace AetherBox.Helpers;

/// <summary>
/// The action.
/// </summary>
public interface IAction : ITexture, IEnoughLevel
{
    /// <summary>
    /// ID of this action.
    /// </summary>
    uint ID { get; }

    /// <summary>
    /// The adjusted Id of this action.
    /// </summary>
    uint AdjustedID { get; }

    /// <summary>
    /// The animation lock time of this action.
    /// </summary>
    float AnimationLockTime { get; }

    /// <summary>
    /// The key of sorting this action.
    /// </summary>
    uint SortKey { get; }

    /// <summary>
    /// 
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal float RecastTimeElapsedRaw { get; }

    /// <summary>
    /// Gets the raw recast time in seconds for a charge of this action.
    /// </summary>
    /// <remarks>
    /// This property retrieves the raw recast time for the next charge of an action of ActionType Action,
    /// using the AdjustedID property as the action's identifier. The recast time is expressed in seconds.
    /// </remarks>
    /// <returns>The raw recast time in seconds for the next charge of an action.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal float RecastTimeOneChargeRaw { get; }

    /// <summary>
    /// Displays the Total recast time (However once Action is no longer in cooldown, just displays 0. Therefore we dont use it until i figure out why)
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal float RecastTimeRaw { get; }

    /// <summary>
    /// Remaining Recast Time for this action.
    /// </summary>
    public float RemainingRecastTime { get; }

    /// <summary>
    /// Is action cooling down.
    /// </summary>
    bool IsCoolingDown { get; }

    /// <summary>
    /// Is in the cd window.
    /// </summary>
    bool IsInCooldown { get; set; }

    /// <summary>
    /// How to use.
    /// </summary>
    /// <returns></returns>
    bool Use();
}

public interface IEnoughLevel
{
    /// <summary>
    /// Player's level is enough for this action's usage.
    /// </summary>
    bool EnoughLevel { get; }

    internal byte Level { get; }
}
