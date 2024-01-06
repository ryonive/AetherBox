using AetherBox.FeaturesSetup;
using Dalamud.Logging;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable disable
namespace AetherBox.Features
{
    public class FeatureProvider : IDisposable
    {
        public FeatureProvider(Assembly assembly)
        {
            // Set the 'Assembly' property with the provided 'assembly' parameter
            this.Assembly = assembly;
        }


        public bool Disposed { get; protected set; }

        public List<BaseFeature> Features { get; }

        public Assembly Assembly { get; init; }

        public virtual void LoadFeatures()
        {
            Type[] types = this.Assembly.GetTypes();
            if (types == null)
            {
                string errorMessage = "Error loading features from assembly: " + Assembly.GetName();
                Svc.Log.Error(errorMessage);
                return;
            }

            foreach (Type type in types.Where(x => x.IsSubclassOf(typeof(Feature)) && !x.IsAbstract))
            {
                try
                {
                    Feature instance = (Feature)Activator.CreateInstance(type);

                    if (instance != null)
                    {
                        instance.InterfaceSetup(AetherBox.Plugin, AetherBox.pluginInterface, AetherBox.Config, this);
                        instance.Setup();

                        if (instance.Ready && AetherBox.Config.EnabledFeatures.Contains(type.Name) || instance.FeatureType == FeatureType.Commands)
                        {
                            if (instance.FeatureType == FeatureType.Disabled || instance.isDebug && !AetherBox.Config.showDebugFeatures)
                                instance.Disable();
                            else
                                instance.Enable();
                        }

                        this.Features.Add((BaseFeature)instance);

                        Svc.Log.Info("Feature loaded successfully: " + type.Name);
                    }
                    else
                    {
                        Svc.Log.Error("Failed to create an instance of feature: " + type.Name);
                    }
                }
                catch (Exception ex)
                {
                    string messageTemplate = "Feature not loaded: " + type.Name;
                    object[] objArray = Array.Empty<object>();
                    PluginLog.Error(ex, messageTemplate, objArray);
                }
            }
        }



        public void UnloadFeatures()
        {
            foreach (BaseFeature feature in this.Features)
            {
                if (feature.Enabled || feature.FeatureType == FeatureType.Commands)
                {
                    try
                    {
                        feature.Disable();
                    }
                    catch (Exception ex)
                    {
                        string messageTemplate = "Cannot disable " + feature.Name;
                        object[] objArray = Array.Empty<object>();
                        PluginLog.Error(ex, messageTemplate, objArray);
                    }
                }
            }
            this.Features.Clear();
        }

        public void Dispose()
        {
            GC.SuppressFinalize((object)this);
            this.UnloadFeatures();
            this.Disposed = true;
        }
    }
}
