extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using System;
using System.Collections.Generic;
using System.Linq;
using SC::SharedComponents.EVE;

namespace EVESharpCore.Questor.Storylines
{
    public class Storyline
    {
        #region Constructors

        public Storyline()
        {
            //_combat = new Combat();
            //_agentInteraction = new AgentInteraction();

            MissionSettings.AgentBlacklist = new List<long>();

            _storylines = new Dictionary<string, IStoryline>
            {
                //{"A Fathers Love".ToLower(), new GenericCourier()}, //lvl4
                //{"A Greener World".ToLower(), new GenericCourier()}, //lvl4
                //{"Eradication".ToLower(), new GenericCourier()}, //lvl4
                //{"Evacuation".ToLower(), new GenericCourier()}, //lvl4
                //{"Illegal Mining".ToLower(), new GenericCombatStoryline()}, //caldari lvl4 note: Extremely high DPS after shooting structures!
                //{"Inspired".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                //{"Missing Persons Report", new GenericCombatStoryline()},
                //{"On the Run".ToLower(), new GenericCourier()}, //lvl4
                //{"Send the Marines".ToLower(), new GenericCourier()}, //lvl4
                {"A Cargo With Attitude".ToLower(), new GenericCourier()}, //lvl4
                {"A Case of Kidnapping".ToLower(), new GenericCombatStoryline()}, //lvl1 and lvl4
                {"A Desperate Rescue".ToLower(), new GenericCourier()}, //lvl4
                {"A Different Drone".ToLower(), new GenericCourier()}, //lvl2
                {"A Fathers Love".ToLower(), new GenericCourier()}, //lvl1 note: 300m3 needed
                {"A Fine Wine".ToLower(), new GenericCourier()}, //lvl4
                {"A Force to Be Reckoned With".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"A Greener World".ToLower(), new GenericCourier()}, //lvl1
                {"A Little Work On The Side".ToLower(), new GenericCourier()}, //lvl1
                {"A Load of Scrap".ToLower(), new GenericCourier()}, //lvl4
                {"A Piece of History".ToLower(), new GenericCourier()},
                {"Amarrian Excavators".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Amarrian Tyrants".ToLower(), new GenericCombatStoryline()},
                {"Amphibian Error".ToLower(), new GenericCourier()}, //lvl4
                {"Ancient Treasures".ToLower(), new GenericCourier()}, //lvl1
                {"An End To EavesDropping".ToLower(), new GenericCombatStoryline()}, //lvl1
                {"A Special Delivery".ToLower(), new GenericCourier()}, // Needs 40k m3 cargo (i.e. Iteron Mark V, T2 CHO rigs) for lvl4
                {"A Watchful Eye".ToLower(), new GenericCourier()},
                {"Black Ops Crisis".ToLower(), new GenericCourier()}, //lvl4
                {"Blood Farm".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Brand New Harvesters".ToLower(), new GenericCourier()}, //lvl4
                //{"Cosmic Anomalies (1 of 5)".ToLower(), new GenericCourier()}, //Tutorial Mission for Exploration: involves scanning
                {"Covering Your Tracks".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Crowd Control".ToLower(), new GenericCombatStoryline()}, //caldari lvl4
                {"Culture Clash".ToLower(), new GenericCourier()}, //lvl1
                {"Diplomatic Incident".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Dissidents".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Ditanium".ToLower(), new GenericCourier()},
                {"Divine Intervention".ToLower(), new GenericCourier()},
                {"Eradication".ToLower(), new GenericCourier()}, //lvl1
                {"Evacuation".ToLower(), new GenericCourier()}, //lvl1
                {"Evolution".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Extract the Renegade".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Federal Confidence".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"Fire and Ice".ToLower(), new GenericCourier()}, //lvl4
                {"Forgotten Outpost".ToLower(), new GenericCombatStoryline()}, //caldari lvl4
                {"Gate to Nowhere".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Heart of the Rogue Drone".ToLower(), new GenericCourier()}, //lvl4
                {"Hidden Hope".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"Hunting Black Dog".ToLower(), new GenericCourier()}, //lvl4
                {"Innocents in the Crossfire".ToLower(), new GenericCombatStoryline()}, //caldari lvl4
                {"Jealous Rivals".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Kidnappers Strike - Ambush in the Dark (1 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - Defend the Civilian Convoy (8 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - Incriminating Evidence (5 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - Possible Leads (4 of 10)".ToLower(), new GenericCourier()}, //lvl3
                {"Kidnappers Strike - Retrieve the Prisoners (9 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - The Final Battle (10 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - The Flu Outbreak (6 of 10)".ToLower(), new GenericCourier()}, //lvl3
                {"Kidnappers Strike - The Interrogation (2 of 10)".ToLower(), new GenericCourier()}, //lvl3
                {"Kidnappers Strike - The Kidnapping (3 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - The Secret Meeting (7 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Materials For War Preparation".ToLower(), new MaterialsForWarPreparation()},
                {"Matriarch".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Nine Tenths of the Wormhole".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Of Fangs and Claws".ToLower(), new GenericCourier()},
                {"On the Run".ToLower(), new GenericCourier()}, //lvl1
                {"Operation Doorstop".ToLower(), new GenericCourier()}, //lvl4
                {"Opiate of the Masses".ToLower(), new GenericCourier()}, //lvl4
                {"Patient Zero".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Pieces of the Past".ToLower(), new GenericCourier()}, //lvl1
                {"Postmodern Primitives".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Prison Transfer".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"Quota Season".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Racetrack Ruckus".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Record Cleaning".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Search and Rescue".ToLower(), new GenericCombatStoryline()}, //lvl1
                {"Send the Marines!".ToLower(), new GenericCourier()}, //lvl4
                {"Serpentis Ship Builders".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"Shifting Rocks".ToLower(), new GenericCourier()}, //lvl4
                {"Shipyard Theft".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Soothe the Salvage Beast".ToLower(), new GenericCombatStoryline()}, //lvl3 and lvl4
                {"Stem the Flow".ToLower(), new GenericCombatStoryline()}, //caldari lvl4
                {"The Blood of Angry Men".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"The Creeping Cold".ToLower(), new GenericCourier()}, //lvl4
                {"The Essence of Speed".ToLower(), new GenericCourier()},
                {"The Governors Ball".ToLower(), new GenericCourier()}, //lvl4
                {"The Graduation Certificate".ToLower(), new GenericCourier()},
                {"The Heir\'s Favorite Slave".ToLower(), new GenericCombatStoryline()}, //amarr
                {"The Heirs Favorite Slave".ToLower(), new GenericCombatStoryline()}, //amarr
                {"Their Secret Defense".ToLower(), new GenericCourier()}, //lvl4
                {"The Latest Style".ToLower(), new GenericCourier()}, //lvl1
                {"The Mouthy Merc".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"The Natural Way".ToLower(), new GenericCourier()}, //lvl4
                {"The Serpent and the Slaves".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"The State of the Empire".ToLower(), new GenericCourier()}, //lvl4
                {"Tomb of the Unknown Soldiers".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"Transaction Data Delivery".ToLower(), new TransactionDataDelivery()},
                {"Unmasking the Traitor".ToLower(), new GenericCourier()}, //lvl4
                {"Very Important Pirates".ToLower(), new GenericCourier()}, //lvl1 and 4
                {"Wartime Advances".ToLower(), new GenericCourier()}, //lvl1
                {"Whispers in the Dark - First Contact (1 of 4)".ToLower(), new GenericCombatStoryline()}, //vs sansha lvl2
                {"Whispers in the Dark - Lay and Pray (2 of 4)".ToLower(), new GenericCombatStoryline()}, //vs sansha lvl2
                {"Whispers in the Dark - The Outpost (4 of 4)".ToLower(), new GenericCombatStoryline()} //vs sansha lvl2
            };
        }

        #endregion Constructors

        #region Properties

        public IStoryline StorylineHandler { get; private set; }

        #endregion Properties

        #region Fields

        public static bool HighSecChecked;
        public readonly Dictionary<string, IStoryline> _storylines;
        private static int _highSecCounter;
        private static DateTime _nextAction = DateTime.UtcNow;
        private static bool _setDestinationStation;
        private static DateTime LastOfferRemove = DateTime.MinValue;
        private int _moveCnt;

        #endregion Fields

        #region Methods

        public static void ChangeStorylineState(StorylineState storylineStateToSet)
        {
            try
            {
                if (State.CurrentStorylineState != storylineStateToSet)
                {
                    Log.WriteLine("New StorylineState [" + storylineStateToSet + "]");
                    State.CurrentStorylineState = storylineStateToSet;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool RouteToStorylineAgentIsSafe(long storylineagentStationId, long solarSystemIdToCheck)
        {
            if ((int)solarSystemIdToCheck != ESCache.Instance.DirectEve.Session.SolarSystemId)
            {
                ESCache.Instance.RouteIsAllHighSecBool = true;
                HighSecChecked = true;
                return true;
            }

            if (!HighSecChecked && (int)solarSystemIdToCheck != ESCache.Instance.DirectEve.Session.SolarSystemId)
            {
                // if we haven't already done so, set Eve's autopilot
                if (!_setDestinationStation)
                {
                    if (!Traveler.SetStationDestination(storylineagentStationId))
                    {
                        Log.WriteLine("GotoAgent: Unable to find route to storyline agent. Skipping.");
                        ChangeStorylineState(StorylineState.Done);
                        return false;
                    }

                    _setDestinationStation = true;
                    _nextAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(2, 4));
                    return false;
                }

                // Make sure we have got a clear path to the agent
                if (!ESCache.Instance.CheckIfRouteIsAllHighSec())
                {
                    if (_highSecCounter < 5)
                    {
                        _highSecCounter++;
                        return false;
                    }

                    Log.WriteLine("GotoAgent: CheckIfRouteIsAllHighSec failed to check [" + solarSystemIdToCheck + "] Skipping.");
                    ChangeStorylineState(StorylineState.RemoveOffer);
                    _highSecCounter = 0;
                    return false;
                }

                if (!ESCache.Instance.RouteIsAllHighSecBool)
                {
                    Log.WriteLine("GotoAgent: Route to agent is through low-sec systems. Skipping.");
                    ChangeStorylineState(StorylineState.RemoveOffer);
                    return false;
                }

                HighSecChecked = true;
                return true;
            }

            return true;
        }

        public static bool StatisticsStorylineState()
        {
            if (Drones.UseDrones && ESCache.Instance.ActiveShip != null && !ESCache.Instance.ActiveShip.IsShipWithNoDroneBay)
            {
                DirectInvType drone = ESCache.Instance.DirectEve.GetInvType(Drones.DroneTypeID);
                if (drone != null)
                {
                    if (Drones.DroneBay == null)
                    {
                        Log.WriteLine("StatisticsStorylineState: if (Drones.DroneBay == null)");
                        return false;
                    }
                    Statistics.LostDrones = (int)Math.Floor((Drones.DroneBay.Capacity - (double)Drones.DroneBay.UsedCapacity) / drone.Volume);
                    if (!Statistics.WriteDroneStatsLog()) return false;
                }
                else
                {
                    Log.WriteLine("Could not find the drone TypeID specified in the character settings xml; this should not happen!");
                }
            }

            if (!Statistics.AmmoConsumptionStatistics()) return false;
            Statistics.FinishedMission = DateTime.UtcNow;

            try
            {
                if (!Statistics.MissionLoggingCompleted)
                    if (!Statistics.WriteMissionStatistics()) return false;

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public void ProcessState()
        {
            try
            {
                switch (State.CurrentStorylineState)
                {
                    case StorylineState.Idle:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: Idle;");
                        IdleState();
                        break;

                    case StorylineState.Arm:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: Arm;");
                        ChangeStorylineState(StorylineHandler.Arm(this));
                        break;

                    case StorylineState.BeforeGotoAgent:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: BeforeGotoAgent;");
                        ChangeStorylineState(StorylineHandler.BeforeGotoAgent(this));
                        break;

                    case StorylineState.GotoAgent:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: GotoAgent;");
                        GotoAgent(StorylineState.AcceptMission);
                        break;

                    case StorylineState.PreAcceptMission:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: PreAcceptMission;");
                        State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                        ChangeStorylineState(StorylineHandler.PreAcceptMission(this));
                        break;

                    case StorylineState.DeclineMission:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: DeclineMission;");
                        if (State.CurrentAgentInteractionState == AgentInteractionState.Idle)
                        {
                            Log.WriteLine("Start conversation [Decline Mission]");

                            State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                            AgentInteraction.Purpose = AgentInteractionPurpose.DeclineMission;
                        }

                        if (MissionSettings.StorylineMission != null && MissionSettings.StorylineMission.Agent != null)
                        {
                            AgentInteraction.ProcessState(MissionSettings.StorylineMission.Agent);
                        }
                        else
                        {
                            AgentInteraction.ChangeAgentInteractionState(AgentInteractionState.Done, null);
                        }

                        if (State.CurrentAgentInteractionState == AgentInteractionState.Done)
                            State.CurrentAgentInteractionState = AgentInteractionState.Idle;

                        break;

                    case StorylineState.AcceptMission:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: AcceptMission; CurrentAgentInteractionState [" + State.CurrentAgentInteractionState + "]");
                        //Logging.Log("Storyline: AcceptMission!!-");
                        if (State.CurrentAgentInteractionState == AgentInteractionState.Idle)
                        {
                            Log.WriteLine("Start conversation [Start Mission]");

                            State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                            AgentInteraction.ForceAccept = true;
                        }

                        if (MissionSettings.StorylineMission != null && MissionSettings.StorylineMission.Agent != null)
                        {
                            AgentInteraction.ProcessState(MissionSettings.StorylineMission.Agent);
                        }
                        else
                        {
                            AgentInteraction.ChangeAgentInteractionState(AgentInteractionState.Done, null);
                        }

                        if (State.CurrentAgentInteractionState == AgentInteractionState.Done)
                        {
                            //AgentInteraction.CloseConversation();
                            State.CurrentAgentInteractionState = AgentInteractionState.Idle;

                            // If there is no mission anymore then we're done (we declined it)
                            if (MissionSettings.StorylineMission == null || MissionSettings.StorylineMission != null && !MissionSettings.StorylineMission.Important)
                                State.CurrentStorylineState = StorylineState.Done;

                            if (MissionSettings.StorylineMission != null && MissionSettings.StorylineMission.State == MissionState.Accepted)
                                State.CurrentStorylineState = StorylineState.ExecuteMission;
                        }
                        break;

                    case StorylineState.ExecuteMission:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: ExecuteMission;");
                        ChangeStorylineState(StorylineHandler.ExecuteMission(this));
                        break;

                    case StorylineState.ReturnToAgent:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: ReturnToAgent;");
                        GotoAgent(StorylineState.CompleteMission);
                        break;

                    case StorylineState.CompleteMission:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: CompleteMission; CurrentAgentInteractionState [" + State.CurrentAgentInteractionState + "]");
                        if (State.CurrentAgentInteractionState == AgentInteractionState.Idle)
                        {
                            Log.WriteLine("Start Conversation [Complete Mission]");

                            State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                        }

                        if (MissionSettings.StorylineMission != null && MissionSettings.StorylineMission.Agent != null)
                        {
                            AgentInteraction.ProcessState(MissionSettings.StorylineMission.Agent);
                        }
                        else
                        {
                            AgentInteraction.ChangeAgentInteractionState(AgentInteractionState.Done, null);
                        }

                        if (State.CurrentAgentInteractionState == AgentInteractionState.Done)
                        {
                            //AgentInteraction.CloseConversation();
                            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                            ChangeStorylineState(StorylineState.Statistics);
                        }

                        break;

                    case StorylineState.Statistics:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: Statistics;");
                        if (!StatisticsStorylineState()) return;
                        ChangeStorylineState(StorylineState.BringSpoilsOfWar);
                        break;

                    case StorylineState.BringSpoilsOfWar:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: BringSpoilsOfWar;");
                        if (!BringSpoilsOfWar()) return;
                        break;

                    case StorylineState.RemoveOffer:
                        Log.WriteLine("Storyline: RemoveOffer;");
                        List<DirectAgentMission> currentStorylines =
                            ESCache.Instance.DirectEve.AgentMissions.Where(m => m.AgentId == MissionSettings.StorylineMission.AgentId)
                                .Where(m => m.Type.ToLower().Contains("Storyline".ToLower()) && m.State == MissionState.Offered)
                                .ToList();

                        // remove the storyline offer here and set the default agent
                        if (currentStorylines.Any())
                        {
                            DirectJournalWindow jw = ESCache.Instance.DirectEve.Windows.OfType<DirectJournalWindow>().FirstOrDefault();

                            if (jw == null)
                            {
                                Log.WriteLine("Opening journal.");
                                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenJournal);
                                return;
                            }

                            if (jw.SelectedMainTab != MainTab.AgentMissions)
                            {
                                Log.WriteLine("Journal window mission tab is not selected. Switching the tab.");
                                jw.SwitchMaintab(MainTab.AgentMissions);
                                return;
                            }

                            DirectAgentMission mission = currentStorylines.FirstOrDefault();
                            if (mission != null)
                            {
                                Log.WriteLine("Removing storyline mission [" + Log.FilterPath(mission.Name) + "] as we were either unable to finish the mission or it was against a blacklisted faction.");
                                mission.RemoveOffer();
                            }
                            else
                            {
                                // just blacklist the agent then if the mission has already accepted...
                                MissionSettings.AgentBlacklist.Add(MissionSettings.StorylineMission.AgentId);
                                Log.WriteLine(
                                    "BlacklistAgent: The agent that provided us with this storyline mission has been added to the session blacklist AgentId[" +
                                    MissionSettings.StorylineMission.AgentId + "]");
                            }
                        }

                        Reset();
                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                            CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                        ChangeStorylineState(StorylineState.Done);
                        break;

                    case StorylineState.BlacklistAgentForThisSession:
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Storyline: BlacklistAgentForThisSession;");
                        // just blacklist the agent then if the mission has already accepted...
                        MissionSettings.AgentBlacklist.Add(MissionSettings.StorylineMission.AgentId);
                        Log.WriteLine("BlacklistAgentForThisSession: The agent that provided us with this storyline mission has been added to the session blacklist AgentId[" + MissionSettings.StorylineMission.AgentId + "]");
                        Reset();
                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                            CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                        ChangeStorylineState(StorylineState.Done);
                        break;

                    case StorylineState.Done:
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: [" + ex + "]");
            }
        }

        //        public DirectAgentMission StorylineMission() {
        //        	return StorylineMission;
        //        }
        public void Reset()
        {
            try
            {
                Log.WriteLine("Storyline.Reset");
                //if (State.CurrentStorylineState != StorylineState.Idle)
                //{
                //    CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                //    _storylineMission = null;
                //    StorylineInstance.Reset();
                //}

                ChangeStorylineState(StorylineState.Idle);
                StorylineHandler = null;
                State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                State.CurrentTravelerState = TravelerState.Idle;
                Traveler.Destination = null;
                _setDestinationStation = false;
                HighSecChecked = false;
                ESCache.Instance.RouteIsAllHighSecBool = false;
                MissionSettings.AgentToPullNextRegularMissionFrom = null;
                MissionSettings.StrCurrentAgentName = null;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: [" + ex + "]");
            }
        }

        private bool BringSpoilsOfWar()
        {
            if (_nextAction > DateTime.UtcNow) return false;

            // Open the item hangar (should still be open)
            if (ESCache.Instance.ItemHangar == null) return false;

            // Do we have anything here we want to bring home, like implants or ?
            //if (to.Items.Any(i => i.GroupId == (int)GroupID.MiscSpecialMissionItems || i.GroupId == (int)GroupID.Livestock))

            if (!ESCache.Instance.ItemHangar.Items.Any(i => i.GroupId >= 738 && i.GroupId <= 750) || _moveCnt > 10)
            {
                ChangeStorylineState(StorylineState.Done);
                _moveCnt = 0;
                return true;
            }

            // Yes, open the ships cargo
            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                if (DebugConfig.DebugUnloadLoot) Log.WriteLine("if (Cache.Instance.CurrentShipsCargo == null)");
                return false;
            }

            // If we are not moving items
            if (ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("BringSpoilsOfWar"))
            {
                // Move all the implants to the cargo bay
                foreach (DirectItem item in ESCache.Instance.ItemHangar.Items.Where(i => i.GroupId >= 738
                                                                                         && i.GroupId <= 750))
                {
                    if (ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity - item.Volume * item.Quantity < 0)
                    {
                        Log.WriteLine("We are full, not moving anything else");
                        ChangeStorylineState(StorylineState.Done);
                        return true;
                    }

                    Log.WriteLine("Moving [" + item.TypeName + "][" + item.ItemId + "] to cargo");
                    _moveCnt++;
                    if (!ESCache.Instance.CurrentShipsCargo.Add(item, item.Quantity)) return false;
                    _nextAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(1000, 3000));
                    return false;
                }
                _nextAction = DateTime.UtcNow.AddSeconds(5);
                return false;
            }

            return false;
        }

        private void GotoAgent(StorylineState nextState)
        {
            if (_nextAction > DateTime.UtcNow)
                return;

            StationDestination baseDestination = Traveler.Destination as StationDestination;
            if (baseDestination == null || baseDestination.StationId != MissionSettings.StorylineMission.Agent.StationId)
            {
                Traveler.Destination = new StationDestination(MissionSettings.StorylineMission.Agent.SolarSystemId, MissionSettings.StorylineMission.Agent.StationId,
                    ESCache.Instance.DirectEve.GetLocationName(MissionSettings.StorylineMission.Agent.StationId));
                return;
            }

            if (!RouteToStorylineAgentIsSafe(MissionSettings.StorylineMission.Agent.StationId, MissionSettings.StorylineMission.Agent.SolarSystemId)) return;

            Traveler.ProcessState();
            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                ChangeStorylineState(nextState);
                Traveler.Destination = null;
                _setDestinationStation = false;
            }
        }

        private void IdleState()
        {
            try
            {
                //if (currentStorylineMission == null)
                //{
                //    ChangeStorylineState(StorylineState.Done);
                //    return;
                //}

                Log.WriteLine("Storyline.Idle: Starting [" + Log.FilterPath(MissionSettings.StorylineMission.Name) + "] for agent [" + MissionSettings.StorylineMission.Agent.Name + "] AgentID[" +
                              MissionSettings.StorylineMission.AgentId + "]");
                HighSecChecked = false;
                ChangeStorylineState(StorylineState.Arm);
                StorylineHandler = _storylines[Log.FilterPath(MissionSettings.StorylineMission.Name.ToLower())];
            }
            catch (Exception exception)
            {
                Log.WriteLine("IterateShipTargetValues - Exception: [" + exception + "]");
            }
        }

        #endregion Methods
    }
}