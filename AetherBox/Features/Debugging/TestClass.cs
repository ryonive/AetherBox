using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using AetherBox.Debugging;
using AetherBox.Helpers;
using AngleSharp.Dom;
using Dalamud;
using EasyCombat.UI.Helpers;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace AetherBox.Features.Debugging;

public class TestClass : DebugHelper
{
    private readonly FeatureProvider provider = new FeatureProvider(Assembly.GetExecutingAssembly());

    public override string Name => "TestClass".Replace("Debug", "") + " Debugging";

    public unsafe override void Draw()
    {
        DrawHeader();
        DrawTestButtons();
/*
        try
        {
            DrawTestTargetPosition();
        }
        catch (Exception ex)
        {
            ex.Log();
        }
*/


    }

    private void DrawHeader()
    {
        ImGuiHelper.TextCentered(AetherColor.CyanBright, Name ?? "");
        ImGuiHelper.SeperatorWithSpacing();
    }
    private void DrawTestButtons()
    {
        if (ImGui.Button("Jump"))
        {
            GeneralActionJump();
        }
        ImGui.SameLine();
        if (ImGui.Button("CD 10"))
        {
            Chat.Instance.SendMessage("/countdown 10");
        }
        ImGui.SameLine();
        if (ImGui.Button("CD cancel"))
        {
            Chat.Instance.SendMessage("/countdown");
        }
        ImGui.SameLine();
        if (ImGui.Button("Open Dalamud Log"))
        {
            Chat.Instance.SendMessage("/xllog");
        }
        ImGuiHelper.SeperatorWithSpacing();
    }
    private unsafe void DrawTestTargetPosition()
    {
        ImGuiHelper.TextCentered(AetherColor.CyanBright, "Positions" ?? "");
        ImGuiHelper.SeperatorWithSpacing();

        float tableWidth = ImGui.GetWindowWidth() / 2;
        float rowHeight = 600;
        float rowHeight2 = ImGui.GetContentRegionAvail().Y;
        Vector2 tableSize = new Vector2(tableWidth, rowHeight );
        if (ImGui.BeginChild("PlayerPositionWindow", tableSize, true, ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (Svc.ClientState.LocalPlayer != null)
            {
                float playerPositionX;
                float playerPositionY;
                float playerPositionZ;

                Vector3 curPos = Svc.ClientState.LocalPlayer.Position;
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
            }
            else
            {
                ImGuiHelper.TextCentered(AetherColor.RedBright, "Svc.ClientState.LocalPlayer is Null!");
            }
            ImGui.EndChild();
        }
        ImGui.SameLine();
        if (ImGui.BeginChild("TargetPositionWindow", tableSize, true))
        {
            if (Svc.ClientState.LocalPlayer != null)
            {
                float targetPositionX;
                float targetPositionY;
                float targetPositionZ;

                Vector3 tarCurPos = Svc.Targets.Target.Position;
                targetPositionX = tarCurPos.X;
                targetPositionY = tarCurPos.Y;
                targetPositionZ = tarCurPos.Z;
                ImGui.Text("Target Position:");
                ImGui.PushItemWidth(75f);
                ImGui.InputFloat("X", ref targetPositionX);
                ImGui.SameLine();
                DrawPositionModButtons("x");
                ImGui.PushItemWidth(75f);
                ImGui.InputFloat("Y", ref targetPositionY);
                ImGui.SameLine();
                DrawPositionModButtons("y");
                ImGui.PushItemWidth(75f);
                ImGui.InputFloat("Z", ref targetPositionZ);
                ImGui.SameLine();
                DrawPositionModButtons("z");
            }
            else
            {
                ImGuiHelper.TextCentered(AetherColor.RedBright, "Svc.ClientState.LocalPlayer is Null!");
            }
            ImGui.EndChild();
        }





        ImGuiHelper.SeperatorWithSpacing();
        ImGui.Text($"Movement Speed: {playerController->MoveControllerWalk.BaseMovementSpeed}");
        ImGui.PushItemWidth(150f);
        ImGui.SliderFloat("Speed Multiplier", ref speedMultiplier, 0f, 20f);
        ImGui.SameLine();
        if (ImGui.Button("Set"))
        {
            SetTargetSpeed(speedMultiplier * 6f);
        }
        ImGui.SameLine();
        if (ImGui.Button("Reset"))
        {
            speedMultiplier = 1f;
            SetTargetSpeed(speedMultiplier * 6f);
        }
        ImGui.Text($"IsMoving: {AgentMap.Instance()->IsPlayerMoving == 1}");
        ImGuiHelper.SeperatorWithSpacing();

        if (Svc.Targets.Target != null || Svc.Targets.PreviousTarget != null)
        {
            Vector3 targetPos = ((Svc.Targets.Target != null) ? Svc.Targets.Target.Position : Svc.Targets.PreviousTarget.Position);

            string str = ((Svc.Targets.Target != null) ? "Target" : "Last Target");
            ImGui.Text($"{str} Position: {targetPos:f3}");
            if (ImGui.Button("TP to " + str))
            {
                SetEnemyPos(targetPos);
            }
            ImGui.Text($"Distance to {str}: {Vector3.Distance(Svc.ClientState.LocalPlayer.Position, targetPos)}");
            try
            {
                if (Svc.Targets.Target != null)
                {
                    ImGui.Text($"IsFlying: {((Structs.Character*)Svc.Targets.Target.Address)->IsFlying}");
                }
            }
            catch (Exception ex)
            { Svc.Log.Warning($"{ex}"); }
            ImGuiHelper.SeperatorWithSpacing();
        }
        Svc.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out var pos);
        ImGui.Text($"Mouse Position: {pos:f3}");
        ImGui.Separator();
        ushort territoryID = Svc.ClientState.TerritoryType;

        TerritoryType map = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(territoryID);
        ImGui.Text($"Territory ID: {territoryID}");
        ImGui.Text($"Territory Name: {map.PlaceName.Value?.Name}");
        if (Svc.ClientState.LocalPlayer != null)
        {
            ImGui.Text("Nearest Aetheryte: " + CoordinatesHelper.GetNearestAetheryte(Svc.ClientState.LocalPlayer.Position, map));
        }
    }

    #region Actionmanager
    public static bool GeneralActionJump()
    {
        return Jump();
    }

    private static unsafe bool Jump()
    {
        return FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2);
    }
    #endregion

    #region Teleport Target Test
    private bool noclip;

    private float displacementFactor = 0.1f;

    private readonly float cameriaH;

    private readonly float cameriaV;

    private Vector3 lastTargetPos;

    private unsafe readonly Structs.PlayerController* playerController = (Structs.PlayerController*)Svc.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 3C 01 75 1E 48 8D 0D");

    private float speedMultiplier = 1f;

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

    private static void SetTargetSpeed(float speedBase)
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

    private static void DrawPositionModButtons(string coord)
    {
        float[] buttonValues = new float[4] { 1f, 3f, 5f, 10f };
        float[] array= buttonValues;
        foreach (float value in array)
        {
            float v = value;
            if (ImGui.Button($"+{value}###{coord}+{value}"))
            {
                Vector3 offset2 = Vector3.Zero;
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
                SetEnemyPos(Svc.Targets.Target.Position + offset2);
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                v = 0f - v;
                Vector3 offset = Vector3.Zero;
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
                SetEnemyPos(Svc.Targets.Target.Position + offset);
            }
            if (Array.IndexOf(buttonValues, value) < buttonValues.Length - 1)
            {
                ImGui.SameLine();
            }
        }
    }

    public static void SetEnemyPosToMouse()
    {
        if (!(Svc.ClientState.LocalPlayer == null) && Svc.Targets.Target != null)
        {
            Vector2 mousePos = ImGui.GetIO().MousePos;
            Svc.GameGui.ScreenToWorld(mousePos, out var pos);
            Svc.Log.Info($"Moving from {pos.X}, {pos.Z}, {pos.Y}");
            if (pos != Vector3.Zero)
            {
                SetEnemyPos(pos);
            }
        }
    }

    public static void SetEnemyPos(Vector3 pos)
    {
        SetEnemyPos(pos.X, pos.Z, pos.Y);
    }

    public unsafe static void SetEnemyPos(float x, float y, float z)
    {
        if (SetPosFunPtr != IntPtr.Zero && Svc.Targets.Target != null)
        {
            ((delegate* unmanaged[Stdcall]<long, float, float, float, long>)SetPosFunPtr)(Svc.Targets.Target.Address, x, z, y);
        }
    }
    #endregion
}
