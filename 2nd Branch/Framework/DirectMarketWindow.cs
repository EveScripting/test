// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectMarketWindow : DirectWindow
    {
        #region Fields

        private List<DirectOrder> _buyOrders;
        private List<DirectOrder> _sellOrders;
        private DateTime _lastMarketWindowLoadOrders = DateTime.UtcNow.AddHours(-1);

        #endregion Fields

        #region Constructors

        internal DirectMarketWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
            IsReady = !(bool) pyWindow.Attribute("sr").Attribute("market").Attribute("loadingType");
            DetailTypeId = (int?) pyWindow.Attribute("sr").Attribute("market").Attribute("sr").Attribute("detailTypeID");
        }

        #endregion Constructors

        #region Properties

        public List<DirectOrder> BuyOrders
        {
            get
            {
                if (_buyOrders == null)
                    _buyOrders = DetailTypeId.HasValue ? GetOrders(false) : new List<DirectOrder>();

                return _buyOrders;
            }
        }

        public int? DetailTypeId { get; internal set; }
        public bool IsReady { get; internal set; }

        public List<DirectOrder> SellOrders
        {
            get
            {
                if (_sellOrders == null)
                    _sellOrders = DetailTypeId.HasValue ? GetOrders(true) : new List<DirectOrder>();

                return _sellOrders;
            }
        }

        #endregion Properties

        #region Methods

        public IEnumerable<DirectOrder> GetMyOrders(bool bid = false)
        {
            var mq = DirectEve.GetLocalSvc("marketQuote");
            var mu = DirectEve.GetLocalSvc("marketutils");
            if (!IsReady)
                return null;
            //IEnumerable<DirectOrder> orders = mq.Call("GetMyOrders").ToList().Select(o => new DirectOrder(this, o));
            if (!bid)
                return
                    PyWindow.Attribute("sr")
                        .Attribute("market")
                        .Attribute("sr")
                        .Attribute("myorders")
                        .Attribute("sr")
                        .Attribute("sellScroll")
                        .Attribute("sr")
                        .Attribute("entries")
                        .ToList()
                        .Select(o => new DirectOrder(DirectEve, o.Attribute("order")));
            return
                PyWindow.Attribute("sr")
                    .Attribute("market")
                    .Attribute("sr")
                    .Attribute("myorders")
                    .Attribute("sr")
                    .Attribute("buyScroll")
                    .Attribute("sr")
                    .Attribute("entries")
                    .ToList()
                    .Select(o => new DirectOrder(DirectEve, o.Attribute("order")));
        }

        public bool LoadOrders()
        {
            if (DateTime.UtcNow > _lastMarketWindowLoadOrders.AddSeconds(6))
            {
            var mu = DirectEve.GetLocalSvc("marketutils");
            var mq = DirectEve.GetLocalSvc("marketQuote");
                if (!IsReady)
                    return false;
                ;

                if (DirectEve.ThreadedCall(PyWindow.Attribute("sr").Attribute("market").Attribute("LoadOrders")))
                {
                    _lastMarketWindowLoadOrders = DateTime.UtcNow;
                    return true;
                }

                return false;
            }

            return true;
        }

        public bool LoadTypeId(int typeId)
        {
            if (DateTime.UtcNow > _lastMarketWindowLoadOrders.AddSeconds(2))
            {
                if (DirectEve.ThreadedCall(PyWindow.Attribute("LoadTypeID_Ext"), typeId))
                {
                    _lastMarketWindowLoadOrders = DateTime.UtcNow;
                    return true;
                }

                return false;
            }

            return false;
        }

        private List<DirectOrder> GetOrders(bool sellOrders)
        {
            var orders = DirectEve.GetLocalSvc("marketQuote").Attribute("orderCache").DictionaryItem(DetailTypeId.Value).GetItemAt(sellOrders ? 0 : 1).ToList();
            return orders.Select(order => new DirectOrder(DirectEve, order)).ToList();
        }

        #endregion Methods
    }
}