extern alias SC;

namespace EVESharpCore.Controllers
{
    /**
    public class StandingsGrindController : BaseController
    {
        #region Constructors

        public StandingsGrindController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            DependsOn = new List<Type>
            {
                typeof(SalvageController),
                typeof(DefenseController)
            };
            CombatMissionsBehaviorInstance = new CombatMissionsBehavior();
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            State.CurrentQuestorState = QuestorState.Idle;
            Time.Instance.StartTime = DateTime.UtcNow;
            Time.Instance.QuestorStarted_DateTime = DateTime.UtcNow;
            Settings.Instance.CharacterMode = "none";

            // add additional controllers
        }

        #endregion Constructors

        #region Properties

        private const string constCareerAgentAmarr1 = "Chakh Madafe";
        private const string constCareerAgentAmarr2 = "Zafarara Fari";
        private const string constCareerAgentAmarr3 = "Joas Alathema";
        private const string constCareerAgentCaldari1 = "Ikonaiki Ebora";
        private const string constCareerAgentCaldari2 = "Yamonen Petihainen";
        private const string constCareerAgentCaldari3 = "Ranta Tarumo";
        private const string constCareerAgentGallente1 = "Berlimaute Remintgarnes";
        private const string constCareerAgentGallente2 = "Hasier Parcie";
        private const string constCareerAgentGallente3 = "Seville Eyron";
        private const string constCareerAgentMinmatar1 = "Fykalia Adaferid";
        private const string constCareerAgentMinmatar2 = "Arninald Beinarakur";
        private const string constCareerAgentMinmatar3 = "Stird Odetlef";

        private readonly float standingsAboveThisWeAreFinished = (float)8.0;
        private long CorpTdGrindStandings;
        private long FactionIdToGrindStandings;
        private float standingsBelowThisWeWillGrind = (float)6.0;
        private bool _setCreatePathRan { get; set; }
        private CombatMissionsBehavior CombatMissionsBehaviorInstance { get; }

        private bool RunOnceAfterStartupalreadyProcessed { get; set; }

        #endregion Properties

        #region Methods

        public override void DoWork()
        {
            try
            {
                if (!_setCreatePathRan)
                    SetCreatePathes();

                if (!RunOnceAfterStartupalreadyProcessed &&
                    ESCache.Instance.DirectEve.Session.CharacterId != null && ESCache.Instance.DirectEve.Session.CharacterId > 0)
                    if (Settings.CharacterXMLExists && DateTime.UtcNow > Time.Instance.NextStartupAction)
                    {
                        try
                        {
                            if (!ESCache.Instance.OkToInteractWithEveNow)
                            {
                                if (DebugConfig.DebugInteractWithEve) Log("Questor: RunOnce: OpenCargoHold: !OkToInteractWithEveNow");
                                return;
                            }

                            if (DebugConfig.DebugCombatMissionsBehavior) Log("QuestorController: RunOnce");

                            ESCache.Instance.IterateShipTargetValues("RunOnceAfterStartup");
                            ESCache.Instance.IterateUnloadLootTheseItemsAreLootItems("RunOnceAfterStartup");
                            ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                        }
                        catch (Exception ex)
                        {
                            Log("Exception [" + ex + "]");
                        }

                        RunOnceAfterStartupalreadyProcessed = true;
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
                        (int)DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalMinutes;
                    Time.Instance.LastUpdateOfSessionRunningTime = DateTime.UtcNow;
                }

                if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("QuestorController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                if (AgentInteraction.boolSwitchAgents)
                {
                    Log("QuestorController: [ State.CurrentQuestorState = QuestorState.SwitchAgents; ]");
                    State.CurrentQuestorState = QuestorState.PickAgentToUseNext;
                    AgentInteraction.boolSwitchAgents = false;
                }

                if (DebugConfig.DebugCombatMissionsBehavior) Log("QuestorController: CurrentQuestorState [" + State.CurrentQuestorState + "]");

                switch (State.CurrentQuestorState)
                {
                    case QuestorState.Idle:
                        State.CurrentQuestorState = QuestorState.Start;
                        break;

                    case QuestorState.PickAgentToUseNext:
                        PickAgentToUseNext();
                        break;

                    case QuestorState.CombatMissionsBehavior:
                        CombatMissionsBehavior.ProcessState();
                        break;

                    case QuestorState.Start:
                        Log("Start Career Agent Mission Behavior");
                        State.CurrentQuestorState = QuestorState.PickAgentToUseNext;
                        break;

                    case QuestorState.Error:
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.QUESTOR_ERROR, "Questor Error."));
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
            if (cm.TryGetController<BuyAmmoController>(out _))
                return false;

            if (cm.TryGetController<BuyPlexController>(out _))
                return false;

            if (cm.TryGetController<BuyLpItemsController>(out _))
                return false;

            return true;
        }

        public void PickAgentToUseNext()
        {
            FindFactionToGrindStandings();
            FindCorporationToGrindStandings();
            FindAgentToGrindStandings();

            if (!ESCache.Instance.EveAccount.CareerAgentCaldari1MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentCaldari1 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentCaldari1;
                if (SwitchAgents(nameof(EveAccount.CareerAgentCaldari1MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentCaldari2MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentCaldari2 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentCaldari2;
                if (SwitchAgents(nameof(EveAccount.CareerAgentCaldari2MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentCaldari3MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentCaldari3 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentCaldari3;
                if (SwitchAgents(nameof(EveAccount.CareerAgentCaldari3MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentMinmatar1MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentMinmatar1 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentMinmatar1;
                if (SwitchAgents(nameof(EveAccount.CareerAgentMinmatar1MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentMinmatar2MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentMinmatar2 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentMinmatar2;
                if (SwitchAgents(nameof(EveAccount.CareerAgentMinmatar2MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentMinmatar3MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentMinmatar3 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentMinmatar3;
                if (SwitchAgents(nameof(EveAccount.CareerAgentMinmatar3MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentGallente1MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentGallente1 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentGallente1;
                if (SwitchAgents(nameof(EveAccount.CareerAgentGallente1MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentGallente2MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentGallente2 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentGallente2;
                if (SwitchAgents(nameof(EveAccount.CareerAgentGallente2MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentGallente3MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentGallente3 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentGallente3;
                if (SwitchAgents(nameof(EveAccount.CareerAgentGallente3MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentAmarr1MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentAmarr1 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentAmarr1;
                if (SwitchAgents(nameof(EveAccount.CareerAgentAmarr1MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentAmarr2MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentAmarr2 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentAmarr2;
                if (SwitchAgents(nameof(EveAccount.CareerAgentAmarr2MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentAmarr3MissionsComplete)
            {
                Log("Attempting to use agent [" + constCareerAgentAmarr3 + "]");
                MissionSettings.strCurrentAgentName = constCareerAgentAmarr3;
                if (SwitchAgents(nameof(EveAccount.CareerAgentAmarr3MissionsComplete))) return;
            }

            ControllerManager.Instance.SetPause(true);
            Log("Pausing: There are no more career agents left to process.");
        }

        //
        // this also should not be here, it would be better for it to be in a common place, like settings.cs or even better in the launcher
        //
        public void SetCreatePathes()
        {
            Statistics.DroneStatsLogPath = Logging.Log.Logpath;
            Statistics.DroneStatslogFile = Path.Combine(Statistics.DroneStatsLogPath, ESCache.Instance.EveAccount.CharacterName + ".DroneStats.log");

            Statistics.WindowStatsLogPath = Path.Combine(Logging.Log.Logpath, "WindowStats\\");
            Statistics.WindowStatslogFile = Path.Combine(Statistics.WindowStatsLogPath,
                ESCache.Instance.EveAccount.CharacterName + ".WindowStats-DayOfYear[" + DateTime.UtcNow.DayOfYear + "].log");
            Statistics.WreckLootStatisticsPath = Logging.Log.Logpath;
            Statistics.WreckLootStatisticsFile = Path.Combine(Statistics.WreckLootStatisticsPath,
                ESCache.Instance.EveAccount.CharacterName + ".WreckLootStatisticsDump.log");

            Statistics.MissionStats3LogPath = Path.Combine(Logging.Log.Logpath, "MissionStats\\");
            Statistics.MissionStats3LogFile = Path.Combine(Statistics.MissionStats3LogPath,
                ESCache.Instance.EveAccount.CharacterName + ".CustomDatedStatistics.csv");
            Statistics.MissionDungeonIdLogPath = Path.Combine(Logging.Log.Logpath, "MissionStats\\");
            Statistics.MissionDungeonIdLogFile = Path.Combine(Statistics.MissionDungeonIdLogPath,
                ESCache.Instance.EveAccount.CharacterName + "Mission-DungeonId-list.csv");
            Statistics.PocketStatisticsPath = Path.Combine(Logging.Log.Logpath, "PocketStats\\");
            Statistics.PocketStatisticsFile = Path.Combine(Statistics.PocketStatisticsPath,
                ESCache.Instance.EveAccount.CharacterName + "pocketstats-combined.csv");
            Statistics.PocketObjectStatisticsPath = Path.Combine(Logging.Log.Logpath, "PocketObjectStats\\");
            Statistics.PocketObjectStatisticsFile = Path.Combine(Statistics.PocketObjectStatisticsPath,
                ESCache.Instance.EveAccount.CharacterName + "PocketObjectStats-combined.csv");
            Statistics.MissionDetailsHtmlPath = Path.Combine(Logging.Log.Logpath, "MissionDetailsHTML\\");
            Statistics.MissionPocketObjectivesPath = Path.Combine(Logging.Log.Logpath, "MissionPocketObjectives\\");

            try
            {
                Directory.CreateDirectory(Logging.Log.Logpath);
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

        public bool SwitchAgents(string NameOfEveAccountSetting)
        {
            DirectAgent agent = ESCache.Instance.DirectEve.GetAgentByName(MissionSettings.strCurrentAgentName);

            if (agent != null)
            {
                MissionSettings.AgentToPullNextRegularMissionFrom = null;
                Log("New agent is [ " + MissionSettings.strCurrentAgentName + " ]");
                State.CurrentQuestorState = QuestorState.CombatMissionsBehavior;
                return true;
            }
            Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete: we could find no agent with that name!");
            ESCache.Instance.TaskSetEveAccountAttribute(NameOfEveAccountSetting, true);
            return false;
        }

        public void TrackCareerAgentsWithNoMissionsAvailable()
        {
            //
            // confirm agent has no missions?
            //
            if (AgentInteraction.boolNoMissionsAvailable)
            {
                //Caldari Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentCaldari1)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentCaldari1MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentCaldari1MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Caldari Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentCaldari2)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentCaldari2MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentCaldari2MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Caldari Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentCaldari3)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentCaldari3MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentCaldari3MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Minmatar Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentMinmatar1)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentMinmatar1MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentMinmatar1MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Minmatar Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentMinmatar2)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentMinmatar2MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentMinmatar2MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Minmatar Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentMinmatar3)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentMinmatar3MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentMinmatar3MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Gallente Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentGallente1)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentGallente1MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentGallente1MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Gallente Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentGallente2)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentGallente2MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentGallente2MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Gallente Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentGallente3)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentGallente3MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentGallente3MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Amarr Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentAmarr1)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentAmarr1MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentAmarr1MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Amarr Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentAmarr2)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentAmarr2MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentAmarr2MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Amarr Agents
                if (MissionSettings.AgentToPullNextRegularMissionFrom != null && MissionSettings.AgentToPullNextRegularMissionFrom.Name == constCareerAgentAmarr3)
                {
                    Log("Marking Agent [" + MissionSettings.AgentToPullNextRegularMissionFrom.Name + "] complete as they have no more missions available to us.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentAmarr3MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentAmarr3MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                }
            }
        }

        private void FindAgentToGrindStandings()
        {
        }

        private void FindCorporationToGrindStandings()
        {
            //
            // Use corporations that get heavy foot traffic / good LP stores, even if we dont think we will be using said LP store, hide in the noise!
            //
            string strStandings = string.Empty;
            switch (FactionIdToGrindStandings)
            {
                case DirectNpcInfo.CaldariStateFactionId:
                    CorpTdGrindStandings = 1000035; //Caldari Navy
                    break;

                case DirectNpcInfo.MinmatarRepublicFactionId:
                    CorpTdGrindStandings = 1000051; //Republic Fleet
                    break;

                case DirectNpcInfo.AmarrEmpireFactionId:
                    CorpTdGrindStandings = 1000084; //Amarr Navy
                    break;

                case DirectNpcInfo.GallenteFederationFactionId:
                    CorpTdGrindStandings = 1000120; //Federation Navy
                    break;
            }

            DirectNpcInfo.NpcCorpIdsToNames.TryGetValue(CorpTdGrindStandings.ToString(), out strStandings);
            Log("Setting Corporation to: [" + strStandings + "]");
        }

        private void FindFactionToGrindStandings()
        {
            //
            // Check Standings: Caldari --> Minmatar --> Amarr --> Gallente
            // FYI: we want to grind until we get to 8.0
            // but we dont want to grind for hat faction unless we are below 6.0,
            // this gives us 2 points of faction standings as "play" so that we arent bouncing between factions when grinding
            //

            float CaldariFactionStanding = ESCache.Instance.DirectEve.Standings.EffectiveStanding(DirectNpcInfo.CaldariStateFactionId, long.Parse(ESCache.Instance.EveAccount.myCharacterId));
            if (standingsAboveThisWeAreFinished > CaldariFactionStanding)
            {
                Log("We have over [" + standingsAboveThisWeAreFinished + "] standings with Caldari");
                float MinmatarFactionStanding = ESCache.Instance.DirectEve.Standings.EffectiveStanding(DirectNpcInfo.MinmatarRepublicFactionId, long.Parse(ESCache.Instance.EveAccount.myCharacterId));
                if (standingsAboveThisWeAreFinished > MinmatarFactionStanding)
                {
                    Log("We have over [" + standingsAboveThisWeAreFinished + "] standings with Minmatar");
                    float AmarrFactionStanding = ESCache.Instance.DirectEve.Standings.EffectiveStanding(DirectNpcInfo.AmarrEmpireFactionId, long.Parse(ESCache.Instance.EveAccount.myCharacterId));
                    if (standingsAboveThisWeAreFinished > AmarrFactionStanding)
                    {
                        Log("We have over [" + standingsAboveThisWeAreFinished + "] standings with Amarr");
                        float GallenteFactionStanding = ESCache.Instance.DirectEve.Standings.EffectiveStanding(DirectNpcInfo.GallenteFederationFactionId, long.Parse(ESCache.Instance.EveAccount.myCharacterId));
                        if (standingsAboveThisWeAreFinished > GallenteFactionStanding)
                        {
                            Log("We have over [" + standingsAboveThisWeAreFinished + "] standings with Gallente");
                            Log("Pausing: We have over [" + standingsAboveThisWeAreFinished + "] standings with all 4 major factions!");
                            ControllerManager.Instance.SetPause(true);
                            return;
                        }

                        //
                        // Gallente
                        //
                        Log("We have [" + GallenteFactionStanding + "] standings with Gallente");
                        FactionIdToGrindStandings = DirectNpcInfo.GallenteFederationFactionId;
                        return;
                    }

                    //
                    // Amarr
                    //
                    Log("We have [" + AmarrFactionStanding + "] standings with Amarr");
                    FactionIdToGrindStandings = DirectNpcInfo.AmarrEmpireFactionId;
                    return;
                }
                //
                // Minmatar
                //
                Log("We have [" + MinmatarFactionStanding + "] standings with Minmatar");
                FactionIdToGrindStandings = DirectNpcInfo.MinmatarRepublicFactionId;
                return;
            }
            //
            // Caldari
            //
            Log("We have [" + CaldariFactionStanding + "] standings with Caldari");
            FactionIdToGrindStandings = DirectNpcInfo.CaldariStateFactionId;
        }

        #endregion Methods
    }
    **/
}