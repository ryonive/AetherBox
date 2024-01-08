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
namespace AetherBox.Features.Debugging
{
    public class ActionDebug : DebugHelper
    {
        private readonly unsafe ActionManager* inst = ActionManager.Instance();
        private ActionType actionType;
        private uint actionID;

        public override string Name => nameof(ActionDebug).Replace("Debug", "") + " Debugging";

        public unsafe float AnimationLock => AddressHelper.ReadField<float>((void*)this.inst, 8);

        public override unsafe void Draw()
        {
            ImGui.Text(this.Name ?? "");
            ImGui.Separator();
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(11, 1);
            interpolatedStringHandler.AppendLiteral("Anim lock: ");
            interpolatedStringHandler.AppendFormatted<float>(this.AnimationLock, "f3");
            ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            ActionManager.Instance()->UseActionLocation(this.actionType, this.actionID, 3758096384UL, (Vector3*)null, 0U);
            List<ActionType> list = ((IEnumerable<ActionType>) Enum.GetValues(typeof (ActionType))).ToList<ActionType>();
            ActionType actionType = list[0];
            int index1 = 0;
            if (ImGui.BeginCombo("Action Type", actionType.ToString()))
            {
                for (int index2 = 0; index2 < list.Count; ++index2)
                {
                    if (ImGui.Selectable(list[index2].ToString(), index1 == index2))
                    {
                        index1 = index2;
                        int num = (int) list[index1];
                    }
                }
                ImGui.EndCombo();
            }
            AtkUnitBase* AddonPtr;
            if (!ImGui.Button("mail delete") || !GenericHelpers.TryGetAddonByName<AtkUnitBase>("LetterViewer", out AddonPtr))
                return;
            if (AddonPtr->UldManager.NodeList[2]->GetAsAtkComponentButton()->IsEnabled)
                Svc.Log.Info("del button enabled");
            else
                Svc.Log.Info("disabled");
        }
    }
}
