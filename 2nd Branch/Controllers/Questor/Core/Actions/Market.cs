// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using EVESharpCore.Framework;
using System.Collections.Generic;

namespace Questor.Modules.Actions
{
    public class Market
    {
        //private DateTime _lastPulse;

        #region Constructors

        public Market()
        {
            Items = new List<DirectItem>();
            ItemsToSell = new List<DirectItem>();
            ItemsToRefine = new List<DirectInvType>();


            /**Logging.Log("Market", "Load InvTypes.xml from [" + InvTypesXMLData + "]", Logging.White);
            try
            {
                XDocument invTypes = XDocument.Load(InvTypesXMLData);
                if (invTypes.Root != null)
                {
                    foreach (XElement element in invTypes.Root.Elements("invtype"))
                    {
                        InvTypesById.Add((int)element.Attribute("id"), new InvTypeMarket(element));
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log("Market", "Unable to load [" + InvTypesXMLData + "] exception was [" + ex.Message + "]", Logging.Orange);
            }
            **/
        }

        #endregion Constructors

        //{
        //public static string InvTypesXMLData

        #region Properties

        public static List<DirectItem> Items { get; set; }
        private static Dictionary<int, DirectInvType> InvTypesById { get; set; }
        private static List<DirectInvType> ItemsToRefine { get; set; }
        private static List<DirectItem> ItemsToSell { get; set; }

        #endregion Properties

        //    get
        //    {
        //        return Settings.Instance.Path + "\\InvTypes.xml";
        //    }
        //}
        /**
        private static DirectInvType _currentMineral;
        private static DirectItem _currentItem;
        private static DateTime _lastExecute = DateTime.MinValue;

        public static bool StartQuickSell(string module, bool sell)
        {
            if (DateTime.UtcNow.Subtract(_lastExecute).TotalSeconds < Time.Instance.Marketsellorderdelay_seconds)
                return false;
            _lastExecute = DateTime.UtcNow;

            DirectItem directItem = ESCache.Instance.ItemHangar.Items.FirstOrDefault(i => i.ItemId == _currentItem.Id);
            if (directItem == null)
            {
                Log.WriteLine(module, "Item " + _currentItem.Name + " no longer exists in the hanger");
                return false;
            }

            // Update Quantity
            _currentItem.QuantitySold = _currentItem.Quantity - directItem.Quantity;

            if (sell)
            {
                DirectMarketActionWindow sellWindow = Cache.Instance.DirectEve.Windows.OfType<DirectMarketActionWindow>().FirstOrDefault(w => w.IsSellAction);

                //
                // if we do not yet have a sell window then start the QuickSell for this item
                //
                if (sellWindow == null || !sellWindow.IsReady || sellWindow.Item.ItemId != _currentItem.Id)
                {
                    Log.WriteLine(module, "Starting QuickSell for " + _currentItem.Name);
                    if (!directItem.QuickSell())
                    {
                        _lastExecute = DateTime.UtcNow.AddSeconds(-5);

                        Log.WriteLine(module, "QuickSell failed for " + _currentItem.Name + ", retrying in 5 seconds");
                        return false;
                    }
                    return false;
                }

                //
                // what happens here if we have a sell window that is not a quicksell window? wont this hang?
                //

                //
                // proceed to the next state
                //

                // Mark as new execution
                _lastExecute = DateTime.UtcNow;
                return true;
            }

            //
            // if we are not selling check to see if we should refine.
            //
            State.CurrentValueDumpState = ValueDumpState.InspectRefinery;
            return false;
        }

        public static bool Inspectorder(string module, bool sell, bool refine, bool undersell, double RefiningEff)
        {
            // Let the order window stay open for a few seconds
            if (DateTime.UtcNow.Subtract(_lastExecute).TotalSeconds < Time.Instance.Marketbuyorderdelay_seconds)
                return false;

            DirectMarketActionWindow sellWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectMarketActionWindow>().FirstOrDefault(w => w.IsSellAction);

            if (sellWindow != null && (!sellWindow.OrderId.HasValue || !sellWindow.Price.HasValue || !sellWindow.RemainingVolume.HasValue))
            {
                Logging.Log(module, "No order available for " + _currentItem.Name);

                sellWindow.Cancel();
                //
                // next state.
                //
                return true;
            }

            if (sellWindow != null)
            {
                double price = sellWindow.Price.Value;
                int quantity = (int)Math.Min(_currentItem.Quantity - _currentItem.QuantitySold, sellWindow.RemainingVolume.Value);
                double totalPrice = quantity * price;

                string otherPrices = " ";
                if (_currentItem.InvType.MedianBuy.HasValue)
                {
                    otherPrices += "[Median buy price: " + (_currentItem.InvType.MedianBuy.Value * quantity).ToString("#,##0.00") + "]";
                }
                else
                {
                    otherPrices += "[No median buy price]";
                }

                if (refine)
                {
                    int portions = quantity / _currentItem.PortionSize;
                    double refinePrice = _currentItem.RefineOutput.Any()
                                             ? _currentItem.RefineOutput.Sum(
                                                 m => m.Quantity * m.InvType.MedianBuy ?? 0) * portions
                                             : 0;
                    refinePrice *= RefiningEff / 100;

                    otherPrices += "[Refine price: " + refinePrice.ToString("#,##0.00") + "]";

                    if (refinePrice > totalPrice)
                    {
                        Logging.Log(module, "InspectRefinery [" + _currentItem.Name + "[" + quantity + "units] is worth more as mins [Refine each: " + (refinePrice / portions).ToString("#,##0.00") + "][Sell each: " + price.ToString("#,##0.00") + "][Refine total: " + refinePrice.ToString("#,##0.00") + "][Sell total: " + totalPrice.ToString("#,##0.00") + "]", Logging.White);

                        // Add it to the refine list
                        ItemsToRefine.Add(_currentItem);

                        sellWindow.Cancel();
                        //
                        // next state.
                        //
                        return true;
                    }
                }

                if (!undersell)
                {
                    if (!_currentItem.InvType.MedianBuy.HasValue)
                    {
                        Logging.Log(module, "No historical price available for " + _currentItem.Name,
                                    Logging.White);

                        sellWindow.Cancel();
                        //
                        // next state.
                        //
                        return true;
                    }

                    double perc = price / _currentItem.InvType.MedianBuy.Value;
                    double total = _currentItem.InvType.MedianBuy.Value * _currentItem.Quantity;

                    // If percentage < 85% and total price > 1m isk then skip this item (we don't undersell)
                    if (perc < 0.85 && total > 1000000)
                    {
                        Logging.Log(module, "Not underselling item " + _currentItem.Name +
                                                   Logging.Orange + " [" + Logging.White +
                                                   "Median buy price: " +
                                                   _currentItem.InvType.MedianBuy.Value.ToString("#,##0.00") +
                                                   Logging.Orange + "][" + Logging.White +
                                                   "Sell price: " + price.ToString("#,##0.00") +
                                                   Logging.Orange + "][" + Logging.White +
                                                   perc.ToString("0%") +
                                                   Logging.Orange + "]", Logging.White);

                        sellWindow.Cancel();
                        //
                        // next state.
                        //
                        return true;
                    }
                }

                // Update quantity sold
                _currentItem.QuantitySold += quantity;

                // Update station price
                if (!_currentItem.StationBuy.HasValue)
                {
                    _currentItem.StationBuy = price;
                }

                _currentItem.StationBuy = (_currentItem.StationBuy + price) / 2;

                if (sell)
                {
                    Logging.Log(module, "Selling " + quantity + " of " + _currentItem.Name +
                                               Logging.Orange + " [" + Logging.White +
                                               "Sell price: " + (price * quantity).ToString("#,##0.00") +
                                               Logging.Orange + "]" + Logging.White +
                                               otherPrices, Logging.White);
                    sellWindow.Accept();

                    // Update quantity sold
                    _currentItem.QuantitySold += quantity;

                    // Re-queue to check again
                    if (_currentItem.QuantitySold < _currentItem.Quantity)
                    {
                        ItemsToSell.Add(_currentItem);
                    }

                    _lastExecute = DateTime.UtcNow;
                    //
                    // next state
                    //
                    return true;
                }
            }
            return true; //how would we get here with no sell window?
        }

        public static bool InspectRefinery(string module, double RefiningEff)
        {
            if (_currentItem.InvType.MedianBuy != null)
            {
                double priceR = _currentItem.InvType.MedianBuy.Value;
                int quantityR = _currentItem.Quantity;
                double totalPriceR = quantityR * priceR;
                int portions = quantityR / _currentItem.PortionSize;
                double refinePrice = _currentItem.RefineOutput.Any() ? _currentItem.RefineOutput.Sum(m => m.Quantity * m.InvType.MedianBuy ?? 0) * portions : 0;
                refinePrice *= RefiningEff / 100;

                if (refinePrice > totalPriceR || totalPriceR <= 1500000 || _currentItem.TypeId == 30497)
                {
                    Logging.Log(module, "InspectRefinery [" + _currentItem.Name + "[" + quantityR + "units] is worth more as mins [Refine each: " + (refinePrice / portions).ToString("#,##0.00") + "][Sell each: " + priceR.ToString("#,##0.00") + "][Refine total: " + refinePrice.ToString("#,##0.00") + "][Sell total: " + totalPriceR.ToString("#,##0.00") + "]", Logging.White);

                    // Add it to the refine list
                    ItemsToRefine.Add(_currentItem);
                }
                else
                {
                    if (Settings.Instance.DebugValuedump)
                    {
                        Logging.Log(module, "InspectRefinery [" + _currentItem.Name + "[" + quantityR + "units] is worth more to sell [Refine each: " + (refinePrice / portions).ToString("#,##0.00") + "][Sell each: " + priceR.ToString("#,##0.00") + "][Refine total: " + refinePrice.ToString("#,##0.00") + "][Sell total: " + totalPriceR.ToString("#,##0.00") + "]", Logging.White);
                    }
                }
            }
            else
            {
                Logging.Log("Selling gives a better price for item " + _currentItem.Name + " [Refine price: " + refinePrice.ToString("#,##0.00") + "][Sell price: " + totalPrice_r.ToString("#,##0.00") + "]");
            }

            _lastExecute = DateTime.UtcNow;
            return true;
        }

        public static bool WaitingToFinishQuickSell(string module)
        {
            DirectMarketActionWindow sellWindow = Cache.Instance.DirectEve.Windows.OfType<DirectMarketActionWindow>().FirstOrDefault(w => w.IsSellAction);
            if (sellWindow == null || !sellWindow.IsReady || sellWindow.Item.ItemId != _currentItem.Id)
            {
                //
                // this closes ANY modal window and moves on, do we want to be more discriminating?
                //
                DirectWindow modal = Cache.Instance.DirectEve.Windows.FirstOrDefault(w => w.IsModal);
                if (modal != null)
                {
                    modal.Close();
                }

                return true;
            }
            return false;
        }

        public static bool RefineItems(string module, bool refine)
        {
            if (refine)
            {
                if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: if (refine)", Logging.Debug);

                if (!Cache.Instance.OpenItemsHangar(module)) return false;
                DirectReprocessingWindow reprocessingWindow = Cache.Instance.DirectEve.Windows.OfType<DirectReprocessingWindow>().FirstOrDefault();

                if (reprocessingWindow == null)
                {
                    if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: if (reprocessingWindow == null)", Logging.Debug);

                    if (DateTime.UtcNow.Subtract(_lastExecute).TotalSeconds > Time.Instance.Marketlookupdelay_seconds)
                    {
                        IEnumerable<DirectItem> refineItems = Cache.Instance.ItemHangar.Items.Where(i => ItemsToRefine.Any(r => r.Id == i.ItemId)).ToList();
                        if (refineItems.Any())
                        {
                            if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: if (refineItems.Any())", Logging.Debug);

                            Cache.Instance.DirectEve.ReprocessStationItems(refineItems);
                            if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: Cache.Instance.DirectEve.ReprocessStationItems(refineItems);", Logging.Debug);
                            _lastExecute = DateTime.UtcNow;
                            return false;
                        }

                        if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: if (refineItems.Any()) was false", Logging.Debug);
                        return false;
                    }

                    return false;
                }

                if (reprocessingWindow.NeedsQuote)
                {
                    if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: if (reprocessingWindow.NeedsQuote)", Logging.Debug);

                    if (DateTime.UtcNow.Subtract(_lastExecute).TotalSeconds > Time.Instance.Marketlookupdelay_seconds)
                    {
                        if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: if (DateTime.UtcNow.Subtract(_lastExecute).TotalSeconds > Time.Instance.Marketlookupdelay_seconds)", Logging.Debug);

                        reprocessingWindow.GetQuotes();
                        _lastExecute = DateTime.UtcNow;
                        return false;
                    }

                    if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: waiting for: if (DateTime.UtcNow.Subtract(_lastExecute).TotalSeconds > Time.Instance.Marketlookupdelay_seconds)", Logging.Debug);
                    return false;
                }

                if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: // Wait till we have a quote", Logging.Debug);

                // Wait till we have a quote
                if (reprocessingWindow.Quotes.Count == 0)
                {
                    if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: if (reprocessingWindow.Quotes.Count == 0)", Logging.Debug);
                    _lastExecute = DateTime.UtcNow;
                    return false;
                }

                if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: // Wait another 5 seconds to view the quote and then reprocess the stuff", Logging.Debug);

                // Wait another 5 seconds to view the quote and then reprocess the stuff
                if (DateTime.UtcNow.Subtract(_lastExecute).TotalSeconds > Time.Instance.Marketlookupdelay_seconds)
                {
                    if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: if (DateTime.UtcNow.Subtract(_lastExecute).TotalSeconds > Time.Instance.Marketlookupdelay_seconds)", Logging.Debug);
                    // TODO: We should wait for the items to appear in our hangar and then sell them...
                    reprocessingWindow.Reprocess();
                    return true;
                }
            }
            else
            {
                if (Settings.Instance.DebugValuedump) Logging.Log(module, "RefineItems: if (!refine)", Logging.Debug);

                if (!Cache.Instance.OpenCargoHold(module)) return false;
                if (!Cache.Instance.ReadyAmmoHangar(module)) return false;

                IEnumerable<DirectItem> refineItems = Cache.Instance.ItemHangar.Items.Where(i => ItemsToRefine.Any(r => r.Id == i.ItemId)).ToList();
                if (refineItems.Any())
                {
                    Logging.Log("Arm", "Moving loot to refine to CargoHold", Logging.White);

                    Cache.Instance.CargoHold.Add(refineItems);
                    _lastExecute = DateTime.UtcNow;
                    return false;
                }

                State.CurrentValueDumpState = ValueDumpState.Idle;
                return true;
            }
            return false;
        }

    **/
    }
}