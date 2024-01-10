using AetherBox.Features;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

#nullable disable
namespace AetherBox.UI
{
    internal class Overlays : Window
    {
        private Feature Feature { get; set; }

        public Overlays(Feature t)
          : base("###Overlay" + t.Name, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding, true)
        {
            Position = new Vector2?(new Vector2(0.0f, 0.0f));
            Feature = t;
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            SizeConstraints = new Window.WindowSizeConstraints?(new WindowSizeConstraints()
            {
                MaximumSize = new Vector2(0.0f, 0.0f)
            });
            AetherBox.Plugin.WindowSystem.AddWindow(this);
        }

        public override void Draw() => Feature.Draw();

        public override bool DrawConditions() => Feature.Enabled;
    }
}
