using System;
using System.Numerics;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using AetherBox.UI;
using ClickLib.Clicks;
using Dalamud.Interface.Utility;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Throttlers;
using ECommons.UIHelpers.Implementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace AetherBox.Features.UI;

public class MarketUpdater : Feature
{
    private Overlays overlay;

    private float height;

    internal bool active;

    public override string Name => "Market Updater";

    public override string Description => "Penny pinches all listings on retainers";

    public override FeatureType FeatureType => FeatureType.Disabled;

    internal new static bool GenericThrottle => FrameThrottler.Throttle("AutoRetainerGenericThrottle", 200);

    public override void Enable()
    {
        overlay = new Overlays(this);
        base.Enable();
    }

    public override void Disable()
    {
        P.Ws.RemoveWindow(overlay);
        base.Disable();
    }

    public unsafe override void Draw()
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon) || addon->UldManager.NodeListCount <= 1 || !addon->UldManager.NodeList[1]->IsVisible())
        {
            return;
        }
        AtkResNode* node;
        node = addon->UldManager.NodeList[1];
        if (!node->IsVisible())
        {
            return;
        }
        AtkResNodeHelper.GetNodePosition(node);
        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(addon->X, (float)addon->Y - height));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(10f, 10f));
        ImGui.Begin($"###{Name}{node->NodeId}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoNavFocus);
        if (ImGui.Button((!active) ? (Name + "###Start") : "Running. Click to abort.###Abort"))
        {
            if (!active)
            {
                active = true;
                TaskManager.Enqueue((Action)YesAlready.DisableIfNeeded, (string)null);
                TaskManager.Enqueue(delegate
                {
                    UpdateListings((int)addon->AtkValues[2].UInt);
                });
            }
            else
            {
                CancelLoop();
            }
        }
        height = ImGui.GetWindowSize().Y;
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void CancelLoop()
    {
        active = false;
        TaskManager.Abort();
        TaskManager.Enqueue((Action)YesAlready.EnableIfNeeded, (string)null);
    }

    private void UpdateListings(int numRetainers)
    {
        int i;
        i = 0;
        if (i >= numRetainers)
        {
            TaskManager.Enqueue(() => active = false);
            TaskManager.Enqueue((Action)YesAlready.EnableIfNeeded, (string)null);
        }
    }

    internal unsafe static bool? SelectRetainerByName(string name)
    {
        if (name.IsNullOrEmpty())
        {
            throw new Exception("Name can not be null or empty");
        }
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerList", out var retainerList) && GenericHelpers.IsAddonReady(retainerList))
        {
            ReaderRetainerList list;
            list = new ReaderRetainerList(retainerList);
            for (int i = 0; i < list.Retainers.Count; i++)
            {
                if (list.Retainers[i].Name == name && GenericThrottle)
                {
                    PluginLog.Debug($"Selecting retainer {list.Retainers[i].Name} with index {i}");
                    ClickRetainerList.Using((nint)retainerList).Retainer(i);
                    return true;
                }
            }
        }
        return false;
    }
}
