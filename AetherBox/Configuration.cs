using System;
using System.Collections.Generic;
using AetherBox.Debugging;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace AetherBox;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }

    public List<string> EnabledFeatures = new List<string>();

    public bool DisabledTheme = false;

    public bool ShowDebugFeatures;

    [NonSerialized]
    private DalamudPluginInterface pluginInterface;

    public DebugConfig Debugging = new DebugConfig();

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        ShowDebugFeatures = false;
    }

    public void Save()
    {
        pluginInterface.SavePluginConfig(this);
    }
}
