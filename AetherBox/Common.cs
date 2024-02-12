using System.Runtime.InteropServices;
using System.Text;
using AetherBox.Helpers;
using Dalamud.Hooking;
using Dalamud.Logging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;

namespace AetherBox;

public static class Common
{
    public unsafe delegate void* AddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** nums, StringArrayData** strings);

    public unsafe delegate void NoReturnAddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    private unsafe delegate void* AddonSetupDelegate(AtkUnitBase* addon);

    private unsafe delegate void FinalizeAddonDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);

    private static Hook<AddonSetupDelegate> AddonSetupHook;

    private static Hook<FinalizeAddonDelegate> FinalizeAddonHook;

    private static nint LastCommandAddress;

    public const int UnitListCount = 18;

    public static List<IHookWrapper> HookList = new List<IHookWrapper>();

    public unsafe static Utf8String* LastCommand { get; private set; }

    public unsafe static void* ThrowawayOut { get; private set; } = (void*)Marshal.AllocHGlobal(1024);


    public static event Action<SetupAddonArgs> OnAddonSetup;

    public static event Action<SetupAddonArgs> OnAddonPreSetup;

    public static event Action<SetupAddonArgs> OnAddonFinalize;

    public unsafe static void Setup()
    {
        LastCommandAddress = Svc.SigScanner.GetStaticAddressFromSig("4C 8D 05 ?? ?? ?? ?? 41 B1 01 49 8B D4 E8 ?? ?? ?? ?? 83 EB 06");
        LastCommand = (Utf8String*)LastCommandAddress;
        AddonSetupHook = Svc.Hook.HookFromSignature<AddonSetupDelegate>("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? C1 E8 14", AddonSetupDetour);
        AddonSetupHook?.Enable();
        FinalizeAddonHook = Svc.Hook.HookFromSignature<FinalizeAddonDelegate>("E8 ?? ?? ?? ?? 48 8B 7C 24 ?? 41 8B C6", FinalizeAddonDetour);
        FinalizeAddonHook?.Enable();
    }

    private unsafe static void* AddonSetupDetour(AtkUnitBase* addon)
    {
        try
        {
            Common.OnAddonPreSetup?.Invoke(new SetupAddonArgs
            {
                Addon = addon
            });
        }
        catch (Exception exception)
        {
            PluginLog.Error(exception, "AddonSetupError");
        }
        void* retVal;
        retVal = AddonSetupHook.Original(addon);
        try
        {
            Common.OnAddonSetup?.Invoke(new SetupAddonArgs
            {
                Addon = addon
            });
        }
        catch (Exception exception2)
        {
            PluginLog.Error(exception2, "AddonSetupError2");
        }
        return retVal;
    }

    private unsafe static void FinalizeAddonDetour(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        try
        {
            Common.OnAddonFinalize?.Invoke(new SetupAddonArgs
            {
                Addon = *atkUnitBase
            });
        }
        catch (Exception exception)
        {
            PluginLog.Error(exception, "FinalizeAddonError");
        }
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

    public unsafe static void Shutdown()
    {
        if (ThrowawayOut != null)
        {
            Marshal.FreeHGlobal(new IntPtr(ThrowawayOut));
            ThrowawayOut = null;
        }
        AddonSetupHook?.Disable();
        AddonSetupHook?.Dispose();
        FinalizeAddonHook?.Disable();
        FinalizeAddonHook?.Dispose();
    }

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


    // List that should hold AnimationLocktimes for actions
    public static SortedList<uint, float> AnimationLockTime = new();

    // to get Excel lists
    public static Lumina.GameData? LuminaGameData = null;
    public static T? LuminaRow<T>(uint row) where T : Lumina.Excel.ExcelRow => LuminaGameData?.GetExcelSheet<T>(Lumina.Data.Language.English)?.GetRow(row);
    public static ExcelSheet<T> GetSheet<T>() where T : ExcelRow => Svc.Data.GetExcelSheet<T>();
}
