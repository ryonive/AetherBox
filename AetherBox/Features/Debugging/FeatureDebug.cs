using System.Reflection;
using AetherBox.Debugging;
using ImGuiNET;

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
            global::AetherBox.AetherBox.P.FeatureProviders.Add(provider);
        }
        if (!ImGui.Button("Unload Features"))
        {
            return;
        }
        foreach (BaseFeature item in global::AetherBox.AetherBox.P.Features.Where((BaseFeature x) => x?.Enabled ?? false))
        {
            item.Disable();
        }
        global::AetherBox.AetherBox.P.FeatureProviders.Clear();
        provider.UnloadFeatures();
    }
}
