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
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//using EVESharpCore.Questor.Storylines;

namespace EVESharpCore.Controllers
{
    public class CourierMissionsController : BaseController
    {
        #region Constructors

        public CourierMissionsController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            DependsOn = new List<Type>
            {
                typeof(DefenseController)
            };

            State.CurrentCourierMissionBehaviorState = CourierMissionsBehaviorState.Idle;
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            //State.CurrentQuestorState = QuestorState.Idle;
            Time.Instance.StartTime = DateTime.UtcNow;
            Time.Instance.Started_DateTime = DateTime.UtcNow;

            // add additional controllers
        }

        #endregion Constructors

        #region Properties

        private bool _setCreatePathRan { get; set; }

        private bool RunOnceAfterStartupalreadyProcessed { get; set; }

        #endregion Properties

        #region Methods

        private static DirectAgentMission _nextMissionDestination;

        private readonly int JumpsToLookForOtherDistributionAgents = 1;

        private readonly int MissionLevelToAccept = 4;

        public IEnumerable<DirectAgent> DistributionAgentsInRangeWithNoMissionYet
        {
            get
            {
                try
                {
                    if (ESCache.Instance.DirectEve.ListDirectAgentsTypeDistribution.Any())
                    {
                        if (DebugConfig.DebugCourierMissions) Log("DistributionAgentsInRangeWithNoMissionYet: Total Distribution Agents [" + ESCache.Instance.DirectEve.ListDirectAgentsTypeDistribution.Count + "] in eve");
                        if (ESCache.Instance.DirectEve.ListDirectAgentsTypeDistribution.Any(i => i.Level == MissionLevelToAccept && i.HaveStandingsToAccessToThisAgent && (i.StationId == ESCache.Instance.DirectEve.Session.LocationId || i.SolarSystem.JumpsHighSecOnly < JumpsToLookForOtherDistributionAgents) && !i.IsAgentMissionAccepted))
                        {
                            if (DebugConfig.DebugCourierMissions) Log("DistributionAgentsInRangeWithNoMissionYet: Total Distribution Agents with MissionLevelToAccept: lvl[" + MissionLevelToAccept + "] is [" + ESCache.Instance.DirectEve.ListDirectAgentsTypeDistribution.Count(i => i.Level == MissionLevelToAccept) + "] agents available");
                            if (DebugConfig.DebugCourierMissions) Log("DistributionAgentsInRangeWithNoMissionYet: Total Distribution Agents and I have standings to access [" + ESCache.Instance.DirectEve.ListDirectAgentsTypeDistribution.Count(i => i.Level == MissionLevelToAccept && i.HaveStandingsToAccessToThisAgent) + "] agents available");
                            if (DebugConfig.DebugCourierMissions) Log("DistributionAgentsInRangeWithNoMissionYet: Total Distribution Agents and no current mission accepted yet [" + ESCache.Instance.DirectEve.ListDirectAgentsTypeDistribution.Count(i => i.Level == MissionLevelToAccept && i.HaveStandingsToAccessToThisAgent && !i.IsAgentMissionAccepted) + "] agents available");
                            if (DebugConfig.DebugCourierMissions) Log("DistributionAgentsInRangeWithNoMissionYet: Total Distribution Agents and within range of [" + JumpsToLookForOtherDistributionAgents + "] jumps [" + ESCache.Instance.DirectEve.ListDirectAgentsTypeDistribution.Count(i => i.Level == MissionLevelToAccept && i.HaveStandingsToAccessToThisAgent && !i.IsAgentMissionAccepted && (i.StationId == ESCache.Instance.DirectEve.Session.LocationId || i.SolarSystem.JumpsHighSecOnly < JumpsToLookForOtherDistributionAgents)) + "] agents available");

                            return ESCache.Instance.DirectEve.ListDirectAgentsTypeDistribution.Where(i => i.Level == MissionLevelToAccept && i.HaveStandingsToAccessToThisAgent && (i.StationId == ESCache.Instance.DirectEve.Session.Station.Id || i.SolarSystem.JumpsHighSecOnly < JumpsToLookForOtherDistributionAgents) && !i.IsAgentMissionAccepted).OrderBy(j => j.SolarSystem.JumpsHighSecOnly);
                        }

                        return new List<DirectAgent>();
                    }

                    return new List<DirectAgent>();
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return new List<DirectAgent>();
                }
            }
        }

        public IOrderedEnumerable<DirectAgentMission> MissionsWeHaveAccepted
        {
            get
            {
                try
                {
                    return ESCache.Instance.DirectEve.AgentMissions.Where(i => i.Agent.IsAgentMissionAccepted && (i.Type.Contains("Trade") || i.Type.Contains("Courier"))).OrderBy(mission => mission.Agent.SolarSystem.JumpsHighSecOnly);
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public DirectAgent NextAgentToAttemptToPullMissionFrom
        {
            get
            {
                try
                {
                    if (DistributionAgentsInRangeWithNoMissionYet != null && DistributionAgentsInRangeWithNoMissionYet.Any(i => i.StationId == ESCache.Instance.DirectEve.Session.StationId && !i.IsAgentMissionAccepted && !i.StorylineAgent))
                    {
                        //foreach (DirectAgentMission agentMission in ESCache.Instance.DirectEve.AgentMissions.Where(i => i.Important && i.State == MissionState.Offered))
                        //{
                        //    Log("Storyline Agent [" + agentMission.Agent.Name + "] offering [" + agentMission.Name + "] type [" + agentMission.Type + "] found");
                        //    return agentMission.Agent;
                        //}

                        if (DistributionAgentsInRangeWithNoMissionYet.Any(i => i.StationId == ESCache.Instance.DirectEve.Session.StationId && !i.IsAgentMissionAccepted && !i.StorylineAgent))
                        {
                            //
                            // we have missions already accepted (at least one mission accepted in journal)
                            //
                            if (DistributionAgentsInRangeWithNoMissionYet.Any(i => i.StationId == ESCache.Instance.DirectEve.Session.StationId && !i.IsAgentMissionAccepted && !i.StorylineAgent && MissionsWeHaveAccepted != null && MissionsWeHaveAccepted.Any() && MissionsWeHaveAccepted.All(acceptedMission => acceptedMission.Name != i.Name)))
                                foreach (DirectAgent distributionAgentInStation in DistributionAgentsInRangeWithNoMissionYet.Where(i => i.StationId == ESCache.Instance.DirectEve.Session.StationId && !i.IsAgentMissionAccepted && !i.StorylineAgent && i.Mission != null && !i.Mission.Important && MissionsWeHaveAccepted.All(acceptedMission => acceptedMission.Name != i.Name)))
                                {
                                    Log("DistributionAgentInStation [" + distributionAgentInStation.Name + "] found in station");
                                    return distributionAgentInStation;
                                }

                            //
                            // we have no missions accepted (no missions accepted in journal)
                            //
                            foreach (DirectAgent distributionAgentInStation in DistributionAgentsInRangeWithNoMissionYet.Where(i => i.StationId == ESCache.Instance.DirectEve.Session.StationId && !i.IsAgentMissionAccepted && !i.StorylineAgent))
                            {
                                Log("DistributionAgentInStation [" + distributionAgentInStation.Name + "] found in station");
                                return distributionAgentInStation;
                            }
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public DirectAgentMission NextMissionDestination
        {
            get
            {
                try
                {
                    if (_nextMissionDestination != null)
                        if (!_nextMissionDestination.Agent.IsAgentMissionExists)
                            _nextMissionDestination = null;

                    if (_nextMissionDestination == null)
                    {
                        //if (ShouldPullAnotherDistributionMission == null)
                        //{
                        //    Log("NextDestination: if (ShouldPullAnotherDistributionMission == null)");
                        //    return null;
                        //}

                        //if ((bool)ShouldPullAnotherDistributionMission)
                        //{
                        //
                        // pick an agent
                        //
                        //if (DistributionAgentsInRangeWithNoMissionYet != null && DistributionAgentsInRangeWithNoMissionYet.Any())
                        //{
                        //    return DistributionAgentsInRangeWithNoMissionYet.FirstOrDefault().Name;
                        //}

                        //
                        // no agents to pull more missions from, continue
                        //
                        //}

                        //
                        // pick a mission
                        //
                        int? jumpsToPickup = null;
                        int? jumpsToDeliver = null;
                        int intMissionNum = 0;

                        if (MissionsWeHaveAccepted.Any())
                        {
                            List<DirectAgentMission> missionsRequiringUsToTravel = (List<DirectAgentMission>)MissionsWeHaveAccepted.Where(i => i.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoDropOffLocation || i.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoPickupLocation);
                            if (missionsRequiringUsToTravel.Any())
                            {
                                intMissionNum = 0;
                                foreach (DirectAgentMission missionRequiringUsToTravel in missionsRequiringUsToTravel)
                                {
                                    intMissionNum++;
                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: missionRequiringUsToTravel: [" + intMissionNum + "][" + missionRequiringUsToTravel.Name + "][" + missionRequiringUsToTravel.Agent.Name + "][" + missionRequiringUsToTravel.CurrentCourierMissionCtrlState + "]");
                                }

                                DirectAgentMission closestMissionNeedsPickup = missionsRequiringUsToTravel.Where(mission => mission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoPickupLocation).OrderBy(i => i.CourierMissionPickupBookmark.SolarSystem.JumpsHighSecOnly).FirstOrDefault();
                                if (closestMissionNeedsPickup != null)
                                {
                                    jumpsToPickup = JumpsToCourierMissionPickupBookmark(closestMissionNeedsPickup);
                                    if (jumpsToPickup == null)
                                        if (DebugConfig.DebugCourierMissions) Log("NextDestination: Null: if (jumpsToPickup == null)");
                                }
                                else if (DebugConfig.DebugCourierMissions)
                                {
                                    Log("NextDestination: Null: if (closestMissionNeedsPickup == null)");
                                }

                                DirectAgentMission closestMissionNeedsDeliver = missionsRequiringUsToTravel.Where(mission => mission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoDropOffLocation).OrderBy(i => i.CourierMissionDropoffBookmark.SolarSystem.JumpsHighSecOnly).FirstOrDefault();
                                if (closestMissionNeedsDeliver != null)
                                {
                                    jumpsToDeliver = JumpsToCourierMissionDropoffBookmark(closestMissionNeedsDeliver);
                                    if (jumpsToDeliver == null)
                                        if (DebugConfig.DebugCourierMissions) Log("NextDestination: Null: if (jumpsToDeliver == null)");
                                }
                                else if (DebugConfig.DebugCourierMissions)
                                {
                                    Log("NextDestination: Null: if (closestMissionNeedsDeliver == null)");
                                }

                                if (jumpsToDeliver == null && jumpsToPickup == null)
                                {
                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: Null: if (jumpsToDeliver == null && jumpsToPickup == null)");
                                    return null;
                                }

                                if (jumpsToPickup != null && jumpsToDeliver == null)
                                {
                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: if (jumpsToPickup[" + jumpsToPickup + "] != null && jumpsToDeliver == null)");
                                    if (closestMissionNeedsPickup.CourierMissionPickupBookmark != null)
                                    {
                                        if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsPickup.CourierMissionPickupBookmark.LocationId [" + closestMissionNeedsPickup.CourierMissionPickupBookmark.LocationId + "] jumps [" + closestMissionNeedsPickup.JumpsToPickupBookmark + "]");
                                        _nextMissionDestination = closestMissionNeedsPickup;
                                        return _nextMissionDestination;
                                    }

                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsPickup.CourierMissionPickupBookmark is null");
                                    return null;
                                }

                                if (jumpsToPickup == null)
                                {
                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: if (jumpsToPickup == null)");
                                    if (closestMissionNeedsDeliver.CourierMissionDropoffBookmark != null)
                                    {
                                        if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsDeliver.CourierMissionDeliverBookmark.LocationId [" + closestMissionNeedsDeliver.CourierMissionDropoffBookmark.LocationId + "] jumps [" + closestMissionNeedsDeliver.JumpsToDropoffBookmark + "]");
                                        _nextMissionDestination = closestMissionNeedsDeliver;
                                        return _nextMissionDestination;
                                    }

                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsDeliver.CourierMissionDeliverBookmark is null");
                                    return null;
                                }

                                if (jumpsToPickup == jumpsToDeliver)
                                {
                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: if (jumpsToPickup[" + jumpsToPickup + "] == jumpsToDeliver[" + jumpsToDeliver + "])");
                                    if (closestMissionNeedsPickup.CourierMissionPickupBookmark != null)
                                    {
                                        if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsPickup.CourierMissionPickupBookmark.LocationId [" + closestMissionNeedsPickup.CourierMissionPickupBookmark.LocationId + "] jumps [" + closestMissionNeedsPickup.JumpsToPickupBookmark + "]");
                                        _nextMissionDestination = closestMissionNeedsPickup;
                                        return _nextMissionDestination;
                                    }

                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsPickup.CourierMissionPickupBookmark is null");
                                    return null;
                                }

                                if (jumpsToDeliver > jumpsToPickup && closestMissionNeedsPickup.CourierMissionPickupBookmark.LocationId != ESCache.Instance.DirectEve.Session.LocationId)
                                {
                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: if (jumpsToDeliver[" + jumpsToDeliver + "] > jumpsToPickup[" + jumpsToPickup + "])");
                                    if (closestMissionNeedsPickup.CourierMissionPickupBookmark != null)
                                    {
                                        if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsPickup.CourierMissionPickupBookmark.LocationId [" + closestMissionNeedsPickup.CourierMissionPickupBookmark.LocationId + "] jumps [" + closestMissionNeedsPickup.JumpsToPickupBookmark + "] locationNumber [" + closestMissionNeedsPickup.CourierMissionPickupBookmark.LocationNumber + "][" + closestMissionNeedsPickup.CourierMissionPickupBookmark.LocationType + "]");
                                        _nextMissionDestination = closestMissionNeedsPickup;
                                        return _nextMissionDestination;
                                    }

                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsPickup.CourierMissionPickupBookmark is null");
                                    return null;
                                }

                                if (jumpsToPickup > jumpsToDeliver && closestMissionNeedsDeliver.CourierMissionDropoffBookmark.LocationId != ESCache.Instance.DirectEve.Session.LocationId)
                                {
                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: if (jumpsToPickup > jumpsToDeliver)");
                                    if (closestMissionNeedsDeliver.CourierMissionDropoffBookmark != null)
                                    {
                                        if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsDeliver.CourierMissionDeliverBookmark.StationId [" + closestMissionNeedsDeliver.CourierMissionDropoffBookmark.LocationId + "] jumps [" + closestMissionNeedsDeliver.CourierMissionDropoffBookmark + "] locationNumber [" + closestMissionNeedsDeliver.CourierMissionDropoffBookmark.LocationNumber + "][" + closestMissionNeedsDeliver.CourierMissionDropoffBookmark.LocationType + "]");
                                        _nextMissionDestination = closestMissionNeedsDeliver;
                                        return _nextMissionDestination;
                                    }

                                    if (DebugConfig.DebugCourierMissions) Log("NextDestination: closestMissionNeedsDeliver.CourierMissionDeliverBookmark is null");
                                    return null;
                                }

                                return null;
                            }

                            Log("NextDestination: Null: if (!missionsRequiringUsToTravel.Any())");
                            intMissionNum = 0;
                            foreach (DirectAgentMission missionWeHaveAccepted in MissionsWeHaveAccepted)
                            {
                                intMissionNum++;
                                Log("NextDestination: MissionWeHaveAccepted: [" + intMissionNum + "][" + missionWeHaveAccepted.Name + "][" + missionWeHaveAccepted.Agent.Name + "][" + missionWeHaveAccepted.CurrentCourierMissionCtrlState + "]");
                            }

                            return null;
                        }

                        Log("NextDestination: Null: if (!MissionsWeHaveAccepted.Any())");
                        intMissionNum = 0;
                        foreach (DirectAgentMission missionInJournal in ESCache.Instance.DirectEve.AgentMissions)
                        {
                            intMissionNum++;
                            Log("NextDestination: MissionInJournal: [" + intMissionNum + "][" + missionInJournal.Name + "][" + missionInJournal.Agent.Name + "][" + missionInJournal.CurrentCourierMissionCtrlState + "]");
                        }

                        DirectAgentMission tempMission = null;
                        if (ESCache.Instance.DirectEve.AgentMissions.Any(i => !i.Important))
                        {
                            //tempMission = ESCache.Instance.DirectEve.AgentMissions.FirstOrDefault(i => !i.Important);
                            //if (tempMission != null) Log("NextDestination: tempMission: [" + intMissionNum + "][" + tempMission.Name + "][" + tempMission.Agent.Name + "][" + tempMission.CurrentCourierMissionCtrlState + "]");
                        }

                        if (ESCache.Instance.InStation && Settings.Instance.EnableStorylines)
                        {
                            if (ESCache.Instance.DirectEve.AgentMissions.Any(i => i.Important && (i.Type.Contains("Trade") || i.Type.Contains("Courier"))))
                            {
                                tempMission = ESCache.Instance.DirectEve.AgentMissions.Find(i => i.Important && (i.Type.Contains("Trade") || i.Type.Contains("Courier")));
                                if (tempMission != null) Log("NextDestination: tempStorylineMission: [" + intMissionNum + "][" + tempMission.Name + "][" + tempMission.Agent.Name + "][" + tempMission.CurrentCourierMissionCtrlState + "]");
                            }

                            if (ESCache.Instance.DirectEve.AgentMissions.Any(i => i.Important && (i.Type.Contains("Mining") || i.Type.Contains("Encounter"))))
                            {
                                tempMission = ESCache.Instance.DirectEve.AgentMissions.Find(i => i.Important && (i.Type.Contains("Mining") || i.Type.Contains("Encounter")));
                                if (tempMission != null)
                                {
                                    DirectJournalWindow jw = ESCache.Instance.DirectEve.Windows.OfType<DirectJournalWindow>().FirstOrDefault();

                                    if (jw == null)
                                    {
                                        Log("Opening journal.");
                                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenJournal);
                                        return null;
                                    }

                                    if (jw.SelectedMainTab != MainTab.AgentMissions)
                                    {
                                        Log("Journal window mission tab is not selected. Switching the tab.");
                                        jw.SwitchMaintab(MainTab.AgentMissions);
                                        return null;
                                    }

                                    tempMission.RemoveOffer();
                                    Log("NextDestination: tempStorylineMission: [" + intMissionNum + "][" + tempMission.Name + "][" + tempMission.Agent.Name + "][" + tempMission.CurrentCourierMissionCtrlState + "]");
                                }
                            }
                        }

                        return tempMission ?? null;
                    }

                    return _nextMissionDestination;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public bool? ShouldPullAnotherDistributionMission
        {
            get
            {
                if (ESCache.Instance.CurrentShipsCargo == null)
                    return null;

                if (ESCache.Instance.CurrentShipsCargo.Items.Count == 0)
                    return true;

                if (ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity > AverageCargoSpaceUsedByTheseMissions)
                    return true;

                return false;
            }
        }

        private int AverageCargoSpaceUsedByTheseMissions => 3000;

        public static bool ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentCourierMissionBehaviorState != _StateToSet)
                {
                    //if (_StateToSet == CourierMissionsBehaviorState.GotoBase)
                    //    State.CurrentTravelerState = TravelerState.Idle;

                    Log("New CourierMissionBehaviorState [" + _StateToSet + "]");
                    State.CurrentCourierMissionBehaviorState = _StateToSet;
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void ClearPerPocketCache()
        {
            //_nextMissionDestination = null;
        }

        public static bool CMCBringSpoilsOfWar()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return true;

            if (MissionSettings.StorylineMission == null || MissionSettings.StorylineMission.Agent.StationId != ESCache.Instance.DirectEve.Session.LocationId)
                return true;

            if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
            {
                if (!Arm.BringSpoilsOfWar()) return false;
                return true;
            }

            return true;
        }

        public override void DoWork()
        {
            try
            {
                if (!_setCreatePathRan)
                    SetCreatePathes();

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

                                if (DebugConfig.DebugCombatMissionsBehavior) Log("QuestorController: RunOnce");

                                ESCache.Instance.IterateShipTargetValues();
                                ESCache.Instance.IterateUnloadLootTheseItemsAreLootItems();
                                //if (Settings.Instance.LootHangarCorpHangarDivisionNumber != null)
                                //{
                                //    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCorpHangar);
                                //}

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

                if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("CourierMissionsController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                if (DebugConfig.DebugCombatMissionsBehavior) Log("CourierMissionsController: CurrentCourierMissionBehaviorState [" + State.CurrentCourierMissionBehaviorState + "]");

                switch (State.CurrentCourierMissionBehaviorState)
                {
                    case CourierMissionsBehaviorState.Idle:
                        ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.Start, false);
                        break;

                    case CourierMissionsBehaviorState.Start:
                        Log("Start CourierMissionsController");
                        if (ESCache.Instance.InStation) ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.CompleteMissions, false);

                        if (ESCache.Instance.InSpace) ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.TravelForMissions, false);

                        break;

                    case CourierMissionsBehaviorState.CompleteMissions:
                        if (!ProcessCourierMissionsInThisStation()) return;
                        ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.TryToAcceptMissions);
                        break;

                    case CourierMissionsBehaviorState.TryToAcceptMissions:
                        if (NextAgentToAttemptToPullMissionFrom != null)
                        {
                            if (NextAgentToAttemptToPullMissionFrom.StationId != ESCache.Instance.DirectEve.Session.StationId)
                            {
                                Log("TryToAcceptMissions: if (NextAgentToAttemptToPullMissionFrom [" + NextAgentToAttemptToPullMissionFrom.Name + "] Station [" + NextAgentToAttemptToPullMissionFrom.Station.Name + "] != This Station[" + ESCache.Instance.DirectEve.Session.Station.Name + "])");
                                ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.GotoBase);
                                return;
                            }

                            if (DebugConfig.DebugCourierMissions) Log("TryToAcceptMissions: if (NextAgentToAttemptToPullMissionFrom != null)");
                            if (!AcceptMission(NextAgentToAttemptToPullMissionFrom)) return;
                        }

                        ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.ProcessCourierMissionsInThisStation);
                        break;

                    case CourierMissionsBehaviorState.UnloadImplants:
                        //if (!AcceptMission) return;
                        ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.CanWePullMoreMissions);
                        break;

                    case CourierMissionsBehaviorState.GotoBase:
                        GotoBaseState();
                        break;

                    case CourierMissionsBehaviorState.ProcessCourierMissionsInThisStation:
                        if (!ProcessCourierMissionsInThisStation()) return;
                        ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.TravelForMissions);
                        break;

                    case CourierMissionsBehaviorState.CanWePullMoreMissions:
                        if (DistributionAgentsInRangeWithNoMissionYet.Any(i => i.StationId == ESCache.Instance.DirectEve.Session.Station.Id && !i.IsAgentMissionAccepted))
                        {
                            ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.TryToAcceptMissions);
                            return;
                        }

                        ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.TravelForMissions);
                        break;

                    case CourierMissionsBehaviorState.TravelForMissions:

                        // gotobase
                        if (NextMissionDestination == null)
                        {
                            //if (ESCache.Instance.InStation && MissionSettings.AgentToPullNextRegularMissionFrom != null && ESCache.Instance.DirectEve.Session.Station.Id != MissionSettings.AgentToPullNextRegularMissionFrom.Station.Id)
                            //{
                            //
                            //}

                            Log("TravelForMissions: Decide: NextDestination is Null: GotoBase.");
                            ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.GotoBase);
                            return;
                        }

                        // go to the appropriate bookmark destination
                        if (!TravelToCourierMissionBookmark()) return;

                        //
                        // We are at destination
                        //

                        Log("TravelForMissions: We have arrived.");
                        foreach (DirectAgentMission myDistributionMission in ESCache.Instance.DirectEve.AgentMissions.Where(i => i.Agent.IsAgentMissionAccepted && i.Type.Contains("Courier")))
                        {
                            if (DebugConfig.DebugCourierMissions) Log("TravelForMissions: myDistributionMission [" + myDistributionMission.Name + "] Type [" + myDistributionMission.Type + "] State was [" + myDistributionMission.CurrentCourierMissionCtrlState + "]");
                            if (myDistributionMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoDropOffLocation)
                                if (myDistributionMission.CourierMissionDropoffBookmark.Station.Name == ESCache.Instance.DirectEve.Session.Station.Name)
                                    CourierMissionCtrl.ChangeCourierMissionCtrlState(myDistributionMission, CourierMissionCtrlState.DropOffItem);

                            if (myDistributionMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoPickupLocation)
                                if (myDistributionMission.CourierMissionPickupBookmark.Station.Name == ESCache.Instance.DirectEve.Session.Station.Name)
                                    CourierMissionCtrl.ChangeCourierMissionCtrlState(myDistributionMission, CourierMissionCtrlState.PickupItem);
                        }

                        if (ESCache.Instance.DirectEve.AgentMissions.Any(i => i.Agent.IsAgentMissionAccepted && i.Type.Contains("Courier") && (i.CurrentCourierMissionCtrlState == CourierMissionCtrlState.DropOffItem || i.CurrentCourierMissionCtrlState == CourierMissionCtrlState.PickupItem)))
                        {
                            Log("We have agents in station with active missions");
                            ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.CompleteMissions);
                            return;
                        }

                        Log("We have no agents in station with active missions; trying to find agents to pull missions from");
                        ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.TryToAcceptMissions);
                        return;

                    case CourierMissionsBehaviorState.Error:
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "Questor Error."));
                        ESCache.Instance.DisableThisInstance();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            if (cm.TryGetController<BuyItemsController>(out _))
                return false;

            if (cm.TryGetController<BuyPlexController>(out _))
                return false;

            if (cm.TryGetController<BuyLpItemsController>(out _))
                return false;

            return true;
        }

        public int? JumpsToBookmark(DirectBookmark myBookmark)
        {
            if (myBookmark != null)
            {
                if (myBookmark.SolarSystem != null)
                    return myBookmark.SolarSystem.JumpsHighSecOnly;

                Log("if (myBookmark.SolarSystem != null)");
                return null;
            }

            Log("if (myBookmark != null)");
            return null;
        }

        public int? JumpsToCourierMissionDropoffBookmark(DirectAgentMission myMission)
        {
            try
            {
                if (myMission != null)
                {
                    if (myMission.Type.Contains("Courier"))
                    {
                        if (myMission.CourierMissionDropoffBookmark != null)
                        {
                            int? jumps = JumpsToBookmark(myMission.CourierMissionDropoffBookmark);
                            if (jumps != null)
                                return (int)jumps;

                            Log("JumpsToCourierMissionDropoffBookmark: if (jumps == null)");
                            return null;
                        }

                        Log("JumpsToCourierMissionDropoffBookmark: if (myAgent.RegularMission.CourierMissionDeliverBookmark == null)");
                        return null;
                    }

                    Log("JumpsToCourierMissionDropoffBookmark: if (!myAgent.RegularMission.Type.Contains(Courier))");
                    return null;
                }

                Log("JumpsToCourierMissionDropoffBookmark: if (myAgent.RegularMission == null)");
                return null;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return null;
            }
        }

        public int? JumpsToCourierMissionPickupBookmark(DirectAgentMission myMission)
        {
            try
            {
                if (myMission != null)
                {
                    if (myMission.Type.Contains("Courier"))
                    {
                        if (myMission.CourierMissionPickupBookmark != null)
                        {
                            int? jumps = JumpsToBookmark(myMission.CourierMissionPickupBookmark);
                            if (jumps != null)
                                return (int)jumps;

                            Log("JumpsToCourierMissionPickupBookmark: if (jumps == null)");
                            return null;
                        }

                        Log("JumpsToCourierMissionPickupBookmark: if (myAgent.RegularMission.CourierMissionDeliverBookmark == null)");
                        return null;
                    }

                    Log("JumpsToCourierMissionPickupBookmark: if (!myAgent.RegularMission.Type.Contains(Courier))");
                    return null;
                }

                Log("JumpsToCourierMissionPickupBookmark: if (myAgent.RegularMission == null)");
                return null;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return null;
            }
        }

        //
        // this also should not be here, it would be better for it to be in a common place, like settings.cs or even better in the launcher
        //
        public void SetCreatePathes()
        {
            Statistics.DroneStatsLogPath = Logging.Log.BotLogpath;
            Statistics.DroneStatslogFile = Path.Combine(Statistics.DroneStatsLogPath, Logging.Log.CharacterName + ".DroneStats.log");

            Statistics.WindowStatsLogPath = Path.Combine(Logging.Log.BotLogpath, "WindowStats\\");
            Statistics.WindowStatslogFile = Path.Combine(Statistics.WindowStatsLogPath,
                Logging.Log.CharacterName + ".WindowStats-DayOfYear[" + DateTime.UtcNow.DayOfYear + "].log");
            Statistics.WreckLootStatisticsPath = Logging.Log.BotLogpath;
            Statistics.WreckLootStatisticsFile = Path.Combine(Statistics.WreckLootStatisticsPath,
                Logging.Log.CharacterName + ".WreckLootStatisticsDump.log");

            Statistics.MissionStats3LogPath = Path.Combine(Logging.Log.BotLogpath, "MissionStats\\");
            Statistics.MissionStats3LogFile = Path.Combine(Statistics.MissionStats3LogPath,
                Logging.Log.CharacterName + ".CustomDatedStatistics.csv");
            Statistics.MissionDungeonIdLogPath = Path.Combine(Logging.Log.BotLogpath, "MissionStats\\");
            Statistics.MissionDungeonIdLogFile = Path.Combine(Statistics.MissionDungeonIdLogPath,
                Logging.Log.CharacterName + "Mission-DungeonId-list.csv");
            Statistics.PocketStatisticsPath = Path.Combine(Logging.Log.BotLogpath, "PocketStats\\");
            Statistics.PocketStatisticsFile = Path.Combine(Statistics.PocketStatisticsPath,
                Logging.Log.CharacterName + "pocketstats-combined.csv");
            Statistics.PocketObjectStatisticsPath = Path.Combine(Logging.Log.BotLogpath, "PocketObjectStats\\");
            Statistics.PocketObjectStatisticsFile = Path.Combine(Statistics.PocketObjectStatisticsPath,
                Logging.Log.CharacterName + "PocketObjectStats-combined.csv");
            Statistics.MissionDetailsHtmlPath = Path.Combine(Logging.Log.BotLogpath, "MissionDetailsHTML\\");
            Statistics.MissionPocketObjectivesPath = Path.Combine(Logging.Log.BotLogpath, "MissionPocketObjectives\\");

            try
            {
                Directory.CreateDirectory(Logging.Log.BotLogpath);
                Directory.CreateDirectory(Logging.Log.ConsoleLogPath);
                Directory.CreateDirectory(Statistics.DroneStatsLogPath);
                Directory.CreateDirectory(Statistics.WreckLootStatisticsPath);
                Directory.CreateDirectory(Statistics.MissionStats3LogPath);
                Directory.CreateDirectory(Statistics.MissionDungeonIdLogPath);
                Directory.CreateDirectory(Statistics.PocketStatisticsPath);
                Directory.CreateDirectory(Statistics.PocketObjectStatisticsPath);
                Directory.CreateDirectory(Statistics.WindowStatsLogPath);
            }
            catch (Exception exception)
            {
                Logging.Log.WriteLine("Problem creating directories for logs [" + exception + "]");
            }

            _setCreatePathRan = true;
        }

        private static void GotoBaseState()
        {
            //
            // if we are already in the correct place, we are done.
            //
            if (ESCache.Instance.InStation)
            {
                //
                // we are docked
                //
                if (MissionSettings.AgentToPullNextRegularMissionFrom.StationId == ESCache.Instance.DirectEve.Session.StationId)
                {
                    if (WeAreDockedAtTheCorrectStationNowWhat(MissionSettings.AgentToPullNextRegularMissionFrom)) return;
                    return;
                }

                if (!CMCBringSpoilsOfWar()) return;
            }

            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log("GotoBase: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "CombatMissionsBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log("GotoBase: Traveler.TravelHome()");

            Traveler.TravelHome(MissionSettings.AgentToPullNextRegularMissionFrom);

            if (State.CurrentTravelerState == TravelerState.AtDestination && ESCache.Instance.InStation)
                if (DebugConfig.DebugGotobase) Log("GotoBase: We are at destination");
        }

        private static bool WeAreDockedAtTheCorrectStationNowWhat(DirectAgent myAgent)
        {
            if (Settings.Instance.BuyPlex && BuyPlexController.ShouldBuyPlex)
            {
                //BuyPlexController.CheckBuyPlex();
                //ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, false);
                //return;
            }

            //
            // we are already docked at the agents station
            //
            if (State.CurrentCombatState != CombatState.OutOfAmmo && myAgent.Mission != null &&
                myAgent.Mission.State == MissionState.Accepted)
            {
                Traveler.Destination = null;
                if (myAgent.Mission != null && DateTime.UtcNow > Time.Instance.LastMissionCompletionError.AddSeconds(30))
                {
                    Log("GotoBase: We are in the agents station: MissionState is Accepted - changing state to: CompleteMission");
                    ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.CompleteMissions, false);
                    return true;
                }

                Log("GotoBase: We are in the agents station: MissionState is Accepted - We tried to Complete the mission and it failed. Continuing");
                ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.UnloadImplants, false);
                return true;
            }

            Traveler.Destination = null;
            Log("GotoBase: We are in the agents station: changing state to: UnloadLoot");
            ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.UnloadImplants, false);
            return true;
        }

        private bool AcceptMission(DirectAgent myAgent)
        {
            if (State.CurrentAgentInteractionState == AgentInteractionState.Idle)
            {
                if (myAgent.IsAgentMissionAccepted)
                    return true;

                //if (MissionSettings.StorylineMissionDetected())
                //{
                //    _storyline.Reset();
                //    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.PrepareStorylineSwitchAgents, false);
                //    return;
                //}
                Log("Start conversation [" + myAgent.Name + "][Start Mission]");
                State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
            }

            AgentInteraction.ProcessState(myAgent);

            if (State.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                //
                // If AgentInteraction changed the state of CurrentCombatMissionBehaviorState to Idle: return
                //
                //if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Idle)
                //    return true;

                if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items != null && ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.CategoryId == (int)CategoryID.Implant) &&
                    ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.CombatShipName.ToLower())
                {
                    Log("Start: if(Cache.Instance.CurrentShipsCargo.Items.Any()) UnloadImplants");
                    ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.UnloadImplants, false);
                    return true;
                }

                //
                // otherwise continue on and change to the Arm state
                //
                State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                //ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.TryToAcceptMissions, false);
                return true;
            }

            return false;
        }

        private bool ProcessCourierMissionsInThisStation()
        {
            try
            {
                //if (ESCache.Instance.InSpace)
                //{
                //    ChangeCourierMissionBehaviorState(CourierMissionsBehaviorState.TravelForMissions);
                //    return false;
                //}

                if (ESCache.Instance.DirectEve.AgentMissions.Any(i => i.Agent.IsAgentMissionAccepted && i.Type.Contains("Courier") && i.CurrentCourierMissionCtrlState != CourierMissionCtrlState.GotoDropOffLocation && i.CurrentCourierMissionCtrlState != CourierMissionCtrlState.GotoPickupLocation && i.CurrentCourierMissionCtrlState != CourierMissionCtrlState.NotEnoughCargoRoom))
                {
                    if (DebugConfig.DebugCourierMissions) Log("ProcessCourierMissionsInThisStation: we still have missions to be procesesed.");
                    foreach (DirectAgentMission myAcceptedMission in ESCache.Instance.DirectEve.AgentMissions.Where(i => i.Agent.IsAgentMissionAccepted && i.Type.Contains("Courier") && i.CurrentCourierMissionCtrlState != CourierMissionCtrlState.GotoPickupLocation && i.CurrentCourierMissionCtrlState != CourierMissionCtrlState.GotoDropOffLocation).OrderByDescending(i => i.WeAreDockedAtDropOffLocation).ThenByDescending(i => i.WeAreDockedAtPickupLocation))
                    {
                        if (DebugConfig.DebugCourierMissions) Log("ProcessCourierMissionsInThisStation: CourierMission [" + myAcceptedMission.Name + "][" + myAcceptedMission.Agent.Name + "][" + myAcceptedMission.Agent.AgentTypeName + "][" + myAcceptedMission.CurrentCourierMissionCtrlState + "] being procesesed.");
                        switch (myAcceptedMission.CurrentCourierMissionCtrlState)
                        {
                            //case CourierMissionCtrlState.GotoDropOffLocation:
                            //case CourierMissionCtrlState.GotoPickupLocation:
                            case CourierMissionCtrlState.ActivateTransportShip:
                            case CourierMissionCtrlState.CompleteMission:
                            case CourierMissionCtrlState.DropOffItem:
                            case CourierMissionCtrlState.PickupItem:
                            case CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation:
                            case CourierMissionCtrlState.ItemsFoundAndBeingMoved:
                            case CourierMissionCtrlState.Statistics:
                            case CourierMissionCtrlState.Start:
                            case CourierMissionCtrlState.Idle:
                                CourierMissionCtrl.ProcessState(myAcceptedMission);
                                continue;
                        }
                    }

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        private bool TravelToCourierMissionBookmark()
        {
            if (NextMissionDestination.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoPickupLocation)
            {
                if (DebugConfig.DebugCourierMissions && !ESCache.Instance.InWarp) Log("Decide: NextDestination [" + NextMissionDestination.CourierMissionPickupBookmark.Title + "][" + NextMissionDestination.CourierMissionPickupBookmark.SolarSystem.Name + "][" + NextMissionDestination.CourierMissionPickupBookmark.LocationId + "] != this StationID [" + ESCache.Instance.DirectEve.Session.LocationId + "] Solarsystem [" + ESCache.Instance.DirectEve.Session.SolarSystem.Name + "]");
                if (!Traveler.TravelToMissionBookmark(NextMissionDestination, NextMissionDestination.CourierMissionPickupBookmark.Title)) return false;
                if (ESCache.Instance.InSpace || !ESCache.Instance.InStation) return false;
                return true;
            }

            if (NextMissionDestination.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoDropOffLocation)
            {
                if (DebugConfig.DebugCourierMissions && !ESCache.Instance.InWarp) Log("Decide: NextDestination [" + NextMissionDestination.CourierMissionDropoffBookmark.Title + "][" + NextMissionDestination.CourierMissionDropoffBookmark.SolarSystem.Name + "][" + NextMissionDestination.CourierMissionDropoffBookmark.LocationId + "] != this StationID [" + ESCache.Instance.DirectEve.Session.LocationId + "] Solarsystem [" + ESCache.Instance.DirectEve.Session.SolarSystem.Name + "]");
                if (!Traveler.TravelToMissionBookmark(NextMissionDestination, NextMissionDestination.CourierMissionDropoffBookmark.Title)) return false;
                if (ESCache.Instance.InSpace || !ESCache.Instance.InStation) return false;
                return true;
            }

            Log("TravelToCourierMissionBookmark: NextMissionDestination [" + NextMissionDestination.Name + "][" + NextMissionDestination.Agent.Name + "] CurrentCourierMissionCtrlState [" + NextMissionDestination.CurrentCourierMissionCtrlState + "]");
            return false;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}