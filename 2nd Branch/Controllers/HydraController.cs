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
using EVESharpCore.Questor.Combat;
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

//using Action = EVESharpCore.Questor.Actions.Action;

namespace EVESharpCore.Controllers
{
    public class HydraController : BaseController
    {
        #region Constructors

        public HydraController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            State.CurrentHydraState = HydraState.Default;
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            Time.Instance.Started_DateTime = DateTime.UtcNow;
        }

        #endregion Constructors

        #region Fields

        private static DateTime _nextCombatMissionCtrlAction = DateTime.UtcNow;

        #endregion Fields

        #region Properties

        private bool RunOnceAfterStartupalreadyProcessed { get; set; }

        #endregion Properties

        #region Methods

        public static void InvalidateCache()
        {
            activeModuleTargetId = 0;
            LastActiveModuleTargetIdUpdate = DateTime.UtcNow.AddDays(-1);
        }

        private static long activeModuleTargetId = 0;
        private static DateTime LastActiveModuleTargetIdUpdate = DateTime.UtcNow.AddDays(-1);

        public static void PushAggroEntityInfo()
        {
            try
            {
                if (activeModuleTargetId != 0)
                    return;

                if (ESCache.Instance.Modules.Any(i => i.IsActive))
                {
                    foreach (ModuleCache activeModule in ESCache.Instance.Modules.Where(i => (i.IsTurret || i.IsEnergyWeapon || i.IsMissileLauncher || i._module.IsVortonProjector) && i.IsActive && i.GroupId != (int)Group.TractorBeam && i.GroupId != (int)Group.Salvager))
                    {
                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == activeModule.TargetId))
                        {
                            EntityCache entityIAmShooting = ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == activeModule.TargetId);
                            if (entityIAmShooting != null)
                            {
                                activeModuleTargetId = activeModule.TargetId;
                                LastActiveModuleTargetIdUpdate = DateTime.UtcNow;
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AggressingTargetId), activeModuleTargetId);
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AggressingTargetDate), LastActiveModuleTargetIdUpdate);
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
                if (ESCache.Instance.EveAccount.LeaderIsAggressingTargetId != 0)
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
                    if (ESCache.Instance.EveAccount.MyEntityMode != ESCache.Instance.MyShipEntity.Mode)
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MyEntityMode), ESCache.Instance.MyShipEntity.Mode);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.GroupId == (int)Group.Stargate))
                    {
                        EntityCache ongridStargate = ESCache.Instance.EntitiesOnGrid.OrderBy(o => o.Distance).FirstOrDefault(i => i.GroupId == (int)Group.Stargate);
                        const bool boolOnGridStargate = true;
                        if (ESCache.Instance.EveAccount.LeaderOnGridWithStargate != boolOnGridStargate)
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithStargate), boolOnGridStargate);
                        if (ESCache.Instance.EveAccount.LeaderOnGridWithEntityIdStargate != ongridStargate.Id)
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithEntityIdStargate), ongridStargate.Id);
                    }

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.GroupId == (int)Group.AsteroidBelt))
                    {
                        EntityCache ongridAsteroidBelt = ESCache.Instance.EntitiesOnGrid.OrderBy(o => o.Distance).FirstOrDefault(i => i.GroupId == (int)Group.AsteroidBelt);
                        const bool boolOnGridAsteroidBelt = true;
                        if (ESCache.Instance.EveAccount.LeaderOnGridWithAsteroidBelt != boolOnGridAsteroidBelt)
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithAsteroidBelt), boolOnGridAsteroidBelt);
                        if (ESCache.Instance.EveAccount.LeaderOnGridWithEntityIdAsteroidBelt != ongridAsteroidBelt.Id)
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithEntityIdAsteroidBelt), ongridAsteroidBelt.Id);
                    }

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsStation|| i.IsCitadel))
                    {
                        EntityCache ongridStation = ESCache.Instance.EntitiesOnGrid.OrderBy(o => o.Distance).FirstOrDefault(i => i.IsStation || i.IsCitadel);
                        const bool boolOnGridStation = true;
                        if (ESCache.Instance.EveAccount.LeaderOnGridWithStation != boolOnGridStation)
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderOnGridWithStation), boolOnGridStation);
                        if (ESCache.Instance.EveAccount.LeaderOnGridWithEntityIdStation != ongridStation.Id)
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
                                if (ESCache.Instance.EveAccount.LeaderLastRequestRegroup != DateTime.UtcNow)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastRequestRegroup), DateTime.UtcNow);
                                break;
                            }

                        case 1:
                            {
                                Log("PushNavigateOnGridInfo: Mode 1: Approaching");
                                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastApproach.AddSeconds(2))
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastApproach), DateTime.UtcNow);
                                if (ESCache.Instance.MyShipEntity.FollowId != 0 && ESCache.Instance.EveAccount.LeaderLastEntityIdApproach != ESCache.Instance.MyShipEntity.FollowId)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdApproach), ESCache.Instance.MyShipEntity.FollowId);

                                break;
                            }

                        case 2:
                            {
                                Log("PushNavigateOnGridInfo: Mode 2: Stopped");
                                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastStoppedShip.AddSeconds(5))
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastStoppedShip), DateTime.UtcNow);
                                break;
                            }

                        case 3:
                            {
                                Log("PushNavigateOnGridInfo: Mode 3: Warping");
                                if (!ESCache.Instance.EveAccount.LeaderWarpDriveActive)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderWarpDriveActive), true);
                                break;
                            }

                        case 4:
                            {
                                Log("PushNavigateOnGridInfo: Mode 4: Orbiting");
                                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastOrbit.AddSeconds(3))
                                {
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastOrbit), DateTime.UtcNow);
                                }

                                if (ESCache.Instance.MyShipEntity.FollowId != 0 && ESCache.Instance.EveAccount.LeaderLastEntityIdOrbit != ESCache.Instance.MyShipEntity.FollowId)
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

                    /**
                    if (FleetMembers != null && FleetMembers.Any())
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

                                FleetMembers.FirstOrDefault(i => i.CharacterId == int.Parse(ESCache.Instance.EveAccount.LeaderCharacterId)).WarpToMember();
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
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
                    **/

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
                                    if (DebugConfig.DebugInteractWithEve) Log("HydraController: RunOnce: OpenCargoHold: !OkToInteractWithEveNow");
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

                if (ESCache.Instance.InWarp)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: if (ESCache.Instance.InWarp)");
                    return;
                }

                if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: CurrentHydraState [" + State.CurrentHydraState + "]");

                switch (State.CurrentHydraState)
                {
                    case HydraState.Default:
                        {
                            try
                            {
                                Log("HydraController: IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "] InsidePosForceField [" + ESCache.Instance.InsidePosForceField + "] InSpace [ " + ESCache.Instance.InSpace + "] InStation [" + ESCache.Instance.InStation + "]");
                                State.CurrentHydraState = HydraState.Idle;
                                Log("HydraController: CurrentHydraState [" + State.CurrentHydraState + "]");
                                break;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception: " + ex);
                                break;
                            }
                        }

                    case HydraState.Idle:
                        {
                            try
                            {
                                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                                    if (!ESCache.Instance.InsidePosForceField)
                                    {
                                        if (ESCache.Instance.EveAccount.IsLeader)
                                        {
                                            State.CurrentHydraState = HydraState.Leader;
                                            Log("HydraController: CurrentHydraState [" + State.CurrentHydraState + "]");
                                            break;
                                        }

                                        State.CurrentHydraState = HydraState.Combat;
                                        Log("HydraController: CurrentHydraState [" + State.CurrentHydraState + "]");
                                    }

                                break;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception: " + ex);
                                break;
                            }
                        }

                    case HydraState.Leader:
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

                    case HydraState.Combat:
                        {
                            try
                            {
                                //
                                // targeting and combat are handled by CombatController and TargetingController
                                //

                                if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName) &&
                                    ESCache.Instance.EveAccount.LeaderIsInSystemId == 0 &&
                                    ESCache.Instance.EveAccount.LeaderEntityId == 0)
                                    return;

                                if (!ESCache.Instance.EveAccount.LeaderInStation &&
                                    !ESCache.Instance.EveAccount.LeaderInSpace)
                                    return;

                                //
                                // If there are no stargates, like in w-space, we cant easily navigate system to system: so dont try to...
                                //
                                if (ESCache.Instance.Stargates.Count > 0)
                                    if ((ESCache.Instance.InStation && !ESCache.Instance.InSpace) ||
                                        (ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.All(i => i.Id != ESCache.Instance.EveAccount.LeaderEntityId)))
                                    {
                                        TravelToLeader();
                                        return;
                                    }

                                if (ESCache.Instance.InsidePosForceField)
                                {
                                    State.CurrentHydraState = HydraState.Idle;
                                    Log("HydraController: We are in a POS ForceField. CurrentHydraState [" + State.CurrentHydraState + "]");
                                    break;
                                }

                                if (ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderEntityId))
                                {
                                    if (NavigateOnGrid.SpeedTank)
                                    {
                                        ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderEntityId).Orbit(NavigateOnGrid.OrbitDistance);
                                        return;
                                    }

                                    if (ESCache.Instance.EveAccount.LeaderLastDock.AddSeconds(5) > DateTime.UtcNow)
                                    {
                                        if (ESCache.Instance.DockableLocations.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdDock))
                                        {
                                            ESCache.Instance.DockableLocations.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdDock).Dock();
                                            return;
                                        }

                                        return;
                                    }

                                    if (ESCache.Instance.EveAccount.LeaderLastJump.AddSeconds(5) > DateTime.UtcNow)
                                    {
                                        if (ESCache.Instance.Stargates.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdJump))
                                        {
                                            ESCache.Instance.Stargates.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdJump).Jump();
                                            return;
                                        }

                                        return;
                                    }

                                    if (ESCache.Instance.EveAccount.LeaderLastRequestRegroup.AddSeconds(5) > DateTime.UtcNow)
                                    {
                                        ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderEntityId).KeepAtRange(5000);
                                        return;
                                    }

                                    if (ESCache.Instance.EveAccount.LeaderLastStoppedShip.AddSeconds(5) > DateTime.UtcNow)
                                    {
                                        NavigateOnGrid.StopMyShip("Hydra: LeaderLastStoppedShip within 5 sec of now");
                                        return;
                                    }

                                    if (ESCache.Instance.EveAccount.LeaderInWarp && ESCache.Instance.EveAccount.LeaderWarpDriveActive && !ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                                        if (ESCache.Instance.Entities.Any(i => i.Distance > (int)Distances.WarptoDistance && i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdWarp))
                                        {
                                            EntityCache LeaderWarpedToHere = ESCache.Instance.Entities.Find(i => i.Distance > (int)Distances.WarptoDistance && i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdWarp);
                                            Log("Hydra: LeaderWarpedToHere [" + LeaderWarpedToHere.Name + "][" + Math.Round(LeaderWarpedToHere.Distance / (double)Distances.OneAu, 2) + "] AU away. Following");
                                            LeaderWarpedToHere.WarpTo();
                                            return;
                                        }

                                    if (ESCache.Instance.EveAccount.LeaderLastActivate.AddSeconds(5) > DateTime.UtcNow)
                                        if (ESCache.Instance.AccelerationGates.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdActivate))
                                        {
                                            ESCache.Instance.AccelerationGates.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdActivate).ActivateAccelerationGate();
                                            return;
                                        }

                                    if (ESCache.Instance.EveAccount.LeaderLastAlign.AddSeconds(5) > DateTime.UtcNow)
                                        if (ESCache.Instance.Entities.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdAlign && ESCache.Instance.EveAccount.LeaderLastEntityIdApproach != ESCache.Instance.MyShipEntity.Id && i.Distance > (int)Distances.WarptoDistance))
                                        {
                                            ESCache.Instance.Entities.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdAlign).AlignTo();
                                            return;
                                        }
                                        else
                                        {
                                            ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderEntityId).KeepAtRange(500);
                                            return;
                                        }

                                    if (ESCache.Instance.EveAccount.LeaderLastStoppedShip.AddSeconds(5) > DateTime.UtcNow)
                                    {
                                        NavigateOnGrid.StopMyShip("Hydra: LeaderLastStoppedShip within 5 sec of now");
                                        return;
                                    }

                                    if (ESCache.Instance.EveAccount.LeaderLastOrbit.AddSeconds(5) > DateTime.UtcNow)
                                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdOrbit && ESCache.Instance.EveAccount.LeaderLastEntityIdApproach != ESCache.Instance.MyShipEntity.Id))
                                        {
                                            ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdOrbit).Orbit(NavigateOnGrid.OrbitDistanceToUse);
                                            return;
                                        }
                                        else
                                        {
                                            ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderEntityId).KeepAtRange(500);
                                            return;
                                        }

                                    if (ESCache.Instance.EveAccount.LeaderLastApproach.AddSeconds(5) > DateTime.UtcNow)
                                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdApproach && ESCache.Instance.EveAccount.LeaderLastEntityIdApproach != ESCache.Instance.MyShipEntity.Id))
                                        {
                                            ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdApproach).Approach();
                                            return;
                                        }
                                        else
                                        {
                                            ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderEntityId).KeepAtRange(500);
                                            return;
                                        }

                                    if (ESCache.Instance.EveAccount.LeaderLastKeepAtRange.AddSeconds(5) > DateTime.UtcNow)
                                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdKeepAtRange && ESCache.Instance.EveAccount.LeaderLastEntityIdApproach != ESCache.Instance.MyShipEntity.Id))
                                            ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderLastEntityIdKeepAtRange).KeepAtRange(ESCache.Instance.EveAccount.LeaderLastKeepAtRangeDistance);
                                        else
                                            ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderEntityId).KeepAtRange(500);
                                }

                                break;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception: " + ex);
                                break;
                            }
                        }

                    case HydraState.Error:
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

        private static void ActivateAction(Action action)
        {
            try
            {
                if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                    return;

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                    return;

                List<EntityCache> targets = new List<EntityCache>();

                try
                {
                    targets = ESCache.Instance.AccelerationGates;
                }
                catch (Exception ex)
                {
                    Logging.Log.WriteLine("Exception [" + ex + "]");
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

                        if (DebugConfig.DebugActivateGate)
                            Logging.Log.WriteLine("if (Cache.Instance.ActiveDrones.Any())");
                        return;
                    }

                    if (Time.Instance.NextApproachAction < DateTime.UtcNow && closest.Distance < 0) // if distance is below 500, we keep at range 1000
                        NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "ActivateAction", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());

                    // if we reach this point we're between <= 2150m && >=0m to the gate
                    if (DateTime.UtcNow > Time.Instance.NextActivateAccelerationGate)
                        //if (AttemptsToActivateGate < 50)
                        //{
                        //AttemptsToActivateGate++;
                        if (closest.ActivateAccelerationGate())
                        {
                            Logging.Log.WriteLine("Activate: [" + closest.Name + "] Move to next pocket and change state to 'NextPocket'");
                            //AttemptsToActivateGate = 0;
                            // Do not change actions, if NextPocket gets a timeout (>2 mins) then it reverts to the last action
                            //_moveToNextPocket = DateTime.UtcNow;
                            State.CurrentCombatMissionCtrlState = ActionControlState.NextPocket;
                        }
                        else
                        {
                            Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(6);
                        }
                    //}
                    //else
                    //{
                    //    _States.CurrentCombatMissionCtrlState = ActionControlState.Error;
                    //    Logging.Log.WriteLine("ERROR: Too many attempts to activate the gate.");
                    //    return;
                    //}

                    return;
                }

                if (closest.Distance < (int)Distances.WarptoDistance) //if we are inside warpto distance then approach
                {
                    if (DebugConfig.DebugActivateGate) Logging.Log.WriteLine("if (closest.Distance < (int)Distances.WarptoDistance)");

                    // Move to the target
                    if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                    {
                        if (closest.IsOrbitedByActiveShip || ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != closest.Id ||
                            (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 50))
                        {
                            closest.Approach();
                            return;
                        }

                        if (DebugConfig.DebugActivateGate)
                            Logging.Log.WriteLine("Cache.Instance.IsOrbiting [" + closest.IsOrbiting + "] Cache.Instance.MyShip.Velocity [" +
                                                  Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "m/s]");
                        if (DebugConfig.DebugActivateGate)
                            if (ESCache.Instance.FollowingEntity != null)
                                Logging.Log.WriteLine("Cache.Instance.Approaching.Id [" + ESCache.Instance.FollowingEntity.Id + "][closest.Id: " + closest.Id + "]");
                        if (DebugConfig.DebugActivateGate) Logging.Log.WriteLine("------------------");
                        return;
                    }

                    if (closest.IsOrbitedByActiveShip || ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != closest.Id)
                    {
                        Logging.Log.WriteLine("Activate: Delaying approach for: [" + Math.Round(Time.Instance.NextApproachAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                                              "] seconds");
                        return;
                    }

                    if (DebugConfig.DebugActivateGate) Logging.Log.WriteLine("------------------");
                    return;
                }

                if (closest.Distance > (int)Distances.WarptoDistance) //we must be outside warpto distance, but we are likely in a DeadSpace so align to the target
                    if (closest.AlignTo())
                        Logging.Log.WriteLine("Activate: AlignTo: [" + closest.Name + "] This only happens if we are asked to Activate something that is outside [" +
                                              Distances.CloseToGateActivationRange + "]");
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void ManageFleet()
        {
            try
            {
                if (Settings.Instance.UseFleetManager)
                {
                    //int slaveCharacterIdToInvite = 0;
                    //if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacterId1))
                    //{
                    //    slaveCharacterIdToInvite = int.Parse(ESCache.Instance.EveAccount.SlaveCharacterId1);
                    //    SendFleetInvite(slaveCharacterIdToInvite);
                    //}

                    //if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacterId2))
                    //{
                    //    slaveCharacterIdToInvite = int.Parse(ESCache.Instance.EveAccount.SlaveCharacterId2);
                    //    SendFleetInvite(slaveCharacterIdToInvite);
                    //}
                }
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}