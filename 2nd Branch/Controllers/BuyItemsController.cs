extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

/*
* Created by SharpDevelop.
* User: duketwo
* Date: 26.06.2016
* Time: 18:31
*
* To change this template use Tools | Options | Coding | Edit Standard Headers.
*/

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of ExampleController.
    /// </summary>
    public class BuyItemsController : BaseController
    {
        #region Constructors

        public BuyItemsController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Fields

        public static int buyAmmoAttempts = 0;
        private const int hoursBetweenAmmoBuy = 72;
        private const int maxAmmoMultiplier = 100;
        private const int maxAvgPriceMultiplier = 4;
        private const int maxBasePriceMultiplier = 16;
        private const int maxStateIterations = 500;
        private static Dictionary<int, int> buyList = new Dictionary<int, int>();

        private static Dictionary<int, int> _moveToCargoList = new Dictionary<int, int>();

        private static DateTime nextAction = DateTime.MinValue;

        private static int orderIterations;

        private static Dictionary<BuyItemsState, int> stateIterations = new Dictionary<BuyItemsState, int>();

        private static TravelerDestination travelerDestination;

        private static int MinAmmoMultiplier
        {
            get
            {
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                    return 4;

                return 20;
            }
        }

        #endregion Fields

        #region Properties

        public static BuyItemsState CurrentBuyItemsState { get; set; } // idle == default

        private static bool StateCheckEveryPulse
        {
            get
            {
                if (stateIterations.ContainsKey(CurrentBuyItemsState))
                    stateIterations[CurrentBuyItemsState]++;
                else
                    stateIterations.AddOrUpdate(CurrentBuyItemsState, 1);

                if (stateIterations[CurrentBuyItemsState] >= maxStateIterations && CurrentBuyItemsState != BuyItemsState.TravelToDestinationStation &&
                    CurrentBuyItemsState != BuyItemsState.TravelToHomeSystem)
                {
                    Log("ERROR:  if (stateIterations[state] >= maxStateIterations)");
                    ChangeBuyItemsControllerState(BuyItemsState.Error, true);
                    return true;
                }

                return true;
            }
        }

        #endregion Properties

        #region Methods

        public static bool ChangeBuyItemsControllerState(BuyItemsState baControllerStateToSet, bool wait = false)
        {
            try
            {
                if (CurrentBuyItemsState != baControllerStateToSet)
                {
                    Log("New BuyAmmoControllerBehaviorState [" + baControllerStateToSet + "]");
                    CurrentBuyItemsState = baControllerStateToSet;
                    if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void ProcessState()
        {
            if (nextAction > DateTime.UtcNow)
                return;

            if (!StateCheckEveryPulse)
                return;

            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return;

            // defense
            //Defense.ProcessState();

            switch (CurrentBuyItemsState)
            {
                case BuyItemsState.Idle:
                    stateIterations = new Dictionary<BuyItemsState, int>();
                    ChangeBuyItemsControllerState(BuyItemsState.AmmoCheck, false);
                    break;

                case BuyItemsState.AmmoCheck:
                    if (!ESCache.Instance.InStation)
                        return;

                    if (buyAmmoAttempts > 5)
                        return;

                    if (ESCache.Instance.EveAccount.LastAmmoBuy.AddHours(hoursBetweenAmmoBuy) > DateTime.UtcNow && ESCache.Instance.SelectedController != "CareerAgentController")
                    {
                        Log("We were buying ammo already in the past [" + hoursBetweenAmmoBuy + "] hours.");
                        ChangeBuyItemsControllerState(BuyItemsState.Done, false);
                        return;
                    }

                    if (ESCache.Instance.AmmoHangar == null)
                        return;

                    buyList = new Dictionary<int, int>();
                    _moveToCargoList = new Dictionary<int, int>();

                    bool buy = false;

                    if (DirectUIModule.DefinedAmmoTypes.Count > 0)
                        foreach (AmmoType ammo in DirectUIModule.DefinedAmmoTypes)
                        {
                            int totalQuantity = 0;
                            if (ESCache.Instance.AmmoHangar.Items != null && ESCache.Instance.AmmoHangar.Items.Count > 0 && ESCache.Instance.AmmoHangar.Items.Any(i => i.TypeId == ammo.TypeId))
                                totalQuantity = ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Stacksize);

                            int minQty = ammo.Quantity * MinAmmoMultiplier;
                            if (totalQuantity < minQty)
                            {
                                Log("Total ammo amount in hangar [" + ammo.Description + "] type [" + ammo.TypeId + "] [" + totalQuantity + "] Minimum amount [" + minQty +
                                    "] We're going to buy ammo.");
                                buy = true;
                                break;
                            }
                        }

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    {
                        XElement xmlFilamentToStock = XElement.Parse("<itemToKeepInStock description = \"" + AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName + "\" typeId = \"" + AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentTypeId + "\"  quantity = \"" + AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentsToStock + "\" />");
                        InventoryItem filamentToStock = new InventoryItem(xmlFilamentToStock);
                        Settings.Instance.ListOfItemsToKeepInStock.Add(filamentToStock);
                    }

                    if (Settings.Instance.ListOfItemsToKeepInStock != null && Settings.Instance.ListOfItemsToKeepInStock.Count > 0)
                        foreach (InventoryItem itemToStock in Settings.Instance.ListOfItemsToKeepInStock)
                        {
                            int totalQuantity = 0;
                            if (ESCache.Instance.AmmoHangar.Items != null && ESCache.Instance.AmmoHangar.Items.Count > 0 && ESCache.Instance.AmmoHangar.Items.Any(i => i.TypeId == itemToStock.TypeId))
                                totalQuantity = ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == itemToStock.TypeId).Sum(i => i.Stacksize);

                            int minQty = itemToStock.Quantity;
                            if (totalQuantity < minQty)
                            {
                                Log("Total itemToStock amount in hangar [" + itemToStock.Name + "] type [" + itemToStock.TypeId + "] [" + totalQuantity + "] Minimum amount [" + minQty +
                                    "] We're going to buy ItemsToKeepInStock.");
                                buy = true;
                                break;
                            }
                        }

                    if (Drones.UseDrones)
                    {
                        List<int> droneTypeIds = new List<int>
                        {
                            Drones.DroneTypeID
                        };

                        /**
                        foreach (var factionFtting in MissionSettings.ListofFactionFittings)
                        {
                            if (factionFtting.DroneTypeID == null)
                                continue;
                            if (factionFtting.DroneTypeID <= 0)
                                continue;
                            if (!droneTypeIds.Contains((int) factionFtting.DroneTypeID))
                                droneTypeIds.Add((int) factionFtting.DroneTypeID);
                        }

                        foreach (var missionFitting in MissionSettings.ListOfMissionFittings)
                        {
                            if (missionFitting.DroneTypeID == null)
                                continue;
                            if (missionFitting.DroneTypeID <= 0)
                                continue;
                            if (!droneTypeIds.Contains((int) missionFitting.DroneTypeID))
                                droneTypeIds.Add((int) missionFitting.DroneTypeID);
                        }
                        **/

                        foreach (int droneTypeId in droneTypeIds)
                        {
                            int totalQuantityDrones = ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == droneTypeId).Sum(i => i.Stacksize);
                            if (totalQuantityDrones < Drones.MinimumNumberOfDronesBeforeWeGoBuyMore)
                            {
                                Log("Total drone amount in hangar [" + totalQuantityDrones + "]  Minimum amount [" + Drones.MinimumNumberOfDronesBeforeWeGoBuyMore +
                                    "] We're going to buy drones of type [" + droneTypeId + "]");
                                buy = true;
                            }
                        }
                    }

                    Log("LastAmmoBuy was on [" + ESCache.Instance.EveAccount.LastAmmoBuy + "]");

                    if (buy)
                    {
                        ChangeBuyItemsControllerState(BuyItemsState.ActivateTransportShip, false);
                    }
                    else
                    {
                        Log("There is still enough ammo / drones / ItemsToStock available in the itemhangar. Changing state to done.");
                        ChangeBuyItemsControllerState(BuyItemsState.Done, false);
                    }

                    break;

                case BuyItemsState.ActivateTransportShip:

                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastAmmoBuy), DateTime.UtcNow);

                    if (!ESCache.Instance.InStation)
                        return;

                    if (ESCache.Instance.AmmoHangar == null)
                        return;

                    if (ESCache.Instance.ShipHangar == null)
                        return;

                    if (ESCache.Instance.ActiveShip.IsHaulingShip)
                    {
                        ChangeBuyItemsControllerState(BuyItemsState.CreateBuyList, false);
                        return;
                    }

                    if (!ESCache.Instance.ActiveShip.IsHaulingShip)
                    {
                        List<DirectItem> ships = ESCache.Instance.ShipHangar.ValidShipsToUse;
                        foreach (DirectItem ship in ships.Where(ship => ship.GivenName == Settings.Instance.TransportShipName))
                        {
                            Log("Making [" + ship.GivenName + "] active. Groupname [" + ship.GroupName + "] TypeName [" + ship.TypeName + "]");
                            ship.ActivateShip();
                            nextAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                        }

                        ChangeBuyItemsControllerState(BuyItemsState.CreateBuyList, false);
                    }
                    break;

                case BuyItemsState.CreateBuyList:

                    if (!ESCache.Instance.InStation)
                        return;

                    if (ESCache.Instance.AmmoHangar == null)
                        return;

                    if (ESCache.Instance.CurrentShipsCargo == null)
                        return;

                    if (ESCache.Instance.CurrentShipsCargo.UsedCapacity == null)
                        return;

                    //var invtypes = Cache.Instance.DirectEve.InvTypes;

                    double freeCargo = ESCache.Instance.CurrentShipsCargo.Capacity - (double)ESCache.Instance.CurrentShipsCargo.UsedCapacity;

                    if (ESCache.Instance.CurrentShipsCargo.Capacity == 0)
                    {
                        Log("if(Cache.Instance.CurrentShipsCargo.Capacity == 0)");
                        nextAction = DateTime.UtcNow.AddSeconds(5);
                        return;
                    }

                    Log("Current [" + ESCache.Instance.ActiveShip.GivenName + "] Cargo [" + Math.Round(ESCache.Instance.CurrentShipsCargo.Capacity, 0) +
                        "] Used Capacity [" +
                        Math.Round((double)ESCache.Instance.CurrentShipsCargo.UsedCapacity, 0) + "] Free Capacity [" + Math.Round(freeCargo, 0) + "]");

                    if (Drones.UseDrones)
                    {
                        List<int> droneTypeIds = new List<int>
                        {
                            Drones.DroneTypeID
                        };
                        /**
                        foreach (var factionFtting in MissionSettings.ListofFactionFittings)
                        {
                            if (factionFtting.DroneTypeID == null)
                                continue;
                            if (factionFtting.DroneTypeID <= 0)
                                continue;
                            if (!droneTypeIds.Contains((int) factionFtting.DroneTypeID))
                                droneTypeIds.Add((int) factionFtting.DroneTypeID);
                        }
                        **/
                        /**
                        foreach (var missionFitting in MissionSettings.ListOfMissionFittings)
                        {
                            if (missionFitting.DroneTypeID == null)
                                continue;
                            if (missionFitting.DroneTypeID <= 0)
                                continue;
                            if (!droneTypeIds.Contains((int) missionFitting.DroneTypeID))
                                droneTypeIds.Add((int) missionFitting.DroneTypeID);
                        }
                        **/
                        foreach (int droneTypeId in droneTypeIds.Distinct())
                        {
                            int totalQuantityDrones = ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == droneTypeId).Sum(i => i.Stacksize);

                            if (totalQuantityDrones < Drones.MinimumNumberOfDronesBeforeWeGoBuyMore && Drones.BuyAmmoDroneAmmount > 0)
                            {
                                Log("Total drone amount in hangar [" + totalQuantityDrones + "]  Minimum amount [" + Drones.MinimumNumberOfDronesBeforeWeGoBuyMore + "]");
                                buyList.AddOrUpdate(droneTypeId, Drones.BuyAmmoDroneAmmount);
                            }
                        }
                    }

                    foreach (KeyValuePair<int, int> buyListKeyValuePair in buyList.ToList())
                    {
                        // create a copy to allow removing elements

                        if (ESCache.Instance.DirectEve.GetInvType(buyListKeyValuePair.Key) == null)
                        {
                            Log("TypeId [" + buyListKeyValuePair.Key + "] does not exist in eve invtypes. THIS SHOULD NOT HAPPEN AT ALL.");
                            buyList.Remove(buyListKeyValuePair.Key);
                            continue;
                        }

                        DirectInvType droneInvType = ESCache.Instance.DirectEve.GetInvType(buyListKeyValuePair.Key);
                        double cargoBefore = freeCargo;
                        freeCargo -= buyListKeyValuePair.Value * droneInvType.Volume;
                        Log("Drones, Reducing freeCargo from [" + cargoBefore + "] to [" + freeCargo + "]");
                    }

                    freeCargo *= 0.995; // leave 0.5% free space
                    bool majorBuySlotUsed = false;

                    // here we could also run through our mission xml folder and seach for the bring, trybring items and add them here ( if we dont have them in our hangar )
                    if (DirectUIModule.DefinedAmmoTypes.Count > 0)
                    {
                        Log("Start adding any needed ammo to the buylist:");

                        //if (Combat.DefinedAmmoTypes.Select(a => a.DamageType).Distinct().Count() != 4)
                        //{
                        //    Log("ERROR: if (Combat.DefinedAmmoTypes.Select(a => a.DamageType).Distinct().Count() != 4)");
                        //    ChangeBuyAmmoControllerState(BuyAmmoState.Error, true);
                        //    return;
                        //}

                        foreach (AmmoType ammo in DirectUIModule.DefinedAmmoTypes)
                            try
                            {
                                int totalQuantity = 0;
                                if (ESCache.Instance.AmmoHangar.Items != null && ESCache.Instance.AmmoHangar.Items.Count > 0 && ESCache.Instance.AmmoHangar.Items.Any(i => i.TypeId == ammo.TypeId))
                                    totalQuantity = ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Stacksize);

                                int minQty = ammo.Quantity * MinAmmoMultiplier;
                                int maxQty = ammo.Quantity * maxAmmoMultiplier;

                                if (ESCache.Instance.DirectEve.GetInvType(ammo.TypeId) == null)
                                {
                                    Log("TypeId [" + ammo.TypeId + "] does not exist in eve invtypes. THIS SHOULD NOT HAPPEN AT ALL.");
                                    continue;
                                }

                                DirectInvType ammoInvType = ESCache.Instance.DirectEve.GetInvType(ammo.TypeId);
                                if (totalQuantity < minQty && !majorBuySlotUsed)
                                {
                                    majorBuySlotUsed = true;
                                    int ammoBuyAmount = Math.Min((int)(freeCargo * 0.35 / ammoInvType.Volume), maxQty); // 35% (maximum) of the volume for the first missing ammo

                                    int amountBefore = 0;
                                    Log("BuyList:  [" + ammo.Description + "] TypeId [" + ammo.TypeId + "] Quantity [" + ammoBuyAmount + "]");
                                    if (buyList.TryGetValue(ammo.TypeId, out amountBefore)) buyList.AddOrUpdate(ammo.TypeId, ammoBuyAmount + amountBefore);
                                    else buyList.AddOrUpdate(ammo.TypeId, ammoBuyAmount);
                                }
                                else
                                {
                                    if (totalQuantity <= maxQty)
                                    {
                                        int ammoBuyAmount = Math.Min((int)(freeCargo * (0.55 / (DirectUIModule.DefinedAmmoTypes.Count - 1)) / ammoInvType.Volume), maxQty); // 55% (maximum) for the rest

                                        int amountBefore = 0;
                                        Log("BuyList:  [" + ammo.Description + "] TypeId [" + ammo.TypeId + "] Quantity [" + ammoBuyAmount + "]");
                                        if (buyList.TryGetValue(ammo.TypeId, out amountBefore)) buyList.AddOrUpdate(ammo.TypeId, ammoBuyAmount + amountBefore);
                                        else buyList.AddOrUpdate(ammo.TypeId, ammoBuyAmount);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log("ERROR: foreach(var ammo in Combat.DefinedAmmoTypes)");
                                Log("Exception [" + ex + "]");
                                ChangeBuyItemsControllerState(BuyItemsState.Error, true);
                                return;
                            }

                        Log("Done adding ammo to the buylist.");
                    }

                    if (Settings.Instance.ListOfItemsToKeepInStock.Count > 0)
                    {
                        Log("BuyList: Start adding any needed ItemsToKeepInStock to the buylist:");

                        foreach (InventoryItem itemToKeepInStock in Settings.Instance.ListOfItemsToKeepInStock)
                            try
                            {
                                int totalQuantity = 0;
                                if (ESCache.Instance.AmmoHangar.Items != null && ESCache.Instance.AmmoHangar.Items.Count > 0 && ESCache.Instance.AmmoHangar.Items.Any(i => i.TypeId == itemToKeepInStock.TypeId))
                                    totalQuantity = ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == itemToKeepInStock.TypeId).Sum(i => i.Stacksize);

                                int minQty = itemToKeepInStock.Quantity;
                                int maxQty = (int)Math.Round(itemToKeepInStock.Quantity * 1.2, 0);

                                if (ESCache.Instance.DirectEve.GetInvType(itemToKeepInStock.TypeId) == null)
                                {
                                    Log("TypeId [" + itemToKeepInStock.TypeId + "] does not exist in eve invtypes. THIS SHOULD NOT HAPPEN AT ALL.");
                                    continue;
                                }

                                DirectInvType ammoInvType = ESCache.Instance.DirectEve.GetInvType(itemToKeepInStock.TypeId);
                                if (totalQuantity < minQty)
                                {
                                    int itemBuyAmount = (int)Math.Round(Math.Min(freeCargo * 0.05 / ammoInvType.Volume, maxQty), 0); // 5% (maximum) of the volume for the first missing item

                                    int amountBefore = 0;
                                    Log("BuyList:  [" + itemToKeepInStock.Name + "] TypeId [" + itemToKeepInStock.TypeId + "] Quantity [" + itemBuyAmount + "]");
                                    if (buyList.TryGetValue(itemToKeepInStock.TypeId, out amountBefore)) buyList.AddOrUpdate(itemToKeepInStock.TypeId, itemBuyAmount + amountBefore);
                                    else buyList.AddOrUpdate(itemToKeepInStock.TypeId, itemBuyAmount);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                                ChangeBuyItemsControllerState(BuyItemsState.Error, true);
                                return;
                            }

                        Log("BuyList: Done adding ItemsToKeepInStock to the buylist.");
                    }

                    int z = 0;
                    double totalVolumeBuyList = 0;
                    const double totalAverageValue = 0;
                    if (buyList != null && buyList.Count > 0)
                    {
                        Log("BuyList: Contains [" + buyList.Count + "] items");
                        foreach (KeyValuePair<int, int> entry in buyList)
                        {
                            DirectInvType buyInvType = ESCache.Instance.DirectEve.GetInvType(entry.Key);
                            double buyTotalVolume = buyInvType.Volume * entry.Value;
                            z++;

                            Log("[" + z + "][" + buyInvType.TypeName + "] typeID [" + entry.Key + "] amount [" + entry.Value + "] m3 [" + buyTotalVolume + "]");
                            totalVolumeBuyList += buyTotalVolume;
                            /**
                            double tempAveragePrice = 0;
                            try
                            {
                                DirectItem buyTempDirectItem = new DirectItem(ESCache.Instance.DirectEve);
                                buyTempDirectItem.TypeId = buyInvType.TypeId;
                                tempAveragePrice = buyTempDirectItem.AveragePrice();
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }

                            if (tempAveragePrice != 0)
                            {
                                Log("[" + z + "][" + buyInvType.TypeName + "] typeID [" + entry.Key + "] averagePrice [" + tempAveragePrice + "]");
                            }
                            else
                            {
                                tempAveragePrice = buyInvType.BasePrice * 3;
                                Log("[" + z + "][" + buyInvType.TypeName + "] typeID [" + entry.Key + "] averagePrice using basePrice [" + tempAveragePrice + "]");
                            }

                            totalAverageValue = totalAverageValue + tempAveragePrice * entry.Value;
                            **/
                        }

                        double currentShipFreeCargo = ESCache.Instance.CurrentShipsCargo.Capacity - (double)ESCache.Instance.CurrentShipsCargo.UsedCapacity;
                        Log("CurrentShipFreeCargo [" + Math.Round(currentShipFreeCargo, 0) + " m3] BuyListTotalVolume [" + Math.Round(totalVolumeBuyList, 0) + " m3] ISK Value [" + Math.Round(totalAverageValue, 0) + "]");

                        if (currentShipFreeCargo < totalVolumeBuyList)
                        {
                            Log("if(currentShipFreeCargo < totalVolumeBuyList)");
                            ChangeBuyItemsControllerState(BuyItemsState.Error, true);
                            return;
                        }

                        //if (ESCache.Instance.MyWalletBalance < totalAverageValue * 3)
                        //{
                        //    Log("if (ESCache.Instance.MyWalletBalance < totalAverageValue * 2)");
                        //    ChangeBuyAmmoControllerState(BuyAmmoState.Error, true);
                        //    return;
                        //}

                        ChangeBuyItemsControllerState(BuyItemsState.TravelToDestinationStation, true);

                        foreach (KeyValuePair<int, int> entry in buyList)
                            _moveToCargoList.Add(entry.Key, entry.Value);

                        travelerDestination = new StationDestination(Settings.Instance.BuyAmmoStationId);
                        return;
                    }

                    Log("BuyList was empty: no need to travel to buy stuff when we have nothing on our list.");
                    ChangeBuyItemsControllerState(BuyItemsState.Done, false);
                    break;

                case BuyItemsState.TravelToDestinationStation:

                    if (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                        return;

                    if (Traveler.Destination != travelerDestination)
                        Traveler.Destination = travelerDestination;

                    Traveler.ProcessState();

                    if (State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        Log("Arrived at destination");
                        ChangeBuyItemsControllerState(BuyItemsState.BuyAmmo, true);
                        orderIterations = 0;
                        Traveler.Destination = null;
                        return;
                    }

                    if (State.CurrentTravelerState == TravelerState.Error)
                    {
                        if (Traveler.Destination != null)
                            Log("Stopped traveling, traveller threw an error...");

                        Traveler.Destination = null;
                        ChangeBuyItemsControllerState(BuyItemsState.Error, true);
                    }

                    break;

                case BuyItemsState.BuyAmmo:

                    if (!ESCache.Instance.InStation)
                        return;

                    if (ESCache.Instance.AmmoHangar == null)
                        return;

                    if (ESCache.Instance.CurrentShipsCargo == null)
                        return;

                    // Is there a market window?
                    DirectMarketWindow marketWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                    if (buyList.Count == 0)
                    {
                        // Close the market window if there is one
                        if (marketWindow != null)
                            marketWindow.Close();

                        Log("Finished buying changing state to MoveItemsToCargo");
                        ChangeBuyItemsControllerState(BuyItemsState.MoveItemsToCargo, false);
                        return;
                    }

                    KeyValuePair<int, int> currentBuyListItem = buyList.FirstOrDefault();
                    int buyItemTypeId = currentBuyListItem.Key;
                    int buyItemQuantity = currentBuyListItem.Value;

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                        return;

                    if (buyItemTypeId == 0)
                    {
                        Log("BuyAmmo: ERROR: buyItemTypeId == 0");
                        ChangeBuyItemsControllerState(BuyItemsState.Error);
                    }

                    // Do we have the ammo we need in the AmmoHangar?
                    if (ESCache.Instance.AmmoHangar.Items != null && ESCache.Instance.AmmoHangar.Items.Count > 0)
                        if (ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == buyItemTypeId).Sum(i => i.Stacksize) >= buyItemQuantity)
                        {
                            DirectItem ammoItemInHangar = ESCache.Instance.AmmoHangar.Items.Find(i => i.TypeId == buyItemTypeId);
                            if (ammoItemInHangar != null)
                                Log("We have [" +
                                    ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == buyItemTypeId)
                                        .Sum(i => i.Stacksize)
                                        .ToString(CultureInfo.InvariantCulture) +
                                    "] " + ammoItemInHangar.TypeName + " in the item hangar.");

                            buyList.Remove(buyItemTypeId);
                            return;
                        }

                    // We do not have enough ammo, open the market window
                    if (marketWindow == null)
                    {
                        if (!ESCache.Instance.OkToInteractWithEveNow)
                        {
                            if (DebugConfig.DebugInteractWithEve) Log("BuyPlexController: MarketWindow: !OkToInteractWithEveNow");
                            return;
                        }

                        nextAction = DateTime.UtcNow.AddSeconds(10);
                        Log("Opening market window");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                        Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
                        return;
                    }

                    // Wait for the window to become ready
                    if (!marketWindow.IsReady)
                        return;

                    // Are we currently viewing the correct ammo orders?
                    if (marketWindow.DetailTypeId != buyItemTypeId)
                    {
                        // No, load the ammo orders
                        Log("Loading market orders window for TypeID [" + buyItemTypeId + "]");
                        if (marketWindow.LoadTypeId(buyItemTypeId))
                        {
                            nextAction = DateTime.UtcNow.AddSeconds(10);
                            return;
                        }

                        return;
                    }

                    // Get the median sell price
                    DirectInvType type = ESCache.Instance.DirectEve.GetInvType(buyItemTypeId);
                    DirectInvType currentBuyItemDirectItem = type;

                    double maxPrice = 0;

                    if (currentBuyItemDirectItem != null)
                    {
                        double avgPrice = currentBuyItemDirectItem.AveragePrice();
                        double basePrice = currentBuyItemDirectItem.BasePrice / currentBuyItemDirectItem.PortionSize;

                        Log("Item [" + currentBuyItemDirectItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice +
                            "] groupID [" +
                            currentBuyItemDirectItem.GroupId + "] groupName [" + currentBuyItemDirectItem.GroupId + "] typeId [" + currentBuyItemDirectItem.TypeId + "]");

                        if (avgPrice != 0)
                        {
                            maxPrice = avgPrice * maxAvgPriceMultiplier; // 3 times the avg price
                        }
                        else
                        {
                            if (basePrice != 0)
                                maxPrice = basePrice * maxBasePriceMultiplier; // 6 times the base price
                            else
                                maxPrice = 1000;
                        }

                        Log("Item [" + currentBuyItemDirectItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice + "]");

                        // Are there any orders with an reasonable price?
                        IEnumerable<DirectOrder> orders;
                        if (maxPrice == 0)
                        {
                            Log("if(maxPrice == 0)");
                            orders =
                                marketWindow.SellOrders.Where(o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.TypeId == buyItemTypeId).ToList();
                        }
                        else
                        {
                            Log("if(maxPrice != 0) max price [" + maxPrice + "]");
                            orders =
                                marketWindow.SellOrders.Where(
                                        o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.Price < maxPrice && o.TypeId == buyItemTypeId)
                                    .ToList();
                            if (!orders.Any())
                            {
                                Log("0 orders found with max price set to [" + maxPrice + "]");
                                if (currentBuyItemDirectItem.CategoryId == (int)CategoryID.Charge && 500 > maxPrice)
                                {
                                    Log("Changing maxPrice of ammo to [" + maxPrice + "]");
                                    maxPrice = 500;
                                }
                                else
                                {
                                    Log("Changing maxPrice to [" + maxPrice + "]");
                                    maxPrice = 2000000;
                                }

                                orders =
                                    marketWindow.SellOrders.Where(
                                            o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.Price < maxPrice && o.TypeId == buyItemTypeId)
                                        .ToList();
                            }
                        }

                        orderIterations++;

                        if (!orders.Any() && orderIterations < 5)
                        {
                            nextAction = DateTime.UtcNow.AddSeconds(5);
                            return;
                        }

                        // Is there any order left?
                        if (!orders.Any())
                        {
                            Log("No reasonably priced [" + currentBuyItemDirectItem.TypeName + "] available! Removing it from the buyList");
                            buyList.Remove(buyItemTypeId);
                            nextAction = DateTime.UtcNow.AddSeconds(3);
                            return;
                        }

                        // How many more of the buyItem do we still need?
                        int neededQuantity = buyItemQuantity - ESCache.Instance.AmmoHangar.Items.Where(i => i.TypeId == buyItemTypeId).Sum(i => i.Stacksize);
                        if (neededQuantity > 0)
                        {
                            // Get the first order
                            DirectOrder order = orders.OrderBy(o => o.Price).FirstOrDefault();
                            if (order != null)
                            {
                                // Calculate how many more of the buyItem we still need
                                int remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                                long orderPrice = (long)(remaining * order.Price);

                                if (ESCache.Instance.DirectEve.Me.Wealth != null && orderPrice < ESCache.Instance.DirectEve.Me.Wealth)
                                {
                                    Log("Buying [" + remaining + "] ammo price [" + order.Price + "]");
                                    order.Buy(remaining, DirectOrderRange.Station);

                                    // Wait for the order to go through
                                    nextAction = DateTime.UtcNow.AddSeconds(10);
                                }
                                else
                                {
                                    Log("ERROR: We don't have enough ISK on our wallet to finish that transaction.");
                                    ChangeBuyItemsControllerState(BuyItemsState.Error, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        Log("ERROR: currentBuyItemDirectItem == null! buyItemTypeId [" + buyItemTypeId + "]");
                        ChangeBuyItemsControllerState(BuyItemsState.Error, true);
                    }

                    break;

                case BuyItemsState.MoveItemsToCargo:
                    if (!LoadItemsToHaul.MoveHangarItems(ESCache.Instance.ItemHangar, ESCache.Instance.CurrentShipsCargo, _moveToCargoList)) return;
                    ChangeBuyItemsControllerState(BuyItemsState.Done, false);
                    break;

                case BuyItemsState.Done:

                    if (ESCache.Instance.DirectEve.Session.StationId != null && ESCache.Instance.DirectEve.Session.StationId > 0 &&
                        ESCache.Instance.DirectEve.Session.StationId == Settings.Instance.BuyAmmoStationId)
                    {
                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                            CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.DelayedGotoBase, true, null);

                        Arm.ChangeArmState(ArmState.Idle, true, null);
                    }

                    ControllerManager.Instance.RemoveController(typeof(BuyItemsController));

                    //
                    //                    Logging.Log("Remaining controllers after removal: ");
                    //			        foreach (var k  in Program.QuestorControllerManagerInstance.ControllerList)
                    //			        {
                    //			          Logging.Log(k.Key.GetType().ToString());
                    //			        }

                    //					Logging.Log("BuyAmmo", "State iterations statistics: []");
                    //					foreach(var kV in stateIterations)
                    //					{
                    //						Logging.Log("BuyAmmo", "State [BuyAmmoState." + kV.Key.ToString() + "] iterations [" + kV.Value + "]" );
                    //					}

                    break;

                case BuyItemsState.Error:
                    CurrentBuyItemsState = BuyItemsState.DisabledForThisSession;
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                        CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.DelayedGotoBase, true, null);
                    Arm.ChangeArmState(ArmState.Idle, true, null);
                    Log("ERROR. BuyAmmo should stay disabled while this session is still active.");
                    ControllerManager.Instance.RemoveController(typeof(BuyItemsController));
                    break;

                case BuyItemsState.DisabledForThisSession:
                    Log("ERROR. BuyAmmo has been disabled during this session.");
                    ControllerManager.Instance.RemoveController(typeof(BuyItemsController));
                    break;

                default:
                    throw new Exception("Invalid value for BuyAmmoState");
            }
        }

        public override void DoWork()
        {
            try
            {
                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (Time.Instance.LastInWarp.AddSeconds(3) > DateTime.UtcNow)
                        return;

                    if (ESCache.Instance.Stargates.Count > 0 && Time.Instance.LastInWarp.AddSeconds(10) > DateTime.UtcNow)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < 10000)
                        {
                            if (DebugConfig.DebugCleanup) Log("CheckModalWindows: We are within 10k of a stargate, do nothing while we wait to jump.");
                            return;
                        }
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