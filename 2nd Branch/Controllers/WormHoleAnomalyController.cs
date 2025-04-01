/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 18:07
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Controllers
{
    public class WormHoleAnomalyController : BaseController
    {
        #region Constructors

        public WormHoleAnomalyController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            State.CurrentWormHoleAnomalyState = WormHoleAnomalyState.Default;
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            Time.Instance.Started_DateTime = DateTime.UtcNow;
        }

        #endregion Constructors

        #region Fields

        private static int _cachedFleetMemberCount;

        private static int _fleetMemberCount;

        private static List<DirectFleetMember> _fleetMembers = new List<DirectFleetMember>();

        //private static DateTime _nextCombatMissionCtrlAction = DateTime.UtcNow;

        #endregion Fields

        #region Properties

        public static List<DirectFleetMember> FleetMembers
        {
            get
            {
                try
                {
                    if (_fleetMembers == null)
                    {
                        _fleetMembers = ESCache.Instance.DirectEve.GetFleetMembers;
                        _fleetMemberCount = _fleetMembers.Count;
                        if (_fleetMemberCount > _cachedFleetMemberCount)
                            _cachedFleetMemberCount = _fleetMemberCount;

                        foreach (DirectFleetMember fleetmember in _fleetMembers)
                            Time.Instance.LastFleetMemberTimeStamp.AddOrUpdate(fleetmember.CharacterId, DateTime.UtcNow);
                    }

                    return _fleetMembers;
                }
                catch (Exception ex)
                {
                    Log("Exception: " + ex);
                    return new List<DirectFleetMember>();
                }
            }
        }

        private bool RunOnceAfterStartupalreadyProcessed { get; set; }

        #endregion Properties

        #region Methods

        public static void InvalidateCache()
        {
            _fleetMembers = null;
        }

        public static void PushAggroEntityInfo()
        {
            try
            {
                if (ESCache.Instance.Modules.Any(i => i.IsActive))
                {
                    foreach (ModuleCache activeModule in ESCache.Instance.Modules.OrderByDescending(o => o.IsHighSlotModule).Where(i => i.IsActive && i.GroupId != (int)Group.TractorBeam && i.GroupId != (int)Group.Salvager))
                    {
                        if (ESCache.Instance.EntitiesOnGrid.OrderBy(o => o.Nearest5kDistance).Any(i => i.Id == activeModule.TargetId))
                        {
                            EntityCache entityIAmShooting = ESCache.Instance.EntitiesOnGrid.OrderBy(o => o.Nearest5kDistance).FirstOrDefault(i => i.Id == activeModule.TargetId);
                            if (entityIAmShooting != null)
                            {
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AggressingTargetId), activeModule.TargetId);
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AggressingTargetDate), DateTime.UtcNow);
                                Log("HydraController: Hydra Target is [" + entityIAmShooting.Name + "] at [" + Math.Round(entityIAmShooting.Distance / 1000, 0) + "k] GroupId [" + entityIAmShooting.GroupId + "] TypeId [" + entityIAmShooting.TypeId + "]");
                            }
                        }

                        break;
                    }

                    return;
                }

                //
                // Clear Targets so that slaves do not keep shooting if the master stops!
                //
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderIsAggressingTargetId), 0);
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }
        }

        public static void PushNavigateOnGridInfo()
        {
            try
            {
                if (ESCache.Instance.MyShipEntity.Mode == 0 || ESCache.Instance.MyShipEntity.Mode != ESCache.Instance.EveAccount.MyEntityMode)
                {
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MyEntityMode), ESCache.Instance.MyShipEntity.Mode);
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.GroupId == (int)Group.Stargate))
                    {
                        EntityCache ongridStargate = ESCache.Instance.EntitiesOnGrid.OrderBy(o => o.Distance).FirstOrDefault(i => i.GroupId == (int)Group.Stargate);
                        const bool boolOnGridStargate = true;
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithStargate), boolOnGridStargate);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithEntityIdStargate), ongridStargate.Id);
                    }

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.GroupId == (int)Group.AsteroidBelt))
                    {
                        EntityCache ongridAsteroidBelt = ESCache.Instance.EntitiesOnGrid.OrderBy(o => o.Distance).FirstOrDefault(i => i.GroupId == (int)Group.AsteroidBelt);
                        const bool boolOnGridAsteroidBelt = true;
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithAsteroidBelt), boolOnGridAsteroidBelt);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithEntityIdAsteroidBelt), ongridAsteroidBelt.Id);
                    }

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsStation|| i.IsCitadel))
                    {
                        EntityCache ongridStation = ESCache.Instance.EntitiesOnGrid.OrderBy(o => o.Distance).FirstOrDefault(i => i.IsStation || i.IsCitadel);
                        const bool boolOnGridStation = true;
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithStation), boolOnGridStation);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithEntityIdStation), ongridStation.Id);
                    }

                    try
                    {
                        //Log("MyShipEntity.Mode [" + ESCache.Instance.MyShipEntity.Mode + "]");
                        //Log("FollowId [" + ESCache.Instance.MyShipEntity.FollowId + "]");
                    }
                    catch (Exception ex)
                    {
                        Log("Exception [" + ex + "]");
                    }

                    switch (ESCache.Instance.MyShipEntity.Mode)
                    {
                        case 0:
                            {
                                Log("PushNavigateOnGridInfo: Mode 0");
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastRequestRegroup), DateTime.UtcNow);
                                break;
                            }

                        case 1:
                            {
                                Log("PushNavigateOnGridInfo: Mode 1: Approaching");
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastApproach), DateTime.UtcNow);
                                if (ESCache.Instance.MyShipEntity.FollowId != 0)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdApproach), ESCache.Instance.MyShipEntity.FollowId);

                                break;
                            }

                        case 2:
                            {
                                Log("PushNavigateOnGridInfo: Mode 2: Stopped");
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastStoppedShip), DateTime.UtcNow);
                                break;
                            }

                        case 3:
                            {
                                Log("PushNavigateOnGridInfo: Mode 3: Warping");
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderWarpDriveActive), true);
                                break;
                            }

                        case 4:
                            {
                                Log("PushNavigateOnGridInfo: Mode 4: Orbiting");
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastOrbit), DateTime.UtcNow);
                                if (ESCache.Instance.MyShipEntity.FollowId != 0)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdOrbit), ESCache.Instance.MyShipEntity.FollowId);

                                break;
                            }

                        case 5:
                            {
                                Log("PushNavigateOnGridInfo: Mode 5: entityDeparting2");
                                break;
                            }

                        case 6:
                            {
                                Log("PushNavigateOnGridInfo: Mode 6: entityPursuit");
                                break;
                            }

                        case 7:
                            {
                                Log("PushNavigateOnGridInfo: Mode 7: entityFleeing");
                                break;
                            }

                        case 8:
                            {
                                Log("PushNavigateOnGridInfo: Mode 8: unknown / unused!");
                                break;
                            }

                        case 9:
                            {
                                Log("PushNavigateOnGridInfo: Mode 9: entityOperating");
                                break;
                            }

                        case 10:
                            {
                                Log("PushNavigateOnGridInfo: Mode 10: entityEngage");
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public static void TravelToLeader()
        {
            if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName) &&
                ESCache.Instance.EveAccount.LeaderIsInSystemId == 0 &&
                ESCache.Instance.EveAccount.LeaderEntityId == 0)
                return;

            if (!ESCache.Instance.EveAccount.LeaderInStation &&
                !ESCache.Instance.EveAccount.LeaderInSpace)
                return;

            //
            // Leader is in system with me!
            //
            if (ESCache.Instance.EveAccount.LeaderIsInSystemId == ESCache.Instance.DirectEve.Session.SolarSystemId)
            {
                //
                // Leader is Docked
                //
                if (ESCache.Instance.EveAccount.LeaderInStation)
                {
                    //
                    // I am docked
                    //
                    if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
                    {
                        //
                        // We are docked in the same station.
                        //
                        if (ESCache.Instance.EveAccount.LeaderInStationId == ESCache.Instance.DirectEve.Session.StationId ||
                            ESCache.Instance.EveAccount.LeaderInStationId == ESCache.Instance.DirectEve.Session.Structureid)
                            return;

                        Traveler.TravelToStationId(ESCache.Instance.EveAccount.LeaderInStationId);
                        return;
                    }

                    //
                    // I am undocked
                    //
                    Traveler.TravelToStationId(ESCache.Instance.EveAccount.LeaderInStationId);
                    return;
                }

                //
                // Leader is in space!
                //
                //
                // I am Docked.
                //
                if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
                {
                    TravelerDestination.Undock();
                    return;
                }

                //
                // I am in space!
                //
                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    //
                    // Leader is on grid with me!
                    //
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderIsInSystemId))
                        if (NavigateOnGrid.SpeedTank)
                            ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderEntityId).Orbit(NavigateOnGrid.OrbitDistance);

                    if (ESCache.Instance.AccelerationGates.Count > 0)
                    {
                        //
                        // fix me
                        //

                        Log("HydraController: fix me: Use of Acceleration gates currently not working.");

                        //var activateAction = new Action { State = ActionState.Activate };
                        //ActivateAction(activateAction);

                        return;
                    }

                    if (FleetMembers != null && FleetMembers.Count > 0)
                    {
                        if (FleetMembers.Any(i => i.CharacterId == int.Parse(ESCache.Instance.EveAccount.LeaderCharacterId)))
                        {
                            if (DateTime.UtcNow > Time.Instance.LastInitiatedWarp.AddSeconds(45))
                            {
                                if (!ESCache.Instance.OkToInteractWithEveNow)
                                {
                                    if (DebugConfig.DebugInteractWithEve) Log("HydraController: !OkToInteractWithEveNow");
                                    return;
                                }

                                FleetMembers.Find(i => i.CharacterId == int.Parse(ESCache.Instance.EveAccount.LeaderCharacterId)).WarpToMember();
                                ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                                Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                                return;
                            }

                            return;
                        }

                        //
                        // you are in fleet but the leader is not.
                        //
                        return;
                    }
                    //
                    // Leader is not yet on grid with me, I need to warp to a bookmark, celestial or to a fleet member!
                    //
                    if (ESCache.Instance.EveAccount.LeaderOnGridWithStargate && ESCache.Instance.Entities.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderOnGridWithEntityIdStargate))
                    {
                        EntityCache leaderIsNearThisStargate = ESCache.Instance.Entities.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderOnGridWithEntityIdStargate);
                        if (leaderIsNearThisStargate != null && leaderIsNearThisStargate.Distance > (int)Distances.WarptoDistance)
                        {
                            leaderIsNearThisStargate.WarpTo();
                            return;
                        }
                    }

                    if (ESCache.Instance.EveAccount.LeaderOnGridWithAsteroidBelt && ESCache.Instance.Entities.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderOnGridWithEntityIdAsteroidBelt))
                    {
                        EntityCache leaderIsNearThisAsteroidBelt = ESCache.Instance.Entities.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderOnGridWithEntityIdAsteroidBelt);
                        if (leaderIsNearThisAsteroidBelt != null && leaderIsNearThisAsteroidBelt.Distance > (int)Distances.WarptoDistance)
                        {
                            leaderIsNearThisAsteroidBelt.WarpTo();
                            return;
                        }
                    }

                    if (ESCache.Instance.EveAccount.LeaderOnGridWithStation && ESCache.Instance.Entities.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderOnGridWithEntityIdStation))
                    {
                        EntityCache leaderIsNearThisStation = ESCache.Instance.Entities.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderOnGridWithEntityIdStation);
                        if (leaderIsNearThisStation != null && leaderIsNearThisStation.Distance > (int)Distances.WarptoDistance)
                        {
                            leaderIsNearThisStation.WarpTo();
                            return;
                        }
                    }
                }
            }

            //
            // Leader is not in system with me. Travel to leader.
            //
            if (ESCache.Instance.EveAccount.LeaderTravelerDestinationSystemId != 0)
                Traveler.TravelToSystemId(ESCache.Instance.EveAccount.LeaderTravelerDestinationSystemId);
            else if (ESCache.Instance.EveAccount.LeaderIsInSystemId != 0)
                Traveler.TravelToSystemId(ESCache.Instance.EveAccount.LeaderIsInSystemId);
        }

        public override void DoWork()
        {
            try
            {
                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    return;

                if (ESCache.Instance.ActiveShip.TypeId != ESCache.Instance.MyShipEntity.TypeId)
                    return;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                    return;

                if (!Settings.Instance.DefaultSettingsLoaded)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: Loading Settings");
                    Settings.Instance.LoadSettings_Initialize();
                }

                if (!RunOnceAfterStartupalreadyProcessed &&
                    ESCache.Instance.DirectEve.Session.CharacterId != null && ESCache.Instance.DirectEve.Session.CharacterId > 0)
                    if (Settings.CharacterXmlExists)
                    {
                        if (DateTime.UtcNow > Time.Instance.NextStartupAction)
                        {
                            try
                            {
                                if (!ESCache.Instance.OkToInteractWithEveNow)
                                {
                                    if (DebugConfig.DebugInteractWithEve) Log("Questor: RunOnce: OpenCargoHold: !OkToInteractWithEveNow");
                                    return;
                                }

                                if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: RunOnce");
                                ESCache.Instance.IterateShipTargetValues();
                                ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }

                            RunOnceAfterStartupalreadyProcessed = true;
                        }
                    }
                    else
                    {
                        Log("Settings.Instance.CharacterName is still null");
                        Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
                        RunOnceAfterStartupalreadyProcessed = false;
                        return;
                    }

                if (DateTime.UtcNow.Subtract(Time.Instance.LastUpdateOfSessionRunningTime).TotalSeconds <
                    Time.Instance.SessionRunningTimeUpdate_seconds)
                {
                    Statistics.SessionRunningTime =
                        (int)DateTime.UtcNow.Subtract(Time.Instance.Started_DateTime).TotalMinutes;
                    Time.Instance.LastUpdateOfSessionRunningTime = DateTime.UtcNow;
                }

                //if (ESCache.Instance.InWarp)
                //{
                //    if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: if (ESCache.Instance.InWarp)");
                //    return;
                //}

                if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: CurrentHydraState [" + State.CurrentHydraState + "]");

                switch (State.CurrentWormHoleAnomalyState)
                {
                    case WormHoleAnomalyState.Default:
                        {
                            try
                            {
                                Log("WormHoleAnomalyController: IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "] InsidePosForceField [" + ESCache.Instance.InsidePosForceField + "] InSpace [ " + ESCache.Instance.InSpace + "] InStation [" + ESCache.Instance.InStation + "]");
                                State.CurrentWormHoleAnomalyState = WormHoleAnomalyState.Idle;
                                Log("WormHoleAnomalyController: CurrentHydraState [" + State.CurrentHydraState + "]");
                                break;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception: " + ex);
                                break;
                            }
                        }

                    case WormHoleAnomalyState.Idle:
                        {
                            try
                            {
                                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                                    if (!ESCache.Instance.InsidePosForceField)
                                    {
                                        if (ESCache.Instance.EveAccount.IsLeader)
                                        {
                                            State.CurrentWormHoleAnomalyState = WormHoleAnomalyState.Leader;
                                            Log("WormHoleAnomalyController: CurrentWormHoleAnomalyState [" + State.CurrentWormHoleAnomalyState + "]");
                                            break;
                                        }

                                        State.CurrentWormHoleAnomalyState = WormHoleAnomalyState.Combat;
                                        Log("WormHoleAnomalyController: CurrentWormHoleAnomalyState [" + State.CurrentWormHoleAnomalyState + "]");
                                    }

                                break;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception: " + ex);
                                break;
                            }
                        }

                    case WormHoleAnomalyState.Leader:
                        {
                            try
                            {
                                if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
                                    return;
                                //
                                // follow weapons
                                //
                                PushAggroEntityInfo();
                                PushNavigateOnGridInfo();
                                //ManageFleet();
                                break;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception: " + ex);
                                break;
                            }
                        }

                    case WormHoleAnomalyState.Combat:
                        {
                            try
                            {
                                //
                                // targeting and combat are handled by CombatController and TargetingController
                                // we expect currently to be put on the correct grid, we dont travel!

                                //
                                // If there are no stargates, like in w-space, we cant easily navigate system to system: so dont try to...
                                //

                                break;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception: " + ex);
                                break;
                            }
                        }

                    case WormHoleAnomalyState.Error:
                        {
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "Questor Error."));
                            string msg = string.Format("Set [{0}] disabled.", ESCache.Instance.EveAccount.MaskedCharacterName);
                            WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.UseScheduler), false);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}