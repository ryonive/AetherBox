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

public class MainWindow : Window, IDisposable
{
    internal readonly AetherBox Plugin;

    private string ? searchString;
    private readonly List<BaseFeature> ? filteredFeatures;
    private bool hornybonk;
    public OpenWindow OpenWindow { get; private set; }

    public MainWindow(AetherBox plugin) : base($"{AetherBox.Name} {AetherBox.Plugin.GetType().Assembly.GetName().Version}###{AetherBox.Name}",
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

        Plugin = plugin;
    }

    public void Dispose()
    {
        Svc.Log.Debug($"Nothing here");
    }

    public override void Draw()
    {
        var contentRegionAvail1 = ImGui.GetContentRegionAvail();
        ref var local = ref ImGui.GetStyle().ItemSpacing;
        var y = contentRegionAvail1.Y;

        try
        {
            if (!ImGui.BeginTable("$" + AetherBox.Name + "TableContainer", 2, ImGuiTableFlags.Resizable))
            {
                return;
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex.Message);
        }


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
                foreach (var obj in Enum.GetValues(typeof(OpenWindow)))
                {
                    if ((OpenWindow)obj != OpenWindow.None)
                    {
                        var interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                        interpolatedStringHandler.AppendFormatted(obj);
                        if (ImGui.Selectable(interpolatedStringHandler.ToStringAndClear(), OpenWindow == (OpenWindow)obj))
                            OpenWindow = (OpenWindow)obj;
                    }
                }
                ImGui.Spacing();
                ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 45f);
                ImGuiEx.ImGuiLineCentered("###Search", () =>
                {
                    ImGui.Text("Search");
                    ImGuiComponents.HelpMarker("Searches feature names and descriptions for a given word or phrase.");
                });
                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.InputText("###FeatureSearch", ref searchString, 500U))
                {
                    if (searchString.Equals("ERP", StringComparison.CurrentCultureIgnoreCase) && !hornybonk)
                    {
                        hornybonk = true;
                        var hornybonkurl = "https://www.youtube.com/watch?v=oO-gc3Lh-oI";
                        Dalamud.Utility.Util.OpenLink($"{hornybonkurl}");
                    }
                    else
                        hornybonk = false;
                    filteredFeatures.Clear();
                    if (searchString.Length > 0)
                    {
                        foreach (var feature in AetherBox.Plugin.Features)
                        {
                            if (feature.FeatureType != FeatureType.Commands && feature.FeatureType != FeatureType.Disabled && (feature.Description.Contains(searchString, StringComparison.CurrentCultureIgnoreCase) || feature.Name.Contains(searchString, StringComparison.CurrentCultureIgnoreCase)))
                                filteredFeatures.Add(feature);
                        }
                    }
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.TableNextColumn();
            if (ImGui.BeginChild("###" + AetherBox.Name + "Right", Vector2.Zero, false, ImGuiWindowFlags.NoDecoration))
            {
                if (filteredFeatures.Count > 0)
                {
                    DrawFeatures(filteredFeatures.ToArray());
                }
                else
                {
                    switch (OpenWindow)
                    {
                        case OpenWindow.Actions:
                            DrawFeatures(AetherBox.Plugin.Features.Where(x =>
                            {
                                if (x.FeatureType != FeatureType.Actions)
                                    return false;
                                return !x.isDebug || AetherBox.Config.showDebugFeatures;
                            }).ToArray());
                            break;
                        case OpenWindow.UI:
                            DrawFeatures(AetherBox.Plugin.Features.Where(x => { if (x.FeatureType != FeatureType.UI) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; }).ToArray());
                            break;
                        case OpenWindow.Targets:
                            DrawFeatures(AetherBox.Plugin.Features.Where(x => { if (x.FeatureType != FeatureType.Targeting) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; }).ToArray());
                            break;
                        case OpenWindow.Chat:
                            DrawFeatures(AetherBox.Plugin.Features.Where(x => { if (x.FeatureType != FeatureType.ChatFeature) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; }).ToArray());
                            break;
                        case OpenWindow.Other:
                            DrawFeatures(AetherBox.Plugin.Features.Where(x => { if (x.FeatureType != FeatureType.Other) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; }).ToArray());
                            break;
                        case OpenWindow.Achievements:
                            DrawFeatures(AetherBox.Plugin.Features.Where(x => { if (x.FeatureType != FeatureType.Achievements) return false; return !x.isDebug || AetherBox.Config.showDebugFeatures; }).ToArray());
                            break;
                        case OpenWindow.Commands:
                            DrawCommands(AetherBox.Plugin.Features.Where(x =>
                            {
                                if (x.FeatureType != FeatureType.Commands)
                                    return false;
                                return !x.isDebug || AetherBox.Config.showDebugFeatures;
                            }).ToArray());
                            break;
                        case OpenWindow.About:
                            DrawAbout();
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
        try
        {
            ImGui.TextWrapped("This is where I test features for Pandora's Box, learn to break the game, or store features ill suited for anything else.");
            ImGui.Spacing();
            ImGui.TextWrapped("If any feature you see here is in Pandora's Box, it means I'm testing modifications to that feature. If you enable it here, make sure the Pandora version is disabled or there will probably be problems.");
            ImGui.Text("Icon by Kadmas");
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawAbout");
        }

    }

    private static void DrawCommands(BaseFeature[] features)
    {
        try
        {
            if (features == null || !features.Any() || features.Length == 0)
                return;
            var interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(13, 1);
            interpolatedStringHandler1.AppendLiteral("featureHeader");
            interpolatedStringHandler1.AppendFormatted(features.First().FeatureType);
            ImGuiEx.ImGuiLineCentered(interpolatedStringHandler1.ToStringAndClear(), () =>
            {
                var interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(0, 1);
                interpolatedStringHandler2.AppendFormatted(features.First().FeatureType);
                ImGui.Text(interpolatedStringHandler2.ToStringAndClear());
            });
            ImGui.Separator();
            if (!ImGui.BeginTable("###CommandsTable", 5, ImGuiTableFlags.Borders))
                return;
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Command");
            ImGui.TableSetupColumn("Parameters");
            ImGui.TableSetupColumn("Description");
            ImGui.TableSetupColumn("Aliases");
            ImGui.TableHeadersRow();
            foreach (var commandFeature in features.Cast<CommandFeature>())
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
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawCommands");
        }

    }

    private void DrawFeatures(IEnumerable<BaseFeature> features)
    {
        try
        {
            if (features == null || !features.Any() || !features.Any())
                return;
            var interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(13, 1);
            interpolatedStringHandler1.AppendLiteral("featureHeader");
            interpolatedStringHandler1.AppendFormatted(features.First().FeatureType);
            ImGuiEx.ImGuiLineCentered(interpolatedStringHandler1.ToStringAndClear(), () =>
            {
                if (filteredFeatures.Count > 0)
                {
                    ImGui.Text("Search Results");
                }
                else
                {
                    var interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(0, 1);
                    interpolatedStringHandler2.AppendFormatted(features.First().FeatureType);
                    ImGui.Text(interpolatedStringHandler2.ToStringAndClear());
                }
            });
            ImGui.Separator();
            foreach (var feature1 in features)
            {
                var feature = feature1;
                var enabled = feature.Enabled;
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
                            AetherBox.Config.EnabledFeatures.RemoveAll(x => x == feature.GetType().Name);
                        }
                        catch (Exception ex)
                        {
                            Svc.Log.Error(ex, "Failed to enabled " + feature.Name);
                        }
                    }
                    AetherBox.Config.InfoSave();
                }
                ImGui.SameLine();
                feature.DrawConfig(ref enabled);
                ImGui.Spacing();
                ImGui.TextWrapped(feature.Description ?? "");
                ImGui.Separator();
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawFeatures");
        }

    }

}
