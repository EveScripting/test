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

namespace EVESharpCore.Questor.Behaviors
{
    public class DecideWhatToDoBehavior
    {
        #region Constructors

        public DecideWhatToDoBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields


        #endregion Fields

        #region Properties

        private static List<string> _enabledBehaviors = new List<string>();

        private static List<string> EnabledBehaviors
        {
            get
            {
                _enabledBehaviors = new List<string>();
                if (AbyssalDeadspaceBehavior.AbyssalDeadspaceControllerEnabled) _enabledBehaviors.Add("AbyssalDeadspaceController");
                //if (AbyssalDeadspaceBehavior.Enabled) _enabledBehaviors.Add("AbyssalDeadspaceController");
                //if (AbyssalDeadspaceBehavior.Enabled) _enabledBehaviors.Add("AbyssalDeadspaceController");
                //if (AbyssalDeadspaceBehavior.Enabled) _enabledBehaviors.Add("AbyssalDeadspaceController");
                //if (AbyssalDeadspaceBehavior.Enabled) _enabledBehaviors.Add("AbyssalDeadspaceController");
                //if (AbyssalDeadspaceBehavior.Enabled) _enabledBehaviors.Add("AbyssalDeadspaceController");
                return _enabledBehaviors ?? new List<string>();
            }
        }

        private static bool AbyssalDeadspaceControllerEnabled { get; set; }
        private static bool CareerAgentControllerEnabled { get; set; }
        private static bool CourierMissionControllerEnabled { get; set; }
        private static bool DumpLootControllerEnabled { get; set; }
        private static bool QuestorControllerEnabled { get; set; }
        private static bool WormHoleAnomolyControllerEnabled { get; set; }
        private static bool ProbeScanControllerEnabled { get; set; }
        private static bool PinataControllerEnabled { get; set; }
        private static bool MiningControllerEnabled { get; set; }

        #endregion Properties

        #region Methods

        public static bool ChangeDecideWhatToDoBehaviorState(DecideWhatToDoBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentDecideWhatToDoBehaviorState != _StateToSet)
                {
                    //if (_StateToSet == DecideWhatToDoBehaviorState.GotoHomeBookmark)
                    //{
                    //    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, "AbyssalPocketNumber", 0);
                    //    Traveler.Destination = null;
                    //    State.CurrentTravelerState = TravelerState.Idle;
                    //}

                    //if (_StateToSet == DecideWhatToDoBehaviorState.GotoAbyssalBookmark)
                    //{
                    //    Traveler.Destination = null;
                    //    State.CurrentTravelerState = TravelerState.Idle;
                    //}

                    //if (_StateToSet == DecideWhatToDoBehaviorState.ExecuteMission)
                    //{
                    //    State.CurrentCombatMissionCtrlState = ActionControlState.Start;
                    //    Traveler.Destination = null;
                    //    State.CurrentTravelerState = TravelerState.AtDestination;
                    //}

                    //Log.WriteLine("New DecideWhatToDoBehaviorState [" + _StateToSet + "]");
                    //State.CurrentDecideWhatToDoBehaviorState = _StateToSet;
                    //if (ESCache.Instance.InStation && !wait) ProcessState();
                    //if (State.CurrentDecideWhatToDoBehaviorState == DecideWhatToDoBehaviorState.GotoHomeBookmark) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            Log.WriteLine("LoadSettings: DecideWhatToDoBehavior");
            //
            // each behavior needs a setting BehaviorEnabled - True/False
            // If the setting for that behavior is enabled then when DecideWhatToDoBehaviorState == Undecided
            // we run each of the Behaviors CheckPrerequisites routine and see if any return true
            // for those that return true we add them to a list and... choose one somehow (randomly?)
            // then we set DecideWhatToDoBehaviorState == BehaviorChoosen
            // and wait for that behavior to set DecideWhatToDoBehaviorState == Undecided whenever it is ready to do so
            //
            // keep in mind that the individual CheckPrerequisites routine in each behavior can and SHOULD check if your in space
            // and check for ships, ammo, drones, whatever to determine if it is appropriate for that behavior to run
            // the idea being that if you run out of drones for missions you could still mine for instance
            //
            //
            AbyssalDeadspaceControllerEnabled = true;
            QuestorControllerEnabled = true;
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("State.CurrentAbyssalDeadspaceBehaviorState is [" + State.CurrentAbyssalDeadspaceBehaviorState + "]");

                switch (State.CurrentDecideWhatToDoBehaviorState)
                {
                    case DecideWhatToDoBehaviorState.Idle:
                        ChangeDecideWhatToDoBehaviorState(DecideWhatToDoBehaviorState.IsThereACachedBehaviorWeShouldChoose);
                        break;

                    case DecideWhatToDoBehaviorState.IsThereACachedBehaviorWeShouldChoose:
                        //
                        // lookup cached var in launcher to see what behaviro was last in use? datetime stamp and determine if it was reccent
                        //
                        // IsThereACachedBehaviorWeShouldChoose();
                        //
                        ChangeDecideWhatToDoBehaviorState(DecideWhatToDoBehaviorState.FindControllersWithMetPrerequisites);
                        break;

                    case DecideWhatToDoBehaviorState.FindControllersWithMetPrerequisites:
                        FindControllersWithMetPrerequisites();
                        break;

                    case DecideWhatToDoBehaviorState.ChooseController:
                        //SwitchCMBState();
                        break;

                    case DecideWhatToDoBehaviorState.Wait:
                        //ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Idle, false);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        //private static List<Controller> ControllersWithMetPrerequisites

        private static bool FindControllersWithMetPrerequisites()
        {
            //if (AbyssalDeadspaceBehavior.AbyssalDeadspaceControllerEnabled)
                //if ()

            return false;
        }

        private static bool EveryPulse()
        {
            try
            {
                if (ESCache.Instance.InStation)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("EveryPulse: if (ESCache.Instance.InStation)");
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("EveryPulse: UseScheduler [" + ESCache.Instance.EveAccount.UseScheduler + "]");
                    if (ESCache.Instance.EveAccount.UseScheduler)
                    {
                        if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("EveryPulse: UseScheduler [" + ESCache.Instance.EveAccount.UseScheduler + "] was true");
                        if (ESCache.Instance.EveAccount.ShouldBeStopped)
                        {
                            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("EveryPulse: if (ESCache.Instance.EveAccount.ShouldBeStopped) was true");
                            ESCache.Instance.CloseEveReason = "if (ESCache.Instance.EveAccount.ShouldBeStopped)";
                            ESCache.Instance.BoolCloseEve = true;
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        private static void IdleCMBState()
        {
        }

        public static void InvalidateCache()
        {
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("DecideWhatToDoBehavior.ResetStatesToDefaults: start");
            State.CurrentDecideWhatToDoBehaviorState = DecideWhatToDoBehaviorState.Idle;
            //State.CurrentArmState = ArmState.Idle;
            //State.CurrentUnloadLootState = UnloadLootState.Idle;
            //State.CurrentTravelerState = TravelerState.AtDestination;
            //Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return;
        }

        private static void StartCMBState()
        {
            //
            // It takes 20 minutes (potentially) to do an abyssal site: if it is within 25min of Downtime (10:35 evetime) pause
            //
            if (Time.Instance.IsItDuringDowntimeNow)
            {
                Log.WriteLine("AbyssalDeadspaceController: Arm: Downtime is less than 25 minutes from now: Pausing");
                ControllerManager.Instance.SetPause(true);
                return;
            }

            //ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Switch, false);
        }

        #endregion Methods
    }
}