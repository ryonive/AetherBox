using System;
using System.Numerics;
using AetherBox;
using AetherBox.Features.Actions;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace AetherBox.UI;

public class ConfigWindow : Window, IDisposable
{
    public ConfigWindow Config { get; private set; }

    public ConfigWindow(AetherBox plugin) : base(
        "Wrong lever cronk",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.Always;

        this.Config = plugin.Config;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        /*var configValue = this.Configuration.ADefaultProperty;
        if (ImGui.Checkbox("Random Config Bool", ref configValue))
        {
            this.Configuration.ADefaultProperty = configValue;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            this.Configuration.Save();
        }*/
    }
}
