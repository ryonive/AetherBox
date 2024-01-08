using AetherBox.FeaturesSetup;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable disable
namespace AetherBox.Features;

public class FeatureProvider : IDisposable
{
    public FeatureProvider(Assembly assembly)
    {
        Assembly = assembly;
    }

    public bool Disposed { get; protected set; }

    public List<BaseFeature> Features { get; }

    public Assembly Assembly { get; init; }

    public virtual void LoadFeatures()
    {
        var types = Assembly.GetTypes();
        if (types == null)
        {
            var errorMessage = "Error loading features from assembly: " + Assembly.GetName();
            Svc.Log.Error(errorMessage);
            return;
        }

        foreach (var type in types.Where(x => x.IsSubclassOf(typeof(Feature)) && !x.IsAbstract))
        {
            try
            {
                var instance = (Feature)Activator.CreateInstance(type);

                if (instance != null)
                {

                    instance.InterfaceSetup(AetherBox.Plugin, AetherBox.pluginInterface, AetherBox.Config, this);
                    instance.Setup();

                    if (instance.Ready && AetherBox.Config.EnabledFeatures.Contains(type.Name) || instance.FeatureType == FeatureType.Commands)
                    {
                        if (instance.FeatureType == FeatureType.Disabled || (instance.isDebug && !AetherBox.Config.showDebugFeatures))
                            instance.Disable();
                        else
                            instance.Enable();
                    }


                    Features.Add(instance); // <---- Feature is null and wont load

                    Svc.Log.Info("Feature loaded successfully: " + type.Name);
                }
                else
                {
                    Svc.Log.Error("Failed to create an instance of feature: " + type.Name);
                }
            }
            catch (Exception ex)
            {
                var messageTemplate = "Feature not loaded: " + type.Name;
                object[] objArray = [];
                Svc.Log.Error(ex, messageTemplate, objArray);
            }
        }
    }

    public void UnloadFeatures()
    {
        foreach (var feature in Features)
        {
            if (feature.Enabled || feature.FeatureType == FeatureType.Commands)
            {
                try
                {
                    feature.Disable();
                }
                catch (Exception ex)
                {
                    var messageTemplate = "Cannot disable " + feature.Name;
                    var objArray = Array.Empty<object>();
                    Svc.Log.Error(ex, messageTemplate, objArray);
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
