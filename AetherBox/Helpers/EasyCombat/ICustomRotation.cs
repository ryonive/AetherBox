using System.ComponentModel;
using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Helpers.EasyCombat;

/// <summary>
/// The custom rotation.
/// </summary>
public interface ICustomRotation : ITexture
{
    /// <summary>
    /// The type of this rotation, pvp, pve, or both.
    /// </summary>
    CombatType Type { get; }

    /// <summary>
    /// Whether show the status in the formal page.
    /// </summary>
    bool ShowStatus { get; }

    /// <summary>
    /// Is this rotation valid.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Why this rotation is not valid.
    /// </summary>
    string WhyNotValid { get; }

    /// <summary>
    /// The class job about this rotation.
    /// </summary>
    ClassJob ClassJob { get; }

    /// <summary>
    /// All jobs.
    /// </summary>
    Job[] Jobs { get; }

    /// <summary>
    /// The game version in writing.
    /// </summary>
    string GameVersion { get; }

    /// <summary>
    /// The name of this rotation.
    /// </summary>
    string RotationName { get; }

    /// <summary>
    /// Configurations about this rotation.
    /// </summary>
    IRotationConfigSet Configs { get; }

    /// <summary>
    /// The type of medicine.
    /// </summary>
    MedicineType MedicineType { get; }

    /// <summary>
    /// All base action.
    /// </summary>
    IBaseAction[] AllBaseActions { get; }

    /// <summary>
    /// All action including base and item.
    /// </summary>
    IAction[] AllActions { get; }

    /// <summary>
    /// All traits.
    /// </summary>
    IBaseTrait[] AllTraits { get; }

    /// <summary>
    /// All bool properties.
    /// </summary>
    //PropertyInfo[] AllBools { get; }

    /// <summary>
    /// All byte properties.
    /// </summary>
    //PropertyInfo[] AllBytesOrInt { get; }

    /// <summary>
    /// All time methods.
    /// </summary>
    //PropertyInfo[] AllFloats { get; }


    internal IAction ActionHealAreaGCD { get; }
    internal IAction ActionHealAreaAbility { get; }
    internal IAction ActionHealSingleGCD { get; }
    internal IAction ActionHealSingleAbility { get; }
    internal IAction ActionDefenseAreaGCD { get; }
    internal IAction ActionDefenseAreaAbility { get; }
    internal IAction ActionDefenseSingleGCD { get; }
    internal IAction ActionDefenseSingleAbility { get; }
    internal IAction ActionMoveForwardGCD { get; }
    internal IAction ActionMoveForwardAbility { get; }
    internal IAction ActionMoveBackAbility { get; }
    internal IAction ActionSpeedAbility { get; }
    internal IAction EsunaStanceNorthGCD { get; }
    internal IAction EsunaStanceNorthAbility { get; }
    internal IAction RaiseShirkGCD { get; }
    internal IAction RaiseShirkAbility { get; }
    internal IAction AntiKnockbackAbility { get; }

    /// <summary>
    /// Try to use this rotation.
    /// </summary>
    /// <param name="newAction">the next action.</param>
    /// <param name="gcdAction">the next gcd action.</param>
    /// <returns>succeed</returns>
    bool TryInvoke(out IAction newAction, out IAction gcdAction);

    /// <summary>
    /// This is an <seealso cref="ImGui"/> method for display the rotation status on Window.
    /// </summary>
    void DisplayStatus();

    /// <summary>
    /// It occur when territory changed or rotation changed.
    /// </summary>
    void OnTerritoryChanged();
}

public interface IBaseTrait : IEnoughLevel, ITexture
{
    /// <summary>
    /// Traid ID
    /// </summary>
    uint ID { get; }
}

public enum MedicineType : byte
{
    /// <summary>
    /// 
    /// </summary>
    Strength,

    /// <summary>
    /// 
    /// </summary>
    Dexterity,

    /// <summary>
    /// 
    /// </summary>
    Intelligence,

    /// <summary>
    /// 
    /// </summary>
    Mind,
}

public interface IBaseAction : IAction
{
    /// <summary>
    /// Is in the mistake actions.
    /// </summary>
    bool IsInMistake { get; set; }

    /// <summary>
    /// Attack Type
    /// </summary>
    AttackType AttackType { get; }

    /// <summary>
    /// Aspect
    /// </summary>
    Aspect Aspect { get; }

    /// <summary>
    /// MP for casting.
    /// </summary>
    uint MPNeed { get; }

    /// <summary>
    /// Casting time
    /// </summary>
    float CastTime { get; }

    /// <summary>
    /// Range of this action.
    /// </summary>
    float Range { get; }

    /// <summary>
    /// Effect range of this action.
    /// </summary>
    float EffectRange { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal bool IsFriendly { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal bool IsEot { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal bool IsHeal { get; }

    /// <summary>
    /// If player has these statuses from player self, this action will not used.
    /// </summary>
    StatusID[] StatusProvide { get; }

    /// <summary>
    /// If player doesn't have these statuses from player self, this action will not used.
    /// </summary>
    StatusID[] StatusNeed { get; }

    /// <summary>
    /// Check for this action, but not for the rotation. It is some additional conditions for this action.
    /// Input data is the target for this action.
    /// </summary>
    Func<BattleChara, bool, bool> ActionCheck { get; }

    /// <summary>
    /// The way to choice the target.
    /// </summary>
    Func<IEnumerable<BattleChara>, bool, BattleChara> ChoiceTarget { get; }

    /// <summary>
    /// The way to choice the target.
    /// </summary>
    Func<IEnumerable<BattleChara>, bool, BattleChara> ChoiceTargetPvP { get; }

    /// <summary>
    /// Is a GCD action.
    /// </summary>
    bool IsRealGCD { get; }

    /// <summary>
    /// Is a simple gcd action, without other cooldown.
    /// </summary>
    bool IsGeneralGCD { get; }

    /// <summary>
    /// The filter for hostiles.
    /// </summary>
    Func<IEnumerable<BattleChara>, IEnumerable<BattleChara>> FilterForHostiles { get; }

    /// <summary>
    /// Is this action a duty action.
    /// </summary>
    bool IsDutyAction { get; }

    /// <summary>
    /// Is this action a LB.
    /// </summary>
    bool IsLimitBreak { get; }

    /// <summary>
    /// Is this action on the slot.
    /// </summary>
    bool IsOnSlot { get; }

    /// <summary>
    /// Can I use this action at this time. It will check a lot of things.
    /// Level, Enabled, Action Status, MP, Player Status, Coll down, Combo, Moving (for casting), Charges, Target, etc.
    /// </summary>
    /// <param name="act"></param>
    /// <param name="option">Options about using this method.</param>
    /// <param name="aoeCount">How many targets do you want this skill to affect</param>
    /// <param name="gcdCountForAbility">The count of gcd for ability to delay. Make it use it earlier when max stack.</param>
    /// <returns>Should I use.</returns>
    bool CanUse(out IAction act, CanUseOption option = CanUseOption.None, byte aoeCount = 0, byte gcdCountForAbility = 0);

    #region CoolDown
    /// <summary>
    /// Current charges count.
    /// </summary>
    ushort CurrentCharges { get; }

    /// <summary>
    /// Max charges count.
    /// </summary>
    ushort MaxCharges { get; }

    /// <summary>
    /// At least has one Charge
    /// </summary>
    bool HasOneCharge { get; }

    /// <summary>
    /// Has it been in cooldown for <paramref name="gcdCount"/> gcds and <paramref name="offset"/> abilities?
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    bool ElapsedOneChargeAfterGCD(uint gcdCount = 0, float offset = 0);

    /// <summary>
    /// Has it been in cooldown for <paramref name="time"/> seconds?
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    bool ElapsedOneChargeAfter(float time);

    /// <summary>
    /// Has it been in cooldown for <paramref name="gcdCount"/> gcds and <paramref name="offset"/> abilities?
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    bool ElapsedAfterGCD(uint gcdCount = 0, float offset = 0);

    /// <summary>
    /// Has it been in cooldown for <paramref name="time"/> seconds?
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    bool ElapsedAfter(float time);

    /// <summary>
    /// Will have at least one charge after <paramref name="gcdCount"/> gcds and <paramref name="offset"/> abilities?
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    bool WillHaveOneChargeGCD(uint gcdCount = 0, float offset = 0);

    /// <summary>
    /// Will have at least one charge after <paramref name="remain"/> seconds?
    /// </summary>
    /// <param name="remain"></param>
    /// <returns></returns>
    bool WillHaveOneCharge(float remain);
    #endregion

    #region Target
    /// <summary>
    /// If target has these statuses from player self, this aciton will not used.
    /// </summary>
    StatusID[] TargetStatus { get; }

    /// <summary>
    /// Action using position.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// The target of this action.   NOTE: Should change name to ActionTarget.
    /// </summary>
    BattleChara Target { get; }

    /// <summary>
    /// Is this action's target type is target only one.
    /// </summary>
    bool IsSingleTarget { get; }

    /// <summary>
    /// How many targets are needed to use this action.
    /// </summary>
    byte AOECount { get; internal set; }

    /// <summary>
    /// How much ttk that this action needs the targets are.
    /// </summary>
    float TimeToKill { get; internal set; }

    /// <summary>
    /// The user set heal ratio.
    /// </summary>
    float AutoHealRatio { get; internal set; }

    /// <summary>
    /// The targets that this action affected on.
    /// </summary>
    BattleChara[] AffectedTargets { get; }

    internal bool FindTarget(bool mustUse, byte aoeCount, out BattleChara target, out BattleChara[] affectedTargets);
    #endregion
}

/// <summary>
/// The options about the method <see cref="IBaseAction.CanUse(out IAction, CanUseOption, byte, byte)"/>.
/// </summary>
[Flags]
public enum CanUseOption : byte
{
    /// <summary>
    /// Nothing serious.
    /// </summary>
    None = 0,

    /// <summary>
    /// AOE only need one target to use.
    /// Moving action don't need to have enough distance to use. 
    /// Skip for StatusProvide and TargetStatus checking.
    /// </summary>
    MustUse = 1 << 0,

    /// <summary>
    /// Use all charges, no keeping one. Do not need to check the combo.
    /// </summary>
    EmptyOrSkipCombo = 1 << 1,

    /// <summary>
    /// Ignore the target data.
    /// </summary>
    IgnoreTarget = 1 << 2,

    /// <summary>
    /// Ignore the check of casting an action while moving.
    /// </summary>
    IgnoreCastCheck = 1 << 3,

    /// <summary>
    /// On the last ability in one GCD.
    /// </summary>
    OnLastAbility = 1 << 4,

    /// <summary>
    /// Ignore clipping check for 0GCDs.
    /// </summary>
    IgnoreClippingCheck = 1 << 5,

    /// <summary>
    /// The combination of <see cref="MustUse"/> and <see cref="EmptyOrSkipCombo"/>
    /// </summary>
    [JsonIgnore]
    MustUseEmpty = MustUse | EmptyOrSkipCombo,
}

public enum Aspect : byte
{
    /// <summary>
    /// 
    /// </summary>
    Fire = 1,

    /// <summary>
    /// 
    /// </summary>
    Ice = 2,

    /// <summary>
    /// 
    /// </summary>
    Wind = 3,

    /// <summary>
    /// 
    /// </summary>
    Earth = 4,

    /// <summary>
    /// 
    /// </summary>
    Lighting = 5,

    /// <summary>
    /// 
    /// </summary>
    Water = 6,

    /// <summary>
    /// 
    /// </summary>
    Piercing = 7,

    /// <summary>
    /// 
    /// </summary>
    None = 7,
}

public interface ITexture
{
    /// <summary>
    /// The icon ID.
    /// </summary>
    uint IconID { get; }

    /// <summary>
    /// The Name about this.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Is this one enabled.
    /// </summary>
    bool IsEnabled { get; set; }
}

/// <summary>
/// The type of the icon
/// </summary>
public enum IconType : byte
{
    /// <summary>
    /// 
    /// </summary>
    Gold,

    /// <summary>
    /// 
    /// </summary>
    Framed,

    /// <summary>
    /// 
    /// </summary>
    Glowing,

    /// <summary>
    /// 
    /// </summary>
    Grey,

    /// <summary>
    /// 
    /// </summary>
    Black,

    /// <summary>
    /// 
    /// </summary>
    Yellow,

    /// <summary>
    /// 
    /// </summary>
    Orange,

    /// <summary>
    /// 
    /// </summary>
    Red,

    /// <summary>
    /// 
    /// </summary>
    Purple,

    /// <summary>
    /// 
    /// </summary>
    Blue,

    /// <summary>
    /// 
    /// </summary>
    Green,

    /// <summary>
    /// 
    /// </summary>
    Role,
}

public enum StatusID : ushort
{
    /// <summary>
    /// 
    /// </summary>
    None = 0,

    /// <summary>
    /// 
    /// </summary>
    Sprint = 50,

    /// <summary>
    /// 
    /// </summary>
    VariantSpiritDart = 3359,

    /// <summary>
    /// 
    /// </summary>
    Reprisal = 1193,

    /// <summary>
    /// 
    /// </summary>
    Thunder = 161,

    /// <summary>
    /// 
    /// </summary>
    Thunder2 = 162,

    /// <summary>
    /// 
    /// </summary>
    Thunder3 = 163,

    /// <summary>
    /// 
    /// </summary>
    Thunder4 = 1210,

    /// <summary>
    /// 
    /// </summary>
    Addle = 1203,

    /// <summary>
    /// 
    /// </summary>
    Bloodbath = 84,

    /// <summary>
    /// 
    /// </summary>
    Feint = 1195,

    #region WHM
    /// <summary>
    /// 
    /// </summary>
    Medica2 = 150,

    /// <summary>
    /// 
    /// </summary>
    TrueMedica2 = 2792,

    /// <summary>
    /// 
    /// </summary>
    Regen1 = 158,

    /// <summary>
    /// 
    /// </summary>
    Regen2 = 897,

    /// <summary>
    /// 
    /// </summary>
    Regen3 = 1330,

    /// <summary>
    /// 
    /// </summary>
    PresenceOfMind = 157,

    /// <summary>
    /// 
    /// </summary>
    ThinAir = 1217,

    /// <summary>
    /// 
    /// </summary>
    DivineBenison = 1218,

    /// <summary>
    /// 
    /// </summary>
    Aero = 143,

    /// <summary>
    /// 
    /// </summary>
    Aero2 = 144,

    /// <summary>
    /// 
    /// </summary>
    Dia = 1871,
    #endregion

    /// <summary>
    /// 
    /// </summary>
    SwiftCast = 167,

    /// <summary>
    /// 
    /// </summary>
    DualCast = 1249,

    /// <summary>
    /// 
    /// </summary>
    TripleCast = 1211,

    /// <summary>
    /// 
    /// </summary>
    SharpCast = 867,

    /// <summary>
    /// 
    /// </summary>
    Thundercloud = 164,

    /// <summary>
    /// 
    /// </summary>
    EnhancedFlare = 2960,

    /// <summary>
    /// 
    /// </summary>
    LeyLines = 737,

    /// <summary>
    /// 
    /// </summary>
    Firestarter = 165,

    /// <summary>
    /// 
    /// </summary>
    Raise = 148,

    /// <summary>
    /// 
    /// </summary>
    Bind1 = 13,

    /// <summary>
    /// 
    /// </summary>
    VerfireReady = 1234,

    /// <summary>
    /// 
    /// </summary>
    VerstoneReady = 1235,

    /// <summary>
    /// 
    /// </summary>
    Acceleration = 1238,

    /// <summary>
    /// 
    /// </summary>
    Doom = 910,

    /// <summary>
    /// 
    /// </summary>
    HallowedGround = 82,

    /// <summary>
    /// 
    /// </summary>
    Holmgang = 409,

    /// <summary>
    /// 
    /// </summary>
    LivingDead = 810,

    /// <summary>
    /// 
    /// </summary>
    WalkingDead = 811,

    /// <summary>
    /// 
    /// </summary>
    SuperBolide = 1836,

    /// <summary>
    /// 
    /// </summary>
    StraightShotReady = 122,

    /// <summary>
    /// 
    /// </summary>
    VenomousBite = 124,

    /// <summary>
    /// 
    /// </summary>
    WindBite = 129,

    /// <summary>
    /// 
    /// </summary>
    CausticBite = 1200,

    /// <summary>
    /// 
    /// </summary>
    StormBite = 1201,

    /// <summary>
    /// 
    /// </summary>
    ShadowBiteReady = 3002,

    /// <summary>
    /// 
    /// </summary>
    BlastArrowReady = 2692,

    /// <summary>
    /// 
    /// </summary>
    RagingStrikes = 125,

    /// <summary>
    /// 
    /// </summary>
    BattleVoice = 141,

    /// <summary>
    /// 
    /// </summary>
    RadiantFinale = 2722,

    /// <summary>
    /// 
    /// </summary>
    ArmyEthos = 1933,

    /// <summary>
    /// 
    /// </summary>
    Rampart = 1191,

    /// <summary>
    /// 
    /// </summary>
    Vengeance = 89,

    /// <summary>
    /// 
    /// </summary>
    Defiance = 91,

    /// <summary>
    /// 
    /// </summary>
    RawIntuition = 735,

    /// <summary>
    /// 
    /// </summary>
    BloodWhetting = 2678,

    /// <summary>
    /// 
    /// </summary>
    InnerRelease = 1177,

    /// <summary>
    /// 
    /// </summary>
    NascentChaos = 1897,

    /// <summary>
    /// 
    /// </summary>
    InnerStrength = 2663,

    /// <summary>
    /// 
    /// </summary>
    SurgingTempest = 2677,

    /// <summary>
    /// 
    /// </summary>
    PrimalRendReady = 2624,

    /// <summary>
    /// 
    /// </summary>
    Combust = 838,

    /// <summary>
    /// 
    /// </summary>
    Combust2 = 843,

    /// <summary>
    /// 
    /// </summary>
    Combust3 = 1881,

    /// <summary>
    /// 
    /// </summary>
    Combust4 = 2041,

    /// <summary>
    /// 
    /// </summary>
    AspectedBenefic = 835,

    /// <summary>
    /// 
    /// </summary>
    AspectedHelios = 836,

    /// <summary>
    /// 
    /// </summary>
    Intersection = 1889,

    /// <summary>
    /// 
    /// </summary>
    Horoscope = 1890,

    /// <summary>
    /// 
    /// </summary>
    HoroscopeHelios = 1891,

    /// <summary>
    /// 
    /// </summary>
    LightSpeed = 841,

    /// <summary>
    /// 
    /// </summary>
    ClarifyingDraw = 2713,

    /// <summary>
    /// 
    /// </summary>
    TheBalance = 1882,

    /// <summary>
    /// 
    /// </summary>
    TheBole = 1883,

    /// <summary>
    /// 
    /// </summary>
    TheArrow = 1884,

    /// <summary>
    /// 
    /// </summary>
    TheSpear = 1885,

    /// <summary>
    /// 
    /// </summary>
    TheEwer = 1886,

    /// <summary>
    /// 
    /// </summary>
    TheSpire = 1887,

    /// <summary>
    /// 
    /// </summary>
    EarthlyDominance = 1224,

    /// <summary>
    /// 
    /// </summary>
    GiantDominance = 1248,

    /// <summary>
    /// 
    /// </summary>
    Troubadour = 1934,

    /// <summary>
    /// 
    /// </summary>
    Tactician1 = 1951,

    /// <summary>
    /// 
    /// </summary>
    Tactician2 = 2177,

    /// <summary>
    /// 
    /// </summary>
    ShieldSamba = 1826,

    /// <summary>
    /// 
    /// </summary>
    DeathsDesign = 2586,

    /// <summary>
    /// 
    /// </summary>
    SoulReaver = 2587,

    /// <summary>
    /// 
    /// </summary>
    EnhancedGibbet = 2588,

    /// <summary>
    /// 
    /// </summary>
    EnhancedGallows = 2589,

    /// <summary>
    /// 
    /// </summary>
    EnhancedVoidReaping = 2590,

    /// <summary>
    /// 
    /// </summary>
    EnhancedCrossReaping = 2591,

    /// <summary>
    /// 
    /// </summary>
    Enshrouded = 2593,

    /// <summary>
    /// 
    /// </summary>
    CircleOfSacrifice = 2972,

    /// <summary>
    /// 
    /// </summary>
    ImmortalSacrifice = 2592,

    /// <summary>
    /// 
    /// </summary>
    BloodSownCircle = CircleOfSacrifice,

    /// <summary>
    /// 
    /// </summary>
    ArcaneCircle = 2599,

    /// <summary>
    /// 
    /// </summary>
    SoulSow = 2594,

    /// <summary>
    /// 
    /// </summary>
    Threshold = 2595,

    /// <summary>
    /// 
    /// </summary>
    EnhancedHarpe = 2845,

    /// <summary>
    /// 
    /// </summary>
    SharperFangandClaw = 802,

    /// <summary>
    /// 
    /// </summary>
    EnhancedWheelingThrust = 803,

    /// <summary>
    /// 
    /// </summary>
    PowerSurge = 2720,

    /// <summary>
    /// 
    /// </summary>
    LifeSurge = 116,

    /// <summary>
    /// 
    /// </summary>
    LanceCharge = 1864,

    /// <summary>
    /// 
    /// </summary>
    DiveReady = 1243,

    /// <summary>
    /// 
    /// </summary>
    RightEye = 1910,

    /// <summary>
    /// 
    /// </summary>
    DraconianFire = 1863,

    /// <summary>
    /// 
    /// </summary>
    ChaoticSpring = 2719,

    /// <summary>
    /// 
    /// </summary>
    ChaosThrust = 118,

    #region MNK
    /// <summary>
    /// 
    /// </summary>
    OpoOpoForm = 107,

    /// <summary>
    /// 
    /// </summary>
    RaptorForm = 108,

    /// <summary>
    /// 
    /// </summary>
    CoerlForm = 109,

    /// <summary>
    /// 
    /// </summary>
    LeadenFist = 1861,

    /// <summary>
    /// 
    /// </summary>
    DisciplinedFist = 3001,

    /// <summary>
    /// 
    /// </summary>
    Demolish = 246,

    /// <summary>
    /// 
    /// </summary>
    PerfectBalance = 110,

    /// <summary>
    /// 
    /// </summary>
    FormlessFist = 2513,

    /// <summary>
    /// 
    /// </summary>
    RiddleOfFire = 1181,

    /// <summary>
    /// 
    /// </summary>
    Brotherhood = 1185,
    #endregion

    /// <summary>
    /// 
    /// </summary>
    SilkenSymmetry = 2693,

    /// <summary>
    /// 
    /// </summary>
    SilkenSymmetry2 = 3017,

    /// <summary>
    /// 
    /// </summary>
    SilkenFlow = 2694,

    /// <summary>
    /// 
    /// </summary>
    SilkenFlow2 = 3018,

    /// <summary>
    /// 
    /// </summary>
    ThreefoldFanDance = 1820,

    /// <summary>
    /// 
    /// </summary>
    FourfoldFanDance = 2699,

    /// <summary>
    /// 
    /// </summary>
    FlourishingStarfall = 2700,

    /// <summary>
    /// 
    /// </summary>
    StandardStep = 1818,

    /// <summary>
    /// 
    /// </summary>
    StandardFinish = 1821,

    /// <summary>
    /// 
    /// </summary>
    TechnicalFinish = 1822,

    /// <summary>
    /// 
    /// </summary>
    TechnicalStep = 1819,

    /// <summary>
    /// 
    /// </summary>
    ClosedPosition1 = 1823,

    /// <summary>
    /// 
    /// </summary>
    ClosedPosition2 = 2026,

    /// <summary>
    /// 
    /// </summary>
    Devilment = 1825,

    /// <summary>
    /// 
    /// </summary>
    FlourishingFinish = 2698,

    /// <summary>
    /// 
    /// </summary>
    Weakness = 43,

    /// <summary>
    /// 
    /// </summary>
    Transcendent = 418,

    /// <summary>
    /// 
    /// </summary>
    BrinkOfDeath = 44,

    /// <summary>
    /// 
    /// </summary>
    Medicated = 49,

    /// <summary>
    /// 
    /// </summary>
    Kardia = 2604,

    /// <summary>
    /// 
    /// </summary>
    Kardion = 2605,

    /// <summary>
    /// 
    /// </summary>
    EukrasianDosis = 2614,

    /// <summary>
    /// 
    /// </summary>
    EukrasianDosis2 = 2615,

    /// <summary>
    /// 
    /// </summary>
    EukrasianDosis3 = 2616,

    /// <summary>
    /// 
    /// </summary>
    EukrasianDiagnosis = 2607,

    /// <summary>
    /// 
    /// </summary>
    EukrasianPrognosis = 2609,

    /// <summary>
    /// 
    /// </summary>
    Kerachole = 2618,

    /// <summary>
    /// 
    /// </summary>
    IronWill = 79,

    /// <summary>
    /// 
    /// </summary>
    Sentinel = 74,

    /// <summary>
    /// 
    /// </summary>
    GoringBlade = 725,

    /// <summary>
    /// 
    /// </summary>
    BladeOfValor = 2721,

    /// <summary>
    /// 
    /// </summary>
    DivineMight = 2673,

    /// <summary>
    /// 
    /// </summary>
    SwordOath = 1902,

    /// <summary>
    /// 
    /// </summary>
    Requiescat = 1368,

    /// <summary>
    /// 
    /// </summary>
    FightOrFlight = 76,

    /// <summary>
    /// 
    /// </summary>
    Grit = 743,

    /// <summary>
    /// 
    /// </summary>
    RoyalGuard = 1833,

    /// <summary>
    /// 
    /// </summary>
    FurtherRuin = 2701,

    /// <summary>
    /// 
    /// </summary>
    SearingLight = 2703,

    /// <summary>
    /// 
    /// </summary>
    IfritsFavor = 2724,

    /// <summary>
    /// 
    /// </summary>
    GarudasFavor = 2725,

    /// <summary>
    /// 
    /// </summary>
    TitansFavor = 2853,

    #region SCH
    /// <summary>
    /// 
    /// </summary>
    Galvanize = 297,

    /// <summary>
    /// 
    /// </summary>
    Dissipation = 791,

    /// <summary>
    /// 
    /// </summary>
    Recitation = 1896,

    /// <summary>
    /// 
    /// </summary>
    Bio = 179,

    /// <summary>
    /// 
    /// </summary>
    Bio2 = 189,

    /// <summary>
    /// 
    /// </summary>
    Biolysis = 1895,

    /// <summary>
    /// 
    /// </summary>
    ChainStratagem = 1221,
    #endregion

    /// <summary>
    /// 
    /// </summary>
    ShadowWall = 747,

    /// <summary>
    /// 
    /// </summary>
    DarkMind = 746,

    /// <summary>
    /// 
    /// </summary>
    SaltedEarth = 749,

    /// <summary>
    /// 
    /// </summary>
    Delirium = 1972,

    /// <summary>
    /// 
    /// </summary>
    BloodWeapon = 742,

    /// <summary>
    /// 
    /// </summary>
    PhantomKamaitachiReady = 2723,

    /// <summary>
    /// 
    /// </summary>
    RaijuReady = 2690,

    /// <summary>
    /// 
    /// </summary>
    Ninjutsu = 496,

    /// <summary>
    /// 
    /// </summary>
    Kassatsu = 497,

    /// <summary>
    /// 
    /// </summary>
    Doton = 501,

    /// <summary>
    /// 
    /// </summary>
    Suiton = 507,

    /// <summary>
    /// 
    /// </summary>
    Hidden = 614,

    /// <summary>
    /// 
    /// </summary>
    Bunshin = 1954,

    /// <summary>
    /// 
    /// </summary>
    TenChiJin = 1186,

    /// <summary>
    /// 
    /// </summary>
    Aurora = 1835,

    /// <summary>
    /// 
    /// </summary>
    Camouflage = 1832,

    /// <summary>
    /// 
    /// </summary>
    Nebula = 1834,

    /// <summary>
    /// 
    /// </summary>
    HeartOfStone = 1840,

    /// <summary>
    /// 
    /// </summary>
    NoMercy = 1831,

    /// <summary>
    /// 
    /// </summary>
    ReadyToRip = 1842,

    /// <summary>
    /// 
    /// </summary>
    ReadyToTear = 1843,

    /// <summary>
    /// 
    /// </summary>
    ReadyToGouge = 1844,

    /// <summary>
    /// 
    /// </summary>
    ReadyToBlast = 2686,

    #region SAM
    /// <summary>
    /// 
    /// </summary>
    Higanbana = 1228,

    /// <summary>
    /// 
    /// </summary>
    MeikyoShisui = 1233,

    /// <summary>
    /// 
    /// </summary>
    EnhancedEnpi = 1236,

    /// <summary>
    /// 
    /// </summary>
    Fugetsu = 1298,

    /// <summary>
    /// 
    /// </summary>
    Fuka = 1299,

    /// <summary>
    /// 
    /// </summary>
    OgiNamikiriReady = 2959,
    #endregion

    /// <summary>
    /// 
    /// </summary>
    Amnesia = 5,

    /// <summary>
    /// 
    /// </summary>
    Stun = 2,

    /// <summary>
    /// 
    /// </summary>
    Sleep = 3,

    /// <summary>
    /// 
    /// </summary>
    Sleep2 = 926,

    /// <summary>
    /// 
    /// </summary>
    Pacification = 6,

    /// <summary>
    /// 
    /// </summary>
    Pacification2 = 620,

    /// <summary>
    /// 
    /// </summary>
    Silence = 7,

    /// <summary>
    /// 
    /// </summary>
    Slow = 9,

    /// <summary>
    /// 
    /// </summary>
    Slow2 = 10,

    /// <summary>
    /// 
    /// </summary>
    Slow3 = 193,

    /// <summary>
    /// 
    /// </summary>
    Slow4 = 561,

    /// <summary>
    /// 
    /// </summary>
    Blind = 15,

    /// <summary>
    /// 
    /// </summary>
    Blind2 = 564,

    /// <summary>
    /// 
    /// </summary>
    Paralysis = 17,

    /// <summary>
    /// 
    /// </summary>
    Paralysis2 = 482,

    /// <summary>
    /// 
    /// </summary>
    Nightmare = 423,

    /// <summary>
    /// 
    /// </summary>
    Patience = 850,

    /// <summary>
    /// 
    /// </summary>
    BattleLitany = 786,

    /// <summary>
    /// 
    /// </summary>
    ReadyForBladeOfFaith = 3019,

    /// <summary>
    /// 
    /// </summary>
    Sheltron = 728,

    /// <summary>
    /// 
    /// </summary>
    HolySheltron = 2674,

    /// <summary>
    /// 
    /// </summary>
    Wildfire = 1946,

    /// <summary>
    /// 
    /// </summary>
    Reassemble = 851,

    /// <summary>
    /// 
    /// </summary>
    TrueNorth = 1250,

    /// <summary>
    /// 
    /// </summary>
    RiddleOfEarth = 1179,

    /// <summary>
    /// 
    /// </summary>
    LucidDreaming = 1204,

    /// <summary>
    /// 
    /// </summary>
    Peloton = 1199,

    /// <summary>
    /// 
    /// </summary>
    Improvisation = 1827,

    /// <summary>
    /// 
    /// </summary>
    _Improvisation = 2695,

    /// <summary>
    /// 
    /// </summary>
    ImprovisedFinish = 2697,

    /// <summary>
    /// 
    /// </summary>
    RisingRhythm = 2696,

    /// <summary>
    /// 
    /// </summary>
    Manafication = 1971,

    /// <summary>
    /// 
    /// </summary>
    Embolden = 2282,

    /// <summary>
    /// 
    /// </summary>
    AethericMimicryTank = 2124,

    /// <summary>
    /// 
    /// </summary>
    AethericMimicryDPS = 2125,

    /// <summary>
    /// 
    /// </summary>
    AethericMimicryHealer = 2126,

    /// <summary>
    /// 
    /// </summary>
    WaxingNocturne = 1718,

    /// <summary>
    /// 
    /// </summary>
    WaningNocturne = 1727,

    /// <summary>
    /// 
    /// </summary>
    BrushWithDeath = 2127,

    /// <summary>
    /// 
    /// </summary>
    Boost = 1716,

    /// <summary>
    /// 
    /// </summary>
    Harmonized = 2118,

    /// <summary>
    /// 
    /// </summary>
    BasicInstinct = 2498,

    /// <summary>
    /// 
    /// </summary>
    Tingling = 2492,

    /// <summary>
    /// 
    /// </summary>
    PhantomFlurry = 2502,

    /// <summary>
    /// 
    /// </summary>
    SurpanakhaFury = 2130,

    /// <summary>
    /// 
    /// </summary>
    Bleeding = 1714,

    /// <summary>
    /// 
    /// </summary>
    DeepFreeze = 1731,

    /// <summary>
    /// 
    /// </summary>
    TouchOfFrost = 2994,

    /// <summary>
    /// 
    /// </summary>
    AuspiciousTrance = 2497,

    /// <summary>
    /// 
    /// </summary>
    Necrosis = 2965,

    /// <summary>
    /// 
    /// </summary>
    MightyGuard = 1719,

    /// <summary>
    /// 
    /// </summary>
    IceSpikes = 1307,

    /// <summary>
    /// 
    /// </summary>
    RespellingSpray = 556,

    /// <summary>
    /// 
    /// </summary>
    Magitek = 2166,

    /// <summary>
    /// 
    /// </summary>
    CircleOfPower = 738,

    /// <summary>
    /// 
    /// </summary>
    Aetherpact = 1223,

    /// <summary>
    /// 
    /// </summary>
    ConfiteorReady = ReadyForBladeOfFaith,

    /// <summary>
    /// 
    /// </summary>
    Bulwark = 77,

    /// <summary>
    /// 
    /// </summary>
    Divination = 1878,

    /// <summary>
    /// 
    /// </summary>
    Mug = 3183,

    /// <summary>
    ///
    /// </summary>
    TrickAttack = 3254,

    /// <summary>
    /// 
    /// </summary>
    Oblation = 2682,

    /// <summary>
    /// 
    /// </summary>
    TheBlackestNight = 1178,

    /// <summary>
    /// 
    /// </summary>
    Overheated = 2688,

    /// <summary>
    /// 
    /// </summary>
    Flamethrower = 1205,

    /// <summary>
    /// 
    /// </summary>
    PassageOfArms = 1175,

    /// <summary>
    /// 
    /// </summary>
    RangedResistance = 941,

    /// <summary>
    /// Invulnerable to ranged attacks.
    /// </summary>
    EnergyField = 584,

    /// <summary>
    /// 
    /// </summary>
    MagicResistance = 942,

    /// <summary>
    /// 
    /// </summary>
    Exaltation = 2717,

    /// <summary>
    /// 
    /// </summary>
    Macrocosmos = 2718,

    /// <summary>
    /// 
    /// </summary>
    CollectiveUnconscious = 849,

    /// <summary>
    /// 
    /// </summary>
    LostSpellforge = 2338,

    /// <summary>
    /// 
    /// </summary>
    MagicalAversion = 2370,

    /// <summary>
    /// 
    /// </summary>
    LostSteelsting = 2339,

    /// <summary>
    /// 
    /// </summary>
    PhysicalAversion = 2369,

    /// <summary>
    /// 
    /// </summary>
    LostRampage = 2559,

    /// <summary>
    /// 
    /// </summary>
    LostBurst = 2558,

    /// <summary>
    /// 
    /// </summary>
    LostBravery = 2341,

    /// <summary>
    /// 
    /// </summary>
    LostProtect = 2333,

    /// <summary>
    /// 
    /// </summary>
    LostShell = 2334,

    /// <summary>
    /// 
    /// </summary>
    LostProtect2 = 2561,

    /// <summary>
    /// 
    /// </summary>
    LostShell2 = 2562,

    /// <summary>
    /// 
    /// </summary>
    LostBubble = 2563,

    /// <summary>
    /// 
    /// </summary>
    LostStoneskin = 151,

    /// <summary>
    /// 
    /// </summary>
    LostFlarestar = 2440,

    /// <summary>
    /// 
    /// </summary>
    LostSeraphStrike = 2484,

    /// <summary>
    /// 
    /// </summary>
    Gobskin = 2114,

    /// <summary>
    /// 
    /// </summary>
    ToadOil = 1737,

    /// <summary>
    /// 
    /// </summary>
    Poison = 18,

    /// <summary>
    /// 
    /// </summary>
    BreathOfMagic = 3712,

    /// <summary>
    /// 
    /// </summary>
    MortalFlame = 3643,

    #region PvP StatusID

    #region General PvP Buffs

    /// <summary>
    ///
    /// </summary>
    PvP_Sprint = 1342,

    /// <summary>
    ///
    /// </summary>
    PvP_Guard = 3054,

    #endregion General PvP Buffs

    #region General PvP DeBuffs

    /// <summary>
    ///
    /// </summary>
    PvP_Stun = 1343,

    /// <summary>
    ///
    /// </summary>
    PvP_Heavy = 1344,

    /// <summary>
    ///
    /// </summary>
    PvP_Bind = 1345,

    /// <summary>
    ///
    /// </summary>
    PvP_Slow = 1346,

    /// <summary>
    ///
    /// </summary>
    PvP_Silence = 1347,

    /// <summary>
    ///
    /// </summary>
    PvP_Sleep = 1348,

    /// <summary>
    ///
    /// </summary>
    PvP_Unguarded = 3021,

    /// <summary>
    ///
    /// </summary>
    PvP_HalfAsleep = 3022,

    /// <summary>
    ///
    /// </summary>
    PvP_DeepFreeze = 3219,

    #endregion General PvP DeBuffs

    #region AST_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_BalanceDrawn = 3101,

    /// <summary>
    ///
    /// </summary>
    PvP_BoleDrawn = 3403,

    /// <summary>
    ///
    /// </summary>
    PvP_ArrowDrawn = 3404,

    /// <summary>
    ///
    /// </summary>
    PvP_Arrow = 3402,

    /// <summary>
    ///
    /// </summary>
    PvP_Balance = 1338,

    /// <summary>
    ///
    /// </summary>
    PvP_Bole = 1339,

    #endregion AST_PvP

    #region BLM_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_AstralFire2 = 3212,

    /// <summary>
    ///
    /// </summary>
    PvP_AstralFire3 = 3213,

    /// <summary>
    ///
    /// </summary>
    PvP_UmbralIce2 = 3214,

    /// <summary>
    ///
    /// </summary>
    PvP_UmbralIce3 = 3215,

    /// <summary>
    ///
    /// </summary>
    PvP_Burst = 3221,

    /// <summary>
    ///
    /// </summary>
    PvP_SoulResonance = 3222,

    /// <summary>
    ///
    /// </summary>
    PvP_Polyglot = 3169,

    /// <summary>
    ///
    /// </summary>
    PvP_AstralWarmth = 3216,

    /// <summary>
    ///
    /// </summary>
    PvP_UmbralFreeze = 3217,

    /// <summary>
    ///
    /// </summary>
    PvP_Burns = 3218,

    #endregion BLM_PvP

    #region BRD_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_FrontlinersMarch = 3138,

    /// <summary>
    ///
    /// </summary>
    PvP_FrontlinersForte = 3140,

    /// <summary>
    ///
    /// </summary>
    PvP_Repertoire = 3137,

    /// <summary>
    ///
    /// </summary>
    PvP_BlastArrowReady = 3142,

    #endregion BRD_PvP

    #region DNC_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_EnAvant = 2048,

    /// <summary>
    ///
    /// </summary>
    PvP_FanDance = 2052,

    /// <summary>
    ///
    /// </summary>
    PvP_Bladecatcher = 3159,

    /// <summary>
    ///
    /// </summary>
    PvP_FlourishingSaberDance = 3160,

    /// <summary>
    ///
    /// </summary>
    PvP_StarfallDance = 3161,

    /// <summary>
    ///
    /// </summary>
    PvP_HoningDance = 3162,

    /// <summary>
    ///
    /// </summary>
    PvP_Acclaim = 3163,

    /// <summary>
    ///
    /// </summary>
    PvP_HoningOvation = 3164,

    #endregion DNC_PvP

    #region DRG_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_FirstmindsFocus = 3178,

    /// <summary>
    ///
    /// </summary>
    PvP_LifeOfTheDragon = 3177,

    /// <summary>
    ///
    /// </summary>
    PvP_Heavensent = 3176,

    #endregion DRG_PvP

    #region DRK_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_BlackestNight = 1308,

    /// <summary>
    ///
    /// </summary>
    PvP_Blackblood = 3033,

    /// <summary>
    ///
    /// </summary>
    PvP_SaltedEarthDMG = 3036,

    /// <summary>
    ///
    /// </summary>
    PvP_SaltedEarthDEF = 3037,

    /// <summary>
    ///
    /// </summary>
    PvP_DarkArts = 3034,

    /// <summary>
    /// DRK's PvP invulnv
    /// </summary>
    PvP_UndeadRedemption = 3039,

    #endregion DRK_PvP

    #region GNB_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_ReadyToRip = 2002,

    /// <summary>
    ///
    /// </summary>
    PvP_ReadyToTear = 2003,

    /// <summary>
    ///
    /// </summary>
    PvP_ReadyToGouge = 2004,

    /// <summary>
    ///
    /// </summary>
    PvP_ReadyToBlast = 3041,

    /// <summary>
    ///
    /// </summary>
    PvP_NoMercy = 3042,

    /// <summary>
    ///
    /// </summary>
    PvP_PowderBarrel = 3043,

    /// <summary>
    ///
    /// </summary>
    PvP_JunctionTank = 3044,

    /// <summary>
    ///
    /// </summary>
    PvP_JunctionDPS = 3045,

    /// <summary>
    ///
    /// </summary>
    PvP_JunctionHealer = 3046,

    #endregion GNB_PvP

    #region MCH_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_HeatStack = 3148,

    /// <summary>
    ///
    /// </summary>
    PvP_Overheated = 3149,

    /// <summary>
    ///
    /// </summary>
    PvP_DrillPrimed = 3150,

    /// <summary>
    ///
    /// </summary>
    PvP_BioblasterPrimed = 3151,

    /// <summary>
    ///
    /// </summary>
    PvP_AirAnchorPrimed = 3152,

    /// <summary>
    ///
    /// </summary>
    PvP_ChainSawPrimed = 3153,

    /// <summary>
    ///
    /// </summary>
    PvP_Analysis = 3158,

    /// <summary>
    ///
    /// </summary>
    PvP_WildfireDebuff = 1323,

    #endregion MCH_PvP	

    #region MNK_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_WindResonance = 2007,

    /// <summary>
    ///
    /// </summary>
    PvP_FireResonance = 3170,

    /// <summary>
    ///
    /// </summary>
    PvP_EarthResonance = 3171,

    /// <summary>
    ///
    /// </summary>
    PvP_PressurePoint = 3172,

    #endregion MNK_PvP

    #region NIN_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_ThreeMudra = 1317,

    /// <summary>
    ///
    /// </summary>
    PvP_Hidden = 1316,

    /// <summary>
    /// 
    /// </summary>
    PvP_FleetingRaijuReady = 3211,

    /// <summary>
    ///
    /// </summary>
    PvP_Bunshin = 2010,

    /// <summary>
    ///
    /// </summary>
    PvP_ShadeShift = 2011,

    /// <summary>
    ///
    /// </summary>
    PvP_SealedHyoshoRanryu = 3194,

    /// <summary>
    ///
    /// </summary>
    PvP_SealedGokaMekkyaku = 3193,

    /// <summary>
    ///
    /// </summary>
    PvP_SealedHuton = 3196,

    /// <summary>
    ///
    /// </summary>
    PvP_SealedDoton = 3197,

    /// <summary>
    ///
    /// </summary>
    PvP_SeakedForkedRaiju = 3195,

    /// <summary>
    ///
    /// </summary>
    PvP_SealedMeisui = 3198,

    #endregion NIN_PvP

    #region PLD_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_HallowedGround = 1302,

    /// <summary>
    /// 
    /// </summary>
    PvP_SwordOath = 1991,

    /// <summary>
    /// 
    /// </summary>
    PvP_Covered = 2413,

    /// <summary>
    /// 
    /// </summary>
    PvP_SacredClaim = 3025,

    /// <summary>
    ///
    /// </summary>
    PvP_HolySheltron = 3026,

    /// <summary>
    ///
    /// </summary>
    PvP_KnightResolve = 3188,

    /// <summary>
    /// 
    /// </summary>
    PvP_BladeofFaith = 3250,

    #endregion PLD_PvP

    #region RDM_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_WhiteShift = 3245,

    /// <summary>
    ///
    /// </summary>
    PvP_BlackShift = 3246,

    /// <summary>
    ///
    /// </summary>
    PvP_Dualcast = 1393,

    /// <summary>
    ///
    /// </summary>
    PvP_EnchantedRiposte = 3234,

    /// <summary>
    ///
    /// </summary>
    PvP_EnchantedRedoublement = 3236,

    /// <summary>
    ///
    /// </summary>
    PvP_EnchantedZwerchhau = 3235,

    /// <summary>
    ///
    /// </summary>
    PvP_VermilionRadiance = 3233,

    /// <summary>
    ///
    /// </summary>
    PvP_MagickBarrier = 3240,

    #endregion RDM_PvP

    #region RPR_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_Soulsow = 2750,

    /// <summary>
    ///
    /// </summary>
    PvP_SoulReaver = 2854,

    /// <summary>
    ///
    /// </summary>
    PvP_GallowsOiled = 2856,

    /// <summary>
    ///
    /// </summary>
    PvP_Enshrouded = 2863,

    /// <summary>
    ///
    /// </summary>
    PvP_ImmortalSacrifice = 3204,

    /// <summary>
    ///
    /// </summary>
    PvP_PlentifulHarvest = 3205,

    /// <summary>
    ///
    /// </summary>
    PvP_DeathWarrant = 3206,

    #endregion RPR_PvP

    #region SAM_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_Kaiten = 3201,

    /// <summary>
    ///
    /// </summary>
    PvP_Midare = 3203,

    /// <summary>
    ///
    /// </summary>
    PvP_Chiten = 1240,

    /// <summary>
    ///
    /// </summary>
    PvP_Kuzushi = 3202,

    #endregion SAM_PvP

    #region SCH_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_Galvanize = 3087,

    /// <summary>
    ///
    /// </summary>
    PvP_Catalyze = 3088,

    /// <summary>
    ///
    /// </summary>
    PvP_Biolysis = 3089,

    /// <summary>
    ///
    /// </summary>
    PvP_Biolytic = 3090,

    /// <summary>
    ///
    /// </summary>
    PvP_Mummification = 3091,

    /// <summary>
    ///
    /// </summary>
    PvP_Expedience = 3092,

    /// <summary>
    ///
    /// </summary>
    PvP_DesperateMeasures = 3093,

    /// <summary>
    ///
    /// </summary>
    PvP_Recitation = 3094,

    /// <summary>
    ///
    /// </summary>
    PvP_SummonSeraph = 3095,

    /// <summary>
    ///
    /// </summary>
    PvP_SeraphFlight = 3096,

    /// <summary>
    ///
    /// </summary>
    PvP_SeraphicVeil = 3097,

    /// <summary>
    ///
    /// </summary>
    PvP_Consolation = 3098,

    #endregion SCH_PvP

    #region SGE_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_Kardia = 2871,

    /// <summary>
    ///
    /// </summary>
    PvP_Kardion = 2872,

    /// <summary>
    ///
    /// </summary>
    PvP_Eukrasia = 3107,

    /// <summary>
    ///
    /// </summary>
    PvP_Addersting = 3115,

    /// <summary>
    ///
    /// </summary>
    PvP_Haima = 3110,

    /// <summary>
    ///
    /// </summary>
    PvP_Haimatinon = 3111,

    /// <summary>
    ///
    /// </summary>
    PvP_EukrasianDosis = 3108,

    /// <summary>
    ///
    /// </summary>
    PvP_Toxicon = 3113,

    #endregion SGE_PvP

    #region SMN_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_FirebirdTrance = 3229,

    /// <summary>
    ///
    /// </summary>
    PvP_DreadwyrmTrance = 3228,

    #endregion SMN_PvP

    #region WAR_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_NascentChaos = 1992,

    /// <summary>
    ///
    /// </summary>
    PvP_InnerRelease = 1303,

    #endregion WAR_PvP

    #region WHM_PvP

    /// <summary>
    ///
    /// </summary>
    PvP_Cure3Ready = 3083,

    #endregion WHM_PvP

    #endregion
}