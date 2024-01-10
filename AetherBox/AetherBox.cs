using AetherBox.UI;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons;
using ECommons.DalamudServices;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using AetherBox.Features;
using System.Linq;
using System.Reflection;
using ImGuiNET;
using ECommons.Automation;
using Module = ECommons.Module;
using ECommons.ChatMethods;
using System.Windows.Forms;
using ECommons.Logging;
using Microsoft.VisualBasic.ApplicationServices;
using static System.Windows.Forms.LinkLabel;
using AetherBox.IPC;

namespace AetherBox
{
    public class AetherBox : IDalamudPlugin, IDisposable
    {
        private const string CommandName = "/atb";

        internal WindowSystem WindowSystem;

        internal MainWindow MainWindow;

        internal OldMainWindow OldMainWindow;

        internal DebugWindow DebugWindow;

        internal static AetherBox Plugin;

        internal static DalamudPluginInterface PluginInterface;

        public static Configuration ? Config;
        // private static Configuration config;

        // public List<FeatureProvider> FeatureProviders = new List<FeatureProvider>();
        public List<FeatureProvider> FeatureProviders = new List<FeatureProvider>();

        private FeatureProvider provider;

        internal TaskManager TaskManager;

        public static string Name => "AetherBox";

        public IEnumerable<BaseFeature> Features => from x in FeatureProviders.Where((x) => !x.Disposed).SelectMany((x) => x.Features)
                                                    orderby x.Name
                                                    select x;

        /*private List<FeatureProvider> FeatureProviders
        {
            get => featureProviders;
            set => featureProviders = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static Configuration Config
        {
            get => config;
            private set => config = value ?? throw new ArgumentNullException(nameof(value));
        }*/

        public AetherBox(DalamudPluginInterface pluginInterface)
        {
            Plugin = this;
            PluginInterface = pluginInterface;
            InitializePlugin();
        }

        private void InitializePlugin()
        {
            ECommonsMain.Init(PluginInterface, this, Module.DalamudReflector);
            WindowSystem = new WindowSystem(Name);

            SetupWindows();

            SetupCommands();

            PandorasBoxIPC.Init();

            SubscribeToEvents();

            Common.Setup();

            provider = new FeatureProvider(Assembly.GetExecutingAssembly());

            LoadFeatures();
        }

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
            Svc.Commands.AddHandler("/atb", new CommandInfo(OnCommandToggleUI)
            {
                HelpMessage = "Toggles main menu UI\n/atb c → Toggles settings menu\n/atb d → Toggles debug menu\"",
                ShowInHelp = true
            });

            Svc.Commands.AddHandler("/atbdev", new CommandInfo(TestCommand)
            {
                HelpMessage = "Test command - just displays a message.",
                ShowInHelp = true
            });

        }

        // Subscribes to events such as chat commands.
        private void SubscribeToEvents()
        {
            PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleSettingsUI;
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

        public void Dispose()
        {
            DisposePlugin();
        }

        // Collective Dispose method
        private void DisposePlugin()
        {
            PandorasBoxIPC.Dispose();
            UnsubscribeFromEvents();
            UnloadFeatures();                   //AetherBox\AetherBox\AetherBox.cs:line 135
            ClearResources();
            ECommonsMain.Dispose();
            Common.Shutdown();
        }

        // Unsubscribes from events such as chat commands.
        private void UnsubscribeFromEvents()
        {
            PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleSettingsUI;
            Svc.Commands.RemoveHandler("/atb");
            Svc.Commands.RemoveHandler("/atbdev");

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

        /// <summary>
        /// Code to be executed when the window is closed.
        /// </summary>
        /*public static void OnClose()
        {
            try
            {
                AetherBox.Config.InfoSave();
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"{ex},");
            }

        }*/

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

        /// <summary>
        /// Toggle main UI without arguments - Toggle settings UI if command is used with a - 'c'
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        internal void OnCommandToggleUI(string command, string args)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(args))
                {
                    // Toggle main UI
                    MainWindow.IsOpen = !MainWindow.IsOpen;
                }
                else if (args.Equals("c", StringComparison.OrdinalIgnoreCase))
                {
                    // Toggle settings UI
                    OldMainWindow.IsOpen = !OldMainWindow.IsOpen;
                }
                else if (args.Equals("d", StringComparison.OrdinalIgnoreCase))
                {
                    // Toggle settings UI
                    DebugWindow.IsOpen = !DebugWindow.IsOpen;
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"{ex}, Error with '/atb' command");
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
                if (Svc.PluginInterface.IsDevMenuOpen && (Svc.PluginInterface.IsDev || AetherBox.Config.showDebugFeatures))
                {
                    if (ImGui.BeginMainMenuBar())
                    {
                        if (ImGui.MenuItem(AetherBox.Name))
                        {
                            MainWindow.IsOpen = !MainWindow.IsOpen;
                        }
                        ImGui.EndMainMenuBar();
                    }
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Warning($"{ex}, Error in ToggleMainUI");
            }
        }

        /// <summary>
        /// Opens the settings UI window via the 'settings' button in the Plugin Installer Menu
        /// </summary>
        private void ToggleSettingsUI()
        {
            try
            {
                OldMainWindow.IsOpen = !OldMainWindow.IsOpen;
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"{ex}, Error with 'ToggleSettingsUI'");
            }
        }

        /// <summary>
        /// Sends a test message!
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        internal static void TestCommand(string command, string args)
        {
            try
            {
                Svc.Chat.Print("This is a test message!", "AetherBox ", (ushort?)UIColor._color541);
                DuoLog.Information("This is a test message!");
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"{ex}, Error with 'TestCommand'");
            }
        }
    }
}

