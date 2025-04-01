extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EVESharpCore.Questor.Behaviors
{
    public class DeepFlowSignaturesBehavior
    {
        #region Constructors

        public DeepFlowSignaturesBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        private static bool StayInLocal = false;

        #endregion Fields

        #region Properties

        public static string HomeBookmarkName { get; set; } = "HomeBookmark";

        #endregion Properties

        #region Methods

        public static bool ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentDeepFlowSignaturesBehaviorState != _StateToSet)
                {
                    if (_StateToSet == DeepFlowSignaturesBehaviorState.GotoHomeBookmark)
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute("AbyssalPocketNumber", 0);
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                    }

                    if (_StateToSet == DeepFlowSignaturesBehaviorState.ExecuteMission)
                    {
                        State.CurrentCombatMissionCtrlState = ActionControlState.Start;
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.AtDestination;
                    }

                    Log.WriteLine("New DeepFlowSignaturesBehaviorState [" + _StateToSet + "]");
                    State.CurrentDeepFlowSignaturesBehaviorState = _StateToSet;
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
            Log.WriteLine("LoadSettings: DeepFlowSignaturesBehavior");

            HomeBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ??
                (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            Log.WriteLine("DeepFlowSignaturesBehavior: HomeBookmarkName [" + HomeBookmarkName + "]");
            StayInLocal = (bool?)CharacterSettingsXml.Element("stayInLocal") ??
                          (bool?)CommonSettingsXml.Element("stayInLocal") ?? false;
            Log.WriteLine("DeepFlowSignaturesBehavior: stayInLocal [" + StayInLocal + "]");

            UseWhitelistOnly = (bool?)CharacterSettingsXml.Element("useSiteWhitelistOnly") ??
                          (bool?)CommonSettingsXml.Element("useSiteWhitelistOnly") ?? true;

            //
            // https://wiki.eveuniversity.org/Combat_sites#anomalies
            //

            WhitelistedSiteNames.Clear();
            if (UseWhitelistOnly)
            {
                XElement xmlElementSiteWhiteListSection = CharacterSettingsXml.Element("siteNameWhitelist") ??
                                                  CommonSettingsXml.Element("siteNameWhitelist");
                if (xmlElementSiteWhiteListSection != null)
                {
                    Log.WriteLine("Loading Site Whitelist");
                    int i = 1;
                    foreach (XElement xmlWhitelistedMission in xmlElementSiteWhiteListSection.Elements("siteName"))
                        if (WhitelistedSiteNames.All(m => m != xmlWhitelistedMission.Value))
                        {
                            Log.WriteLine("   Any Site containing [" + Log.FilterPath(xmlWhitelistedMission.Value) + "] in the name will be allowed");
                            WhitelistedSiteNames.Add(Log.FilterPath(xmlWhitelistedMission.Value));
                            i++;
                        }

                    Log.WriteLine("Site Whitelist now has [" + WhitelistedSiteNames.Count + "] entries");
                    if (WhitelistedSiteNames.Count == 0) UseWhitelistOnly = false;
                }
            }

            BlacklistedSiteNames.Clear();
            XElement xmlElementSiteBlackListSection = CharacterSettingsXml.Element("siteNameBlacklist") ??
                                                  CommonSettingsXml.Element("siteNameBlacklist");
            if (xmlElementSiteBlackListSection != null)
            {
                Log.WriteLine("Loading Site Blacklist");
                int i = 1;
                foreach (XElement xmlBlacklistedMission in xmlElementSiteBlackListSection.Elements("siteName"))
                    if (WhitelistedSiteNames.All(m => m != xmlBlacklistedMission.Value))
                    {
                        Log.WriteLine("   Any Site containing [" + Log.FilterPath(xmlBlacklistedMission.Value) + "] in the name will be skipped");
                        WhitelistedSiteNames.Add(Log.FilterPath(xmlBlacklistedMission.Value));
                        i++;
                    }

                Log.WriteLine("Site Blacklist now has [" + WhitelistedSiteNames.Count + "] entries");
            }

            ListOfBlacklistedSolarSystemNames.Clear();
            ListOfBlacklistedSolarSystemNames.Add("Jita");
            ListOfBlacklistedSolarSystemNames.Add("Amarr");
            ListOfBlacklistedSolarSystemNames.Add("Rens");
            ListOfBlacklistedSolarSystemNames.Add("Perimeter");

            HealthCheckMinimumShieldPercentage =
                (int?)CharacterSettingsXml.Element("HealthCheckMinimumShieldPercentage") ??
                (int?)CommonSettingsXml.Element("HealthCheckMinimumShieldPercentage") ?? 30;
            Log.WriteLine("HighSecCombatSignaturesBehavior: HealthCheckMinimumShieldPercentage [" + HealthCheckMinimumShieldPercentage + "]");
            HealthCheckMinimumArmorPercentage =
                (int?)CharacterSettingsXml.Element("HealthCheckMinimumArmorPercentage") ??
                (int?)CommonSettingsXml.Element("HealthCheckMinimumArmorPercentage") ?? 30;
            Log.WriteLine("HighSecCombatSignaturesBehavior: HealthCheckMinimumArmorPercentage [" + HealthCheckMinimumArmorPercentage + "]");
            HealthCheckMinimumCapacitorPercentage =
                (int?)CharacterSettingsXml.Element("HealthCheckMinimumCapacitorPercentage") ??
                (int?)CommonSettingsXml.Element("HealthCheckMinimumCapacitorPercentage") ?? 30;
            Log.WriteLine("HighSecCombatSignaturesBehavior: HealthCheckMinimumCapacitorPercentage [" + HealthCheckMinimumCapacitorPercentage + "]");
        }

        private static bool DetectPlayersAndMoveOnIfNeeded()
        {
            if (ESCache.Instance.InSpace)
            {
                if (ESCache.Instance.InWarp)
                {
                    Panic.LastMissionInvadedByTimeStamp = DateTime.UtcNow.AddDays(-1);
                    return false;
                }

                if (ESCache.Instance.EntitiesNotSelf.Any(i => i.IsPlayer) && Combat.Combat.PotentialCombatTargets.Count > 0)
                {
                    if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.HaveLootRights))
                    {
                        int WrecksWeDoNotOwnCount = ESCache.Instance.Wrecks.Count(i => !i.HaveLootRights);
                        const int WreckWeDoNotOwnLimit = 2;

                        if (DateTime.UtcNow > Panic.LastMissionInvadedByTimeStamp.AddMinutes(4))
                        {
                            Log.WriteLine("There are wrecks on grid that we do not own: [" + WrecksWeDoNotOwnCount + "]");
                            Panic.LastMissionInvadedByTimeStamp = DateTime.UtcNow;
                        }

                        if (WrecksWeDoNotOwnCount >= WreckWeDoNotOwnLimit)
                        {
                            Log.WriteLine("[" + WrecksWeDoNotOwnCount + "] WrecksWeDoNotOwnCount has reached the limit of [" + WreckWeDoNotOwnLimit + "] We should move on");
                            Panic.SetStartPanickingState();
                            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.PickSite);
                            return true;
                        }

                        if (DateTime.UtcNow > Panic.LastMissionInvadedByTimeStamp.AddSeconds(ESCache.Instance.RandomNumber(45, 70)))
                        {
                            Log.WriteLine("Player [" + ESCache.Instance.EntitiesNotSelf.FirstOrDefault(i => i.IsPlayer) + "] has been on grid for 45+ seconds");
                        }

                        return false;
                    }

                    return false;
                }
                else
                {
                    Panic.LastMissionInvadedByTimeStamp = DateTime.UtcNow.AddDays(-1);
                }

                return false;
            }

            return false;
        }

        // example:
        // ZZZ-123, Unknown
        // YYY-234, Relic
        // XXX-345, Data
        // WWW-456, Gas
        // VVV-567, Ore
        // UUU-678, Combat
        // TTT-789, Wormhole
        // SSS-890, Other
        //
        public static List<DirectSystemScanResult> ScanResults = new List<DirectSystemScanResult>();

        private bool? _inDeepFlowSite = null;

        public bool InDeepFlowSite
        {
            get
            {
                if (_inDeepFlowSite != null)
                    return _inDeepFlowSite ?? false;

                if (ESCache.Instance.Stargates.Any(i => i.IsOnGridWithMe))
                {
                    _inDeepFlowSite = false;
                    return _inDeepFlowSite ?? false;
                }

                if (ESCache.Instance.Entities.Any(i => i.IsStation && i.IsOnGridWithMe))
                {
                    _inDeepFlowSite = false;
                    return _inDeepFlowSite ?? false;
                }

                if (ESCache.Instance.Entities.Any(i => i.IsCitadel && i.IsOnGridWithMe))
                {
                    _inDeepFlowSite = false;
                    return _inDeepFlowSite ?? false;
                }

                if (State.CurrentDeepFlowSignaturesBehaviorState == DeepFlowSignaturesBehaviorState.DoneWithCurrentSite)
                {
                    _inDeepFlowSite = false;
                    return _inDeepFlowSite ?? false;
                }

                if (ESCache.Instance.Entities.Any(i => i.TypeId == (int)TypeID.Anomaly))
                {
                    _inDeepFlowSite = true;
                    return _inDeepFlowSite ?? true;
                }

                _inDeepFlowSite = false;
                return _inDeepFlowSite ?? false;
            }
        }


        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugHighSecCombatSignaturesBehavior) Log.WriteLine("State.CurrentDeepFlowSignaturesBehaviorState is [" + State.CurrentDeepFlowSignaturesBehaviorState + "]");

                switch (State.CurrentDeepFlowSignaturesBehaviorState)
                {
                    case DeepFlowSignaturesBehaviorState.Idle:
                        MissionSettings.MissionSafeDistanceFromStructure = 22000;
                        IdleCMBState();
                        break;

                    case DeepFlowSignaturesBehaviorState.Start:
                        StartCMBState();
                        break;

                    case DeepFlowSignaturesBehaviorState.Switch:
                        SwitchCMBState();
                        break;

                    case DeepFlowSignaturesBehaviorState.Arm:
                        ArmCMBState();
                        break;

                    case DeepFlowSignaturesBehaviorState.LocalWatch:
                        LocalWatchCMBState();
                        break;

                    case DeepFlowSignaturesBehaviorState.WaitingforBadGuytoGoAway:
                        WaitingFoBadGuyToGoAway();
                        break;

                    case DeepFlowSignaturesBehaviorState.WarpOutStation:
                        WarpOutBookmarkCMBState();
                        break;

                    case DeepFlowSignaturesBehaviorState.TravelToTargetSystem:
                        TravelToNextSystem();
                        break;

                    case DeepFlowSignaturesBehaviorState.UseScanner:
                        UseScannerState();
                        break;

                    case DeepFlowSignaturesBehaviorState.DoneWithCurrentSite:
                        DoneWithCurrentAnomalyState();
                        break;

                    case DeepFlowSignaturesBehaviorState.PickSite:
                        PickAnomalyState();
                        break;

                    case DeepFlowSignaturesBehaviorState.BookmarkAnomaly:
                        BookmarkAnomalyState();
                        break;

                    case DeepFlowSignaturesBehaviorState.ExecuteMission:
                        Salvage.LootWhileSpeedTanking = true;
                        if (DetectPlayersAndMoveOnIfNeeded()) return;
                        ExecuteSiteState();
                        break;

                    case DeepFlowSignaturesBehaviorState.GotoHomeBookmark:
                        GotoHomeBookmarkState();
                        break;

                    case DeepFlowSignaturesBehaviorState.UnloadLoot:
                        UnloadLootCMBState();
                        break;

                    case DeepFlowSignaturesBehaviorState.Default:
                        State.CurrentDeepFlowSignaturesBehaviorState = DeepFlowSignaturesBehaviorState.Idle;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static int HealthCheckMinimumShieldPercentage = 30;
        private static int HealthCheckMinimumArmorPercentage = 30;
        private static int HealthCheckMinimumCapacitorPercentage = 30;

        private static bool TravelToTargetSystem()
        {
            var theAgencyWindow = ESCache.Instance.Windows.OfType<DirectTheAgencyWindow>().FirstOrDefault();
            if (theAgencyWindow == null)
            {
                if (DirectEve.Interval(7000)) theAgencyWindow.Close();
            }

            Log.WriteLine("TravelToTargetSystem");
            return false;
        }

        private static void ArmCMBState()
        {
            if (!AttemptToBuyAmmo()) return;

            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin Arm");
                Arm.ChangeArmState(ArmState.Begin, true, null);
            }

            if (!ESCache.Instance.InStation) return;

            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.NotEnoughAmmo ||
                State.CurrentArmState == ArmState.NotEnoughDrones)
            {
                if (Settings.Instance.BuyAmmo)
                {
                    Log.WriteLine("Armstate [" + State.CurrentArmState + "]: BuyAmmo [" + Settings.Instance.BuyAmmo + "]");
                    ESCache.Instance.EveAccount.LastAmmoBuy.AddDays(-1);
                    Arm.ChangeArmState(ArmState.Done, true, null);
                    return;
                }

                Log.WriteLine("Armstate [" + State.CurrentArmState + "]: BuyAmmo [" + Settings.Instance.BuyAmmo + "]");
                Arm.ChangeArmState(ArmState.NotEnoughAmmo, true, null);
                return;
            }

            if (State.CurrentArmState == ArmState.Done)
            {
                Arm.ChangeArmState(ArmState.Idle, true, null);

                if (Settings.Instance.BuyAmmo && BuyItemsController.CurrentBuyItemsState != BuyItemsState.DisabledForThisSession)
                {
                    BuyItemsController.CurrentBuyItemsState = BuyItemsState.Idle;
                    ControllerManager.Instance.RemoveController(typeof(BuyItemsController));
                }

                State.CurrentDroneControllerState = DroneControllerState.WaitingForTargets;
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.LocalWatch, true);
            }
        }

        private static bool AttemptToBuyAmmo()
        {
            if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace || ESCache.Instance.InWormHoleSpace)
                return true;

            if (Settings.Instance.BuyAmmo)
                if (BuyItemsController.CurrentBuyItemsState != BuyItemsState.Done && BuyItemsController.CurrentBuyItemsState != BuyItemsState.DisabledForThisSession)
                    if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastAmmoBuy.AddHours(8) && DateTime.UtcNow > ESCache.Instance.EveAccount.LastBuyLpItemAttempt.AddHours(1))
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastAmmoBuyAttempt), DateTime.UtcNow);
                        ControllerManager.Instance.AddController(new BuyItemsController());
                        return false;
                    }

            return true;
        }

        private static void ProcessAlerts()
        {
            //TimeSpan ItHasBeenThisLongSinceWeStartedThePocket = Statistics.StartedPocket - DateTime.UtcNow;
            //int minutesInPocket = ItHasBeenThisLongSinceWeStartedThePocket.Minutes;
            //if (minutesInPocket > (AbyssalPocketWarningSeconds / 60) && !WeHaveBeenInPocketTooLong_WarningSent)
            //{
            //    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ABYSSAL_POCKET_TIME, "HighSecAnomalyBehavior: FYI: We have been in pocket number [" + ActionControl.PocketNumber + "] for [" + minutesInPocket + "] min: You might need to check that we arnt stuck."));
            //    WeHaveBeenInPocketTooLong_WarningSent = true;
            //    Log.WriteLine("We have been in AbyssalPocket [" + ActionControl.PocketNumber + "] for more than [" + AbyssalPocketWarningSeconds + "] seconds!");
            //    return;
            //}

            return;
        }

        public static void ClearPerSystemCache()
        {
            CachedSolarSystemtoGotoNextId = null;
            AnomaliesInvadedByPlayers = new Dictionary<string, int>();
            return;
        }

        private static Dictionary<string, int> AnomaliesInvadedByPlayers = new Dictionary<string, int>();

        public static bool OtherPlayersHereAreCapableOfRunningThisSite(DirectSystemScanResult AnomalyScanResult)
        {
            if (ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer))
            {
                int frigates = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsFrigate && e.IsPlayer);
                int cruisers = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsCruiser && e.IsPlayer);
                int battlecruisers = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsBattlecruiser && e.IsPlayer);
                int battleships = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsBattleship && e.IsPlayer);
                EntityCache missionInvadedBy = ESCache.Instance.EntitiesNotSelf.FirstOrDefault(e => e.IsPlayer);
                int battleshipsNeeded = 0;
                int logisticsNeeded = 0;
                int playerCountMininum = 0;
                bool ignorefrigates = false;
                bool ignorecruisers = false;

                //Triglavian Invasion
                if (AnomalyScanResult.TypeName.Contains("Conduit"))
                {
                    battleshipsNeeded = 1;
                    logisticsNeeded = 0;
                    playerCountMininum = 1;

                    if (AnomalyScanResult.TypeName.Contains("Emerging Conduit"))
                    {
                        ignorefrigates = true;
                        ignorecruisers = true;
                        battleshipsNeeded = 1;
                        logisticsNeeded = 0;
                        playerCountMininum = 1;
                    }
                    else if (AnomalyScanResult.TypeName.Contains("Minor Conduit"))
                    {
                        ignorefrigates = true;
                        ignorecruisers = true;
                        battleshipsNeeded = 1;
                        logisticsNeeded = 0;
                        playerCountMininum = 1;
                    }
                    else if (AnomalyScanResult.TypeName.Contains("Major Conduit"))
                    {
                        ignorefrigates = true;
                        ignorecruisers = true;
                        battleshipsNeeded = 3;
                        logisticsNeeded = 2;
                        playerCountMininum = 6;
                    }
                    else if (AnomalyScanResult.TypeName.Contains("Observatory"))
                    {
                        ignorefrigates = true;
                        ignorecruisers = true;
                        battleshipsNeeded = 3;
                        logisticsNeeded = 2;
                        playerCountMininum = 8;
                    }
                }

                return false;
            }

            return false;
        }

        public static bool ShouldWeMoveOnToAnotherSite(DirectSystemScanResult AnomalyScanResult)
        {
            return false;
        }

        private static bool EveryPulse()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugHighSecCombatSignaturesBehavior) Log.WriteLine("CMBEveryPulse: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)");
                return false;
            }

            if (ESCache.Instance.InSite)
            {
                if (DebugConfig.DebugHighSecCombatSignaturesBehavior) Log.WriteLine("CMBEveryPulse: if (ESCache.Instance.InAbyssalDeadspace)");

                if (State.CurrentDeepFlowSignaturesBehaviorState != DeepFlowSignaturesBehaviorState.ExecuteMission &&
                    State.CurrentDeepFlowSignaturesBehaviorState != DeepFlowSignaturesBehaviorState.DoneWithCurrentSite)
                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.ExecuteMission);

                ProcessAlerts();
                //next process panic as needed
            }

            Panic.ProcessState(HomeBookmarkName);

            if (State.CurrentPanicState == PanicState.StartPanicking || State.CurrentPanicState == PanicState.Panicking || State.CurrentPanicState == PanicState.Panic)
            {
                return false;
            }

            if (State.CurrentPanicState == PanicState.Resume)
            {
                if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    Panic.ChangePanicState(PanicState.Normal);
                    State.CurrentTravelerState = TravelerState.Idle;
                    return true;
                }

                if (DebugConfig.DebugHighSecCombatSignaturesBehavior) Log.WriteLine("CMBEveryPulse: if (State.CurrentPanicState == PanicState.Resume)");
                return false;
            }

            if (ESCache.Instance.InStation)
            {
                if (State.CurrentDeepFlowSignaturesBehaviorState == DeepFlowSignaturesBehaviorState.ExecuteMission ||
                    State.CurrentDeepFlowSignaturesBehaviorState == DeepFlowSignaturesBehaviorState.DoneWithCurrentSite)
                {
                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.UseScanner, true);
                    return true;
                }
            }

            if (ESCache.Instance.InSpace)
            {
                if (State.CurrentDeepFlowSignaturesBehaviorState != DeepFlowSignaturesBehaviorState.PickSite && State.CurrentDeepFlowSignaturesBehaviorState != DeepFlowSignaturesBehaviorState.TravelToTargetSystem)
                {
                    if (!ESCache.Instance.InWarp && !ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                    {
                        /**
                        if (ESCache.Instance.ClosestCitadel != null && ESCache.Instance.ClosestCitadel.IsOnGridWithMe)
                        {
                            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.PickSite, true);
                            return true;
                        }

                        if (ESCache.Instance.ClosestStation != null && ESCache.Instance.ClosestStation.IsOnGridWithMe)
                        {
                            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.PickSite, true);
                            return true;
                        }
                        **/

                        if (Combat.Combat.PotentialCombatTargets.Count == 0 && ESCache.Instance.AccelerationGates.Count > 0)
                        {
                            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.ExecuteMission, true);
                            return true;
                        }
                    }
                }
            }

            return true;
        }

        private static bool _previousInAnomaly = false;

        private static void ReloadIfNeeded()
        {
            bool inAnomaly = ESCache.Instance.InAnomaly;
            if (_previousInAnomaly != inAnomaly && ESCache.Instance.EntitiesOnGrid.Any(e => e.BracketType == BracketType.NPC_Frigate
                                                                                            || e.BracketType == BracketType.NPC_Cruiser
                                                                                            || e.BracketType == BracketType.NPC_Battleship
                                                                                            || e.BracketType == BracketType.NPC_Destroyer))
            {
                if (!_previousInAnomaly && inAnomaly && DirectEve.Interval(10000, 10000, inAnomaly.ToString()))
                {
                    Log.WriteLine($"NPCs found on grid and inAnomaly has been changed. Reloading.");
                    Combat.Combat.ReloadAll();
                }

                _previousInAnomaly = inAnomaly;
            }
        }

        private static void ExecuteSiteState()
        {
            if (!ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugDeepFlowSignaturesBehavior) Log.WriteLine("ExecuteSiteState: if (!ESCache.Instance.InSpace)");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                if (DebugConfig.DebugDeepFlowSignaturesBehavior) Log.WriteLine("ExecuteSiteState: if (ESCache.Instance.CurrentShipsCargo == null)");
                return;
            }

            if (DebugConfig.DebugDeepFlowSignaturesBehavior) Log.WriteLine(" ExecuteMission: _actionControl.ProcessState();");

            ActionControl.ProcessState(null, null);

            if (NavigateOnGrid.ChooseNavigateOnGridTargetIds != null)
                NavigateOnGrid.NavigateIntoRange(NavigateOnGrid.ChooseNavigateOnGridTargetIds, "ClearPocket", true);

            ReloadIfNeeded();

            if (State.CurrentCombatMissionCtrlState == ActionControlState.Done)
            {
                Log.WriteLine("Done: if (State.CurrentCombatMissionCtrlState == ActionControlState.Done)");
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.DoneWithCurrentSite, true, null);
                ESCache.Instance.LootedContainers.Clear();
                return;
            }

            if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
            {
                Log.WriteLine("Error");
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "Questor Error."));
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.DoneWithCurrentSite, true, null);
                ESCache.Instance.LootedContainers.Clear();
            }
        }

        private static void GotoHomeBookmarkState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "GotoHomeBookmarkState", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: Traveler.TravelToBookmarkName(" + HomeBookmarkName + ");");

            Traveler.TravelToBookmarkName(HomeBookmarkName);

            if (State.CurrentTravelerState == TravelerState.AtDestination || ESCache.Instance.CachedBookmarks.Count == 0)
            {
                Traveler.Destination = null;
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.Start, true);
            }
        }

        private static void IdleCMBState()
        {
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentDroneControllerState = DroneControllerState.Idle;
            State.CurrentSalvageState = SalvageState.Idle;
            State.CurrentStorylineState = StorylineState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;

            if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }
            else
            {
                Salvage.OpenWrecks = true;
            }

            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                return;
            }

            if (ESCache.Instance.InSpace)
            {
                if (ESCache.Instance.ActiveShip == null)
                    return;

                if (ESCache.Instance.ActiveShip.GivenName.ToLower() == Settings.Instance.SalvageShipName.ToLower())
                {
                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.UseScanner);
                    return;
                }

                Log.WriteLine("We started in space, but our Ship's Name [" + ESCache.Instance.ActiveShip.GivenName + "] does not match the SalvageShipname [" + Settings.Instance.SalvageShipName + "]");
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.GotoHomeBookmark);
                return;
            }

            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.UseScanner);
        }

        public static void InvalidateCache()
        {
            // Method intentionally left empty.
        }

        public static void ClearPerPocketCache()
        {
            myDirectSystemScanResult = null;
            return;
        }

        private static void LocalWatchCMBState()
        {
            if (Settings.Instance.UseLocalWatch)
            {
                Time.Instance.LastLocalWatchAction = DateTime.UtcNow;

                if (DebugConfig.DebugArm) Log.WriteLine("Starting: Is LocalSafe check...");
                if (ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    Log.WriteLine("local is clear");
                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.WarpOutStation);
                    return;
                }

                Log.WriteLine("Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.WaitingforBadGuytoGoAway);
                return;
            }

            if (ESCache.Instance.DirectEve.Me.PVPTimerExist)
            {
                Log.WriteLine("LocalWatchCheck: We have pvp timer: waiting");
                return;
            }

            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.WarpOutStation);
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("ResetStatesToDefaults");
            State.CurrentHighSecCombatSignaturesBehaviorState = HighSecCombatSignaturesBehaviorState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            return;
        }

        private static void StartCMBState()
        {

            //
            // It takes 20 minutes (potentially) to do an anomaly: if it is within 25min of Downtime (10:35 evetime) pause
            //
            if (Time.Instance.IsItDuringDowntimeNow)
            {
                Log.WriteLine("Arm: Downtime is less than 25 minutes from now: Pausing");
                ControllerManager.Instance.SetPause(true);
                return;
            }

            if (ESCache.Instance.InSpace)
            {
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.UseScanner);
                return;
            }

            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.Switch);
        }

        private static void SwitchCMBState()
        {
            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                Arm.SwitchShipsOnly = true;
                Arm.ChangeArmState(ArmState.ActivateCombatShip, true, null);
            }

            if (DebugConfig.DebugArm) Log.WriteLine("CombatMissionBehavior.Switch is Entering Arm.Processstate");
            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Done");
                Arm.SwitchShipsOnly = false;
                Arm.ChangeArmState(ArmState.Idle, true, null);
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.UnloadLoot);
            }
        }

        private static void UnloadLootCMBState()
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return;

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

                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.Arm, true);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void WaitingFoBadGuyToGoAway()
        {
            if (DateTime.UtcNow.Subtract(Time.Instance.LastLocalWatchAction).TotalMinutes <
                Time.Instance.WaitforBadGuytoGoAway_minutes + ESCache.Instance.RandomNumber(1, 3))
                return;

            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.LocalWatch);
        }

        private static void WarpOutBookmarkCMBState()
        {
            if (ESCache.Instance.EveAccount.OtherToonsAreStillLoggingIn)
            {
                Log.WriteLine("WarpOutBookmarkCMBState: Waiting for other toons to finish logging in before we undock!");
                return;
            }

            if (!string.IsNullOrEmpty(Settings.Instance.UndockBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> warpOutBookmarks = ESCache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "");
                if (warpOutBookmarks != null && warpOutBookmarks.Any())
                {
                    DirectBookmark warpOutBookmark = warpOutBookmarks.FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.Distance < 10000000 && b.Distance > (int)Distances.WarptoDistance);

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Log.WriteLine("No Bookmark");
                        State.CurrentTravelerState = TravelerState.Idle;
                        ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.UseScanner);
                    }
                    else if (warpOutBookmark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + warpOutBookmark.Title);
                            Traveler.Destination = new BookmarkDestination(warpOutBookmark);
                        }

                        Traveler.ProcessState();
                        if (State.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            State.CurrentTravelerState = TravelerState.Idle;
                            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.UseScanner);
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmark in System");
                        State.CurrentTravelerState = TravelerState.Idle;
                        ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.UseScanner);
                    }

                    return;
                }
            }

            Log.WriteLine("No Bookmark in System");
            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.PickSite);
        }

        private static List<string> DeepFlowRiftSiteBlacklist = new List<string>();

        private static bool InDeepFlowRiftSite
        {
            get
            {
                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.DeepFlowRift))
                    return true;

                return false;
            }
        }

        private static void UseScannerState()
        {
            if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
            {
                TravelerDestination.Undock();
                return;
            }

            if (!ESCache.Instance.InSpace)
                return;

            if (Time.Instance.LastDockAction.AddSeconds(15) > DateTime.UtcNow)
                return;

            if (!InDeepFlowRiftSite)
            {
                if (!Scanner.AutoProbe()) return;

                if (Scanner.SignatureScanResults.Any())
                {
                    foreach (var site in Scanner.SignatureScanResults)
                    {
                        if (DebugConfig.DebugDeepFlowSignaturesBehavior) Log.WriteLine("SignatureScanResult: ID [" + site.Id + "] SignalStrength [" + site.SignalStrength + "] TypeName [" + site.TypeName + "] GroupName [" + site.GroupName + "] IsDeepFlowSite [" + site.IsDeepFlowSite + "]");
                    }
                }

                if (Scanner.SignatureScanResults.Any(i => i.IsDeepFlowSite))
                {
                    if (DebugConfig.DebugDeepFlowSignaturesBehavior) Log.WriteLine("UseScannerState: if (Scanner.SignatureScanResults.Any(i => i.IsDeepFlowSite))");
                    if (ESCache.Instance.InWarp)
                        return;

                    foreach (var DeepFlowRiftSiteScanResult in Scanner.SignatureScanResults.Where(x => x.IsDeepFlowSite && DeepFlowRiftSiteBlacklist.Contains(x.Id)))
                    {
                        //missing IsOreSite, IsIceSite
                        if (DirectEve.Interval(30000)) Log.WriteLine("SignatureScanResult: ID [" + DeepFlowRiftSiteScanResult.Id + "] SignalStrength [" + DeepFlowRiftSiteScanResult.SignalStrength + "]");
                        DeepFlowRiftSiteScanResult.WarpTo();
                        return;
                    }

                    return;
                }
                else if (DebugConfig.DebugDeepFlowSignaturesBehavior) Log.WriteLine("UseScannerState: No DeepFlowSites found! if (!Scanner.SignatureScanResults.Any(i => i.IsDeepFlowSite))");

                //
                //if (!ListOfSolarSystemsWeHaveChecked.Contains(ESCache.Instance.DirectEve.Session.SolarSystem.Id))
                //    ListOfSolarSystemsWeHaveChecked.Add(ESCache.Instance.DirectEve.Session.SolarSystem.Id);
                //
                //if (!ListOfSolarSystemsWeShouldAvoid.Contains(ESCache.Instance.DirectEve.Session.SolarSystem.Id))
                //    ListOfSolarSystemsWeShouldAvoid.Add(ESCache.Instance.DirectEve.Session.SolarSystem.Id);

                SetDestinationToNextSystem();
                if (CachedSolarSystemtoGotoNextId != null)
                {
                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.TravelToTargetSystem);
                    return;
                }

                return;
            }

            if (State.CurrentDeepFlowSignaturesBehaviorState != DeepFlowSignaturesBehaviorState.DoneWithCurrentSite)
            {
                Log.WriteLine("UseScannerState: if (ESCache.Instance.InAnomaly)");
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.ExecuteMission);
            }

            return;
        }

        private static void DoneWithCurrentAnomalyState()
        {
            try
            {
                if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
                {
                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.PickSite, true);
                    return;
                }

                if (!ESCache.Instance.InSpace) return;

                if (ESCache.Instance.InWarp) return;

                if (ESCache.Instance.ClosestCitadel != null && ESCache.Instance.ClosestCitadel.IsOnGridWithMe)
                {
                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.PickSite, true);
                    return;
                }

                if (ESCache.Instance.ClosestStation != null && ESCache.Instance.ClosestStation.IsOnGridWithMe)
                {
                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.PickSite, true);
                    return;
                }

                if (Panic.HomeBookmarkIsWhereWeWantToGo(HomeBookmarkName)) return;
                if (Panic.FreeportCitadelIsWhereWeWantToGo()) return;
                if (Panic.NPCStationIsWhereWeWantToGo()) return;

                if (ESCache.Instance.ClosestPlanet != null)
                {
                    if (ESCache.Instance.ClosestPlanet.Distance > (double)Distances.HalfAu)
                    {
                        if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                            return;

                        ESCache.Instance.ClosestPlanet.WarpTo();
                        return;
                    }

                    if (ESCache.Instance.InWarp)
                        return;

                    if (Combat.Combat.PotentialCombatTargets.Count > 0)
                    {
                        if (ESCache.Instance.Planets.Any(i => i.Distance > (double)Distances.HalfAu))
                        {
                            EntityCache PlanetToGoto = ESCache.Instance.Planets.Where(i => i.Distance > (double)Distances.HalfAu).OrderBy(i => i.Distance).FirstOrDefault();
                            if (PlanetToGoto.Distance > (double)Distances.HalfAu)
                            {
                                if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                                    return;

                                PlanetToGoto.WarpTo();
                            }

                            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.PickSite);
                            return;
                        }

                        //
                        // ?!? We are at a planet with NPCs and there are no other planets !?! I dont think this can actually happen
                        //
                    }

                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.PickSite);
                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return;
            }
        }

        private static DirectBookmark EscalationBookmarkInLocal
        {
            get
            {
                if (ESCache.Instance.CachedBookmarks.Count > 0)
                {
                    if (ESCache.Instance.CachedBookmarks.Any(i => i.SolarSystem.Id == ESCache.Instance.DirectEve.Session.SolarSystem.Id))
                    {
                        IEnumerable<DirectBookmark> BookmarksInLocal = ESCache.Instance.CachedBookmarks.Where(i => i.SolarSystem.Id == ESCache.Instance.DirectEve.Session.SolarSystem.Id);
                        foreach (DirectBookmark bookmarkInLocal in BookmarksInLocal)
                        {
                            if (bookmarkInLocal.Title.Contains("DED") || bookmarkInLocal.Title.Contains("complex") || bookmarkInLocal.Title.Contains("escelation") || bookmarkInLocal.Title.Contains("site") || bookmarkInLocal.Title.Contains("Warp Gate"))
                            {
                                return bookmarkInLocal;
                            }
                        }

                        return null;
                    }

                    return null;
                }

                return null;
            }
        }

        private static void PickAnomalyState()
        {
            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.UseScanner);
            return;

            if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
            {
                TravelerDestination.Undock();
                return;
            }

            if (!ESCache.Instance.InSpace)
                return;

            if (ESCache.Instance.InWarp)
            {
                if (myDirectSystemScanResult != null)
                {
                    ListOfAnomalyIDsWeHaveChecked.AddOrUpdate(myDirectSystemScanResult.Id, DateTime.UtcNow);
                    Log.WriteLine("WarpToThisAnomaly: We have made it into warp to [" + myDirectSystemScanResult.Id + "][" + myDirectSystemScanResult.TypeName + "][" + myDirectSystemScanResult.GroupName + "]");
                }

                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.ExecuteMission);
                return;
            }

            if (Time.Instance.LastDockAction.AddSeconds(15) > DateTime.UtcNow)
                return;

            if (EscalationBookmarkInLocal != null)
            {
                Traveler.TravelToBookmark(EscalationBookmarkInLocal);
                return;
            }

            if (!ESCache.Instance.InAnomaly)
            {
                if (Scanner.ProbeScanResultsReady)
                {
                    if (!ESCache.Instance.InWarp && !ESCache.Instance.MyShipEntity.HasInitiatedWarp) Log.WriteLine("ProbeScanResultsReady: Anomalies [" + Scanner.AnomalyScanResultCount + "] Signatures [" + Scanner.SignatureScanResultCount + "]");
                    if (Scanner.AnomalyScanResults.Count > 0)
                    {
                        //
                        // we want to filter the results based on the name of the anom and move on if we find none of that type
                        //
                        int intAnomNum = 0;
                        foreach (DirectSystemScanResult AnomalyScanResult in Scanner.AnomalyScanResults.OrderByDescending(i => myDirectSystemScanResult != null && i.Id == myDirectSystemScanResult.Id))
                        {
                            if (ListOfAnomalyIDsWeHaveChecked.ContainsKey(AnomalyScanResult.Id))
                                continue;

                            if (BlacklistedSiteNames.Any(i => i.ToLower() == AnomalyScanResult.TypeName.ToLower()))
                            {
                                Log.WriteLine("Anomaly ID [" + AnomalyScanResult.Id + "] TypeName [" + AnomalyScanResult.TypeName + "] GroupName [" + AnomalyScanResult.GroupName + "] ScanGroup [" + AnomalyScanResult.ScanGroup + "] is blacklisted: skipping");
                                continue;
                            }

                            if (UseWhitelistOnly)
                            {
                                if (WhitelistedSiteNames.Any(i => i.ToLower() == AnomalyScanResult.TypeName.ToLower()))
                                {
                                    intAnomNum++;
                                    if (!ESCache.Instance.InWarp && !ESCache.Instance.MyShipEntity.HasInitiatedWarp) Log.WriteLine("Anomaly ID [" + AnomalyScanResult.Id + "] TypeName [" + AnomalyScanResult.TypeName + "] GroupName [" + AnomalyScanResult.GroupName + "] ScanGroup [" + AnomalyScanResult.ScanGroup + "] is whitelisted: processing");
                                    WarpToThisAnomaly(AnomalyScanResult);
                                    return;
                                }

                                continue;
                            }

                            intAnomNum++;
                            Log.WriteLine("Anomaly ID [" + AnomalyScanResult.Id + "] TypeName [" + AnomalyScanResult.TypeName + "] GroupName [" + AnomalyScanResult.GroupName + "] ScanGroup [" + AnomalyScanResult.ScanGroup + "] is available: processing");
                            WarpToThisAnomaly(AnomalyScanResult);
                            return;
                        }

                        Log.WriteLine("No Anomalies left in [" + ESCache.Instance.DirectEve.Session.SolarSystem.Name + "] to process");
                    }

                    if (!StayInLocal)
                    {
                        if (ListOfSolarSystemsWeHaveChecked.Count > 0 && ListOfSolarSystemsWeHaveChecked.Count > 12)
                            ListOfSolarSystemsWeHaveChecked.Clear();

                        if (!ListOfSolarSystemsWeHaveChecked.Contains(ESCache.Instance.DirectEve.Session.SolarSystem.Id))
                            ListOfSolarSystemsWeHaveChecked.Add(ESCache.Instance.DirectEve.Session.SolarSystem.Id);

                        if (ListOfSolarSystemsWeShouldAvoid.Count > 0 && ListOfSolarSystemsWeShouldAvoid.Count > 12)
                            ListOfSolarSystemsWeShouldAvoid.Clear();

                        if (!ListOfSolarSystemsWeShouldAvoid.Contains(ESCache.Instance.DirectEve.Session.SolarSystem.Id))
                            ListOfSolarSystemsWeShouldAvoid.Add(ESCache.Instance.DirectEve.Session.SolarSystem.Id);

                        SetDestinationToNextSystem();
                        ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.TravelToTargetSystem);
                        return;
                    }
                    else
                    {
                        if (Panic.HomeBookmarkIsWhereWeWantToGo(HomeBookmarkName)) return;
                        if (Panic.FreeportCitadelIsWhereWeWantToGo()) return;
                        if (Panic.NPCStationIsWhereWeWantToGo()) return;
                    }

                    return;
                }

                Log.WriteLine("if (!Scanner.ProbeScanResultsReady)");
                return;
            }

            if (State.CurrentDeepFlowSignaturesBehaviorState != DeepFlowSignaturesBehaviorState.DoneWithCurrentSite)
            {
                Log.WriteLine("UseScannerState: if (ESCache.Instance.InAnomaly)");
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.ExecuteMission);
            }

            return;
        }

        public static DirectSystemScanResult myDirectSystemScanResult = null;

        private static void WarpToThisAnomaly(DirectSystemScanResult AnomalyScanResult)
        {
            if (AnomalyScanResult.ScanGroup == ScanGroup.Anomaly)
            {
                if (!ESCache.Instance.InWarp && !ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                {
                    if (ESCache.Instance.InAnomaly)
                    {
                        Log.WriteLine("WarpToThisAnomaly: We are in an old Anom: moving...");
                        ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.DoneWithCurrentSite);
                        return;
                    }

                    if (20000 > ESCache.Instance.ClosestStargate.Distance)
                    {
                        Log.WriteLine("WarpToThisAnomaly from a Stargate: HasInitiatedWarp [" + ESCache.Instance.MyShipEntity.HasInitiatedWarp + "] InWarp [" + ESCache.Instance.InWarp + "]");
                        myDirectSystemScanResult = AnomalyScanResult;
                        myDirectSystemScanResult.WarpTo();
                        return;
                    }

                    if ((double)Distances.HalfAu > ESCache.Instance.ClosestPlanet.Distance)
                    {
                        Log.WriteLine("WarpToThisAnomaly from a Planet: HasInitiatedWarp [" + ESCache.Instance.MyShipEntity.HasInitiatedWarp + "] InWarp [" + ESCache.Instance.InWarp + "]");
                        myDirectSystemScanResult = AnomalyScanResult;
                        myDirectSystemScanResult.WarpTo();
                        return;
                    }

                    if (ESCache.Instance.ClosestCitadel != null && (double)Distances.HalfAu > ESCache.Instance.ClosestCitadel.Distance)
                    {
                        Log.WriteLine("WarpToThisAnomaly from a Citadel: HasInitiatedWarp [" + ESCache.Instance.MyShipEntity.HasInitiatedWarp + "] InWarp [" + ESCache.Instance.InWarp + "]");
                        myDirectSystemScanResult = AnomalyScanResult;
                        myDirectSystemScanResult.WarpTo();
                        return;
                    }

                    if (ESCache.Instance.ClosestStation != null && (double)Distances.HalfAu > ESCache.Instance.ClosestStation.Distance)
                    {
                        Log.WriteLine("WarpToThisAnomaly from a Station: HasInitiatedWarp [" + ESCache.Instance.MyShipEntity.HasInitiatedWarp + "] InWarp [" + ESCache.Instance.InWarp + "]");
                        myDirectSystemScanResult = AnomalyScanResult;
                        myDirectSystemScanResult.WarpTo();
                        return;
                    }

                    Log.WriteLine("We are not at a gate or a planet: we should move before proceeding to the next anom");
                    ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.DoneWithCurrentSite);
                    return;
                }

                if (ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                    return;

                Log.WriteLine(".[" + myDirectSystemScanResult.Id + "][" + myDirectSystemScanResult.TypeName + "][" + myDirectSystemScanResult.GroupName + "][" + myDirectSystemScanResult.ScanGroup + "].");
                return;
            }

            Log.WriteLine("![" + myDirectSystemScanResult.Id + "][" + myDirectSystemScanResult.TypeName + "][" + myDirectSystemScanResult.GroupName + "][" + myDirectSystemScanResult.ScanGroup + "]!");
            return;
        }

        public static void ClearInfoDuringPanicSoThatWeWillReturnToThisAnom()
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.DeepFlowSignaturesController) && ESCache.Instance.InAnomaly)
            {
                ListOfAnomalyIDsWeHaveChecked.Remove(myDirectSystemScanResult.Id);
            }
        }

        private static List<long> ListOfSolarSystemsWeHaveChecked = new List<long>();
        private static List<long> ListOfSolarSystemsWeShouldAvoid = new List<long>();
        private static List<string> ListOfBlacklistedSolarSystemNames = new List<string>();
        private static List<string> BlacklistedSiteNames = new List<string>();
        private static List<string> WhitelistedSiteNames = new List<string>();
        private static Dictionary<string, DateTime> ListOfAnomalyIDsWeHaveChecked = new Dictionary<string, DateTime>();
        private static bool UseWhitelistOnly = true;

        //private static DirectSolarSystem _solarSystemToGotoNext;
        private static DirectSolarSystem SolarSystemToGotoNext
        {
            get
            {
                //
                // need options to stay in the same constellation and or the same region: maybe a list of constellations?
                //
                if (ESCache.Instance.Stargates.Any(i => i.LeadsToSolarSystem.IsHighSecuritySpace && !ListOfSolarSystemsWeShouldAvoid.Contains(i.LeadsToSolarSystem.Id)))
                {
                    foreach (EntityCache stargate in ESCache.Instance.Stargates.OrderBy(x => ListOfSolarSystemsWeHaveChecked.Contains(x.LeadsToSolarSystem.Id)).Where(i => i.LeadsToSolarSystem.IsHighSecuritySpace && !ListOfSolarSystemsWeShouldAvoid.Contains(i.LeadsToSolarSystem.Id) && !ListOfBlacklistedSolarSystemNames.Contains(i.LeadsToSolarSystem.Name) && SystemMeetsTravelLimitations(i.LeadsToSolarSystem)))
                    {
                        return stargate.LeadsToSolarSystem;
                    }
                }

                ListOfSolarSystemsWeShouldAvoid = new List<long>();
                if (ESCache.Instance.Stargates.Any(i => i.LeadsToSolarSystem.IsHighSecuritySpace && !ListOfSolarSystemsWeShouldAvoid.Contains(i.LeadsToSolarSystem.Id)))
                {
                    foreach (EntityCache stargate in ESCache.Instance.Stargates.OrderBy(x => ListOfSolarSystemsWeHaveChecked.Contains(x.LeadsToSolarSystem.Id)).Where(i => i.LeadsToSolarSystem.IsHighSecuritySpace && !ListOfSolarSystemsWeShouldAvoid.Contains(i.LeadsToSolarSystem.Id) && !ListOfBlacklistedSolarSystemNames.Contains(i.LeadsToSolarSystem.Name) && SystemMeetsTravelLimitations(i.LeadsToSolarSystem)))
                    {
                        return stargate.LeadsToSolarSystem;
                    }
                }

                ListOfSolarSystemsWeShouldAvoid = new List<long>();
                if (ESCache.Instance.Stargates.Any(i => i.LeadsToSolarSystem.IsHighSecuritySpace && !ListOfSolarSystemsWeShouldAvoid.Contains(i.LeadsToSolarSystem.Id)))
                {
                    foreach (EntityCache stargate in ESCache.Instance.Stargates.OrderBy(x => ListOfSolarSystemsWeHaveChecked.Contains(x.LeadsToSolarSystem.Id)).Where(i => i.LeadsToSolarSystem.IsHighSecuritySpace && !ListOfSolarSystemsWeShouldAvoid.Contains(i.LeadsToSolarSystem.Id) && !ListOfBlacklistedSolarSystemNames.Contains(i.LeadsToSolarSystem.Name)))
                    {
                        return stargate.LeadsToSolarSystem;
                    }
                }

                return ESCache.Instance.ClosestStargate.LeadsToSolarSystem;
            }
        }

        private static void SetDestinationToNextSystem()
        {
            if (SolarSystemToGotoNext != null)
            {
                CachedSolarSystemtoGotoNextId = SolarSystemToGotoNext.Id;
                Log.WriteLine("SolarSystemToGotoNext [" + SolarSystemToGotoNext.Name + "] Id[" + SolarSystemToGotoNext.Id + "]");
                return;
            }

            Log.WriteLine("SolarSystemToGotoNext is null");
            return;
        }

        private static bool limitTravelToSameRegion = false;
        private static bool limitTravelToSameConstellation = false;

        private static bool SystemMeetsTravelLimitations(DirectSolarSystem destinationSolarSystem)
        {
            if (limitTravelToSameRegion)
            {
                if (ESCache.Instance.DirectEve.Session.Region.Id != destinationSolarSystem.Region.Id)
                {
                    return false;
                }
            }

            if (limitTravelToSameConstellation)
            {
                if (ESCache.Instance.DirectEve.Session.Constellation.Id != destinationSolarSystem.Constellation.Id)
                {
                    return false;
                }
            }

            return false;
        }

        private static long? CachedSolarSystemtoGotoNextId { get; set; } = null;

        private static void TravelToNextSystem()
        {

            /**
            //close solar system map view window
            if (ESCache.Instance.SolarSystemMapPanelWindow != null)
            {
                if (DirectEve.Interval(7000)) ESCache.Instance.SolarSystemMapPanelWindow.Close();
            }

            //close probe scanner window
            if (ESCache.Instance.probeScannerWindow != null)
            {
                if (DirectEve.Interval(7000)) ESCache.Instance.probeScannerWindow.Close();
            }

            //close directional scanner window
            if (ESCache.Instance.directionalScannerWindow != null)
            {
                if (DirectEve.Interval(7000)) ESCache.Instance.directionalScannerWindow.Close();
            }
            **/

            if (CachedSolarSystemtoGotoNextId != null)
            {
                if (State.CurrentTravelerState != TravelerState.AtDestination)
                {
                    Traveler.TravelToSystemId((long)CachedSolarSystemtoGotoNextId);
                    return;
                }

                CachedSolarSystemtoGotoNextId = null;
                ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.Idle);
                return;
            }

            ChangeDeepFlowSignaturesBehaviorState(DeepFlowSignaturesBehaviorState.Idle);
            return;
        }

        private static void BookmarkAnomalyState()
        {
            return;
        }

        #endregion Methods
    }
}