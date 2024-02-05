using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AetherBox.Debugging;
using AetherBox.Features.Debugging;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;
using ImGuiNET;

namespace AetherBox.Features.Debugging;

public class IslandDebug : DebugHelper
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct AgentMJICraftSchedule
    {
        [StructLayout(LayoutKind.Explicit, Size = 152)]
        public struct ItemData
        {
            [FieldOffset(16)]
            public unsafe fixed ushort Materials[4];

            [FieldOffset(32)]
            public ushort ObjectId;
        }

        [StructLayout(LayoutKind.Explicit, Size = 12)]
        public struct EntryData
        {
            [FieldOffset(0)]
            public ushort CraftObjectId;

            [FieldOffset(2)]
            public ushort u2;

            [FieldOffset(4)]
            public uint u4;

            [FieldOffset(8)]
            public byte StartingSlot;

            [FieldOffset(9)]
            public byte Duration;

            [FieldOffset(10)]
            public byte Started;

            [FieldOffset(11)]
            public byte Efficient;
        }

        [StructLayout(LayoutKind.Explicit, Size = 84)]
        public struct WorkshopData
        {
            [FieldOffset(0)]
            public byte NumScheduleEntries;

            [FieldOffset(8)]
            public unsafe fixed byte EntryData[72];

            [FieldOffset(80)]
            public uint UsedTimeSlots;

            public unsafe Span<EntryData> Entries => new Span<EntryData>(Unsafe.AsPointer(ref EntryData[0]), 6);
        }

        [StructLayout(LayoutKind.Explicit, Size = 2912)]
        public struct AgentData
        {
            [FieldOffset(0)]
            public int InitState;

            [FieldOffset(4)]
            public int SettingAddonId;

            [FieldOffset(208)]
            public StdVector<ItemData> Items;

            [FieldOffset(1024)]
            public unsafe fixed byte WorkshopData[336];

            [FieldOffset(1448)]
            public uint CurScheduleSettingObjectIndex;

            [FieldOffset(1452)]
            public int CurScheduleSettingWorkshop;

            [FieldOffset(1456)]
            public int CurScheduleSettingStartingSlot;

            [FieldOffset(2024)]
            public byte CurScheduleSettingNumMaterials;

            [FieldOffset(2064)]
            public uint RestCycles;

            [FieldOffset(2068)]
            public uint NewRestCycles;

            [FieldOffset(2904)]
            public byte CurrentCycle;

            [FieldOffset(2905)]
            public byte CycleInProgress;

            [FieldOffset(2906)]
            public byte CurrentIslandRank;

            public unsafe Span<WorkshopData> Workshops => new Span<WorkshopData>(Unsafe.AsPointer(ref WorkshopData[0]), 4);
        }

        [FieldOffset(0)]
        public AgentInterface AgentInterface;

        [FieldOffset(40)]
        public unsafe AgentData* Data;
    }

    public unsafe AgentMJICraftSchedule* Agent = (AgentMJICraftSchedule*)AgentModule.Instance()->GetAgentByInternalId(AgentId.MJICraftSchedule);

    private static byte[] R1 = new byte[2];

    private static byte[] R2 = new byte[2];

    private static byte[] R3 = new byte[2];

    private static byte[] R4 = new byte[2];

    private readonly List<byte[]> rests = new List<byte[]> { R1, R2, R3, R4 };

    public override string Name => "IslandDebug".Replace("Debug", "") + " Debugging";

    public unsafe AgentMJICraftSchedule.AgentData* AgentData
    {
        get
        {
            if (Agent == null)
            {
                return null;
            }
            return Agent->Data;
        }
    }

    public unsafe override void Draw()
    {
        ImGui.Text(Name ?? "");
        ImGui.Separator();
        ImGui.Text($"OnIsland State: {MJIManager.Instance()->IsPlayerInSanctuary}");
        ImGui.Text($"Current Rank: {MJIManager.Instance()->IslandState.CurrentRank}");
        ImGui.Text($"Total Farm Slots: {MJIManager.Instance()->GetFarmSlotCount()}");
        ImGui.Text($"Total Pasture Slots: {MJIManager.Instance()->GetPastureSlotCount()}");
        ImGui.Text($"Current Mode: {MJIManager.Instance()->CurrentMode}");
        ImGui.Separator();
        ImGuiEx.TextV("Rest Cycles: " + string.Join(", ", GetCurrentRestDays()));
        ImGui.SameLine();
        if (ImGui.Button("Void Second Rest"))
        {
            SetRestCycles(8321u);
        }
        if (AgentData != null)
        {
            ImGui.Text($"Rest Mask: {AgentData->RestCycles} || {AgentData->RestCycles:X}");
        }
    }

    private unsafe List<int> GetCurrentRestDays()
    {
        byte restDays1;
        restDays1 = *MJIManager.Instance()->CraftworksRestDays;
        byte restDays2;
        restDays2 = MJIManager.Instance()->CraftworksRestDays[1];
        byte restDays3;
        restDays3 = MJIManager.Instance()->CraftworksRestDays[2];
        byte restDays4;
        restDays4 = MJIManager.Instance()->CraftworksRestDays[3];
        return new List<int> { restDays1, restDays2, restDays3, restDays4 };
    }

    public unsafe void SetRestCycles(uint mask)
    {
        Svc.Log.Info($"Setting rest: {mask:X}");
        AgentData->NewRestCycles = mask;
        SynthesizeEvent(5uL, new AtkValue[1]
        {
            new AtkValue
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = 0
            }
        });
    }

    private unsafe void SynthesizeEvent(ulong eventKind, Span<AtkValue> args)
    {
        byte* intPtr = stackalloc byte[12];
        Unsafe.InitBlock(intPtr, 0, 12);
        int* eventData;
        eventData = (int*)intPtr;
        Agent->AgentInterface.ReceiveEvent(eventData, args.GetPointer(0), (uint)args.Length, eventKind);
    }
}