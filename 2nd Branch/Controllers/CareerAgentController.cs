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
using System.Linq;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Controllers
{
    public class CareerAgentController : BaseController
    {
        #region Constructors

        public CareerAgentController()
        {
            IgnorePause = false;
            IgnoreModal = false;
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
            finally
            {
                LocalPulse = DateTime.UtcNow.AddMilliseconds(1000);
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
                                    if (DebugConfig.DebugInteractWithEve) Log("CareerAgentController: RunOnce: OpenCargoHold: !OkToInteractWithEveNow");
                                    return;
                                }

                                if (DebugConfig.DebugCombatMissionsBehavior) Log("CareerAgentController: RunOnce");

                                ESCache.Instance.IterateShipTargetValues();
                                ESCache.Instance.IterateUnloadLootTheseItemsAreLootItems();

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
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("CareerAgentController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                if (AgentInteraction.boolSwitchAgents && State.CurrentQuestorState != QuestorState.CareerAgentPrepareToMoveAgents)
                {
                    Log("CareerAgentController: [ if (AgentInteraction.boolSwitchAgents) ]");
                    State.CurrentQuestorState = QuestorState.CareerAgentPrepareToMoveAgents;
                    return;
                }

                if (DebugConfig.DebugCombatMissionsBehavior) Log("CareerAgentController: CurrentQuestorState [" + State.CurrentQuestorState + "]");

                switch (State.CurrentQuestorState)
                {
                    case QuestorState.Idle:
                        ChangeQuestorControllerState(QuestorState.Start);
                        break;

                    case QuestorState.Start:
                        Log("Start Career Agent Mission Behavior");
                        ChangeQuestorControllerState(QuestorState.CombatMissionsBehavior);
                        break;

                    case QuestorState.CareerAgentPrepareToMoveAgents:
                        CareerAgentPrepareToMoveAgents();
                        break;

                    case QuestorState.CombatMissionsBehavior:
                        if (ESCache.Instance.InStation && ESCache.Instance.ActiveShip.GroupId == (int)Group.RookieShip)
                        {
                            if (MissionSettings.AgentToPullNextRegularMissionFrom.Mission != null)
                            {
                                if (MissionSettings.AgentToPullNextRegularMissionFrom.Mission.Name.Contains("6 of 10") ||
                                    MissionSettings.AgentToPullNextRegularMissionFrom.Mission.Name.Contains("7 of 10") ||
                                    MissionSettings.AgentToPullNextRegularMissionFrom.Mission.Name.Contains("8 of 10") ||
                                    MissionSettings.AgentToPullNextRegularMissionFrom.Mission.Name.Contains("9 of 10") ||
                                    MissionSettings.AgentToPullNextRegularMissionFrom.Mission.Name.Contains("10 of 10"))
                                {
                                    if (DirectEve.Interval(10000))
                                    {
                                        string msg = "We are still in a RookieShip. We need to change to a regular frig. Waiting for manual intervention.";
                                        Log(msg);
                                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, msg));
                                        Util.PlayNoticeSound();
                                        Util.PlayNoticeSound();
                                        Util.PlayNoticeSound();
                                        Util.PlayNoticeSound();
                                        ESCache.Instance.PauseAfterNextDock = true;
                                        //ESCache.Instance.DisableThisInstance();
                                        break;
                                    }

                                    break;
                                }
                            }
                        }

                        CombatMissionsBehavior.ProcessState(MissionSettings.AgentToPullNextRegularMissionFrom);
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

        public bool CareerAgentPrepareToMoveAgents()
        {
            int JitaP4M4Station = 60003760;
            if (ESCache.Instance.DirectEve.Session.StationId != JitaP4M4Station)
            {
                Log("Do we have ships to sell?");
                List<DirectItem> ships = ESCache.Instance.ShipHangar.Items.Where(x => x.GroupId == (int)Group.Frigate &&
                                                                                      x.Quantity == 1 &&
                                                                                      x.GivenName == null &&
                                                                                      (x.TypeName.ToLower() == "Slasher".ToLower() || //Minmatar
                                                                                       x.TypeName.ToLower() == "Rifter".ToLower() || //Minmatar
                                                                                       x.TypeName.ToLower() == "Breacher".ToLower() || //Minmatar
                                                                                       x.TypeName.ToLower() == "Burst".ToLower() || //Minmatar
                                                                                       x.TypeName.ToLower() == "Probe".ToLower() || //Minmatar
                                                                                       x.TypeName.ToLower() == "Vigil".ToLower() || //Minmatar
                                                                                       x.TypeName.ToLower() == "Crucifier".ToLower() || //Amarr
                                                                                       x.TypeName.ToLower() == "Executioner".ToLower() || //Amarr
                                                                                       x.TypeName.ToLower() == "Inquisitor".ToLower() || //Amarr
                                                                                       x.TypeName.ToLower() == "Magnate".ToLower() || //Amarr
                                                                                       x.TypeName.ToLower() == "Punisher".ToLower() || //Amarr
                                                                                       x.TypeName.ToLower() == "Tormentor".ToLower() || //Amarr
                                                                                       x.TypeName.ToLower() == "Bantam".ToLower() || //Caldari
                                                                                       x.TypeName.ToLower() == "Condor".ToLower() || //Caldari
                                                                                       x.TypeName.ToLower() == "Griffin".ToLower() || //Caldari
                                                                                       x.TypeName.ToLower() == "Heron".ToLower() || //Caldari
                                                                                       x.TypeName.ToLower() == "Kestrel".ToLower() || //Caldari
                                                                                       x.TypeName.ToLower() == "Merlin".ToLower() || //Caldari
                                                                                       x.TypeName.ToLower() == "Atron".ToLower() || //Gallente
                                                                                       x.TypeName.ToLower() == "Imicus".ToLower() || //Gallente
                                                                                       x.TypeName.ToLower() == "Incursus".ToLower() || //Gallente
                                                                                       x.TypeName.ToLower() == "Maulus".ToLower() || //Gallente
                                                                                       x.TypeName.ToLower() == "Navitas".ToLower() || //Gallente
                                                                                       x.TypeName.ToLower() == "Tristan".ToLower() //Gallente
                                                                                       )
                                                                                      ).ToList();

                if (DebugConfig.TryToSellAllPackagedShips)
                {
                    if (ships.Any())
                    {
                        Log("Ships to sell count [" + ships.Count + "]");
                        if (!Dump(ships)) return false;
                    }
                }


                Log("Load Ammo");
                if (!Arm.PrepareToMoveToNewStation()) return false;

                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.RestartOfEveClientNeeded), true);
                if (!DirectEve.Interval(10000))
                    return false;

                Log("Done preparing to move agents: restarting questor");
                ESCache.Instance.CloseEveReason = "Done with this career agent: restart eve to kick the bot into using next agent (bad hack)";
                ESCache.Instance.BoolRestartEve = true;
                //ESCache.Instance.RestartBot(ESCache.Instance.CloseEveReason); broken? fixme
                return true;
            }

            return true;
        }

        private int sellErrorCnt = 0;
        private bool _sellPerformed = false;
        private DateTime _sellPerformedDateTime = DateTime.MinValue;

        public bool Dump(List <DirectItem> loot2dump)
        {
            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log("ItemHangar is null.");
                return false;
            }

            if (loot2dump.Any())
            {
                var anyMultiSellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().Any();

                if (ESCache.Instance.SellError && anyMultiSellWnd)
                {
                    Log("Sell error, closing window and trying again.");
                    var sellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().FirstOrDefault();
                    sellWnd.Cancel();
                    sellErrorCnt++;
                    LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                    ESCache.Instance.SellError = false;

                    if (sellErrorCnt > 20)
                    {
                        Log($"Too many errors while dumping loot: pausing");
                        ESCache.Instance.PauseAfterNextDock = true;
                        return false;
                    }

                    return false;
                }

                if (!anyMultiSellWnd)
                {
                    Log($"Opening MultiSellWindow with {loot2dump.Count} items.");
                    ESCache.Instance.DirectEve.MultiSell(loot2dump);
                    LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                    _sellPerformed = false;
                    return false;
                }
                else
                {
                    var sellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().FirstOrDefault();
                    if (sellWnd.AddingItemsThreadRunning)
                    {
                        Log($"Waiting for items to be added to the multisell window.");
                        LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                        return false;
                    }
                    else
                    {

                        if (sellWnd.GetDurationComboValue() != DurationComboValue.IMMEDIATE)
                        {
                            Log($"Setting duration combo value to {DurationComboValue.IMMEDIATE}.");
                            Log($"Currently not working correctly, you need to select IMMEDIATE manually.");
                            sellWnd.SetDurationCombovalue(DurationComboValue.IMMEDIATE);
                            LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                            return false;
                        }

                        if (sellWnd.GetSellItems().All(i => !i.HasBid))
                        {
                            Log($"Only items without a bid are left. Done. " +
                                $"Changing to next state ({nameof(DumpLootControllerState.TravelToHomeStation)}).");
                            sellWnd.Cancel();
                            LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                            return true;
                        }

                        if (_sellPerformed)
                        {
                            var secondsSince =
                                Math.Abs((DateTime.UtcNow - _sellPerformedDateTime).TotalSeconds);
                            Log($"We just performed a sell [{secondsSince}] seconds ago. Waiting for timeout.");
                            LocalPulse = UTCNowAddMilliseconds(1500, 2000);

                            if (secondsSince <= 16) return false;

                            Log($"Timeout reached. Canceling the trade and changing to next state.");
                            sellWnd.Cancel();
                            LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                            return true;
                        }


                        Log($"Items added. Performing trade.");
                        sellWnd.PerformTrade();
                        _sellPerformed = true;
                        _sellPerformedDateTime = DateTime.UtcNow;
                        LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                        return false;
                    }
                }
            }
            else
            {
                Log($"Sold all items.");
                return true;
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