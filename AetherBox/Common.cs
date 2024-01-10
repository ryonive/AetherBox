using AetherBox.Helpers;
using Dalamud.Hooking;
using Dalamud.Logging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AetherBox;

public static class Common
{
    public unsafe delegate void* AddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** nums, StringArrayData** strings);

    public unsafe delegate void NoReturnAddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    private unsafe delegate void* AddonSetupDelegate(AtkUnitBase* addon);

    private unsafe delegate void FinalizeAddonDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);

    private static Hook<AddonSetupDelegate> ? AddonSetupHook;

    private static Hook<FinalizeAddonDelegate> ? FinalizeAddonHook;

    //private static nint LastCommandAddress;

    public const int UnitListCount = 18;

    public static List<IHookWrapper> HookList = new List<IHookWrapper>();

    public static unsafe Utf8String* LastCommand { get; private set; }

    public static unsafe void* ThrowawayOut { get; private set; } = (void*)Marshal.AllocHGlobal(1024);


    public static event Action < SetupAddonArgs > ? OnAddonSetup;

    public static event Action < SetupAddonArgs > ? OnAddonPreSetup;

    public static event Action < SetupAddonArgs > ? OnAddonFinalize;

    public static unsafe void Setup()
    {
        var  LastCommandAddress = Svc.SigScanner.GetStaticAddressFromSig("4C 8D 05 ?? ?? ?? ?? 41 B1 01 49 8B D4 E8 ?? ?? ?? ?? 83 EB 06");
        LastCommand = (Utf8String*)LastCommandAddress;
        AddonSetupHook = Svc.Hook.HookFromSignature<AddonSetupDelegate>("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? C1 E8 14", AddonSetupDetour);
        AddonSetupHook?.Enable();
        FinalizeAddonHook = Svc.Hook.HookFromSignature<FinalizeAddonDelegate>("E8 ?? ?? ?? ?? 48 8B 7C 24 ?? 41 8B C6", FinalizeAddonDetour);
        FinalizeAddonHook?.Enable();
    }

    private static unsafe void* AddonSetupDetour(AtkUnitBase* addon)
    {
        try
        {
            OnAddonPreSetup?.Invoke(new SetupAddonArgs
            {
                Addon = addon
            });
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, "AddonSetupError");
        }
        var retVal = AddonSetupHook.Original(addon);
        try
        {
            OnAddonSetup?.Invoke(new SetupAddonArgs
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

    private static unsafe void FinalizeAddonDetour(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        try
        {
            OnAddonFinalize?.Invoke(new SetupAddonArgs
            {
                Addon = *atkUnitBase
            });
        }
        catch (Exception exception)
        {
            Svc.Log.Error(exception, "FinalizeAddonError");
        }
        FinalizeAddonHook?.Original(unitManager, atkUnitBase);
    }

    public static unsafe AtkUnitBase* GetUnitBase(string name, int index = 1)
    {
        return (AtkUnitBase*)Svc.GameGui.GetAddonByName(name, index);
    }

    public static unsafe AtkValue* CreateAtkValueArray(params object[] values)
    {
        var atkValues = (AtkValue*)Marshal.AllocHGlobal(values.Length * sizeof(AtkValue));
        if (atkValues == null)
        {
            return null;
        }
        try
        {
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                if (v is not uint uintValue)
                {
                    if (v is not int intValue)
                    {
                        if (v is not float floatValue)
                        {
                            if (v is not bool boolValue)
                            {
                                if (v is not string stringValue)
                                {
                                    throw new ArgumentException($"Unable to convert type {v.GetType()} to AtkValue");
                                }
                                atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String;
                                var stringBytes = Encoding.UTF8.GetBytes(stringValue);
                                var stringAlloc = Marshal.AllocHGlobal(stringBytes.Length + 1);
                                Marshal.Copy(stringBytes, 0, stringAlloc, stringBytes.Length);
                                Marshal.WriteByte(stringAlloc, stringBytes.Length, 0);
                                atkValues[i].String = (byte*)stringAlloc;
                            }
                            else
                            {
                                atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool;
                                atkValues[i].Byte = boolValue ? (byte)1 : (byte)0;
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

    public static unsafe void Shutdown()
    {
        if (ThrowawayOut != null)
        {
            Marshal.FreeHGlobal(new nint(ThrowawayOut));
            ThrowawayOut = null;
        }
        AddonSetupHook?.Disable();
        AddonSetupHook?.Dispose();
        FinalizeAddonHook?.Disable();
        FinalizeAddonHook?.Dispose();
    }

    public static unsafe AtkUnitBase* GetAddonByID(uint id)
    {
        var unitManagers = &AtkStage.GetSingleton()->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;
        for (var i = 0; i < 18; i++)
        {
            var unitManager = unitManagers + i;
            foreach (var j in Enumerable.Range(0, Math.Min(unitManager->Count, unitManager->EntriesSpan.Length)))
            {
                var unitBase = unitManager->EntriesSpan[j].Value;
                if (unitBase != null && unitBase->ID == id)
                {
                    return unitBase;
                }
            }
        }
        return null;
    }

    public static unsafe AtkResNode* GetNodeByID(AtkUldManager* uldManager, uint nodeId, NodeType? type = null)
    {
        return GetNodeByID<AtkResNode>(uldManager, nodeId, type);
    }

    public static unsafe T* GetNodeByID<T>(AtkUldManager* uldManager, uint nodeId, NodeType? type = null) where T : unmanaged
    {
        for (var i = 0; i < uldManager->NodeListCount; i++)
        {
            var j = uldManager->NodeList[i];
            if (j->NodeID == nodeId && (!type.HasValue || j->Type == type.Value))
            {
                return (T*)j;
            }
        }
        return null;
    }

    public static HookWrapper<T> Hook<T>(string signature, T detour, int addressOffset = 0) where T : Delegate
    {
        var addr = Svc.SigScanner.ScanText(signature);
        var wh = new HookWrapper<T>(Svc.Hook.HookFromAddress(addr + addressOffset, detour));
        HookList.Add(wh);
        return wh;
    }

    public static unsafe HookWrapper<T> Hook<T>(void* address, T detour) where T : Delegate
    {
        var wh = new HookWrapper<T>(Svc.Hook.HookFromAddress(new nint(address), detour));
        HookList.Add(wh);
        return wh;
    }

    public static HookWrapper<T> Hook<T>(nuint address, T detour) where T : Delegate
    {
        var wh = new HookWrapper<T>(Svc.Hook.HookFromAddress((nint)address, detour));
        HookList.Add(wh);
        return wh;
    }

    public static HookWrapper<T> Hook<T>(nint address, T detour) where T : Delegate
    {
        var wh = new HookWrapper<T>(Svc.Hook.HookFromAddress(address, detour));
        HookList.Add(wh);
        return wh;
    }
}
