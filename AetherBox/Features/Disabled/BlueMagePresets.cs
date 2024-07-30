using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;
namespace AetherBox.Features.Disabled;
public class BlueMagePresets : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Loadouts")]
        public List<Loadout> Loadouts { get; set; } = new List<Loadout>();

    }

    [Serializable]
    public class Loadout
    {
        public string Name { get; set; }

        public uint[] Actions { get; set; }

        public Loadout(string name = "Unnamed Loadout")
        {
            Name = name;
            Actions = new uint[24];
        }

        public static Loadout FromPreset(string preset)
        {
            try
            {
                byte[] bytes;
                bytes = Convert.FromBase64String(preset);
                return JsonSerializer.Deserialize<Loadout>(Encoding.UTF8.GetString(bytes));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string ToPreset()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this)));
        }

        public int ActionCount(uint id)
        {
            return Actions.Count((x) => x == id);
        }

        public unsafe bool ActionUnlocked(uint id)
        {
            uint normalId;
            normalId = Misc.AozToNormal(id);
            uint link;
            link = Misc.Action.GetRow(normalId).UnlockLink;
            return UIState.Instance()->IsUnlockLinkUnlocked(link);
        }

        public bool CanApply()
        {
            IPlayerCharacter? localPlayer;
            localPlayer = Svc.ClientState.LocalPlayer;
            if ((object)localPlayer == null || localPlayer.ClassJob.Id != 36)
            {
                return false;
            }
            if (Svc.Condition[ConditionFlag.InCombat])
            {
                return false;
            }
            uint[] actions;
            actions = Actions;
            foreach (uint action in actions)
            {
                if (action > Misc.AozAction.RowCount)
                {
                    return false;
                }
                if (action != 0)
                {
                    if (ActionCount(action) > 1)
                    {
                        return false;
                    }
                    if (!ActionUnlocked(action))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public unsafe bool Apply()
        {
            ActionManager* actionManager;
            actionManager = ActionManager.Instance();
            uint[] arr;
            arr = new uint[24];
            for (int i = 0; i < 24; i++)
            {
                arr[i] = Misc.AozToNormal(Actions[i]);
            }
            fixed (uint* ptr = arr)
            {
                if (!actionManager->SetBlueMageActions(ptr))
                {
                    return false;
                }
            }
            return true;
        }

        //private unsafe void ApplyToHotbar(int id, uint[] aozActions)
        //{
        //    RaptureHotbarModule* hotbarModule;
        //    hotbarModule = RaptureHotbarModule.Instance();
        //    for (int i = 0; i < 12; i++)
        //    {
        //        uint normalAction;
        //        normalAction = Misc.AozToNormal(aozActions[i]);
        //        RaptureHotbarModule.Hotbar* slot;
        //        slot = (RaptureHotbarModule.Hotbar*)hotbarModule->GetSlotById((uint)(id - 1), (uint)i);
        //        if (normalAction == 0)
        //        {
        //            slot->(HotbarSlotType.Empty, 0u);
        //        }
        //        else
        //        {
        //            slot->Set(HotbarSlotType.Action, normalAction);
        //        }
        //    }
        //}
    }

    public override string Name => "Blue Mage Presets";

    public override string Description => "There's a good reason the game only gives us 5 spell loadouts to save. Those reasons are as follows:";

    public override FeatureType FeatureType => FeatureType.Disabled;

    public Configs Config { get; private set; }

    public override bool UseAutoConfig => false;

    protected unsafe override DrawConfigDelegate DrawConfigTree => delegate (ref bool hasChanged)
    {
        if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
        {
            Loadout loadout;
            loadout = new Loadout();
            new List<uint>();
            for (int i = 0; i < 24; i++)
            {
                loadout.Actions.SetValue(Misc.NormalToAoz(ActionManager.Instance()->GetActiveBlueMageActionInSlot(i)), i);
            }
            Config.Loadouts.Add(loadout);
            hasChanged = true;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Create preset from current spell loadout.");
        }
        try
        {
            foreach (Loadout current in Config.Loadouts)
            {
                ImGui.Text(current.Name + "##" + current.GetHashCode());
            }
        }
        catch
        {
        }
        if (hasChanged)
        {
            SaveConfig(Config);
        }
    };

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        base.Disable();
    }
}
