using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework;
using EVESharpCore.Questor.BackgroundTasks;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void MoveToAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                return;

            //we cant move in bastion mode, do not try
            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                return;

            ESCache.Instance.NormalNavigation = false;

            string target = action.GetParameterValue("target");

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
                target = "Acceleration Gate";

            int DistanceToApproach;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToApproach))
            {
                DistanceToApproach = 5000;

                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && MissionSettings.MyMission != null && MissionSettings.MyMission.Name.ToLower().Contains("Cash Flow for Capsuleers".ToLower()))
                    DistanceToApproach = 500;
            }

            bool stopWhenTargeted;
            if (!bool.TryParse(action.GetParameterValue("StopWhenTargeted"), out stopWhenTargeted))
                stopWhenTargeted = false;

            bool stopWhenAggressed;
            if (!bool.TryParse(action.GetParameterValue("StopWhenAggressed"), out stopWhenAggressed))
                stopWhenAggressed = false;

            bool orderDescending;
            if (!bool.TryParse(action.GetParameterValue("OrderDescending"), out orderDescending))
                orderDescending = false;

            List<EntityCache> targets = new List<EntityCache>();
            if (ESCache.Instance.EntitiesOnGrid.Count > 0)
                if (ESCache.Instance.EntitiesOnGrid.Any(e => e.Name.ToLower() == target.ToLower()))
                    targets = ESCache.Instance.EntitiesOnGrid.Where(e => e.Name.ToLower() == target.ToLower()).ToList();

            if (target.Contains(" Wreck") && targets.Any(i => i.IsWreck))
            {
                List<EntityCache> tempTargets = new List<EntityCache>();
                tempTargets = targets.Where(i => i.IsWreck && !i.IsWreckEmpty).ToList();
                targets = tempTargets;
                if (targets.All(i => i.IsWreckEmpty))
                {
                    Log.WriteLine("Wreck [" + target + "] is empty. proceeding to next action");
                    NextAction(myMission, myAgent, true);
                    return;
                }
            }

            if (targets.Count == 0)
            {
                Log.WriteLine("no entities found named [" + target + "] proceeding to next action");
                NextAction(myMission, myAgent, true);
                return;
            }

            EntityCache moveToTarget = targets.OrderBy(t => t.Distance).FirstOrDefault();

            if (orderDescending)
            {
                Log.WriteLine(" moveTo: orderDescending == true");
                moveToTarget = targets.OrderByDescending(t => t.Distance).FirstOrDefault();
            }

            //Combat.Combat.GetBestPrimaryWeaponTarget(Combat.Combat.MaxRange, false, "Combat");

            if (moveToTarget != null)
            {
                if (stopWhenTargeted)
                    if (Combat.Combat.TargetedBy.Count > 0)
                        if (ESCache.Instance.FollowingEntity != null)
                            if (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity != 0 && DateTime.UtcNow > Time.Instance.NextApproachAction)
                            {
                                if (!NavigateOnGrid.StopMyShip("Stop ship, we have been targeted and are [" + DistanceToApproach + "] from [ID: " + moveToTarget.Name +
                                                               "][" +
                                                               Math.Round(moveToTarget.Distance / 1000, 0) + "k away]")) return;
                                NextAction(myMission, myAgent, true);
                            }

                if (stopWhenAggressed)
                    if (Combat.Combat.Aggressed.Any(t => !t.IsSentry))
                        if (ESCache.Instance.FollowingEntity != null)
                            if (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity != 0 && DateTime.UtcNow > Time.Instance.NextApproachAction)
                            {
                                if (!NavigateOnGrid.StopMyShip("Stop ship, we have been aggressed and are [" + DistanceToApproach + "] from [ID: " + moveToTarget.Name + "][" +
                                                               Math.Round(moveToTarget.Distance / 1000, 0) + "k away]")) return;
                                NextAction(myMission, myAgent, true);
                            }

                if (moveToTarget.Distance < DistanceToApproach) // if we are inside the range that we are supposed to approach assume we are done
                {
                    Log.WriteLine("We are [" + Math.Round(moveToTarget.Distance, 0) + "] from a [" + target + "] we do not need to go any further");
                    NextAction(myMission, myAgent, true);

                    if (ESCache.Instance.FollowingEntity != null)
                        if (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity != 0 && DateTime.UtcNow > Time.Instance.NextApproachAction)
                            if (!ESCache.Instance.InAbyssalDeadspace && !NavigateOnGrid.StopMyShip("Stop ship, we are inside [" + DistanceToApproach + "] from [ID: " + moveToTarget.Name + "][" +
                                                                                                   Math.Round(moveToTarget.Distance / 1000, 0) + "k away]")) return;

                    return;
                }

                NavigateOnGrid.NavigateToTarget(moveToTarget, DistanceToApproach);
            }
        }

        #endregion Methods
    }
}