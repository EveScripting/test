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

using SC::SharedComponents.Extensions;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using EVESharpCore.Cache;
using System.Xml;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectContainer : DirectInvType
    {
        #region Fields

        /// <summary>
        ///     Item Id
        /// </summary>
        private long _itemId;

        /// <summary>
        ///     Items cache
        /// </summary>
        private List<DirectItem> _items;

        /// <summary>
        ///     Flag reference
        /// </summary>
        private PyObject _pyFlag { get; set; }

        /// <summary>
        ///     Inventory reference
        /// </summary>
        private PyObject _pyInventory;

        /// <summary>
        ///     Is this the ship's modules 'container'
        /// </summary>
        private bool _shipModules;

        /// <summary>
        ///     Associated window cache
        /// </summary>
        private DirectContainerWindow _window;

        /// <summary>
        ///     Window name
        /// </summary>
        private readonly string _windowName;



        /// <summary>
        ///     Ships cache
        /// </summary>
        private List<DirectItem> _ships;



        #endregion Fields

        #region Constructors

        internal DirectContainer(DirectEve directEve, PyObject pyInventory, PyObject pyFlag)
            : base(directEve)
        {
            _pyInventory = pyInventory;
            _pyFlag = pyFlag;

            TypeId = (int)pyInventory.Attribute("_typeID");
        }

        public PyObject GetPyInventory() => _pyInventory;

        /// <summary>
        ///     DirectContainer
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="pyInventory"></param>
        /// <param name="pyFlag"></param>
        /// <param name="windowName"></param>
        internal DirectContainer(DirectEve directEve, PyObject pyInventory, PyObject pyFlag, string windowName)
            : this(directEve, pyInventory, pyFlag)
        {
            _windowName = windowName;
        }

        /// <summary>
        ///     DirectContainer
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="pyInventory"></param>
        /// <param name="pyFlag"></param>
        /// <param name="itemId"></param>
        internal DirectContainer(DirectEve directEve, PyObject pyInventory, PyObject pyFlag, long itemId)
            : this(directEve, pyInventory, pyFlag)
        {
            _itemId = itemId;
            _windowName = string.Empty;
        }

        /// <summary>
        ///     DirectContainer
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="pyInventory"></param>
        /// <param name="shipModules"></param>
        internal DirectContainer(DirectEve directEve, PyObject pyInventory, bool shipModules) : base(directEve)
        {
            // You can't build a DirectContainer with these parameters if its not shipModules
            if (!shipModules)
                throw new Exception("Invalid container");

            _pyInventory = pyInventory;
            _pyFlag = PySharp.PyNone;
            _windowName = string.Empty;
            _shipModules = true;
        }

        #endregion Constructors

        #region Properties

        public long ItemId => _itemId;

        //public bool CanBeStacked => Items.Any() && IEnumerableExtensions.DistinctBy(Items, i => i.TypeId).Where(e => !e.IsSingleton).Where(i => Items.Count(n => n.TypeId == i.TypeId && !n.IsSingleton) > 2).Any();

        public bool CanBeStacked
        {
            get
            {
                try
                {
                    if (Items.Count > 0)
                    {
                        IEnumerable<DirectItem> UniqueTypeIDItemsInThisContainer = Items.Where(x => !x.IsSingleton).DistinctBy(i => i.TypeId);

                        foreach (var thisUniqueTypeIdItem in UniqueTypeIDItemsInThisContainer
                            .OrderBy(a => a.IsCommonMissionItem)
                            .ThenBy(b => b.CategoryId == (int)CategoryID.Charge)
                            .ThenBy(b => b.CategoryId == (int)CategoryID.Drone)
                            )
                        {
                            if (Items.Where(x => !x.IsSingleton).Count(i => i.TypeId == thisUniqueTypeIdItem.TypeId) >= 2)
                            {
                                return true;
                            }
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


        /// <summary>
        ///     Return the container's capacity
        /// </summary>
        /// <returns></returns>
        public new double Capacity
        {
            get
            {
                if (_shipModules)
                    return 0;

                if (GroupId == (int)Group.Capsule)
                    return 0;

                double tempCapacity = 0;
                if (_pyFlag.IsValid)
                {
                    tempCapacity =  (double)_pyInventory.Call("GetCapacity", _pyFlag).Attribute("capacity");
                    if (tempCapacity == 0 && (DebugConfig.DebugInventoryContainers || DebugConfig.DebugUnloadLoot))
                        Log.WriteLine("ContainerName [" + _windowName + "] (double)_pyInventory.Call(GetCapacity, _pyFlag).Attribute(capacity); returned 0;");

                    if (tempCapacity == 0 && Items == null || !Items.Any())
                    {
                        //FixMe this is a really bad hack
                        if (GroupId == (int)Group.Frigate)
                            return 100;

                        if (GroupId == (int)Group.TransportShip)
                            return 4000;

                        if (GroupId == (int)Group.Freighter)
                            return 10000;

                        if (TypeId == (int)TypeID.Orca)
                            return 500;
                    }

                    //Log.WriteLine("ContainerName [" + _windowName + "] has [" + tempCapacity + "] m3 capacity");
                    return tempCapacity;
                }

                tempCapacity = (double)_pyInventory.Call("GetCapacity").Attribute("capacity");
                if (tempCapacity == 0)
                    Log.WriteLine("ContainerName [" + _windowName + "] (double)_pyInventory.Call(GetCapacity).Attribute(capacity); returned 0;");

                //Log.WriteLine("ContainerName [" + _windowName + "] has [" + tempCapacity + "] m3 capacity");
                return tempCapacity;
            }
        }

        public double? FreeCapacity
        {
            get
            {
                if (!IsReady || !IsValid)
                    return null;

                if (_shipModules)
                    return 0;

                if (Items.Count > 0)
                    return Capacity - UsedCapacity;

                return Capacity;
            }
        }



        private DateTime LastInventoryWindowRefreshAttempt = DateTime.MinValue;

        /// <summary>
        ///     Is the container ready?
        /// </summary>
        public bool IsReady
        {
            get
            {
                if (_shipModules)
                {
                    if (DebugConfig.DebugArm) DirectEve.Log("IsReady: if (_shipModules)");
                    return true;
                }

                if (Window == null)
                {
                    if (DebugConfig.DebugArm) DirectEve.Log("IsReady: if (Window == null)");
                    return false;
                }

                if (!IsValid)
                {
                    if (DebugConfig.DebugArm) DirectEve.Log("IsReady: if (!IsValid)");
                    return false;
                }

                //if (WaitingForLockedItems())
                //{
                //    if (DebugConfig.DebugArm) DirectEve.Log("IsReady: if (WaitingForLockedItems())");
                //    return false;
                //}

                if (!Window.IsReady)
                {
                    if (DebugConfig.DebugArm) DirectEve.Log("IsReady: if (!Window.IsReady)");
                    return false;
                }

                var listed = _pyInventory.Attribute("listed");

                if (!listed.IsValid)
                {
                    if (DebugConfig.DebugArm) DirectEve.Log("IsReady: if (!listed.IsValid)");
                    return false;
                }

                if (_pyFlag.IsValid && listed.GetPyType() == PyType.SetType)
                {
                    if (!_pyInventory.Attribute("listed").PySet_Contains(_pyFlag))
                    {
                        if (DebugConfig.DebugArm) DirectEve.Log("IsReady: if (!_pyInventory.Attribute(listed).PySet_Contains(_pyFlag))");
                        //DirectEve.Log($"Listed does not contain the current flag {_pyFlag.ToInt()}");
                        Window.RefreshInvWindowCont();
                        return false;
                    }
                }

                if (!InvItem.IsValid)
                {
                    if (LastInventoryWindowRefreshAttempt == DateTime.MinValue)
                    {
                        LastInventoryWindowRefreshAttempt = DateTime.UtcNow;
                    }

                    //If timestamp has aged past certain # of seconds.... then do this next part, otherwise dont
                    if (LastInventoryWindowRefreshAttempt.AddSeconds(10) > DateTime.UtcNow)
                    {
                        Window.RefreshInvWindowCont();
                        return false;
                    }

                    //create timestamp here....
                    DirectEve.Log("[" + Window.Name + "] InvItem is not valid! Restart Eve");
                    if (DirectEve.Session.IsInDockableLocation)
                    {
                        ESCache.Instance.CloseEveReason = "InvItem is not valid!";
                        ESCache.Instance.BoolRestartEve = true;
                        return false;
                    }

                    return false;
                }

                return true;
            }
        }

        /// <summary>
        ///     Is the container valid?
        /// </summary>
        /// <remarks>
        ///     Valid is not the same as ready!
        /// </remarks>
        public bool IsValid
        {
            get
            {
                if (_pyInventory.IsValid)
                {
                    if (_pyInventory.Attribute("listed").IsValid)
                        return true;

                    if (DebugConfig.DebugArm) DirectEve.Log("Directcontainer: IsValid: !if (_pyInventory.Attribute(listed).IsValid)");
                    return false;
                }

                if (DebugConfig.DebugArm) DirectEve.Log("Directcontainer: IsValid: !if (_pyInventory.IsValid)");
                return false;
            }
        }

        /// <summary>
        ///     Get the items from the container
        /// </summary>
        public List<DirectItem> Items
        {
            get
            {
                if (_items == null)
                {
                    _items = DirectItem.GetItems(DirectEve, _pyInventory, _pyFlag);

                    // Special case
                    var categoryShip = (int)DirectEve.Const.CategoryShip;

                    if (_windowName.Contains("StationItems") || _windowName.Contains("StructureItemHangar"))
                        _items.RemoveAll(i => i.CategoryId == categoryShip);
                    if (_windowName.Contains("StationShips") || _windowName.Contains("StructureShipHangar"))
                        _items.RemoveAll(i => i.CategoryId != categoryShip);

                    // Special case #2 (filter out hi/med/low slots)
                    if (_shipModules)
                    {
                        var flags = new List<int>();
                        for (var i = 0; i < 8; i++)
                        {
                            flags.Add((int)DirectEve.Const["flagHiSlot" + i]);
                            flags.Add((int)DirectEve.Const["flagMedSlot" + i]);
                            flags.Add((int)DirectEve.Const["flagLoSlot" + i]);
                        }

                        _items.RemoveAll(i => !flags.Any(f => f == i.FlagId));
                    }

                    if (_items == null)
                        _items = new List<DirectItem>();

                    return _items;
                }

                return _items;
            }
        }

        public List<DirectItem> ValidShipsToUse
        {
            get
            {
                _ships = Items.Where(i => i.IsValidShipToUse).ToList();
                return _ships;
            }
        }
        public DirectContainerWindow PrimaryWindow => DirectEve.Windows.OfType<DirectContainerWindow>().FirstOrDefault(w => w.IsPrimary());

        /// <summary>
        ///     Return the container's used capacity
        /// </summary>
        /// <returns></returns>
        public double? UsedCapacity
        {
            get
            {
                if (_shipModules)
                    return 0;

                double tempUsedCapacity = 0;
                if (_pyFlag.IsValid)
                {
                    tempUsedCapacity = (double)_pyInventory.Call("GetCapacity", _pyFlag).Attribute("used");
                    if (tempUsedCapacity == 0 && Items != null && Items.Any())
                    {
                        Log.WriteLine("ContainerName [" + _windowName + "] (double)_pyInventory.Call(GetCapacity, _pyFlag).Attribute(capacity); returned 0;");
                        return null;
                    }

                    //Log.WriteLine("ContainerName [" + _windowName + "] has [" + tempUsedCapacity + "] m3 used");
                    return tempUsedCapacity;
                }

                tempUsedCapacity = (double)_pyInventory.Call("GetCapacity").Attribute("used");
                if (tempUsedCapacity == 0)
                    Log.WriteLine("ContainerName [" + _windowName + "] (double)_pyInventory.Call(GetCapacity).Attribute(capacity); returned 0;");

                //Log.WriteLine("ContainerName [" + _windowName + "] has [" + tempUsedCapacity + "] m3 used");
                return tempUsedCapacity;
            }
        }

        public int UsedCapacityPercentage
        {
            get
            {
                if (UsedCapacity == null)
                    return -1;

                if (Capacity == 0)
                    return 0;

                if (UsedCapacity == 0)
                    return 0;

                if (FreeCapacity == 0)
                    return 100;

                return (int)Math.Round((double)UsedCapacity / Capacity, 0);
            }
        }

        public int FreeCapacityPercentage
        {
            get
            {
                if (Capacity == 0)
                    return 0;

                if (UsedCapacity == 0)
                    return 100;

                if (FreeCapacity == 0)
                    return 0;

                return (int)Math.Round((double)FreeCapacity / Capacity, 0);
            }
        }

        /// <summary>
        ///     Get the associated window for this container
        /// </summary>
        public DirectContainerWindow Window
        {
            get
            {
                try
                {
                    if (_shipModules)
                        return null;

                    if (_window == null && !string.IsNullOrEmpty(_windowName))
                    {
                        if (DirectEve.Windows.OfType<DirectContainerWindow>().Any())
                        {
                            _window = DirectEve.Windows.OfType<DirectContainerWindow>().FirstOrDefault(w => w.Name.Contains(_windowName));
                            if (DebugConfig.DebugArm)
                            {
                                if (_window == null)
                                {
                                    if (DebugConfig.DebugArm) Log.WriteLine("Window: Found [" + DirectEve.Windows.OfType<DirectContainerWindow>().Count() + "] DirectContainerWindow(s)");
                                    if (DebugConfig.DebugArm) Log.WriteLine("Window: _windowName we are looking for [" + _windowName + "] Do we have a DirectContainerWindow for this hangar at all?");

                                    int windowNum = 0;
                                    foreach (var window in DirectEve.Windows.OfType<DirectContainerWindow>())
                                    {
                                        windowNum++;
                                        if (DebugConfig.DebugArm)
                                        {
                                            Log.WriteLine("[" + windowNum + "] DirectContainerWindow");
                                            Log.WriteLine("[" + windowNum + "] Debug_Window.Name: [" + window.Name + "]");
                                            Log.WriteLine("[" + windowNum + "] Debug_Window.Html: [" + window.Html + "]");
                                            Log.WriteLine("[" + windowNum + "] Debug_Window.Type: [" + window.Guid + "]");
                                            Log.WriteLine("[" + windowNum + "] Debug_Window.IsModal: [" + window.IsModal + "]");
                                            Log.WriteLine("[" + windowNum + "] Debug_Window.Caption: [" + window.Caption + "]");
                                        }
                                    }

                                    Log.WriteLine("Window: _window is still null.");
                                }
                            }
                        }
                    }

                    if (_window == null && _itemId != 0)
                    {
                        if (DebugConfig.DebugArm)
                        {
                            Log.WriteLine("Window: if (_window == null && _itemId != 0)");
                        }

                        _window = DirectEve.Windows.OfType<DirectContainerWindow>().FirstOrDefault(w => (w.CurrInvIdItem == _itemId || w.GetIdsFromTree(false).Contains(_itemId)) && !w.IsPrimary())
                            ?? DirectEve.Windows.OfType<DirectContainerWindow>().FirstOrDefault(w => w.IsPrimary() && (w.CurrInvIdItem == _itemId || w.GetIdsFromTree(false).Contains(_itemId)));

                        if (DebugConfig.DebugArm)
                        {
                            if (_window == null) Log.WriteLine("Window: _window is still null!!");
                        }
                    }

                    return _window;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        private PyObject InvItem => _pyInventory.Attribute("_item");

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Add an item to this container
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Add(DirectItem item)
        {
            return Add(item, item.Stacksize);
        }

        public bool WaitingForLockedItems()
        {
            return !DirectEve.NoLockedItemsOrWaitAndClearLocks("WaitingForLockedItems");
        }

        /// <summary>
        ///     Add an item to this container
        /// </summary>
        /// <param name="item"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public bool Add(DirectItem item, int quantity)
        {
            if (WaitingForLockedItems())
                return false;

            // You can't fit modules like this
            if (_shipModules)
                return false;

            if (item.LocationId == -1 || quantity < 1)
                return false;

            Dictionary<string, object> keywords = new Dictionary<string, object>
            {
                { "qty", quantity }
            };

            if (_pyFlag.IsValid)
                keywords.Add("flag", _pyFlag);
            if (!_pyFlag.IsValid && GroupId == (int)DirectEve.Const.GroupAuditLogSecureContainer)
                keywords.Add("flag", DirectEve.Const.FlagUnlocked);
            return DirectEve.ThreadedCallWithKeywords(_pyInventory.Attribute("Add"), keywords, item.ItemId, item.LocationId);
        }

        /// <summary>
        ///     Add multiple items to this container
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public bool Add(IEnumerable<DirectItem> items)
        {
            // You can't fit modules like this
            if (_shipModules)
                return false;

            if (!items.Any())
                return true;

            if (WaitingForLockedItems())
                return false;

            if (!Window.InvController.IsValid)
                return false;

            if (!Window.IsReady)
                return false;

            var fromCointainerId = items.First().LocationId;

            //return DirectEve.ThreadedCall(Controller.Attribute("AddItems"), items.Select(i => i.PyItem));
            var keywords = new Dictionary<string, object>();
            if (_pyFlag.IsValid)
                keywords.Add("flag", _pyFlag);
            if (!_pyFlag.IsValid && GroupId == (int)DirectEve.Const.GroupAuditLogSecureContainer)
                keywords.Add("flag", DirectEve.Const.FlagUnlocked);
            return DirectEve.ThreadedCallWithKeywords(_pyInventory.Attribute("MultiAdd"), keywords, items.Select(i => i.ItemId), items.First().LocationId);
        }

        /// <summary>
        ///     Jettison item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        /// <remarks>
        ///     This will fail on items not located in the ship's cargo hold
        /// </remarks>
        public bool Jettison(long itemId)
        {
            //see: attributeCanBeJettisoned - const.py

            return Jettison(new[] { itemId });
        }

        /// <summary>
        ///     Jettison items
        /// </summary>
        /// <param name="itemIds"></param>
        /// <returns></returns>
        /// <remarks>
        ///     This will fail on items not located in the ship's cargo hold
        /// </remarks>
        public bool Jettison(IEnumerable<long> itemIds)
        {
            // You can't jettison modules
            if (_shipModules)
                return false;

            if (itemIds.Count() == 0)
                return true;

            var jettison = DirectEve.GetLocalSvc("gameui").Call("GetShipAccess").Attribute("Jettison");
            return DirectEve.ThreadedCall(jettison, itemIds);
        }

        public bool LaunchForSelf(IEnumerable<long> itemIds)
        {
            if (GroupId != (int)Group.MobileTractor && GroupId != (int)Group.MobileDepot)
                return false;

            if (!DirectEve.Interval(3000, 4000))
                return false;

            // You can't jettison modules
            if (_shipModules)
                return false;

            if (!itemIds.Any())
                return true;

            PyObject pyLaunchForSelf = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("LaunchForSelf");

            if (pyLaunchForSelf == null || !pyLaunchForSelf.IsValid)
            {
                Log.WriteLine("LaunchForSelf: if (pyLaunchForSelf == null || !pyLaunchForSelf.IsValid)");
                return false;
            }

            return DirectEve.ThreadedCall(pyLaunchForSelf, itemIds);
        }

        public bool StackAmmoHangar()
        {
            if (Time.Instance.LastStackAmmoHangarAction.AddMinutes(15) > DateTime.UtcNow)
            {
                Log.WriteLine("AmmoHangar was last stacked [" + Time.Instance.LastStackAmmoHangarAction.ToShortTimeString() + "] returning true");
                return true;
            }

            if (!DirectEve.Interval(2000, 3000))
            {
                Log.WriteLine("if (!DirectEve.Interval(2000, 3000))");
                return false;
            }

            if (!StackAll()) return false;
            Time.Instance.LastStackAmmoHangarAction = DateTime.UtcNow;
            return true;
        }

        public bool StackLootHangar()
        {
            if (Time.Instance.LastStackLootHangarAction.AddMinutes(15) > DateTime.UtcNow)
            {
                if (DirectEve.Interval(30000)) Log.WriteLine("LootHangar was last stacked [" + Time.Instance.LastStackLootHangarAction.ToShortTimeString() + "] returning true");
                return true;
            }

            if (!DirectEve.Interval(2000, 3000))
            {
                Log.WriteLine("if (!DirectEve.Interval(2000, 3000))");
                return false;
            }

            if (!StackAll()) return false;
            Time.Instance.LastStackLootHangarAction = DateTime.UtcNow;
            return true;
        }

        public bool OrganizeItemHangar()
        {
            try
            {
                //blueprint containers tends to fill up, so we need to rename the full ones and make new containers as needed!
                //Buy SMALL Standard Container? go get one if none local?
                //ConstructContainer
                //Name Container
                //MoveBlueprintsIntoBlueprintContainer
                if (!StackItemHangar()) return false;
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public bool StackItemHangar()
        {
            try
            {
                if (Time.Instance.LastStackItemHangarAction.AddMinutes(15) > DateTime.UtcNow)
                {
                    Log.WriteLine("LootHangar was last stacked [" + Time.Instance.LastStackItemHangarAction.ToShortTimeString() + "] returning true");
                    return true;
                }

                if (!DirectEve.Interval(2000, 3000))
                {
                    Log.WriteLine("if (!DirectEve.Interval(2000, 3000))");
                    return false;
                }

                if (!StackAll()) return false;
                Time.Instance.LastStackItemHangarAction = DateTime.UtcNow;
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public bool StackShipsCargo()
        {
            if (Time.Instance.LastStackCargoAction.AddMinutes(5) > DateTime.UtcNow)
            {
                Log.WriteLine("LootHangar was last stacked [" + Time.Instance.LastStackCargoAction.ToShortTimeString() + "] returning true");
                return true;
            }

            if (!DirectEve.Interval(2000, 3000))
            {
                Log.WriteLine("if (!DirectEve.Interval(2000, 3000))");
                return false;
            }

            if (!StackAll()) return false;
            Time.Instance.LastStackCargoAction = DateTime.UtcNow;
            return true;
        }

        /// <summary>
        ///     Stack all the items in the container
        /// </summary>
        /// <returns></returns>
        public bool StackAll()
        {
            try
            {
                if (!CanBeStacked)
                {
                    //Time.Instance.NextStackHangarAction = DateTime.UtcNow.AddMinutes(10);
                    Log.WriteLine("StackAll: Container [" + _windowName + "] looks orderly: no need to stack right now");
                    return true;
                }

                if (WaitingForLockedItems())
                {
                    Log.WriteLine("StackAll: if (WaitingForLockedItems())");
                    return false;
                }

                if (!DirectEve.Session.IsInDockableLocation && DirectEve.GetItemHangar() == this)
                {
                    Log.WriteLine("StackAll: if (!DirectEve.Session.IsInDockableLocation && DirectEve.GetItemHangar() == this)");
                    return false;
                }

                if (!DirectEve.Session.IsInDockableLocation && DirectEve.GetShipHangar() == this)
                {
                    Log.WriteLine("StackAll: if (!DirectEve.Session.IsInDockableLocation && DirectEve.GetShipHangar() == this)");
                    return false;
                }

                try
                {
                    //if (DirectEve.DictHanagarLastStackingTracking.Any())
                    //{
                    //    if (DirectEve.DictHanagarLastStackingTracking.ContainsKey(this._itemId))
                    //    {
                    //        if (DirectEve.DictHanagarLastStackingTracking[_itemId].AddMinutes(1) > DateTime.UtcNow)
                    //        {
                    //            Log.WriteLine("StackAll: We stacked this container already in the last minute. return true");
                    //            return true;
                    //        }
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                // You can't stack modules
                if (_shipModules)
                {
                    Log.WriteLine("StackAll: if (_shipModules)");
                    return false;
                }

                //if (Time.Instance.NextStackHangarAction > DateTime.UtcNow)
                //{
                //    Log.WriteLine("Hangar has been stacked within the last 10 minutes");
                //    return true;
                //}

                if (!DirectEve.Interval(2000, 4000))
                {
                    Log.WriteLine("Hangar StackAll: if (!DirectEve.Interval(1000, 2000))");
                    return false;
                }

                bool resultOfStacking = _pyFlag.IsValid
                    ? DirectEve.ThreadedCall(_pyInventory.Attribute("StackAll"), _pyFlag)
                    : DirectEve.ThreadedCall(_pyInventory.Attribute("StackAll"));

                if (resultOfStacking)
                {
                    Log.WriteLine("Hangar StackAll returned true");
                }

                return resultOfStacking;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public void StartLoadingAllDynamicItems()
        {
            foreach (var item in Items)
            {
                _ = item.DynamicItem;
            }
        }

        /// <summary>
        ///     Get a item container
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        internal static DirectContainer GetContainer(DirectEve directEve, long itemId, bool doRemoteCall = true)
        {
            //string calledFrom = "GetContainer";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            var inventory = GetInventory(directEve, "GetInventoryFromId", itemId, doRemoteCall);
            return new DirectContainer(directEve, inventory, PySharp.PyNone, itemId);
        }

        /// <summary>
        ///     Get the corporation hangar container based on division name
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="divisionName"></param>
        /// <returns></returns>
        internal static DirectContainer GetCorporationHangar(DirectEve directEve, string divisionName)
        {
            //string calledFrom = "GetCorporationHangar";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            var divisions = directEve.GetLocalSvc("corp").Call("GetDivisionNames");
            for (var i = 1; i <= 7; i++)
                if (string.Compare(divisionName, (string)divisions.DictionaryItem(i), true) == 0)
                    return GetCorporationHangar(directEve, i);

            return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);
        }

        /// <summary>
        ///     Get the corporation hangar container based on division id (1-7)
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="divisionId"></param>
        /// <returns></returns>
        internal static DirectContainer GetCorporationHangar(DirectEve directEve, int divisionId = 1)
        {
            try
            {
                //string calledFrom = "GetCorporationHangar";
                //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

                PyObject flag = null;
                switch (divisionId)
                {
                    case 1:
                        flag = directEve.Const.FlagCorpSAG1;
                        break;

                    case 2:
                        flag = directEve.Const.FlagCorpSAG2;
                        break;

                    case 3:
                        flag = directEve.Const.FlagCorpSAG3;
                        break;

                    case 4:
                        flag = directEve.Const.FlagCorpSAG4;
                        break;

                    case 5:
                        flag = directEve.Const.FlagCorpSAG5;
                        break;

                    case 6:
                        flag = directEve.Const.FlagCorpSAG6;
                        break;

                    case 7:
                        flag = directEve.Const.FlagCorpSAG7;
                        break;
                }

                if (flag == null)
                    return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

                long? locationId = directEve.Session.Structureid ?? directEve.Session.StationId ?? directEve.Session.LocationId;

                if (locationId != null)
                {
                    long itemId = 0;
                    try
                    {
                        try
                        {
                            itemId = (long)directEve.GetLocalSvc("officeManager").Call("GetCorpOfficeAtLocation").Attribute("officeID");
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        if (itemId != 0)
                        {
                            PyObject inventory = GetInventory(directEve, "GetInventoryFromId", itemId);

                            if (inventory == null) return null;

                            if (directEve.Windows.All(i => !i.Name.Contains("StructureCorpHangars") && !i.Name.Contains("StationCorpHangars")))
                            {
                                directEve.ExecuteCommand(DirectCmd.OpenCorpHangar);
                                return null;
                            }

                            if (directEve.Session.Structureid != null)
                                return new DirectContainer(directEve, inventory, flag, "StructureCorpHangars");
                            if (directEve.Session.StationId != null)
                                return new DirectContainer(directEve, inventory, flag, "StationCorpHangars");

                            Log.WriteLine("GetCorporationHangar: Structureid and StationId were null");
                            return null;
                        }

                        Log.WriteLine("GetCorporationHangar: itemid == 0");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                        return null;
                    }
                }

                Log.WriteLine("GetCorporationHangar: locationid == null");
                return null;
            }
            catch (Exception ex)
            {
                directEve.Log("Exception [" + ex + "]");
                return null;
            }
        }

        internal static DirectContainer GetCorporationHangarArray(DirectEve directEve, long itemId, string divisionName)
        {
            //string calledFrom = "GetCorporationHangarArray";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            var divisions = directEve.GetLocalSvc("corp").Call("GetDivisionNames");
            for (var i = 1; i <= 7; i++)
                if (string.Compare(divisionName, (string)divisions.DictionaryItem(i), true) == 0)
                    return GetCorporationHangarArray(directEve, itemId, i);

            return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);
        }

        internal static DirectContainer GetCorporationHangarArray(DirectEve directEve, long itemId, int divisionId)
        {
            //string calledFrom = "GetCorporationHangarArray";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            PyObject flag = null;
            switch (divisionId)
            {
                case 1:
                    flag = directEve.Const.FlagHangar;
                    break;

                case 2:
                    flag = directEve.Const.FlagCorpSAG2;
                    break;

                case 3:
                    flag = directEve.Const.FlagCorpSAG3;
                    break;

                case 4:
                    flag = directEve.Const.FlagCorpSAG4;
                    break;

                case 5:
                    flag = directEve.Const.FlagCorpSAG5;
                    break;

                case 6:
                    flag = directEve.Const.FlagCorpSAG6;
                    break;

                case 7:
                    flag = directEve.Const.FlagCorpSAG7;
                    break;
            }

            if (flag == null)
                return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            var inventory = GetInventory(directEve, "GetInventoryFromId", itemId);
            return new DirectContainer(directEve, inventory, flag, "POSCorpHangar_" + itemId + "_" + (divisionId - 1));
        }

        /// <summary>
        ///     Get the item hangar container
        /// </summary>
        /// <param name="directEve"></param>
        /// <returns></returns>
        internal static DirectContainer GetItemHangar(DirectEve directEve)
        {
            //string calledFrom = "GetItemHangar";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            var inStructure = directEve.Session.Structureid.HasValue;
            var name = inStructure ? "StructureItemHangar" : "StationItems";
            var id = inStructure ? directEve.Session.Structureid.Value : (long)directEve.Const.ContainerHangar;
            var inventory = GetInventory(directEve, "GetInventory", id);
            return new DirectContainer(directEve, inventory, directEve.Const.FlagHangar, name);
        }

        /// <summary>
        ///     Get the ship hangar container
        /// </summary>
        /// <param name="directEve"></param>
        /// <returns></returns>
        internal static DirectContainer GetShipHangar(DirectEve directEve)
        {
            //string calledFrom = "GetShipHangar";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            var inStructure = directEve.Session.Structureid.HasValue;
            var name = inStructure ? "StructureShipHangar" : "StationShips";
            var id = inStructure ? directEve.Session.Structureid.Value : (long)directEve.Const.ContainerHangar;
            var inventory = GetInventory(directEve, "GetInventory", id);
            return new DirectContainer(directEve, inventory, directEve.Const.FlagHangar, name);
        }

        /// <summary>
        ///     Get the ship's cargo container
        /// </summary>
        /// <param name="directEve"></param>
        /// <returns></returns>
        internal static DirectContainer GetShipsCargo(DirectEve directEve)
        {
            //string calledFrom = "GetShipsCargo";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            if (!directEve.Session.ShipId.HasValue)
                return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            var inventory = GetInventory(directEve, "GetInventoryFromId", directEve.Session.ShipId.Value);
            return new DirectContainer(directEve, inventory, directEve.Const.FlagCargo, "ActiveShipCargo");
        }

        /// <summary>
        ///     Get the ship's drone container
        /// </summary>
        /// <param name="directEve"></param>
        /// <returns></returns>
        internal static DirectContainer GetShipsDroneBay(DirectEve directEve)
        {
            //string calledFrom = "GetShipsDroneBay";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            if (!directEve.Session.ShipId.HasValue)
                return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            var inventory = GetInventory(directEve, "GetInventoryFromId", directEve.Session.ShipId.Value);
            return new DirectContainer(directEve, inventory, directEve.Const.FlagDroneBay, "ShipDroneBay");
        }

        /// <summary>
        ///     Get the ship's fleet hangar
        /// </summary>
        /// <param name="directEve"></param>
        /// <returns></returns>
        internal static DirectContainer GetShipsFleetHangar(DirectEve directEve)
        {
            //string calledFrom = "GetShipsFleetHangar";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            if (!directEve.Session.ShipId.HasValue)
                return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            PyObject inventory = GetInventory(directEve, "GetInventoryFromId", directEve.Session.ShipId.Value);
            //return new DirectContainer(directEve, inventory, directEve.Const.FlagFleetHangar, "ShipFleetHangar" + directEve.Session.ShipId.Value);
            return new DirectContainer(directEve, inventory, directEve.Const.FlagFleetHangar, "ShipFleetHangar");
        }

        /// <summary>
        ///     Get the ship's modules 'container'
        /// </summary>
        /// <param name="directEve"></param>
        /// <returns></returns>
        internal static DirectContainer GetShipsModules(DirectEve directEve)
        {
            //string calledFrom = "GetShipsModules";
            //if (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            if (!directEve.Session.ShipId.HasValue)
                return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            var inventory = GetInventory(directEve, "GetInventoryFromId", directEve.Session.ShipId.Value);
            return new DirectContainer(directEve, inventory, true);
        }

        /// <summary>
        ///     Get the ship's ore hold
        /// </summary>
        /// <param name="directEve"></param>
        /// <returns></returns>
        internal static DirectContainer GetShipsGeneralMiningHold(DirectEve directEve)
        {
            //string calledFrom = "GetShipsOreHold";
            // (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            if (!directEve.Session.ShipId.HasValue)
                return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            PyObject inventory = GetInventory(directEve, "GetInventoryFromId", directEve.Session.ShipId.Value);
            return new DirectContainer(directEve, inventory, directEve.Const.FlagGeneralMiningHold, "ShipGeneralMiningHold");
        }

        internal static DirectContainer GetShipsMineralHold(DirectEve directEve)
        {
            //string calledFrom = "GetShipsOreHold";
            // (!directEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return null;

            if (!directEve.Session.ShipId.HasValue)
                return new DirectContainer(directEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            PyObject inventory = GetInventory(directEve, "GetInventoryFromId", directEve.Session.ShipId.Value);
            return new DirectContainer(directEve, inventory, directEve.Const.FlagMineralHold, "ShipMineralHold");
        }

        /// <summary>
        ///     Get the inventory object using the specified method (GetInventory or GetInventoryFromId) and an Id (e.g. ship-id,
        ///     etc)
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="method"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static PyObject GetInventory(DirectEve directEve, string method, long id, bool doRemoteCall = true)
        {
            var inventories = directEve.GetLocalSvc("invCache").Attribute("inventories").ToDictionary();
            foreach (var inventory in inventories)
            {
                //directEve.Log(inventory.Key.LogObject());
                //directEve.Log(inventory.Value.LogObject());
                var keyid = (long)inventory.Key.GetItemAt(0);
                // value is a invCacheContainer obj type
                if (keyid != id)
                    continue;

                return inventory.Value;
            }

            if (!doRemoteCall)
                return PySharp.PyZero;

            // Do a threaded call and consider this failed (for now)
            directEve.ThreadedLocalSvcCall("invCache", method, id);
            // Return none
            return PySharp.PyNone;
        }

        public static List<DirectContainer> GetStationContainers(DirectEve directEve)
        {
            List<int> containerGroups = new List<int>() {
                directEve.Const["groupCargoContainer"].ToInt(),
                directEve.Const["groupSecureCargoContainer"].ToInt(),
                directEve.Const["groupAuditLogSecureContainer"].ToInt(),
                directEve.Const["groupFreightContainer"].ToInt(),
                };

            var stationContainers = new List<DirectContainer>();
            var d = directEve.GetLocalSvc("invCache").Attribute("inventories").ToDictionary();
            foreach (var kv in d)
            {
                var invType = directEve.GetInvType(kv.Value["_typeID"].ToInt());
                if (containerGroups.Contains(invType.GroupId))
                {
                    var container = GetContainer(directEve, kv.Value["_itemID"].ToLong());
                    stationContainers.Add(container);
                }
            }
            return stationContainers;
        }

        #endregion Methods
    }
}