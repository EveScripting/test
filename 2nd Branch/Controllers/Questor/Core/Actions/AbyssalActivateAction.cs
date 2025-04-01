extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using System;
using System.Linq;
using EVESharpCore.Questor.Behaviors;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework;
using SC::SharedComponents.IPC;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static string AccelerationGateDistanceForLogs
        {
            get
            {
                if (ESCache.Instance.AccelerationGates.Count > 0)
                {
                    return Math.Round(AccelerationGateDistance / 1000, 0).ToString();
                }

                return "0";
            }
        }

        private static double AccelerationGateDistance
        {
            get
            {
                if (ESCache.Instance.AccelerationGates.Count > 0)
                {
                    return ESCache.Instance.AccelerationGates.FirstOrDefault().Distance;
                }

                return 0;
            }
        }

        private static void AbyssalActivateAction(Action action)
        {
            try
            {
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("Entering: AbyssalActivateAction");
                if (NextActionBool) NextAction(null, null, true);

                if (ESCache.Instance.InWarp)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (ESCache.Instance.InWarp)");
                    return;
                }

                if (ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (ESCache.Instance.MyShipEntity.HasInitiatedWarp)");
                    return;
                }

                if (Time.Instance.LastInWarp.AddSeconds(3) > DateTime.UtcNow)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (Time.Instance.LastInWarp.AddSeconds(3) > DateTime.UtcNow)");
                    return;
                }

                if (Time.Instance.LastActivateAccelerationGate.AddSeconds(20) > DateTime.UtcNow)
                {
                    if (ESCache.Instance.AccelerationGates.All(i => i.Id != ESCache.Instance.OldAccelerationGateId))
                    {
                        //
                        // this never gets run? see: ExecutePocketActions: if (ESCache.Instance.InAbyssalDeadspace && _lastNormalX != 0 && _lastNormalY != 0 && _lastNormalZ != 0 && ESCache.Instance.DistanceFromMe(_lastNormalX, _lastNormalY, _lastNormalZ) > (double)Distances.MaxPocketsDistanceKm)
                        //
                        DirectSession.SetSessionNextSessionReady(7000, 9000);
                        Log.WriteLine("Entering Pocket number [" + ActionControl.PocketNumber + "]");
                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                        ESCache.Instance.ClearPerPocketCache();
                        Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                        Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(13);
                        if (DirectEve.Interval(30000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Gate Activated"));
                        //DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "EndGate Activated"));
                        Log.WriteLine("Change state to 'NextPocket' LostDrones [" + Drones.LostDrones + "] AllDronesInSpaceCount [" + Drones.AllDronesInSpaceCount + "]");
                        ChangeCombatMissionCtrlState(ActionControlState.NextPocket, null, null);
                    }
                }

                if (ESCache.Instance.AccelerationGates.Count == 0)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (!ESCache.Instance.AccelerationGates.Any())");
                    if (!_waiting)
                    {
                        Log.WriteLine("Activate: Can't find any gates to activate! Waiting [" + Time.Instance.NoGateFoundRetryDelay_seconds + "] seconds before giving up");
                        _waitingSince = DateTime.UtcNow;
                        _waiting = true;
                        return;
                    }

                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (_waiting)");
                    return;
                }

                EntityCache closest = ESCache.Instance.AccelerationGates.OrderBy(t => t.Distance).FirstOrDefault(); // at least one gate must exists at this point
                if (closest != null)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (closest != null)");
                    NavigateOnGrid.NavigateToTarget(closest, 0);

                    if (closest.Distance > (int)Distances.GateActivationRange)
                    {
                        Log.WriteLine("ActivateAction: [" + closest.Name + "][" + Math.Round(closest.Distance / 1000, 0) + "k away ] going [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + " m/s]: approach gate");
                        closest.Approach();
                    }

                    try
                    {
                        if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway)
                        {
                            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (AbyssalDeadspaceBehavior.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && AbyssalDeadspaceBehavior.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway)");
                            double FarthestDroneDistance = 0;
                            if (Drones.ActiveDroneCount > 0)
                            {
                                FarthestDroneDistance = Math.Round(Drones.ActiveDrones.OrderByDescending(i => i.Distance).FirstOrDefault().Distance / 1000, 0);
                            }

                            if (AccelerationGateDistance < 15000)  //&& Drones.ActiveDrones.Any(i => i.Distance > 10000))
                            {
                                Drones.DronesShouldBePulled = true;
                                Log.WriteLine("14BSSpawn: DronesShouldBePulled [" + Drones.DronesShouldBePulled + "] AccelerationGate [" + AccelerationGateDistanceForLogs + "k] Farthest Drone [" + FarthestDroneDistance + "] ");
                            }
                            else if (Drones.DronesShouldBePulled && AccelerationGateDistance > 16000 && Combat.Combat.PotentialCombatTargets.Any(i => i.IsWebbingMe))
                            {
                                Log.WriteLine("14BSSpawn: DronesShouldBePulled [" + Drones.DronesShouldBePulled + "] AccelerationGate [" + AccelerationGateDistanceForLogs + "k] ");
                                Drones.DronesShouldBePulled = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    if (closest.Distance > (double)Distances.GateActivationRange)
                    {
                        if (Combat.Combat.PotentialCombatTargets.Any())
                        {
                            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (closest.Distance > (double)Distances.GateActivationRange)");
                            closest.Orbit(500);
                        }
                        else
                        {
                            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (closest.Distance > (double)Distances.GateActivationRange)");
                            closest.Approach();
                        }
                    }

                    if ((double)Distances.GateActivationRange + 4000 > closest.Distance)
                    {
                        Log.WriteLine("ActivateAction: [" + closest.Name + "]: We are within [" + ((int)Distances.GateActivationRange + 4000) + "] of [" + closest.Name + "]@[" + closest.Nearest1KDistance + "]");

                        try
                        {
                            if (!ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds > 10)
                            {
                                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (!AbyssalDeadspaceBehavior.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && ESCache.Instance.DirectEve.Me.AbyssalContentExpirationRemainingSeconds > 10)");
                                if (!Combat.Combat.BoolReloadWeaponsAsap)
                                {
                                    Log.WriteLine("ActivateAction: BoolReloadWeaponsAsap = true");
                                    Combat.Combat.BoolReloadWeaponsAsap = true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        // We cant activate if we have drones out
                        if (Drones.ActiveDroneCount > 0)
                        {
                            //
                            // Tell the drones module to retract drones
                            Drones.DronesShouldBePulled = true;
                            Log.WriteLine("ActivateAction: Waiting for drones to return to the drone bay: Farthest Drone [" + Math.Round(Drones.ActiveDrones.OrderByDescending(i => i.Distance).FirstOrDefault().Distance / 1000, 0) + "k away][" + Math.Round(Drones.ActiveDrones.OrderByDescending(i => i.Velocity).FirstOrDefault().Distance, 0) + " m/s]");
                            if (Drones.WaitForDronesToReturn)
                            {
                                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (WaitForDronesToReturn)");
                                return;
                            }
                        }

                        if (!HealthCheck())
                        {
                            NavigateOnGrid.LogMyCurrentHealth("Activate HealthCheck Failed: Waiting");
                            return;
                        }

                        if ((double)Distances.GateActivationRange - ESCache.Instance.MyShipEntity.Velocity > closest.Distance)
                        {
                            Log.WriteLine("Activate: [" + closest.Name + "] at [" + Math.Round(closest.Distance, 0) + "m] ");
                            NavigateOnGrid.LogWhereAmIOnGrid();
                            foreach (var container in ESCache.Instance.Containers)
                            {
                                Log.WriteLine("Wreck [" + container.Name + "] TypeName [" + container.TypeName + "] IsWreckEmpty [" + container.IsWreckEmpty + "] Distance from Gate [" + Math.Round((double)container._directEntity.DirectAbsolutePosition.GetDistance(ESCache.Instance.AccelerationGates.FirstOrDefault()._directEntity.DirectAbsolutePosition) / 1000, 0) + "k] IsTractorActive [" + container.IsTractorActive + "]");
                            }

                            //Make note of the ID of the gate and verify this gate is not on grid with us after we activate BEFORE we goto nextAction

                            if (closest.ActivateAccelerationGate())
                            {
                                return;
                            }

                            Log.WriteLine("Activate: [" + closest.Name + "] at [" + Math.Round(closest.Distance, 0) + "m] failed: retrying");
                            return;
                        }

                        return;
                    }

                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("We are [" + Math.Round(closest.Distance / 1000, 0) + "k] from the gate");
                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool HealthCheckNeeded
        {
            get
            {
                //abyssal timer uncomfortably low
                if (10 > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                    return false;

                //exiting abyssal deadspace
                if (ESCache.Instance.AccelerationGates.Count > 0 && ESCache.Instance.AccelerationGates.FirstOrDefault().Name.Contains("Origin Conduit"))
                    return false;

                if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs)
                    return false;

                return true;
            }
        }

        public static bool HealthCheck()
        {
            if (ESCache.Instance.ActiveShip != null && HealthCheckNeeded)
            {
                if (ESCache.Instance.ActiveShip.IsShieldTanked && AbyssalDeadspaceBehavior.HealthCheckMinimumShieldPercentage > ESCache.Instance.ActiveShip.ShieldPercentage)
                {
                    Log.WriteLine("HealthCheck Failed: Waiting: HealthCheckMinimumShieldPercentage [" + AbyssalDeadspaceBehavior.HealthCheckMinimumShieldPercentage + "] > ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "]");
                    return false;
                }

                if (ESCache.Instance.ActiveShip.IsArmorTanked && AbyssalDeadspaceBehavior.HealthCheckMinimumArmorPercentage > ESCache.Instance.ActiveShip.ArmorPercentage)
                {
                    Log.WriteLine("HealthCheck Failed: Waiting: HealthCheckMinimumShieldPercentage [" + AbyssalDeadspaceBehavior.HealthCheckMinimumArmorPercentage + "] > ArmorPercentage [" + ESCache.Instance.ActiveShip.ArmorPercentage + "]");
                    return false;
                }

                if (ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.HealthCheckMinimumCapacitorPercentage > ESCache.Instance.ActiveShip.CapacitorPercentage)
                {
                    Log.WriteLine("HealthCheck Failed: Waiting: HealthCheckMinimumCapacitorPercentage [" + AbyssalDeadspaceBehavior.HealthCheckMinimumCapacitorPercentage + "] > CapacitorPercentage [" + ESCache.Instance.ActiveShip.CapacitorPercentage + "]");
                    return false;
                }
            }

            return true;
        }
        #endregion Methods
    }
}