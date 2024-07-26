using System;
using System.Linq;
using AetherBox;
using AetherBox.Features;
using AetherBox.Features.UI;
using AetherBox.FeaturesSetup;
using ClickLib.Clicks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AetherBox.Features.UI;

public class AutoJoinPF : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("None", "", 1, null)]
        public bool JoinNone;

        [FeatureConfigOption("Duty Roulette", "", 2, null)]
        public bool JoinDutyRoulette;

        [FeatureConfigOption("Dungeons", "", 3, null)]
        public bool JoinDungeons;

        [FeatureConfigOption("Guildhests", "", 4, null)]
        public bool JoinGuildhests;

        [FeatureConfigOption("Trials", "", 5, null)]
        public bool JoinTrials;

        [FeatureConfigOption("Raids", "", 6, null)]
        public bool JoinRaids;

        [FeatureConfigOption("High End Duty", "", 7, null)]
        public bool JoinHighEndDuty;

        [FeatureConfigOption("PvP", "", 8, null)]
        public bool JoinPvP;

        [FeatureConfigOption("Quest Battles", "", 9, null)]
        public bool JoinQuestBattles;

        [FeatureConfigOption("FATEs", "", 10, null)]
        public bool JoinFATEs;

        [FeatureConfigOption("Treasure Hunt", "", 11, null)]
        public bool JoinTreasureHunt;

        [FeatureConfigOption("The Hunt", "", 12, null)]
        public bool JoinTheHunt;

        [FeatureConfigOption("Gathering Forays", "", 13, null)]
        public bool JoinGatheringForays;

        [FeatureConfigOption("Deep Dungeons", "", 14, null)]
        public bool JoinDeepDungeons;

        [FeatureConfigOption("Field Operations", "", 15, null)]
        public bool JoinFieldOperations;

        [FeatureConfigOption("V&C Dungeon Finder", "", 16, null)]
        public bool JoinVCDungeonFinder;
    }

    public readonly struct Categories
    {
        public int IconID { get; }

        public string Name { get; }

        public Func<bool> GetConfigValue { get; }

        public Categories(int iconID, string name, Func<bool> configValue)
        {
            IconID = iconID;
            Name = name;
            GetConfigValue = configValue;
        }
    }

    private readonly Categories[] categories;

    public override string Name => "Auto-Join Party Finder Groups";

    public override string Description => "Whenever you click a Party Finder listing, this will bypass the description window and auto click the join button.";

    public override FeatureType FeatureType => FeatureType.UI;

    public override bool UseAutoConfig => true;

    public Configs Config { get; private set; }

    public AutoJoinPF()
    {
        categories = new Categories[16]
        {
            new Categories(61699, "None", () => Config.JoinNone),
            new Categories(61801, "Dungeons", () => Config.JoinDungeons),
            new Categories(61802, "Raids", () => Config.JoinRaids),
            new Categories(61803, "Guildhests", () => Config.JoinGuildhests),
            new Categories(61804, "Trials", () => Config.JoinTrials),
            new Categories(61805, "Quest Battles", () => Config.JoinQuestBattles),
            new Categories(61806, "PvP", () => Config.JoinPvP),
            new Categories(61807, "Duty Roulette", () => Config.JoinDutyRoulette),
            new Categories(61808, "Treasure Hunt", () => Config.JoinTreasureHunt),
            new Categories(61809, "FATEs", () => Config.JoinFATEs),
            new Categories(61815, "Gathering Forays", () => Config.JoinGatheringForays),
            new Categories(61819, "The Hunt", () => Config.JoinTheHunt),
            new Categories(61824, "Deep Dungeons", () => Config.JoinDeepDungeons),
            new Categories(61832, "High End Duty", () => Config.JoinHighEndDuty),
            new Categories(61837, "Field Operations", () => Config.JoinFieldOperations),
            new Categories(61846, "VC Dungeon Finder", () => Config.JoinVCDungeonFinder)
        };
    }

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        Common.OnAddonSetup += RunFeature;
        Common.OnAddonSetup += ConfirmYesNo;
        base.Enable();
    }

    private unsafe void RunFeature(SetupAddonArgs obj)
    {
        if (!(obj.AddonName != "LookingForGroupDetail"))
        {
            TaskManager.Enqueue(() => new IntPtr(obj.Addon->AtkValues[11].String) != 0);
            TaskManager.Enqueue(delegate
            {
                AutoJoin(obj.Addon);
            });
        }
    }

    private unsafe void AutoJoin(AtkUnitBase* addon)
    {
        if (!IsPrivatePF(addon) && !IsSelfParty(addon) && CanJoinPartyType(GetPartyType(addon)))
        {
            Callback.Fire(addon, false, 0);
        }
    }

    private unsafe bool IsPrivatePF(AtkUnitBase* addon)
    {
        return addon->UldManager.NodeList[111]->IsVisible();
    }

    private unsafe bool IsSelfParty(AtkUnitBase* addon)
    {
        return MemoryHelper.ReadSeStringNullTerminated(new IntPtr(addon->AtkValues[11].String)).ToString() == Svc.ClientState.LocalPlayer.Name.TextValue;
    }

    private unsafe string GetPartyType(AtkUnitBase* addon)
    {
        return categories.FirstOrDefault((Categories x) => x.IconID == addon->AtkValues[16].Int).Name;
    }

    public bool CanJoinPartyType(string categoryName)
    {
        return categories.FirstOrDefault((Categories c) => c.Name == categoryName).GetConfigValue();
    }

    internal unsafe void ConfirmYesNo(SetupAddonArgs obj)
    {
        if (!Svc.Condition[ConditionFlag.Occupied39] && !(obj.AddonName != "SelectYesno") && GenericHelpers.TryGetAddonByName<AtkUnitBase>("LookingForGroupDetail", out var lfgAddon) && lfgAddon->IsVisible() && CanJoinPartyType(GetPartyType(lfgAddon)) && obj.Addon->UldManager.NodeList[15]->IsVisible())
        {
            new ClickSelectYesNo((nint)obj.Addon).Yes();
        }
    }

    public override void Disable()
    {
        SaveConfig(Config);
        Common.OnAddonSetup -= RunFeature;
        Common.OnAddonSetup -= ConfirmYesNo;
        base.Disable();
    }
}
