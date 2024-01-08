using AetherBox.Features;
using AetherBox.Helpers;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using ECommons.DalamudServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop.Attributes;
using ImGuiNET;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#nullable disable
namespace AetherBox.Debugging
{
    public static class DebugManager
    {
        private static readonly Dictionary<string, Action> DebugPages = new Dictionary<string, Action>();
        private static float SidebarSize = 0.0f;
        private static bool SetupDebugHelpers = false;
        private static readonly List<DebugHelper> DebugHelpers = new List<DebugHelper>();
        private static readonly Stopwatch InitDelay = Stopwatch.StartNew();
        private static ulong BeginModule = 0;
        private static ulong EndModule = 0;
        private static readonly Dictionary<string, object> SavedValues = new Dictionary<string, object>();

        public static void RegisterDebugPage(string key, Action action)
        {
            if (DebugManager.DebugPages.ContainsKey(key))
                DebugManager.DebugPages[key] = action;
            else
                DebugManager.DebugPages.Add(key, action);
            DebugManager.SidebarSize = 0.0f;
        }

        public static void RemoveDebugPage(string key)
        {
            if (DebugManager.DebugPages.ContainsKey(key))
                DebugManager.DebugPages.Remove(key);
            DebugManager.SidebarSize = 0.0f;
        }

        public static void Reload()
        {
            DebugManager.DebugHelpers.RemoveAll((Predicate<DebugHelper>)(dh =>
            {
                if (!dh.FeatureProvider.Disposed)
                    return false;
                DebugManager.RemoveDebugPage(dh.FullName);
                dh.Dispose();
                return true;
            }));
            foreach (FeatureProvider featureProvider in AetherBox.P.FeatureProviders)
            {
                if (!featureProvider.Disposed)
                {
                    foreach (Type type in ((IEnumerable<Type>)featureProvider.Assembly.GetTypes()).Where<Type>((Func<Type, bool>)(t => t.IsSubclassOf(typeof(DebugHelper)) && !t.IsAbstract)))
                    {
                        Type t = type;
                        if (!DebugManager.DebugHelpers.Any<DebugHelper>((Func<DebugHelper, bool>)(h => h.GetType() == t)))
                        {
                            DebugHelper instance = (DebugHelper) Activator.CreateInstance(t);
                            instance.FeatureProvider = featureProvider;
                            instance.Plugin = AetherBox.P;
                            DebugManager.RegisterDebugPage(instance.FullName, new Action(instance.Draw));
                            DebugManager.DebugHelpers.Add(instance);
                        }
                    }
                }
            }
        }

        public static void DrawDebugWindow()
        {
            if (DebugManager.InitDelay.ElapsedMilliseconds < 500L)
                return;
            if (AetherBox.P == null)
            {
                Svc.Log.Info("null");
            }
            else
            {
                if (!DebugManager.SetupDebugHelpers)
                {
                    DebugManager.SetupDebugHelpers = true;
                    try
                    {
                        foreach (FeatureProvider featureProvider in AetherBox.P.FeatureProviders)
                        {
                            if (!featureProvider.Disposed)
                            {
                                foreach (Type type in ((IEnumerable<Type>)featureProvider.Assembly.GetTypes()).Where<Type>((Func<Type, bool>)(t => t.IsSubclassOf(typeof(DebugHelper)) && !t.IsAbstract)))
                                {
                                    DebugHelper instance = (DebugHelper) Activator.CreateInstance(type);
                                    instance.FeatureProvider = featureProvider;
                                    instance.Plugin = AetherBox.P;
                                    DebugManager.RegisterDebugPage(instance.FullName, new Action(instance.Draw));
                                    DebugManager.DebugHelpers.Add(instance);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Error(ex, "");
                        DebugManager.SetupDebugHelpers = false;
                        DebugManager.DebugHelpers.Clear();
                        AetherBox.P.DebugWindow.IsOpen = false;
                        return;
                    }
                }
                if ((double)DebugManager.SidebarSize < 150.0)
                {
                    DebugManager.SidebarSize = 150f;
                    try
                    {
                        foreach (string key in DebugManager.DebugPages.Keys)
                        {
                            double x = (double) ImGui.CalcTextSize(key).X;
                            ImGuiStylePtr style = ImGui.GetStyle();
                            double num1 = (double) style.FramePadding.X * 5.0;
                            double num2 = x + num1;
                            style = ImGui.GetStyle();
                            double num3 = (double) style.ScrollbarSize;
                            float num4 = (float) (num2 + num3);
                            if ((double)num4 > (double)DebugManager.SidebarSize)
                                DebugManager.SidebarSize = num4;
                        }
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Error(ex, "");
                    }
                }
                if (ImGui.BeginChild("###" + AetherBox.Name + "DebugPages", new Vector2(DebugManager.SidebarSize, -1f) * ImGui.GetIO().FontGlobalScale, true))
                {
                    List<string> list = DebugManager.DebugPages.Keys.ToList<string>();
                    list.Sort((Comparison<string>)((s, s1) => s.StartsWith("[") && !s1.StartsWith("[") ? 1 : string.CompareOrdinal(s, s1)));
                    foreach (string str in list)
                    {
                        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 4);
                        interpolatedStringHandler.AppendFormatted(str);
                        interpolatedStringHandler.AppendLiteral("##");
                        interpolatedStringHandler.AppendFormatted(AetherBox.Name);
                        interpolatedStringHandler.AppendFormatted("DebugPages");
                        interpolatedStringHandler.AppendFormatted("Config");
                        if (ImGui.Selectable(interpolatedStringHandler.ToStringAndClear(), AetherBox.Config.Debugging.SelectedPage == str))
                        {
                            AetherBox.Config.Debugging.SelectedPage = str;
                            AetherBox.Config.Save();
                        }
                    }
                }
                ImGui.EndChild();
                ImGui.SameLine();
                if (ImGui.BeginChild("###" + AetherBox.Name + "DebugPagesView", new Vector2(-1f, -1f), true, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    if (!string.IsNullOrEmpty(AetherBox.Config.Debugging.SelectedPage))
                    {
                        if (DebugManager.DebugPages.ContainsKey(AetherBox.Config.Debugging.SelectedPage))
                        {
                            try
                            {
                                DebugManager.DebugPages[AetherBox.Config.Debugging.SelectedPage]();
                                goto label_41;
                            }
                            catch (Exception ex)
                            {
                                Svc.Log.Error(ex, "");
                                ImGui.TextColored(new Vector4(1f, 0.0f, 0.0f, 1f), ex.ToString());
                                goto label_41;
                            }
                        }
                    }
                    ImGui.Text("Select Debug Page");
                }
            label_41:
                ImGui.EndChild();
            }
        }

        public static void Dispose()
        {
            foreach (DebugHelper debugHelper in DebugManager.DebugHelpers)
            {
                DebugManager.RemoveDebugPage(debugHelper.FullName);
                debugHelper.Dispose();
            }
            DebugManager.DebugHelpers.Clear();
            DebugManager.DebugPages.Clear();
        }

        private static unsafe Vector2 GetNodePosition(AtkResNode* node)
        {
            Vector2 nodePosition = new Vector2(node->X, node->Y);
            for (AtkResNode* parentNode = node->ParentNode; (IntPtr)parentNode != IntPtr.Zero; parentNode = parentNode->ParentNode)
                nodePosition = nodePosition * new Vector2(parentNode->ScaleX, parentNode->ScaleY) + new Vector2(parentNode->X, parentNode->Y);
            return nodePosition;
        }

        private static unsafe Vector2 GetNodeScale(AtkResNode* node)
        {
            if ((IntPtr)node == IntPtr.Zero)
                return new Vector2(1f, 1f);
            Vector2 nodeScale = new Vector2(node->ScaleX, node->ScaleY);
            while ((IntPtr)node->ParentNode != IntPtr.Zero)
            {
                node = node->ParentNode;
                nodeScale *= new Vector2(node->ScaleX, node->ScaleY);
            }
            return nodeScale;
        }

        private static unsafe bool GetNodeVisible(AtkResNode* node)
        {
            if ((IntPtr)node == IntPtr.Zero)
                return false;
            for (; (IntPtr)node != IntPtr.Zero; node = node->ParentNode)
            {
                if (!node->IsVisible)
                    return false;
            }
            return true;
        }

        public static unsafe void HighlightResNode(AtkResNode* node)
        {
            Vector2 nodePosition = DebugManager.GetNodePosition(node);
            Vector2 nodeScale = DebugManager.GetNodeScale(node);
            Vector2 vector2 = new Vector2((float) node->Width, (float) node->Height) * nodeScale;
            bool nodeVisible = DebugManager.GetNodeVisible(node);
            ImDrawListPtr foregroundDrawList = ImGui.GetForegroundDrawList();
            foregroundDrawList.AddRectFilled(nodePosition, nodePosition + vector2, nodeVisible ? 1426128640U : 1426063615U);
            foregroundDrawList = ImGui.GetForegroundDrawList();
            foregroundDrawList.AddRect(nodePosition, nodePosition + vector2, nodeVisible ? 4278255360U : 4278190335U);
        }

        public static void ClickToCopyText(string text, string textCopy = null)
        {
            if (textCopy == null)
                textCopy = text;
            ImGui.Text(text ?? "");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (textCopy != text)
                    ImGui.SetTooltip(textCopy);
            }
            if (!ImGui.IsItemClicked())
                return;
            ImGui.SetClipboardText(textCopy ?? "");
        }

        public static unsafe void ClickToCopy(void* address)
        {
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
            interpolatedStringHandler.AppendFormatted<ulong>((ulong)address, "X");
            DebugManager.ClickToCopyText(interpolatedStringHandler.ToStringAndClear());
        }

        public static unsafe void ClickToCopy<T>(T* address) where T : unmanaged
        {
            DebugManager.ClickToCopy((void*)address);
        }

        public static void SeStringToText(Dalamud.Game.Text.SeStringHandling.SeString seStr)
        {
            int count = 0;
            ImGui.BeginGroup();
            foreach (Payload payload in seStr.Payloads)
            {
                if (!(payload is UIForegroundPayload foregroundPayload))
                {
                    if (payload is TextPayload textPayload)
                    {
                        ImGui.Text(textPayload.Text ?? "");
                        ImGui.SameLine();
                    }
                }
                else if (foregroundPayload.ColorKey == (ushort)0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, uint.MaxValue);
                    ++count;
                }
                else
                {
                    uint num1 = foregroundPayload.UIColor.UIForeground >> 24 & (uint) byte.MaxValue;
                    uint num2 = foregroundPayload.UIColor.UIForeground >> 16 & (uint) byte.MaxValue;
                    uint num3 = foregroundPayload.UIColor.UIForeground >> 8 & (uint) byte.MaxValue;
                    int uiForeground = (int) foregroundPayload.UIColor.UIForeground;
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4((float)num1 / (float)byte.MaxValue, (float)num2 / (float)byte.MaxValue, (float)num3 / (float)byte.MaxValue, 1f));
                    ++count;
                }
            }
            ImGui.EndGroup();
            if (count <= 0)
                return;
            ImGui.PopStyleColor(count);
        }

        private static unsafe void PrintOutValue(
          ulong addr,
          List<string> path,
          Type type,
          object value,
          MemberInfo member)
        {
            DefaultInterpolatedStringHandler interpolatedStringHandler;
            try
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    value = type.GetMethod("ToArray")?.Invoke(value, (object[])null);
                    type = value.GetType();
                }
                Attribute customAttribute1 = member.GetCustomAttribute(typeof (ValueParser));
                FixedBufferAttribute customAttribute2 = (FixedBufferAttribute) member.GetCustomAttribute(typeof (FixedBufferAttribute));
                FixedArrayAttribute customAttribute3 = (FixedArrayAttribute) member.GetCustomAttribute(typeof (FixedArrayAttribute));
                Attribute customAttribute4 = member.GetCustomAttribute(typeof (FixedSizeArrayAttribute<>));
                if (customAttribute1 is ValueParser valueParser)
                    valueParser.ImGuiPrint(type, value, member, addr);
                else if (type.IsPointer)
                {
                    void* voidPtr = Pointer.Unbox((object) (Pointer) value);
                    if ((IntPtr)voidPtr != IntPtr.Zero)
                    {
                        ulong num = (ulong) voidPtr;
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                        interpolatedStringHandler.AppendFormatted<ulong>((ulong)voidPtr, "X");
                        DebugManager.ClickToCopyText(interpolatedStringHandler.ToStringAndClear());
                        if (DebugManager.BeginModule > 0UL && num >= DebugManager.BeginModule && num <= DebugManager.EndModule)
                        {
                            ImGui.SameLine();
                            ImGui.PushStyleColor(ImGuiCol.Text, 4291543295U);
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 1);
                            interpolatedStringHandler.AppendLiteral("ffxiv_dx11.exe+");
                            interpolatedStringHandler.AppendFormatted<ulong>(num - DebugManager.BeginModule, "X");
                            DebugManager.ClickToCopyText(interpolatedStringHandler.ToStringAndClear());
                            ImGui.PopStyleColor();
                        }
                        try
                        {
                            Type elementType = type.GetElementType();
                            object structure = SafeMemory.PtrToStructure(new IntPtr(voidPtr), elementType);
                            ImGui.SameLine();
                            long addr1 = (long) voidPtr;
                            List<string> path1 = new List<string>((IEnumerable<string>) path);
                            DebugManager.PrintOutObject(structure, (ulong)addr1, path1);
                        }
                        catch
                        {
                        }
                    }
                    else
                        ImGui.Text("null");
                }
                else if (type.IsArray)
                {
                    Array array = (Array) value;
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 3);
                    interpolatedStringHandler.AppendLiteral("Values##");
                    interpolatedStringHandler.AppendFormatted(member.Name);
                    interpolatedStringHandler.AppendLiteral("-");
                    interpolatedStringHandler.AppendFormatted<ulong>(addr);
                    interpolatedStringHandler.AppendLiteral("-");
                    interpolatedStringHandler.AppendFormatted(string.Join("-", (IEnumerable<string>)path));
                    if (!ImGui.TreeNode(interpolatedStringHandler.ToStringAndClear()))
                        return;
                    for (int index = 0; index < array.Length; ++index)
                    {
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
                        interpolatedStringHandler.AppendLiteral("[");
                        interpolatedStringHandler.AppendFormatted<int>(index);
                        interpolatedStringHandler.AppendLiteral("]");
                        ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                        ImGui.SameLine();
                        long addr2 = (long) addr;
                        List<string> path2 = new List<string>((IEnumerable<string>) path);
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
                        interpolatedStringHandler.AppendLiteral("_arrValue_");
                        interpolatedStringHandler.AppendFormatted<int>(index);
                        path2.Add(interpolatedStringHandler.ToStringAndClear());
                        Type elementType = type.GetElementType();
                        object obj = array.GetValue(index);
                        MemberInfo member1 = member;
                        DebugManager.PrintOutValue((ulong)addr2, path2, elementType, obj, member1);
                    }
                    ImGui.TreePop();
                }
                else if (customAttribute2 != null)
                {
                    if (customAttribute4 != null)
                    {
                        Type genericArgument1 = customAttribute4.GetType().GetGenericArguments()[0];
                        int num = (int) customAttribute4.GetType().GetProperty("Count").GetValue((object) customAttribute4);
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 4);
                        interpolatedStringHandler.AppendLiteral("Fixed ");
                        interpolatedStringHandler.AppendFormatted(DebugManager.ParseTypeName(genericArgument1));
                        interpolatedStringHandler.AppendLiteral(" Array##");
                        interpolatedStringHandler.AppendFormatted(member.Name);
                        interpolatedStringHandler.AppendLiteral("-");
                        interpolatedStringHandler.AppendFormatted<ulong>(addr);
                        interpolatedStringHandler.AppendLiteral("-");
                        interpolatedStringHandler.AppendFormatted(string.Join("-", (IEnumerable<string>)path));
                        if (!ImGui.TreeNode(interpolatedStringHandler.ToStringAndClear()))
                            return;
                        if (genericArgument1.Namespace + "." + genericArgument1.Name == "FFXIVClientStructs.Interop.Pointer`1")
                        {
                            Type genericArgument2 = genericArgument1.GetGenericArguments()[0];
                            void** voidPtr = (void**) addr;
                            ImGuiIOPtr io;
                            if ((IntPtr)voidPtr != IntPtr.Zero)
                            {
                                for (int index = 0; index < num; ++index)
                                {
                                    if ((IntPtr)voidPtr[index] == IntPtr.Zero)
                                    {
                                        io = ImGui.GetIO();
                                        if (io.KeyAlt)
                                        {
                                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 1);
                                            interpolatedStringHandler.AppendLiteral("[");
                                            interpolatedStringHandler.AppendFormatted<int>(index);
                                            interpolatedStringHandler.AppendLiteral("] null");
                                            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                                        }
                                    }
                                    else
                                    {
                                        object structure = SafeMemory.PtrToStructure(new IntPtr(voidPtr[index]), genericArgument2);
                                        if (structure == null)
                                        {
                                            io = ImGui.GetIO();
                                            if (io.KeyAlt)
                                            {
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 1);
                                                interpolatedStringHandler.AppendLiteral("[");
                                                interpolatedStringHandler.AppendFormatted<int>(index);
                                                interpolatedStringHandler.AppendLiteral("] error");
                                                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                                            }
                                        }
                                        else
                                        {
                                            object obj = structure;
                                            long addr3 = (long) voidPtr[index];
                                            List<string> path3 = new List<string>((IEnumerable<string>) path);
                                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
                                            interpolatedStringHandler.AppendLiteral("_arrValue_");
                                            interpolatedStringHandler.AppendFormatted<int>(index);
                                            path3.Add(interpolatedStringHandler.ToStringAndClear());
                                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
                                            interpolatedStringHandler.AppendLiteral("[");
                                            interpolatedStringHandler.AppendFormatted<int>(index);
                                            interpolatedStringHandler.AppendLiteral("] ");
                                            interpolatedStringHandler.AppendFormatted<object>(structure);
                                            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                            DebugManager.PrintOutObject(obj, (ulong)addr3, path3, headerText: stringAndClear);
                                        }
                                    }
                                }
                            }
                            else
                                ImGui.Text("Null Pointer");
                        }
                        else if (genericArgument1.IsGenericType)
                        {
                            ImGui.Text("Unable to display generic types.");
                        }
                        else
                        {
                            IntPtr addr4 = (IntPtr) (long) addr;
                            for (int index = 0; index < num; ++index)
                            {
                                object structure = SafeMemory.PtrToStructure(addr4, genericArgument1);
                                object obj = structure;
                                long int64 = addr4.ToInt64();
                                List<string> path4 = new List<string>((IEnumerable<string>) path);
                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
                                interpolatedStringHandler.AppendLiteral("_arrValue_");
                                interpolatedStringHandler.AppendFormatted<int>(index);
                                path4.Add(interpolatedStringHandler.ToStringAndClear());
                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
                                interpolatedStringHandler.AppendLiteral("[");
                                interpolatedStringHandler.AppendFormatted<int>(index);
                                interpolatedStringHandler.AppendLiteral("] ");
                                interpolatedStringHandler.AppendFormatted<object>(structure);
                                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                DebugManager.PrintOutObject(obj, (ulong)int64, path4, headerText: stringAndClear);
                                addr4 += (IntPtr)Marshal.SizeOf(genericArgument1);
                            }
                        }
                        ImGui.TreePop();
                    }
                    else if (customAttribute3 != null)
                    {
                        if (customAttribute3.Type == typeof(string) && customAttribute3.Count == 1)
                        {
                            string stringUtF8 = Marshal.PtrToStringUTF8((IntPtr) (long) addr);
                            if (stringUtF8 != null)
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));
                                ImGui.TextDisabled("\"");
                                ImGui.SameLine();
                                ImGui.Text(stringUtF8);
                                ImGui.SameLine();
                                ImGui.PopStyleVar();
                                ImGui.TextDisabled("\"");
                            }
                            else
                                ImGui.TextDisabled("null");
                        }
                        else
                        {
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 4);
                            interpolatedStringHandler.AppendLiteral("Fixed ");
                            interpolatedStringHandler.AppendFormatted(DebugManager.ParseTypeName(customAttribute3.Type));
                            interpolatedStringHandler.AppendLiteral(" Array##");
                            interpolatedStringHandler.AppendFormatted(member.Name);
                            interpolatedStringHandler.AppendLiteral("-");
                            interpolatedStringHandler.AppendFormatted<ulong>(addr);
                            interpolatedStringHandler.AppendLiteral("-");
                            interpolatedStringHandler.AppendFormatted(string.Join("-", (IEnumerable<string>)path));
                            if (!ImGui.TreeNode(interpolatedStringHandler.ToStringAndClear()))
                                return;
                            IntPtr addr5 = (IntPtr) (long) addr;
                            for (int index = 0; index < customAttribute3.Count; ++index)
                            {
                                object structure = SafeMemory.PtrToStructure(addr5, customAttribute3.Type);
                                object obj = structure;
                                long int64 = addr5.ToInt64();
                                List<string> path5 = new List<string>((IEnumerable<string>) path);
                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
                                interpolatedStringHandler.AppendLiteral("_arrValue_");
                                interpolatedStringHandler.AppendFormatted<int>(index);
                                path5.Add(interpolatedStringHandler.ToStringAndClear());
                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
                                interpolatedStringHandler.AppendLiteral("[");
                                interpolatedStringHandler.AppendFormatted<int>(index);
                                interpolatedStringHandler.AppendLiteral("] ");
                                interpolatedStringHandler.AppendFormatted<object>(structure);
                                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                DebugManager.PrintOutObject(obj, (ulong)int64, path5, headerText: stringAndClear);
                                addr5 += (IntPtr)Marshal.SizeOf(customAttribute3.Type);
                            }
                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 4);
                        interpolatedStringHandler.AppendLiteral("Fixed ");
                        interpolatedStringHandler.AppendFormatted(DebugManager.ParseTypeName(customAttribute2.ElementType));
                        interpolatedStringHandler.AppendLiteral(" Buffer##");
                        interpolatedStringHandler.AppendFormatted(member.Name);
                        interpolatedStringHandler.AppendLiteral("-");
                        interpolatedStringHandler.AppendFormatted<ulong>(addr);
                        interpolatedStringHandler.AppendLiteral("-");
                        interpolatedStringHandler.AppendFormatted(string.Join("-", (IEnumerable<string>)path));
                        if (!ImGui.TreeNode(interpolatedStringHandler.ToStringAndClear()))
                            return;
                        bool flag1 = true;
                        bool flag2 = false;
                        if (customAttribute2.ElementType == typeof(byte) && customAttribute2.Length > 128)
                        {
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 3);
                            interpolatedStringHandler.AppendLiteral("scrollBuffer##");
                            interpolatedStringHandler.AppendFormatted(member.Name);
                            interpolatedStringHandler.AppendLiteral("-");
                            interpolatedStringHandler.AppendFormatted<ulong>(addr);
                            interpolatedStringHandler.AppendLiteral("-");
                            interpolatedStringHandler.AppendFormatted(string.Join("-", (IEnumerable<string>)path));
                            flag1 = ImGui.BeginChild(interpolatedStringHandler.ToStringAndClear(), new Vector2(ImGui.GetTextLineHeight() * 30f, ImGui.GetTextLineHeight() * 8f), true);
                            flag2 = true;
                        }
                        if (flag1)
                        {
                            float cursorPosX = ImGui.GetCursorPosX();
                            ImGuiIOPtr io;
                            for (uint index = 0; (long)index < (long)customAttribute2.Length; ++index)
                            {
                                if (customAttribute2.ElementType == typeof(byte))
                                {
                                    byte num1 = *(byte*) (addr + (ulong) index);
                                    if (index != 0U && index % 16U != 0U)
                                        ImGui.SameLine();
                                    double num2 = (double) cursorPosX;
                                    io = ImGui.GetIO();
                                    double num3 = (double) ImGui.CalcTextSize(io.KeyShift ? "0000" : "000").X * (double) (index % 16U);
                                    ImGui.SetCursorPosX((float)(num2 + num3));
                                    io = ImGui.GetIO();
                                    string stringAndClear;
                                    if (!io.KeyShift)
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<byte>(num1, "X2");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    else
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<byte>(num1, "000");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    ImGui.Text(stringAndClear);
                                }
                                else if (customAttribute2.ElementType == typeof(short))
                                {
                                    short num = *(short*) (addr + (ulong) (index * 2U));
                                    if (index != 0U && index % 8U != 0U)
                                        ImGui.SameLine();
                                    io = ImGui.GetIO();
                                    string stringAndClear;
                                    if (!io.KeyShift)
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<short>(num, "X4");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    else
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<short>(num, "000000");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    ImGui.Text(stringAndClear);
                                }
                                else if (customAttribute2.ElementType == typeof(ushort))
                                {
                                    ushort num = *(ushort*) (addr + (ulong) (index * 2U));
                                    if (index != 0U && index % 8U != 0U)
                                        ImGui.SameLine();
                                    io = ImGui.GetIO();
                                    string stringAndClear;
                                    if (!io.KeyShift)
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<ushort>(num, "X4");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    else
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<ushort>(num, "00000");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    ImGui.Text(stringAndClear);
                                }
                                else if (customAttribute2.ElementType == typeof(int))
                                {
                                    int num = *(int*) (addr + (ulong) (index * 4U));
                                    if (index != 0U && index % 4U != 0U)
                                        ImGui.SameLine();
                                    io = ImGui.GetIO();
                                    string stringAndClear;
                                    if (!io.KeyShift)
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<int>(num, "X8");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    else
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<int>(num, "0000000000");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    ImGui.Text(stringAndClear);
                                }
                                else if (customAttribute2.ElementType == typeof(uint))
                                {
                                    uint num = *(uint*) (addr + (ulong) (index * 4U));
                                    if (index != 0U && index % 4U != 0U)
                                        ImGui.SameLine();
                                    io = ImGui.GetIO();
                                    string stringAndClear;
                                    if (!io.KeyShift)
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<uint>(num, "X8");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    else
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<uint>(num, "000000000");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    ImGui.Text(stringAndClear);
                                }
                                else if (customAttribute2.ElementType == typeof(long))
                                {
                                    long num = *(long*) (addr + (ulong) (index * 8U));
                                    io = ImGui.GetIO();
                                    string stringAndClear;
                                    if (!io.KeyShift)
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<long>(num, "X16");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    else
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<long>(num);
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    ImGui.Text(stringAndClear);
                                }
                                else if (customAttribute2.ElementType == typeof(ulong))
                                {
                                    ulong num = (ulong) *(long*) (addr + (ulong) (index * 8U));
                                    io = ImGui.GetIO();
                                    string stringAndClear;
                                    if (!io.KeyShift)
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<ulong>(num, "X16");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    else
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<ulong>(num);
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    ImGui.Text(stringAndClear);
                                }
                                else
                                {
                                    byte num = *(byte*) (addr + (ulong) index);
                                    if (index != 0U && index % 16U != 0U)
                                        ImGui.SameLine();
                                    io = ImGui.GetIO();
                                    string stringAndClear;
                                    if (!io.KeyShift)
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<byte>(num, "X2");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    else
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                                        interpolatedStringHandler.AppendFormatted<byte>(num, "000");
                                        stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    }
                                    ImGui.TextDisabled(stringAndClear);
                                }
                            }
                        }
                        if (flag2)
                            ImGui.EndChild();
                        ImGui.TreePop();
                    }
                }
                else if (!type.IsPrimitive)
                {
                    switch (value)
                    {
                        case ILazyRow lazyRow:
                            PropertyInfo property = lazyRow.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
                            if (property != (PropertyInfo)null)
                            {
                                MethodInfo getMethod = property.GetGetMethod();
                                if (getMethod != (MethodInfo)null)
                                {
                                    DebugManager.PrintOutObject(getMethod.Invoke((object)lazyRow, Array.Empty<object>()), addr, new List<string>((IEnumerable<string>)path));
                                    break;
                                }
                            }
                            DebugManager.PrintOutObject(value, addr, new List<string>((IEnumerable<string>)path));
                            break;
                        case Lumina.Text.SeString seString:
                            ImGui.Text(seString.RawString ?? "");
                            break;
                        default:
                            DebugManager.PrintOutObject(value, addr, new List<string>((IEnumerable<string>)path));
                            break;
                    }
                }
                else if (value is IntPtr num4)
                {
                    ulong int64 = (ulong) num4.ToInt64();
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                    interpolatedStringHandler.AppendFormatted<IntPtr>(num4, "X");
                    DebugManager.ClickToCopyText(interpolatedStringHandler.ToStringAndClear());
                    if (DebugManager.BeginModule <= 0UL || int64 < DebugManager.BeginModule || int64 > DebugManager.EndModule)
                        return;
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, 4291543295U);
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 1);
                    interpolatedStringHandler.AppendLiteral("ffxiv_dx11.exe+");
                    interpolatedStringHandler.AppendFormatted<ulong>(int64 - DebugManager.BeginModule, "X");
                    DebugManager.ClickToCopyText(interpolatedStringHandler.ToStringAndClear());
                    ImGui.PopStyleColor();
                }
                else
                {
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                    interpolatedStringHandler.AppendFormatted<object>(value);
                    ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                }
            }
            catch (Exception ex)
            {
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
                interpolatedStringHandler.AppendLiteral("{");
                interpolatedStringHandler.AppendFormatted<Exception>(ex);
                interpolatedStringHandler.AppendLiteral("}");
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            }
        }

        public static unsafe void PrintOutObject<T>(T* ptr, bool autoExpand = false, string headerText = null) where T : unmanaged
        {
            DebugManager.PrintOutObject<T>(ptr, new List<string>(), autoExpand, headerText);
        }

        public static unsafe void PrintOutObject<T>(
          T* ptr,
          List<string> path,
          bool autoExpand = false,
          string headerText = null)
          where T : unmanaged
        {
            DebugManager.PrintOutObject((object)*ptr, (ulong)ptr, path, autoExpand, headerText);
        }

        public static void PrintOutObject(object obj, ulong addr, bool autoExpand = false, string headerText = null)
        {
            DebugManager.PrintOutObject(obj, addr, new List<string>(), autoExpand, headerText);
        }

        public static void SetSavedValue<T>(string key, T value)
        {
            if (AetherBox.Config.Debugging.SavedValues.ContainsKey(key))
                AetherBox.Config.Debugging.SavedValues.Remove(key);
            AetherBox.Config.Debugging.SavedValues.Add(key, (object)value);
            AetherBox.Config.Save();
        }

        public static T GetSavedValue<T>(string key, T defaultValue)
        {
            return !AetherBox.Config.Debugging.SavedValues.ContainsKey(key) ? defaultValue : (T)AetherBox.Config.Debugging.SavedValues[key];
        }

        private static string ParseTypeName(Type type, List<Type> loopSafety = null)
        {
            if (!type.IsGenericType)
                return type.Name;
            if (loopSafety == null)
                loopSafety = new List<Type>();
            if (loopSafety.Contains(type))
                return "..." + type.Name;
            loopSafety.Add(type);
            return type.Name.Split('`')[0] + "<" + string.Join<string>(',', ((IEnumerable<Type>)type.GetGenericArguments()).Select<Type, string>((Func<Type, string>)(t => DebugManager.ParseTypeName(t, loopSafety) ?? ""))) + ">";
        }

        public static unsafe void PrintOutObject(
          object obj,
          ulong addr,
          List<string> path,
          bool autoExpand = false,
          string headerText = null)
        {
            if (obj is Utf8String utf8String)
            {
                string empty = string.Empty;
                Exception exception = (Exception) null;
                try
                {
                    int num = utf8String.BufUsed > (long) int.MaxValue ? int.MaxValue : (int) utf8String.BufUsed;
                    if (num > 1)
                        empty = Encoding.UTF8.GetString(utf8String.StringPtr, num - 1);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                if (exception != null)
                {
                    ImGui.TextDisabled(exception.Message);
                    ImGui.SameLine();
                }
                else
                {
                    ImGui.Text("\"" + empty + "\"");
                    ImGui.SameLine();
                }
            }
            int count = 0;
            bool flag = false;
            DefaultInterpolatedStringHandler interpolatedStringHandler;
            try
            {
                if (DebugManager.EndModule == 0UL)
                {
                    if (DebugManager.BeginModule == 0UL)
                    {
                        try
                        {
                            DebugManager.BeginModule = (ulong)Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64();
                            DebugManager.EndModule = DebugManager.BeginModule + (ulong)Process.GetCurrentProcess().MainModule.ModuleMemorySize;
                        }
                        catch
                        {
                            DebugManager.EndModule = 1UL;
                        }
                    }
                }
                ImGui.PushStyleColor(ImGuiCol.Text, 4278255615U);
                ++count;
                if (autoExpand)
                    ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                if (headerText == null)
                {
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                    interpolatedStringHandler.AppendFormatted<object>(obj);
                    headerText = interpolatedStringHandler.ToStringAndClear();
                }
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 3);
                interpolatedStringHandler.AppendFormatted(headerText);
                interpolatedStringHandler.AppendLiteral("##print-obj-");
                interpolatedStringHandler.AppendFormatted<ulong>(addr, "X");
                interpolatedStringHandler.AppendLiteral("-");
                interpolatedStringHandler.AppendFormatted(string.Join("-", (IEnumerable<string>)path));
                if (ImGui.TreeNode(interpolatedStringHandler.ToStringAndClear()))
                {
                    StructLayoutAttribute structLayoutAttribute = obj.GetType().StructLayoutAttribute;
                    LayoutKind layoutKind = structLayoutAttribute != null ? structLayoutAttribute.Value : LayoutKind.Sequential;
                    ulong num1 = 0;
                    flag = true;
                    ImGui.PopStyleColor();
                    --count;
                    foreach (FieldInfo field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
                    {
                        ImGuiIOPtr io;
                        if (field.IsStatic)
                        {
                            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.75f, 1f), "static");
                            ImGui.SameLine();
                        }
                        else
                        {
                            if (layoutKind == LayoutKind.Explicit && field.GetCustomAttribute(typeof(FieldOffsetAttribute)) is FieldOffsetAttribute customAttribute)
                                num1 = (ulong)customAttribute.Value;
                            ImGui.PushStyleColor(ImGuiCol.Text, 4287137928U);
                            ulong address = addr + num1;
                            io = ImGui.GetIO();
                            int num2 = io.KeyShift ? 1 : 0;
                            string addressString = DebugManager.GetAddressString((void*) address, num2 != 0);
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 1);
                            interpolatedStringHandler.AppendLiteral("[0x");
                            interpolatedStringHandler.AppendFormatted<ulong>(num1, "X");
                            interpolatedStringHandler.AppendLiteral("]");
                            DebugManager.ClickToCopyText(interpolatedStringHandler.ToStringAndClear(), addressString);
                            ImGui.PopStyleColor();
                            ImGui.SameLine();
                        }
                        FixedBufferAttribute customAttribute1 = (FixedBufferAttribute) field.GetCustomAttribute(typeof (FixedBufferAttribute));
                        if (customAttribute1 != null)
                        {
                            FixedArrayAttribute customAttribute2 = (FixedArrayAttribute) field.GetCustomAttribute(typeof (FixedArrayAttribute));
                            Attribute customAttribute3 = field.GetCustomAttribute(typeof (FixedSizeArrayAttribute<>));
                            ImGui.Text("fixed");
                            ImGui.SameLine();
                            if (customAttribute3 != null)
                            {
                                Type genericArgument = customAttribute3.GetType().GetGenericArguments()[0];
                                int num3 = (int) customAttribute3.GetType().GetProperty("Count").GetValue((object) customAttribute3);
                                Vector4 col = new Vector4(0.2f, 0.9f, 0.9f, 1f);
                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
                                interpolatedStringHandler.AppendFormatted(DebugManager.ParseTypeName(genericArgument));
                                interpolatedStringHandler.AppendLiteral("[");
                                interpolatedStringHandler.AppendFormatted<int>(num3);
                                interpolatedStringHandler.AppendLiteral("]");
                                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                ImGui.TextColored(col, stringAndClear);
                            }
                            else if (customAttribute2 != null)
                            {
                                if (customAttribute2.Type == typeof(string) && customAttribute2.Count == 1)
                                {
                                    ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), customAttribute2.Type.Name ?? "");
                                }
                                else
                                {
                                    Vector4 col = new Vector4(0.2f, 0.9f, 0.9f, 1f);
                                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
                                    interpolatedStringHandler.AppendFormatted(customAttribute2.Type.Name);
                                    interpolatedStringHandler.AppendLiteral("[");
                                    interpolatedStringHandler.AppendFormatted<int>(customAttribute2.Count, "X");
                                    interpolatedStringHandler.AppendLiteral("]");
                                    string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                    ImGui.TextColored(col, stringAndClear);
                                }
                            }
                            else
                            {
                                Vector4 col = new Vector4(0.2f, 0.9f, 0.9f, 1f);
                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 2);
                                interpolatedStringHandler.AppendFormatted(customAttribute1.ElementType.Name);
                                interpolatedStringHandler.AppendLiteral("[0x");
                                interpolatedStringHandler.AppendFormatted<int>(customAttribute1.Length, "X");
                                interpolatedStringHandler.AppendLiteral("]");
                                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                ImGui.TextColored(col, stringAndClear);
                            }
                        }
                        else if (field.FieldType.IsArray)
                        {
                            Array array = (Array) field.GetValue(obj);
                            Vector4 col = new Vector4(0.2f, 0.9f, 0.9f, 1f);
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
                            ref DefaultInterpolatedStringHandler local = ref interpolatedStringHandler;
                            Type type = field.FieldType.GetElementType();
                            if ((object)type == null)
                                type = field.FieldType;
                            string typeName = DebugManager.ParseTypeName(type);
                            local.AppendFormatted(typeName);
                            interpolatedStringHandler.AppendLiteral("[");
                            interpolatedStringHandler.AppendFormatted<int>(array.Length);
                            interpolatedStringHandler.AppendLiteral("]");
                            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                            ImGui.TextColored(col, stringAndClear);
                        }
                        else
                            ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), DebugManager.ParseTypeName(field.FieldType) ?? "");
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.4f, 1f), field.Name + ": ");
                        string str = (obj.GetType().FullName ?? "UnknownType") + "." + field.Name;
                        io = ImGui.GetIO();
                        if (io.KeyShift && ImGui.IsItemHovered())
                            ImGui.SetTooltip(str);
                        io = ImGui.GetIO();
                        if (io.KeyShift && ImGui.IsItemClicked())
                            ImGui.SetClipboardText(str);
                        ImGui.SameLine();
                        if (str == "FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject.Name" && customAttribute1 != null)
                            DebugManager.PrintOutObject((object)MemoryHelper.ReadSeString((IntPtr)((long)addr + (long)num1), customAttribute1.Length), addr + num1);
                        else if (customAttribute1 != null)
                        {
                            long addr1 = (long) addr + (long) num1;
                            List<string> path1 = new List<string>((IEnumerable<string>) path);
                            path1.Add(field.Name);
                            Type fieldType = field.FieldType;
                            object obj1 = field.GetValue(obj);
                            FieldInfo member = field;
                            DebugManager.PrintOutValue((ulong)addr1, path1, fieldType, obj1, (MemberInfo)member);
                        }
                        else if (field.FieldType == typeof(bool) && str.StartsWith("FFXIVClientStructs.FFXIV"))
                        {
                            byte num4 = *(byte*) (addr + num1);
                            long addr2 = (long) addr + (long) num1;
                            List<string> path2 = new List<string>((IEnumerable<string>) path);
                            path2.Add(field.Name);
                            Type fieldType = field.FieldType;
                            // ISSUE: variable of a boxed type
                            __Boxed<bool> local = (System.ValueType) (num4 > (byte) 0);
                            FieldInfo member = field;
                            DebugManager.PrintOutValue((ulong)addr2, path2, fieldType, (object)local, (MemberInfo)member);
                        }
                        else
                        {
                            long addr3 = (long) addr + (long) num1;
                            List<string> path3 = new List<string>((IEnumerable<string>) path);
                            path3.Add(field.Name);
                            Type fieldType = field.FieldType;
                            object obj2 = field.GetValue(obj);
                            FieldInfo member = field;
                            DebugManager.PrintOutValue((ulong)addr3, path3, fieldType, obj2, (MemberInfo)member);
                        }
                        if (layoutKind == LayoutKind.Sequential && !field.IsStatic)
                            num1 += (ulong)Marshal.SizeOf(field.FieldType);
                    }
                    foreach (PropertyInfo property in obj.GetType().GetProperties())
                    {
                        ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1f), DebugManager.ParseTypeName(property.PropertyType) ?? "");
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.2f, 0.6f, 0.4f, 1f), property.Name + ": ");
                        ImGui.SameLine();
                        if (property.PropertyType.IsByRefLike || property.GetMethod.GetParameters().Length != 0)
                        {
                            ImGui.TextDisabled("Unable to display");
                        }
                        else
                        {
                            long addr4 = (long) addr;
                            List<string> path4 = new List<string>((IEnumerable<string>) path);
                            path4.Add(property.Name);
                            Type propertyType = property.PropertyType;
                            object obj3 = property.GetValue(obj);
                            PropertyInfo member = property;
                            DebugManager.PrintOutValue((ulong)addr4, path4, propertyType, obj3, (MemberInfo)member);
                        }
                    }
                    flag = false;
                    ImGui.TreePop();
                }
                else
                {
                    ImGui.PopStyleColor();
                    --count;
                }
            }
            catch (Exception ex)
            {
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
                interpolatedStringHandler.AppendLiteral("{");
                interpolatedStringHandler.AppendFormatted<Exception>(ex);
                interpolatedStringHandler.AppendLiteral("}");
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            }
            if (flag)
                ImGui.TreePop();
            if (count <= 0)
                return;
            ImGui.PopStyleColor(count);
        }

        public static unsafe string GetAddressString(
          void* address,
          out bool isRelative,
          bool absoluteOnly = false)
        {
            ulong num = (ulong) address;
            isRelative = false;
            if (absoluteOnly)
            {
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                interpolatedStringHandler.AppendFormatted<ulong>(num, "X");
                return interpolatedStringHandler.ToStringAndClear();
            }
            try
            {
                if (DebugManager.EndModule == 0UL)
                {
                    if (DebugManager.BeginModule == 0UL)
                    {
                        try
                        {
                            DebugManager.BeginModule = (ulong)Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64();
                            DebugManager.EndModule = DebugManager.BeginModule + (ulong)Process.GetCurrentProcess().MainModule.ModuleMemorySize;
                        }
                        catch
                        {
                            DebugManager.EndModule = 1UL;
                        }
                    }
                }
            }
            catch
            {
            }
            if (DebugManager.BeginModule > 0UL && num >= DebugManager.BeginModule && num <= DebugManager.EndModule)
            {
                isRelative = true;
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 1);
                interpolatedStringHandler.AppendLiteral("ffxiv_dx11.exe+");
                interpolatedStringHandler.AppendFormatted<ulong>(num - DebugManager.BeginModule, "X");
                return interpolatedStringHandler.ToStringAndClear();
            }
            DefaultInterpolatedStringHandler interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(0, 1);
            interpolatedStringHandler1.AppendFormatted<ulong>(num, "X");
            return interpolatedStringHandler1.ToStringAndClear();
        }

        public static unsafe string GetAddressString(void* address, bool absoluteOnly = false)
        {
            return DebugManager.GetAddressString(address, out bool _, absoluteOnly);
        }

        public static unsafe void PrintAddress(void* address)
        {
            bool isRelative;
            string addressString = DebugManager.GetAddressString(address, out isRelative);
            if (isRelative)
            {
                DebugManager.ClickToCopyText(DebugManager.GetAddressString(address, true));
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, 4291543295U);
                DebugManager.ClickToCopyText(addressString);
                ImGui.PopStyleColor();
            }
            else
                DebugManager.ClickToCopyText(addressString);
        }
    }
}
