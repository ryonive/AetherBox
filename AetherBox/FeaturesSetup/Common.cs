using AetherBox.Helpers;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#nullable disable
namespace AetherBox
{
    public static class Common
    {
        private static Dalamud.Hooking.Hook<AddonSetupDelegate> AddonSetupHook;
        private static Dalamud.Hooking.Hook<Common.FinalizeAddonDelegate> FinalizeAddonHook;
        private static IntPtr LastCommandAddress;
        public const int UnitListCount = 18;
        public static List<IHookWrapper> HookList = new List<IHookWrapper>();
        private static object[] flag;

        public static unsafe Utf8String* LastCommand { get; private set; }

        public static unsafe void* ThrowawayOut { get; private set; } = (void*)Marshal.AllocHGlobal(1024);

        public static event Action<SetupAddonArgs> OnAddonSetup;

        public static event Action<SetupAddonArgs> OnAddonPreSetup;

        public static event Action<SetupAddonArgs> OnAddonFinalize;

        public static unsafe void Setup()
        {
            try
            {
                // Attempt to get the LastCommandAddress
                LastCommandAddress = Svc.SigScanner.GetStaticAddressFromSig("4C 8D 05 ?? ?? ?? ?? 41 B1 01 49 8B D4 E8 ?? ?? ?? ?? 83 EB 06");
                LastCommand = (Utf8String*)LastCommandAddress;

                // Attempt to hook AddonSetup
                AddonSetupHook = Svc.Hook.HookFromSignature<Common.AddonSetupDelegate>("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? C1 E8 14", AddonSetupDetour);
                AddonSetupHook?.Enable();

                // Attempt to hook FinalizeAddon
                FinalizeAddonHook = Svc.Hook.HookFromSignature<Common.FinalizeAddonDelegate>("E8 ?? ?? ?? ?? 48 8B 7C 24 ?? 41 8B C6", FinalizeAddonDetour);
                FinalizeAddonHook?.Enable();
            }
            catch (Exception ex)
            {
                var errorMessage = "Error during setup: " + ex.Message;
                Svc.Log.Error(ex, errorMessage);
                // Handle the error as needed, e.g., throw an exception or return.
            }
        }


        private static unsafe void* AddonSetupDetour(AtkUnitBase* addon)
        {
            try
            {
                OnAddonPreSetup?.Invoke(new SetupAddonArgs()
                {
                    Addon = addon
                });
            }
            catch (Exception ex)
            {
                var objArray = Array.Empty<object>();
                Svc.Log.Error(ex, "AddonSetupError", objArray);
            }
            var voidPtr = AddonSetupHook.Original(addon);
            try
            {
                OnAddonSetup?.Invoke(new SetupAddonArgs()
                {
                    Addon = addon
                });
            }
            catch (Exception ex)
            {
                var objArray = Array.Empty<object>();
                Svc.Log.Error(ex, "AddonSetupError2", objArray);
            }
            return voidPtr;
        }

        private static unsafe void FinalizeAddonDetour(
          AtkUnitManager* unitManager,
          AtkUnitBase** atkUnitBase)
        {
            try
            {
                OnAddonFinalize?.Invoke(new SetupAddonArgs()
                {
                    Addon = *atkUnitBase
                });
            }
            catch (Exception ex)
            {
                var objArray = Array.Empty<object>();
                Svc.Log.Error(ex, "FinalizeAddonError", objArray);
            }
            var finalizeAddonHook = FinalizeAddonHook;
            if (finalizeAddonHook == null)
                return;
            finalizeAddonHook.Original(unitManager, atkUnitBase);
        }

        public static unsafe AtkUnitBase* GetUnitBase(string name, int index = 1)
        {
            return (AtkUnitBase*)Svc.GameGui.GetAddonByName(name, index);
        }

        public static unsafe AtkValue* CreateAtkValueArray(params object[] values)
        {
            return CreateAtkValueArray(flag, values);
        }

        public static unsafe AtkValue* CreateAtkValueArray(byte flag, params object[] values)
        {
            var atkValueArray = (AtkValue*) Marshal.AllocHGlobal(values.Length * sizeof (AtkValue));
            if ((IntPtr)atkValueArray == IntPtr.Zero)
                return (AtkValue*)null;
            try
            {
                for (var index = 0; index < values.Length; ++index)
                {
                    var obj = values[index];
                    switch (obj)
                    {
                        case uint num2:
                            atkValueArray[index].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt;
                            atkValueArray[index].UInt = num2;
                            break;
                        case int num3:
                            atkValueArray[index].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
                            atkValueArray[index].Int = num3;
                            break;
                        case float num4:
                            atkValueArray[index].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Float;
                            atkValueArray[index].Float = num4;
                            break;
                        case bool flag1:
                            atkValueArray[index].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool;
                            atkValueArray[index].Byte = (byte)(flag1 ? 1 : 0);
                            break;
                        case string s:
                            atkValueArray[index].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String;
                            var bytes = Encoding.UTF8.GetBytes(s);
                            var num1 = Marshal.AllocHGlobal(bytes.Length + 1);
                            Marshal.Copy(bytes, 0, num1, bytes.Length);
                            Marshal.WriteByte(num1, bytes.Length, 0);
                            atkValueArray[index].String = (byte*)num1;
                            break;
                        default:
                            var interpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 1);
                            interpolatedStringHandler.AppendLiteral("Unable to convert type ");
                            interpolatedStringHandler.AppendFormatted<Type>(obj.GetType());
                            interpolatedStringHandler.AppendLiteral(" to AtkValue");
                            throw new ArgumentException(interpolatedStringHandler.ToStringAndClear());
                    }
                }
            }
            catch
            {
                return (AtkValue*)null;
            }
            return atkValueArray;
        }

        public static unsafe void Shutdown()
        {
            if ((IntPtr)ThrowawayOut != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(new IntPtr(ThrowawayOut));
                ThrowawayOut = (void*)null;
            }
            AddonSetupHook?.Disable();
            AddonSetupHook?.Dispose();
            FinalizeAddonHook?.Disable();
            FinalizeAddonHook?.Dispose();
        }

        public static unsafe AtkUnitBase* GetAddonByID(uint id)
        {
            var atkUnitListPtr1 = &AtkStage.GetSingleton()->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;
            for (var index1 = 0; index1 < 18; ++index1)
            {
                var atkUnitListPtr2 = atkUnitListPtr1 + index1;
                foreach (var index2 in Enumerable.Range(0, Math.Min(atkUnitListPtr2->Count, atkUnitListPtr2->EntriesSpan.Length)))
                {
                    var addonById = atkUnitListPtr2->EntriesSpan[index2].Value;
                    if ((IntPtr)addonById != IntPtr.Zero && addonById->ID == (int)id)
                        return addonById;
                }
            }
            return (AtkUnitBase*)null;
        }

        public static unsafe AtkResNode* GetNodeByID(
          AtkUldManager* uldManager,
          uint nodeId,
          NodeType? type = null)
        {
            return GetNodeByID<AtkResNode>(uldManager, nodeId, type);
        }

        public static unsafe T* GetNodeByID<T>(AtkUldManager* uldManager, uint nodeId, NodeType? type = null) where T : unmanaged
        {
            for (var index = 0; index < uldManager->NodeListCount; ++index)
            {
                var nodeById = uldManager->NodeList[index];
                if ((int)nodeById->NodeID == (int)nodeId && (!type.HasValue || nodeById->Type == type.Value))
                    return (T*)nodeById;
            }
            return (T*)null;
        }

        public static HookWrapper<T> Hook<T>(string signature, T detour, int addressOffset = 0) where T : Delegate
        {
            var hookWrapper = new HookWrapper<T>(Svc.Hook.HookFromAddress<T>(Svc.SigScanner.ScanText(signature) +  addressOffset, detour));
            HookList.Add(hookWrapper);
            return hookWrapper;
        }

        public static unsafe HookWrapper<T> Hook<T>(void* address, T detour) where T : Delegate
        {
            var hookWrapper = new HookWrapper<T>(Svc.Hook.HookFromAddress<T>(new IntPtr(address), detour));
            HookList.Add(hookWrapper);
            return hookWrapper;
        }

        public static HookWrapper<T> Hook<T>(UIntPtr address, T detour) where T : Delegate
        {
            var hookWrapper = new HookWrapper<T>(Svc.Hook.HookFromAddress<T>((IntPtr) address, detour));
            HookList.Add(hookWrapper);
            return hookWrapper;
        }

        public static HookWrapper<T> Hook<T>(IntPtr address, T detour) where T : Delegate
        {
            var hookWrapper = new HookWrapper<T>(Svc.Hook.HookFromAddress<T>(address, detour));
            HookList.Add(hookWrapper);
            return hookWrapper;
        }

        public unsafe delegate void* AddonOnUpdate(
          AtkUnitBase* atkUnitBase,
          NumberArrayData** nums,
          StringArrayData** strings);

        public unsafe delegate void NoReturnAddonOnUpdate(
          AtkUnitBase* atkUnitBase,
          NumberArrayData** numberArrayData,
          StringArrayData** stringArrayData);

        private unsafe delegate void* AddonSetupDelegate(AtkUnitBase* addon);

        private unsafe delegate void FinalizeAddonDelegate(
          AtkUnitManager* unitManager,
          AtkUnitBase** atkUnitBase);
    }
}
