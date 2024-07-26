using System;
using System.Runtime.InteropServices;
using AetherBox.Helpers;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Helpers;

internal static class IslandSanctuaryHelper
{
    private unsafe delegate nint ReceiveEventDelegate(AtkEventListener* eventListener, AtkEventType eventType, uint eventParam, void* eventData, void* inputData);

    public enum ScheduleListEntryType
    {
        NormalEntry = 0,
        LastEntry = 1,
        Category = 2
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct ScheduleListEntry
    {
        [FieldOffset(0)]
        public ScheduleListEntryType Type;

        [FieldOffset(4)]
        public uint Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MJICraftScheduleSettingData
    {
        [FieldOffset(424)]
        public StdVector<Pointer<Pointer<ScheduleListEntry>>> Entries;

        [FieldOffset(484)]
        public int NumEntries;

        public unsafe int FindEntryIndex(uint rowId)
        {
            for (int i = 0; i < NumEntries; i++)
            {
                ScheduleListEntry* p;
                p = Entries.Span[i].Value->Value;
                if (p->Type != ScheduleListEntryType.Category && p->Value == rowId - 1)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AddonMJICraftScheduleSetting
    {
        [FieldOffset(0)]
        public AtkUnitBase AtkUnitBase;

        [FieldOffset(544)]
        public unsafe MJICraftScheduleSettingData* Data;
    }

    public unsafe static bool IsWorkshopUnlocked(int w, out int maxWorkshops)
    {
        maxWorkshops = 0;
        try
        {
            byte currentRank;
            currentRank = MJIManager.Instance()->IslandState.CurrentRank;
            switch (w)
            {
                case 1:
                    if (currentRank < 3)
                    {
                        maxWorkshops = 0;
                        break;
                    }
                    goto default;
                case 2:
                    if (currentRank < 6)
                    {
                        maxWorkshops = 1;
                        break;
                    }
                    goto default;
                case 3:
                    if (currentRank < 8)
                    {
                        maxWorkshops = 2;
                        break;
                    }
                    goto default;
                case 4:
                    if (currentRank < 14)
                    {
                        maxWorkshops = 3;
                        break;
                    }
                    goto default;
                default:
                    return true;
            }
            return false;
        }
        catch (Exception e)
        {
            e.Log();
            return false;
        }
    }

    public unsafe static bool isCraftworkObjectCraftable(MJICraftworksObject item)
    {
        return MJIManager.Instance()->IslandState.CurrentRank >= item.LevelReq;
    }

    public unsafe static bool isWorkshopOpen()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("MJICraftSchedule", out var addon))
        {
            return addon->IsVisible();
        }
        return false;
    }

    public unsafe static bool isCraftSelectOpen()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("MJICraftScheduleSetting", out var addon))
        {
            return addon->IsVisible();
        }
        return false;
    }

    public unsafe static int? GetOpenCycle()
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("MJICraftSchedule", out var addon) || !addon->IsVisible() || addon->AtkValues->Type == (FFXIVClientStructs.FFXIV.Component.GUI.ValueType)0)
        {
            return null;
        }
        return (int)addon->AtkValues->UInt;
    }
}
