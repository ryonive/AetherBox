using System;
using AetherBox.Debugging;
using AetherBox.Features.Debugging;
using Dalamud.Game;
using ECommons.DalamudServices;
using ImGuiNET;

namespace AetherBox.Features.Debugging;

public class HousingDebug : DebugHelper
{
	public enum HousingZone : byte
	{
		Unknown = 0,
		Mist = 83,
		Goblet = 85,
		LavenderBeds = 84,
		Shirogane = 129,
		Firmament = 211
	}

	public enum Floor : byte
	{
		Unknown = byte.MaxValue,
		Ground = 0,
		First = 1,
		Cellar = 10
	}

	public class SeAddressBase
	{
		public readonly nint Address;

		public SeAddressBase(ISigScanner sigScanner, string signature, int offset = 0)
		{
			Address = sigScanner.GetStaticAddressFromSig(signature);
			if (Address != IntPtr.Zero)
			{
				Address += offset;
			}
			ulong baseOffset;
			baseOffset = (ulong)(((IntPtr)Address).ToInt64() - ((IntPtr)sigScanner.Module.BaseAddress).ToInt64());
			Svc.Log.Debug($"{GetType().Name} address 0x{((IntPtr)Address).ToInt64():X16}, baseOffset 0x{baseOffset:X16}.");
		}
	}

	public sealed class PositionInfoAddress : SeAddressBase
	{
		private readonly struct PositionInfo
		{
			private unsafe readonly byte* address;

			public unsafe ushort House => (ushort)((address != null && InHouse) ? ((uint)(*(ushort*)(address + 38560) + 1)) : 0u);

			public unsafe ushort Ward => (ushort)((address != null) ? ((uint)(*(ushort*)(address + 38562) + 1)) : 0u);

			public unsafe bool Subdivision
			{
				get
				{
					if (address != null)
					{
						return address[38569] == 2;
					}
					return false;
				}
			}

			public unsafe HousingZone Zone
			{
				get
				{
					if (address != null)
					{
						return (HousingZone)address[38564];
					}
					return HousingZone.Unknown;
				}
			}

			public unsafe byte Plot => (byte)((address != null && !InHouse) ? ((uint)(address[38568] + 1)) : 0u);

			public unsafe Floor Floor
			{
				get
				{
					if (address != null)
					{
						return (Floor)address[38660];
					}
					return Floor.Unknown;
				}
			}

			private unsafe bool InHouse => address[38569] == 0;

			private unsafe PositionInfo(byte* address)
			{
				this.address = address;
			}

			public unsafe static implicit operator PositionInfo(nint ptr)
			{
				return new PositionInfo((byte*)ptr);
			}

			public unsafe static implicit operator PositionInfo(byte* ptr)
			{
				return new PositionInfo(ptr);
			}

			public unsafe static implicit operator bool(PositionInfo ptr)
			{
				return ptr.address != null;
			}
		}

		private unsafe PositionInfo Info
		{
			get
			{
				byte** intermediate;
				intermediate = *(byte***)Address;
				return (intermediate == null) ? null : (*intermediate);
			}
		}

		public ushort Ward => Info.Ward;

		public HousingZone Zone => Info.Zone;

		public ushort House => Info.House;

		public bool Subdivision => Info.Subdivision;

		public byte Plot => Info.Plot;

		public Floor Floor => Info.Floor;

		public PositionInfoAddress(ISigScanner sigScanner)
			: base(sigScanner, "40 ?? 48 83 ?? ?? 33 DB 48 39 ?? ?? ?? ?? ?? 75 ?? 45")
		{
		}
	}

	public const string PositionInfo = "40 ?? 48 83 ?? ?? 33 DB 48 39 ?? ?? ?? ?? ?? 75 ?? 45";

	public override string Name => "HousingDebug".Replace("Debug", "") + " Debugging";

	public override void Draw()
	{
		ImGui.Text(Name ?? "");
		ImGui.Separator();
		PositionInfoAddress pia;
		pia = new PositionInfoAddress(Svc.SigScanner);
		ImGui.Text($"District: {pia.Zone}");
		ImGui.Text($"Ward: {pia.Ward}");
		ImGui.Text($"House: {pia.House}");
		ImGui.Text($"Subdivision: {pia.Subdivision}");
		ImGui.Text($"Plot: {pia.Plot}");
		ImGui.Text($"Floor: {pia.Floor}");
	}
}
