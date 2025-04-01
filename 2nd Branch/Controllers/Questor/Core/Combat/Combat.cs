extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;

namespace EVESharpCore.Questor.Combat
{
    public static partial class Combat
    {
        #region Fields

        private static bool? _doWeCurrentlyHaveProjectilesMounted;
        private static bool? _doWeCurrentlyHaveTurretsMounted;
        private static string DefaultCombatShipName;
        private static string AnomicShipName;

        public static List<EntityCache> HighValueTargetsTargeted
        {
            get
            {
                try
                {
                    if (ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Count > 0)
                    {
                        List<EntityCache> highValueTargetsTargeted = ESCache.Instance.EntitiesOnGrid.Where(t => !t.IsLowValueTarget && (t.IsTarget || t.IsTargeting) && !t.IsWreck && !t.IsContainer).ToList();
                        if (highValueTargetsTargeted != null && highValueTargetsTargeted.Count > 0)
                        {
                            return highValueTargetsTargeted;
                        }

                        return new List<EntityCache>();
                    }

                    return new List<EntityCache>();
                }
                catch (Exception ex)
                {
                   Log.WriteLine("Exception [" + ex + "]");
                   return new List<EntityCache>();
                }
            }
        }

        public static EntityCache LastTargetPrimaryWeaponsWereShooting;

        public static List<EntityCache> LowValueTargetsTargeted
        {
            get
            {
                try
                {
                    if (ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Count > 0)
                    {
                        List<EntityCache> lowValueTargetsTargeted = ESCache.Instance.EntitiesOnGrid.Where(t => t.IsLowValueTargetThatIsTargeted && !t.IsWreck && !t.IsContainer).ToList();
                        if (lowValueTargetsTargeted != null && lowValueTargetsTargeted.Count > 0)
                        {
                            return lowValueTargetsTargeted;
                        }

                        return new List<EntityCache>();
                    }

                    return new List<EntityCache>();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<EntityCache>();
                }
            }
        }

        private static int? maxHighValueTargets;
        private static int? maxLowValueTargets;
        public static long? PreferredPrimaryWeaponTargetID;
        public static string ScanningShipName;
        private static List<EntityCache> _aggressed;
        private static bool _isJammed;
        private static bool _killSentries;

        private static DateTime _lastCombatProcessState;

        private static double? _maxrange { get; set; }
        private static double? _maxWeaponRange;
        private static double? _maxTargetRange;
        private static double? _maxMiningRange;
        private static List<EntityCache> _potentialCombatTargets;
        private static EntityCache _preferredPrimaryWeaponTarget;
        private static List<EntityCache> _primaryWeaponPriorityEntities;
        private static List<PriorityTarget> _primaryWeaponPriorityTargets;
        private static List<PriorityTarget> _primaryWeaponPriorityTargetsPerFrameCaching;
        private static List<EntityCache> _targetedBy { get; set; }
        private static int _weaponNumber;
        private static int icount = 0;

        #endregion Fields

        #region Properties

        public static bool AddDampenersToPrimaryWeaponsPriorityTargetList { get; set; }

        public static bool AddECMsToPrimaryWeaponsPriorityTargetList { get; set; }

        public static bool AddNeutralizersToPrimaryWeaponsPriorityTargetList { get; set; }

        public static bool AddTargetPaintersToPrimaryWeaponsPriorityTargetList { get; set; }

        public static bool AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList { get; set; }

        public static bool AddWarpScramblersToPrimaryWeaponsPriorityTargetList { get; set; }

        public static bool AddWebifiersToPrimaryWeaponsPriorityTargetList { get; set; }

        public static List<EntityCache> Aggressed
        {
            get { return _aggressed ?? (_aggressed = PotentialCombatTargets.Where(e => e.IsAttacking).ToList()); }
        }

        public static void ClearPerPocketCache()
        {
            _doWeCurrentlyHaveProjectilesMounted = null;
            LastTargetPrimaryWeaponsWereShooting = null;
            return;
        }

        public static string CombatShipName
        {
            get
            {
                try
                {
                    if (MissionSettings.MyMission != null)
                    {
                        if (MissionSettings.MyMission.Name.Contains("Anomic"))
                            return AnomicShipName;

                        if (!string.IsNullOrEmpty(MissionSettings.MissionSpecificShipName))
                            return MissionSettings.MissionSpecificShipName;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return DefaultCombatShipName;
            }
        }

        public static int DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage { get; set; }
        private static bool DontShootFrigatesWithSiegeorAutoCannons { get; set; }

        public static bool KillSentries
        {
            get
            {
                if (MissionSettings.MissionKillSentries != null)
                    return (bool)MissionSettings.MissionKillSentries;

                if (ESCache.Instance.InAnomaly && ESCache.Instance.MyShipEntity.IsFrigate)
                    return true;

                if (ESCache.Instance.InAnomaly && ESCache.Instance.MyShipEntity.IsCruiser)
                    return true;

                return _killSentries;
            }
            set => _killSentries = value;
        }

        public static double ListPriorityTargetsEveryXSeconds { get; set; }

        public static int MaximumTargetValueToConsiderTargetALowValueTarget { get; set; }

        public static double MaxRange
        {
            get
            {
                if (_maxrange == null)
                {
                    if (ESCache.Instance.Weapons.Count > 0)
                    {
                        /**
                        if (ESCache.Instance.MyShipEntity.IsFrigate)
                        {
                            _maxrange = MaxTargetRange;
                            return _maxrange ?? 0;
                        }
                        **/

                        if (Drones.DronesKillHighValueTargets)
                        {
                            _maxrange = Math.Min(Math.Max(ESCache.Instance.WeaponRange, Drones.DroneControlRange), ESCache.Instance.ActiveShip.MaxTargetRange);
                            return _maxrange ?? 0;
                        }

                        _maxrange = Math.Min(Math.Min(ESCache.Instance.WeaponRange, MaxTargetRange), ESCache.Instance.ActiveShip.MaxTargetRange);
                        return _maxrange ?? 0;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Nestor)
                    {
                        _maxrange = Math.Min(50000, ESCache.Instance.ActiveShip.MaxTargetRange);
                        return _maxrange ?? 0;
                    }

                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Logistics)
                    {
                        _maxrange = Math.Min(80000, ESCache.Instance.ActiveShip.MaxTargetRange);
                        return _maxrange ?? 0;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Augoror)
                    {
                        _maxrange = Math.Min(65000, ESCache.Instance.ActiveShip.MaxTargetRange);
                        return _maxrange ?? 0;
                    }

                    return _maxrange ?? 0;
                }

                return _maxrange ?? 0;
            }
        }

        public static double MaxWeaponRange
        {
            get
            {
                try
                {
                    if (_maxWeaponRange == null)
                    {
                        if (ESCache.Instance.Weapons.Count > 0)
                        {
                            /**
                            if (ESCache.Instance.MyShipEntity.IsFrigate)
                            {
                                _maxWeaponRange = MaxTargetRange;
                                return _maxWeaponRange ?? 0;
                            }
                            **/

                            _maxWeaponRange = Math.Min(ESCache.Instance.WeaponRange, MaxTargetRange);
                            return _maxWeaponRange ?? 0;
                        }

                        return _maxWeaponRange ?? 0;
                    }

                    return _maxWeaponRange ?? 0;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return ESCache.Instance.WeaponRange;
                }
            }
        }

        public static double MaxMiningRange
        {
            get
            {
                try
                {
                    if (_maxMiningRange == null)
                    {
                        if (ESCache.Instance.MiningEquipment.Count > 0)
                        {
                            /**
                            if (ESCache.Instance.MyShipEntity.IsFrigate)
                            {
                                _maxWeaponRange = MaxTargetRange;
                                return _maxWeaponRange ?? 0;
                            }
                            **/

                            _maxMiningRange = Math.Min(ESCache.Instance.MiningRange, MaxTargetRange);
                            return _maxMiningRange ?? 0;
                        }

                        return _maxMiningRange ?? 0;
                    }

                    return _maxMiningRange ?? 0;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 15000;
                }
            }
        }

        public static double MaxTargetRange
        {
            get
            {
                if (_maxTargetRange == null)
                {
                    _maxTargetRange = Math.Round(ESCache.Instance.ActiveShip.MaxTargetRange, 0);
                    return _maxTargetRange ?? 0;
                }

                return _maxTargetRange ?? 0;
            }
        }

        public static int MinimumAmmoCharges { get; set; }
        public static int MinimumTargetValueToConsiderTargetAHighValueTarget { get; set; }
        public static int NosDistance { get; set; }

        public static List<EntityCache> PotentialCombatTargets
        {
            get
            {
                if (_potentialCombatTargets == null)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        _potentialCombatTargets = ESCache.Instance.EntitiesOnGrid.Where(e => e.IsPotentialCombatTarget).ToList();
                        if (!ESCache.Instance.Paused && DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("PotentialCombatTargets [" + _potentialCombatTargets.Count() + "]");
                        if (_potentialCombatTargets == null || _potentialCombatTargets.Count == 0)
                            _potentialCombatTargets = new List<EntityCache>();

                        return _potentialCombatTargets ?? new List<EntityCache>();
                    }

                    return new List<EntityCache>();
                }

                return _potentialCombatTargets;
            }
        }

        public static EntityCache PreferredPrimaryWeaponTarget
        {
            get
            {
                if (_preferredPrimaryWeaponTarget == null)
                {
                    if (PreferredPrimaryWeaponTargetID != null)
                    {
                        _preferredPrimaryWeaponTarget = ESCache.Instance.EntitiesOnGrid.Find(e => e.Id == PreferredPrimaryWeaponTargetID);

                        return _preferredPrimaryWeaponTarget ?? null;
                    }

                    return null;
                }

                return _preferredPrimaryWeaponTarget;
            }
            set
            {
                if (value == null)
                {
                    if (_preferredPrimaryWeaponTarget != null)
                    {
                        _preferredPrimaryWeaponTarget = null;
                        PreferredPrimaryWeaponTargetID = null;
                        if (DebugConfig.DebugPreferredPrimaryWeaponTarget)
                            Log.WriteLine("[ null ]");
                    }
                }
                else if ((_preferredPrimaryWeaponTarget != null && _preferredPrimaryWeaponTarget.Id != value.Id) || _preferredPrimaryWeaponTarget == null)
                {
                    _preferredPrimaryWeaponTarget = value;
                    PreferredPrimaryWeaponTargetID = value.Id;
                    if (DebugConfig.DebugPreferredPrimaryWeaponTarget)
                        Log.WriteLine(value.Name + " [" + value.MaskedId + "][" + Math.Round(value.Distance / 1000, 0) + "k] isTarget [" +
                                      value.IsTarget + "]");
                }
            }
        }

        public static List<EntityCache> PrimaryWeaponPriorityEntities
        {
            get
            {
                try
                {
                    if (_primaryWeaponPriorityTargets != null)
                    {
                        _primaryWeaponPriorityEntities =
                            PrimaryWeaponPriorityTargets.OrderByDescending(pt => pt.PrimaryWeaponPriority)
                                .ThenBy(pt => pt.Entity.Nearest5kDistance)
                                .Select(pt => pt.Entity)
                                .ToList();
                        return _primaryWeaponPriorityEntities ?? new List<EntityCache>();
                    }

                    if (DebugConfig.DebugAddPrimaryWeaponPriorityTarget)
                        Log.WriteLine("if (_primaryWeaponPriorityTargets.Any()) none available yet");
                    return _primaryWeaponPriorityEntities ?? new List<EntityCache>();
                }
                catch (NullReferenceException)
                {
                    return new List<EntityCache>();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return new List<EntityCache>();
                }
            }
        }

        public static List<PriorityTarget> PrimaryWeaponPriorityTargets
        {
            get
            {
                try
                {
                    if (_primaryWeaponPriorityTargetsPerFrameCaching == null)
                    {
                        if (_primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Count > 0)
                        {
                            foreach (PriorityTarget _primaryWeaponPriorityTarget in _primaryWeaponPriorityTargets)
                                if (ESCache.Instance.EntitiesOnGrid.All(e => e.Id != _primaryWeaponPriorityTarget.EntityID))
                                {
                                    Log.WriteLine("[" + _primaryWeaponPriorityTarget.Name + "] ID[" +
                                                  _primaryWeaponPriorityTarget.MaskedID + "] PriorityLevel [" +
                                                  _primaryWeaponPriorityTarget.PrimaryWeaponPriority + "] is dead / gone");
                                    _primaryWeaponPriorityTargets.Remove(_primaryWeaponPriorityTarget);
                                    break;
                                }

                            _primaryWeaponPriorityTargetsPerFrameCaching = _primaryWeaponPriorityTargets;
                            return _primaryWeaponPriorityTargets;
                        }

                        _primaryWeaponPriorityTargets = new List<PriorityTarget>();
                        _primaryWeaponPriorityTargetsPerFrameCaching = _primaryWeaponPriorityTargets;
                        return _primaryWeaponPriorityTargets;
                    }

                    return _primaryWeaponPriorityTargetsPerFrameCaching;
                }
                catch (NullReferenceException)
                {
                    return new List<PriorityTarget>();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return new List<PriorityTarget>();
                }
            }
            set => _primaryWeaponPriorityTargets = value;
        }

        public static int RemoteRepairDistance { get; set; }

        private static int? _targetedByCount = null;

        public static int TargetedByCount
        {
            get
            {
                if (_targetedByCount != null)
                    return _targetedByCount ?? 0;

                _targetedByCount = TargetedBy.Count;
                return _targetedByCount ?? 0;
            }
        }

        public static List<EntityCache> TargetedBy
        {
            get
            {
                if (_targetedBy != null)
                    return _targetedBy ?? new List<EntityCache>();

                _targetedBy = PotentialCombatTargets.Where(e => e.IsTargetedBy).ToList();
                return _targetedBy ?? new List<EntityCache>();
            }
        }

        public static List<EntityCache> TargetingMe { get; set; }
        private static int MaxCharges { get; set; }

        private static int MaxTotalTargets
        {
            get
            {
                try
                {
                    return maxHighValueTargets + maxLowValueTargets ?? 4;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 2;
                }
            }
        }

        #endregion Properties

        #region Methods

        /**
        public static bool CheckForECMPriorityTargetsInOrder(EntityCache currentTarget, double distance)
        {
            try
            {
                return
                    true && SetPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.WarpScrambler, distance) ||
                    true && SetPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.Jamming, distance) ||
                    true && SetPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.Webbing, distance) ||
                    AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList &&
                    SetPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.TrackingDisrupting, distance) ||
                    AddNeutralizersToPrimaryWeaponsPriorityTargetList &&
                    SetPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.Neutralizing, distance) ||
                    AddTargetPaintersToPrimaryWeaponsPriorityTargetList &&
                    SetPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.TargetPainting, distance) ||
                    AddDampenersToPrimaryWeaponsPriorityTargetList && SetPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.Dampening, distance);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }
        **/

        private static EntityCache _currentWeaponTarget = null;

        public static EntityCache CurrentWeaponTarget()
        {
            try
            {
                if (ESCache.Instance.Weapons.Count == 0) return null;

                if (_currentWeaponTarget != null)
                    return _currentWeaponTarget;

                ModuleCache weapon = ESCache.Instance.Weapons.Find(m => m.IsOnline && !m.IsReloadingAmmo && m.IsActive);
                if (weapon != null)
                {
                    var _tempCWT = ESCache.Instance.EntityById(weapon.TargetId);

                    if (_tempCWT != null && _tempCWT.IsReadyToShoot)
                    {
                        _currentWeaponTarget = _tempCWT;
                        return _currentWeaponTarget;
                    }

                    return null;
                }

                return null;
            }
            catch (Exception exception)
            {
                Log.WriteLine("exception [" + exception + "]");
            }

            return null;
        }

        public static bool DoWeCurrentlyHaveTurretsMounted()
        {
            try
            {
                if (_doWeCurrentlyHaveTurretsMounted == null)
                {
                    foreach (ModuleCache m in ESCache.Instance.Modules)
                        if (m.GroupId == (int)Group.ProjectileWeapon
                            || m.GroupId == (int)Group.EnergyWeapon
                            || m.GroupId == (int)Group.HybridWeapon
                            || m.GroupId == (int)Group.PrecursorWeapon
                        )
                        {
                            _doWeCurrentlyHaveTurretsMounted = true;
                            return _doWeCurrentlyHaveTurretsMounted ?? true;
                        }

                    _doWeCurrentlyHaveTurretsMounted = false;
                    return _doWeCurrentlyHaveTurretsMounted ?? false;
                }

                return _doWeCurrentlyHaveTurretsMounted ?? false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }

            return false;
        }

        public static bool DoWeCurrentlyProjectilesMounted()
        {
            try
            {
                if (_doWeCurrentlyHaveProjectilesMounted == null)
                {
                    foreach (ModuleCache m in ESCache.Instance.Modules)
                        if (m.GroupId == (int)Group.ProjectileWeapon
                        )
                        {
                            _doWeCurrentlyHaveProjectilesMounted = true;
                            return _doWeCurrentlyHaveProjectilesMounted ?? true;
                        }

                    _doWeCurrentlyHaveProjectilesMounted = false;
                    return _doWeCurrentlyHaveProjectilesMounted ?? false;
                }

                return _doWeCurrentlyHaveProjectilesMounted ?? false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }

            return false;
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: Combat");
                DefaultCombatShipName =
                    (string)CharacterSettingsXml.Element("combatShipName") ??
                    (string)CommonSettingsXml.Element("combatShipName") ?? "My frigate of doom";
                Log.WriteLine("Combat: combatShipName [" + DefaultCombatShipName + "]");
                AnomicShipName =
                    (string)CharacterSettingsXml.Element("anomicShipName") ??
                    (string)CommonSettingsXml.Element("anomicShipName") ?? "";
                Log.WriteLine("Combat: anomicShipName [" + AnomicShipName + "]");
                DontShootFrigatesWithSiegeorAutoCannons =
                    (bool?)CharacterSettingsXml.Element("DontShootFrigatesWithSiegeorAutoCannons") ??
                    (bool?)CommonSettingsXml.Element("DontShootFrigatesWithSiegeorAutoCannons") ?? false;
                Log.WriteLine("Combat: DontShootFrigatesWithSiegeorAutoCannons [" + DontShootFrigatesWithSiegeorAutoCannons + "]");
                maxHighValueTargets =
                    (int?)CharacterSettingsXml.Element("maximumHighValueTargets") ??
                    (int?)CommonSettingsXml.Element("maximumHighValueTargets") ?? 2;
                Log.WriteLine("Combat: maximumHighValueTargets [" + maxHighValueTargets + "]");
                maxLowValueTargets =
                    (int?)CharacterSettingsXml.Element("maximumLowValueTargets") ??
                    (int?)CommonSettingsXml.Element("maximumLowValueTargets") ?? 2;
                Log.WriteLine("Combat: maximumLowValueTargets [" + maxLowValueTargets + "]");
                DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage =
                    (int?)CharacterSettingsXml.Element("doNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage") ??
                    (int?)CommonSettingsXml.Element("doNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage") ?? 60;
                Log.WriteLine("Combat: DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage [" + DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage + "]");
                MinimumTargetValueToConsiderTargetAHighValueTarget =
                    (int?)CharacterSettingsXml.Element("minimumTargetValueToConsiderTargetAHighValueTarget") ??
                    (int?)CommonSettingsXml.Element("minimumTargetValueToConsiderTargetAHighValueTarget") ?? 3;
                Log.WriteLine("Combat: minimumTargetValueToConsiderTargetAHighValueTarget [" + MinimumTargetValueToConsiderTargetAHighValueTarget + "]");
                MaximumTargetValueToConsiderTargetALowValueTarget =
                    (int?)CharacterSettingsXml.Element("maximumTargetValueToConsiderTargetALowValueTarget") ??
                    (int?)CommonSettingsXml.Element("maximumTargetValueToConsiderTargetALowValueTarget") ?? 2;
                Log.WriteLine("Combat: maximumTargetValueToConsiderTargetALowValueTarget [" + MaximumTargetValueToConsiderTargetALowValueTarget + "]");
                AddDampenersToPrimaryWeaponsPriorityTargetList =
                    (bool?)CharacterSettingsXml.Element("addDampenersToPrimaryWeaponsPriorityTargetList") ??
                    (bool?)CommonSettingsXml.Element("addDampenersToPrimaryWeaponsPriorityTargetList") ?? true;
                AddECMsToPrimaryWeaponsPriorityTargetList =
                    (bool?)CharacterSettingsXml.Element("addECMsToPrimaryWeaponsPriorityTargetList") ??
                    (bool?)CommonSettingsXml.Element("addECMsToPrimaryWeaponsPriorityTargetList") ?? true;
                AddNeutralizersToPrimaryWeaponsPriorityTargetList =
                    (bool?)CharacterSettingsXml.Element("addNeutralizersToPrimaryWeaponsPriorityTargetList") ??
                    (bool?)CommonSettingsXml.Element("addNeutralizersToPrimaryWeaponsPriorityTargetList") ?? true;
                AddTargetPaintersToPrimaryWeaponsPriorityTargetList =
                    (bool?)CharacterSettingsXml.Element("addTargetPaintersToPrimaryWeaponsPriorityTargetList") ??
                    (bool?)CommonSettingsXml.Element("addTargetPaintersToPrimaryWeaponsPriorityTargetList") ?? true;
                AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList =
                    (bool?)CharacterSettingsXml.Element("addTrackingDisruptorsToPrimaryWeaponsPriorityTargetList") ??
                    (bool?)CommonSettingsXml.Element("addTrackingDisruptorsToPrimaryWeaponsPriorityTargetList") ?? true;
                AddWarpScramblersToPrimaryWeaponsPriorityTargetList =
                    (bool?)CharacterSettingsXml.Element("addWarpScramblersToPrimaryWeaponsPriorityTargetList") ??
                    (bool?)CommonSettingsXml.Element("addWarpScramblersToPrimaryWeaponsPriorityTargetList") ?? true;
                AddWebifiersToPrimaryWeaponsPriorityTargetList =
                    (bool?)CharacterSettingsXml.Element("addWebifiersToPrimaryWeaponsPriorityTargetList") ??
                    (bool?)CommonSettingsXml.Element("addWebifiersToPrimaryWeaponsPriorityTargetList") ?? true;
                ListPriorityTargetsEveryXSeconds =
                    (double?)CharacterSettingsXml.Element("listPriorityTargetsEveryXSeconds") ??
                    (double?)CommonSettingsXml.Element("listPriorityTargetsEveryXSeconds") ?? 300;
                GlobalAllowOverLoadOfWeapons = (bool?)CharacterSettingsXml.Element("overloadWeapons") ??
                                               (bool?)CommonSettingsXml.Element("overloadWeapons") ??
                                               (bool?)CharacterSettingsXml.Element("allowOverloadOfWeapons") ??
                                               (bool?)CommonSettingsXml.Element("allowOverloadOfWeapons") ?? false;
                GlobalAllowOverLoadOfEcm =
                    (bool?)CharacterSettingsXml.Element("allowOverLoadOfEcm") ??
                    (bool?)CommonSettingsXml.Element("allowOverLoadOfEcm") ?? false;
                GlobalAllowOverLoadOfSpeedMod =
                    (bool?)CharacterSettingsXml.Element("allowOverLoadOfSpeedMod") ??
                    (bool?)CommonSettingsXml.Element("allowOverLoadOfSpeedMod") ?? false;
                Log.WriteLine("Combat: allowOverLoadOfSpeedMod [" + GlobalAllowOverLoadOfSpeedMod + "]");
                GlobalAllowOverLoadOfWebs =
                    (bool?)CharacterSettingsXml.Element("allowOverLoadOfWebs") ??
                    (bool?)CommonSettingsXml.Element("allowOverLoadOfWebs") ?? false;
                Log.WriteLine("Combat: allowOverLoadOfWebs [" + GlobalAllowOverLoadOfWebs + "]");
                GlobalWeaponOverloadDamageAllowed =
                    (int?)CharacterSettingsXml.Element("weaponOverloadDamageAllowed") ??
                    (int?)CommonSettingsXml.Element("weaponOverloadDamageAllowed") ?? 70;
                Log.WriteLine("Combat: weaponOverloadDamageAllowed [" + GlobalWeaponOverloadDamageAllowed + "]");
                GlobalEcmOverloadDamageAllowed =
                    (int?)CharacterSettingsXml.Element("ecmOverloadDamageAllowed") ??
                    (int?)CommonSettingsXml.Element("ecmOverloadDamageAllowed") ?? 50;
                GlobalSpeedModOverloadDamageAllowed =
                    (int?)CharacterSettingsXml.Element("speedModOverloadDamageAllowed") ??
                    (int?)CommonSettingsXml.Element("speedModOverloadDamageAllowed") ?? 50;
                Log.WriteLine("Combat: speedModOverloadDamageAllowed [" + GlobalSpeedModOverloadDamageAllowed + "]");
                GlobalWebOverloadDamageAllowed =
                    (int?)CharacterSettingsXml.Element("webOverloadDamageAllowed") ??
                    (int?)CommonSettingsXml.Element("webOverloadDamageAllowed") ?? 50;
                Log.WriteLine("Combat: webOverloadDamageAllowed [" + GlobalWebOverloadDamageAllowed + "]");
                NosDistance =
                    (int?)CharacterSettingsXml.Element("NosDistance") ??
                    (int?)CommonSettingsXml.Element("NosDistance") ?? 38000;
                Log.WriteLine("Combat: NosDistance [" + NosDistance + "]");
                RemoteRepairDistance =
                    (int?)CharacterSettingsXml.Element("remoteRepairDistance") ??
                    (int?)CommonSettingsXml.Element("remoteRepairDistance") ?? 2000;
                MinimumAmmoCharges =
                    (int?)CharacterSettingsXml.Element("minimumAmmoCharges") ??
                    (int?)CommonSettingsXml.Element("minimumAmmoCharges") ?? 1;
                if (MinimumAmmoCharges < 1)
                    MinimumAmmoCharges = 1;
                KillSentries =
                    (bool?)CharacterSettingsXml.Element("killSentries") ??
                    (bool?)CommonSettingsXml.Element("killSentries") ?? false;
                Log.WriteLine("Combat: killSentries [" + KillSentries + "]");
                ScanningShipName =
                    (string)CharacterSettingsXml.Element("scanningShipName") ??
                    (string)CommonSettingsXml.Element("scanningShipName") ?? "cloaky!";
                FocusFireWhenWeaponsAndDronesAreInRangeOfDifficultTargets =
                    (bool?)CharacterSettingsXml.Element("focusFireWhenWeaponsAndDronesAreInRangeOfDifficultTargets") ??
                    (bool?)CommonSettingsXml.Element("focusFireWhenWeaponsAndDronesAreInRangeOfDifficultTargets") ?? true;
                AllowChangingAmmoInWspace =
                    (bool?)CharacterSettingsXml.Element("allowChangingAmmoInWspace") ??
                    (bool?)CommonSettingsXml.Element("allowChangingAmmoInWspace") ?? true;
                Log.WriteLine("Combat: allowChangingAmmoInWspace [" + AllowChangingAmmoInWspace + "]");
                AllowUsingSeigeModules =
                    (bool?)CharacterSettingsXml.Element("allowUsingSeigeModules") ??
                    (bool?)CommonSettingsXml.Element("allowUsingSeigeModules") ?? true;
                    Log.WriteLine("Combat: allowUsingSeigeModules [" + AllowUsingSeigeModules + "]");
                AllowUsingBastionModules =
                    (bool?)CharacterSettingsXml.Element("allowUsingBastionModules") ??
                    (bool?)CommonSettingsXml.Element("allowUsingBastionModules") ?? true;
                    Log.WriteLine("Combat: allowUsingBastionModules [" + AllowUsingBastionModules + "]");
                try
                {
                    MissionSettings.DefaultDamageType =
                    (DamageType)
                    Enum.Parse(typeof(DamageType),
                        (string)CharacterSettingsXml.Element("defaultDamageType") ?? (string)CommonSettingsXml.Element("defaultDamageType") ?? "Explosive",
                        true);
                    Log.WriteLine("defaultDamageType [" + MissionSettings.DefaultDamageType + "]");
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Error Loading Weapon and targeting Settings [" + exception + "]");
            }
        }

        public static bool RemovePrimaryWeaponPriorityTargets(List<EntityCache> targets)
        {
            try
            {
                if (targets.Count > 0 && _primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Count > 0 &&
                    _primaryWeaponPriorityTargets.Any(pt => targets.Any(t => t.Id == pt.EntityID)))
                {
                    _primaryWeaponPriorityTargets.RemoveAll(pt => targets.Any(t => t.Id == pt.EntityID));
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            return false;
        }

        public static void RemovePrimaryWeaponPriorityTargetsByName(string stringEntitiesToRemove)
        {
            try
            {
                IEnumerable<EntityCache> entitiesToRemove = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == stringEntitiesToRemove.ToLower()).ToList();
                if (entitiesToRemove.Any())
                {
                    Log.WriteLine("removing [" + stringEntitiesToRemove + "] from the PWPT List");
                    RemovePrimaryWeaponPriorityTargets(entitiesToRemove.ToList());
                    return;
                }

                Log.WriteLine("[" + stringEntitiesToRemove + "] was not found on grid");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool UnlockHighValueTarget(string reason, bool OutOfRangeOnly = false)
        {
            EntityCache unlockThisHighValueTarget = null;
            long preferredId = PreferredPrimaryWeaponTarget != null ? PreferredPrimaryWeaponTarget.Id : -1;

            if (!OutOfRangeOnly)
            {
                if (LowValueTargetsTargeted.Count > maxLowValueTargets &&
                    MaxTotalTargets <= LowValueTargetsTargeted.Count + HighValueTargetsTargeted.Count)
                    return UnlockLowValueTarget(reason, OutOfRangeOnly);

                try
                {
                    if (HighValueTargetsTargeted.Count(t => t.Id != preferredId) >= maxHighValueTargets)
                        unlockThisHighValueTarget = HighValueTargetsTargeted.Where(h => (h.IsTarget && h.IsIgnored)
                                                                                        ||
                                                                                        (h.IsTarget && !h.IsPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                        !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                        !h.IsWarpScramblingMe && !h.IsInOptimalRange &&
                                                                                        PotentialCombatTargets.Count >= 3)
                                                                                        ||
                                                                                        (h.IsTarget && !h.IsPreferredPrimaryWeaponTarget &&
                                                                                        !h.IsDronePriorityTarget && h.IsHigherPriorityPresent &&
                                                                                        !h.IsPrimaryWeaponPriorityTarget &&
                                                                                        HighValueTargetsTargeted.Count == maxHighValueTargets &&
                                                                                        !h.IsWarpScramblingMe)
                                                                                        ||
                                                                                        (h.IsTarget && !h.IsPreferredPrimaryWeaponTarget &&
                                                                                        !h.IsDronePriorityTarget && !h.IsInOptimalRange &&
                                                                                        !h.IsPrimaryWeaponPriorityTarget &&
                                                                                        HighValueTargetsTargeted.Count == maxHighValueTargets &&
                                                                                        !h.IsWarpScramblingMe &&
                                                                                        h.IsAnyOtherUnTargetedHighValueTargetInOptimal)
                                                                                        ||
                                                                                        (h.IsTarget && h.Distance > MaxRange))
                            .OrderByDescending(t => t.Distance > MaxRange)
                            .ThenByDescending(t => t.Nearest5kDistance)
                            .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                    //ignore this exception
                }
            }
            else
            {
                try
                {
                    unlockThisHighValueTarget = HighValueTargetsTargeted.Where(t => t.Distance > MaxRange)
                        .Where(h => (h.IsTarget && h.IsIgnored && !h.IsWarpScramblingMe) || (h.IsTarget && !h.IsPreferredDroneTarget &&
                                    !h.IsDronePriorityTarget && !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                    !h.IsWarpScramblingMe)
                                    ||
                                    (h.IsTarget && !h.IsPreferredPrimaryWeaponTarget &&
                                    !h.IsDronePriorityTarget && !h.IsInOptimalRange &&
                                    !h.IsPrimaryWeaponPriorityTarget &&
                                    HighValueTargetsTargeted.Count == maxHighValueTargets &&
                                    !h.IsWarpScramblingMe &&
                                    h.IsAnyOtherUnTargetedHighValueTargetInOptimal)
                        )
                        .OrderByDescending(t => t.Nearest5kDistance)
                        .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                    //ignore this exception
                }
            }

            if (unlockThisHighValueTarget != null)
            {
                Log.WriteLine("Unlocking HighValue " + unlockThisHighValueTarget.Name + "[" + Math.Round(unlockThisHighValueTarget.Distance / 1000, 0) +
                              "k] myTargtingRange:[" + MaxTargetRange + "] myWeaponRange[:" + ESCache.Instance.WeaponRange + "] to make room for [" +
                              reason + "]");
                if (unlockThisHighValueTarget.UnlockTarget()) return false;
                return true;
            }

            return true;
        }

        public static bool FocusFireWhenWeaponsAndDronesAreInRangeOfDifficultTargets = true;
        public static bool AllowChangingAmmoInWspace = true;
        public static bool AllowUsingSeigeModules = false;
        public static bool AllowUsingBastionModules = false;

        private static bool UnlockLowValueTarget(string reason, bool OutOfWeaponsRange = false)
        {
            EntityCache unlockThisLowValueTarget = null;
            if (!OutOfWeaponsRange)
            {
                try
                {
                    unlockThisLowValueTarget = LowValueTargetsTargeted.Where(h => (h.IsTarget && h.IsIgnored)
                                                                                  ||
                                                                                  (h.IsTarget && !h.IsPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                  !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                  !h.IsWarpScramblingMe && !h.IsInOptimalRange &&
                                                                                  PotentialCombatTargets.Count >= 3)
                                                                                  ||
                                                                                  (h.IsTarget && !h.IsPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                  !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                  !h.IsWarpScramblingMe && LowValueTargetsTargeted.Count ==
                                                                                  maxLowValueTargets)
                                                                                  ||
                                                                                  (h.IsTarget && !h.IsPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                  !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                  h.IsHigherPriorityPresent && !h.IsWarpScramblingMe &&
                                                                                  LowValueTargetsTargeted.Count == maxLowValueTargets)
                                                                                  ||
                                                                                  (h.IsTarget && h.Distance > MaxRange))
                        .OrderByDescending(t => t.Distance < (Drones.UseDrones ? Drones.MaxDroneRange : MaxRange))
                        .ThenByDescending(t => t.Nearest5kDistance)
                        .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                    //ignore this exception
                }
            }
            else
            {
                try
                {
                    unlockThisLowValueTarget = LowValueTargetsTargeted.Where(t => t.Distance > MaxTargetRange)
                        .Where(h => h.IsTarget)
                        .OrderByDescending(t => t.Nearest5kDistance)
                        .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                    //ignore this exception
                }
            }

            if (unlockThisLowValueTarget != null)
            {
                Log.WriteLine("Unlocking LowValue " + unlockThisLowValueTarget.Name + "[" + Math.Round(unlockThisLowValueTarget.Distance / 1000, 0) +
                              "k] myTargtingRange:[" +
                              MaxTargetRange + "] myWeaponRange[:" + ESCache.Instance.WeaponRange + "] to make room for [" + reason + "]");
                if (unlockThisLowValueTarget.UnlockTarget()) return false;
                return true;
            }

            return true;
        }

        #endregion Methods
    }
}