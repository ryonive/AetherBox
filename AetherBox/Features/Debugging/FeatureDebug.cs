using AetherBox.Debugging;
using AetherBox.Helpers;
using ECommons.DalamudServices;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AetherBox.Features.Debugging;

public class FeatureDebug : DebugHelper
{
    private readonly FeatureProvider provider = new FeatureProvider(Assembly.GetExecutingAssembly());

    public override string Name => "FeatureDebug".Replace("Debug", "") + " Debugging";

    public override void Draw()
    {
        ImGui.Text(Name ?? "");
        ImGui.Separator();
        if (ImGui.Button("Load Features"))
        {
            provider.LoadFeatures();
            global::AetherBox.AetherBox.Plugin.FeatureProviders.Add(provider);
        }
        if (!ImGui.Button("Unload Features"))
        {
            return;
        }
        foreach (BaseFeature item in global::AetherBox.AetherBox.Plugin.Features.Where((BaseFeature x) => x?.Enabled ?? false))
        {
            item.Disable();
        }
        global::AetherBox.AetherBox.Plugin.FeatureProviders.Clear();
        provider.UnloadFeatures();
    }
}
