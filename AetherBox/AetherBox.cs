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
using System.Reflection;

namespace AetherBox;

public class AetherBox : IDalamudPlugin, IDisposable
{
    public static string Name => "AetherBox";
    private const string CommandName = "/atb";
    private const string TestCommandName = "/atbtest";
    internal WindowSystem WindowSystem;
    internal MainWindow MainWindow;
    internal DebugWindow DebugWindow;

    internal static AetherBox Plugin;
    internal static DalamudPluginInterface PluginInterface;
    internal static Configuration Config;

    public List<FeatureProvider> FeatureProviders = new List<FeatureProvider>();
    private FeatureProvider provider;
    public IEnumerable<BaseFeature> Features => FeatureProviders.Where(x => !x.Disposed).SelectMany(x => x.Features).OrderBy(x => x.Name);
    internal TaskManager TaskManager;

    [PluginService]
    public static IAddonLifecycle AddonLifecycle { get; private set; }

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
        ECommonsMain.Init(PluginInterface, Plugin, Module.DalamudReflector, Module.ObjectFunctions);

        #region Initialize Windows
        var closeImage = LoadImage("close.png");
        var iconImage = LoadImage("icon.png");
        var bannerImage = LoadImage("banner.png");
        WindowSystem = new WindowSystem();
        MainWindow = new MainWindow(bannerImage, iconImage);
        DebugWindow = new DebugWindow();
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(DebugWindow);
        #endregion
        TaskManager = new TaskManager();
        Config = (PluginInterface.GetPluginConfig() as Configuration) ?? new Configuration();
        Config.Initialize(Svc.PluginInterface);
        #region Commands
        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "This command is used to toggle various UI elements:\n" +
                            "/atb                         → alias for '/Aetherbox' \n" +
                          "/atb menu or m    → Toggles the main menu UI.\n" +
                          "/atb debug or d     → Toggles the debug menu.",
            ShowInHelp = true,
        });
        Svc.Commands.AddHandler(TestCommandName, new CommandInfo(TestCommand) // Add a reserved command handler for "/atb text"
        {
            HelpMessage = "Sends a test message in the chatbox.",
            ShowInHelp = false
        });
        #endregion

        #region Events
        Svc.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += ToggleDebugUI;
        #endregion

        Common.Setup();
        PandorasBoxIPC.Init();
        //Events.Init();
        //AFKTimer.Init();
        provider = new FeatureProvider(Assembly.GetExecutingAssembly());
        provider.LoadFeatures();
        FeatureProviders.Add(provider);
        #endregion
    }
    #endregion

    #region Dispose phase
    public void Dispose()
    {
        Svc.Commands.RemoveHandler(CommandName);
        Svc.Commands.RemoveHandler(TestCommandName);

        foreach (BaseFeature item in Features.Where((BaseFeature x) => x?.Enabled ?? false))
        {
            item.Disable();
            Svc.Log.Debug($"Feature '{item.Name}' has been disabled.");
            item.Dispose();
        }
        foreach (var f in Features.Where(x => x is not null && x.Enabled))
        {
            f.Disable();
            Svc.Log.Debug($"Feature '{f.Name}' has been disabled.");
            f.Dispose();
        }

        provider.UnloadFeatures();


        Svc.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= ToggleDebugUI;
        WindowSystem.RemoveAllWindows();
        MainWindow = null;
        DebugWindow = null;
        WindowSystem = null;
        ECommonsMain.Dispose();
        FeatureProviders.Clear();
        Common.Shutdown();
        PandorasBoxIPC.Dispose();
        //Events.Disable();
        //AFKTimer.Dispose();
        Plugin = null;
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
            if (string.IsNullOrWhiteSpace(args) || args.Equals("menu", StringComparison.OrdinalIgnoreCase) || args.Equals("m", StringComparison.OrdinalIgnoreCase))
            {
                // Toggle main UI
                MainWindow.IsOpen = !MainWindow.IsOpen;
            }
            else if (args.Equals("d", StringComparison.OrdinalIgnoreCase) || args.Equals("debug", StringComparison.OrdinalIgnoreCase))
            {
                // Toggle Debug UI
                DebugWindow.IsOpen = !DebugWindow.IsOpen;
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error with 'OnCommand' command");
        }
    }

    /// <summary>
    /// Opens the main UI window via the 'Main' button in the Plugin Installer Menu
    /// </summary>
    public void ToggleMainUI()
    {
        try
        {
            MainWindow.IsOpen = !MainWindow.IsOpen;
        }
        catch (Exception ex)
        {
            Svc.Log.Warning($"{ex}, Error in ToggleMainUI");
        }
    }

    /// <summary>
    /// Opens the settings UI window via the 'settings' button in the Plugin Installer Menu
    /// </summary>
    public void ToggleDebugUI()
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
    private void TestCommand(string command, string args)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                Svc.Chat.Print("This is a test message!", "AetherBox ", (ushort?)UIColor._color541);
                Svc.Log.Debug("This is a test message!");
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