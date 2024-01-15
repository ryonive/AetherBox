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
            : base("###Overlay" + t.Name, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding, forceMainWindow: true)
        {
            base.Position = new Vector2(0f, 0f);
            Feature = t;
            base.IsOpen = true;
            base.ShowCloseButton = false;
            base.RespectCloseHotkey = false;
            base.DisableWindowSounds = true;
            base.SizeConstraints = new WindowSizeConstraints
            {
                MaximumSize = new Vector2(0f, 0f)
            };
            global::AetherBox.AetherBox.P.Ws.AddWindow(this);
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
