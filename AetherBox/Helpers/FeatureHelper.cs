using System;
using System.Linq;
using System.Reflection;
using AetherBox;
using AetherBox.Features;
using AetherBox.Features.UI;
using ECommons.Reflection;

namespace AetherBox.Helpers;

public static class FeatureHelper
{
    internal static bool IsBusy
    {
        get
        {
            if (!global::AetherBox.AetherBox.P.TaskManager.IsBusy)
            {
                return WorkshopTurnin.active;
            }
            return true;
        }
    }

    private static bool IsEnabled(BaseFeature feature)
    {
        return global::AetherBox.AetherBox.Config.EnabledFeatures.Contains(feature.GetType().Name);
    }

    public static bool IsEnabled<T>() where T : BaseFeature
    {
        return IsEnabled((T)Activator.CreateInstance((from x in Assembly.GetExecutingAssembly().GetTypes()
                                                      where x == typeof(T)
                                                      select x).First()));
    }

    public static void EnableFeature<T>() where T : BaseFeature
    {
        Type t;
        t = (from x in Assembly.GetExecutingAssembly().GetTypes()
             where x == typeof(T)
             select x).First();
        BaseFeature f;
        f = global::AetherBox.AetherBox.P.Features.Where((BaseFeature x) => x.GetType().Name == t.Name).FirstOrDefault();
        if (f != null && !f.Enabled)
        {
            f.Enable();
        }
    }

    public static void DisableFeature<T>() where T : BaseFeature
    {
        Type t;
        t = (from x in Assembly.GetExecutingAssembly().GetTypes()
             where x == typeof(T)
             select x).First();
        BaseFeature f;
        f = global::AetherBox.AetherBox.P.Features.Where((BaseFeature x) => x.GetType().Name == t.Name).FirstOrDefault();
        if (f != null && f.Enabled)
        {
            f.Disable();
        }
    }

    public static FeatureConfig GetConfig<T>() where T : BaseFeature
    {
        Type t;
        t = (from x in Assembly.GetExecutingAssembly().GetTypes()
             where x == typeof(T)
             select x).First();
        BaseFeature f;
        f = global::AetherBox.AetherBox.P.Features.Where((BaseFeature x) => x.GetType().Name == t.Name).FirstOrDefault();
        if (f != null)
        {
            object config;
            config = f.GetType().GetProperties().FirstOrDefault((PropertyInfo x) => x.PropertyType.IsSubclassOf(typeof(FeatureConfig)))
                .GetValue(f);
            if (config != null)
            {
                return (FeatureConfig)config;
            }
        }
        return null;
    }

    public static bool? IsEnabled(this FeatureConfig config, string propname)
    {
        if (config.GetFoP(propname) != null)
        {
            return (bool)config.GetFoP(propname);
        }
        return null;
    }

    public static void ToggleConfig(this FeatureConfig config, string propName, bool state)
    {
        if (config.GetFoP(propName) != null)
        {
            config.SetFoP(propName, state);
        }
    }
}
