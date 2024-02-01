using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;

namespace AetherBox.Features.Actions;

public class ISLockAndMove : Feature
{
    private bool lockingOn;

    public override string Name => "Island Sanctuary Lock & Move";

    public override string Description => "Primitive auto gatherer for Island Sanctuary. After gathering from an island sanctuary node, try to auto-lock onto the nearest gatherable and walk towards it.";

    public override FeatureType FeatureType => FeatureType.Actions;

    public override void Enable()
    {
        Svc.Framework.Update += CheckToJump;
        Svc.Condition.ConditionChange += CheckToLockAndMove;
        base.Enable();
    }

    public override void Disable()
    {
        Svc.Framework.Update -= CheckToJump;
        Svc.Condition.ConditionChange -= CheckToLockAndMove;
        base.Disable();
    }

    private unsafe void CheckToJump(IFramework framework)
    {
        if ((Svc.Targets.Target != null) && Svc.Targets.Target.ObjectKind == ObjectKind.CardStand && IsMoving() && BaseFeature.IsTargetLocked && MJIManager.Instance()->IsPlayerInSanctuary != 0 && ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, 2u, 3758096384uL, checkRecastActive: true, checkCastingActive: true, null) == 0 && Vector3.Distance(Svc.Targets.Target.Position, Player.Object.Position) > 8f && !TaskManager.IsBusy)
        {
            TaskManager.DelayNext(new Random().Next(300, 550));
            TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2u, 3758096384uL, 0u, 0u, 0u, null));
        }
    }

    private unsafe void CheckToLockAndMove(ConditionFlag flag, bool value)
    {
        if (flag != ConditionFlag.OccupiedInQuestEvent || value || MJIManager.Instance()->IsPlayerInSanctuary != 1 || (object)Svc.ClientState.LocalPlayer == null || Svc.ClientState.LocalPlayer.IsCasting)
        {
            return;
        }
        TaskManager.DelayNext(300);
        TaskManager.Enqueue(delegate
        {
            List<GameObject> list;
            list = Svc.Objects.Where((GameObject x) => x.ObjectKind == ObjectKind.CardStand && x.IsTargetable).ToList();
            if (list.Count != 0)
            {
                GameObject gameObject;
                gameObject = list.OrderBy((GameObject x) => Vector3.Distance(x.Position, Player.Object.Position)).FirstOrDefault();
                if (gameObject != null && gameObject.IsTargetable)
                {
                    Svc.Targets.Target = gameObject;
                }
                if (MJIManager.Instance()->CurrentMode == 1)
                {
                    TaskManager.Enqueue(delegate
                    {
                        lockingOn = true;
                        Chat.Instance.SendMessage("/lockon on");
                    });
                    TaskManager.DelayNext(new Random().Next(100, 250));
                    TaskManager.Enqueue(delegate
                    {
                        if (BaseFeature.IsTargetLocked)
                        {
                            Chat.Instance.SendMessage("/automove on");
                            lockingOn = false;
                        }
                    });
                }
            }
        });
    }
}
