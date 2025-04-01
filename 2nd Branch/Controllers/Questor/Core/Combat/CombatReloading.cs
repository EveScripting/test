extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;

namespace EVESharpCore.Questor.Combat
{
    public static partial class Combat
    {
        #region Fields

        private static int _findAmmoAttempts;

        private static DateTime _lastFindAmmoAttempt = DateTime.UtcNow.AddHours(-1);

        private static DateTime _lastReloadAllIteration = DateTime.UtcNow;

        private static DateTime _lastReloadIteration = DateTime.UtcNow;

        private static EntityCache _entityToUseForAmmo { get; set; } = null;

        public static EntityCache EntityToUseForAmmo
        {
            get
            {
                try
                {
                    if (_entityToUseForAmmo != null)
                        return _entityToUseForAmmo;

                    if (KillTarget != null)
                    {
                        _entityToUseForAmmo = KillTarget;
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForAmmo [" + _entityToUseForAmmo.Name + "][" + _entityToUseForAmmo.Nearest1KDistance + "k]");
                        return _entityToUseForAmmo ?? null;
                    }

                    if (PotentialCombatTargets.Any(i => i.IsOnGridWithMe))
                    {
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForAmmo if (Combat.Combat.PotentialCombatTargets.Any())");
                        _entityToUseForAmmo = PotentialCombatTargets.Where(i => i.IsOnGridWithMe).OrderBy(i => i.IsCurrentTarget).ThenBy(i => i.Distance).FirstOrDefault();
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForAmmo [" + _entityToUseForAmmo.Name + "][" + _entityToUseForAmmo.Nearest1KDistance + "]");
                        return _entityToUseForAmmo ?? null;
                    }

                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForRangeCalc: KillTarget == null and !PotentialCombatTargets.Any() - using my Ship (0k!)");
                    _entityToUseForAmmo = ESCache.Instance.MyShipEntity;
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForAmmo [" + _entityToUseForAmmo.Name + "][" + _entityToUseForAmmo.Nearest1KDistance + "]");
                    return _entityToUseForAmmo ?? null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    _entityToUseForAmmo = ESCache.Instance.MyShipEntity;
                    return _entityToUseForAmmo ?? null;
                }
            }
        }

        #endregion Fields

        #region Properties

        public static bool OutOfAmmo
        {
            get
            {
                if (ESCache.Instance.ActiveShip == null)
                    return false;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Shuttle)
                    return false;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Industrial)
                    return false;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.TransportShip)
                    return false;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                    return false;

                if (ESCache.Instance.Weapons.Count == 0)
                    return false;

                if (ESCache.Instance.Weapons.All(i => i.TypeId == (int)TypeID.CivilianGatlingAutocannon))
                    return false;

                if (ESCache.Instance.Weapons.All(i => i.TypeId == (int)TypeID.CivilianGatlingPulseLaser))
                    return false;

                if (ESCache.Instance.Weapons.All(i => i.TypeId == (int)TypeID.CivilianGatlingRailgun))
                    return false;

                if (ESCache.Instance.Weapons.All(i => i.TypeId == (int)TypeID.CivilianLightElectronBlaster))
                    return false;

                if (ESCache.Instance.CurrentShipsCargo == null)
                    return false;

                if (ESCache.Instance.CurrentShipsCargo.Items == null)
                    return false;

                if (DirectUIModule.DefinedAmmoTypes.Count > 0)
                {
                    if (ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
                    {
                        foreach (AmmoType IndividualAmmo in DirectUIModule.DefinedAmmoTypes)
                        {
                            if (ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == IndividualAmmo.TypeId).Sum(i => i.Quantity) > 50)
                                return false;
                        }

                        return true;
                    }

                    return true;
                }

                return false;
            }
        }

        private static List<DirectItem> _AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance = null;

        public static List<DirectItem> AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance(int AmmoRangeNeeded, DamageType neededDamageType, bool ShortestDistanceAmmo = false)
        {
            if (_AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance != null)
                return _AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance;

            var tempListOfAmmoInCargo = AmmoInCargoThatCanReachThisSpecificDistance(AmmoRangeNeeded, ShortestDistanceAmmo);

            if (tempListOfAmmoInCargo != null && tempListOfAmmoInCargo.Any())
            {
                _AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance = new List<DirectItem>();
                foreach (DirectItem individualAmmo in tempListOfAmmoInCargo.Where(x => x.DefinedAsAmmoType.DamageType >= neededDamageType))
                {
                    _AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance.Add(individualAmmo);
                }

                return _AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance ?? new List<DirectItem>();
            }

            return new List<DirectItem>();
        }

        private static List<DirectItem> _AmmoInCargoThatCanReachThisSpecificDistance = null;

        public static List<DirectItem> AmmoInCargoThatCanReachThisSpecificDistance(double AmmoRangeNeeded, bool ShortestDistanceAmmo)
        {
            if (_AmmoInCargoThatCanReachThisSpecificDistance != null)
                return _AmmoInCargoThatCanReachThisSpecificDistance;

            if (UsableAmmoInCargo.Any())
            {
                _AmmoInCargoThatCanReachThisSpecificDistance = new List<DirectItem>();

                if (ShortestDistanceAmmo)
                {
                    //
                    // Shortest Range Ammo
                    //
                    int definedAmmoShortestRangeAvaialble = UsableAmmoInCargo.OrderBy(x => x.DefinedAsAmmoType.Range).FirstOrDefault().DefinedAsAmmoType.Range;
                    foreach (DirectItem individualAmmo in UsableAmmoInCargo.OrderBy(x => definedAmmoShortestRangeAvaialble >= x.DefinedAsAmmoType.Range))
                    {
                        _AmmoInCargoThatCanReachThisSpecificDistance.Add(individualAmmo);
                    }

                    return _AmmoInCargoThatCanReachThisSpecificDistance ?? new List<DirectItem>();
                }

                //
                // Ammo that can hit at this range
                //
                bool WeHaveAmmoThatCanHitThat = UsableAmmoInCargo.Count(x => x.DefinedAsAmmoType.Range >= AmmoRangeNeeded) > 0;
                if (WeHaveAmmoThatCanHitThat)
                {
                    foreach (DirectItem individualAmmo in UsableAmmoInCargo.Where(x => x.DefinedAsAmmoType.Range >= AmmoRangeNeeded))
                    {
                        _AmmoInCargoThatCanReachThisSpecificDistance.Add(individualAmmo);
                    }

                    return _AmmoInCargoThatCanReachThisSpecificDistance ?? new List<DirectItem>();
                }

                int definedAmmoMaxRangeAvaialble = UsableAmmoInCargo.OrderByDescending(x => x.DefinedAsAmmoType.Range).FirstOrDefault().DefinedAsAmmoType.Range;
                foreach (DirectItem individualAmmo in UsableAmmoInCargo.Where(i => i.DefinedAsAmmoType.Range >= definedAmmoMaxRangeAvaialble))
                {
                    _AmmoInCargoThatCanReachThisSpecificDistance.Add(individualAmmo);
                }

                return _AmmoInCargoThatCanReachThisSpecificDistance ?? new List<DirectItem>();
            }

            return new List<DirectItem>();
        }

        private static List<DirectItem> _usableAmmoInCargo { get; set; } = null;

        public static List<DirectItem> UsableAmmoInCargo
        {
            get
            {
                try
                {
                    if (_usableAmmoInCargo == null)
                    {
                        if (DebugConfig.DebugAmmoManagement && DirectEve.Interval(30000))
                        {
                            Log.WriteLine("Items in Cargo: start");
                            foreach (var thisItem in ESCache.Instance.CurrentShipsCargo.Items)
                            {
                                Log.WriteLine("thisItem [" + thisItem.TypeName + "][" + thisItem.TypeId + "]");
                            }
                            Log.WriteLine("Items in Cargo: done");

                            foreach (var thisDefinedAmmoType in DirectUIModule.DefinedAmmoTypes.Where(x => !x.OnlyUseAsOverrideAmmo))
                            {
                                Log.WriteLine("thisDefinedAmmoType [" + thisDefinedAmmoType.Description + "][" + thisDefinedAmmoType.TypeId + "][" + thisDefinedAmmoType.DamageType + "] Range [" + thisDefinedAmmoType.Range + "]");
                            }
                        }

                        _usableAmmoInCargo = ESCache.Instance.CurrentShipsCargo.Items.Where(i => DirectUIModule.DefinedAmmoTypes.Where(x => !x.OnlyUseAsOverrideAmmo).Any(j => j.TypeId == i.TypeId)).OrderBy(x => x.OptimalRange).Distinct().ToList();
                        if (ESCache.Instance.InAbyssalDeadspace)
                        {
                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikVilaFrigateSwarmerSpawn)
                            {
                                if (ESCache.Instance.Weapons.Any(i => i.IsMissileLauncher && !i._module.IsVortonProjector))
                                {
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                                    {
                                        //dont modify usable ammo in cargo: return all ammo
                                        return _usableAmmoInCargo ?? new List<DirectItem>();
                                    }

                                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.Contains("Vedmak")))
                                    {
                                        //dont modify usable ammo in cargo: return all ammo
                                        return _usableAmmoInCargo ?? new List<DirectItem>();
                                    }

                                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 4 || PotentialCombatTargets.All(i => i.IsNPCFrigate))
                                    {
                                        if (_usableAmmoInCargo.Any(i => i.TypeName.ToLower().Contains(" fury ".ToLower())) && _usableAmmoInCargo.Any(x => !x.TypeName.ToLower().Contains(" fury ".ToLower())))
                                        {
                                            //we have both fury and non-fury ammo and we are fighting frigates, we should use non-fury ammo!
                                            if (_usableAmmoInCargo.Any(x => !x.TypeName.ToLower().Contains(" fury ".ToLower())))
                                            {
                                                _usableAmmoInCargo = _usableAmmoInCargo.Where(x => !x.TypeName.ToLower().Contains(" fury ".ToLower())).Distinct().ToList();

                                                if (DebugConfig.DebugAmmoManagement)
                                                {
                                                    Log.WriteLine("_usableAmmoInCargo: start");
                                                    foreach (var thisItem in _usableAmmoInCargo)
                                                    {
                                                        Log.WriteLine("thisItem [" + thisItem.TypeName + "][" + thisItem.TypeId + "]");
                                                    }
                                                    Log.WriteLine("_usableAmmoInCargo: done");
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                            {
                                if (ESCache.Instance.Weapons.Any(i => i.IsMissileLauncher && !i._module.IsVortonProjector))
                                {
                                    if (_usableAmmoInCargo.Any(i => i.TypeName.ToLower().Contains(" fury ".ToLower())) && _usableAmmoInCargo.Any(x => !x.TypeName.ToLower().Contains(" fury ".ToLower())))
                                    {
                                        if (_usableAmmoInCargo.Any(x => !x.TypeName.ToLower().Contains(" fury ".ToLower())))
                                        {
                                            //we have both fury and non-fury ammo and we are fighting frigates, we should use non-fury ammo!
                                            _usableAmmoInCargo = _usableAmmoInCargo.Where(x => !x.TypeName.ToLower().Contains(" fury ".ToLower())).Distinct().ToList();
                                        }
                                    }
                                }
                            }
                        }
                        return _usableAmmoInCargo ?? new List<DirectItem>();
                    }

                    return _usableAmmoInCargo ?? new List<DirectItem>();
                }
                catch (Exception)
                {
                    return new List<DirectItem>();
                }
            }
        }

        private static List<DirectItem> _usableSpecialAmmoInCargo = null;

        public static List<DirectItem> UsableSpecialAmmoInCargo
        {
            get
            {
                try
                {
                    if (_usableSpecialAmmoInCargo == null)
                    {
                        if (DebugConfig.DebugAmmoManagement)
                        {
                            Log.WriteLine("Items in Cargo: start");
                            foreach (var thisItem in ESCache.Instance.CurrentShipsCargo.Items)
                            {
                                Log.WriteLine("thisItem [" + thisItem.TypeName + "][" + thisItem.TypeId + "]");
                            }
                            Log.WriteLine("Items in Cargo: done");

                            foreach (var thisDefinedAmmoType in DirectUIModule.DefinedAmmoTypes)
                            {
                                Log.WriteLine("thisDefinedAmmoType [" + thisDefinedAmmoType.Description + "][" + thisDefinedAmmoType.TypeId + "][" + thisDefinedAmmoType.DamageType + "] Range [" + thisDefinedAmmoType.Range + "]");
                            }
                        }

                        _usableSpecialAmmoInCargo = ESCache.Instance.CurrentShipsCargo.Items.Where(i => DirectUIModule.DefinedAmmoTypes.Any(j => j.OverrideTargetName != "NPCNameNeedsToBePartOfAmmoDefintionIfYouWantToUseThis" && j.TypeId == i.TypeId)).OrderBy(x => x.OptimalRange).ToList();
                        return _usableSpecialAmmoInCargo ?? new List<DirectItem>();
                    }

                    return _usableSpecialAmmoInCargo ?? new List<DirectItem>();
                }
                catch (Exception)
                {
                    return new List<DirectItem>();
                }
            }
        }

        public static List<AmmoType> CorrectAmmoTypesInCargoByRange
        {
            get
            {
                try
                {
                    if (CorrectAmmoTypesToUseByRange == null || CorrectAmmoTypesToUseByRange.Count == 0)
                    {
                        if (DebugConfig.DebugCorrectAmmoTypeInCargo) Log.WriteLine("correctAmmoTypeInCargo: if (correctAmmoTypeToUse == null || !correctAmmoTypeToUse.Any())");
                        return new List<AmmoType>();
                    }

                    if (ESCache.Instance.CurrentShipsCargo != null)
                    {
                        //
                        // Look for the correct ammo for this mission
                        //

                        if (ESCache.Instance.CurrentShipsCargo == null)
                            return new List<AmmoType>();

                        if (ESCache.Instance.CurrentShipsCargo.Items == null)
                            return new List<AmmoType>();

                        List<AmmoType> tempCorrectAmmoTypesInCargo;
                        List<DirectItem> MatchingItems = null;
                        try
                        {
                            MatchingItems = UsableAmmoInCargo.Where(j => j.ThisAmmoIsCorrectDamageTypeAmmoForKillTarget && j.AmmoType.Range > EntityToUseForAmmo.Distance).OrderBy(x => x.AmmoType.Range).ToList();
                        }
                        catch (Exception)
                        {
                            //ignore this exception
                        }

                        if (MatchingItems != null)
                            tempCorrectAmmoTypesInCargo = CorrectAmmoTypesToUseByRange.Where(a => MatchingItems.Any(x => x.TypeId == a.TypeId)).OrderBy(i => i.Range).ToList();
                        else
                            tempCorrectAmmoTypesInCargo = CorrectAmmoTypesToUseByRange.ToList();

                        if (tempCorrectAmmoTypesInCargo.Count > 0)
                            return tempCorrectAmmoTypesInCargo;

                        //
                        // Look for any valid ammo in our cargo
                        //
                        tempCorrectAmmoTypesInCargo = DirectUIModule.DefinedAmmoTypes.AsEnumerable().OrderByDescending(a => ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.ToList().Any(c => c.TypeId == a.TypeId && c.Quantity >= MinimumAmmoCharges)).ToList();
                        if (tempCorrectAmmoTypesInCargo.Count > 0)
                        {
                            int num = 0;
                            foreach (var tempCorrectAmmoTypeInCargo in tempCorrectAmmoTypesInCargo)
                            {
                                num++;
                                if (DebugConfig.DebugCorrectAmmoTypeInCargo) Log.WriteLine("CorrectAmmoTypeInCargo: [" + num + "][" + tempCorrectAmmoTypeInCargo.Description + "][" + tempCorrectAmmoTypeInCargo.TypeId + "][" + tempCorrectAmmoTypeInCargo.DamageType + "][" + tempCorrectAmmoTypeInCargo.Range + "]");
                            }
                            return tempCorrectAmmoTypesInCargo;
                        }
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        private static List<AmmoType> CorrectAmmoTypesToUseByRange
        {
            get
            {
                try
                {
                    if (DirectUIModule.DefinedAmmoTypes.Count == 0)
                    {
                        if (DebugConfig.DebugReloadAll) Log.WriteLine("correctAmmoTypeToUse: if (!DefinedAmmoTypes.Any())");
                        return new List<AmmoType>();
                    }

                    double targetDistance = 10000;
                    string targetName = "unknown";
                    if (EntityToUseForAmmo != null)
                    {
                        targetDistance = Math.Round(EntityToUseForAmmo.Distance/ 1000, 0);
                        targetName = EntityToUseForAmmo.Name;
                    }

                    List<AmmoType> tempCorrectAmmoTypesToUse = DirectUIModule.DefinedAmmoTypes.Where(a => a.Range > targetDistance).OrderByDescending(a => MissionSettings.CurrentDamageType != null && a.DamageType == MissionSettings.CurrentDamageType).ThenBy(a => a.Range).ToList();
                    if (tempCorrectAmmoTypesToUse.Count > 0)
                    {
                        int num = 0;
                        foreach (var tempCorrectAmmoTypeToUse in tempCorrectAmmoTypesToUse)
                        {
                            num++;
                            if (DebugConfig.DebugCorrectAmmoTypeToUse) Log.WriteLine("CorrectAmmoTypeToUse [" + num + "][" + tempCorrectAmmoTypeToUse.Description + "][" + tempCorrectAmmoTypeToUse.TypeId + "][" + tempCorrectAmmoTypeToUse.DamageType + "] MaxRange [" + tempCorrectAmmoTypeToUse.Range + "] targetName [" + targetName + "] targetDistance [" + targetDistance + "k away]");
                        }

                        return tempCorrectAmmoTypesToUse;
                    }

                    if (DebugConfig.DebugCorrectAmmoTypeToUse) Log.WriteLine("Error. [ DefinedAmmoTypes.Where(a => a.DamageType == MissionSettings.CurrentDamageType).ToList(); ] resulted in null");
                    return new List<AmmoType>();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<AmmoType>();
                }
            }
        }

        public static AmmoType CorrectAmmoTypeToUseByRange
        {
            get
            {
                try
                {
                    AmmoType tempAmmo;
                    if (CorrectAmmoTypesInCargoByRange == null || CorrectAmmoTypesInCargoByRange.Count == 0)
                    {
                        Log.WriteLine("correctAmmoTypeToUseByRange: if (correctAmmoTypeInCargo == null || !correctAmmoTypeInCargo.Any())");
                        return null;
                    }

                    if (EntityToUseForAmmo != null)
                    {
                        tempAmmo = CorrectAmmoTypesInCargoByRange.Where(a => a.Range >= EntityToUseForAmmo.Distance).OrderBy(a => a.Range).FirstOrDefault();
                        if (tempAmmo != null)
                        {
                            if (DebugConfig.DebugCorrectAmmoTypeToUseByRange) Log.WriteLine("correctAmmoTypeToUseByRange [" + tempAmmo.Description + "][" + tempAmmo.TypeId + "][" + tempAmmo.DamageType + "] MaxRange [" + tempAmmo.Range + "][" + EntityToUseForAmmo.Name + "][" + Math.Round(EntityToUseForAmmo.Distance/1000, 0) + "k away]");
                            return tempAmmo;
                    	}

                        //
                        // retry any others that might be too short of range?
                        //
                        tempAmmo = CorrectAmmoTypesInCargoByRange.OrderBy(a => a.Range >= EntityToUseForAmmo.Distance).ThenBy(a => a.Range).FirstOrDefault();
                        if (tempAmmo != null)
                        {
                            if (DebugConfig.DebugCorrectAmmoTypeToUseByRange) Log.WriteLine("! correctAmmoTypeToUseByRange [" + tempAmmo.Description + "][" + tempAmmo.TypeId + "][" + tempAmmo.DamageType + "] MaxRange [" + tempAmmo.Range + "][" + EntityToUseForAmmo.Name + "][" + Math.Round(EntityToUseForAmmo.Distance / 1000, 0) + "k away] !");
                            return tempAmmo;
                        }

                        return null;
                    }

                    tempAmmo = CorrectAmmoTypesInCargoByRange.OrderBy(a => a.Range).FirstOrDefault();
                    if (tempAmmo != null)
                    {
                        Log.WriteLine("correctAmmoTypeToUseByRange: usingFirstAmmoFoundInCargo! [" + tempAmmo.Description + "][" + tempAmmo.TypeId + "][" + tempAmmo.DamageType + "] MaxRange [" + tempAmmo.Range + "][" + EntityToUseForAmmo.Name + "][" + Math.Round(EntityToUseForAmmo.Distance / 1000, 0) + "k away]");
                        return tempAmmo;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        #endregion Properties

        #region Methods

        //
        // this will change ammo as needed, unlike the in game reload command
        //
        public static bool ReloadAll()
        {
            if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon) || !Combat.UsableAmmoInCargo.Any())
            {
                //TryingToChangeOrReloadAmmo = false;
                return true;
            }

            if (DirectEve.Interval(30000)) Log.WriteLine("ReloadAll()");
            AmmoManagementBehavior.ChangeAmmoManagementBehaviorState(States.AmmoManagementBehaviorState.Monitor);
            return true;

            if (ESCache.Instance.Weapons.Count > 0)
            {
                foreach (ModuleCache weapon in ESCache.Instance.Weapons)
                {
                    if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
                    {

                        if (KillTarget != null && KillTarget._directEntity.IsNPCWormHoleSpaceDrifter && (ESCache.Instance.ActiveShip.IsDread || ESCache.Instance.ActiveShip.IsMarauder || ESCache.Instance.ActiveShip.IsBattleship))
                        {
                            if (DebugConfig.DebugReloadAll) Log.WriteLine("ReloadAll: if (ESCache.Instance.ActiveShip.IsDread || ESCache.Instance.ActiveShip.IsMarauder)");
                            if (ESCache.Instance.InWarp)
                            {
                                Log.WriteLine("ReloadAll: if (ESCache.Instance.InWarp)");
                                ReloadAmmo(weapon, _weaponNumber);
                                return true;
                            }
                        }

                        if (DebugConfig.DebugReloadAll) Log.WriteLine("ReloadAll: 1");
                        ReloadAmmo(weapon, _weaponNumber);
                        return true;
                    }
                }

                if (DebugConfig.DebugReloadAll) Log.WriteLine("ReloadAll: Reached the end");
                return true;
            }

            return true;
        }


        public static bool ReloadAmmo(ModuleCache weapon, int weaponNumber, bool force = false)
        {
            if (weapon.IsEnergyWeapon)
            {
                ReloadEnergyWeaponAmmo(weapon, weaponNumber);
                return true;
            }

            ReloadNormalAmmo(weapon, weaponNumber, ChargeToLoadIntoWeapon(CorrectAmmoTypeToUseByRange), force);
            return true;
        }

        public static bool ReloadThisSpecialAmmo(ModuleCache weapon, int weaponNumber, bool force = false)
        {
            try
            {
                var thisOverdideAmmoTypeNeeded = DirectUIModule.DefinedAmmoTypes.Where(x => x.OverrideTargetName != "NPCNameNeedsToBePartOfAmmoDefintionIfYouWantToUseThis").FirstOrDefault(i => ESCache.Instance.Targets.Any(x => x.Name.ToLower().Contains(i.OverrideTargetName.ToLower())) && Combat.KillTarget != null && Combat.KillTarget.Name.ToLower().Contains(i.OverrideTargetName.ToLower()));
                if (thisOverdideAmmoTypeNeeded != null)
                {
                    var thisAmmoInCargo = UsableSpecialAmmoInCargo.FirstOrDefault(i => i.TypeId == thisOverdideAmmoTypeNeeded.TypeId);
                    if (thisAmmoInCargo != null)
                    {
                        if (weapon.IsEnergyWeapon)
                        {
                            ReloadEnergyWeaponAmmo(weapon, weaponNumber);
                            return true;
                        }

                        ReloadNormalAmmo(weapon, weaponNumber, thisAmmoInCargo, force);
                        return true;
                    }
                }
                //
                // if we dont have any Override ammo left use regular ammo
                //
                ReloadAmmo(weapon, weaponNumber, force);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool? AreWeOutOfAmmo(EntityCache entity)
        {
            if (MissionSettings.MyMission != null && MissionSettings.MyMission.Name.Contains("Anomic"))
                return false;

            if (DateTime.UtcNow > _lastFindAmmoAttempt.AddSeconds(30))
                _findAmmoAttempts = 0;

            _lastFindAmmoAttempt = DateTime.UtcNow;
            _findAmmoAttempts++;

            if (_findAmmoAttempts > 40)
            {
                ChangeCombatState(CombatState.OutOfAmmo, "if (_findAmmoAttempts > 40)");
                return true;
            }

            if (DirectUIModule.DefinedAmmoTypes.Any(a => a.DamageType == MissionSettings.CurrentDamageType))
            {
                if (CorrectAmmoTypesToUseByRange == null)
                {
                    Log.WriteLine("ReloadNormalAmmo:: if (correctAmmoToUse == null)");
                    return true;
                }

                if (CorrectAmmoTypesToUseByRange != null && CorrectAmmoTypesToUseByRange.Count == 0)
                {
                    Log.WriteLine("ReloadNormalAmmo:: correctAmmoToUse was empty. MissionSettings.CurrentDamageType [" + MissionSettings.CurrentDamageType + "]");
                    foreach (AmmoType thisAmmo in DirectUIModule.DefinedAmmoTypes)
                        Log.WriteLine("ReloadNormalAmmo:: AmmoType: [" + thisAmmo.Description + "] TypeId [" + thisAmmo.TypeId + "] DamageType [" + thisAmmo.DamageType + "] Quantity [" + thisAmmo.Quantity + "] Range [" + thisAmmo.Range + "]");
                }

                if (CorrectAmmoTypesToUseByRange != null && CorrectAmmoTypesToUseByRange.Count > 0)
                {
                    if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Items == null)
                    {
                        Log.WriteLine("ReloadNormalAmmo:: CurrentShipsCargo == null");
                        return null;
                    }

                    if (ESCache.Instance.CurrentShipsCargo.Items == null)
                    {
                        Log.WriteLine("ReloadNormalAmmo:: CurrentShipsCargo.Items == null");
                        return null;
                    }

                    if (ESCache.Instance.CurrentShipsCargo.Items.Count == 0)
                    {
                        Log.WriteLine("ReloadNormalAmmo:: !CurrentShipsCargo.Items.Any()");
                        return true;
                    }

                    if (CorrectAmmoTypesInCargoByRange == null)
                    {
                        Log.WriteLine("ReloadNormalAmmo:: if (correctAmmoInCargo == null)");
                        return true;
                    }

                    if (CorrectAmmoTypesInCargoByRange != null && CorrectAmmoTypesInCargoByRange.Count == 0)
                    {
                        foreach (AmmoType individualCorrectAmmoType in CorrectAmmoTypesToUseByRange)
                            Log.WriteLine("ReloadNormalAmmo:: missing individualCorrectAmmoType [" + individualCorrectAmmoType.Description + "][" + individualCorrectAmmoType.DamageType + "] Quantity [" + individualCorrectAmmoType.Quantity + "] MinimumAmmoCharges [" + MinimumAmmoCharges + "]");

                        if (MissionSettings.AnyAmmoOfTypeLeft(MissionSettings.CurrentDamageType))
                        {
                            Log.WriteLine($"No charges left in ships cargo, using the remaining charges in the launchers before swapping to the second best damage type.");
                            return true;
                        }

                        Log.WriteLine("ReloadNormalAmmo:: not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" +
                                      MinimumAmmoCharges +
                                      "] Note: CurrentDamageType [" + MissionSettings.CurrentDamageType + "]");

                        int itemnum = 0;
                        foreach (DirectItem ammoItem in ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.CategoryId == (int)CategoryID.Charge))
                        {
                            itemnum++;
                            Log.WriteLine("[" + itemnum + "] CargoHoldItem: [" + ammoItem.TypeName + "] TypeId [" + ammoItem.TypeId + "] Quantity [" + ammoItem.Quantity + "]");
                        }

                        ChangeCombatState(CombatState.OutOfAmmo, "not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold");
                        return true;
                    }
                }
            }
            else if (ESCache.Instance.CurrentShipsCargo == null)
            {
                ChangeCombatState(CombatState.OutOfAmmo, "if Cache.Instance.CurrentShipsCargo == null");
                return true;
            }

            if (CorrectAmmoTypeToUseByRange == null)
            {
                Log.WriteLine("ReloadNormalAmmo: We do not have any ammo left that can hit [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 0) + "]!");
                return true;
            }

            _findAmmoAttempts = 0;
            return false;
        }

        private static bool ReloadEnergyWeaponAmmo(ModuleCache weapon, int weaponNumber, DirectItem myChargeToLoadIntoWeapon = null, bool force = false)
        {
            if (_lastReloadIteration.AddSeconds(1) > DateTime.UtcNow)
                return false;

            _lastReloadIteration = DateTime.UtcNow;

            if (Time.Instance.LastReloadAttemptTimeStamp != null && Time.Instance.LastReloadAttemptTimeStamp.ContainsKey(weapon.ItemId))
                if (DateTime.UtcNow < Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId].AddSeconds(1))
                {
                    if (DebugConfig.DebugReloadAll)
                        Log.WriteLine("Weapon [" + _weaponNumber + "] was just attempted to be reloaded [" +
                                      Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId]).TotalSeconds,
                                          0) +
                                      "] seconds ago");
                    return true;
                }

            if (Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(weapon.ItemId))
                if (DateTime.UtcNow < Time.Instance.LastReloadedTimeStamp[weapon.ItemId].AddSeconds(1))
                {
                    if (DebugConfig.DebugReloadAll)
                        Log.WriteLine("Weapon [" + _weaponNumber + "] was just reloaded [" +
                                      Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastReloadedTimeStamp[weapon.ItemId]).TotalSeconds,
                                          0) +
                                      "] seconds ago");
                    return true;
                }

            //if (!ESCache.Instance.InMission) return true;
            if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.Any(i => i.TypeId == (int)TypeID.CivilianGatlingAutocannon
                                                  || i.TypeId == (int)TypeID.CivilianGatlingPulseLaser
                                                  || i.TypeId == (int)TypeID.CivilianGatlingRailgun
                                                  || i.TypeId == (int)TypeID.CivilianLightElectronBlaster))
                return true;

            IEnumerable<AmmoType> correctAmmo = DirectUIModule.DefinedAmmoTypes.Where(a => a.DamageType == MissionSettings.CurrentDamageType && a.Range > EntityToUseForAmmo.Distance).ToList();

            IEnumerable<AmmoType> correctAmmoInCargo =
                correctAmmo.Where(a => ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId == a.TypeId))
                    .ToList();

            correctAmmoInCargo =
                correctAmmoInCargo.Where(
                        a =>
                            ESCache.Instance.CurrentShipsCargo != null &&
                            ESCache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId == a.TypeId && e.Quantity >= MinimumAmmoCharges))
                    .ToList();

            if (!correctAmmoInCargo.Any())
                if (MissionSettings.AnyAmmoOfTypeLeft(MissionSettings.CurrentDamageType))
                {
                    Log.WriteLine($"No charges left in ships cargo, using the remaining charges in the launchers before swapping to the second best damage type.");
                    return true;
                }
                else
                {
                    ChangeCombatState(CombatState.OutOfAmmo, "ReloadEnergyWeapon: not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold");
                    return false;
                }

            if (weapon.ChargeQty > 0)
            {
                IEnumerable<AmmoType> areWeMissingAmmo = correctAmmoInCargo.Where(a => weapon.ChargeQty > 0 && a.TypeId == weapon.Charge.TypeId);
                if (!areWeMissingAmmo.Any())
                    Log.WriteLine("ReloadEnergyWeaponAmmo: We have ammo loaded that does not have a full reload available in the cargo.");
            }


            if (CorrectAmmoTypeToUseByRange == null)
            {
                if (DebugConfig.DebugCorrectAmmoTypeToUseByRange || DebugConfig.DebugReloadorChangeAmmo || DebugConfig.DebugReloadAll) Log.WriteLine("ReloadEnergyWeaponAmmo: if (correctAmmoTypeToUseByRange == null) return true");
                return true;
            }

            if (myChargeToLoadIntoWeapon == null)
            {
                myChargeToLoadIntoWeapon = ChargeToLoadIntoWeapon(CorrectAmmoTypeToUseByRange);
            }
            if (myChargeToLoadIntoWeapon == null)
            {
                Log.WriteLine("ReloadNormalAmmo: if (myChargeToLoadIntoWeapon == null)");
                return true;
            }

            if (DebugConfig.DebugReloadorChangeAmmo)
                Log.WriteLine("ReloadEnergyWeaponAmmo: best possible ammo: [" + myChargeToLoadIntoWeapon.TypeId + "][" + myChargeToLoadIntoWeapon.DefinedDamageTypeForThisItem + "]");
            if (DebugConfig.DebugReloadorChangeAmmo)
                Log.WriteLine("ReloadEnergyWeaponAmmo: best possible ammo: [" + EntityToUseForAmmo.Name + "][" + Math.Round(EntityToUseForAmmo.Distance / 1000, 0) + "]");

            if (myChargeToLoadIntoWeapon == null)
            {
                if (DebugConfig.DebugReloadorChangeAmmo)
                    Log.WriteLine("ReloadEnergyWeaponAmmo: We do not have any ammo left that can hit [" + EntityToUseForAmmo.Name + "][" + Math.Round(EntityToUseForAmmo.Distance/1000, 0) + "]!");
                return false;
            }

            if (DebugConfig.DebugReloadorChangeAmmo)
                Log.WriteLine("ReloadEnergyWeaponAmmo: charge: [" + myChargeToLoadIntoWeapon.TypeName + "][" + myChargeToLoadIntoWeapon.TypeId + "]");

            if (weapon.ChargeQty > 0 && weapon.Charge.TypeId == myChargeToLoadIntoWeapon.TypeId)
            {
                if (DebugConfig.DebugReloadorChangeAmmo)
                    Log.WriteLine("ReloadEnergyWeaponAmmo: We have DefinedAmmoTypes of that type Loaded Already");
                return true;
            }

            if (weapon.IsReloadingAmmo)
                return true;

            if (weapon.ChargeQty > 0 && weapon.Charge.TypeId == myChargeToLoadIntoWeapon.TypeId)
            {
                //
                // no need to change ammo if its the same type: lasers last a very long time before getting damage / being destroyed
                //
                return true;
            }

            //
            // if the weapon is active and the ammo type we want to use is different than what is in the gun (see above) deactivate the weapon
            //
            if (weapon.IsActive)
            {
                weapon.Click();
                return false;
            }

            if (weapon.ChangeAmmo(myChargeToLoadIntoWeapon, weaponNumber, (double)myChargeToLoadIntoWeapon.MaxRange, EntityToUseForAmmo))
                return true;

            return false;
        }

        private static DirectItem _chargeToLoadIntoWeapon;

        public static DirectItem ChargeToLoadIntoWeapon(AmmoType myCorrectAmmoTypeToUseByRange = null)
        {
            try
            {
                if (myCorrectAmmoTypeToUseByRange == null)
                {
                    myCorrectAmmoTypeToUseByRange = CorrectAmmoTypeToUseByRange;
                }

                if (_chargeToLoadIntoWeapon != null)
                    return _chargeToLoadIntoWeapon;

                if (ESCache.Instance.CurrentShipsCargo != null)
                {
                    if (ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
                    {
                        _chargeToLoadIntoWeapon = Combat.UsableAmmoInCargo.Find(e => e.TypeId == myCorrectAmmoTypeToUseByRange.TypeId && e.Quantity >= MinimumAmmoCharges);
                        if (_chargeToLoadIntoWeapon == null)
                        {
                            if (DebugConfig.DebugReloadAll)
                                Log.WriteLine("We have no ammo matching typeID [" + myCorrectAmmoTypeToUseByRange.TypeId + "] in cargo?! This should have shown up as out of ammo. Note: MinimumAmmoCharges [" + MinimumAmmoCharges + "]");
                            return null;
                        }

                        return _chargeToLoadIntoWeapon;
                    }
                    else
                    {
                        if (DebugConfig.DebugReloadAll)
                            Log.WriteLine("We have no items in cargo at all?! This should have shown up as out of ammo");
                        return null;
                    }
                }
                else
                {
                    if (DebugConfig.DebugReloadAll) Log.WriteLine("CurrentShipsCargo is null?!");
                    return null;
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return null;
            }
        }
        private static bool ReloadNormalAmmo(ModuleCache weapon, int weaponNumber, DirectItem myChargeToLoadIntoWeapon = null, bool force = false)
        {
            if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement) Log.WriteLine("ReloadNormalAmmo [" + weapon.TypeName + "] Current Charge [" + weapon.ChargeName + "][" + weaponNumber + "][" + force + "]");
            if (_lastReloadIteration.AddSeconds(1) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugReloadAll) Log.WriteLine("ReloadNormalAmmo: waiting");
                return false;
            }

            _lastReloadIteration = DateTime.UtcNow;

            //if (State.CurrentHydraState != HydraState.Combat && State.CurrentHydraState != HydraState.Leader)
            //    if (MissionSettings.MyMission != null && MissionSettings.MyMission.Name != null && MissionSettings.MyMission.Name.Contains("Anomic"))
            //        return true;

            if (!weapon.IsEnergyWeapon && Time.Instance.LastReloadAttemptTimeStamp != null && Time.Instance.LastReloadAttemptTimeStamp.ContainsKey(weapon.ItemId))
                if (DateTime.UtcNow < Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId].AddSeconds(ESCache.Instance.RandomNumber(20, 30)))
                {
                    if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement)
                        Log.WriteLine("Weapon [" + _weaponNumber + "] was just attempted to be reloaded [" +
                                      Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId]).TotalSeconds,
                                          0) +
                                      "] seconds ago");
                    return true;
                }

            if (!weapon.IsEnergyWeapon && Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(weapon.ItemId))
                if (DateTime.UtcNow < Time.Instance.LastReloadedTimeStamp[weapon.ItemId].AddSeconds(ESCache.Instance.RandomNumber(20, 30)))
                {
                    if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement)
                        Log.WriteLine("Weapon [" + _weaponNumber + "] was just reloaded [" +
                                      Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastReloadedTimeStamp[weapon.ItemId]).TotalSeconds,
                                          0) +
                                      "] seconds ago");
                    return true;
                }

            //if (!ESCache.Instance.InMission) return true;
            if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.Any(i => i.TypeId == (int)TypeID.CivilianGatlingAutocannon
                                                  || i.TypeId == (int)TypeID.CivilianGatlingPulseLaser
                                                  || i.TypeId == (int)TypeID.CivilianGatlingRailgun
                                                  || i.TypeId == (int)TypeID.CivilianLightElectronBlaster))
            {
                if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement) Log.WriteLine("ReloadNormalAmmo: Civilian weapons do not use ammo: and thus do not reload");
                return true;
            }

            bool? areWeOutOfAmmoBool = AreWeOutOfAmmo(EntityToUseForAmmo);

            if (areWeOutOfAmmoBool == null)
                return false;
            if ((bool)areWeOutOfAmmoBool)
            {
                ChangeCombatState(CombatState.OutOfAmmo, "ReloadNormalAmmo: if ((bool)areWeOutOfAmmoBool)");
                if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement) Log.WriteLine("ReloadNormalAmmo: OutOfAmmo");
                return true;
            }

            //
            // if we made it this far we are not out of ammo.
            //

            if (weapon == null)
            {
                if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement) Log.WriteLine("weapon == null");
                return true;
            }

            /**
            if (weapon.GroupId == (int)GroupID.PrecursorWeapon)
                if (DateTime.UtcNow > Time.Instance.LastWeaponUnloadToCargo.AddSeconds(45))
                    if (weapon._module.UnloadToCargo())
                    {
                        Time.Instance.LastWeaponUnloadToCargo = DateTime.UtcNow;
                        return false;
                    }
            **/

            if (CorrectAmmoTypeToUseByRange == null)
            {
                if (DebugConfig.DebugCorrectAmmoTypeToUseByRange || DebugConfig.DebugReloadorChangeAmmo || DebugConfig.DebugReloadAll) Log.WriteLine("ReloadNormalAmmo: if (correctAmmoTypeToUseByRange == null) return true");
                return true;
            }

            if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement)
            {
                Log.WriteLine("DefinedAmmoTypes are currently:");
                int intAmmoType = 0;
                foreach (var definedAmmoType in DirectUIModule.DefinedAmmoTypes)
                {
                    intAmmoType++;
                    Log.WriteLine("# [" + intAmmoType + "][" + definedAmmoType.Description + "] TypeID [" + definedAmmoType.TypeId + "] Range [" + definedAmmoType.Range + "] DamageType [" + definedAmmoType.DamageType + "]");
                }
            }

            if (weapon.ChargeQty > 0 && (long)weapon.ChargeQty >= MinimumAmmoCharges && weapon.Charge.TypeId == CorrectAmmoTypeToUseByRange.TypeId)
            {
                if (!force)
                {
                    if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement)
                        Log.WriteLine("[" + weaponNumber + "] ChargeQty [" + (long)weapon.ChargeQty + "] MaxRange [ " + weapon.MaxRange + " ] if we have 0 charges MaxRange will be 0");
                    Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId] = DateTime.UtcNow;
                    return true;
                }

                if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement)
                    Log.WriteLine("[" + weaponNumber + "] ChargeQty [" + (long)weapon.ChargeQty + "]  MaxRange [ " + weapon.MaxRange + " ] if we have 0 charges MaxRange will be 0");
                Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId] = DateTime.UtcNow;
            }

            if (weapon.IsReloadingAmmo)
            {
                if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement)
                    Log.WriteLine("We are already reloading, wait - weapon.IsReloadingAmmo [" + weapon.IsReloadingAmmo + "]");
                return false;
            }

            try
            {
                if (myChargeToLoadIntoWeapon == null)
                {
                    myChargeToLoadIntoWeapon = ChargeToLoadIntoWeapon(CorrectAmmoTypeToUseByRange);
                }
                if (myChargeToLoadIntoWeapon == null)
                {
                    Log.WriteLine("ReloadNormalAmmo: if (myChargeToLoadIntoWeapon == null)");
                    return true;
                }

                if (weapon.ChargeQty > 0 &&
                    DirectUIModule.DefinedAmmoTypes.Any(i => KillTarget != null && i.IsValidAmmo(KillTarget.TypeId, KillTarget.GroupId)) &&
                    DirectUIModule.DefinedAmmoTypes.All(i => KillTarget != null && i.IsValidAmmo(KillTarget.TypeId, KillTarget.GroupId) && i.TypeId != weapon.Charge.TypeId) &&
                    DirectUIModule.DefinedAmmoTypes.Any(i => KillTarget != null && i.IsValidAmmo(KillTarget.TypeId, KillTarget.GroupId) && i.TypeId == CorrectAmmoTypeToUseByRange.TypeId))
                {
                    Log.WriteLine("ReloadNormalAmmo: We have [" + weapon.Charge.TypeName + "] in the gun which is not a defined ammo type currently: change ammo to [" + CorrectAmmoTypeToUseByRange.Description + "]");
                    if (EntityToUseForAmmo != null && weapon.ChangeAmmo(myChargeToLoadIntoWeapon, weaponNumber, CorrectAmmoTypeToUseByRange.Range, EntityToUseForAmmo))
                        return true;

                    return true;
                }

                if (weapon.ChargeQty > 0 && weapon.Charge.TypeId == myChargeToLoadIntoWeapon.TypeId)
                {
                    if (weapon.ReloadAmmo(myChargeToLoadIntoWeapon, weaponNumber, CorrectAmmoTypeToUseByRange.Range))
                        return true;

                    Log.WriteLine("ReloadAmmo for [" + weapon.ItemId + "] failed.");
                    return true;
                }

                if (EntityToUseForAmmo != null && weapon.ChangeAmmo(myChargeToLoadIntoWeapon, weaponNumber, CorrectAmmoTypeToUseByRange.Range, EntityToUseForAmmo))
                    return true;

                Log.WriteLine("ChangeAmmo for [" + weapon.ItemId + "] failed.");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }

            return true;
        }

        #endregion Methods
    }
}