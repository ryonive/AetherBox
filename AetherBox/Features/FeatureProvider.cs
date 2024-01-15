using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AetherBox;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using Dalamud.Logging;
using ECommons.DalamudServices;

namespace AetherBox.Features;

public class FeatureProvider : IDisposable
{
    public bool Disposed { get; protected set; }

    public List<BaseFeature> Features { get; } = new List<BaseFeature>();


    public Assembly Assembly { get; init; }

    public FeatureProvider(Assembly assembly)
    {
        Assembly = assembly;
    }

    public virtual void LoadFeatures()
    {
        foreach (Type t in from x in Assembly.GetTypes()
                           where x.IsSubclassOf(typeof(Feature)) && !x.IsAbstract
                           select x)
        {
            try
            {
                Feature feature;
                feature = (Feature)Activator.CreateInstance(t);
                feature.InterfaceSetup(global::AetherBox.AetherBox.P, global::AetherBox.AetherBox.pi, global::AetherBox.AetherBox.Config, this);
                feature.Setup();
                if ((feature.Ready && global::AetherBox.AetherBox.Config.EnabledFeatures.Contains(t.Name)) || feature.FeatureType == FeatureType.Commands)
                {
                    if (feature.FeatureType == FeatureType.Disabled || (feature.isDebug && !global::AetherBox.AetherBox.Config.showDebugFeatures))
                    {
                        feature.Disable();
                    }
                    else
                    {
                        feature.Enable();
                    }
                }
                Features.Add(feature);
            }
            catch (Exception exception)
            {
                PluginLog.Error(exception, "Feature not loaded: " + t.Name);
            }
        }
    }

    public void UnloadFeatures()
    {
        foreach (BaseFeature t in Features)
        {
            if (t.Enabled || t.FeatureType == FeatureType.Commands)
            {
                try
                {
                    t.Disable();
                }
                catch (Exception exception)
                {
                    PluginLog.Error(exception, "Cannot disable " + t.Name);
                }
            }
        }
        Features.Clear();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        UnloadFeatures();
        Disposed = true;
    }
}
