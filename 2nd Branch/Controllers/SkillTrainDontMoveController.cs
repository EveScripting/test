
extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;

namespace EVESharpCore.Controllers
{
    public class SkillTrainDontMoveController : BaseController
    {
        #region Constructors

        public SkillTrainDontMoveController()
        {
            AllowRunInStation = false;
            IgnorePause = false;
            IgnoreModal = false;
            State.CurrentCombatDontMoveBehaviorState = CombatDontMoveBehaviorState.Default;
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            Time.Instance.Started_DateTime = DateTime.UtcNow;
            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastQuestorStarted), DateTime.UtcNow);
            DependsOn = new List<Type>
            {
                typeof(SalvageController),
                typeof(DefenseController)
            };
        }

        #endregion Constructors

        #region Fields

        #endregion Fields

        #region Properties

        private bool RunOnceAfterStartupAlreadyProcessed { get; set; }

        #endregion Properties

        #region Methods

        private static void IdleCMBState()
        {
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentDroneControllerState = DroneControllerState.Idle;
            State.CurrentSalvageState = SalvageState.Idle;
            State.CurrentStorylineState = StorylineState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;

            ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
        }

        public static bool ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentAbyssalDeadspaceBehaviorState != _StateToSet)
                {
                    Log("New AbyssalDeadspaceBehaviorState [" + _StateToSet + "]");
                    State.CurrentAbyssalDeadspaceBehaviorState = _StateToSet;
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        private static void StartCMBState()
        {
            //
            // It takes 20 minutes (potentially) to do an abyssal site: if it is within 25min of Downtime (10:35 evetime) pause
            //
            if (Time.Instance.IsItDuringDowntimeNow)
            {
                Log("AbyssalDeadspaceController: Arm: Downtime is less than 25 minutes from now: Pausing");
                ControllerManager.Instance.SetPause(true);
                return;
            }

            if (ESCache.Instance.InStation)
            {
                //...

                if (ESCache.Instance.EveAccount.ShouldBeStopped)
                {
                    ESCache.Instance.CloseEveReason = "if (DateTime.UtcNow > ESCache.Instance.EveAccount.EndTime)";
                    ESCache.Instance.BoolCloseEve = true;
                    return;
                }
            }
        }


        public static void ProcessState()
        {
            try
            {
                //if (!EveryPulse()) return;

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("State.CurrentAbyssalDeadspaceBehaviorState is [" + State.CurrentAbyssalDeadspaceBehaviorState + "]");

                switch (State.CurrentAbyssalDeadspaceBehaviorState)
                {
                    case AbyssalDeadspaceBehaviorState.Idle:
                        MissionSettings.MissionSafeDistanceFromStructure = 22000;
                        IdleCMBState();
                        break;

                    case AbyssalDeadspaceBehaviorState.Start:
                        StartCMBState();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public override void DoWork()
        {
            try
            {
                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    return;

                if (ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.TypeId != ESCache.Instance.MyShipEntity.TypeId)
                    return;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                    return;

                if (!Settings.Instance.DefaultSettingsLoaded)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("CombatDontMoveController: Loading Settings");
                    Settings.Instance.LoadSettings_Initialize();
                }

                if (!RunOnceAfterStartupAlreadyProcessed &&
                    ESCache.Instance.DirectEve.Session.CharacterId != null && ESCache.Instance.DirectEve.Session.CharacterId > 0)
                    if (Settings.CharacterXmlExists)
                    {
                        if (DateTime.UtcNow > Time.Instance.NextStartupAction)
                        {
                            try
                            {
                                if (!ESCache.Instance.OkToInteractWithEveNow)
                                {
                                    if (DebugConfig.DebugInteractWithEve) Log("CombatDontMoveController: RunOnce: OpenCargoHold: !OkToInteractWithEveNow");
                                    return;
                                }

                                if (DebugConfig.DebugCombatMissionsBehavior) Log("CombatDontMoveController: RunOnce");
                                ESCache.Instance.IterateShipTargetValues();
                                ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }

                            RunOnceAfterStartupAlreadyProcessed = true;
                        }
                    }
                    else
                    {
                        Log("Settings.Instance.CharacterName is still null");
                        Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
                        RunOnceAfterStartupAlreadyProcessed = false;
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
                //    if (DebugConfig.DebugCombatMissionsBehavior) Log("CombatDontMoveController: if (ESCache.Instance.InWarp)");
                //    return;
                //}

                if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("CombatDontMoveController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

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

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}