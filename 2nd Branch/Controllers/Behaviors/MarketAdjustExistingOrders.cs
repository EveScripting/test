extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Py;

namespace EVESharpCore.Questor.Behaviors
{
    public class MarketAdjustBehavior
    {
        #region Constructors

        public MarketAdjustBehavior()
        {
            ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.Idle);
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        private static Dictionary<long, double> _myOriginalMarketOrderPrice = new Dictionary<long, double>();
        private const int OrderModificationDelayInMinutes = 30;
        private static int? _buyOrderRandomSmallPercentage;
        private static DateTime _lastStateChange;
        private static DateTime _nextMarketOrdersUpdate = DateTime.UtcNow.AddDays(-1);
        private static DateTime _nextPulse = DateTime.MinValue;

        private static int? _sellOrderRandomSmallPercentage;
        private static int _buyOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder = 50;
        private static int _buyOrderDoNotAdjustOrdersAboveThisPercentagePerChange = 30;
        private static int _buyOrderRandomSmallPercentageHigh = 20;

        private static int _buyOrderRandomSmallPercentageLow = 5;

        private static bool _checkBuyOrders;

        private static bool _checkSellOrders;

        private static IEnumerable<DirectOrder> _myBuyOrders;

        private static IEnumerable<DirectOrder> _mySellOrders;

        private static int _publicOrdersWithThisLowVolumeRemainingAreIgnored = 1;

        private static int _sellOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder = 50;

        private static int _sellOrderDoNotAdjustOrdersAboveThisPercentagePerChange = 30;

        private static int _sellOrderRandomSmallPercentageHigh = 20;

        private static int _sellOrderRandomSmallPercentageLow = 5;

        #endregion Fields

        #region Properties

        private static int BuyOrderRandomSmallPercentage
        {
            get
            {
                if (_buyOrderRandomSmallPercentage == null)
                {
                    _buyOrderRandomSmallPercentage = ESCache.Instance.RandomNumber(_buyOrderRandomSmallPercentageLow, _buyOrderRandomSmallPercentageHigh);
                    return (int)_buyOrderRandomSmallPercentage;
                }

                return (int)_buyOrderRandomSmallPercentage;
            }
        }

        private static DirectMarketWindow MarketWindow => ESCache.Instance.DirectEve.GetMarketWindow();

        private static bool IsLowestPriceOrderInIsk(DirectOrder myOrder)
        {
            return false;
        }

        private static bool IsLowestPriceTheOrderNeedsToBe
        {
            get
            {
                //todo: fixme
                return false;
            }
        }

        private static int SellOrderRandomSmallPercentage
        {
            get
            {
                if (_sellOrderRandomSmallPercentage == null)
                {
                    _sellOrderRandomSmallPercentage = ESCache.Instance.RandomNumber(_sellOrderRandomSmallPercentageLow, _sellOrderRandomSmallPercentageHigh);
                    return (int)_sellOrderRandomSmallPercentage;
                }

                return (int)_sellOrderRandomSmallPercentage;
            }
        }

        #endregion Properties

        #region Methods

        private static bool ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState stateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentMarketAdjustBehaviorState != stateToSet)
                {
                    Log.WriteLine("New MarketAdjustBehaviorState [" + stateToSet + "]");
                    State.CurrentMarketAdjustBehaviorState = stateToSet;
                    _lastStateChange = DateTime.UtcNow;
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

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            _checkSellOrders =
                (bool?)CharacterSettingsXml.Element("CheckSellOrders") ??
                (bool?)CommonSettingsXml.Element("CheckSellOrders") ?? false;

            _checkBuyOrders =
                (bool?)CharacterSettingsXml.Element("CheckBuyOrders") ??
                (bool?)CommonSettingsXml.Element("CheckBuyOrders") ?? false;

            _publicOrdersWithThisLowVolumeRemainingAreIgnored =
                (int?)CharacterSettingsXml.Element("PublicOrdersWithThisLowVolumeRemainingAreIgnored") ??
                (int?)CommonSettingsXml.Element("PublicOrdersWithThisLowVolumeRemainingAreIgnored") ?? 1;

            _sellOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder =
                (int?)CharacterSettingsXml.Element("SellOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder") ??
                (int?)CommonSettingsXml.Element("SellOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder") ?? 30;

            _sellOrderDoNotAdjustOrdersAboveThisPercentagePerChange =
                (int?)CharacterSettingsXml.Element("SellOrderDoNotAdjustOrdersAboveThisPercentagePerChange") ??
                (int?)CommonSettingsXml.Element("SellOrderDoNotAdjustOrdersAboveThisPercentagePerChange") ?? 30;

            _sellOrderRandomSmallPercentageLow =
                (int?)CharacterSettingsXml.Element("SellRandomSmallPercentageLow") ??
                (int?)CommonSettingsXml.Element("SellRandomSmallPercentageLow") ?? 15;

            _sellOrderRandomSmallPercentageHigh =
                (int?)CharacterSettingsXml.Element("SellRandomSmallPercentageHigh") ??
                (int?)CommonSettingsXml.Element("SellRandomSmallPercentageHigh") ?? 30;

            _buyOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder =
                (int?)CharacterSettingsXml.Element("BuyOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder") ??
                (int?)CommonSettingsXml.Element("BuyOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder") ?? 35;

            _buyOrderDoNotAdjustOrdersAboveThisPercentagePerChange =
                (int?)CharacterSettingsXml.Element("BuyOrderDoNotAdjustOrdersAboveThisPercentagePerChange") ??
                (int?)CommonSettingsXml.Element("BuyOrderDoNotAdjustOrdersAboveThisPercentagePerChange") ?? 30;

            _buyOrderRandomSmallPercentageLow =
                (int?)CharacterSettingsXml.Element("BuyOrderRandomSmallPercentageLow") ??
                (int?)CommonSettingsXml.Element("BuyOrderRandomSmallPercentageLow") ?? 15;

            _buyOrderRandomSmallPercentageHigh =
                (int?)CharacterSettingsXml.Element("BuyOrderRandomSmallPercentageHigh") ??
                (int?)CommonSettingsXml.Element("BuyOrderRandomSmallPercentageHigh") ?? 30;
        }

        //https://wiki.eveuniversity.org/Trading
        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugMarketOrders) Log.WriteLine("State.MarketAdjustBehaviorState is [" + State.CurrentMarketAdjustBehaviorState + "]");

                switch (State.CurrentMarketAdjustBehaviorState)
                {
                    case MarketAdjustBehaviorState.Default:
                        ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.Idle);
                        break;

                    case MarketAdjustBehaviorState.Idle:
                        IdleState();
                        break;

                    case MarketAdjustBehaviorState.Start:
                        StartState();
                        break;

                    case MarketAdjustBehaviorState.WaitForNextMarketUpdate:
                        WaitForNextMarketUpdateState();
                        break;

                    case MarketAdjustBehaviorState.LoadOrdersBeforeProcessingSellOrders:
                        LoadOrdersBeforeProcessingSellOrdersState();
                        break;

                    case MarketAdjustBehaviorState.PullListOfMySellOrders:
                        PullMySellOrdersState();
                        break;

                    case MarketAdjustBehaviorState.CheckSellOrders:
                        CheckSellOrdersState();
                        break;

                    case MarketAdjustBehaviorState.LoadOrdersBeforeProcessingBuyOrders:
                        LoadOrdersBeforeProcessingBuyOrdersState();
                        break;

                    case MarketAdjustBehaviorState.PullListOfMyBuyOrders:
                        PullMyBuyOrdersState();
                        break;

                    case MarketAdjustBehaviorState.CheckBuyOrders:
                        CheckBuyOrdersState();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private double MinimumPercentageOfMyOrderQuantityToConsiderAnOrderCompetition(DirectOrder myOrder)
        {
            if (50000 > myOrder.Price)
            {
                return .10;
            }

            if (150000 > myOrder.Price)
            {
                return .10;
            }
            //
            // etc
            //

            return .10;
        }

        private int MinVolumeToConsiderACompetingOrderCompetition(DirectOrder myOrder)
        {
            return (int)Math.Max(Math.Round((double)myOrder.VolumeRemaining * MinimumPercentageOfMyOrderQuantityToConsiderAnOrderCompetition(myOrder), 0), 1);
        }

        private IEnumerable<DirectOrder> AllSellOrdersIncludingMine(DirectOrder mySellOrder)
        {
            if (MarketWindow == null) return new List<DirectOrder>();

            return MarketWindow.SellOrders.Where(o =>
                    o.StationId == ESCache.Instance.DirectEve.Session.StationId &&
                    o.TypeId == mySellOrder.TypeId &&
                    o.VolumeRemaining >= MinVolumeToConsiderACompetingOrderCompetition(mySellOrder))
                .ToList();
        }

        private IEnumerable<DirectOrder> AllBuyOrdersIncludingMine(DirectOrder myBuyOrder)
        {
            if (MarketWindow == null) return new List<DirectOrder>();

            return MarketWindow.BuyOrders.Where(o =>
                    o.StationId == ESCache.Instance.DirectEve.Session.StationId &&
                    o.TypeId == myBuyOrder.TypeId &&
                    o.VolumeRemaining >= MinVolumeToConsiderACompetingOrderCompetition(myBuyOrder))
                .ToList();
        }

        private static void CheckBuyOrdersState()
        {
            if (!_checkBuyOrders)
                ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.LoadOrdersBeforeProcessingSellOrders);

            if (_nextMarketOrdersUpdate > DateTime.UtcNow)
            {
                if (DebugConfig.DebugMarketOrders) Log.WriteLine("CheckBuyOrdersMAEOState: Next Market Orders check will be delayed until [" + _nextMarketOrdersUpdate.ToShortTimeString() + "]");
                return;
            }

            const int ordersWithThisLowVolumeRemainingAreIgnored = 2;
            if (MarketWindow == null)
                return;

            if (_myBuyOrders == null)
                return;

            if (_myBuyOrders.Any())
            {
                int buyOrderNumber = 0;
                foreach (DirectOrder myBuyOrder in _myBuyOrders)
                {
                    if (!MarketWindow.IsReady)
                        return;

                    if (ordersWithThisLowVolumeRemainingAreIgnored >= myBuyOrder.VolumeRemaining)
                        continue;

                    buyOrderNumber++;

                    if (_myOriginalMarketOrderPrice != null)
                    {
                        if (_myOriginalMarketOrderPrice.Any(i => i.Key != myBuyOrder.OrderId))
                            _myOriginalMarketOrderPrice.Add(myBuyOrder.OrderId, myBuyOrder.Price);
                    }
                    else
                    {
                        _myOriginalMarketOrderPrice = new Dictionary<long, double>();
                    }

                    bool? tempBool = HasItBeenLongEnoughSinceOurLastCheckOfThisOrder(myBuyOrder, buyOrderNumber);
                    if (tempBool == null) return; //wait
                    if (!(bool)tempBool) continue; //process next order

                    // Are there any orders with an reasonable price?

                    //todo: fixme

                    //
                    // so that we do not have to calc skills only deal with orders made in the local station
                    //
                    if (myBuyOrder.StationId == ESCache.Instance.DirectEve.Session.StationId)
                    {
                        if (MarketWindow.DetailTypeId != myBuyOrder.TypeId)
                        {
                            // No, load the orders for this typeid
                            if (MarketWindow.LoadTypeId(myBuyOrder.TypeId))
                            {
                                Log.WriteLine("Loading market orders for [" + myBuyOrder.TypeName + "] typeId [" + myBuyOrder.TypeId + "] Our order has [" + myBuyOrder.VolumeRemaining + "] volumeRemaining. Priced at [" + myBuyOrder.Price + "] currently.");
                                return;
                            }

                            return;
                        }

                        /**
                        //
                        // is my order the lowest priced sell order?
                        //
                        if (AllSellOrdersIncludingMine.Any())
                        {
                            Log.WriteLine("MySellOrder: OrderId [" + myBuyOrder.OrderId + "] for [" + myBuyOrder.TypeName + "] Quantity left [" + myBuyOrder.VolumeRemaining + "] [" + myBuyOrder.Price + "]");

                            DirectOrder publicBuyOrderHigherThanMine = allSellOrdersIncludingMine.OrderByDescending(i => i.Price).FirstOrDefault(i => i.Price > myBuyOrder.Price && i.OrderId != myBuyOrder.OrderId);

                            if (publicBuyOrderHigherThanMine != null)
                            {
                                Log.WriteLine("publicBuyOrder: OrderId [" + publicBuyOrderHigherThanMine.OrderId + "] for [" + publicBuyOrderHigherThanMine.TypeName + "] Quantity left [" + publicBuyOrderHigherThanMine.VolumeRemaining + "] [" + publicBuyOrderHigherThanMine.Price + "] is priced below my order");

                                //
                                // Decide if the price we would adjust is "too high" or not
                                // Do not adjust price if we are increasing the buy price more than 10% of where we are now
                                //if (myBuyOrder.Price * 1.1 > publicBuyOrderHigherThanMine.Price)
                                if (!IsHighestPublicBuyOrderWithinPercentage(myBuyOrder, publicBuyOrderHigherThanMine, BuyOrderDoNotAdjustOrdersAboveThisPercentagePerChange, buyOrderNumber)) continue;
                                if (!IsHighestPublicBuyOrderWithinPercentage(myBuyOrder, publicBuyOrderHigherThanMine, BuyOrderRandomSmallPercentage, buyOrderNumber)) continue;

                                if (MyOriginalMarketOrderPrice[myBuyOrder.OrderId] * (BuyOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder / 100 + 1) > publicBuyOrderHigherThanMine.Price)
                                {
                                    //
                                    // Change the price
                                    //
                                    double newPrice = publicBuyOrderHigherThanMine.Price + .01;

                                    if (ESCache.Instance.DirectEve.Me.Wealth < 100)
                                    {
                                        Log.WriteLine("We cant adjust [" + myBuyOrder.TypeName + "] from [" + myBuyOrder.Price + "] to [" + newPrice + " ] because we dont have the brokers fee in our local wallet of the 100 isk needed!");
                                        Time.Instance.LastMarketOrderAdjustmentTimeStamp[myBuyOrder.OrderId] = DateTime.UtcNow;
                                        return;
                                    }

                                    if (myBuyOrder.ModifyOrder(newPrice))
                                    {
                                        Log.WriteLine("Adjust my buy Order for [" + myBuyOrder.TypeName + "] from [" + myBuyOrder.Price + "] to [" + newPrice + " ]");
                                        Time.Instance.LastMarketOrderAdjustmentTimeStamp[myBuyOrder.OrderId] = DateTime.UtcNow;
                                        return;
                                    }
                                }

                                if (DebugConfig.DebugMarketOrders) Log.WriteLine("if (MyOriginalMarketOrderPrice[myBuyOrder.OrderId] * 1.25 > publicBuyOrderHigherThanMine.Price)");
                            }
                        }
                        **/
                    }
                }
            }

            int randomDelayinMinutes = ESCache.Instance.RandomNumber(17, 35);
            _nextMarketOrdersUpdate = DateTime.UtcNow.AddMinutes(randomDelayinMinutes);
            Log.WriteLine("CheckSellOrdersMAEOState: Next Market Orders check will be delayed for [" + randomDelayinMinutes + "] minutes");
            ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.WaitForNextMarketUpdate);
        }

        private static void CheckSellOrdersState()
        {
            if (!_checkSellOrders)
                ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.LoadOrdersBeforeProcessingBuyOrders);

            if (_nextMarketOrdersUpdate > DateTime.UtcNow)
            {
                if (DebugConfig.DebugMarketOrders) Log.WriteLine("CheckSellOrdersMAEOState: Next Market Orders check will be delayed until [" + _nextMarketOrdersUpdate.ToShortTimeString() + "]");
                return;
            }

            if (MarketWindow == null)
            {
                if (DebugConfig.DebugMarketOrders) Log.WriteLine("CheckSellOrdersMAEOState: if (MarketWindow == null)");
                return;
            }

            if (_mySellOrders == null)
            {
                if (DebugConfig.DebugMarketOrders) Log.WriteLine("CheckSellOrdersMAEOState: if (mySellOrders == null)");
                return;
            }

            if (DebugConfig.DebugMarketOrders) Log.WriteLine("CheckSellOrdersMAEOState: We have [" + _mySellOrders.Count() + "] Sell Orders");

            if (_mySellOrders != null && _mySellOrders.Any())
            {
                int sellOrderNumber = 0;
                foreach (DirectOrder mySellOrder in _mySellOrders)
                {
                    sellOrderNumber++;
                    if (!MarketWindow.IsReady)
                    {
                        if (DebugConfig.DebugMarketOrders) Log.WriteLine("[" + sellOrderNumber + "] CheckSellOrdersState: if (!MarketWindow.IsReady)");
                        return;
                    }

                    /**
                    if (MyOriginalMarketOrderPrice != null)
                    {
                        if (MyOriginalMarketOrderPrice.All(i => i.Key != mySellOrder.OrderId))
                        {
                            Log.WriteLine("[" + sellOrderNumber + "] Adding this order info to MyOriginalMarketOrderPrice");
                            MyOriginalMarketOrderPrice.Add(mySellOrder.OrderId, mySellOrder.Price);
                        }
                    }
                    else
                    {
                        MyOriginalMarketOrderPrice = new Dictionary<long, double>();
                        return;
                    }
                    **/

                    bool? tempBool = HasItBeenLongEnoughSinceOurLastCheckOfThisOrder(mySellOrder, sellOrderNumber);
                    if (tempBool == null) return; //wait
                    if (!(bool)tempBool) continue; //process next order

                    if (MarketWindow.DetailTypeId != mySellOrder.TypeId)
                    {
                        // No, load the orders for this typeid
                        if (MarketWindow.LoadTypeId(mySellOrder.TypeId))
                        {
                            Log.WriteLine("[" + sellOrderNumber + "] Loading market orders for [" + mySellOrder.TypeName + "] typeId [" + mySellOrder.TypeId + "] Our order has [" + mySellOrder.VolumeRemaining + "] volumeRemaining. Priced at [" + mySellOrder.Price + "] currently.");
                            return;
                        }

                        return;
                    }

                    // Are there any orders with an reasonable price?
                    IOrderedEnumerable<DirectOrder> allSellOrdersLowerThanMyOrder = MarketWindow.SellOrders.Where(o =>
                        o.StationId == ESCache.Instance.DirectEve.Session.StationId &&
                        o.TypeId == mySellOrder.TypeId &&
                        mySellOrder.Price > o.Price &&
                        _mySellOrders.All(order => order.OrderId != o.OrderId) &&
                        o.VolumeRemaining > _publicOrdersWithThisLowVolumeRemainingAreIgnored).OrderByDescending(i => i.Price);

                    //
                    // so that we do not have to calc skills only deal with orders made in the local station
                    //
                    if (mySellOrder.StationId == ESCache.Instance.DirectEve.Session.StationId)
                        if (allSellOrdersLowerThanMyOrder != null && allSellOrdersLowerThanMyOrder.Any())
                        {
                            Log.WriteLine("[" + sellOrderNumber + "] MySellOrder [" + mySellOrder.TypeName + "] OrderId [" + mySellOrder.OrderId + "] Quantity left [" + mySellOrder.VolumeRemaining + "] [" + mySellOrder.Price + "]");

                            DirectOrder publicSellOrderLowerThanMine = allSellOrdersLowerThanMyOrder.FirstOrDefault();
                            int HowManyOrdersBetweenMineAndLowset = 0;
                            int HowManyOrdersAreLessThan1PercentLower = 0;
                            int HowManyOrdersAreLessThan5PercentLower = 0;
                            int HowManyOrdersAreLessThan10PercentLower = 0;
                            int HowManyOrdersAreLessThan20PercentLower = 0;
                            int HowManyOrdersAreLessThan30PercentLower = 0;
                            int HowManyOrdersAreLessThan40PercentLower = 0;
                            int HowManyOrdersAreLessThan50PercentLower = 0;
                            int HowManyOrdersAreMoreThan50PercentLower = 0;

                            if (publicSellOrderLowerThanMine != null)
                            {
                                foreach (DirectOrder thisPublicSellOrder in allSellOrdersLowerThanMyOrder)
                                {
                                    HowManyOrdersBetweenMineAndLowset++;
                                    double percentageDifference = 100 - (thisPublicSellOrder.Price / mySellOrder.Price * 100);
                                    if (1 > percentageDifference)
                                        HowManyOrdersAreLessThan1PercentLower++;
                                    else if (5 > percentageDifference)
                                        HowManyOrdersAreLessThan5PercentLower++;
                                    else if (10 > percentageDifference)
                                        HowManyOrdersAreLessThan10PercentLower++;
                                    else if (20 > percentageDifference)
                                        HowManyOrdersAreLessThan20PercentLower++;
                                    else if (30 > percentageDifference)
                                        HowManyOrdersAreLessThan30PercentLower++;
                                    else if (40 > percentageDifference)
                                        HowManyOrdersAreLessThan40PercentLower++;
                                    else if (50 > percentageDifference)
                                        HowManyOrdersAreLessThan50PercentLower++;
                                    else if (percentageDifference > 50)
                                        HowManyOrdersAreMoreThan50PercentLower++;
                                }

                                Log.WriteLine("[" + sellOrderNumber + "]     publicSellOrder: [" + publicSellOrderLowerThanMine.TypeName + "] OrderId [" + publicSellOrderLowerThanMine.OrderId + "] Quantity left [" + publicSellOrderLowerThanMine.VolumeRemaining + "] [" + publicSellOrderLowerThanMine.Price + "] is priced below my order");

                                //
                                // Decide if the price we would adjust is "too low" or not
                                // Do not adjust price is we are reducing price 20% or more below where we are now
                                if (!IsLowestPublicSellOrderWithinPercentage(mySellOrder, publicSellOrderLowerThanMine, _sellOrderDoNotAdjustOrdersAboveThisPercentagePerChange, sellOrderNumber)) continue;
                                if (!IsLowestPublicSellOrderWithinPercentage(mySellOrder, publicSellOrderLowerThanMine, SellOrderRandomSmallPercentage, sellOrderNumber)) continue;

                                //
                                // If my original order (not necessarily what my current price is set at) * 75% is still higher than the price I will be adjusting to beat then adjust.
                                //
                                //if (MyOriginalMarketOrderPrice[mySellOrder.OrderId] * (SellOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder / 100) > publicSellOrderLowerThanMine.Price)
                                {
                                    //
                                    // Change the price
                                    //
                                    double newPrice = publicSellOrderLowerThanMine.Price - .01;
                                    if ((ESCache.Instance.DirectEve.Me.Wealth ?? 0) < 100)
                                    {
                                        Log.WriteLine("[" + sellOrderNumber + "] We cant adjust: from [" + mySellOrder.Price + "] to [" + newPrice + " ] because we dont have the brokers fee in our local wallet of the 100 isk needed! mySellOrder: Item: [" + mySellOrder.TypeName + "] ");
                                        Time.Instance.LastMarketOrderAdjustmentTimeStamp[mySellOrder.OrderId] = DateTime.UtcNow;
                                        return;
                                    }

                                    if (mySellOrder.ModifyOrder(newPrice))
                                    {
                                        Log.WriteLine("[" + sellOrderNumber + "] Adjust Order from [" + mySellOrder.Price + "] to [" + newPrice + " ] mySellOrder Item: [" + mySellOrder.TypeName + "]");
                                        Time.Instance.LastMarketOrderAdjustmentTimeStamp[mySellOrder.OrderId] = DateTime.UtcNow;
                                        return;
                                    }

                                    Log.WriteLine("[" + sellOrderNumber + "] Modifying the order from [" + mySellOrder.Price + "] to [" + newPrice + " ] mySellOrder Item: [" + mySellOrder.TypeName + "] failed?");
                                    return;
                                }

                                //Time.Instance.LastMarketOrderAdjustmentTimeStamp[mySellOrder.OrderId] = DateTime.UtcNow;
                                //Log.WriteLine("[" + sellOrderNumber + "] Do Not Adjust: Original Price modified more than [" + SellOrderDoNotAdjustOrdersAboveThisPercentageOverTheLifeOfTheOrder + "]% rule ] mySellOrder Item: [" + mySellOrder.TypeName + "]");
                                //continue;
                            }
                        }
                }
            }

            int randomDelayinMinutes = ESCache.Instance.RandomNumber(17, 35);
            _nextMarketOrdersUpdate = DateTime.UtcNow.AddMinutes(randomDelayinMinutes);
            Log.WriteLine("CheckSellOrdersMAEOState: Next Market Orders check will be delayed for [" + randomDelayinMinutes + "] minutes");
            ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.LoadOrdersBeforeProcessingBuyOrders);
        }

        private static bool EveryPulse()
        {
            if (_nextPulse > DateTime.UtcNow)
                return false;

            _nextPulse = DateTime.UtcNow.AddSeconds(3);

            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return false;

            if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
            {
                if (DebugConfig.DebugMarketOrders) Log.WriteLine("MarketAdjutExistingOrdersController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                ControllerManager.Instance.SetPause(true);
                ESCache.Instance.PauseAfterNextDock = false;
                return false;
            }

            return true;
        }

        private static bool? HasItBeenLongEnoughSinceOurLastCheckOfThisOrder(DirectOrder myOrder, int OrderNumber)
        {
            if (Time.Instance.LastMarketOrderAdjustmentTimeStamp != null)
            {
                if (Time.Instance.LastMarketOrderAdjustmentTimeStamp.Any(i => DateTime.UtcNow < i.Value.AddSeconds(7)))
                {
                    if (DebugConfig.DebugMarketOrders) Log.WriteLine("[" + myOrder + "] We have modified an order recently. waiting.");
                    return null;
                }

                if (Time.Instance.LastMarketOrderAdjustmentTimeStamp.ContainsKey(myOrder.OrderId))
                    if (DateTime.UtcNow < Time.Instance.LastMarketOrderAdjustmentTimeStamp[myOrder.OrderId].AddMinutes(OrderModificationDelayInMinutes))
                    {
                        if (DebugConfig.DebugMarketOrders) Log.WriteLine("[" + myOrder + "] MyOrder [" + myOrder.TypeName + "] OrderId [" + myOrder.OrderId + "] Quantity left [" + myOrder.VolumeRemaining + "] [" + myOrder.Price + "] has been recently adjusted: process the next order");
                        return false;
                    }

                if (Time.Instance.LastMarketOrderCheckTimeStamp.ContainsKey(myOrder.OrderId))
                    if (DateTime.UtcNow < Time.Instance.LastMarketOrderCheckTimeStamp[myOrder.OrderId].AddMinutes(OrderModificationDelayInMinutes))
                    {
                        if (DebugConfig.DebugMarketOrders) Log.WriteLine("[" + OrderNumber + "] MyOrder [" + myOrder.TypeName + "] OrderId [" + myOrder.OrderId + "] Quantity left [" + myOrder.VolumeRemaining + "] [" + myOrder.Price + "] has been recently adjusted: process the next order");
                        return false;
                    }
            }
            else
            {
                Time.Instance.LastMarketOrderAdjustmentTimeStamp = new Dictionary<long, DateTime>();
            }

            return true;
        }

        private static void IdleState()
        {
            if (ESCache.Instance.InSpace)
                return;

            ResetStatesToDefaults();

            ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.Start, false);
        }

        private static bool IsHighestPublicBuyOrderWithinPercentage(DirectOrder myBuyOrder, DirectOrder publicBuyOrderHigherThanMine, int thisPercentage, int buyOrderNumber)
        {
            if (thisPercentage <= 0)
            {
                Log.WriteLine("[" + buyOrderNumber + "] Buy: Do Not Adjust: if (thisPercentage <= 0)");
                return false;
            }

            if (myBuyOrder == null)
            {
                Log.WriteLine("[" + buyOrderNumber + "] Buy: Do Not Adjust: if (myBuyOrder == null)");
                return false;
            }

            if (myBuyOrder.Price == 0)
            {
                Log.WriteLine("[" + buyOrderNumber + "] Buy: Do Not Adjust: if (myBuyOrder.Price == 0)");
                return false;
            }

            if (publicBuyOrderHigherThanMine.Price == 0)
            {
                Log.WriteLine("[" + buyOrderNumber + "] Buy: Do Not Adjust: if (publicBuyOrderHigherThanMine.Price == 0)");
                return false;
            }

            double percentageDifference = Math.Round(100 - (myBuyOrder.Price / publicBuyOrderHigherThanMine.Price * 100), 2);
            if (thisPercentage > percentageDifference)
                return true;

            Log.WriteLine("[" + buyOrderNumber + "] Buy: Do Not Adjust: if  " + thisPercentage + " > " + percentageDifference + " [ Modify order more than [" + thisPercentage + "]% rule ] Item: [" + myBuyOrder.TypeName + "]");
            Time.Instance.LastMarketOrderCheckTimeStamp[myBuyOrder.OrderId] = DateTime.UtcNow;
            return false;
        }

        private static bool IsLowestPublicSellOrderWithinPercentage(DirectOrder mySellOrder, DirectOrder publicSellOrderLowerThanMine, int thisPercentage, int sellOrderNumber)
        {
            if (thisPercentage <= 0)
            {
                Log.WriteLine("[" + sellOrderNumber + "] Sell: Do Not Adjust: if (thisPercentage <= 0)");
                return false;
            }

            if (mySellOrder == null)
            {
                Log.WriteLine("[" + sellOrderNumber + "] Sell: Do Not Adjust: if (mySellOrder == null)");
                return false;
            }

            if (mySellOrder.Price == 0)
            {
                Log.WriteLine("[" + sellOrderNumber + "] Sell: Do Not Adjust: if (mySellOrder.Price == 0)");
                return false;
            }

            if (publicSellOrderLowerThanMine.Price == 0)
            {
                Log.WriteLine("[" + sellOrderNumber + "] Sell: Do Not Adjust: if (publicSellOrderLowerThanMine.Price == 0)");
                return false;
            }

            double percentageDifference = 100 - (publicSellOrderLowerThanMine.Price / mySellOrder.Price * 100);
            if (thisPercentage > percentageDifference)
                return true;

            Log.WriteLine("[" + sellOrderNumber + "] Sell: Do Not Adjust: if not thisPercentage [" + thisPercentage + "%] > percentageDifference [" + percentageDifference + "%] [ Modify order more than [" + thisPercentage + "]% rule ] Item: [" + mySellOrder.TypeName + "]");
            Time.Instance.LastMarketOrderCheckTimeStamp[mySellOrder.OrderId] = DateTime.UtcNow;
            return false;
        }

        private static void LoadOrdersBeforeProcessingBuyOrdersState()
        {
            if (MarketWindow == null)
                return;

            if (!MarketWindow.LoadOrders())
                return;

            if (DateTime.UtcNow > _lastStateChange.AddSeconds(5))
                ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.PullListOfMyBuyOrders);
        }

        private static void LoadOrdersBeforeProcessingSellOrdersState()
        {
            if (MarketWindow == null)
                return;

            if (!MarketWindow.LoadOrders())
                return;

            if (DateTime.UtcNow > _lastStateChange.AddSeconds(4))
                ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.PullListOfMySellOrders);
        }

        private static void OrderAttributes(DirectOrder order)
        {
            Log.WriteLine("---------------------------------------------------------");
            Log.WriteLine("SellOrder: OrderId [" + order.OrderId + "] Attributes");
            foreach (KeyValuePair<string, PyObject> attribute in order.PyOrder.Attributes())
                Log.WriteLine("Key: " + attribute.Key + " Value: " + attribute.Value);
            Log.WriteLine("---------------------------------------------------------");
        }

        private static void PullMyBuyOrdersState()
        {
            if (_myBuyOrders != null)
            {
                Log.WriteLine("PullMyBuyOrdersMAEOState: We have [" + _myBuyOrders.Count() + "] buy orders: _nextMarketOrdersUpdate [" + _nextMarketOrdersUpdate.ToShortTimeString() + "]");
                ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.CheckBuyOrders);
                return;
            }

            if (MarketWindow == null)
                return;

            const bool getBuyOrdersTrue = true;
            _myBuyOrders = MarketWindow.GetMyOrders(getBuyOrdersTrue);
        }

        private static void PullMySellOrdersState()
        {
            if (_mySellOrders != null)
            {
                Log.WriteLine("PullMySellOrdersMAEOState: We have [" + _mySellOrders.Count() + "] sell orders: _nextMarketOrdersUpdate [" + _nextMarketOrdersUpdate.ToShortTimeString() + "]");
                ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.CheckSellOrders);
                return;
            }

            if (MarketWindow == null)
                return;

            const bool GetBuyOrdersFalse = false;
            _mySellOrders = MarketWindow.GetMyOrders(GetBuyOrdersFalse);
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("MarketAdjustBehavior.ResetStatesToDefaults: start");
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentMarketAdjustBehaviorState = MarketAdjustBehaviorState.Idle;
            _myBuyOrders = null;
            _mySellOrders = null;
            _sellOrderRandomSmallPercentage = null;
            _buyOrderRandomSmallPercentage = null;
            Log.WriteLine("MarketAdjustBehavior.ResetStatesToDefaults: done");
            return;
        }

        private static void StartState()
        {
            if (ESCache.Instance.InSpace)
                return;

            if (ESCache.Instance.InStation)
                ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.WaitForNextMarketUpdate, false);
        }

        private static void WaitForNextMarketUpdateState()
        {
            if (DateTime.UtcNow > _nextMarketOrdersUpdate)
            {
                if (_checkSellOrders)
                {
                    ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.LoadOrdersBeforeProcessingSellOrders);
                    return;
                }

                if (_checkBuyOrders)
                {
                    ChangeMarketAdjustBehaviorState(MarketAdjustBehaviorState.LoadOrdersBeforeProcessingBuyOrders);
                    return;
                }

                _nextMarketOrdersUpdate = DateTime.UtcNow.AddMinutes(2);
                Log.WriteLine("WaitForNextMarketUpdateState: both CheckSellOrders and CheckBuyOrders settings are false, waiting 2 min.");
            }
        }

        #endregion Methods
    }
}