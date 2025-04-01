extern alias SC;

using EVESharpCore.Cache;

using EVESharpCore.Logging;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using SC::SharedComponents.IPC;
using System;
using System.Linq;
using System.Xml.Linq;

namespace EVESharpCore.Questor.Behaviors
{
    public class SalvageGridBehavior
    {
        #region Constructors

        public SalvageGridBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields
        #endregion Fields

        #region Properties

        public static string HomeBookmarkName { get; set; }
        public static string SalvageGridBookmarkName { get; set; }

        #endregion Properties

        #region Methods

        public static bool ChangeSalvageGridBehaviorState(SalvageGridBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentSalvageGridBehaviorState != _StateToSet)
                {
                    if (_StateToSet == SalvageGridBehaviorState.GotoHomeBookmark)
                    {
                        //WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, "AbyssalPocketNumber", 0);
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                    }

                    if (_StateToSet == SalvageGridBehaviorState.SalvageGrid)
                    {
                        State.CurrentCombatMissionCtrlState = ActionControlState.Start;
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.AtDestination;
                    }

                    Log.WriteLine("New SalvageStateBehaviorState [" + _StateToSet + "]");
                    State.CurrentSalvageGridBehaviorState = _StateToSet;
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
            Log.WriteLine("LoadSettings: SalvageGridBehavior");

            SalvageGridBookmarkName =
                (string)CharacterSettingsXml.Element("salvageGridBookmarkName") ?? (string)CharacterSettingsXml.Element("salvageGridBookmarkName") ?? "salvagegrid";
            Log.WriteLine("SalvageGridBehavior: SalvageGridBookmarkName [" + SalvageGridBookmarkName + "]");
            HomeBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CharacterSettingsXml.Element("HomeBookmark") ??
                (string)CommonSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            Log.WriteLine("SalvageGridBehavior: HomeBookmarkName [" + HomeBookmarkName + "]");
        }

        public static void ProcessState()
        {
            try
            {
                if (DebugConfig.DebugSalvageGridBehavior) Log.WriteLine("State.CurrentSalvageGridBehaviorState is [" + State.CurrentSalvageGridBehaviorState + "]");

                switch (State.CurrentSalvageGridBehaviorState)
                {
                    case SalvageGridBehaviorState.Idle:
                        MissionSettings.MissionSafeDistanceFromStructure = 22000;
                        if (ESCache.Instance.InSpace && ESCache.Instance.InMission)
                            ChangeSalvageGridBehaviorState(SalvageGridBehaviorState.SalvageGrid);
                        break;

                    case SalvageGridBehaviorState.SalvageGrid:
                        Salvage.LootWhileSpeedTanking = true;
                        break;

                    case SalvageGridBehaviorState.GotoHomeBookmark:
                        GotoHomeBookmarkState();
                        break;

                    case SalvageGridBehaviorState.UnloadLoot:
                        UnloadLootCMBState();
                        break;

                    case SalvageGridBehaviorState.Default:
                        ChangeSalvageGridBehaviorState(SalvageGridBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void GotoHomeBookmarkState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "AbyssalDeadspaceBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: Traveler.TravelToBookmarkName(" + HomeBookmarkName + ");");

            Traveler.TravelToBookmarkName(HomeBookmarkName);

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("HomeBookmark is defined as [" + HomeBookmarkName + "] and should be a bookmark of a station or citadel we can dock at: why are we still in space?!");
                    return;
                }

                Traveler.Destination = null;
                ChangeSalvageGridBehaviorState(SalvageGridBehaviorState.Start, true);
            }
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("SalvageGridBehavior.ResetStatesToDefaults: start");
            State.CurrentSalvageGridBehaviorState = SalvageGridBehaviorState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("SalvageGridBehavior.ResetStatesToDefaults: done");
            return;
        }

        private static void UnloadLootCMBState()
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return;

                if (State.CurrentUnloadLootState == UnloadLootState.Idle)
                {
                    Log.WriteLine("UnloadLoot: Begin");
                    State.CurrentUnloadLootState = UnloadLootState.Begin;
                }

                UnloadLoot.ProcessState();

                if (State.CurrentUnloadLootState == UnloadLootState.Done)
                {
                    State.CurrentUnloadLootState = UnloadLootState.Idle;

                    if (State.CurrentCombatState == CombatState.OutOfAmmo)
                        Log.WriteLine("State.CurrentCombatState == CombatState.OutOfAmmo");

                    ChangeSalvageGridBehaviorState(SalvageGridBehaviorState.Arm, true);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Methods
    }
}