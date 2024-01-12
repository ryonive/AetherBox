using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using AetherBox.Features;
using AetherBox.IPC;
using AetherBox.UI;
using Dalamud.Game.Command;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Module = ECommons.Module;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ChatMethods;
using ECommons.Logging;
using ImGuiNET;

namespace AetherBox;

public class AetherBox : IDalamudPlugin, IDisposable
{
    private const string CommandName = "/atb";

    internal WindowSystem WindowSystem;

    internal MainWindow MainWindow;

    internal OldMainWindow OldMainWindow;

    internal DebugWindow DebugWindow;

    internal static global::AetherBox.AetherBox Plugin;

    internal static DalamudPluginInterface PluginInterface;

    public static Configuration Config;

    public List<FeatureProvider> FeatureProviders = new List<FeatureProvider>();

    private FeatureProvider provider;

    internal TaskManager TaskManager;

    [PluginService]
    public static IAddonLifecycle AddonLifecycle { get; private set; }

    public static string Name => "AetherBox";

    public IEnumerable<BaseFeature> Features => from x in FeatureProviders.Where((FeatureProvider x) => !x.Disposed).SelectMany((FeatureProvider x) => x.Features)
                                                orderby x.Name
                                                select x;

    public AetherBox(DalamudPluginInterface pluginInterface)
    {
        Plugin = this;
        PluginInterface = pluginInterface;
        InitializePlugin();
    }

    #region Initialise Phase
    private void InitializePlugin()
    {
        #region Default load order
        ECommonsMain.Init(PluginInterface, Plugin, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);

        #region Initialize Windows
        WindowSystem = new WindowSystem();
        var closeImage = LoadImage("close.png");
        var iconImage = LoadImage("icon.png");
        var bannerImage = LoadImage("banner.png");

        MainWindow = new MainWindow(bannerImage, iconImage);
        OldMainWindow = new OldMainWindow(iconImage, closeImage);
        DebugWindow = new DebugWindow();

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(OldMainWindow);
        WindowSystem.AddWindow(DebugWindow);
        #endregion

        TaskManager = new TaskManager();

        Config = (PluginInterface.GetPluginConfig() as Configuration) ?? new Configuration();

        Config.Initialize(Svc.PluginInterface);

        #region Commands
        Svc.Commands.AddHandler("/AetherBox", new CommandInfo(OnCommand)
        {
            HelpMessage = "This command is used to toggle various UI elements:\n" +
                            "/atb                         → alias for '/Aetherbox' \n" +
                          "/atb menu or m    → Toggles the main menu UI.\n" +
                          "/atb debug or d     → Toggles the debug menu.\n" +
                          "/atb old or o           → Toggles the old main menu UI.",
            ShowInHelp = true,
        });
        Svc.Commands.AddHandler("/aetherbox", new CommandInfo(OnCommand)
        {
            ShowInHelp = false,
        });
        Svc.Commands.AddHandler("/atb", new CommandInfo(OnCommand)
        {
            ShowInHelp = false,
        });
        Svc.Commands.AddHandler("/atbtext", new CommandInfo(TestCommand) // Add a reserved command handler for "/atb text"
        {
            HelpMessage = "Sends a test message in the chatbox.",
            ShowInHelp = false
        });
        #endregion

        PandorasBoxIPC.Init();

        #region Events
        Svc.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += ToggleDebugUI;
        #endregion

        Common.Setup();

        provider = new FeatureProvider(Assembly.GetExecutingAssembly());

        provider.LoadFeatures();
        FeatureProviders.Add(provider);
        #endregion

        //SetupWindows();
        //SetupCommands();
        //SubscribeToEvents();
        //LoadFeatures();
    }
    #endregion

    #region Old Initialise phase
    // Sets up the plugin windows like 'main menu' and the settings window.
    private void SetupWindows()
    {
        var closeImage = LoadImage("close.png");
        var iconImage = LoadImage("icon.png");
        var bannerImage = LoadImage("banner.png");

        MainWindow = new MainWindow(bannerImage, iconImage);
        OldMainWindow = new OldMainWindow(iconImage, closeImage);
        DebugWindow = new DebugWindow();

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(OldMainWindow);
        WindowSystem.AddWindow(DebugWindow);
    }

    // Sets up Commands to toggle the visible state of main menu and settings window.
    private void SetupCommands()
    {
        // Add a command handler for "/AetherBox"
        Svc.Commands.AddHandler("/AetherBox", new CommandInfo(OnCommand)
        {
            HelpMessage = "This command is used to toggle various UI elements:\n" +
                            "/atb                         → alias for '/Aetherbox' \n" +
                          "/atb menu or m    → Toggles the main menu UI.\n" +
                          "/atb debug or d     → Toggles the debug menu.\n" +
                          "/atb old or o           → Toggles the old main menu UI.\n\n",
            ShowInHelp = true,
        });

        Svc.Commands.AddHandler("/aetherbox", new CommandInfo(OnCommand)
        {
            ShowInHelp = false,
        });

        Svc.Commands.AddHandler("/atb", new CommandInfo(OnCommand)
        {
            ShowInHelp = false,
        });

        // Add a reserved command handler for "/atb text"
        Svc.Commands.AddHandler("/atbtext", new CommandInfo(TestCommand)
        {
            HelpMessage = "Sends a test message in the chatbox.\n",
            ShowInHelp = false
        });
    }

    // Subscribes to events such as chat commands.
    private void SubscribeToEvents()
    {
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleDebugUI;
    }

    // Loads all the plugin features like 'Auto Follow'
    private void LoadFeatures()
    {
        if (PluginInterface.GetPluginConfig() is not Configuration configuration)
            configuration = new Configuration();
        Config = configuration;
        Config.Initialize(Svc.PluginInterface);

        provider.LoadFeatures();
        FeatureProviders.Add(provider);
    }
    #endregion

    #region Dispose phase
    public void Dispose()
    {
        Svc.Commands.RemoveHandler("/AetherBox");
        Svc.Commands.RemoveHandler("/aetherbox");
        Svc.Commands.RemoveHandler("/atb");
        Svc.Commands.RemoveHandler("/atbtext");
        foreach (BaseFeature item in Features.Where((BaseFeature x) => x?.Enabled ?? false))
        {
            item.Disable();
        }
        provider.UnloadFeatures();
        PandorasBoxIPC.Dispose();
        Svc.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= ToggleDebugUI;
        WindowSystem.RemoveAllWindows();
        MainWindow = null;
        DebugWindow = null;
        OldMainWindow = null;
        WindowSystem = null;
        ECommonsMain.Dispose();
        FeatureProviders.Clear();
        Common.Shutdown();
        Plugin = null;
    }
    #endregion

    #region Old Dispose Phase
    // Collective Dispose method
    private void DisposePlugin()
    {
        PandorasBoxIPC.Dispose();
        UnsubscribeFromEvents();
        UnloadFeatures();
        ClearResources();
        ECommonsMain.Dispose();
        Common.Shutdown();
    }

    // Unsubscribes from events such as chat commands.
    private void UnsubscribeFromEvents()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleDebugUI;

        Svc.Commands.RemoveHandler("/AetherBox");
        Svc.Commands.RemoveHandler("/aetherbox");
        Svc.Commands.RemoveHandler("/atb");
        Svc.Commands.RemoveHandler("/atbtext");
    }

    // Unloads all the plugin features like 'Auto Follow'
    private void UnloadFeatures()
    {
        if (Features != null)
        {
            foreach (var baseFeature in Features.Where(x => x != null && x.Enabled))
                baseFeature.Disable();
        }
        if (provider != null)
        {
            provider.UnloadFeatures();
        }
    }

    /// <summary>
    /// Unloads all UI elements like the settings menu
    /// </summary>
    private void ClearResources()
    {
        WindowSystem.RemoveAllWindows();
        DebugWindow = null;
        OldMainWindow = null;
        MainWindow = null;
        WindowSystem = null;
    }
    #endregion



    /// <summary>
    /// Toggle main UI without arguments
    /// </summary>
    /// <param name="command"></param>
    /// <param name="args"></param>
    internal void OnCommand(string command, string args)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                // Toggle main UI
                MainWindow.IsOpen = !MainWindow.IsOpen;
            }
            else if (args.Equals("menu", StringComparison.OrdinalIgnoreCase))
            {
                // Toggle main UI
                MainWindow.IsOpen = !MainWindow.IsOpen;
            }
            else if (args.Equals("m", StringComparison.OrdinalIgnoreCase))
            {
                // Toggle main UI
                MainWindow.IsOpen = !MainWindow.IsOpen;
            }
            else if (args.Equals("debug", StringComparison.OrdinalIgnoreCase))
            {
                // Toggle Debug UI
                DebugWindow.IsOpen = !DebugWindow.IsOpen;
            }
            else if (args.Equals("d", StringComparison.OrdinalIgnoreCase))
            {
                // Toggle Debug UI
                DebugWindow.IsOpen = !DebugWindow.IsOpen;
            }
            else if (args.Equals("old", StringComparison.OrdinalIgnoreCase))
            {
                // Toggle OldMainWindow UI
                OldMainWindow.IsOpen = !OldMainWindow.IsOpen;
            }
            else if (args.Equals("o", StringComparison.OrdinalIgnoreCase))
            {
                // Toggle OldMainWindow UI
                OldMainWindow.IsOpen = !OldMainWindow.IsOpen;
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error with 'OnCommandToggleUI' command");
        }
    }

    /// <summary>
    /// Opens the main UI window via the 'Main' button in the Plugin Installer Menu
    /// </summary>
    private void ToggleMainUI()
    {
        try
        {
            MainWindow.IsOpen = !MainWindow.IsOpen;
            if (!Svc.PluginInterface.IsDevMenuOpen || (!Svc.PluginInterface.IsDev && !Config.showDebugFeatures) || !ImGui.BeginMainMenuBar())
            {
                return;
            }
            if (ImGui.MenuItem(Name))
            {
                if (ImGui.GetIO().KeyShift)
                {
                    DebugWindow.IsOpen = !DebugWindow.IsOpen;
                }
                else
                {
                    MainWindow.IsOpen = !MainWindow.IsOpen;
                }
            }
            ImGui.EndMainMenuBar();
        }
        catch (Exception ex)
        {
            Svc.Log.Warning($"{ex}, Error in ToggleMainUI");
        }
    }

    /// <summary>
    /// Opens the settings UI window via the 'settings' button in the Plugin Installer Menu
    /// </summary>
    private void ToggleDebugUI()
    {
        try
        {
            DebugWindow.IsOpen = !DebugWindow.IsOpen;
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error with 'DebugWindow'");
        }
    }

    /// <summary>
    /// Sends a test message , with /atb text
    /// </summary>
    /// <param name="command"></param>
    /// <param name="args"></param>
    internal static void TestCommand(string command, string args)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                Svc.Chat.Print("This is a test message!", "AetherBox ", (ushort?)UIColor._color541);
                DuoLog.Information("This is a test message!");
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error with 'TestCommand'");
        }
    }

    /// <summary>
    /// Loads an image. (note image should be located in the build folder)
    /// </summary>
    /// <param name="imageName"></param>
    /// <returns></returns>
    public static IDalamudTextureWrap LoadImage(string imageName)
    {
        var imagesDirectory = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!);
        var imagePath = Path.Combine(imagesDirectory, imageName);

        if (File.Exists(imagePath))
        {
            try
            {
                return PluginInterface.UiBuilder.LoadImage(imagePath);
            }
            catch (Exception ex)
            {
                Svc.Log.Warning($"{ex}, Error loading image");
                return null;
            }
        }
        else
        {
            Svc.Log.Error($"Image not found: {imagePath}");
            return null;
        }
    }
}