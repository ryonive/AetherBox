using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;

namespace AetherBox.Helpers;

public static unsafe partial class GenericHelpers
{

    public static string Read(this Span<byte> bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0)
            {
                fixed (byte* ptr = bytes)
                {
                    return Marshal.PtrToStringUTF8((nint)ptr, i);
                }
            }
        }
        fixed (byte* ptr = bytes)
        {
            return Marshal.PtrToStringUTF8((nint)ptr, bytes.Length);
        }
    }

    public static string ParamsPlaceholderPrefix = "$";
    public static string Params(this string? defaultValue, params object?[] objects)
    {
        defaultValue ??= "";
        var guid = Guid.NewGuid().ToString();
        defaultValue = defaultValue.Replace($"{ParamsPlaceholderPrefix}{ParamsPlaceholderPrefix}", guid);
        for (int i = 0; i < objects.Length; i++)
        {
            var str = objects[i]?.ToString() ?? "";
            defaultValue = defaultValue.Replace($"{ParamsPlaceholderPrefix}{i}", str);
        }
        foreach (var obj in objects)
        {
            defaultValue = defaultValue.ReplaceFirst(ParamsPlaceholderPrefix, obj?.ToString() ?? "");
        }
        return defaultValue.Replace(guid, ParamsPlaceholderPrefix);
    }

    /// <summary>
    /// Attempts to get first instance of addon by name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Addon"></param>
    /// <param name="AddonPtr"></param>
    /// <returns></returns>
    public static bool TryGetAddonByName<T>(string Addon, out T* AddonPtr) where T : unmanaged
    {
        var a = Svc.GameGui.GetAddonByName(Addon, 1);
        if (a == IntPtr.Zero)
        {
            AddonPtr = null;
            return false;
        }
        else
        {
            AddonPtr = (T*)a;
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Safe(System.Action a, bool suppressErrors = false)
    {
        try
        {
            a();
        }
        catch (Exception e)
        {
            if (!suppressErrors) PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
    }
}
