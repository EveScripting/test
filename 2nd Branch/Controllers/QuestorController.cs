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
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.IO;

namespace EVESharpCore.Controllers
{
    public class QuestorController : BaseController
    {
        #region Constructors

        public QuestorController() : base()
        {
            IgnorePause = false;
            IgnoreModal = true;
            DependsOn = new List<Type>
            {
                typeof(SalvageController),
                typeof(DefenseController),
                typeof(AmmoManagementController),
            };
            CombatMissionsBehaviorInstance = new CombatMissionsBehavior();
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            State.CurrentQuestorState = QuestorState.Idle;
            Time.Instance.StartTime = DateTime.UtcNow;
            Time.Instance.Started_DateTime = DateTime.UtcNow;

            // add additional controllers
        }

        #endregion Constructors

        #region Properties

        private bool _setCreatePathRan { get; set; }
        private CombatMissionsBehavior CombatMissionsBehaviorInstance { get; }

        private bool RunOnceAfterStartupalreadyProcessed { get; set; }

        #endregion Properties

        #region Methods

        public bool ChangeQuestorControllerState(QuestorState stateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentQuestorState != stateToSet)
                {
                    Log("New QuestorState [" + stateToSet + "]");
                    State.CurrentQuestorState = stateToSet;
                    if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public override void DoWork()
        {
            try
            {
                ProcessState();
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

        public void ProcessState()
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
                                ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastSessionReady), DateTime.UtcNow);
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.DoneLaunchingEveInstance), true);
                                //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AllowSimultaneousLogins), false);
                                //ESCache.Instance.DirectEve.SetAudioDisabled();
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
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("QuestorController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                if (DebugConfig.DebugCombatMissionsBehavior) Log("QuestorController: CurrentQuestorState [" + State.CurrentQuestorState + "]");

                switch (State.CurrentQuestorState)
                {
                    case QuestorState.Idle:
                        ChangeQuestorControllerState(QuestorState.Start, false);
                        break;

                    case QuestorState.CombatMissionsBehavior:
                        CombatMissionsBehavior.ProcessState(MissionSettings.AgentToPullNextRegularMissionFrom);
                        break;

                    case QuestorState.Start:
                        Log("Start Mission Behavior");
                        ChangeQuestorControllerState(QuestorState.CombatMissionsBehavior, false);
                        break;

                    case QuestorState.Error:
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

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}