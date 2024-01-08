using AetherBox.Debugging;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable disable
namespace AetherBox.Features.Debugging
{
    public class IslandDebug : DebugHelper
    {
        public unsafe IslandDebug.AgentMJICraftSchedule* Agent = (IslandDebug.AgentMJICraftSchedule*) AgentModule.Instance()->GetAgentByInternalId(AgentId.MJICraftSchedule);
        private static byte[] R1 = new byte[2];
        private static byte[] R2 = new byte[2];
        private static byte[] R3 = new byte[2];
        private static byte[] R4 = new byte[2];
        private readonly List<byte[]> rests = new List<byte[]>()
    {
      IslandDebug.R1,
      IslandDebug.R2,
      IslandDebug.R3,
      IslandDebug.R4
    };

        public override string Name => nameof(IslandDebug).Replace("Debug", "") + " Debugging";

        public unsafe IslandDebug.AgentMJICraftSchedule.AgentData* AgentData
        {
            get
            {
                return (IntPtr)this.Agent == IntPtr.Zero ? (IslandDebug.AgentMJICraftSchedule.AgentData*)null : this.Agent->Data;
            }
        }

        public override unsafe void Draw()
        {
            ImGui.Text(this.Name ?? "");
            ImGui.Separator();
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
            interpolatedStringHandler.AppendLiteral("Current Rank: ");
            interpolatedStringHandler.AppendFormatted<byte>(MJIManager.Instance()->IslandState.CurrentRank);
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
            interpolatedStringHandler.AppendLiteral("Total Farm Slots: ");
            interpolatedStringHandler.AppendFormatted<byte>(MJIManager.Instance()->GetFarmSlotCount());
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
            interpolatedStringHandler.AppendLiteral("Total Pasture Slots: ");
            interpolatedStringHandler.AppendFormatted<byte>(MJIManager.Instance()->GetPastureSlotCount());
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            ImGui.Separator();
            ImGuiEx.TextV("Rest Cycles: " + string.Join<int>(", ", (IEnumerable<int>)this.GetCurrentRestDays()));
            ImGui.SameLine();
            if (ImGui.Button("Void Second Rest"))
                this.SetRestCycles(8321U);
            if ((IntPtr)this.AgentData == IntPtr.Zero)
                return;
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 2);
            interpolatedStringHandler.AppendLiteral("Rest Mask: ");
            interpolatedStringHandler.AppendFormatted<uint>(this.AgentData->RestCycles);
            interpolatedStringHandler.AppendLiteral(" || ");
            interpolatedStringHandler.AppendFormatted<uint>(this.AgentData->RestCycles, "X");
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
        }

        private unsafe List<int> GetCurrentRestDays()
        {
            // ISSUE: reference to a compiler-generated field
            return new List<int>()
      {
        (int) MJIManager.Instance()->CraftworksRestDays.FixedElementField,
        (int) MJIManager.Instance()->CraftworksRestDays[1],
        (int) MJIManager.Instance()->CraftworksRestDays[2],
        (int) MJIManager.Instance()->CraftworksRestDays[3]
      };
        }

        public unsafe void SetRestCycles(uint mask)
        {
            IPluginLog log = Svc.Log;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
            interpolatedStringHandler.AppendLiteral("Setting rest: ");
            interpolatedStringHandler.AppendFormatted<uint>(mask, "X");
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            object[] objArray = Array.Empty<object>();
            log.Info(stringAndClear, objArray);
            this.AgentData->NewRestCycles = mask;
            this.SynthesizeEvent(5UL, (Span<AtkValue>)new AtkValue[1]
            {
        new AtkValue() { Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int, Int = 0 }
            });
        }

        private unsafe void SynthesizeEvent(ulong eventKind, Span<AtkValue> args)
        {
            int* eventData = stackalloc int[3];
            // ISSUE: initblk instruction
            __memset((IntPtr)eventData, 0, 12);
            this.Agent->AgentInterface.ReceiveEvent((void*)eventData, args.GetPointer<AtkValue>(0), (uint)args.Length, eventKind);
        }

        [StructLayout(LayoutKind.Explicit, Size = 64)]
        public struct AgentMJICraftSchedule
        {
            [FieldOffset(0)]
            public AgentInterface AgentInterface;
            [FieldOffset(40)]
            public unsafe IslandDebug.AgentMJICraftSchedule.AgentData* Data;

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

                public unsafe Span<IslandDebug.AgentMJICraftSchedule.EntryData> Entries
                {
                    get
                    {
                        return new Span<IslandDebug.AgentMJICraftSchedule.EntryData>(Unsafe.AsPointer<byte>(ref this.EntryData.FixedElementField), 6);
                    }
                }
            }

            [StructLayout(LayoutKind.Explicit, Size = 2912)]
            public struct AgentData
            {
                [FieldOffset(0)]
                public int InitState;
                [FieldOffset(4)]
                public int SettingAddonId;
                [FieldOffset(208)]
                public StdVector<IslandDebug.AgentMJICraftSchedule.ItemData> Items;
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

                public unsafe Span<IslandDebug.AgentMJICraftSchedule.WorkshopData> Workshops
                {
                    get
                    {
                        return new Span<IslandDebug.AgentMJICraftSchedule.WorkshopData>(Unsafe.AsPointer<byte>(ref this.WorkshopData.FixedElementField), 4);
                    }
                }
            }
        }
    }
}
