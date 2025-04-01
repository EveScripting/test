extern alias SC;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Questor.Storylines
{
    internal enum MaterialsForWarPreparationArmState
    {
        MakeTransportShipActive,
        CheckMarketForOre,
        TravelToOreSystem,
        BuyOre,
        MoveOreToShip,
        DeclineMission
    }

    public class MaterialsForWarPreparation : IStoryline
    {
        #region Fields

        private DateTime _nextAction;

        private MaterialsForWarPreparationArmState CurrentArmState { get; set; } = MaterialsForWarPreparationArmState.MakeTransportShipActive;

        private bool ChangeMaterialsForWarPreperationArmState(MaterialsForWarPreparationArmState _MFWStateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (CurrentArmState != _MFWStateToSet)
                {
                    Log.WriteLine("New MaterialsForWarPreparationArmState [" + _MFWStateToSet + "]");
                    CurrentArmState = _MFWStateToSet;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        #endregion Fields

        #region Properties

        private int BuyOreSolarSystemId { get; set; }
        private int BuyOreStationId { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Arm does nothing but get into a (assembled) shuttle
        /// </summary>
        /// <returns></returns>
        public StorylineState Arm(Storyline storyline)
        {
            if (_nextAction > DateTime.UtcNow)
            {
                if (DebugConfig.DebugStorylineMissions) Log.WriteLine("MaterialsForWar: Arm: if (_nextAction > DateTime.UtcNow)");
                return StorylineState.Arm;
            }

            switch (CurrentArmState)
            {
                case MaterialsForWarPreparationArmState.MakeTransportShipActive:

                    if (ESCache.Instance.ShipHangar == null)
                    {
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("MaterialsForWar: Arm: MakeTransportShipActive: if (ESCache.Instance.ShipHangar == null)");
                        return StorylineState.Arm;
                    }
                    if (MissionSettings.StorylineMission.Agent.AgentWindow == null)
                    {
                        MissionSettings.StorylineMission.Agent.OpenAgentWindow(true);
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("MaterialsForWar: Arm: MakeTransportShipActive: if (MissionSettings.StorylineMission.Agent.Window == null)");
                        return StorylineState.Arm;
                    }

                    if (MissionSettings.StorylineMission.Agent.AgentWindow.Objective.Contains("Veldspar"))
                    {
                        MissionSettings.MaterialsForWarOreID = 1230; //Veldspar
                        MissionSettings.MaterialsForWarOreQty = 1000;
                    }

                    if (MissionSettings.StorylineMission.Agent.AgentWindow.Objective.Contains("Scordite"))
                    {
                        MissionSettings.MaterialsForWarOreID = 1228;
                        MissionSettings.MaterialsForWarOreQty = 1665;
                    }

                    if (MissionSettings.StorylineMission.Agent.AgentWindow.Objective.Contains("Omber"))
                    {
                        MissionSettings.MaterialsForWarOreID = 1227;
                        MissionSettings.MaterialsForWarOreQty = 10000;
                    }

                    if (MissionSettings.StorylineMission.Agent.AgentWindow.Objective.Contains("Kernite"))
                    {
                        MissionSettings.MaterialsForWarOreID = 20;
                        MissionSettings.MaterialsForWarOreQty = 8000;
                    }

                    if (ESCache.Instance.ActiveShip.GivenName == null)
                    {
                        if (DebugConfig.DebugArm || DebugConfig.DebugStorylineMissions) Log.WriteLine("if (Cache.Instance.ActiveShip == null)");
                        _nextAction = DateTime.UtcNow.AddSeconds(3);
                        return StorylineState.Arm;
                    }

                    List<DirectItem> ships = ESCache.Instance.ShipHangar.ValidShipsToUse;
                    bool transportShipInCurrentHangar = ships.Any(ship => ship.IsStorylineHaulingShip);

                    if (!transportShipInCurrentHangar)
                    {
                        Log.WriteLine("No transport ship named [" + Settings.Instance.StorylineTransportShipName + "] found. Declining Mission.");
                        return StorylineState.BlacklistAgentForThisSession;
                    }

                    if (!ESCache.Instance.ActiveShip.IsStorylineHaulingShip)
                    {
                        foreach (DirectItem ship in ships.Where(ship => ship.IsStorylineHaulingShip))
                        {
                            Log.WriteLine("Found a storyline transport ship named [" + Settings.Instance.StorylineTransportShipName + "]: Activating.");
                            ship.ActivateShip();
                            _nextAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                            return StorylineState.Arm;
                        }
                    }

                    ChangeMaterialsForWarPreperationArmState(MaterialsForWarPreparationArmState.CheckMarketForOre);
                    return StorylineState.Arm;

                case MaterialsForWarPreparationArmState.CheckMarketForOre:
                    {
                        try
                        {
                            int oreid = MissionSettings.MaterialsForWarOreID;
                            int orequantity = MissionSettings.MaterialsForWarOreQty;
                            DirectEve directEve = ESCache.Instance.DirectEve;
                            DirectMarketWindow marketWindow = directEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();
                            // We do not have enough ore, open the market window
                            if (marketWindow == null)
                            {
                                if (!ESCache.Instance.OkToInteractWithEveNow)
                                {
                                    if (DebugConfig.DebugInteractWithEve) Log.WriteLine("MaterialsForWar.CheckMarketForOre: !OkToInteractWithEveNow");
                                    return StorylineState.Arm;
                                }

                                if (directEve.ExecuteCommand(DirectCmd.OpenMarket))
                                {
                                    _nextAction = DateTime.UtcNow.AddSeconds(10);
                                    Log.WriteLine("Opening market window");
                                    ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                                    Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
                                    return StorylineState.Arm;
                                }

                                return StorylineState.Arm;
                            }

                            // Wait for the window to become ready (this includes loading the ore info)
                            if (marketWindow != null && !marketWindow.IsReady)
                            {
                                if(DebugConfig.DebugStorylineMissions) Log.WriteLine("CheckMarketForOre: if (marketWindow != null && !marketWindow.IsReady)");
                                return StorylineState.Arm;
                            }

                            // Are we currently viewing ore orders?
                            if (marketWindow.DetailTypeId != oreid)
                                if (marketWindow.LoadTypeId(oreid))
                                {
                                    Log.WriteLine("Loading market window with typeid:" + oreid);
                                    _nextAction = DateTime.UtcNow.AddSeconds(5);
                                    return StorylineState.Arm;
                                }

                            // Get the median sell price
                            DirectInvType type = ESCache.Instance.DirectEve.GetInvType(oreid);

                            DirectInvType OreTypeNeededForThisMission = type;
                            double maxPrice = 0;

                            if (OreTypeNeededForThisMission != null)
                            {
                                Log.WriteLine("OreTypeNeededForThisMission.BasePrice: " + OreTypeNeededForThisMission.BasePrice);
                                maxPrice = OreTypeNeededForThisMission.BasePrice / OreTypeNeededForThisMission.PortionSize;
                                maxPrice *= 10;
                            }
                            else
                            {
                                Log.WriteLine("OreTypeNeededForThisMission == null");
                            }

                            IEnumerable<DirectOrder> orders;

                            if (maxPrice != 0 && marketWindow.SellOrders != null && marketWindow.SellOrders.Count > 0)
                            {
                                Log.WriteLine("Max price is: " + maxPrice);
                                orders = marketWindow.SellOrders.Where(sellorderInRegion =>
                                    ESCache.Instance.SolarSystems != null &&
                                    ESCache.Instance.SolarSystems.Any(solarsystem =>
                                        solarsystem.Id == sellorderInRegion.SolarSystemId &&
                                        solarsystem.IsHighSecuritySpace)).Where(highSecSellOrder => highSecSellOrder.StationId != -1 &&
                                                                                               highSecSellOrder.StationId != 0 &&
                                                                                               highSecSellOrder.Price < maxPrice &&
                                                                                               highSecSellOrder.VolumeRemaining > orequantity).ToList();
                            }
                            else
                            {
                                Log.WriteLine("Max price could not be found. Declining Mission");
                                return StorylineState.BlacklistAgentForThisSession;
                            }

                            if (!orders.Any())
                            {
                                marketWindow.Close();
                                Log.WriteLine("There are no orders in the current region. Declining Mission");
                                return StorylineState.BlacklistAgentForThisSession;
                            }

                            DirectOrder order = orders.OrderBy(s => s.Jumps).FirstOrDefault();
                            if (order != null)
                            {
                                Log.WriteLine("Using order from station: " + order.StationId + " volume remaining: " + order.VolumeRemaining);
                                BuyOreStationId = order.StationId;
                                BuyOreSolarSystemId = order.SolarSystemId;
                                ChangeMaterialsForWarPreperationArmState(MaterialsForWarPreparationArmState.TravelToOreSystem);
                                if (marketWindow != null)
                                    marketWindow.Close();
                                return StorylineState.Arm;
                            }

                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                            break;
                        }
                    }

                case MaterialsForWarPreparationArmState.TravelToOreSystem:

                    if (BuyOreStationId == 0 || BuyOreSolarSystemId == 0)
                    {
                        Log.WriteLine("if (buyOreStationId == 0 || buyOreSolarSystemId == 0) - Declining Mission");
                        return StorylineState.BlacklistAgentForThisSession;
                    }

                    if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != BuyOreSolarSystemId)
                    {
                        Traveler.Destination = new StationDestination(BuyOreSolarSystemId, BuyOreStationId,
                            ESCache.Instance.DirectEve.GetLocationName(BuyOreStationId));
                        return StorylineState.Arm;
                    }

                    if (!Storyline.RouteToStorylineAgentIsSafe(BuyOreStationId, BuyOreSolarSystemId))
                    {
                        if (Storyline.HighSecChecked && !ESCache.Instance.RouteIsAllHighSecBool)
                        {
                            Log.WriteLine("This Materials For War mission is located in Low Security Space: Declining Mission");
                            return StorylineState.RemoveOffer;
                        }

                        return StorylineState.Arm;
                    }

                    if (DebugConfig.DebugStorylineMissions) Log.WriteLine("We are still traveling");
                    Traveler.ProcessState();
                    if (State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("We are at the destination");
                        Traveler.Destination = null;
                        //_setDestinationStation = false;
                        ChangeMaterialsForWarPreperationArmState(MaterialsForWarPreparationArmState.BuyOre);
                        return StorylineState.Arm;
                    }

                    break;

                case MaterialsForWarPreparationArmState.BuyOre:
                    {
                        int oreid = MissionSettings.MaterialsForWarOreID;
                        int orequantity = MissionSettings.MaterialsForWarOreQty;
                        DirectEve directEve = ESCache.Instance.DirectEve;
                        DirectMarketWindow marketWindow = directEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                        // We do not have enough ore, open the market window
                        if (marketWindow == null)
                        {
                            if (!ESCache.Instance.OkToInteractWithEveNow)
                            {
                                if (DebugConfig.DebugInteractWithEve) Log.WriteLine("MaterialsForWar.CheckMarketForOre: !OkToInteractWithEveNow");
                                return StorylineState.Arm;
                            }

                            if (directEve.ExecuteCommand(DirectCmd.OpenMarket))
                            {
                                _nextAction = DateTime.UtcNow.AddSeconds(10);
                                Log.WriteLine("Opening market window");
                                ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                                Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
                                return StorylineState.Arm;
                            }

                            return StorylineState.Arm;
                        }

                        // Wait for the window to become ready (this includes loading the ore info)
                        if (!marketWindow.IsReady)
                        {
                            if (DebugConfig.DebugStorylineMissions) Log.WriteLine("BuyOre: if (!marketWindow.IsReady)");
                            return StorylineState.Arm;
                        }

                        // Are we currently viewing ore orders?
                        if (marketWindow.DetailTypeId != oreid)
                        {
                            // No, load the ore orders
                            if (marketWindow.LoadTypeId(oreid))
                            {
                                Log.WriteLine("Loading market window with typeid:" + oreid);
                                _nextAction = DateTime.UtcNow.AddSeconds(5);
                                return StorylineState.Arm;
                            }

                            return StorylineState.Arm;
                        }

                        // Get the median sell price
                        DirectInvType type = ESCache.Instance.DirectEve.GetInvType(oreid);

                        DirectInvType OreTypeNeededForThisMission = type;
                        double maxPrice = 0;

                        if (OreTypeNeededForThisMission != null)
                        {
                            maxPrice = OreTypeNeededForThisMission.BasePrice / OreTypeNeededForThisMission.PortionSize;
                            maxPrice *= 10;
                        }

                        IEnumerable<DirectOrder> orders;

                        if (maxPrice != 0)
                            orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId && o.Price < maxPrice).ToList();
                        else
                            orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId).ToList();

                        // Do we have orders that sell enough ore for the mission?
                        if (!orders.Any())
                            orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId).ToList();

                        if (!orders.Any() || orders.Sum(o => o.VolumeRemaining) < orequantity)
                        {
                            Log.WriteLine("Not enough (reasonably priced) ore available! maxPrice [" + maxPrice + "]: Declining Mission!");

                            // Close the market window
                            marketWindow.Close();

                            // No, decline mission
                            return StorylineState.BlacklistAgentForThisSession;
                        }

                        // How much ore do we still need?
                        int neededQuantity = orequantity - ESCache.Instance.ItemHangar.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity);
                        if (neededQuantity > 0)
                        {
                            // Get the first order
                            DirectOrder order = orders.OrderBy(o => o.Price).FirstOrDefault();
                            if (order != null)
                            {
                                // Calculate how much ore we still need
                                int remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                                order.Buy(remaining, DirectOrderRange.Station);

                                Log.WriteLine("Buying [" + remaining + "][" + order.TypeName + "] ore");

                                // Wait for the order to go through
                                _nextAction = DateTime.UtcNow.AddSeconds(10);
                            }
                        }
                        else
                        {
                            ChangeMaterialsForWarPreperationArmState(MaterialsForWarPreparationArmState.MoveOreToShip);

                            if (marketWindow != null)
                                marketWindow.Close();

                            return StorylineState.Arm;
                        }
                        break;
                    }

                case MaterialsForWarPreparationArmState.DeclineMission:
                    {
                        return StorylineState.DeclineMission;
                    }

                case MaterialsForWarPreparationArmState.MoveOreToShip:
                    {
                        if (ESCache.Instance.ItemHangar == null) return StorylineState.Arm;
                        if (ESCache.Instance.CurrentShipsCargo == null) return StorylineState.Arm;
                        if (ESCache.Instance.CurrentShipsCargo.Items == null)
                        {
                            Log.WriteLine("if (ESCache.Instance.CurrentShipsCargo.Items == null)");
                            return StorylineState.Arm;
                        }

                        if (ESCache.Instance.ItemHangar.Items == null)
                        {
                            Log.WriteLine("if (ESCache.Instance.ItemHangar.Items == null)");
                            return StorylineState.Arm;
                        }

                        List<DirectItem> items = ESCache.Instance.ItemHangar.Items.Where(k => k.TypeId == MissionSettings.MaterialsForWarOreID).ToList();
                        List<DirectItem> itemsInShipCargo = ESCache.Instance.CurrentShipsCargo.Items
                            .Where(k => k.TypeId == MissionSettings.MaterialsForWarOreID)
                            .ToList();

                        if (itemsInShipCargo.Count > 0 && itemsInShipCargo.Sum(i => i.Stacksize) >= MissionSettings.MaterialsForWarOreQty)
                        {
                            Log.WriteLine("We have enough ore in the ships cargo.");
                            return StorylineState.GotoAgent;
                        }

                        if (items.Count == 0 || items.Sum(k => k.Stacksize) < MissionSettings.MaterialsForWarOreQty)
                        {
                            Log.WriteLine("Ore for MaterialsForWar: typeID [" + MissionSettings.MaterialsForWarOreID + "] not found in ItemHangar: Declining Mission");
                            return StorylineState.RemoveOffer;
                        }

                        int oreIncargo = 0;
                        foreach (DirectItem cargoItem in ESCache.Instance.CurrentShipsCargo.Items.ToList())
                        {
                            if (cargoItem.TypeId != MissionSettings.MaterialsForWarOreID)
                                continue;

                            oreIncargo += cargoItem.Quantity;
                        }

                        int oreToLoad = MissionSettings.MaterialsForWarOreQty - oreIncargo;
                        if (oreToLoad <= 0)
                        {
                            Log.WriteLine("return StorylineState.GotoAgent");
                            return StorylineState.GotoAgent;
                        }

                        DirectItem item = items.FirstOrDefault();
                        if (item != null)
                        {
                            int moveOreQuantity = Math.Min(item.Stacksize, oreToLoad);

                            double volumeToMove = item.Volume * moveOreQuantity;
                            if (ESCache.Instance.CurrentShipsCargo.Capacity > 0 && ESCache.Instance.CurrentShipsCargo.Capacity - (double)ESCache.Instance.CurrentShipsCargo.UsedCapacity < volumeToMove)
                            {
                                Log.WriteLine("Transport ship has not enough free space. total cargospace [" + Math.Round(ESCache.Instance.CurrentShipsCargo.Capacity, 2) + "] Used [" + Math.Round((double)ESCache.Instance.CurrentShipsCargo.UsedCapacity, 2) + "] vol to move [" + Math.Round(volumeToMove, 2) + "]: Declining Mission.");
                                return StorylineState.RemoveOffer;
                            }
                            if (!ESCache.Instance.CurrentShipsCargo.Add(item, moveOreQuantity)) return StorylineState.Arm;
                            Log.WriteLine("Moving [" + moveOreQuantity + "] units of Ore [" + item.TypeName + "] Stack size: [" + item.Stacksize +
                                          "] from hangar to CargoHold");
                            _nextAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(3, 6));
                            return StorylineState.Arm; // you can only move one set of items per frame
                        }
                    }
                    break;
            }
            return StorylineState.Arm;
        }

        public StorylineState BeforeGotoAgent(Storyline storyline)
        {
            Reset();
            DirectEve directEve = ESCache.Instance.DirectEve;
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.BeforeGotoAgent;

            int oreid = MissionSettings.MaterialsForWarOreID;
            int orequantity = MissionSettings.MaterialsForWarOreQty;

            // Open the item hangar
            if (ESCache.Instance.ItemHangar == null) return StorylineState.BeforeGotoAgent;

            // Is there a market window?
            DirectMarketWindow marketWindow = directEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

            // Do we have the ore we need in the Item Hangar?.

            if (ESCache.Instance.ItemHangar.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity) >= orequantity)
            {
                DirectItem thisOreInhangar = ESCache.Instance.ItemHangar.Items.Find(i => i.TypeId == oreid);
                if (thisOreInhangar != null)
                    Log.WriteLine("We have [" + ESCache.Instance.ItemHangar.Items.Where(i => i.TypeId == oreid)
                                      .Sum(i => i.Quantity)
                                      .ToString(CultureInfo.InvariantCulture) +
                                  "] " + thisOreInhangar.TypeName + " in the item hangar accepting mission");

                // Close the market window if there is one
                if (marketWindow != null)
                    marketWindow.Close();

                return StorylineState.GotoAgent;
            }

            if (ESCache.Instance.CurrentShipsCargo == null) return StorylineState.BeforeGotoAgent;

            if (ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity) >= orequantity)
            {
                DirectItem thisOreInhangar = ESCache.Instance.CurrentShipsCargo.Items.Find(i => i.TypeId == oreid);
                if (thisOreInhangar != null)
                    Log.WriteLine("We have [" +
                                  ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == oreid)
                                      .Sum(i => i.Quantity)
                                      .ToString(CultureInfo.InvariantCulture) + "] " +
                                  thisOreInhangar.TypeName + " in the CargoHold accepting mission");

                // Close the market window if there is one
                if (marketWindow != null)
                    marketWindow.Close();

                return StorylineState.GotoAgent;
            }

            // We do not have enough ore, open the market window
            if (marketWindow == null)
            {
                if (!ESCache.Instance.OkToInteractWithEveNow)
                {
                    if (DebugConfig.DebugInteractWithEve) Log.WriteLine("MaterialsForWar: MarketWindow: !OkToInteractWithEveNow");
                    return StorylineState.GotoAgent;
                }

                if (directEve.ExecuteCommand(DirectCmd.OpenMarket))
                {
                    _nextAction = DateTime.UtcNow.AddSeconds(10);
                    Log.WriteLine("Opening market window");
                    ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                    Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
                    return StorylineState.BeforeGotoAgent;
                }

                return StorylineState.BeforeGotoAgent;
            }

            // Wait for the window to become ready (this includes loading the ore info)
            if (!marketWindow.IsReady)
                return StorylineState.BeforeGotoAgent;

            // Are we currently viewing ore orders?
            if (marketWindow.DetailTypeId != oreid)
            {
                // No, load the ore orders
                if (marketWindow.LoadTypeId(oreid))
                {
                    Log.WriteLine("Loading market window");
                    _nextAction = DateTime.UtcNow.AddSeconds(5);
                    return StorylineState.BeforeGotoAgent;
                }

                return StorylineState.BeforeGotoAgent;
            }

            // Get the median sell price
            DirectInvType type = ESCache.Instance.DirectEve.GetInvType(oreid);

            DirectInvType OreTypeNeededForThisMission = type;
            double maxPrice = 0;

            if (OreTypeNeededForThisMission != null)
            {
                maxPrice = OreTypeNeededForThisMission.BasePrice / OreTypeNeededForThisMission.PortionSize;
                maxPrice *= 10;
            }

            IEnumerable<DirectOrder> orders;

            if (maxPrice != 0)
                orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId && o.Price < maxPrice).ToList();
            else
                orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId).ToList();

            // Do we have orders that sell enough ore for the mission?

            orders = orders.Where(o => o.StationId == directEve.Session.StationId).ToList();
            if (!orders.Any() || orders.Sum(o => o.VolumeRemaining) < orequantity)
            {
                Log.WriteLine("Not enough (reasonably priced) ore available! maxPrice [" + maxPrice + "] Declining Mission.");

                // Close the market window
                marketWindow.Close();
                return StorylineState.BlacklistAgentForThisSession;
            }

            // How much ore do we still need?
            int neededQuantity = orequantity - ESCache.Instance.ItemHangar.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity);
            if (neededQuantity > 0)
            {
                // Get the first order
                DirectOrder order = orders.OrderBy(o => o.Price).FirstOrDefault();
                if (order != null)
                {
                    // Calculate how much ore we still need
                    int remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                    order.Buy(remaining, DirectOrderRange.Station);
                    Log.WriteLine("Buying [" + remaining + "] ore");
                    // Wait for the order to go through
                    _nextAction = DateTime.UtcNow.AddSeconds(10);
                }
            }

            return StorylineState.BeforeGotoAgent;
        }

        /// <summary>
        ///     We have no execute mission code
        /// </summary>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            return StorylineState.CompleteMission;
        }

        /// <summary>
        ///     We have no combat/delivery part in this mission, just accept it
        /// </summary>
        /// <returns></returns>
        public StorylineState PostAcceptMission(Storyline storyline)
        {
            // Close the market window (if its open)
            return StorylineState.CompleteMission;
        }

        /// <summary>
        ///     Check if we have kernite in station
        /// </summary>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            return StorylineState.AcceptMission;
        }

        public void Reset()
        {
            ChangeMaterialsForWarPreperationArmState(MaterialsForWarPreparationArmState.MakeTransportShipActive);
            BuyOreStationId = 0;
            BuyOreSolarSystemId = 0;
            //_highSecChecked = false;
            //_highSecCounter = 0;
            //_setDestinationStation = false;
        }

        #endregion Methods
    }
}