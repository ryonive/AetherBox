using AetherBox.UI;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using AetherBox.Features;
using System.Linq;
using Lumina.Data.Parsing;
using System.Configuration;
using System.Reflection;
using ImGuiNET;
using ECommons.Automation;

namespace AetherBox;
#nullable disable

/// <summary>
/// The AetherBox class is the main entry point of the plugin, implementing the functionality required for the plugin.
/// </summary>
/// <remarks> This class implements IDalamudPlugin and IDisposable interfaces for integration with the Dalamud plugin architecture and resource management, respectively.</remarks>
public class AetherBox : IDalamudPlugin
{
    private const string CommandName = "/atb";
    internal WindowSystem WindowSystem;
    internal MainWindow MainWindow;

    //internal DebugWindow DebugWindow;
    internal static AetherBox Plugin;

    internal static DalamudPluginInterface pluginInterface;
    public static Configuration Config;
    public List<FeatureProvider> FeatureProviders = new List<FeatureProvider>();
    private FeatureProvider provider;
    internal TaskManager TaskManager;

    public static string Name => nameof(AetherBox);

    public IEnumerable<BaseFeature> Features
    {
        get
        {
            return this.FeatureProviders.Where(x => !x.Disposed).SelectMany(x => x.Features).OrderBy(x => x.Name);
        }
    }

    /// <summary>
    /// Property: Manages the commands within the Dalamud framework for this plugin.
    /// </summary>
    private ICommandManager CommandManager { get; init; }

    /// <summary>
    /// Constructor: Initializes the AetherBox plugin with necessary dependencies.
    /// </example>
    public AetherBox(DalamudPluginInterface pluginInterface, ICommandManager commandManager)
    {
        AetherBox.Plugin = this;
        AetherBox.pluginInterface = pluginInterface;
        this.Initialize();
    }

    private void Initialize()
    {
        ECommonsMain.Init(AetherBox.pluginInterface, Plugin, ECommons.Module.DalamudReflector);
        this.WindowSystem = new WindowSystem();
        var imageClosePath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, "close.png");
        var closeImage = pluginInterface.UiBuilder.LoadImage(imageClosePath);
        var imagePath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, "icon.png");
        var iconImage = pluginInterface.UiBuilder.LoadImage(imagePath);
        this.MainWindow = new MainWindow(Plugin, iconImage, closeImage);
        //this.DebugWindow = new DebugWindow();
        this.WindowSystem.AddWindow((Window)this.MainWindow);
        //this.WindowSystem.AddWindow((Window)this.DebugWindow);
        //this.TaskManager = new TaskManager();
        if (!(AetherBox.pluginInterface.GetPluginConfig() is Configuration configuration))
            configuration = new Configuration();
        AetherBox.Config = configuration;
        AetherBox.Config.Initialize(Svc.PluginInterface);
        Svc.Commands.AddHandler("/atb", new CommandInfo(new CommandInfo.HandlerDelegate(this.OnCommand))
        {
            HelpMessage = "Opens the " + AetherBox.Name + " menu.",
            ShowInHelp = true
        });
        //PandorasBoxIPC.Init();
        Svc.PluginInterface.UiBuilder.Draw += new Action(this.WindowSystem.Draw);
        Svc.PluginInterface.UiBuilder.OpenConfigUi += new Action(this.DrawConfigUI);
        //Common.Setup();
        this.provider = new FeatureProvider(Assembly.GetExecutingAssembly());
        this.provider.LoadFeatures();
        this.FeatureProviders.Add(this.provider);
    }

    public void Dispose()
    {
        MainWindow.IconImage.Dispose();
        MainWindow.CloseButtonTexture.Dispose();
        Svc.Commands.RemoveHandler("/atb");
        foreach (var baseFeature in this.Features.Where(x => x != null && x.Enabled)) baseFeature.Disable();
        this.provider.UnloadFeatures();
        //PandorasBoxIPC.Dispose();
        Svc.PluginInterface.UiBuilder.Draw -= new Action(this.WindowSystem.Draw);
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= new Action(this.DrawConfigUI);
        this.WindowSystem.RemoveAllWindows();
        this.MainWindow = (MainWindow)null;
        //this.DebugWindow = (DebugWindow)null;
        this.WindowSystem = (WindowSystem)null;
        ECommonsMain.Dispose();
        this.FeatureProviders.Clear();
        //Common.Shutdown();
        AetherBox.Plugin = (AetherBox)null;
    }


    private void OnCommand(string command, string args)
    {
        //if ((args == "debug" || args == "d") && AetherBox.Config.showDebugFeatures)
        //this.DebugWindow.IsOpen = !this.DebugWindow.IsOpen;
        //else
        this.MainWindow.IsOpen = !this.MainWindow.IsOpen;
    }

    public void DrawConfigUI()
    {
        try
        {
            this.MainWindow.IsOpen = !this.MainWindow.IsOpen;
            if (!Svc.PluginInterface.IsDevMenuOpen || !Svc.PluginInterface.IsDev && !AetherBox.Config.showDebugFeatures || !ImGui.BeginMainMenuBar())
                return;
            if (ImGui.MenuItem(AetherBox.Name))
            {
                //if (ImGui.GetIO().KeyShift)
                //this.DebugWindow.IsOpen = !this.DebugWindow.IsOpen;
                //else
                this.MainWindow.IsOpen = !this.MainWindow.IsOpen;
            }
            ImGui.EndMainMenuBar();
        }
        catch (Exception ex)
        {
            Svc.Log.Warning($"Error in DrawConfigUI: {ex}");
            // You might want to handle the error differently or log it as an error
        }
    }


    /// <summary>
    /// Method: Loads an image from the 'Images' folder within the assembly directory.
    /// </example>
    public IDalamudTextureWrap LoadImage(string imageName)
    {
        try
        {
            // Assuming the 'Images' folder is in the same directory as the assembly
            var imagesDirectory = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!);
            var imagePath = Path.Combine(imagesDirectory, imageName);

            // Check if the file exists before trying to load it
            if (File.Exists(imagePath))
            {
                try
                {
                    return pluginInterface.UiBuilder.LoadImage(imagePath);
                }
                catch (Exception ex)
                {
                    Svc.Log.Warning($"Error loading image: {ex}");
                    // You might want to return null or handle the error differently here
                    return null;
                }
            }
            else
            {
                // Handle the case where the image does not exist
                // You could log an error or throw an exception, depending on your error handling strategy
                Svc.Log.Error($"Image not found: {imagePath}");
                // You might want to return null or handle the error differently here
                return null;
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning($"Error in LoadImage: {ex}");
            // You might want to return null or handle the error differently here
            return null;
        }
    }

}
