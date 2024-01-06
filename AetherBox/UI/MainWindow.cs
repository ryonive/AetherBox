using AetherBox;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers.Extensions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AetherBox.UI;

public class MainWindow : Window
{
    internal readonly IDalamudTextureWrap ? IconImage;
    internal readonly IDalamudTextureWrap ? CloseButtonTexture;
    internal readonly AetherBox Plugin;
    private static float Scale => ImGuiHelpers.GlobalScale;

    public bool IsCategoryInfoOpen { get; set; } = false;

    private bool isCategorySettingsOpen = false;
    private string? selectedCategory;

    private string ? searchString;
    private readonly List<BaseFeature> ? filteredFeatures;
    private bool hornybonk;
    public OpenWindow OpenWindow { get; private set; }

    public MainWindow(
        AetherBox? plugin = null,
        IDalamudTextureWrap? iconImage = null,
        IDalamudTextureWrap? closeButtonTexture = null)
        : base($"{AetherBox.Name} {AetherBox.Plugin.GetType().Assembly.GetName().Version}###{AetherBox.Name}",
               ImGuiWindowFlags.NoScrollbar,
               false)
    {
        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(300, 500);
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(250, 300),
            MaximumSize = new Vector2(5000, 5000)
        };
        RespectCloseHotkey = true;

        this.IconImage = iconImage;
        this.CloseButtonTexture = closeButtonTexture;
        this.Plugin = plugin;

        CloseButtonTexture = plugin?.LoadImage("close.png");
    }

    public void Dispose()
    {
    }
    /*
        public override void Draw()
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
            try
            {
                using var table = ImRaii.Table("AetherBox Config Table", 2, ImGuiTableFlags.Resizable);
                if (table)
                {
                    // Set the width of the navigation panel column
                    var navigationPanelWidth = 150 * Scale;
                    ImGui.TableSetupColumn("AetherBox Config Side Bar", ImGuiTableColumnFlags.WidthFixed, navigationPanelWidth);
                    ImGui.TableNextColumn();

                    // Draw the icon at the top
                    DrawHeader();

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    try
                    {
                        DrawNavigationpanel();
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Warning(ex, "Something wrong with navigation panel");
                    }

                    ImGui.TableNextColumn();

                    try
                    {
                        DrawBody();
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Warning(ex, "Something wrong with body");
                    }

                }

            }
            catch (Exception ex)
            {
                Svc.Log.Warning(ex, "Something wrong with config window.");
            }
        }


        private void DrawNavigationpanel()
        {
            // Info
            if (ImGui.Selectable("Info", IsCategoryInfoOpen))
            {
                IsCategoryInfoOpen = !IsCategoryInfoOpen; // Toggle the state
                if (IsCategoryInfoOpen)
                {
                    selectedCategory = "Info";
                }
                else if (selectedCategory == "Info")
                {
                    selectedCategory = null;
                }
            }

            //Settings
            if (ImGui.Selectable("Settings", isCategorySettingsOpen))
            {
                isCategorySettingsOpen = !isCategorySettingsOpen; // Toggle the state
                if (isCategorySettingsOpen)
                {
                    selectedCategory = "Settings";
                }
                else if (selectedCategory == "Settings")
                {
                    selectedCategory = null;
                }
            }

            // ... additional categories with the same pattern



            // Calculate the space available for the button, or set a fixed size
            var spaceForButton = 50.0f * Scale; // Example size, adjust as needed

            // Assuming 'CloseButtonTexture' is the variable holding your loaded texture
            if (CloseButtonTexture != null)
            {
                // Calculate the center position for the button
                var windowWidth = ImGui.GetContentRegionAvail().X;
                var windowHeight = ImGui.GetWindowHeight();
                var buttonPosX = (windowWidth - spaceForButton) * 0.5f; // Center the button horizontally
                var offsetX = 9.5f; // Adjust this value as needed to align the button correctly
                buttonPosX += offsetX; // Apply the offset
                var buttonPosY = windowHeight - spaceForButton - ImGui.GetStyle().ItemSpacing.Y; // Position the button at the bottom

                // Set the cursor position to the calculated X and Y positions
                ImGui.SetCursorPosX(buttonPosX);
                ImGui.SetCursorPosY(buttonPosY);

                // Draw the image button without padding and background color
                if (ImGuiExtra.NoPaddingNoColorImageButton(CloseButtonTexture.ImGuiHandle, new Vector2(spaceForButton, spaceForButton)))
                {
                    // Ensure 'Plugin.Configuration' is not null before saving
                    if (AetherBox.Config != null)
                    {
                        // Save the settings
                        AetherBox.Config.Save();

                        // Ensure 'Svc.Log' is not null before logging
                        // Log the information
                        Svc.Log?.Information("Settings have been saved.");

                        // Close the window by toggling the visibility off
                        this.IsOpen = false; // Assuming 'IsOpen' is a property that controls the window's visibility
                    }

                }
            }

        }

        /// <summary>
        /// Draws the Top left Icon
        /// </summary>
        private void DrawHeader()
        {
            // Calculate the available width for the header and constrain the image to that width while maintaining aspect ratio
            var availableWidth = ImGui.GetContentRegionAvail().X;
            var aspectRatio = (float)this.IconImage.Width / this.IconImage.Height;
            var imageWidth = availableWidth;
            var imageHeight = imageWidth / aspectRatio;

            // Ensure the image is not taller than a certain threshold, e.g., 100 pixels
            var maxHeight = 100.0f * Scale;
            if (imageHeight > maxHeight)
            {
                imageHeight = maxHeight;
                imageWidth = imageHeight * aspectRatio;
            }

            // Center the image in the available space
            var spaceBeforeImage = (availableWidth - imageWidth) * 0.5f;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spaceBeforeImage);

            // Draw the image
            ImGui.Image(this.IconImage.ImGuiHandle, new Vector2(imageWidth, imageHeight));
        }

        private PluginInfoUI pluginInfoUI = new();
        private PluginSettingsUI pluginSettingsUI = new();

        private void DrawBody()
        {
            if (selectedCategory != null)
            {
                switch (selectedCategory)
                {
                    case "Info":
                        pluginInfoUI.DrawUI();
                        break;
                    case "Settings":
                        pluginSettingsUI.DrawUI();
                        break;
                        // Add more cases as needed for additional categories
                }
            }
        }
    */

    public override void Draw()
    {
        var contentRegionAvail1 = ImGui.GetContentRegionAvail();
        ref var local = ref ImGui.GetStyle().ItemSpacing;
        var y = contentRegionAvail1.Y;
        if (!ImGui.BeginTable("$" + AetherBox.Name + "TableContainer", 2, ImGuiTableFlags.Resizable))
            return;
        try
        {
            ImGui.TableSetupColumn("###LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2f);
            ImGui.TableNextColumn();
            var contentRegionAvail2 = ImGui.GetContentRegionAvail();
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
            if (ImGui.BeginChild("###" + AetherBox.Name + "Left", contentRegionAvail2 with
            {
                Y = y
            }, false, ImGuiWindowFlags.NoDecoration))
            {
                foreach (object obj in Enum.GetValues(typeof(OpenWindow)))
                {
                    if ((OpenWindow)obj != OpenWindow.None)
                    {
                        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                        interpolatedStringHandler.AppendFormatted<object>(obj);
                        if (ImGui.Selectable(interpolatedStringHandler.ToStringAndClear(), this.OpenWindow == (OpenWindow)obj))
                            this.OpenWindow = (OpenWindow)obj;
                    }
                }
                ImGui.Spacing();
                ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 45f);
                ImGuiEx.ImGuiLineCentered("###Search", (Action)(() =>
                {
                    ImGui.Text("Search");
                    ImGuiComponents.HelpMarker("Searches feature names and descriptions for a given word or phrase.");
                }));
                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.InputText("###FeatureSearch", ref this.searchString, 500U))
                {
                    if (this.searchString.Equals("ERP", StringComparison.CurrentCultureIgnoreCase) && !this.hornybonk)
                    {
                        this.hornybonk = true;
                        string hornybonkurl = "https://www.youtube.com/watch?v=oO-gc3Lh-oI";
                        Dalamud.Utility.Util.OpenLink($"{hornybonkurl}");
                    }
                    else
                        this.hornybonk = false;
                    this.filteredFeatures.Clear();
                    if (this.searchString.Length > 0)
                    {
                        foreach (var feature in AetherBox.Plugin.Features)
                        {
                            if (feature.FeatureType != FeatureType.Commands && feature.FeatureType != FeatureType.Disabled && (feature.Description.Contains(this.searchString, StringComparison.CurrentCultureIgnoreCase) || feature.Name.Contains(this.searchString, StringComparison.CurrentCultureIgnoreCase)))
                                this.filteredFeatures.Add(feature);
                        }
                    }
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.TableNextColumn();
            if (ImGui.BeginChild("###" + AetherBox.Name + "Right", Vector2.Zero, false, ImGuiWindowFlags.NoDecoration))
            {
                if (this.filteredFeatures.Count > 0)
                {
                    this.DrawFeatures(this.filteredFeatures.ToArray());
                }
                else
                {
                    switch (this.OpenWindow)
                    {
                        case OpenWindow.Actions:
                            this.DrawFeatures(AetherBox.Plugin.Features.Where<BaseFeature>((Func<BaseFeature, bool>)(x =>
                            {
                                if (x.FeatureType != FeatureType.Actions)
                                    return false;
                                return !x.isDebug || AetherBox.Config.showDebugFeatures;
                            })).ToArray<BaseFeature>());
                            break;
                        case OpenWindow.UI:
                            this.DrawFeatures(AetherBox.Plugin.Features.Where<BaseFeature>((Func<BaseFeature, bool>)(x => { if (x.FeatureType != FeatureType.UI) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; })).ToArray<BaseFeature>());
                            break;
                        case OpenWindow.Targets:
                            this.DrawFeatures(AetherBox.Plugin.Features.Where<BaseFeature>((Func<BaseFeature, bool>)(x => { if (x.FeatureType != FeatureType.Targeting) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; })).ToArray<BaseFeature>());
                            break;
                        case OpenWindow.Chat:
                            this.DrawFeatures(AetherBox.Plugin.Features.Where<BaseFeature>((Func<BaseFeature, bool>)(x => { if (x.FeatureType != FeatureType.ChatFeature) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; })).ToArray<BaseFeature>());
                            break;
                        case OpenWindow.Other:
                            this.DrawFeatures(AetherBox.Plugin.Features.Where<BaseFeature>((Func<BaseFeature, bool>)(x => { if (x.FeatureType != FeatureType.Other) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; })).ToArray<BaseFeature>());
                            break;
                        case OpenWindow.Achievements:
                            this.DrawFeatures(AetherBox.Plugin.Features.Where<BaseFeature>((Func<BaseFeature, bool>)(x => { if (x.FeatureType != FeatureType.Achievements) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; })).ToArray<BaseFeature>());
                            break;
                        case OpenWindow.Commands:
                            MainWindow.DrawCommands(AetherBox.Plugin.Features.Where<BaseFeature>((Func<BaseFeature, bool>)(x =>
                            {
                                if (x.FeatureType != FeatureType.Commands)
                                    return false;
                                return !x.isDebug || AetherBox.Config.showDebugFeatures;
                            })).ToArray<BaseFeature>());
                            break;
                        case OpenWindow.About:
                            MainWindow.DrawAbout();
                            break;
                    }
                }
            }
            ImGui.EndChild();
        }
        catch (Exception ex)
        {
            ex.Log();
            ImGui.EndTable();
        }
        ImGui.EndTable();
    }

    private static void DrawAbout()
    {
        ImGui.TextWrapped("This is where I test features for Pandora's Box, learn to break the game, or store features ill suited for anything else.");
        ImGui.Spacing();
        ImGui.TextWrapped("If any feature you see here is in Pandora's Box, it means I'm testing modifications to that feature. If you enable it here, make sure the Pandora version is disabled or there will probably be problems.");
        ImGui.Text("Icon by Kadmas");
    }

    private static void DrawCommands(BaseFeature[] features)
    {
        if (features == null || !((IEnumerable<BaseFeature>)features).Any<BaseFeature>() || features.Length == 0)
            return;
        DefaultInterpolatedStringHandler interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(13, 1);
        interpolatedStringHandler1.AppendLiteral("featureHeader");
        interpolatedStringHandler1.AppendFormatted<FeatureType>(((IEnumerable<BaseFeature>)features).First<BaseFeature>().FeatureType);
        ImGuiEx.ImGuiLineCentered(interpolatedStringHandler1.ToStringAndClear(), (Action)(() =>
        {
            DefaultInterpolatedStringHandler interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(0, 1);
            interpolatedStringHandler2.AppendFormatted<FeatureType>(((IEnumerable<BaseFeature>)features).First<BaseFeature>().FeatureType);
            ImGui.Text(interpolatedStringHandler2.ToStringAndClear());
        }));
        ImGui.Separator();
        if (!ImGui.BeginTable("###CommandsTable", 5, ImGuiTableFlags.Borders))
            return;
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("Command");
        ImGui.TableSetupColumn("Parameters");
        ImGui.TableSetupColumn("Description");
        ImGui.TableSetupColumn("Aliases");
        ImGui.TableHeadersRow();
        foreach (CommandFeature commandFeature in features.Cast<CommandFeature>())
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextWrapped(commandFeature.Name);
            ImGui.TableNextColumn();
            ImGui.TextWrapped(commandFeature.Command);
            ImGui.TableNextColumn();
            ImGui.TextWrapped(string.Join(", ", (IEnumerable<string>)commandFeature.Parameters));
            ImGui.TableNextColumn();
            ImGui.TextWrapped(commandFeature.Description ?? "");
            ImGui.TableNextColumn();
            ImGui.TextWrapped(string.Join(", ", commandFeature.Alias) ?? "");
        }
        ImGui.EndTable();
    }

    private void DrawFeatures(IEnumerable<BaseFeature> features)
    {
        if (features == null || !features.Any<BaseFeature>() || !features.Any<BaseFeature>())
            return;
        DefaultInterpolatedStringHandler interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(13, 1);
        interpolatedStringHandler1.AppendLiteral("featureHeader");
        interpolatedStringHandler1.AppendFormatted<FeatureType>(features.First<BaseFeature>().FeatureType);
        ImGuiEx.ImGuiLineCentered(interpolatedStringHandler1.ToStringAndClear(), (Action)(() =>
        {
            if (this.filteredFeatures.Count > 0)
            {
                ImGui.Text("Search Results");
            }
            else
            {
                DefaultInterpolatedStringHandler interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(0, 1);
                interpolatedStringHandler2.AppendFormatted<FeatureType>(features.First<BaseFeature>().FeatureType);
                ImGui.Text(interpolatedStringHandler2.ToStringAndClear());
            }
        }));
        ImGui.Separator();
        foreach (BaseFeature feature1 in features)
        {
            BaseFeature feature = feature1;
            bool enabled = feature.Enabled;
            if (ImGui.Checkbox("###" + feature.Name, ref enabled))
            {
                if (enabled)
                {
                    try
                    {
                        feature.Enable();
                        if (feature.Enabled)
                            AetherBox.Config.EnabledFeatures.Add(feature.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Error(ex, "Failed to enabled " + feature.Name);
                    }
                }
                else
                {
                    try
                    {
                        feature.Disable();
                        AetherBox.Config.EnabledFeatures.RemoveAll((Predicate<string>)(x => x == feature.GetType().Name));
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Error(ex, "Failed to enabled " + feature.Name);
                    }
                }
                AetherBox.Config.Save();
            }
            ImGui.SameLine();
            feature.DrawConfig(ref enabled);
            ImGui.Spacing();
            ImGui.TextWrapped(feature.Description ?? "");
            ImGui.Separator();
        }
    }

}
