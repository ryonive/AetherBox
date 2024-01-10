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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

#nullable disable
namespace AetherBox.Features.Actions
{
    public class ISLockAndMove : Feature
    {
        private bool lockingOn;

        public override string Name => "Island Sanctuary Lock & Move";

        public override string Description
        {
            get
            {
                return "Primitive auto gatherer for Island Sanctuary. After gathering from an island sanctuary node, try to auto-lock onto the nearest gatherable and walk towards it.";
            }
        }

        public override FeatureType FeatureType => FeatureType.Actions;

        public override void Enable()
        {
            Svc.Framework.Update += new IFramework.OnUpdateDelegate(this.CheckToJump);
            Svc.Condition.ConditionChange += new ICondition.ConditionChangeDelegate(this.CheckToLockAndMove);
            base.Enable();
        }

        public override void Disable()
        {
            Svc.Framework.Update -= new IFramework.OnUpdateDelegate(this.CheckToJump);
            Svc.Condition.ConditionChange -= new ICondition.ConditionChangeDelegate(this.CheckToLockAndMove);
            base.Disable();
        }

        private unsafe void CheckToJump(IFramework framework)
        {
            if (Svc.Targets.Target == (GameObject)null || Svc.Targets.Target.ObjectKind != ObjectKind.CardStand || !this.IsMoving() || !BaseFeature.IsTargetLocked || MJIManager.Instance()->IsPlayerInSanctuary == (byte)0 || ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, 2U, 3758096384UL, true, true, (uint*)null) != 0U || (double)Vector3.Distance(Svc.Targets.Target.Position, Player.Object.Position) <= 8.0 || this.TaskManager.IsBusy)
                return;
            this.TaskManager.DelayNext(new Random().Next(300, 550));
            this.TaskManager.Enqueue((Func<bool?>)(() => new bool?(ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2U, 3758096384UL, 0U, 0U, 0U, (void*)null))));
        }

        private unsafe void CheckToLockAndMove(ConditionFlag flag, bool value)
        {
            if (flag != ConditionFlag.OccupiedInQuestEvent || value || MJIManager.Instance()->IsPlayerInSanctuary != (byte)1 || Svc.ClientState.LocalPlayer == null || Svc.ClientState.LocalPlayer.IsCasting)
                return;
            this.TaskManager.DelayNext(300);
            this.TaskManager.Enqueue((Action)(() =>
            {
                List<GameObject> list = Svc.Objects.Where<GameObject>((Func<GameObject, bool>) (x => x.ObjectKind == ObjectKind.CardStand && x.IsTargetable)).ToList<GameObject>();
                if (list.Count == 0)
                    return;
                GameObject gameObject = list.OrderBy<GameObject, float>((Func<GameObject, float>) (x => Vector3.Distance(x.Position, Player.Object.Position))).FirstOrDefault<GameObject>();
                if (gameObject != (GameObject)null && gameObject.IsTargetable)
                    Svc.Targets.Target = gameObject;
                if (MJIManager.Instance()->CurrentMode != 1U)
                    return;
                this.TaskManager.Enqueue((Action)(() =>
          {
                  this.lockingOn = true;
                  Chat.Instance.SendMessage("/lockon on");
              }));
                this.TaskManager.DelayNext(new Random().Next(100, 250));
                this.TaskManager.Enqueue((Action)(() =>
          {
                  if (!BaseFeature.IsTargetLocked)
                      return;
                  Chat.Instance.SendMessage("/automove on");
                  this.lockingOn = false;
              }));
            }));
        }
    }
}
