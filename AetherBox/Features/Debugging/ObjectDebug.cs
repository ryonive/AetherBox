using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using AetherBox.Debugging;
using AetherBox.Features.Debugging;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Interop.Attributes;
using ImGuiNET;

namespace AetherBox.Features.Debugging;

public class ObjectDebug : DebugHelper
{
	[StructLayout(LayoutKind.Explicit, Size = 416)]
	[VTableAddress("48 8d 05 ?? ?? ?? ?? c7 81 80 00 00 00 00 00 00 00", 3, false)]
	public struct GameObject
	{
		[FieldOffset(16)]
		public Vector3 DefaultPosition;

		[FieldOffset(32)]
		public float DefaultRotation;

		[FieldOffset(48)]
		public unsafe fixed byte Name[64];

		[FieldOffset(116)]
		public uint ObjectID;

		[FieldOffset(120)]
		public uint LayoutID;

		[FieldOffset(128)]
		public uint DataID;

		[FieldOffset(132)]
		public uint OwnerID;

		[FieldOffset(136)]
		public ushort ObjectIndex;

		[FieldOffset(140)]
		public byte ObjectKind;

		[FieldOffset(141)]
		public byte SubKind;

		[FieldOffset(142)]
		public byte Gender;

		[FieldOffset(144)]
		public byte YalmDistanceFromPlayerX;

		[FieldOffset(145)]
		public byte TargetStatus;

		[FieldOffset(146)]
		public byte YalmDistanceFromPlayerZ;

		[FieldOffset(149)]
		public ObjectTargetableFlags TargetableStatus;

		[FieldOffset(176)]
		public Vector3 Position;

		[FieldOffset(192)]
		public float Rotation;

		[FieldOffset(196)]
		public float Scale;

		[FieldOffset(200)]
		public float Height;

		[FieldOffset(204)]
		public float VfxScale;

		[FieldOffset(208)]
		public float HitboxRadius;

		[FieldOffset(224)]
		public Vector3 DrawOffset;

		[FieldOffset(244)]
		public EventId EventId;

		[FieldOffset(248)]
		public uint FateId;

		[FieldOffset(256)]
		public unsafe DrawObject* DrawObject;

		[FieldOffset(272)]
		public uint NamePlateIconId;

		[FieldOffset(276)]
		public int RenderFlags;

		[FieldOffset(344)]
		public unsafe LuaActor* LuaActor;
	}

	private float hbr;

	public override string Name => "ObjectDebug".Replace("Debug", "") + " Debugging";

	public unsafe override void Draw()
	{
		ImGui.Text(Name ?? "");
		ImGui.Separator();
		foreach (Dalamud.Game.ClientState.Objects.Types.GameObject obj in Svc.Objects.Where((Dalamud.Game.ClientState.Objects.Types.GameObject o) => o.IsHostile()))
		{
			ImGui.Text($"{obj.Name} > {Vector3.Distance(Svc.ClientState.LocalPlayer.Position, obj.Position):f1}y");
			ImGui.PushItemWidth(200f);
			ImGui.SliderFloat($"Hitbox Radius###{obj.Name}{obj.ObjectId}", ref ((GameObject*)obj.Address)->HitboxRadius, 0f, 100f);
		}
	}
}
