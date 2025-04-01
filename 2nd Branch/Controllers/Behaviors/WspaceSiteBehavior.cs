extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Questor.Behaviors
{
    public class WspaceSiteBehavior
    {
        #region Constructors

        private WspaceSiteBehavior()
        {
            //ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        #endregion Fields

        #region Properties

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

            ChangeWspaceSiteBehaviorState(WspaceSiteBehaviorState.Start, false);
        }

        public static bool ChangeWspaceSiteBehaviorState(WspaceSiteBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentWspaceSiteBehaviorState != _StateToSet)
                {
                    Log.WriteLine("New WspaceSiteBehaviorState [" + _StateToSet + "]");
                    State.CurrentWspaceSiteBehaviorState = _StateToSet;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
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
                Log.WriteLine("WSpaceSiteController: Arm: Downtime is less than 25 minutes from now: Pausing");
                ControllerManager.Instance.SetPause(true);
                return;
            }

            if (ESCache.Instance.InStation)
            {
                if (ESCache.Instance.EveAccount.ShouldBeStopped)
                {
                    ESCache.Instance.CloseEveReason = "if (ESCache.Instance.EveAccount.ShouldBeStopped)";
                    ESCache.Instance.BoolCloseEve = true;
                    return;
                }

                Log.WriteLine("WSpaceSiteController: InStation");
            }
        }

        private static bool EveryPulse()
        {
            try
            {
                if (ESCache.Instance.InStation)
                {
                    if (DirectEve.Interval(180000)) MemoryOptimizer.OptimizeMemory();
                }

                if (ESCache.Instance.InsidePosForceField)
                {
                    if (DirectEve.Interval(180000)) MemoryOptimizer.OptimizeMemory();
                }

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                    return false;

                NavigateOnGrid.NavigateInWSpace();
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                //if (DebugConfig.DebugWspaceSiteBehavior) Log.WriteLine("State.CurrentWspaceSiteBehaviorState is [" + State.CurrentWspaceSiteBehaviorState + "]");

                switch (State.CurrentWspaceSiteBehaviorState)
                {
                    case WspaceSiteBehaviorState.Idle:
                        MissionSettings.MissionSafeDistanceFromStructure = 22000;
                        IdleCMBState();
                        break;

                    case WspaceSiteBehaviorState.Start:
                        StartCMBState();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }

}