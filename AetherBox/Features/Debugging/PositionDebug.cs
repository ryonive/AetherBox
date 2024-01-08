using AetherBox.Debugging;
using AetherBox.Helpers;
using Dalamud;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable disable
namespace AetherBox.Features.Debugging
{
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
        private readonly unsafe PositionDebug.PlayerController* playerController = (PositionDebug.PlayerController*) Svc.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 3C 01 75 1E 48 8D 0D");
        private float speedMultiplier = 1f;

        public override string Name => nameof(PositionDebug).Replace("Debug", "") + " Debugging";

        public override unsafe void Draw()
        {
            ImGui.Text(this.Name ?? "");
            ImGui.Separator();
            if ((GameObject)Svc.ClientState.LocalPlayer != (GameObject)null)
            {
                Vector3 position = Svc.ClientState.LocalPlayer.Position;
                this.playerPositionX = position.X;
                this.playerPositionY = position.Y;
                this.playerPositionZ = position.Z;
                ImGui.Text("Your Position:");
                ImGui.PushItemWidth(75f);
                ImGui.InputFloat("X", ref this.playerPositionX);
                ImGui.SameLine();
                PositionDebug.DrawPositionModButtons("x");
                ImGui.PushItemWidth(75f);
                ImGui.InputFloat("Y", ref this.playerPositionY);
                ImGui.SameLine();
                PositionDebug.DrawPositionModButtons("y");
                ImGui.PushItemWidth(75f);
                ImGui.InputFloat("Z", ref this.playerPositionZ);
                ImGui.SameLine();
                PositionDebug.DrawPositionModButtons("z");
                if (ImGui.Checkbox("No Clip Mode", ref this.noclip))
                {
                    if (this.noclip)
                        Svc.Framework.Update += new IFramework.OnUpdateDelegate(this.NoClipMode);
                    else
                        Svc.Framework.Update -= new IFramework.OnUpdateDelegate(this.NoClipMode);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Hold CTRL");
                ImGui.SameLine();
                ImGui.InputFloat("Displacement Factor", ref this.displacementFactor);
                ImGui.Separator();
            }
            PositionDebug.CameraEx* activeCamera = (PositionDebug.CameraEx*) CameraManager.Instance()->GetActiveCamera();
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
            interpolatedStringHandler.AppendLiteral("Camera H: ");
            interpolatedStringHandler.AppendFormatted<float>(activeCamera->DirH, "f3");
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
            interpolatedStringHandler.AppendLiteral("Camera V: ");
            interpolatedStringHandler.AppendFormatted<float>(activeCamera->DirV, "f3");
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            ImGui.Separator();
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 1);
            interpolatedStringHandler.AppendLiteral("Movement Speed: ");
            interpolatedStringHandler.AppendFormatted<float>(this.playerController->MoveControllerWalk.BaseMovementSpeed);
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            ImGui.PushItemWidth(150f);
            ImGui.SliderFloat("Speed Multiplier", ref this.speedMultiplier, 0.0f, 20f);
            ImGui.SameLine();
            if (ImGui.Button("Set"))
                PositionDebug.SetSpeed(this.speedMultiplier * 6f);
            ImGui.SameLine();
            if (ImGui.Button("Reset"))
            {
                this.speedMultiplier = 1f;
                PositionDebug.SetSpeed(this.speedMultiplier * 6f);
            }
            ImGui.Separator();
            if (Svc.Targets.Target != (GameObject)null || Svc.Targets.PreviousTarget != (GameObject)null)
            {
                Vector3 pos = Svc.Targets.Target != (GameObject) null ? Svc.Targets.Target.Position : Svc.Targets.PreviousTarget.Position;
                string str = Svc.Targets.Target != (GameObject) null ? "Target" : "Last Target";
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 4);
                interpolatedStringHandler.AppendFormatted(str);
                interpolatedStringHandler.AppendLiteral(" Position: x: ");
                interpolatedStringHandler.AppendFormatted<float>(pos.X, "f3");
                interpolatedStringHandler.AppendLiteral(", y: ");
                interpolatedStringHandler.AppendFormatted<float>(pos.Y, "f3");
                interpolatedStringHandler.AppendLiteral(", z: ");
                interpolatedStringHandler.AppendFormatted<float>(pos.Z, "f3");
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                if (ImGui.Button("TP to " + str))
                    PositionDebug.SetPos(pos);
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 2);
                interpolatedStringHandler.AppendLiteral("Distance to ");
                interpolatedStringHandler.AppendFormatted(str);
                interpolatedStringHandler.AppendLiteral(": ");
                interpolatedStringHandler.AppendFormatted<float>(Vector3.Distance(Svc.ClientState.LocalPlayer.Position, pos));
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                ImGui.Separator();
            }
            Vector3 worldPos;
            Svc.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out worldPos);
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 3);
            interpolatedStringHandler.AppendLiteral("Mouse Position: x: ");
            interpolatedStringHandler.AppendFormatted<float>(worldPos.X, "f3");
            interpolatedStringHandler.AppendLiteral(", y: ");
            interpolatedStringHandler.AppendFormatted<float>(worldPos.Y, "f3");
            interpolatedStringHandler.AppendLiteral(", z: ");
            interpolatedStringHandler.AppendFormatted<float>(worldPos.Z, "f3");
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            ImGui.Separator();
            ushort territoryType = Svc.ClientState.TerritoryType;
            TerritoryType row = Svc.Data.GetExcelSheet<TerritoryType>().GetRow((uint) territoryType);
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
            interpolatedStringHandler.AppendLiteral("Territory ID: ");
            interpolatedStringHandler.AppendFormatted<ushort>(territoryType);
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 1);
            interpolatedStringHandler.AppendLiteral("Territory Name: ");
            interpolatedStringHandler.AppendFormatted<SeString>(row.PlaceName.Value?.Name);
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            if (!((GameObject)Svc.ClientState.LocalPlayer != (GameObject)null))
                return;
            ImGui.Text("Nearest Aetheryte: " + CoordinatesHelper.GetNearestAetheryte(Svc.ClientState.LocalPlayer.Position, row));
        }

        private unsafe void NoClipMode(IFramework framework)
        {
            if (!this.noclip)
                return;
            PositionDebug.CameraEx* activeCamera = (PositionDebug.CameraEx*) CameraManager.Instance()->GetActiveCamera();
            double x = -Math.Sin((double) activeCamera->DirH);
            double z = -Math.Cos((double) activeCamera->DirH);
            double y = Math.Sin((double) activeCamera->DirV);
            if (!((GameObject)Svc.ClientState.LocalPlayer != (GameObject)null))
                return;
            Vector3 position = Svc.ClientState.LocalPlayer.Position;
            Vector3 vector3 = Vector3.Multiply(this.displacementFactor, new Vector3((float) x, (float) y, (float) z));
            if (!ImGui.GetIO().KeyAlt)
                return;
            PositionDebug.SetPos(position + vector3);
        }

        private static void DrawPositionModButtons(string coord)
        {
            float[] array = new float[4]{ 1f, 3f, 5f, 10f };
            foreach (float num1 in array)
            {
                float num2 = num1;
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 3);
                interpolatedStringHandler.AppendLiteral("+");
                interpolatedStringHandler.AppendFormatted<float>(num1);
                interpolatedStringHandler.AppendLiteral("###");
                interpolatedStringHandler.AppendFormatted(coord);
                interpolatedStringHandler.AppendLiteral("+");
                interpolatedStringHandler.AppendFormatted<float>(num1);
                if (ImGui.Button(interpolatedStringHandler.ToStringAndClear()))
                {
                    Vector3 zero = Vector3.Zero;
                    switch (coord)
                    {
                        case "x":
                            zero.X = num2;
                            break;
                        case "y":
                            zero.Y = num2;
                            break;
                        case "z":
                            zero.Z = num2;
                            break;
                    }
                    PositionDebug.SetPos(Svc.ClientState.LocalPlayer.Position + zero);
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    float num3 = -num2;
                    Vector3 zero = Vector3.Zero;
                    switch (coord)
                    {
                        case "x":
                            zero.X = num3;
                            break;
                        case "y":
                            zero.Y = num3;
                            break;
                        case "z":
                            zero.Z = num3;
                            break;
                    }
                    PositionDebug.SetPos(Svc.ClientState.LocalPlayer.Position + zero);
                }
                if (Array.IndexOf<float>(array, num1) < array.Length - 1)
                    ImGui.SameLine();
            }
        }

        public static void SetSpeed(float speedBase)
        {
            IntPtr result;
            Svc.SigScanner.TryScanText("f3 ?? ?? ?? ?? ?? ?? ?? e8 ?? ?? ?? ?? 48 ?? ?? ?? ?? ?? ?? 0f ?? ?? e8 ?? ?? ?? ?? f3 ?? ?? ?? ?? ?? ?? ?? f3 ?? ?? ?? ?? ?? ?? ?? f3 ?? ?? ?? f3", out result);
            SafeMemory.Write<float>(result + new IntPtr(4) + (IntPtr)Marshal.ReadInt32(result + new IntPtr(4)) + new IntPtr(4) + new IntPtr(20), speedBase);
            PositionDebug.SetMoveControlData(speedBase);
        }

        private static void SetMoveControlData(float speed)
        {
            // ISSUE: cast to a function pointer type
            // ISSUE: function pointer call
            SafeMemory.Write<float>(__calli((__FnPtr < IntPtr(byte) >) Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 ?? ?? 74 ?? 83 ?? ?? 75 ?? 0F ?? ?? ?? 66"))((byte)1) + new IntPtr(8), speed);
        }

        public static void SetPosToMouse()
        {
            if ((GameObject)Svc.ClientState.LocalPlayer == (GameObject)null)
                return;
            Vector3 worldPos;
            Svc.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out worldPos);
            IPluginLog log = Svc.Log;
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 3);
            interpolatedStringHandler.AppendLiteral("Moving from ");
            interpolatedStringHandler.AppendFormatted<float>(worldPos.X);
            interpolatedStringHandler.AppendLiteral(", ");
            interpolatedStringHandler.AppendFormatted<float>(worldPos.Z);
            interpolatedStringHandler.AppendLiteral(", ");
            interpolatedStringHandler.AppendFormatted<float>(worldPos.Y);
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            object[] objArray = Array.Empty<object>();
            log.Info(stringAndClear, objArray);
            if (!(worldPos != Vector3.Zero))
                return;
            PositionDebug.SetPos(worldPos);
        }

        public static void SetPos(Vector3 pos) => PositionDebug.SetPos(pos.X, pos.Z, pos.Y);

        public static void SetPos(float x, float y, float z)
        {
            if (PositionDebug.SetPosFunPtr == IntPtr.Zero || !((GameObject)Svc.ClientState.LocalPlayer != (GameObject)null))
                return;
            // ISSUE: cast to a function pointer type
            // ISSUE: function pointer call
            long num = __calli((__FnPtr<long (long, float, float, float)>) PositionDebug.SetPosFunPtr)((float) (long) Svc.ClientState.LocalPlayer.Address, x, z, (long) y);
        }

        private static IntPtr SetPosFunPtr
        {
            get
            {
                IntPtr result;
                return !Svc.SigScanner.TryScanText("E8 ?? ?? ?? ?? 83 4B 70 01", out result) ? IntPtr.Zero : result;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct PlayerController
        {
            [FieldOffset(16)]
            public PositionDebug.PlayerMoveControllerWalk MoveControllerWalk;
            [FieldOffset(336)]
            public PositionDebug.PlayerMoveControllerFly MoveControllerFly;
            [FieldOffset(1369)]
            public byte ControlMode;
        }

        [StructLayout(LayoutKind.Explicit, Size = 320)]
        public struct PlayerMoveControllerWalk
        {
            [FieldOffset(16)]
            public Vector3 MovementDir;
            [FieldOffset(88)]
            public float BaseMovementSpeed;
            [FieldOffset(144)]
            public float MovementDirRelToCharacterFacing;
            [FieldOffset(148)]
            public byte Forced;
            [FieldOffset(160)]
            public Vector3 MovementDirWorld;
            [FieldOffset(176)]
            public float RotationDir;
            [FieldOffset(272)]
            public uint MovementState;
            [FieldOffset(276)]
            public float MovementLeft;
            [FieldOffset(280)]
            public float MovementFwd;
        }

        [StructLayout(LayoutKind.Explicit, Size = 176)]
        public struct PlayerMoveControllerFly
        {
            [FieldOffset(102)]
            public byte IsFlying;
            [FieldOffset(156)]
            public float AngularAscent;
        }

        [StructLayout(LayoutKind.Explicit, Size = 688)]
        public struct CameraEx
        {
            [FieldOffset(304)]
            public float DirH;
            [FieldOffset(308)]
            public float DirV;
            [FieldOffset(312)]
            public float InputDeltaHAdjusted;
            [FieldOffset(316)]
            public float InputDeltaVAdjusted;
            [FieldOffset(320)]
            public float InputDeltaH;
            [FieldOffset(324)]
            public float InputDeltaV;
            [FieldOffset(328)]
            public float DirVMin;
            [FieldOffset(332)]
            public float DirVMax;
        }
    }
}
