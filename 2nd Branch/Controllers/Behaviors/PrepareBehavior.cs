extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Caching;
using EVESharpCore.Questor.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Traveller;
using SC::SharedComponents.Events;

namespace EVESharpCore.Questor.Behaviors
{
    public class PrepareBehavior
    {
        private static DateTime _lastPulse;

        private static double _lastX;
        private static double _lastY;
        private static double _lastZ;

        public PrepareBehavior()
        {
            _lastPulse = DateTime.MinValue;
            ResetStatesToDefaults();
        }

        private static bool ResetStatesToDefaults()
        {
            _States.CurrentPrepareState = PrepareState.Idle;
            return true;
        }

        public static bool ChangePrepareBehaviorState(PrepareState prepareStateToSet, bool wait = false ,string logMessage = null)
        {
            try
            {
                if (_States.CurrentPrepareState != prepareStateToSet)
                {
                    Log.WriteLine("New PrepareBehaviorState [" + prepareStateToSet.ToString() + "]");
                    _States.CurrentPrepareState = prepareStateToSet;
                    if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        private static void IdlePrepareState()
        {
            if (QCache.Instance.InSpace)
            {
                //
                // In this case where should we assume we should go?
                //
                ChangePrepareBehaviorState(PrepareState.GotoBase);
                return;
            }

            _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
            _States.CurrentArmState = ArmState.Idle;
            _States.CurrentDroneState = DroneState.Idle;
            _States.CurrentSalvageState = SalvageState.Idle;
            _States.CurrentTravelerState = TravelerState.AtDestination;
            _States.CurrentUnloadLootState = UnloadLootState.Idle;

            if (Settings.Instance.AutoStart)
            {
                ChangePrepareBehaviorState(PrepareState.CheckPrerequisitesForLvl4CombatMissionShips);
                return;
            }
        }

        private static bool AttemptToBuyAmmo()
        {
            if (Settings.Instance.BuyAmmo)
                if (BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.Done && BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.DisabledForThisSession)
                {
                    if (DateTime.UtcNow > QCache.Instance.EveAccount.LastAmmoBuy.AddHours(8) && DateTime.UtcNow > QCache.Instance.EveAccount.LastBuyLpItemAttempt.AddHours(1))
                    {
                        QCache.Instance.InjectorWcfClient.GetPipeProxy.SetEveAccountAttributeValue(QCache.Instance.CharName, "LastAmmoBuyAttempt", DateTime.UtcNow);
                        ControllerManager.Instance.AddController(new BuyAmmoController());
                        return false;
                    }
                }

            return true;
        }

        private static void GotoBaseCMBState()
        {
            Salvage.CurrentlyShouldBeSalvaging = false;

            if (QCache.Instance.InSpace && !QCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(QCache.Instance.BigObjectsandGates.FirstOrDefault(), "CombatMissionsBehaviorState.GotoBase");
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: Traveler.TravelHome()");

            Traveler.TravelHome("CombatMissionsBehavior.TravelHome");

            if (_States.CurrentTravelerState == TravelerState.AtDestination && QCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: We are at destination");

                if (Settings.Instance.BuyPlex && BuyPlexController.ShouldBuyPlex)
                {
                    BuyPlexController.CheckBuyPlex();
                    ChangePrepareBehaviorState(PrepareState.Idle);
                    return;
                }

                if (QCache.Instance.Agent.AgentId != 0)
                    try
                    {
                        MissionSettings.UpdateMissionName(QCache.Instance.Agent.AgentId);
                    }
                    catch (Exception exception)
                    {
                        Log.WriteLine("Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID); [" + exception + "]");
                    }

                if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
                {
                    Log.WriteLine("CMB: if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)");
                    Traveler.Destination = null;
                    ChangePrepareBehaviorState(PrepareState.Error, true);
                    return;
                }

                if (_States.CurrentCombatState != CombatState.OutOfAmmo && MissionSettings.Mission != null &&
                    MissionSettings.Mission.State == (int) MissionState.Accepted)
                {
                    Traveler.Destination = null;
                    //ChangePrepareBehaviorState(PrepareState.CompleteMission, true);
                    return;
                }

                Traveler.Destination = null;
                ChangePrepareBehaviorState(PrepareState.UnloadLoot, true);
                return;
            }

            return;
        }

        private static void TravelerCMBState()
        {
            try
            {
                if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
                {
                    if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                var destination = QCache.Instance.DirectEve.Navigation.GetDestinationPath();
                if (destination == null || destination.Count == 0)
                {
                    Log.WriteLine("No destination?");
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

                if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                    destination[0] = QCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.LastOrDefault())
                {
                    if (QCache.Instance.AllBookmarks != null && QCache.Instance.AllBookmarks.Any())
                    {
                        IEnumerable<DirectBookmark> bookmarks = QCache.Instance.AllBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.FirstOrDefault() != null && bookmarks.Any())
                        {
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                            return;
                        }

                        Log.WriteLine("Destination: [" + QCache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                        var lastSolarSystemInRoute = destination.LastOrDefault();


                        Log.WriteLine("Destination: [" + lastSolarSystemInRoute + "]");
                        Traveler.Destination = new SolarSystemDestination(destination.LastOrDefault());
                        return;
                    }

                    return;
                }

                if (_States.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
                    {
                        Log.WriteLine("an error has occurred");
                        ChangePrepareBehaviorState(PrepareState.Error, true);
                        return;
                    }

                    if (QCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangePrepareBehaviorState(PrepareState.Error, true);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangePrepareBehaviorState(PrepareState.Idle, true);
                    return;
                }

                Traveler.ProcessState();
                return;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return;
            }
        }

        private static bool? CheckSkillsForTypeId(int TypeId, string TypeName)
        {
            try
            {
                if (DateTime.UtcNow > Time.Instance.LastRefreshMySkills.AddSeconds(10))
                {
                    QCache.Instance.DirectEve.Skills.RefreshMySkills();
                    Time.Instance.LastRefreshMySkills = DateTime.UtcNow;
                    return null;
                }

                if (!QCache.Instance.DirectEve.Skills.AreMySkillsReady)
                {
                    return null;
                }

                List<Tuple<int, int>> listofSkillsStillneeded = new List<Tuple<int, int>>();
                listofSkillsStillneeded = QCache.Instance.DirectEve.Skills.GetRequiredSkillsForType(TypeId);
                if (listofSkillsStillneeded == null)
                {
                    Log.WriteLine("Prepare: if (listofSkillsStillneeded == null)");
                    return false;
                }

                if (listofSkillsStillneeded != null && listofSkillsStillneeded.Any())
                {
                    Log.WriteLine("Prepare: We lack [" + listofSkillsStillneeded.Count + "] skills to fly a [" + TypeName + "][" + TypeId + "]");
                    return false;
                }

                Log.WriteLine("Prepare: We have all of the skill prerequisites to fly a [" + TypeName + "][" + TypeId + "]");
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool? BoolDoWeHaveSkillsForOurDefinedLvl4MissionShips
        {
            get
            {
                foreach (var factionfitting in MissionSettings.ListofLvl4FactionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShip = CheckPrerequisitesForaCombatMissionShip(factionfitting.FittingName, 4);
                    if (boolDoWeHaveSkillsForOurDefinedShip != null && !(bool)boolDoWeHaveSkillsForOurDefinedShip)
                    {
                        return false;
                    }

                    continue;
                }

                foreach (var missionfitting in MissionSettings.ListOfLvl4MissionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShip = CheckPrerequisitesForaCombatMissionShip(missionfitting.FittingName, 4);
                    if (boolDoWeHaveSkillsForOurDefinedShip != null && !(bool)boolDoWeHaveSkillsForOurDefinedShip)
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }
        }

        private static bool? BoolDoWeHaveSkillsForOurDefinedLvl4MissionFittings
        {
            get
            {
                foreach (var missionfitting in MissionSettings.ListOfLvl4MissionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShipFittings = CheckPrerequisitesForaCombatMissionShipFittings(missionfitting.FittingName, 4);
                    if (boolDoWeHaveSkillsForOurDefinedShipFittings != null && !(bool)boolDoWeHaveSkillsForOurDefinedShipFittings)
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }
        }

        private static bool? BoolDoWeHaveSkillsForOurDefinedLvl3MissionShip
        {
            get
            {
                foreach (var factionfitting in MissionSettings.ListofLvl3FactionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShip = CheckPrerequisitesForaCombatMissionShip(factionfitting.FittingName, 3);
                    if (boolDoWeHaveSkillsForOurDefinedShip != null && !(bool)boolDoWeHaveSkillsForOurDefinedShip)
                    {
                        return false;
                    }

                    continue;
                }

                foreach (var missionfitting in MissionSettings.ListOfLvl3MissionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShip = CheckPrerequisitesForaCombatMissionShip(missionfitting.FittingName, 3);
                    if (boolDoWeHaveSkillsForOurDefinedShip != null && !(bool)boolDoWeHaveSkillsForOurDefinedShip)
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }
        }

        private static bool? BoolDoWeHaveSkillsForOurDefinedLvl3MissionShipFittings
        {
            get
            {
                foreach (var factionfitting in MissionSettings.ListofLvl3FactionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShipFittings = CheckPrerequisitesForaCombatMissionShipFittings(factionfitting.FittingName, 3);
                    if (boolDoWeHaveSkillsForOurDefinedShipFittings != null && !(bool)boolDoWeHaveSkillsForOurDefinedShipFittings)
                    {
                        return false;
                    }

                    continue;
                }

                foreach (var missionfitting in MissionSettings.ListOfLvl3MissionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShipFittings = CheckPrerequisitesForaCombatMissionShipFittings(missionfitting.FittingName, 3);
                    if (boolDoWeHaveSkillsForOurDefinedShipFittings != null && !(bool)boolDoWeHaveSkillsForOurDefinedShipFittings)
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }
        }

        private static bool? BoolDoWeHaveSkillsForOurDefinedLvl2MissionShip
        {
            get
            {
                foreach (var factionfitting in MissionSettings.ListofLvl2FactionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShip = CheckPrerequisitesForaCombatMissionShip(factionfitting.FittingName, 2);
                    if (boolDoWeHaveSkillsForOurDefinedShip != null && !(bool)boolDoWeHaveSkillsForOurDefinedShip)
                    {
                        return false;
                    }

                    continue;
                }

                foreach (var missionfitting in MissionSettings.ListOfLvl2MissionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShip = CheckPrerequisitesForaCombatMissionShip(missionfitting.FittingName, 2);
                    if (boolDoWeHaveSkillsForOurDefinedShip != null && !(bool)boolDoWeHaveSkillsForOurDefinedShip)
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }
        }

        private static bool? BoolDoWeHaveSkillsForOurDefinedLvl2MissionShipFittings
        {
            get
            {
                foreach (var factionfitting in MissionSettings.ListofLvl2FactionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShipFittings = CheckPrerequisitesForaCombatMissionShipFittings(factionfitting.FittingName, 2);
                    if (boolDoWeHaveSkillsForOurDefinedShipFittings != null && !(bool)boolDoWeHaveSkillsForOurDefinedShipFittings)
                    {
                        return false;
                    }

                    continue;
                }

                foreach (var missionfitting in MissionSettings.ListOfLvl2MissionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShipFittings = CheckPrerequisitesForaCombatMissionShipFittings(missionfitting.FittingName, 2);
                    if (boolDoWeHaveSkillsForOurDefinedShipFittings != null && !(bool)boolDoWeHaveSkillsForOurDefinedShipFittings)
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }
        }

        private static bool? BoolDoWeHaveSkillsForOurDefinedLvl1MissionShip
        {
            get
            {
                foreach (var factionfitting in MissionSettings.ListofLvl1FactionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShip = CheckPrerequisitesForaCombatMissionShip(factionfitting.FittingName, 1);
                    if (boolDoWeHaveSkillsForOurDefinedShip != null && !(bool)boolDoWeHaveSkillsForOurDefinedShip)
                    {
                        return false;
                    }

                    continue;
                }

                foreach (var missionfitting in MissionSettings.ListOfLvl1MissionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShip = CheckPrerequisitesForaCombatMissionShip(missionfitting.FittingName, 1);
                    if (boolDoWeHaveSkillsForOurDefinedShip != null && !(bool)boolDoWeHaveSkillsForOurDefinedShip)
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }
        }

        private static bool? BoolDoWeHaveSkillsForOurDefinedLvl1MissionShipFittings
        {
            get
            {
                foreach (var factionfitting in MissionSettings.ListofLvl1FactionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShipFittings = CheckPrerequisitesForaCombatMissionShipFittings(factionfitting.FittingName, 1);
                    if (boolDoWeHaveSkillsForOurDefinedShipFittings != null && !(bool)boolDoWeHaveSkillsForOurDefinedShipFittings)
                    {
                        return false;
                    }

                    continue;
                }

                foreach (var missionfitting in MissionSettings.ListOfLvl1MissionFittings)
                {
                    bool? boolDoWeHaveSkillsForOurDefinedShipFittings = CheckPrerequisitesForaCombatMissionShipFittings(missionfitting.FittingName, 1);
                    if (boolDoWeHaveSkillsForOurDefinedShipFittings != null && !(bool)boolDoWeHaveSkillsForOurDefinedShipFittings)
                    {
                        return false;
                    }

                    continue;
                }

                return true;
            }
        }

        private static bool? CheckPrerequisitesForaCombatMissionShip(string stringCombatShipFitting, int LevelOfMission)
        {
            try
            {
                DirectFitting CombatShipFitting = null;
                if (MissionSettings.LookForFitting(stringCombatShipFitting, LevelOfMission) != null)
                {
                    if (MissionSettings.ListOfInGameSavedFittings != null && MissionSettings.ListOfInGameSavedFittings.Any())
                    {
                        CombatShipFitting = MissionSettings.ListOfInGameSavedFittings.FirstOrDefault(i => i.Name == stringCombatShipFitting);
                        if (CombatShipFitting != null)
                        {
                            //
                            // What ship do I need to check?
                            //
                            bool? boolCheckSkillsForShipType = CheckSkillsForTypeId(CombatShipFitting.ShipTypeId, CombatShipFitting.Name);

                            if (boolCheckSkillsForShipType == null)
                            {
                                //
                                // wait a few seconds and try again.
                                //
                                return null;
                            }

                            if ((bool)!boolCheckSkillsForShipType)
                            {
                                //
                                // prereqs not met
                                //
                                return false;
                            }

                            if ((bool)boolCheckSkillsForShipType)
                            {
                                //
                                // we have the prereqs
                                //
                                return true;
                            }

                            return false;
                        }

                        return false;
                    }

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

        private static DirectFitting _combatShipFittingLastMatched = null;

        private static bool? CheckPrerequisitesForaCombatMissionShipFittings(string stringCombatShipFitting, int LevelOfMission)
        {
            try
            {
                DirectFitting CombatShipFitting = null;
                if (MissionSettings.LookForFitting(stringCombatShipFitting, LevelOfMission) != null)
                {
                    if (MissionSettings.ListOfInGameSavedFittings != null && MissionSettings.ListOfInGameSavedFittings.Any())
                    {
                        CombatShipFitting = MissionSettings.ListOfInGameSavedFittings.FirstOrDefault(i => i.Name == stringCombatShipFitting);
                        if (CombatShipFitting != null)
                        {
                            //
                            // What ship do I need to check?
                            //
                            foreach (DirectItem module in CombatShipFitting.Modules)
                            {
                                bool? boolCheckSkillsForShipType = CheckSkillsForTypeId(module.TypeId, module.TypeName);

                                if (boolCheckSkillsForShipType == null)
                                {
                                    //
                                    // wait a few seconds and try again.
                                    //
                                    return null;
                                }

                                if ((bool)!boolCheckSkillsForShipType)
                                {
                                    //
                                    // prereqs not met
                                    //
                                    return false;
                                }

                                if ((bool)boolCheckSkillsForShipType)
                                {
                                    //
                                    // we have the prereqs, check the next module
                                    //
                                    continue;
                                }
                            }

                            //
                            // if we have checked all modules and didnt return false, all modules must have the prereqs
                            //

                            _combatShipFittingLastMatched = CombatShipFitting;
                            return true;
                        }

                        return false;
                    }

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

        public static float AgentStandingNeeded(int agentLevel)
        {
            switch (agentLevel)
            {
                case 1:
                    return AgentInteraction.StandingsNeededToAccessLevel1Agent;
                case 2:
                    return AgentInteraction.StandingsNeededToAccessLevel2Agent;
                case 3:
                    return AgentInteraction.StandingsNeededToAccessLevel3Agent;
                case 4:
                    return AgentInteraction.StandingsNeededToAccessLevel4Agent;
                case 5:
                    return AgentInteraction.StandingsNeededToAccessLevel5Agent;

                return AgentInteraction.StandingsNeededToAccessLevel4Agent;
            }

            return 0;
        }

        public static DirectAgent FindDefinedAgentOfThisLevelThatWeHAveStandingsToUse(int agentLevel = 4)
        {
            if (MissionSettings.ListOfDirectAgents.Any(i => i.Level == agentLevel))
            {
                foreach (DirectAgent thisDirectAgent in MissionSettings.ListOfDirectAgents.Where(i => i.Level == agentLevel && QCache.Instance.AgentBlacklist.All(x => x != i.AgentId)).OrderByDescending(o => o.LoyaltyPoints))
                {
                    if (thisDirectAgent.Level == agentLevel)
                    {
                        float myEffectiveStandings = MaximumStandingUsedToAccessAgent(thisDirectAgent);
                        if (myEffectiveStandings > AgentStandingNeeded(agentLevel))
                        {
                            Log.WriteLine("We have enough standings to use [" + thisDirectAgent.Name + "] Level [" + thisDirectAgent.Level + "][" + thisDirectAgent.FactionName + "][" + thisDirectAgent.DivisionName + "] LP [" + thisDirectAgent.LoyaltyPoints + "] Standings [" + myEffectiveStandings + "]");
                            return thisDirectAgent;
                        }

                        Log.WriteLine("We lack standings to use [" + thisDirectAgent.Name + "] Level [" + thisDirectAgent.Level + "][" + thisDirectAgent.FactionName + "][" + thisDirectAgent.DivisionName + "] LP [" + thisDirectAgent.LoyaltyPoints + "] Standings [" + myEffectiveStandings + "]");
                        continue;
                    }

                    continue;
                }

                Log.WriteLine("No Agents were found in the list of agents that we have standings to use. Add some lower level agents or agents we have better standings with.");
                return null;
            }

            Log.WriteLine("No Agents were found in the list of agents!");
            return null;
        }

        private static float? _agentEffectiveStandingtoMe = null;

        public static float AgentEffectiveStandingtoMe(DirectAgent myAgent)
        {
            try
            {
                if (!QCache.Instance.InSpace && !QCache.Instance.InStation)
                    return 0;

                if (_agentEffectiveStandingtoMe == null)
                {
                    _agentEffectiveStandingtoMe = QCache.Instance.DirectEve.Standings.EffectiveStanding(myAgent.AgentId, QCache.Instance.DirectEve.Session.CharacterId ?? -1);
                    QCache.Instance.InjectorWcfClient.GetPipeProxy.SetEveAccountAttributeValue(QCache.Instance.CharName, "AgentStandings", _agentEffectiveStandingtoMe);
                }

                return (float)_agentEffectiveStandingtoMe;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        private static float? _agentCorpEffectiveStandingtoMe = null;

        public static float AgentCorpEffectiveStandingtoMe(DirectAgent myAgent)
        {
            try
            {
                if (!QCache.Instance.InSpace && !QCache.Instance.InStation)
                    return 0;

                if (_agentCorpEffectiveStandingtoMe == null)
                {
                    _agentCorpEffectiveStandingtoMe = QCache.Instance.DirectEve.Standings.EffectiveStanding(myAgent.CorpId, QCache.Instance.DirectEve.Session.CharacterId ?? -1);
                    QCache.Instance.InjectorWcfClient.GetPipeProxy.SetEveAccountAttributeValue(QCache.Instance.CharName, "AgentCorpStandings", _agentCorpEffectiveStandingtoMe);
                }

                return (float)_agentCorpEffectiveStandingtoMe;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        private static float? _agentFactionEffectiveStandingtoMe = null;

        public static float AgentFactionEffectiveStandingtoMe(DirectAgent myAgent)
        {
            try
            {
                if (!QCache.Instance.InSpace && !QCache.Instance.InStation)
                    return 0;

                if (_agentFactionEffectiveStandingtoMe == null)
                {
                    _agentFactionEffectiveStandingtoMe = QCache.Instance.DirectEve.Standings.EffectiveStanding(myAgent.FactionId, QCache.Instance.DirectEve.Session.CharacterId ?? -1);
                    QCache.Instance.InjectorWcfClient.GetPipeProxy.SetEveAccountAttributeValue(QCache.Instance.CharName, "AgentFactionStandings", _agentFactionEffectiveStandingtoMe);
                }

                return (float)_agentFactionEffectiveStandingtoMe;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        private static float? _maximumStandingUsedToAccessAgent = null;

        public static float MaximumStandingUsedToAccessAgent(DirectAgent myAgent)
        {
            try
            {
                if (!QCache.Instance.InSpace && !QCache.Instance.InStation)
                    return 0;

                if (_maximumStandingUsedToAccessAgent == null)
                {
                    _maximumStandingUsedToAccessAgent = Math.Max(AgentEffectiveStandingtoMe(myAgent), Math.Max(AgentCorpEffectiveStandingtoMe(myAgent), AgentFactionEffectiveStandingtoMe(myAgent)));
                    QCache.Instance.InjectorWcfClient.GetPipeProxy.SetEveAccountAttributeValue(QCache.Instance.CharName, "StandingUsedToAccessAgent", _maximumStandingUsedToAccessAgent);
                }

                return (float)_maximumStandingUsedToAccessAgent;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        private static bool PrepareEveryPulse()
        {
            if (!QCache.Instance.InSpace && !QCache.Instance.InStation)
                return false;

            if (QCache.Instance.InStation && QCache.Instance.PauseAfterNextDock)
            {
                if (DebugConfig.DebugCombatMissionsBehavior) Log.WriteLine("PrepareBehavior: EveryPulse: if (QCache.Instance.InStation && QCache.Instance.PauseAfterNextDock)");
                ControllerManager.Instance.SetPause(true);
                QCache.Instance.PauseAfterNextDock = false;
                return false;
            }

            Panic.ProcessState();

            if (_States.CurrentPanicState == PanicState.Resume)
            {
                if (QCache.Instance.InSpace || QCache.Instance.InStation)
                {
                    _States.CurrentPanicState = PanicState.Normal;
                    _States.CurrentTravelerState = TravelerState.Idle;
                    ChangePrepareBehaviorState(PrepareState.GotoBase);
                    return true;
                }

                return false;
            }

            return true;
        }

        private static MissionFitting myMissingMissionFitting = null;

        private static void CheckPrerequisitesForLvl4CombatMissionShipState()
        {
            if (BoolDoWeHaveSkillsForOurDefinedLvl4MissionShips == null)
            {
                return;
            }

            if (BoolDoWeHaveSkillsForOurDefinedLvl4MissionShips != null && (bool)BoolDoWeHaveSkillsForOurDefinedLvl4MissionShips)
            {
                //
                // we can fly the ship, check the fittings
                //
                if (BoolDoWeHaveSkillsForOurDefinedLvl4MissionFittings == null)
                {
                    return;
                }

                if (BoolDoWeHaveSkillsForOurDefinedLvl4MissionFittings != null && (bool)BoolDoWeHaveSkillsForOurDefinedLvl4MissionFittings)
                {
                    //
                    // we can fly the ship and have the skills for the fittings!
                    //

                    //
                    // agent standings?
                    //
                    try
                    {
                        DirectAgent myLevel4Agent = FindDefinedAgentOfThisLevelThatWeHAveStandingsToUse(4);
                        if (myLevel4Agent != null)
                        {
                            if (QCache.Instance.DirectEve.Session.LocationId == myLevel4Agent.StationId)
                            {
                                myMissingMissionFitting = null;
                                foreach (MissionFitting fitting in MissionSettings.ListOfLvl4MissionFittings)
                                {
                                    bool boolFoundFitting = false;
                                    foreach (DirectItem ship in QCache.Instance.ShipHangar.Items.Where(i => i.GivenName != null))
                                    {
                                        if (ship.GivenName.ToLower() == fitting.FittingName.ToLower())
                                        {
                                            boolFoundFitting = true;
                                            Log.WriteLine("Found Ship [" + ship.GivenName + "] type [" + ship.TypeName + "] for Fitting [" + fitting.FittingName + "] useb by mission [" + fitting.MissionName + "]");
                                            break;
                                        }
                                    }

                                    if (!boolFoundFitting)
                                    {
                                        myMissingMissionFitting = fitting;
                                        Log.WriteLine("Missing Ship for fitting [" + fitting.FittingName + "] ship [" + fitting.Ship + "] used by mission [" + fitting.MissionName + "]");
                                        ChangePrepareBehaviorState(PrepareState.BuyFittingsForaLvl4CombatMissionShip);
                                        return;
                                    }

                                    continue;
                                }

                                if (!Arm.ActivateShip(Combat.Combat.Level4CombatShipName))
                                {
                                    if (_States.CurrentArmState == ArmState.NotEnoughAmmo)
                                    {
                                        //
                                        // we do not have the correct ship and we need one.
                                        //
                                        ChangePrepareBehaviorState(PrepareState.BuyFittingsForaLvl4CombatMissionShip);
                                        return;
                                    }

                                    //
                                    // we are in the correct ship
                                    //
                                    ChangePrepareBehaviorState(PrepareState.CheckItemHangarForAmmoAndDrones);
                                    return;
                                }

                                //
                                // we are in the correct ship
                                //

                                ChangePrepareBehaviorState(PrepareState.CheckItemHangarForAmmoAndDrones);
                                return;
                            }

                            //
                            // go to market hub and buy the ship and fittings if needed.
                            //
                        }
                        else //null!
                        {
                            Log.WriteLine("Prepare: We could not find a level [" + 4 + "] agent in the agentlist that we have the standings to access.]");
                            ChangePrepareBehaviorState(PrepareState.CheckPrerequisitesForLvl3CombatMissionShips);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    return;
                }

                //
                // we do not have the prerequisites for all of the fittings
                //
                Log.WriteLine("Prepare: We are missing skill prerequisites for the fittings for the LVL4 ship defined as [" + Combat.Combat.Level4CombatShipName + "]");
                ChangePrepareBehaviorState(PrepareState.CheckPrerequisitesForLvl3CombatMissionShips);
                return;
            }

            Log.WriteLine("Prepare: We are missing skill prerequisites for the LVL4 ship defined as [" + Combat.Combat.Level4CombatShipName + "]");
            ChangePrepareBehaviorState(PrepareState.CheckPrerequisitesForLvl3CombatMissionShips);
            return;
        }

        public static List<InventoryItem> ListOfItemsToBuyForTheMissingShip = new List<InventoryItem>();

        private static void BuildAListOfItemsWeNeedToBuyForaLvl4CombatMissionShipState()
        {
            if (myMissingMissionFitting.CostToBuy > QCache.Instance.DirectEve.Me.Wealth)
            {
                Log.WriteLine("We are missing a fitting named [" + myMissingMissionFitting.FittingName + "] ship [" + myMissingMissionFitting.Ship + "] cost [" + String.Format("{0:#,##0}", myMissingMissionFitting.CostToBuy) + "] Wallet Balance [" + String.Format("{0:#,##0}", QCache.Instance.DirectEve.Me.Wealth) + "] and cant afford to buy it!");
                ChangePrepareBehaviorState(PrepareState.CheckPrerequisitesForLvl3CombatMissionShips);
                return;
            }

            foreach (var inGameSavedFit in MissionSettings.ListOfInGameSavedFittings)
            {
                if (inGameSavedFit.Name.ToLower() == myMissingMissionFitting.FittingName.ToLower())
                {
                    //
                    // build list of items to buy from the fitting + ship...
                    //
                    foreach (DirectItem item in inGameSavedFit.Modules)
                    {
                        InventoryItem thisInventoryItemModule = new InventoryItem(item.TypeId, item.Quantity, item.TypeName);
                        Log.WriteLine("Add [" + thisInventoryItemModule.Name + "] TypeId [" + thisInventoryItemModule.TypeId + "] to the list of items to buy for the missing ship");
                        ListOfItemsToBuyForTheMissingShip.Add(thisInventoryItemModule);

                        //
                        // check for the item in hte local ahnagar and only add it to the list if needed
                        //
                    }

                    InventoryItem thisInventoryItemShip = new InventoryItem(inGameSavedFit.ShipTypeId, 1, inGameSavedFit.Name);
                    Log.WriteLine("Add [" + thisInventoryItemShip.Name + "] TypeId [" + thisInventoryItemShip.TypeId + "] to the list of items to buy for the missing ship");
                    //
                    // check for a packaged ship in hte local hangar and only add it to the list if needed
                    //
                    ListOfItemsToBuyForTheMissingShip.Add(thisInventoryItemShip);
                }
            }

            if (ListOfItemsToBuyForTheMissingShip.Count() > 1)
            {
                Log.WriteLine("We have [" + ListOfItemsToBuyForTheMissingShip.Count() + "] items in the list of items to buy for the missing ship.");
                ChangePrepareBehaviorState(PrepareState.BuyFittingsForaLvl4CombatMissionShip);
                return;
            }


            return;
        }

        public static void ProcessState()
        {
            try
            {
                if (!PrepareEveryPulse()) return;

                if (DebugConfig.DebugPrepareBehavior) Log.WriteLine("_States.CurrentPrepareBehaviorState is [" + _States.CurrentPrepareState + "]");

                switch (_States.CurrentPrepareState)
                {
                    case PrepareState.Idle:
                        IdlePrepareState();
                        break;

                    case PrepareState.GotoBase:
                        GotoBaseCMBState();
                        break;

                    case PrepareState.CheckPrerequisitesForLvl4CombatMissionShips:
                        CheckPrerequisitesForLvl4CombatMissionShipState();
                        break;

                    case PrepareState.BuildAListOfItemsWeNeedToBuyForaLvl4CombatMissionShip:
                        BuildAListOfItemsWeNeedToBuyForaLvl4CombatMissionShipState();
                        break;

                    case PrepareState.BuyFittingsForaLvl4CombatMissionShip:

                        break;

                    case PrepareState.UnloadLoot:
                        //UnloadLootCMBState();
                        break;

                    case PrepareState.Traveler:
                        TravelerCMBState();
                        break;

                    case PrepareState.GotoNearestStation:
                        //GotoNearestStationCMBState();
                        break;

                    case PrepareState.Default:
                        ChangePrepareBehaviorState(PrepareState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }
    }
}