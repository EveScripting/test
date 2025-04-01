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
using System.Management.Instrumentation;

namespace EVESharpCore.Questor.Activities
{
    public static class Arm
    {
        #region Fields

        private static HashSet<long> BoosterTypesToConsumeInStation = new HashSet<long>();
        public static bool SwitchShipsOnly;
        private static DirectInvType _droneInvTypeItem;
        private static int _fittingFitIterations;
        private static int _itemsLeftToMoveQuantity;
        private static DateTime _lastArmAction { get; set; }
        private static DateTime _lastActivateShipAction { get; set; }
        private static DateTime _lastFitAction = DateTime.UtcNow;
        private static int AncillaryShieldBoosterScripts;
        private static bool bWaitingonScripts;
        private static int CapacitorInjectorScripts;
        private static IEnumerable<DirectItem> cargoItems;
        private static bool CustomFittingFound;

        private static bool DefaultFittingChecked;

        //false; //flag to check for the correct default fitting before using the fitting manager
        private static bool DefaultFittingFound;

        private static int DroneBayRetries;
        private static DirectItem fromContainerItem;
        private static IEnumerable<DirectItem> fromContainerItems;
        private static DateTime NextSwitchShipsRetry = DateTime.MinValue;
        private static int SensorBoosterScripts;
        private static int SensorDampenerScripts;
        private static int SwitchingShipRetries;
        private static int TrackingComputerScripts;
        private static int TrackingDisruptorScripts;
        private static int TrackingLinkScripts;
        private static bool UseMissionShip;
        private static int WeHaveThisManyOfThoseItemsInAmmoHangar;
        private static int WeHaveThisManyOfThoseItemsInCargo;
        private static int WeHaveThisManyOfThoseItemsInItemHangar;

        #endregion Fields

        #region Properties

        public static bool ArmLoadCapBoosters
        {
            get
            {
                try
                {
                    if (Settings.Instance.CapacitorInjectorScript != 0 && Settings.Instance.NumberOfCapBoostersToLoad != 0)
                        return true;

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private static DirectInvType DroneInvTypeItem
        {
            get
            {
                try
                {
                    if (_droneInvTypeItem == null)
                    {
                        if (DebugConfig.DebugArm)
                            Log.WriteLine(" Drones.DroneTypeID: " + Drones.DroneTypeID);

                        _droneInvTypeItem = ESCache.Instance.DirectEve.GetInvType(Drones.DroneTypeID);
                    }

                    return _droneInvTypeItem;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        #endregion Properties

        #region Methods

        private static int _moveCnt;

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
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, true, null)) return false;
                    return false;
                }

                if (Combat.Combat.CombatShipName.ToLower() == Settings.Instance.SalvageShipName.ToLower())
                {
                    Log.WriteLine("CombatShipName cannot be set to the same name as SalvageShipName, change one of them. Setting ArmState to NotEnoughAmmo so that we do not potentially undock in the wrong ship");
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, true, null)) return false;
                    return false;
                }

                if (SwitchingShipRetries > 4)
                {
                    Log.WriteLine("Could not switch ship after 4 retries. Error.");
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, true, null)) return false;
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

                    var PossibleCombatShips = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                 && i.GivenName != null
                                                 && i.GivenName == shipName);
                    if (PossibleCombatShips.Count() > 1)
                    {
                        var FirstCombatShip = PossibleCombatShips.FirstOrDefault();
                        if (PossibleCombatShips.All(i => i.TypeId != FirstCombatShip.TypeId))
                        {
                            Log.WriteLine("We have more than one CombatShip an they are not the same type of ship. Error!");
                            return false;
                        }
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
                            ChangeArmState(ArmState.Idle, true, null);
                            CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                            return false;
                        }

                        if (!ChangeArmState(ArmState.NotEnoughAmmo, true, null)) return false;
                        return false;
                    }

                    Log.WriteLine("No Ships?");
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, true, null)) return false;
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

        public static bool BringSpoilsOfWar()
        {
            if (_lastArmAction > DateTime.UtcNow.AddSeconds(1))
            {
                if (DebugConfig.DebugArm) Log.WriteLine("BringSpoilsOfWar: if (_lastArmAction > DateTime.UtcNow.AddSeconds(1))");
                return false;
            }

            // Open the item hangar (should still be open)
            if (ESCache.Instance.ItemHangar == null)
            {
                if (DebugConfig.DebugArm) Log.WriteLine("BringSpoilsOfWar: if (ESCache.Instance.ItemHangar == null)");
                return false;
            }

            if (ESCache.Instance.ItemHangar.Items.All(i => i.CategoryId != (int) CategoryID.Implant) || _moveCnt > 10)
            {
                if (DebugConfig.DebugArm) Log.WriteLine("BringSpoilsOfWar: if (ESCache.Instance.ItemHangar.Items.All(i => i.CategoryId != (int) CategoryID.Implant) || _moveCnt > 10)");
                _moveCnt = 0;
                return true;
            }

            // Yes, open the ships cargo
            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                if (DebugConfig.DebugArm) Log.WriteLine("if (Cache.Instance.CurrentShipsCargo == null)");
                return false;
            }

            // If we are not moving items
            if (ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("BringSpoilsOfWar"))
            {
                if (DebugConfig.DebugArm) Log.WriteLine("BringSpoilsOfWar: if (ESCache.Instance.DirectEve.GetLockedItems().Count == 0)");
                if (ESCache.Instance.ItemHangar.Items.Any(i => i.CategoryId == (int) CategoryID.Implant))
                {
                    if (DebugConfig.DebugArm) Log.WriteLine("BringSpoilsOfWar: if (ESCache.Instance.ItemHangar.Items.Any(i => i.CategoryId == (int) CategoryID.Implant))");
                    // Move all the implants to the cargo bay
                    foreach (DirectItem item in ESCache.Instance.ItemHangar.Items.Where(i => i.CategoryId == (int) CategoryID.Implant))
                    {
                        if (ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity - (item.Volume * item.Quantity) < 0)
                        {
                            Log.WriteLine("We are full, not moving anything else");
                            return true;
                        }

                        if (!ESCache.Instance.CurrentShipsCargo.Add(item, item.Quantity)) return false;
                        Log.WriteLine("Moving [" + item.TypeName + "][" + item.ItemId + "] to cargo");
                        _moveCnt++;
                        _lastArmAction = DateTime.UtcNow;
                        return false;
                    }

                    if (DebugConfig.DebugArm) Log.WriteLine("BringSpoilsOfWar: We are done processing implants from ItemHangar");
                    _lastArmAction = DateTime.UtcNow;
                    return false;
                }

                if (DebugConfig.DebugArm) Log.WriteLine("BringSpoilsOfWar: !if (ESCache.Instance.ItemHangar.Items.Any(i => i.CategoryId == (int) CategoryID.Implant))");
                return true;
            }

            if (DebugConfig.DebugArm) Log.WriteLine("BringSpoilsOfWar: if (ESCache.Instance.DirectEve.GetLockedItems().Count > 0)");
            return false;
        }

        public static bool ChangeArmState(ArmState state, bool wait, DirectAgent myAgent)
        {
            try
            {
                Log.WriteLine("ChangeArmState [" + state + "] CurrentArmState [" + State.CurrentArmState + "]");
                switch (state)
                {
                    case ArmState.OpenShipHangar:
                        State.CurrentCombatState = CombatState.Idle;
                        break;

                    case ArmState.NotEnoughAmmo:
                        Log.WriteLine("case ArmState.NotEnoughAmmo:");
                        ControllerManager.Instance.SetPause(true);
                        State.CurrentCombatState = CombatState.Idle;
                        break;

                    case ArmState.FittingManagerHasFailed:
                        Log.WriteLine("case ArmState.FittingManagerHasFailed:");
                        ControllerManager.Instance.SetPause(true);
                        State.CurrentCombatState = CombatState.Idle;
                        break;

                    case ArmState.MoveAbyssalDeadspaceFilament:
                        Log.WriteLine("Arm: Attempt to move a [" + AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName + "] from Item Hangar to my ships cargohold");
                        break;
                }

                if (State.CurrentArmState != state)
                {
                    ClearDataBetweenStates();
                    Log.WriteLine("New ArmState [" + state + "]");
                    State.CurrentArmState = state;
                    if (wait)
                        _lastArmAction = DateTime.UtcNow;
                    else
                        ProcessState(myAgent);
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
            _itemsLeftToMoveQuantity = 0;
        } // check

        public static bool ShouldWeStayDockedDueToNegaciveBoosterEffects()
        {
            try
            {
                List<NegativeBoosterEffect> NegativeBoosterEffects = ESCache.Instance.DirectEve.Me.GetAllNegativeBoosterEffects();

                if (NegativeBoosterEffects.Any())
                {
                    if (ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule) && NegativeBoosterEffects.Any(i => i == NegativeBoosterEffect.boosterShieldBoostAmountPenalty))
                    {
                        LogNegativeBoosterEffects(NegativeBoosterEffects);
                        Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(60);
                        return true; //staydocked
                    }

                    if (ESCache.Instance.Modules.Any(i => i.IsArmorRepairModule) && NegativeBoosterEffects.Any(i => i == NegativeBoosterEffect.boosterArmorRepairAmountPenalty))
                    {
                        LogNegativeBoosterEffects(NegativeBoosterEffects);
                        Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(60);
                        return true; //staydocked
                    }

                    if (!ESCache.Instance.Modules.Any(i => i.IsArmorRepairModule && i.IsShieldRepairModule) && NegativeBoosterEffects.Any(i => i == NegativeBoosterEffect.boosterShieldCapacityPenalty))
                    {
                        LogNegativeBoosterEffects(NegativeBoosterEffects);
                        Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(60);
                        return true; //staydocked
                    }

                    if (ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule) && NegativeBoosterEffects.Any(i => i == NegativeBoosterEffect.boosterCapacitorCapacityPenalty))
                    {
                        LogNegativeBoosterEffects(NegativeBoosterEffects);
                        Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(60);
                        return true; //staydocked
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void LogNegativeBoosterEffects(List<NegativeBoosterEffect> NegativeBoosterEffects)
        {
            Log.WriteLine("Negative Booster effects found: [" + NegativeBoosterEffects.Count + "]");
            foreach (NegativeBoosterEffect negBoosterEffect in NegativeBoosterEffects)
            {
                Log.WriteLine("Negative Booster effect found: [" + negBoosterEffect.ToString() + "]");
            }

            return;
        }

        public static bool ConsumeBoosters(DirectAgent myAgent = null) // --> ArmState.MoveMobileTractor
        {
            try
            {
                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(ESCache.Instance.RandomNumber(1500, 2000)))
                    return false;

                if (ESCache.Instance.ActiveShip.GroupId == (int) Group.Shuttle ||
                    ESCache.Instance.ActiveShip.GroupId == (int) Group.Industrial ||
                    ESCache.Instance.ActiveShip.GroupId == (int) Group.TransportShip ||
                    (myAgent != null && myAgent.Mission != null && MissionSettings.CourierMission(myAgent.Mission)) ||
                    ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
                {
                    Log.WriteLine("We are not in our combatship, no need to consume boosters");
                    ChangeArmState(ArmState.MoveBoosters, false, myAgent);
                    return false;
                }

                if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Items == null)
                    return false;

                if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.Any(i => i.TypeId == (int) TypeID.CivilianGatlingAutocannon
                                                      || i.TypeId == (int) TypeID.CivilianGatlingPulseLaser
                                                      || i.TypeId == (int) TypeID.CivilianGatlingRailgun
                                                      || i.TypeId == (int) TypeID.CivilianLightElectronBlaster))
                {
                    Log.WriteLine("No ammo needed for civilian guns: done");
                    ChangeArmState(ArmState.MoveBoosters, false, myAgent);
                    return false;
                }

                try
                {
                    if (MissionSettings.MissionBoosterTypes != null && MissionSettings.MissionBoosterTypes.Count > 0)
                        if (!CheckTheseBoosters(MissionSettings.MissionBoosterTypes, ESCache.Instance.AmmoHangar))
                            return false;

                    if (ESCache.Instance.ActiveShip.GivenName.ToLower() == Combat.Combat.CombatShipName.ToLower())
                        if (BoosterTypesToConsumeInStation != null && BoosterTypesToConsumeInStation.Count > 0)
                            if (!CheckTheseBoosters(BoosterTypesToConsumeInStation, ESCache.Instance.AmmoHangar))
                                return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error while processing Booster Itemhangar Items exception was: [" + exception + "]");
                }

                ChangeArmState(ArmState.MoveBoosters, false, myAgent);
                return false;
            }
            catch (Exception ex)
            {
                if (DebugConfig.DebugArm) Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void InvalidateCache()
        {
            _droneInvTypeItem = null;
            cargoItems = null;
            fromContainerItem = null;
            fromContainerItems = null;
        } // check

        public static bool LoadSavedFitting(string myFittingToLoad, ArmState nextArmState, DirectAgentMission myMission) // --> ArmState.MoveDrones
        {
            try
            {
                if (_lastFitAction.AddMilliseconds(4000) > DateTime.UtcNow)
                {
                    Log.WriteLine("if (_lastFitAction.AddMilliseconds(4000) > DateTime.UtcNow)");
                    return false;
                }

                DirectAgent myAgent = null;
                if (myMission != null)
                    myAgent = myMission.Agent;

                if (!Settings.Instance.UseFittingManager)
                {
                    Log.WriteLine("if (!Settings.Instance.UseFittingManager || ESCache.Instance.SelectedController == AbyssalDeadspaceController)");
                    ChangeArmState(nextArmState, false, myAgent);
                    return true;
                }

                try
                {
                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    {
                        if (myMission == null)
                        {
                            Log.WriteLine("if (myMission == null)");
                            ChangeArmState(nextArmState, false, myAgent);
                            return true;
                        }

                        if (ESCache.Instance.DirectEve.AgentMissions.Any(m => m.AgentId == myMission.Agent.AgentId) && ESCache.Instance.DirectEve.AgentMissions.Find(m => m.AgentId == myMission.Agent.AgentId).State != MissionState.Accepted)
                        {
                            Log.WriteLine("if (ESCache.Instance.DirectEve.AgentMissions.Any(m => m.AgentId == myMission.Agent.AgentId) && ESCache.Instance.DirectEve.AgentMissions.FirstOrDefault(m => m.AgentId == myMission.Agent.AgentId).State != MissionState.Accepted)");
                            ChangeArmState(nextArmState, false, myAgent);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    ChangeArmState(nextArmState, true, myAgent);
                    return true;
                }

                if (Settings.Instance.UseFittingManager) //&& MissionSettings.RegularMission != null)
                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    {
                        //let's check first if we need to change fitting at all
                        if (!string.IsNullOrEmpty(myFittingToLoad) && !string.IsNullOrEmpty(MissionSettings.CurrentFit) && myFittingToLoad.Equals(MissionSettings.CurrentFit))
                        {
                            Log.WriteLine("Correct fitting is already loaded.");
                            ChangeArmState(ArmState.MoveDrones, false, myAgent);
                            return true;
                        }

                        //let's check first if we need to change fitting at all
                        if (string.IsNullOrEmpty(myFittingToLoad))
                        {
                            Log.WriteLine("No fitting to load.");
                            ChangeArmState(nextArmState, false, myAgent);
                            return true;
                        }

                        if (!DoesDefaultFittingExist(myAgent))
                        {
                            Log.WriteLine("if (!DoesDefaultFittingExist(WeAreInThisStateForLogs()))");
                            return false;
                        }

                        if (!DefaultFittingFound || (UseMissionShip && !MissionSettings.ChangeMissionShipFittings))
                        {
                            Log.WriteLine("if (!DefaultFittingFound || UseMissionShip && !MissionSettings.ChangeMissionShipFittings)");
                            ChangeArmState(nextArmState, false, myAgent);
                            return false;
                        }

                        //let's check first if we need to change fitting at all
                        if (!string.IsNullOrEmpty(myFittingToLoad) && !string.IsNullOrEmpty(MissionSettings.CurrentFit) &&
                            myFittingToLoad.Equals(MissionSettings.CurrentFit))
                        {
                            Log.WriteLine("Correct fitting is already loaded.");
                            ChangeArmState(ArmState.MoveDrones, false, myAgent);
                            return true;
                        }

                        if (ESCache.Instance.FittingManagerWindow == null)
                        {
                            Log.WriteLine("if (ESCache.Instance.FittingManagerWindow == null)");
                            return false;
                        }

                        bool FoundFittingInGame = false;

                        if (myMission != null)
                        {
                            if (myMission.Faction == null)
                                return false;

                            Log.WriteLine("Looking for saved fitting named: [" + myFittingToLoad + "-" + myMission.Faction.Name + "]");

                            if (ESCache.Instance.FittingManagerWindow.Fittings.All(i => i.Name.ToLower() != myFittingToLoad.ToLower() + "-" + myMission.Faction))
                            {
                                FoundFittingInGame = true;
                                myFittingToLoad = myFittingToLoad + "-" + myMission.Faction.Name;
                            }

                            if (!FoundFittingInGame)
                            {
                                Log.WriteLine("Fitting named: [" + myFittingToLoad + "-" + myMission.Faction + " ] does not exist in game. Add it.");
                            }
                        }

                        Log.WriteLine("Looking for saved fitting named: [" + myFittingToLoad + "]");
                        if (ESCache.Instance.FittingManagerWindow.Fittings.Any(i => i.Name.ToLower() == myFittingToLoad.ToLower()))
                        {
                            FoundFittingInGame = true;
                        }

                        if (!FoundFittingInGame)
                        {
                            Log.WriteLine("Fitting named: [" + myFittingToLoad + " ] does not exist in game. Add it.");
                            Log.WriteLine("for now attempting to use the default fitting named [" + MissionSettings.DefaultFittingName + "]");
                            myFittingToLoad = MissionSettings.DefaultFittingName;
                        }

                        foreach (DirectFitting fitting in ESCache.Instance.FittingManagerWindow.Fittings)
                        {
                            //ok found it
                            DirectActiveShip currentShip = ESCache.Instance.ActiveShip;
                            if (myFittingToLoad.ToLower().Equals(fitting.Name.ToLower()) && fitting.ShipTypeId == currentShip.TypeId)
                            {
                                Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                                Log.WriteLine("Found saved fitting named: [ " + fitting.Name + " ][" +
                                              Math.Round(Time.Instance.NextArmAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                                if (_fittingFitIterations > 3)
                                {
                                    Log.WriteLine("Fitting Iterations [" + _fittingFitIterations + "]");
                                    ChangeArmState(ArmState.NotEnoughAmmo, false, myAgent);
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

                        //if we did not find it, we'll set currentfit to default
                        //this should provide backwards compatibility without trying to fit always
                        if (!CustomFittingFound)
                        {
                            if (UseMissionShip)
                            {
                                Log.WriteLine("Could not find fitting for this ship typeid.  Using current fitting.");
                                ChangeArmState(nextArmState, false, myAgent);
                                return false;
                            }

                            Log.WriteLine("Could not find fitting - using current");
                            ChangeArmState(nextArmState, false, myAgent);
                            return false;
                        }
                    }

                Log.WriteLine("!if (Settings.Instance.UseFittingManager)...");
                ChangeArmState(nextArmState, false, myAgent);
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
                Log.WriteLine("LoadSettings: Arm");
                BoosterTypesToConsumeInStation = new HashSet<long>();
                XElement boostersToConsumeInStationXml = CharacterSettingsXml.Element("boosterTypesToConsumeInStation") ?? CommonSettingsXml.Element("boosterTypesToConsumeInStation") ?? CharacterSettingsXml.Element("boosterTypes") ?? CommonSettingsXml.Element("boosterTypes");

                if (boostersToConsumeInStationXml != null)
                    foreach (XElement boosterToInject in boostersToConsumeInStationXml.Elements("boosterTypeToConsumeInStation"))
                    {
                        long booster = int.Parse(boosterToInject.Value);
                        DirectInvType boosterInvType = ESCache.Instance.DirectEve.GetInvType(int.Parse(boosterToInject.Value));
                        Log.WriteLine("Adding booster [" + boosterInvType.TypeName + "] to the list of boosters that will attempt to be injected during arm.");
                        BoosterTypesToConsumeInStation.Add(booster);
                    }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Error Loading Booster Settings [" + exception + "]");
            }
        }

        public static bool MoveItemsToCargo(DirectContainer fromContainer, DirectContainer toContainer, string itemName, int totalQuantityToMove, ArmState nextState,
            bool moveToNextStateIfQuantityIsBelowAsk, DirectAgent myAgent)
        {
            try
            {
                if (_lastArmAction.AddSeconds(1) > DateTime.UtcNow)
                    return false;

                if (string.IsNullOrEmpty(itemName))
                {
                    ChangeArmState(nextState, false, myAgent);
                    return false;
                }

                if (toContainer.WaitingForLockedItems()) return false;

                if (!LookForItem(fromContainer, itemName, toContainer))
                {
                    if (DebugConfig.DebugArm) Log.WriteLine("if (!LookForItem(fromContainer, itemName, CourierMissionToContainer))");
                    return false;
                }

                if (WeHaveThisManyOfThoseItemsInCargo + WeHaveThisManyOfThoseItemsInItemHangar < totalQuantityToMove)
                    if (!moveToNextStateIfQuantityIsBelowAsk)
                    {
                        Log.WriteLine("ItemHangar has: [" + WeHaveThisManyOfThoseItemsInItemHangar + "][" + itemName + "] we need [" + totalQuantityToMove +
                                      "] units)");
                        ControllerManager.Instance.SetPause(true);
                        ChangeArmState(ArmState.NotEnoughAmmo, false, myAgent);
                        return true;
                    }

                _itemsLeftToMoveQuantity = totalQuantityToMove - WeHaveThisManyOfThoseItemsInCargo > 0 ? totalQuantityToMove - WeHaveThisManyOfThoseItemsInCargo : 0;

                //  here we check if we have enough free m3 in our ship hangar

                if (toContainer == null)
                    return false;

                if (toContainer.UsedCapacity == null)
                    return false;

                if (fromContainerItem != null)
                {
                    int amountThatWillFitInToContainer = 0;
                    double freeCapacityOfToContainer = toContainer.Capacity - (double)toContainer.UsedCapacity;
                    amountThatWillFitInToContainer = Convert.ToInt32(freeCapacityOfToContainer / fromContainerItem.Volume);

                    _itemsLeftToMoveQuantity = Math.Min(amountThatWillFitInToContainer, _itemsLeftToMoveQuantity);

                    Log.WriteLine("Capacity [" + toContainer.Capacity + "] freeCapacity [" + freeCapacityOfToContainer + "] amount [" + amountThatWillFitInToContainer +
                                  "] _itemsLeftToMoveQuantity [" + _itemsLeftToMoveQuantity + "]");
                }
                else // we've got none of the item in our hangars, return true to move on
                {
                    Log.WriteLine("if (fromContainerItem == null)");
                    ChangeArmState(nextState, false, myAgent);
                    return true;
                }

                if (_itemsLeftToMoveQuantity <= 0)
                {
                    Log.WriteLine("if (_itemsLeftToMoveQuantity <= 0)");
                    ChangeArmState(nextState, false, myAgent);
                    return false;
                }

                Log.WriteLine("_itemsLeftToMoveQuantity: " + _itemsLeftToMoveQuantity);

                if (fromContainerItem != null && !string.IsNullOrEmpty(fromContainerItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                {
                    if (fromContainerItem.ItemId <= 0 || fromContainerItem.Volume == 0.00 || fromContainerItem.Quantity == 0)
                        return false;

                    int moveItemQuantity = Math.Min(fromContainerItem.Stacksize, _itemsLeftToMoveQuantity);
                    moveItemQuantity = Math.Max(moveItemQuantity, 1);
                    _itemsLeftToMoveQuantity -= moveItemQuantity;
                    bool movingItemsThereAreNoMoreItemsToGrabAtPickup = _itemsLeftToMoveQuantity > 0;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MovingItemsThereAreNoMoreItemsToGrabAtPickup), movingItemsThereAreNoMoreItemsToGrabAtPickup);
                    Log.WriteLine("Moving Item [" + fromContainerItem.TypeName + "] from FromContainer to CourierMissionToContainer: We have [" + _itemsLeftToMoveQuantity +
                                  "] more item(s) to move after this");
                    if (!toContainer.Add(fromContainerItem, moveItemQuantity)) return false;
                    _lastArmAction = DateTime.UtcNow;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void ProcessState(DirectAgent myAgent = null)
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
                    ChangeArmState(ArmState.LoadSavedFitting, true, myAgent);
                }

                /**
                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                {
                    bool? tempBool = AgentInteraction.ShouldWeDeclineThisMission(myAgent);
                    if (tempBool == null) return;
                    if ((bool)tempBool)
                    {
                        AgentInteraction.ChangeAgentInteractionState(AgentInteractionState.DeclineMission, myAgent, true);
                        AgentInteraction.ProcessState(myAgent);
                        return;
                    }
                }
                **/

                /**
                foreach (var window in ESCache.Instance.Windows)
                {
                    var notAllItemsCouldBeFitted = false;

                    if (!string.IsNullOrEmpty(window.Html))
                    {
                        notAllItemsCouldBeFitted |= window.Html.ToLower().Contains("Not all the items could be fitted".ToLower());
                    }

                    if (notAllItemsCouldBeFitted)
                    {
                        Log.WriteLine("[notAllItemsCouldBeFitted] Closing modal window...");
                        Log.WriteLine("Content of modal window (HTML): [" + window.Html.Replace("\n", "").Replace("\r", "") + "]");
                        _fittingFitIterations = 0;
                        ChangeArmState(ArmState.LoadSavedFitting);
                        window.Close();
                        return;
                    }
                }
                **/

                if (DebugConfig.DebugArm) Log.WriteLine("State.CurrentArmState [" + State.CurrentArmState + "]");
                switch (State.CurrentArmState)
                {
                    case ArmState.Idle:
                        break;

                    case ArmState.Begin:
                        if (!BeginArm()) break;
                        break;

                    case ArmState.ActivateCombatShip:
                        if (!ActivateCombatShip(myAgent)) return;
                        break;

                    case ArmState.ActivateScanningShip:
                        if (!ActivateScanningShip(myAgent)) return;
                        break;

                    case ArmState.ActivateMissionSpecificShip:
                        if (!ActivateMissionSpecificShip(myAgent)) return;
                        break;

                    case ArmState.RepairShop:
                        if (!RepairShop(myAgent)) return;
                        break;

                    case ArmState.StripFitting:
                        if (!StripFitting(myAgent)) return;
                        break;

                    case ArmState.LoadSavedFitting:
                        if (myAgent != null)
                        {
                            if (!LoadSavedFitting(MissionSettings.FittingToTryToLoad(myAgent.Mission), ArmState.MoveDrones, myAgent.Mission)) return;
                        }
                        else
                        {
                            if (!LoadSavedFitting(MissionSettings.DefaultFittingName, ArmState.MoveDrones, null)) return;
                        }

                        break;

                    case ArmState.MoveDrones:
                        if (!MoveDrones(myAgent)) return;
                        break;

                    case ArmState.MoveMissionItems:
                        if (!MoveMissionItems(myAgent)) return;
                        break;

                    case ArmState.MoveAbyssalDeadspaceFilament:
                        if (!MoveAbyssalDeadspaceFilamentItems(myAgent)) return;
                        break;

                    case ArmState.MoveOptionalItems:
                        if (!MoveOptionalItems(myAgent)) return;
                        break;

                    case ArmState.MoveScripts:
                        if (!MoveScripts(myAgent)) return;
                        break;

                    case ArmState.MoveCapBoosters:
                        if (!MoveCapBoosters(myAgent)) return;
                        break;

                    case ArmState.PrepareMoveAmmo:
                        if (!PrepareToMoveAmmo(myAgent)) return;
                        break;

                    case ArmState.PrepareToMoveToNewStation:
                        if (!PrepareToMoveToNewStation()) return;
                        break;

                    case ArmState.MoveAmmo:
                        if (!MoveAmmo(myAgent)) return;
                        break;

                    case ArmState.MoveMiningCrystals:
                        if (!MoveMiningCrystals()) return;
                        break;

                    case ArmState.ConsumeBoosters:
                        if (!ConsumeBoosters(myAgent)) return;
                        break;

                    case ArmState.MoveBoosters:
                        if (!MoveBoosters(myAgent)) return;
                        break;

                    case ArmState.MoveMobileTractor:
                        if (!MoveMobileTractorItems(myAgent)) return;
                        break;

                    case ArmState.StackAmmoHangar:
                        if (!StackAmmoHangar(myAgent)) return;
                        break;

                    case ArmState.Cleanup:
                        if (!ArmCleanup(myAgent)) return;
                        break;

                    case ArmState.Done:
                        break;

                    case ArmState.ActivateTransportShip:
                        if (!ActivateTransportShip(myAgent)) return;
                        break;

                    case ArmState.ActivateSalvageShip:
                        if (!ActivateSalvageShip(myAgent)) return;
                        break;

                    case ArmState.ActivateMiningShip:
                        if (!ActivateMiningShip()) return;
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

        public static bool RefreshMissionItems()
        {
            MissionSettings.FactionSpecificShip = null;
            MissionSettings.MissionItems.Clear();
            MissionSettings.MoveMissionItems = string.Empty;
            MissionSettings.MoveOptionalMissionItems = string.Empty;

            if (MissionSettings.MyMission != null && MissionSettings.MyMission.Faction == null)
            {
                Log.WriteLine("if (MissionSettings.MyMission != null && MissionSettings.MyMission.Faction == null)");
                return true;
            }

            if (!File.Exists(MissionSettings.MissionXmlPath(MissionSettings.MyMission)))
            {
                Log.WriteLine("if (!File.Exists(MissionSettings.MissionXmlPath(MissionSettings.MyMission)))");
                return true;
            }

            MissionSettings.LoadMissionXmlData(MissionSettings.MyMission);
            return true;
        }

        private static bool ActivateCombatShip(DirectAgent myAgent) // -> ArmState.RepairShop
        {
            try
            {
                if (string.IsNullOrEmpty(Combat.Combat.CombatShipName))
                {
                    Log.WriteLine("Could not find CombatShipName: " + Combat.Combat.CombatShipName + " in settings!");
                    ChangeArmState(ArmState.NotEnoughAmmo, false, myAgent);
                    return false;
                }

                if (!ActivateShip(Combat.Combat.CombatShipName))
                    return false;

                if (DateTime.UtcNow > _lastArmAction.AddSeconds(6))
                {
                    if (SwitchShipsOnly)
                    {
                        ChangeArmState(ArmState.Done, true, myAgent);
                        SwitchShipsOnly = false;
                        return true;
                    }

                    ChangeArmState(ArmState.RepairShop, false, myAgent);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ActivateMissionSpecificShip(DirectAgent myAgent) // -> ArmState.Done
        {
            try
            {
                if (DebugConfig.DebugArm) Log.WriteLine("ActivateMissionSpecificShip Started");
                if (MissionSettings.MissionSpecificShipName != null && !string.IsNullOrEmpty(MissionSettings.MissionSpecificShipName))
                {
                    Log.WriteLine("Could not find MissionSpecificShip defined.");
                    ChangeArmState(ArmState.ActivateCombatShip, false, myAgent);
                    return false;
                }

                if (!ActivateShip(MissionSettings.MissionSpecificShipName))
                    return false;

                ChangeArmState(ArmState.RepairShop, false, myAgent);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ActivateMiningShip()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.Instance.MiningShipName))
                {
                    Log.WriteLine("Could not find MiningShipName: " + Settings.Instance.MiningShipName + " in settings!");
                    ChangeArmState(ArmState.NotEnoughAmmo, true, null);
                    return false;
                }

                if (!ActivateShip(Settings.Instance.MiningShipName)) return false;

                Log.WriteLine("Done");
                ChangeArmState(ArmState.Cleanup, true, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ActivateSalvageShip(DirectAgent myAgent)
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.Instance.SalvageShipName))
                {
                    Log.WriteLine("Could not find salvageshipName: " + Settings.Instance.SalvageShipName + " in settings!");
                    ChangeArmState(ArmState.NotEnoughAmmo, true, myAgent);
                    return false;
                }

                if (!ActivateShip(Settings.Instance.SalvageShipName)) return false;

                Log.WriteLine("Done");
                ChangeArmState(ArmState.Cleanup, false, myAgent);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ActivateScanningShip(DirectAgent myAgent) // -> ArmState.RepairShop
        {
            try
            {
                if (string.IsNullOrEmpty(Combat.Combat.ScanningShipName))
                {
                    Log.WriteLine("Could not find CombatShipName: " + Combat.Combat.ScanningShipName + " in settings!");
                    ChangeArmState(ArmState.NotEnoughAmmo, false, myAgent);
                    return false;
                }

                if (!ActivateShip(Combat.Combat.ScanningShipName))
                    return false;

                if (DateTime.UtcNow > _lastArmAction.AddSeconds(6))
                {
                    if (SwitchShipsOnly)
                    {
                        ChangeArmState(ArmState.Done, false, myAgent);
                        SwitchShipsOnly = false;
                        return true;
                    }

                    ChangeArmState(ArmState.RepairShop, false, myAgent);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ActivateTransportShip(DirectAgent myAgent) // check
        {
            if (string.IsNullOrEmpty(Settings.Instance.TransportShipName))
            {
                Log.WriteLine("Could not find transportshipName in settings!");
                ChangeArmState(ArmState.NotEnoughAmmo, true, myAgent);
                return false;
            }

            if (!ActivateShip(Settings.Instance.TransportShipName)) return false;

            Log.WriteLine("Done");
            ChangeArmState(ArmState.Cleanup, false, myAgent);
            return true;
        }

        private static bool BeginArm(DirectAgent myAgent = null) // --> ArmState.ActivateCombatShip
        {
            try
            {
                Time.Instance.LastReloadAttemptTimeStamp = new Dictionary<long, DateTime>();
                Time.Instance.LastReloadedTimeStamp = new Dictionary<long, DateTime>();
                UseMissionShip = false; // Were we successful in activating the mission specific ship?
                DefaultFittingChecked = false; //flag to check for the correct default fitting before using the fitting manager
                DefaultFittingFound = false; //Did we find the default fitting?
                CustomFittingFound = false;
                SwitchShipsOnly = false;
                if (DebugConfig.DebugArm)
                    Log.WriteLine("Cache.Instance.BringOptionalMissionItemQuantity is [" + MissionSettings.MoveOptionalMissionItemQuantity + "]");
                DroneBayRetries = 0;
                SwitchingShipRetries = 0;

                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && MissionSettings.MyMission != null)
                {
                    if (!RefreshMissionItems()) return false;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    Combat.Combat.LoadSettings(Settings.CharacterSettingsXml, Settings.CommonSettingsXml);

                //if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.FleetAbyssalDeadspaceController))
                //    Combat.Combat.LoadSettings(Settings.CharacterSettingsXml, Settings.CommonSettingsXml);

                State.CurrentCombatState = CombatState.Idle;
                _fittingFitIterations = 0;

                if (myAgent != null)
                    Log.WriteLine("Arm.Begin: RegularMission.Type [" + myAgent.Mission.Type + "]");

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.MiningController))
                {
                    ChangeArmState(ArmState.MoveMiningCrystals, true, null);
                    return true;
                }

                if (myAgent != null && MissionSettings.CourierMission(myAgent.Mission))
                    ChangeArmState(ArmState.ActivateTransportShip, false, myAgent);
                else
                    ChangeArmState(ArmState.ActivateCombatShip, false, myAgent);

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static int BoosterSlot(DirectItem booster)
        {
            if (booster.GroupId != (int) Group.Booster)
                return 0;

            //
            // Slot # 1
            // Blue Pill
            // X-Instinct
            // Mindflood
            // Exile
            //
            if (booster.TypeId == 10155 ||
                booster.TypeId == 10156 ||
                booster.TypeId == 9950 ||
                booster.TypeId == 15457 ||
                booster.TypeId == 15458 ||
                booster.TypeId == 15459 ||
                booster.TypeId == 15463 ||
                booster.TypeId == 15464 ||
                booster.TypeId == 15465 ||
                booster.TypeId == 15479 ||
                booster.TypeId == 15480 ||
                booster.TypeId == 25349 ||
                booster.TypeId == 28670 ||
                booster.TypeId == 28676 ||
                booster.TypeId == 28680 ||
                booster.TypeId == 28682 ||
                booster.TypeId == 3898
            )
                return 1;

            //
            // Slot # 2
            // Sooth Sayer
            // Frenix
            // Drop
            //
            if (booster.TypeId == 10164 ||
                booster.TypeId == 10165 ||
                booster.TypeId == 10166 ||
                booster.TypeId == 15460 ||
                booster.TypeId == 15461 ||
                booster.TypeId == 15462 ||
                booster.TypeId == 15466 ||
                booster.TypeId == 15467 ||
                booster.TypeId == 15468 ||
                booster.TypeId == 28674 ||
                booster.TypeId == 28678 ||
                booster.TypeId == 28684
            )
                return 2;

            //
            // Slot # 3
            // Crash
            //
            if (booster.TypeId == 10151 ||
                booster.TypeId == 10152 ||
                booster.TypeId == 9947 ||
                booster.TypeId == 28672
            )
                return 3;

            ///
            /// Slot #10
            ///
            /// if (_booster.TypeId == )
            /// {
            ///     return 10;
            /// }
            return 255;
        }

        public static bool CheckTheseBoosters(HashSet<long> boostersToCheck, DirectContainer containerToPullBoostersFrom)
        {
            try
            {
                if (containerToPullBoostersFrom != null && containerToPullBoostersFrom.Items != null && containerToPullBoostersFrom.Items.Count > 0)
                {
                    if (containerToPullBoostersFrom.Items.Any(i => boostersToCheck.Contains(i.TypeId)))
                    {
                        int boosterNumber = 0;
                        foreach (DirectItem boosterDirectItem in containerToPullBoostersFrom.Items.Where(i => boostersToCheck.Contains(i.TypeId)))
                        {
                            boosterNumber++;
                            try
                            {
                                Log.WriteLine("[" + boosterNumber + "][" + boosterDirectItem.TypeName + "] Slot [" + BoosterSlot(boosterDirectItem) + "]");
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }

                            switch (BoosterSlot(boosterDirectItem))
                            {
                                case 1:
                                    if (SafeToConsumeBoosters())
                                    {
                                        if (ReallyConsumeBooster(boosterNumber, boosterDirectItem, "LastConsumeBoosterSlot1"))
                                            return false;

                                        return false;
                                    }

                                    break;

                                case 2:
                                    if (SafeToConsumeBoosters())
                                    {
                                        if (ReallyConsumeBooster(boosterNumber, boosterDirectItem, "LastConsumeBoosterSlot2"))
                                            return false;

                                        return false;
                                    }

                                    break;

                                case 3:
                                    if (SafeToConsumeBoosters())
                                    {
                                        if (ReallyConsumeBooster(boosterNumber, boosterDirectItem, "LastConsumeBoosterSlot3"))
                                            return false;

                                        return false;
                                    }

                                    break;

                                case 4:
                                    if (SafeToConsumeBoosters())
                                    {
                                        if (ReallyConsumeBooster(boosterNumber, boosterDirectItem, "LastConsumeBoosterSlot4"))
                                            return false;

                                        return false;
                                    }

                                    break;

                                case 10:
                                    if (SafeToConsumeBoosters())
                                    {
                                        if (ReallyConsumeBooster(boosterNumber, boosterDirectItem, "LastConsumeBoosterSlot10"))
                                            return false;

                                        return false;
                                    }

                                    break;

                                case 255:
                                    if (SafeToConsumeBoosters())
                                    {
                                        if (ReallyConsumeBooster(boosterNumber, boosterDirectItem, "LastConsumeBoosterSlot255"))
                                            return false;

                                        return false;
                                    }

                                    break;

                                default:
                                    if (SafeToConsumeBoosters())
                                        if (ReallyConsumeBooster(boosterNumber, boosterDirectItem, "LastConsumeBoosterSlotUnknown"))
                                            return false;

                                    break;
                            }
                        }

                        return true;
                    }

                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ArmCleanup(DirectAgent myAgent)
        {
            try
            {
                if (ESCache.Instance.Windows.OfType<DirectFittingManagerWindow>().Any())
                {
                    ESCache.Instance.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault().Close();
                    Statistics.LogWindowActionToWindowLog("FittingManager", "Closing FittingManager");
                    return true;
                }

                ChangeArmState(ArmState.Done, false, myAgent);
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static bool DoesDefaultFittingExist(DirectAgent myAgent)
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

                        ChangeArmState(ArmState.MoveMissionItems, false, myAgent);
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

        private static bool FillCargoWithAmmo(Dictionary<int, int> moveToCargoList, ArmState nextArmState)
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return false;

                if (ESCache.Instance.AmmoHangar == null)
                    return false;

                if (ESCache.Instance.CurrentShipsCargo == null)
                    return false;

                IEnumerable<DirectItem> itemsThatCouldBeMoved = ESCache.Instance.AmmoHangar.Items.Where(i => moveToCargoList.ContainsKey(i.TypeId)).ToList();

                if (itemsThatCouldBeMoved.Any())
                {
                    DirectItem itemToPotentiallyMove = itemsThatCouldBeMoved.FirstOrDefault();

                    if (itemToPotentiallyMove != null)
                    {
                        int maxUnitsBasedOnVolumeToTryToMove = Math.Min(itemToPotentiallyMove.Stacksize, moveToCargoList[itemToPotentiallyMove.TypeId]);

                        if (ESCache.Instance.CurrentShipsCargo.Capacity == 0 && ESCache.Instance.CurrentShipsCargo.UsedCapacity == 0)
                        {
                            Log.WriteLine("FillCargoWithAmmo: CurrentShipsCargo.Capacity == 0");
                            _lastArmAction = DateTime.UtcNow;
                            return false;
                        }

                        if (ESCache.Instance.CurrentShipsCargo.FreeCapacity == 0 && ESCache.Instance.CurrentShipsCargo.Items.Count == 0)
                        {
                            Log.WriteLine("FillCargoWithAmmo: if (myCurrentShipFreeCargo == 0 && !ESCache.Instance.CurrentShipsCargo.Items.Any())");
                            _lastArmAction = DateTime.UtcNow;
                        }

                        DirectInvType itemInvType = ESCache.Instance.DirectEve.GetInvType(itemToPotentiallyMove.TypeId);
                        Log.WriteLine("myCurrentShipFreeCargo [" + ESCache.Instance.CurrentShipsCargo.FreeCapacity + "] itemInvType [" + itemInvType.TypeName + "] Volume each [" + itemInvType.Volume + "] TypeID [" + itemInvType.TypeId + "] ");
                        if (ESCache.Instance.CurrentShipsCargo.FreeCapacity > 0 && itemInvType.Volume > 0)
                        {
                            int numofUnitsThatWillFitinCargo = (int) Math.Round((double)ESCache.Instance.CurrentShipsCargo.FreeCapacity / itemInvType.Volume, 0);
                            int maxVolumeToMove = Math.Min(maxUnitsBasedOnVolumeToTryToMove, numofUnitsThatWillFitinCargo);
                            if (maxVolumeToMove >= moveToCargoList[itemToPotentiallyMove.TypeId])
                                moveToCargoList.Remove(itemToPotentiallyMove.TypeId);

                            maxVolumeToMove = Math.Max(1, maxVolumeToMove);

                            if (numofUnitsThatWillFitinCargo <= 0)
                            {
                                Log.WriteLine("Nothing else will fit in the Ships CargoHold");
                                ChangeArmState(nextArmState, false, null);
                                return true;
                            }

                            if (ESCache.Instance.CurrentShipsCargo.FreeCapacity <= 5)
                            {
                                Log.WriteLine("We havent yet totally filled up, but weo do have less than 5 m3 free.to avoid getting stuck we are going to consider this full enough");
                                ChangeArmState(nextArmState, false, null);
                                return true;
                            }

                            if (maxVolumeToMove * itemInvType.Volume > ESCache.Instance.CurrentShipsCargo.FreeCapacity)
                            {
                                Log.WriteLine("We were trying to move: ammo [" + itemToPotentiallyMove.TypeName + "] quantity [" + maxVolumeToMove + "] to cargohold: but that many units will not fit (bad math logic?!)");
                                return false;
                            }

                            if (DirectEve.Interval(8000))
                            {
                                if (!ESCache.Instance.CurrentShipsCargo.Add(itemToPotentiallyMove, maxVolumeToMove)) return false;
                                Log.WriteLine("Moving ammo [" + itemToPotentiallyMove.TypeName + "] quantity [" + maxVolumeToMove + "] to cargohold: Cargohold has [" + ESCache.Instance.CurrentShipsCargo.FreeCapacity + "] m3 free");
                            }

                            _lastArmAction = DateTime.UtcNow;
                            return false;
                        }
                    }
                }

                Log.WriteLine("Done moving ammo to cargohold");
                ChangeArmState(nextArmState, false, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool LookForItem(DirectContainer fromContainer, string itemToFind, DirectContainer hangarToCheckForItemsdWeAlreadyMoved)
        {
            try
            {
                WeHaveThisManyOfThoseItemsInCargo = 0;
                WeHaveThisManyOfThoseItemsInItemHangar = 0;
                WeHaveThisManyOfThoseItemsInAmmoHangar = 0;
                cargoItems = new List<DirectItem>();

                fromContainerItems = new List<DirectItem>();
                fromContainerItem = null;

                // check the local cargo for items and subtract the items in the cargo from the quantity we still need to move to our cargohold
                //
                if (hangarToCheckForItemsdWeAlreadyMoved != null && hangarToCheckForItemsdWeAlreadyMoved.Items.Count > 0)
                {
                    cargoItems =
                        hangarToCheckForItemsdWeAlreadyMoved.Items.Where(i => (i.TypeName ?? string.Empty).ToLower().Equals(itemToFind.ToLower())).ToList();
                    WeHaveThisManyOfThoseItemsInCargo = cargoItems.Sum(i => i.Stacksize);
                    //do not return here
                }

                //
                // check itemhangar for the item
                //
                try
                {
                    if (fromContainer == null) return false;
                    if (fromContainer.Items.Count > 0)
                        if (fromContainer.Items.Any(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()))
                        {
                            fromContainerItems =
                                fromContainer.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()).ToList();
                            fromContainerItem = fromContainerItems.OrderBy(s => s.Stacksize).FirstOrDefault();
                            WeHaveThisManyOfThoseItemsInItemHangar = fromContainerItems.Sum(i => i.Stacksize);
                            if (DebugConfig.DebugArm)
                                Log.WriteLine("We have [" + WeHaveThisManyOfThoseItemsInItemHangar + "] [" + itemToFind + "] in ItemHangar");
                            return true;
                        }
                }
                catch (Exception ex)
                {
                    if (DebugConfig.DebugArm) Log.WriteLine("Exception [" + ex + "]");
                }

                return true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        private static bool MoveAbyssalDeadspaceFilamentItems(DirectAgent myAgent)
        {
            if (!MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName, AbyssalDeadspaceBehavior.numAbyssalFillamentsToBring,
                ArmState.MoveOptionalItems, true, myAgent)) return false;

            if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.All(i => i.TypeName.Contains(AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName)))
            {
                //
                // we are 'out of ammo' here. we should go buy more, or at least pause
                //
                Log.WriteLine("We did not find a [" + AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName + "] in the item hangar. pausing");
                ESCache.Instance.PauseAfterNextDock = true;
            }

            return false;
        }

        private static bool MoveMiningCrystals()
        {
            try
            {
                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(ESCache.Instance.RandomNumber(1500, 2000)))
                    return false;

                /**
                if (ESCache.Instance.ActiveShip.GroupId == (int)GroupID.Shuttle ||
                    ESCache.Instance.ActiveShip.GroupId == (int)GroupID.Industrial ||
                    ESCache.Instance.ActiveShip.GroupId == (int)GroupID.TransportShip ||
                    MissionSettings.CourierMission(myAgent) ||
                    ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
                {
                    Log.WriteLine("We are not in our combatship, no need to consume boosters");
                    ChangeArmState(ArmState.MoveMobileTractor);
                    return false;
                }
                **/

                if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Items == null)
                    return false;

                if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.Any(i => i.TypeId == (int)TypeID.CivilianGatlingAutocannon
                                                      || i.TypeId == (int)TypeID.CivilianGatlingPulseLaser
                                                      || i.TypeId == (int)TypeID.CivilianGatlingRailgun
                                                      || i.TypeId == (int)TypeID.CivilianLightElectronBlaster))
                {
                    Log.WriteLine("No mining crystals needed if you have civilian guns: done");
                    ChangeArmState(ArmState.MoveMobileTractor, true, null);
                    return false;
                }

                try
                {
                    if (ESCache.Instance.ActiveShip.GivenName.ToLower() == Settings.Instance.MiningShipName.ToLower())
                    {
                        //
                        // load mining crystals here
                        //
                    }
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error while processing Booster Itemhangar Items exception was: [" + exception + "]");
                }

                ChangeArmState(ArmState.MoveDrones, true, null);
                return false;
            }
            catch (Exception ex)
            {
                if (DebugConfig.DebugArm) Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }
        private static bool MoveAmmo(DirectAgent myAgent) // --> ArmState.ConsumeBoosters
        {
            try
            {
                if (_lastArmAction.AddSeconds(1) > DateTime.UtcNow)
                    return false;

                AmmoType missing = null;
                bool ammoMissing = false;
                foreach (AmmoType ammo in DirectUIModule.DefinedAmmoTypes)
                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == ammo.TypeId && !i.IsSingleton && i.Quantity >= ammo.Quantity))
                    {
                        missing = ammo;
                        ammoMissing = true;
                    }

                if (!ammoMissing)
                {
                    Log.WriteLine("We have no more ammo types to be loaded. We are finished moving Ammo.");
                    ChangeArmState(ArmState.ConsumeBoosters, false, myAgent);
                    return true;
                }

                AmmoType CurrentAmmoTypeToLoad = missing;

                if (CurrentAmmoTypeToLoad == null)
                {
                    Log.WriteLine("We have no more ammo types to be loaded. We are finished moving Ammo.");
                    ChangeArmState(ArmState.ConsumeBoosters, false, myAgent);
                    return false;
                }

                try
                {
                    List<DirectItem> AmmoHangarItems = null;
                    IEnumerable<DirectItem> AmmoItems = null;
                    if (ESCache.Instance.AmmoHangar != null && ESCache.Instance.AmmoHangar.Items != null)
                    {
                        if (DebugConfig.DebugArm) Log.WriteLine("if (Cache.Instance.AmmoHangar != null && Cache.Instance.AmmoHangar.Items != null)");
                        AmmoHangarItems =
                            ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == CurrentAmmoTypeToLoad.TypeId)
                                .OrderBy(i => !i.IsSingleton)
                                .ThenByDescending(i => i.Quantity)
                                .ToList();
                        AmmoItems = AmmoHangarItems.ToList();
                    }

                    if (AmmoHangarItems == null)
                    {
                        _lastArmAction = DateTime.UtcNow;
                        Log.WriteLine("if(AmmoHangarItems == null)");
                        return false;
                    }

                    if (DebugConfig.DebugArm)
                        Log.WriteLine("Ammohangar has [" + AmmoHangarItems.Count + "] items with the right typeID [" + CurrentAmmoTypeToLoad.TypeId +
                                      "] for this ammoType. MoveAmmo will use AmmoHangar");

                    try
                    {
                        int itemnum = 0;

                        if (AmmoItems != null)
                        {
                            AmmoItems = AmmoItems.ToList();
                            if (AmmoItems.Any())
                                foreach (DirectItem item in AmmoItems)
                                {
                                    itemnum++;
                                    int moveAmmoQuantity = Math.Min(item.Stacksize, CurrentAmmoTypeToLoad.Quantity);

                                    moveAmmoQuantity = Math.Max(moveAmmoQuantity, 1);

                                    if (DebugConfig.DebugArm)
                                        Log.WriteLine("In Hangar we have: [" + itemnum + "] TypeName [" + item.TypeName + "] StackSize [" +
                                                      item.Stacksize +
                                                      "] - CurrentAmmoToLoad.Quantity [" + CurrentAmmoTypeToLoad.Quantity + "] Actual moveAmmoQuantity [" +
                                                      moveAmmoQuantity +
                                                      "]");

                                    if (moveAmmoQuantity <= item.Stacksize && moveAmmoQuantity >= 1)
                                    {
                                        Log.WriteLine("Moving [" + moveAmmoQuantity + "] units of DefinedAmmoTypes  [" + item.TypeName +
                                                      "] from [ AmmoHangar ] to CargoHold");
                                        //
                                        // move items to cargo
                                        //
                                        if (!ESCache.Instance.CurrentShipsCargo.Add(item, moveAmmoQuantity)) return false;
                                        _lastArmAction = DateTime.UtcNow;

                                        //
                                        // subtract the moved items from the items that need to be moved
                                        //

                                        CurrentAmmoTypeToLoad.Quantity -= moveAmmoQuantity;
                                    }
                                    else
                                    {
                                        Log.WriteLine("While calculating what to move we wanted to move [" + moveAmmoQuantity + "] units of DefinedAmmoTypes  [" +
                                                      item.TypeName +
                                                      "] from [ AmmoHangar ] to CargoHold, but somehow the current Item Stacksize is only [" +
                                                      item.Stacksize + "]");
                                        continue;
                                    }

                                    return false; //you can only move one set of items per frame.
                                }
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.WriteLine("AmmoItems Exception [" + exception + "]");
                    }
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error while processing Itemhangar Items exception was: [" + exception + "]");
                }

                ChangeArmState(ArmState.ConsumeBoosters, false, myAgent);
                return false;
            }
            catch (Exception ex)
            {
                if (DebugConfig.DebugArm) Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool MoveBoosters(DirectAgent myAgent)
        {
            try
            {
                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(ESCache.Instance.RandomNumber(1500, 2000)))
                    return false;

                /**
                if (ESCache.Instance.ActiveShip.GroupId == (int)GroupID.Shuttle ||
                    ESCache.Instance.ActiveShip.GroupId == (int)GroupID.Industrial ||
                    ESCache.Instance.ActiveShip.GroupId == (int)GroupID.TransportShip ||
                    MissionSettings.CourierMission(myAgent) ||
                    ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
                {
                    Log.WriteLine("We are not in our combatship, no need to consume boosters");
                    ChangeArmState(ArmState.MoveMobileTractor);
                    return false;
                }
                **/

                if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Items == null)
                    return false;

                if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.Any(i => i.TypeId == (int) TypeID.CivilianGatlingAutocannon
                                                      || i.TypeId == (int) TypeID.CivilianGatlingPulseLaser
                                                      || i.TypeId == (int) TypeID.CivilianGatlingRailgun
                                                      || i.TypeId == (int) TypeID.CivilianLightElectronBlaster))
                {
                    Log.WriteLine("No ammo needed for civilian guns: done");
                    ChangeArmState(ArmState.MoveMobileTractor, false, myAgent);
                    return false;
                }

                try
                {
                    if (ESCache.Instance.ActiveShip.GivenName.ToLower() == Combat.Combat.CombatShipName.ToLower())
                        if (Defense.BoosterTypesToLoadIntoCargo != null && Defense.BoosterTypesToLoadIntoCargo.Count > 0)
                            foreach (long boosterTypeIdToLoadIntoCargo in Defense.BoosterTypesToLoadIntoCargo)
                            {
                                DirectItem tempBooster = new DirectItem(ESCache.Instance.DirectEve)
                                {
                                    TypeId = (int)boosterTypeIdToLoadIntoCargo
                                };

                                string boosterNameToLoad = tempBooster.TypeName;
                                if (!MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, boosterNameToLoad, 1, ArmState.MoveMobileTractor, false, myAgent)) return false;
                            }
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error while processing Booster Itemhangar Items exception was: [" + exception + "]");
                }

                ChangeArmState(ArmState.MoveMobileTractor, false, myAgent);
                return false;
            }
            catch (Exception ex)
            {
                if (DebugConfig.DebugArm) Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool MoveCapBoosters(DirectAgent myAgent) // --> ArmState.MoveAmmo
        {
            if (Settings.Instance.CapacitorInjectorScript == 0 || Settings.Instance.NumberOfCapBoostersToLoad == 0)
            {
                ChangeArmState(ArmState.PrepareMoveAmmo, false, myAgent);
                return false;
            }

            DirectInvType capBoosterInvTypeItem = ESCache.Instance.DirectEve.GetInvType(Settings.Instance.CapacitorInjectorScript);
            if (capBoosterInvTypeItem == null)
            {
                Log.WriteLine("if (capBoosterInvTypeItem == null)");
                return false;
            }

            WeHaveThisManyOfThoseItemsInCargo = -1;
            if (ESCache.Instance.CurrentShipsCargo.Items == null || ESCache.Instance.CurrentShipsCargo.Items.Count == 0)
            {
                Log.WriteLine("if (ESCache.Instance.CurrentShipsCargo.Items == null || ESCache.Instance.CurrentShipsCargo.Items.Count == 0)");
                return false;
            }

            cargoItems = ESCache.Instance.CurrentShipsCargo.Items.Where(i => (i.TypeName ?? string.Empty).ToLower().Equals(capBoosterInvTypeItem.TypeName.ToLower())).ToList();
            if (cargoItems != null)
            {
                WeHaveThisManyOfThoseItemsInCargo = cargoItems.Sum(i => i.Stacksize);

                if (Settings.Instance.NumberOfCapBoostersToLoad > WeHaveThisManyOfThoseItemsInCargo)
                {
                    Log.WriteLine("CapBooster Item TypeName [" + capBoosterInvTypeItem.TypeName + "]");
                    Log.WriteLine("Calling MoveItemsToCargo");
                    if (!MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, capBoosterInvTypeItem.TypeName, Settings.Instance.NumberOfCapBoostersToLoad, ArmState.PrepareMoveAmmo, false, myAgent))
                        return false;

                    return false;
                }

                ChangeArmState(ArmState.PrepareMoveAmmo, false, myAgent);
                return false;
            }

            return false;
        }

        private static bool MoveDrones(DirectAgent myAgent) // --> ArmState.MoveMissionItems
        {
            try
            {
                if (!Drones.UseDrones)
                {
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.MiningController))
                    {
                        if (DebugConfig.DebugArm) Log.WriteLine("Arm.MoveDrones: UseDrones is [" + Drones.UseDrones + "] Changing ArmState to Done");
                        ChangeArmState(ArmState.Done, true, null);
                        return false;
                    }

                    if (DebugConfig.DebugArm) Log.WriteLine("Arm.MoveDrones: UseDrones is [" + Drones.UseDrones + "] Changing ArmState to MoveBringItems");
                    ChangeArmState(ArmState.MoveMissionItems, false, myAgent);
                    return false;
                }

                if (ESCache.Instance.ActiveShip.GroupId == (int) Group.Shuttle ||
                    ESCache.Instance.ActiveShip.GroupId == (int) Group.Industrial ||
                    ESCache.Instance.ActiveShip.GroupId == (int) Group.TransportShip)
                {
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.MiningController))
                    {
                        if (DebugConfig.DebugArm) Log.WriteLine("Arm.MoveDrones: We are in an Industrial Ship (hauling?) changing ArmState to Done");
                        ChangeArmState(ArmState.Done, true, null);
                        return false;
                    }

                    if (DebugConfig.DebugArm) Log.WriteLine("Arm.MoveDrones: ActiveShip GroupID is [" + ESCache.Instance.ActiveShip.GroupId + "] Which we assume is a Shuttle, Industrial, TransportShip: Changing ArmState to MoveBringItems");
                    ChangeArmState(ArmState.MoveMissionItems, false, myAgent);
                    return false;
                }

                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
                {
                    if (DebugConfig.DebugArm) Log.WriteLine("Arm.MoveDrones: ActiveShip Name is [" + ESCache.Instance.ActiveShip.GivenName + "] Which is not the CombatShipname [" + Combat.Combat.CombatShipName + "]: Changing ArmState to MoveBringItems");
                    ChangeArmState(ArmState.MoveMissionItems, false, myAgent);
                    return false;
                }

                if (DroneInvTypeItem == null)
                {
                    Log.WriteLine("(DroneInvTypeItem == null)");
                    return false;
                }

                if (DebugConfig.DebugArm) Log.WriteLine("Arm.MoveDrones: DroneInvTypeItem.TypeName: " + DroneInvTypeItem.TypeName);

                if (!MoveDronesToDroneBay(ESCache.Instance.AmmoHangar, DroneInvTypeItem.TypeName, ArmState.MoveMissionItems, ArmState.MoveDrones, myAgent))
                    return false;

                if (DebugConfig.DebugArm) Log.WriteLine("Arm.MoveDrones: MoveDronesToDroneBay returned true! CurrentArmState is [" + State.CurrentArmState + "]: this should NOT still be MoveDrones!");
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        // check
        private static bool MoveDronesToDroneBay(DirectContainer fromContainer, string itemName, ArmState nextState, ArmState fromState, DirectAgent myAgent)
        {
            try
            {
                if (DebugConfig.DebugArm) Log.WriteLine("(re)Entering MoveDronesToDroneBay");

                if (string.IsNullOrEmpty(itemName))
                {
                    Log.WriteLine("if (string.IsNullOrEmpty(MoveItemTypeName))");
                    ChangeArmState(nextState, false, myAgent);
                    return false;
                }

                if (fromContainer == null)
                {
                    Log.WriteLine("if (fromContainer == null)");
                    return false;
                }

                if (Drones.DroneBay == null)
                    return false;

                if (DroneBayRetries > 10)
                {
                    ChangeArmState(ArmState.MoveMissionItems, false, myAgent);
                    return false;
                }

                if (Drones.DroneBay.Capacity == 0 && DroneBayRetries <= 10)
                {
                    DroneBayRetries++;
                    Log.WriteLine("Dronebay: not yet ready. Capacity [" + Drones.DroneBay.Capacity + "] UsedCapacity [" + Drones.DroneBay.UsedCapacity +
                                  "]");
                    Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(2);
                    return false;
                }

                if (!LookForItem(fromContainer, itemName, Drones.DroneBay))
                {
                    Log.WriteLine("if (!LookForItem(MoveItemTypeName, Drones.DroneBay))");
                    return false;
                }

                if (Drones.DroneBay != null && DroneInvTypeItem != null && Drones.DroneBay.Items != null && fromContainer != null &&
                    fromContainer.Items != null)
                    if (Drones.DroneBay.Items.Any(d => d.TypeId != DroneInvTypeItem.TypeId))
                    {
                        Log.WriteLine("We have other drones in the bay, moving them to the ammo hangar.");
                        IEnumerable<DirectItem> other_drones = Drones.DroneBay.Items.Where(d => d.TypeId != DroneInvTypeItem.TypeId);
                        if (!fromContainer.Add(other_drones)) return false;
                        Time.Instance.NextArmAction = DateTime.UtcNow.AddMilliseconds(300);
                        return false;
                    }

                Log.WriteLine("Dronebay details: Capacity [" + Drones.DroneBay.Capacity + "] UsedCapacity [" + Drones.DroneBay.UsedCapacity + "]");

                if ((int) Drones.DroneBay.Capacity == (int) Drones.DroneBay.UsedCapacity)
                {
                    Log.WriteLine("Dronebay is Full [" + Drones.DroneBay.Capacity + " m3]");
                    ChangeArmState(nextState, false, myAgent);
                    return false;
                }

                if (Drones.DroneBay != null && DroneInvTypeItem != null && DroneInvTypeItem.Volume != 0)
                {
                    int neededDrones = (int) Math.Floor((Drones.DroneBay.Capacity - (double)Drones.DroneBay.UsedCapacity) / DroneInvTypeItem.Volume);
                    _itemsLeftToMoveQuantity = neededDrones;

                    Log.WriteLine("neededDrones: [" + neededDrones + "]");

                    if (neededDrones == 0)
                    {
                        Log.WriteLine("MoveItems");
                        ChangeArmState(ArmState.MoveMissionItems, false, myAgent);
                        return false;
                    }

                    if (WeHaveThisManyOfThoseItemsInCargo + WeHaveThisManyOfThoseItemsInItemHangar < neededDrones)
                    {
                        Log.WriteLine("ItemHangar has: [" + WeHaveThisManyOfThoseItemsInItemHangar + "] fromContainer has: [" +
                                      WeHaveThisManyOfThoseItemsInAmmoHangar + "][" + itemName + "] we need [" + neededDrones +
                                      "] drones to fill the DroneBay)");
                        ControllerManager.Instance.SetPause(true);
                        ChangeArmState(ArmState.NotEnoughDrones, false, myAgent);
                        return true;
                    }

                    //  here we check if we have enough free m3 in our drone hangar

                    if (Drones.DroneBay != null && DroneInvTypeItem != null && DroneInvTypeItem.Volume != 0)
                    {
                        double freeCapacity = Drones.DroneBay.Capacity - (double)Drones.DroneBay.UsedCapacity;

                        Log.WriteLine("freeCapacity [" + freeCapacity + "] _itemsLeftToMoveQuantity [" + _itemsLeftToMoveQuantity + "]" +
                                      " DroneInvTypeItem.Volume [" +
                                      DroneInvTypeItem.Volume + "]");

                        int amount = Convert.ToInt32(freeCapacity / DroneInvTypeItem.Volume);
                        _itemsLeftToMoveQuantity = Math.Min(amount, _itemsLeftToMoveQuantity);

                        Log.WriteLine("freeCapacity [" + freeCapacity + "] amount [" + amount + "] _itemsLeftToMoveQuantity [" +
                                      _itemsLeftToMoveQuantity + "]");
                    }
                    else
                    {
                        Log.WriteLine("Drones.DroneBay || ItemHangarItem != null");
                        ChangeArmState(nextState, false, myAgent);
                        return false;
                    }

                    if (_itemsLeftToMoveQuantity <= 0)
                    {
                        Log.WriteLine("if (_itemsLeftToMoveQuantity <= 0)");
                        ChangeArmState(nextState, false, myAgent);
                        return false;
                    }

                    if (fromContainerItem != null && !string.IsNullOrEmpty(fromContainerItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                    {
                        if (fromContainerItem.ItemId <= 0 || fromContainerItem.Volume == 0.00 || fromContainerItem.Quantity == 0)
                            return false;

                        IEnumerable<DirectItem> dronesItemsInAmmoHangar = ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == DroneInvTypeItem.TypeId);
                        int amountOfDrones = dronesItemsInAmmoHangar.Sum(i => i.Stacksize);

                        Log.WriteLine("There is a total of [" + amountOfDrones + "] of " + DroneInvTypeItem.TypeName + " in Itemhangar.");

                        if (dronesItemsInAmmoHangar.Any() && amountOfDrones > _itemsLeftToMoveQuantity)
                        {
                            List<DirectItem> dronesToMove = new List<DirectItem>();
                            foreach (DirectItem droneItem in dronesItemsInAmmoHangar.OrderBy(a => a.Stacksize))
                            {
                                int qtyToMove = droneItem.Stacksize;
                                if (qtyToMove <= _itemsLeftToMoveQuantity && _itemsLeftToMoveQuantity - qtyToMove >= 0)
                                {
                                    dronesToMove.Add(droneItem);
                                    _itemsLeftToMoveQuantity -= qtyToMove;
                                }

                                if (_itemsLeftToMoveQuantity == 0) break;
                            }

                            if (dronesToMove.Count > 0)
                            {
                                dronesToMove.ForEach(d => Log.WriteLine("(Multi) Dronename: " + d.TypeName + " Stacksize: " + d.Stacksize));
                                if (!Drones.DroneBay.Add(dronesToMove)) return false;
                                _lastArmAction = DateTime.UtcNow;
                                return false;
                            }

                            if (dronesItemsInAmmoHangar.Any(i => i.Stacksize >= _itemsLeftToMoveQuantity))
                            {
                                DirectItem stackToMove = dronesItemsInAmmoHangar.FirstOrDefault(i => i.Stacksize >= _itemsLeftToMoveQuantity);
                                int qtyToMove = Math.Min(stackToMove.Stacksize, _itemsLeftToMoveQuantity);
                                _itemsLeftToMoveQuantity -= qtyToMove;
                                dronesToMove.ForEach(d => Log.WriteLine("(Single) Dronename: " + stackToMove.TypeName + " Stacksize: " + qtyToMove));
                                if (!Drones.DroneBay.Add(stackToMove, qtyToMove)) return false;
                                _lastArmAction = DateTime.UtcNow;
                                return false;
                            }
                        }
                        else
                        {
                            // this should not be called anymore.
                            int moveDroneQuantity = Math.Min(fromContainerItem.Stacksize, _itemsLeftToMoveQuantity);
                            moveDroneQuantity = Math.Max(moveDroneQuantity, 1);

                            _itemsLeftToMoveQuantity -= moveDroneQuantity;
                            Log.WriteLine("Moving Item(5) [" + fromContainerItem.TypeName + "] from ItemHangar to DroneBay: We have [" +
                                          _itemsLeftToMoveQuantity +
                                          "] more item(s) to move after this");

                            if (!Drones.DroneBay.Add(fromContainerItem, moveDroneQuantity)) return false;
                            _lastArmAction = DateTime.UtcNow;
                            return false;
                        }
                    }

                    return true;
                }

                if (DroneInvTypeItem != null) Log.WriteLine("droneTypeId: DroneInvTypeItem.TypeId [" + DroneInvTypeItem.TypeId + "] Vol [" + DroneInvTypeItem.Volume + "] is highly likely to be incorrect in your settings xml");
                else Log.WriteLine("DroneInvTypeItem == null");
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool MoveMissionItems(DirectAgent myAgent) // --> MoveOptionalItems
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
            {
                ChangeArmState(ArmState.MoveAbyssalDeadspaceFilament, false, null);
                return false;
            }

            //if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.FleetAbyssalDeadspaceController))
            //{
            //    ChangeArmState(ArmState.MoveAbyssalDeadspaceFilament, false, null);
            //    return false;
            //}

            if (!MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, MissionSettings.MoveMissionItems, Math.Max(MissionSettings.MoveMissionItemsQuantity, 1), ArmState.MoveOptionalItems, false, myAgent)) return false;

            return false;
        }

        private static bool MoveMobileTractorItems(DirectAgent myAgent)
        {
            if (!Salvage.UseMobileTractor)
            {
                Log.WriteLine("Arm: Not using Mobile Tractor: UseMobileTractor [" + Salvage.UseMobileTractor + "]");
                ChangeArmState(ArmState.StackAmmoHangar, false, myAgent);
                return false;
            }

            if (ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
            {
                Log.WriteLine("Arm: Not using Mobile Tractor: if (ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())");
                ChangeArmState(ArmState.StackAmmoHangar, false, myAgent);
                return false;
            }

            if (ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
            {
                Log.WriteLine("Arm: Not using Mobile Tractor: we arent in the combatship");
                ChangeArmState(ArmState.StackAmmoHangar, false, myAgent);
                return false;
            }

            if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.GroupId == (int) Group.MobileTractor))
            {
                //
                // we are 'out of ammo' here. we should go buy more, or at least pause
                //
                Log.WriteLine("We have a [ Mobile Tractor Unit ] in our cargo. continuing");
                ChangeArmState(ArmState.StackAmmoHangar, false, myAgent);
                return true;
            }

            if (!MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, "Mobile Tractor Unit", 1, ArmState.StackAmmoHangar, true, myAgent))
            {
                if (DebugConfig.DebugArm) Log.WriteLine("Arm: MoveMobileTractorItems: MoveItemsToCargo: Returned false");
                return false;
            }

            if (ESCache.Instance.AmmoHangar != null && ESCache.Instance.AmmoHangar.Items.All(i => i.GroupId != (int) Group.MobileTractor))
            {
                //
                // we are 'out of ammo' here. we should go buy more, or at least pause
                //
                Log.WriteLine("We did not find a [ Mobile Tractor Unit ] in the item hangar. continuing");
                ChangeArmState(ArmState.StackAmmoHangar, false, myAgent);
                return true;
            }

            return true;
        }

        // --> MoveOptionalItems
        private static bool MoveOptionalItems(DirectAgent myAgent) // --> ArmState.MoveScripts
        {
            if (!MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, MissionSettings.MoveOptionalMissionItems, Math.Max(MissionSettings.MoveOptionalMissionItemQuantity, 1), ArmState.MoveScripts, true, myAgent)) return false;
            return false;
        }

        // Chant - 05/02/2016 - need to load sensor manipulation scripts if specified
        private static bool MoveScripts(DirectAgent myAgent) // --> ArmState.MoveCapBoosters
        {
            if (ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
            {
                Log.WriteLine("if (Cache.Instance.ActiveShip.GivenName != Combat.CombatShipName)");
                ChangeArmState(ArmState.MoveCapBoosters, false, myAgent);
                return false;
            }

            int TrackingDisruptorScriptsLeft = 0;
            int TrackingComputerScriptsLeft = 0;
            int TrackingLinkScriptsLeft = 0;
            int SensorBoosterScriptsLeft = 0;
            int SensorDampenerScriptsLeft = 0;
            int CapacitorInjectorScriptsLeft = 0;
            int AncillaryShieldBoosterScriptsLeft = 0;

            if (!bWaitingonScripts)
            {
                TrackingDisruptorScriptsLeft =
                    TrackingDisruptorScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.TrackingDisruptor).Sum(i => i.Quantity));
                TrackingComputerScriptsLeft =
                    TrackingComputerScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.TrackingComputer).Sum(i => i.Quantity));
                TrackingLinkScriptsLeft =
                    TrackingLinkScripts = Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.TrackingLink).Sum(i => i.Quantity));
                SensorBoosterScriptsLeft =
                    SensorBoosterScripts = Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.SensorBooster)
                        .Sum(i => i.Quantity));
                SensorDampenerScriptsLeft =
                    SensorDampenerScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.SensorDampener).Sum(i => i.Quantity));
                CapacitorInjectorScriptsLeft =
                    CapacitorInjectorScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.CapacitorInjector).Sum(i => i.Quantity));
                AncillaryShieldBoosterScriptsLeft =
                    AncillaryShieldBoosterScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.AncillaryShieldBooster).Sum(i => i.Quantity));

                bWaitingonScripts = true;
            }
            else
            {
                TrackingDisruptorScriptsLeft = Math.Max(0,
                    TrackingDisruptorScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.TrackingDisruptorScript).Sum(i => i.Quantity)));
                TrackingComputerScriptsLeft = Math.Max(0,
                    TrackingComputerScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.TrackingComputerScript).Sum(i => i.Quantity)));
                TrackingLinkScriptsLeft = Math.Max(0,
                    TrackingLinkScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.TrackingLinkScript).Sum(i => i.Quantity)));
                SensorBoosterScriptsLeft = Math.Max(0,
                    SensorBoosterScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.SensorBoosterScript).Sum(i => i.Quantity)));
                SensorDampenerScriptsLeft = Math.Max(0,
                    SensorDampenerScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.SensorDampenerScript).Sum(i => i.Quantity)));
                CapacitorInjectorScriptsLeft = Math.Max(0,
                    CapacitorInjectorScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.CapacitorInjectorScript).Sum(i => i.Quantity)));
                AncillaryShieldBoosterScriptsLeft = Math.Max(0,
                    AncillaryShieldBoosterScripts -
                    Math.Abs(
                        ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.AncillaryShieldBoosterScript).Sum(i => i.Quantity)));
            }

            DirectInvType _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(Settings.Instance.TrackingDisruptorScript);
            if (TrackingDisruptorScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                if (MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, _ScriptInvTypeItem.TypeName, TrackingDisruptorScriptsLeft, ArmState.MoveScripts, true, myAgent))
                {
                    Log.WriteLine("Not enough Tracking Disruptor scripts in hangar");
                    TrackingDisruptorScriptsLeft = 0;
                    TrackingDisruptorScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(Settings.Instance.TrackingComputerScript);
            if (TrackingComputerScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                if (MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, _ScriptInvTypeItem.TypeName, TrackingComputerScriptsLeft, ArmState.MoveScripts, true, myAgent))
                {
                    Log.WriteLine("Not enough Tracking Computer scripts in hangar");
                    TrackingComputerScriptsLeft = 0;
                    TrackingComputerScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(Settings.Instance.TrackingLinkScript);
            if (TrackingLinkScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                if (MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, _ScriptInvTypeItem.TypeName, TrackingLinkScriptsLeft, ArmState.MoveScripts, true, myAgent))
                {
                    Log.WriteLine("Not enough Tracking Link scripts in hangar");
                    TrackingLinkScriptsLeft = 0;
                    TrackingLinkScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(Settings.Instance.SensorBoosterScript);
            if (SensorBoosterScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                Log.WriteLine("[" + SensorBoosterScriptsLeft + "] SensorBoosterScriptsLeft");
                if (MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, _ScriptInvTypeItem.TypeName, SensorBoosterScripts, ArmState.MoveScripts, true, myAgent))
                {
                    Log.WriteLine("Not enough Sensor Booster scripts in hangar");
                    SensorBoosterScriptsLeft = 0;
                    SensorBoosterScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(Settings.Instance.SensorDampenerScript);
            if (SensorDampenerScriptsLeft >= 1 && _ScriptInvTypeItem != null)

            {
                if (MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, _ScriptInvTypeItem.TypeName, SensorDampenerScriptsLeft, ArmState.MoveScripts, true, myAgent))
                {
                    Log.WriteLine("Not enough Sensor Dampener scripts in hangar");
                    SensorDampenerScriptsLeft = 0;
                    SensorDampenerScripts = 0;
                }
                return false;
            }

            /**
            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(Settings.Instance.CapacitorInjectorScript);
            if (CapacitorInjectorScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                if (MoveItemsToCargo(ESCache.Instance.ItemHangar, ESCache.Instance.CurrentShipsCargo, _ScriptInvTypeItem.TypeName, CapacitorInjectorScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Log.WriteLine("Not enough Capacitor Injector scripts in hangar");
                    CapacitorInjectorScriptsLeft = 0;
                    CapacitorInjectorScripts = 0;
                }
                return false;
            }
            **/

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(Settings.Instance.AncillaryShieldBoosterScript);
            if (AncillaryShieldBoosterScriptsLeft >= 1 && _ScriptInvTypeItem != null)

            {
                if (MoveItemsToCargo(ESCache.Instance.AmmoHangar, ESCache.Instance.CurrentShipsCargo, _ScriptInvTypeItem.TypeName, AncillaryShieldBoosterScriptsLeft, ArmState.MoveScripts, true, myAgent))
                {
                    Log.WriteLine("Not enough Ancillary Shield Booster scripts in hangar");
                    AncillaryShieldBoosterScriptsLeft = 0;
                    AncillaryShieldBoosterScripts = 0;
                }
                return false;
            }

            bWaitingonScripts = false;
            ChangeArmState(ArmState.MoveCapBoosters, false, myAgent);
            return false;
        }

        private static bool PrepareToMoveAmmo(DirectAgent myAgent)
        {
            try
            {
                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && MissionSettings.MyMission != null && (MissionSettings.MyMission.Type.Contains("Courier") || MissionSettings.MyMission.Type.Contains("Trade")))
                {
                    Log.WriteLine("We are on a courier / trade mission: no need to move ammo");
                    ChangeArmState(ArmState.ConsumeBoosters, false, myAgent);
                    return false;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                {
                    Log.WriteLine("We are using the HighSecAnomalyController: no need to move ammo?!?");
                    ChangeArmState(ArmState.Done, false, myAgent);
                    return false;
                }

                if (ESCache.Instance.ActiveShip.GroupId == (int) Group.Shuttle ||
                    ESCache.Instance.ActiveShip.GroupId == (int) Group.Industrial ||
                    ESCache.Instance.ActiveShip.GroupId == (int) Group.TransportShip ||
                    (myAgent != null && myAgent.Mission != null && MissionSettings.CourierMission(myAgent.Mission)) ||
                    ESCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
                {
                    Log.WriteLine("We are not in our combatship, no need to move ammo");
                    ChangeArmState(ArmState.ConsumeBoosters, false, myAgent);
                    return false;
                }

                if (ESCache.Instance.CurrentShipsCargo == null)
                    return false;

                if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items == null)
                    return false;

                if (ESCache.Instance.AmmoHangar == null)
                {
                    Log.WriteLine("PrepareToMoveAmmo: if (ESCache.Instance.AmmoHangar == null)");
                    return false;
                }


                //this does not work in stations! modules are only populated in space!
                if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.Any(i => i.TypeId == (int) TypeID.CivilianGatlingAutocannon
                                                      || i.TypeId == (int) TypeID.CivilianGatlingPulseLaser
                                                      || i.TypeId == (int) TypeID.CivilianGatlingRailgun
                                                      || i.TypeId == (int) TypeID.CivilianLightElectronBlaster))
                {
                    Log.WriteLine("No ammo needed for civilian guns: done");
                    ChangeArmState(ArmState.ConsumeBoosters, false, myAgent);
                    return false;
                }

                if (DirectUIModule.DefinedAmmoTypes != null && DirectUIModule.DefinedAmmoTypes.Count > 0)
                {
                    int ammoNum = 0;
                    foreach (AmmoType ammo in DirectUIModule.DefinedAmmoTypes.Where(i => i.Quantity == 0 || i.Quantity == 1).ToList())
                    {
                        ammoNum++;
                        Log.WriteLine("Combat.Combat.DefinedAmmoTypes [" + ammoNum + "][" + ammo.DamageType + "] TypeID [" + ammo.TypeId + "] Quantity is [" + ammo.Quantity + "], how?");
                        DirectUIModule._definedAmmoTypes = new List<AmmoType>();
                        break;
                    }

                    foreach (AmmoType ammo in DirectUIModule.DefinedAmmoTypes.Where(i => i.Quantity > 0).ToList())
                    {
                        ammoNum++;
                        if (DebugConfig.DebugArm)
                            Log.WriteLine("Combat.Combat.DefinedAmmoTypes [" + ammoNum + "][" + ammo.DamageType + "] TypeID [" + ammo.TypeId + "] Quantity [" + ammo.Quantity + "]");
                    }

                    ammoNum = 0;
                    foreach (AmmoType ammo in DirectUIModule.DefinedAmmoTypes.Where(i => i.Quantity > 0).ToList())
                    {
                        ammoNum++;
                        if (ESCache.Instance.AmmoHangar.Items.Count > 0)
                        {
                            if (ESCache.Instance.AmmoHangar.Items.Any(i => i.TypeId == ammo.TypeId && !i.IsSingleton && ammo.Quantity > 0 && i.Quantity >= ammo.Quantity))
                            {
                                int ammoQuantity = ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == ammo.TypeId && !i.IsSingleton && ammo.Quantity > 0 && i.Quantity >= ammo.Quantity).Sum(j => j.Stacksize);
                                DirectItem ammoItemStack = ESCache.Instance.AmmoHangar.Items.Find(i => i.TypeId == ammo.TypeId && !i.IsSingleton && ammo.Quantity > 0 && i.Quantity >= ammo.Quantity);
                                Log.WriteLine("We have [" + ammoQuantity + "][" + ammoItemStack.TypeName + "][" + ammo.DamageType + "] in the hangar which is more than the minimum [" + ammo.Quantity + "]");
                                continue;
                            }

                            if (ESCache.Instance.ActiveShip.GroupId == (int)Group.RookieShip)
                            {
                                Log.WriteLine("We are using a Rookie Ship: assume we are using civilian weapons! no need to move ammo");
                                ChangeArmState(ArmState.ConsumeBoosters, false, myAgent);
                                return false;
                            }

                            DirectItem missingAmmo = new DirectItem(ESCache.Instance.DirectEve);
                            missingAmmo.TypeId = ammo.TypeId;
                            Log.WriteLine("Error: missing [" + ammo.Quantity + "][" + missingAmmo.TypeName + "] TypeID [" + ammo.TypeId + "][" + ammo.DamageType + "] ammo in the AmmoHangar. --- Total Items Found: [" + ESCache.Instance.AmmoHangar.Items.Count + "] UseCorpAmmoHangar [" + Settings.Instance.UseCorpAmmoHangar + "] AmmoCorpHangarDivisionNumber [" + Settings.Instance.AmmoCorpHangarDivisionNumber + "]");
                            ChangeArmState(ArmState.NotEnoughAmmo, false, myAgent);
                            return false;
                        }

                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                            return true;

                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                        {
                            if (ESCache.Instance.ActiveShip.GroupId == (int)Group.RookieShip)
                            {
                                //if we have no items in the ammohangar Arm is done
                                ChangeArmState(ArmState.Done, false, myAgent);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Frigate)
                            {
                                //if we have no items in the ammohangar Arm is done
                                ChangeArmState(ArmState.Done, false, myAgent);
                                return true;
                            }
                        }

                        Log.WriteLine("Error: We have no items in the AmmoHangar: waiting for items: [" + Settings.Instance.UseCorpAmmoHangar + "] AmmoCorpHangarDivisionNumber [" + Settings.Instance.AmmoCorpHangarDivisionNumber + "]");
                        return false;
                    }
                }

                ChangeArmState(ArmState.MoveAmmo, false, myAgent);
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static bool PrepareToMoveToNewStation() // --> ArmState.StackAmmoHangar
        {
            try
            {
                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(ESCache.Instance.RandomNumber(3000, 4000)))
                {
                    if (DebugConfig.DebugArm) Log.WriteLine("if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(ESCache.Instance.RandomNumber(3000, 4000)))");
                    return false;
                }

                if (ESCache.Instance.ItemHangar != null && ESCache.Instance.CurrentShipsCargo != null)
                {
                    if (ESCache.Instance.ItemHangar.Items.Count > 0)
                    {
                        if (DirectUIModule.DefinedAmmoTypes != null)
                        {
                            if (DirectUIModule.DefinedAmmoTypes.Count > 0)
                            {
                                Dictionary<int, int> listOfItemsToLoad = new Dictionary<int, int>();
                                foreach (AmmoType individualAmmoType in DirectUIModule.DefinedAmmoTypes)
                                    foreach (DirectItem individualItemInHangar in ESCache.Instance.ItemHangar.Items.Where(i => i.CategoryId == (int) CategoryID.Charge))
                                        if (individualItemInHangar.TypeId == individualAmmoType.TypeId)
                                            listOfItemsToLoad.AddOrUpdate(individualAmmoType.TypeId, individualItemInHangar.Quantity);

                                if (!FillCargoWithAmmo(listOfItemsToLoad, ArmState.Done)) return false;
                                ChangeArmState(ArmState.Done, false, null);
                                return true;
                            }

                            Log.WriteLine("Combat.Combat.DefinedAmmoTypes list is empty!");
                            ChangeArmState(ArmState.Done, false, null);
                            return true;
                        }

                        Log.WriteLine("Combat.Combat.DefinedAmmoTypes == null");
                        ChangeArmState(ArmState.Done, false, null);
                        return true;
                    }

                    Log.WriteLine("if (!ESCache.Instance.ItemHangar.Items.Any())");
                    ChangeArmState(ArmState.Done, false, null);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ReallyConsumeBooster(int boosterNumber, DirectItem boosterDirectItem, string lastConsumeBoosterSlotTimeStamp)
        {
            if (Time.Instance.LastBoosterInjectAttempt.ContainsKey(boosterDirectItem.TypeId))
                if (DateTime.UtcNow < Time.Instance.LastBoosterInjectAttempt[boosterDirectItem.TypeId].AddMinutes(ESCache.Instance.RandomNumber(50, 60)))
                {
                    Log.WriteLine("[" + boosterNumber + "][" + boosterDirectItem.TypeName + "] has been attempted less than an hour ago, skipping");
                    return false;
                }

            Time.Instance.LastBoosterInjectAttempt.AddOrUpdate(boosterDirectItem.TypeId, DateTime.UtcNow);

            if (boosterDirectItem.ConsumeBooster())
            {
                Log.WriteLine("[" + boosterNumber + "][" + boosterDirectItem.TypeName + "] Consuming booster");
                if (DirectEve.Interval(30000)) ESCache.Instance.TaskSetEveAccountAttribute(lastConsumeBoosterSlotTimeStamp, DateTime.UtcNow);
                ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                return true;
            }

            Log.WriteLine("[" + boosterNumber + "][" + boosterDirectItem.TypeName + "] Unable to consume booster");
            return false;
        }

        private static bool RepairShop(DirectAgent myAgent) // --> ArmState.LoadSavedFitting
        {
            try
            {
                if (!Cleanup.RepairItems()) return false; //attempt to use repair facilities if avail in station

                ChangeArmState(ArmState.StripFitting, false, myAgent);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        // --> ArmState.MoveAmmo
        // --> ArmState.StackAmmoHangar
        private static bool SafeToConsumeBoosters()
        {
            List<DirectDgmEffect> activeBoosterEffects = ESCache.Instance.DirectEve.Me.GetAllBoosterEffects();

            if (activeBoosterEffects != null && activeBoosterEffects.Any())
                return false;

            return true;
        }

        private static bool StackAmmoHangar(DirectAgent myAgent) // --> ArmState.Done
        {
            if (!ESCache.Instance.AmmoHangar.StackAmmoHangar()) return false;
            ArmCleanup(myAgent);
            ChangeArmState(ArmState.Done, true, myAgent);
            return true;
        }

        private static bool StripFitting(DirectAgent myAgent)
        {
            // if we have offline modules and fittingmanage is disabled pause and disable this instance so it can be fixed manually
            /*
            if (MissionSettings.OfflineModulesFound && !Settings.Instance.UseFittingManager)
            {
                Log.WriteLine("We have offline modules but fitting manager is disabled! Pausing and disabling so this can be fixed manually.");
                ESCache.Instance.DisableThisInstance();
                ESCache.Instance.PauseAfterNextDock = true;
                return true;
            }
            */

            if (!Settings.Instance.UseFittingManager)
            {
                Log.WriteLine("StripFitting: UseFittingManager [" + Settings.Instance.UseFittingManager + "]");
                ChangeArmState(ArmState.MoveDrones, false, myAgent);
                return true;
            }

            if (!MissionSettings.OfflineModulesFound)
            {
                ChangeArmState(ArmState.LoadSavedFitting, false, myAgent);
                return true;
            }

            // if there are no offline modules we do not need to strip the fitting
            if (!MissionSettings.OfflineModulesFound)
            {
                ChangeArmState(ArmState.LoadSavedFitting, false, myAgent);
                return true;
            }

            if (ESCache.Instance.FittingManagerWindow == null) return false;

            MissionSettings.CurrentFit = string.Empty; // force to actually select the correct mission fitting
            DirectActiveShip currentShip = ESCache.Instance.ActiveShip;
            if (!currentShip.StripFitting()) return false;
            _lastFitAction = DateTime.UtcNow;
            ChangeArmState(ArmState.LoadSavedFitting, false, myAgent);
            return true;
        }

        private static string WeAreInThisStateForLogs()
        {
            return State.CurrentCombatMissionBehaviorState + "." + State.CurrentArmState;
        }

        #endregion Methods
    }
}