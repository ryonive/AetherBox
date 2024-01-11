using System;
using System.Numerics;
using System.Runtime.InteropServices;
using AetherBox.Debugging;
using AetherBox.Helpers;
using Dalamud;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Features.Debugging;

public class PositionDebug : DebugHelper
{
	private float playerPositionX;

	private float playerPositionY;

	private float playerPositionZ;

	private bool noclip;

	private float displacementFactor = 0.1f;

	private readonly float cameriaH;

	private readonly float cameriaV;

	private Vector3 lastTargetPos;

	private unsafe readonly Structs.PlayerController* playerController = (Structs.PlayerController*)Svc.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 3C 01 75 1E 48 8D 0D");

	private float speedMultiplier = 1f;

	public override string Name => "PositionDebug".Replace("Debug", "") + " Debugging";

	private static nint SetPosFunPtr
	{
		get
		{
			if (!Svc.SigScanner.TryScanText("E8 ?? ?? ?? ?? 83 4B 70 01", out var ptr))
			{
				return IntPtr.Zero;
			}
			return ptr;
		}
	}

	public unsafe override void Draw()
	{
		ImGui.Text(Name ?? "");
		ImGui.Separator();
		if (Svc.ClientState.LocalPlayer != null)
		{
			Vector3 curPos;
			curPos = Svc.ClientState.LocalPlayer.Position;
			playerPositionX = curPos.X;
			playerPositionY = curPos.Y;
			playerPositionZ = curPos.Z;
			ImGui.Text("Your Position:");
			ImGui.PushItemWidth(75f);
			ImGui.InputFloat("X", ref playerPositionX);
			ImGui.SameLine();
			DrawPositionModButtons("x");
			ImGui.PushItemWidth(75f);
			ImGui.InputFloat("Y", ref playerPositionY);
			ImGui.SameLine();
			DrawPositionModButtons("y");
			ImGui.PushItemWidth(75f);
			ImGui.InputFloat("Z", ref playerPositionZ);
			ImGui.SameLine();
			DrawPositionModButtons("z");
			if (ImGui.Checkbox("No Clip Mode", ref noclip))
			{
				if (noclip)
				{
					Svc.Framework.Update += NoClipMode;
				}
				else
				{
					Svc.Framework.Update -= NoClipMode;
				}
			}
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Hold ALT");
			}
			ImGui.SameLine();
			ImGui.InputFloat("Displacement Factor", ref displacementFactor);
			ImGui.Separator();
		}
		Structs.CameraEx* camera;
		camera = (Structs.CameraEx*)CameraManager.Instance()->GetActiveCamera();
		ImGui.Text($"Camera H: {camera->DirH:f3}");
		ImGui.Text($"Camera V: {camera->DirV:f3}");
		ImGui.Separator();
		ImGui.Text($"Movement Speed: {playerController->MoveControllerWalk.BaseMovementSpeed}");
		ImGui.PushItemWidth(150f);
		ImGui.SliderFloat("Speed Multiplier", ref speedMultiplier, 0f, 20f);
		ImGui.SameLine();
		if (ImGui.Button("Set"))
		{
			SetSpeed(speedMultiplier * 6f);
		}
		ImGui.SameLine();
		if (ImGui.Button("Reset"))
		{
			speedMultiplier = 1f;
			SetSpeed(speedMultiplier * 6f);
		}
		ImGui.Text($"IsMoving: {AgentMap.Instance()->IsPlayerMoving == 1}");
		ImGui.Separator();
		if (Svc.Targets.Target != null || Svc.Targets.PreviousTarget != null)
		{
			Vector3 targetPos;
			targetPos = ((Svc.Targets.Target != null) ? Svc.Targets.Target.Position : Svc.Targets.PreviousTarget.Position);
			string str;
			str = ((Svc.Targets.Target != null) ? "Target" : "Last Target");
			ImGui.Text($"{str} Position: {targetPos:f3}");
			if (ImGui.Button("TP to " + str))
			{
				SetPos(targetPos);
			}
			ImGui.Text($"Distance to {str}: {Vector3.Distance(Svc.ClientState.LocalPlayer.Position, targetPos)}");
			try
			{
				ImGui.Text($"IsFlying: {((Structs.Character*)Svc.Targets.Target.Address)->IsFlying}");
			}
			catch
			{
			}
			ImGui.Separator();
		}
		Svc.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out var pos);
		ImGui.Text($"Mouse Position: {pos:f3}");
		ImGui.Separator();
		ushort territoryID;
		territoryID = Svc.ClientState.TerritoryType;
		TerritoryType map;
		map = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(territoryID);
		ImGui.Text($"Territory ID: {territoryID}");
		ImGui.Text($"Territory Name: {map.PlaceName.Value?.Name}");
		if (Svc.ClientState.LocalPlayer != null)
		{
			ImGui.Text("Nearest Aetheryte: " + CoordinatesHelper.GetNearestAetheryte(Svc.ClientState.LocalPlayer.Position, map));
		}
	}

	private unsafe void NoClipMode(IFramework framework)
	{
		if (!noclip)
		{
			return;
		}
		Structs.CameraEx* camera;
		camera = (Structs.CameraEx*)CameraManager.Instance()->GetActiveCamera();
		double xDisp;
		xDisp = 0.0 - Math.Sin(camera->DirH);
		double zDisp;
		zDisp = 0.0 - Math.Cos(camera->DirH);
		double yDisp;
		yDisp = Math.Sin(camera->DirV);
		if (Svc.ClientState.LocalPlayer != null)
		{
			Vector3 curPos;
			curPos = Svc.ClientState.LocalPlayer.Position;
			Vector3 newPos;
			newPos = Vector3.Multiply(displacementFactor, new Vector3((float)xDisp, (float)yDisp, (float)zDisp));
			if (ImGui.GetIO().KeyAlt)
			{
				SetPos(curPos + newPos);
			}
		}
	}

	private static void DrawPositionModButtons(string coord)
	{
		float[] buttonValues;
		buttonValues = new float[4] { 1f, 3f, 5f, 10f };
		float[] array;
		array = buttonValues;
		foreach (float value in array)
		{
			float v;
			v = value;
			if (ImGui.Button($"+{value}###{coord}+{value}"))
			{
				Vector3 offset2;
				offset2 = Vector3.Zero;
				switch (coord)
				{
				case "x":
					offset2.X = v;
					break;
				case "y":
					offset2.Y = v;
					break;
				case "z":
					offset2.Z = v;
					break;
				}
				SetPos(Svc.ClientState.LocalPlayer.Position + offset2);
			}
			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			{
				v = 0f - v;
				Vector3 offset;
				offset = Vector3.Zero;
				switch (coord)
				{
				case "x":
					offset.X = v;
					break;
				case "y":
					offset.Y = v;
					break;
				case "z":
					offset.Z = v;
					break;
				}
				SetPos(Svc.ClientState.LocalPlayer.Position + offset);
			}
			if (Array.IndexOf(buttonValues, value) < buttonValues.Length - 1)
			{
				ImGui.SameLine();
			}
		}
	}

	public static void SetSpeed(float speedBase)
	{
		Svc.SigScanner.TryScanText("f3 ?? ?? ?? ?? ?? ?? ?? e8 ?? ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 0f ?? ?? e8 ?? ?? ?? ?? f3 ?? ?? ?? ?? ?? ?? ?? f3 ?? ?? ?? ?? ?? ?? ?? f3 ?? ?? ?? f3", out var address);
		address = address + 4 + Marshal.ReadInt32(address + 4) + 4;
		SafeMemory.Write(address + 20, speedBase);
		SetMoveControlData(speedBase);
	}

	private unsafe static void SetMoveControlData(float speed)
	{
		SafeMemory.Write(((delegate* unmanaged[Stdcall]<byte, nint>)Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 ?? ?? 74 ?? 83 ?? ?? 75 ?? 0F ?? ?? ?? 66"))(1) + 8, speed);
	}

	public static void SetPosToMouse()
	{
		if (!(Svc.ClientState.LocalPlayer == null))
		{
			Vector2 mousePos;
			mousePos = ImGui.GetIO().MousePos;
			Svc.GameGui.ScreenToWorld(mousePos, out var pos);
			Svc.Log.Info($"Moving from {pos.X}, {pos.Z}, {pos.Y}");
			if (pos != Vector3.Zero)
			{
				SetPos(pos);
			}
		}
	}

	public static void SetPos(Vector3 pos)
	{
		SetPos(pos.X, pos.Z, pos.Y);
	}

	public unsafe static void SetPos(float x, float y, float z)
	{
		if (SetPosFunPtr != IntPtr.Zero && Svc.ClientState.LocalPlayer != null)
		{
			((delegate* unmanaged[Stdcall]<long, float, float, float, long>)SetPosFunPtr)(Svc.ClientState.LocalPlayer.Address, x, z, y);
		}
	}
}
