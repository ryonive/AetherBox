using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AetherBox.Helpers;
using ImGuiNET;

namespace AetherBox.Helpers;

public abstract class ValueParser : Attribute
{
    public class HexValue : ValueParser
    {
        public override string GetString(Type type, object obj, MemberInfo member, ulong parentAddr)
        {
            return $"{obj:X}";
        }
    }

    public class FixedString : ValueParser
    {
        public unsafe override string GetString(Type type, object obj, MemberInfo member, ulong parentAddr)
        {
            FixedBufferAttribute fixedBuffer;
            fixedBuffer = (FixedBufferAttribute)member.GetCustomAttribute(typeof(FixedBufferAttribute));
            if (fixedBuffer == null || fixedBuffer.ElementType != typeof(byte))
            {
                return $"[Not a fixed byte buffer] {obj}";
            }
            FieldOffsetAttribute fieldOffset;
            fieldOffset = (FieldOffsetAttribute)member.GetCustomAttribute(typeof(FieldOffsetAttribute));
            if (fieldOffset == null)
            {
                return $"[No FieldOffset] {obj}";
            }
            byte* addr;
            addr = (byte*)(parentAddr + (ulong)fieldOffset.Value);
            return Marshal.PtrToStringAnsi(new IntPtr(addr), fixedBuffer.Length) ?? "";
        }
    }

    public abstract string GetString(Type type, object obj, MemberInfo member, ulong parentAddr);

    public virtual void ImGuiPrint(Type type, object value, MemberInfo member, ulong parentAddr)
    {
        ImGui.Text("[" + GetType().Name + "]");
        ImGui.SameLine();
        ImGui.Text(GetString(type, value, member, parentAddr));
    }
}
