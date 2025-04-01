using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static DirectWorldPosition _lastNormalDirectWorldPosition;

        private static void ActivateAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                    return;

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                    return;

                bool optional;
                if (!bool.TryParse(action.GetParameterValue("optional"), out optional))
                    optional = false;

                string target = action.GetParameterValue("target");
                string alternativeTarget = action.GetParameterValue("alternativetarget");

                if (string.IsNullOrEmpty(target))
                    target = "Acceleration Gate";

                if (string.IsNullOrEmpty(alternativeTarget))
                    alternativeTarget = "Acceleration Gate";

                if (NextActionBool) NextAction(myMission, myAgent, true);

                List<EntityCache> targets = new List<EntityCache>();

                try
                {
                    //if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp) return;
                    //if (Time.Instance.LastInWarp.AddSeconds(3) > DateTime.UtcNow) return;

                    if (ESCache.Instance.DistanceFromMe(_lastNormalDirectWorldPosition.PositionInSpace) > (double)Distances.MaxPocketsDistanceKm)
                    {
                        Log.WriteLine("We are in the next pocket: change state to 'NextPocket'");
                        SetHereToCurrentXYZCoord();
                        ChangeCombatMissionCtrlState(ActionControlState.NextPocket, myMission, myAgent);
                        return;
                    }

                    targets = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == target.ToLower()).ToList();
                    if (!targets.Any())
                    {
                        if (myMission != null && myAgent != null)
                            if (CurrentMissionHint(myAgent).Contains("GoToGate"))
                                targets = ESCache.Instance.EntitiesOnGrid.Where(i => CurrentMissionHint(myAgent).Contains(i.Id.ToString())).ToList();

                        if (targets.Count == 0)
                        {
                            targets = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == alternativeTarget.ToLower()).ToList();
                            if (targets.Any()) Log.WriteLine("First target not found, using alternative target [" + alternativeTarget + "]");

                            if (targets.Count == 0)
                                targets = ESCache.Instance.AccelerationGates;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                if (targets.Count == 0)
                {
                    if (!_waiting)
                    {
                        Log.WriteLine("Activate: Can't find [" + target + "] to activate! Waiting [" + Time.Instance.NoGateFoundRetryDelay_seconds + "] seconds before giving up");
                        _waitingSince = DateTime.UtcNow;
                        _waiting = true;
                        return;
                    }

                    if (_waiting)
                    {
                        if (DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds > Time.Instance.NoGateFoundRetryDelay_seconds)
                        {
                            Log.WriteLine("Activate: After [" + Time.Instance.NoGateFoundRetryDelay_seconds + "] seconds of waiting the gate is still not on grid");
                            if (optional) //if this action has the optional parameter defined as true then we are done if we cant find the gate
                            {
                                DoneAction(myMission, myAgent);
                                return;
                            }

                            ChangeCombatMissionCtrlState(ActionControlState.Error, myMission, myAgent);
                            return;
                        }

                        return;
                    }

                    return;
                }

                EntityCache closest = targets.OrderBy(t => t.Distance).FirstOrDefault(); // at least one gate must exists at this point

                if (closest.Distance <= (int)Distances.GateActivationRange + 150) // if gate < 2150m => within activation range
                {
                    // We cant activate if we have drones out
                    if (Drones.ActiveDroneCount > 0)
                    {
                        if (ESCache.Instance.EntitiesOnGrid.Any(t => t.Velocity > 0 && t.IsTargetedBy && t.WarpScrambleChance > 0))
                            return;

                        // Tell the drones module to retract drones
                        Drones.DronesShouldBePulled = true;

                        Log.WriteLine("ActivateAction: Waiting for drones to return to the drone bay: Farthest Drone [" + Math.Round(Drones.ActiveDrones.OrderByDescending(i => i.Distance).FirstOrDefault().Distance / 1000, 0) + "k away][" + Math.Round(Drones.ActiveDrones.OrderByDescending(i => i.Velocity).FirstOrDefault().Distance, 0) + " m/s]");
                        if (Drones.WaitForDronesToReturn)
                        {
                            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (WaitForDronesToReturn)");
                            return;
                        }
                    }

                    if (!ESCache.Instance.InAbyssalDeadspace)
                    {
                        //if (Time.Instance.NextApproachAction < DateTime.UtcNow && closest.Distance < 0) // if distance is below 500, we keep at range 1000
                        //    NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "ActivateAction", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());

                        if (Salvage.CreateSalvageBookmarks) // salvage bookmarks
                            BookmarkPocketForSalvaging(myMission);
                    }

                    if (!Combat.Combat.BoolReloadWeaponsAsap && ESCache.Instance.Weapons.Any(i => !i.IsCivilianWeapon))
                    {
                        Log.WriteLine("ActivateAction: BoolReloadWeaponsAsap = true");
                        Combat.Combat.BoolReloadWeaponsAsap = true;
                    }

                    // if we reach this point we're between <= 2150m && >=0m to the gate
                    if (DateTime.UtcNow > Time.Instance.NextActivateAccelerationGate)
                        if (closest.ActivateAccelerationGate())
                        {
                            SetHereToCurrentXYZCoord();
                            Log.WriteLine("Activate: [" + closest.Name + "]");
                            // Do not change actions, if NextPocket gets a timeout (>2 mins) then it reverts to the last action
                        }

                    return;
                }

                if (closest.Distance < (int)Distances.WarptoDistance) //if we are inside warpto distance then approach
                {
                    if (DebugConfig.DebugActivateGate) Log.WriteLine("if (closest.Distance < (int)Distances.WarptoDistance)");

                    // Move to the target
                    if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                    {
                        if (closest.IsOrbitedByActiveShip || ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != closest.Id ||
                            (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 50) || (ESCache.Instance.FollowingEntity.Id == closest.Id && closest.Distance > (int)Distances.GateActivationRange))
                        {
                            closest._directEntity.MoveToViaAStar();
                            return;
                        }

                        if (DebugConfig.DebugActivateGate)
                            Log.WriteLine("Cache.Instance.IsOrbiting [" + closest.IsOrbitedByActiveShip + "] Cache.Instance.MyShip.Velocity [" +
                                          Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "m/s]");
                        if (DebugConfig.DebugActivateGate)
                            if (ESCache.Instance.FollowingEntity != null)
                                Log.WriteLine("Cache.Instance.Approaching.Id [" + ESCache.Instance.FollowingEntity.Id + "][closest.Id: " + closest.Id + "]");
                        if (DebugConfig.DebugActivateGate) Log.WriteLine("------------------");
                        return;
                    }

                    if (closest.IsOrbitedByActiveShip || ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != closest.Id)
                    {
                        Log.WriteLine("Activate: Delaying approach for: [" + Math.Round(Time.Instance.NextApproachAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                                      "] seconds");
                        return;
                    }

                    if (DebugConfig.DebugActivateGate) Log.WriteLine("------------------");
                    return;
                }

                if (closest.Distance > (int)Distances.WarptoDistance) //we must be outside warpto distance, but we are likely in a DeadSpace so align to the target
                    if (closest.AlignTo())
                        Log.WriteLine("Activate: AlignTo: [" + closest.Name + "] This only happens if we are asked to Activate something that is outside [" +
                                      Distances.CloseToGateActivationRange + "]");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }
}