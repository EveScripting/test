//
// (c) duketwo 2022
//

extern alias SC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.EVE.DatabaseSchemas;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.SQLite;
using SC::SharedComponents.Utility;
using ServiceStack.OrmLite;
using SharpDX.Direct2D1;
using System.Windows.Media;
using System.Xml;

namespace EVESharpCore.Controllers.Abyssal
{
    enum DataSurveyDumpState
    {
        Start,
        ActivateTransportShip,
        EmptyTransportShip,
        LoadSurveys,
        LoadLootToCargo,
        TravelToDumpSurveysStation,
        TravelToSellLootStation,
        SellSurveysToNPCBuyOrders,
        SellLootToBuyOrders,
        Done,
    }


    public partial class AbyssalController : AbyssalBaseController
    {

        private DataSurveyDumpState _dataSurveyDumpState;
        private TravelerDestination _travelerDestination;
        private int errorCnt;
        private bool _sellPerformed;
        private DateTime _sellPerformedDateTime;

        public bool? NeedToDumpDatabaseSurveys()
        {
            try
            {
                //
                // moving hundreds of blueprints lagged us out: need to break that up into smaller batches
                //
                if (!ESCache.Instance.InStation)
                    return false;

                if (ESCache.Instance.InWormHoleSpace)
                {
                    Log("InWormHoleSpace [true] we cant go to empire to sell, so dont try.");
                    return false;
                }

                if (!DirectEve.Interval(60000 * 15, 60000 * 15, AbyssalFilamentsActivated.ToString())) //15min of once every run
                {
                    return false;
                }

                if (ESCache.Instance.ShipHangar == null)
                {
                    Log("Shiphangar is null.");
                    return null;
                }

                if (!ESCache.Instance.ShipHangar.Items.Any())
                {
                    Log("ShipHangar is empty");
                    return null;
                }

                if (ESCache.Instance.ItemHangar == null)
                {
                    Log("ItemHangar is null.");
                    return null;
                }

                if (!ESCache.Instance.ItemHangar.Items.Any())
                {
                    Log("ItemHangar is empty");
                    return null;
                }

                // check if we are at the homestation, else false
                if (!AreWeDockedInHomeSystem())
                {
                    Log("AreWeDockedInHomeSystem [false]");
                    return false;
                }

                var transportship = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                                            && i.GivenName != null
                                                                            && i.GivenName == Settings.Instance.TransportShipName).ToList();

                // check if we are docked and a transport ship is available in the ship hangar, else false
                if (!transportship.Any())
                {
                    Log("No transport ship found.");
                    return false;
                }

                if (!ESCache.Instance.CachedBookmarks.Any())
                {
                    Log("No bookmarks found?! No bookmark named [" + _surveyDumpBookmarkName + "] found. We cannot sell surveys if we dont have a bookmark with that name telling us where to bring the items to sell");
                    return null;
                }

                if (!ESCache.Instance.BookmarksThatContain(_surveyDumpBookmarkName).Any())
                {
                    Log("No bookmark named [" + _surveyDumpBookmarkName + "] found. We cannot sell surveys if we dont have a bookmark with that name telling us where to bring the items to sell");
                    return null;
                }

                //if (ESCache.Instance.EveAccount.WalletBalance > 200_000_000)
                //{
                //    Log("WalletBalance [" + ESCache.Instance.EveAccount.WalletBalance + "] > 200mil - not selling database surveys");
                //    return false;
                //}

                // check if survey value is > (2b + 100m for each day of month) to add some randomness
                //var day = DateTime.Now.Day;
                double triglavianSurveyDatabaseBuyValuePer = 100000;
                var surveyAmount = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(e => e.TypeId == (int)TypeID.TriglavianSurveyDatabase).Sum(e => (long)e.Stacksize);
                var surveyIskValue = surveyAmount * triglavianSurveyDatabaseBuyValuePer;

                var ISKValueOurLootHasToBeMoreThan = _overThisSurveyValueGoSell;
                Log("Survey amount: [" + string.Format("{0:#,0}", surveyAmount) + "] Survey ISK value [" + string.Format("{0:#,0}", surveyIskValue) + " ] ISKValueOurLootHasToBeMoreThan: [" + string.Format("{0:#,0}", ISKValueOurLootHasToBeMoreThan) + "]");

                if (!ESCache.Instance.IsSafeToTravelIntoEmpireFromHere)
                {
                    Log("IsSafeToTravelIntoEmpireFromHere [False] cannot travel: canceling dumping of Survey Databases");
                    return false;
                }

                if (surveyIskValue > ISKValueOurLootHasToBeMoreThan)
                {
                    Log("Survey ISK value [" + string.Format("{0:#,0}", surveyIskValue) + "]  is greater than [" + string.Format("{0:#,0}", ISKValueOurLootHasToBeMoreThan) + "] Dumping.");
                    _dataSurveyDumpState = DataSurveyDumpState.Start;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log("Exception: [" + ex + "]");
                return false;
            }
        }

        internal void DumpDatabaseSurveys()
        {
            switch (_dataSurveyDumpState)
            {
                case DataSurveyDumpState.Start:
                    _dataSurveyDumpState = DataSurveyDumpState.ActivateTransportShip;
                    break;
                case DataSurveyDumpState.ActivateTransportShip:
                    ActivateTransportShip();
                    break;
                case DataSurveyDumpState.EmptyTransportShip:
                    EmptyTransportShip();
                    break;
                case DataSurveyDumpState.LoadSurveys:
                    LoadSurveys();
                    break;
                case DataSurveyDumpState.TravelToDumpSurveysStation:
                    TravelToDumpStation();
                    break;
                case DataSurveyDumpState.SellSurveysToNPCBuyOrders:
                    DumpSurveys();
                    break;
                case DataSurveyDumpState.LoadLootToCargo:
                    LoadLoot();
                    break;
                case DataSurveyDumpState.TravelToSellLootStation:
                    break;
                case DataSurveyDumpState.Done:
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    break;
                case DataSurveyDumpState.SellLootToBuyOrders:
                    //SellLoot();
                    break;
            }
        }


        private void ActivateTransportShip()
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

            var transportship = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                            && i.GivenName != null
                                                            && i.GivenName == Settings.Instance.TransportShipName).ToList();
            if (ESCache.Instance.ActiveShip == null)
            {
                Log("Active ship is null.");
                return;
            }

            if (ESCache.Instance.ActiveShip.GivenName == Settings.Instance.TransportShipName)
            {
                _dataSurveyDumpState = DataSurveyDumpState.EmptyTransportShip;
                Log("We are in a transport ship now.");
                return;
            }

            if (transportship.Any())
            {
                transportship.FirstOrDefault().ActivateShip();
                Log("Found a transport ship. Making it active.");
                LocalPulse = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                return;
            }

        }
        private void EmptyTransportShip()
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

                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("EmptyTransportShip"))
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
                _dataSurveyDumpState = DataSurveyDumpState.LoadSurveys;
            }

        }

        private bool _isItSafeToKeepLoadingLoot_AntiGank = false;

        private bool? IsItSafeToKeepLoadingLoot_AntiGank
        {
            get
            {
                if (!DirectEve.HasFrameChanged())
                    return _isItSafeToKeepLoadingLoot_AntiGank;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.TransportShip)
                {
                    _isItSafeToKeepLoadingLoot_AntiGank = true;
                    return _isItSafeToKeepLoadingLoot_AntiGank;
                }

                if (ESCache.Instance.CurrentShipsCargo == null)
                {
                    return null;
                }

                if (!ESCache.Instance.CurrentShipsCargo.Items.Any())
                {
                    _isItSafeToKeepLoadingLoot_AntiGank = true;
                    return _isItSafeToKeepLoadingLoot_AntiGank;
                }

                if (300_000_000 > ESCache.Instance.CurrentShipsCargo.Items.Sum(i => i.AveragePrice() * Math.Max(1, i.Quantity)))
                {
                    _isItSafeToKeepLoadingLoot_AntiGank = true;
                    return _isItSafeToKeepLoadingLoot_AntiGank;
                }

                return false;
            }
        }

        private void LoadSurveys()
        {

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

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log("Current ships cargo is null.");
                return;
            }

            var itemHangar = ESCache.Instance.DirectEve.GetItemHangar();

            if (itemHangar.Items.Any(e => e.TypeId == (int)TypeID.TriglavianSurveyDatabase))
            {

                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("LoadSurveysToCargo"))
                    return;

                if (ESCache.Instance.CurrentShipsCargo.Add(ESCache.Instance.DirectEve.GetItemHangar().Items.Where(e => e.TypeId == (int)TypeID.TriglavianSurveyDatabase)))
                {
                    Log("Moving Triglavian Survey Databases into cargo.");
                    LocalPulse = UTCNowAddMilliseconds(3000, 3500);
                    return;
                }

                return;
            }
            else
            {
                Log("No Triglavian Survey Databases found in itemhangar");
            }

            Log("Checking for Surveys in Cargo");
            if (ESCache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId == (int)TypeID.TriglavianSurveyDatabase))
            {
                Log("Found Surveys in Cargo");
                if (_dataSurveyDumpState == DataSurveyDumpState.LoadSurveys)
                {
                    Log("if (_dataSurveyDumpState == DataSurveyDumpState.LoadSurveysAndLootToCargo)");
                    var fbmx = ESCache.Instance.CachedBookmarks.Where(i => i.BookmarkType == BookmarkType.Station).OrderByDescending(e => e.IsInCurrentSystem).FirstOrDefault(b => b.Title.ToLower().Contains(_surveyDumpBookmarkName.ToLower()));
                    if (fbmx == null)
                    {
                        _state = AbyssalState.Error;
                        Log($"No bookmark found containing: surveyDumpBookmarkName [ {_surveyDumpBookmarkName} ] We need a bookmark to the station we want to sell the surveys in");
                        return;
                    }

                    Log("Found bookmark: [" + fbmx.Title + "]: TravelToDumpSurveysStation");
                    _dataSurveyDumpState = DataSurveyDumpState.TravelToDumpSurveysStation;
                    _travelerDestination = new BookmarkDestination(fbmx);
                    return;
                }
            }

            Log("No Surveys Found in Cargo: Going Home");
            myAbyssalState = AbyssalState.TravelToHomeLocation;
        }

        private void LoadLoot()
        {

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

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log("Current ships cargo is null.");
                return;
            }

            var itemHangar = ESCache.Instance.DirectEve.GetItemHangar();

            Log("Checking for Loot in ItemHangar");
            if (itemHangar.Items.Any(e => e.TypeId != (int)_filamentTypeId && e.IsAbyssalLootToSell) && 300_000_000 > ESCache.Instance.CurrentShipsCargo.Items.Sum(i => i.AveragePrice()))
            {

                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("LoadLootToCargo"))
                    return;

                Log("Found [" + itemHangar.Items.Count(e => e.TypeId != (int)_filamentTypeId && e.IsAbyssalLootToSell) + "] Loot items in ItemHangar worth [" + itemHangar.Items.Where(e => e.TypeId != (int)_filamentTypeId && e.IsAbyssalLootToSell).Sum(i => i.AveragePrice() * Math.Max(1, i.Quantity)) + "]");
                if (ESCache.Instance.DirectEve.GetItemHangar().Items.Any(e => e.IsBlueprintCopy) && ESCache.Instance.CurrentShipsCargo.Items.Any() && 20 > ESCache.Instance.CurrentShipsCargo.Items.Count(i => i.IsBlueprintCopy))
                {
                    foreach (var item in ESCache.Instance.DirectEve.GetItemHangar().Items.Where(e => e.IsBlueprintCopy).RandomPermutation())
                    {
                        if (ESCache.Instance.CurrentShipsCargo.FreeCapacity > item.TotalVolume)
                        {
                            ESCache.Instance.CurrentShipsCargo.Add(item);
                            Log("Moving Blueprint [" + item.TypeName + "] x [" + item.Quantity + "][" + item.TotalVolume + " m3] into cargo.");
                            LocalPulse = UTCNowAddMilliseconds(3000, 3500);
                            return;
                        }
                    }
                }

                foreach (var item in ESCache.Instance.DirectEve.GetItemHangar().Items.Where(e => e.TypeId != (int)_filamentTypeId && e.IsAbyssalLootToSell).RandomPermutation())
                {
                    //if (!DirectEve.Interval(200))
                    //    return; ;

                    if (IsItSafeToKeepLoadingLoot_AntiGank != null && (bool)IsItSafeToKeepLoadingLoot_AntiGank)
                    {
                        if (DebugConfig.DebugUnloadLoot) Log("IsItSafeToKeepLoadingLoot_AntiGank [true]");
                        if (ESCache.Instance.CurrentShipsCargo.FreeCapacity > 0)
                        {
                            Log("CurrentShipsCargo.FreeCapacity [" + Math.Round((double)ESCache.Instance.CurrentShipsCargo.FreeCapacity, 2) + "]");
                            if (item.IsBlueprintCopy && ESCache.Instance.CurrentShipsCargo.Items.Count(i => i.IsBlueprintCopy) > 30)
                                continue;

                            if (ESCache.Instance.CurrentShipsCargo.FreeCapacity > item.TotalVolume)
                            {
                                Log("FreeCapacity [" + ESCache.Instance.CurrentShipsCargo.FreeCapacity + "] TotalVolume needed [" + item.TotalVolume + "]");
                                ESCache.Instance.CurrentShipsCargo.Add(item);
                                Log("Moving Loot item [" + item.TypeName + "] x [" + item.Quantity + "][" + item.TotalVolume + " m3] into cargo.");
                                LocalPulse = UTCNowAddMilliseconds(3000, 3500);
                                return;
                            }

                            var totalVolume = item.Quantity * item.Volume;
                            if (totalVolume > ESCache.Instance.CurrentShipsCargo.FreeCapacity)
                            {
                                // try to move it partially or skip if the volume of one is more than left free cargo
                                if (item.Volume > ESCache.Instance.CurrentShipsCargo.FreeCapacity)
                                {
                                    Log($"[ {item.TypeName} ][ {item.Volume} m3] > [ {ESCache.Instance.CurrentShipsCargo.FreeCapacity} m3]: We cant fit any more in our cargo");
                                    continue; // pick the next item in that case
                                }
                                else
                                {
                                    var quantityToMove = Convert.ToInt32(Math.Floor((double)ESCache.Instance.CurrentShipsCargo.FreeCapacity / item.Volume));
                                    Log($"Adding [ {item.TypeName} ] partially. Total quantity [ {item.Quantity} ] Moving quantity [ {quantityToMove} ]");
                                    if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("DumpLootController"))
                                        return;
                                    ESCache.Instance.CurrentShipsCargo.Add(item, quantityToMove);
                                    LocalPulse = UTCNowAddMilliseconds(3000, 3500);
                                    return;
                                }
                            }
                            else
                            {
                                // add item to the list
                                ESCache.Instance.CurrentShipsCargo.Add(item);
                                Log($"Added {item.TypeName} to CaregoHold");
                            }


                        }

                        Log("Done Loading Loot - No More m3: Cargo Value [" + ESCache.Instance.CurrentShipsCargo.Items.Sum(i => i.AveragePrice() * Math.Max(1, i.Quantity)) + "]");
                        break;
                    }

                    Log("Done Loading Loot - Cargo Value [" + ESCache.Instance.CurrentShipsCargo.Items.Sum(i => i.AveragePrice() * Math.Max(1, i.Quantity)) + "]");
                    break;
                }
            }
            else
            {
                Log("Done Loading Loot");
            }

            Log("Checking for loot in Cargo");
            if (ESCache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId != (int)_filamentTypeId && e.IsAbyssalLootToSell))
            {
                Log("Found loot in cargo");
                if (_dataSurveyDumpState == DataSurveyDumpState.LoadLootToCargo)
                {
                    //
                    // go to jita
                    //
                    _dataSurveyDumpState = DataSurveyDumpState.TravelToDumpSurveysStation;
                    _travelerDestination = new DockableLocationDestination(60003760);
                    return;
                }
            }

            Log("No Loot Found in Cargo: Going Home");
            myAbyssalState = AbyssalState.TravelToHomeLocation;
        }

        private void TravelToDumpStation()
        {

            if (ESCache.Instance.DirectEve.Session.IsInSpace && ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.IsWarpingByMode)
                return;

            if (Traveler.Destination != _travelerDestination)
                Traveler.Destination = _travelerDestination;

            Traveler.ProcessState();

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Log($"Arrived at {_surveyDumpBookmarkName}. Starting to dump the surveys.");
                _dataSurveyDumpState = DataSurveyDumpState.SellSurveysToNPCBuyOrders;

                Traveler.Destination = null;
                return;
            }

            if (State.CurrentTravelerState == TravelerState.Error)
            {
                if (Traveler.Destination != null)
                    Log("Stopped traveling, traveler threw an error.");

                Traveler.Destination = null;

                _state = AbyssalState.Error;
                _travelerDestination = null;
                return;
            }

        }
        private void DumpSurveys()
        {
            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log("ItemHangar is null.");
                return;
            }


            if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Capacity == 0)
            {
                Log("Current ships cargo is null.");
                return;
            }

            var shipsCargo = ESCache.Instance.CurrentShipsCargo;

            if (shipsCargo.Items.Any(i => i.TypeId == (int)TypeID.TriglavianSurveyDatabase))
            {
                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("Dump"))
                    return;
                if (ESCache.Instance.DirectEve.GetItemHangar().Add(shipsCargo.Items.Where(i => i.TypeId == (int)TypeID.TriglavianSurveyDatabase)))
                {
                    Log($"Moving CargoHold Items into itemhangar.");
                    LocalPulse = UTCNowAddMilliseconds(2000, 3500);
                    return;
                }

                return;
            }

            var loot2dump = ESCache.Instance.ItemHangar.Items.Where(i =>i.TypeId == (int)TypeID.TriglavianSurveyDatabase).ToList();
            if (loot2dump.Count > 10)
            {
                loot2dump = loot2dump.RandomPermutation().Take(4).ToList();
            }

            if (loot2dump.Any())
            {
                Log("loot2dump contains [" + loot2dump.Count() + "] stacks of items");
                var anyMultiSellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().Any();
                if (ESCache.Instance.SellError && anyMultiSellWnd)
                {
                    Log("Sell error, closing window and trying again.");
                    var sellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().FirstOrDefault();
                    sellWnd.Cancel();
                    errorCnt++;
                    LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                    ESCache.Instance.SellError = false;

                    if (errorCnt > 20)
                    {
                        Log($"Too many errors while dumping loot, error.");
                        _state = AbyssalState.Error;
                        return;
                    }

                    return;
                }

                if (!anyMultiSellWnd)
                {
                    Log($"Opening MultiSellWindow with {loot2dump.Count} items.");
                    ESCache.Instance.DirectEve.MultiSell(loot2dump);
                    LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                    _sellPerformed = false;
                    return;
                }
                else
                {
                    var sellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().FirstOrDefault();
                    if (sellWnd.AddingItemsThreadRunning)
                    {
                        Log($"Waiting for items to be added.");
                        LocalPulse = UTCNowAddMilliseconds(6000, 7000);
                        return;
                    }
                    else
                    {

                        if (sellWnd.GetDurationComboValue() != DurationComboValue.IMMEDIATE)
                        {
                            ESCache.Instance.PauseAfterNextDock = true;
                            Log($"Setting duration combo value to {DurationComboValue.IMMEDIATE}.");
                            Log($"Currently not working correctly, you need to select IMMEDIATE manually.");
                            sellWnd.SetDurationCombovalue(DurationComboValue.IMMEDIATE);
                            LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                            return;
                        }

                        if (sellWnd.GetSellItems().All(i => !i.HasBid))
                        {
                            Log($"Only items without a bid are left. Done. ");
                            sellWnd.Cancel();
                            LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                            _dataSurveyDumpState = DataSurveyDumpState.Done;
                            return;
                        }

                        if (sellWnd.GetSellItems().Any(i => 70 > i.PricePercentage))
                        {
                            var thisOrder = sellWnd.GetSellItems().Where(x => 70 > x.PricePercentage).FirstOrDefault();
                            if (thisOrder != null)
                            {
                                Log("[" + thisOrder.ItemName + "] 70% > PricePercentage [" + thisOrder.PricePercentage + "] not selling this item for that low of a price: removing item");
                                thisOrder.RemoveItem();
                                LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                                return;
                            }
                        }

                        if (_sellPerformed)
                        {
                            var secondsSince =
                                Math.Abs((DateTime.UtcNow - _sellPerformedDateTime).TotalSeconds);
                            Log($"We just performed a sell [{Math.Round(secondsSince, 0)}] seconds ago. Waiting for timeout.");
                            LocalPulse = UTCNowAddMilliseconds(1000, 2000);

                            if (secondsSince <= 16) return;

                            Log($"Timeout reached. Canceling the trade and changing to next state.");
                            sellWnd.Cancel();
                            LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                            _dataSurveyDumpState = DataSurveyDumpState.Done;
                            return;
                        }


                        Log($"Items added. Performing trade.");
                        sellWnd.PerformTrade();
                        _sellPerformed = true;
                        _sellPerformedDateTime = DateTime.UtcNow;
                        LocalPulse = UTCNowAddMilliseconds(2000, 4000);
                        return;

                    }
                }
            }
            else
            {
                Log($"Sold all items. Done");
                _dataSurveyDumpState = DataSurveyDumpState.Done;
                return;
            }

        }

        private void DumpLoot()
        {
            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log("ItemHangar is null.");
                return;
            }


            if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Capacity == 0)
            {
                Log("Current ships cargo is null.");
                return;
            }

            var shipsCargo = ESCache.Instance.CurrentShipsCargo;

            if (shipsCargo.Items.Any(e => e.TypeId != _filamentTypeId && e.IsAbyssalLootToSell))
            {
                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("Dump"))
                    return;
                if (ESCache.Instance.DirectEve.GetItemHangar().Add(shipsCargo.Items))
                {
                    Log($"Moving CargoHold Items into itemhangar.");
                    LocalPulse = UTCNowAddMilliseconds(2000, 3500);
                    return;
                }

                return;
            }

            var loot2dump = ESCache.Instance.ItemHangar.Items.Where(i => i.TypeId == (int)TypeID.TriglavianSurveyDatabase).ToList();
            if (loot2dump.Count > 10)
            {
                loot2dump = loot2dump.RandomPermutation().Take(4).ToList();
            }

            if (loot2dump.Any())
            {
                Log("loot2dump contains [" + loot2dump.Count() + "] stacks of items");
                var anyMultiSellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().Any();
                if (ESCache.Instance.SellError && anyMultiSellWnd)
                {
                    Log("Sell error, closing window and trying again.");
                    var sellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().FirstOrDefault();
                    sellWnd.Cancel();
                    errorCnt++;
                    LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                    ESCache.Instance.SellError = false;

                    if (errorCnt > 20)
                    {
                        Log($"Too many errors while dumping loot, error.");
                        _state = AbyssalState.Error;
                        return;
                    }

                    return;
                }

                if (!anyMultiSellWnd)
                {
                    Log($"Opening MultiSellWindow with {loot2dump.Count} items.");
                    ESCache.Instance.DirectEve.MultiSell(loot2dump);
                    LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                    _sellPerformed = false;
                    return;
                }
                else
                {
                    var sellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().FirstOrDefault();
                    if (sellWnd.AddingItemsThreadRunning)
                    {
                        Log($"Waiting for items to be added.");
                        LocalPulse = UTCNowAddMilliseconds(6000, 7000);
                        return;
                    }
                    else
                    {

                        if (sellWnd.GetDurationComboValue() != DurationComboValue.IMMEDIATE)
                        {
                            ESCache.Instance.PauseAfterNextDock = true;
                            Log($"Setting duration combo value to {DurationComboValue.IMMEDIATE}.");
                            Log($"Currently not working correctly, you need to select IMMEDIATE manually.");
                            sellWnd.SetDurationCombovalue(DurationComboValue.IMMEDIATE);
                            LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                            return;
                        }

                        if (sellWnd.GetSellItems().All(i => !i.HasBid))
                        {
                            Log($"Only items without a bid are left. Done. ");
                            sellWnd.Cancel();
                            LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                            _dataSurveyDumpState = DataSurveyDumpState.Done;
                            return;
                        }

                        if (sellWnd.GetSellItems().Any(i => 70 > i.PricePercentage))
                        {
                            var thisOrder = sellWnd.GetSellItems().Where(x => 70 > x.PricePercentage).FirstOrDefault();
                            if (thisOrder != null)
                            {
                                Log("[" + thisOrder.ItemName + "] 70% > PricePercentage [" + thisOrder.PricePercentage + "] not selling this item for that low of a price: removing item");
                                thisOrder.RemoveItem();
                                LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                                return;
                            }
                        }

                        if (_sellPerformed)
                        {
                            var secondsSince =
                                Math.Abs((DateTime.UtcNow - _sellPerformedDateTime).TotalSeconds);
                            Log($"We just performed a sell [{Math.Round(secondsSince, 0)}] seconds ago. Waiting for timeout.");
                            LocalPulse = UTCNowAddMilliseconds(1000, 2000);

                            if (secondsSince <= 16) return;

                            Log($"Timeout reached. Canceling the trade and changing to next state.");
                            sellWnd.Cancel();
                            LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                            _dataSurveyDumpState = DataSurveyDumpState.Done;
                            return;
                        }


                        Log($"Items added. Performing trade.");
                        sellWnd.PerformTrade();
                        _sellPerformed = true;
                        _sellPerformedDateTime = DateTime.UtcNow;
                        LocalPulse = UTCNowAddMilliseconds(2000, 4000);
                        return;

                    }
                }
            }
            else
            {
                Log($"Sold all items. Done");
                _dataSurveyDumpState = DataSurveyDumpState.Done;
                return;
            }

        }

    }
}
