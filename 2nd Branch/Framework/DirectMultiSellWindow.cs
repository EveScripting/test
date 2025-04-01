extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    public enum DurationComboValue
    {
        IMMEDIATE = 0,
        DAY = 1,
        THREEDAYS = 3,
        WEEK = 7,
        TWOWEEKS = 14,
        MONTH = 30,
        THREEMONTHS = 90,
    }

    public class DirectMultiSellWindow : DirectWindow
    {
        internal DirectMultiSellWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
        }

        //carbonui.uicore.uicore.registry.windows[10]
        //SellItemsWindow
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects 0,1,2,3
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[0]
        //window_controls_cont
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[1]
        //Resizer
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2]
        //content
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects 0,1,2
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[0]
        //__loadingParent
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[1]
        //headerParent
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2]
        //main
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
        //ButtonGroup
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects._childrenObjects[0]
        //btns
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects 0,1,2
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[0]
        //ButtonWrapper
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
        //confirm_sell_button
        //
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[1]
        //ButtonWrapper
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
        //cancel_sell_button
        //
        //
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[2]
        //OverflowButton
        //
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
        //mainCont
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects 0,1,2,3
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
        //infoCont
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
        //bottomLeft
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
        //EveLabelSmall
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
        //combo
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
        //__maincontent
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
        //__expanderParent
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
        //iconParent
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
        //__textClipper
        //
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
        //label
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
        //ComboUnderlay
        //
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
        //bottomRight
        //
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1]
        //dropCont
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[2]
        //locationCont
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[3]
        //scrollCont
        //
        //carbonui.uicore.uicore.registry.windows[10].children._childrenObjects[3]
        //underlay
        //
        //
        //active (bool) - True if the window is active.
        //AddItem(self, item) - Add an item to the list.
        //AddItems(self, items) - Add a list of items to the list.
        //btnGroup
        //cannotTradeItemList
        //children
        //content
        //display (bool) - True if the window is displayed.
        //itemDict
        //itemList
        //itemsNeedRepackaging
        //itemsScroll
        //locationCont
        //mainCont
        //myOrderCount
        //orderCap
        //orderCountLabel
        //pickRadius (int)
        //pickState (int)
        //sellItemList
        //state (int)
        //totalAmt
        //tradeOnConfirm (bool)
        //tradeForCorpSettingConfig - sellUseCorp
        //
        //
        private List<double> _getSums;

        public long BaseStationId => PyWindow.Attribute("baseStationID").ToLong();

        public double BrokersFee => GetSums()[0];

        public double SalesTax => GetSums()[1];
        public double TotalSum => GetSums()[2];

        private List<double> GetSums()
        {
            if (_getSums == null)
            {
                var obj = PyWindow.Call("GetSums").ToList<double>();
                if (obj.Count > 2)
                    obj[2] = obj[2] - obj[1] - obj[0];
                _getSums = obj;
            }
            return _getSums;
        }

        public DurationComboValue GetDurationComboValue()
        {
            var val = PyWindow.Attribute("durationCombo").Attribute("selectedValue").ToInt();
            return (DurationComboValue)val;
        }

        public void SetDurationCombovalue(DurationComboValue v)
        {
            PyWindow.Attribute("durationCombo").Call("SetValue", (int)v);
        }

        public void PerformTrade()
        {
            if (GetSellItems().All(i => !i.HasBid))
            {
                DirectEve.Log($"Can't perform trade, only items without a bid are within the sell list.");
                return;
            }
            DirectEve.ThreadedCall(PyWindow.Attribute("PerformTrade"));
        }

        public void Cancel()
        {
            DirectEve.ThreadedCall(PyWindow.Attribute("Cancel"));
        }

        public bool AddingItemsThreadRunning => !PyWindow.Attribute("addItemsThread").Attribute("endTime").IsValid;

        public List<DirectMultiSellWindowItem> GetSellItems()
        {
            var ret = new List<DirectMultiSellWindowItem>();
            var list = PyWindow.Attribute("itemList").ToList();
            foreach (var item in list)
            {
                ret.Add(new DirectMultiSellWindowItem(DirectEve, item));
            }
            return ret;
        }
    }
}
