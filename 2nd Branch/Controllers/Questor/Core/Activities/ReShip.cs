extern alias SC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Questor.Activities
{
    public static class ReShip
    {
        #region Fields

        public static bool SwitchShipsOnly;
        private static int _fittingFitIterations;
        private static DateTime _lastArmAction { get; set; }
        private static DateTime _lastActivateShipAction { get; set; }
        private static DateTime _lastFitAction = DateTime.UtcNow;
        private static bool CustomFittingFound;

        private static bool DefaultFittingChecked;

        //false; //flag to check for the correct default fitting before using the fitting manager
        private static bool DefaultFittingFound;

        private static DateTime NextSwitchShipsRetry = DateTime.MinValue;
        private static int SwitchingShipRetries;

        #endregion Fields

        #region Properties

        #endregion Properties

        #region Methods

        public static bool ActivateShip(string shipName)
        {
            try
            {
                if (DateTime.UtcNow < _lastActivateShipAction.AddMilliseconds(ESCache.Instance.RandomNumber(4000, 5000)))
                    return false;

                //
                // is the ShipName is already the current ship? (we may have started in the right ship!)
                //

                if (ESCache.Instance.DirectEve.ActiveShip == null)
                {
                    Log.WriteLine("Activeship is null.");
                    return false;
                }

                if (shipName == null)
                {
                    Log.WriteLine("shipName == null.");
                    return false;
                }

                if (ESCache.Instance.DirectEve.ActiveShip.GivenName == null)
                {
                    Log.WriteLine("ESCache.Instance.DirectEve.ActiveShip.GivenName == null.");
                    return false;
                }

                if (ESCache.Instance.ActiveShip.GivenName.ToLower() == shipName.ToLower())
                    return true;

                if (NextSwitchShipsRetry > DateTime.UtcNow) return false;

                //
                // Check and warn the use if their config is hosed.
                //
                if (string.IsNullOrEmpty(Combat.Combat.CombatShipName) || string.IsNullOrEmpty(Settings.Instance.SalvageShipName))
                {
                    Log.WriteLine("if (string.IsNullOrEmpty(Combat.Combat.CombatShipName) || string.IsNullOrEmpty(Settings.Instance.SalvageShipName))");
                    if (!ChangeReShipState(ReShipState.MissingShip, true)) return false;
                    return false;
                }

                if (Combat.Combat.CombatShipName.ToLower() == Settings.Instance.SalvageShipName.ToLower())
                {
                    Log.WriteLine("CombatShipName cannot be set to the same name as SalvageShipName, change one of them. Setting ArmState to NotEnoughAmmo so that we do not potentially undock in the wrong ship");
                    if (!ChangeReShipState(ReShipState.MissingShip, true)) return false;
                    return false;
                }

                if (SwitchingShipRetries > 4)
                {
                    Log.WriteLine("Could not switch ship after 4 retries. Error.");
                    if (!ChangeReShipState(ReShipState.MissingShip, true)) return false;
                    return false;
                }

                //
                // we have the shipname configured but it is not the current ship
                //
                if (!string.IsNullOrEmpty(shipName))
                {
                    if (ESCache.Instance.ShipHangar == null)
                    {
                        Log.WriteLine("Arm: ActivateShip: if (ESCache.Instance.ShipHangar == null)");
                        return false;
                    }

                    List<DirectItem> shipsInShipHangar = ESCache.Instance.ShipHangar.ValidShipsToUse;
                    if (shipsInShipHangar.Any(s => s.ShipNameMatches(shipName)))
                    {
                        DirectItem ship = shipsInShipHangar.Find(s => s.ShipNameMatches(shipName));
                        if (ship != null)
                        {
                            Log.WriteLine("Making [" + ship.GivenName + "] active. Groupname [" + ship.GroupName + "] TypeName [" + ship.TypeName + "]");
                            ship.ActivateShip();
                            SwitchingShipRetries++;
                            NextSwitchShipsRetry = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(4, 6));
                            _lastActivateShipAction = DateTime.UtcNow;
                            return false;
                        }

                        return false;
                    }

                    if (ESCache.Instance.ShipHangar.Items.Count > 0)
                    {
                        Log.WriteLine("Unable to find a ship named [" + shipName.ToLower() + "] in this station. SelectedController [" + ESCache.Instance.SelectedController + "] Found the following ships:");
                        foreach (DirectItem shipInShipHangar in ESCache.Instance.ShipHangar.Items.Where(i => i.GivenName != null))
                            Log.WriteLine("GivenName [" + shipInShipHangar.GivenName.ToLower() + "] TypeName[" + shipInShipHangar.TypeName + "]");

                        if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GroupId == (int) Group.Capsule)
                        {
                            Log.WriteLine("Capsule detected... this shouldn't happen, disabling this instance.");

                            ESCache.Instance.DisableThisInstance();
                        }

                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                        {
                            ChangeReShipState(ReShipState.Idle, true);
                            CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                            return false;
                        }

                        if (!ChangeReShipState(ReShipState.MissingShip, true)) return false;
                        return false;
                    }

                    Log.WriteLine("No Ships?");
                    if (!ChangeReShipState(ReShipState.MissingShip, true)) return false;
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

        public static bool ChangeReShipState(ReShipState state, bool wait)
        {
            try
            {
                Log.WriteLine("ChangeArmState [" + state + "] CurrentArmState [" + State.CurrentArmState + "]");
                switch (state)
                {
                    case ReShipState.OpenShipHangar:
                        State.CurrentCombatState = CombatState.Idle;
                        break;

                    case ReShipState.FittingManagerHasFailed:
                        Log.WriteLine("case ArmState.FittingManagerHasFailed:");
                        ControllerManager.Instance.SetPause(true);
                        State.CurrentCombatState = CombatState.Idle;
                        break;
                }

                if (State.CurrentReShipState != state)
                {
                    ClearDataBetweenStates();
                    Log.WriteLine("New ReShipState [" + state + "]");
                    State.CurrentReShipState = state;
                    if (wait)
                        _lastArmAction = DateTime.UtcNow;
                    else
                        ProcessState();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void ClearDataBetweenStates()
        {

        } // check

        public static void InvalidateCache()
        {

        } // check

        public static bool LoadDefaultFitting(ReShipState nextReShipState)
        {
            try
            {
                if (_lastFitAction.AddMilliseconds(4000) > DateTime.UtcNow)
                {
                    Log.WriteLine("if (_lastFitAction.AddMilliseconds(4000) > DateTime.UtcNow)");
                    return false;
                }

                if (!DoesDefaultFittingExist())
                {
                    Log.WriteLine("if (!DoesDefaultFittingExist(WeAreInThisStateForLogs()))");
                    return false;
                }

                if (!DefaultFittingFound)
                {
                    Log.WriteLine("if (!DefaultFittingFound || UseMissionShip && !MissionSettings.ChangeMissionShipFittings)");
                    ChangeReShipState(nextReShipState, false);
                    return false;
                }

                if (ESCache.Instance.FittingManagerWindow == null)
                {
                    Log.WriteLine("if (ESCache.Instance.FittingManagerWindow == null)");
                    return false;
                }

                bool FoundFittingInGame = false;

                Log.WriteLine("Looking for saved fitting named: [" + MissionSettings.DefaultFittingName + "]");
                if (ESCache.Instance.FittingManagerWindow.Fittings.Any(i => i.Name.ToLower() == MissionSettings.DefaultFittingName.ToLower()))
                {
                    FoundFittingInGame = true;
                }

                if (!FoundFittingInGame)
                {
                    Log.WriteLine("Fitting: [" + MissionSettings.DefaultFittingName + " ] does not exist in game. Add it.");
                    return false;
                }

                foreach (DirectFitting fitting in ESCache.Instance.FittingManagerWindow.Fittings)
                {
                    //ok found it
                    DirectActiveShip currentShip = ESCache.Instance.ActiveShip;
                    if (MissionSettings.DefaultFittingName.ToLower().Equals(fitting.Name.ToLower()) && fitting.ShipTypeId == currentShip.TypeId)
                    {
                        Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                        Log.WriteLine("Found saved fitting named: [ " + fitting.Name + " ][" +
                                        Math.Round(Time.Instance.NextArmAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                        if (_fittingFitIterations > 3)
                        {
                            Log.WriteLine("Fitting Iterations [" + _fittingFitIterations + "]");
                            ChangeReShipState(ReShipState.FittingManagerHasFailed, false);
                        }
                        //switch to the requested fitting for the current mission
                        _fittingFitIterations++;
                        if (!fitting.Fit())
                        {
                            Log.WriteLine("if (!fitting.Fit())");
                            return false;
                        }
                        Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(7);
                        Log.WriteLine("Changing fitting to [" + fitting.Name + "] and waiting 5 seconds for the eve client to load the fitting (and move ammo or drones as needed)");
                        _lastArmAction = DateTime.UtcNow;
                        _lastFitAction = DateTime.UtcNow;
                        if (_fittingFitIterations > 1) MissionSettings.CurrentFit = fitting.Name;
                        MissionSettings.OfflineModulesFound = false;
                        MissionSettings.DamagedModulesFound = false;
                        CustomFittingFound = true;
                        return false;
                    }
                }

                if (_fittingFitIterations > 1) _fittingFitIterations = 0;

                Log.WriteLine("!if (Settings.Instance.UseFittingManager)...");
                ChangeReShipState(nextReShipState, false);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: ReShip");
                //BoosterTypesToConsumeInStation = new HashSet<long>();
                XElement boostersToConsumeInStationXml = CharacterSettingsXml.Element("boosterTypesToConsumeInStation") ?? CommonSettingsXml.Element("boosterTypesToConsumeInStation") ?? CharacterSettingsXml.Element("boosterTypes") ?? CommonSettingsXml.Element("boosterTypes");

                if (boostersToConsumeInStationXml != null)
                    foreach (XElement boosterToInject in boostersToConsumeInStationXml.Elements("boosterTypeToConsumeInStation"))
                    {
                        long booster = int.Parse(boosterToInject.Value);
                        DirectInvType boosterInvType = ESCache.Instance.DirectEve.GetInvType(int.Parse(boosterToInject.Value));
                        Log.WriteLine("Adding booster [" + boosterInvType.TypeName + "] to the list of boosters that will attempt to be injected during arm.");
                        //BoosterTypesToConsumeInStation.Add(booster);
                    }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Error Loading Booster Settings [" + exception + "]");
            }
        }

        public static void ProcessState()
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return;

                if (ESCache.Instance.InSpace)
                    return;

                if (Time.Instance.NextArmAction > DateTime.UtcNow)
                    return;

                if (ESCache.Instance.EveAccount.NotAllItemsCouldBeFitted)
                {
                    Log.WriteLine("notAllItemsCouldBeFitted window Detected via CleanupController. Setting ArmSate.LoadSavedFitting");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NotAllItemsCouldBeFitted), false);
                    ChangeReShipState(ReShipState.FittingManagerHasFailed, true);
                }

                if (DebugConfig.DebugArm) Log.WriteLine("State.CurrentArmState [" + State.CurrentArmState + "]");
                switch (State.CurrentArmState)
                {
                    case ArmState.Idle:
                        break;

                    case ArmState.Begin:
                        if (!BeginReShip()) break;
                        break;

                    case ArmState.LoadSavedFitting:
                        if (!LoadDefaultFitting(ReShipState.OnlineAllModules)) return;
                        break;

                    case ArmState.Done:
                        break;

                    case ArmState.NotEnoughDrones: //This is logged in questor.cs - do not double log, stay in this state until dislodged elsewhere
                        break;

                    case ArmState.NotEnoughAmmo: //This is logged in questor.cs - do not double log, stay in this state until dislodged elsewhere
                        break;

                    case ArmState.FittingManagerHasFailed: //This is logged in questor.cs - do not double log, stay in this state until dislodged elsewhere
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool BeginReShip() // --> ArmState.ActivateCombatShip
        {
            try
            {
                Time.Instance.LastReloadAttemptTimeStamp = new Dictionary<long, DateTime>();
                Time.Instance.LastReloadedTimeStamp = new Dictionary<long, DateTime>();
                DefaultFittingChecked = false; //flag to check for the correct default fitting before using the fitting manager
                DefaultFittingFound = false; //Did we find the default fitting?
                CustomFittingFound = false;
                SwitchShipsOnly = false;
                if (DebugConfig.DebugArm)
                    Log.WriteLine("Cache.Instance.BringOptionalMissionItemQuantity is [" + MissionSettings.MoveOptionalMissionItemQuantity + "]");
                SwitchingShipRetries = 0;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    Combat.Combat.LoadSettings(Settings.CharacterSettingsXml, Settings.CommonSettingsXml);

                //if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.FleetAbyssalDeadspaceController))
                //    Combat.Combat.LoadSettings(Settings.CharacterSettingsXml, Settings.CommonSettingsXml);

                State.CurrentCombatState = CombatState.Idle;
                _fittingFitIterations = 0;

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ArmCleanup()
        {
            try
            {
                if (Settings.Instance.UseFittingManager)
                {
                    if (ESCache.Instance.FittingManagerWindow != null)
                    {
                        ESCache.Instance.FittingManagerWindow.Close();
                        Statistics.LogWindowActionToWindowLog("FittingManager", "Closing FittingManager");
                        ESCache.Instance.FittingManagerWindow = null;
                        return true;
                    }
                }

                ChangeReShipState(ReShipState.Done, false);
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static bool DoesDefaultFittingExist()
        {
            try
            {
                DefaultFittingFound = false;
                if (!DefaultFittingChecked)
                {
                    if (DebugConfig.DebugFittingMgr)
                        Log.WriteLine("Character Settings XML says Default Fitting is [" + MissionSettings.DefaultFittingName + "]");

                    if (ESCache.Instance.FittingManagerWindow == null)
                    {
                        Log.WriteLine("FittingManagerWindow is null");
                        return false;
                    }

                    if (DebugConfig.DebugFittingMgr)
                        Log.WriteLine("Character Settings XML says Default Fitting is [" + MissionSettings.DefaultFittingName + "]");

                    if (ESCache.Instance.FittingManagerWindow.Fittings.Count > 0)
                    {
                        if (DebugConfig.DebugFittingMgr)
                            Log.WriteLine("if (Cache.Instance.FittingManagerWindow.Fittings.Any())");
                        int i = 1;
                        foreach (DirectFitting fitting in ESCache.Instance.FittingManagerWindow.Fittings)
                        {
                            //ok found it
                            if (DebugConfig.DebugFittingMgr)
                                Log.WriteLine("[" + i + "] Found a Fitting Named: [" + fitting.Name + "]");

                            if (fitting.Name.ToLower().Equals(MissionSettings.DefaultFittingName.ToLower()))
                            {
                                DefaultFittingChecked = true;
                                DefaultFittingFound = true;
                                Log.WriteLine("[" + i + "] Found Default Fitting [" + fitting.Name + "]");
                                return true;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Fittings found in the Fitting Manager at all!  Disabling fitting manager.");
                        DefaultFittingChecked = true;
                        DefaultFittingFound = false;
                        return true;
                    }

                    if (!DefaultFittingFound)
                    {
                        Log.WriteLine("Error! Could not find Default Fitting [" + MissionSettings.DefaultFittingName.ToLower() +
                                      "].  Disabling fitting manager.");
                        DefaultFittingChecked = true;
                        DefaultFittingFound = false;
                        Settings.Instance.UseFittingManager = false;
                        Log.WriteLine("Closing Fitting Manager");
                        if (ESCache.Instance.FittingManagerWindow != null)
                            ESCache.Instance.FittingManagerWindow.Close();

                        ChangeReShipState(ReShipState.Done, false);
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        #endregion Methods
    }
}