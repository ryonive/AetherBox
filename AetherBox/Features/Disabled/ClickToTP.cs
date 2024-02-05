using System.Collections.Generic;
using AetherBox.Features.Debugging;
using AetherBox.FeaturesSetup;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ImGuiNET;
namespace AetherBox.Features.Disabled;
public class ClickToTP : CommandFeature
{
    private bool active;

    public override string Name => "Click to TP";

    public override string Command { get; set; } = "/tpclick";


    public override string[] Alias => new string[1] { "/tpc" };

    public override string Description => "";

    public override List<string> Parameters => new List<string> { "" };

    public override bool isDebug => true;

    public override FeatureType FeatureType => FeatureType.Disabled;

    protected override void OnCommand(List<string> args)
    {
        if (!active)
        {
            active = true;
            Svc.Framework.Update += ModifyPOS;
            Svc.Log.Info("Enabling ClickToTP");
        }
        else
        {
            active = false;
            Svc.Framework.Update -= ModifyPOS;
            Svc.Log.Info("Disabling ClickToTP");
        }
    }

    private void ModifyPOS(IFramework framework)
    {
        if (active && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            PositionDebug.SetPosToMouse();
        }
    }
}
