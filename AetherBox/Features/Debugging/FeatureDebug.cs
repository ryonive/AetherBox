using System.Reflection;
using AetherBox.Debugging;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using EasyCombat.UI.Helpers;
using ECommons.Automation;
using ECommons.DalamudServices;
using ImGuiNET;

namespace AetherBox.Features.Debugging;

public class FeatureDebug : DebugHelper
{
    private readonly FeatureProvider provider = new FeatureProvider(Assembly.GetExecutingAssembly());

    public override string Name => "FeatureDebug".Replace("Debug", "") + " Debugging";

    public override void Draw()
    {
        ImGuiHelper.TextCentered(AetherColor.DarkType, $"{BaseFeature.AetherBoxPayload}\n {Name}" ?? "");
        ImGuiHelper.SeperatorWithSpacing();
        if (ImGui.Button("Load Features"))
        {
            provider.LoadFeatures();
            AetherBox.P.FeatureProviders.Add(provider);
        }
        ImGui.SameLine();

        if (ImGui.Button($"Unload Features"))
        {
            foreach (BaseFeature item in AetherBox.P.Features.Where((BaseFeature x) => x?.Enabled ?? false))
            {
                item.Disable();
                Svc.Log.Debug($"{item.Name} was disabled!");
            }
            AetherBox.P.FeatureProviders.Clear();
            provider.UnloadFeatures();
        }
        ImGuiHelper.SeperatorWithSpacing();

        var enabledFeatures = AetherBox.P.Features
    .Where(feature => feature.Enabled)
    .OrderBy(feature => feature.FeatureType)
    .ThenBy(feature => feature.Name);

        var disabledFeatures = AetherBox.P.Features
    .Where(feature => !feature.Enabled)
    .OrderBy(feature => feature.FeatureType)
    .ThenBy(feature => feature.Name);

        ImGuiHelper.TextUnderlined(AetherColor.Green, $"Enabled Features");
        ImGui.Spacing();
        ImGui.Spacing();
        foreach (BaseFeature item in enabledFeatures)
        {
            string status = item.Enabled ? "Enabled" : "Disabled";
            string featureName = item.Name;
            FeatureType featureType = item.FeatureType;
            string featureTypeName = Enum.GetName(typeof(FeatureType), featureType);

            ImGui.Indent();
            ImGui.TextColored(AetherColor.GreenBright, $"{featureName} - [{featureTypeName}]" ?? "NULL");
            ImGuiHelper.Tooltip($"[{status}] \n{featureName}");
            ImGui.Unindent();
        }

        ImGuiHelper.SeperatorWithSpacing();

        ImGuiHelper.TextUnderlined(AetherColor.RedBright, $"Enabled Features");
        ImGui.Spacing();
        ImGui.Spacing();
        foreach (BaseFeature item in disabledFeatures)
        {
            string status = item.Enabled ? "Enabled" : "Disabled";
            string featureName = item.Name;
            FeatureType featureType = item.FeatureType;
            string featureTypeName = Enum.GetName(typeof(FeatureType), featureType);

            ImGui.Indent();
            ImGui.TextColored(AetherColor.DalamudRed, $"{featureName} - [{featureTypeName}]" ?? "NULL");
            ImGuiHelper.Tooltip($"[{status}] \n{featureName}");
            ImGui.Unindent();
        }




        ImGuiHelper.SeperatorWithSpacing();
    }
}
