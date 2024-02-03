using System.Reflection;
using AetherBox.Debugging;
using AetherBox.Helpers;
using EasyCombat.UI.Helpers;
using ECommons.Automation;
using ImGuiNET;

namespace AetherBox.Features.Debugging;

public class FeatureDebug : DebugHelper
{
    private readonly FeatureProvider provider = new FeatureProvider(Assembly.GetExecutingAssembly());

    public override string Name => "FeatureDebug".Replace("Debug", "") + " Debugging";

    public override void Draw()
    {
        ImGuiHelper.TextCentered(AetherColor.CyanBright, Name ?? "");
        ImGuiHelper.SeperatorWithSpacing();
        if (ImGui.Button("Load Features"))
        {
            provider.LoadFeatures();
            AetherBox.P.FeatureProviders.Add(provider);
        }
        if (!ImGui.Button("Unload Features"))
        {
            return;
        }
        foreach (BaseFeature item in AetherBox.P.Features.Where((BaseFeature x) => x?.Enabled ?? false))
        {
            item.Disable();
        }
        AetherBox.P.FeatureProviders.Clear();
        provider.UnloadFeatures();

        ImGuiHelper.SeperatorWithSpacing();

        if (ImGui.Button("Jump"))
        {
            BaseFeature.Jump();
        }
        ImGui.SameLine();
        if (ImGui.Button("CD 10"))
        {
            Chat.Instance.SendMessage("/countdown 10");
        }
        ImGui.SameLine();
        if (ImGui.Button("CD cancel"))
        {
            Chat.Instance.SendMessage("/countdown");
        }
        ImGuiHelper.SeperatorWithSpacing();
    }
}
