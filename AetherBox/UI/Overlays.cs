using AetherBox.Features;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace AetherBox.UI
{
    internal class Overlays : Window
    {
        private Feature Feature { get; set; }

        public Overlays(Feature t)
            : base($"###Overlay{t.Name}", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding, forceMainWindow: true)
        {
            Feature = t;
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            DisableWindowSounds = true;
            SizeConstraints = new WindowSizeConstraints { MaximumSize = new Vector2(0f, 0f) };
            AetherBox.P?.Ws?.AddWindow(this);
            Position = Vector2.Zero;
        }


        public override void Draw()
        {
            Feature.Draw();
        }

        public override bool DrawConditions()
        {
            return Feature.Enabled;
        }
    }
}
