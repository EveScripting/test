extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using SC::SharedComponents.Extensions;
using System;
using System.Linq;
using System.Xml.Linq;
using SC::SharedComponents.EVE;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Behaviors
{
    public class WSpaceScoutBehavior
    {
        #region Constructors

        private WSpaceScoutBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        private static ActionQueueAction _autoProbeAction;
        private static string _currentSig;

        #endregion Fields

        #region Properties

        public static string HomeBookmark { get; set; }

        public static string ScanningSpotBookmark { get; set; }

        #endregion Properties

        #region Methods

        public static bool ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentWSpaceScoutBehaviorState != _StateToSet)
                {
                    if (_StateToSet == WSpaceScoutBehaviorState.GotoHomeBookmark)
                        State.CurrentTravelerState = TravelerState.Idle;

                    Log.WriteLine("New WSpaceScoutBehaviorState [" + _StateToSet + "]");
                    State.CurrentWSpaceScoutBehaviorState = _StateToSet;
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
            Log.WriteLine("Looking for XML Setting similar to: <HomeBookmark>HomeBookmark</HomeBookmark>");
            HomeBookmark =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CharacterSettingsXml.Element("HomeBookmark") ??
                (string)CommonSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            Log.WriteLine("Looking for XML Setting similar to: <ScanningSpotBookmark>ScanSpotBookmark</ScanningSpotBookmark>");
            ScanningSpotBookmark =
                (string)CharacterSettingsXml.Element("ScanningSpotBookmark") ?? (string)CharacterSettingsXml.Element("ScanningSpotBookmark") ??
                (string)CommonSettingsXml.Element("ScanningSpotBookmark") ?? (string)CommonSettingsXml.Element("ScanningSpotBookmark") ?? "ScanningSpotBookmark";
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugWSpaceScoutBehavior) Log.WriteLine("State.CurrentWSpaceScoutBehaviorState is [" + State.CurrentWSpaceScoutBehaviorState + "]");

                switch (State.CurrentWSpaceScoutBehaviorState)
                {
                    case WSpaceScoutBehaviorState.Idle:
                        //IdleState();
                        break;

                    case WSpaceScoutBehaviorState.Start:
                        //StartState();
                        break;

                    case WSpaceScoutBehaviorState.Switch:
                        SwitchState();
                        break;

                    case WSpaceScoutBehaviorState.Arm:
                        ArmScoutState();
                        break;

                    case WSpaceScoutBehaviorState.IsItSafeToUndock:
                        IsItSafeToUndock();
                        break;

                    case WSpaceScoutBehaviorState.GotoScanningSpot:
                        GotoScanningSpotState();
                        break;

                    case WSpaceScoutBehaviorState.ScanSignatures:
                        ScanSignaturesState();
                        break;

                    case WSpaceScoutBehaviorState.BookmarkSites:
                        break;

                    case WSpaceScoutBehaviorState.GotoHomeBookmark:
                        GotoHomeBookmarkState();
                        break;

                    case WSpaceScoutBehaviorState.UnloadLoot:
                        UnloadLootScoutState();
                        break;

                    case WSpaceScoutBehaviorState.Default:
                        ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void ArmScoutState()
        {
            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                Arm.ChangeArmState(ArmState.Begin, true, null);
            }

            if (!ESCache.Instance.InStation) return;

            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.NotEnoughAmmo ||
                State.CurrentArmState == ArmState.NotEnoughDrones)
            {
                Log.WriteLine("Armstate [" + State.CurrentArmState + "]");
                Arm.ChangeArmState(ArmState.NotEnoughAmmo, true, null);
                return;
            }

            if (State.CurrentArmState == ArmState.Done)
            {
                Arm.ChangeArmState(ArmState.Idle, true, null);

                State.CurrentDroneControllerState = DroneControllerState.WaitingForTargets;
                ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.Error, true);
            }
        }

        private static bool EveryPulse()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return false;

            if (ESCache.Instance.InWormHoleSpace)
            {
                if (DebugConfig.DebugWSpaceScoutBehavior) Log.WriteLine("WSpaceScoutBehavior: EveryPulse: if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)");
                return true;
            }

            if (Settings.Instance.FinishWhenNotSafe)
                if (ESCache.Instance.InSpace &&
                    !ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    Log.WriteLine("Going back to base");
                    ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.GotoHomeBookmark, true);
                }

            Panic.ProcessState(string.Empty);

            if (State.CurrentPanicState == PanicState.Resume)
            {
                if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    State.CurrentPanicState = PanicState.Normal;
                    State.CurrentTravelerState = TravelerState.Idle;
                    ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.Error);
                    return true;
                }

                return false;
            }

            return true;
        }

        private static void GotoHomeBookmarkState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "CombatMissionsBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: Traveler.TravelHome()");

            Traveler.TravelToBookmarkName(HomeBookmark);

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Traveler.Destination = null;
                ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.Start, true);
            }
        }

        private static void GotoScanningSpotState()
        {
            if (DebugConfig.DebugWSpaceScoutBehavior) Log.WriteLine("GotoBase: Traveler.TravelToBookmarkName()");

            Traveler.TravelToBookmarkName(HomeBookmark);

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Traveler.Destination = null;
                ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.Start, true);
            }
        }

        private static void IdleState()
        {
            ResetStatesToDefaults();
            ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.Start);
        }

        private static void IsItSafeToUndock()
        {
            //
            // Check with the Launcher and verify that no other 'scout' thinks we should stay docked
            //
            // If any account that is on my 'team' has IsLocalSafe == false
            //

            ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.GotoScanningSpot);
        }

        private static bool ResetStatesToDefaults()
        {
            Log.WriteLine("AbyssalDeadspaceBehavior.ResetStatesToDefaults: start");
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return true;
        }

        private static void ScanSignaturesState()
        {
            DateTime waitUntil = DateTime.MinValue;

            _autoProbeAction = new ActionQueueAction(() =>
            {
                try
                {
                    if (_autoProbeAction == null)
                    {
                        Log.WriteLine("AutoProbeAction finished.");
                        return;
                    }

                    if (waitUntil > DateTime.UtcNow)
                    {
                        _autoProbeAction.QueueAction();
                        return;
                    }

                    if (ESCache.Instance.InStation)
                    {
                        Log.WriteLine("That doesn't work in stations.");
                        return;
                    }

                    /**
                    if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                    {
                        Log.WriteLine("Waiting, in warp.");
                        _autoProbeAction.QueueAction();
                        return;
                    }
                    **/

                    if (ESCache.Instance.probeScannerWindow == null)
                    {
                        Log.WriteLine("Opening probe scanner window.");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdToggleSolarSystemMap);
                        _autoProbeAction.QueueAction();
                        waitUntil = DateTime.UtcNow.AddSeconds(2);
                        return;
                    }

                    if (ESCache.Instance.probeScannerWindow.pyProbeScannerPalette == null)
                    {
                        _autoProbeAction.QueueAction();
                        return;
                    }

                    if (Scanner.GetProbes().Count == 0)
                    {
                        Log.WriteLine("No probes found in space, launching probes.");
                        ModuleCache probeLauncher = ESCache.Instance.Modules.Find(m => m.GroupId == (int)Group.ProbeLauncher);
                        if (probeLauncher == null)
                        {
                            Log.WriteLine("No probe launcher found.");
                            return;
                        }

                        if (probeLauncher.InLimboState || probeLauncher.IsActive)
                        {
                            Log.WriteLine("Probe launcher is active or reloading.");
                            _autoProbeAction.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(2);
                            return;
                        }

                        if (ESCache.Instance.CurrentShipsCargo.Items.DistinctBy(i => i.TypeId).Any(i => !i.IsSingleton
                                                                                                        && ESCache.Instance.CurrentShipsCargo.Items.Count(n => n.TypeId == i.TypeId) >= 2))
                        {
                            Log.WriteLine("Stacking current ship hangar.");
                            ESCache.Instance.CurrentShipsCargo.StackShipsCargo();
                            _autoProbeAction.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(2);
                            return;
                        }

                        if (!Defense.DeactivateCloak()) return;

                        if (probeLauncher.ChargeQty == 0 || probeLauncher.ChargeQty < 8)
                        {
                            IOrderedEnumerable<DirectItem> probes = ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.GroupId == (int)Group.Probes).OrderBy(i => i.Stacksize);
                            IOrderedEnumerable<DirectItem> coreProbes = probes.Where(i => i.TypeName.Contains("Core")).OrderBy(i => i.Stacksize);
                            //IOrderedEnumerable<DirectItem> combatProbes = probes.Where(i => i.TypeName.Contains("Combat")).OrderBy(i => i.Stacksize);
                            DirectItem charge = coreProbes.FirstOrDefault();
                            if (charge == null)
                            {
                                Log.WriteLine("No core probes found in cargohold.");
                                return;
                            }

                            if (charge.Stacksize < 8)
                            {
                                Log.WriteLine("Probe stacksize was smaller than 8.");
                                return;
                            }

                            probeLauncher.ChangeAmmo(charge, 0, 0);
                            _autoProbeAction.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(11);
                            return;
                        }

                        Log.WriteLine("Launching probes.");
                        probeLauncher.Click();

                        _autoProbeAction.QueueAction();
                        waitUntil = DateTime.UtcNow.AddSeconds(2);
                        return;
                    }

                    if (Scanner.GetProbes().Count != 8)
                    {
                        //TODO: check probe range, can't be retrieved if below CONST.MIN_PROBE_RECOVER_DISTANCE
                        Log.WriteLine("Probe amount is != 8, recovering probes.");
                        _autoProbeAction.QueueAction();
                        bool OurProbesAreTooCloseToRecover = false;
                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.GroupId == (int)Group.Probes))
                            foreach (DirectScannerProbe deployedProbe in Scanner.GetProbes())
                            {
                                if (ESCache.Instance.EntitiesOnGrid.All(i => i.Id != deployedProbe.ProbeId))
                                    continue;

                                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Distance < (double)Distances.MIN_PROBE_RECOVER_DISTANCE))
                                    OurProbesAreTooCloseToRecover = true;
                            }

                        if (!OurProbesAreTooCloseToRecover)
                            Scanner.RecoverProbes();

                        waitUntil = DateTime.UtcNow.AddSeconds(2);
                        return;
                    }

                    if (Scanner.IsProbeScanning())
                    {
                        Log.WriteLine("Probe scan active, waiting.");
                        _autoProbeAction.QueueAction();
                        waitUntil = DateTime.UtcNow.AddSeconds(1);
                        return;
                    }

                    /**
                    List<DirectSystemScanResult> list = mapViewWindow.SystemScanResults.ToList();
                    Log.WriteLine($"Loading {list.Count} results to datagridview.");
                    var resList = list.Select(d => new { d.Id, d.Deviation, d.SignalStrength, d.PreviousSignalStrength, d.ScanGroup, d.IsPointResult, d.IsSphereResult, d.MultiPointResult, PosVector3 = d.Pos.ToString(), DataVector3 = d.Data.ToString() }).ToList();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = resList;
                            _onlyLoadPScanResults = false;
                        }));
                    });
                    **/

                    foreach (DirectSystemScanResult item in Scanner.FilteredSystemScanResults.Where(r => r.ScanGroup == ScanGroup.Signature && r.SignalStrength < 1))
                    {
                        //...
                    }

                    DirectSystemScanResult result = Scanner.FilteredSystemScanResults.FirstOrDefault(r => r.ScanGroup == ScanGroup.Signature && r.SignalStrength < 1);
                    if (result != null)
                        if (result.Id != _currentSig)
                        {
                            Log.WriteLine("First sig or sig has changed, setting probe range to max.");
                            Scanner.SetMaxProbeRange();
                            Scanner.MoveProbesTo(result.Pos);
                            Scanner.ProbeScan();
                            _currentSig = result.Id;
                            _autoProbeAction.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(2);
                            return;
                        }
                        else
                        {
                            Log.WriteLine("Decreasing probe range and initiating scan again.");
                            Scanner.DecreaseProbeRange();
                            _currentSig = result.Id;
                            Scanner.MoveProbesTo(result.Pos);
                            Scanner.ProbeScan();
                            waitUntil = DateTime.UtcNow.AddSeconds(2);
                            _autoProbeAction.QueueAction();
                            return;
                        }
                    Log.WriteLine("No results found or finished auto probing.");
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            });

            _autoProbeAction.Initialize().QueueAction();
            _currentSig = null;
        }

        private static void StartState()
        {
            if (ESCache.Instance.InWormHoleSpace)
            {
                //
                // If we start in space verify we have probes, if we do not go home
                //
                if (ESCache.Instance.InSpace)
                {
                    if (ESCache.Instance.Modules.Any(i => i.GroupId == (int)Group.ProbeLauncher))
                    {
                        ModuleCache probeLauncher = ESCache.Instance.Modules.Find(i => i.GroupId == (int)Group.ProbeLauncher);
                        if (probeLauncher.Charge == null || probeLauncher.ChargeQty < 8)
                        {
                            ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.GotoHomeBookmark);
                            return;
                        }

                        if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Count > 0 && ESCache.Instance.CachedBookmarks.All(bookmark => bookmark.IsInCurrentSystem && bookmark.Title != HomeBookmark))
                        {
                            ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.ScanSignatures);
                            return;
                        }

                        return;
                    }

                    Log.WriteLine("WSpaceScoutBehavior: Idle: We have no probe launcher! Going home.");
                    ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.GotoHomeBookmark);
                }

                if (ESCache.Instance.InStation)
                {
                    Log.WriteLine("WSpaceScoutBehavior: Idle: We are in a Station or Citadel. Make sure we are in the right one.");
                    ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.GotoHomeBookmark);
                }
            }

            Log.WriteLine("WSpaceScoutBehavior: Idle: We started in K-Space: do nothing. (cloak?!)");
        }

        private static void SwitchState()
        {
            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                Arm.SwitchShipsOnly = true;
                Arm.ChangeArmState(ArmState.ActivateScanningShip, true, null);
            }

            if (DebugConfig.DebugArm) Log.WriteLine("WSpaceScoutBehavior.Switch is Entering Arm.Processstate");
            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Done");
                Arm.SwitchShipsOnly = false;
                Arm.ChangeArmState(ArmState.Idle, true, null);
                ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.UnloadLoot);
            }
        }

        private static void UnloadLootScoutState()
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return;

                if (ESCache.Instance.DirectEve.Session.Structureid.HasValue)
                {
                    Log.WriteLine("Currently Unloadloot does not work in Citadels, manually move your loot.");
                    ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.Arm);
                    return;
                }

                if (State.CurrentUnloadLootState == UnloadLootState.Idle)
                {
                    Log.WriteLine("UnloadLoot: Begin");
                    State.CurrentUnloadLootState = UnloadLootState.Begin;
                }

                UnloadLoot.ProcessState();

                if (State.CurrentUnloadLootState == UnloadLootState.Done)
                {
                    State.CurrentUnloadLootState = UnloadLootState.Idle;

                    if (State.CurrentCombatState == CombatState.OutOfAmmo)
                        Log.WriteLine("State.CurrentCombatState == CombatState.OutOfAmmo");

                    ChangeWSpaceScoutBehaviorState(WSpaceScoutBehaviorState.Arm, true);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Methods
    }
}