using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using AetherBox.Debugging;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using AetherBox.Helpers.Extensions;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using Dalamud.Utility;
using EasyCombat.UI.Helpers;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop.Attributes;
using ImGuiNET;
using Lumina.Excel;
using Dalamud.Plugin;
using ECommons.Automation;

namespace AetherBox.UI;

public class MainWindow : Window
{
    private readonly IDalamudTextureWrap ? IconImage;
    private readonly IDalamudTextureWrap ? BannerImage;

    private static float Scale => ImGuiHelpers.GlobalScale;

    private string searchString = string.Empty;
    private readonly List<BaseFeature> FilteredFeatures = new List<BaseFeature>();
    private bool toothless;
    private bool erp;
    public OpenCatagory OpenCatagory { get; private set; }
    public string InfoMarker { get; private set; } = "More information can be found\nby either hovering the mouse over the featurename or checkbox";
    public MainWindow(IDalamudTextureWrap bannerImage, IDalamudTextureWrap iconImage)
        : base($"{BaseFeature.AetherBoxPayload} - v{AetherBox.P.GetType().Assembly.GetName().Version}###{AetherBox.Name}", ImGuiWindowFlags.AlwaysUseWindowPadding, false)
    {
        // Set initial size and size condition
        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(850, 420);

        // Set window size constraints
        var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
        if (primaryScreen != null)
        {
            var workingArea = primaryScreen.WorkingArea;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(430, 250),
                MaximumSize = new Vector2(900, workingArea.Height)
                //MaximumSize = new Vector2(workingArea.Width, workingArea.Height)
            };
        }
        else
        {
            Svc.Log.Error("Primairy screen not available");
        }

        // Initialize other properties
        RespectCloseHotkey = true;
        IconImage = iconImage;
        BannerImage = bannerImage; // Assign the passed banner image
        OnCloseSfxId = 24;
        OnOpenSfxId = 23;
        AllowPinning = true;
        AllowClickthrough = true;
    }

    public static void Dispose()
    {
        foreach (DebugHelper debugHelper in DebugHelpers)
        {
            RemoveDebugPage(debugHelper.FullName);
            debugHelper.Dispose();
        }
        DebugHelpers.Clear();
        DebugPages.Clear();
    }

    public override void Draw()
    {
        try
        {
            DrawTab();
        }
        catch (Exception ex)
        {
            ex.Log();
        }
    }

    private void DrawTab()
    {

        if (ImGui.BeginTabBar("MyTabBar"))
        {

            if (ImGui.BeginTabItem("Menu"))
            {
                DrawBody();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Debug"))
            {
                DrawDebugWindow();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }


    #region Main menu
    private void DrawBody()
    {
        var contentRegionAvail = ImGui.GetContentRegionAvail();
        _ = ref ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = contentRegionAvail.Y;
        if (!ImGui.BeginTable("$" + AetherBox.Name + "TableContainer", 2, ImGuiTableFlags.Resizable))
        {
            return;
        }

        try
        {

            ImGui.TableSetupColumn("###LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2f);
            ImGui.TableNextColumn();
            Vector2 regionSize = ImGui.GetContentRegionAvail();
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
            string str_id = "###" + AetherBox.Name + "Left";
            Vector2 size = regionSize;
            size.Y = topLeftSideHeight;
            if (ImGui.BeginChild(str_id, size, border: false, ImGuiWindowFlags.NoDecoration))
            {
                DrawHeader();
                foreach (object window in Enum.GetValues(typeof(OpenCatagory)))
                {
                    if ((OpenCatagory)window != 0 && ImGui.Selectable($"{window}", OpenCatagory == (OpenCatagory)window))
                    {
                        OpenCatagory = (OpenCatagory)window;
                    }
                }
                ImGui.Spacing();
                ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 45f);
                ImGuiEx.ImGuiLineCentered("###Search", delegate
                {
                    ImGui.Text("Search");
                    ImGuiComponents.HelpMarker("Searches feature names and descriptions for a given word or phrase.\nTrying searching for \"certain\" things like \"toothless\" or \"ERP\"  ");
                });
                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.InputText("###FeatureSearch", ref searchString, 500u))
                {
                    if (searchString.Equals("toothless", StringComparison.CurrentCultureIgnoreCase) && !toothless)
                    {
                        toothless = true;
                        var YTurl = "https://www.youtube.com/watch?v=4t7BgyA7IOI";
                        Util.OpenLink(YTurl);
                    }
                    else
                    {
                        toothless = false;
                    }
                    if (searchString.Equals("ERP", StringComparison.CurrentCultureIgnoreCase) && !erp)
                    {
                        erp = true;
                        var YTurlerp = "https://www.youtube.com/shorts/DU22u5YNtPo?feature=share";
                        Util.OpenLink(YTurlerp);
                    }
                    else
                    {
                        erp = false;
                    }
                    FilteredFeatures.Clear();
                    if (searchString.Length > 0)
                    {
                        foreach (BaseFeature feature in AetherBox.P.Features)
                        {
                            if (feature.FeatureType != FeatureType.Commands && feature.FeatureType != 0 && (feature.Description.Contains(searchString, StringComparison.CurrentCultureIgnoreCase) || feature.Name.Contains(searchString, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                FilteredFeatures.Add(feature);
                            }
                        }
                    }
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.TableNextColumn();
            if (ImGui.BeginChild("###" + AetherBox.Name + "Right", Vector2.Zero, false, ImGuiWindowFlags.NoDecoration))
            {
                //DrawBanner();
                if (FilteredFeatures?.Count > 0)
                {
                    DrawFeatures(FilteredFeatures?.ToArray());
                }
                else
                {
                    switch (OpenCatagory)
                    {
                        case OpenCatagory.Actions:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Actions && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.UI:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.UI && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Other:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Other && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Targets:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Targeting && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Chat:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.ChatFeature && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Achievements:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Achievements && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Commands:
                            DrawCommands(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Commands && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Debug:
                            //DrawDebugFeatures(AetherBox.P.Features.Where((BaseFeature x) => (x.isDebug)).ToArray());
                            break;
                        case OpenCatagory.QuickLinks:
                            QuickLinks.DrawQuickLinks();
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

    private void DrawHeader()
    {
        try
        {
            // Calculate the available width for the header and constrain the image to that width while maintaining aspect ratio
            var availableWidth = ImGui.GetContentRegionAvail().X;
            if (IconImage != null)
            {
                var aspectRatio = (float)IconImage.Width / IconImage.Height;
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
            else
            {
                Svc.Log.Error("Icon Image is Null!");
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawHeader");
        }

    }

    private static void DrawDebugFeatures(BaseFeature[] features)
    {
        try
        {
            ImGui.Spacing();
            ImGuiExtra.TextCentered("Enable the forbidden features.");
            ImGui.Spacing();
            //ImGui.Checkbox("Show debug features and commands", ref AetherBox.Config.ShowDebugFeatures);

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
            if (features == null || !features.Any())
                return;

            // Header
            var featureType = features.First().FeatureType;
            var headerText = FilteredFeatures?.Count > 0 ? "Search Results" : featureType.ToString();
            ImGuiHelper.ImGuiLineCentered("featureHeader" + featureType, () => ImGui.Text(headerText));
            ImGuiHelper.HelpMarker(InfoMarker ?? "");
            ImGuiHelper.SeperatorWithSpacing();

            // Feature List
            foreach (var feature in features)
            {
                DrawFeature(feature);
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawFeatures");
        }
    }

    private void DrawFeature(BaseFeature feature)
    {
        var enabled = feature.Enabled;
        if (ImGui.Checkbox("###" + feature.Name, ref enabled))
        {
            ToggleFeature(feature, enabled);
        }
        ImGuiHelper.ColoredTextTooltip(feature.Description ?? "", AetherColor.GhostType);
        ImGui.SameLine();
        feature.DrawConfig(ref enabled);
        ImGuiHelper.SeperatorWithSpacing();
    }

    private static void ToggleFeature(BaseFeature feature, bool enabled)
    {
        try
        {
            if (enabled)
            {
                feature.Enable();
                if (feature.Enabled)
                {
                    AetherBox.Config?.EnabledFeatures.Add(feature.GetType().Name);
                }
            }
            else
            {
                feature.Disable();
                AetherBox.Config.EnabledFeatures.RemoveAll(x => x == feature.GetType().Name);
            }

            if (AetherBox.Config != null)
            {
                AetherBox.Config.Save();
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Failed to enable/disable " + feature.Name);
        }
    }
    #endregion

    #region Debug
    private static readonly Dictionary<string, Action> DebugPages = new Dictionary<string, Action>();

    private static float SidebarSize = 0f;

    private static bool SetupDebugHelpers = false;

    private static readonly List<DebugHelper> DebugHelpers = new List<DebugHelper>();

    //private static readonly Stopwatch InitDelay = Stopwatch.StartNew();

    private static ulong BeginModule = 0uL;

    private static ulong EndModule = 0uL;

    private static readonly Dictionary<string, object> SavedValues = new Dictionary<string, object>();
    private static void DrawDebugWindow()
    {
        if (AetherBox.P == null)
        {
            Svc.Log.Info("null");
            return;
        }
        if (!SetupDebugHelpers)
        {
            SetupDebugHelpers = true;
            try
            {
                foreach (FeatureProvider tp in AetherBox.P.FeatureProviders)
                {
                    if (tp.Disposed)
                    {
                        continue;
                    }
                    foreach (Type item in from t in tp.Assembly.GetTypes()
                                          where t.IsSubclassOf(typeof(DebugHelper)) && !t.IsAbstract
                                          select t)
                    {
                        DebugHelper debugger;
                        debugger = (DebugHelper)Activator.CreateInstance(item);
                        debugger.FeatureProvider = tp;
                        debugger.Plugin = global::AetherBox.AetherBox.P;
                        RegisterDebugPage(debugger.FullName, debugger.Draw);
                        DebugHelpers.Add(debugger);
                    }
                }
            }
            catch (Exception ex3)
            {
                Svc.Log.Error(ex3, "");
                SetupDebugHelpers = false;
                DebugHelpers.Clear();
                //AetherBox.P.DebugWindow.IsOpen = false;
                return;
            }
        }
        if (SidebarSize < 150f)
        {
            SidebarSize = 150f;
            try
            {
                foreach (string key in DebugPages.Keys)
                {
                    float s2;
                    s2 = ImGui.CalcTextSize(key).X + ImGui.GetStyle().FramePadding.X * 5f + ImGui.GetStyle().ScrollbarSize;
                    if (s2 > SidebarSize)
                    {
                        SidebarSize = s2;
                    }
                }
            }
            catch (Exception ex2)
            {
                Svc.Log.Error(ex2, "");
            }
        }
        using ImRaii.IEndObject table = ImRaii.Table("DebugManagerTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table.Success)
        {
            return;
        }
        ImGui.TableSetupColumn("##DebugManagerSelectionColumn", ImGuiTableColumnFlags.WidthFixed, 200f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##DebugManagerContentsColumn", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();
        using (ImRaii.Child("###" + AetherBox.Name + "DebugPages", new Vector2(SidebarSize, -1f) * ImGui.GetIO().FontGlobalScale, border: true))
        {
            List<string> list;
            list = DebugPages.Keys.ToList();
            list.Sort((string s, string s1) => (s.StartsWith("[") && !s1.StartsWith("[")) ? 1 : string.CompareOrdinal(s, s1));
            foreach (string i in list)
            {
                if (ImGui.Selectable($"{i}##{AetherBox.Name}{"DebugPages"}{"Config"}", AetherBox.Config?.Debugging.SelectedPage == i))
                {
                    if (AetherBox.Config == null) return;
                    AetherBox.Config.Debugging.SelectedPage = i;
                    AetherBox.Config.Save();
                }
            }
        }
        ImGui.TableNextColumn();
        using (ImRaii.Child("###" + AetherBox.Name + "DebugPagesView", new Vector2(-1f, -1f), border: true, ImGuiWindowFlags.HorizontalScrollbar))
        {
            if (string.IsNullOrEmpty(AetherBox.Config?.Debugging.SelectedPage) || !DebugPages.ContainsKey(AetherBox.Config.Debugging.SelectedPage))
            {
                ImGui.Text("Select Debug Page");
                return;
            }
            try
            {
                DebugPages[global::AetherBox.AetherBox.Config.Debugging.SelectedPage]();
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "");
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), ex.ToString());
            }
        }

    }

    public static void RegisterDebugPage(string key, Action action)
    {
        if (DebugPages.ContainsKey(key))
        {
            DebugPages[key] = action;
        }
        else
        {
            DebugPages.Add(key, action);
        }
        SidebarSize = 0f;
    }

    public static void RemoveDebugPage(string key)
    {
        if (DebugPages.ContainsKey(key))
        {
            DebugPages.Remove(key);
        }
        SidebarSize = 0f;
    }

    public static void Reload()
    {
        DebugHelpers.RemoveAll(delegate (DebugHelper dh)
        {
            if (!dh.FeatureProvider.Disposed)
            {
                return false;
            }
            RemoveDebugPage(dh.FullName);
            dh.Dispose();
            return true;
        });
        foreach (FeatureProvider tp in global::AetherBox.AetherBox.P.FeatureProviders)
        {
            if (tp.Disposed)
            {
                continue;
            }
            foreach (Type t2 in from t in tp.Assembly.GetTypes()
                                where t.IsSubclassOf(typeof(DebugHelper)) && !t.IsAbstract
                                select t)
            {
                if (!DebugHelpers.Any((DebugHelper h) => h.GetType() == t2))
                {
                    DebugHelper debugger;
                    debugger = (DebugHelper)Activator.CreateInstance(t2);
                    debugger.FeatureProvider = tp;
                    debugger.Plugin = global::AetherBox.AetherBox.P;
                    RegisterDebugPage(debugger.FullName, debugger.Draw);
                    DebugHelpers.Add(debugger);
                }
            }
        }
    }

    private unsafe static Vector2 GetNodePosition(AtkResNode* node)
    {
        Vector2 pos;
        pos = new Vector2(node->X, node->Y);
        for (AtkResNode* par = node->ParentNode; par != null; par = par->ParentNode)
        {
            pos *= new Vector2(par->ScaleX, par->ScaleY);
            pos += new Vector2(par->X, par->Y);
        }
        return pos;
    }

    private unsafe static Vector2 GetNodeScale(AtkResNode* node)
    {
        if (node == null)
        {
            return new Vector2(1f, 1f);
        }
        Vector2 scale;
        scale = new Vector2(node->ScaleX, node->ScaleY);
        while (node->ParentNode != null)
        {
            node = node->ParentNode;
            scale *= new Vector2(node->ScaleX, node->ScaleY);
        }
        return scale;
    }

    private unsafe static bool GetNodeVisible(AtkResNode* node)
    {
        if (node == null)
        {
            return false;
        }
        while (node != null)
        {
            if (!node->IsVisible)
            {
                return false;
            }
            node = node->ParentNode;
        }
        return true;
    }

    public unsafe static void HighlightResNode(AtkResNode* node)
    {
        Vector2 position;
        position = GetNodePosition(node);
        Vector2 scale;
        scale = GetNodeScale(node);
        Vector2 size;
        size = new Vector2((int)node->Width, (int)node->Height) * scale;
        bool nodeVisible;
        nodeVisible = GetNodeVisible(node);
        ImGui.GetForegroundDrawList().AddRectFilled(position, position + size, nodeVisible ? 1426128640u : 1426063615u);
        ImGui.GetForegroundDrawList().AddRect(position, position + size, nodeVisible ? 4278255360u : 4278190335u);
    }

    public static void ClickToCopyText(string text, string textCopy = null)
    {
        if (textCopy == null)
        {
            textCopy = text;
        }
        ImGui.Text(text ?? "");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (textCopy != text)
            {
                ImGui.SetTooltip(textCopy);
            }
        }
        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(textCopy ?? "");
        }
    }

    public unsafe static void ClickToCopy(void* address)
    {
        ClickToCopyText(GetAddressString(address, absoluteOnly: true));
    }

    public unsafe static void ClickToCopy<T>(T* address) where T : unmanaged
    {
        ClickToCopy((void*)address);
    }

    public static void SeStringToText(Dalamud.Game.Text.SeStringHandling.SeString seStr)
    {
        int pushColorCount;
        pushColorCount = 0;
        ImGui.BeginGroup();
        foreach (Payload p in seStr.Payloads)
        {
            if (!(p is UIForegroundPayload c))
            {
                if (p is TextPayload t)
                {
                    ImGui.Text(t.Text ?? "");
                    ImGui.SameLine();
                }
            }
            else if (c.ColorKey == 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, uint.MaxValue);
                pushColorCount++;
            }
            else
            {
                uint r;
                r = (c.UIColor.UIForeground >> 24) & 0xFFu;
                uint g;
                g = (c.UIColor.UIForeground >> 16) & 0xFFu;
                uint b;
                b = (c.UIColor.UIForeground >> 8) & 0xFFu;
                _ = c.UIColor.UIForeground;
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4((float)r / 255f, (float)g / 255f, (float)b / 255f, 1f));
                pushColorCount++;
            }
        }
        ImGui.EndGroup();
        if (pushColorCount > 0)
        {
            ImGui.PopStyleColor(pushColorCount);
        }
    }

    private unsafe static void PrintOutValue(ulong addr, List<string> path, Type type, object value, MemberInfo member)
    {
        try
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                value = type.GetMethod("ToArray")?.Invoke(value, null);
                type = value.GetType();
            }
            Attribute? customAttribute;
            customAttribute = member.GetCustomAttribute(typeof(ValueParser));
            FixedBufferAttribute fixedBuffer;
            fixedBuffer = (FixedBufferAttribute)member.GetCustomAttribute(typeof(FixedBufferAttribute));
            FixedArrayAttribute fixedArray;
            fixedArray = (FixedArrayAttribute)member.GetCustomAttribute(typeof(FixedArrayAttribute));
            Attribute fixedSizeArray;
            fixedSizeArray = member.GetCustomAttribute(typeof(FixedSizeArrayAttribute<>));
            if (customAttribute is ValueParser vp)
            {
                vp.ImGuiPrint(type, value, member, addr);
            }
            else if (type.IsPointer)
            {
                void* unboxed = Pointer.Unbox((Pointer)value);
                if (unboxed != null)
                {
                    ulong unboxedAddr = (ulong)unboxed;
                    ClickToCopyText($"{unboxedAddr:X}");
                    if (BeginModule != 0 && unboxedAddr >= BeginModule && unboxedAddr <= EndModule)
                    {
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Text, 4291543295u);
                        ClickToCopyText($"ffxiv_dx11.exe+{unboxedAddr - BeginModule:X}");
                        ImGui.PopStyleColor();
                    }
                    try
                    {
                        Type eType;
                        eType = type.GetElementType();
                        object? obj;
                        obj = SafeMemory.PtrToStructure(new IntPtr(unboxed), eType);
                        ImGui.SameLine();
                        PrintOutObject(obj, (ulong)unboxed, new List<string>(path));
                        return;
                    }
                    catch
                    {
                        return;
                    }
                }
                ImGui.Text("null");
            }
            else if (type.IsArray)
            {
                Array arr;
                arr = (Array)value;
                if (ImGui.TreeNode($"Values##{member.Name}-{addr}-{string.Join("-", path)}"))
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        ImGui.Text($"[{i}]");
                        ImGui.SameLine();
                        PrintOutValue(addr, new List<string>(path) { $"_arrValue_{i}" }, type.GetElementType(), arr.GetValue(i), member);
                    }
                    ImGui.TreePop();
                }
            }
            else if (fixedBuffer != null)
            {
                if (fixedSizeArray != null)
                {
                    Type fixedType;
                    fixedType = fixedSizeArray.GetType().GetGenericArguments()[0];
                    int size;
                    size = (int)fixedSizeArray.GetType().GetProperty("Count").GetValue(fixedSizeArray);
                    if (!ImGui.TreeNode($"Fixed {ParseTypeName(fixedType)} Array##{member.Name}-{addr}-{string.Join("-", path)}"))
                    {
                        return;
                    }
                    if (fixedType.Namespace + "." + fixedType.Name == "FFXIVClientStructs.Interop.Pointer`1")
                    {
                        Type pointerType;
                        pointerType = fixedType.GetGenericArguments()[0];
                        void** arrAddr;
                        arrAddr = (void**)addr;
                        if (arrAddr != null)
                        {
                            for (int j = 0; j < size; j++)
                            {
                                if (arrAddr[j] == null)
                                {
                                    if (ImGui.GetIO().KeyAlt)
                                    {
                                        ImGui.Text($"[{j}] null");
                                    }
                                    continue;
                                }
                                object arrObj;
                                arrObj = SafeMemory.PtrToStructure(new IntPtr(arrAddr[j]), pointerType);
                                if (arrObj == null)
                                {
                                    if (ImGui.GetIO().KeyAlt)
                                    {
                                        ImGui.Text($"[{j}] error");
                                    }
                                }
                                else
                                {
                                    PrintOutObject(arrObj, (ulong)arrAddr[j], new List<string>(path) { $"_arrValue_{j}" }, autoExpand: false, $"[{j}] {arrObj}");
                                }
                            }
                        }
                        else
                        {
                            ImGui.Text("Null Pointer");
                        }
                    }
                    else if (fixedType.IsGenericType)
                    {
                        ImGui.Text("Unable to display generic types.");
                    }
                    else
                    {
                        nint arrAddr2;
                        arrAddr2 = (nint)addr;
                        for (int k = 0; k < size; k++)
                        {
                            object arrObj2;
                            arrObj2 = SafeMemory.PtrToStructure(arrAddr2, fixedType);
                            PrintOutObject(arrObj2, (ulong)((IntPtr)arrAddr2).ToInt64(), new List<string>(path) { $"_arrValue_{k}" }, autoExpand: false, $"[{k}] {arrObj2}");
                            arrAddr2 += Marshal.SizeOf(fixedType);
                        }
                    }
                    ImGui.TreePop();
                }
                else if (fixedArray != null)
                {
                    if (fixedArray.Type == typeof(string) && fixedArray.Count == 1)
                    {
                        string text;
                        text = Marshal.PtrToStringUTF8((nint)addr);
                        if (text != null)
                        {
                            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
                            ImGui.TextDisabled("\"");
                            ImGui.SameLine();
                            ImGui.Text(text);
                            ImGui.SameLine();
                            ImGui.PopStyleVar();
                            ImGui.TextDisabled("\"");
                        }
                        else
                        {
                            ImGui.TextDisabled("null");
                        }
                    }
                    else if (ImGui.TreeNode($"Fixed {ParseTypeName(fixedArray.Type)} Array##{member.Name}-{addr}-{string.Join("-", path)}"))
                    {
                        nint arrAddr3;
                        arrAddr3 = (nint)addr;
                        for (int l = 0; l < fixedArray.Count; l++)
                        {
                            object arrObj3;
                            arrObj3 = SafeMemory.PtrToStructure(arrAddr3, fixedArray.Type);
                            PrintOutObject(arrObj3, (ulong)((IntPtr)arrAddr3).ToInt64(), new List<string>(path) { $"_arrValue_{l}" }, autoExpand: false, $"[{l}] {arrObj3}");
                            arrAddr3 += Marshal.SizeOf(fixedArray.Type);
                        }
                        ImGui.TreePop();
                    }
                }
                else
                {
                    if (!ImGui.TreeNode($"Fixed {ParseTypeName(fixedBuffer.ElementType)} Buffer##{member.Name}-{addr}-{string.Join("-", path)}"))
                    {
                        return;
                    }
                    bool display;
                    display = true;
                    bool child;
                    child = false;
                    if (fixedBuffer.ElementType == typeof(byte) && fixedBuffer.Length > 128)
                    {
                        display = ImGui.BeginChild($"scrollBuffer##{member.Name}-{addr}-{string.Join("-", path)}", new Vector2(ImGui.GetTextLineHeight() * 30f, ImGui.GetTextLineHeight() * 8f), border: true);
                        child = true;
                    }
                    if (display)
                    {
                        float sX;
                        sX = ImGui.GetCursorPosX();
                        for (uint m = 0u; m < fixedBuffer.Length; m++)
                        {
                            if (fixedBuffer.ElementType == typeof(byte))
                            {
                                byte v;
                                v = *(byte*)(addr + m);
                                if (m != 0 && m % 16 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.SetCursorPosX(sX + ImGui.CalcTextSize(ImGui.GetIO().KeyShift ? "0000" : "000").X * (float)(m % 16));
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v:000}" : $"{v:X2}");
                            }
                            else if (fixedBuffer.ElementType == typeof(short))
                            {
                                short v7;
                                v7 = *(short*)(addr + m * 2);
                                if (m != 0 && m % 8 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v7:000000}" : $"{v7:X4}");
                            }
                            else if (fixedBuffer.ElementType == typeof(ushort))
                            {
                                ushort v8;
                                v8 = *(ushort*)(addr + m * 2);
                                if (m != 0 && m % 8 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v8:00000}" : $"{v8:X4}");
                            }
                            else if (fixedBuffer.ElementType == typeof(int))
                            {
                                int v6;
                                v6 = *(int*)(addr + m * 4);
                                if (m != 0 && m % 4 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v6:0000000000}" : $"{v6:X8}");
                            }
                            else if (fixedBuffer.ElementType == typeof(uint))
                            {
                                uint v5;
                                v5 = *(uint*)(addr + m * 4);
                                if (m != 0 && m % 4 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v5:000000000}" : $"{v5:X8}");
                            }
                            else if (fixedBuffer.ElementType == typeof(long))
                            {
                                long v4;
                                v4 = *(long*)(addr + m * 8);
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v4}" : $"{v4:X16}");
                            }
                            else if (fixedBuffer.ElementType == typeof(ulong))
                            {
                                ulong v3;
                                v3 = *(ulong*)(addr + m * 8);
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v3}" : $"{v3:X16}");
                            }
                            else
                            {
                                byte v2;
                                v2 = *(byte*)(addr + m);
                                if (m != 0 && m % 16 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.TextDisabled(ImGui.GetIO().KeyShift ? $"{v2:000}" : $"{v2:X2}");
                            }
                        }
                    }
                    if (child)
                    {
                        ImGui.EndChild();
                    }
                    ImGui.TreePop();
                }
            }
            else if (!type.IsPrimitive)
            {
                if (!(value is ILazyRow ilr))
                {
                    if (value is Lumina.Text.SeString seString)
                    {
                        ImGui.Text(seString.RawString ?? "");
                    }
                    else
                    {
                        PrintOutObject(value, addr, new List<string>(path));
                    }
                    return;
                }
                PropertyInfo p2;
                p2 = ilr.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
                if (p2 != null)
                {
                    MethodInfo getter;
                    getter = p2.GetGetMethod();
                    if (getter != null)
                    {
                        PrintOutObject(getter.Invoke(ilr, Array.Empty<object>()), addr, new List<string>(path));
                        return;
                    }
                }
                PrintOutObject(value, addr, new List<string>(path));
            }
            else if (value is nint p)
            {
                ulong pAddr;
                pAddr = (ulong)((IntPtr)p).ToInt64();
                ClickToCopyText($"{p:X}");
                if (BeginModule != 0 && pAddr >= BeginModule && pAddr <= EndModule)
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, 4291543295u);
                    ClickToCopyText($"ffxiv_dx11.exe+{pAddr - BeginModule:X}");
                    ImGui.PopStyleColor();
                }
            }
            else
            {
                ImGui.Text($"{value}");
            }
        }
        catch (Exception ex)
        {
            ImGui.Text($"{{{ex}}}");
        }
    }

    public unsafe static void PrintOutObject<T>(T* ptr, bool autoExpand = false, string headerText = null) where T : unmanaged
    {
        PrintOutObject(ptr, new List<string>(), autoExpand, headerText);
    }

    public unsafe static void PrintOutObject<T>(T* ptr, List<string> path, bool autoExpand = false, string headerText = null) where T : unmanaged
    {
        PrintOutObject(*ptr, (ulong)ptr, path, autoExpand, headerText);
    }

    public static void PrintOutObject(object obj, ulong addr, bool autoExpand = false, string headerText = null)
    {
        PrintOutObject(obj, addr, new List<string>(), autoExpand, headerText);
    }

    public static void SetSavedValue<T>(string key, T value)
    {
        if (global::AetherBox.AetherBox.Config.Debugging.SavedValues.ContainsKey(key))
        {
            global::AetherBox.AetherBox.Config.Debugging.SavedValues.Remove(key);
        }
        global::AetherBox.AetherBox.Config.Debugging.SavedValues.Add(key, value);
        global::AetherBox.AetherBox.Config.Save();
    }

    public static T GetSavedValue<T>(string key, T defaultValue)
    {
        if (!global::AetherBox.AetherBox.Config.Debugging.SavedValues.ContainsKey(key))
        {
            return defaultValue;
        }
        return (T)global::AetherBox.AetherBox.Config.Debugging.SavedValues[key];
    }

    private static string ParseTypeName(Type type, List<Type> loopSafety = null)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }
        if (loopSafety == null)
        {
            loopSafety = new List<Type>();
        }
        if (loopSafety.Contains(type))
        {
            return "..." + type.Name;
        }
        loopSafety.Add(type);
        string obj;
        obj = type.Name.Split('`')[0];
        IEnumerable<string> gArgs;
        gArgs = from t in type.GetGenericArguments()
                select ParseTypeName(t, loopSafety) ?? "";
        return obj + "<" + string.Join(',', gArgs) + ">";
    }

    public unsafe static void PrintOutObject(object obj, ulong addr, List<string> path, bool autoExpand = false, string headerText = null)
    {
        if (obj is Utf8String utf8String)
        {
            string text;
            text = string.Empty;
            Exception err;
            err = null;
            try
            {
                int s;
                s = (int)((utf8String.BufUsed > int.MaxValue) ? int.MaxValue : utf8String.BufUsed);
                if (s > 1)
                {
                    text = Encoding.UTF8.GetString(utf8String.StringPtr, s - 1);
                }
            }
            catch (Exception ex2)
            {
                err = ex2;
            }
            if (err != null)
            {
                ImGui.TextDisabled(err.Message);
                ImGui.SameLine();
            }
            else
            {
                ImGui.Text("\"" + text + "\"");
                ImGui.SameLine();
            }
        }
        int pushedColor;
        pushedColor = 0;
        bool openedNode;
        openedNode = false;
        try
        {
            if (EndModule == 0L && BeginModule == 0L)
            {
                try
                {
                    BeginModule = (ulong)((IntPtr)Process.GetCurrentProcess().MainModule.BaseAddress).ToInt64();
                    EndModule = BeginModule + (ulong)Process.GetCurrentProcess().MainModule.ModuleMemorySize;
                }
                catch
                {
                    EndModule = 1uL;
                }
            }
            ImGui.PushStyleColor(ImGuiCol.Text, 4278255615u);
            pushedColor++;
            if (autoExpand)
            {
                ImGui.SetNextItemOpen(is_open: true, ImGuiCond.Appearing);
            }
            if (headerText == null)
            {
                headerText = $"{obj}";
            }
            if (ImGui.TreeNode($"{headerText}##print-obj-{addr:X}-{string.Join("-", path)}"))
            {
                LayoutKind layoutKind;
                layoutKind = obj.GetType().StructLayoutAttribute?.Value ?? LayoutKind.Sequential;
                ulong offsetAddress;
                offsetAddress = 0uL;
                openedNode = true;
                ImGui.PopStyleColor();
                pushedColor--;
                FieldInfo[] fields;
                fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
                foreach (FieldInfo f in fields)
                {
                    if (f.IsStatic)
                    {
                        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.75f, 1f), "static");
                        ImGui.SameLine();
                    }
                    else
                    {
                        if (layoutKind == LayoutKind.Explicit && f.GetCustomAttribute(typeof(FieldOffsetAttribute)) is FieldOffsetAttribute o)
                        {
                            offsetAddress = (ulong)o.Value;
                        }
                        ImGui.PushStyleColor(ImGuiCol.Text, 4287137928u);
                        string addressText;
                        addressText = GetAddressString((void*)(addr + offsetAddress), ImGui.GetIO().KeyShift);
                        ClickToCopyText($"[0x{offsetAddress:X}]", addressText);
                        ImGui.PopStyleColor();
                        ImGui.SameLine();
                    }
                    FixedBufferAttribute fixedBuffer;
                    fixedBuffer = (FixedBufferAttribute)f.GetCustomAttribute(typeof(FixedBufferAttribute));
                    if (fixedBuffer != null)
                    {
                        FixedArrayAttribute fixedArray;
                        fixedArray = (FixedArrayAttribute)f.GetCustomAttribute(typeof(FixedArrayAttribute));
                        Attribute fixedSizeArray;
                        fixedSizeArray = f.GetCustomAttribute(typeof(FixedSizeArrayAttribute<>));
                        ImGui.Text("fixed");
                        ImGui.SameLine();
                        if (fixedSizeArray != null)
                        {
                            Type fixedType;
                            fixedType = fixedSizeArray.GetType().GetGenericArguments()[0];
                            int size;
                            size = (int)fixedSizeArray.GetType().GetProperty("Count").GetValue(fixedSizeArray);
                            ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), $"{ParseTypeName(fixedType)}[{size}]");
                        }
                        else if (fixedArray != null)
                        {
                            if (fixedArray.Type == typeof(string) && fixedArray.Count == 1)
                            {
                                ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), fixedArray.Type.Name ?? "");
                            }
                            else
                            {
                                ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), $"{fixedArray.Type.Name}[{fixedArray.Count:X}]");
                            }
                        }
                        else
                        {
                            ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), $"{fixedBuffer.ElementType.Name}[0x{fixedBuffer.Length:X}]");
                        }
                    }
                    else if (f.FieldType.IsArray)
                    {
                        Array arr;
                        arr = (Array)f.GetValue(obj);
                        ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), $"{ParseTypeName(f.FieldType.GetElementType() ?? f.FieldType)}[{arr.Length}]");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), ParseTypeName(f.FieldType) ?? "");
                    }
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.4f, 1f), f.Name + ": ");
                    string fullFieldName;
                    fullFieldName = (obj.GetType().FullName ?? "UnknownType") + "." + f.Name;
                    if (ImGui.GetIO().KeyShift && ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(fullFieldName);
                    }
                    if (ImGui.GetIO().KeyShift && ImGui.IsItemClicked())
                    {
                        ImGui.SetClipboardText(fullFieldName);
                    }
                    ImGui.SameLine();
                    if (fullFieldName == "FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject.Name" && fixedBuffer != null)
                    {
                        PrintOutObject(MemoryHelper.ReadSeString((nint)(addr + offsetAddress), fixedBuffer.Length), addr + offsetAddress);
                    }
                    else if (fixedBuffer != null)
                    {
                        PrintOutValue(addr + offsetAddress, new List<string>(path) { f.Name }, f.FieldType, f.GetValue(obj), f);
                    }
                    else if (f.FieldType == typeof(bool) && fullFieldName.StartsWith("FFXIVClientStructs.FFXIV"))
                    {
                        byte b;
                        b = *(byte*)(addr + offsetAddress);
                        PrintOutValue(addr + offsetAddress, new List<string>(path) { f.Name }, f.FieldType, b != 0, f);
                    }
                    else
                    {
                        PrintOutValue(addr + offsetAddress, new List<string>(path) { f.Name }, f.FieldType, f.GetValue(obj), f);
                    }
                    if (layoutKind == LayoutKind.Sequential && !f.IsStatic)
                    {
                        offsetAddress += (ulong)Marshal.SizeOf(f.FieldType);
                    }
                }
                PropertyInfo[] properties;
                properties = obj.GetType().GetProperties();
                foreach (PropertyInfo p in properties)
                {
                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), ParseTypeName(p.PropertyType) ?? "");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.2f, 0.6f, 0.4f, 1f), p.Name + ": ");
                    ImGui.SameLine();
                    if (p.PropertyType.IsByRefLike || p.GetMethod.GetParameters().Length != 0)
                    {
                        ImGui.TextDisabled("Unable to display");
                        continue;
                    }
                    PrintOutValue(addr, new List<string>(path) { p.Name }, p.PropertyType, p.GetValue(obj), p);
                }
                openedNode = false;
                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
                pushedColor--;
            }
        }
        catch (Exception ex)
        {
            ImGui.Text($"{{{ex}}}");
        }
        if (openedNode)
        {
            ImGui.TreePop();
        }
        if (pushedColor > 0)
        {
            ImGui.PopStyleColor(pushedColor);
        }
    }

    public unsafe static string GetAddressString(void* address, out bool isRelative, bool absoluteOnly = false)
    {
        ulong ulongAddress;
        ulongAddress = (ulong)address;
        isRelative = false;
        if (!absoluteOnly)
        {
            try
            {
                if (EndModule == 0L && BeginModule == 0L)
                {
                    try
                    {
                        BeginModule = (ulong)((IntPtr)Process.GetCurrentProcess().MainModule.BaseAddress).ToInt64();
                        EndModule = BeginModule + (ulong)Process.GetCurrentProcess().MainModule.ModuleMemorySize;
                    }
                    catch
                    {
                        EndModule = 1uL;
                    }
                }
            }
            catch
            {
            }
            if (BeginModule != 0 && ulongAddress >= BeginModule && ulongAddress <= EndModule)
            {
                isRelative = true;
                return $"ffxiv_dx11.exe+{ulongAddress - BeginModule:X}";
            }
            return $"{ulongAddress:X}";
        }
        return $"{ulongAddress:X}";
    }

    public unsafe static string GetAddressString(void* address, bool absoluteOnly = false)
    {
        bool isRelative;
        return GetAddressString(address, out isRelative, absoluteOnly);
    }

    public unsafe static void PrintAddress(void* address)
    {
        string addressString;
        addressString = GetAddressString(address, out var isRelative);
        if (isRelative)
        {
            ClickToCopyText(GetAddressString(address, absoluteOnly: true));
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, 4291543295u);
            ClickToCopyText(addressString);
            ImGui.PopStyleColor();
        }
        else
        {
            ClickToCopyText(addressString);
        }
    }
    #endregion


    /// <summary>
    /// Code to be executed when the window is closed.
    /// </summary>
    public override void OnClose()
    {
        try
        {
            AetherBox.Config.Save();
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at OnClose");
        }
    }
}
