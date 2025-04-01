extern alias SC;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.Storylines;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Events;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Questor.Activities
{
    public static class AgentInteraction
    {
        #region Constructors

        static AgentInteraction()
        {
        }

        #endregion Constructors

        #region Fields

        public const string strAccept = "Accept";
        public const string strClose = "Close";
        public const string strCompleteMission = "Complete Mission";
        public const string strDecline = "Decline";
        public const string strDelay = "Delay";
        public const string strLocateCharacter = "Locate Character";
        public const string strMyOfficesAreLocatedAt = "My offices are located at";
        public const string strNoJobsAvailable = "no jobs available";
        public const string strCareerAgentCompletedAllCareerMissions = "I'm deeply grateful for everything you've done, ";
        public const string strQuit = "Quit";
        public const string strReferral = "referral";
        public const string strRequestMission = "Request Mission";
        public const string strViewMission = "View Mission";
        public static DateTime _lastAgentWindowInteraction { get; set; }
        public static bool boolNoMissionsAvailable;
        public static bool boolSwitchAgents;
        private static readonly DateTime _lastMissionDecline = DateTime.UtcNow.AddDays(-1);

        private static bool _agentStandingsCheckFlag;
        private static DateTime _agentStandingsCheckTimeOut = DateTime.UtcNow.AddDays(1);
        private static DateTime _waitingOnMissionTimer = DateTime.UtcNow;
        private static bool _waitToDeclineThisMissionWaitUntilWeWillNotHarmStandings;
        private static DateTime _waitUntilThisTimeToTryToDeclineAnyOtherMissions = DateTime.UtcNow.AddDays(-1);

        #endregion Fields

        #region Properties

        public static bool ForceAccept { get; set; }

        public static DirectWindow JournalWindow { get; set; }

        public static Dictionary<long, AgentButtonType> LastButtonPushedPerAgentId { get; set; } = new Dictionary<long, AgentButtonType>();

        public static AgentInteractionPurpose Purpose { get; set; }

        #endregion Properties

        #region Methods

        private static bool AreStandingsHighEnoughToDeclineAnotherGreyListedMission(DirectAgentMission myMission)
        {
            if (myMission != null && myMission.Name.ToLower().Contains("Anomic"))
                return true;

            if (myMission == null)
            {
                Log.WriteLine("AreStandingsHighEnoughToDeclineAnotherGreyListedMission: if (MissionSettings.RegularMission == null)");
                return true;
            }

            //
            // personal standings to agent must be above -2.0 or we lose access
            // MaximumStandingUsedToAccessAgent (highest of personal, corp or faction standing) must be above the agent's access level or we lose access
            //
            if (AreStandingsHighEnoughToDeclineAnotherMissionWithoutLosingAccess(myMission))
                if (myMission.Agent.MaximumStandingUsedToAccessAgent > MissionSettings.MinAgentGreyListStandings)
                {
                    if (myMission.Agent.AgentEffectiveStandingtoMe > 1.5)
                        return true;

                    return false;
                }

            return false;
        }

        private static bool AreStandingsHighEnoughToDeclineAnotherMissionWithoutLosingAccess(DirectAgentMission myMission)
        {
            if (myMission != null && myMission.Name.ToLower().Contains("Anomic"))
                return true;

            if (myMission == null)
            {
                Log.WriteLine("AreStandingsHighEnoughToDeclineAnotherMissionWithoutLosingAccess: if (MissionSettings.RegularMission == null)");
                return true;
            }

            //
            // personal standings to agent must be above -2.0 or we lose access
            // MaximumStandingUsedToAccessAgent (highest of personal, corp or faction standing) must be above the agent's access level or we lose access
            //
            if (myMission.Agent.MaximumStandingUsedToAccessAgent - .3 > myMission.Agent.EffectiveStandingNeededToAccessAgent())
            {
                if (myMission.Agent.AgentEffectiveStandingtoMe < -0.8)
                    return false;

                return true;
            }

            return false;
        }

        public static bool ChangeAgentInteractionState(AgentInteractionState agentInteractionState, DirectAgent myAgent, bool wait = false)
        {
            try
            {
                if (State.CurrentAgentInteractionState != agentInteractionState)
                {
                    _waitingOnMissionTimer = DateTime.UtcNow;
                    State.CurrentAgentInteractionState = agentInteractionState;
                    Log.WriteLine("New AgentInteractionState [" + agentInteractionState + "]");

                    if (myAgent == null)
                        return true;

                    if (LastButtonPushedPerAgentId != null && LastButtonPushedPerAgentId.Count > 0 && LastButtonPushedPerAgentId.ContainsKey(myAgent.AgentId))
                    {
                        AgentButtonType tempButton;
                        LastButtonPushedPerAgentId.TryGetValue(myAgent.AgentId, out tempButton);
                        if (tempButton == AgentButtonType.COMPLETE_MISSION)
                            LastButtonPushedPerAgentId.AddOrUpdate(myAgent.AgentId, AgentButtonType.None);
                    }

                    if (wait)
                        return true;
                    ProcessState(myAgent);

                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(State.CurrentAgentInteractionState + ": Exception [" + ex + "]");
                return false;
            }
        }

        public static bool CloseAgentWindowIfRequestMissionButtonExists(string StateForLogs, DirectAgent myAgent)
        {
            if (myAgent == null)
            {
                Log.WriteLine("[" + StateForLogs + "]: the Agent we were passed is null, how?");
                return true;
            }

            if (!myAgent.OpenAgentWindow(true)) return false;

            if (RequestButton(myAgent) != null)
            {
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                {
                    if (myAgent.CareerAgentWindow.Close()) return true;
                    return false;
                }

                if (myAgent.AgentWindow.Close()) return true;
                return false;
            }

            //
            // if complete button doesnt exist or it does exist and we recently pushed it...
            //

            return true;
        }

        private static bool DoWeNeedHumanIntervention(DirectAgent myAgent)
        {
            try
            {
                if (ESCache.Instance.EveAccount.NeedHumanIntervention)
                {
                    Log.WriteLine("needHumanIntervention: Window Detected via CleanupController");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NeedHumanIntervention), false);
                    MissionSettings.MissionCompletionErrors++;
                    Time.Instance.LastMissionCompletionError = DateTime.UtcNow;

                    Log.WriteLine("This window indicates an error completing a mission: [" + MissionSettings.MissionCompletionErrors +
                                  "] errors already we will stop questor and halt restarting when we reach 3");

                    if (MissionSettings.MissionCompletionErrors > 3 && ESCache.Instance.InStation)
                        if (MissionSettings.MissionXMLIsAvailable &&
                            !MissionSettings.DeclineMissionsWithTooManyMissionCompletionErrors)
                        {
                            Log.WriteLine("ERROR: Mission XML is available for [" + myAgent.Mission.Name +
                                          "] but we still did not complete the mission after 3 tries! - ERROR!");
                            Log.WriteLine("DeclineMissionsWithTooManyMissionCompletionErrors is [" +
                                          MissionSettings.DeclineMissionsWithTooManyMissionCompletionErrors + "] not Declining Mission");
                            ESCache.Instance.PauseAfterNextDock = true;
                            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                            return true;
                        }
                        else if (!MissionSettings.MissionXMLIsAvailable &&
                                 !MissionSettings.DeclineMissionsWithTooManyMissionCompletionErrors)
                        {
                            Log.WriteLine("ERROR: Mission XML is missing for [" + myAgent.Mission.Name +
                                          "] and we we unable to complete the mission after 3 tries! - ERROR!");
                            Log.WriteLine("DeclineMissionsWithTooManyMissionCompletionErrors is [" +
                                          MissionSettings.DeclineMissionsWithTooManyMissionCompletionErrors + "] not Declining Mission");
                            ESCache.Instance.DeactivateScheduleAndCloseEveAfterNextDock = true;
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.UseScheduler), false);
                            //we purposely disable autostart so that when we quit eve and questor here it stays closed until manually restarted as this error is fatal (and repeating)
                            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                            return true;
                        }
                        else if (MissionSettings.DeclineMissionsWithTooManyMissionCompletionErrors)
                        {
                            Log.WriteLine("ERROR: [" + myAgent.Mission.Name + "] is not able to complete successfully after 3 tries! - ERROR!");
                            Log.WriteLine("DeclineMissionsWithTooManyMissionCompletionErrors is [" +
                                          MissionSettings.DeclineMissionsWithTooManyMissionCompletionErrors + "] Declining Mission");
                            ChangeAgentInteractionState(AgentInteractionState.DeclineMission, null, false);
                            return true;
                        }

                    ChangeAgentInteractionState(AgentInteractionState.Done, null, false);
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            //
            // Verify settings and log errors as needed
            //

            try
            {
                Log.WriteLine("LoadSettings: AgentInteraction");
                if (MissionSettings.AgentToPullNextRegularMissionFrom == null || !MissionSettings.AgentToPullNextRegularMissionFrom.IsValid)
                    if (MissionSettings.AgentToPullNextRegularMissionFrom != null)
                    {
                        Log.WriteLine("if (Cache.Instance.Agent != null) - AgentInteraction.AgentId = (long)Cache.Instance.Agent.AgentId");

                        if (MissionSettings.AgentToPullNextRegularMissionFrom == null || !MissionSettings.AgentToPullNextRegularMissionFrom.IsValid)
                            Log.WriteLine("AgentInteraction.Agent == null || !AgentInteraction.Agent.IsValid");
                    }
                    else
                    {
                        Log.WriteLine("Cache.Instance.Agent == null");
                        Log.WriteLine("Unable to locate agent 2  [" + MissionSettings.StrCurrentAgentName + "]");
                    }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: " + ex);
            }
        }

        private static bool OpenJournalWindow()
        {
            if (ESCache.Instance.InStation)
            {
                JournalWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectJournalWindow>().FirstOrDefault();
                if (JournalWindow == null)
                {
                    if (DateTime.UtcNow < Time.Instance.NextWindowAction)
                        return false;

                    if (!ESCache.Instance.OkToInteractWithEveNow)
                    {
                        if (DebugConfig.DebugInteractWithEve) Log.WriteLine("AgentInteraction: OpenJournalWindow: !OkToInteractWithEveNow");
                        return false;
                    }

                    if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenJournal))
                    {
                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                        Statistics.LogWindowActionToWindowLog("JournalWindow", "Opening JournalWindow");
                        Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(1, 2));
                        Log.WriteLine("Opening Journal Window: waiting [" + Math.Round(Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                    }

                    return false;
                }

                return true;
            }

            return false;
        }

        public static bool PressAcceptButtonIfItExists(string StateForLogs, DirectAgent myAgent)
        {
            if (!myAgent.OpenAgentWindow(true)) return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
            {
                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.NO_JOBS_AVAILABLE))
                {
                    myAgent.CareerAgentWindow.Close();
                    //ESCache.Instance.CloseQuestor(true, "NO_JOBS_AVAILABLE");
                    ESCache.Instance.PauseAfterNextDock = true;
                    return false;
                }
            }
            else if (myAgent.AgentWindow.Buttons.Any(i => i.Type == AgentButtonType.NO_JOBS_AVAILABLE))
            {
                myAgent.AgentWindow.Close();
                //ESCache.Instance.CloseQuestor(true, "NO_JOBS_AVAILABLE");
                ESCache.Instance.PauseAfterNextDock = true;
                return false;
            }

            if (ESCache.Instance.InStation)
            {
                if (myAgent.StationId == ESCache.Instance.DirectEve.Session.StationId)
                {
                    if (AcceptButton(myAgent) != null && myAgent.Mission != null && DateTime.UtcNow > Time.Instance.LastAcceptMissionAttempt.AddSeconds(3))
                    {
                        Log.WriteLine("[" + StateForLogs + "]: Found [ Accept Mission ] button for Agent [" + myAgent.Name + "].");
                        //Are we in station with the agent? I dont think ANY agents allow pulling missions remotely.

                        Log.WriteLine("[" + StateForLogs + "]: Found [ Accept Mission ] button. We are in station with [" + myAgent.Name + "]. Pressing Accept");
                        if (PressAgentWindowButton(AcceptButton(myAgent)))
                        {
                            LastButtonPushedPerAgentId.AddOrUpdate(myAgent.AgentId, AgentButtonType.ACCEPT);
                            Log.WriteLine("Saying [Accept]");
                            Time.Instance.LastAcceptMissionAttempt = DateTime.UtcNow;
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ACCEPT_MISSION, "Accepting mission."));
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MissionStarted), DateTime.UtcNow);
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LoyaltyPoints), myAgent.LoyaltyPoints);
                            Statistics.StartedMission = DateTime.UtcNow;
                            Statistics.FinishedMission = DateTime.UtcNow;
                            return false;
                        }

                        Log.WriteLine("[" + StateForLogs + "]: Pressing [ Accept Mission ] Button Failed!");
                        return false;
                    }

                    if (CompleteButton(myAgent) != null)
                        return true;

                    return false;
                }

                Log.WriteLine("[" + StateForLogs + "]: [ Accept Mission ] Button Exists, but we are not in station with [" + myAgent.Name + "]");
                return true;
            }

            //
            // if the accept button exists, but we recently pushed it, we should wait...
            //
            return false;
        }

        public static bool PressCompleteButtonIfItExists(string StateForLogs, DirectAgent myAgent)
        {
            if (myAgent == null)
            {
                Log.WriteLine("[" + StateForLogs + "]: the Agent we were passed is null, how?");
                return false;
            }

            if (!myAgent.OpenAgentWindow(true)) return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
            {
                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.NO_JOBS_AVAILABLE))
                {
                    myAgent.CareerAgentWindow.Close();
                    //ESCache.Instance.CloseQuestor(true, "NO_JOBS_AVAILABLE");
                    ESCache.Instance.PauseAfterNextDock = true;
                    return false;
                }
            }
            else if (myAgent.AgentWindow.Buttons.Any(i => i.Type == AgentButtonType.NO_JOBS_AVAILABLE))
            {
                myAgent.AgentWindow.Close();
                //ESCache.Instance.CloseQuestor(true, "NO_JOBS_AVAILABLE");
                ESCache.Instance.PauseAfterNextDock = true;
                return false;
            }

            if (myAgent.Mission == null)
            {
                Log.WriteLine("[" + StateForLogs + "]: we have no mission yet?!");
                if (!PressRequestButtonIfItExists(StateForLogs, myAgent)) return false;
                return true;
            }

            if (myAgent.Mission.IsMissionFinished != null && !(bool)myAgent.Mission.IsMissionFinished) return true;

            if (CompleteButton(myAgent) != null && myAgent.Mission != null && DateTime.UtcNow > Time.Instance.LastMissionCompletionError.AddSeconds(20) && DateTime.UtcNow > Time.Instance.LastCompleteMissionAttempt.AddSeconds(3))
            {
                AgentButtonType lastButtonPushed = AgentButtonType.None;
                if (LastButtonPushedPerAgentId.ContainsKey(myAgent.AgentId))
                    LastButtonPushedPerAgentId.TryGetValue(myAgent.AgentId, out lastButtonPushed);

                Log.WriteLine("[" + StateForLogs + "]: Found [ Complete Mission ] button: LastButtonPushed [" + lastButtonPushed + "]");
                if (lastButtonPushed == AgentButtonType.COMPLETE_MISSION)
                {
                    Log.WriteLine("[" + StateForLogs + "]: Attempted to complete the mission and failed: doing mission: return true");
                    Time.Instance.LastMissionCompletionError = DateTime.UtcNow;
                    return true;
                }

                if (PressAgentWindowButton(CompleteButton(myAgent)))
                {
                    Log.WriteLine("[" + StateForLogs + "]: Found [ Complete Mission ] button: Complete Button Pushed");
                    Time.Instance.LastCompleteMissionAttempt = DateTime.UtcNow;
                    LastButtonPushedPerAgentId.AddOrUpdate(myAgent.AgentId, AgentButtonType.COMPLETE_MISSION);
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                    {
                        if (ESCache.Instance.DirectEve.Session.StationId != myAgent.StationId)
                        {
                            Log.WriteLine("[" + StateForLogs + "]: We are not in the same station as the agent, so we are going to go home.");
                            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                            ClearMissionSpecificSettings();
                            return false;
                        }
                    }

                    ClearMissionSpecificSettings();
                    return false;
                }

                Log.WriteLine("[" + StateForLogs + "]: Pressing [ Complete Mission ] Button Failed!");
                return false;
            }

            //
            // if complete button doesnt exist or it does exist and we recently pushed it...
            //

            return true;
        }

        public static bool PressViewButtonIfItExists(string StateForLogs, DirectAgent myAgent)
        {
            if (myAgent == null)
            {
                Log.WriteLine("[" + StateForLogs + "]: the Agent we were passed is null, how?");
                return false;
            }

            if (!myAgent.OpenAgentWindow(true)) return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
            {
                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.NO_JOBS_AVAILABLE))
                {
                    myAgent.CareerAgentWindow.Close();
                    //ESCache.Instance.CloseQuestor(true, "NO_JOBS_AVAILABLE");
                    ESCache.Instance.PauseAfterNextDock = true;
                    return false;
                }

                //there is no view button with career agents!
                return true;
            }
            else if (myAgent.AgentWindow.Buttons.Any(i => i.Type == AgentButtonType.NO_JOBS_AVAILABLE))
            {
                myAgent.AgentWindow.Close();
                //ESCache.Instance.CloseQuestor(true, "NO_JOBS_AVAILABLE");
                ESCache.Instance.PauseAfterNextDock = true;
                return false;
            }

            if (myAgent.Mission == null)
            {
                Log.WriteLine("[" + StateForLogs + "]: we have no mission yet?!");
                if (!PressRequestButtonIfItExists(StateForLogs, myAgent)) return false;
                return true;
            }

            if (myAgent.AgentWindow.ViewMode == "SinglePaneView" && CompleteButton(myAgent) == null && QuitButton(myAgent) == null && AcceptButton(myAgent) == null && ViewButton(myAgent) != null)
            {
                Log.WriteLine("[" + StateForLogs + "]: Found [ View ] button.");
                if (PressAgentWindowButton(ViewButton(myAgent)))
                {
                    LastButtonPushedPerAgentId.AddOrUpdate(myAgent.AgentId, AgentButtonType.VIEW_MISSION);
                    return false;
                }

                Log.WriteLine("[" + StateForLogs + "]: Pressing [ View ] Button Failed!");
                return false;
            }

            return true;
        }

        public static void ProcessState(DirectAgent myAgent)
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return;

                if (ESCache.Instance.InSpace)
                    return;

                if (myAgent == null)
                {
                    ChangeAgentInteractionState(AgentInteractionState.Done, null);
                    return;
                }

                if (!myAgent.CanAccessAgent)
                {
                    Log.WriteLine($"Error: Can't access this agent, the standing requirement is not met.");
                    return;
                }

                if (myAgent.Level == 4 && !ESCache.Instance.DirectEve.Me.IsOmegaClone)
                {
                    Log.WriteLine($"Error: Can't access a level 4 agent while being in alpha state.");
                    return;
                }

                if (DoWeNeedHumanIntervention(myAgent)) return;

                if (ESCache.Instance.EveAccount.BotUsesHydra && ESCache.Instance.EveAccount.IsLeader)
                    PushAgentInfo(myAgent);

                if (DateTime.UtcNow < _lastAgentWindowInteraction.AddMilliseconds(ESCache.Instance.RandomNumber(2500, 3000)))
                    return;

                if (!WaitOnAgentWindowButtonResponse(myAgent)) return;

                if (DebugConfig.DebugAgentInteraction) Log.WriteLine("State.CurrentAgentInteractionState [" + State.CurrentAgentInteractionState + "]");

                switch (State.CurrentAgentInteractionState)
                {
                    case AgentInteractionState.Idle:
                        break;

                    case AgentInteractionState.Done:
                        break;

                    case AgentInteractionState.StartConversation:
                        StartConversation(myAgent);
                        break;

                    case AgentInteractionState.ReplyToAgent:
                        ReplyToAgent(myAgent);
                        break;

                    case AgentInteractionState.WeHaveAMissionWaiting:
                        WeHaveAMissionWaiting(myAgent);
                        break;

                    case AgentInteractionState.PrepareForOfferedMission:
                        PrepareForOfferedMission(myAgent.Mission);
                        break;

                    case AgentInteractionState.AcceptMission:
                        AcceptMission(myAgent);
                        break;

                    case AgentInteractionState.DeclineMission:
                        DeclineMission(myAgent);
                        break;

                    case AgentInteractionState.WaitForDeclineTimerToExpire:
                        if (DateTime.UtcNow > _waitUntilThisTimeToTryToDeclineAnyOtherMissions)
                            ChangeAgentInteractionState(AgentInteractionState.DeclineMission, myAgent, false);
                        break;

                    case AgentInteractionState.CloseConversation:
                        break;

                    case AgentInteractionState.UnexpectedDialogOptions:
                        Log.WriteLine("UnexpectedDialogOptions AgentInteraction. Pausing.");
                        ControllerManager.Instance.SetPause(true);
                        break;
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        public static void PushAgentInfo(DirectAgent myAgent)
        {
            if (ESCache.Instance.EveAccount.LeaderHomeSystemId != myAgent.SolarSystemId)
            {
                if (DebugConfig.DebugAgentInteractionReplyToAgent) Log.WriteLine("AgentInteraction: PushAgentInfo: LeaderHomeSystemId [" + myAgent.SolarSystemId + "] ");
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderHomeSystemId), myAgent.SolarSystemId);
            }

            if (ESCache.Instance.EveAccount.LeaderHomeStationId != myAgent.StationId)
            {
                if (DebugConfig.DebugAgentInteractionReplyToAgent) Log.WriteLine("AgentInteraction: PushAgentInfo: LeaderHomeStationId [" + myAgent.StationId + "] ");
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderHomeStationId), myAgent.StationId);
            }
        }

        private static bool? ShouldWeDeclineThisMission(DirectAgentMission myMission)
        {
            //
            // Do we want to decline this mission?
            //
            if (myMission == null)
                return false;

            if (myMission != null)
            {
                if (myMission.State != MissionState.Offered)
                    if (!myMission.Name.Contains("Anomic"))
                        return false;
            }

            //
            // LowSec?
            //

            //
            // at this point we have not yet accepted the mission, thus we do not have the bookmark in people and places
            // we cannot and should not accept the mission without checking the route first, declining after accepting incurs a much larger penalty to standings
            //
            bool? tempBool = IsThisMissionLocatedInLowSec(myMission);
            if (tempBool == null) return null;
            if ((bool)tempBool)
            {
                Log.WriteLine("RegularMission [" + Log.FilterPath(myMission.Name) + "] would take us through low sec! Expires [" + myMission.ExpiresOn + "]");
                MissionSettings.LastReasonMissionAttemptedToBeDeclined = "RegularMission [" + Log.FilterPath(myMission.Name) + "] would take us through low sec!";
                return true;
            }

            //
            // Faction is on the Faction Blacklist?
            //
            if (MissionSettings.IsFactionBlacklisted(myMission))
            {
                if (AreStandingsHighEnoughToDeclineAnotherMissionWithoutLosingAccess(myMission))
                {
                    Log.WriteLine("Faction [" + myMission.Faction.Name + "] is a faction we have blacklisted [" + Log.FilterPath(myMission.Name) + "] Expires [" + myMission.ExpiresOn + "]");
                    MissionSettings.LastReasonMissionAttemptedToBeDeclined = "Faction [" + myMission.Faction.Name + "] is Blacklisted";
                    return true;
                }

                Log.WriteLine("Faction [" + myMission.Faction.Name + "] is a faction we have blacklisted [" + Log.FilterPath(myMission.Name) + "] Expires [" + myMission.ExpiresOn + "]");
                Log.WriteLine("We cannot safely decline another mission at the moment. Waiting for the 2 hour time since our last decline to expire. If we were to decline the mission youd likely lose access to this agent and we assume doing a blacklsited mission == bad.");
                MissionSettings.LastReasonMissionAttemptedToBeDeclined = "Faction [" + myMission.Faction.Name + "] is Blacklisted";
                _waitToDeclineThisMissionWaitUntilWeWillNotHarmStandings = true;
                return true;
            }

            if (DebugConfig.DebugDecline) Log.WriteLine("Faction [" + myMission.Faction.Name + "] is not on the faction blacklist");

            //
            // Mission is on the mission Blacklist?
            //
            if (MissionSettings.IsMissionBlacklisted(myMission))
            {
                if (AreStandingsHighEnoughToDeclineAnotherMissionWithoutLosingAccess(myMission))
                {
                    Log.WriteLine("RegularMission [" + Log.FilterPath(myMission.Name) + "] is a mission we have blacklisted. Expires [" + myMission.ExpiresOn + "]");
                    MissionSettings.LastReasonMissionAttemptedToBeDeclined = "RegularMission [" + Log.FilterPath(myMission.Name) + "] is on the mission Blacklist";
                    return true;
                }

                Log.WriteLine("RegularMission [" + Log.FilterPath(myMission.Name) + "] is a mission we have blacklisted. Expires [" + myMission.ExpiresOn + "]");
                Log.WriteLine("We cannot safely decline another mission at the moment. Waiting for the 2 hour timew since our last decline to expire. If we were to decline the mission youd likely lose access to this agent and we assume doing a blacklsited mission == bad.");
                MissionSettings.LastReasonMissionAttemptedToBeDeclined = "RegularMission [" + Log.FilterPath(myMission.Name) + "] is on the mission Blacklist";
                _waitToDeclineThisMissionWaitUntilWeWillNotHarmStandings = true;
                return true;
            }

            if (DebugConfig.DebugDecline) Log.WriteLine("[" + myMission.Name + "] is not on the blacklist and might be on the GreyList we have not checked yet");

            //
            // GreyListed?
            //
            if (MissionSettings.MissionGreylist.Any(m => m.ToLower() == myMission.Name.ToLower()))
            {
                if (AreStandingsHighEnoughToDeclineAnotherGreyListedMission(myMission))
                {
                    Log.WriteLine("RegularMission [" + Log.FilterPath(myMission.Name) + "] is a mission we have greylisted. Expires [" + myMission.ExpiresOn + "]");
                    MissionSettings.LastReasonMissionAttemptedToBeDeclined = "RegularMission [" + Log.FilterPath(myMission.Name) + "] is on the mission Greylist";
                    return true;
                }

                Log.WriteLine("RegularMission [" + Log.FilterPath(myMission.Name) + "] is a mission we have greylisted. Expires [" + myMission.ExpiresOn + "]");
                Log.WriteLine("We cannot safely decline another mission at the moment. Waiting is not necessary since this mission is only greylisted we are going to accept this mission!");
                return false;
            }

            // If not forced to accept, decline non storyline mining, trade missions
            //if (!ForceAccept)
                if (myMission.State == MissionState.Offered)
                {
                    Log.WriteLine("[" + myMission.Name + "] is in MissionState [" + myMission.State + "] Type [" + myMission.Type + "]");
                    if (!myMission.Important)
                    {
                        Log.WriteLine("[" + myMission.Name + "] is Storyline [" + myMission.Important + "] this should be false");
                        if (myMission.Type.ToLower().Contains("Mining".ToLower())) //MissionSettings.RegularMission.Type.ToLower().Contains("Trade".ToLower())
                        {
                            Log.WriteLine("[" + myMission.Name + "] is Type [" + myMission.Type + "] - should be mining if we got here");
                            if (AreStandingsHighEnoughToDeclineAnotherMissionWithoutLosingAccess(myMission))
                            {
                                Log.WriteLine("[" + myMission.Name + "] is a [" + myMission.Type + "] - non-storyline mission and will be declined based on this alone. ");
                                MissionSettings.LastReasonMissionAttemptedToBeDeclined = "RegularMission [" + Log.FilterPath(myMission.Name) + "] is in lowsec";
                                return true;
                            }

                            Log.WriteLine("[" + myMission.Name + "] is a [" + myMission.Type + "] - non-storyline mission and will be declined based on this alone. ");
                            MissionSettings.LastReasonMissionAttemptedToBeDeclined = "RegularMission [" + Log.FilterPath(myMission.Name) + "] is in lowsec";
                            _waitToDeclineThisMissionWaitUntilWeWillNotHarmStandings = true;
                            return true;
                        }

                        if (Settings.Instance.DoNotTryToDoEncounterMissions && myMission.Type.ToLower().Contains("Encounter".ToLower()) && !myMission.Name.Contains("Anomic")) //MissionSettings.RegularMission.Type.ToLower().Contains("Trade".ToLower())
                        {
                            Log.WriteLine("[" + myMission.Name + "] is Type [" + myMission.Type + "]");
                            if (AreStandingsHighEnoughToDeclineAnotherMissionWithoutLosingAccess(myMission))
                            {
                                Log.WriteLine("[" + myMission.Name + "] is a [" + myMission.Type + "] - non-storyline mission and will be declined because it is an encounter mission");
                                MissionSettings.LastReasonMissionAttemptedToBeDeclined = "RegularMission [" + Log.FilterPath(myMission.Name) + "] will be declined because it is an encounter mission";
                                return true;
                            }

                            Log.WriteLine("[" + myMission.Name + "] is a [" + myMission.Type + "] - non-storyline mission and will be declined based on this alone. ");
                            MissionSettings.LastReasonMissionAttemptedToBeDeclined = "RegularMission [" + Log.FilterPath(myMission.Name) + "] is in lowsec";
                            _waitToDeclineThisMissionWaitUntilWeWillNotHarmStandings = true;
                            return true;
                        }
                    }
                }

            return false;
        }

        private static DirectAgentButton AcceptButton(DirectAgent myAgent)
        {
            DirectAgentButton _acceptButton = FindAgentResponse(AgentButtonType.ACCEPT, myAgent);
            if (_acceptButton == null)
                ChangeLastButtonPushed(AgentButtonType.ACCEPT, AgentButtonType.None, myAgent);
            return _acceptButton;
        }

        private static void AcceptMission(DirectAgent myAgent)
        {
            try
            {
                if (!PressViewButtonIfItExists("AcceptMission", myAgent)) return;

                if (!PressRequestButtonIfItExists("AcceptMission", myAgent)) return;

                if (!PressCompleteButtonIfItExists("AcceptMission", myAgent)) return;

                if (!PressAcceptButtonIfItExists("AcceptMission", myAgent)) return;

                PrepareStatsWhenStartingNewMission(myAgent);
                Log.WriteLine("AcceptMission: [" + myAgent.Name + "] We must already have a mission and can't yet complete it. Setting AgentInteractionState to Done");
                ChangeAgentInteractionState(AgentInteractionState.Done, myAgent, false);
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static bool AgentStandingsCheck(DirectAgent myAgent)
        {
            try
            {
                if (myAgent == null) return true;
                if (myAgent.Level == 1) return true;

                //
                // Standings Check: if this is a totally new agent this check will timeout after 20 seconds
                //
                if (DateTime.UtcNow < _agentStandingsCheckTimeOut)
                {
                    if (myAgent.MaximumStandingUsedToAccessAgent == (float)0.00)
                    {
                        if (!_agentStandingsCheckFlag)
                        {
                            _agentStandingsCheckTimeOut = DateTime.UtcNow.AddSeconds(10);
                            _agentStandingsCheckFlag = true;
                        }

                        Log.WriteLine(" Agent [" + myAgent.Name + "] Standings show as [" +
                                      myAgent.MaximumStandingUsedToAccessAgent + " and must not yet be available. retrying for [" +
                                      Math.Round(_agentStandingsCheckTimeOut.Subtract(DateTime.UtcNow).TotalSeconds, 0) + " sec]");
                        return false;
                    }

                    _agentStandingsCheckTimeOut = DateTime.UtcNow.AddMinutes(-1);
                    Log.WriteLine("MaximumStandingUsedToAccessAgent: " + Math.Round(myAgent.MaximumStandingUsedToAccessAgent, 2));
                    Log.WriteLine("[Personal]" + Math.Round(myAgent.AgentEffectiveStandingtoMe, 2) + " [Corp]" + Math.Round(myAgent.AgentCorpEffectiveStandingtoMe, 2) + " [Faction]" +
                                  Math.Round(myAgent.AgentFactionEffectiveStandingtoMe, 2));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool BoolGoToBaseNeeded(DirectAgent myAgent)
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                return false;

            const string textToLookForFromAgent = strMyOfficesAreLocatedAt;
            return FindAgentAgentSaysText(textToLookForFromAgent, myAgent);
        }

        private static void ChangeLastButtonPushed(AgentButtonType fromButton, AgentButtonType toButton, DirectAgent myAgent)
        {
            if (LastButtonPushedPerAgentId.ContainsKey(myAgent.AgentId))
                if (LastButtonPushedPerAgentId[myAgent.AgentId] == fromButton)
                {
                    Log.WriteLine("LastButtonPushed changed [" + fromButton + "] to [" + toButton + "]");
                    LastButtonPushedPerAgentId.AddOrUpdate(myAgent.AgentId, toButton);
                }
        }

        private static bool CheckForAgentDeclineTimer(DirectAgent myAgent)
        {
            // Check for agent decline timer
            string briefingHtml = myAgent.AgentWindow.Briefing;
            try
            {
                if (!string.IsNullOrEmpty(briefingHtml) && myAgent != null && myAgent.Mission != null && myAgent.Mission.Faction != null)
                {
                    Statistics.SaveMissionHtmlDetails(briefingHtml, myAgent.Mission.Name + "-Briefing-", myAgent.Mission.Faction.Name);
                }
            }
            catch (Exception){}

            if (!string.IsNullOrEmpty(briefingHtml) && briefingHtml.Contains("Declining a mission from this agent within the next"))
            {
                Regex hourRegex = new Regex("\\s(?<hour>\\d+)\\shour");
                Regex minuteRegex = new Regex("\\s(?<minute>\\d+)\\sminute");
                Match hourMatch = hourRegex.Match(briefingHtml);
                Match minuteMatch = minuteRegex.Match(briefingHtml);
                int hours = 0;
                int minutes = 0;
                if (hourMatch.Success)
                {
                    string hourValue = hourMatch.Groups["hour"].Value;
                    hours = Convert.ToInt32(hourValue);
                }
                if (minuteMatch.Success)
                {
                    string minuteValue = minuteMatch.Groups["minute"].Value;
                    minutes = Convert.ToInt32(minuteValue);
                }

                //
                // standings are below the blacklist minimum
                // (any lower and we might lose access to this agent)
                // and no other agents are NOT available (or are also in cool-down)
                //
                if (MissionSettings.WaitDecline)
                {
                    //
                    // if true we ALWAYS wait (or switch agents?!?)
                    //
                    if (DateTime.UtcNow > _waitUntilThisTimeToTryToDeclineAnyOtherMissions)
                    {
                        _waitUntilThisTimeToTryToDeclineAnyOtherMissions = DateTime.UtcNow.AddMinutes(minutes);
                        _waitUntilThisTimeToTryToDeclineAnyOtherMissions = _waitUntilThisTimeToTryToDeclineAnyOtherMissions.AddHours(hours);
                        if (DateTime.UtcNow > _waitUntilThisTimeToTryToDeclineAnyOtherMissions)
                            _waitUntilThisTimeToTryToDeclineAnyOtherMissions = DateTime.UtcNow.AddMinutes(ESCache.Instance.RandomNumber(1, 4));
                    }

                    Log.WriteLine("Waiting [" + _waitUntilThisTimeToTryToDeclineAnyOtherMissions + "] minutes to before trying decline again because waitDecline setting is set to true (and we would have otherwise lost standing!)");
                    ChangeAgentInteractionState(AgentInteractionState.WaitForDeclineTimerToExpire, myAgent);
                    return false;
                }

                return true;
            }

            return true;
        }

        private static void ClearMissionSpecificSettings()
        {
            Log.WriteLine("ClearMissionSpecificSettings");
            MissionSettings.ClearMissionSpecificSettings();
            _waitToDeclineThisMissionWaitUntilWeWillNotHarmStandings = false;
        }

        private static DirectAgentButton CloseButton(DirectAgent myAgent)
        {
            DirectAgentButton _closeButton = FindAgentResponse(AgentButtonType.CLOSE, myAgent);
            if (_closeButton == null)
                ChangeLastButtonPushed(AgentButtonType.CLOSE, AgentButtonType.None, myAgent);

            return _closeButton;
        }

        private static DirectAgentButton CompleteButton(DirectAgent myAgent)
        {
            DirectAgentButton _completeButton = FindAgentResponse(AgentButtonType.COMPLETE_MISSION, myAgent);
            if (_completeButton == null)
            {
                if (LastButtonPushedPerAgentId.ContainsKey(myAgent.AgentId))
                {
                    AgentButtonType tempButton;
                    LastButtonPushedPerAgentId.TryGetValue(myAgent.AgentId, out tempButton);
                    if (tempButton == AgentButtonType.COMPLETE_MISSION)
                    {
                        GatherMissionStatistics(myAgent);
                        Log.WriteLine("Saying [Complete Mission] ISKMissionReward [" + Statistics.ISKMissionReward +
                                      "] LoyaltyPointsForCurrentMission [" +
                                      Statistics.LoyaltyPointsForCurrentMission + "]");
                        //DirectEvent
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.COMPLETE_MISSION, "Completing mission."));
                        ChangeAgentInteractionState(AgentInteractionState.Done, null, false);
                        return null;
                    }
                }

                ChangeLastButtonPushed(AgentButtonType.COMPLETE_MISSION, AgentButtonType.None, myAgent);
                return null;
            }

            return _completeButton;
        }

        private static DirectAgentButton DeclineButton(DirectAgent myAgent)
        {
            DirectAgentButton _declineButton = FindAgentResponse(AgentButtonType.DECLINE, myAgent);
            if (_declineButton == null)
                ChangeLastButtonPushed(AgentButtonType.DECLINE, AgentButtonType.None, myAgent);

            return _declineButton;
        }

        private static void DeclineMission(DirectAgent myAgent)
        {
            try
            {
                if (!OpenJournalWindow()) return;

                if (_waitToDeclineThisMissionWaitUntilWeWillNotHarmStandings)
                    if (_waitUntilThisTimeToTryToDeclineAnyOtherMissions > DateTime.UtcNow)
                    {
                        Log.WriteLine("Waiting [" + _waitUntilThisTimeToTryToDeclineAnyOtherMissions + "] minutes to before trying decline again because waitDecline setting is set to true (and we would have otherwise lost standing!)");
                        ChangeAgentInteractionState(AgentInteractionState.WaitForDeclineTimerToExpire, myAgent);
                        return;
                    }

                if (!PressRequestButtonIfItExists("DeclineMission", myAgent)) return;

                if (!CheckForAgentDeclineTimer(myAgent)) return;

                if (!RemoveStorylineMission(myAgent)) return;

                if (!PressDeclineButtonIfItExists("DeclineMission", myAgent)) return;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static DirectAgentButton DelayButton(DirectAgent myAgent)
        {
            DirectAgentButton _delayButton = FindAgentResponse(AgentButtonType.DELAY, myAgent);
            if (_delayButton == null)
                ChangeLastButtonPushed(AgentButtonType.DELAY, AgentButtonType.None, myAgent);

            return _delayButton;
        }

        private static bool FindAgentAgentSaysText(string textToLookForFromAgent, DirectAgent myAgent)
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                return false;

            if (myAgent != null)
            {
                if (myAgent.AgentWindow != null)
                {
                    if (!myAgent.AgentWindow.BriefingEmpty)
                    {
                        bool? _tempResponse = myAgent.AgentWindow.Briefing.Contains(textToLookForFromAgent);
                        if (_tempResponse != null)
                            return (bool)_tempResponse;

                        return false;
                    }

                    return false;
                }

                return false;
            }

            return false;
        }

        private static DirectAgentButton FindAgentResponse(AgentButtonType buttonToLookForInAgentWindow, DirectAgent myAgent)
        {
            if (DateTime.UtcNow < _lastAgentWindowInteraction.AddMilliseconds(ESCache.Instance.RandomNumber(400, 1000)))
                return null;

            if (myAgent != null)
            {
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                {
                    if (myAgent.CareerAgentWindow != null)
                    {
                        DirectAgentButton _tempResponse = myAgent.CareerAgentWindow.Buttons.Find(i => i.Type == buttonToLookForInAgentWindow);
                        if (_tempResponse != null)
                            return _tempResponse;
                    }

                    return null;
                }

                if (myAgent.AgentWindow != null)
                {
                    DirectAgentButton _tempResponse = myAgent.AgentWindow.Buttons.Find(i => i.Type == buttonToLookForInAgentWindow);
                    if (_tempResponse != null)
                        return _tempResponse;
                }

                return null;
            }

            return null;
        }

        private static void GatherMissionStatistics(DirectAgent myAgent)
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
            {
                return;
            }

            // let's try to get the isk and lp value here again
            int lpCurrentMission = 0;
            int iskFinishedMission = 0;
            Statistics.ISKMissionReward = 0;
            Statistics.LoyaltyPointsForCurrentMission = 0;
            Regex iskRegex = new Regex(@"([0-9]+)((\.([0-9]+))*) ISK", RegexOptions.Compiled);
            foreach (Match itemMatch in iskRegex.Matches(myAgent.AgentWindow.Objective))
            {
                int val = 0;
                int.TryParse(Regex.Match(itemMatch.Value.Replace(".", ""), @"\d+").Value, out val);
                iskFinishedMission += val;
            }

            Regex lpRegex = new Regex(@"([0-9.]+) Loyalty Points", RegexOptions.Compiled);
            foreach (Match itemMatch in lpRegex.Matches(myAgent.AgentWindow.Objective))
            {
                int val = 0;
                int.TryParse(Regex.Match(itemMatch.Value.Replace(".", ""), @"\d+").Value, out val);
                lpCurrentMission += val;
            }

            Statistics.LoyaltyPointsForCurrentMission = lpCurrentMission;
            Statistics.ISKMissionReward = iskFinishedMission;
        }

        private static bool IsThisBookmarkLocatedInHighSec(DirectBookmark bookmarkToCheck)
        {
            if (DebugConfig.DebugDecline) Log.WriteLine("bookmark: System: [" + bookmarkToCheck.SolarSystem.Name + "] Security [" + bookmarkToCheck.SolarSystem.GetSecurity() + "] IsHighSecuritySpace [" + bookmarkToCheck.SolarSystem.IsHighSecuritySpace + "]");
            if (bookmarkToCheck.SolarSystem.IsHighSecuritySpace)
            {
                if (DebugConfig.DebugDecline) Log.WriteLine("bookmark: if (bookmarkToCheck.SolarSystem.IsHighSecuritySpace)");

                if (bookmarkToCheck.SolarSystem.Id == ESCache.Instance.DirectEve.Session.SolarSystem.Id)
                {
                    if (DebugConfig.DebugDecline) Log.WriteLine("bookmark: we are in high sec space and in local with the bookmark");
                    return true;
                }

                if (bookmarkToCheck.SolarSystem.Name == ESCache.Instance.DirectEve.Session.SolarSystem.Name)
                {
                    if (DebugConfig.DebugDecline) Log.WriteLine("bookmark: we are in high sec space and in local with the bookmark.");
                    return true;
                }

                if (bookmarkToCheck.SolarSystem.JumpsHighSecOnly > 0)
                {
                    if (DebugConfig.DebugDecline) Log.WriteLine("bookmark: the bookmark is in high sec space and our path to that system is all high sec [" + bookmarkToCheck.SolarSystem.JumpsHighSecOnly + "] jumps");
                    return true;
                }

                Log.WriteLine("bookmark: System: [" + bookmarkToCheck.SolarSystem.Name + "] Is High Sec but it would appear we have to travel through lowsec to get there!");
                //this would mean we had to travel through lowsec!
                return false;
            }

            Log.WriteLine("bookmark: System: [" + bookmarkToCheck.SolarSystem.Name + "] Is Low Sec!!!");
            return false;
        }

        private static bool? IsThisMissionLocatedInLowSec(DirectAgentMission myMission)
        {
            try
            {
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                    return false;

                if (myMission != null)
                {
                    //regular mission
                    if (!MissionSettings.CourierMission(myMission))
                    {
                        if (DebugConfig.DebugDecline) Log.WriteLine("IsThisMissionLocatedInLowSec: This mission is a regular (non-courier) mission");
                        DirectBookmark missionBookmark = myMission.Bookmarks.FirstOrDefault();
                        if (missionBookmark != null)
                            return !IsThisBookmarkLocatedInHighSec(missionBookmark);

                        if (DebugConfig.DebugDecline) Log.WriteLine("There are No Bookmarks Associated with [" + Log.FilterPath(myMission.Name) + "] yet");
                    }

                    //courier mission
                    if (MissionSettings.CourierMission(myMission))
                    {
                        Log.WriteLine("IsThisMissionLocatedInLowSec: This mission is a courier mission");

                        bool? tempBool = myMission.RouteContainsLowSecuritySystems;
                        if (tempBool == null)
                            return null;

                        if (tempBool == true)
                            return tempBool;

                        tempBool = myMission.LowSecWarning;
                        if (tempBool == null)
                            return null;

                        if (tempBool == true)
                            return tempBool;

                        return false;
                    }

                    if (!ESCache.Instance.Windows.OfType<DirectAgentWindow>().Any(w => w.AgentId == myMission.Agent.AgentId))
                    {
                        Log.WriteLine("IsThisMissionLocatedInLowSec: if (Agent.Window == null)");
                        if (!myMission.Agent.OpenAgentWindow(true)) return null;
                        return null;
                    }

                    if (ESCache.Instance.Windows.OfType<DirectAgentWindow>().Any(w => w.AgentId == myMission.Agent.AgentId) && myMission.Agent.AgentWindow.ObjectiveEmpty)
                    {
                        if (myMission.Agent.AgentWindow.ViewMode == "SinglePaneView" && myMission.Agent.AgentWindow.Buttons.Any(i => i.Type == AgentButtonType.VIEW_MISSION))
                            if (DateTime.UtcNow > Time.Instance.NextWindowAction)
                            {
                                if (myMission.Agent.AgentWindow.Buttons.Find(button => button.Type == AgentButtonType.VIEW_MISSION).Click())
                                {
                                    Log.WriteLine("if (Agent.Window.Buttons.FirstOrDefault(button => button.Type == ButtonType.VIEW_MISSION).Click())");
                                    Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(1);
                                    return null;
                                }
                            }

                        return null;
                    }

                    Log.WriteLine(myMission.Agent.AgentWindow.Objective);

                    if (myMission.Agent.AgentWindow.Objective.Contains("The route generated by current autopilot settings contains low security systems!") || myMission.Agent.AgentWindow.Objective.Contains("(Low Sec Warning!)") || myMission.Agent.AgentWindow.Objective.Contains("Low Sec"))
                    {
                        if (Purpose != AgentInteractionPurpose.RemoteMissionAmmoCheck) Log.WriteLine("[" + myMission.Name + "] is located in low security space!");
                        return true;
                    }

                    /**
                    if (!MissionSettings.CourierMission)
                    {
                        DirectBookmark missionBookmark = MissionSettings.RegularMission.Bookmarks.FirstOrDefault();
                        if (missionBookmark != null)
                            Log.WriteLine("mission bookmark: System: [" + missionBookmark.LocationId + "]");
                        else
                            Log.WriteLine("There are No Bookmarks Associated with [" + Log.FilterPath(MissionSettings.RegularMission.Name) + "] yet");

                        if (MissionSettings.RegularMission.Agent.Window.Objective.Contains("The route generated by current autopilot settings contains low security systems!") || MissionSettings.RegularMission.Agent.Window.Objective.Contains("(Low Sec Warning!)"))
                        {
                            if (Purpose != AgentInteractionPurpose.RemoteMissionAmmoCheck) Log.WriteLine("[" + MissionSettings.RegularMission.Name + "] is located in low security space!");
                            return true;
                        }

                        return false;
                    }
                    **/

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool NoMoreMissionsAvailable(DirectAgent myAgent)
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
            {
                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.COMPLETE_MISSION))
                    return false;

                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.ACCEPT))
                    return false;

                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.QUIT_MISSION))
                    return false;

                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.DELAY))
                    return false;

                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.REQUEST_MISSION))
                    return false;

                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.VIEW_MISSION))
                    return false;

                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.CLOSE))
                {
                    return true;
                }

                return false;
            }

            bool tempNoMoreJobs = FindAgentAgentSaysText(strNoJobsAvailable, myAgent);
            if (tempNoMoreJobs) return true;

            tempNoMoreJobs = FindAgentAgentSaysText(strCareerAgentCompletedAllCareerMissions, myAgent);
            if (tempNoMoreJobs) return true;

            tempNoMoreJobs = FindAgentAgentSaysText(strReferral, myAgent);
            if (tempNoMoreJobs) return true;

            //
            // there are likely missions available
            //
            return false;
        }

        private static bool PleaseDropBy(DirectAgent myAgent)
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                return false;

            const string textToLookForFromAgent = "Please drop by";
            return FindAgentAgentSaysText(textToLookForFromAgent, myAgent);
        }

        private static void PrepareForOfferedMission(DirectAgentMission myMission)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("PrepareForOfferedMission: InSpace [" + ESCache.Instance.InSpace + "] return");
                    return;
                }

                if (myMission == null || !myMission.Agent.IsValid)
                {
                    Log.WriteLine("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                    return;
                }

                if (myMission != null)
                {
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastMissionName), myMission.Name);
                    Log.WriteLine("[" + myMission.Agent.Name + "] effective standing is [" + myMission.Agent.MaximumStandingUsedToAccessAgent + "] and toward me is [" + myMission.Agent.AgentEffectiveStandingtoMeText +
                                  "], minAgentGreyListStandings: [" +
                                  MissionSettings.MinAgentGreyListStandings + "]");

                    bool? tempBool = ShouldWeDeclineThisMission(myMission);
                    if (tempBool == null) return;
                    if ((bool)tempBool)
                    {
                        ChangeAgentInteractionState(AgentInteractionState.DeclineMission, myMission.Agent, false);
                        return;
                    }

                    if (!string.IsNullOrEmpty(myMission.GetAgentMissionRawCsvHint()))
                    {
                        Log.WriteLine("PrepareForOfferedMission: --------------------------[" + Log.FilterPath(myMission.Name) + "] Objective Info----------------------------");
                        Log.WriteLine("PrepareForOfferedMission: MissionSettings.RegularMission.State [" + myMission.State + "]");
                        Log.WriteLine(
                            "agentMissionInfo for [" + Log.FilterPath(myMission.Name) + "] is [" + myMission.GetAgentMissionRawCsvHint() + "] while we are still in station.");
                        Statistics.SaveMissionPocketObjectives(myMission.GetAgentMissionRawCsvHint(), Log.FilterPath(myMission.Name), 0);
                        Log.WriteLine("PrepareForOfferedMission: ---------------------------------------------------------------------------");
                    }

                    if (!MissionSettings.CourierMission(myMission))
                    {
                        ClearMissionSpecificSettings();
                        // we want to clear this every time, not only if the xml exists. else we run into troubles with faction damagetype selection

                        if (File.Exists(MissionSettings.MissionXmlPath(myMission)))
                        {
                            MissionSettings.LoadMissionXmlData(myMission);
                        }
                        else
                        {
                            Log.WriteLine("Missing mission xml [" + myMission.Name + "] from [" + MissionSettings.MissionXmlPath(myMission) + "] !!!");
                            MissionSettings.MissionXMLIsAvailable = false;
                            if (MissionSettings.RequireMissionXML)
                            {
                                Log.WriteLine("Stopping Questor because RequireMissionXML is true in your character XML settings");
                                Log.WriteLine("You will need to create a mission XML for [" + myMission.Name + "]");
                                State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                                ControllerManager.Instance.SetPause(true);
                                return;
                            }
                        }

                        if (Purpose == AgentInteractionPurpose.RemoteMissionAmmoCheck)
                        {
                            Purpose = AgentInteractionPurpose.StartMission;
                            Log.WriteLine("RemoteMissionAmmoCheck: Done");
                            ChangeAgentInteractionState(AgentInteractionState.Done, myMission.Agent, false);
                            return;
                        }
                    }

                    Log.WriteLine("PrepareForOfferedMission: RegularMission State [" + myMission.State + "]");

                    //
                    // do we need to check and make sure the agent is in the same station as us before we attempt to accept a mission?!
                    //
                    if (myMission.State == MissionState.Offered)
                    {
                        if (MissionSettings.CourierMission(myMission))
                        {
                            if (ESCache.Instance.CurrentShipsCargo == null) return;
                            if (MissionSettings.M3NeededForCargo(myMission) == null) return;
                            if (ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
                            {
                                if (ESCache.Instance.CurrentShipsCargo.FreeCapacity != null && ESCache.Instance.CurrentShipsCargo.FreeCapacity > MissionSettings.M3NeededForCargo(myMission))
                                {
                                    Log.WriteLine("Courier:  [" + Math.Round(ESCache.Instance.CurrentShipsCargo.FreeCapacity ?? 0, 0) + "] Cargo Capacity and the Courier Mission needs [" + MissionSettings.M3NeededForCargo(myMission) + "]: Accepting [" + myMission.Name + "]");
                                    ChangeAgentInteractionState(AgentInteractionState.AcceptMission, myMission.Agent, false);
                                    return;
                                }

                                Log.WriteLine("Courier: [" + Math.Round(ESCache.Instance.CurrentShipsCargo.FreeCapacity ?? 0, 0) + "] Cargo Capacity and the Courier Mission needs [" + MissionSettings.M3NeededForCargo(myMission) + "]: Deferring [" + myMission.Name + "] until later");
                                CourierMissionCtrl.ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.NotEnoughCargoRoom);
                                ChangeAgentInteractionState(AgentInteractionState.Done, myMission.Agent, false);
                                return;
                            }
                        }

                        Log.WriteLine("Accepting mission [" + myMission.Name + "]");
                        ChangeAgentInteractionState(AgentInteractionState.AcceptMission, myMission.Agent, false);
                        return;
                    }
                    // If we already accepted the mission, close the conversation

                    if (myMission.State == MissionState.Accepted)
                    {
                        Log.WriteLine("PrepareForOfferedMission: Done with AgentInteraction");
                        ChangeAgentInteractionState(AgentInteractionState.Done, myMission.Agent);
                        return;
                    }

                    if (myMission.State == MissionState.OfferExpired)
                    {
                        Log.WriteLine("PrepareForOfferedMission: MissionState is OfferExpired: we should extend this to delete this mission from the journal");
                        ChangeAgentInteractionState(AgentInteractionState.Done, myMission.Agent, false);
                        return;
                    }

                    return;
                }

                Log.WriteLine("PrepareForOfferedMission: RegularMission == null: Changing AgentInteractionState to Idle");
                ChangeAgentInteractionState(AgentInteractionState.Idle, myMission.Agent, false);
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void PrepareStatsWhenStartingNewMission(DirectAgent myAgent)
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
            {
                return;
            }

            // statsp
            int lpCurrentMission = 0;
            int iskFinishedMission = 0;
            Statistics.ISKMissionReward = 0;
            Statistics.LoyaltyPointsForCurrentMission = 0;
            Regex iskRegex = new Regex(@"([0-9]+)((\.([0-9]+))*) ISK", RegexOptions.Compiled);
            foreach (Match itemMatch in iskRegex.Matches(myAgent.AgentWindow.Objective))
            {
                int val = 0;
                const string thousandsSeperator = ",";
                int.TryParse(Regex.Match(itemMatch.Value.Replace(thousandsSeperator, ""), @"\d+").Value, out val);
                iskFinishedMission += val;
            }

            Regex lpRegex = new Regex(@"([0-9]+) Loyalty Points", RegexOptions.Compiled);
            foreach (Match itemMatch in lpRegex.Matches(myAgent.AgentWindow.Objective))
            {
                int val = 0;
                int.TryParse(Regex.Match(itemMatch.Value, @"\d+").Value, out val);
                lpCurrentMission += val;
            }

            Statistics.LoyaltyPointsTotal = myAgent.LoyaltyPoints ?? 0;
            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LoyaltyPoints), myAgent.LoyaltyPoints);
            Statistics.LoyaltyPointsForCurrentMission = lpCurrentMission;
            Statistics.ISKMissionReward = iskFinishedMission;
            ESCache.Instance.Wealth = ESCache.Instance.DirectEve.Me.Wealth ?? 0;
            Log.WriteLine("ISK finished mission [" + iskFinishedMission + "] LoyalityPoints [" + lpCurrentMission + "]");
        }

        private static bool PressAgentWindowButton(DirectAgentButton ButtonToPress)
        {
            if (ButtonToPress.Click())
            {
                _lastAgentWindowInteraction = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        private static bool PressDeclineButtonIfItExists(string StateForLogs, DirectAgent myAgent)
        {
            if (myAgent == null)
            {
                Log.WriteLine("[" + StateForLogs + "]: the Agent we were passed is null, how?");
                return true;
            }

            if (!myAgent.OpenAgentWindow(true)) return false;

            if (myAgent.Mission == null)
            {
                Log.WriteLine("[" + StateForLogs + "]: we have no mission yet?!");
                if (!PressRequestButtonIfItExists(StateForLogs, myAgent)) return false;
                return true;
            }

            bool boolIWeAreDecliningaStorylineMission = myAgent.Mission.Important;

            if (DeclineButton(myAgent) != null && myAgent.Mission != null)
            {
                if (Time.Instance.LastDeclineMissionAttempt.AddSeconds(5) > DateTime.UtcNow)
                {
                    Log.WriteLine("Decline button exists: but could not be pushed yet: waiting");
                    return true;
                }

                Log.WriteLine("[" + StateForLogs + "]: Found [ Decline Mission ] button. ButtonName [" + DeclineButton(myAgent).ButtonName + "] Text [" + DeclineButton(myAgent).Text+ "] Type [" + DeclineButton(myAgent).Type + "]");
                if (PressAgentWindowButton(DeclineButton(myAgent)))
                {
                    LastButtonPushedPerAgentId.AddOrUpdate(myAgent.AgentId, AgentButtonType.DECLINE);
                    Time.Instance.LastDeclineMissionAttempt = DateTime.UtcNow;
                    ClearMissionSpecificSettings();
                    _waitUntilThisTimeToTryToDeclineAnyOtherMissions = DateTime.UtcNow.AddMinutes(120);
                    Statistics.WriteMissionAcceptDeclineStatsLog(true, false, myAgent.AgentEffectiveStandingtoMe, myAgent.AgentCorpEffectiveStandingtoMe, myAgent.AgentFactionEffectiveStandingtoMe, _lastMissionDecline, MissionSettings.LastReasonMissionAttemptedToBeDeclined);

                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DECLINE_MISSION, "Declining mission."));
                    if (boolIWeAreDecliningaStorylineMission)
                    {
                        MissionSettings.StorylineInstance.Reset();
                        State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                        State.CurrentCourierMissionBehaviorState = CourierMissionsBehaviorState.Idle;
                        ChangeAgentInteractionState(AgentInteractionState.Done, myAgent, false);
                        return true;
                    }

                    ChangeAgentInteractionState(AgentInteractionState.StartConversation, myAgent, false);
                    return false;
                }

                Log.WriteLine("[" + StateForLogs + "]: Pressing [ Decline Mission ] Button Failed!");
                return false;
            }

            if (DeclineButton(myAgent) == null)
                return true;

            //
            // if the decline button still exists, but we have clicked the button in the last few seconds wait for the button to go away before attempting to proceed or push the button again
            //
            Log.WriteLine("Decline button exists: but could not be pushed for some reason");
            return false;
        }

        private static bool PressRequestButtonIfItExists(string StateForLogs, DirectAgent myAgent)
        {
            if (myAgent == null)
            {
                Log.WriteLine("[" + StateForLogs + "]: the Agent we were passed is null, how?");
                return false;
            }

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
            {
                if (myAgent.CareerAgentWindow.Buttons.Any(i => i.Type == AgentButtonType.NO_JOBS_AVAILABLE))
                {
                    myAgent.CareerAgentWindow.Close();
                    //ESCache.Instance.CloseQuestor(true, "NO_JOBS_AVAILABLE");
                    ESCache.Instance.PauseAfterNextDock = true;
                    return false;
                }
            }
            else if (myAgent.AgentWindow.Buttons.Any(i => i.Type == AgentButtonType.NO_JOBS_AVAILABLE))
            {
                myAgent.AgentWindow.Close();
                //ESCache.Instance.CloseQuestor(true, "NO_JOBS_AVAILABLE");
                ESCache.Instance.PauseAfterNextDock = true;
                return false;
            }

            if (!ESCache.Instance.InStation || myAgent.StationId != ESCache.Instance.DirectEve.Session.StationId)
            {
                Log.WriteLine("We are not in station with the agent [" + myAgent.Name + "][" + myAgent.StationName + "]");
                if (ESCache.Instance.InStation) Log.WriteLine("We are in [" + ESCache.Instance.DirectEve.Session.Station.Name + "]");
                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.Storyline)
                {
                    CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                    ChangeAgentInteractionState(AgentInteractionState.Done, myAgent);
                    return false;
                }

                //we are trying to go do a storyline dont want to push any buttons... (right?)
                return true;
            }

            if (!myAgent.OpenAgentWindow(true)) return false;

            if (RequestButton(myAgent) != null)
            {
                Log.WriteLine("[" + StateForLogs + "]: Found [ Request Mission ] button.");
                if (PressAgentWindowButton(RequestButton(myAgent)))
                {
                    LastButtonPushedPerAgentId.AddOrUpdate(myAgent.AgentId, AgentButtonType.REQUEST_MISSION);
                    return false;
                }

                Log.WriteLine("[" + StateForLogs + "]: Pressing [ Requesting Mission ] Button Failed!");
                return false;
            }

            return true;
        }

        private static DirectAgentButton QuitButton(DirectAgent myAgent)
        {
            DirectAgentButton _quitButton = FindAgentResponse(AgentButtonType.QUIT_MISSION, myAgent);
            if (_quitButton == null)
                ChangeLastButtonPushed(AgentButtonType.QUIT_MISSION, AgentButtonType.None, myAgent);

            return _quitButton;
        }

        private static bool RemoveStorylineMission(DirectAgent myAgent)
        {
            if (State.CurrentStorylineState == StorylineState.DeclineMission || State.CurrentStorylineState == StorylineState.AcceptMission)
            {
                DirectJournalWindow jw = ESCache.Instance.DirectEve.Windows.OfType<DirectJournalWindow>().FirstOrDefault();

                if (jw == null)
                {
                    Log.WriteLine("Opening journal.");
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenJournal);
                    return false;
                }

                if (jw.SelectedMainTab != MainTab.AgentMissions)
                {
                    Log.WriteLine("Journal window mission tab is not selected. Switching the tab.");
                    jw.SwitchMaintab(MainTab.AgentMissions);
                    return false;
                }

                if (myAgent.Mission != null && myAgent.Mission.Important)
                {
                    Log.WriteLine("Storyline: Removing Storyline [" + myAgent.Mission.Name + "] from the mission journal");
                    myAgent.Mission.RemoveOffer();
                }

                Log.WriteLine("Storyline: Setting StorylineState.Done");
                State.CurrentStorylineState = StorylineState.Done;
                State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Storyline;
                ChangeAgentInteractionState(AgentInteractionState.Idle, myAgent, false);
                return false;
            }

            return true;
        }

        private static void ReplyToAgent(DirectAgent myAgent)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("ReplyToAgent: InSpace [" + ESCache.Instance.InSpace + "] return");
                    return;
                }

                if (!myAgent.OpenAgentWindow(true))
                {
                    Log.WriteLine("ReplyToAgent: if (!myAgent [" + myAgent.Name + "].OpenAgentWindow(true))");
                    return;
                }

                //
                // Read the possibly responses and make sure we are 'doing the right thing' - set AgentInteractionPurpose to fit the state of the agent window
                //
                if (PleaseDropBy(myAgent) || BoolGoToBaseNeeded(myAgent))
                {
                    Log.WriteLine("agent [" + myAgent.Name + "] if (PleaseDropBy || MissionSettings.AgentToPullNextRegularMissionFrom.Window.Html.Contains(Please drop by) || boolGoToBaseNeeded)");
                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.Storyline)
                        CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);

                    ChangeAgentInteractionState(AgentInteractionState.Idle, myAgent, false);
                    return;
                }

                if (NoMoreMissionsAvailable(myAgent))
                {
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                    {
                        boolSwitchAgents = true;
                        boolNoMissionsAvailable = true;
                        Log.WriteLine("agent [" + myAgent.Name + "] has no missions available. Switching to next CareerAgent");
                        myAgent.CareerAgentWindow.Close();
                        MissionSettings.TrackCareerAgentsWithNoMissionsAvailable(myAgent);
                        return;
                    }

                    if (myAgent.StorylineAgent)
                    {
                        Log.WriteLine("Storyline agent [" + myAgent.Name + "] has no more missions available for now. resetting to the use the normal agent.");
                        myAgent.AgentWindow.Close();
                        MissionSettings.ClearMissionSpecificSettings();
                        return;
                    }

                    Log.WriteLine("Pausing: agent [" + myAgent.Name + "] has no missions available. Define more / different agents in the character XML.");
                    myAgent.AgentWindow.Close();
                    ControllerManager.Instance.SetPause(true);
                    return;
                }

                if (RequestButton(myAgent) == null &&
                    CompleteButton(myAgent) == null &&
                    ViewButton(myAgent) == null &&
                    AcceptButton(myAgent) == null &&
                    DeclineButton(myAgent) == null &&
                    DelayButton(myAgent) == null &&
                    QuitButton(myAgent) == null)
                    try
                    {
                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                        {
                            Log.WriteLine("Agent Name [" + myAgent.Name + "]");
                            //Log.WriteLine("Agent.Window: WindowState [" + myAgent.CareerAgentWindow.WindowState + "]");
                            Log.WriteLine("Agent.Window: IsReady [" + myAgent.CareerAgentWindow.IsReady + "]");
                            Log.WriteLine("Agent.Window: Ready [" + myAgent.CareerAgentWindow.Ready + "]");
                            Log.WriteLine("Agent.Window: Buttons Count [" + myAgent.CareerAgentWindow.Buttons.Count + "]");
                            if (myAgent.CareerAgentWindow.Buttons.Any())
                            {
                                foreach (var item in myAgent.CareerAgentWindow.Buttons)
                                {
                                    Log.WriteLine("Agent.Window: Button [" + item.ButtonName + "] Text [" + item.Text + "] Type [" + item.Type + "]");
                                }
                            }
                        }
                        else
                        {
                            Log.WriteLine("Agent Name [" + myAgent.Name + "]");
                            Log.WriteLine("Agent.Window: WindowState [" + myAgent.AgentWindow.WindowState + "]");
                            Log.WriteLine("Agent.Window: IsReady [" + myAgent.AgentWindow.IsReady + "]");
                            Log.WriteLine("Agent.Window: Ready [" + myAgent.AgentWindow.Ready + "]");
                            Log.WriteLine("Agent.Window: Buttons Count [" + myAgent.AgentWindow.Buttons.Count + "]");
                            if (myAgent.AgentWindow.Buttons.Any())
                            {
                                foreach (var item in myAgent.AgentWindow.Buttons)
                                {
                                    Log.WriteLine("Agent.Window: Button [" + item.ButtonName + "] Text [" + item.Text + "] Type [" + item.Type + "]");
                                }
                            }
                            //Log.WriteLine("Agent.Window: AgentSays [" + myAgent.Window.AgentSays + "]");
                            Log.WriteLine("Agent.Window: Briefing [" + myAgent.AgentWindow.Briefing + "]");
                            Log.WriteLine("Agent.Window: Objective [" + myAgent.AgentWindow.Objective + "]");
                            Log.WriteLine("Agent.Window: Caption [" + myAgent.AgentWindow.Caption + "]");
                            Log.WriteLine("Agent.Window: Html [" + myAgent.AgentWindow.Html + "]");
                            Log.WriteLine("Agent.Window: ViewMode [" + myAgent.AgentWindow.ViewMode + "]");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                if (myAgent.StationId == ESCache.Instance.DirectEve.Session.StationId) //do not change the AgentInteractionPurpose if we are checking which ammo type to use.
                {
                    if (DebugConfig.DebugAgentInteractionReplyToAgent)
                        Log.WriteLine(
                            "if (Purpose != AgentInteractionPurpose.AmmoCheck) //do not change the AgentInteractionPurpose if we are checking which ammo type to use.");

                    //
                    // if we arent checking the mission details for ammo purposes then
                    //
                    if (AcceptButton(myAgent) != null && DeclineButton(myAgent) != null)
                        if (myAgent.StationId == ESCache.Instance.DirectEve.Session.StationId)
                        {
                            Log.WriteLine("Found accept and decline buttons: Changing to AgentInteractionState.WeHaveAMissionWaiting");
                            ChangeAgentInteractionState(AgentInteractionState.WeHaveAMissionWaiting, myAgent, false);
                            return;
                        }

                    /**
                    if (completeButton != null && quitButton != null && closeButton != null && Statistics.MissionCompletionErrors == 0)
                        if (Purpose != AgentInteractionPurpose.CompleteMission)
                        {
                            Log.WriteLine("ReplyToAgent: Found complete button, Changing Purpose to CompleteMission");

                            //we have a mission in progress here, attempt to complete it
                            if (DateTime.UtcNow > _agentWindowTimeStamp.AddSeconds(30))
                                Purpose = AgentInteractionPurpose.CompleteMission;
                        }
                    **/

                    //if (requestButton != null && closeButton != null)
                }

                if (!PressViewButtonIfItExists("ReplyToAgent: [" + myAgent.Name + "] ViewButton", myAgent)) return;

                if (!PressRequestButtonIfItExists("ReplyToAgent: [" + myAgent.Name + "] RequestButton", myAgent)) return;

                if (!PressCompleteButtonIfItExists("ReplyToAgent: [" + myAgent.Name + "] CompleteButton", myAgent)) return;

                if (NoMoreMissionsAvailable(myAgent))
                {
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                    {
                        boolSwitchAgents = true;
                        boolNoMissionsAvailable = true;
                        Log.WriteLine("agent [" + myAgent.Name + "] has no missions available. Switching to next CareerAgent");
                        myAgent.CareerAgentWindow.Close();
                        MissionSettings.TrackCareerAgentsWithNoMissionsAvailable(myAgent);
                        return;
                    }

                    /**
                    if (myAgent.StorylineAgent)
                    {
                        Log.WriteLine("Storyline agent [" + myAgent.Name + "] has no more missions available for now. resetting to the use the normal agent.");
                        myAgent.AgentWindow.Close();
                        MissionSettings.ClearMissionSpecificSettings();
                        return;
                    }

                    Log.WriteLine("Pausing: agent [" + myAgent.Name + "] has no missions available. Define more / different agents in the character XML.");
                    myAgent.AgentWindow.Close();
                    ControllerManager.Instance.SetPause(true);
                    return;
                    **/
                }

                Log.WriteLine("ReplyToAgent: [" + myAgent.Name + "] We must be doing a mission and can't yet complete it. Setting AgentInteractionState to Done");
                if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Storyline && State.CurrentStorylineState == StorylineState.AcceptMission)
                    Storyline.ChangeStorylineState(StorylineState.Arm);
                ChangeAgentInteractionState(AgentInteractionState.Done, myAgent, true);
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static DirectAgentButton RequestButton(DirectAgent myAgent)
        {
            DirectAgentButton _requestButton = FindAgentResponse(AgentButtonType.REQUEST_MISSION, myAgent);
            if (_requestButton == null)
                ChangeLastButtonPushed(AgentButtonType.REQUEST_MISSION, AgentButtonType.None, myAgent);

            return _requestButton;
        }

        private static void StartConversation(DirectAgent myAgent)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("StartConversation: InSpace [" + ESCache.Instance.InSpace + "] return");
                    return;
                }

                if (!AgentStandingsCheck(myAgent)) return;

                if (Purpose == AgentInteractionPurpose.RemoteMissionAmmoCheck)
                {
                    Log.WriteLine("RemoteMissionAmmoCheck: Checking ammo type");
                    ChangeAgentInteractionState(AgentInteractionState.WeHaveAMissionWaiting, myAgent, false);
                }
                else
                {
                    Log.WriteLine("Replying to agent");
                    ChangeAgentInteractionState(AgentInteractionState.ReplyToAgent, myAgent, false);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static DirectAgentButton ViewButton(DirectAgent myAgent)
        {
            DirectAgentButton _viewButton = FindAgentResponse(AgentButtonType.VIEW_MISSION, myAgent);
            if (_viewButton == null)
                ChangeLastButtonPushed(AgentButtonType.VIEW_MISSION, AgentButtonType.None, myAgent);

            return _viewButton;
        }

        private static bool WaitOnAgentWindowButtonResponse(DirectAgent myAgent)
        {
            const bool waitForButtonsToChange = true;
            const bool buttonsHaveChanged = true;
            const bool timeoutReached = true;

            if (DateTime.UtcNow > _lastAgentWindowInteraction.AddSeconds(5))
                return timeoutReached;

            AgentButtonType lastButtonPushed = AgentButtonType.None;
            if (LastButtonPushedPerAgentId.ContainsKey(myAgent.AgentId))
                LastButtonPushedPerAgentId.TryGetValue(myAgent.AgentId, out lastButtonPushed);

            if (DebugConfig.DebugAgentInteractionReplyToAgent) Log.WriteLine("LastButtonPushed [" + lastButtonPushed + "]");

            switch (lastButtonPushed)
            {
                case AgentButtonType.None:
                    if (!myAgent.OpenAgentWindow(true)) return false;

                    if (myAgent.AgentWindow != null)
                    {
                        if (myAgent.AgentWindow.Buttons.Count > 0)
                            return buttonsHaveChanged;
                    }

                    if (myAgent.CareerAgentWindow != null)
                    {
                        if (myAgent.CareerAgentWindow.Buttons.Count > 0)
                            return buttonsHaveChanged;
                    }

                    return false;

                case AgentButtonType.ACCEPT:
                    return waitForButtonsToChange;

                case AgentButtonType.COMPLETE_MISSION:
                    return waitForButtonsToChange;

                case AgentButtonType.DECLINE:
                    return waitForButtonsToChange;

                case AgentButtonType.REQUEST_MISSION:
                    return waitForButtonsToChange;

                case AgentButtonType.VIEW_MISSION:
                    return waitForButtonsToChange;
            }

            return false;
        }

        /**
        //
        // find the name of the station we are in so we can use it to align to the station if need be during a mission
        //
        private static string StationNameWeWereInWhenWeLastAcceptedaMission;
        private static void NoteWhichStationWeAreIn()
        {
            //StationNameWeWereInWhenWeLastAcceptedaMission =
            foreach (EntityCache station in ESCache.Instance.Stations)
            {
                station.Id;
            }
        }
        **/

        private static void WeHaveAMissionWaiting(DirectAgent myAgent)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("WeHaveAMissionWaiting: InSpace [" + ESCache.Instance.InSpace + "] return");
                    return;
                }

                if (!OpenJournalWindow()) return;

                if (DebugConfig.DebugAgentInteractionReplyToAgent)
                    Log.WriteLine("WeHaveAMissionWaiting: JournalWindow is open");

                if (!PressRequestButtonIfItExists("WaitForMission: RequestButton", myAgent)) return;

                if (DebugConfig.DebugAgentInteractionReplyToAgent)
                    Log.WriteLine("WeHaveAMissionWaiting: PressRequestButtonIfItExists finished");

                if (!PressViewButtonIfItExists("WaitForMission: ViewButton", myAgent)) return;

                if (DebugConfig.DebugAgentInteractionReplyToAgent)
                    Log.WriteLine("WeHaveAMissionWaiting: PressViewButtonIfItExists finished");

                if (myAgent.Mission == null)
                {
                    if (DebugConfig.DebugAgentInteractionReplyToAgent)
                        Log.WriteLine("WeHaveAMissionWaiting:if (myAgent.Mission == null)");

                    if (DateTime.UtcNow.Subtract(_waitingOnMissionTimer).TotalSeconds > 30)
                    {
                        Log.WriteLine("WaitForMission: Unable to find mission from that agent (yet?) : AgentInteraction.AgentId [" +
                                      myAgent.AgentId + "]");
                        JournalWindow.Close();
                        if (DateTime.UtcNow.Subtract(_waitingOnMissionTimer).TotalSeconds > 120)
                        {
                            const string msg =
                                "AgentInteraction: WaitforMission: Journal would not open/refresh - mission was null: restarting EVE Session";
                            Log.WriteLine(msg);
                            ESCache.Instance.CloseEveReason = msg;
                            ESCache.Instance.BoolRestartEve = true;
                        }
                    }

                    return;
                }

                Log.WriteLine("RegularMission names [" + myAgent.Mission.Name + "] found in journal.");
                ChangeAgentInteractionState(AgentInteractionState.PrepareForOfferedMission, myAgent, false);
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Methods
    }
}