extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Traveller;
using System.Globalization;
using EVESharpCore.Questor.Actions;

namespace EVESharpCore.Questor.Behaviors
{
    public class InsuranceFraudBehavior
    {
        #region Constructors

        private InsuranceFraudBehavior()
        {
        }

        #endregion Constructors

        private static Dictionary<int, int> _buyList = new Dictionary<int, int>();
        private static int _orderIterations = 0;
        private static int _maxAvgPriceMultiplier = 4;
        private static int _maxBasePriceMultiplier = 16;
        public static string InsuranceFraudSelfDestructBookmarkName { get; set; }

        private static string HomeBookmarkName { get; set; }
        private static int? InsuranceFraudShipType { get; set; }

        #region Methods

        public static bool ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                //if (_StateToSet == InsuranceFraudBehaviorState.Monitor)
                //{
                    //Combat.Combat.BoolReloadWeaponsAsap = false;
                    //TryingToChangeOrReloadAmmo = false;
                //}

                //if (_StateToSet.ToString().Contains("Handle"))
                    //TryingToChangeOrReloadAmmo = true;

                if (State.CurrentInsuranceFraudBehaviorState != _StateToSet)
                {
                    Log.WriteLine("New InsuranceFraudBehaviorState [" + _StateToSet + "]");
                    State.CurrentInsuranceFraudBehaviorState = _StateToSet;
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
            Log.WriteLine("LoadSettings: InsuranceFraudBehavior");

            HomeBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            Log.WriteLine("LoadSettings: HomeBookmarkName [" + HomeBookmarkName + "]");
            InsuranceFraudShipType =
                (int?)CharacterSettingsXml.Element("insuranceFraudShipType") ?? (int?)CommonSettingsXml.Element("insuranceFraudShipType") ?? 602;
            Log.WriteLine("LoadSettings: InsuranceFraudShipType [" + InsuranceFraudShipType + "]");
            InsuranceFraudSelfDestructBookmarkName =
                (string)CharacterSettingsXml.Element("insuranceFraudSelfDestructBookmark") ?? (string)CommonSettingsXml.Element("insuranceFraudSelfDestructBookmark") ?? "selfDestructSpot";
            Log.WriteLine("LoadSettings: InsuranceFraudSelfDestructBookmarkName [" + InsuranceFraudSelfDestructBookmarkName + "]");
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;
                if (DebugConfig.DebugInsuranceFraudBehavior) Log.WriteLine("State.CurrentInsuranceFraudBehaviorState [" + State.CurrentInsuranceFraudBehaviorState + "]");

                switch (State.CurrentInsuranceFraudBehaviorState)
                {
                    case InsuranceFraudBehaviorState.Idle:
                        IdleInsuranceFraudState();
                        break;

                    case InsuranceFraudBehaviorState.GoHome:
                        GotoHomeBookmarkState();
                        break;

                    case InsuranceFraudBehaviorState.Start:
                        if (ESCache.Instance.EveAccount.ConnectToTestServer)
                        {
                            //we assume we are at the home station: the home station should have a market available: should we test that?
                            ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.DetermineShipToBuy);
                        }

                        return;

                    case InsuranceFraudBehaviorState.DetermineShipToBuy:
                        ProccessDetermineShipToBuyState();
                        break;

                    case InsuranceFraudBehaviorState.BuyShip:
                        ProcessBuyShipState();
                        break;

                    case InsuranceFraudBehaviorState.ReadyShip:
                        ProcessReadyShipState();
                        break;

                    case InsuranceFraudBehaviorState.GoToSelfDestructSpot:
                        ProccessGoToSelfDestructSpotState();
                        break;

                    case InsuranceFraudBehaviorState.SelfDestruct:
                        ProccessSelfDestructState();
                        break;

                    case InsuranceFraudBehaviorState.WaitForPod:
                        Log.WriteLine("WaitForPod");
                        break;

                    case InsuranceFraudBehaviorState.NotEnoughIsk:
                        if (DirectEve.Interval(30000)) Log.WriteLine("Not Enough ISK");
                        break;

                    case InsuranceFraudBehaviorState.Default:
                        ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        /**
        private static void ProcessAlerts()
        {
            TimeSpan ItHasBeenThisLongSinceWeStartedThePocket = Statistics.StartedPocket - DateTime.UtcNow;
            int minutesInPocket = ItHasBeenThisLongSinceWeStartedThePocket.Minutes;
            if (minutesInPocket > (AbyssalPocketWarningSeconds / 60) && !WeHaveBeenInPocketTooLong_WarningSent)
            {
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ABYSSAL_POCKET_TIME, "AbyssalDeadspace: FYI: We have been in pocket number [" + ActionControl.PocketNumber + "] for [" + minutesInPocket + "] min: You might need to check that we arnt stuck."));
                WeHaveBeenInPocketTooLong_WarningSent = true;
                Log.WriteLine("We have been in AbyssalPocket [" + ActionControl.PocketNumber + "] for more than [" + AbyssalPocketWarningSeconds + "] seconds!");
                return;
            }

            return;
        }
        **/

        private static bool EveryPulse()
        {
            if (!ESCache.Instance.EveAccount.ConnectToTestServer)
            {
                Log.WriteLine("if (!ESCache.Instance.EveAccount.ConnectToTestServer)");
                ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.Error);
                return false;
            }

            if (ESCache.Instance.InSpace && ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsPod && State.CurrentInsuranceFraudBehaviorState != InsuranceFraudBehaviorState.GoHome)
            {
                ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.GoHome);
                return false;
            }

            if (DebugConfig.DebugInsuranceFraudBehavior) Log.WriteLine("InsuranceFraudBehavior: EveryPulse: return true;");
            return true;
        }

        private static void IdleInsuranceFraudState()
        {
            if (ESCache.Instance.InSpace && ESCache.Instance.Weapons.Any())
            {
                ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.Start);
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
                ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.Start, false);
            }
        }

        private static void ProccessDetermineShipToBuyState()
        {
            //we need to test to make sure there are orders locally in this station!
            //we need to make sure we have the skills for the ship we are trying to buy (do we?)
            _buyList.Clear();
            _buyList.Add((int)InsuranceFraudShipType, 1);
            ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.BuyShip);
            return;
        }

        private static void ProcessBuyShipState()
        {

            if (!ESCache.Instance.InStation)
                return;

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                return;

            if (ESCache.Instance.CurrentShipsCargo == null)
                return;

            // Is there a market window?
            var marketWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

            if (!_buyList.Any())
            {
                // Close the market window if there is one
                if (marketWindow != null)
                    marketWindow.Close();

                Log.WriteLine("Finished buying changing state to MoveItemsToCargo");
                ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.ReadyShip);
                return;
            }

            var currentBuyListItem = _buyList.FirstOrDefault();

            var typeID = currentBuyListItem.Key;
            var itemQuantity = currentBuyListItem.Value;

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                return;

            // Do we have the items we need in the Item Hangar?
            if (ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == typeID).Sum(i => i.Stacksize) >= itemQuantity)
            {
                var itemInHangar = ESCache.Instance.DirectEve.GetItemHangar().Items.FirstOrDefault(i => i.TypeId == typeID);
                if (itemInHangar != null)
                    Log.WriteLine("We have [" +
                        ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == typeID)
                            .Sum(i => i.Stacksize)
                            .ToString(CultureInfo.InvariantCulture) +
                        "] " + itemInHangar.TypeName + " in the item hangar.");

                _buyList.Remove(typeID);
                return;
            }

            // We do not have enough of that type, open the market window
            if (marketWindow == null)
            {
                Log.WriteLine("Opening market window");
                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                return;
            }

            // Wait for the window to become ready
            if (!marketWindow.IsReady)
                return;

            // Are we currently viewing the correct orders?
            if (marketWindow.DetailTypeId != typeID)
            {
                // No, load the orders
                marketWindow.LoadTypeId(typeID);

                Log.WriteLine("Loading market window");
                return;
            }

            // Get the median sell price
            var type = ESCache.Instance.DirectEve.GetInvType(typeID);

            var currentItem = type;
            double maxPrice = 0;

            if (currentItem != null)
            {
                var avgPrice = currentItem.AveragePrice();
                var basePrice = currentItem.BasePrice / currentItem.PortionSize;

                Log.WriteLine("Item [" + currentItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice +
                    "] groupID [" +
                    currentItem.GroupId + "] groupName [" + currentItem.GroupId + "]");

                if (avgPrice != 0)
                {
                    maxPrice = avgPrice * _maxAvgPriceMultiplier; // 3 times the avg price
                }
                else
                {
                    if (basePrice != 0)
                        maxPrice = basePrice * _maxBasePriceMultiplier; // 6 times the base price
                    else
                        maxPrice = 1000;
                }

                Log.WriteLine("Item [" + currentItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice + "]");
            }

            // Are there any orders with an reasonable price?
            IEnumerable<DirectOrder> orders = new List<DirectOrder>();
            if (maxPrice == 100)
            {
                Log.WriteLine("max price [" + maxPrice + "]");
                orders =
                    marketWindow.SellOrders.Where(
                            o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.Price == 100 && o.TypeId == typeID)
                        .ToList();
            }

            _orderIterations++;

            if (!orders.Any() && _orderIterations < 10)
            {
                return;
            }

            // Is there any order left?
            if (!orders.Any())
            {
                Log.WriteLine("No reasonably priced item available! Removing this item from the buyList");
                _buyList.Remove(typeID);
                return;
            }

            // How many items do we still need?
            var neededQuantity = itemQuantity - ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == typeID).Sum(i => i.Stacksize);
            if (neededQuantity > 0)
            {
                // Get the first order
                var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                if (order != null)
                {
                    // Calculate how many we still need
                    var remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                    var orderPrice = (long)(remaining * order.Price);

                    if (ESCache.Instance.DirectEve.Me.Wealth != null && orderPrice < ESCache.Instance.DirectEve.Me.Wealth)
                    {
                        Log.WriteLine("Buying [" + remaining + "] item price [" + order.Price + "]");
                        order.Buy(remaining, DirectOrderRange.Station);

                        // Wait for the order to go through
                        //LocalPulse = DateTime.UtcNow.AddSeconds(10);
                    }
                    else
                    {
                        Log.WriteLine("Error: We don't have enough ISK on our wallet to finish that transaction.");
                        ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.NotEnoughIsk);
                        return;
                    }
                }
            }
        }

        private static void ProcessReadyShipState()
        {
            if (ESCache.Instance.ItemHangar == null)
            {
                Log.WriteLine("if (ESCache.Instance.ItemHangar == null)");
                return;
            }

            if (!ESCache.Instance.ItemHangar.Items.Any())
            {
                Log.WriteLine("No Items found in ItemHangar");
                return;
            }

            if (ESCache.Instance.ItemHangar.Items.Any(i => i.TypeId == InsuranceFraudShipType))
            {
                Log.WriteLine("Found Item with TypeId [" + InsuranceFraudShipType + "]");
                if (!ESCache.Instance.ItemHangar.Items.Any(i => i.IsSingleton && i.TypeId == InsuranceFraudShipType))
                {
                    Log.WriteLine("No Ships with TypeID [" + InsuranceFraudShipType + "] are assembled");
                    if (ESCache.Instance.ItemHangar.Items.Any(i => i.TypeId == InsuranceFraudShipType))
                    {
                        DirectItem ShipDirectItem = ESCache.Instance.ItemHangar.Items.FirstOrDefault(i => i.TypeId == InsuranceFraudShipType);
                        Log.WriteLine("AssembleShip [" + ShipDirectItem.TypeName + "] TypeId [" + ShipDirectItem.TypeId + "]");
                        ShipDirectItem.AssembleShip();
                        return;
                    }

                    return;
                }

                ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.GoToSelfDestructSpot);
                return;
            }

            Log.WriteLine("if (!ESCache.Instance.ItemHangar.Items.Any(i => i.TypeId == InsuranceFraudShipType))");
            return;
        }

        private static void ProccessGoToSelfDestructSpotState()
        {
            if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
            {
                TravelerDestination.Undock();
                return;
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoAbyssalBookmarkState: Traveler.TravelToBookmarkName([" + InsuranceFraudSelfDestructBookmarkName + " ])");

            if (ESCache.Instance.DockableLocations.Any(i => i.IsOnGridWithMe))
                if (Time.Instance.LastDamagedModuleCheck.AddSeconds(10) < DateTime.UtcNow && ESCache.Instance.InSpace
                    && ESCache.Instance.Modules.Any(m => m.DamagePercent > 0)
                    && !ESCache.Instance.Paused
                    && State.CurrentAbyssalDeadspaceBehaviorState != States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark)
                {
                    foreach (ModuleCache mod in ESCache.Instance.Modules.Where(m => m.DamagePercent > 1))
                        Log.WriteLine("Damaged module: [" + mod.TypeName + "] Damage% [" + Math.Round(mod.DamagePercent, 1) + "]");

                    Log.WriteLine("Damaged modules found, going back to base trying to fit again");
                    MissionSettings.CurrentFit = string.Empty;
                    MissionSettings.DamagedModulesFound = true;
                    ESCache.Instance.NeedRepair = true;
                    Traveler.Destination = null;
                    Traveler.ChangeTravelerState(TravelerState.Idle);

                    if (State.CurrentAbyssalDeadspaceBehaviorState != States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark)
                    {
                        Log.WriteLine("Damaged Modules Found! Go Home.");
                        State.CurrentAbyssalDeadspaceBehaviorState = States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark;
                        return;
                    }

                    return;
                }

            Traveler.TravelToBookmarkName(InsuranceFraudSelfDestructBookmarkName);

            if (ESCache.Instance.InSpace && (ESCache.Instance.MyShipEntity.HasInitiatedWarp || ESCache.Instance.InWarp))
                return;

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                if (ESCache.Instance.CurrentShipsCargo == null) return;
                ESCache.Instance.CurrentShipsCargo.StackShipsCargo();
                ActionControl.ChangeCombatMissionCtrlState(ActionControlState.Start, null, null);
                Traveler.Destination = null;
                //if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.FleetAbyssalDeadspaceController))
                //{
                //    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.ActivateFleetAbyssalFilaments, true);
                //    return;
                //}

                ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.SelfDestruct);
            }
        }

        private static void ProccessSelfDestructState()
        {
            Log.WriteLine("ProccessSelfDestructState");
            if (!ESCache.Instance.InSpace)
                return;

            if (ESCache.Instance.InStation)
                return;

            if (ESCache.Instance.ActiveShip == null)
                return;

            if (ESCache.Instance.ActiveShip.Entity == null)
                return;

            if (ESCache.Instance.ActiveShip.Entity.TypeId != InsuranceFraudShipType)
            {
                Log.WriteLine("if (ESCache.Instance.ActiveShip.Entity.TypeId != InsuranceFraudShipType)");
                return;
            }

            if (ESCache.Instance.ActiveShip.SelfDestructShip())
            {
                ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.WaitForPod);
                return;
            }

            return;
        }

        public static void InvalidateCache()
        {
            //_cachedAmmoDirectItem = null;
        }

        private static bool ResetStatesToDefaults()
        {
            // intentionally left empty.
            return true;
        }

        private static void StartInsuranceFraudState()
        {
            ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.DetermineShipToBuy);
        }

        private static void ChangeToDefaultInsuranceFraudState()
        {
            //
            // runs on every grid change (in warp?)
            //

            ChangeInsuranceFraudBehaviorState(InsuranceFraudBehaviorState.Idle);
        }

        #endregion Methods
    }
}