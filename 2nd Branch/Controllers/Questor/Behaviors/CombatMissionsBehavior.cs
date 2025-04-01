extern alias SC;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.Storylines;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Events;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Behaviors
{
    public class CombatMissionsBehavior
    {
        #region Constructors

        public CombatMissionsBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        private static bool _previousInMission;

        #endregion Fields

        #region Methods

        public static bool ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState _CMBStateToSet, bool wait, DirectAgent myAgent)
        {
            try
            {
                if (State.CurrentCombatMissionBehaviorState != _CMBStateToSet)
                {
                    if (_CMBStateToSet == CombatMissionsBehaviorState.Arm)
                        AgentInteraction.LastButtonPushedPerAgentId = new Dictionary<long, AgentButtonType>();

                    if (_CMBStateToSet == CombatMissionsBehaviorState.GotoBase)
                        State.CurrentTravelerState = TravelerState.Idle;

                    if (_CMBStateToSet == CombatMissionsBehaviorState.Start)
                        State.CurrentAgentInteractionState = AgentInteractionState.Idle;

                    if (_CMBStateToSet == CombatMissionsBehaviorState.CourierMission)
                        if (MissionSettings.StorylineMission != null && MissionSettings.StorylineMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.Idle)
                            CourierMissionCtrl.ChangeCourierMissionCtrlState(MissionSettings.StorylineMission, CourierMissionCtrlState.Start);

                    Log.WriteLine("New CombatMissionsBehaviorState [" + _CMBStateToSet + "]");
                    State.CurrentCombatMissionBehaviorState = _CMBStateToSet;
                    if (!wait) ProcessState(myAgent);
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        private static bool CmbBringSpoilsOfWar()
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

            return false;
        }

        private static bool PrepareToMoveToNewStation(DirectAgent myAgent)
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return true;

            if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
            {
                if (!Cleanup.RepairItems()) return false;

                if (ESCache.Instance.ItemHangar != null && ESCache.Instance.ItemHangar.Items.Count == 0) return true;

                if (State.CurrentArmState == ArmState.Idle)
                {
                    Log.WriteLine("Begin: Arm.PrepareToMoveToNewStation");
                    Arm.ChangeArmState(ArmState.PrepareToMoveToNewStation, true, myAgent);
                }

                Arm.ProcessState(myAgent);

                if (State.CurrentArmState == ArmState.Done)
                {
                    Arm.ChangeArmState(ArmState.Idle, true, myAgent);
                    return true;
                }

                return false;
            }

            return true;
        }

        private static Stopwatch CombatMissionBehaviorStopWatch = new Stopwatch();

        public static void ProcessState(DirectAgent myAgent)
        {
            try
            {
                CombatMissionBehaviorStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 1 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                CombatMissionBehaviorStopWatch.Restart();

                if (!CmbEveryPulse(myAgent)) return;

                CombatMissionBehaviorStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 2 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                CombatMissionBehaviorStopWatch.Restart();

                if (string.IsNullOrEmpty(MissionSettings.StrCurrentAgentName))
                {
                    if (DebugConfig.DebugAgentInteractionReplyToAgent)
                        Log.WriteLine("if (string.IsNullOrEmpty(Cache.Instance.strCurrentAgentName)) return;");
                    return;
                }

                CombatMissionBehaviorStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 4 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                CombatMissionBehaviorStopWatch.Restart();

                if (DebugConfig.DebugCombatMissionsBehavior) Log.WriteLine("State.CurrentCombatMissionBehaviorState is [" + State.CurrentCombatMissionBehaviorState + "]");

                switch (State.CurrentCombatMissionBehaviorState)
                {
                    case CombatMissionsBehaviorState.Idle:
                        IdleCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 11 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.DelayedGotoBase:
                        DelayedGotoBaseCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 14 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.Start:
                        StartCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 15 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.Switch:
                        SwitchCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 17 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.Arm:
                        if (MissionSettings.MyMission != null)
                        {
                            ArmCmbState(MissionSettings.MyMission);
                            CombatMissionBehaviorStopWatch.Stop();
                            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 19 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                            CombatMissionBehaviorStopWatch.Restart();
                            break;
                        }
                        else
                        {
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, false, myAgent);
                        }

                        break;

                    case CombatMissionsBehaviorState.LocalWatch:
                        LocalWatchCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 21 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.WaitingforBadGuytoGoAway:
                        WaitingFoBadGuyToGoAway(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 24 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.WarpOutStation:
                        WarpOutBookmarkCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 26 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.GotoMission:
                        GotoMissionCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 28 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.ExecuteMission:
                        ExecuteMissionCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 35 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.GotoBase:
                        GotoBaseCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 38 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.CompleteMission:
                        CompleteMissionCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 40 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.Statistics:
                        StatisticsCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 45 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.UnloadLoot:
                        UnloadLootCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 50 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.PrepareStorylineSwitchAgents:
                        Log.WriteLine("agent is [ " + MissionSettings.StorylineMission.Agent.Name + " ][" + MissionSettings.StorylineMission.Agent.SolarSystem.Name + "]");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.PrepareStorylineChangeFitting, false, MissionSettings.StorylineMission.Agent);
                        break;

                    case CombatMissionsBehaviorState.PrepareStorylineChangeFitting:
                        if (!Settings.Instance.UseFittingManager)
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Storyline, false, MissionSettings.StorylineMission.Agent);

                        if (State.CurrentArmState == ArmState.Idle)
                            Arm.ChangeArmState(ArmState.Begin, true, MissionSettings.StorylineMission.Agent);

                        Arm.ProcessState(MissionSettings.StorylineMission.Agent);

                        if (State.CurrentArmState == ArmState.Done)
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Storyline, false, MissionSettings.StorylineMission.Agent);

                        break;

                    case CombatMissionsBehaviorState.PrepareStorylineGotoBase:
                        PrepareStorylineGotoBaseCmbState(MissionSettings.StorylineMission.Agent);
                        break;

                    case CombatMissionsBehaviorState.Storyline:
                        MissionSettings.StorylineInstance.ProcessState();
                        if (State.CurrentStorylineState == StorylineState.Done)
                        {
                            Log.WriteLine("We have completed the storyline, resetting agent and returning to base");
                            MissionSettings.ClearMissionSpecificSettings();

                            if (myAgent == null)
                            {
                                Log.WriteLine("Storyline: done: if (MissionSettings.AgentToPullNextRegularMissionFrom == null)");
                                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true, null);
                            }

                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                        }
                        break;

                    case CombatMissionsBehaviorState.StorylineReturnToBase:
                        StorylineReturnToBaseCmbState(myAgent);
                        break;

                    case CombatMissionsBehaviorState.CourierMissionArm:
                        CourierMissionArmCmbState(myAgent);
                        break;

                    case CombatMissionsBehaviorState.CourierMission:
                        if (MissionSettings.MyMission != null)
                        {
                            CourierMissionCtrl.ProcessState(MissionSettings.MyMission);
                            return;
                        }

                        Log.WriteLine("CombatMissionsBehavior.ProcessState: CourierMission: RegularMission must be complete. GoToBase");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                        break;

                    case CombatMissionsBehaviorState.Traveler:
                        TravelerCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 70 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.GotoNearestStation:
                        GotoNearestStationCmbState(myAgent);
                        CombatMissionBehaviorStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 75 Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                        CombatMissionBehaviorStopWatch.Restart();
                        break;

                    case CombatMissionsBehaviorState.Default:
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, false, myAgent);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void StatisticsCmbState(DirectAgent myAgent)
        {
            if (Drones.UseDrones && ESCache.Instance.ActiveShip != null && !ESCache.Instance.ActiveShip.IsShipWithNoDroneBay)
            {
                DirectInvType drone = ESCache.Instance.DirectEve.GetInvType(Drones.DroneTypeID);
                if (drone != null && drone.Volume != 0)
                {
                    if (Drones.DroneBay == null)
                    {
                        Log.WriteLine("StatisticsCmbState: if (Drones.DroneBay == null)");
                        return;
                    }

                    if (drone.CategoryId != (int)CategoryID.Drone)
                    {
                        Log.WriteLine("StatisticsCmbState: if (drone.CategoryId != CategoryID.Drone) CategoryID is [" + drone.CategoryId + "][" + drone.CategoryName + "]");
                        return;
                    }

                    Statistics.LostDrones = (int)Math.Floor((Drones.DroneBay.Capacity - (double)Drones.DroneBay.UsedCapacity) / drone.Volume);
                    if (!Statistics.WriteDroneStatsLog()) return;
                }
                else
                {
                    Log.WriteLine("Could not find the drone TypeID specified in the character settings xml; this should not happen!");
                }
            }

            if (!Statistics.AmmoConsumptionStatistics()) return;
            Statistics.FinishedMission = DateTime.UtcNow;

            try
            {
                if (!Statistics.MissionLoggingCompleted)
                    if (!Statistics.WriteMissionStatistics()) return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot, false, myAgent);
        }

        private static void ArmCmbState(DirectAgentMission myMission)
        {
            if (!AttemptToBuyAmmo()) return;
            if (!AttemptToBuyLpItems()) return;

            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin Arm");
                Arm.ChangeArmState(ArmState.Begin, true, myMission.Agent);
            }

            if (!ESCache.Instance.InStation) return;

            Arm.ProcessState(myMission.Agent);

            if (State.CurrentArmState == ArmState.NotEnoughAmmo)
            {
                Log.WriteLine("Armstate.NotEnoughAmmo");
                Arm.ChangeArmState(ArmState.Idle, true, myMission.Agent);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true, myMission.Agent);
                return;
            }

            if (State.CurrentArmState == ArmState.NotEnoughDrones)
            {
                Log.WriteLine("Armstate.NotEnoughDrones");
                Arm.ChangeArmState(ArmState.Idle, true, myMission.Agent);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true, myMission.Agent);
                return;
            }

            if (State.CurrentArmState == ArmState.Done)
            {
                Arm.ChangeArmState(ArmState.Idle, true, myMission.Agent);

                if (Settings.Instance.BuyAmmo && BuyItemsController.CurrentBuyItemsState != BuyItemsState.DisabledForThisSession)
                {
                    BuyItemsController.CurrentBuyItemsState = BuyItemsState.Idle;
                    ControllerManager.Instance.RemoveController(typeof(BuyItemsController));
                }

                State.CurrentDroneControllerState = DroneControllerState.WaitingForTargets;
                if (MissionSettings.CourierMission(myMission))
                {
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CourierMission, false, myMission.Agent);
                    return;
                }

                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.LocalWatch, false, myMission.Agent);
            }
        }

        private static bool AttemptToBuyAmmo()
        {
            if (Settings.Instance.BuyAmmo)
            {
                if (MissionSettings.StorylineMission != null && MissionSettings.StorylineMission.Agent.StationId == ESCache.Instance.DirectEve.Session.StationId)
                {
                    Log.WriteLine("We are at a Storyline Agents Station. We only attempt to check for and buy ammo when we are at our regular agents station.");
                    return true;
                }

                if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Storyline || State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.StorylineReturnToBase)
                {
                    Log.WriteLine("We are on a Storyline. We only attempt to check for and buy ammo when we are at our regular agents station.");
                    return true;
                }

                if (BuyItemsController.CurrentBuyItemsState != BuyItemsState.Done && BuyItemsController.CurrentBuyItemsState != BuyItemsState.DisabledForThisSession)
                    if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastAmmoBuy.AddHours(8) && DateTime.UtcNow > ESCache.Instance.EveAccount.LastBuyLpItemAttempt.AddHours(1))
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastAmmoBuyAttempt), DateTime.UtcNow);
                        ControllerManager.Instance.AddController(new BuyItemsController());
                        return false;
                    }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                    if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastAmmoBuy.AddHours(1) && DateTime.UtcNow > ESCache.Instance.EveAccount.LastAmmoBuyAttempt.AddHours(.5))
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastAmmoBuyAttempt), DateTime.UtcNow);
                        ControllerManager.Instance.AddController(new BuyItemsController());
                        return false;
                    }
            }

            return true;
        }

        private static bool AttemptToBuyLpItems()
        {
            if (Settings.Instance.BuyLpItems)
                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastBuyLpItems.AddDays(8) && DateTime.UtcNow > ESCache.Instance.EveAccount.LastBuyLpItemAttempt.AddDays(1))
                {
                    State.CurrentBuyLpItemsState = BuyLpItemsState.Idle;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastBuyLpItemAttempt), DateTime.UtcNow);
                    ControllerManager.Instance.AddController(new BuyLpItemsController());
                    return false;
                }

            return true;
        }

        private static bool AttemptToCourierContractItems()
        {
            if (Settings.Instance.CreateCourierContracts)
                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastCreateContract.AddDays(1) && DateTime.UtcNow > ESCache.Instance.EveAccount.LastCreateContractAttempt.AddHours(4))
                {
                    State.CurrentCourierContractState = CourierContractState.Idle;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastCreateContractAttempt), DateTime.UtcNow);
                    ControllerManager.Instance.AddController(new CourierContractController());
                    return false;
                }

            return true;
        }

        private static bool CmbEveryPulse(DirectAgent myAgent)
        {
            if (!Settings.Instance.DefaultSettingsLoaded)
                Settings.Instance.LoadSettings_Initialize();

            CombatMissionBehaviorStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 1a Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
            CombatMissionBehaviorStopWatch.Restart();

            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return false;

            CombatMissionBehaviorStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 1b Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
            CombatMissionBehaviorStopWatch.Restart();

            if (Settings.Instance.FinishWhenNotSafe && State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.GotoNearestStation)
                if (ESCache.Instance.InSpace &&
                    !ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    if (ESCache.Instance.ClosestDockableLocation != null)
                    {
                        Log.WriteLine("Station found. Going to nearest station");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoNearestStation, true, null);
                    }
                    else
                    {
                        Log.WriteLine("Station not found. Going back to base");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                    }
                }

            CombatMissionBehaviorStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 1c Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
            CombatMissionBehaviorStopWatch.Restart();

            Panic.ProcessState(string.Empty);

            CombatMissionBehaviorStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 1d Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
            CombatMissionBehaviorStopWatch.Restart();

            if (State.CurrentPanicState == PanicState.Resume)
            {
                if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    State.CurrentPanicState = PanicState.Normal;

                    if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Storyline)
                    {
                        Log.WriteLine("PanicState.Resume: CMB State is Storyline");
                        if (MissionSettings.StorylineInstance.StorylineHandler is GenericCombatStoryline)
                        {
                            (MissionSettings.StorylineInstance.StorylineHandler as GenericCombatStoryline).CurrentGenericCombatStorylineState = GenericCombatStorylineState.GotoMission;
                            Log.WriteLine("PanicState.Resume: Setting GenericCombatStorylineState to GotoMission");
                        }

                        return true;
                    }

                    CombatMissionBehaviorStopWatch.Stop();
                    if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 1k Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                    CombatMissionBehaviorStopWatch.Restart();

                    if (MissionSettings.MyMission != null && MissionSettings.MyMission.Important && !MissionSettings.MyMission.Name.ToLower().Contains("Cash Flow for Capsuleers".ToLower()))
                    {
                        Log.WriteLine("PanicState.Resume: MissionSettings.RegularMission.Important");
                        if (MissionSettings.StorylineInstance.StorylineHandler is GenericCombatStoryline)
                        {
                            (MissionSettings.StorylineInstance.StorylineHandler as GenericCombatStoryline).CurrentGenericCombatStorylineState = GenericCombatStorylineState.GotoMission;
                            Log.WriteLine("PanicState.Resume: Setting GenericCombatStorylineState to GotoMission");
                        }

                        return true;
                    }

                    CombatMissionBehaviorStopWatch.Stop();
                    if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 1m Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                    CombatMissionBehaviorStopWatch.Restart();

                    State.CurrentTravelerState = TravelerState.Idle;
                    if (!Cleanup.RepairItems()) return false;

                    CombatMissionBehaviorStopWatch.Stop();
                    if (DebugConfig.DebugDefensePerformance) Log.WriteLine("CombatMissionBehavior: 1o Took [" + Util.ElapsedMicroSeconds(CombatMissionBehaviorStopWatch) + "]");
                    CombatMissionBehaviorStopWatch.Restart();

                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission, false, myAgent);
                    return true;
                }

                return false;
            }

            return true;
        }

        private static void CompleteMissionCmbState(DirectAgent myAgent)
        {
            if (!ESCache.Instance.InStation) return;

            if (State.CurrentAgentInteractionState == AgentInteractionState.Idle)
            {
                Log.WriteLine("Start Conversation [Complete RegularMission]");
                State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
            }

            AgentInteraction.ProcessState(myAgent);

            if (State.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                State.CurrentAgentInteractionState = AgentInteractionState.Idle;

                if (myAgent.Mission != null && MissionSettings.MissionCompletionErrors == 0)
                {
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Statistics, false, myAgent);
                    return;
                }

                Log.WriteLine("Skipping statistics: We have not yet completed a mission");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot, false, myAgent);
            }
        }

        private static void CourierMissionArmCmbState(DirectAgent myAgent)
        {
            if (!AttemptToBuyLpItems()) return;

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CourierMission, false, myAgent);
        }

        private static void DelayedGotoBaseCmbState(DirectAgent myAgent)
        {
            Log.WriteLine("Heading back to base");
            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
        }

        private static void ExecuteMissionCmbState(DirectAgent myAgent)
        {
            if (!ESCache.Instance.InSpace)
                return;

            if (!ESCache.Instance.InMission)
            {
                return;
            }

            ActionControl.ProcessState(myAgent.Mission, myAgent);

            bool inMission = ESCache.Instance.InMission;
            if (_previousInMission != inMission && ESCache.Instance.EntitiesOnGrid.Any(e => e.BracketType == BracketType.NPC_Frigate
                                                                                            || e.BracketType == BracketType.NPC_Cruiser
                                                                                            || e.BracketType == BracketType.NPC_Battleship
                                                                                            || e.BracketType == BracketType.NPC_Destroyer))
            {
                if (!_previousInMission && inMission && DirectEve.Interval(10000, 12000, inMission.ToString()))
                {
                    Log.WriteLine($"NPCs found on grid and InMission has been changed. Reloading.");
                    Combat.Combat.ReloadAll();
                }

                _previousInMission = inMission;
            }

            if (State.CurrentCombatState == CombatState.OutOfAmmo)
            {
                Log.WriteLine("Out of DefinedAmmoTypes! - Not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" +
                              Combat.Combat.MinimumAmmoCharges +
                              "]");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                ESCache.Instance.LootedContainers.Clear();
            }

            if (State.CurrentCombatMissionCtrlState == ActionControlState.Done)
            {
                Log.WriteLine("Done: if (State.CurrentCombatMissionCtrlState == ActionControlState.Done)");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                ESCache.Instance.LootedContainers.Clear();
            }

            if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
            {
                Log.WriteLine("Error");
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "Questor Error."));
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                ESCache.Instance.LootedContainers.Clear();
            }
        }

        private static void GotoBaseCmbState(DirectAgent myAgent)
        {
            //
            // if we are already in the correct place, we are done.
            //
            if (ESCache.Instance.InStation)
            {
                if (myAgent == null)
                {
                    Log.WriteLine("GotoBaseCmbState: if (myAgent == null)");
                    return;
                }
                //
                // we are docked
                //
                if (myAgent.StationId == ESCache.Instance.DirectEve.Session.StationId)
                {
                    if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBaseCmbState: if (MissionSettings.AgentToPullNextRegularMissionFrom.StationId == ESCache.Instance.DirectEve.Session.StationId)");
                    if (WeAreDockedAtTheCorrectStationNowWhat(myAgent)) return;
                    return;
                }

                if (!PrepareToMoveToNewStation(myAgent))
                {
                    if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBaseCmbState: if (!PrepareToMoveToNewStation())");
                    return;
                }

                if (!CmbBringSpoilsOfWar())
                {
                    if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBaseCmbState: if (!BringSpoilsOfWar())");
                    return;
                }
            }

            //
            // if we arent already in the correct place, travel...
            //

            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "CombatMissionsBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: Traveler.TravelHome()");

            Traveler.TravelHome(MissionSettings.AgentToPullNextRegularMissionFrom);

            if (State.CurrentTravelerState == TravelerState.AtDestination && ESCache.Instance.InStation)
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: We are at destination");
        }

        private static void GotoMissionCmbState(DirectAgent myAgent)
        {
            try
            {
                if (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.HasInitiatedWarp) return;

                if (MissionSettings.MyMission == null || MissionSettings.MyMission.State != MissionState.Accepted)
                {
                    Log.WriteLine("if (MissionSettings.RegularMission == null || (MissionSettings.RegularMission != null && MissionSettings.RegularMission.State != MissionState.Accepted))");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Start, true, myAgent);
                }

                MissionBookmarkDestination missionDestination = Traveler.Destination as MissionBookmarkDestination;

                if (missionDestination == null || missionDestination.AgentId != myAgent.AgentId)
                    if (MissionSettings.GetMissionBookmark(myAgent, "Encounter") != null)
                    {
                        Log.WriteLine("Setting Destination to 1st bookmark from Agent: " + myAgent.Name + " with [" + "Encounter" +
                                      "] in the title");
                        Traveler.Destination =
                            new MissionBookmarkDestination(MissionSettings.GetMissionBookmark(myAgent, "Encounter"));
                        if (ESCache.Instance.DirectEve.Navigation.GetLocation(Traveler.Destination.SolarSystemId) != null)
                        {
                            ESCache.Instance.MissionSolarSystem = ESCache.Instance.DirectEve.Navigation.GetLocation(Traveler.Destination.SolarSystemId);
                            Log.WriteLine("MissionSolarSystem is [" + ESCache.Instance.MissionSolarSystem.Name + "]");
                        }
                    }
                    else
                    {
                        //
                        // we used to "gotobase" here. I dont think thats needed
                        //
                        Log.WriteLine("We have no mission bookmark available for our current/normal agent: waiting for bookmark");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Start, true, myAgent);
                        return;
                    }

                Traveler.ProcessState();

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    State.CurrentCombatMissionCtrlState = ActionControlState.Start;
                    Traveler.Destination = null;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.ExecuteMission, true, myAgent);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void GotoNearestStationCmbState(DirectAgent myAgent)
        {
            if (!ESCache.Instance.InSpace || (ESCache.Instance.InSpace && ESCache.Instance.InWarp) || (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity.HasInitiatedWarp)) return;

            if (ESCache.Instance.DockableLocations != null)
            {
                if (ESCache.Instance.ClosestDockableLocation.Distance <= (int)Distances.DockingRange)
                {
                    if (ESCache.Instance.ClosestDockableLocation.Dock())
                    {
                        Log.WriteLine("[" + ESCache.Instance.ClosestDockableLocation.Name + "] which is [" + Math.Round(ESCache.Instance.ClosestDockableLocation.Distance / 1000, 0) + "k away]");
                        return;
                    }

                    return;
                }
                NavigateOnGrid.NavigateToTarget(ESCache.Instance.ClosestDockableLocation, 0);

                return;
            }

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true, myAgent);
        }

        private static void IdleCmbState(DirectAgent myAgent)
        {
            if (ESCache.Instance.InSpace)
            {
                Log.WriteLine("Idle: We started in space: GoToBase");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                return;
            }

            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentDroneControllerState = DroneControllerState.Idle;
            State.CurrentSalvageState = SalvageState.Idle;
            State.CurrentStorylineState = StorylineState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            State.CurrentUnloadLootState = UnloadLootState.Idle;

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Start, true, myAgent);
        }

        private static void LocalWatchCmbState(DirectAgent myAgent)
        {
            if (Settings.Instance.UseLocalWatch)
            {
                Time.Instance.LastLocalWatchAction = DateTime.UtcNow;

                if (DebugConfig.DebugArm) Log.WriteLine("Starting: Is LocalSafe check...");
                if (ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    Log.WriteLine("local is clear");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.WarpOutStation, false, myAgent);
                    return;
                }

                Log.WriteLine("Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.WaitingforBadGuytoGoAway, true, myAgent);
                return;
            }

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.WarpOutStation, false, myAgent);
        }

        private static void PrepareStorylineGotoBaseCmbState(DirectAgent myAgent)
        {
            if (DebugConfig.DebugGotobase) Log.WriteLine("PrepareStorylineGotoBase: AvoidBumpingThings()");
            NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "CombatMissionsBehaviorState.PrepareStorylineGotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());

            if (DebugConfig.DebugGotobase) Log.WriteLine("PrepareStorylineGotoBase: Traveler.TravelHome()");

            if (Settings.Instance.StoryLineBaseBookmark != "")
            {
                if (!Traveler.TravelToBookmarkName(Settings.Instance.StoryLineBaseBookmark))
                    Traveler.TravelHome(MissionSettings.AgentToPullNextRegularMissionFrom);
            }
            else
            {
                Traveler.TravelHome(MissionSettings.AgentToPullNextRegularMissionFrom);
            }

            if (State.CurrentTravelerState == TravelerState.AtDestination && ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("PrepareStorylineGotoBase: We are at destination");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Storyline, true, myAgent);
            }
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: start");
            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return;
        }

        private static void StartCmbState(DirectAgent myAgent)
        {
            if (MissionSettings.StorylineMissionDetected())
            {
                Log.WriteLine("if (MissionSettings.StorylineMissionDetected())");
                MissionSettings.StorylineInstance.Reset();
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.PrepareStorylineSwitchAgents, true, myAgent);
                return;
            }

            if (!ESCache.LootAlreadyUnloaded)
            {
                Log.WriteLine("if (!ESCache.LootAlreadyUnloaded)");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Switch, true, myAgent);
                return;
            }

            if (ESCache.Instance.InSpace)
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                return;
            }

            if (ESCache.Instance.InStation)
            {
                if (Statistics.MissionsThisSession >= 10 && myAgent.Level != 1)
                {
                    ESCache.Instance.CloseEveReason = "Statistics.MissionsThisSession >= 10: the schedule will restart questor as needed";
                    ESCache.Instance.BoolRestartEve = true;
                }
            }

            if (ESCache.Instance.InStation)
            {
                if (ESCache.Instance.EveAccount.ShouldBeStopped)
                {
                    ESCache.Instance.CloseEveReason = "if (ESCache.Instance.EveAccount.ShouldBeStopped)";
                    ESCache.Instance.BoolCloseEve = true;
                    return;
                }
            }

            if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }
            else
            {
                Salvage.OpenWrecks = true;
            }

            if (State.CurrentAgentInteractionState == AgentInteractionState.Idle)
            {
                ESCache.Instance.Wealth = ESCache.Instance.DirectEve.Me.Wealth ?? 0;

                Statistics.WrecksThisMission = 0;
                Log.WriteLine("Start conversation [Start Mission]");
                State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
            }

            AgentInteraction.ProcessState(myAgent);

            if (AgentInteraction.LastButtonPushedPerAgentId.ContainsKey(myAgent.AgentId))
            {
                AgentButtonType tempButton;
                AgentInteraction.LastButtonPushedPerAgentId.TryGetValue(myAgent.AgentId, out tempButton);
                if (tempButton == AgentButtonType.COMPLETE_MISSION)
                {
                    if (State.CurrentAgentInteractionState == AgentInteractionState.Done)
                    {
                        State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                        if (MissionSettings.CourierMission(myAgent.Mission))
                        {
                            AgentInteraction.LastButtonPushedPerAgentId.AddOrUpdate(myAgent.AgentId, AgentButtonType.None);
                            CourierMissionCtrl.ChangeCourierMissionCtrlState(myAgent.Mission, CourierMissionCtrlState.Start);
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CourierMissionArm, false, myAgent);
                            return;
                        }

                        AgentInteraction.LastButtonPushedPerAgentId.AddOrUpdate(myAgent.AgentId, AgentButtonType.None);
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot, false, myAgent);
                        return;
                    }

                    return;
                }
            }

            if (State.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                //
                // If AgentInteraction changed the state of CurrentCombatMissionBehaviorState to Idle: return
                //
                if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Idle)
                    return;

                if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items != null && ESCache.Instance.CurrentShipsCargo.Items.Count > 0 &&
                    ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
                {
                    Log.WriteLine("Start: if(Cache.Instance.CurrentShipsCargo.Items.Any()) UnloadLoot");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot, false, myAgent);
                    return;
                }

                //
                // otherwise continue on and change to the Arm state
                //
                State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Arm, false, myAgent);
            }
        }

        private static void StorylineReturnToBaseCmbState(DirectAgent myAgent)
        {
            MissionSettings.StorylineInstance.Reset();
            if (DebugConfig.DebugGotobase) Log.WriteLine("StorylineReturnToBase: AvoidBumpingThings()");
            NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "CombatMissionsBehaviorState.StorylineReturnToBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());

            if (DebugConfig.DebugGotobase) Log.WriteLine("StorylineReturnToBase: TravelToStorylineBase");

            Traveler.TravelHome(MissionSettings.AgentToPullNextRegularMissionFrom);

            if (State.CurrentTravelerState == TravelerState.AtDestination && ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("StorylineReturnToBase: We are at destination");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Switch, true, myAgent);
            }
        }

        private static void SwitchCmbState(DirectAgent myAgent)
        {
            if (!ESCache.Instance.InStation && ESCache.Instance.InSpace)
            {
                Log.WriteLine("Switch: We are in space: GoToBase");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                return;
            }

            if (MissionSettings.MyMission != null && MissionSettings.MyMission.Type.ToLower().Contains("Courier".ToLower()) &&
                (MissionSettings.MyMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.DropOffItem ||
                 MissionSettings.MyMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoDropOffLocation ||
                 MissionSettings.MyMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoPickupLocation))
            {
                Log.WriteLine("We doing a courier mission: CombatMissionsBehaviorState.CourierMission");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CourierMission, false, myAgent);
            }

            if (ESCache.Instance.DirectEve.Session.StationId != null && myAgent != null &&
                ESCache.Instance.DirectEve.Session.StationId != myAgent.StationId && MissionSettings.MyMission != null && !MissionSettings.MyMission.Type.Contains("Trade") && !MissionSettings.MyMission.Type.Contains("Courier"))
            {
                Log.WriteLine("We're not in the right station, going home.");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Items == null || ESCache.Instance.ItemHangar == null ||
                ESCache.Instance.ItemHangar.Items == null)
                return;

            if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items != null && ESCache.Instance.CurrentShipsCargo.Items.Count > 0 &&
                ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
            {
                Log.WriteLine("if(Cache.Instance.CurrentShipsCargo.Items.Any())");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot, false, myAgent);
                return;
            }

            if (State.CurrentArmState == ArmState.Idle)
                if (myAgent != null && myAgent.DivisionId == 24) //24 == security
                {
                    Log.WriteLine("Begin: Using Agent [" + myAgent.Name + "] Level [" + myAgent.Level + "] Division [" + myAgent.DivisionName + "]");
                    Arm.SwitchShipsOnly = true;
                    if (MissionSettings.MyMission != null && MissionSettings.MyMission.Faction == null)
                        return;

                    Arm.ChangeArmState(ArmState.ActivateCombatShip, true, myAgent);
                }
                else
                {
                    Arm.ChangeArmState(ArmState.Done, true, myAgent);
                }

            if (DebugConfig.DebugArm) Log.WriteLine("CombatMissionBehavior.Switch is Entering Arm.Processstate");

            Arm.ProcessState(myAgent);

            if (State.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Switch: Done: GoToBase");
                Arm.SwitchShipsOnly = false;
                Arm.ChangeArmState(ArmState.Idle, false, myAgent);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
            }
        }

        private static void TravelerCmbState(DirectAgent myAgent)
        {
            try
            {
                if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                {
                    if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                List<long> destination = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();
                if (destination == null || destination.Count == 0)
                {
                    Log.WriteLine("No destination?");
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

                if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                    destination[0] = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.LastOrDefault())
                {
                    if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Count > 0)
                    {
                        IEnumerable<DirectBookmark> bookmarks = ESCache.Instance.CachedBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.Any())
                        {
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                            return;
                        }

                        Log.WriteLine("Destination: [" + ESCache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                        long lastSolarSystemInRoute = destination.LastOrDefault();

                        Log.WriteLine("Destination: [" + lastSolarSystemInRoute + "]");
                        Traveler.Destination = new SolarSystemDestination(destination.LastOrDefault());
                        return;
                    }

                    return;
                }

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
                    {
                        Log.WriteLine("an error has occurred");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true, myAgent);
                        return;
                    }

                    if (ESCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true, myAgent);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, true, myAgent);
                    return;
                }

                Traveler.ProcessState();
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void UnloadLootCmbState(DirectAgent myAgent)
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return;

                if (State.CurrentUnloadLootState == UnloadLootState.Idle)
                    if (!ESCache.LootAlreadyUnloaded)
                    {
                        Log.WriteLine("UnloadLoot: Begin");
                        State.CurrentUnloadLootState = UnloadLootState.Begin;
                    }
                    else
                    {
                        Log.WriteLine("UnloadLoot: LootAlreadyUnloaded: Done");
                        State.CurrentUnloadLootState = UnloadLootState.Done;
                    }

                UnloadLoot.ProcessState();

                if (State.CurrentUnloadLootState == UnloadLootState.Done)
                {
                    Log.WriteLine("UnloadLoot: Done: Setting LootAlreadyUnloaded = true");
                    ESCache.LootAlreadyUnloaded = true;
                    State.CurrentUnloadLootState = UnloadLootState.Idle;

                    if (State.CurrentCombatState == CombatState.OutOfAmmo)
                    {
                        Log.WriteLine("State.CurrentCombatState == CombatState.OutOfAmmo");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, true, myAgent);
                        return;
                    }

                    int intMissionNum = 0;
                    if (ESCache.Instance.DirectEve.AgentMissions != null && ESCache.Instance.DirectEve.AgentMissions.Count > 0)
                    {
                        foreach (DirectAgentMission tempMission in ESCache.Instance.DirectEve.AgentMissions)
                        {
                            intMissionNum++;
                            Log.WriteLine("[" + intMissionNum + "] Mission [" + tempMission.Name + "] is in State [" + tempMission.State + "]");
                        }

                        if (ESCache.Instance.DirectEve.AgentMissions.Any(i => i.Agent.IsAgentMissionAccepted))
                        {
                            if (MissionSettings.StorylineMission != null && MissionSettings.StorylineMission.Agent.IsAgentMissionAccepted)
                            {
                                if (MissionSettings.StorylineMission.Agent.AgentWindow == null)
                                {
                                    MissionSettings.StorylineMission.Agent.OpenAgentWindow(true);
                                    Log.WriteLine("if (MissionSettings.StorylineMission.Agent.Window == null)");
                                    return;
                                }

                                if (MissionSettings.StorylineMission.Agent.AgentWindow != null)
                                {
                                    if (AgentInteraction.PressCompleteButtonIfItExists("UnloadLoot Press CompleteButton", MissionSettings.StorylineMission.Agent)) return;

                                    if (!MissionSettings.StorylineMission.Agent.AgentWindow.Objective.Contains("Objectives Complete"))
                                    {
                                        Log.WriteLine("UnloadLoot: We have at least one mission that is currently Accepted");
                                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Arm, true, myAgent);
                                        return;
                                    }
                                }
                            }

                            //if (myAgent.Window == null) return;
                            if (ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.CareerAgentController))
                            {
                                if (myAgent.AgentWindow != null && !myAgent.AgentWindow.Objective.Contains("Objectives Complete"))
                                {
                                    Log.WriteLine("UnloadLoot: We have at least one mission that is currently Accepted");
                                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Arm, true, myAgent);
                                    return;
                                }
                            }
                        }
                    }

                    if (MissionSettings.StorylineMission != null && MissionSettings.StorylineMission.State == MissionState.Accepted)
                    {
                        Log.WriteLine("UnloadLoot: StorylineMission [" + MissionSettings.StorylineMission.Name + "][" + MissionSettings.StorylineMission.State + "]: Arm");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Storyline, true, myAgent);
                        return;
                    }

                    Log.WriteLine("SelectedController: [" + ESCache.Instance.SelectedController + "], CombatMissionsBehaviorState: [" + State.CurrentCombatMissionBehaviorState + "]");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Start, true, myAgent);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void WaitingFoBadGuyToGoAway(DirectAgent myAgent)
        {
            if (DateTime.UtcNow.Subtract(Time.Instance.LastLocalWatchAction).TotalMinutes <
                Time.Instance.WaitforBadGuytoGoAway_minutes + ESCache.Instance.RandomNumber(1, 3))
                return;

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.LocalWatch, true, myAgent);
        }

        private static void WarpOutBookmarkCmbState(DirectAgent myAgent)
        {
            if (ESCache.Instance.InStation &&  DateTime.UtcNow > Time.Instance.LastUndockAction.AddSeconds(10))
            {
                TravelerDestination.Undock();
                return;
            }

            if (!ESCache.Instance.InSpace)
                return;

            if (!string.IsNullOrEmpty(Settings.Instance.UndockBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> warpOutBookmarks = ESCache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "");
                if (warpOutBookmarks != null && warpOutBookmarks.Any())
                {
                    DirectBookmark warpOutBookmark =
                        warpOutBookmarks.OrderByDescending(b => b.CreatedOn)
                            .FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && 100000000 > b.DistanceFromEntity(ESCache.Instance.ClosestStation._directEntity));

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Log.WriteLine("No Bookmark");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission, false, myAgent);
                    }
                    else if (warpOutBookmark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + warpOutBookmark.Title);
                            Traveler.Destination = new BookmarkDestination(warpOutBookmark);
                        }

                        Traveler.ProcessState();
                        if (State.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            Traveler.Destination = null;
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission, false, myAgent);
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmark in System");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission, false, myAgent);
                    }

                    return;
                }
            }

            Log.WriteLine("No Bookmark in System");
            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission, false, myAgent);
        }

        private static bool WeAreDockedAtTheCorrectStationNowWhat(DirectAgent myAgent)
        {
            if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
            {
                Log.WriteLine("CMB: if (State.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)");
                Traveler.Destination = null;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true, myAgent);
                return true;
            }

            //
            // we are already docked at the regular (non-storyline!) agents station
            //
            if (State.CurrentCombatState != CombatState.OutOfAmmo && myAgent.Mission != null &&
                myAgent.Mission.State == MissionState.Accepted)
            {
                Traveler.Destination = null;
                if (myAgent.Mission != null && myAgent.Mission.Type.Contains("Encounter") && DateTime.UtcNow > Time.Instance.LastMissionCompletionError.AddSeconds(30))
                {
                    Log.WriteLine("GotoBase: We are in [" + myAgent.Name + "]'s station: [" + myAgent.Mission.Name + "] MissionState is Accepted - changing state to: CompleteMission");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CompleteMission, false, myAgent);
                    return true;
                }

                Log.WriteLine("GotoBase: We are in [" + myAgent.Name + "]'s station: [" + myAgent.Mission.Name + "] MissionState is Accepted - We tried to Complete the mission and it failed. Continuing");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot, false, myAgent);
                return true;
            }

            Traveler.Destination = null;
            Log.WriteLine("GotoBase: We are in [" + myAgent.Name + "]'s station: changing state to: UnloadLoot");
            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot, false, myAgent);
            return true;
        }

        #endregion Methods
    }
}