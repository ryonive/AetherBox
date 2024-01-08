using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

#nullable disable
namespace AetherBox.Helpers
{
    public class OverrideMovement : IDisposable
    {
        public bool IgnoreUserInput;
        public Vector3 DesiredPosition;
        public float Precision = 0.01f;
        [Signature("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D")]
        private Hook<OverrideMovement.RMIWalkDelegate> _rmiWalkHook;
        [Signature("E8 ?? ?? ?? ?? 0F B6 0D ?? ?? ?? ?? B8")]
        private Hook<OverrideMovement.RMIFlyDelegate> _rmiFlyHook;

        public bool Enabled
        {
            get => this._rmiWalkHook.IsEnabled;
            set
            {
                if (value)
                {
                    this._rmiWalkHook.Enable();
                    this._rmiFlyHook.Enable();
                }
                else
                {
                    this._rmiWalkHook.Disable();
                    this._rmiFlyHook.Disable();
                }
            }
        }

        public OverrideMovement()
        {
            Svc.Hook.InitializeFromAttributes((object)this);
            var log1 = Svc.Log;
            var interpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 1);
            interpolatedStringHandler.AppendLiteral("RMIWalk address: 0x");
            interpolatedStringHandler.AppendFormatted<IntPtr>(this._rmiWalkHook.Address, "X");
            var stringAndClear1 = interpolatedStringHandler.ToStringAndClear();
            var objArray1 = Array.Empty<object>();
            log1.Information(stringAndClear1, objArray1);
            var log2 = Svc.Log;
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
            interpolatedStringHandler.AppendLiteral("RMIFly address: 0x");
            interpolatedStringHandler.AppendFormatted<IntPtr>(this._rmiFlyHook.Address, "X");
            var stringAndClear2 = interpolatedStringHandler.ToStringAndClear();
            var objArray2 = Array.Empty<object>();
            log2.Information(stringAndClear2, objArray2);
        }

        public void Dispose()
        {
            this._rmiWalkHook.Dispose();
            this._rmiFlyHook.Dispose();
        }

        private unsafe void RMIWalkDetour(
          void* self,
          float* sumLeft,
          float* sumForward,
          float* sumTurnLeft,
          byte* haveBackwardOrStrafe,
          byte* a6,
          byte bAdditiveUnk)
        {
            this._rmiWalkHook.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);
            if (bAdditiveUnk != (byte)0 || !this.IgnoreUserInput && ((double)*sumLeft != 0.0 || (double)*sumForward != 0.0))
                return;
            var destination = this.DirectionToDestination(false);
            if (!destination.HasValue)
                return;
            var direction = destination.Value.h.ToDirection();
            *sumLeft = direction.X;
            *sumForward = direction.Y;
        }

        private unsafe void RMIFlyDetour(void* self, PlayerMoveControllerFlyInput* result)
        {
            this._rmiFlyHook.Original(self, result);
            if (!this.IgnoreUserInput && ((double)result->Forward != 0.0 || (double)result->Left != 0.0 || (double)result->Up != 0.0))
                return;
            var destination = this.DirectionToDestination(true);
            if (!destination.HasValue)
                return;
            var direction = destination.Value.h.ToDirection();
            result->Forward = direction.Y;
            result->Left = direction.X;
            result->Up = destination.Value.v.Rad;
        }

        private unsafe (NumberHelper.Angle h, NumberHelper.Angle v)? DirectionToDestination(
          bool allowVertical)
        {
            var localPlayer = Svc.ClientState.LocalPlayer;
            if ((GameObject)localPlayer == (GameObject)null)
                return new (NumberHelper.Angle, NumberHelper.Angle)?();
            var vector3 = this.DesiredPosition - localPlayer.Position;
            if ((double)vector3.LengthSquared() <= (double)this.Precision * (double)this.Precision)
                return new (NumberHelper.Angle, NumberHelper.Angle)?();
            var angle1 = NumberHelper.Angle.FromDirection(vector3.X, vector3.Z);
            var angle2 = allowVertical ? NumberHelper.Angle.FromDirection(vector3.Y, new Vector2(vector3.X, vector3.Z).Length()) : new NumberHelper.Angle();
            var angle3 = ((CameraEx*) CameraManager.Instance()->GetActiveCamera())->DirH.Radians() + 180.Degrees();
            return new (NumberHelper.Angle, NumberHelper.Angle)?((angle1 - angle3, angle2));
        }

        private unsafe delegate void RMIWalkDelegate(
          void* self,
          float* sumLeft,
          float* sumForward,
          float* sumTurnLeft,
          byte* haveBackwardOrStrafe,
          byte* a6,
          byte bAdditiveUnk);

        private unsafe delegate void RMIFlyDelegate(void* self, PlayerMoveControllerFlyInput* result);
    }
}
