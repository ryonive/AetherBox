using System;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using ClickLib;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
namespace AetherBox.Features.Experiments;
public class AutoMonsterToss : Feature
{
    public override string Name => "Auto Monster Toss";

    public override string Description => "Auto play the Monster Toss minigame in the Gold Saucer";

    public override FeatureType FeatureType => FeatureType.Other;

    public bool Initialized { get; set; }

    private VirtualKey ConflictKey { get; set; } = VirtualKey.SHIFT;


    public override void Enable()
    {
        base.Enable();
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "BasketBall", OnAddonSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "BasketBall", OnAddonSetup);
        Svc.Framework.Update += OnUpdate;
        Initialized = true;
    }

    public override void Disable()
    {
        base.Disable();
        Svc.Framework.Update -= OnUpdate;
        Svc.AddonLifecycle.UnregisterListener(OnAddonSetup);
        Svc.AddonLifecycle.UnregisterListener(OnAddonSetup);
        TaskManager?.Abort();
        Initialized = false;
    }

    private void OnUpdate(IFramework framework)
    {
        if (TaskManager.IsBusy && Svc.KeyState[ConflictKey])
        {
            TaskManager.Abort();
            Notify.Success("ConflictKey used on AutoMonsterToss");
        }
    }

    private unsafe void OnAddonSetup(AddonEvent type, AddonArgs args)
    {
        switch (type)
        {
            case AddonEvent.PostDraw:
            {
                if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("BasketBall", out var addon) || !GenericHelpers.IsAddonReady(addon))
                {
                    break;
                }
                if (GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out var addonSelectString) && GenericHelpers.IsAddonReady(&addonSelectString->AtkUnitBase))
                {
                    Click.TrySendClick("select_string1");
                    break;
                }

                AtkComponentButton* button = addon->GetButtonNodeById(10u);
                if (button != null && button->IsEnabled)
                {
                    addon->GetNodeById(12u)->ChildNode->PrevSiblingNode->PrevSiblingNode->SetWidth(450);
                    Callback.Fire(addon, true, 11, 1, 0);
                }
                break;
            }
            case AddonEvent.PreFinalize:
                TaskManager.Enqueue((Func<bool?>)StartAnotherRound, (string)null);
                break;
        }
    }

    private unsafe static bool? StartAnotherRound()
    {
        if (GenericHelpers.IsOccupied())
        {
            return false;
        }
        Dalamud.Game.ClientState.Objects.Types.IGameObject machineTarget;
        machineTarget = Svc.Targets.PreviousTarget;
        FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* machine;
        machine = ((machineTarget.DataId == 2004804) ? ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)machineTarget.Address) : ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)null));
        if (machine != null)
        {
            TargetSystem.Instance()->InteractWithObject(machine);
            return true;
        }
        return false;
    }
}
