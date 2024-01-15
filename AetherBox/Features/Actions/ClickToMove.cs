using System.Numerics;
using AetherBox.Features;
using AetherBox.Features.Actions;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ImGuiNET;

namespace AetherBox.Features.Actions;

public class ClickToMove : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Distance to Keep", "", 1, null, IntMin = 0, IntMax = 30, EditorSize = 300)]
        public VirtualKey keybind;
    }

    private readonly OverrideMovement movement = new OverrideMovement();

    public override string Name => "Click to Move";

    public override string Description => "Like those other games.";

    public override FeatureType FeatureType => FeatureType.Disabled;

    public Configs Config { get; private set; }

    protected override DrawConfigDelegate DrawConfigTree => delegate
    {
    };

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.Framework.Update += MoveTo;
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        Svc.Framework.Update -= MoveTo;
        base.Disable();
    }

    private static bool CheckHotkeyState(VirtualKey key)
    {
        return !Svc.KeyState[key];
    }

    private void MoveTo(IFramework framework)
    {
        if (CheckHotkeyState(VirtualKey.LBUTTON))
        {
            Vector2 mousePos;
            mousePos = ImGui.GetIO().MousePos;
            Svc.GameGui.ScreenToWorld(mousePos, out var pos);
            Svc.Log.Info($"m1 pressed, moving to {pos.X}, {pos.Y}, {pos.Z}");
            movement.DesiredPosition = pos;
        }
    }
}
