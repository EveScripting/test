extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace EVESharpCore.Controllers
{
    public class ItemTransportController : BaseController
    {
        #region Enums

        private enum TransportState
        {
            Idle,
            TravelingToPickupStation,
            PickingUpItems,
            TravelingToDeliveryStation,
            DeliveringItems,
            WaitOnErrorCooldown,  // For error cooldown
            Done,
            Error
        }

        #endregion

        #region Settings Properties

        private static string _pickupStationBookmarkName;
        private static string _deliveryStationBookmarkName;
        private static List<int> _typeIDsToTransport = new List<int>();
        private static List<int> _groupIDsToTransport = new List<int>();
        private static List<int> _categoryIDsToTransport = new List<int>();

        private static List<long> _tempExcludedTypeIDs = new List<long>();
        private static List<long> _excludedTypeIDs = new List<long>();
        private static List<int> _excludedGroupIDs = new List<int>();
        private static List<int> _excludedCategoryIDs = new List<int>();

        private static DirectBookmark _pickupStationBookmark = null;
        private static DirectBookmark pickupStationBookmark
        {
            get
            {
                if (string.IsNullOrEmpty(_pickupStationBookmarkName))
                {
                    Log("_pickupStationBookmarkName is Empty");
                    return null;
                }

                if (ESCache.Instance.CachedBookmarks.Any())
                {
                    if (_pickupStationBookmark != null)
                        return _pickupStationBookmark;

                    var _pickupStationBookmarks = ESCache.Instance.BookmarksThatContain(_pickupStationBookmarkName);
                    if (_pickupStationBookmarks.Any())
                    {
                        _pickupStationBookmark = _pickupStationBookmarks.FirstOrDefault();//Verify this is a station!?
                        return _pickupStationBookmark;
                    }

                    Log("pickupStationBookmark == null ");
                    return null;
                }

                Log("No bookmarks?!");
                return null;
            }
        }

        private static DirectBookmark _deliveryStationBookmark = null;
        private static DirectBookmark deliveryStationBookmark
        {
            get
            {
                if (ESCache.Instance.CachedBookmarks.Any())
                {
                    if (_deliveryStationBookmark != null)
                        return _deliveryStationBookmark;

                    var _deliveryStationBookmarks = ESCache.Instance.BookmarksThatContain(_deliveryStationBookmarkName);
                    if (_deliveryStationBookmarks.Any())
                    {
                        _deliveryStationBookmark = _deliveryStationBookmarks.FirstOrDefault();//Verify this is a station!?
                        return _deliveryStationBookmark;
                    }

                    Log("_deliveryStationBookmark == null");
                    return null;
                }

                Log("No Bookmarks?!");
                return null;
            }
        }

        public static void ClearPerCargoHoldTypeCache()
        {
            _tempExcludedTypeIDs.Clear();
        }


        private static void StartTraveler()
        {
            Log("StartTraveler: start");
            Traveler.Destination = null;
            State.CurrentTravelerState = TravelerState.Idle;
            Log("StartTraveler: done");
            return;
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                _pickupStationBookmarkName =
                (string)CharacterSettingsXml.Element("TransportPickupStationBookmarkName") ??
                (string)CommonSettingsXml.Element("TransportPickupStationBookmarkName") ?? "PickupStation";
            Log($"ItemTransportController: Pickup Station Bookmark Name: {_pickupStationBookmarkName}");

            _deliveryStationBookmarkName =
                (string)CharacterSettingsXml.Element("TransportDeliveryStationBookmarkName") ??
                (string)CommonSettingsXml.Element("TransportDeliveryStationBookmarkName") ?? "DeliveryStation";
            Log($"ItemTransportController: Delivery Station Bookmark Name: {_deliveryStationBookmarkName}");

            _typeIDsToTransport = CharacterSettingsXml.Element("TypeIDsToTransport")?
                .Elements("TypeIDToTransport")
                .Where(x => !x.IsEmpty)
                .Select(x => (int)x)
                .ToList() ?? new List<int>();

            if (_typeIDsToTransport.Any())
            {
                Log($"ItemTransportController: _typeIDsToTransport contains [" + _typeIDsToTransport.Count() + "] TypeIDs");
                foreach (var _typeIDToTransport in _typeIDsToTransport)
                {
                    Log($"ItemTransportController: TypeID: [" + _typeIDToTransport + "]");
                }
            }

            _groupIDsToTransport = CharacterSettingsXml.Element("GroupIDsToTransport")?
                .Elements("GroupIDToTransport")
                .Where(x => !x.IsEmpty)
                .Select(x => (int)x)
                .ToList() ?? new List<int>();

            if (_groupIDsToTransport.Any())
            {
                Log($"ItemTransportController: _groupIDsToTransport contains [" + _groupIDsToTransport.Count() + "] GroupIDs");
                foreach (var _groupIDToTransport in _groupIDsToTransport)
                {
                    Log($"ItemTransportController: GroupID: [" + _groupIDToTransport + "]");
                }
            }

            _categoryIDsToTransport = CharacterSettingsXml.Element("CategoryIDsToTransport")?
                .Elements("CategoryIDToTransport")
                .Where(x => !x.IsEmpty)
                .Select(x => (int)x)
                .ToList() ?? new List<int>();

            if (_categoryIDsToTransport.Any())
            {
                Log($"ItemTransportController: _categoryIDsToTransport contains [" + _categoryIDsToTransport.Count() + "] CategoryIDs");
                foreach (var _categoryIDToTransport in _categoryIDsToTransport)
                {
                    Log($"ItemTransportController: CategoryID: [" + _categoryIDToTransport + "]");
                }
            }


            _excludedTypeIDs = CharacterSettingsXml.Element("ExcludedTypeIDs")?
                .Elements("ExcludedTypeID")
                .Where(x => !x.IsEmpty)
                .Select(x => (long)x)
                .ToList() ?? new List<long>();
            if (_excludedTypeIDs.Any())
            {
                Log($"ItemTransportController: _excludedTypeIDs contains [" + _excludedTypeIDs.Count() + "] TypeIDs");
                foreach (var _excludedTypeID in _excludedTypeIDs)
                {
                    Log($"ItemTransportController: TypeID: [" + _excludedTypeID + "]");
                }
            }

            _excludedGroupIDs = CharacterSettingsXml.Element("ExcludedGroupIDs")?
                .Elements("ExcludedGroupID")
                .Where(x => !x.IsEmpty)
                .Select(x => (int)x)
                .ToList() ?? new List<int>();
            if (_excludedGroupIDs.Any())
            {
                Log($"ItemTransportController: _excludedGroupIDs contains [" + _excludedGroupIDs.Count() + "] GroupIDs");
                foreach (var _excludedGroupID in _excludedGroupIDs)
                {
                    Log($"ItemTransportController: GroupID: [" + _excludedGroupID + "]");
                }
            }

            _excludedCategoryIDs = CharacterSettingsXml.Element("ExcludedCategoryIDs")?
                .Elements("ExcludedCategoryID")
                .Where(x => !x.IsEmpty)
                .Select(x => (int)x)
                .ToList() ?? new List<int>();
            if (_excludedCategoryIDs.Any())
            {
                Log($"ItemTransportController: _excludedCategoryIDs contains [" + _excludedCategoryIDs.Count() + "] CategoryIDs");
                foreach (var _excludedCategoryID in _excludedCategoryIDs)
                {
                    Log($"ItemTransportController: CategoryID: [" + _excludedCategoryID + "]");
                }
            }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        #endregion

        #region Fields

        private TransportState _currentState = TransportState.Idle;
        private TransportState _previousState = TransportState.Idle;
        private TravelerDestination _destination;

        private bool _allowPartialLoads = true;

        // Error handling
        private int _errorCounter = 0;
        private const int MaxErrorCount = 3;
        private DateTime _errorCooldownUntil = DateTime.UtcNow;
        private TimeSpan _errorCooldownDuration = TimeSpan.FromSeconds(30);

        #endregion

        #region Overrides

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {
            // No broadcast messages handled here
        }

        public void EveryPulse()
        {
            if (ESCache.Instance.InStation)
            {
                //if (_currentState == WhateverWeThingTheWrongStateIs)
                //_currentState = TransportState.Blah
                //

            }

            if (ESCache.Instance.InSpace)
            {
                //if (_currentState == WhateverWeThingTheWrongStateIs)
                //_currentState = TransportState.Blah
                //

            }
        }

        public override void DoWork()
        {
            try
            {
                EveryPulse();
                if (DebugConfig.DebugItemTransportController) Log("_currentState [" + _currentState + "]");

                switch (_currentState)
                {
                    case TransportState.Idle:
                        StateIdle();
                        break;

                    case TransportState.TravelingToPickupStation:
                        StateTravelToPickupStation();
                        break;

                    case TransportState.PickingUpItems:
                        StatePickupItems();
                        break;

                    case TransportState.TravelingToDeliveryStation:
                        StateTravelToDeliveryStation();
                        break;

                    case TransportState.DeliveringItems:
                        StateDeliverItems();
                        break;

                    case TransportState.WaitOnErrorCooldown:
                        StateWaitOnErrorCooldown();
                        break;

                    case TransportState.Done:
                        StateDone();
                        break;

                    case TransportState.Error:
                        StateError();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        #endregion

        #region States

        private void StateIdle()
        {
            if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: Idle");
            StartTraveler();
            _currentState = TransportState.TravelingToPickupStation;
        }

        private void StateTravelToPickupStation()
        {
            if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: TravelingToPickupStation");

            if (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity?.HasInitiatedWarp == true)
                return;

            if (pickupStationBookmark == null)
            {
                Log("pickupStationBookmark == null");
                Log("_pickupStationBookmarkName [" + _pickupStationBookmarkName + "]");
                return;
            }

            if (_destination == null ||
                (_destination is BookmarkDestination sDest && sDest.BookmarkId != pickupStationBookmark.BookmarkId))
            {
                Log("Setting Destination to [" + pickupStationBookmark.Description + "]");
                _destination = new BookmarkDestination(pickupStationBookmark);
                Traveler.Destination = _destination;
                return;
            }

            Traveler.ProcessState();

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Log("Arrived at Pickup Station");
                Traveler.Destination = null;
                _currentState = TransportState.PickingUpItems;
                return;
            }
            //else if (State.CurrentTravelerState == TravelerState.Error)
            //{
            //    TriggerErrorCooldown("Error while traveling to pickup station.");
            //}
        }

        private bool? PickupItemsFromItemHangarToAmmoHold()
        {
            if (ESCache.Instance.ActiveShip.HasAmmoHold)
            {
                return Pickup(ESCache.Instance.ItemHangar, ESCache.Instance.CurrentShipsAmmoHold, "ItemHangar", "CurrentShipsAmmoHold");
            }

            return true;
        }

        private bool? PickupItemsFromItemHangarToMineralHold()
        {
            if (ESCache.Instance.ActiveShip.HasMineralHold)
            {
                return Pickup(ESCache.Instance.ItemHangar, ESCache.Instance.CurrentShipsMineralHold, "ItemHangar", "CurrentShipsMineralHold");
            }

            return true;
        }

        private bool? PickupItemsFromItemHangarToGeneralMiningHold()
        {
            if (ESCache.Instance.ActiveShip.HasGeneralMiningHold)
            {
                return Pickup(ESCache.Instance.ItemHangar, ESCache.Instance.CurrentShipsGeneralMiningHold, "ItemHangar", "CurrentShipsGeneralMiningHold");
            }

            return true;
        }

        private bool? PickupItemsFromItemHangarToOreHold()
        {
            if (ESCache.Instance.ActiveShip.HasOreHold)
            {
                //return Pickup(ESCache.Instance.ItemHangar, ESCache.Instance.CurrentShipsAmmoHold, "ItemHangar", "CurrentShipsAmmoHold");
            }

            return true;
        }

        private bool? PickupItemsFromItemHangarToCargoHold()
        {
            return Pickup(ESCache.Instance.ItemHangar, ESCache.Instance.CurrentShipsCargo, "ItemHangar", "CurrentShipsCargo");
        }

        private bool? PickupItemsFromItemHangarToFleetHangar()
        {
            if (DebugConfig.DebugItemTransportController) Log("Checking if ship has FleetHangar...");
            if (ESCache.Instance.ActiveShip.HasFleetHangar)
            {
                Log("Shiptype: HasFleetHangar!");
                return Pickup(ESCache.Instance.ItemHangar, ESCache.Instance.CurrentShipsFleetHangar, "ItemHangar", "CurrentShipsFleetHangar");
            }

            return true;
        }

        private bool? Pickup(DirectContainer fromContainer, DirectContainer toContainer, string NameOfFromContainer, string NameOfToContainer)
        {
            if (toContainer == null)
            {
                Log("[" + NameOfToContainer + "] == null");
                return null;
            }

            if (fromContainer == null)
            {
                Log("[" + NameOfFromContainer + "] == null");
                return null;
            }

            if (toContainer.Capacity == 0)
            {
                Log("[" + NameOfToContainer + "].Capacity == 0");
                return null;
            }

            if (fromContainer.Capacity == 0)
            {
                Log("[" + NameOfFromContainer + "].Capacity == 0");
                return null;
            }

            if (toContainer.UsedCapacity == null)
            {
                Log("[" + NameOfToContainer + "].Capacity == null");
                return null;
            }

            if (fromContainer.UsedCapacity == null)
            {
                Log("[" + NameOfFromContainer + "].Capacity == null");
                return null;
            }

            if (100 >= toContainer.UsedCapacityPercentage)
            {
                if (DirectEve.Interval(30000, 30000, NameOfToContainer + NameOfFromContainer))
                {
                    if (_typeIDsToTransport.Any())
                    {
                        Log("_typeIDsToTransport contains [" + _typeIDsToTransport.Count() + "]");
                        foreach (var _typeIDToTransport in _typeIDsToTransport)
                        {
                            DirectItem TempDirectItem = null;
                            TempDirectItem.TypeId = _typeIDToTransport;
                            if (TempDirectItem != null)
                            {
                                Log("[" + TempDirectItem.TypeName + "] TypeID[" + TempDirectItem.TypeId + "]");
                            }
                        }
                    }

                    if (_groupIDsToTransport.Any())
                    {
                        Log("_groupIDsToTransport contains [" + _groupIDsToTransport.Count() + "]");
                        foreach (var _groupIDToTransport in _groupIDsToTransport)
                        {
                            Log("GroupID [" + _groupIDToTransport + "]");
                        }
                    }

                    if (_categoryIDsToTransport.Any())
                    {
                        Log("_categoryIDsToTransport contains [" + _categoryIDsToTransport.Count() + "]");
                        foreach (var _categoryIDToTransport in _categoryIDsToTransport)
                        {
                            Log("CategoryID [" + _categoryIDToTransport + "]");
                        }
                    }

                    if (_categoryIDsToTransport.Any())
                    {
                        Log("_tempExcludedTypeIDs contains [" + _tempExcludedTypeIDs.Count() + "]");
                        foreach (var _tempExcludedTypeID in _tempExcludedTypeIDs)
                        {
                            Log("TypeID [" + _tempExcludedTypeID + "]");
                        }
                    }
                }

                if (!_typeIDsToTransport.Any() && DirectEve.Interval(30000, 30000, NameOfToContainer + NameOfFromContainer)) Log("_typeIDsToTransport list is empty.");
                if (!_groupIDsToTransport.Any() && DirectEve.Interval(30000, 30000, NameOfToContainer + NameOfFromContainer)) Log("_groupIDsToTransport list is empty.");
                if (!_categoryIDsToTransport.Any() && DirectEve.Interval(30000, 30000, NameOfToContainer + NameOfFromContainer)) Log("_categoryIDsToTransport list is empty.");

                var ItemsToMoveNoExclusions = fromContainer.Items.Where(x => (!x.IsSingleton || x.IsBlueprintCopy) &&
                    x.IsAllowedInThisHoldType(NameOfToContainer) &&
                    (_typeIDsToTransport.Contains(x.TypeId) ||
                    _groupIDsToTransport.Contains(x.GroupId) ||
                    _categoryIDsToTransport.Contains(x.CategoryId)
                    ));

                if (ItemsToMoveNoExclusions.Any())
                {
                    if (DebugConfig.DebugItemTransportController && DirectEve.Interval(30000, 30000, NameOfToContainer + NameOfFromContainer))
                    {
                        Log("ItemsToMoveNoExclusions contains [" + ItemsToMoveNoExclusions.Count() + "] items from [" + NameOfFromContainer + "]");
                        foreach (var ItemToMoveNoExclusions in ItemsToMoveNoExclusions)
                        {
                            Log("[" + ItemToMoveNoExclusions.TypeName + "] TypeID[" + ItemToMoveNoExclusions.TypeId + "] GroupID[" + ItemToMoveNoExclusions.GroupId + "] CategoryID[" + ItemToMoveNoExclusions.CategoryId + "] Quantity [" + ItemToMoveNoExclusions.Quantity + "] IsSingleton[" + ItemToMoveNoExclusions.IsSingleton + "]");
                        }
                    }
                    var ItemsToMoveWithExclusions = ItemsToMoveNoExclusions.Where(x =>
                    !_tempExcludedTypeIDs.Contains(x.TypeId)
                    && !_excludedTypeIDs.Contains(x.TypeId)
                    && !_excludedGroupIDs.Contains(x.GroupId)
                    && !_excludedCategoryIDs.Contains(x.CategoryId)
                    );

                    if (ItemsToMoveWithExclusions.Any())
                    {
                        if (DebugConfig.DebugItemTransportController && DirectEve.Interval(30000, 30000, NameOfToContainer + NameOfFromContainer))
                        {
                            Log("ItemsToMoveWithExclusions contains [" + ItemsToMoveWithExclusions.Count() + "] items from the [" + NameOfFromContainer + "]");
                            foreach (var ItemToMoveWithExclusions in ItemsToMoveWithExclusions)
                            {
                                Log("[" + ItemToMoveWithExclusions.TypeName + "] TypeID[" + ItemToMoveWithExclusions.TypeId + "] GroupID[" + ItemToMoveWithExclusions.GroupId + "] CategoryID[" + ItemToMoveWithExclusions.CategoryId + "] Quantity [" + ItemToMoveWithExclusions.Quantity + "] IsSingleton[" + ItemToMoveWithExclusions.IsSingleton + "]");
                            }
                        }

                        foreach (var item in ItemsToMoveWithExclusions.OrderBy(i => i.TotalVolume)) //smallest stack of items first!
                        {
                            if (item.Volume > toContainer.Capacity)
                            {
                                Log("[" + item.TypeName + "] item.Volume [" + item.Volume + " ] > [" + toContainer + "].Capacity [" + toContainer.Capacity + "] adding [" + item.TypeName + "][" + item.TypeId + "] to _excludedTypeIDs");
                                _excludedTypeIDs.Add(item.TypeId);
                                continue;
                            }

                            if (item.Volume > toContainer.FreeCapacity)
                            {
                                Log("[" + item.TypeName + "] item.Volume [" + item.Volume + " ] > [" + toContainer + "].FreeCapacity [" + toContainer.FreeCapacity + "] adding it to _excludedTypeIDs because the item will not fit");
                                _tempExcludedTypeIDs.Add(item.TypeId);
                                continue;
                            }

                            bool? MoveResult = MoveItems(fromContainer, toContainer, item, item.Quantity);
                            if (MoveResult == null)
                            {
                                Log("MoveResult = null: Waiting");
                                return null;
                            }

                            if (MoveResult.Value)
                            {
                                Log("Moved [" + item.TypeName + "][" + item.TypeId + "][" + item.Quantity + "] to [" + NameOfToContainer + "]");
                                return false;
                            }
                            else
                            {
                                Log("Could not move [" + item.TypeName + "][" + item.TypeId + "][" + item.Quantity + "]");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        Log("!ItemsToMoveWithExclusions.Any()");
                        if (DebugConfig.DebugItemTransportController & DirectEve.Interval(30000, 30000, NameOfToContainer + NameOfFromContainer))
                        {
                            Log("Items in the ItemsToMoveNoExclusions List: Do you have your exclusions setup properly?");
                            foreach (var item in ItemsToMoveNoExclusions)
                            {
                                Log("[" + item.TypeName + "] typeID [" + item.TypeId + "] groupID [" + item.GroupId + "] CategoryID [" + item.CategoryId + "]");
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    Log("!ItemsToMoveNoExclusions.Any()");
                    if (DebugConfig.DebugItemTransportController)
                    {
                        if (fromContainer == null) Log("[" + NameOfFromContainer + "] is null");

                        if (fromContainer.Items.Any() && DirectEve.Interval(30000, 30000, NameOfToContainer + NameOfFromContainer))
                        {
                            Log("Items in [" + NameOfFromContainer + "]");
                            foreach (var item in fromContainer.Items)
                            {
                                Log("[" + item.TypeName + "] typeID [" + item.TypeId + "] groupID [" + item.GroupId + "] CategoryID [" + item.CategoryId + "] Quantity [" + item.Quantity + "] IsSingleton [" + item.IsSingleton + "]");
                                continue;
                            }
                        }
                        else Log("[" + NameOfFromContainer + "] has no items?!");
                    }
                }

                ClearPerCargoHoldTypeCache();
                return true;
            }

            ClearPerCargoHoldTypeCache();
            return true;
        }

        private void StatePickupItems()
        {
            if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: PickingUpItems");


            //this may need to be a setting?
            //how do we determine where the items are going?
            //Generally the choices are OreHangar, CargoHold, CorpHangar

            //
            //Do we have an OreHold? Do we have any ore left to move?
            //
            //PickupItemsFromItemHangarToOreHold();
            if (ESCache.Instance.ActiveShip.HasOreHold)
            {
                if (DebugConfig.DebugItemTransportController) Log("ShipType: HasOreHold");
                bool? PickupItemsFromItemHangarToOreHoldResult = PickupItemsFromItemHangarToOreHold();
                if (PickupItemsFromItemHangarToOreHoldResult == null) return;
                if (!PickupItemsFromItemHangarToOreHoldResult.Value) return;
            }

            //
            //Do we have a Mineral Hold? Do we have any minerals left to move?
            //
            if (ESCache.Instance.ActiveShip.HasAmmoHold)
            {
                if (DebugConfig.DebugItemTransportController) Log("ShipType: HasAmmoHold");
                bool? PickupItemsFromItemHangarToAmmoHoldResult = PickupItemsFromItemHangarToAmmoHold();
                if (PickupItemsFromItemHangarToAmmoHoldResult == null) return;
                if (!PickupItemsFromItemHangarToAmmoHoldResult.Value) return;
            }

            //
            //Do we have a Mineral Hold? Do we have any minerals left to move?
            //
            if (ESCache.Instance.ActiveShip.HasMineralHold)
            {
                if (DebugConfig.DebugItemTransportController) Log("ShipType: HasMineralHold");
                bool? PickupItemsFromItemHangarToMineralHoldResult = PickupItemsFromItemHangarToMineralHold();
                if (PickupItemsFromItemHangarToMineralHoldResult == null) return;
                if (!PickupItemsFromItemHangarToMineralHoldResult.Value) return;
            }

            //
            //Do we have a General Mining Hold? Do we have any ore left to move?
            //
            if (ESCache.Instance.ActiveShip.HasGeneralMiningHold)
            {
                if (DebugConfig.DebugItemTransportController) Log("ShipType: HasGeneralMiningHold");
                bool? PickupItemsFromItemHangarToGeneralMiningHoldResult = PickupItemsFromItemHangarToGeneralMiningHold();
                if (PickupItemsFromItemHangarToGeneralMiningHoldResult == null) return;
                if (!PickupItemsFromItemHangarToGeneralMiningHoldResult.Value) return;
            }

            //
            // Do we have a CargoHold? Do we have any regular cargo left to move?
            //
            bool? PickupItemsFromItemHangarToCargoHoldResult = PickupItemsFromItemHangarToCargoHold();
            if (PickupItemsFromItemHangarToCargoHoldResult == null) return;
            if (!PickupItemsFromItemHangarToCargoHoldResult.Value) return;

            //
            // Do we have a FleetHangar? Do we have any regular cargo left to move?
            //

            if (ESCache.Instance.ActiveShip.HasFleetHangar)
            {
                if (DebugConfig.DebugItemTransportController) Log("ShipType: HasFleetHangar");
                bool? PickupItemsFromItemHangarToFleetHangarResult = PickupItemsFromItemHangarToFleetHangar();
                if (PickupItemsFromItemHangarToFleetHangarResult == null) return;
                if (!PickupItemsFromItemHangarToFleetHangarResult.Value) return;
            }
            else
            {
                Log("StatePickupItems: Shiptype does NOT have FleetHangar.");
            }

            //
            // Generally speaking we can only take so much cargo per run and we dont care if there is more left, we will get it on the next run
            // we just need to fill the ship as much as we can
            //

            // Are we full? and/or are there no more items to move?

            if (DoWeHaveAnyCargo == null) return;

            if (!DoWeHaveAnyCargo ?? false)
            {
                Log("No Cargo Found: We are done transporting");
                _currentState = TransportState.Done;
                return;
            }

            Log("Picked up items. Traveling to delivery station.");
            StartTraveler();
            _currentState = TransportState.TravelingToDeliveryStation;
            return;
        }

        private bool? DoWeHaveAnyCargo
        {
            get
            {
                if (ESCache.Instance.CurrentShipsCargo.UsedCapacity == null)
                    return null;

                if (ESCache.Instance.CurrentShipsCargo.UsedCapacity > 0)
                    return true;

                //if (ESCache.Instance.ActiveShip.HasOreHold)
                //{
                //    if (ESCache.Instance.CurrentShipsOreHold.UsedCapacity > 0)
                //        return true;
                //}

                if (ESCache.Instance.ActiveShip.HasFleetHangar)
                {
                    if (ESCache.Instance.CurrentShipsFleetHangar.UsedCapacity == null)
                        return null;

                    if (ESCache.Instance.CurrentShipsFleetHangar.UsedCapacity > 0)
                        return true;
                }

                if (ESCache.Instance.ActiveShip.HasMineralHold)
                {
                    if (ESCache.Instance.CurrentShipsMineralHold.UsedCapacity == null)
                        return null;

                    if (ESCache.Instance.CurrentShipsMineralHold.UsedCapacity > 0)
                        return true;
                }

                if (ESCache.Instance.ActiveShip.HasGeneralMiningHold)
                {
                    if (ESCache.Instance.CurrentShipsGeneralMiningHold.UsedCapacity == null)
                        return null;

                    if (ESCache.Instance.CurrentShipsGeneralMiningHold.UsedCapacity > 0)
                        return true;
                }

                if (ESCache.Instance.ActiveShip.HasAmmoHold)
                {
                    if (ESCache.Instance.CurrentShipsAmmoHold.UsedCapacity == null)
                        return null;

                    if (ESCache.Instance.CurrentShipsAmmoHold.UsedCapacity > 0)
                        return true;
                }

                return false;
            }
        }

        private void StateTravelToDeliveryStation()
        {
            if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: TravelingToDeliveryStation");
            // (Implementation remains the same as before)
            if (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity?.HasInitiatedWarp == true)
                return;

            if (_destination == null ||
                (_destination is BookmarkDestination sDest && sDest.BookmarkId != deliveryStationBookmark.BookmarkId))
            {
                Log("Setting Destination to [" + deliveryStationBookmark.Description + "]");
                _destination = new BookmarkDestination(deliveryStationBookmark);
                Traveler.Destination = _destination;
                return;
            }

            Traveler.ProcessState();

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Log("Arrived at Delivery Station");
                Traveler.Destination = null;
                _currentState = TransportState.DeliveringItems;
                return;
            }
            //else if (State.CurrentTravelerState == TravelerState.Error)
            //{
            //   TriggerErrorCooldown("Error traveling to delivery station.");
            //}
        }

        private bool? DropOff(DirectContainer fromContainer, DirectContainer toContainer, string fromContainerName, string toContainerName)
        {
            try
            {
                if (fromContainer == null)
                {
                    Log("[" + fromContainerName + "] is null");
                    return null;
                }

                if (toContainer == null)
                {
                    Log("[" + toContainerName + "] is null");
                    return null;
                }

                if (toContainer.Capacity == 0)
                {
                    Log("[" + toContainerName + "] Capacity == 0");
                    return null;
                }

                if (fromContainer.Capacity == 0)
                {
                    Log("[" + fromContainerName + "] Capacity == 0");
                    return null;
                }

                if (fromContainer.Items.Any())
                {
                    foreach (var item in fromContainer.Items)
                    {
                        bool? MoveResult = MoveItems(fromContainer, toContainer, item, item.Quantity);
                        if (MoveResult == null)
                            Log("MoveResult = null: Waiting");

                        if (MoveResult.Value)
                        {
                            Log("Moved [" + item.TypeName + "][" + item.TypeId + "][" + item.Quantity + "] to the [" + toContainerName + "]");
                            return false;
                        }
                        else
                        {
                            Log("Could not move [" + item.TypeName + "][" + item.TypeId + "][" + item.Quantity + "]");
                            continue;
                        }
                    }
                }
                else Log("[" + fromContainerName + "]: has no items");

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private bool? MoveItemsFromAmmoHoldToItemHangar()
        {
            try
            {
                if (ESCache.Instance.ActiveShip.HasAmmoHold)
                {
                    DropOff(ESCache.Instance.CurrentShipsAmmoHold, ESCache.Instance.ItemHangar, "CurrentShipsAmmoHold", "ItemHangar");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private bool? MoveItemsFromMineralHoldToItemHangar()
        {
            try
            {
                if (ESCache.Instance.ActiveShip.HasAmmoHold)
                {
                    DropOff(ESCache.Instance.CurrentShipsMineralHold, ESCache.Instance.ItemHangar, "CurrentShipsMineralHold", "ItemHangar");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private bool? MoveItemsFromGeneralMiningHoldToItemHangar()
        {
            try
            {
                if (ESCache.Instance.ActiveShip.HasAmmoHold)
                {
                    DropOff(ESCache.Instance.CurrentShipsGeneralMiningHold, ESCache.Instance.ItemHangar, "CurrentShipsGeneralMiningHold", "ItemHangar");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private bool? MoveItemsFromOreHoldToItemHangar()
        {
            try
            {
                if (ESCache.Instance.ActiveShip.HasAmmoHold)
                {
                    //DropOff(ESCache.Instance.CurrentShipsOreHold, ESCache.Instance.ItemHangar, "CurrentShipsOreHold", "ItemHangar");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private bool? MoveItemsFromCargoHoldToItemHangar()
        {
            try
            {
                if (ESCache.Instance.ActiveShip.HasAmmoHold)
                {
                    DropOff(ESCache.Instance.CurrentShipsCargo, ESCache.Instance.ItemHangar, "CurrentShipsCargo", "ItemHangar");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private bool? MoveItemsFromFleetHangarToItemHangar()
        {
            try
            {
                if (ESCache.Instance.ActiveShip.HasAmmoHold)
                {
                    DropOff(ESCache.Instance.CurrentShipsFleetHangar, ESCache.Instance.ItemHangar, "CurrentShipsFleetHangar", "ItemHangar");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private void StateDeliverItems()
        {
            try
            {
                if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: DeliveringItems");


                //
                // Do we have anything in the Ammo Hold? Do we have one?
                //
                if (ESCache.Instance.ActiveShip.HasAmmoHold)
                {
                    bool? MoveItemsFromAmmoHoldToItemHangarResult = MoveItemsFromAmmoHoldToItemHangar();
                    if (MoveItemsFromAmmoHoldToItemHangarResult == null) return;
                    if (!MoveItemsFromAmmoHoldToItemHangarResult.Value) return;
                }

                //
                // Do we have anything in the OreHold? Do we have one?
                //
                if (ESCache.Instance.ActiveShip.HasOreHold)
                {
                    bool? MoveItemsFromOreHoldToItemHangarResult = MoveItemsFromOreHoldToItemHangar();
                    if (MoveItemsFromOreHoldToItemHangarResult == null) return;
                    if (!MoveItemsFromOreHoldToItemHangarResult.Value) return;
                }

                //
                // Do we have anything in the MineralHold? Do we have one?
                //
                if (ESCache.Instance.ActiveShip.HasMineralHold)
                {
                    bool? MoveItemsFrommineralHoldToItemHangarResult = MoveItemsFromMineralHoldToItemHangar();
                    if (MoveItemsFrommineralHoldToItemHangarResult == null) return;
                    if (!MoveItemsFrommineralHoldToItemHangarResult.Value) return;
                }

                //
                // Do we have anything in the MineralHold? Do we have one?
                //
                if (ESCache.Instance.ActiveShip.HasGeneralMiningHold)
                {
                    bool? MoveItemsFromGeneralMiningHoldToItemHangarResult = MoveItemsFromGeneralMiningHoldToItemHangar();
                    if (MoveItemsFromGeneralMiningHoldToItemHangarResult == null) return;
                    if (!MoveItemsFromGeneralMiningHoldToItemHangarResult.Value) return;
                }

                //
                // Do we have anything in the FleetHangar? Do we have one?
                //

                if (ESCache.Instance.ActiveShip.HasFleetHangar)
                {
                    bool? MoveItemsFromFleetHangarToItemHangarResult = MoveItemsFromFleetHangarToItemHangar();
                    if (MoveItemsFromFleetHangarToItemHangarResult == null) return;
                    if (!MoveItemsFromFleetHangarToItemHangarResult.Value) return;
                }

                //
                // Do we have anything in the CargoHold
                //
                bool? MoveItemsFromCargoHoldToItemHangarResult = MoveItemsFromCargoHoldToItemHangar();
                if (MoveItemsFromCargoHoldToItemHangarResult == null) return;
                if (!MoveItemsFromCargoHoldToItemHangarResult.Value) return;

                //
                // determine if we are ready to return to pickup station?
                // Check various holds and make sure they are empty(ish)?
                // if they arent we should error here before trying to leave!
                //
                if (DoWeHaveAnyCargo == null) return;

                if (DoWeHaveAnyCargo ?? false)
                {
                    Log("DoWeHaveAnyCargo [true] waiting?!");
                    return;
                }


                Log("Delivered items, returning to pickup station to check if more remain.");
                StartTraveler();
                _currentState = TransportState.TravelingToPickupStation;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        private static DateTime _lastMoveItemsAction = DateTime.UtcNow.AddDays(-1);

        public static bool? MoveItems(DirectContainer fromContainer, DirectContainer toContainer, DirectItem fromContainerItem, double totalQuantityToMove)
        {
            try
            {
                double _itemsLeftToMoveQuantity = fromContainerItem.Quantity;

                if (_lastMoveItemsAction.AddSeconds(1) > DateTime.UtcNow)
                    return null;

                if (fromContainerItem == null)
                {
                    return null;
                }

                if (toContainer.WaitingForLockedItems()) return null;

                //  here we check if we have enough free m3 in our ship hangar

                if (toContainer == null)
                    return null;

                if (toContainer.UsedCapacity == null)
                    return null;

                if (fromContainerItem != null)
                {
                    double amountThatWillFitInToContainer = 0;
                    double freeCapacityOfToContainer = toContainer.Capacity - (double)toContainer.UsedCapacity;
                    amountThatWillFitInToContainer = freeCapacityOfToContainer / fromContainerItem.Volume;

                    _itemsLeftToMoveQuantity = Math.Min(amountThatWillFitInToContainer, _itemsLeftToMoveQuantity);

                    Log("Capacity [" + toContainer.Capacity + "] freeCapacity [" + freeCapacityOfToContainer + "] usedCapacity [" + toContainer.UsedCapacity + "] amountThatWillFitInToContainer [" + amountThatWillFitInToContainer +
                                  "] itemToMove Volume [" + fromContainerItem.Volume + " per unit] _itemsLeftToMoveQuantity [" + _itemsLeftToMoveQuantity + "]");
                }

                if (_itemsLeftToMoveQuantity <= 0)
                {
                    Log("if (_itemsLeftToMoveQuantity <= 0)");
                    return true;
                }

                if (fromContainerItem.Volume > toContainer.FreeCapacity)
                {
                    Log("Error: if (fromContainerItem.Volume > toContainer.FreeCapacity)");
                    return false;
                }

                Log("_itemsLeftToMoveQuantity: " + _itemsLeftToMoveQuantity);

                if (fromContainerItem != null && !string.IsNullOrEmpty(fromContainerItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                {
                    if (fromContainerItem.ItemId <= 0 || fromContainerItem.Volume == 0.00 || fromContainerItem.Quantity == 0)
                        return true;

                    double moveItemQuantity = Math.Min(fromContainerItem.Stacksize, _itemsLeftToMoveQuantity);
                    moveItemQuantity = Math.Max(moveItemQuantity, 1);
                    if (moveItemQuantity > 2000000)
                        moveItemQuantity = 2000000;

                    _itemsLeftToMoveQuantity -= moveItemQuantity;
                    bool movingItemsThereAreNoMoreItemsToGrabAtPickup = _itemsLeftToMoveQuantity > 0;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MovingItemsThereAreNoMoreItemsToGrabAtPickup), movingItemsThereAreNoMoreItemsToGrabAtPickup);
                    Log("Moving Item [" + fromContainerItem.TypeName + "] num of units [" + moveItemQuantity + "] m3 [" + fromContainerItem.Volume * moveItemQuantity + "] from FromContainer to toContainer: We have [" + _itemsLeftToMoveQuantity +
                                  "] more item(s) to move after this");

                    if (DebugConfig.DebugItemTransportControllerDontMoveItems)
                    {
                        Log("Verify the math above and disable debug to actually try to move the items. DONT try to move them if the math is wrong");
                        _lastMoveItemsAction = DateTime.UtcNow;
                        return true;
                    }

                    if (!toContainer.Add(fromContainerItem, (int)moveItemQuantity)) return false;
                    _lastMoveItemsAction = DateTime.UtcNow;
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private void StateWaitOnErrorCooldown()
        {
            Log($"State: WaitOnErrorCooldown. Waiting until {_errorCooldownUntil.ToLongTimeString()}.");
            if (DateTime.UtcNow < _errorCooldownUntil)
                return; // Wait out the cooldown

            if (_errorCounter >= MaxErrorCount)
            {
                _currentState = TransportState.Error;
                return;
            }

            Log($"Error cooldown finished; retrying. Error attempts: {_errorCounter}/{MaxErrorCount}.");
            _currentState = _previousState; // Go back to the state before the error
        }

        private void StateDone()
        {
            Log("State: Done. Transport task completed successfully.");
            IsWorkDone = true;
        }

        private void StateError()
        {
            Log("State: Error - Transport Bot encountered a critical error and is stopping.");
            Log("Please review logs for details.");

            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "ItemTransportController encountered an error and stopped."));
            IsWorkDone = true;
            ControllerManager.Instance.SetPause(true);
        }

        #endregion

        #region Error Handling (Enhanced Error Cooldown - No Changes)

        private void TriggerErrorCooldown(string errorMessage = "")
        {
            _errorCounter++;
            Log($"Error in state {_currentState}: {errorMessage}. Attempt {_errorCounter}/{MaxErrorCount}. Entering cooldown.");
            if (_errorCounter >= MaxErrorCount)
            {
                Log("Reached max errors. Transitioning to Error state.");
                _currentState = TransportState.Error;
                return;
            }

            _previousState = _currentState;
            _errorCooldownUntil = DateTime.UtcNow + _errorCooldownDuration;
            _currentState = TransportState.WaitOnErrorCooldown;
        }

        #endregion

        #region Helpers (Helper Functions - Mostly unchanged)

        private bool IsExcludedGroup(int groupId)
        {
            return _excludedGroupIDs.Contains(groupId);
        }

        private bool CheckHangarForTransportItems(DirectContainer itemHangar)
        {
            if (itemHangar == null) return false;
            return itemHangar.Items.Any(i => IsValidTransportItem(i));
        }

        private bool IsValidTransportItem(DirectItem item)
        {
            if (item == null) return false;
            if (_excludedGroupIDs.Contains(item.GroupId)) return false; // Exclude by Group ID
            if (_typeIDsToTransport.Any())  // Include by specific Item IDs (if any are specified)
            {
                return _typeIDsToTransport.Contains(item.TypeId);
            }

            return true; // If no specific item IDs, include all non-excluded items
        }

        #endregion
    }
}