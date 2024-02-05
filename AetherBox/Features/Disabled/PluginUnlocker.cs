using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AetherBox.FeaturesSetup;
using Dalamud.Plugin;
using ECommons.DalamudServices;
namespace AetherBox.Features.Disabled;
public class PluginUnlocker : Feature
{
    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("SortaKinda", "", 0, null, HelpText = "Makes it work in PvP")]
        public bool SortaKinda = true;
    }

    public override string Name => "Plugin Unlocker";

    public override string Description => "Can't stand plugins not working in PvP areas despite having nothing to do with PvP? Me too.";

    public override FeatureType FeatureType => FeatureType.Disabled;

    public Configs Config { get; private set; }

    public override bool UseAutoConfig => true;

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        if (Config.SortaKinda)
        {
            SortaKindaUnlockPvP();
        }
        base.Enable();
    }

    public override void Disable()
    {
        SaveConfig(Config);
        base.Disable();
    }

    internal static void SortaKindaUnlockPvP()
    {
        try
        {
            IDalamudPlugin plugin;
            plugin = GetPluginByName("sortakinda");
            MethodInfo openConfigWindowMethod;
            openConfigWindowMethod = plugin.GetType().GetMethod("OpenConfigWindow");
            if (openConfigWindowMethod != null)
            {
                DynamicMethod newOpenConfigWindowMethod;
                newOpenConfigWindowMethod = new DynamicMethod("OpenConfigWindow", openConfigWindowMethod.ReturnType, (from p in openConfigWindowMethod.GetParameters()
                                                                                                                      select p.ParameterType).ToArray(), openConfigWindowMethod.DeclaringType);
                ILGenerator iLGenerator;
                iLGenerator = newOpenConfigWindowMethod.GetILGenerator();
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Call, plugin.GetType().GetMethod("Toggle"));
                iLGenerator.Emit(OpCodes.Ret);
                plugin.GetType().GetField("OpenConfigWindow", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(plugin, newOpenConfigWindowMethod.CreateDelegate(openConfigWindowMethod.DeclaringType));
            }
        }
        catch (Exception e)
        {
            Svc.Log.Error(e.Message + "\n" + e.StackTrace);
        }
    }

    private static IDalamudPlugin GetPluginByName(string internalName)
    {
        try
        {
            object pluginManager;
            pluginManager = Svc.PluginInterface.GetType().Assembly.GetType("Dalamud.Service`1", throwOnError: true).MakeGenericType(Svc.PluginInterface.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", throwOnError: true)).GetMethod("Get")
                .Invoke(null, BindingFlags.Default, null, Array.Empty<object>(), null);
            foreach (object t in (IList)pluginManager.GetType().GetProperty("InstalledPlugins").GetValue(pluginManager))
            {
                if ((string)t.GetType().GetProperty("Name").GetValue(t) == internalName)
                {
                    IDalamudPlugin plugin;
                    plugin = (IDalamudPlugin)(t.GetType().Name == "LocalDevPlugin" ? t.GetType().BaseType : t.GetType()).GetField("instance", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(t);
                    if ((bool)plugin.GetType().GetField("Init", BindingFlags.Static | BindingFlags.NonPublic).GetValue(plugin))
                    {
                        return plugin;
                    }
                    throw new Exception(internalName + " is not initialized");
                }
            }
            return null;
        }
        catch (Exception e)
        {
            Svc.Log.Error("Can't find " + internalName + " plugin: " + e.Message);
            Svc.Log.Error(e.StackTrace);
            return null;
        }
    }
}
