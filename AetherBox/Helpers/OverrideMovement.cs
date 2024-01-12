using System;
using System.Numerics;
using AetherBox.Helpers;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace AetherBox.Helpers;

public class OverrideMovement
{
    // is causing hook leak
    // +0x13620A0
    // +0x135B0C0

    private unsafe delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);

	private unsafe delegate void RMIFlyDelegate(void* self, PlayerMoveControllerFlyInput* result);

	public bool IgnoreUserInput;

	public Vector3 DesiredPosition;

	public float Precision = 0.01f;

	[Signature("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D")]
	private Hook<RMIWalkDelegate> _rmiWalkHook;

	[Signature("E8 ?? ?? ?? ?? 0F B6 0D ?? ?? ?? ?? B8")]
	private Hook<RMIFlyDelegate> _rmiFlyHook;

	public bool Enabled
	{
		get
		{
			return _rmiWalkHook.IsEnabled;
		}
		set
		{
			if (value)
			{
				_rmiWalkHook.Enable();
				_rmiFlyHook.Enable();
			}
			else
			{
				_rmiWalkHook.Disable();
				_rmiFlyHook.Disable();
			}
		}
	}

	public OverrideMovement()
	{
		Svc.Hook.InitializeFromAttributes(this);
		Svc.Log.Information($"RMIWalk address: 0x{_rmiWalkHook.Address:X}");
		Svc.Log.Information($"RMIFly address: 0x{_rmiFlyHook.Address:X}");
	}

    public void Dispose()
    {
        // Disable hooks before disposing to prevent further hook calls.
        //Enabled = false;

        // Dispose of the hooks.
        _rmiWalkHook.Dispose();
        _rmiFlyHook.Dispose();

    }

    private unsafe void RMIWalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
	{
		_rmiWalkHook.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);
		if (bAdditiveUnk == 0 && (IgnoreUserInput || (*sumLeft == 0f && *sumForward == 0f)))
		{
			(NumberHelper.Angle, NumberHelper.Angle)? relDir;
			relDir = DirectionToDestination(allowVertical: false);
			if (relDir.HasValue)
			{
				Vector2 dir;
				dir = relDir.Value.Item1.ToDirection();
				*sumLeft = dir.X;
				*sumForward = dir.Y;
			}
		}
	}

	private unsafe void RMIFlyDetour(void* self, PlayerMoveControllerFlyInput* result)
	{
		_rmiFlyHook.Original(self, result);
		if (IgnoreUserInput || (result->Forward == 0f && result->Left == 0f && result->Up == 0f))
		{
			(NumberHelper.Angle, NumberHelper.Angle)? relDir;
			relDir = DirectionToDestination(allowVertical: true);
			if (relDir.HasValue)
			{
				Vector2 dir;
				dir = relDir.Value.Item1.ToDirection();
				result->Forward = dir.Y;
				result->Left = dir.X;
				result->Up = relDir.Value.Item2.Rad;
			}
		}
	}

	private unsafe (NumberHelper.Angle h, NumberHelper.Angle v)? DirectionToDestination(bool allowVertical)
	{
		PlayerCharacter player;
		player = Svc.ClientState.LocalPlayer;
		if (player == null)
		{
			return null;
		}
		Vector3 dist;
		dist = DesiredPosition - player.Position;
		if (dist.LengthSquared() <= Precision * Precision)
		{
			return null;
		}
		NumberHelper.Angle angle;
		angle = NumberHelper.Angle.FromDirection(dist.X, dist.Z);
		NumberHelper.Angle dirV;
		dirV = (allowVertical ? NumberHelper.Angle.FromDirection(dist.Y, new Vector2(dist.X, dist.Z).Length()) : default(NumberHelper.Angle));
		CameraEx* camera;
		camera = (CameraEx*)CameraManager.Instance()->GetActiveCamera();
		NumberHelper.Angle cameraDir;
		cameraDir = camera->DirH.Radians() + 180.Degrees();
		return (angle - cameraDir, dirV);
	}
}
