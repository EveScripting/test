extern alias SC;

using EVESharpCore.Logging;
using System;
using EVESharpCore.Questor.States;
using EVESharpCore.Cache;
using EVESharpCore.Lookup;
using EVESharpCore.Framework;
using EVESharpCore.Traveller;
using System.Linq;
using EVESharpCore.Questor.BackgroundTasks;
using SC::SharedComponents.EVE;
using EVESharpCore.Controllers;
using EVESharpCore.Controllers.Abyssal;
using SC::SharedComponents.Utility;
using EVESharpCore.Questor.Activities;
using System.Globalization;
using System.Collections.Generic;

namespace EVESharpCore.Questor.Behaviors
{
    public class GatherItemsBehavior
    {
        #region Constructors

        public GatherItemsBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields
        #endregion Fields

        #region Methods

        public static bool ChangeGatherItemsBehaviorState(GatherItemsBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentGatherItemsBehaviorState != _StateToSet)
                {
                    if (_StateToSet == GatherItemsBehaviorState.TravelToMarketSystem)
                        State.CurrentTravelerState = TravelerState.Idle;

                    if (_StateToSet == GatherItemsBehaviorState.TravelToToLocation)
                        State.CurrentTravelerState = TravelerState.Idle;

                    Log.WriteLine("New GatherItemsBehaviorState [" + _StateToSet + "]");
                    State.CurrentGatherItemsBehaviorState = _StateToSet;
                    if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void temp()
        {
            try
            {
                if (DirectEve.Interval(20000))
                {
                    Log.WriteLine("GatherShipsBehavior.ProcessState: [" + State.CurrentCombatMissionBehaviorState + "]");

                    if (ESCache.Instance.DirectEve.GetAssets().Any())
                    {
                        var CachedAssets = ESCache.Instance.DirectEve.GetAssets();
                        Log.WriteLine("Assets[" + CachedAssets.Count() + "]: results limited to 100 so we dont fill the log");
                        foreach (var item in CachedAssets.Take(100).OrderBy(i => i.SolarSystemName))
                        {
                            try
                            {
                                Log.WriteLine("  " + item.TypeName + " [" + item.Quantity + "] IsSingleton [" + item.IsSingleton + "][" + item.SolarSystemName + "][" + item.StationName + "]");
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }
                        }

                        var Assets_ShipsOnly = CachedAssets.Where(i => i.CategoryId == (int)CategoryID.Ship && i.SolarSystem != null && !i.SolarSystem.IsWormholeSystem);
                        Log.WriteLine("Assets: Ships [" + Assets_ShipsOnly.Count() + "]: results limited to 100 so we dont fill the log");
                        foreach (var item in Assets_ShipsOnly.Take(100).OrderBy(i => i.SolarSystem.Jumps))
                        {
                            try
                            {
                                Log.WriteLine("  " + item.TypeName + " [" + item.Quantity + "] IsSingleton [" + item.IsSingleton + "][" + item.SolarSystemName + "][" + item.StationName + "]");
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }
                        }

                        // Do something with the assets here...
                    }
                    else Log.WriteLine("Assets: No Assets Found");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool EveryPulse()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGatherItemsBehavior) Log.WriteLine("EveryPulse: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)");
                return false;
            }

            Panic.ProcessState(Settings.Instance.HomeBookmarkName);

            if (State.CurrentPanicState == PanicState.StartPanicking || State.CurrentPanicState == PanicState.Panicking || State.CurrentPanicState == PanicState.Panic)
            {
                return false;
            }

            if (State.CurrentPanicState == PanicState.Resume)
            {
                if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    Panic.ChangePanicState(PanicState.Normal);
                    State.CurrentTravelerState = TravelerState.Idle;
                    return true;
                }

                if (DebugConfig.DebugGatherItemsBehavior) Log.WriteLine("EveryPulse: if (State.CurrentPanicState == PanicState.Resume)");
                return false;
            }

            return true;
        }

        public static bool DoWeHaveItemsToGather
        {
            get
            {
                try
                {
                    if (ESCache.Instance.DirectEve.GetAssets().Any())
                    {
                        //
                        // we need to add the ability to NOT gather stuff from our Home Station! We dont want to be moving our ammo/drones/and such: but we DO want to move our loot.
                        // might need to ponder this a bit and maybe add a blacklist of stuff to not move? or maybe a whitelist of stuff to move?
                        //
                        var CachedAssets = ESCache.Instance.DirectEve.GetAssets();
                        var AssetsNotInJita_ItemsOnly = CachedAssets.Where(i => i.CategoryId != (int)CategoryID.Ship &&
                                                                                i.SolarSystem != null &&
                                                                                !i.SolarSystem.IsWormholeSystem &&
                                                                                i.LocationId != ESCache.Instance.StationIDJitaP4M4 &&
                                                                                i.SolarSystem.IsHighSecuritySpace &&
                                                                                i.SolarSystem.CanBeReachedUsingHighSecOnly);
                        if (AssetsNotInJita_ItemsOnly.Any())
                        {
                            return true;
                        }

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
        }

        public static void ClearPerSystemCache()
        {
            _nextStationToGatherItemsFromUs = null;
        }

        public static void ClearPerPocketCache()
        {
            _nextStationToGatherItemsFromUs = null;
        }

        public static long? _nextStationToGatherItemsFromUs = null;
        public static long NextStationToGatherItemsFrom
        {
            get
            {
                try
                {
                    if (_nextStationToGatherItemsFromUs != null)
                        return _nextStationToGatherItemsFromUs ?? 0;

                    if (ESCache.Instance.DirectEve.GetAssets().Any())
                    {
                        var CachedAssets = ESCache.Instance.DirectEve.GetAssets();
                        var AssetsNotInJita_ItemsOnly = CachedAssets.Where(i => i.CategoryId != (int)CategoryID.Ship &&
                                                        !i.IsSingleton && //this indicates this ship is not assembled
                                                        i.SolarSystem != null &&
                                                        !i.SolarSystem.IsWormholeSystem &&
                                                        i.LocationId != ESCache.Instance.StationIDJitaP4M4 &&
                                                        i.SolarSystem.IsHighSecuritySpace &&
                                                        i.SolarSystem.CanBeReachedUsingHighSecOnly);

                        if (AssetsNotInJita_ItemsOnly.Any())
                        {
                            _nextStationToGatherItemsFromUs = AssetsNotInJita_ItemsOnly.OrderBy(i => i.SolarSystem.Jumps).FirstOrDefault().LocationId;
                        }

                        return 0;
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 0;
                }
            }
        }


        public static void ProcessState()
        {
            // Method intentionally left empty.
            try
            {
                if (ESCache.Instance.DirectEve.Session.IsWspace)
                {
                    if (DebugConfig.DebugGatherItemsBehavior) Log.WriteLine("ProcessState: We are in wspace, returning");
                    return;
                }

                if (!EveryPulse()) return;

                if (DebugConfig.DebugGatherItemsBehavior) Log.WriteLine("State.CurrentGatherItemsBehaviorState is [" + State.CurrentGatherItemsBehaviorState + "]");

                switch (State.CurrentGatherItemsBehaviorState)
                {
                    case GatherItemsBehaviorState.Idle:
                        if (DoWeHaveItemsToGather)
                        {
                            ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.Start);
                            return;
                        }

                        break;

                    case GatherItemsBehaviorState.Start:
                        //ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.LeaveShip);
                        //We need to make sure we are in Jita before we start gathering things: we then need to get into our transportship!
                        ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.TravelToMarketSystem);
                        break;

                    case GatherItemsBehaviorState.TravelToToLocation:
                        TravelToToLocationState();
                        break;

                    case GatherItemsBehaviorState.FindStationToGatherItemsFrom:
                        FindNextStationToGatherItemsFromState();
                        break;

                    case GatherItemsBehaviorState.ActivateTransportShip:
                        ActivateTransportShipState();
                        break;

                    case GatherItemsBehaviorState.LoadItems:
                        LoadItemsState();
                        break;

                    case GatherItemsBehaviorState.UnloadCargoHold:
                        UnloadCargoHoldState();
                        break;

                    case GatherItemsBehaviorState.TravelToMarketSystem:
                        TravelToMarketSystemState();
                        break;

                    case GatherItemsBehaviorState.Default:
                        ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void UnloadCargoHoldState()
        {
            if (ESCache.Instance.ActiveShip == null)
            {
                Log.WriteLine("Active ship is null.");
                return;
            }

            if (ESCache.Instance.ItemHangar == null)
            {
                Log.WriteLine($"Itemhangar is null.");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log.WriteLine("Current ships cargo is null.");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo.Items.Any())
            {

                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("UnloadLoot"))
                    return;

                if (DirectEve.Interval(10000))
                {
                    if (ESCache.Instance.ItemHangar.Add(ESCache.Instance.CurrentShipsCargo.Items))
                    {
                        Log.WriteLine($"Moving items into itemhangar.");
                        return;
                    }
                }
            }
            else
            {
                // done
                Log.WriteLine("We finished unloading cargohold");
                ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.Start);
                return;
            }
        }

        private static void LoadItemsState()
        {
            MoveAllItemsToCargo(ESCache.Instance.ItemHangar, ESCache.Instance.CurrentShipsCargo);
            return;
        }

        public static bool MoveAllItemsToCargo(DirectContainer fromContainer, DirectContainer toContainer)
        {
            try
            {
                //
                // this is probably crazy slow with a lot of stacks of items... but it should work
                //
                if (!DirectEve.Interval(800))
                    return false;

                if (fromContainer == null) return false;

                if (!fromContainer.Items.Any())
                {
                    ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.TravelToMarketSystem);
                    return true;
                }

                if (toContainer.WaitingForLockedItems()) return false;

                int _itemsLeftToMoveQuantity = 0;
                //_itemsLeftToMoveQuantity = totalQuantityToMove - WeHaveThisManyOfThoseItemsInCargo > 0 ? totalQuantityToMove - WeHaveThisManyOfThoseItemsInCargo : 0;

                //  here we check if we have enough free m3 in our ship hangar

                if (toContainer == null)
                    return false;

                foreach (var fromContainerItem in fromContainer.Items)
                {
                    int amountThatWillFitInToContainer = 0;
                    double freeCapacityOfToContainer = toContainer.Capacity - (double)toContainer.UsedCapacity;
                    amountThatWillFitInToContainer = Convert.ToInt32(freeCapacityOfToContainer / fromContainerItem.Volume);

                    _itemsLeftToMoveQuantity = fromContainerItem.Quantity;

                    Log.WriteLine("Capacity [" + toContainer.Capacity + "] freeCapacity [" + freeCapacityOfToContainer + "] amount [" + amountThatWillFitInToContainer +
                                    "] _itemsLeftToMoveQuantity [" + _itemsLeftToMoveQuantity + "]");

                    if (_itemsLeftToMoveQuantity <= 0)
                    {
                        Log.WriteLine("if (_itemsLeftToMoveQuantity <= 0)");
                        continue;
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
                        return false;
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


        private static void ActivateTransportShipState()
        {
            if (!ESCache.Instance.InStation)
                return;


            if (ESCache.Instance.DirectEve.GetShipHangar() == null)
            {
                Log.WriteLine("Shiphangar is null.");
                return;
            }

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log.WriteLine("ItemHangar is null.");
                return;
            }

            var ShipsWithCorrectTransportShipName = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                             && i.GivenName != null
                                                             && i.GivenName == Settings.Instance.TransportShipName).ToList();
            if (ESCache.Instance.ActiveShip == null)
            {
                Log.WriteLine("Active ship is null.");
                return;
            }

            Log.WriteLine("ActiveShip is [" + ESCache.Instance.ActiveShip.GivenName + "] TypeID [" + ESCache.Instance.ActiveShip.TypeId + "]");

            if (ESCache.Instance.ActiveShip.GivenName == Settings.Instance.TransportShipName)
            {
                ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.FindStationToGatherItemsFrom);
                Log.WriteLine("We are in our transport ship now.");
                return;
            }

            if (ShipsWithCorrectTransportShipName.Any())
            {
                var transportShip = ShipsWithCorrectTransportShipName.OrderByDescending(i => i.GroupId == (int)Group.TransportShip).FirstOrDefault();
                if (transportShip != null)
                {
                    transportShip.ActivateShip();
                    Log.WriteLine("Found our transport ship named [" + transportShip.GivenName + "]. Making it active.");
                    return;
                }
                else Log.WriteLine("if (transportShip == null) !");
            }
            else
            {
                Log.WriteLine("TransportShipName [" + Settings.Instance.TransportShipName + "] was not found in Ship Hangar");
                Util.PlayNoticeSound();
                ControllerManager.Instance.SetPause(true);
            }
        }

        private static void TravelToMarketSystemState()
        {
            try
            {
                try
                {
                    Traveler.TravelToStationId(ESCache.Instance.StationIDJitaP4M4);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    Log.WriteLine("Arrived at destination");
                    Traveler.Destination = null;
                    ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.ActivateTransportShip);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void TravelToToLocationState()
        {
            try
            {
                try
                {
                    Traveler.TravelToStationId(CachedNextStationToGatherItemsFrom);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    Log.WriteLine("Arrived at destination");
                    Traveler.Destination = null;
                    ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.LoadItems);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static long CachedNextStationToGatherItemsFrom = 0;

        private static void FindNextStationToGatherItemsFromState()
        {
            try
            {
                if (ESCache.Instance.InStation)
                {
                    if (Time.Instance.LastUndockAction.AddSeconds(10) > DateTime.UtcNow)
                    {
                        Log.WriteLine("waiting for undock");
                        return;
                    }

                    if (NextStationToGatherItemsFrom != 0)
                    {
                        CachedNextStationToGatherItemsFrom = NextStationToGatherItemsFrom;
                        if (CachedNextStationToGatherItemsFrom == ESCache.Instance.DirectEve.Session.LocationId)
                        {
                            Log.WriteLine("PickNextShipToGrabState: We are at the location of the items we want to grab: locationID[" + CachedNextStationToGatherItemsFrom + "]");
                            ChangeGatherItemsBehaviorState(GatherItemsBehaviorState.TravelToToLocation);
                            return;
                        }

                        return;
                    }

                    Log.WriteLine("PickNextShipToGrabState: No more ships to grab");
                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: start");
            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return;
        }

        #endregion Methods
    }
}