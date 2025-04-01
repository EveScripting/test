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

using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Storylines;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectRepairShopWindow : DirectWindow
    {
        internal DirectRepairShopWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
        }

        //carbonui.uicore.uicore.registry.windows[9]
        //repairshop
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects 0,1,2,3
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[0]
        //window_controls_cont
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[1]
        //resizer
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2]
        //content
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects 0,1,2
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[0]
        //__loadingParent
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[1]
        //headerParent
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2]
        //main
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1,2
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
        //topParent
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
        //ButtonGroup
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects._childrenObjects[0]
        //btns
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects 0,1,2,3
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[0]
        //ButtonWrapper
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
        //Pick New Item_Btn //you appear to have nothing to repair. These facilities can fix assembled ships, drones, modules, and cargo containers
        //
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[1]
        //ButtonWrapper
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
        //Repair Item_Btn
        //
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[2]
        //ButtonWrapper
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
        //Repair All_Btn
        //
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects._childrenObjects[0].children._childrenObjects[3]
        //OverflowButton
        //
        //
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[2]
        //scroll
        //
        //carbonui.uicore.uicore.registry.windows[9].children._childrenObjects[3]
        //underlay
        //
        //
        //
        //active (bool)
        //Buttonresult????
        //children
        //content
        //DisplayrepairQuote(self, items)
        //QuoteItems(self)
        //Repair(selfm items)
        //RepairAll(self)
        //repairAllBtn
        //repaireItemBtn
        //repairItems(self, items)
        //


        public string AvgDamage()
        {
            try
            {
                return (string)PyWindow.Attribute("avgDamageLabel").Attribute("text");
            }
            catch
            {
                return "";
            }
        }

        public List<PyObject> GetAll()
        {
            return PyWindow.Call("GetAll").ToList<PyObject>();
        }

        public List<PyObject> GetSelected()
        {
            return PyWindow.Call("GetSelected").ToList<PyObject>();
        }

        public bool IsItemRepairable(DirectItem i)
        {
            if (!i.IsSingleton)
                return false;

            PyObject r = PySharp.Import("repair");
            if (r.IsValid)
                return r.Call("IsRepairable", i.PyItem).ToBool();
            return false;
        }

        public bool QuoteItems()
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("QuoteItems"));
        }

        public bool RepairAll()
        {
            if (Time.Instance.LastWindowInteraction.AddSeconds(2) > DateTime.UtcNow)
            {
                return false;
            }

            if (DirectEve.ThreadedCall(PyWindow.Attribute("RepairAll")))
            {
                Time.Instance.LastWindowInteraction = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        public bool RepairItems(List<DirectItem> items)
        {
            if (Time.Instance.LastWindowInteraction.AddSeconds(2) > DateTime.UtcNow)
            {
                return false;
            }

            IEnumerable<PyObject> PyItems = items.Where(i => IsItemRepairable(i)).Select(i => i.PyItem);
            if (PyItems.Any())
            {
                if (DirectEve.ThreadedCall(PyWindow.Attribute("DisplayRepairQuote"), PyItems))
                {
                    int count = 0;
                    if (DebugConfig.DebugArm)
                    {
                        Log.WriteLine("Attempting to repair these items:");
                        foreach (var repairableItem in items.Where(i => IsItemRepairable(i)))
                        {
                            count++;
                            if (DebugConfig.DebugArm) Log.WriteLine("[" + count + "][" + repairableItem.TypeName + "]");
                        }
                    }

                    Time.Instance.LastWindowInteraction = DateTime.UtcNow;
                    return true;
                }
            }

            return false;
        }

        // OpenWindow -> SelectAll() -> GetSelected() > 0 -> QuoteItems() -> GetAll() > 0 -> RepairAll()

        public bool SelectAll()
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("scroll").Attribute("SelectAll"));
        }
    }
}