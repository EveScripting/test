extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using SC::SharedComponents.Utility;
using SC::SharedComponents.EVE;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Extensions;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.Py;
using System.Xml.Linq;
using EVESharpCore.Questor.Actions.Base;

namespace EVESharpCore.Questor.BackgroundTasks
{
    public static class Scanner
    {
        public static bool ScanGasSites { get; set; } = false;
        public static bool ScanOreSites { get; set; } = false;
        public static bool ScanIceSites { get; set; } = false;
        public static bool ScanCombatSites { get; set; } = false;
        public static bool ScanWormholes { get; set; } = false;
        public static bool ScanRelicSites { get; set; } = false;
        public static bool ScanDataSites { get; set; } = false;
        public static bool ScanHomefrontSites { get; set; } = false;

        public static Dictionary<long, DateTime> AsteroidBeltsWithNoOre = new Dictionary<long, DateTime>();
        public static Dictionary<long, DateTime> AsteroidBeltsWithOtherMiners = new Dictionary<long, DateTime>();
        public static Dictionary<long, DateTime> AsteroidBeltsWithPvP = new Dictionary<long, DateTime>();
        public static Dictionary<long, DateTime> AsteroidBeltsWithNPCs = new Dictionary<long, DateTime>();
        public static Dictionary<long, DateTime> AsteroidBeltsWeAreMiningIn = new Dictionary<long, DateTime>();
        public static Dictionary<long, DateTime> AsteroidsWeWereMining = new Dictionary<long, DateTime>();

        public static Dictionary<string, DateTime> SitesWithNoOre = new Dictionary<string, DateTime>();
        public static Dictionary<string, DateTime> SitesWithNoe = new Dictionary<string, DateTime>();
        public static Dictionary<string, DateTime> SitesWithOtherMiners = new Dictionary<string, DateTime>();
        public static Dictionary<string, DateTime> SitesWithPvP = new Dictionary<string, DateTime>();
        public static Dictionary<string, DateTime> SitesWithNPCs = new Dictionary<string, DateTime>();
        public static Dictionary<string, DateTime> SitesWeAreMiningIn = new Dictionary<string, DateTime>();

        #region Methods

        public static bool? _selectedControllerUsesScanner = null;

        public static bool SelectedControllerUsesScanner
        {
            get
            {
                if (_selectedControllerUsesScanner != null)
                    return _selectedControllerUsesScanner ?? true;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                {
                    _selectedControllerUsesScanner = true;
                    return _selectedControllerUsesScanner ?? true;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecCombatSignaturesController))
                {
                    _selectedControllerUsesScanner = true;
                    return _selectedControllerUsesScanner ?? true;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WSpaceScoutController))
                {
                    _selectedControllerUsesScanner = true;
                    return _selectedControllerUsesScanner ?? true;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.ExplorationNoWeaponsController))
                {
                    _selectedControllerUsesScanner = true;
                    return _selectedControllerUsesScanner ?? true;
                }

                _selectedControllerUsesScanner = false;
                return _selectedControllerUsesScanner ?? false;
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            Log.WriteLine("LoadSettings: ExplorationNoWeaponsBehavior");

            ScanGasSites =
                (bool?)CharacterSettingsXml.Element("scanGasSites") ??
                (bool?)CommonSettingsXml.Element("scanGasSites") ?? false;
            Log.WriteLine("Probe Scanner: scanGasSites [" + ScanGasSites + "]");
            ScanOreSites =
                (bool?)CharacterSettingsXml.Element("scanOreSites") ??
                (bool?)CommonSettingsXml.Element("scanOreSites") ?? false;
            Log.WriteLine("Probe Scanner: scanOreSites [" + ScanOreSites + "]");
            ScanIceSites =
                (bool?)CharacterSettingsXml.Element("scanIceSites") ??
                (bool?)CommonSettingsXml.Element("scanIceSites") ?? false;
            Log.WriteLine("Probe Scanner: scanIceSites [" + ScanIceSites + "]");
            ScanCombatSites =
                (bool?)CharacterSettingsXml.Element("scanCombatSites") ??
                (bool?)CommonSettingsXml.Element("scanCombatSites") ?? false;
            Log.WriteLine("Probe Scanner: scanCombatSites [" + ScanCombatSites + "]");
            ScanWormholes =
                (bool?)CharacterSettingsXml.Element("scanWormholes") ??
                (bool?)CommonSettingsXml.Element("scanWormholes") ?? true;
            Log.WriteLine("Probe Scanner: scanWormholes [" + ScanWormholes + "]");
            ScanRelicSites =
                (bool?)CharacterSettingsXml.Element("scanRelicSites") ??
                (bool?)CommonSettingsXml.Element("scanRelicSites") ?? false;
            Log.WriteLine("Probe Scanner: scanRelicSites [" + ScanRelicSites + "]");
            ScanDataSites =
                (bool?)CharacterSettingsXml.Element("scanDataSites") ??
                (bool?)CommonSettingsXml.Element("scanDataSites") ?? false;
            Log.WriteLine("Probe Scanner: scanDataSites [" + ScanDataSites + "]");
            ScanHomefrontSites =
                (bool?)CharacterSettingsXml.Element("scanHomefrontSites") ??
                (bool?)CommonSettingsXml.Element("scanHomefrontSites") ?? false;
            Log.WriteLine("Probe Scanner: scanHomefrontSites [" + ScanHomefrontSites + "]");
        }



        private static DateTime PerformNextDscan = DateTime.MinValue;
        private static DateTime PerformNextProbeScan = DateTime.MinValue;
        private static DateTime waitUntil = DateTime.MinValue;

        private static bool executedScan = false;

        private static List<DirectDirectionalScanResult> _scanResults;
        private static List<DirectSystemScanResult> _systemScanResults { get; set; }
        public static List<DirectProbeScannerWindowScanResult> ProbeScannerWindowScanResults = new List<DirectProbeScannerWindowScanResult>();

        public static List<DirectSystemScanResult> HistoryOfSystemScanResults = new List<DirectSystemScanResult>();

        public static IEnumerable<DirectSystemScanResult> FilteredSystemScanResults
        {
            get
            {
                IEnumerable<DirectSystemScanResult> _filteredSystemScanResults = new List<DirectSystemScanResult>();

                foreach (var systemScanResult in SystemScanResults)
                {
                    if (DebugConfig.DebugProbeScanner && DirectEve.Interval(5000)) Log.WriteLine("systemScanResult: [" + systemScanResult.Id + "] SignalStrength [" + Math.Round(systemScanResult.SignalStrength, 4) + "] groupId [" + systemScanResult.GroupID + "][" + systemScanResult.GroupName + "] typeId [" + systemScanResult.TypeID + "][" + systemScanResult.TypeName + "]");
                }

                if (SystemScanResults == null)
                    return new List<DirectSystemScanResult>();

                if (SystemScanResults.Count == 0)
                    return new List<DirectSystemScanResult>();

                //
                // to filter out site types we REALLY NEED to ignore signatures in game, otherwise the ones we ignore in code without ignoring in game will make ti impossible to scan correctly
                //
                _filteredSystemScanResults = SystemScanResults
                .Where(r => r.ScanGroup == ScanGroup.Signature) //&&
                                                                //(r.IsUnknownSite ||
                                                                //(ScanGasSites && r.IsGasSite) ||
                                                                //(ScanOreSites && r.IsOreSite) ||
                                                                //(ScanIceSites && r.IsIceSite) ||
                                                                //(ScanCombatSites && r.IsCombatSite) ||
                                                                //(ScanWormholes && r.IsWormhole) ||
                                                                //(ScanRelicSites && r.IsRelicSite) ||
                                                                //(ScanDataSites && r.IsDataSite) ||
                                                                //(ScanHomefrontSites && r.IsHomefrontSite)))
                .OrderByDescending(i => i.SignalStrength);
                //.ThenByDescending(i => i.IsSphereResult);

                return _filteredSystemScanResults;
            }
        }
        public static IEnumerable<DirectSystemScanResult> NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
        public static IEnumerable<DirectSystemScanResult> FullyScannedFilteredSystemScanResults = new List<DirectSystemScanResult>();

        public static bool InvalidateCache()
        {
            //pyProbeScannerPalette = null;
            ProbeScannerWindowScanResults = new List<DirectProbeScannerWindowScanResult>();
            _systemScanResults = null;
            _selectedControllerUsesScanner = null;
            return true;
        }

        public static List<DirectScannerProbe> GetProbes()
        {
            var Probes = new List<DirectScannerProbe>();
            var pyProbes = ESCache.Instance.DirectEve.GetLocalSvc("scanSvc").Attribute("probeTracker").Attribute("probeData").ToDictionary<long>();
            foreach (var pyProbe in pyProbes)
                //DirectEve.Log($"{pyProbe.Value.LogObject()}");
                Probes.Add(new DirectScannerProbe(ESCache.Instance.DirectEve, pyProbe.Value));

            return Probes;
        }

        public static List<DirectSystemScanResult> SystemScanResults
        {
            get
            {
                if (_systemScanResults == null)
                {
                    var _historyOfScanResults = Scanner.HistoryOfSystemScanResults.ToList();
                    _systemScanResults = new List<DirectSystemScanResult>();
                    var pyResults = ESCache.Instance.DirectEve.GetLocalSvc("scanSvc").Call("GetResults").GetItemAt(0).ToList();
                    foreach (var pyResult in pyResults)
                    {
                        var newScanResult = new DirectSystemScanResult(ESCache.Instance.DirectEve, pyResult);
                        _systemScanResults.Add(newScanResult);
                        if (_historyOfScanResults.All(r => r.Id != newScanResult.Id))
                        {
                            Scanner.HistoryOfSystemScanResults.Add(newScanResult);
                            continue;
                        }

                        continue;
                    }

                    foreach (var result in _historyOfScanResults)
                    {
                        //remove stale entries
                        if (ESCache.Instance.DirectEve.Session.SolarSystemId == result.SolarSystemId)
                        {
                            if (_systemScanResults.All(r => r.Id != result.Id && r.SolarSystemId == result.SolarSystemId))
                            {
                                Scanner.HistoryOfSystemScanResults = Scanner.HistoryOfSystemScanResults.Where(i => i.Id != result.Id).ToList();
                                continue;
                            }
                        }

                        continue;
                    }
                }

                return _systemScanResults;
            }
        }


        public static void DecreaseProbeRange()
        {
            foreach (var p in GetProbes()) p.DecreaseProbeRange();
        }

        public static bool IsAnyProbeAtMinRange
        {
            get
            {
                foreach (var p in GetProbes())
                {
                    if (p.IsAtMinRange)
                        return true;
                }
                return false;
            }
        }

        public static bool IsAnyProbeAtMaxRange
        {
            get
            {
                foreach (var p in GetProbes())
                {
                    if (p.IsAtMaxRange)
                        return true;
                }
                return false;
            }
        }

        public static bool LaunchProbesIfNeeded()
        {
            if (GetProbes().Count == 0)
            {
                Log.WriteLine("No probes found in space, launching probes.");
                DirectUIModule probeLauncher = ESCache.Instance.DirectEve.Modules.Find(m => m.GroupId == (int)Group.ProbeLauncher);
                if (probeLauncher == null)
                {
                    Log.WriteLine("No probe launcher found.");
                    return false;
                }

                if (probeLauncher.IsInLimboState || probeLauncher.IsActive)
                {
                    Log.WriteLine("Probe launcher is active or reloading.");
                    return false;
                }

                if (ESCache.Instance.DirectEve.GetShipsCargo() == null)
                {
                    Log.WriteLine("if (DirectEve.GetShipsCargo() == null)");
                    return false;
                }

                if (ESCache.Instance.DirectEve.GetShipsCargo().CanBeStacked)
                {
                    Log.WriteLine("Stacking current ship hangar.");
                    ESCache.Instance.DirectEve.GetShipsCargo().StackShipsCargo();
                    return false;
                }

                if (!ESCache.Instance.DirectEve.ActiveShip.DeactivateCloak())
                {
                    Log.WriteLine("if (!DirectEve.ActiveShip.DeactivateCloak())");
                    return false;
                }

                if (probeLauncher.Charge == null || probeLauncher.ChargeQty < 8)
                {
                    IOrderedEnumerable<DirectItem> probes = ESCache.Instance.DirectEve.GetShipsCargo().Items.Where(i => i.GroupId == (int)Group.Probes).OrderBy(i => i.Stacksize);
                    IOrderedEnumerable<DirectItem> coreProbes = probes.Where(i => i.TypeName.Contains("Core")).OrderBy(i => i.Stacksize);
                    IOrderedEnumerable<DirectItem> combatProbes = probes.Where(i => i.TypeName.Contains("Combat")).OrderBy(i => i.Stacksize);
                    DirectItem charge = coreProbes.FirstOrDefault();
                    if (charge == null)
                    {
                        Log.WriteLine("No core probes found in cargohold.");
                        return false;
                    }

                    if (charge.Stacksize < 8)
                    {
                        Log.WriteLine("Probe stacksize was smaller than 8.");
                        return false;
                    }

                    if (50 > ESCache.Instance.DirectEve.ActiveShip.Entity.Velocity)
                    {
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);
                        return false;
                    }

                    Log.WriteLine("probeLauncher.ChangeAmmo(charge);");
                    probeLauncher.ChangeAmmo(charge);
                    return false;
                }


                if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive && ESCache.Instance.DirectEve.Me.JumpCloakRemainingSeconds > 0)
                {
                    Log.WriteLine("if (DirectEve.Me.IsJumpCloakActive && DirectEve.Me.JumpCloakRemainingSeconds > 0): waiting");
                    return false;
                }

                Log.WriteLine("Launching probes: probeLauncher.Click();");
                probeLauncher.Click();
                return false;
            }

            return true;
        }

        public static bool PullProbesIfNeeded()
        {
            if (GetProbes().Count != 8)
            {
                //TODO: check probe range, can't be retrieved if below CONST.MIN_PROBE_RECOVER_DISTANCE
                Log.WriteLine("Probe amount is != 8, recovering probes.");
                RecoverProbes();
                return false;
            }

            return true;
        }

        public static void IncreaseProbeRange()
        {
            if (!ESCache.Instance.InSpace)
                return;

            foreach (var p in GetProbes()) p.IncreaseProbeRange();
        }

        public static bool IsProbeScanning()
        {
            if (!ESCache.Instance.InSpace)
                return false;

            return ESCache.Instance.DirectEve.GetLocalSvc("scanSvc").Call("IsScanning").ToBool();
        }

        public static bool ClearIgnoredResult()
        {
            if (!ESCache.Instance.InSpace)
                return false;

            return ESCache.Instance.DirectEve.GetLocalSvc("scanSvc").Call("ClearIgnoredResults").ToBool();
        }

        public static void MoveProbesTo(Vec3 dest)
        {
            var pinpointOffsets = GetPinPointCoordinates();
            var i = 0;
            foreach (var p in GetProbes())
            {
                if (DebugConfig.DebugProbeScanner) Log.WriteLine("pinpointOffsets Probe#[" + i + "] x[" + pinpointOffsets[i].X + "] y[" + pinpointOffsets[i].Y + "] z[" + pinpointOffsets[i].Z + "]");
                p.SetLocation(dest + pinpointOffsets[i] * (149598000000 * p.RangeAu));
                i++;
            }
        }

        public static List<Vec3> GetPinPointCoordinates()
        {
            var offsets = new List<Vec3>() { new Vec3(0, 0, 0), new Vec3(0, 0.5, 0), new Vec3(0, -0.5, 0) };

            double GetXValue(double i)
            {
                //return 0.4d * Math.Cos(i * 2 * Math.PI / 5);
                //return 0.3d * Math.Cos(i * 2 * Math.PI / 5);
                return 0.2d * Math.Cos(i * 2 * Math.PI / 5);
            }

            double GetZValue(double i)
            {
                //return 0.4d * Math.Sin(i * 2 * Math.PI / 5);
                //return 0.3d * Math.Sin(i * 2 * Math.PI / 5);
                return 0.2d * Math.Sin(i * 2 * Math.PI / 5);
            }

            for (var i = 0; i < 5; i++) offsets.Add(new Vec3(GetXValue(i), 0, GetZValue(i)));
            return offsets;
        }

        private static int cachedSystemId = 0;

        public static int _probeScansInThisSystem = 0;

        public static int ProbeScansInThisSystem
        {
            get
            {
                if (ESCache.Instance.DirectEve.Session.SolarSystemId == null)
                {
                    Log.WriteLine("if (DirectEve.Session.SolarSystemId == null)");
                    return 0;
                }

                if ((long)ESCache.Instance.DirectEve.Session.SolarSystemId != cachedSystemId)
                {
                    Log.WriteLine("if ((long)DirectEve.Session.SolarSystemId [" + ESCache.Instance.DirectEve.Session.SolarSystemId + "] != cachedSystemId [" + cachedSystemId + "])");
                    //reset to 0;
                    cachedSystemId = (int)ESCache.Instance.DirectEve.Session.SolarSystemId;
                    _probeScansInThisSystem = 0;
                    return 0;
                }

                return _probeScansInThisSystem;
            }
            set
            {
                _probeScansInThisSystem = value;
            }
        }

        public static bool RecoverProbes()
        {
            var probes = GetProbes();
            if (probes.Any())
            {
                return ESCache.Instance.DirectEve.ThreadedCall(ESCache.Instance.DirectEve.GetLocalSvc("scanSvc").Attribute("RecoverProbes"), probes.Select(p => p.ProbeId));
            }

            return false;
        }

        public static void RefreshUI()
        {
            foreach (var p in GetProbes()) p.RefreshUI();
        }

        public static void SetMaxProbeRange()
        {
            foreach (var p in GetProbes()) p.SetMaxProbeRange();
        }

        public static bool ProbeScan()
        {
            if (Scanner.IsProbeScanning())
                return false;

            if (!Scanner.GetProbes().Any())
                return false;

            if (ESCache.Instance.probeScannerWindow == null)
            {
                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdToggleSolarSystemMap);
                return false;
            }

            if (ESCache.Instance.probeScannerWindow.pyProbeScannerPalette == null)
            {
                return false;
            }

            var primaryButton = ESCache.Instance.probeScannerWindow.pyProbeScannerPalette.Attribute("primaryButton");
            var primaryButtonController = ESCache.Instance.probeScannerWindow.pyProbeScannerPalette.Attribute("primaryButtonController");

            if (!primaryButton.IsValid)
                return false;

            if (!primaryButtonController.IsValid)
                return false;

            var disabled = (bool?)primaryButton.Attribute("disabled");
            var buttonLabel = primaryButtonController.Attribute("label").ToUnicodeString();

            if (buttonLabel != "Analyze")
                return false;

            if (!disabled.HasValue || disabled.Value)
                return false;

            if (!ESCache.Instance.DirectEve.ThreadedCall(ESCache.Instance.probeScannerWindow.pyProbeScannerPalette.Attribute("Analyze")))
                return false;

            Scanner.ProbeScansInThisSystem++;
            return true;
        }

        public static bool AutoProbe(bool ScanGasSites = false, bool ScanOreSites = false, bool ScanIceSites = false, bool ScanCombatSites = false, bool ScanWormholes = true, bool ScanRelicSites = false, bool ScanDataSites = false, bool ScanHomefrontSites = false)
        {
            if (waitUntil > DateTime.UtcNow)
            {
                return false;
            }

            if (ESCache.Instance.InStation)
            {
                return false;
            }

            if (ESCache.Instance.InWarp)
            {
                if (DirectEve.Interval(10000)) Log.WriteLine("Waiting, in warp.");
                return false;
            }

            if (ESCache.Instance.probeScannerWindow == null)
            {
                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdToggleSolarSystemMap);
                return false;
            }

            if (ESCache.Instance.probeScannerWindow.pyProbeScannerPalette == null)
            {
                return false;
            }

            if (!LaunchProbesIfNeeded())
            {
                waitUntil = DateTime.UtcNow.AddSeconds(2);
                return false;
            }

            if (!PullProbesIfNeeded())
            {
                waitUntil = DateTime.UtcNow.AddSeconds(2);
                return false;
            }

            // TODO: use cloaks

            if (IsProbeScanning())
            {
                if (DirectEve.Interval(10000)) Log.WriteLine("Probe scan active, waiting.");
                waitUntil = DateTime.UtcNow.AddSeconds(2.5);
                return false;
            }

            NotYetFullyScannedSystemScanResults = FilteredSystemScanResults.Where(r => r.SignalStrength < 1).ToList();
            FullyScannedFilteredSystemScanResults = FilteredSystemScanResults.Where(r => r.SignalStrength >= 1).ToList();

            if (!ESCache.Instance.InWormHoleSpace)
            {
                if (NotYetFullyScannedSystemScanResults.Count() == 1)
                {
                    var site = NotYetFullyScannedSystemScanResults.FirstOrDefault();
                    if (site.IsOreSite && !ScanOreSites)
                    {
                        Log.WriteLine("[" + site.Id + "][" + site.TypeName + "] IsOreSite [" + site.IsOreSite + "] SignalStrength [" + site.SignalStrength + "] Not scanning ore sites");
                        NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
                    }
                    else if (site.IsRelicSite && !ScanRelicSites)
                    {
                        Log.WriteLine("[" + site.Id + "][" + site.TypeName + "] IsRelicSite [" + site.IsRelicSite + "] SignalStrength [" + site.SignalStrength + "] Not scanning relic sites");
                        NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
                    }
                    else if (site.IsDataSite && !ScanDataSites)
                    {
                        Log.WriteLine("[" + site.Id + "][" + site.TypeName + "] IsDataSite [" + site.IsDataSite + "] SignalStrength [" + site.SignalStrength + "] Not scanning data sites");
                        NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
                    }
                    else if (site.IsGasSite && !ScanGasSites)
                    {
                        Log.WriteLine("[" + site.Id + "][" + site.TypeName + "] IsGasSite [" + site.IsGasSite + "] SignalStrength [" + site.SignalStrength + "] Not scanning gas sites");
                        NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
                    }
                    else if (site.IsIceSite && !ScanIceSites)
                    {
                        Log.WriteLine("[" + site.Id + "][" + site.TypeName + "] IsIceSite [" + site.IsIceSite + "] SignalStrength [" + site.SignalStrength + "] Not scanning ice sites");
                        NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
                    }
                    else if (site.IsCombatSite && !ScanCombatSites)
                    {
                        Log.WriteLine("[" + site.Id + "][" + site.TypeName + "] IsCombatSite [" + site.IsCombatSite + "] SignalStrength [" + site.SignalStrength + "] Not scanning combat sites");
                        NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
                    }
                    else if (site.IsWormhole && !ScanWormholes)
                    {
                        Log.WriteLine("[" + site.Id + "][" + site.TypeName + "] IsWormhole [" + site.IsWormhole + "] SignalStrength [" + site.SignalStrength + "] Not scanning wormholes");
                        NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
                    }
                    else if (site.IsHomefrontSite && !ScanHomefrontSites)
                    {
                        Log.WriteLine("[" + site.Id + "][" + site.TypeName + "] IsHomefrontSite [" + site.IsHomefrontSite + "] SignalStrength [" + site.SignalStrength + "] Not scanning homefront sites");
                        NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
                    }
                }
                //bool ScanGasSites = false, bool ScanOreSites = false, bool ScanIceSites = false, bool ScanCombatSites = false, bool ScanWormholes = true, bool ScanRelicSites = false, bool ScanDataSites = false, bool ScanHomefrontSites = false
                else if (NotYetFullyScannedSystemScanResults.All(r =>
                        ((r.IsGasSite && !ScanGasSites) ||
                        (r.IsOreSite && !ScanOreSites) ||
                        (r.IsIceSite && !ScanIceSites) ||
                        (r.IsCombatSite && !ScanCombatSites) ||
                        (r.IsWormhole && !ScanWormholes) ||
                        (r.IsRelicSite && !ScanRelicSites) ||
                        (r.IsDataSite && !ScanDataSites) ||
                        (r.IsHomefrontSite && !ScanHomefrontSites))))
                {
                    Log.WriteLine("All sites left to scan are sites of a type we do not need to finish scanning down");
                    Log.WriteLine("ScanGasSites [" + ScanGasSites + "] ScanOreSites [" + ScanOreSites + "] ScanIceSites [" + ScanIceSites + "] ScanCombatSites [" + ScanCombatSites + "] ScanWormholes [" + ScanWormholes + "] ScanRelicSites [" + ScanRelicSites + "] ScanDataSites [" + ScanDataSites + "] ScanHomefrontSites [" + ScanHomefrontSites + "]");
                    foreach (var site in NotYetFullyScannedSystemScanResults)
                    {
                        Log.WriteLine("[" + site.Id + "][" + site.TypeName + "] IsGasSite [" + site.IsGasSite + "] IsOreSite [" + site.IsOreSite + "] IsIceSite [" + site.IsIceSite + "] IsCombatSite [" + site.IsCombatSite + "] IsWormhole [" + site.IsWormhole + "] IsRelicSite [" + site.IsRelicSite + "] IsDataSite [" + site.IsDataSite + "] IsHomefrontSite [" + site.IsHomefrontSite + "]");
                        continue;
                    }

                    NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
                }
            }

            if (DirectEve.Interval(15000))
            {
                foreach (DirectSystemScanResult tempSystemScanResult in FilteredSystemScanResults)
                {
                    Log.WriteLine("ScanResult: [" + tempSystemScanResult.Id + "] SignalStrength [" + Math.Round(tempSystemScanResult.SignalStrength, 4) + "] groupId [" + tempSystemScanResult.GroupID + "][" + tempSystemScanResult.GroupName + "] typeId [" + tempSystemScanResult.TypeID + "][" + tempSystemScanResult.TypeName + "]");
                }
            }

            var result = NotYetFullyScannedSystemScanResults.FirstOrDefault();
            if (result != null)
            {
                if (false)
                {
                    /**
                    Vector3 nextPlanetToScanFrom = Scanner.NextPlanetToScanFrom;
                    Log.WriteLine("First sig or sig has changed: Reposition and Set 16AU Range");
                    mapViewWindow.SetMaxProbeRange();
                    //mapViewWindow.MoveProbesTo(result.Pos);
                    mapViewWindow.MoveProbesTo(nextPlanetToScanFrom);
                    mapViewWindow.ProbeScan();
                    //Scanner.UpdatePreviousTempScanResults(result);
                    //_currentSig = result.Id;
                    _autoProbeAction.QueueAction();
                    waitUntil = DateTime.UtcNow.AddSeconds(2);
                    return;
                    **/
                }
                else
                {
                    if (IsAnyProbeAtMinRange)
                    {

                        //
                        // we should track the sigs left and somehow skip a signature that we have not successfully scanned after x tries
                        //
                        Vec3 nextPlanetToScanFrom = result.vec3NextPlanetToScanFrom;
                        Log.WriteLine("[" + result.Id + "] SignalStrength [" + Math.Round(result.SignalStrength, 4) + "] Probe reached minimum range. moving planets [" + result.Id + "] groupId [" + result.GroupID + "][" + result.GroupName + "] typeId [" + result.TypeID + "][" + result.TypeName + "]");
                        Scanner.RefreshUI();
                        SetMaxProbeRange();
                        MoveProbesTo(nextPlanetToScanFrom);
                        Log.WriteLine("Status: nextPlanetToScanFrom [" + Scanner.strNextPlanetToScanFrom + "] there are [" + Scanner.PlanetsLeftToScanFromString + "] more planets. Analyze Pushed [" + ProbeScansInThisSystem + "] times");
                        ProbeScan();
                        waitUntil = DateTime.UtcNow.AddSeconds(2);
                        return false;
                    }
                    else
                    {
                        if (result.SignalStrength == 0)
                        {
                            string strOldPlanet = strNextPlanetToScanFrom;
                            Vec3 nextPlanetToScanFrom = result.vec3NextPlanetToScanFrom;
                            Log.WriteLine("[" + result.Id + "] SignalStrength [" + Math.Round(result.SignalStrength, 4) + "] Reposition and Set 16AU Range: skip planet [" + strOldPlanet + "]! moving on to [" + Scanner.strNextPlanetToScanFrom + "]");
                            SetMaxProbeRange();
                            //mapViewWindow.MoveProbesTo(result.Pos);
                            MoveProbesTo(nextPlanetToScanFrom);
                        }
                        else
                        {
                            //
                            //https://www.youtube.com/watch?v=od1Zl9khKSc
                            //
                            Log.WriteLine("Decreasing Probe Range [" + result.Id + "] SignalStrength [" + Math.Round(result.SignalStrength, 4) + "] groupId [" + result.GroupID + "][" + result.GroupName + "] typeId [" + result.TypeID + "][" + result.TypeName + "]");
                            Scanner.RefreshUI();
                            DecreaseProbeRange();
                            MoveProbesTo(result.Pos);
                        }

                        ProbeScan();
                        waitUntil = DateTime.UtcNow.AddSeconds(2);
                        return false;
                    }
                }
            }

            try
            {
                Log.WriteLine("finished auto probing in SolarSystemID [" + ESCache.Instance.DirectEve.Session.SolarSystemId + "].");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            if (FullyScannedFilteredSystemScanResults.Any())
            {
                Log.WriteLine("--------------Filtered-----------------");
                foreach (DirectSystemScanResult SystemScanResult in FullyScannedFilteredSystemScanResults)
                {
                    Log.WriteLine("[" + SystemScanResult.Id + "] SignalStrength [" + Math.Round(SystemScanResult.SignalStrength, 4) + "] groupId [" + SystemScanResult.GroupID + "][" + SystemScanResult.GroupName + "] typeId [" + SystemScanResult.TypeID + "][" + SystemScanResult.TypeName + "]");
                }

                Log.WriteLine("-------------Unfiltered----------------");
                foreach (DirectSystemScanResult SystemScanResult in SystemScanResults)
                {
                    Log.WriteLine("[" + SystemScanResult.Id + "] SignalStrength [" + Math.Round(SystemScanResult.SignalStrength, 4) + "] groupId [" + SystemScanResult.GroupID + "][" + SystemScanResult.GroupName + "] typeId [" + SystemScanResult.TypeID + "][" + SystemScanResult.TypeName + "]");
                }
            }
            else Log.WriteLine("if (!FullyScannedFilteredSystemScanResults.Any())");

            return true;
        }

        private static void SetDirectionalScanSettings(DirectDirectionalScannerWindow directionalScannerWindow)
        {
            //directionalScannerWindow.IncreaseProbeRange(); //why are we setting probe range here?
            return;
        }

        private static List<DirectDirectionalScanResult> ListOfShipsSeenWhileNooneWasInLocal;

        public static void ClearPerPocketCache()
        {
            return;
        }

        private static int ProbeScanAttempts = 0;

        public static bool ProbeScanResultsReady
        {
            get
            {
                if (ProbeScanAttempts >= 1)
                    return true;

                return false;
            }
        }

        public static void ClearPerSystemCache()
        {
            FullyScannedFilteredSystemScanResults = new List<DirectSystemScanResult>();
            NotYetFullyScannedSystemScanResults = new List<DirectSystemScanResult>();
            ListOfShipsSeenWhileNooneWasInLocal = new List<DirectDirectionalScanResult>();
            AnomalyScanResults = new List<DirectSystemScanResult>();
            SignatureScanResults = new List<DirectSystemScanResult>();
            ProbeScanAttempts = 0;
            return;
        }

        private static void CacheDirectionalScanResults(List<DirectDirectionalScanResult> myDirectionalScannerResults)
        {
            if (ESCache.Instance.DirectEve.Session.CharactersInLocal.Count == 0)
            {
                if (!myDirectionalScannerResults.Except(ListOfShipsSeenWhileNooneWasInLocal).Any())
                    return;

                ListOfShipsSeenWhileNooneWasInLocal = myDirectionalScannerResults;
            }

            return;
        }

        public static List<DirectSystemScanResult> AnomalyScanResults = new List<DirectSystemScanResult>();
        public static List<DirectSystemScanResult> SignatureScanResults = new List<DirectSystemScanResult>();
        public static List<DirectSystemScanResult> CachedSystemScanResults = new List<DirectSystemScanResult>();

        public static int AnomalyScanResultCount
        {
            get
            {
                if (AnomalyScanResults == null)
                    return 0;

                if (AnomalyScanResults.Count > 0)
                {
                    return AnomalyScanResults.Count;
                }

                return 0;
            }
        }

        public static int SignatureScanResultCount
        {
            get
            {
                if (SignatureScanResults == null)
                    return 0;

                if (SignatureScanResults.Count > 0)
                {
                    return SignatureScanResults.Count;
                }

                return 0;
            }
        }

        private static bool WeHaveNewAnomalies
        {
            get
            {
                bool boolNewAnoms = !AnomalyScanResults.SequenceEqual(CachedSystemScanResults.Where(i => i.IsAnomaly));
                return boolNewAnoms;
            }
        }

        private static bool WeHaveNewSignatures
        {
            get
            {
                bool boolNewSignatures = !SignatureScanResults.SequenceEqual(CachedSystemScanResults.Where(i => !i.IsAnomaly));
                return boolNewSignatures;
            }
        }

        private static void PerformProbeScan()
        {
            try
            {
                if (PerformNextProbeScan > DateTime.UtcNow)
                {
                    if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (PerformNextProbeScan > DateTime.UtcNow)");
                    return;
                }

                if (ESCache.Instance.Paused)
                {
                    if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.EveAccount.ManuallyPausedViaUI)");
                    return;
                }

                if (ESCache.Instance.InAnomaly)
                {
                    if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.InAnomaly)");
                    return;
                }

                if (ESCache.Instance.InStation)
                {
                    if (DebugConfig.DebugProbeScanner) Log.WriteLine("That doesn't work in stations.");
                    return;
                }

                if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                {
                    if (DebugConfig.DebugProbeScanner) Log.WriteLine("Waiting, in warp.");
                    return;
                }

                if (ESCache.Instance.probeScannerWindow == null)
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdToggleSolarSystemMap);
                    return;
                }

                if (ESCache.Instance.probeScannerWindow.pyProbeScannerPalette == null)
                {
                    if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (Scanner.pyProbeScannerPalette == null)");
                    return;
                }

                if (IsProbeScanning())
                {
                    Log.WriteLine("Probe scan active, waiting.");
                    PerformNextProbeScan = DateTime.UtcNow.AddSeconds(15);
                    return;
                }

                ProbeScanAttempts++;
                Log.WriteLine("ProbeScanAttempts [" + ProbeScanAttempts + "]");
                CachedSystemScanResults = SystemScanResults;

                if (WeHaveNewAnomalies)
                {
                    int intAnomNumber = 0;
                    AnomalyScanResults = new List<DirectSystemScanResult>();
                    foreach (DirectSystemScanResult myAnom in CachedSystemScanResults.Where(r => r.ScanGroup == ScanGroup.Anomaly))
                    {
                        intAnomNumber++;
                        if (DebugConfig.DebugProbeScanner) Log.WriteLine("ProbeScan: [" + intAnomNumber + "][" + myAnom.Id + "] GroupName [" + myAnom.GroupName + "] SignalStrength [" + Math.Round(myAnom.SignalStrength, 2) + "]");
                        AnomalyScanResults.Add(myAnom);
                        continue;
                    }
                }

                if (WeHaveNewSignatures)
                {
                    int intSignatureNumber = 0;
                    SignatureScanResults = new List<DirectSystemScanResult>();
                    foreach (DirectSystemScanResult mySignature in CachedSystemScanResults.Where(r => r.ScanGroup == ScanGroup.Signature))
                    {
                        intSignatureNumber++;
                        if (DebugConfig.DebugProbeScanner) Log.WriteLine("ProbeScan: [" + intSignatureNumber + "][" + mySignature.Id + "] GroupName [" + mySignature.GroupName + "]");
                        SignatureScanResults.Add(mySignature);
                        continue;
                    }
                }

                PerformNextProbeScan = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(5000, 7000));
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void CloseProbeScannerWindow()
        {
            try
            {
                //close solar system map view window
                if (ESCache.Instance.SolarSystemMapPanelWindow != null)
                {
                    Log.WriteLine("Closing SolarSystemMapPanelWindow");
                    if (DirectEve.Interval(7000)) ESCache.Instance.SolarSystemMapPanelWindow.Close();
                }

                if (ESCache.Instance.probeScannerWindow != null)
                {
                    Log.WriteLine("Closing ProbeScannerWindow");
                    if (DirectEve.Interval(7000)) ESCache.Instance.probeScannerWindow.Close();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
                return;
            }
        }

        private static void PerformDirectionalScan()
        {
            try
            {
                if (PerformNextDscan > DateTime.UtcNow)
                    return;

                if (DebugConfig.DebugDisableCleanup) Log.WriteLine("PerformDirectionalScan()");
                if (ESCache.Instance.directionalScannerWindow == null)
                    return;

                if (executedScan)
                {
                    List<DirectDirectionalScanResult> ListDirectionalScanResults = ESCache.Instance.directionalScannerWindow.DirectionalScanResults;
                    int ShipsFoundOnScan = 0;
                    if (ListDirectionalScanResults.Any(i => i.CategoryId == (int)CategoryID.Ship && ESCache.Instance.Entities.All(x => x.Name != i.Name)))
                        ShipsFoundOnScan = ListDirectionalScanResults.Count(i => i.CategoryId == (int)CategoryID.Ship && ESCache.Instance.Entities.All(x => x.Name != i.Name));

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    {
                        int ShipsGuristasHuntPodsFoundOnScan = 0;
                        if (ListDirectionalScanResults.Any(i => i.GroupId == (int)Group.GuristasHuntCapsule && ESCache.Instance.Entities.All(x => x.Name != i.Name)))
                            ShipsGuristasHuntPodsFoundOnScan = ListDirectionalScanResults.Count(i => i.GroupId == (int)Group.GuristasHuntCapsule && ESCache.Instance.Entities.All(x => x.Name != i.Name));

                        Log.WriteLine($"D-Scan executed. [" + ListDirectionalScanResults.Count + "] items [" + ShipsFoundOnScan + "] Hunt Pods found");
                    }
                    else
                    {
                        Log.WriteLine($"D-Scan executed. [" + ListDirectionalScanResults.Count + "] items [" + ShipsFoundOnScan + "] ships found");
                    }

                    CacheDirectionalScanResults(ListDirectionalScanResults);

                    int intResult = 0;
                    foreach (DirectDirectionalScanResult directionalScanResult in ListDirectionalScanResults)
                    {
                        intResult++;
                        if (directionalScanResult.CategoryId != (int)CategoryID.Ship && directionalScanResult.CategoryId != (int)CategoryID.Entity)
                            continue;

                        /**
                        if (directionalScanResult.GroupId != (int)Group.Frigate &&
                            directionalScanResult.GroupId != (int)Group.AssaultShip &&
                            directionalScanResult.GroupId != (int)Group.Interceptor &&
                            directionalScanResult.GroupId != (int)Group.Interdictor &&
                            directionalScanResult.GroupId != (int)Group.CovertOpsFrigate &&
                            directionalScanResult.GroupId != (int)Group.ExpeditionFrigate &&
                            directionalScanResult.GroupId != (int)Group.Destroyer &&
                            directionalScanResult.GroupId != (int)Group.CommandDestroyer &&
                            directionalScanResult.GroupId != (int)Group.TacticalDestroyer &&
                            directionalScanResult.GroupId != (int)Group.ElectronicAttackShip &&
                            directionalScanResult.GroupId != (int)Group.Cruiser &&
                            directionalScanResult.GroupId != (int)Group.HeavyAssaultShip &&
                            directionalScanResult.GroupId != (int)Group.HeavyInterdictor &&
                            directionalScanResult.GroupId != (int)Group.CombatReconShip &&
                            directionalScanResult.GroupId != (int)Group.ForceReconShip &&
                            directionalScanResult.GroupId != (int)Group.Logistics &&
                            directionalScanResult.GroupId != (int)Group.StrategicCruiser &&
                            directionalScanResult.GroupId != (int)Group.CommandShip &&
                            directionalScanResult.GroupId != (int)Group.Battlecruiser &&
                            directionalScanResult.GroupId != (int)Group.Battleship &&
                            directionalScanResult.GroupId != (int)Group.BlackOps &&
                            directionalScanResult.GroupId != (int)Group.Marauder &&
                            directionalScanResult.GroupId != (int)Group.Carrier &&
                            directionalScanResult.GroupId != (int)Group.Titan &&
                            directionalScanResult.GroupId != (int)Group.Dreadnaught)
                            continue;
                        **/
                        if (directionalScanResult.GroupId == (int)Group.TransportShip ||
                            directionalScanResult.GroupId != (int)Group.Freighter ||
                            directionalScanResult.GroupId != (int)Group.JumpFreighter ||
                            directionalScanResult.GroupId != (int)Group.Industrial ||
                            directionalScanResult.GroupId != (int)Group.Capsule)
                            continue;

                        if (ListOfShipsSeenWhileNooneWasInLocal.Any(i => i.Name == directionalScanResult.Name && i.TypeId == directionalScanResult.TypeId))
                            continue;

                        Log.WriteLine("[" + intResult + "][" + directionalScanResult.TypeName + "][" + directionalScanResult.TypeId + "] Name [" + directionalScanResult.Name + "]");
                    }

                    executedScan = false;
                    return;
                }

                if (!ESCache.Instance.directionalScannerWindow.IsDirectionalScanning())
                {
                    //SetDirectionalScanSettings(ESCache.Instance.directionalScannerWindow);
                    Log.WriteLine("Executed directional scan.");
                    ESCache.Instance.directionalScannerWindow.DirectionalScan();
                    executedScan = true;
                    PerformNextDscan = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(4000, 6000));
                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
                return;
            }
        }

        private static bool ShouldWeDoADirectionalScan()
        {
            try
            {
                if (ESCache.Instance.Paused)
                    return false;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    return false;

                if (ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController))
                    return false;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HydraController))
                    return false;

                //Includes QuestorController and similar
                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    return false;

                if (ESCache.Instance.InWormHoleSpace)
                    return true;

                if (ESCache.Instance.DirectEve.Session.SolarSystem.IsLowSecuritySpace)
                    return true;

                if (ESCache.Instance.DirectEve.Me.IsAtWar)
                    return true;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.FindGuriastasHuntPodsController))
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ShouldWeDoAProbeScan()
        {
            if (ESCache.Instance.Paused)
                return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
            {
                if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.SelectedController == AbyssalDeadspaceController)");
                return false;
            }

            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.InAbyssalDeadspace)");
                return false;
            }

            if (ESCache.Instance.InAnomaly)
            {
                if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.InAnomaly)");
                return false;
            }

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController)) if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController))
                {
                    if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.SelectedController == CombatDontMoveController)");
                    return false;
                }

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HydraController))
                return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.SalvageGridController))
            {
                if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.SelectedController == SalvageGridController)");
                return false;
            }

            //Includes QuestorController and similar
            if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                return false;

            if (ESCache.Instance.MyShipEntity.GroupId == (int)Group.TransportShip)
            {
                if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.MyShipEntity.GroupId == (int)GroupID.TransportShip)");
                return false;
            }

            if (ESCache.Instance.MyShipEntity.GroupId == (int)Group.Freighter)
            {
                if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.MyShipEntity.GroupId == (int)GroupID.TransportShip)");
                return false;
            }

            if (ESCache.Instance.MyShipEntity.GroupId == (int)Group.BlockadeRunner)
            {
                if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.MyShipEntity.GroupId == (int)GroupID.BlockadeRunner)");
                return false;
            }

            if (ESCache.Instance.MyShipEntity.GroupId == (int)Group.Industrial)
            {
                if (DebugConfig.DebugProbeScanner) Log.WriteLine("if (ESCache.Instance.MyShipEntity.GroupId == (int)GroupID.Industrial)");
                return false;
            }

            if (State.CurrentHighSecAnomalyBehaviorState == HighSecAnomalyBehaviorState.ExecuteMission)
                return false;

            return true;
        }

        public static Dictionary<long, bool> PlanetsWeHaveScannedFrom = new Dictionary<long, bool>();

        private static void InitializePlanets()
        {
            if (ESCache.Instance.Planets.Count > 0)
            {
                foreach (EntityCache planet in ESCache.Instance.Planets)
                {
                    if (PlanetsWeHaveScannedFrom.ContainsKey(planet.Id))
                        PlanetsWeHaveScannedFrom.Clear();
                }
            }
        }

        private static List<DirectSystemScanResult> PreviousTempScanResults = new List<DirectSystemScanResult>();

        private static void UpdatePreviousTempScanResults(DirectSystemScanResult result)
        {
            if (PreviousTempScanResults.Contains(result))
            {
                PreviousTempScanResults.Remove(result);
                PreviousTempScanResults.Add(result);
            }
        }

        public static string strNextPlanetToScanFrom { get; set; } = string.Empty;

        public static string PlanetsLeftToScanFromString = string.Empty;

        public static int PlanetsLeftToScanFrom()
        {
            int planetsLeftToScanFrom = 0;
            foreach (EntityCache planet in ESCache.Instance.Planets)
            {
                if (!PlanetsWeHaveScannedFrom.ContainsKey(planet.Id))
                {
                    planetsLeftToScanFrom++;
                }
            }

            return planetsLeftToScanFrom;
        }

        public static DateTime LastScanAction = DateTime.UtcNow;

        private static bool ShouldWeCloseMapViewWindow()
        {
            if (ESCache.Instance.Paused)
                return false;

            if (!ESCache.Instance.InWarp)
                return false;

            if (ESCache.Instance.InWormHoleSpace)
                return false;

            if (!ShouldWeDoADirectionalScan() && !ShouldWeDoAProbeScan())
                if (DateTime.UtcNow > LastScanAction.AddMinutes(2))
                    return true;

            return false;
        }

        public static void ProcessState()
        {
            if (!ESCache.Instance.DirectEve.Session.IsReady)
                return;

            if (ESCache.Instance.InSpace)
            {
                if (ShouldWeDoADirectionalScan())
                    PerformDirectionalScan();

                if (ShouldWeDoAProbeScan())
                    PerformProbeScan();

                if (ShouldWeCloseMapViewWindow())
                    CloseProbeScannerWindow();
            }
        }

        #endregion Methods
    }
}
