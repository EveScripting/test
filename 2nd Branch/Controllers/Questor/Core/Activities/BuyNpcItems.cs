extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.Storylines;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Questor.Actions
{
    public static class BuyNpcItems
    {
        #region Fields

        private static Dictionary<InventoryItem, int> ListOfNpcItemsToBuy = new Dictionary<InventoryItem, int>();

        private static int buySolarSystemId;
        private static int buyStationId;

        #endregion Fields

        #region Methods

        public static bool ChangeBuyNpcItemsState(BuyNpcItemsState state, bool wait = false)
        {
            try
            {
                if (State.CurrentBuyNpcItemsState != state)
                {
                    Log.WriteLine("New BuyNpcItemsState [" + state + "]");
                    State.CurrentBuyNpcItemsState = state;
                    if (wait)
                        return true;
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

        public static void ProcessState()
        {
            switch (State.CurrentBuyNpcItemsState)
            {
                case BuyNpcItemsState.Idle:
                    if (ListOfNpcItemsToBuy != null && ListOfNpcItemsToBuy.Count > 0)
                        ChangeBuyNpcItemsState(BuyNpcItemsState.Start);
                    break;

                case BuyNpcItemsState.Start:
                    ChangeBuyNpcItemsState(BuyNpcItemsState.ActivateTransportShip);
                    break;

                case BuyNpcItemsState.ActivateTransportShip:
                    if (Arm.ActivateShip(Settings.Instance.TransportShipName)) return;
                    ChangeBuyNpcItemsState(BuyNpcItemsState.CheckMarket);
                    break;

                case BuyNpcItemsState.CheckMarket:
                    if (ListOfNpcItemsToBuy != null && ListOfNpcItemsToBuy.Count > 0)
                        foreach (KeyValuePair<InventoryItem, int> NpcItemToBuy in ListOfNpcItemsToBuy)
                        {
                            bool? boolCheckMarketForItem = CheckMarketForItem(NpcItemToBuy.Key, NpcItemToBuy.Value);
                            if (boolCheckMarketForItem == null)
                            {
                                Log.WriteLine("CheckMarket: Unable to find any reasonabley priced [" + NpcItemToBuy.Key.Name + "] in the region. continuing");
                                continue;
                            }

                            if (!(bool)boolCheckMarketForItem) return;

                            ChangeBuyNpcItemsState(BuyNpcItemsState.TravelToNpcMarketStation);
                        }

                    break;

                case BuyNpcItemsState.TravelToNpcMarketStation:
                    bool? boolTravelTpNpcMarketStation = TravelToNpcMarketStation(buyStationId, buySolarSystemId);
                    if (boolTravelTpNpcMarketStation == null)
                    {
                        Log.WriteLine("TravelToNpcMarketStation: Error: aborting");
                        ChangeBuyNpcItemsState(BuyNpcItemsState.Error);
                    }

                    if (!(bool)boolTravelTpNpcMarketStation) return;

                    ChangeBuyNpcItemsState(BuyNpcItemsState.BuyNpcMarketItems);
                    break;

                case BuyNpcItemsState.BuyNpcMarketItems:

                    break;

                case BuyNpcItemsState.Done:
                    break;
            }
        }

        private static bool? CheckMarketForItem(InventoryItem ItemToLookFor, int QuantityOfItemToFind)
        {
            DirectMarketWindow marketWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();
            // We do not have enough ore, open the market window
            if (marketWindow == null)
            {
                if (!ESCache.Instance.OkToInteractWithEveNow)
                {
                    if (DebugConfig.DebugInteractWithEve) Log.WriteLine("CheckMarketForItem: !OkToInteractWithEveNow");
                    return false;
                }

                if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket))
                {
                    //_nextAction = DateTime.UtcNow.AddSeconds(10);
                    Log.WriteLine("Opening market window");
                    ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                    Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
                    return false;
                }

                return false;
            }

            // Wait for the window to become ready (this includes loading the ore info)
            if (!marketWindow.IsReady)
                return false;

            // Are we currently viewing ore orders?
            if (marketWindow.DetailTypeId != ItemToLookFor.TypeId)
            {
                // No, load the ore orders
                if (marketWindow.LoadTypeId(ItemToLookFor.TypeId))
                {
                    Log.WriteLine("CheckMarketForItem: Loading market window with typeid:" + ItemToLookFor);
                    //_nextAction = DateTime.UtcNow.AddSeconds(5);
                    return false;
                }

                return false;
            }

            double maxPrice = 0;
            maxPrice = ItemToLookFor.SellOrderValue * 2;

            IEnumerable<DirectOrder> orders;

            if (maxPrice != 0 && marketWindow.SellOrders != null && marketWindow.SellOrders.Count > 0)
            {
                Log.WriteLine("CheckMarketForItem: Max price is: " + maxPrice);
                orders = marketWindow.SellOrders.Where(o => ESCache.Instance.SolarSystems.Any(v => v.Id == o.SolarSystemId && v.IsHighSecuritySpace) && o.StationId != -1 && o.StationId != 0 && o.Price < maxPrice &&
                                                                                                                                                            o.VolumeRemaining > QuantityOfItemToFind)
                    .ToList();
            }
            else
            {
                Log.WriteLine("CheckMarketForItem: Max price could not be found. aborting");
                return null;
            }

            if (!orders.Any())
            {
                marketWindow.Close();
                Log.WriteLine("CheckMarketForItem: There are no oders in the current region. aborting");
                return null;
            }

            DirectOrder order = orders.OrderBy(s => s.Jumps).FirstOrDefault();
            if (order != null)
            {
                Log.WriteLine("CheckMarketForItem: Using order from station: " + order.StationId + " volume remaining: " + order.VolumeRemaining);
                buyStationId = order.StationId;
                buySolarSystemId = order.SolarSystemId;
                marketWindow.Close();
                return true;
            }

            return false;
        }

        private static bool? TravelToNpcMarketStation(int buyStationId, int buySolarSystemId)
        {
            if (buyStationId == 0 || buySolarSystemId == 0)
            {
                Log.WriteLine("TravelToNpcMarketStation: buyStationId [" + buyStationId + "] buySolarSystemId [" + buyStationId + "] neither can be 0!");
                return null;
            }

            if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != buySolarSystemId)
            {
                Traveler.Destination = new StationDestination(buySolarSystemId, buyStationId,
                    ESCache.Instance.DirectEve.GetLocationName(buyStationId));
                return false;
            }

            if (!Storyline.RouteToStorylineAgentIsSafe(buyStationId, buySolarSystemId))
            {
                if (Storyline.HighSecChecked && !ESCache.Instance.RouteIsAllHighSecBool)
                    Log.WriteLine("TravelToNpcMarketStation: Route takes us through lowsec, this should not occur! Error!");

                return false;
            }

            Traveler.ProcessState();
            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Traveler.Destination = null;
                State.CurrentBuyNpcItemsState = BuyNpcItemsState.BuyNpcMarketItems;
                return true;
            }

            return false;
        }

        #endregion Methods
    }
}