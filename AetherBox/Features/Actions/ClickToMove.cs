using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

#nullable disable
namespace AetherBox.Features.Actions
{
    public class ClickToMove : Feature
    {
        private readonly OverrideMovement movement = new OverrideMovement();

        public override string Name => "Click to Move";

        public override string Description => "Like those other games.";

        public override FeatureType FeatureType => FeatureType.Disabled;

        public ClickToMove.Configs Config { get; private set; }

        protected override BaseFeature.DrawConfigDelegate DrawConfigTree
        {
            get => (BaseFeature.DrawConfigDelegate)((ref bool hasChanged) => { });
        }

        public override void Enable()
        {
            this.Config = this.LoadConfig<ClickToMove.Configs>() ?? new ClickToMove.Configs();
            Svc.Framework.Update += new IFramework.OnUpdateDelegate(this.MoveTo);
            base.Enable();
        }

        public override void Disable()
        {
            this.SaveConfig<ClickToMove.Configs>(this.Config);
            Svc.Framework.Update -= new IFramework.OnUpdateDelegate(this.MoveTo);
            base.Disable();
        }

        private static bool CheckHotkeyState(VirtualKey key) => !Svc.KeyState[key];

        private void MoveTo(IFramework framework)
        {
            if (!ClickToMove.CheckHotkeyState(VirtualKey.LBUTTON))
                return;
            Vector3 worldPos;
            Svc.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out worldPos);
            IPluginLog log = Svc.Log;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 3);
            interpolatedStringHandler.AppendLiteral("m1 pressed, moving to ");
            interpolatedStringHandler.AppendFormatted<float>(worldPos.X);
            interpolatedStringHandler.AppendLiteral(", ");
            interpolatedStringHandler.AppendFormatted<float>(worldPos.Y);
            interpolatedStringHandler.AppendLiteral(", ");
            interpolatedStringHandler.AppendFormatted<float>(worldPos.Z);
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            object[] objArray = Array.Empty<object>();
            log.Info(stringAndClear, objArray);
            this.movement.DesiredPosition = worldPos;
        }

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Distance to Keep", "", 1, null, IntMin = 0, IntMax = 30, EditorSize = 300)]
            public VirtualKey keybind;
        }
    }
}
