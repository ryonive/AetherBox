using System.Reflection;
using AetherBox.Attributes;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.IPC;
using AetherBox.UI;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;

namespace AetherBox;

public class AetherBox : IDalamudPlugin
{
    internal static PluginCommandManager<IDalamudPlugin> ? pluginCommandManager;
    public AetherBox(IDalamudPlugin? plugin) => pluginCommandManager ??= new PluginCommandManager<IDalamudPlugin>(plugin);

    public static string Name => "AetherBox";
    private const string CommandName = "/atb";
    private const string TestCommandName = "/atbtest";

    internal WindowSystem? Ws;
    internal MainWindow? MainWindow;
    internal DebugWindow? DebugWindow;

    internal static AetherBox? P;
    internal static DalamudPluginInterface? pi;
    public static Configuration? Config;
    public List<FeatureProvider> FeatureProviders = new List<FeatureProvider>();
    private FeatureProvider? provider;
    internal TaskManager? TaskManager;


    public IEnumerable<BaseFeature> Features => FeatureProviders.Where(x => !x.Disposed).SelectMany(x => x.Features).OrderBy(x => x.Name);

    [PluginService]
    public static IAddonLifecycle? AddonLifecycle { get; private set; }

    public AetherBox(DalamudPluginInterface pluginInterface)
    {
        P = this;
        pi = pluginInterface;
        if (!pluginInterface.Inject(this))
        {
            Svc.Log.Error("Failed loading AetherBox!");
            return;
        }
        pluginCommandManager ??= new(P);
        InitializePlugin();
    }

    #region Initialise Phase
    private void InitializePlugin()
    {
        #region Default load order
        ECommonsMain.Init(pi, P, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);
        #region Initialize Windows
        var closeImage = LoadImage("close.png");
        var iconImage = LoadImage("icon.png");
        var bannerImage = LoadImage("banner.png");
        Ws = new WindowSystem();
        MainWindow = new MainWindow(bannerImage, iconImage);
        DebugWindow = new DebugWindow();
        Ws.AddWindow(MainWindow);
        Ws.AddWindow(DebugWindow);
        #endregion
        TaskManager = new TaskManager();
        Config = (pi?.GetPluginConfig() as Configuration) ?? new Configuration();
        Config?.Initialize(Svc.PluginInterface);
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
        Svc.PluginInterface.UiBuilder.Draw += Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += ToggleDebugUI;
        #endregion

        Common.Setup();
        PandorasBoxIPC.Init();
        Events.Init();
        AFKTimer.Init();
        provider = new FeatureProvider(Assembly.GetExecutingAssembly());
        provider.LoadFeatures();
        FeatureProviders?.Add(provider);
        #endregion
    }
    #endregion

    #region Dispose phase
    public void Dispose()
    {
        Svc.Commands.RemoveHandler(CommandName);
        Svc.Commands.RemoveHandler(TestCommandName);
        Svc.Log.Debug($"Disabling and Disposing features");
        foreach (BaseFeature item in Features)
        {
            try
            {
                // Disable the feature (if enabled)
                if (item.Enabled)
                {
                    item.Disable();
                }

                // Dispose of the feature
                item.Dispose();

                Svc.Log.Debug($"Feature '{item.Name}' has been disabled and disposed.");
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, $"Error while disposing or disabling feature '{item.Name}'.");
            }
        }
        provider?.UnloadFeatures();
        Svc.PluginInterface.UiBuilder.Draw -= Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= ToggleDebugUI;
        Ws?.RemoveAllWindows();
        MainWindow = null;
        DebugWindow = null;
        Ws = null;
        ECommonsMain.Dispose();
        FeatureProviders?.Clear();
        Common.Shutdown();
        PandorasBoxIPC.Dispose();
        Events.Disable();
        AFKTimer.Dispose();
        P = null;
    }
    #endregion

    /// <summary>
    /// Toggle main UI without arguments
    /// </summary>
    /// <param name="command"></param>
    /// <param name="args"></param>
    private void OnCommand(string command, string args)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(args) || args.Equals("menu", StringComparison.OrdinalIgnoreCase) || args.Equals("m", StringComparison.OrdinalIgnoreCase))
            {
                if (MainWindow != null)
                {
                    // Toggle main UI
                    MainWindow.IsOpen = !MainWindow.IsOpen;
                }

            }
            else if (args.Equals("d", StringComparison.OrdinalIgnoreCase) || args.Equals("debug", StringComparison.OrdinalIgnoreCase))
            {
                if (DebugWindow != null)
                {
                    // Toggle Debug UI
                    DebugWindow.IsOpen = !DebugWindow.IsOpen;
                }

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
                Svc.Chat.Print("This is a test message!", "AetherBox ", (ushort?)ECommons.ChatMethods.UIColor._color541);
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
    public static Dalamud.Interface.Internal.IDalamudTextureWrap LoadImage(string imageName)
    {
        var imagesDirectory = Path.Combine(pi.AssemblyLocation.Directory?.FullName!);
        var imagePath = Path.Combine(imagesDirectory, imageName);

        if (File.Exists(imagePath))
        {
            try
            {
                return pi.UiBuilder.LoadImage(imagePath);
            }
            catch (Exception ex)
            {
                Svc.Log.Warning($"{ex}, Error loading image");
                throw new InvalidOperationException("Error loading image", ex);
            }
        }
        else
        {
            Svc.Log.Error($"Image not found: {imagePath}");
            throw new InvalidOperationException($"Image not found: {imagePath}");
        }
    }
}