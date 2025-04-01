extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SC::SharedComponents.EVE;
using EVESharpCore.Framework.Lookup;
using System.Windows.Documents;
using SC::SharedComponents.IPC;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;

namespace EVESharpCore.Questor.BackgroundTasks
{
    public static class Salvage
    {
        #region Constructors

        static Salvage()
        { }

        #endregion Constructors

        #region Fields

        private static readonly List<string> MissionsThatDoNotRequireLootingToComplete = new List<string>
        {
            "Angel Extravaganza",
            "Guristas Extravaganza",
            "Attack of the Drones",
            "Dread Pirate Scarlet",
            "Pirate Invasion",
            "Rogue Drone Harassment",
            "The Blockade",
            "The Assault"
            //"The Assault",
        };

        private static Dictionary<long, DateTime> OpenedContainers = new Dictionary<long, DateTime>();

        private static bool SalvageAll
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                    return true;

                //if (ESCache.Instance.DirectEve.Me.IsInvasionActive)
                //    return true;

                //behavior dedicated to looting / salvaging
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.SalvageGridController))
                    return true;

                //ships with many salvagers (noctis / destroyers, etc)
                if (Salvagers.Count >= 3)
                {
                    OpenWrecks = true;
                    return true;
                }

                if (ESCache.Instance.ActiveShip.GivenName.ToLower() == Settings.Instance.SalvageShipName.ToLower())
                    return true;

                return false;
            }
        }

        private static List<EntityCache> WrecksWeNoLongerNeedToTractor = new List<EntityCache>();
        private static List<EntityCache> _allOngridWrecksAndContainers;
        private static DateTime _lastUnTargetAllWrecks;
        private static int? _maximumWreckTargets;
        private static double? _salvagerRange;
        private static List<ModuleCache> _salvagers { get; set; }
        private static List<EntityCache> _targetedWrecksToSalvage;
        private static List<EntityCache> _targetedWrecksToTractor;
        private static double? _tractorBeamRange;
        private static List<ModuleCache> _tractorBeams { get; set; }
        private static List<EntityCache> SalvageTheseInstead;

        #endregion Fields

        #region Properties

        private static bool? _lootEverything { get; set; }
        private static int? _reserveCargoCapacity { get; set; }
        public static bool AfterMissionSalvaging { get; set; }
        private static int AgeofBookmarksForSalvageBehavior { get; set; }
        public static int AgeofSalvageBookmarksToExpire { get; set; }
        public static bool CreateSalvageBookmarks { get; set; }
        private static string CreateSalvageBookmarksIn { get; set; }
        private static string AllowSalvagerToStealOnlyWithThisShipName { get; set; }
        public static bool AllowSalvagerToSteal
        {
            get
            {
                if (ESCache.Instance.ActiveShip != null)
                {
                    if (!string.IsNullOrEmpty(ESCache.Instance.ActiveShip.GivenName) && !string.IsNullOrEmpty(AllowSalvagerToStealOnlyWithThisShipName))
                    {
                        if (ESCache.Instance.ActiveShip.GivenName == AllowSalvagerToStealOnlyWithThisShipName)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }
        public static bool DoNotDoANYSalvagingOutsideMissionActions { get; set; }
        public static bool FirstSalvageBookmarksInSystem { get; set; }
        public static int? GlobalMaximumWreckTargets { get; set; }
        public static int SecondsWaitForLootAction { get; set; }

        public static bool LootEverything
        {
            get
            {
                try
                {
                    if (MissionSettings.MyMission != null && MissionSettings.MyMission.Faction != null && !MissionSettings.MyMission.Faction.PirateFaction)
                        return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                if (MissionSettings.MissionLootEverything != null)
                    return (bool)MissionSettings.MissionLootEverything;

                if (_lootEverything != null)
                    return (bool)_lootEverything;

                return false;
            }
            set => _lootEverything = value;
        }

        public static bool LootItemRequiresTarget { get; set; }

        public static bool LootOnlyWhatYouCanWithoutSlowingDownMissionCompletion { get; set; }

        public static bool LootWhileSpeedTanking { get; set; }

        public static bool GlobalUseMobileTractor { get; set; }

        public static bool UseMobileTractor
        {
            get
            {
                if (GlobalUseMobileTractor)
                {
                    /**
                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    {
                        switch (MissionSettings.RegularMission.Name)
                        {
                            case "Vengeance":
                            case "Rogue Drone Harassment":
                            case "Dread Pirate Scarlet":
                            case "Angels Extravaganza":
                            case "Attack of the Drones":
                            case "Silence The Informant":
                            case "The Blockade":
                            case "Worlds Collide":
                            case "The Score":
                                return true;
                                break;
                        }
                    }
                    **/

                    return true;
                }

                return false;
            }
        }

        public static int MaximumWreckTargets
        {
            get
            {
                try
                {
                    if (_maximumWreckTargets != null)
                        return (int)_maximumWreckTargets;

                    if (ESCache.Instance.InAbyssalDeadspace)
                        return 1;

                    if (ESCache.Instance.MaxLockedTargets == 0)
                        return 0;

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.SalvageGridController))
                        return ESCache.Instance.MaxLockedTargets ?? 2;

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) && Salvagers.Count >= 3)
                        return ESCache.Instance.MaxLockedTargets ?? 2;

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController) && Salvagers.Count >= 3)
                        return ESCache.Instance.MaxLockedTargets ?? 2;

                    if (GlobalMaximumWreckTargets != null)
                    {
                        if (string.Equals(ESCache.Instance.ActiveShip.GivenName, Combat.Combat.CombatShipName, StringComparison.CurrentCultureIgnoreCase)) //||
                                                                                                                       //MissionSettings.MissionSpecificMissionFitting != null && !string.IsNullOrEmpty(MissionSettings.MissionSpecificMissionFitting.Ship) && ESCache.Instance.ActiveShip.GivenName.ToLower() == MissionSettings.MissionSpecificMissionFitting.Ship.ToLower())
                        {
                            //
                            // if target by anything use the character level setting for MaxWreck Targets
                            // If the character level setting is misconfigured to use more than 3 targets for wrecks, limit it to 3
                            //

                            if (GlobalMaximumWreckTargets > 3)
                            {
                                _maximumWreckTargets = 3;
                                return (int)_maximumWreckTargets;
                            }

                            _maximumWreckTargets = (int)GlobalMaximumWreckTargets;
                            return (int)_maximumWreckTargets;

                            //
                            // if not in combat (nothing targeting us) add 2 additional wreck targets to the max with the cieling set to MaxLockedTargets (skills and ship limits)
                            //
                        }

                        //
                        // if we arent in the combat ship assume we can use all targeting slots (minus 1!)
                        //
                        int totalSalvagersAndTractors = 0;
                        if (Salvagers.Count > 0)
                            totalSalvagersAndTractors += Salvagers.Count;

                        if (TractorBeams.Count > 0)
                            totalSalvagersAndTractors += TractorBeams.Count;

                        _maximumWreckTargets = Math.Min(ESCache.Instance.MaxLockedTargets ?? 2 - 1, totalSalvagersAndTractors);
                        return (int)_maximumWreckTargets;
                    }

                    //
                    // if we somehow havent set the character level setting for max wrecks targets then set the # of wrecks targets based on the # of total targeting slots available
                    //
                    if (ESCache.Instance.MaxLockedTargets < 3)
                    {
                        _maximumWreckTargets = 1;
                        return (int)_maximumWreckTargets;
                    }

                    if (ESCache.Instance.MaxLockedTargets >= 3)
                    {
                        _maximumWreckTargets = 2;
                        return (int)_maximumWreckTargets;
                    }

                    _maximumWreckTargets = 1;
                    return (int)_maximumWreckTargets;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 1;
                }
            }
        }

        public static int MinimumWreckCount { get; set; }

        public static bool OpenWrecks { get; set; } = true;

        private static int ReserveCargoCapacity
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                    return 1;

                if (ESCache.Instance.InMission)
                    if (MissionSettings.MyMission != null && MissionSettings.MyMission.Name.ToLower().Contains("Damsel In Distress".ToLower()))
                        return 1;

                if (_reserveCargoCapacity != null)
                    return (int)_reserveCargoCapacity;

                return 1;
            }
            set => _reserveCargoCapacity = value;
        }

        private static bool SalvageMultipleMissionsinOnePass { get; set; }

        private static int SalvagerMinimumCapacitor { get; set; }

        public static List<ModuleCache> Salvagers
        {
            get
            {
                if (_salvagers == null)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (ESCache.Instance.Modules.Any(m => m.GroupId == (int)Group.Salvager))
                        {
                            _salvagers = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.Salvager).ToList();
                            return _salvagers;
                        }

                        return new List<ModuleCache>();
                    }

                    return new List<ModuleCache>();
                }

                return _salvagers ?? new List<ModuleCache>();
            }
        }

        private static int TractorBeamMinimumCapacitor { get; set; }

        public static double? TractorBeamRange
        {
            get
            {
                if (_tractorBeamRange == null)
                {
                    if (TractorBeams.Count > 0)
                    {
                        _tractorBeamRange = TractorBeams.Min(t => t.OptimalRange);
                        return _tractorBeamRange;
                    }

                    return null;
                }

                return _tractorBeamRange;
            }
        }

        public static List<ModuleCache> TractorBeams
        {
            get
            {
                if (_tractorBeams == null)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (ESCache.Instance.Modules.Any(m => m.IsOnline && m.GroupId == (int)Group.TractorBeam))
                        {
                            _tractorBeams = ESCache.Instance.Modules.Where(m => m.IsOnline && m.GroupId == (int)Group.TractorBeam).ToList();
                            return _tractorBeams;
                        }

                        return new List<ModuleCache>();
                    }

                    return new List<ModuleCache>();
                }

                return _tractorBeams ?? new List<ModuleCache>();
            }
        }

        private static bool UnloadLootAtStation { get; set; }

        private static bool UseGatesInSalvage { get; set; }

        private static List<int> WreckBlackList { get; set; } = new List<int>();

        private static bool WreckBlackListMediumWrecks { get; set; }

        private static bool WreckBlackListSmallWrecks { get; set; }

        private static List<EntityCache> AllOnGridWrecksAndContainers
        {
            get
            {
                if (_allOngridWrecksAndContainers == null)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (ESCache.Instance.EntitiesOnGrid.Count > 0)
                        {
                            _allOngridWrecksAndContainers = ESCache.Instance.EntitiesOnGrid.Where(t => t.IsWreck || t.IsContainer).ToList();
                            return _allOngridWrecksAndContainers;
                        }

                        return new List<EntityCache>();
                    }

                    return new List<EntityCache>();
                }

                return _allOngridWrecksAndContainers;
            }
        }

        private static int ModuleNumber { get; set; }

        public static double? SalvagerRange
        {
            get
            {
                if (_salvagerRange == null)
                {
                    if (Salvagers.Count > 0)
                    {
                        _salvagerRange = 5000;
                        return _salvagerRange;
                    }

                    return 0;
                }

                return _salvagerRange;
            }
        }

        private static List<EntityCache> TargetedWrecksToSalvage
        {
            get
            {
                if (_targetedWrecksToSalvage == null)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (SalvageTheseInstead != null)
                            return SalvageTheseInstead.Where(i => i.Distance < Salvagers.Min(s => s.OptimalRange)).ToList();

                        if (ESCache.Instance.Targets != null && ESCache.Instance.Targets.Count > 0)
                        {
                            //
                            // if we re-add the salvage blacklist then we need to also handle untergeting the wrecks that we do not wish to salvage!
                            //
                            _targetedWrecksToSalvage = ESCache.Instance.Targets.Where(t => t.GroupId == (int)Group.Wreck && t.Distance < SalvagerRange).OrderByDescending(x => x.HaveLootRights).ToList();

                            return _targetedWrecksToSalvage;
                        }

                        return new List<EntityCache>();
                    }

                    return new List<EntityCache>();
                }

                return _targetedWrecksToSalvage;
            }
        }

        private static List<EntityCache> TargetedWrecksToTractor
        {
            get
            {
                if (_targetedWrecksToTractor == null)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (ESCache.Instance.Targets.Count > 0)
                        {
                            _targetedWrecksToTractor = ESCache.Instance.Targets.Where(t => (t.GroupId == (int)Group.Wreck || t.GroupId == (int)Group.CargoContainer) && t.Distance < TractorBeamRange).ToList();
                            return _targetedWrecksToTractor;
                        }

                        return new List<EntityCache>();
                    }

                    return new List<EntityCache>();
                }

                return _targetedWrecksToTractor;
            }
        }

        #endregion Properties

        #region Methods

        public static bool ProcessSalvagers(IEnumerable<EntityCache> SalvageThese = null)
        {
            if (ESCache.Instance.Containers == null) return false;
            if (ESCache.Instance.Containers.Count == 0)
            {
                if (DebugConfig.DebugSalvage) Log.WriteLine("ProcessSalvagers: if (!ESCache.Instance.Containers.Any())");
                return true;
            }

            SalvageTheseInstead = null;

            if (SalvageThese != null && SalvageThese.Any())
                SalvageTheseInstead = SalvageThese.ToList();

            if (Salvagers.Count == 0)
            {
                if (DebugConfig.DebugSalvage) Log.WriteLine("Debug: We have no salvagers fitted.");
                return true;
            }

            if (ESCache.Instance.InMission && ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.CapacitorPercentage < SalvagerMinimumCapacitor)
            {
                if (DebugConfig.DebugSalvage)
                    Log.WriteLine("Capacitor [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] below [" +
                                  SalvagerMinimumCapacitor +
                                  "%] SalvagerMinimumCapacitor");
                return true;
            }

            if (TargetedWrecksToSalvage.Count == 0)
            {
                if (DebugConfig.DebugSalvage) Log.WriteLine("Debug: if (TargetedWrecksToSalvage.Count == 0)");
                return true;
            }

            if (Time.Instance.NextSalvageAction > DateTime.UtcNow)
            {
                if (DebugConfig.DebugSalvage)
                    Log.WriteLine("Debug: Cache.Instance.NextSalvageAction is still in the future, waiting");
                return true;
            }

            //
            // Activate
            //
            int salvagersProcessedThisTick = 0;
            int WreckNumber = 0;
            foreach (EntityCache wreckToSalvage in TargetedWrecksToSalvage.Where(x => x.IsTarget).OrderByDescending(i => i.IsLootTarget).ThenByDescending(x => x.HaveLootRights))
            {
                WreckNumber++;

                foreach (ModuleCache salvager in Salvagers)
                {
                    ModuleNumber++;
                    if (salvager.IsActive)
                    {
                        if (DebugConfig.DebugSalvage)
                            Log.WriteLine("[" + WreckNumber + "][::" + ModuleNumber + "] _ Salvager is: IsActive [" + salvager.IsActive + "]. Continue");
                        continue;
                    }

                    if (salvager.InLimboState)
                    {
                        if (DebugConfig.DebugSalvage)
                            Log.WriteLine("[" + WreckNumber + "][::" + ModuleNumber + "] __ Salvager is: InLimboState [" + salvager.InLimboState +
                                          "] IsDeactivating [" +
                                          salvager.IsDeactivating + "] IsActivatable [" + salvager.IsActivatable + "] IsOnline [" + salvager.IsOnline +
                                          "] TargetId [" + salvager.TargetId + "]. Continue");
                        continue;
                    }

                    //
                    // this tractor has already been activated at least once
                    //
                    if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(salvager.ItemId))
                        if (Time.Instance.LastActivatedTimeStamp[salvager.ItemId].AddSeconds(5) > DateTime.UtcNow)
                            continue;

                    //
                    // if we have more wrecks on the field then we have salvagers that have not yet been activated
                    //
                    if (TargetedWrecksToSalvage.Count >= Salvagers.Count(i => !i.IsActive))
                    {
                        if (DebugConfig.DebugSalvage)
                            Log.WriteLine("We have [" + TargetedWrecksToSalvage.Count + "] wrecks  and [" + Salvagers.Count(i => !i.IsActive) +
                                          "] available salvagers of [" +
                                          Salvagers.Count + "] total");
                        //
                        // skip activating any more salvagers on this wreck that already has at least 1 salvager on it.
                        //
                        if (Salvagers.Any(i => i.IsActive && i.LastTargetId == wreckToSalvage.Id))
                        {
                            if (DebugConfig.DebugSalvage)
                                Log.WriteLine("Not assigning another salvager to wreck [" + wreckToSalvage.Name + "][" + wreckToSalvage.MaskedId + "]at[" +
                                              Math.Round(wreckToSalvage.Distance / 1000, 0) + "k] as it already has at least 1 salvager active");
                            //
                            // Break out of the Foreach salvager in salvagers and continue to the next wreck
                            //
                            break;
                        }
                    }

                    Log.WriteLine("Activating salvager [" + ModuleNumber + "] on [" + wreckToSalvage.Name + "][ID: " + wreckToSalvage.MaskedId + "] we have [" +
                                  TargetedWrecksToSalvage.Count +
                                  "] wrecks targeted in salvager range");
                    if (salvager.Activate(wreckToSalvage))
                        try
                        {
                            salvagersProcessedThisTick++;
                            Time.Instance.NextSalvageAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.SalvageDelayBetweenActions_milliseconds);
                            Time.Instance.LastActivatedTimeStamp[wreckToSalvage.Id] = DateTime.UtcNow;

                            //
                            // return, no more processing this tick
                            //
                            return false;
                        }
                        catch
                        {
                            return false;
                        }

                    //
                    // move on to the next salvager
                    //
                }

                //
                // move on to the next wreck
                //
            }

            return true;
        }

        private static void BlacklistWrecks()
        {
            //
            // if enabled the following would keep you from looting or salvaging small wrecks
            //
            //list of small wreck
            if (WreckBlackListSmallWrecks)
            {
                WreckBlackList.Add(26557);
                WreckBlackList.Add(26561);
                WreckBlackList.Add(26564);
                WreckBlackList.Add(26567);
                WreckBlackList.Add(26570);
                WreckBlackList.Add(26573);
                WreckBlackList.Add(26576);
                WreckBlackList.Add(26579);
                WreckBlackList.Add(26582);
                WreckBlackList.Add(26585);
                WreckBlackList.Add(26588);
                WreckBlackList.Add(26591);
                WreckBlackList.Add(26594);
                WreckBlackList.Add(26935);
            }

            //
            // if enabled the following would keep you from looting or salvaging medium wrecks
            //
            //list of medium wreck
            if (WreckBlackListMediumWrecks)
            {
                WreckBlackList.Add(26558);
                WreckBlackList.Add(26562);
                WreckBlackList.Add(26568);
                WreckBlackList.Add(26574);
                WreckBlackList.Add(26580);
                WreckBlackList.Add(26586);
                WreckBlackList.Add(26592);
                WreckBlackList.Add(26934);
            }
        }

        public static bool ChangeSalvageState(SalvageState _SalvageStateToSet, bool wait = false)
        {
            try
            {
                if (_processStateIterations > 5)
                    wait = true;

                if (State.CurrentSalvageState != _SalvageStateToSet)
                {
                    if (DebugConfig.DebugSalvage) Log.WriteLine("New SalvageState [" + _SalvageStateToSet + "]");
                    State.CurrentSalvageState = _SalvageStateToSet;
                    if (!wait)
                    {
                        _processStateIterations++;
                        ProcessState();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        private static DirectItem MobileTractorInCargo
        {
            get
            {
                if (ESCache.Instance.CurrentShipsCargo == null)
                    return null;

                if (ESCache.Instance.CurrentShipsCargo.Items == null || ESCache.Instance.CurrentShipsCargo.Items.Count == 0)
                    return null;

                if (ESCache.Instance.CurrentShipsCargo.Items.All(i => i.GroupId != (int)Group.MobileTractor))
                    return null;

                if (ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.GroupId == (int)Group.MobileTractor))
                {
                    return ESCache.Instance.CurrentShipsCargo.Items.Find(i => i.GroupId == (int)Group.MobileTractor);
                }

                return null;
            }
        }

        public static void DeployMobileTractor()
        {
            try
            {
                if (ShouldWeDeployMobileTractorHere())
                {
                    Log.WriteLine("DeployMobileTractor: Attempting to deploy mobile tractor unit");
                    if (MobileTractorInCargo != null && !MobileTractorInCargo.LaunchForSelf()) return;
                }

                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return;
            }
        }

        private static bool ShouldWeDeployMobileTractorHere()
        {
            if (MobileTractorInCargo == null)
            {
                if (DebugConfig.DebugMobileTractor) Log.WriteLine("if (MobileTractorInCargo == null) return false");
                return false;
            }

            if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsMobileTractor))
            {
                if (DebugConfig.DebugMobileTractor) Log.WriteLine("if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsMobileTractor))");
                return false;
            }

            if (Combat.Combat.PotentialCombatTargets.Count == 0)
            {
                if (DebugConfig.DebugMobileTractor) Log.WriteLine("if (!Combat.Combat.PotentialCombatTargets.Any())");
                return false;
            }

            if (3 >= Combat.Combat.PotentialCombatTargets.Count)
            {
                if (DebugConfig.DebugMobileTractor) Log.WriteLine("if (3 >= Combat.Combat.PotentialCombatTargets.Count())");
                return false;
            }

            if (ESCache.Instance.AccelerationGates.All(i => i.Distance > 5000))
            {
                if (DebugConfig.DebugMobileTractor) Log.WriteLine("if (ESCache.Instance.AccelerationGates != null && ESCache.Instance.AccelerationGates.All(i => i.Distance > 5000))");
                return false;
            }

            return true;
        }

        public static bool PickupMobileTractor()
        {
            try
            {
                if (!ESCache.Instance.EntitiesOnGrid.All(i => i.IsMobileTractor))
                    return true;

                if (!UseMobileTractor)
                    return true;

                EntityCache mobileTractor = ESCache.Instance.EntitiesOnGrid.Find(i => i.IsMobileTractor);

                if (mobileTractor != null)
                {
                    if (ESCache.Instance.CurrentShipsCargo == null) return false;

                    if (100 > ESCache.Instance.CurrentShipsCargo.FreeCapacity)
                    {
                        Log.WriteLine("if (100 > ESCache.Instance.CurrentShipsCargo.FreeCapacity) We do not have room for the mobile tractor: Bookmarking it and leaving it behind!");
                        return true;
                    }

                    if (mobileTractor.Distance > (double)Distances.ScoopRange)
                    {
                        mobileTractor.Approach();
                        return false;
                    }

                    if (!mobileTractor.ScoopToCargoHold) return false;
                    Log.WriteLine("Scooped MobileTractor to Cargo Hold");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public static void InvalidateCache()
        {
            try
            {
                //
                // this list of variables is cleared every pulse.
                //
                _maximumWreckTargets = null;
                _allOngridWrecksAndContainers = null;
                _targetedWrecksToSalvage = null;
                _targetedWrecksToTractor = null;
                _salvagers = null;
                _salvagerRange = null;
                _tractorBeamRange = null;
                _tractorBeams = null;
                _processStateIterations = 0;
                if (ESCache.Instance.InAbyssalDeadspace)
                    OpenWrecks = true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        public static void ClearPerPocketCache()
        {
            WrecksWeNoLongerNeedToTractor = new List<EntityCache>();
            Time.Instance.LastActivatedTimeStamp = new Dictionary<long, DateTime>();
            tractorsProcessedThisTick = 0;
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: Salvage");
                SecondsWaitForLootAction =
                    (int?)CharacterSettingsXml.Element("secondsWaitForLootAction") ??
                    (int?)CommonSettingsXml.Element("secondsWaitForLootAction") ?? 60;
                Log.WriteLine("Salvage: secondsWaitForLootAction [" + SecondsWaitForLootAction + "]");
                LootEverything =
                    (bool?)CharacterSettingsXml.Element("lootEverything") ??
                    (bool?)CommonSettingsXml.Element("lootEverything") ?? true;
                Log.WriteLine("Salvage: lootEverything [" + LootEverything + "]");
                UseGatesInSalvage =
                    (bool?)CharacterSettingsXml.Element("useGatesInSalvage") ??
                    (bool?)CommonSettingsXml.Element("useGatesInSalvage") ?? false;
                Log.WriteLine("Salvage: useGatesInSalvage [" + UseGatesInSalvage + "]");
                // if our mission does not DeSpawn (likely someone in the mission looting our stuff?) use the gates when salvaging to get to our bookmarks
                CreateSalvageBookmarks =
                    (bool?)CharacterSettingsXml.Element("createSalvageBookmarks") ??
                    (bool?)CommonSettingsXml.Element("createSalvageBookmarks") ?? false;
                Log.WriteLine("Salvage: createSalvageBookmarks [" + CreateSalvageBookmarks + "]");
                CreateSalvageBookmarksIn =
                    (string)CharacterSettingsXml.Element("createSalvageBookmarksIn") ??
                    (string)CommonSettingsXml.Element("createSalvageBookmarksIn") ?? "Player";
                Log.WriteLine("Salvage: createSalvageBookmarksIn [" + CreateSalvageBookmarksIn + "]");
                MinimumWreckCount =
                    (int?)CharacterSettingsXml.Element("minimumWreckCount") ??
                    (int?)CommonSettingsXml.Element("minimumWreckCount") ?? 1;
                Log.WriteLine("Salvage: minimumWreckCount [" + MinimumWreckCount + "]");
                AfterMissionSalvaging =
                    (bool?)CharacterSettingsXml.Element("afterMissionSalvaging") ??
                    (bool?)CommonSettingsXml.Element("afterMissionSalvaging") ?? false;
                Log.WriteLine("Salvage: afterMissionSalvaging [" + AfterMissionSalvaging + "]");
                FirstSalvageBookmarksInSystem =
                    (bool?)CharacterSettingsXml.Element("FirstSalvageBookmarksInSystem") ??
                    (bool?)CommonSettingsXml.Element("FirstSalvageBookmarksInSystem") ?? false;
                Log.WriteLine("Salvage: FirstSalvageBookmarksInSystem [" + FirstSalvageBookmarksInSystem + "]");
                SalvageMultipleMissionsinOnePass =
                    (bool?)CharacterSettingsXml.Element("salvageMultpleMissionsinOnePass") ??
                    (bool?)CommonSettingsXml.Element("salvageMultpleMissionsinOnePass") ?? false;
                Log.WriteLine("Salvage: salvageMultpleMissionsinOnePass [" + SalvageMultipleMissionsinOnePass + "]");
                UnloadLootAtStation =
                    (bool?)CharacterSettingsXml.Element("unloadLootAtStation") ??
                    (bool?)CommonSettingsXml.Element("unloadLootAtStation") ?? false;
                Log.WriteLine("Salvage: unloadLootAtStation [" + UnloadLootAtStation + "]");
                ReserveCargoCapacity =
                    (int?)CharacterSettingsXml.Element("reserveCargoCapacity") ??
                    (int?)CommonSettingsXml.Element("reserveCargoCapacity") ?? 0;
                Log.WriteLine("Salvage: reserveCargoCapacity [" + ReserveCargoCapacity + "]");
                GlobalMaximumWreckTargets =
                    (int?)CharacterSettingsXml.Element("maximumWreckTargets") ??
                    (int?)CommonSettingsXml.Element("maximumWreckTargets") ?? 0;
                Log.WriteLine("Salvage: maximumWreckTargets [" + GlobalMaximumWreckTargets + "]");
                WreckBlackListSmallWrecks =
                    (bool?)CharacterSettingsXml.Element("WreckBlackListSmallWrecks") ??
                    (bool?)CommonSettingsXml.Element("WreckBlackListSmallWrecks") ?? false;
                Log.WriteLine("Salvage: WreckBlackListSmallWrecks [" + WreckBlackListSmallWrecks + "]");
                WreckBlackListMediumWrecks =
                    (bool?)CharacterSettingsXml.Element("WreckBlackListMediumWrecks") ??
                    (bool?)CommonSettingsXml.Element("WreckBlackListMediumWrecks") ?? false;
                Log.WriteLine("Salvage: WreckBlackListMediumWrecks [" + WreckBlackListMediumWrecks + "]");
                AgeofBookmarksForSalvageBehavior =
                    (int?)CharacterSettingsXml.Element("ageofBookmarksForSalvageBehavior") ??
                    (int?)CommonSettingsXml.Element("ageofBookmarksForSalvageBehavior") ?? 45;
                Log.WriteLine("Salvage: ageofBookmarksForSalvageBehavior [" + AgeofBookmarksForSalvageBehavior + "]");
                AgeofSalvageBookmarksToExpire = (int?)CharacterSettingsXml.Element("ageofSalvageBookmarksToExpire") ??
                                                (int?)CommonSettingsXml.Element("ageofSalvageBookmarksToExpire") ?? 120;
                Log.WriteLine("Salvage: ageofSalvageBookmarksToExpire [" + AgeofSalvageBookmarksToExpire + "]");
                LootOnlyWhatYouCanWithoutSlowingDownMissionCompletion =
                    (bool?)CharacterSettingsXml.Element("lootOnlyWhatYouCanWithoutSlowingDownMissionCompletion") ??
                    (bool?)CommonSettingsXml.Element("lootOnlyWhatYouCanWithoutSlowingDownMissionCompletion") ?? false;
                Log.WriteLine("Salvage: lootOnlyWhatYouCanWithoutSlowingDownMissionCompletion [" + LootOnlyWhatYouCanWithoutSlowingDownMissionCompletion + "]");
                TractorBeamMinimumCapacitor =
                    (int?)CharacterSettingsXml.Element("tractorBeamMinimumCapacitor") ??
                    (int?)CommonSettingsXml.Element("tractorBeamMinimumCapacitor") ?? 3;
                Log.WriteLine("Salvage: tractorBeamMinimumCapacitor [" + TractorBeamMinimumCapacitor + "]");
                SalvagerMinimumCapacitor =
                    (int?)CharacterSettingsXml.Element("salvagerMinimumCapacitor") ??
                    (int?)CommonSettingsXml.Element("salvagerMinimumCapacitor") ?? 0;
                Log.WriteLine("Salvage: salvagerMinimumCapacitor [" + SalvagerMinimumCapacitor + "]");
                DoNotDoANYSalvagingOutsideMissionActions =
                    (bool?)CharacterSettingsXml.Element("doNotDoANYSalvagingOutsideMissionActions") ??
                    (bool?)CommonSettingsXml.Element("doNotDoANYSalvagingOutsideMissionActions") ?? false;
                Log.WriteLine("Salvage: doNotDoANYSalvagingOutsideMissionActions [" + DoNotDoANYSalvagingOutsideMissionActions + "]");
                LootItemRequiresTarget =
                    (bool?)CharacterSettingsXml.Element("lootItemRequiresTarget") ??
                    (bool?)CommonSettingsXml.Element("lootItemRequiresTarget") ?? false;
                Log.WriteLine("Salvage: lootItemRequiresTarget [" + LootItemRequiresTarget + "]");
                LootWhileSpeedTanking = (bool?)CharacterSettingsXml.Element("lootWhileSpeedTanking") ??
                                        (bool?)CommonSettingsXml.Element("lootWhileSpeedTanking") ?? false;
                Log.WriteLine("Salvage: lootWhileSpeedTanking [" + LootWhileSpeedTanking + "]");
                BlacklistWrecks();
                GlobalUseMobileTractor =
                    (bool?)CharacterSettingsXml.Element("useMobileTractorTest") ??
                    (bool?)CommonSettingsXml.Element("useMobileTractorTest") ??
                    (bool?)CharacterSettingsXml.Element("UseMobileTractorTest") ??
                    (bool?)CommonSettingsXml.Element("UseMobileTractorTest") ?? false;
                Log.WriteLine("Salvage: useMobileTractorTest [" + UseMobileTractor + "]");
                AllowSalvagerToStealOnlyWithThisShipName =
                    (string)CharacterSettingsXml.Element("allowSalvagerToStealOnlyWithThisShipName") ??
                    (string)CommonSettingsXml.Element("allowSalvagerToStealOnlyWithThisShipName") ?? "Thief";
                Log.WriteLine("Salvage: allowSalvagerToStealOnlyWithThisShipName [" + AllowSalvagerToStealOnlyWithThisShipName + "]");
            }
            catch (Exception exception)
            {
                Log.WriteLine("Error Loading Loot and Salvage Settings [" + exception + "]");
            }
        }

        internal static double lootItemsIskTotal { get; set; } = 0;

        /// <summary>
        ///     Loot any wrecks & cargo containers close by
        /// </summary>
        public static bool LootWrecks()
        {
            try
            {
                if (ESCache.Instance.Containers == null) return false;
                if (ESCache.Instance.Containers.Count == 0)
                {
                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("LootWrecks: if (!ESCache.Instance.Containers.Any()), waiting");
                    return true;
                }

                if (Time.Instance.NextLootAction > DateTime.UtcNow)
                {
                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("LootWrecks: Cache.Instance.NextLootAction is still in the future, waiting");
                    return true;
                }

                if (!ESCache.Instance.InAbyssalDeadspace)
                {
                    //
                    // when full return to base and unloadloot
                    //
                    if (UnloadLootAtStation && ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Capacity > 150 &&
                        ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity < 50)
                    {
                        if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
                        {
                            if (Drones.DronePriorityEntities.Any(pt => pt.IsWarpScramblingMe))
                                return false;

                            Log.WriteLine("(mission) We are full, heading back to base to dump loot ");
                            CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                            return true;
                        }

                        Log.WriteLine("We are full: we are using a behavior that does not have a supported place to auto dump loot: error!");
                    }
                }

                // Open a container in range
                int containersProcessedThisTick = 0;

                if (DebugConfig.DebugLootWrecks)
                {
                    int containersInRangeCount = 0;
                    if (ESCache.Instance.Containers == null) return false;
                    if (ESCache.Instance.Containers.Any(i => i.Distance < (double)Distances.ScoopRange))
                        containersInRangeCount = ESCache.Instance.Containers.Count(i => i.Distance < (double)Distances.ScoopRange);

                    List<EntityCache> containersOutOfRange = ESCache.Instance.Containers.Where(e => e.Distance >= (int)Distances.ScoopRange).ToList();
                    int containersOutOfRangeCount = 0;
                    if (containersOutOfRange.Count > 0)
                        containersOutOfRangeCount = containersOutOfRange.Count;

                    Log.WriteLine("Debug: containersInRange count [" + containersInRangeCount + "]");
                    Log.WriteLine("Debug: containersOutOfRange count [" + containersOutOfRangeCount + "]");
                }

                if (ESCache.Instance.CurrentShipsCargo == null)
                {
                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("LootWrecks: if (Cache.Instance.CurrentShipsCargo == null)");
                    return true;
                }

                List<ItemCache> shipsCargo = new List<ItemCache>();
                double freeCargoCapacity = 0;
                if (ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
                {
                    shipsCargo = ESCache.Instance.CurrentShipsCargo.Items.Select(i => new ItemCache(i)).ToList();
                    freeCargoCapacity = ESCache.Instance.CurrentShipsCargo.Capacity - (double)ESCache.Instance.CurrentShipsCargo.UsedCapacity;
                }
                else
                {
                    freeCargoCapacity = ESCache.Instance.CurrentShipsCargo.Capacity;
                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("if (!Cache.Instance.CurrentShipsCargo.Items.Any()) - really? 0 items in cargo?");
                }

                if (DebugConfig.DebugLootWrecks)
                    Log.WriteLine("FreeCargoCapacity [" + freeCargoCapacity + "]");

                if (ESCache.Instance.Containers == null) return false;
                foreach (
                    EntityCache containerEntity in
                    ESCache.Instance.Containers.Where(e => e.Distance <= (int)Distances.ScoopRange && (e.HaveLootRights || Salvage.AllowSalvagerToSteal)).OrderByDescending(i => i.IsLootTarget))
                {
                    containersProcessedThisTick++;

                    // Empty wreck, ignore
                    if (containerEntity.IsWreckEmpty) //this only returns true if it is a wreck, not for cargo containers, spawn containers, etc.
                    {
                        ESCache.Instance.LootedContainers.Add(containerEntity.Id);
                        if (DebugConfig.DebugLootWrecks) Log.WriteLine("Ignoring Empty Wreck");
                        continue;
                    }

                    if (WreckBlackList.Any(a => a == containerEntity.TypeId) && !ESCache.Instance.ListofContainersToLoot.Contains(containerEntity.Id))
                    {
                        ESCache.Instance.LootedContainers.Add(containerEntity.Id);
                        if (DebugConfig.DebugTargetWrecks)
                            Log.WriteLine("WreckBlackList.Any(a => a == containerEntity.TypeId) && !ESCache.Instance.ListofContainersToLoot.Contains(containerEntity.Id)");

                        continue;
                    }

                    // We looted this container
                    if (ESCache.Instance.LootedContainers.Contains(containerEntity.Id) && !ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("We have already looted [" + containerEntity.Id + "]");
                        continue;
                    }

                    // Ignore open request within 10 seconds
                    if (OpenedContainers.ContainsKey(containerEntity.Id) && DateTime.UtcNow.Subtract(OpenedContainers[containerEntity.Id]).TotalSeconds < 10)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("We attempted to open [" + containerEntity.Id + "] less than 10 sec ago, ignoring");
                        continue;
                    }

                    // Don't even try to open a wreck if you are speed tanking and you are not processing a loot action
                    if (NavigateOnGrid.SpeedTank && !LootWhileSpeedTanking && !OpenWrecks)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("SpeedTank is true and OpenWrecks is false [" + containerEntity.Id + "]");
                        continue;
                    }

                    // Don't even try to open a wreck if you are specified LootEverything as false and you are not processing a loot action
                    //      this is currently commented out as it would keep Golems and other non-speed tanked ships from looting the field as they cleared
                    //      missions, but NOT stick around after killing things to clear it ALL. Looteverything==false does NOT mean loot nothing
                    //if (Settings.Instance.LootEverything == false && Cache.Instance.OpenWrecks == false)
                    //    continue;

                    // Open the container
                    ESCache.Instance.ContainerInSpace = ESCache.Instance.DirectEve.GetContainer(containerEntity.Id);
                    if (ESCache.Instance.ContainerInSpace == null)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("if (Cache.Instance.ContainerInSpace == null)");
                        continue;
                    }

                    if (ESCache.Instance.ContainerInSpace.Window == null)
                    {
                        if (containerEntity.OpenCargo())
                        {
                            if (DebugConfig.DebugLootWrecks)
                                Log.WriteLine("if (containerEntity.OpenCargo())");
                        }

                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("if (Cache.Instance.ContainerInSpace.Window == null)");

                        return true;
                    }

                    if (!ESCache.Instance.ContainerInSpace.Window.IsReady)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("LootWrecks: Cache.Instance.ContainerInSpace.Window is not ready");
                        return true;
                    }

                    if (ESCache.Instance.ContainerInSpace.Window.IsReady)
                    {
                        Log.WriteLine("Opened container [" + containerEntity.Name + "][" + Math.Round(containerEntity.Distance / 1000, 0) + "k][ID: " +
                                      containerEntity.MaskedId + "]");
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("LootWrecks: Cache.Instance.ContainerInSpace.Window is ready");
                        OpenedContainers[containerEntity.Id] = DateTime.UtcNow;

                        // List its items
                        IEnumerable<ItemCache> items = ESCache.Instance.ContainerInSpace.Items.Select(i => new ItemCache(i)).ToList();
                        if (items.Any())
                            Log.WriteLine("Found [" + items.Count() + "] items in [" + containerEntity.Name + "][" +
                                          Math.Round(containerEntity.Distance / 1000, 0) + "k][" +
                                          containerEntity.MaskedId + "]");

                        // Build a list of items to loot
                        List<ItemCache> lootItems = new List<ItemCache>();

                        // log wreck contents to file
                        if (!Statistics.WreckStatistics(items, containerEntity)) break;
                        if (!Statistics.LootStatisticsCsv(items, containerEntity)) break;

                        if (items.Any())
                            foreach (ItemCache item in items.OrderByDescending(i => i.IsContraband).ThenByDescending(i => i.IskPerM3))
                            {
                                if (freeCargoCapacity < 400)
                                {
                                    if (item.GroupId == (int)Group.CapacitorGroupCharge)
                                        continue;

                                    if (item.GroupId == (int)Group.CombatDrone && item.Volume > 5)
                                        continue;
                                } //this should allow BSs to not pickup large low value items but haulers and noctis' to scoop everything

                                // We pick up loot depending on isk per m3
                                bool _isMissionItem = MissionSettings.MissionItems.Contains((item.Name ?? string.Empty).ToLower());

                                // Never pick up contraband (unless its the mission item)
                                if (item.IsContraband) //is the mission item EVER contraband?!
                                {
                                    if (DebugConfig.DebugLootWrecks)
                                        Log.WriteLine("[" + item.Name + "] is not the mission item and is considered Contraband: ignore it");
                                    ESCache.Instance.LootedContainers.Add(containerEntity.Id);
                                    continue;
                                }

                                if (!LootOnlyWhatYouCanWithoutSlowingDownMissionCompletion)
                                    if (!_isMissionItem && !LootEverything)
                                        continue;

                                try
                                {
                                    // We are at our max, either make room or skip the item
                                    if (freeCargoCapacity - item.TotalVolume <= (item.IsMissionItem ? 0 : ReserveCargoCapacity))
                                    {
                                        Log.WriteLine("We Need More m3: FreeCargoCapacity [" + freeCargoCapacity + "] - [" + item.Name + "][" +
                                                      item.TotalVolume +
                                                      "total][" + item.Volume + "each]");

                                        // Make a list of items which are worth less
                                        List<ItemCache> worthLess = null;
                                        if (_isMissionItem)
                                            worthLess = shipsCargo;
                                        else if (item.IskPerM3.HasValue)
                                            worthLess = shipsCargo.Where(sc => sc.IskPerM3.HasValue && (sc.IskPerM3 < item.IskPerM3 || sc.GroupId == (int)Group.CapacitorGroupCharge)) .ToList();
                                        else
                                            worthLess = shipsCargo.Where(sc => !sc.IsMissionItem && (sc.IskPerM3.HasValue || sc.GroupId == (int)Group.CapacitorGroupCharge)).ToList();

                                        if (State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                                        {
                                            // Remove mission item from this list
                                            worthLess.RemoveAll(wl => MissionSettings.MissionItems.Contains((wl.Name ?? string.Empty).ToLower()));
                                            if (!string.IsNullOrEmpty(MissionSettings.MoveMissionItems))
                                                worthLess.RemoveAll(wl => (wl.Name ?? string.Empty).ToLower() == MissionSettings.MoveMissionItems.ToLower());

                                            // Consider dropping ammo if it concerns the mission item!
                                            if (!_isMissionItem)
                                                worthLess.RemoveAll(wl => DirectUIModule.DefinedAmmoTypes.Any(a => a.TypeId == wl.TypeId));
                                        }

                                        // Nothing is worth less then the current item
                                        if (worthLess.Count == 0)
                                        {
                                            if (DebugConfig.DebugLootWrecks)
                                                Log.WriteLine("[" + item.Name + "] ::: if (!worthLess.Any()) continue ");
                                            continue;
                                        }

                                        // Not enough space even if we dumped the crap
                                        if (freeCargoCapacity + worthLess.Sum(wl => wl.TotalVolume) < item.TotalVolume)
                                        {
                                            if (item.IsMissionItem)
                                                Log.WriteLine("Not enough space for [" + item.Name + "] Need [" + item.TotalVolume +
                                                              "] maximum available [" +
                                                              (freeCargoCapacity + worthLess.Sum(wl => wl.TotalVolume)) + "]");
                                            continue;
                                        }

                                        // Start clearing out items that are worth less
                                        List<DirectItem> moveTheseItems = new List<DirectItem>();
                                        foreach (
                                            ItemCache wl in
                                            worthLess.OrderBy(wl => wl.IskPerM3 ?? double.MaxValue)
                                                .ThenByDescending(wl => wl.TotalVolume))
                                        {
                                            // Mark this item as moved
                                            moveTheseItems.Add(wl.DirectItem);

                                            // Subtract (now) free volume
                                            freeCargoCapacity += wl.TotalVolume;

                                            // We freed up enough space?
                                            if (freeCargoCapacity - item.TotalVolume >= ReserveCargoCapacity)
                                                break;
                                        }

                                        if (moveTheseItems.Count > 0)
                                        {
                                            //GotoBase and dump loot in the hopes that we can grab what we need on the next run
                                            if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
                                            {
                                                if (Drones.DronePriorityEntities.Any(pt => pt.IsWarpScramblingMe))
                                                    return false;

                                                Log.WriteLine("We are full, not enough room for the mission item. Heading back to base to dump loot.");
                                                CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                                            }

                                            return true;
                                        }

                                        return true;
                                    }

                                    // Update free space
                                    freeCargoCapacity -= item.TotalVolume;
                                    lootItems.Add(item);
                                }
                                catch (Exception exception)
                                {
                                    Log.WriteLine("We Need More m3: Exception [" + exception + "]");
                                }
                            }

                        // Loot actual items
                        if (lootItems.Count != 0)
                        {
                            Log.WriteLine("Looting container [" + containerEntity.Name + "][" + Math.Round(containerEntity.Distance / 1000, 0) +
                                          "k][ID: " +
                                          containerEntity.MaskedId + "], [" + lootItems.Count + "] valuable items");
                            if (DebugConfig.DebugLootWrecks || ESCache.Instance.InAbyssalDeadspace || ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController) || ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController))
                            {
                                int icount = 0;
                                Log.WriteLine("lootItemsIskTotal [" + lootItemsIskTotal + "] was the previous loot total");
                                if (lootItems != null && lootItems.Count > 0)
                                {
                                    foreach (ItemCache lootItem in lootItems)
                                    {
                                        icount++;


                                        try
                                        {
                                            lootItemsIskTotal = Math.Round(lootItemsIskTotal + (lootItem.Value ?? 0 * lootItem.Quantity), 0);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.WriteLine("Exception [" + ex + "]");
                                        }

                                        Log.WriteLine("[" + icount + "] LootItems Contains: [" + lootItem.Name + "] Quantity [" + lootItem.Quantity +
                                                      "] value [" + lootItem.Value + " isk ] groupID [" + lootItem.GroupId + "] typeID [" + lootItem.TypeId +
                                                      "] lootItemsIskTotal [" + lootItemsIskTotal + "]");

                                        if (lootItem.GroupId == (int)Group.Drugs ||
                                            lootItem.GroupId == (int)Group.ToxicWaste ||
                                            lootItem.TypeId == (int)TypeID.Small_Arms ||
                                            lootItem.TypeId == (int)TypeID.Ectoplasm)
                                        {
                                            lootItems.Remove(lootItem);
                                            Log.WriteLine("[" + icount + "] Removed this from LootItems before looting [" + lootItem.Name +
                                                          "] Quantity[" +
                                                          lootItem.Quantity + "k] isContraband [" + lootItem.IsContraband + "] groupID [" +
                                                          lootItem.GroupId +
                                                          "] typeID [" + lootItem.TypeId + "] isCommonMissionItem [" + lootItem.IsCommonMissionItem +
                                                          "]");
                                        }
                                    }
                                }

                                string msg = "lootItemsIskTotal [" + lootItemsIskTotal + "] is the new loot total";
                                Log.WriteLine(msg);
                                //DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                                if (lootItemsIskTotal != 0)
                                {
                                    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LootValueGatheredToday), Math.Round(lootItemsIskTotal, 0));
                                }
                            }

                            if (!ESCache.Instance.CurrentShipsCargo.Add(lootItems.Select(i => i.DirectItem))) return false;
                        }
                        else
                        {
                            Log.WriteLine("Container [" + containerEntity.Name + "][" + Math.Round(containerEntity.Distance / 1000, 0) + "k][ID: " +
                                          containerEntity.MaskedId + "] contained no valuable items");
                        }

                        if (containerEntity.CargoWindow != null)
                        {
                            Log.WriteLine("Attempting to CloseCargoWindow named [" + containerEntity.CargoWindow.Name + "]");
                            containerEntity.CargoWindow.Close();
                        }

                        return false;
                    }

                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("Reached End of LootWrecks Routine w/o finding a wreck to loot");

                    return true;
                }

                return true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return true;
            }
        }

        public static bool LootWrecks_ValuableOnly()
        {
            try
            {
                if (ESCache.Instance.Containers == null) return false;
                if (ESCache.Instance.Containers.Count == 0)
                {
                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("LootWrecks: if (!ESCache.Instance.Containers.Any()), waiting");
                    return true;
                }

                if (Time.Instance.NextLootAction > DateTime.UtcNow)
                {
                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("LootWrecks: Cache.Instance.NextLootAction is still in the future, waiting");
                    return true;
                }

                if (!ESCache.Instance.InAbyssalDeadspace)
                {
                    //
                    // when full return to base and unloadloot
                    //
                    if (UnloadLootAtStation && ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Capacity > 150 &&
                        ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity < 50)
                    {
                        if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
                        {
                            if (Drones.DronePriorityEntities.Any(pt => pt.IsWarpScramblingMe))
                                return false;

                            Log.WriteLine("(mission) We are full, heading back to base to dump loot ");
                            CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                            return true;
                        }

                        Log.WriteLine("We are full: we are using a behavior that does not have a supported place to auto dump loot: error!");
                    }
                }

                // Open a container in range
                int containersProcessedThisTick = 0;

                if (DebugConfig.DebugLootWrecks)
                {
                    int containersInRangeCount = 0;
                    if (ESCache.Instance.Containers == null) return false;
                    if (ESCache.Instance.Containers.Any(i => i.Distance < (double)Distances.ScoopRange))
                        containersInRangeCount = ESCache.Instance.Containers.Count(i => i.Distance < (double)Distances.ScoopRange);

                    List<EntityCache> containersOutOfRange = ESCache.Instance.Containers.Where(e => e.Distance >= (int)Distances.ScoopRange).ToList();
                    int containersOutOfRangeCount = 0;
                    if (containersOutOfRange.Count > 0)
                        containersOutOfRangeCount = containersOutOfRange.Count;

                    Log.WriteLine("Debug: containersInRange count [" + containersInRangeCount + "]");
                    Log.WriteLine("Debug: containersOutOfRange count [" + containersOutOfRangeCount + "]");
                }

                if (ESCache.Instance.CurrentShipsCargo == null)
                {
                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("LootWrecks: if (Cache.Instance.CurrentShipsCargo == null)");
                    return true;
                }

                List<ItemCache> shipsCargo = new List<ItemCache>();
                double freeCargoCapacity = 0;
                if (ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
                {
                    shipsCargo = ESCache.Instance.CurrentShipsCargo.Items.Select(i => new ItemCache(i)).ToList();
                    freeCargoCapacity = ESCache.Instance.CurrentShipsCargo.Capacity - (double)ESCache.Instance.CurrentShipsCargo.UsedCapacity;
                }
                else
                {
                    freeCargoCapacity = ESCache.Instance.CurrentShipsCargo.Capacity;
                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("if (!Cache.Instance.CurrentShipsCargo.Items.Any()) - really? 0 items in cargo?");
                }

                if (DebugConfig.DebugLootWrecks)
                    Log.WriteLine("FreeCargoCapacity [" + freeCargoCapacity + "]");

                if (ESCache.Instance.Containers == null) return false;
                foreach (
                    EntityCache containerEntity in
                    ESCache.Instance.Containers.Where(e => e.Distance <= (int)Distances.ScoopRange && (e.HaveLootRights || Salvage.AllowSalvagerToSteal)).OrderByDescending(i => i.IsLootTarget))
                {
                    containersProcessedThisTick++;

                    // Empty wreck, ignore
                    if (containerEntity.IsWreckEmpty) //this only returns true if it is a wreck, not for cargo containers, spawn containers, etc.
                    {
                        ESCache.Instance.LootedContainers.Add(containerEntity.Id);
                        if (DebugConfig.DebugLootWrecks) Log.WriteLine("Ignoring Empty Wreck");
                        continue;
                    }

                    if (WreckBlackList.Any(a => a == containerEntity.TypeId) && !ESCache.Instance.ListofContainersToLoot.Contains(containerEntity.Id))
                    {
                        ESCache.Instance.LootedContainers.Add(containerEntity.Id);
                        if (DebugConfig.DebugTargetWrecks)
                            Log.WriteLine("WreckBlackList.Any(a => a == containerEntity.TypeId) && !ESCache.Instance.ListofContainersToLoot.Contains(containerEntity.Id)");

                        continue;
                    }

                    // We looted this container
                    if (ESCache.Instance.LootedContainers.Contains(containerEntity.Id) && !ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("We have already looted [" + containerEntity.Id + "]");
                        continue;
                    }

                    // Ignore open request within 10 seconds
                    if (OpenedContainers.ContainsKey(containerEntity.Id) && DateTime.UtcNow.Subtract(OpenedContainers[containerEntity.Id]).TotalSeconds < 10)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("We attempted to open [" + containerEntity.Id + "] less than 10 sec ago, ignoring");
                        continue;
                    }

                    // Don't even try to open a wreck if you are speed tanking and you are not processing a loot action
                    if (NavigateOnGrid.SpeedTank && !LootWhileSpeedTanking && !OpenWrecks)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("SpeedTank is true and OpenWrecks is false [" + containerEntity.Id + "]");
                        continue;
                    }

                    // Don't even try to open a wreck if you are specified LootEverything as false and you are not processing a loot action
                    //      this is currently commented out as it would keep Golems and other non-speed tanked ships from looting the field as they cleared
                    //      missions, but NOT stick around after killing things to clear it ALL. Looteverything==false does NOT mean loot nothing
                    //if (Settings.Instance.LootEverything == false && Cache.Instance.OpenWrecks == false)
                    //    continue;

                    // Open the container
                    ESCache.Instance.ContainerInSpace = ESCache.Instance.DirectEve.GetContainer(containerEntity.Id);
                    if (ESCache.Instance.ContainerInSpace == null)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("if (Cache.Instance.ContainerInSpace == null)");
                        continue;
                    }

                    if (ESCache.Instance.ContainerInSpace.Window == null)
                    {
                        if (containerEntity.OpenCargo())
                        {
                            if (DebugConfig.DebugLootWrecks)
                                Log.WriteLine("if (containerEntity.OpenCargo())");
                            Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
                        }

                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("if (Cache.Instance.ContainerInSpace.Window == null)");

                        return true;
                    }

                    if (!ESCache.Instance.ContainerInSpace.Window.IsReady)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("LootWrecks: Cache.Instance.ContainerInSpace.Window is not ready");
                        return true;
                    }

                    if (ESCache.Instance.ContainerInSpace.Window.IsReady)
                    {
                        Log.WriteLine("Opened container [" + containerEntity.Name + "][" + Math.Round(containerEntity.Distance / 1000, 0) + "k][ID: " +
                                      containerEntity.MaskedId + "]");
                        if (DebugConfig.DebugLootWrecks)
                            Log.WriteLine("LootWrecks: Cache.Instance.ContainerInSpace.Window is ready");
                        OpenedContainers[containerEntity.Id] = DateTime.UtcNow;
                        Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);

                        // List its items
                        IEnumerable<ItemCache> items = ESCache.Instance.ContainerInSpace.Items.Select(i => new ItemCache(i)).ToList();
                        if (items.Any())
                            Log.WriteLine("Found [" + items.Count() + "] items in [" + containerEntity.Name + "][" +
                                          Math.Round(containerEntity.Distance / 1000, 0) + "k][" +
                                          containerEntity.MaskedId + "]");

                        // Build a list of items to loot
                        List<ItemCache> lootItems = new List<ItemCache>();

                        // log wreck contents to file
                        if (!Statistics.WreckStatistics(items, containerEntity)) break;
                        if (!Statistics.LootStatisticsCsv(items, containerEntity)) break;

                        if (items.Any())
                            foreach (ItemCache item in items.OrderByDescending(i => i.IsContraband).ThenByDescending(i => i.IskPerM3))
                            {
                                if (freeCargoCapacity < 400)
                                {
                                    if (item.GroupId == (int)Group.CapacitorGroupCharge)
                                        continue;

                                    if (item.GroupId == (int)Group.CombatDrone && item.Volume > 5)
                                        continue;
                                } //this should allow BSs to not pickup large low value items but haulers and noctis' to scoop everything

                                // We pick up loot depending on isk per m3
                                bool _isMissionItem = MissionSettings.MissionItems.Contains((item.Name ?? string.Empty).ToLower());

                                // Never pick up contraband (unless its the mission item)
                                if (item.IsContraband) //is the mission item EVER contraband?!
                                {
                                    if (DebugConfig.DebugLootWrecks)
                                        Log.WriteLine("[" + item.Name + "] is not the mission item and is considered Contraband: ignore it");
                                    ESCache.Instance.LootedContainers.Add(containerEntity.Id);
                                    continue;
                                }

                                if (!LootOnlyWhatYouCanWithoutSlowingDownMissionCompletion)
                                    if (!_isMissionItem && !LootEverything)
                                        continue;

                                try
                                {
                                    // We are at our max, either make room or skip the item
                                    if (freeCargoCapacity - item.TotalVolume <= (item.IsMissionItem ? 0 : ReserveCargoCapacity))
                                    {
                                        Log.WriteLine("We Need More m3: FreeCargoCapacity [" + freeCargoCapacity + "] - [" + item.Name + "][" +
                                                      item.TotalVolume +
                                                      "total][" + item.Volume + "each]");

                                        // Make a list of items which are worth less
                                        List<ItemCache> worthLess = null;
                                        if (_isMissionItem)
                                            worthLess = shipsCargo;
                                        else if (item.IskPerM3.HasValue)
                                            worthLess = shipsCargo.Where(sc => sc.IskPerM3.HasValue && sc.IskPerM3 < item.IskPerM3).ToList();
                                        else
                                            worthLess = shipsCargo.Where(sc => !sc.IsMissionItem && sc.IskPerM3.HasValue).ToList();

                                        if (State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                                        {
                                            // Remove mission item from this list
                                            worthLess.RemoveAll(wl => MissionSettings.MissionItems.Contains((wl.Name ?? string.Empty).ToLower()));
                                            if (!string.IsNullOrEmpty(MissionSettings.MoveMissionItems))
                                                worthLess.RemoveAll(wl => (wl.Name ?? string.Empty).ToLower() == MissionSettings.MoveMissionItems.ToLower());

                                            // Consider dropping ammo if it concerns the mission item!
                                            if (!_isMissionItem)
                                                worthLess.RemoveAll(wl => DirectUIModule.DefinedAmmoTypes.Any(a => a.TypeId == wl.TypeId));
                                        }

                                        // Nothing is worth less then the current item
                                        if (worthLess.Count == 0)
                                        {
                                            if (DebugConfig.DebugLootWrecks)
                                                Log.WriteLine("[" + item.Name + "] ::: if (!worthLess.Any()) continue ");
                                            continue;
                                        }

                                        // Not enough space even if we dumped the crap
                                        if (freeCargoCapacity + worthLess.Sum(wl => wl.TotalVolume) < item.TotalVolume)
                                        {
                                            if (item.IsMissionItem)
                                                Log.WriteLine("Not enough space for [" + item.Name + "] Need [" + item.TotalVolume +
                                                              "] maximum available [" +
                                                              (freeCargoCapacity + worthLess.Sum(wl => wl.TotalVolume)) + "]");
                                            continue;
                                        }

                                        // Start clearing out items that are worth less
                                        List<DirectItem> moveTheseItems = new List<DirectItem>();
                                        foreach (
                                            ItemCache wl in
                                            worthLess.OrderBy(wl => wl.IskPerM3 ?? double.MaxValue)
                                                .ThenByDescending(wl => wl.TotalVolume))
                                        {
                                            // Mark this item as moved
                                            moveTheseItems.Add(wl.DirectItem);

                                            // Subtract (now) free volume
                                            freeCargoCapacity += wl.TotalVolume;

                                            // We freed up enough space?
                                            if (freeCargoCapacity - item.TotalVolume >= ReserveCargoCapacity)
                                                break;
                                        }

                                        if (moveTheseItems.Count > 0)
                                        {
                                            //GotoBase and dump loot in the hopes that we can grab what we need on the next run
                                            if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
                                            {
                                                if (Drones.DronePriorityEntities.Any(pt => pt.IsWarpScramblingMe))
                                                    return false;

                                                Log.WriteLine("We are full, not enough room for the mission item. Heading back to base to dump loot.");
                                                CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                                            }

                                            return true;
                                        }

                                        return true;
                                    }

                                    // Update free space
                                    freeCargoCapacity -= item.TotalVolume;
                                    lootItems.Add(item);
                                }
                                catch (Exception exception)
                                {
                                    Log.WriteLine("We Need More m3: Exception [" + exception + "]");
                                }
                            }

                        // Loot actual items
                        if (lootItems.Count != 0)
                        {
                            Log.WriteLine("Looting container [" + containerEntity.Name + "][" + Math.Round(containerEntity.Distance / 1000, 0) +
                                          "k][ID: " +
                                          containerEntity.MaskedId + "], [" + lootItems.Count + "] valuable items");
                            if (DebugConfig.DebugLootWrecks || ESCache.Instance.InAbyssalDeadspace || ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController) || ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                            {
                                int icount = 0;
                                if (lootItems != null && lootItems.Count > 0)
                                    foreach (ItemCache lootItem in lootItems)
                                    {
                                        icount++;
                                        Log.WriteLine("[" + icount + "] LootItems Contains: [" + lootItem.Name + "] Quantity [" + lootItem.Quantity +
                                                      "] value [" + lootItem.Value + " isk ] groupID [" + lootItem.GroupId + "] typeID [" + lootItem.TypeId +
                                                      "]");
                                        if (lootItem.GroupId == (int)Group.Drugs ||
                                            lootItem.GroupId == (int)Group.ToxicWaste ||
                                            lootItem.TypeId == (int)TypeID.Small_Arms ||
                                            lootItem.TypeId == (int)TypeID.Ectoplasm)
                                        {
                                            lootItems.Remove(lootItem);
                                            Log.WriteLine("[" + icount + "] Removed this from LootItems before looting [" + lootItem.Name +
                                                          "] Quantity[" +
                                                          lootItem.Quantity + "k] isContraband [" + lootItem.IsContraband + "] groupID [" +
                                                          lootItem.GroupId +
                                                          "] typeID [" + lootItem.TypeId + "] isCommonMissionItem [" + lootItem.IsCommonMissionItem +
                                                          "]");
                                        }
                                    }
                            }

                            if (!ESCache.Instance.CurrentShipsCargo.Add(lootItems.Select(i => i.DirectItem))) return false;
                        }
                        else
                        {
                            Log.WriteLine("Container [" + containerEntity.Name + "][" + Math.Round(containerEntity.Distance / 1000, 0) + "k][ID: " +
                                          containerEntity.MaskedId + "] contained no valuable items");
                        }

                        if (containerEntity.CargoWindow != null)
                        {
                            Log.WriteLine("Attempting to CloseCargoWindow named [" + containerEntity.CargoWindow.Name + "]");
                            containerEntity.CargoWindow.Close();
                        }

                        return false;
                    }

                    if (DebugConfig.DebugLootWrecks)
                        Log.WriteLine("Reached End of LootWrecks Routine w/o finding a wreck to loot");

                    return true;
                }

                return true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return true;
            }
        }

        public static void MoveIntoRangeOfWrecks()
        // DO NOT USE THIS ANYWHERE EXCEPT A PURPOSEFUL SALVAGE BEHAVIOR! - if you use this while in combat it will make you go poof quickly.
        {
            //we cant move in bastion mode, do not try
            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                return;

            if (ESCache.Instance.UnlootedContainers.Count == 0 && ESCache.Instance.Wrecks.Count == 0)
            {
                Log.WriteLine("There are 0 UnlootedContainers or Wrecks left on the field: no UnlootedContainers to approach.");
                return;
            }

            Salvage.TargetWrecks(ESCache.Instance.UnlootedContainers);

            EntityCache closestWreck = ESCache.Instance.UnlootedContainers.FirstOrDefault()
                ?? ESCache.Instance.Wrecks.FirstOrDefault();

            if (closestWreck != null && Math.Round(closestWreck.Distance, 0) > (int)Distances.SafeScoopRange)
            {
                NavigateOnGrid.NavigateToTarget(closestWreck, 0);
                return;
            }

            if (closestWreck != null && closestWreck.Distance <= (int)Distances.SafeScoopRange && ESCache.Instance.FollowingEntity != null)
                if (Time.Instance.NextApproachAction < DateTime.UtcNow)
                    if (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity > 1 && DateTime.UtcNow > Time.Instance.NextApproachAction)
                        NavigateOnGrid.StopMyShip("Stop ship, ClosestWreck [" + Math.Round(closestWreck.Distance, 0) +
                                                  "m] is within scooprange [" + (int)Distances.SafeScoopRange + "m] and we were approaching");
        }

        private static bool NearSalvageBookmark
        {
            get
            {
                try
                {
                    if (ESCache.Instance.InWarp) return false;

                    if (ESCache.Instance.CachedBookmarks.Count > 0)
                    {
                        if (ESCache.Instance.CachedBookmarks.Any(i => i.IsInCurrentSystem && (double)Distances.OnGridWithMe > i.Distance && i.Title.ToLower().Contains("salvage".ToLower())))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return false;
            }
        }

        private static bool ShouldWeDecloak()
        {
            //
            // insert reasons to decloak
            //
            return false;
        }

        public static void AbyssalSalvageProcessState()
        {
            if (!ESCache.Instance.Wrecks.Any())
                return;

            if (ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.AbyssalController) &&
                ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
            {
                if (NavigateOnGrid.ChooseNavigateOnGridTargetIds != null)
                    NavigateOnGrid.NavigateIntoRange(NavigateOnGrid.ChooseNavigateOnGridTargetIds, "AbyssalSalvageProcessState", true);
            }

            switch (State.CurrentSalvageState)
            {
                case SalvageState.TargetWrecks:
                    try
                    {
                        if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                        {
                            ChangeSalvageState(SalvageState.LootWrecks, false);
                            return;
                        }

                        if (ESCache.Instance.Wrecks.Any(i => i.IsReadyToTarget && (!i.IsTarget && !i.IsTargeting)))
                        {
                            if (DebugConfig.DebugSalvage) Log.WriteLine("SalvageState.TargetWrecks..");
                            TargetWrecks();
                        }

                        ChangeSalvageState(SalvageState.LootWrecks, true);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                        break;
                    }

                case SalvageState.LootWrecks:
                    try
                    {
                        if (DebugConfig.DebugSalvage) Log.WriteLine("SalvageState.LootWrecks:");

                        LootWrecks();

                        if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                            return;

                        if (ESCache.Instance.Wrecks.Any(i => i.IsReadyToTarget && (!i.IsTarget && !i.IsTargeting)))
                        {
                            ChangeSalvageState(SalvageState.TargetWrecks, false);
                            break;
                        }

                        ChangeSalvageState(SalvageState.SalvageWrecks, false);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                        break;
                    }

                case SalvageState.SalvageWrecks:
                    try
                    {
                        if (DebugConfig.DebugSalvage) Log.WriteLine("SalvageState.SalvageWrecks:");
                        ProcessTractorBeams();

                        if (!ESCache.Instance.Wrecks.Any(i => i.IsTarget || i.IsTargeting) && !ESCache.Instance.Wrecks.Any(i => (double)Distances.ScoopRange > i.Distance))
                        {
                            ChangeSalvageState(SalvageState.TargetWrecks, false);
                            break;
                        }

                        ChangeSalvageState(SalvageState.LootWrecks, true);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                        break;
                    }

                case SalvageState.StackItems:

                    ChangeSalvageState(SalvageState.TargetWrecks, false);
                    break;

                case SalvageState.Idle:
                    if (WeShouldSalvageThisStuff)
                    {
                        if (DebugConfig.DebugSalvage) Log.WriteLine("SalvageState.Idle:");
                        ChangeSalvageState(SalvageState.TargetWrecks, false);
                    }

                    break;

                default:

                    // Unknown state, goto first state
                    ChangeSalvageState(SalvageState.TargetWrecks, false);
                    break;
            }
        }

        private static int _processStateIterations = 0;

        public static void ProcessState()
        {
            try
            {
                try
                {
                    if (Time.Instance.LastJumpAction.AddSeconds(8) > DateTime.UtcNow)
                        return;

                    if (Time.Instance.LastDockAction.AddSeconds(8) > DateTime.UtcNow)
                        return;

                    if (Time.Instance.LastActivateFilamentAttempt.AddSeconds(8) > DateTime.UtcNow)
                        return;

                    if (Time.Instance.LastActivateKeyActivationWindowAttempt.AddSeconds(8) > DateTime.UtcNow)
                        return;

                    if (Time.Instance.LastActivateAccelerationGate.AddSeconds(8) > DateTime.UtcNow)
                        return;

                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        AbyssalSalvageProcessState();
                        return;
                    }

                    switch (State.CurrentSalvageState)
                    {
                        case SalvageState.TargetWrecks:
                            try
                            {
                                if (!UnTargetAllWrecks()) return;
                                if (DebugConfig.DebugSalvage) Log.WriteLine("SalvageState.TargetWrecks:");
                                if (!TargetWrecks()) return;

                                // Wait some time here (wait = true) before ProcessState() again as this is the completion of a State loop AND it takes time to target things
                                // Other states can generally proceed from State to State with no delay
                                //
                                ChangeSalvageState(SalvageState.LootWrecks, true);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                                break;
                            }

                        case SalvageState.LootWrecks:
                            try
                            {
                                if (DebugConfig.DebugSalvage) Log.WriteLine("SalvageState.LootWrecks:");

                                if (50 > ESCache.Instance.Containers.Count())
                                {
                                    if (!LootWrecks()) return;
                                }
                                else
                                {
                                    if (!LootWrecks_ValuableOnly()) return;
                                }

                                ChangeSalvageState(SalvageState.SalvageWrecks, false);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                                break;
                            }

                        case SalvageState.SalvageWrecks:
                            try
                            {
                                if (DebugConfig.DebugSalvage) Log.WriteLine("SalvageState.SalvageWrecks:");
                                ProcessTractorBeams();
                                ProcessSalvagers();

                                ChangeSalvageState(SalvageState.StackItems, false);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                                break;
                            }

                        case SalvageState.StackItems:
                            try
                            {
                                if (ESCache.Instance.CurrentShipsCargo == null)
                                    break;

                                if (DebugConfig.DebugSalvage) Log.WriteLine("SalvageState.StackItems:");
                                if (AllOnGridWrecksAndContainers != null && AllOnGridWrecksAndContainers.Count > 0)
                                {
                                    if (!ESCache.Instance.CurrentShipsCargo.StackShipsCargo()) return;
                                }

                                ChangeSalvageState(SalvageState.TargetWrecks, false);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                                break;
                            }


                        case SalvageState.Idle:
                            if (WeShouldSalvageThisStuff)
                            {
                                if (DebugConfig.DebugSalvage) Log.WriteLine("SalvageState.Idle:");
                                ChangeSalvageState(SalvageState.TargetWrecks, true);
                            }

                            break;

                        default:

                            // Unknown state, goto first state
                            ChangeSalvageState(SalvageState.TargetWrecks, true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool WeShouldSalvageThisStuff
        {
            get
            {
                //cant salvage in stations
                if (ESCache.Instance.InStation)
                {
                    if (DebugConfig.DebugSalvage) Log.WriteLine("WeShouldSalvageThisStuff [ false ]");
                    return false;
                }

                if (!ESCache.Instance.InSpace)
                    return false;

                //cant salvage in stations
                if (ESCache.Instance.InWarp)
                    return false;

                //cant salvage if we are cloaked
                if (ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.IsCloaked)
                    return false;

                if (AllOnGridWrecksAndContainers == null)
                    return false;

                if (AllOnGridWrecksAndContainers.Count == 0)
                    return false;

                if (TractorBeams.Count == 0 && AllOnGridWrecksAndContainers.All(i => i.Distance > 10000))
                    return false;

                if (Salvagers.Count == 0 && AllOnGridWrecksAndContainers.All(i => i.Distance > 5000))
                    return false;

                return true;
            }
        }

        private static List<EntityCache> DetermineWhichWrecksToLock(List<EntityCache> targetTheseEntities = null)
        {
            if (targetTheseEntities != null && targetTheseEntities.Any(i => !i.IsTarget))
                return targetTheseEntities;

            List<EntityCache> wrecksToLock = ESCache.Instance.UnlootedContainers.Where(i => i.IsReadyForSalvagerToTarget)
                .OrderByDescending(i => i.HaveLootRights)
                .ThenByDescending(i => !i.IsWreckEmpty)
                .ThenByDescending(i => i.IsLargeWreck)
                .ThenByDescending(i => !i.IsMediumWreck)
                .ThenByDescending(i => !i.IsSmallWreck).ToList();

            if (wrecksToLock.Count == 0)
            {
                wrecksToLock = ESCache.Instance.Wrecks.Where(i => i.IsReadyForSalvagerToTarget)
                    .OrderByDescending(i => i.HaveLootRights)
                    .ThenByDescending(i => !i.IsWreckEmpty)
                    .ThenByDescending(i => i.IsLargeWreck)
                    .ThenByDescending(i => !i.IsMediumWreck)
                    .ThenByDescending(i => !i.IsSmallWreck).ToList();
            }

            return wrecksToLock;
        }

        private static bool UnTargetWrecksAsNeeded()
        {
            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Debug: if (ESCache.Instance.InAbyssalDeadspace).");
                if (ESCache.Instance.Wrecks.Where(x => x.IsTarget).Any(i => i.IsWreckEmpty || !i.IsInTractorRange))
                {
                    var unlockthiswreck = ESCache.Instance.Wrecks.Where(x => x.IsTarget).FirstOrDefault(i => i.IsWreckEmpty || !i.IsInTractorRange);
                    Log.WriteLine("Wreck [" + unlockthiswreck.Name + "] IsWreckEmpty [" + unlockthiswreck.IsWreckEmpty + "] IsInTractorRange [" + unlockthiswreck.IsInTractorRange + "] Distance [" + unlockthiswreck.Nearest1KDistance + "]");
                    unlockthiswreck.UnlockTarget();
                }

                return true;
            }

            if (AllOnGridWrecksAndContainers == null)
                return false;

            if (AllOnGridWrecksAndContainers.Count == 0)
                return false;
            //
            // UnTarget Wrecks/Containers, etc as they get into or out of range
            //
            IEnumerable<EntityCache> lockedWrecks = ESCache.Instance.Targets.Where(x => x.IsWreck || x.IsContainer).ToList();
            foreach (EntityCache lockedWreck in lockedWrecks.OrderByDescending(i => i.IsLootTarget))
            {
                // Unlock if outside tractor range loot range
                if (TractorBeams.Count > 0)
                {
                    //If you do not have loot rights you cant tractor the wreck / container either: but you CAN salvage it!
                    if (lockedWreck.Distance > TractorBeams.Min(t => t.OptimalRange) || (!lockedWreck.HaveLootRights && Salvagers.Count == 0))
                    {
                        if (lockedWreck.UnlockTarget())
                        {
                            Log.WriteLine("Cargo Container [" + lockedWreck.Name + "][" + Math.Round(lockedWreck.Distance / 1000, 0) + "k][ID: " + lockedWreck.MaskedId +
                                      "] is outside tractor range, unlocking");

                            Time.Instance.LastActivatedTimeStamp.Remove(lockedWreck.Id);
                            return false;
                        }
                    }
                }
                else if (Salvagers.Count > 0 && TractorBeams.Count == 0)
                {
                    if (lockedWreck.Distance > Salvagers.Min(t => t.OptimalRange))
                    {
                        if (lockedWreck.UnlockTarget())
                        {
                            Log.WriteLine("Cargo Container [" + lockedWreck.Name + "][" + Math.Round(lockedWreck.Distance / 1000, 0) + "k][ID: " + lockedWreck.MaskedId +
                                      "] is outside salvager range, unlocking");
                            Time.Instance.LastActivatedTimeStamp.Remove(lockedWreck.Id);
                            return false;
                        }
                    }
                }
                else
                {
                    if (lockedWreck.Distance > (double)Distances.SafeScoopRange)
                    {
                        if (lockedWreck.UnlockTarget())
                        {
                            Log.WriteLine("Cargo Container [" + lockedWreck.Name + "][" + Math.Round(lockedWreck.Distance / 1000, 0) + "k][ID: " + lockedWreck.MaskedId + "] is locked but not in scoop range and we have no tractor or salvager, unlocking container.");
                            Time.Instance.LastActivatedTimeStamp.Remove(lockedWreck.Id);
                            return false;
                        }
                    }
                }

                if (Salvagers.Count > 0 && lockedWreck.GroupId == (int)Group.Wreck)
                {
                    if (DebugConfig.DebugTargetWrecks)
                        Log.WriteLine("Debug: if (hasSalvagers && wreck.GroupId != (int)Group.CargoContainer))");
                    return true;
                }

                if (Salvagers.Count == 0)
                    if (lockedWreck.IsWreckEmpty || ESCache.Instance.LootedContainers.Contains(lockedWreck.Id)
                    ) //this  only returns true if it is a wreck, not for cargo containers, spawn containers, etc.
                    {
                        if (lockedWreck.UnlockTarget())
                        {
                            Log.WriteLine("Wreck: [" + lockedWreck.Name + "][" + Math.Round(lockedWreck.Distance / 1000, 0) + "k][ID: " + lockedWreck.MaskedId +
                                      "] wreck is empty, unlocking");

                            if (!ESCache.Instance.LootedContainers.Contains(lockedWreck.Id)) //
                                ESCache.Instance.LootedContainers.Add(lockedWreck.Id);
                            return false;
                        }
                    }

                double mySafeScoopRange = Math.Max(0, (int)Distances.ScoopRange - ESCache.Instance.MyShipEntity.Velocity);
                // Unlock if within loot range
                if (lockedWreck.Distance < mySafeScoopRange && ESCache.Instance.LootedContainers.Contains(lockedWreck.Id) && Salvagers.Count == 0)
                {
                    if (lockedWreck.UnlockTarget())
                    {
                        Log.WriteLine("Cargo Container [" + lockedWreck.Name + "][" + Math.Round(lockedWreck.Distance / 1000, 0) + "k][ID: " + lockedWreck.MaskedId +
                                  "] within loot range, unlocking");
                        return false;
                    }
                }

                // Unlock if within loot range
                if (!OpenWrecks)
                {
                    if (lockedWreck.UnlockTarget())
                    {
                        Log.WriteLine("Cargo Container [" + lockedWreck.Name + "][" + Math.Round(lockedWreck.Distance / 1000, 0) + "k][ID: " + lockedWreck.MaskedId + "]  openwrecks is false: unlocking");
                        return false;
                    }
                }

                if (lockedWrecks != null && lockedWrecks.Any())
                    if (lockedWrecks.Count() == ESCache.Instance.MaxLockedTargets || lockedWrecks.Count() > MaximumWreckTargets)
                    {
                        if (lockedWreck.UnlockTarget())
                        {
                            Log.WriteLine("Cargo Container [" + lockedWreck.Name + "][" + Math.Round(lockedWreck.Distance / 1000, 0) + "k][ID: " + lockedWreck.MaskedId + "] we have [" + lockedWrecks.Count() + "] wrecks targeted!: unlocking");
                            return false;
                        }
                    }
            }

            return true;
        }

        private static bool TargetWrecksAsNeeded(List<EntityCache> targetTheseEntities = null)
        {
            if (AllOnGridWrecksAndContainers == null)
            {
                if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Debug: if (AllOnGridWrecksAndContainers == null)");
                return false;
            }

            if (AllOnGridWrecksAndContainers.Count == 0)
            {
                if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Debug: if (!AllOnGridWrecksAndContainers.Any())");
                return false;
            }

            int tractorAndSalvageWreckTargetCount = AllOnGridWrecksAndContainers.Count(x => x.IsTarget || x.IsTargeting);

            if (tractorAndSalvageWreckTargetCount >= MaximumWreckTargets)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine("Debug: else if (tractorAndSalvageWreckTargetCount [" + tractorAndSalvageWreckTargetCount + "] >= MaximumWreckTargets [" + MaximumWreckTargets + "])");
                return true;
            }

            int combatTargetCount = ESCache.Instance.Targets.Count(i => !i.IsWreck && !i.IsContainer);
            if (combatTargetCount + ESCache.Instance.Targets.Count(i => i.IsWreck || i.IsContainer) >= ESCache.Instance.MaxLockedTargets)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine("Debug: if (combatTargetCount + ESCache.Instance.Targets.Count(i => i.IsWreck || i.IsContainer) >= ESCache.Instance.MaxLockedTargets) continue");
                return true;
            }

            if (!OpenWrecks)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine("Debug: OpenWrecks is false, we do not need to target any wrecks.");
                return true;
            }

            //
            // TargetWrecks/Container, etc If needed
            //
            List<EntityCache> attemptToTargetThese = DetermineWhichWrecksToLock(targetTheseEntities);

            if (attemptToTargetThese != null && attemptToTargetThese.Count > 0)
            {
                foreach (EntityCache wreckNotYetLocked in attemptToTargetThese)
                {
                    if (!wreckNotYetLocked.IsInTractorRange && !wreckNotYetLocked.IsInSalvagerRange)
                    {
                        if (DebugConfig.DebugTargetWrecks)
                            Log.WriteLine("Debug: if (!wreckNotYetLocked.IsInTractorRange) continue Distance [" + wreckNotYetLocked.Nearest1KDistance + "] TractorRange [" + Math.Round((double)TractorBeamRange/1000, 0) +"k]");
                        continue;
                    }

                    //
                    // Note: you cant tractor a wreck you dont have loot rights for even if you want to ninja loot it
                    // so in that case you do not need to target it.
                    // you DO need to target it if you intend to ninja salvage it however!
                    //
                    if (!wreckNotYetLocked.HaveLootRights && Salvagers.Count == 0) //|| targetTheseEntities != null)
                    {
                        if (DebugConfig.DebugTargetWrecks)
                            Log.WriteLine("Debug: if (!wreck.HaveLootRights)");
                        continue;
                    }

                    if (!SalvageAll)
                        if (WreckBlackList.Any(a => a == wreckNotYetLocked.TypeId) && !ESCache.Instance.ListofContainersToLoot.Contains(wreckNotYetLocked.Id))
                        {
                            ESCache.Instance.LootedContainers.Add(wreckNotYetLocked.Id);
                            if (DebugConfig.DebugTargetWrecks)
                                Log.WriteLine("Debug: if (Settings.Instance.WreckBlackList.Any(a => a == wreck.TypeId)");
                            continue;
                        }

                    if (wreckNotYetLocked.GroupId != (int)Group.Wreck && wreckNotYetLocked.GroupId != (int)Group.CargoContainer)
                    {
                        if (DebugConfig.DebugTargetWrecks)
                            Log.WriteLine("Debug: if (wreck.GroupId != (int)Group.Wreck && wreck.GroupId != (int)Group.CargoContainer)");
                        continue;
                    }

                    if (Salvagers.Count == 0)
                    {
                        // Ignore already looted wreck
                        if (ESCache.Instance.LootedContainers.Contains(wreckNotYetLocked.Id))
                        {
                            if (DebugConfig.DebugTargetWrecks)
                                Log.WriteLine("Debug: Ignoring Already Looted Entity ID [" + wreckNotYetLocked.Id + "]");
                            continue;
                        }

                        // Ignore empty wrecks
                        if (wreckNotYetLocked.IsWreckEmpty) //this only returns true if it is a wreck, not for cargo containers, spawn containers, etc.
                        {
                            ESCache.Instance.LootedContainers.Add(wreckNotYetLocked.Id);
                            if (DebugConfig.DebugTargetWrecks)
                                Log.WriteLine("Debug: Ignoring Empty Entity ID [" + wreckNotYetLocked.Id + "]");
                            continue;
                        }

                        if (WrecksWeNoLongerNeedToTractor.Contains(wreckNotYetLocked))
                        {
                            if (DebugConfig.DebugTargetWrecks)
                                Log.WriteLine("Debug: Ignoring wreck Entity ID [" + wreckNotYetLocked.Id + "] because it is in WrecksWeNoLongerNeedToTractor");
                            continue;
                        }
                    }

                    if (!SalvageAll)
                        if (WreckBlackList.Any(a => a == wreckNotYetLocked.TypeId))
                        {
                            Log.WriteLine("Cargo Container [" + wreckNotYetLocked.Name + "][" + Math.Round(wreckNotYetLocked.Distance / 1000, 0) + "k][ID: " + wreckNotYetLocked.MaskedId + "] wreck is on our blacklist, unlocking");
                            ESCache.Instance.LootedContainers.Add(wreckNotYetLocked.Id);
                            continue;
                        }

                    //
                    // this should be only wrecks that are inside tractor range if we have tractors, or inside salvage range if we have salvagers
                    // if we have neither we should not be locking up wrecks
                    //
                    if (wreckNotYetLocked.IsReadyToTarget && wreckNotYetLocked.LockTarget("Salvage"))
                    {
                        Log.WriteLine("Salvage: Locking [" + wreckNotYetLocked.Name + "][" + Math.Round(wreckNotYetLocked.Distance / 1000, 0) + "k][ID: " + wreckNotYetLocked.MaskedId + "][" +
                                      Math.Round(wreckNotYetLocked.Distance / 1000, 0) + "k away] MaxWreckTargets [" + MaximumWreckTargets + "] CurrentWrecksTargeted [" + tractorAndSalvageWreckTargetCount + "]");
                        continue;
                    }
                }

                if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Debug: Done targeting wrecks: attemptToTargetThese count [" + attemptToTargetThese.Count + "]");
                return true;
            }

            if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Debug: !if (attemptToTargetThese != null && attemptToTargetThese.Any())");
            return true;
        }

        /// <summary>
        ///     Target wrecks within range
        /// </summary>
        public static bool TargetWrecks(List<EntityCache> targetTheseEntities = null)
        {
            // We are jammed, we do not need to log (Combat does this already)
            if (ESCache.Instance.MaxLockedTargets == 0)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine(
                        "Debug: if (Cache.Instance.MaxLockedTargets == 0)");
                return true;
            }

            if (!UnTargetWrecksAsNeeded()) return false;
            if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Debug: UnTargetWrecksAsNeeded() returned true");
            TargetWrecksAsNeeded(targetTheseEntities);
            return true;
        }

        private static bool ShouldUnTargetWrecks()
        {
            if (!ESCache.Instance.InMission)
                return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.SalvageGridController))
                return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.None))
                return false;

            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (ESCache.Instance.Wrecks.Count > 0 && ESCache.Instance.Wrecks.All(i => i.IsTarget && i.IsWreckEmpty))
                    return true;

                if (_lastUnTargetAllWrecks.AddSeconds(30 + ESCache.Instance.RandomNumber(1, 60)) > DateTime.UtcNow)
                {
                    ClearPerPocketCache();
                    return true;
                }

                return false;
            }

            if (_lastUnTargetAllWrecks.AddSeconds(150 + ESCache.Instance.RandomNumber(1, 60)) > DateTime.UtcNow)
                return false;

            if (ESCache.Instance.Targets.Count == 0)
                return false;

            return true;
        }

        public static bool UnTargetAllWrecks()
        {
            if (!ShouldUnTargetWrecks()) return true;

            foreach (EntityCache wreck in ESCache.Instance.Targets.Where(i => i.IsWreck))
            {
                Log.WriteLine("unlocking [" + wreck.TypeName + "] at [" + wreck.Nearest1KDistance + "]");
                wreck.UnlockTarget();
                Time.Instance.LastActivatedTimeStamp.Remove(wreck.Id);
            }

            _lastUnTargetAllWrecks = DateTime.UtcNow;
            return true;
        }

        private static bool DeactivateTractorBeams()
        {
            foreach (ModuleCache tractorBeam in TractorBeams)
            {
                ModuleNumber++;

                if (tractorBeam.InLimboState)
                {
                    DebugTractorBeamInfo(tractorBeam, 0, ModuleNumber, "ProcessTractorBeams: Deactivating: InLimboState");
                    continue;
                }

                if (!tractorBeam.IsActive) continue;

                EntityCache wreckWeHaveTractored = ESCache.Instance.Targets.Find(y => (y.IsWreck || y.IsContainer) && tractorBeam.TargetId == y.Id);

                bool currentWreckUnlooted = false;

                if (DebugConfig.DebugTractorBeams)
                    Log.WriteLine("MyShip.Velocity [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "]");
                if (NavigateOnGrid.SpeedTank || (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity > 200))
                {
                    if (DebugConfig.DebugTractorBeams)
                        Log.WriteLine("if (Cache.Instance.MyShip.Velocity > 200)");
                    if (ESCache.Instance.UnlootedContainers.Any(unlootedcontainer => tractorBeam.TargetId == unlootedcontainer.Id))
                    {
                        currentWreckUnlooted = true;
                        if (DebugConfig.DebugTractorBeams)
                            Log.WriteLine("if (tractorBeam.TargetId == unlootedcontainer.Id) break;");
                    }

                    //
                    // if we are going 'fast' and have a salvager keep the wreck tractored so that we can salvage it
                    //
                    if (Salvagers.Count > 0) continue;
                }

                // If the wreck no longer exists, or its within loot range then disable the tractor beam
                // If the wreck no longer exist, beam should be deactivated automatically. Without our interaction.
                // Only deactivate while NOT speed tanking
                if (tractorBeam.IsActive && !NavigateOnGrid.SpeedTank)
                {
                    if (wreckWeHaveTractored == null || UnTractorThisWreck(wreckWeHaveTractored, currentWreckUnlooted))
                    {
                        if (DebugConfig.DebugTractorBeams)
                            if (wreckWeHaveTractored != null)
                            {
                                Log.WriteLine(
                                   "[" + ModuleNumber + "] Tractorbeam: IsActive [" + tractorBeam.IsActive + "] and the wreck [" + wreckWeHaveTractored.Name ??
                                   "null" + "] is in SafeScoopRange [" + Math.Round(wreckWeHaveTractored.Distance / 1000, 0) + "]");
                            }
                            else
                            {
                                Log.WriteLine("[" + ModuleNumber + "] Tractorbeam: IsActive [" + tractorBeam.IsActive + "] on what? wreck was null!");
                            }

                        if (tractorBeam.Click())
                        {
                            tractorsProcessedThisTick++;
                            Time.Instance.NextTractorBeamAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.SalvageDelayBetweenActions_milliseconds);
                            if (tractorsProcessedThisTick < Settings.Instance.NumberOfModulesToActivateInCycle)
                            {
                                if (DebugConfig.DebugTractorBeams)
                                    Log.WriteLine("[" + ModuleNumber + "] Tractorbeam: Process Next Tractorbeam");
                                continue;
                            }

                            if (DebugConfig.DebugTractorBeams)
                                Log.WriteLine("[" + ModuleNumber + "] Tractorbeam: We have processed [" +
                                              Settings.Instance.NumberOfModulesToActivateInCycle +
                                              "] tractors this tick, return");
                            return false;
                        }
                    }

                    continue;
                }

                if (!ESCache.Instance.InAbyssalDeadspace && Salvagers.Count == 0) WrecksWeNoLongerNeedToTractor.Add(TargetedWrecksToTractor.Find(t => t.Id == tractorBeam.TargetId));
            }

            return true;
        }

        private static bool ActivateTractorBeams()
        {
            //
            // Activate tractorbeams
            //
            int WreckNumber = 0;
            foreach (EntityCache wreckToActivateTractorbeam in TargetedWrecksToTractor.Where(x => x.IsTarget && !x.IsTargeting && x.HaveLootRights).OrderByDescending(i => i.IsLootTarget))
            {
                WreckNumber++;
                // This velocity check solves some bugs where velocity showed up as 150000000m/s
                if ((int)wreckToActivateTractorbeam.Velocity >= 495 && !ESCache.Instance.InAbyssalDeadspace) //if the wreck is already moving assume we should not tractor it.
                {
                    if (DebugConfig.DebugTractorBeams)
                        Log.WriteLine("[" + WreckNumber + "] Wreck [" + wreckToActivateTractorbeam.Name + "] at [" + Math.Round(wreckToActivateTractorbeam.Distance / 1000, 0) + "][" + wreckToActivateTractorbeam.MaskedId +
                                      "] is already moving at [" + Math.Round(wreckToActivateTractorbeam.Velocity, 0) + "m/s] and we are [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "]: do not tractor a wreck that is moving");
                    continue;
                }

                if (TractorBeams.Any(i => i.IsActive && i.TargetId == wreckToActivateTractorbeam.Id))
                {
                    if (DebugConfig.DebugTractorBeams)
                        Log.WriteLine("[" + WreckNumber + "] Wreck [" + wreckToActivateTractorbeam.Name + "] at [" + Math.Round(wreckToActivateTractorbeam.Distance / 1000, 0) + "][" + wreckToActivateTractorbeam.MaskedId +
                                      "] is already being tractored.");
                    continue;
                }

                // Is this wreck within range?
                if (wreckToActivateTractorbeam.Distance < (int)Distances.SafeScoopRange)
                    continue;

                if (TractorBeams.Count == 0) return true;

                foreach (ModuleCache tractorBeam in TractorBeams)
                {
                    ModuleNumber++;
                    if (tractorBeam.IsActive)
                    {
                        if (DebugConfig.DebugTractorBeams || ESCache.Instance.InAbyssalDeadspace)
                            Log.WriteLine("[" + WreckNumber + "][::" + ModuleNumber + "] _ Tractorbeam is: IsActive [" + tractorBeam.IsActive +
                                          "]. Continue");
                        continue;
                    }

                    if (tractorBeam.InLimboState)
                    {
                        DebugTractorBeamInfo(tractorBeam, WreckNumber, ModuleNumber, "ProcessTractorBeams: Activate: InLimboState");
                        continue;
                    }

                    //
                    // this tractor has already been activated at least once
                    //
                    if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(tractorBeam.ItemId))
                        if (Time.Instance.LastActivatedTimeStamp[tractorBeam.ItemId].AddSeconds(2) > DateTime.UtcNow)
                            continue;

                    if (TractorBeams.Any(i => i.IsActive && i.TargetId == wreckToActivateTractorbeam.Id))
                    {
                        DebugTractorBeamInfo(tractorBeam, WreckNumber, ModuleNumber, "ProcessTractorBeams: Activate: TargetId is already the wreck we want");
                        continue;
                    }

                    if (tractorBeam.Activate(wreckToActivateTractorbeam))
                    {
                        tractorsProcessedThisTick++;
                        Log.WriteLine("[" + WreckNumber + "][::" + ModuleNumber + "] Activating tractorbeam [" + ModuleNumber + "] on [" + wreckToActivateTractorbeam.Name +
                                      "][" +
                                      Math.Round(wreckToActivateTractorbeam.Distance / 1000, 0) + "k][" + wreckToActivateTractorbeam.MaskedId + "] IsWreckEmpty [" + wreckToActivateTractorbeam.IsWreckEmpty + "]");
                        Time.Instance.NextTractorBeamAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.SalvageDelayBetweenActions_milliseconds);
                        Time.Instance.LastActivatedTimeStamp[wreckToActivateTractorbeam.Id] = DateTime.UtcNow;
                        break; //we do not need any more tractors on this wreck
                    }
                }

                if (tractorsProcessedThisTick > Settings.Instance.NumberOfModulesToActivateInCycle)
                    return true;

                //
                // move on to the next wreck
                //
            }

            return true;
        }

        private static int tractorsProcessedThisTick = 0;

        public static bool ProcessTractorBeams()
        {
            if (ESCache.Instance.Containers == null) return false;
            if (ESCache.Instance.Containers.Count == 0)
            {
                if (DebugConfig.DebugTractorBeams)
                    Log.WriteLine("ProcessTractorBeams: if (!ESCache.Instance.Containers.Any()), waiting");
                return true;
            }

            if (Time.Instance.NextTractorBeamAction > DateTime.UtcNow)
            {
                if (DebugConfig.DebugTractorBeams)
                    Log.WriteLine("ProcessTractorBeams: Cache.Instance.NextTractorBeamAction is still in the future, waiting");
                return true;
            }

            if (TractorBeams.Count == 0)
                return true;

            if (!ESCache.Instance.InAbyssalDeadspace)
            {
                if (ESCache.Instance.InMission && ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.CapacitorPercentage < TractorBeamMinimumCapacitor)
                {
                    Log.WriteLine("Capacitor [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] below [" +
                                      TractorBeamMinimumCapacitor +
                                      "%] TractorBeamMinimumCapacitor");
                    return true;
                }

                if (ESCache.Instance.InMission && ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.Capacitor < TractorBeams.FirstOrDefault().CapacitorNeed)
                {
                    if (DebugConfig.DebugTractorBeams)
                        Log.WriteLine("Capacitor [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "] below [" +
                                      TractorBeams.FirstOrDefault().CapacitorNeed +
                                      "] TractorBeam Activation Cost");
                    return true;
                }
            }

            tractorsProcessedThisTick = 0;
            ModuleNumber = 0;

            if (!DeactivateTractorBeams()) return false;
            if (!ActivateTractorBeams()) return false;
            return true;
        }

        private static void DebugTractorBeamInfo(ModuleCache tractorBeam, int wreckNumber, int moduleNumber, string logMessage = "")
        {
            if (DebugConfig.DebugTractorBeams)
                Log.WriteLine("[" + wreckNumber + "][::" + moduleNumber + "] " + logMessage + "__ Tractorbeam is: InLimboState [" + tractorBeam.InLimboState +
                              "] IsDeactivating [" + tractorBeam.IsDeactivating + "] IsActivatable [" + tractorBeam.IsActivatable +
                              "] IsOnline [" +
                              tractorBeam.IsOnline + "] TargetId [" + tractorBeam.TargetId +
                              "]. Continue");
        }

        private static bool UnTractorThisWreck(EntityCache wreck, bool currentWreckUnlooted)
        {
            try
            {
                double mySafeScoopRange = Math.Max(0, (int)Distances.ScoopRange - ESCache.Instance.ActiveShip.MaxVelocity);
                if (wreck.Distance <= mySafeScoopRange && Salvagers.Count == 0)
                {
                    if (!currentWreckUnlooted)
                    {
                        if (ESCache.Instance.MyShipEntity != null)
                        {
                            if (ESCache.Instance.MyShipEntity.Velocity < 300)
                                return true;

                            return false;
                        }

                        return false;
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

        #endregion Methods
    }
}