using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using AetherBox;
using AetherBox.Debugging;
using AetherBox.Features;
using AetherBox.Helpers;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using ECommons.DalamudServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop.Attributes;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Text;

namespace AetherBox.Debugging;

public static class DebugManager
{
    private static readonly Dictionary<string, Action> DebugPages = new Dictionary<string, Action>();

    private static float SidebarSize = 0f;

    private static bool SetupDebugHelpers = false;

    private static readonly List<DebugHelper> DebugHelpers = new List<DebugHelper>();

    private static readonly Stopwatch InitDelay = Stopwatch.StartNew();

    private static ulong BeginModule = 0uL;

    private static ulong EndModule = 0uL;

    private static readonly Dictionary<string, object> SavedValues = new Dictionary<string, object>();

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
        foreach (FeatureProvider tp in global::AetherBox.AetherBox.Plugin.FeatureProviders)
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
                    debugger.Plugin = global::AetherBox.AetherBox.Plugin;
                    RegisterDebugPage(debugger.FullName, debugger.Draw);
                    DebugHelpers.Add(debugger);
                }
            }
        }
    }

    public static void DrawDebugWindow()
    {
        if (InitDelay.ElapsedMilliseconds < 500)
        {
            return;
        }
        if (global::AetherBox.AetherBox.Plugin == null)
        {
            Svc.Log.Info("null");
            return;
        }
        if (!SetupDebugHelpers)
        {
            SetupDebugHelpers = true;
            try
            {
                foreach (FeatureProvider tp in global::AetherBox.AetherBox.Plugin.FeatureProviders)
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
                        debugger.Plugin = global::AetherBox.AetherBox.Plugin;
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
                global::AetherBox.AetherBox.Plugin.DebugWindow.IsOpen = false;
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
        using (ImRaii.Child("###" + global::AetherBox.AetherBox.Name + "DebugPages", new Vector2(SidebarSize, -1f) * ImGui.GetIO().FontGlobalScale, border: true))
        {
            List<string> list;
            list = DebugPages.Keys.ToList();
            list.Sort((string s, string s1) => (s.StartsWith("[") && !s1.StartsWith("[")) ? 1 : string.CompareOrdinal(s, s1));
            foreach (string i in list)
            {
                if (ImGui.Selectable($"{i}##{AetherBox.Name}{"DebugPages"}{"Config"}", global::AetherBox.AetherBox.Config.Debugging.SelectedPage == i))
                {
                    global::AetherBox.AetherBox.Config.Debugging.SelectedPage = i;
                    global::AetherBox.AetherBox.Config.Save();
                }
            }
        }
        ImGui.TableNextColumn();
        using (ImRaii.Child("###" + global::AetherBox.AetherBox.Name + "DebugPagesView", new Vector2(-1f, -1f), border: true, ImGuiWindowFlags.HorizontalScrollbar))
        {
            if (string.IsNullOrEmpty(global::AetherBox.AetherBox.Config.Debugging.SelectedPage) || !DebugPages.ContainsKey(global::AetherBox.AetherBox.Config.Debugging.SelectedPage))
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
            Attribute? customAttribute = member.GetCustomAttribute(typeof(ValueParser));
            FixedBufferAttribute fixedBuffer = (FixedBufferAttribute)member.GetCustomAttribute(typeof(FixedBufferAttribute));
            FixedArrayAttribute fixedArray = (FixedArrayAttribute)member.GetCustomAttribute(typeof(FixedArrayAttribute));
            Attribute fixedSizeArray = member.GetCustomAttribute(typeof(FixedSizeArrayAttribute<>));
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
                        Type eType = type.GetElementType();
                        object? obj = SafeMemory.PtrToStructure(new IntPtr(unboxed), eType);
                        ImGui.SameLine();
                        PrintOutObject(obj, (ulong)unboxedAddr, new List<string>(path));
                        return;
                    }
                    catch
                    {
                        return;
                    }
                }
                else
                {
                    ImGui.Text("null");
                }
                ImGui.Text("null");
            }
            else if (type.IsArray)
            {
                Array arr = (Array)value;
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
                    Type fixedType = fixedSizeArray.GetType().GetGenericArguments()[0];
                    int size = (int)fixedSizeArray.GetType().GetProperty("Count").GetValue(fixedSizeArray);
                    if (!ImGui.TreeNode($"Fixed {ParseTypeName(fixedType)} Array##{member.Name}-{addr}-{string.Join("-", path)}"))
                    {
                        return;
                    }
                    if (fixedType.Namespace + "." + fixedType.Name == "FFXIVClientStructs.Interop.Pointer`1")
                    {
                        Type pointerType = fixedType.GetGenericArguments()[0];
                        void** arrAddr = (void**)addr;
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
                                object arrObj = SafeMemory.PtrToStructure(new IntPtr(arrAddr[j]), pointerType);
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
                        nint arrAddr2 = (nint)addr;
                        for (int k = 0; k < size; k++)
                        {
                            object arrObj2 = SafeMemory.PtrToStructure(arrAddr2, fixedType);
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
                        string text = Marshal.PtrToStringUTF8((nint)addr);
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
                        nint arrAddr3 = (nint)addr;
                        for (int l = 0; l < fixedArray.Count; l++)
                        {
                            object arrObj3 = SafeMemory.PtrToStructure(arrAddr3, fixedArray.Type);
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
                    bool display = true;
                    bool child = false;
                    if (fixedBuffer.ElementType == typeof(byte) && fixedBuffer.Length > 128)
                    {
                        display = ImGui.BeginChild($"scrollBuffer##{member.Name}-{addr}-{string.Join("-", path)}", new Vector2(ImGui.GetTextLineHeight() * 30f, ImGui.GetTextLineHeight() * 8f), border: true);
                        child = true;
                    }
                    if (display)
                    {
                        float sX = ImGui.GetCursorPosX();
                        for (uint m = 0u; m < fixedBuffer.Length; m++)
                        {
                            if (fixedBuffer.ElementType == typeof(byte))
                            {
                                byte v = *(byte*)(addr + m);
                                if (m != 0 && m % 16 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.SetCursorPosX(sX + ImGui.CalcTextSize(ImGui.GetIO().KeyShift ? "0000" : "000").X * (float)(m % 16));
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v:000}" : $"{v:X2}");
                            }
                            else if (fixedBuffer.ElementType == typeof(short))
                            {
                                short v7 = *(short*)(addr + m * 2);
                                if (m != 0 && m % 8 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v7:000000}" : $"{v7:X4}");
                            }
                            else if (fixedBuffer.ElementType == typeof(ushort))
                            {
                                ushort v8 = *(ushort*)(addr + m * 2);
                                if (m != 0 && m % 8 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v8:00000}" : $"{v8:X4}");
                            }
                            else if (fixedBuffer.ElementType == typeof(int))
                            {
                                int v6 = *(int*)(addr + m * 4);
                                if (m != 0 && m % 4 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v6:0000000000}" : $"{v6:X8}");
                            }
                            else if (fixedBuffer.ElementType == typeof(uint))
                            {
                                uint v5 = *(uint*)(addr + m * 4);
                                if (m != 0 && m % 4 != 0)
                                {
                                    ImGui.SameLine();
                                }
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v5:000000000}" : $"{v5:X8}");
                            }
                            else if (fixedBuffer.ElementType == typeof(long))
                            {
                                long v4 = *(long*)(addr + m * 8);
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v4}" : $"{v4:X16}");
                            }
                            else if (fixedBuffer.ElementType == typeof(ulong))
                            {
                                ulong v3 = *(ulong*)(addr + m * 8);
                                ImGui.Text(ImGui.GetIO().KeyShift ? $"{v3}" : $"{v3:X16}");
                            }
                            else
                            {
                                byte v2 = *(byte*)(addr + m);
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
                PropertyInfo p2 = ilr.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
                if (p2 != null)
                {
                    MethodInfo getter = p2.GetGetMethod();
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
                ulong pAddr = (ulong)((IntPtr)p).ToInt64();
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
}
