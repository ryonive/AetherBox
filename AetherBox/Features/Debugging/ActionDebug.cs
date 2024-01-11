using AetherBox.Debugging;
using AetherBox.Helpers;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#nullable disable
namespace AetherBox.Features.Debugging;

public class ActionDebug : DebugHelper
{
    private unsafe readonly ActionManager* inst = ActionManager.Instance();

    private ActionType actionType;

    private uint actionID;

    public override string Name => "ActionDebug".Replace("Debug", "") + " Debugging";

    public unsafe float AnimationLock => AddressHelper.ReadField<float>(inst, 8);

    public unsafe override void Draw()
    {
        ImGui.Text(Name ?? "");
        ImGui.Separator();
        ImGui.Text($"Anim lock: {AnimationLock:f3}");
        ActionManager.Instance()->UseActionLocation(actionType, actionID, 3758096384uL, null);
        List<ActionType> actionTypes = ((ActionType[])Enum.GetValues(typeof(ActionType))).ToList();
        ActionType prevType = actionTypes[0];
        int selectedTypeIndex = 0;
        if (ImGui.BeginCombo("Action Type", prevType.ToString()))
        {
            for (int i = 0; i < actionTypes.Count; i++)
            {
                if (ImGui.Selectable(actionTypes[i].ToString(), selectedTypeIndex == i))
                {
                    selectedTypeIndex = i;
                    _ = actionTypes[selectedTypeIndex];
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.Button("mail delete") && GenericHelpers.TryGetAddonByName<AtkUnitBase>("LetterViewer", out var addon))
        {
            if (addon->UldManager.NodeList[2]->GetAsAtkComponentButton()->IsEnabled)
            {
                Svc.Log.Info("del button enabled");
            }
            else
            {
                Svc.Log.Info("disabled");
            }
        }
    }
}
