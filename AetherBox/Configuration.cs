//using AetherBox.Debugging;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

#nullable disable
namespace AetherBox
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public List<string> EnabledFeatures = new List<string>();
        public bool showDebugFeatures;
        [NonSerialized]
        private DalamudPluginInterface pluginInterface;
        //public DebugConfig Debugging = new DebugConfig();

        public int Version { get; set; }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save() => pluginInterface.SavePluginConfig(this);
    }
}
