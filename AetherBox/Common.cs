using System.Runtime.InteropServices;
using System.Text;

using AetherBox.Helpers;

using Dalamud.Hooking;
using Dalamud.Memory;

using ECommons.DalamudServices;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace AetherBox;

/// <summary>
/// A static class containing common utility methods.
/// </summary>
public static unsafe class Common
{
    /// <summary>
    /// Delegate for AddonOnUpdate method.
    /// </summary>
    /// <param name="atkUnitBase">The AtkUnitBase pointer.</param>
    /// <param name="nums">Pointer to NumberArrayData.</param>
    /// <param name="strings">Pointer to StringArrayData.</param>
    /// <returns>The result pointer.</returns>
    public delegate void* AddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** nums, StringArrayData** strings);
    //public unsafe delegate void* AddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** nums, StringArrayData** strings);

    public delegate void NoReturnAddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);
    //public unsafe delegate void NoReturnAddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    private delegate void* AddonSetupDelegate(AtkUnitBase* addon);

    private static Hook<AddonSetupDelegate> AddonSetupHook;

    private delegate void FinalizeAddonDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);
    //private unsafe delegate void FinalizeAddonDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);

    private static Hook<FinalizeAddonDelegate> FinalizeAddonHook;

    private static IntPtr LastCommandAddress;
    //private static nint LastCommandAddress;

    //public const int UnitListCount = 18;

    public static List<IHookWrapper> HookList = new List<IHookWrapper>();

    public static Utf8String* LastCommand { get; private set; }
    //public unsafe static Utf8String* LastCommand { get; private set; }

    public static void* ThrowawayOut { get; private set; } = (void*)Marshal.AllocHGlobal(1024);
    //public unsafe static void* ThrowawayOut { get; private set; } = (void*)Marshal.AllocHGlobal(1024);

    /// <summary>
    /// Event triggered when an addon is set up.
    /// </summary>
    public static event Action<SetupAddonArgs> OnAddonSetup;

    /// <summary>
    /// Event triggered before an addon is set up.
    /// </summary>
    public static event Action<SetupAddonArgs> OnAddonPreSetup;

    /// <summary>
    /// Event triggered when an addon is finalized.
    /// </summary>
    public static event Action<SetupAddonArgs> OnAddonFinalize;

    /// <summary>
    /// Initializes the Common class and sets up hooks.
    /// </summary>
    public unsafe static void Setup()
    {
        // Find the memory address for a specific signature.
        LastCommandAddress = Svc.SigScanner.GetStaticAddressFromSig("4C 8D 05 ?? ?? ?? ?? 41 B1 01 49 8B D4 E8 ?? ?? ?? ?? 83 EB 06");

        // Cast the address to a Utf8String pointer.
        LastCommand = (Utf8String*)LastCommandAddress;

        // Set up a hook for the AddonSetupDetour method.
        AddonSetupHook = Svc.Hook.HookFromSignature<AddonSetupDelegate>("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? C1 E8 14", AddonSetupDetour);

        // Enable the AddonSetupHook.
        AddonSetupHook?.Enable();

        // Set up a hook for the FinalizeAddonDetour method.
        FinalizeAddonHook = Svc.Hook.HookFromSignature<FinalizeAddonDelegate>("E8 ?? ?? ?? ?? 48 8B 7C 24 ?? 41 8B C6", FinalizeAddonDetour);

        // Enable the FinalizeAddonHook.
        FinalizeAddonHook?.Enable();
    }

    /// <summary>
    /// Detour method for the AddonSetupDelegate hook.
    /// </summary>
    /// <param name="addon">The AtkUnitBase pointer.</param>
    /// <returns>The result pointer.</returns>
    private unsafe static void* AddonSetupDetour(AtkUnitBase* addon)
    {
        try
        {
            // Invoke the OnAddonPreSetup event before setup.
            Common.OnAddonPreSetup?.Invoke(new SetupAddonArgs
            {
                Addon = addon
            });
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, "AddonSetupError");
        }

        void* retVal;
        // Call the original AddonSetupDelegate method.
        retVal = AddonSetupHook.Original(addon);

        try
        {
            // Invoke the OnAddonSetup event after setup.
            Common.OnAddonSetup?.Invoke(new SetupAddonArgs
            {
                Addon = addon
            });
        }
        catch (Exception exception2)
        {
            Svc.Log.Error(exception2, "AddonSetupError2");
        }
        return retVal;
    }

    /// <summary>
    /// Detour method for the FinalizeAddonDelegate hook.
    /// </summary>
    /// <param name="unitManager">The AtkUnitManager pointer.</param>
    /// <param name="atkUnitBase">The AtkUnitBase pointer.</param>
    private unsafe static void FinalizeAddonDetour(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        try
        {
            // Invoke the OnAddonFinalize event.
            Common.OnAddonFinalize?.Invoke(new SetupAddonArgs
            {
                Addon = *atkUnitBase
            });
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, "FinalizeAddonError");
        }

        // Call the original FinalizeAddonDelegate method.
        FinalizeAddonHook?.Original(unitManager, atkUnitBase);
    }

    public unsafe static AtkUnitBase* GetUnitBase(string name, int index = 1)
    {
        return (AtkUnitBase*)Svc.GameGui.GetAddonByName(name, index);
    }


    public unsafe static AtkValue* CreateAtkValueArray(params object[] values)
    {
        AtkValue* atkValues;
        atkValues = (AtkValue*)Marshal.AllocHGlobal(values.Length * sizeof(AtkValue));
        if (atkValues == null)
        {
            return null;
        }
        try
        {
            for (int i = 0; i < values.Length; i++)
            {
                object v;
                v = values[i];
                if (!(v is uint uintValue))
                {
                    if (!(v is int intValue))
                    {
                        if (!(v is float floatValue))
                        {
                            if (!(v is bool boolValue))
                            {
                                if (!(v is string stringValue))
                                {
                                    throw new ArgumentException($"Unable to convert type {v.GetType()} to AtkValue");
                                }
                                atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String;
                                byte[] stringBytes;
                                stringBytes = Encoding.UTF8.GetBytes(stringValue);
                                nint stringAlloc;
                                stringAlloc = Marshal.AllocHGlobal(stringBytes.Length + 1);
                                Marshal.Copy(stringBytes, 0, stringAlloc, stringBytes.Length);
                                Marshal.WriteByte(stringAlloc, stringBytes.Length, 0);
                                atkValues[i].String = (byte*)stringAlloc;
                            }
                            else
                            {
                                atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool;
                                atkValues[i].Byte = (boolValue ? ((byte)1) : ((byte)0));
                            }
                        }
                        else
                        {
                            atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Float;
                            atkValues[i].Float = floatValue;
                        }
                    }
                    else
                    {
                        atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
                        atkValues[i].Int = intValue;
                    }
                }
                else
                {
                    atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt;
                    atkValues[i].UInt = uintValue;
                }
            }
            return atkValues;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Shuts down the Common class and disables hooks.
    /// </summary>
    public unsafe static void Shutdown()
    {
        // Free allocated memory if it's not null.
        if (ThrowawayOut != null)
        {
            Marshal.FreeHGlobal(new IntPtr(ThrowawayOut));
            ThrowawayOut = null;
        }
        // Disable and dispose the AddonSetupHook.
        AddonSetupHook?.Disable();
        AddonSetupHook?.Dispose();

        // Disable and dispose the FinalizeAddonHook.
        FinalizeAddonHook?.Disable();
        FinalizeAddonHook?.Dispose();
    }

    public const int UnitListCount = 18;
    public unsafe static AtkUnitBase* GetAddonByID(uint id)
    {
        AtkUnitList* unitManagers;
        unitManagers = &AtkStage.GetSingleton()->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;
        for (int i = 0; i < 18; i++)
        {
            AtkUnitList* unitManager;
            unitManager = unitManagers + i;
            foreach (int j in Enumerable.Range(0, Math.Min(unitManager->Count, unitManager->EntriesSpan.Length)))
            {
                AtkUnitBase* unitBase;
                unitBase = unitManager->EntriesSpan[j].Value;
                if (unitBase != null && unitBase->ID == id)
                {
                    return unitBase;
                }
            }
        }
        return null;
    }

    public unsafe static AtkResNode* GetNodeByID(AtkUldManager* uldManager, uint nodeId, NodeType? type = null)
    {
        return GetNodeByID<AtkResNode>(uldManager, nodeId, type);
    }

    public unsafe static T* GetNodeByID<T>(AtkUldManager* uldManager, uint nodeId, NodeType? type = null) where T : unmanaged
    {
        for (int i = 0; i < uldManager->NodeListCount; i++)
        {
            AtkResNode* j;
            j = uldManager->NodeList[i];
            if (j->NodeID == nodeId && (!type.HasValue || j->Type == type.Value))
            {
                return (T*)j;
            }
        }
        return null;
    }

    public static HookWrapper<T> Hook<T>(string signature, T detour, int addressOffset = 0) where T : Delegate
    {
        nint addr;
        addr = Svc.SigScanner.ScanText(signature);
        HookWrapper<T> wh;
        wh = new HookWrapper<T>(Svc.Hook.HookFromAddress(addr + addressOffset, detour));
        HookList.Add(wh);
        return wh;
    }

    public unsafe static HookWrapper<T> Hook<T>(void* address, T detour) where T : Delegate
    {
        HookWrapper<T> wh;
        wh = new HookWrapper<T>(Svc.Hook.HookFromAddress(new IntPtr(address), detour));
        HookList.Add(wh);
        return wh;
    }

    public static HookWrapper<T> Hook<T>(nuint address, T detour) where T : Delegate
    {
        HookWrapper<T> wh;
        wh = new HookWrapper<T>(Svc.Hook.HookFromAddress((nint)address, detour));
        HookList.Add(wh);
        return wh;
    }

    public static HookWrapper<T> Hook<T>(nint address, T detour) where T : Delegate
    {
        HookWrapper<T> wh;
        wh = new HookWrapper<T>(Svc.Hook.HookFromAddress(address, detour));
        HookList.Add(wh);
        return wh;
    }
}

/// <summary>
/// Arguments for addon setup events.
/// </summary>
public unsafe class SetupAddonArgs
{
    /// <summary>
    /// <br>Gets or sets the addon pointer.</br>
    /// <br>Gets or sets the reference to a part of the game.</br>
    /// </summary>
    public AtkUnitBase* Addon { get; init; }
    private string addonName;
    public string AddonName => addonName ??= MemoryHelper.ReadString(new IntPtr(Addon->Name), 0x20).Split('\0')[0];
}