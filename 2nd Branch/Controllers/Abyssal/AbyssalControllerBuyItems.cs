//
// (c) duketwo 2022
//

extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
//using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Controllers.Abyssal
{

    enum BuyItemsState
    {
        Start,
        ActivateCombatShip,
        CreateBuyList,
        ActivateTransportShip,
        EmptyTransportShip,
        TravelToMarketStation,
        BuyItems,
        MoveItemsToCargo,
        TravelToHomeSystem,
        UnloadLoot,
    }

    public partial class AbyssalController : AbyssalBaseController
    {
        private BuyItemsState _buyItemsState;
        private Dictionary<int, int> _buyList = new Dictionary<int, int>();
        private Dictionary<int, int> _moveToCargoList = new Dictionary<int, int>();
        private int _maxAvgPriceMultiplier = 4;
        private int _maxBasePriceMultiplier = 16;
        private int _orderIterations = 0;

        internal void BuyItems()
        {
            if (DebugConfig.DebugBuyItems) Log("_buyItemsState [" + _buyItemsState + "]");
            switch (_buyItemsState)
            {
                case BuyItemsState.Start:
                    _buyItemsState = BuyItemsState.ActivateCombatShip;
                    break;
                case BuyItemsState.ActivateCombatShip:
                    ActivateCombatShip();
                    break;
                case BuyItemsState.CreateBuyList:
                    CreateBuyList();
                    break;
                case BuyItemsState.ActivateTransportShip:
                    ActivateTransport();
                    break;
                case BuyItemsState.EmptyTransportShip:
                    EmptyTransport();
                    break;
                case BuyItemsState.TravelToMarketStation:
                    TravelToMarketSystem();
                    break;
                case BuyItemsState.BuyItems:
                    BuyItemsInMarketStation();
                    break;
                case BuyItemsState.MoveItemsToCargo:
                    MoveItemsToCargo();
                    break;
                case BuyItemsState.TravelToHomeSystem:
                    TravelToHomeSystem();
                    break;
                case BuyItemsState.UnloadLoot:
                    AbyssalUnloadLoot();
                    break;
            }
        }

        private void ActivateCombatShip()
        {
            if (!ESCache.Instance.InStation)
                return;


            if (ESCache.Instance.DirectEve.GetShipHangar() == null)
            {
                Log("Shiphangar is null.");
                return;
            }

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log("ItemHangar is null.");
                return;
            }

            var PossibleCombatShips = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                             && i.GivenName != null
                                                             && i.GivenName == Combat.CombatShipName);
            if (PossibleCombatShips.Count() > 1)
            {
                var FirstCombatShip = PossibleCombatShips.FirstOrDefault();
                if (PossibleCombatShips.All(i => i.TypeId != FirstCombatShip.TypeId))
                {
                    Log("We have more than one CombatShip an they are not the same type of ship. Error!");
                    return;
                }
            }

            var combatship = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                             && i.GivenName != null
                                                             && i.GivenName == Combat.CombatShipName).ToList();
            if (ESCache.Instance.ActiveShip == null)
            {
                Log("Active ship is null.");
                return;
            }

            if (ESCache.Instance.ActiveShip.GivenName == Combat.CombatShipName)
            {
                _buyItemsState = BuyItemsState.CreateBuyList;
                Log("We are in our Combat Ship");
                return;
            }

            Log("ActiveShip is [" + ESCache.Instance.ActiveShip.GivenName + "] TypeID [" + ESCache.Instance.ActiveShip.TypeId + "]");

            if (combatship.Any())
            {
                combatship.FirstOrDefault().ActivateShip();
                Log("Found a combat ship named [" + Combat.CombatShipName + "]. Making it active.");
                LocalPulse = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                return;
            }
        }

        private void ActivateTransport()
        {
            if (!ESCache.Instance.InStation)
                return;


            if (ESCache.Instance.DirectEve.GetShipHangar() == null)
            {
                Log("Shiphangar is null.");
                return;
            }

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log("ItemHangar is null.");
                return;
            }

            var ShipsWithCorrectTransportShipName = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                             && i.GivenName != null
                                                             && i.GivenName == Settings.Instance.TransportShipName).ToList();
            if (ESCache.Instance.ActiveShip == null)
            {
                Log("Active ship is null.");
                return;
            }

            Log("ActiveShip is [" + ESCache.Instance.ActiveShip.GivenName + "] TypeID [" + ESCache.Instance.ActiveShip.TypeId + "]");

            if (ESCache.Instance.ActiveShip.GivenName == Settings.Instance.TransportShipName)
            {
                _buyItemsState = BuyItemsState.EmptyTransportShip;
                Log("We are in our transport ship now.");
                return;
            }

            if (ShipsWithCorrectTransportShipName.Any())
            {
                var transportShip = ShipsWithCorrectTransportShipName.OrderByDescending(i => i.GroupId == (int)Group.TransportShip).FirstOrDefault();
                if (transportShip != null)
                {
                    transportShip.ActivateShip();
                    Log("Found our transport ship named [" + transportShip.GivenName + "]. Making it active.");
                    LocalPulse = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                    return;
                }
                else Log("if (transportShip == null) !");
            }
            else
            {
                Log("TransportShipName [" + Settings.Instance.TransportShipName + "] was not found in Ship Hangar");
                Util.PlayNoticeSound();
                ControllerManager.Instance.SetPause(true);
            }
        }

        private void EmptyTransport()
        {
            if (ESCache.Instance.ActiveShip == null)
            {
                Log("Active ship is null.");
                return;
            }

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log($"Itemhangar is null.");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log("Current ships cargo is null.");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo.Items.Any())
            {

                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("EmptyTransport"))
                    return;
                if (ESCache.Instance.DirectEve.GetItemHangar().Add(ESCache.Instance.CurrentShipsCargo.Items))
                {
                    Log($"Moving items into itemhangar.");
                    LocalPulse = UTCNowAddMilliseconds(3000, 3500);
                    return;
                }
            }
            else
            {
                Time.Instance.StartedTravelToMarketStation = DateTime.UtcNow;
                _buyItemsState = BuyItemsState.TravelToMarketStation;
            }
        }

        /// <summary>
        /// ItemHangar, ShipsCargo and ShipHangar needs to be opened before calling
        /// </summary>
        internal bool? DoWeNeedToBuyItems
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InStation)
                        return false;

                    if (!Settings.Instance.AllowBuyingItems)
                    {
                        Log("allowBuyingItems [false]");
                        return false;
                    }

                    if (DebugConfig.DebugBuyItems) Log("if (ESCache.Instance.DirectEve.Me != null || ESCache.Instance.DirectEve.Me.Wealth == null)");
                    if (ESCache.Instance.DirectEve.Me != null && ESCache.Instance.DirectEve.Me.Wealth == null)
                    {
                        Log("if (ESCache.Instance.DirectEve.Me != null && ESCache.Instance.DirectEve.Me.Wealth == null) return false");
                        return null;
                    }

                    if (DebugConfig.DebugBuyItems) Log("Wealth is [" + ESCache.Instance.DirectEve.Me.Wealth + "]");

                    if (ESCache.Instance.ShipHangar == null)
                    {
                        Log("Shiphangar is null.");
                        return null;
                    }

                    if (DebugConfig.DebugBuyItems) Log("ShipHangar is fine");

                    if (ESCache.Instance.ItemHangar == null)
                    {
                        Log("ItemHangar is null.");
                        return null;
                    }

                    if (DebugConfig.DebugBuyItems) Log("ItemHangar is fine");

                    if (ESCache.Instance.CurrentShipsCargo == null)
                    {
                        Log("if (ESCache.Instance.CurrentShipsCargo == null)");
                        return null;
                    }

                    if (DebugConfig.DebugBuyItems) Log("CurrentShipsCargo is fine");

                    // check if we are at the homestation, else false
                    if (!AreWeDockedInHomeSystem())
                    {
                        Log("AreWeDockedInHomeSystem [false]");
                        return false;
                    }

                    if (DebugConfig.DebugBuyItems) Log("AreWeDockedInHomeSystem [true]");

                    var transportship = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsValidShipToUse
                                                                && i.GivenName == Settings.Instance.TransportShipName).ToList();

                    // check if we are docked and a transport ship is available in the ship hangar, else false
                    if (!transportship.Any())
                    {
                        int intShipCount = 0;
                        Log("No transport ship named [" + Settings.Instance.TransportShipName + "] found.");
                        foreach (var shipInHangar in ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsValidShipToUse))
                        {
                            intShipCount++;
                            Log("Found Ship [" + intShipCount + "] named [" + shipInHangar.GivenName + "] TypeName [" + shipInHangar.TypeName + "] TypeId [" + shipInHangar.TypeId + "]");
                        }

                        return false;
                    }

                    if (DebugConfig.DebugBuyItems) Log("transportship is fine");

                    if (!ESCache.Instance.IsSafeToTravelIntoEmpireFromHere)
                    {
                        Log("IsSafeToTravelIntoEmpireFromHere [" + ESCache.Instance.IsSafeToTravelIntoEmpireFromHere + "]");
                        return false;
                    }

                    if (DebugConfig.DebugBuyItems) Log("IsSafeToTravelIntoEmpireFromHere [true]");

                    if (BuildBuyList().Any())
                    {
                        if (DebugConfig.DebugBuyItems) Log("BuildBuyList");

                        if (!ESCache.Instance.IsSafeToTravelIntoEmpireFromHere)
                        {
                            Log("IsSafeToTravelIntoEmpireFromHere: false: Canceling Buying Items: This will need to be done manually");
                            return false;
                        }

                        if (DebugConfig.DebugBuyItems) Log("IsSafeToTravelIntoEmpireFromHere [true].");
                        return true;
                    }

                    Log("BuildBuyList is empty: we have the supplies we need");
                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private Dictionary<int, int> BuildBuyList()
        {
            var randomCacheDurationTimeSpan = TimeSpan.FromHours(2);
            var buyList = new Dictionary<int, int>();
            int intCount = 0;
            foreach (var item in _shipsCargoBayList)
            {
                intCount++;
                var typeId = item.Item1;
                var amount = item.Item2;

                // Boosters are handled below, skip them
                if (_boosterList.Any(e => e.Item1 == typeId))
                    continue;

                var minMultiplier = 2;
                var maxMultiplier = DirectEve.CachedRandom(3, 6, randomCacheDurationTimeSpan, localUniqueName: typeId.ToString());

                // We only need 1 MTU
                if (typeId == _mtuTypeId)
                    maxMultiplier = 1;

                if (typeId == _naniteRepairPasteTypeId)
                {
                    if (ESCache.Instance.ActiveShip.IsFrigate)
                    {
                        maxMultiplier = DirectEve.CachedRandom(8, 13, randomCacheDurationTimeSpan, localUniqueName: typeId.ToString());
                    }
                    else if (ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                    	maxMultiplier = DirectEve.CachedRandom(5, 8, randomCacheDurationTimeSpan, localUniqueName: typeId.ToString());
                    }
                    else
                    {
                    	maxMultiplier = DirectEve.CachedRandom(8, 13, randomCacheDurationTimeSpan, localUniqueName: typeId.ToString());
                    }
                }

                if (typeId == _filamentTypeId)
                    maxMultiplier = DirectEve.CachedRandom(6, 9, randomCacheDurationTimeSpan, localUniqueName: typeId.ToString());

                //if (typeId == _MyAmmoTypeId)
                //    maxMultiplier = Rnd.Next(17, 24);

                if (minMultiplier > maxMultiplier)
                    minMultiplier = maxMultiplier;

                var countInHangarAndShipsBay = GetAmountofTypeIdLeftItemhangarAndCargo(typeId);

                DirectItem Item = new DirectItem(ESCache.Instance.DirectEve);
                Item.TypeId = typeId;
                if (DirectEve.Interval(10000)) Log("[" + intCount + "] _shipsCargoBayList contains [" + Item.TypeName + "] TypeID  [" + typeId + "] Amount [" + amount + "]");

                if (countInHangarAndShipsBay < amount * minMultiplier)
                {
                    DirectItem missingItem = new DirectItem(ESCache.Instance.DirectEve);
                    missingItem.TypeId = typeId;
                    if (missingItem.TypeName.ToLower().Contains("filament") && ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader && DirectEve.FleetMembers.Count > 1)
                    {
                        Log("We are in a fleet and are not the leader: we dont need any filaments: ignoring missing [" + missingItem.TypeName + "]: continuing");
                        continue;
                    }

                    Log($"We are missing [" + missingItem.TypeName + "][" + missingItem.TypeId + "] countInHangarAndShipsBay [" + countInHangarAndShipsBay + "] amount [" + amount + "] * maxMultiplier [" + maxMultiplier + "] Adding [" + amount * maxMultiplier + "] to the buy list!");
                    buyList.Add(typeId, amount * maxMultiplier);
                    continue;
                }
            }

            if (Drones.UseDrones)
            {
                if (_droneBayItemList != null && _droneBayItemList.Any())
                {
                    foreach (var item in _droneBayItemList)
                    {
                        var typeId = item.Item1;
                        var amount = item.Item2;
                        var minMultiplier = 1;
                        var maxMultiplier = DirectEve.CachedRandom(2, 4, randomCacheDurationTimeSpan, localUniqueName: typeId.ToString());
                		var mutated = item.Item4;

                        // Skip mutated drones, as they are not avail on the market
                		if (mutated)
                            continue;

                        var countInHangarAndDroneBay = GetAmountofTypeIdLeftItemhangarAndDroneBay(typeId, item.Item4);

                        if (countInHangarAndDroneBay < amount * minMultiplier)
                        {
                            DirectItem missingItem = new DirectItem(ESCache.Instance.DirectEve);
                            missingItem.TypeId = typeId;
                            Log($"We are missing [" + missingItem.TypeName + "] countInHangarAndDroneBay [" + countInHangarAndDroneBay + "] amount [" + amount + "] * maxMultiplier [" + maxMultiplier + "] Adding [" + amount * maxMultiplier + "] to the buy list");
                            buyList.Add(typeId, amount * maxMultiplier);
                            continue;
                        }
                    }
                }
            }

            if (!DebugConfig.DebugDisableDrugsBoosters)
            {
                if (_boosterList != null && _boosterList.Any())
                {
                    foreach (var item in _boosterList)
                    {
                        var typeId = item.Item1;
                        var amount = item.Item2 * 5;
                        var minMultiplier = 1;
                        var maxMultiplier = DirectEve.CachedRandom(15, 19, randomCacheDurationTimeSpan, localUniqueName: typeId.ToString());
                        if (ESCache.Instance.DirectEve.Me.Wealth != null && 100000000 > ESCache.Instance.DirectEve.Me.Wealth)
                            maxMultiplier = 1;

                        var countInHangarAndShipsBay = GetAmountofTypeIdLeftItemhangarAndCargo(typeId);

                        if (countInHangarAndShipsBay < amount * minMultiplier)
                        {
                            DirectItem missingItem = new DirectItem(ESCache.Instance.DirectEve);
                            missingItem.TypeId = typeId;
                            Log($"We are missing [" + missingItem.TypeName + "] countInHangarAndShipsBay [" + countInHangarAndShipsBay + "] amount [" + amount + "] * maxMultiplier [" + maxMultiplier + "] Adding [" + amount * maxMultiplier + "] to the buy list!.!");
                            buyList.Add(typeId, amount * maxMultiplier);
                            continue;
                        }
                    }
                }
            }

            if (!buyList.Any())
                Log("We have everything we need: buylist is empty!");

            return buyList;
        }

        private void CreateBuyList()
        {

            _buyList = new Dictionary<int, int>();
            _orderIterations = 0;
            _moveToCargoList = new Dictionary<int, int>();

            // Create a buylist based on _shipsCargoBayList, _droneBayItemList, _boosterList, filamentTypeId
            var hangar = ESCache.Instance.DirectEve.GetItemHangar();
            var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
            var droneBay = ESCache.Instance.DirectEve.GetShipsDroneBay();

            if (hangar == null || shipsCargo == null || droneBay == null)
                return;

            _buyList = BuildBuyList();

            // Make a copy of buylist and save it as _moveToCargoList
            _moveToCargoList = _buyList.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);

            if (_buyList.Any())
            {
                Log($"---- Buylist ----");
                foreach (var item in _buyList.ToList())
                {
                    Log($"TypeName [{Framework.GetInvType(item.Key)?.TypeName ?? "Unknown TypeName"}]TypeId [{item.Key}] Amount [{item.Value}] Total Volume [{Framework.GetInvType(item.Key)?.Volume * item.Value}]");
                }
                Log($"---- End Buylist ----");

                _buyItemsState = BuyItemsState.ActivateTransportShip;
            }
            else
            {
                Log("Warning: The buylist was empty.");
                _state = AbyssalState.Start;
            }
        }

        private void TravelToMarketSystem()
        {
            if (ESCache.Instance.DirectEve.Session.IsInSpace && ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.IsWarpingByMode)
                return;

            //if (!ESCache.Instance.EveAccount.ConnectToTestServer)
            //{
            //    if (_travelerDestination == null || (_travelerDestination is DockableLocationDestination && (_travelerDestination as DockableLocationDestination).DockableLocationId != 60003760))
            //    {
            //        Log("Setting _travelerDestination to Jita 4/4.");
            //        _travelerDestination = new DockableLocationDestination(60003760);
            //    }
            //}

            if (ESCache.Instance.EveAccount.ConnectToTestServer)
            {
                Log("Arrived at destination");
                _buyItemsState = BuyItemsState.BuyItems;
                return;
            }

            //if (Traveler.Destination != _travelerDestination)
            //    Traveler.Destination = _travelerDestination;

            if (DirectEve.Interval(20000))
            {
                if (_buyList == null && _buyList.Any())
                {
                    Log("We are going to market to buy some items:");
                    foreach (var itemToBuy in _buyList)
                    {
                        DirectItem directItemToBuy = new DirectItem(ESCache.Instance.DirectEve);
                        directItemToBuy.TypeId = itemToBuy.Key;
                        Log("[" + directItemToBuy.TypeName + "] we want to buy [" + itemToBuy.Value + "] units");
                    }
                }
            }

            try
            {
                //fixme
                //Traveler.TravelToBookmarkName(MarketBookmarkName);
                Traveler.TravelToStationId(ESCache.Instance.StationIDJitaP4M4);
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }

            //Traveler.ProcessState();

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Log("Arrived at destination");
                _buyItemsState = BuyItemsState.BuyItems;
                Traveler.Destination = null;

                return;
            }

            if (State.CurrentTravelerState == TravelerState.Error)
            {
                if (Traveler.Destination != null)
                    Log("Stopped traveling, traveler threw an error...");

                Traveler.Destination = null;
                _state = AbyssalState.Error;
                return;
            }

        }

        private void BuyItemsInMarketStation()
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

                Log("Finished buying changing state to MoveItemsToCargo");
                _buyItemsState = BuyItemsState.MoveItemsToCargo;
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
                    Log("We have [" +
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
                LocalPulse = DateTime.UtcNow.AddSeconds(10);

                Log("Opening market window");
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

                Log("Loading market window");

                LocalPulse = DateTime.UtcNow.AddSeconds(10);
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

                Log("Item [" + currentItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice +
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
                    {
                        if (type.TypeId == (int)_naniteRepairPasteTypeId)
                            maxPrice = 60000;

                        if (type.GroupId == (int)Group.LightMissiles)
                            maxPrice = 2500;

                        if (type.GroupId == (int)Group.HeavyMissiles)
                            maxPrice = 250;

                        if (type.GroupId == (int)Group.AbyssalDeadspaceFilament)
                        {
                            if (AbyssalTier == 0)
                                maxPrice = 70_000;
                            if (AbyssalTier == 1)
                                maxPrice = 70_000;
                            if (AbyssalTier == 2)
                                maxPrice = 150_000;
                            if (AbyssalTier == 3)
                                maxPrice = 8_000_000;
                            if (AbyssalTier == 4)
                                maxPrice = 15_000_000;
                            if (AbyssalTier == 5)
                                maxPrice = 22_000_000;
                            if (AbyssalTier == 6)
                                maxPrice = 28_000_000;
                        }

                        if (type.CategoryId == (int)CategoryID.Drone)
                        {
                            /**
                            if (type.TypeName.Contains("II"))
                                maxPrice = 800000;
                            else if (type.TypeName.Contains("Federation Navy"))
                                maxPrice = 2000000;
                            else if (type.TypeName.Contains("Federation Navy"))
                                maxPrice = 200000;
                            else if (type.TypeName.Contains("Federation Navy"))
                                maxPrice = 200000;
                            else
                                maxPrice = 200000;
                            **/
                        }

                        maxPrice = 1000;
                    }
                }

                Log("Item [" + currentItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice + "]");
            }

            // Are there any orders with an reasonable price?
            IEnumerable<DirectOrder> orders;
            if (maxPrice == 0)
            {
                Log("if(maxPrice == 0)");
                orders =
                    marketWindow.SellOrders.Where(o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.TypeId == typeID).ToList();
            }
            else
            {
                Log("if(maxPrice != 0) max price [" + maxPrice + "]");
                orders =
                    marketWindow.SellOrders.Where(
                            o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.Price < maxPrice && o.TypeId == typeID)
                        .ToList();
            }

            _orderIterations++;

            if (!orders.Any() && _orderIterations < 5)
            {
                LocalPulse = DateTime.UtcNow.AddSeconds(5);
                return;
            }

            // Is there any order left?
            if (!orders.Any())
            {
                Log("No reasonably priced item available! Removing this item from the buyList");
                _buyList.Remove(typeID);
                LocalPulse = DateTime.UtcNow.AddSeconds(3);
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
                        Log("Buying [" + remaining + "] item price [" + order.Price + "]");
                        order.Buy(remaining, DirectOrderRange.Station);
                        marketWindow.Close();
                        // Wait for the order to go through
                        LocalPulse = DateTime.UtcNow.AddSeconds(10);
                    }
                    else
                    {
                        Log("Error: We don't have enough ISK on our wallet to finish that transaction.");
                        _state = AbyssalState.Error;
                        return;
                    }
                }
            }
        }


        private void MoveItemsToCargo()
        {
            if (!ESCache.Instance.InStation)
                return;

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                return;

            if (ESCache.Instance.CurrentShipsCargo == null)
                return;

            List<DirectItem> items = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => _moveToCargoList.ContainsKey(i.TypeId)).ToList();
            if (items.Any())
            {
                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("MoveItemsToCargo"))
                {
                    Log($"Waiting on locked items.");
                    return;
                }

                if (ESCache.Instance.CurrentShipsCargo.UsedCapacity == null)
                {
                    Log($"if (ESCache.Instance.CurrentShipsCargo.UsedCapacity == null)");
                    return;
                }

                foreach (var item in items.OrderBy(a => Guid.NewGuid()))
                {
                    var maxAmountToMove = Math.Min(item.Stacksize, _moveToCargoList[item.TypeId]);
                    maxAmountToMove = Math.Max(1, maxAmountToMove);
                    var volumeToMove = item.Volume * maxAmountToMove;


                    var remainingCapacity = ESCache.Instance.CurrentShipsCargo.Capacity - (double)ESCache.Instance.CurrentShipsCargo.UsedCapacity;

                    if (volumeToMove > remainingCapacity)
                    {
                        Log($"Not enough cargo space left to add [" + maxAmountToMove + "][" + item.TypeName + "]. We can only grab some of those...");
                        maxAmountToMove = (int)Math.Floor(remainingCapacity / item.Volume);
                        if (0 >= maxAmountToMove)
                        {
                            Log("We cant fit any more [" + item.TypeName + "]");
                            continue;
                        }
                    }

                    if (ESCache.Instance.CurrentShipsCargo.Add(item, maxAmountToMove))
                    {
                        LocalPulse = DateTime.UtcNow.AddSeconds(5);
                        Log($"Moving Amount [{maxAmountToMove}] TypeName [{item.TypeName}] to the current ships cargohold.");
                        return;
                    }
                }

                if (ESCache.Instance.CurrentShipsCargo != null && !ESCache.Instance.CurrentShipsCargo.Items.Any())
                {
                    Log("No Items in cargohold");
                    return;
                }

                Log("CargoHold contains [" + ESCache.Instance.CurrentShipsCargo.Items.Count() + "] items");
            }

            Log("Done moving items to cargohold");
            _buyItemsState = BuyItemsState.TravelToHomeSystem;
        }

        private void TravelToHomeSystem()
        {
            if (State.CurrentTravelerState != TravelerState.AtDestination)
            {
                Traveler.TravelToBookmark(myHomebookmark);
            }
            else
            {
                Traveler.Destination = null;
                State.CurrentTravelerState = TravelerState.Idle;
                Log($"Arrived at the home station.");
                _buyItemsState = BuyItemsState.UnloadLoot;
            }
        }

        private void AbyssalUnloadLoot()
        {

            if (!AreWeDockedInHomeSystem())
            {
                Log("We are not docking in the home system, going to the home system.");
                _buyItemsState = BuyItemsState.TravelToHomeSystem;
                return;
            }

            if (ESCache.Instance.ActiveShip == null)
            {
                Log("Active ship is null.");
                return;
            }

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log($"Itemhangar is null.");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log("Current ships cargo is null.");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo.Items.Any())
            {

                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("UnloadLoot"))
                    return;
                if (ESCache.Instance.DirectEve.GetItemHangar().Add(ESCache.Instance.CurrentShipsCargo.Items))
                {
                    Log($"Moving items into itemhangar.");
                    LocalPulse = UTCNowAddMilliseconds(3000, 3500);
                    return;
                }
            }
            else
            {
                // done
                Log("We finished buying items");
                _state = AbyssalState.Start;
                _buyItemsState = BuyItemsState.Start;
            }
        }
    }
}
