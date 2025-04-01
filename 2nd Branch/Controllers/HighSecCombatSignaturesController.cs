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
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Questor.Behaviors;

namespace EVESharpCore.Controllers
{
    public class HighSecCombatSignaturesController : BaseController
    {
        #region Constructors

        public HighSecCombatSignaturesController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            //State.CurrentAnomalyState = AnomalyState.Default;
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            Time.Instance.Started_DateTime = DateTime.UtcNow;
        }

        #endregion Constructors

        #region Fields

        private static int _cachedFleetMemberCount;

        private static int _fleetMemberCount;

        private static List<DirectFleetMember> _fleetMembers = new List<DirectFleetMember>();

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
                    return null;
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
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.AggressingTargetId), activeModule.TargetId);
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.AggressingTargetDate), DateTime.UtcNow);
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
                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LeaderIsAggressingTargetId), 0);
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }
        }

        public static void ProcessState()
        {
            HighSecCombatSignaturesBehavior.ProcessState();
        }

        public override void DoWork()
        {
            try
            {
                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    return;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                    return;

                if (!Settings.Instance.DefaultSettingsLoaded)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("HighSecAnomalyController: Loading Settings");
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
                                    if (DebugConfig.DebugInteractWithEve) Log("HighSecAnomalyController: RunOnce: OpenCargoHold: !OkToInteractWithEveNow");
                                    return;
                                }

                                if (DebugConfig.DebugCombatMissionsBehavior) Log("HighSecAnomalyController: RunOnce");
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

                //if (DebugConfig.DebugCombatMissionsBehavior) Log("AnomalyController: CurrentAnomalyState [" + State.CurrentAnomalyState + "]");

                ProcessState();
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

        private static void ManageFleet()
        {
            try
            {
                if (Settings.Instance.UseFleetManager)
                {
                    //int slaveCharacterIdToInvite = 0;
                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacterName1))
                    {
                        //slaveCharacterIdToInvite = int.Parse(ESCache.Instance.EveAccount.SlaveCharacter1Rep);
                        //SendFleetInvite(slaveCharacterIdToInvite);
                    }

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacterName2))
                    {
                        //slaveCharacterIdToInvite = int.Parse(ESCache.Instance.EveAccount.SlaveCharacter2RepairGroup);
                        //SendFleetInvite(slaveCharacterIdToInvite);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }
        }

        private static void SendFleetInvite(int charId)
        {
            try
            {
                if (FleetMembers != null && FleetMembers.All(i => i.CharacterId != charId))
                {
                    if (Time.Instance.LastFleetInvite.ContainsKey(charId))
                    {
                        if (DateTime.UtcNow < Time.Instance.LastFleetInvite[charId].AddSeconds(ESCache.Instance.RandomNumber(1, 3)))
                        {
                            Log("Fleet Invite for [" + charId + "] has been sent less than 4 minutes, skipping");
                            return;
                        }

                        if (DateTime.UtcNow < Time.Instance.LastFleetInvite[charId].AddMinutes(ESCache.Instance.RandomNumber(2, 4)))
                            return;
                    }

                    if (Time.Instance.LastFleetMemberTimeStamp.ContainsKey(charId))
                    {
                        if (DateTime.UtcNow < Time.Instance.LastFleetMemberTimeStamp[charId].AddSeconds(3))
                        {
                            Log("CharacterId [" + charId + "] was just in the fleet less than 60 sec ago, waiting before reinviting");
                            return;
                        }

                        if (DateTime.UtcNow < Time.Instance.LastFleetMemberTimeStamp[charId].AddSeconds(ESCache.Instance.RandomNumber(50, 60)))
                        {
                            Log("CharacterId [" + charId + "] was just in the fleet less than 60 sec ago, waiting before reinviting");
                            return;
                        }
                    }

                    /**
                    if (ESCache.Instance.DirectEve.InviteToFleet(charId))
                    {
                        Log("Hydra: FleetInvite Sent to charId [" + charId + "]");
                        Time.Instance.LastFleetInvite.AddOrUpdate(charId, DateTime.UtcNow);
                    }
                    **/
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