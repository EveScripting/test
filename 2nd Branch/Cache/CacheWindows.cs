extern alias SC;

using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Cache
{
    public partial class ESCache
    {
        #region Fields

        private DirectLoyaltyPointStoreWindow _lpStore;
        private DirectFittingManagerWindow _fittingManagerWindow;

        #endregion Fields

        #region Properties

        /**
        private DirectMapViewWindow myDirectMapViewWindowForDScan
        {
            get
            {
                DirectWindow win = ESCache.Instance.DirectEve.Windows.Find(w => w.GetType() == typeof(DirectMapViewWindow));

                if (win == null)
                {
                    Log.WriteLine("Opening MapViewWindow.");
                    if (ESCache.Instance.DirectEve.IsDirectionalScannerWindowOpen || ESCache.Instance.DirectEve.IsProbeScannerWindowOpen)
                    {
                        Log.WriteLine($"The DirectionalScanner and the ProbeScanner needs to be docked in within the MapViewWindow.");
                        return null;
                    }

                    if (ESCache.Instance.DirectEve.IsDirectionalScannerWindowOpen)
                    {
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.ToggleProbeScanner);
                        return null;
                    }

                    if (ESCache.Instance.DirectEve.IsProbeScannerWindowOpen)
                    {
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenDirectionalScanner);
                        return null;
                    }

                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenDirectionalScanner);
                    return null;
                }

                DirectMapViewWindow mapViewWindow = (DirectMapViewWindow)win;

                if (!mapViewWindow.IsProbeScanOpen())
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.ToggleProbeScanner);
                    return null;
                }

                if (!mapViewWindow.IsDirectionalScanOpen())
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenDirectionalScanner);
                    return null;
                }

                if (!mapViewWindow.IsDirectionalScannerDocked() || !mapViewWindow.IsProbeScannerDocked())
                {
                    Log.WriteLine($"The DirectionalScanner and the ProbeScanner needs to be docked in within the MapViewWindow.");
                    return null;
                }

                return mapViewWindow;
            }
        }
        **/

        //SolarSystemMapPanel
        public DirectWindow SolarSystemMapPanelWindow
        {
            get
            {
                DirectWindow _solarSystemMapPanelWindow = ESCache.Instance.Windows.Where(x => x.Name == "solar_system_map_panel").FirstOrDefault();

                if (_solarSystemMapPanelWindow == null)
                {
                    Log.WriteLine("SolarSystemMapPanelWindow not found");
                    foreach (var window in ESCache.Instance.Windows)
                    {
                        if (DirectEve.Interval(5000, 5000, window.Name)) Log.WriteLine("Window name: [" + window.Name + "]");
                    }

                    //if (ESCache.Instance.DirectEve.IsProbeScannerNonDockedWindowOpen)
                    //{
                    //    Log.WriteLine("The ProbeScanner is open but needs to be docked in within the MapViewWindow!");
                    //    return null;
                    //}

                    if (DirectEve.Interval(6000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdToggleSolarSystemMap);
                    return null;
                }


                return _solarSystemMapPanelWindow ?? null;
            }
        }


        public DirectMapViewWindow probeScannerWindow
        {
            get
            {
                DirectMapViewWindow _probeScannerWindow = ESCache.Instance.Windows.OfType<DirectMapViewWindow>().FirstOrDefault();

                if (_probeScannerWindow == null)
                {
                    Log.WriteLine("probeScannerWindow not found");
                    if (DirectEve.Interval(6000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.ToggleProbeScanner);
                    return null;
                }

                return _probeScannerWindow ?? null;
            }
        }

        public DirectDirectionalScannerWindow directionalScannerWindow
        {
            get
            {
                DirectDirectionalScannerWindow _directionalScannerWindow = ESCache.Instance.Windows.OfType<DirectDirectionalScannerWindow>().FirstOrDefault();

                if (_directionalScannerWindow == null)
                {
                    Log.WriteLine("directionalScannerWindow not found");
                    if (DirectEve.Interval(6000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenDirectionalScanner);
                    return null;
                }

                return _directionalScannerWindow ?? null;
            }
        }

        public DirectFittingManagerWindow FittingManagerWindow
        {
            get
            {
                try
                {
                    if (Instance.InStation && Settings.Instance.UseFittingManager)
                    {
                        if (_fittingManagerWindow == null)
                        {
                            if (!Instance.InStation || Instance.InSpace)
                            {
                                Log.WriteLine("Opening Fitting Manager: We are not in station?! There is no Fitting Manager in space, waiting...");
                                return null;
                            }

                            if (Instance.InStation)
                            {
                                if (Instance.Windows.Count > 0)
                                {
                                    if (Instance.Windows.OfType<DirectFittingManagerWindow>().Any())
                                    {
                                        DirectFittingManagerWindow __fittingManagerWindow = Instance.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault();
                                        if (__fittingManagerWindow != null && __fittingManagerWindow.IsReady)
                                        {
                                            _fittingManagerWindow = __fittingManagerWindow;
                                            return _fittingManagerWindow;
                                        }
                                    }

                                    if (DateTime.UtcNow > Time.Instance.NextWindowAction)
                                    {
                                        Log.WriteLine("Opening Fitting Manager Window");
                                        Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(1, 2));
                                        Instance.DirectEve.OpenFittingManager();
                                        Statistics.LogWindowActionToWindowLog("FittingManager", "Opening FittingManager");
                                        return null;
                                    }

                                    if (DebugConfig.DebugFittingMgr)
                                        Log.WriteLine("NextWindowAction is still in the future [" +
                                                      Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds +
                                                      "] sec");
                                    return null;
                                }

                                return null;
                            }

                            return null;
                        }

                        return _fittingManagerWindow;
                    }

                    Log.WriteLine("Opening Fitting Manager: We are not in station?! There is no Fitting Manager in space, waiting...");
                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Unable to define FittingManagerWindow [" + exception + "]");
                    return null;
                }
            }
            set => _fittingManagerWindow = value;
        }

        public DirectLoyaltyPointStoreWindow LpStore
        {
            get
            {
                try
                {
                    if (Instance.InStation)
                    {
                        if (_lpStore == null)
                        {
                            if (!Instance.InStation)
                            {
                                Log.WriteLine("Opening LP Store: We are not in station?! There is no LP Store in space, waiting...");
                                return null;
                            }

                            if (Instance.InStation)
                            {
                                if (Instance.Windows.Count > 0)
                                {
                                    _lpStore = Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();

                                    if (_lpStore == null)
                                    {
                                        if (DateTime.UtcNow > Time.Instance.NextLPStoreAction)
                                        {
                                            if (!Instance.OkToInteractWithEveNow)
                                            {
                                                if (DebugConfig.DebugInteractWithEve) Log.WriteLine("LPStore: !OkToInteractWithEveNow");
                                                return null;
                                            }

                                            if (Instance.DirectEve.ExecuteCommand(DirectCmd.OpenLpstore))
                                            {
                                                Log.WriteLine("Opening loyalty point store");
                                                Time.Instance.NextLPStoreAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(30, 240));
                                                ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                                                Statistics.LogWindowActionToWindowLog("LPStore", "Opening LPStore");
                                                return null;
                                            }

                                            return null;
                                        }

                                        return null;
                                    }

                                    return _lpStore;
                                }

                                return null;
                            }

                            return null;
                        }

                        return _lpStore;
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Unable to define LPStore [" + exception + "]");
                    return null;
                }
            }
            private set => _lpStore = value;
        }

        private DirectFleetWindow _fleetWindow;

        public DirectFleetWindow FleetWindow
        {
            get
            {
                try
                {
                    if (_fleetWindow == null)
                    {
                        if (Instance.Windows.Count > 0)
                        {
                            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                                return null;

                            if (Time.Instance.Started_DateTime.AddSeconds(45) > DateTime.UtcNow)
                                return null;

                            _fleetWindow = Instance.Windows.OfType<DirectFleetWindow>().FirstOrDefault();

                            if (_fleetWindow == null)
                            {
                                if (DateTime.UtcNow > Time.Instance.NextFleetWindowAction)
                                {
                                    if (!Instance.OkToInteractWithEveNow)
                                    {
                                        if (DebugConfig.DebugInteractWithEve) Log.WriteLine("FleetWindow: !OkToInteractWithEveNow");
                                        return null;
                                    }

                                    if (Instance.DirectEve.ExecuteCommand(DirectCmd.OpenFleet))
                                    {
                                        Log.WriteLine("Opening fleet window");
                                        Time.Instance.NextFleetWindowAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(15, 60));
                                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                                        Statistics.LogWindowActionToWindowLog("FleetWindow", "Opening FleetWindow");
                                        return null;
                                    }

                                    return null;
                                }

                                return null;
                            }

                            return _fleetWindow;
                        }

                        return null;
                    }

                    return _fleetWindow;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public DirectMarketWindow MarketWindow { get; set; }
        public DirectContainerWindow PrimaryInventoryWindow { get; set; }

        #endregion Properties

        #region Methods

        public bool CloseFittingManager()
        {
            if (Settings.Instance.UseFittingManager)
            {
                if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
                    return false;

                if (Instance.Windows.OfType<DirectFittingManagerWindow>().Any())
                {
                    if (DirectEve.Interval(3000))
                    {
                        Log.WriteLine("Closing Fitting Manager Window");
                        Instance.FittingManagerWindow.Close();
                        Statistics.LogWindowActionToWindowLog("FittingManager", "Closing FittingManager");
                        Instance.FittingManagerWindow = null;
                        Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
                        return true;
                    }

                    return false;
                }

                return true;
            }

            return true;
        }

        public bool CloseLPStore()
        {
            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
                return false;

            if (!Instance.InStation)
            {
                Log.WriteLine("Closing LP Store: We are not in station?!");
                return false;
            }

            if (Instance.InStation)
            {
                Instance.LpStore = Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
                if (Instance.LpStore != null)
                {
                    if (DirectEve.Interval(3000))
                    {
                        Log.WriteLine("Closing loyalty point store");
                        Instance.LpStore.Close();
                        Statistics.LogWindowActionToWindowLog("LPStore", "Closing LPStore");
                        return false;
                    }

                    return false;
                }

                return true;
            }

            return true;
        }

        public bool CloseMarket()
        {
            if (DateTime.UtcNow < Time.Instance.NextWindowAction)
                return false;

            if (Instance.InStation)
            {
                Instance.MarketWindow = Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                if (Instance.MarketWindow == null)
                    return true;

                if (DirectEve.Interval(3000))
                {
                    Instance.MarketWindow.Close();
                    Statistics.LogWindowActionToWindowLog("MarketWindow", "Closing MarketWindow");
                    return true;
                }

                return false;
            }

            return true;
        }

        public bool ClosePrimaryInventoryWindow()
        {
            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
                return false;
            try
            {
                foreach (DirectWindow window in Instance.Windows)
                    if (window.Guid.Contains("form.Inventory"))
                    {
                        if (DebugConfig.DebugHangars)
                            Log.WriteLine("ClosePrimaryInventoryWindow: Closing Primary Inventory Window Named [" + window.Name + "]");
                        window.Close();
                        Statistics.LogWindowActionToWindowLog("Inventory (main)", "Close Inventory");
                        Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddMilliseconds(500);
                        return false;
                    }

                return true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Unable to complete ClosePrimaryInventoryWindow [" + exception + "]");
                return false;
            }
        }

        public bool DebugInventoryWindows()
        {
            Log.WriteLine("DebugInventoryWindows: *** Start Listing Inventory Windows ***");
            int windowNumber = 0;
            foreach (DirectWindow window in Instance.Windows)
                if (window.Guid.ToLower().Contains("inventory"))
                {
                    windowNumber++;
                    Log.WriteLine("----------------------------  #[" + windowNumber + "]");
                    Log.WriteLine("DebugInventoryWindows.Name:    [" + window.Name + "]");
                    Log.WriteLine("DebugInventoryWindows.Type:    [" + window.Guid + "]");
                    Log.WriteLine("DebugInventoryWindows.Caption: [" + window.Caption + "]");
                }
            Log.WriteLine("DebugInventoryWindows: ***  End Listing Inventory Windows  ***");
            return true;
        }

        public DirectWindow GetWindowByCaption(string caption)
        {
            return Windows.Find(w => w.Caption.Contains(caption));
        }

        public DirectWindow GetWindowByName(string name)
        {
            DirectWindow WindowToFind = null;
            try
            {
                if (Instance.Windows.Count == 0)
                    return null;

                if (name == "Local")
                    WindowToFind = Windows.Find(w => w.Name.StartsWith("chatchannel_local"));

                if (WindowToFind == null)
                    WindowToFind = Windows.Find(w => w.Name == name);

                if (WindowToFind != null)
                    return WindowToFind;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }

            return null;
        }

        public bool ListInvTree()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
                {
                    if (DebugConfig.DebugHangars)
                        Log.WriteLine("Debug: if (DateTime.UtcNow < NextOpenHangarAction)");
                    return false;
                }

                if (DebugConfig.DebugHangars)
                    Log.WriteLine("Debug: about to: if (!Cache.Instance.OpenInventoryWindow");

                if (!Instance.OpenInventoryWindow()) return false;

                Instance.PrimaryInventoryWindow =
                    (DirectContainerWindow)Instance.Windows.Find(w => w.Guid.Contains("form.Inventory") && w.Name.Contains("Inventory"));

                if (Instance.PrimaryInventoryWindow != null && Instance.PrimaryInventoryWindow.IsReady)
                {
                    List<long> idsInInvTreeView = Instance.PrimaryInventoryWindow.GetIdsFromTree(false);
                    if (DebugConfig.DebugHangars)
                        Log.WriteLine("Debug: IDs Found in the Inv Tree [" + idsInInvTreeView.Count + "]");

                    if (Instance.PrimaryInventoryWindow.ExpandCorpHangarView())
                    {
                        Statistics.LogWindowActionToWindowLog("Corporate Hangar", "ExpandCorpHangar executed");
                        Log.WriteLine("ExpandCorpHangar executed");
                        Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(4);
                        return false;
                    }

                    foreach (long itemInTree in idsInInvTreeView)
                        Log.WriteLine("ID: " + itemInTree);
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

        public bool OpenContainerInSpace(EntityCache containerToOpen)
        {
            if (DateTime.UtcNow < Time.Instance.NextLootAction)
                return false;

            if (Instance.InSpace && containerToOpen.Distance <= (int)Distances.ScoopRange)
            {
                Instance.ContainerInSpace = Instance.DirectEve.GetContainer(containerToOpen.Id);

                if (Instance.ContainerInSpace != null)
                {
                    if (Instance.ContainerInSpace.Window == null)
                    {
                        if (containerToOpen.OpenCargo())
                        {
                            Log.WriteLine("Opening Container: waiting [" +
                                          Math.Round(Time.Instance.NextLootAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + " sec]");
                            return false;
                        }

                        return false;
                    }

                    if (!Instance.ContainerInSpace.Window.IsReady)
                    {
                        Log.WriteLine("Container window is not ready");
                        return false;
                    }

                    if (Instance.ContainerInSpace.Window.IsPrimary())
                    {
                        Log.WriteLine("Opening Container window as secondary");
                        Instance.ContainerInSpace.Window.OpenAsSecondary();
                        Statistics.LogWindowActionToWindowLog("ContainerInSpace", "Opening ContainerInSpace");
                        Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
                        return true;
                    }
                }

                return true;
            }
            Log.WriteLine("Not in space or not in scoop range");
            return true;
        }

        public bool OpenIndustryWindow()
        {
            if (!ESCache.Instance.InStation)
            {
                Log.WriteLine("We are not in station?!");
                return false;
            }

            if (ESCache.Instance.InStation)
            {
                var industryWindow = ESCache.Instance.Windows.OfType<DirectIndustryWindow>().FirstOrDefault();
                if (industryWindow == null)
                {
                    /**
                    if (Time.Instance.LastLoginRewardClaim.AddHours(5) > DateTime.UtcNow)
                    {
                        if (DirectEve.Interval(300000)) Log.WriteLine("less than 5 hours have passed since our last LoginRewards check: waiting ~30min");
                        return false;
                    }
                    **/

                    if (DirectEve.Interval(5000, 7000))
                    {
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenIndustry))
                        {
                            Log.WriteLine("Opening Industry Window");
                        }

                        return false;
                    }

                    return false;
                }

                if (DebugConfig.DebugWindows) Log.WriteLine("industryWindow != null");
                return true;
            }

            return true;
        }

        public bool OpenTheAgencyWindow()
        {
            var theAgencyWindow = ESCache.Instance.Windows.OfType<DirectTheAgencyWindow>().FirstOrDefault();
            if (theAgencyWindow == null)
            {
                /**
                if (Time.Instance.LastLoginRewardClaim.AddHours(5) > DateTime.UtcNow)
                {
                    if (DirectEve.Interval(300000)) Log.WriteLine("less than 5 hours have passed since our last LoginRewards check: waiting ~30min");
                    return false;
                }
                **/

                if (DirectEve.Interval(15000, 17000))
                {
                    if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenAgencyNew))
                    {
                        Log.WriteLine("Opening The Agency Window");
                        return false;
                    }

                    Log.WriteLine("Waiting for The Agency Window");
                    return false;
                }

                Log.WriteLine("Waiting for The Agency Window!");
                return false;
            }

            //Log.WriteLine("theAgencyWindow != null");
            return true;
        }

        private bool OpenInventoryWindow()
        {
            if (Instance.Windows.Count > 0)
            {
                Instance.PrimaryInventoryWindow =
                    (DirectContainerWindow)Instance.Windows.Find(w => w.Guid.Contains("form.Inventory") && w.Name.Contains("Inventory"));

                if (Instance.PrimaryInventoryWindow == null)
                {
                    if (!Instance.OkToInteractWithEveNow)
                    {
                        if (DebugConfig.DebugInteractWithEve) Log.WriteLine("LPStore: !OkToInteractWithEveNow");
                        return false;
                    }

                    if (Instance.DirectEve.ExecuteCommand(DirectCmd.OpenInventory))
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("Cache.Instance.InventoryWindow is null, opening InventoryWindow");
                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                        Statistics.LogWindowActionToWindowLog("Inventory (main)", "Open Inventory");
                        Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(2, 3));
                        Log.WriteLine("Opening Inventory Window: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                        return false;
                    }

                    return false;
                }

                if (Instance.PrimaryInventoryWindow != null)
                {
                    if (DebugConfig.DebugHangars) Log.WriteLine("Cache.Instance.InventoryWindow exists");
                    if (Instance.PrimaryInventoryWindow.IsReady)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("Cache.Instance.InventoryWindow exists and is ready");
                        return true;
                    }

                    return false;
                }

                return false;
            }

            return false;
        }

        public bool OpenMarket()
        {
            if (DateTime.UtcNow < Time.Instance.NextWindowAction)
                return false;

            if (Instance.InStation)
            {
                Instance.MarketWindow = Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                if (Instance.MarketWindow == null)
                {
                    if (!Instance.OkToInteractWithEveNow)
                    {
                        if (DebugConfig.DebugInteractWithEve) Log.WriteLine("OpenMarket: !OkToInteractWithEveNow");
                        return false;
                    }

                    if (Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket))
                    {
                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                        Statistics.LogWindowActionToWindowLog("MarketWindow", "Opening MarketWindow");
                        Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(2, 3));
                        Log.WriteLine("Opening Market Window: waiting [" + Math.Round(Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                        return false;
                    }

                    return false;
                }

                return true;
            }

            return false;
        }

        #endregion Methods
    }
}