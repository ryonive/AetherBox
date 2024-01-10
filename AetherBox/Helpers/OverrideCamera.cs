using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace AetherBox.Helpers
{
    public class OverrideCamera : IDisposable
    {
        private unsafe delegate void RMICameraDelegate(CameraEx* self, int inputMode, float speedH, float speedV);

        public bool IgnoreUserInput;
        public NumberHelper.Angle DesiredAzimuth;
        public NumberHelper.Angle DesiredAltitude;
        public NumberHelper.Angle SpeedH = 360.Degrees();
        public NumberHelper.Angle SpeedV = 360.Degrees();

        // Hook for RMICameraDelegate
        [Signature("40 53 48 83 EC 70 44 0F 29 44 24 ?? 48 8B D9")]
        private Hook<RMICameraDelegate> _rmiCameraHook;

        public bool Enabled
        {
            get => _rmiCameraHook.IsEnabled;
            set
            {
                if (value)
                {
                    _rmiCameraHook.Enable();
                    Svc.Log.Debug("OverrideCamera enabled.");
                }
                else
                {
                    _rmiCameraHook.Disable();
                    Svc.Log.Debug("OverrideCamera disabled.");
                }
            }
        }

        public OverrideCamera()
        {
            Svc.Hook.InitializeFromAttributes(this);
            Svc.Log.Information($"RMICamera address: 0x{_rmiCameraHook.Address:X}");
        }

        public void Dispose()
        {
            _rmiCameraHook.Dispose();
        }

        private unsafe void RMICameraDetour(CameraEx* self, int inputMode, float speedH, float speedV)
        {
            _rmiCameraHook.Original(self, inputMode, speedH, speedV);

            if (IgnoreUserInput || inputMode == 0)
            {
                float dt = Framework.Instance()->FrameDeltaTime;
                NumberHelper.Angle deltaH = (DesiredAzimuth - self->DirH.Radians()).Normalized();
                NumberHelper.Angle deltaV = (DesiredAltitude - self->DirV.Radians()).Normalized();
                float maxH = SpeedH.Rad * dt;
                float maxV = SpeedV.Rad * dt;
                self->InputDeltaH = Math.Clamp(deltaH.Rad, 0f - maxH, maxH);
                self->InputDeltaV = Math.Clamp(deltaV.Rad, 0f - maxV, maxV);
            }
        }
    }
}
