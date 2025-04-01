extern alias SC;

using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Events;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Traveller;
using System.Runtime.CompilerServices;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.Utility;
//using SharedComponents.Utility;

namespace EVESharpCore.Cache
{
    public class EntityCache
    {
        #region Constructors

        public EntityCache(DirectEntity entity)
        {
            _directEntity = entity;
            _thisEntityCacheCreated = DateTime.UtcNow;
        }

        #endregion Constructors

        #region Fields

        public readonly DirectEntity _directEntity;
        private const int DictionaryCountThreshold = 250;
        public readonly DateTime _thisEntityCacheCreated = DateTime.UtcNow;
        private string _givenName;
        private bool? _isAbyssalDeadspaceDeviantAutomataSuppressorTower;
        //private bool? _isAbyssalDeadspaceSmallDeviantAutomataSuppressorTower;
        //private bool? _isAbyssalDeadspaceMediumDeviantAutomataSuppressorTower;
        private bool? _isAbyssalDeadspaceMultibodyTrackingPylonTower;
        private bool? _isAbyssalDeadspaceTriglavianBioAdaptiveCache;
        private bool? _isAbyssalDeadspaceTriglavianExtractionNode;
        private bool? _isAbyssalPrecursorCache;
        private bool? _isBadIdea;
        private bool? _isCorrectSizeForMyWeapons;
        private bool? _isEntityIShouldKeepShooting;
        private bool? _isEntityIShouldKeepShootingWithDrones;
        private bool? _isHigherPriorityPresent;
        private bool? _isHighValueTarget;
        private bool? _isIgnored;
        private bool? _isInOptimalRange;
        private bool? _isLowValueTarget;
        private bool? _isOnGridWithMe;
        private bool? _isPreferredDroneTarget;
        private bool? _isPreferredPrimaryWeaponTarget;
        private bool? _isPrimaryWeaponKillPriority;
        private bool? _isReadyForDronesToShoot;
        private bool? _isReadyToShoot;
        private bool? _isReadyToTarget;
        private bool? _isTooCloseTooFastTooSmallToHit;
        private bool? _isTrackable;
        private double? _nearest1KDistance;
        private double? _nearest5KDistance;
        private bool? _npcHasNeutralizers;
        private double? _npcRemoteArmorRepairChance;
        private double? _npcRemoteShieldRepairChance;
        private PrimaryWeaponPriority? _primaryWeaponPriorityLevel;

        private int? _targetValue;

        private double? _warpScrambleChance;

        #endregion Fields

        #region Properties


        private double? _angularVelocity = null;

        public double AngularVelocity
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (Combat.DoWeCurrentlyHaveTurretsMounted())
                    {
                        _angularVelocity = _directEntity.AngularVelocity;
                        return (double)_angularVelocity;
                    }

                    return 0;
                }

                return 0;
            }
        }

        //ArmorMaxHitPoints is static and will never change across the life of the Entity
        public double ArmorMaxHitPoints => _directEntity.MaxArmor ?? 0;
        public double ArmorCurrentHitPoints => (_directEntity.MaxArmor * _directEntity.ArmorPct) ?? 0;
        public double ArmorPct => Math.Round(_directEntity.ArmorPct, 2);

        //ArmorResistanceEm is static and will never change across the life of the Entity: it should be taking into account abyssal effects
        public double ArmorResistanceEm => _directEntity.ArmorResistanceEM ?? 0;
        //ArmorResistanceExplosive is static and will never change across the life of the Entity: it should be taking into account abyssal effects
        public double ArmorResistanceExplosive => _directEntity.ArmorResistanceExplosive ?? 0;
        //ArmorResistanceKinetic is static and will never change across the life of the Entity: it should be taking into account abyssal effects
        public double ArmorResistanceKinetic => _directEntity.ArmorResistanceKinetic ?? 0;
        //ArmorResistanceThermal is static and will never change across the life of the Entity: it should be taking into account abyssal effects
        public double ArmorResistanceThermal => _directEntity.ArmorResistanceThermal ?? 0;

        //BestDamageTypes is static and will never change across the life of the Entity
        public string stringBestDamageTypes
        {
            get
            {
                string _temp = string.Empty;
                foreach (var thisDamageType in BestDamageTypes)
                {
                    if (string.IsNullOrEmpty(_temp))
                    {
                        _temp = thisDamageType.ToString() + ";";
                        continue;
                    }

                    _temp = _temp + thisDamageType.ToString() + ";";
                    continue;
                }

                return _temp;
            }
        }

        public List<DamageType> BestDamageTypes
        {
            get
            {
                Tuple<long, string> tupleToFind = new Tuple<long, string>(Id, nameof(BestDamageTypes));
                if (ESCache.Instance.DictionaryCachedPerPocket.ContainsKey(tupleToFind))
                {
                    return (List<DamageType>) ESCache.Instance.DictionaryCachedPerPocket[tupleToFind];
                }

                List<DamageType> tempBestDamageTypes = _directEntity.BestDamageTypes;
                if (tempBestDamageTypes.Count > 0)
                {
                    Tuple<long, string> tupleToAdd = new Tuple<long, string>(Id, nameof(BestDamageTypes));
                    ESCache.Instance.DictionaryCachedPerPocket.AddOrUpdate(tupleToAdd, tempBestDamageTypes);
                    return tempBestDamageTypes;
                }

                return new List<DamageType>();
            }
        }

        //BracketType is static and will never change across the life of the Entity
        public BracketType BracketType => _directEntity.BracketType;

        public bool ScoopToCargoHold => _directEntity.ScoopToCargoHold;

        public double GetSecondsToKill(Dictionary<DirectDamageType, float> damagePairs)
        {
            return GetSecondsToKill(damagePairs, new List<EntityCache>() { this });
        }

        public static double GetSecondsToKill(Dictionary<DirectDamageType, float> damagePairs, List<EntityCache> entities)
        {
            // This will return Shield, Armor, Structure base HP combined if resists can't be read
            double effectiveHealthEM = 0;
            double effectiveHealthKinetic = 0;
            double effectiveHealthExplosive = 0;
            double effectiveHealthThermal = 0;

            foreach (var ent in entities)
            {
                effectiveHealthEM += ent._directEntity.EmEHP.Value;
                effectiveHealthKinetic += ent._directEntity.KinEHP.Value;
                effectiveHealthExplosive += ent._directEntity.ExpEHP.Value;
                effectiveHealthThermal += ent._directEntity.TrmEHP.Value;
            }

            if (effectiveHealthEM <= 0 && effectiveHealthKinetic <= 0 && effectiveHealthExplosive <= 0 && effectiveHealthThermal <= 0)
            {
                return 0;
            }

            // Remove entries with 0 DPS
            damagePairs = damagePairs.Where(x => x.Value > 0.0).ToDictionary(x => x.Key, x => x.Value);

            var totalDps = damagePairs.Sum(x => x.Value);

            if (totalDps == 0.0)
            {
                return double.PositiveInfinity;
            }

            var totalEffEHp = 0d;
            // Calculate the damage distribution percentages and total eff ehp
            foreach (var pair in damagePairs)
            {
                var percentage = pair.Value / totalDps;

                switch (pair.Key)
                {
                    case DirectDamageType.EM:

                        if (Double.IsInfinity(effectiveHealthEM))
                            return Double.PositiveInfinity;

                        effectiveHealthEM *= percentage;
                        totalEffEHp += effectiveHealthEM;
                        break;
                    case DirectDamageType.KINETIC:

                        if (Double.IsInfinity(effectiveHealthKinetic))
                            return Double.PositiveInfinity;

                        effectiveHealthKinetic *= percentage;
                        totalEffEHp += effectiveHealthKinetic;
                        break;
                    case DirectDamageType.EXPLO:

                        if (Double.IsInfinity(effectiveHealthExplosive))
                            return Double.PositiveInfinity;

                        effectiveHealthExplosive *= percentage;
                        totalEffEHp += effectiveHealthExplosive;
                        break;
                    case DirectDamageType.THERMAL:

                        if (Double.IsInfinity(effectiveHealthThermal))
                            return Double.PositiveInfinity;

                        effectiveHealthThermal *= percentage;
                        totalEffEHp += effectiveHealthThermal;
                        break;
                }
            }

            double secondsToKill = totalEffEHp / totalDps;
            return secondsToKill;
        }

        private DirectItem DirectItemFromAmmoType(AmmoType myAmmoType)
        {
            DirectItem tempDirectItem = new DirectItem(ESCache.Instance.DirectEve)
            {
                TypeId = (int)myAmmoType.TypeId
            };

            return tempDirectItem;
        }

        public AmmoType BestAvailableAmmoType(List<AmmoType> listAvailableAmmo)
        {
            //BestDamageTypes
            foreach (AmmoType individualAvailableAmmo in listAvailableAmmo.OrderByDescending(x => AmmoDamageCalc(DirectItemFromAmmoType(x))))
            {
                return individualAvailableAmmo;
            }

            return null;
        }

        public DirectContainerWindow CargoWindow
        {
            get
            {
                try
                {
                    if (ESCache.Instance.Windows.Count == 0)
                        return null;

                    return ESCache.Instance.Windows.OfType<DirectContainerWindow>().FirstOrDefault(w => w.ItemId == Id);
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public int CategoryId
        {
            get
            {
                try
                {
                    Tuple<long, string> tupleToFind = new Tuple<long, string>(Id, nameof(CategoryId));
                    if (ESCache.Instance.DictionaryCachedPerPocket.ContainsKey(tupleToFind))
                    {
                        return (int)ESCache.Instance.DictionaryCachedPerPocket[tupleToFind];
                    }

                    int tempCategoryId = _directEntity.CategoryId;
                    Tuple<long, string> tupleToAdd = new Tuple<long, string>(Id, nameof(CategoryId));
                    ESCache.Instance.DictionaryCachedPerPocket.AddOrUpdate(tupleToAdd, tempCategoryId);
                    return tempCategoryId;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]" );
                    return 0;
                }
            }
        }

        public double Distance => _directEntity.Distance;

        public double DistanceInAU
        {
            get
            {
                if (!RecursionCheck(nameof(DistanceInAU)))
                    return -1;

                return Math.Round(_directEntity.Distance / (double)Distances.OneAu, 2);
            }
        }

        /// <summary>
        /// Returns the average distance to the list of entities
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public double DistanceTo(IEnumerable<EntityCache> entities)
        {
            if (!entities.Any())
                return 0;

            var totalDist = 0d;
            var entCount = entities.Count();

            foreach (var entity in entities)
            {
                totalDist += entity.DistanceTo(entity);
            }

            return totalDist / entCount;
        }

        public double DistanceTo(EntityCache ent)
        {
            try
            {
                if (ent != null)
                    return ent._directEntity.DirectAbsolutePosition.GetDistance(this._directEntity.DirectAbsolutePosition) - (double)_directEntity.BallRadius - (double)ent._directEntity.BallRadius;
                return 0;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        public double DistanceTo(DirectWorldPosition DWP)
        {
            try
            {
                return _directEntity.DirectAbsolutePosition.GetDistance(DWP);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        /**
        public bool IsBeingShotAtByOurDrones
        {
            get
            {
                if (Drones.ActiveDrones.Any())
                {
                    EntityCache drone = Drones.ActiveDrones.FirstOrDefault();
                    drone.IsLastTargetDronesWereShooting
                }

                return false;
            }
        }
        **/

        private Dictionary<string, int> DictionaryRecursionTracking = new Dictionary<string, int>();

        private bool RecursionCheck(string CheckThisRoutine)
        {
            if (DictionaryRecursionTracking.ContainsKey(CheckThisRoutine))
            {
                int tempInt = 0;
                DictionaryRecursionTracking.TryGetValue(CheckThisRoutine, out tempInt);
                if (tempInt >= 1)
                {
                    if (tempInt > 100)
                    {
                        Log.WriteLine(CheckThisRoutine + "has been run [" + tempInt + "] times this frame");
                        DictionaryRecursionTracking.AddOrUpdate(CheckThisRoutine, tempInt++);
                        return false;
                    }

                    DictionaryRecursionTracking.AddOrUpdate(CheckThisRoutine, tempInt++);
                    return true;
                }

                DictionaryRecursionTracking.AddOrUpdate(CheckThisRoutine, 1);
                return true;
            }

            DictionaryRecursionTracking.AddOrUpdate(CheckThisRoutine, 1);
            return true;
        }

        private bool? _isKillTarget;

        public bool IsKillTarget
        {
            get
            {
                if (!RecursionCheck(nameof(IsKillTarget)))
                    return false;

                if (_directEntity != null && IsValid)
                {
                    if (_isKillTarget == null)
                    {
                        if (Combat._pickPrimaryWeaponTarget == null)
                            return false;

                        if (Combat._pickPrimaryWeaponTarget.Id == Id)
                        {
                            _isKillTarget = true;
                            return (bool)_isKillTarget;
                        }

                        _isKillTarget = false;
                        return (bool)_isKillTarget;
                    }

                    return (bool)_isKillTarget;
                }

                return false;
            }
        }

        private bool? _isDroneKillTarget;

        public bool IsDroneKillTarget
        {
            get
            {
                if (!RecursionCheck(nameof(IsDroneKillTarget)))
                    return false;

                if (_directEntity != null && IsValid)
                {
                    if (_isDroneKillTarget == null)
                    {
                        if (Drones._cachedDroneTarget == null)
                            return false;

                        if (Drones._cachedDroneTarget.Id == Id)
                        {
                            _isDroneKillTarget = true;
                            return (bool)_isDroneKillTarget;
                        }

                        _isDroneKillTarget = false;
                        return (bool)_isDroneKillTarget;
                    }

                    return (bool)_isDroneKillTarget;
                }

                return false;
            }
        }

        public bool WeShouldFocusFire
        {
            get
            {
                if (!RecursionCheck(nameof(WeShouldFocusFire)))
                    return false;

                if (_directEntity != null && IsValid)
                {
                    if (!Combat.FocusFireWhenWeaponsAndDronesAreInRangeOfDifficultTargets)
                        return false;

                    if (!IsInDroneRange)
                        return false;

                    if (!IsInRangeOfWeapons)
                        return false;

                    if (BracketType == BracketType.NPC_Battlecruiser)
                        return true;

                    if (BracketType == BracketType.NPC_Battleship && Combat.PotentialCombatTargets.All(i => i.IsBattleship))
                        return true;

                    if (BracketType == BracketType.Battlecruiser)
                        return true;

                    if (Name.ToLower().Contains("Vila Vedmak".ToLower()))
                        return false;

                    if (Name.ToLower().Contains("Vedmak".ToLower()))
                        return true;

                    if (TypeId == 48087) //Starving Vedmak
                        return true;

                    if (TypeId == 48090) //Starving Vedmak
                        return true;

                    if (TypeId == 48091) //Harrowing Vedmak
                        return true;

                    if (TypeId == 48092) //Striking Vedmak
                        return true;

                    return false;
                }

                return false;
            }
        }

        public DronePriority DronePriorityLevel
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (Drones.DronePriorityTargets.Any(pt => pt.EntityID == _directEntity.Id))
                        {
                            DronePriority currentTargetPriority = Drones.DronePriorityTargets.Where(t => t.Entity.IsTarget
                                                                                                         && t.EntityID == Id)
                                .Select(pt => pt.DronePriority)
                                .FirstOrDefault();

                            return currentTargetPriority;
                        }

                        return DronePriority.NotUsed;
                    }

                    return DronePriority.NotUsed;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return DronePriority.NotUsed;
                }
            }
        }

        public long FactionId => _directEntity.FactionId;

        public Faction Faction => _directEntity.Faction;

        public long FollowId => _directEntity.FollowId;
        public double GetBounty => _directEntity != null ? _directEntity.GetBounty() : 0;

        //GivenName is static (for NPCs) and will never change across the life of the Entity
        public string GivenName
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCacheCreated)
                            Log.WriteLine("The EntityCache instance that represents [" + _directEntity.Name + "][" +
                                          Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                          MaskedId + "] was created [" + _thisEntityCacheCreated + "]");
                        if (string.IsNullOrEmpty(_givenName))
                            _givenName = _directEntity.GivenName;

                        return _givenName ?? "";
                    }

                    return "";
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return "";
                }
            }
        }

        //GroupId is static (for NPCs) and will never change across the life of the Entity
        public int GroupId
        {
            get
            {
                try
                {
                    Tuple<long, string> tupleToFind = new Tuple<long, string>(Id, nameof(GroupId));
                    if (ESCache.Instance.DictionaryCachedPerPocket.ContainsKey(tupleToFind))
                    {
                        return (int)ESCache.Instance.DictionaryCachedPerPocket[tupleToFind];
                    }

                    int tempGroupId = _directEntity.GroupId;
                    Tuple<long, string> tupleToAdd = new Tuple<long, string>(Id, nameof(GroupId));
                    ESCache.Instance.DictionaryCachedPerPocket.AddOrUpdate(tupleToAdd, tempGroupId);
                    return tempGroupId;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 0;
                }
            }
        }

        public bool HasExploded => _directEntity.HasExploded;

        public bool HasInitiatedWarp => _directEntity.IsWarpingByMode;

        public bool HasReleased => _directEntity.HasReleased;

        public bool HaveLootRights
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (ESCache.Instance.InAbyssalDeadspace)
                            return true;

                        if (ESCache.Instance.InWormHoleSpace)
                            return true;

                        //True for 0.0 too
                        //if (ESCache.Instance.InZeroZero)
                        //    return true;

                        if (GroupId != (int)Group.Wreck
                            && GroupId != (int)Group.SpawnContainer
                            && GroupId != (int)Group.CargoContainer)
                            return false;

                        if (GroupId == (int)Group.SpawnContainer)
                            return true;

                        if (_directEntity.IsOwnedByMe)
                            return true;

                        if (IsOwnedByAFleetMember)
                            return true;

                        if (_directEntity.IsOwnedByMyCorp)
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsOwnedByAFleetMember
        {
            get
            {
                if (ESCache.Instance.MyLeaderAndSlaveCharacterIDsFromLauncher.Contains(_directEntity.OwnerId.ToString()))
                    return true;

                return false;
            }
        }

        public double EffectiveHitpointsViaEm
        {
            get
            {
                return ShieldEffectiveHitpointsViaEm * _directEntity.ArmorEffectiveHitpointsViaEM;
            }
        }

        public double EffectiveHitpointsViaExplosive
        {
            get
            {
                return ShieldEffectiveHitpointsViaExplosive * _directEntity.ArmorEffectiveHitpointsViaExplosive;
            }
        }

        public double EffectiveHitpointsViaKinetic
        {
            get
            {
                return ShieldEffectiveHitpointsViaKinetic * _directEntity.ArmorEffectiveHitpointsViaKinetic;
            }
        }

        public double EffectiveHitpointsViaThermal
        {
            get
            {
                return ShieldEffectiveHitpointsViaThermal * _directEntity.ArmorEffectiveHitpointsViaThermal;
            }
        }

        public double ShieldEffectiveHitpointsViaEm
        {
            get
            {
                return ShieldResistanceEm * ShieldMaxHitPoints;
            }
        }

        public double ShieldEffectiveHitpointsViaExplosive
        {
            get
            {
                return ShieldResistanceEm * ShieldMaxHitPoints;
            }
        }

        public double ShieldEffectiveHitpointsViaKinetic
        {
            get
            {
                return ShieldResistanceKinetic * ShieldMaxHitPoints;
            }
        }

        public double ShieldEffectiveHitpointsViaThermal
        {
            get
            {
                return ShieldResistanceThermal * ShieldMaxHitPoints;
            }
        }

        public bool IsSmallPCTEntity
        {
            get
            {
                if (Combat.PotentialCombatTargets.Any(i => i.Id == Id))
                {
                    if (!IsMediumPCTEntity && !IsLargePCTEntity)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsMediumPCTEntity
        {
            get
            {
                if (Combat.PotentialCombatTargets.Any(i => i.Id == Id))
                {
                    if (IsCruiser)
                        return true;

                    if (IsBattlecruiser)
                        return true;

                    if (IsNPCCruiser)
                        return true;

                    if (IsNPCBattlecruiser)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsLargePCTEntity
        {
            get
            {
                if (Combat.PotentialCombatTargets.Any(i => i.Id == Id))
                {
                    if (IsBattleship)
                        return true;

                    if (IsNPCBattleship)
                        return true;

                    if (IsNpcCapitalEscalation)
                        return true;

                    if (IsNPCCapitalShip)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsInDPSGroup1
        {
            get
            {
                double intDivideEntitiesIntoThisManyPiles = ESCache.Instance.DPSGroupCount;
                double EntityCount = 0;
                double NumberOfEntitiesPerPile = 0;

                if (IsSmallPCTEntity)
                {
                    List<EntityCache> ListOfSmallPCTEntities = Combat.PotentialCombatTargets.Where(i => i.IsSmallPCTEntity).ToList();
                    EntityCount = ListOfSmallPCTEntities.Count();
                    NumberOfEntitiesPerPile = Math.Ceiling(EntityCount / intDivideEntitiesIntoThisManyPiles);


                }

                if (IsMediumPCTEntity)
                {
                    List<EntityCache> ListOfMediumPCTEntities = Combat.PotentialCombatTargets.Where(i => i.IsMediumPCTEntity).ToList();
                    EntityCount = Combat.PotentialCombatTargets.Count(i => i.IsMediumPCTEntity);
                    NumberOfEntitiesPerPile = Math.Ceiling(EntityCount / intDivideEntitiesIntoThisManyPiles);
                }

                if (IsLargePCTEntity)
                {
                    List<EntityCache> ListOfLargePCTEntities = Combat.PotentialCombatTargets.Where(i => i.IsLargePCTEntity).ToList();
                    EntityCount = Combat.PotentialCombatTargets.Count(i => i.IsLargePCTEntity);
                    NumberOfEntitiesPerPile = Math.Ceiling(EntityCount / intDivideEntitiesIntoThisManyPiles);
                }




                return false;
            }
        }

        public bool IsInDPSGroup2
        {
            get
            {
                return false;
            }
        }

        public bool IsInDPSGroup3
        {
            get
            {
                return false;
            }
        }

        public bool IsInDPSGroup4
        {
            get
            {
                return false;
            }
        }

        public bool IsInDPSGroup5
        {
            get
            {
                return false;
            }
        }

        public bool IsInDPSGroup6
        {
            get
            {
                return false;
            }
        }

        public bool IsInDPSGroup7
        {
            get
            {
                return false;
            }
        }

        public bool IsInDPSGroup8
        {
            get
            {
                return false;
            }
        }

        public bool IsInDPSGroup9
        {
            get
            {
                return false;
            }
        }

        public bool IsInDPSGroup10
        {
            get
            {
                return false;
            }
        }

        public bool IsInMyDPSGroup
        {
            get
            {
                if (ESCache.Instance.EveAccount.DPSGroup == 1 && IsInDPSGroup1)
                {
                    return true;
                }

                if (ESCache.Instance.EveAccount.DPSGroup == 2 && IsInDPSGroup2)
                {
                    return true;
                }

                if (ESCache.Instance.EveAccount.DPSGroup == 3 && IsInDPSGroup3)
                {
                    return true;
                }

                if (ESCache.Instance.EveAccount.DPSGroup == 4 && IsInDPSGroup4)
                {
                    return true;
                }

                if (ESCache.Instance.EveAccount.DPSGroup == 5 && IsInDPSGroup5)
                {
                    return true;
                }

                if (ESCache.Instance.EveAccount.DPSGroup == 6 && IsInDPSGroup6)
                {
                    return true;
                }

                if (ESCache.Instance.EveAccount.DPSGroup == 7 && IsInDPSGroup7)
                {
                    return true;
                }

                if (ESCache.Instance.EveAccount.DPSGroup == 8 && IsInDPSGroup8)
                {
                    return true;
                }

                if (ESCache.Instance.EveAccount.DPSGroup == 9 && IsInDPSGroup9)
                {
                    return true;
                }

                if (ESCache.Instance.EveAccount.DPSGroup == 10 && IsInDPSGroup10)
                {
                    return true;
                }

                return false;
            }
        }

        private long? _characterId = null;

        //CharacterId is static and will never change across the life of the Entity
        public long? CharacterId
        {
            get
            {
                try
                {
                    if (_characterId == null)
                    {
                        if (Name == ESCache.Instance.EveAccount.LeaderCharacterName)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.LeaderCharacterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName1)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter1ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName2)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter2ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName3)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter3ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName4)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter4ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName5)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter5ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName6)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter6ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName7)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter7ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName8)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter8ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName9)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter9ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName10)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter10ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName11)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter11ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName12)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter12ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName13)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter13ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName14)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter14ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName15)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter15ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName16)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter16ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName17)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter17ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName18)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter18ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName19)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter19ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName20)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter20ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName21)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter21ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName22)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter22ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName23)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter23ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName24)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter24ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName25)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter25ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName26)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter26ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName27)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter27ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName28)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter28ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName29)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter29ChracterId);
                            return _characterId;
                        }

                        if (Name == ESCache.Instance.EveAccount.SlaveCharacterName30)
                        {
                            _characterId = long.Parse(ESCache.Instance.EveAccount.SlaveCharacter30ChracterId);
                            return _characterId;
                        }

                        return null;
                    }

                    return _characterId;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public int HealthPct
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                        return (int)((ShieldPct + ArmorPct + StructurePct) * 100);

                    return 0;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public int TotalCurrentHitpoints
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                        return (int)(ShieldMaxHitPoints + ArmorMaxHitPoints + StructureMaxHitPoints);

                    return 0;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public long Id => _directEntity.Id;

        private bool? _isAbyssalCenter;
        public bool IsAbyssalCenter
        {
            get
            {
                if (_isAbyssalCenter == null)
                {
                    if (TypeId == (int)TypeID.AbyssalCenter)
                    {
                        _isAbyssalCenter = true;
                        return (bool)_isAbyssalCenter;
                    }

                    _isAbyssalCenter = false;
                    return (bool)_isAbyssalCenter;
                }

                return (bool)_isAbyssalCenter;
            }
        }

        public bool IsAbyssalDeadspaceTachyonCloud
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        if (TypeId == (int)TypeID.SmallTachyonCloud)
                        {
                            //_directEntity.DrawSphereAround(8000);
                            return true;
                        }

                        if (TypeId == (int)TypeID.MediumTachyonCloud)
                        {
                            //_directEntity.DrawSphereAround(15000);
                            return true;
                        }

                        if (TypeId == (int)TypeID.LargeTachyonCloud)
                        {
                            //_directEntity.DrawSphereAround(20000);
                            return true;
                        }

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        //IsAbyssalDeadspaceBioluminesenceCloud is static and will never change across the life of the Entity
        public bool IsAbyssalDeadspaceBioluminesenceCloud
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= TypeId == (int)TypeID.SmallBioluminescenceCloud;
                        result |= TypeId == (int)TypeID.MediumBioluminescenceCloud;
                        result |= TypeId == (int)TypeID.LargeBioluminescenceCloud;

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        //IsAbyssalDeadspaceDeviantAutomataSuppressor is static and will never change across the life of the Entity
        public bool IsAbyssalDeadspaceDeviantAutomataSuppressor
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isAbyssalDeadspaceDeviantAutomataSuppressorTower == null)
                        {
                            bool result = false;
                            result |= TypeId == (int)TypeID.ShortRangeDeviantAutomataSuppressor;
                            result |= TypeId == (int)TypeID.MediumRangeDeviantAutomataSuppressor;

                            _isAbyssalDeadspaceDeviantAutomataSuppressorTower = result;
                            if (DebugConfig.DebugEntityCache)
                                Log.WriteLine("Adding [" + Name + "] to _isAbyssalDeadspaceDeviantAutomataSuppressorTower as [" + _isAbyssalDeadspaceDeviantAutomataSuppressorTower + "]");
                            return (bool)_isAbyssalDeadspaceDeviantAutomataSuppressorTower;
                        }

                        return (bool)_isAbyssalDeadspaceDeviantAutomataSuppressorTower;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsAbyssalDeadspaceSmallDeviantAutomataSuppressor
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (TypeId == (int)TypeID.ShortRangeDeviantAutomataSuppressor)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsAbyssalDeadspaceMediumDeviantAutomataSuppressor
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (TypeId == (int)TypeID.MediumRangeDeviantAutomataSuppressor)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }


        //IsAbyssalDeadspaceFilamentCloud is static and will never change across the life of the Entity
        public bool IsAbyssalDeadspaceFilamentCloud
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= TypeId == (int)TypeID.SmallFilamentCloud;
                        result |= TypeId == (int)TypeID.MediumFilamentCloud;
                        result |= TypeId == (int)TypeID.LargeFilamentCloud;

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        //IsAbyssalDeadspaceMultibodyTrackingPylon is static and will never change across the life of the Entity
        public bool IsAbyssalDeadspaceMultibodyTrackingPylon
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isAbyssalDeadspaceMultibodyTrackingPylonTower == null)
                        {
                            bool result = false;
                            result |= TypeId == (int)TypeID.ShortRangeMultibodyTrackingPylon;
                            result |= TypeId == (int)TypeID.MediumRangeMultibodyTrackingPylon;

                            _isAbyssalDeadspaceMultibodyTrackingPylonTower = result;
                            if (DebugConfig.DebugEntityCache)
                                Log.WriteLine("Adding [" + Name + "] to EntityIsAbyssalDeadspaceAoeTowerWeapon as [" + _isAbyssalDeadspaceMultibodyTrackingPylonTower + "]");
                            return (bool)_isAbyssalDeadspaceMultibodyTrackingPylonTower;
                        }

                        return (bool)_isAbyssalDeadspaceMultibodyTrackingPylonTower;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        //IsAbyssalDeadspaceTriglavianExtractionNode is static and will never change across the life of the Entity
        public bool IsAbyssalDeadspaceTriglavianExtractionNode
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isAbyssalDeadspaceTriglavianExtractionNode == null)
                        {
                            _isAbyssalDeadspaceTriglavianExtractionNode = false;

                            if (!ESCache.Instance.InAbyssalDeadspace)
                                return false;

                            //If we are in a frigate assume we never want to kill these...
                            if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer || ESCache.Instance.ActiveShip.IsAssaultShip)
                            {
                                _isAbyssalDeadspaceTriglavianExtractionNode = false;
                                return (bool)_isAbyssalDeadspaceTriglavianExtractionNode;
                            }

                            if (TypeId == (int)TypeID.AbyssalDeadspaceTriglavianExtractionNode && ESCache.Instance.InAbyssalDeadspace)
                                _isAbyssalDeadspaceTriglavianExtractionNode = true;

                            if (TypeId == (int)TypeID.AbyssalDeadspaceTriglavianExtractionSubNode && ESCache.Instance.InAbyssalDeadspace)
                                _isAbyssalDeadspaceTriglavianExtractionNode = true;

                            if (Name == "Triglavian Extraction Node" && ESCache.Instance.InAbyssalDeadspace)
                                _isAbyssalDeadspaceTriglavianExtractionNode = true;

                            if (Name == "Triglavian Extraction SubNode" && ESCache.Instance.InAbyssalDeadspace)
                                _isAbyssalDeadspaceTriglavianExtractionNode = true;

                            return _isAbyssalDeadspaceTriglavianExtractionNode ?? false;
                        }

                        return (bool)_isAbyssalDeadspaceTriglavianExtractionNode;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        //IsAbyssalDeadspaceTriglavianBioAdaptiveCache is static and will never change across the life of the Entity

        public bool IsAbyssalBioAdaptiveCache
        {
            get
            {
                return IsAbyssalDeadspaceTriglavianBioAdaptiveCache;
            }
        }

        public bool IsAbyssalDeadspaceTriglavianBioAdaptiveCache
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isAbyssalDeadspaceTriglavianBioAdaptiveCache == null)
                        {
                            _isAbyssalDeadspaceTriglavianBioAdaptiveCache = false;

                            if (TypeId == (int) TypeID.AbyssalDeadspaceTriglavianBioAdaptiveCache && ESCache.Instance.InAbyssalDeadspace)
                                _isAbyssalDeadspaceTriglavianBioAdaptiveCache = true;

                            if (TypeId == (int)TypeID.AbyssalDeadspaceTriglavianBioCombinativeCache && ESCache.Instance.InAbyssalDeadspace)
                                _isAbyssalDeadspaceTriglavianBioAdaptiveCache = true;

                            if (Name == "Triglavian BioAdaptive Cache" && ESCache.Instance.InAbyssalDeadspace)
                                _isAbyssalDeadspaceTriglavianBioAdaptiveCache = true;

                            if (Name == "Triglavian Biocombinative Cache" && ESCache.Instance.InAbyssalDeadspace)
                                _isAbyssalDeadspaceTriglavianBioAdaptiveCache = true;

                            return _isAbyssalDeadspaceTriglavianBioAdaptiveCache ?? false;
                        }

                        return (bool)_isAbyssalDeadspaceTriglavianBioAdaptiveCache;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToTarget
        {
            get
            {
                try
                {
                    if (!IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                        return false;

                    if (IsTarget)
                        return false;

                    if (IsTargeting)
                        return false;

                    //
                    // If we have any moving entities, shoot them first before targeting the BioAdaptiveCache
                    //
                    //if ((ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer) && ESCache.Instance.EntitiesNotSelf.Any(i => i.Velocity > 0))
                    //    return false;

                    if (!IsInDroneRange && Drones.UseDrones)
                        return false;

                    if (IsInDroneRange && Drones.UseDrones)
                        return true;

                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (ESCache.Instance.EntitiesNotSelf.All(i => i.Velocity == 0))
                            return true;

                        return false;
                    }

                    if (!IsInRangeOfWeapons)
                        return false;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToShoot
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToShoot)))
                        return false;

                    if (!IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                        return false;

                    if (!IsTarget)
                        return false;

                    //
                    // If we have any moving entities, shoot them first before targeting the BioAdaptiveCache
                    //
                    //if ((ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer) && ESCache.Instance.EntitiesNotSelf.Any(i => i.Velocity > 0))
                    //    return false;

                    if (!IsInDroneRange && Drones.UseDrones)
                        return false;

                    if (IsInDroneRange && Drones.UseDrones)
                        return true;

                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (ESCache.Instance.EntitiesNotSelf.All(i => i.Velocity == 0))
                            return true;

                        return false;
                    }

                    if (!IsInRangeOfWeapons)
                        return false;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToBeNavigateOnGridTarget
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToBeNavigateOnGridTarget)))
                        return false;

                    if (!IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                        return false;

                    if (IsTarget)
                        return false;

                    if (IsTargeting)
                        return false;

                    //
                    // If we have any moving entities, shoot them first before targeting the BioAdaptiveCache
                    //
                    //if ((ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer) && ESCache.Instance.EntitiesNotSelf.Any(i => i.Velocity > 0))
                    //    return false;

                    if (!IsInDroneRange && Drones.UseDrones)
                        return false;

                    if (IsInDroneRange && Drones.UseDrones)
                        return true;

                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.All(i => !i.IsNPCFrigate && !i.IsNPCDestroyer && !i.IsNPCCruiser && !i.IsNPCBattlecruiser && !i.IsNPCBattleship))
                            return true;

                        return false;
                    }

                    if (!IsInRangeOfWeapons)
                        return false;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsAbyssalDeadspaceTriglavianExtractionNodeReadyToTarget
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsAbyssalDeadspaceTriglavianExtractionNodeReadyToTarget)))
                        return false;

                    //If we are in a frigate assume we do not want to shoot these!
                    if (ESCache.Instance.ActiveShip.Entity.IsFrigate)
                        return false;

                    if (!IsAbyssalDeadspaceTriglavianExtractionNode)
                        return false;

                    if (IsTarget)
                        return false;

                    if (Distance > Salvage.TractorBeamRange + 10000)
                        return false;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }



        //IsAbyssalPrecursorCache is static and will never change across the life of the Entity
        public bool IsAbyssalPrecursorCache
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsAbyssalPrecursorCache)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isAbyssalPrecursorCache == null)
                        {
                            bool result = false;
                            result |= GroupId == (int)Group.AbyssalBioAdaptiveCache && ESCache.Instance.InAbyssalDeadspace;
                            _isAbyssalPrecursorCache = result;
                            if (DebugConfig.DebugEntityCache)
                                Log.WriteLine("Adding [" + Name + "] to EntityIsAbyssalDeadspaceAoeTowerWeapon as [" + _isAbyssalPrecursorCache + "]");
                            return (bool)_isAbyssalPrecursorCache;
                        }

                        return (bool)_isAbyssalPrecursorCache;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        //IsMobileTractor is static and will never change across the life of the Entity
        public bool IsMobileTractor
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (GroupId == (int)Group.MobileTractor)
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private bool? _isPlayerSpawnLocation;
        public bool IsPlayerSpawnLocation
        {
            get
            {
                if (_isPlayerSpawnLocation == null)
                {
                    if (TypeId == (int)TypeID.PlayerSpawnLocation)
                    {
                        _isPlayerSpawnLocation = true;
                        return (bool)_isPlayerSpawnLocation;
                    }

                    _isPlayerSpawnLocation = false;
                    return (bool)_isPlayerSpawnLocation;
                }

                return (bool)_isPlayerSpawnLocation;
            }
        }

        public bool POSControlTower
        {
            get
            {
                try
                {
                    if (GroupId == (int)Group.POSControlTower)
                    {
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]"); return false;
                }
            }
        }

        public bool IsInTractorRange
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (ESCache.Instance.InAbyssalDeadspace)
                            return true; //If used for targeting in AbyssalDeadspace we want to target it if we dont have any tractors

                        if (!Salvage.TractorBeams.Any())
                            return false;

                        if (Salvage.TractorBeamRange > Distance)
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        private bool? _isInSalvagerRange = null;

        public bool IsInSalvagerRange
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isInSalvagerRange != null)
                            return (bool)_isInSalvagerRange;

                        if (Salvage.Salvagers.Count == 0)
                        {
                            _isInSalvagerRange = false;
                            return (bool)_isInSalvagerRange;
                        }

                        if (Salvage.SalvagerRange > 0 && Salvage.SalvagerRange >= Distance)
                        {
                            _isInSalvagerRange = true;
                            return (bool)_isInSalvagerRange;
                        }

                        _isInSalvagerRange = false;
                        return (bool)_isInSalvagerRange;
                    }

                    _isInSalvagerRange = true;
                    return (bool)_isInSalvagerRange;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        public bool IsReadyForSalvagerToTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (IsReadyToTarget)
                        {
                            if (IsInTractorRange)
                                return true;

                            if (IsInSalvagerRange)
                                return true;

                            if (Distance > (double)Distances.ScoopRange)
                                return true;

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        public bool IsAccelerationGate => _directEntity.IsAccelerationGate;

        public bool IsAbyssalAccelerationGateReadyToBeNavigateOnGridTarget
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsAbyssalAccelerationGateReadyToBeNavigateOnGridTarget)))
                        return false;

                    if (!ESCache.Instance.InAbyssalDeadspace)
                        return false;

                    if (!IsAccelerationGate)
                        return false;

                    //
                    // If we have any moving entities, shoot them first before targeting the BioAdaptiveCache
                    //
                    //if ((ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer) && ESCache.Instance.EntitiesNotSelf.Any(i => i.Velocity > 0))
                    //    return false;


                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.All(i => !i.IsNPCFrigate && !i.IsNPCDestroyer && !i.IsNPCCruiser && !i.IsNPCBattlecruiser && !i.IsNPCBattleship))
                            return true;

                        return false;
                    }

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public DronePriority IsActiveDroneEwarType
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (IsWarpScramblingMe)
                            return DronePriority.WarpScrambler;

                        if (IsWebbingMe)
                            return DronePriority.Webbing;

                        if (IsNeutralizingMe)
                            return DronePriority.PriorityKillTarget;

                        if (IsTryingToJamMe)
                            return DronePriority.PriorityKillTarget;

                        if (IsSensorDampeningMe)
                            return DronePriority.PriorityKillTarget;

                        if (IsTargetPaintingMe)
                            return DronePriority.PriorityKillTarget;

                        if (IsTrackingDisruptingMe)
                            return DronePriority.PriorityKillTarget;

                        return DronePriority.NotUsed;
                    }

                    return DronePriority.NotUsed;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return DronePriority.NotUsed;
                }
            }
        }

        public bool IsActiveTarget => _directEntity.IsActiveTarget;

        public bool IsAnyOtherUnTargetedHighValueTargetInOptimal
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (Combat.PotentialCombatTargets.Count > 0)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => !i.IsTarget && !i.IsTargeting && IsHighValueTarget && i.IsInOptimalRange && i.IsTrackable))
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public bool IsAnyOtherUnTargetedLowValueTargetInOptimal
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (Combat.PotentialCombatTargets.Count > 0)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => !i.IsTarget && !i.IsTargeting && IsLowValueTarget && i.IsInOptimalRange && i.IsTrackable))
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public bool IsApproachedByActiveShip => _directEntity.IsApproachedOrKeptAtRangeByActiveShip;

        public bool IsApproaching => _directEntity.IsApproachingOrKeptAtRange;

        public bool IsAsteroid
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= CategoryId == (int)CategoryID.Asteroid;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsAsteroidBelt
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= GroupId == (int)Group.AsteroidBelt;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsAttacking => _directEntity.IsAttacking;

        //IsBadIdea is static and will never change across the life of the Entity
        public bool IsBadIdea
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsBadIdea)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isBadIdea == null)
                        {
                            if (ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Count > 0)
                            {
                                if (ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Contains(Id))
                                {
                                    _isBadIdea = true;
                                    return (bool)_isBadIdea;
                                }
                            }

                            if (ESCache.Instance.ListEntityIDs_IsBadIdeaFalse.Count > 0)
                            {
                                if (ESCache.Instance.ListEntityIDs_IsBadIdeaFalse.Contains(Id))
                                {
                                    _isBadIdea = false;
                                    return (bool)_isBadIdea;
                                }
                            }

                            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HydraController))
                            {
                                _isBadIdea = false;
                                ESCache.Instance.ListEntityIDs_IsBadIdeaFalse.Add(Id);
                                return (bool)_isBadIdea;
                            }

                            if (ESCache.Instance.InWormHoleSpace)
                            {
                                _isBadIdea = false;
                                ESCache.Instance.ListEntityIDs_IsBadIdeaFalse.Add(Id);
                                return (bool)_isBadIdea;
                            }

                            if (ESCache.Instance.InMission && (IsLargeCollidableWeAlwaysWantToBlowupLast || IsLargeCollidableWeAlwaysWantToBlowupFirst))
                            {
                                if (DebugConfig.DebugIsBadIdea) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: false if (ESCache.Instance.InMission && (IsLargeCollidableWeAlwaysWantToBlowupLast || IsLargeCollidableWeAlwaysWantToBlowupFirst))");
                                return false;
                            }

                            if (IsInMyEveSharpFleet)
                            {
                                if (DebugConfig.DebugIsBadIdea) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: if (IsInMyEveSharpFleet)");
                                _isBadIdea = true;
                                ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Add(Id);
                                return (bool)_isBadIdea;
                            }

                            //if (ESCache.Instance.MyFleetMembersAsEntities != null && ESCache.Instance.MyFleetMembersAsEntities.Any(i => i.Id != null && i.Id == Id))
                            //{
                            //    if (DebugConfig.DebugIsBadIdea) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: if (ESCache.Instance.MyFleetMembersAsEntities.Any(i => i.Id == Id))");
                            //    _isBadIdea = true;
                            //    ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Add(Id);
                            //    return (bool)_isBadIdea;
                            //}

                            if (ESCache.Instance.InAbyssalDeadspace)
                            {
                                if (IsNPCDrone)
                                {
                                    if (DebugConfig.DebugIsBadIdea) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: if (ESCache.Instance.InAbyssalDeadspace) && if (IsNPCDrone)");
                                    _isBadIdea = true;
                                    ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Add(Id);
                                    return (bool)_isBadIdea;
                                }
                            }

                            if (IsPlayer)
                            {
                                if (DebugConfig.DebugIsBadIdea) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: if (IsPlayer)");
                                _isBadIdea = true;
                                ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Add(Id);
                                return (bool)_isBadIdea;
                            }

                            if (IsStation)
                            {
                                if (DebugConfig.DebugIsBadIdea) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: if (IsCitadel)");
                                _isBadIdea = true;
                                ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Add(Id);
                                return (bool)_isBadIdea;
                            }

                            if (IsCitadel)
                            {
                                if (DebugConfig.DebugIsBadIdea) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: if (IsStation)");
                                _isBadIdea = true;
                                ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Add(Id);
                                return (bool)_isBadIdea;
                            }

                            if (GroupId == (int) Group.ConcordDrone
                                || GroupId == (int) Group.PoliceDrone
                                || GroupId == (int) Group.CustomsOfficial
                                || GroupId == (int) Group.Billboard
                                || GroupId == (int) Group.Stargate
                                || GroupId == (int) Group.SentryGun
                                || GroupId == (int) Group.MissionContainer
                                || GroupId == (int) Group.CustomsOffice
                                || GroupId == (int) Group.GasCloud
                                || GroupId == (int) Group.ConcordBillboard
                                || GroupId == (int) Group.Capsule
                            )
                            {
                                if (DebugConfig.DebugIsBadIdea) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: By GroupID [" + GroupId + "]");
                                _isBadIdea = true;
                                ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Add(Id);
                                return _isBadIdea ?? true;
                            }

                            if (_directEntity.IsBlue)
                            {
                                Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: IsBlue [" + _directEntity.IsBlue + "] EffectiveStanding [" + _directEntity.EffectiveStanding + "]");
                                _isBadIdea = true;
                                ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Add(Id);
                                return _isBadIdea ?? true;
                            }

                            /**
                            if (Name.Contains("♦ "))
                            {
                                Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: if (Name.Contains(♦ ))");
                                _isBadIdea = true;
                                ESCache.Instance.ListEntityIDs_IsBadIdeaTrue.Add(Id);
                                return _isBadIdea ?? true;
                            }
                            **/

                            if (DebugConfig.DebugIsBadIdea) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsBadIdea: false");
                            _isBadIdea = false;
                            ESCache.Instance.ListEntityIDs_IsBadIdeaFalse.Add(Id);
                            return _isBadIdea ?? false;
                        }

                        return (bool)_isBadIdea;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsBattlecruiser => _directEntity.IsBattlecruiser;

        public bool IsBattleship => _directEntity.IsBattleship;

        public bool IsMarauder => _directEntity.IsMarauder;

        public bool IsPod => _directEntity.IsPod;

        public bool IsShuttle => _directEntity.IsShuttle;

        public bool IsHauler
        {
            get
            {
                if (GroupId == (int)Group.TransportShip)
                    return true;

                if (GroupId == (int)Group.Industrial)
                    return true;

                if (GroupId == (int)Group.Freighter)
                    return true;

                if (GroupId == (int)Group.JumpFreighter)
                    return true;

                return false;
            }
        }

        public bool IsMiningShip
        {
            get
            {
                if (GroupId == (int) Group.MiningBarge)
                    return true;

                if (GroupId == (int) Group.Exhumer)
                    return true;

                if (TypeId == (int) TypeID.Venture)
                    return true;

                return false;
            }
        }

        public bool IsCelestial
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= CategoryId == (int)CategoryID.Celestial;
                        result |= IsStation;
                        result |= IsMoon;
                        result |= IsAsteroidBelt;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsPlanet
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (GroupId == (int)Group.Planet)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsMoon
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (GroupId == (int)Group.Moon)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }


        public bool IsWithinOptimalOfDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (!ESCache.Instance.InSpace)
                            return false;

                        if (Drones.ActiveDroneCount == 0)
                            return false;

                        if (6000 > _directEntity.DirectAbsolutePosition.GetDistance(Drones.ActiveDrones.FirstOrDefault()._directEntity.DirectAbsolutePosition))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsCloseToDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (!ESCache.Instance.InSpace)
                            return false;

                        if (Drones.ActiveDroneCount == 0)
                            return false;

                        if (10000 > _directEntity.DirectAbsolutePosition.GetDistance(Drones.ActiveDrones.FirstOrDefault()._directEntity.DirectAbsolutePosition))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        public bool IsWithin5KOfOurDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (!ESCache.Instance.InSpace)
                            return false;

                        if (Drones.ActiveDroneCount == 0)
                            return false;

                        if (Drones.ActiveDrones.Any(i => 5000 > i._directEntity.DirectAbsolutePosition.GetDistance(_directEntity.DirectAbsolutePosition)))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsWithin10KOfOurDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (!ESCache.Instance.InSpace)
                            return false;

                        if (Drones.ActiveDroneCount == 0)
                            return false;

                        if (Drones.ActiveDrones.Any(i => 10000 > i._directEntity.DirectAbsolutePosition.GetDistance(_directEntity.DirectAbsolutePosition)))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsWithin15KOfOurDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (!ESCache.Instance.InSpace)
                            return false;

                        if (Drones.ActiveDroneCount == 0)
                            return false;

                        if (Drones.ActiveDrones.Any(i => 15000 > i._directEntity.DirectAbsolutePosition.GetDistance(_directEntity.DirectAbsolutePosition)))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public string stringEwarTypes
        {
            get
            {
                string _stringEwarTypes = string.Empty;
                if (IsNeutralizingMe)
                    _stringEwarTypes = _stringEwarTypes + " IsNeutralizingMe ";
                if (IsWarpScramblingMe)
                    _stringEwarTypes = _stringEwarTypes + " IsWarpScramblingMe ";
                if (IsWebbingMe)
                    _stringEwarTypes = _stringEwarTypes + " IsWebbingMe ";
                if (IsTargetPaintingMe)
                    _stringEwarTypes = _stringEwarTypes + " IsTargetPaintingMe ";
                if (IsSensorDampeningMe)
                    _stringEwarTypes = _stringEwarTypes + " IsSensorDampeningMe ";
                if (IsTrackingDisruptingMe)
                    _stringEwarTypes = _stringEwarTypes + " IsTrackingDisruptingMe ";

                if (_stringEwarTypes == string.Empty)
                {
                    return "None";
                }

                return _stringEwarTypes;
            }
        }

        public bool IsWithin20KOfOurDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (!ESCache.Instance.InSpace)
                            return false;

                        if (Drones.ActiveDroneCount == 0)
                            return false;

                        if (Drones.ActiveDrones.Any(i => 20000 > i._directEntity.DirectAbsolutePosition.GetDistance(_directEntity.DirectAbsolutePosition)))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsWithin25KOfOurDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (!ESCache.Instance.InSpace)
                            return false;

                        if (Drones.ActiveDroneCount == 0)
                            return false;

                        if (Drones.ActiveDrones.Any(i => 25000 > i._directEntity.DirectAbsolutePosition.GetDistance(_directEntity.DirectAbsolutePosition)))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsWithin30KOfOurDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (!ESCache.Instance.InSpace)
                            return false;

                        if (Drones.ActiveDroneCount == 0)
                            return false;

                        if (Drones.ActiveDrones.Any(i => 30000 > i._directEntity.DirectAbsolutePosition.GetDistance(_directEntity.DirectAbsolutePosition)))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsContainer => _directEntity.IsContainer;

        //IsCorrectSizeForMyWeapons is static and will never change across the life of the Entity
        public bool IsCorrectSizeForMyWeapons
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isCorrectSizeForMyWeapons == null)
                        {
                            if (Drones.DronesKillHighValueTargets)
                            {
                                if (IsFrigate)
                                    return false;

                                return true;
                            }

                            if (ESCache.Instance.MyShipEntity.IsFrigate)
                                if (IsFrigate || IsNPCFrigate)
                                {
                                    _isCorrectSizeForMyWeapons = true;
                                    return (bool)_isCorrectSizeForMyWeapons;
                                }

                            if (ESCache.Instance.MyShipEntity.IsCruiser)
                                if (IsCruiser || IsNPCCruiser)
                                {
                                    _isCorrectSizeForMyWeapons = true;
                                    return (bool)_isCorrectSizeForMyWeapons;
                                }

                            if (ESCache.Instance.MyShipEntity.IsBattlecruiser || ESCache.Instance.MyShipEntity.IsBattleship)
                                if (IsBattleship || IsBattlecruiser || IsNPCBattlecruiser || IsNPCBattleship)
                                {
                                    _isCorrectSizeForMyWeapons = true;
                                    return (bool)_isCorrectSizeForMyWeapons;
                                }

                            return false;
                        }

                        return (bool)_isCorrectSizeForMyWeapons;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                }

                return false;
            }
        }

        public bool IsCruiser => _directEntity.IsCruiser;

        public bool IsCurrentTarget
        {
            get
            {
                if (Combat.CurrentWeaponTarget() == null)
                    return false;

                if (Combat.CurrentWeaponTarget().Id == Id)
                    return true;

                return false;
            }
        }

        //IsCustomsOffice is static and will never change across the life of the Entity
        public bool IsCustomsOffice
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= GroupId == (int)Group.CustomsOffice;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsDestroyer => _directEntity.IsDestroyer;
        public bool IsDronePriorityTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (Drones.DronePriorityTargets.All(i => i.EntityID != Id))
                            return false;

                        return true;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsEntityIShouldKeepShooting
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isEntityIShouldKeepShooting == null)
                        {
                            if (IsReadyToShoot
                                && IsInOptimalRange && !IsLargeCollidable
                                && ((!IsFrigate && !IsNPCFrigate) || !IsTooCloseTooFastTooSmallToHit)
                                && ArmorPct * 100 < Combat.DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage)
                            {
                                if (DebugConfig.DebugCombat)
                                    Log.WriteLine("[" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + " GroupID [" + GroupId +
                                                  "]] has less than 60% armor, keep killing this target");
                                _isEntityIShouldKeepShooting = true;
                                return (bool)_isEntityIShouldKeepShooting;
                            }

                            _isEntityIShouldKeepShooting = false;
                            return (bool)_isEntityIShouldKeepShooting;
                        }

                        return (bool)_isEntityIShouldKeepShooting;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception: [" + ex + "]");
                }

                return false;
            }
        }

        public bool IsEntityIShouldKeepShootingWithDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isEntityIShouldKeepShootingWithDrones == null)
                        {
                            if (IsReadyForDronesToShoot
                                && IsInDroneRange
                                && !IsLargeCollidable
                                && (IsFrigate || IsNPCFrigate || Drones.DronesKillHighValueTargets)
                                && ShieldPct * 100 < 80)
                            {
                                if (DebugConfig.DebugDrones)
                                    Log.WriteLine("[" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + " GroupID [" + GroupId +
                                                  "]] has less than 60% armor, keep killing this target");
                                _isEntityIShouldKeepShootingWithDrones = true;
                                return (bool)_isEntityIShouldKeepShootingWithDrones;
                            }

                            _isEntityIShouldKeepShootingWithDrones = false;
                            return (bool)_isEntityIShouldKeepShootingWithDrones;
                        }

                        return (bool)_isEntityIShouldKeepShootingWithDrones;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception: [" + ex + "]");
                }

                return false;
            }
        }

        public bool IsEntityIShouldLeaveAlone => _directEntity.IsEntityIShouldLeaveAlone;

        public bool IsEwarImmune => _directEntity.IsEwarImmune;

        public bool IsEwarTarget => _directEntity.IsEwarTarget;

        public bool KillSentries
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsSentry && IsPrimaryWeaponPriorityTarget)
                        return true;

                    if (IsSentry && IsDronePriorityTarget)
                        return true;

                    if (Combat.KillSentries)
                        return true;

                    if (ESCache.Instance.InAnomaly)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsFactionWarfareNPC => _directEntity.IsFactionWarfareNPC;

        public bool IsFrigate => _directEntity.IsFrigate;

        public bool IsGasCloud
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= GroupId == (int)Group.GasCloud;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsHigherPriorityPresent
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isHigherPriorityPresent == null)
                            if (Combat.PrimaryWeaponPriorityTargets.Count > 0 || Drones.DronePriorityTargets.Count > 0)
                            {
                                if (Combat.PrimaryWeaponPriorityTargets.Count > 0)
                                {
                                    if (Combat.PrimaryWeaponPriorityTargets.Any(pt => pt.EntityID == Id))
                                    {
                                        PrimaryWeaponPriority _currentPrimaryWeaponPriority =
                                            Combat.PrimaryWeaponPriorityEntities.Where(t => t.Id == _directEntity.Id)
                                                .Select(pt => pt.PrimaryWeaponPriorityLevel)
                                                .FirstOrDefault();

                                        if (
                                            !Combat.PrimaryWeaponPriorityEntities.All(
                                                pt => pt.PrimaryWeaponPriorityLevel < _currentPrimaryWeaponPriority && pt.Distance < Combat.MaxRange))
                                        {
                                            _isHigherPriorityPresent = true;
                                            return (bool)_isHigherPriorityPresent;
                                        }

                                        _isHigherPriorityPresent = false;
                                        return (bool)_isHigherPriorityPresent;
                                    }

                                    if (Combat.PrimaryWeaponPriorityEntities.Any(e => e.Distance < Combat.MaxRange))
                                    {
                                        _isHigherPriorityPresent = true;
                                        return (bool)_isHigherPriorityPresent;
                                    }

                                    _isHigherPriorityPresent = false;
                                    return (bool)_isHigherPriorityPresent;
                                }

                                if (Drones.DronePriorityTargets.Count > 0)
                                {
                                    if (Drones.DronePriorityTargets.Any(pt => pt.EntityID == _directEntity.Id))
                                    {
                                        DronePriority _currentEntityDronePriority =
                                            Drones.DronePriorityEntities.Where(t => t.Id == _directEntity.Id)
                                                .Select(pt => pt.DronePriorityLevel)
                                                .FirstOrDefault();

                                        if (
                                            !Drones.DronePriorityEntities.All(
                                                pt => pt.DronePriorityLevel < _currentEntityDronePriority && pt.Distance < Drones.MaxDroneRange))
                                            return true;

                                        return false;
                                    }

                                    if (Drones.DronePriorityEntities.Any(e => e.Distance < Drones.MaxDroneRange))
                                    {
                                        _isHigherPriorityPresent = true;
                                        return (bool)_isHigherPriorityPresent;
                                    }

                                    _isHigherPriorityPresent = false;
                                    return (bool)_isHigherPriorityPresent;
                                }

                                _isHigherPriorityPresent = false;
                                return (bool)_isHigherPriorityPresent;
                            }

                        return _isHigherPriorityPresent ?? false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsHighValueTargetThatIsTargeted
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (!IsLowValueTarget && (IsTarget || IsTargeting))
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsHighValueTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isHighValueTarget == null)
                        {
                            if (ESCache.Instance.DictionaryIsHighValueTarget.Count > 0 && ESCache.Instance.DictionaryIsHighValueTarget.Count > DictionaryCountThreshold)
                                if (DebugConfig.DebugEntityCache)
                                    Log.WriteLine("We have [" + ESCache.Instance.DictionaryIsHighValueTarget.Count +
                                                  "] Entities in Cache.Instance.DictionaryIsHighValueTarget");

                            if (ESCache.Instance.DictionaryIsHighValueTarget.Count > 0)
                            {
                                bool value;
                                if (ESCache.Instance.DictionaryIsHighValueTarget.TryGetValue(Id, out value))
                                {
                                    _isHighValueTarget = value;
                                    return (bool)_isHighValueTarget;
                                }
                            }

                            if (TargetValue != null)
                            {
                                if (!IsIgnored && !IsContainer && !IsBadIdea && !IsCustomsOffice && !IsFactionWarfareNPC && !IsPlayer)
                                    if (TargetValue >= Combat.MinimumTargetValueToConsiderTargetAHighValueTarget)
                                    {
                                        if (IsSentry && !KillSentries && !IsEwarTarget)
                                        {
                                            _isHighValueTarget = false;
                                            if (DebugConfig.DebugEntityCache)
                                                Log.WriteLine("Adding [" + Name + "] to DictionaryIsHighValueTarget as [" + _isHighValueTarget + "]");
                                            return (bool)_isHighValueTarget;
                                        }

                                        _isHighValueTarget = true;
                                        if (DebugConfig.DebugEntityCache)
                                            Log.WriteLine("Adding [" + Name + "] to DictionaryIsHighValueTarget as [" + _isHighValueTarget + "]");
                                        ESCache.Instance.DictionaryIsHighValueTarget.AddOrUpdate(Id, (bool)_isHighValueTarget);
                                        return (bool)_isHighValueTarget;
                                    }

                                _isHighValueTarget = false;
                                return (bool)_isHighValueTarget;
                            }

                            _isHighValueTarget = false;
                            if (IsNPCBattleship || IsNPCBattlecruiser)
                                _isHighValueTarget = true;

                            if (IsNPCCruiser && ESCache.Instance.EntitiesOnGrid.All(i => !i.IsNPCBattleship && !i.IsNPCBattlecruiser))
                                _isHighValueTarget = true;

                            if (DebugConfig.DebugEntityCache)
                                Log.WriteLine("Adding [" + Name + "] to DictionaryIsHighValueTarget as [" + _isHighValueTarget + "]");
                            ESCache.Instance.DictionaryIsHighValueTarget.AddOrUpdate(Id, (bool)_isHighValueTarget);
                            return (bool)_isHighValueTarget;
                        }

                        return (bool)_isHighValueTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsIgnored
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (IsWarpScramblingMe) return false;

                        if (_isIgnored == null)
                        {
                            if (ActionControl.IgnoreTargets != null && ActionControl.IgnoreTargets.Count > 0)
                            {
                                _isIgnored = ActionControl.IgnoreTargets.Contains(Name.Trim());
                                if ((bool)_isIgnored)
                                {
                                    if (Combat.PreferredPrimaryWeaponTarget != null && Combat.PreferredPrimaryWeaponTarget.Id != Id)
                                        Combat.PreferredPrimaryWeaponTarget = null;

                                    if (ESCache.Instance.DictionaryIsLowValueTarget.ContainsKey(Id))
                                        ESCache.Instance.DictionaryIsLowValueTarget.Remove(Id);

                                    if (ESCache.Instance.DictionaryIsHighValueTarget.ContainsKey(Id))
                                        ESCache.Instance.DictionaryIsHighValueTarget.Remove(Id);

                                    if (DebugConfig.DebugEntityCache)
                                        Log.WriteLine("[" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "] isIgnored [" +
                                                      _isIgnored +
                                                      "]");
                                    return (bool)_isIgnored;
                                }

                                if (IsSentry && !KillSentries)
                                    return true;

                                _isIgnored = false;
                                if (DebugConfig.DebugEntityCache)
                                    Log.WriteLine("[" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "] isIgnored [" + _isIgnored +
                                                  "]");
                                return (bool)_isIgnored;
                            }

                            _isIgnored = false;
                            if (DebugConfig.DebugEntityCache)
                                Log.WriteLine("[" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "] isIgnored [" + _isIgnored + "]");
                            return (bool)_isIgnored;
                        }

                        if (DebugConfig.DebugEntityCache)
                            Log.WriteLine("[" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "] isIgnored [" + _isIgnored + "]");
                        return (bool)_isIgnored;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsInDroneRange
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsInDroneRange)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (!Drones.UseDrones)
                            return IsInRangeOfWeapons;

                        if (Drones.MaxDroneRange > 0)
                        {
                            if (Distance < Drones.MaxDroneRange)
                                return true;

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsInRangeOfAnyAmmoWeHaveWithUs
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsInRangeOfAnyAmmoWeHaveWithUs)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))
                        {
                            if (5000 > Distance)
                                return true;
                        }

                        if (Combat.UsableAmmoInCargo.Any())
                        {
                            float MaxRangeOfLongRangeAmmo = (float)Combat.UsableAmmoInCargo.OrderByDescending(x => x.MaxRange).FirstOrDefault().AmmoType.Range;
                            if (MaxRangeOfLongRangeAmmo > Distance)
                                return true;

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsInRangeOfWeapons
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsInRangeOfWeapons)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (ESCache.Instance.Weapons.Any())
                        {
                            if (Combat.MaxWeaponRange > 0)
                            {
                                if (Combat.MaxWeaponRange > Distance)
                                    return true;

                                return false;
                            }
                        }

                        if (ESCache.Instance.MiningEquipment.Any())
                        {
                            if (15000 > Distance)
                                return true;

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsInMyFleet
        {
            get
            {
                if (!IsPlayer)
                    return false;

                foreach (var fleetMember in ESCache.Instance.MyFleetMembersAsEntities)
                {
                    if (fleetMember.Id == Id)
                        return true;
                }

                return false;
            }
        }

        //IsInMyEveSharpFleet is static and will never change across the life of the Entity
        public bool IsInMyEveSharpFleet
        {
            get
            {
                if (ESCache.Instance.EveAccount.LeaderCharacterName == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName1 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName2 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName3 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName4 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName5 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName6 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName7 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName8 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName9 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName10 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName11 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName12 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName13 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName14 == Name)
                    return true;

                if (ESCache.Instance.EveAccount.SlaveCharacterName15 == Name)
                    return true;

                return false;
            }
        }

        //IsInMyRepairGroup is static and will never change across the life of the Entity
        public bool IsInMyRepairGroup
        {
            get
            {
                if (IsInMyEveSharpFleet)
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (ESCache.Instance.MyCorpMatesAsEntities.Any(i => i.Id == Id) || IsInMyEveSharpFleet)");
                    if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.RepairGroup))
                    {
                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("My RepairGroup not found: we will not repair things based on the repair group if it is not set in the launcher!");
                        return false;
                    }

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName))
                    {
                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName))");
                        if (ESCache.Instance.EveAccount.LeaderCharacterName == Name)
                        {
                            if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (ESCache.Instance.EveAccount.LeaderCharacterName == Name)");
                            if (ESCache.Instance.EveAccount.LeaderRepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (ESCache.Instance.EveAccount.LeaderRepairGroup == ESCache.Instance.EveAccount.RepairGroup)");
                                return true;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter1RepairGroup))
                    {
                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter1RepairGroup))");
                        if (ESCache.Instance.EveAccount.SlaveCharacterName1 == Name)
                        {
                            if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (ESCache.Instance.EveAccount.SlaveCharacterName1 == Name)");
                            if (ESCache.Instance.EveAccount.SlaveCharacter1RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (ESCache.Instance.EveAccount.SlaveCharacter1RepairGroup == ESCache.Instance.EveAccount.RepairGroup)");
                                return true;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter2RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName2 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter2RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter3RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName3 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter3RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter4RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName4 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter4RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter5RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName5 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter5RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter6RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName6 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter6RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter7RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName7 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter7RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter8RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName8 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter8RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter9RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName9 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter9RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter10RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName10 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter10RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter11RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName11 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter11RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter12RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName12 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter12RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter13RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName13 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter13RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter14RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName14 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter14RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacter15RepairGroup) &&
                        ESCache.Instance.EveAccount.SlaveCharacterName15 == Name &&
                        ESCache.Instance.EveAccount.SlaveCharacter15RepairGroup == ESCache.Instance.EveAccount.RepairGroup)
                        return true;

                    return false;
                }

                return false;
            }
        }

        private bool? IsInOptimalRangeForProjectileGuns
        {
            get
            {
                if (Combat.DoWeCurrentlyProjectilesMounted())
                {
                    try
                    {
                        if (IsTrackable)
                        {
                            //Can we fix this to allow for mixed weapon systems? fixme
                            if (Distance < ESCache.Instance.Weapons.FirstOrDefault().OptimalRange + (ESCache.Instance.Weapons.FirstOrDefault().FallOff * .75) && Distance < Combat.MaxRange)
                            {
                                if (AmmoManagementBehavior.CachedAmmoTypeCurrentlyLoaded != null && AmmoManagementBehavior.CachedAmmoTypeCurrentlyLoaded.Range >= Distance)
                                {
                                    return true;
                                }

                                //
                                // we may be in optimal range here, but we defined the ammo as not reaching this far, so pay attention to what we are being told and assume we are not in optinalrange...
                                //
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

                return null;
            }
        }

        private bool? IsInOptimalRangeForTurrets
        {
            get
            {
                if (Combat.DoWeCurrentlyHaveTurretsMounted())
                {
                    try
                    {
                        if (IsTrackable)
                        {
                            //Can we fix this to allow for mixed weapon systems? fixme
                            if (Distance < ESCache.Instance.Weapons.FirstOrDefault().OptimalRange && Distance < Combat.MaxRange)
                            {
                                try
                                {
                                    if (AmmoManagementBehavior.CachedAmmoTypeCurrentlyLoaded != null && AmmoManagementBehavior.CachedAmmoTypeCurrentlyLoaded.Range > Distance)
                                    {
                                        return true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteLine("Exception [" + ex + "]");
                                    return true;
                                }

                                //
                                // we may be in optimal range here, but we defined the ammo as not reaching this far, so pay attention to what we are being told and assume we not not in optinalrange...
                                //
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

                return null;
            }
        }

        private bool? IsInOptimalRangeForMissiles
        {
            get
            {
                try
                {
                    if (ESCache.Instance.Weapons.Any(i => i.IsMissileLauncher))
                    {
                        //
                        // this has to be missiles, if its in range its in optimalrange
                        //
                        if (Distance < Combat.MaxRange)
                        {
                            if (AmmoManagementBehavior.CachedAmmoTypeCurrentlyLoaded != null && AmmoManagementBehavior.CachedAmmoTypeCurrentlyLoaded.Range > Distance)
                            {
                                return true;
                            }

                            //
                            // we may be in optimal range here, but we defined the ammo as not reaching this far, so pay attention to what we are being told and assume we not not in optinalrange...
                            //
                            return false;
                        }

                        return false;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public bool IsInOptimalRange
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsInOptimalRange)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isInOptimalRange == null)
                        {
                            if (Drones.DronesKillHighValueTargets)
                                if (IsInDroneRange)
                                    return true;

                            if (ESCache.Instance.Weapons.Count > 0)
                            {
                                bool? _IsInOptimalRangeForProjectileGuns = IsInOptimalRangeForProjectileGuns;
                                if (_IsInOptimalRangeForProjectileGuns != null)
                                {
                                    if ((bool)_IsInOptimalRangeForProjectileGuns)
                                    {
                                        _isInOptimalRange = true;
                                        return _isInOptimalRange ?? false;
                                    }

                                    _isInOptimalRange = false;
                                    return _isInOptimalRange ?? false;
                                }

                                bool? _IsInOptimalRangeForTurrets = IsInOptimalRangeForTurrets;
                                if (_IsInOptimalRangeForTurrets != null)
                                {
                                    if ((bool)_IsInOptimalRangeForTurrets)
                                    {
                                        _isInOptimalRange = true;
                                        return _isInOptimalRange ?? false;
                                    }

                                    _isInOptimalRange = false;
                                    return _isInOptimalRange ?? false;
                                }

                                bool? _IsInOptimalRangeForMissiles = IsInOptimalRangeForMissiles;
                                if (_IsInOptimalRangeForMissiles != null)
                                {
                                    //Missiles
                                    if ((bool)IsInOptimalRangeForMissiles)
                                    {
                                        _isInOptimalRange = true;
                                        return _isInOptimalRange ?? false;
                                    }

                                    _isInOptimalRange = false;
                                    return _isInOptimalRange ?? false;
                                }

                                //
                                // What kind of weapon is this?
                                //
                                int intCount = 0;
                                foreach (var thisWeapon in ESCache.Instance.Weapons)
                                {
                                    intCount++;
                                    Log.WriteLine("[" + intCount + "][" + thisWeapon.TypeName + "] TypeID [" + thisWeapon.TypeId + "] GroupID [" + thisWeapon.GroupId + "] What kind of weapon is this? fixme");
                                }
                            }

                            _isInOptimalRange = false;
                            return (bool)_isInOptimalRange;
                        }

                        return (bool)_isInOptimalRange;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsInOptimalRangeOrNothingElseAvail
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (IsInOptimalRange)
                            return true;

                        //
                        // this is used by the targeting routine to determine what to target!
                        //
                        if (!IsTarget && !IsContainer)
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsInWebRange
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (ESCache.Instance.Modules.Any(i => i.GroupId == (int)Group.StasisWeb))
                        {
                            ModuleCache web = ESCache.Instance.Modules.Find(i => i.GroupId == (int)Group.StasisWeb);
                            if (Distance > web.MaxRange)
                                return false;

                            return true;
                        }

                        //
                        // if we dont have any webs rturn true!
                        //
                        return true;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTryingToJamMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("electronic"))
                        {
                            if (!ESCache.Instance.ListOfJammingEntities.Contains(Id)) ESCache.Instance.ListOfJammingEntities.Add(Id);
                            return true;
                        }

                        if (ESCache.Instance.ListOfJammingEntities.Contains(Id))
                            return true;

                        if (_directEntity.IsTryingToJamMe)
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        //IsLargeCollidable is static and will never change across the life of the Entity
        public bool IsLargeCollidable => _directEntity.IsLargeCollidable;

        public bool IsLargeCollidableWeAlwaysWantToBlowupLast
        {
            get
            {
                if (_directEntity.IsLargeCollidable)
                {
                    if (ESCache.Instance.InMission && MissionSettings.MyMission != null)
                    {
                        if (MissionSettings.MyMission.Name.Contains("In the Midst of Deadspace (3 of 5)") && Name.ToLower() == "EM Forcefield".ToLower())
                            return true;

                        if (MissionSettings.MyMission.Name.Contains("In the Midst of Deadspace (3 of 5)") && Name.ToLower() == "Imperial Armory".ToLower() && ESCache.Instance.EntitiesOnGrid.All(i => i.Name.ToLower() != "EM Forecefield".ToLower()))
                            return true;

                        if (MissionSettings.MyMission.Name.Contains("In the Midst of Deadspace (4 of 5)") && Name.ToLower() == "Imperial Stargate".ToLower())
                            return true;

                        if (MissionSettings.MyMission.Name.Contains("In the Midst of Deadspace (5 of 5)") && Name.ToLower() == "Caldari Manufacturing Plant".ToLower())
                            return true;

                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                        {
                            if (Name.Contains("Tahmar") || Name.Contains("Steon") || Name.Contains("Tahamar") || Name.Contains(" Outpost"))
                                return true;
                        }
                    }

                    return false;
                }

                if (GroupId == (int)Group.DeadSpaceOverseersStructure )
                {
                    return true;
                }

                if (GroupId == (int)Group.DeadSpaceOverseersSentry)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsLargeCollidableWeAlwaysWantToBlowupFirst
        {
            get
            {
                if (GroupId == (int)Group.DeadSpaceOverseersStructure || GroupId == (int)Group.DeadSpaceOverseersBelongings)
                {
                    if (ESCache.Instance.InMission && MissionSettings.MyMission != null)
                    {
                        if (MissionSettings.MyMission.Name.ToLower().Contains("Shipyard Theft".ToLower()) && Name == "Silo")
                            return true;
                    }

                    if (Name == "Radiating Telescope" && ESCache.Instance.Entities.Any(i => i.Name == "Supply Crate"))
                        return true;
                }

                return false;
            }
        }

        public bool IsMinable
        {
            get
            {
                if (ESCache.Instance.ActiveShip.HasGasHarvesters)
                {
                    if (IsGasCloud)
                    {
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.HasMiningLasers)
                {
                    if (IsAsteroid && !Name.Contains("Mercoxit"))
                    {
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.HasMiningLasersForMercoxit)
                {
                    if (IsAsteroid && Name.Contains("Mercoxit"))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public bool IsDecloakedTransmissionRelay => _directEntity.IsDecloakedTransmissionRelay;
        public bool IsLargeWreck => _directEntity.IsLargeWreck;

        public bool IsEntityDronesAreShooting
        {
            get
            {
                if (Drones.ActiveDrones.Any(i => i.FollowId == Id))
                    return true;

                return false;
            }
        }

        public bool IsLowestHealthNpcWithThisSameName
        {
            get
            {
                EntityCache LowestHealthNPCWithThisSameName = Combat.PotentialCombatTargets.Where(i => i.Name == Name)
                    .OrderByDescending(x => !x.IsTargetedBy)
                    .ThenByDescending(x => !x.IsAttacking)
                    .ThenBy(x => x.Health)
                    .FirstOrDefault();

                if (LowestHealthNPCWithThisSameName != null)
                {
                    if (LowestHealthNPCWithThisSameName.Id == Id && Health != 1)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsLastTargetDronesWereShooting
        {
            get
            {
                if (Drones.ActiveDrones.Any(i => i.IsEntityDronesAreShooting))
                    if (Drones.ActiveDrones.Find(i => i.IsEntityDronesAreShooting).Name == Name)
                    {
                        if (IsEntityDronesAreShooting)
                            return true;

                        return false;
                    }

                if (Drones.LastTargetIDDronesEngaged != null && Id == Drones.LastTargetIDDronesEngaged)
                    return true;

                return false;
            }
        }

        public bool IsLastTargetPrimaryWeaponsWereShooting => Combat.LastTargetPrimaryWeaponsWereShooting != null && Id == Combat.LastTargetPrimaryWeaponsWereShooting.Id;

        public bool IsLootTarget => ESCache.Instance.ListofContainersToLoot.Contains(Id);

        public bool IsLowValueTargetThatIsTargeted
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsLowValueTarget && (IsTarget || IsTargeting))
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsLowValueTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isLowValueTarget == null)
                        {
                            if (ESCache.Instance.DictionaryIsLowValueTarget.Count > DictionaryCountThreshold)
                                if (DebugConfig.DebugEntityCache)
                                    Log.WriteLine("We have [" + ESCache.Instance.DictionaryIsLowValueTarget.Count +
                                                  "] Entities in Cache.Instance.DictionaryIsLowValueTarget");

                            if (ESCache.Instance.DictionaryIsLowValueTarget.Count > 0)
                            {
                                bool value;
                                if (ESCache.Instance.DictionaryIsLowValueTarget.TryGetValue(Id, out value))
                                {
                                    _isLowValueTarget = value;
                                    return (bool)_isLowValueTarget;
                                }
                            }

                            if (!IsIgnored && !IsContainer && !IsBadIdea && !IsCustomsOffice && !IsFactionWarfareNPC && !IsPlayer)
                            {
                                if (TargetValue != null && TargetValue <= Combat.MaximumTargetValueToConsiderTargetALowValueTarget)
                                {
                                    if (IsSentry && !KillSentries && !IsEwarTarget)
                                    {
                                        _isLowValueTarget = false;
                                        if (DebugConfig.DebugEntityCache)
                                            Log.WriteLine("Adding [" + Name + "] to DictionaryIsLowValueTarget as [" + _isLowValueTarget + "]");
                                        return (bool)_isLowValueTarget;
                                    }

                                    if (TargetValue < 0 && Velocity == 0)
                                    {
                                        _isLowValueTarget = false;
                                        if (DebugConfig.DebugEntityCache)
                                            Log.WriteLine("Adding [" + Name + "] to DictionaryIsLowValueTarget as [" + _isLowValueTarget + "]");
                                        return (bool)_isLowValueTarget;
                                    }

                                    _isLowValueTarget = true;
                                    if (DebugConfig.DebugEntityCache)
                                        Log.WriteLine("Adding [" + Name + "] to DictionaryIsLowValueTarget as [" + _isLowValueTarget + "]");
                                    ESCache.Instance.DictionaryIsLowValueTarget.AddOrUpdate(Id, (bool)_isLowValueTarget);
                                    return (bool)_isLowValueTarget;
                                }

                                _isLowValueTarget = false;
                                if (DebugConfig.DebugEntityCache)
                                    Log.WriteLine("Adding [" + Name + "] to DictionaryIsLowValueTarget as [" + _isLowValueTarget + "]");
                                return (bool)_isLowValueTarget;
                            }

                            _isLowValueTarget = false;
                            return (bool)_isLowValueTarget;
                        }

                        return (bool)_isLowValueTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsMediumWreck => _directEntity.IsMediumWreck;

        public bool IsMiscJunk => _directEntity.IsMiscJunk;

        public bool IsNeutralizingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("ewEnergyNeut"))
                        {
                            if (!ESCache.Instance.ListNeutralizingEntities.Contains(Id)) ESCache.Instance.ListNeutralizingEntities.Add(Id);
                            return true;
                        }

                        if (ESCache.Instance.ListNeutralizingEntities.Contains(Id))
                            return true;

                        if (ESCache.Instance.InAbyssalDeadspace && (Name.Contains("Starving") || Name.Contains("Nullcharge") || Name.Contains("Dissipator") || Name.Contains("Firewatcher") || Name.Contains("Sentinel")))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsNotYetTargetingMeAndNotYetTargeted
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= (IsNpc || IsNpcByGroupID) && !IsTargeting && !IsTarget && !IsContainer && CategoryId == (int)CategoryID.Entity &&
                                  Distance < Combat.MaxTargetRange && !IsIgnored && !IsBadIdea && !IsTargetedBy && !IsEntityIShouldLeaveAlone &&
                                  !IsFactionWarfareNPC && !IsLargeCollidable && !IsStation && !IsCitadel;

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        private bool? _isEntityEngagedByMyOtherToons = null;

        private bool? _isPossibleToDropFactionModules = null;

        public bool IsPossibleToDropFactionModules
        {
            get
            {
                try
                {
                    if (_isPossibleToDropFactionModules != null)
                        return _isPossibleToDropFactionModules ?? false;

                    if (Name.Contains(DirectNpcInfo.AngelCartelFaction.HighTierNameWillContain))
                    {
                        _isPossibleToDropFactionModules = true;
                        return _isPossibleToDropFactionModules ?? true;
                    }

                    if (Name.Contains(DirectNpcInfo.BloodRaiderCovenantFaction.HighTierNameWillContain))
                    {
                        _isPossibleToDropFactionModules = true;
                        return _isPossibleToDropFactionModules ?? true;
                    }

                    if (Name.Contains(DirectNpcInfo.GuristasPiratesFaction.HighTierNameWillContain))
                    {
                        _isPossibleToDropFactionModules = true;
                        return _isPossibleToDropFactionModules ?? true;
                    }

                    if (Name.Contains(DirectNpcInfo.SanshasNationFaction.HighTierNameWillContain))
                    {
                        _isPossibleToDropFactionModules = true;
                        return _isPossibleToDropFactionModules ?? true;
                    }

                    if (Name.Contains(DirectNpcInfo.SerpentisFaction.HighTierNameWillContain))
                    {
                        _isPossibleToDropFactionModules = true;
                        return _isPossibleToDropFactionModules ?? true;
                    }

                    if (Name.ToLower().Contains("Personal effects".ToLower()))
                    {
                        _isPossibleToDropFactionModules = true;
                        return _isPossibleToDropFactionModules ?? true;
                    }

                    if (Name.ToLower().Contains("Overseer".ToLower()))
                    {
                        _isPossibleToDropFactionModules = true;
                        return _isPossibleToDropFactionModules ?? true;
                    }

                    if (Name.ToLower().Contains("Officer".ToLower()))
                    {
                        _isPossibleToDropFactionModules = true;
                        return _isPossibleToDropFactionModules ?? true;
                    }

                    // todo
                    // Add Officer names
                    // Add Deadspace NPCs

                    _isPossibleToDropFactionModules = false;
                    return _isPossibleToDropFactionModules ?? false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public bool IsEntityEngagedByMyOtherToons
        {
            get
            {
                if (_isEntityEngagedByMyOtherToons == null)
                {
                    if (ESCache.Instance.EveAccount.EntityIdsEngagedByOthers.Count > 0)
                    {
                        foreach (long entityIdEngagedByOthers in ESCache.Instance.EveAccount.EntityIdsEngagedByOthers)
                        {
                            if (entityIdEngagedByOthers == Id)
                            {
                                _isEntityEngagedByMyOtherToons = true;
                                return (bool)_isEntityEngagedByMyOtherToons;
                            }
                        }

                        _isEntityEngagedByMyOtherToons = false;
                        return (bool)_isEntityEngagedByMyOtherToons;
                    }

                    _isEntityEngagedByMyOtherToons = false;
                    return (bool)_isEntityEngagedByMyOtherToons;
                }

                return (bool)_isEntityEngagedByMyOtherToons;
            }
        }

        public bool IsNpc => _directEntity.IsNpc;

        public bool IsNPCBattlecruiser => _directEntity.IsNPCBattlecruiser;

        public bool IsNpcCapitalEscalation => _directEntity.IsNpcCapitalEscalation;

        public bool IsNPCBattleship => _directEntity.IsNPCBattleship;

        public bool IsNpcByGroupID => _directEntity.IsNpcByGroupID;
        public bool IsNPCCapitalShip => _directEntity.IsNPCCapitalShip;

        public bool IsNPCCruiser => _directEntity.IsNPCCruiser;

        public bool IsNPCDestroyer => _directEntity.IsNPCDestroyer;

        public bool IsNPCDrone => _directEntity.IsNPCDrone;

        public bool IsNPCFrigate => _directEntity.IsNPCFrigate;

        public bool IsOnGridWithMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isOnGridWithMe == null)
                        {
                            if (IsMoon || IsPlanet)
                            {
                                if (Distance < (double)Distances.OneAu / 10) //10th of an AU
                                {
                                    _isOnGridWithMe = true;
                                    return (bool)_isOnGridWithMe;
                                }

                                return false;
                            }

                            if (Distance < (double)Distances.OnGridWithMe)
                            {
                                _isOnGridWithMe = true;
                                return (bool)_isOnGridWithMe;
                            }

                            _isOnGridWithMe = false;
                            return (bool)_isOnGridWithMe;
                        }

                        return (bool)_isOnGridWithMe;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsOrbitedByActiveShip => _directEntity.IsOrbitedByActiveShip;

        public bool IsOrbiting => _directEntity.IsOrbiting;

        public bool IsOreOrIce
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= GroupId == (int)Group.Plagioclase;
                        result |= GroupId == (int)Group.Spodumain;
                        result |= GroupId == (int)Group.Kernite;
                        result |= GroupId == (int)Group.Hedbergite;
                        result |= GroupId == (int)Group.Arkonor;
                        result |= GroupId == (int)Group.Bistot;
                        result |= GroupId == (int)Group.Pyroxeres;
                        result |= GroupId == (int)Group.Crokite;
                        result |= GroupId == (int)Group.Jaspet;
                        result |= GroupId == (int)Group.Omber;
                        result |= GroupId == (int)Group.Scordite;
                        result |= GroupId == (int)Group.Gneiss;
                        result |= GroupId == (int)Group.Veldspar;
                        result |= GroupId == (int)Group.Hemorphite;
                        result |= GroupId == (int)Group.DarkOchre;
                        result |= GroupId == (int)Group.Ice;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsVeldspar
        {
            get
            {
                if (GroupId == (int)Group.Veldspar)
                    return true;

                return false;
            }
        }

        public bool IsPlagioclase
        {
            get
            {
                if (GroupId == (int)Group.Plagioclase)
                    return true;

                return false;
            }
        }

        public bool IsSpodumain
        {
            get
            {
                if (GroupId == (int)Group.Spodumain)
                    return true;

                return false;
            }
        }

        public bool IsKernite
        {
            get
            {
                if (GroupId == (int)Group.Kernite)
                    return true;

                return false;
            }
        }

        public bool IsHedbergite
        {
            get
            {
                if (GroupId == (int)Group.Hedbergite)
                    return true;

                return false;
            }
        }

        public bool IsArkonor
        {
            get
            {
                if (GroupId == (int)Group.Arkonor)
                    return true;

                return false;
            }
        }

        public bool IsBistot
        {
            get
            {
                if (GroupId == (int)Group.Bistot)
                    return true;

                return false;
            }
        }

        public bool IsPyroxeres
        {
            get
            {
                if (GroupId == (int)Group.Pyroxeres)
                    return true;

                return false;
            }
        }

        public bool IsCrokite
        {
            get
            {
                if (GroupId == (int)Group.Crokite)
                    return true;

                return false;
            }
        }

        public bool IsJaspet
        {
            get
            {
                if (GroupId == (int)Group.Jaspet)
                    return true;

                return false;
            }
        }

        public bool IsOmber
        {
            get
            {
                if (GroupId == (int)Group.Omber)
                    return true;

                return false;
            }
        }

        public bool IsScordite
        {
            get
            {
                if (GroupId == (int)Group.Scordite)
                    return true;

                return false;
            }
        }

        public bool IsGneiss
        {
            get
            {
                if (GroupId == (int)Group.Gneiss)
                    return true;

                return false;
            }
        }

        public bool IsHemorphite
        {
            get
            {
                if (GroupId == (int)Group.Hemorphite)
                    return true;

                return false;
            }
        }

        public bool IsDarkOchre
        {
            get
            {
                if (GroupId == (int)Group.DarkOchre)
                    return true;

                return false;
            }
        }

        public bool IsIce
        {
            get
            {
                if (GroupId == (int)Group.Ice)
                    return true;

                return false;
            }
        }

        public bool IsMiner
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (!IsPlayer)
                        return false;

                    if (TypeId == (int)TypeID.Venture)
                        return true;

                    if (GroupId == (int)Group.MiningBarge)
                        return true;

                    if (GroupId == (int)Group.Exhumer)
                        return true;

                    if (GroupId == (int)Group.BlockadeRunner)
                        return true;

                    if (GroupId == (int)Group.TransportShip)
                        return true;

                    if (GroupId == (int)Group.Industrial)
                        return true;

                    return _directEntity.IsPlayer;
                }

                return false;
            }
        }

        public bool IsMissile
        {
            get
            {
                if (GroupId == (int)Group.Rockets)
                    return true;

                if (GroupId == (int)Group.LightMissiles)
                    return true;

                if (GroupId == (int)Group.HeavyMissiles)
                    return true;

                if (GroupId == (int)Group.HeavyAssaultMissiles)
                    return true;

                if (GroupId == (int)Group.CruiseMissiles)
                    return true;

                if (GroupId == (int)Group.Torpedoes)
                    return true;

                //if (GroupId == (int)GroupID.CitadelTorpedoes)
                //    return true;

                //if (GroupId == (int)GroupID.CitadelCruiseMissiles)
                //    return true;

                return false;
            }
        }


        public bool PlayerInAShipThatCanGank
        {
            get
            {
                if (!IsPlayer)
                    return false;

                if (IsDestroyer)
                    return true;

                if (IsBattlecruiser)
                    return true;

                if (TypeId == (int)TypeID.Tornado)
                    return true;

                if (TypeId == (int)TypeID.Talos)
                    return true;

                return false;
            }
        }

        //IsPlayer is static and will never change across the life of the Entity
        public bool IsPlayer
        {
            get
            {
                if (!RecursionCheck(nameof(IsPlayer)))
                    return false;

                if (_directEntity != null && IsValid)
                {
                    if (GroupId == (int)Group.AbyssalSpaceshipEntities)
                        return false;

                    if (GroupId == (int)Group.AbyssalDeadspaceDroneEntities)
                        return false;

                    if (GroupId == (int)Group.InvadingPrecursorEntities)
                        return false;

                    if (IsNpc)
                        return false;

                    if (IsNpcByGroupID)
                        return false;

                    return _directEntity.IsPlayer;
                }

                return false;
            }
        }

        public bool IsStandingsBlue
        {
            get
            {
                return (5 >= ESCache.Instance.DirectEve.Standings.GetCorporationRelationship(_directEntity.CorpId));
            }
        }

        public bool IsStandingsPositive
        {
            get
            {
                return (ESCache.Instance.DirectEve.Standings.GetCorporationRelationship(_directEntity.CorpId) > 0.01);
            }
        }

        public bool IsStandingsNegative
        {
            get
            {
                return (ESCache.Instance.DirectEve.Standings.GetCorporationRelationship(_directEntity.CorpId) < 0.00);
            }
        }

        public bool IsStandingsNeutral
        {
            get
            {
                return (ESCache.Instance.DirectEve.Standings.GetCorporationRelationship(_directEntity.CorpId) == 0.00);
            }
        }

        public bool IsStandingsRed
        {
            get
            {
                return (ESCache.Instance.DirectEve.Standings.GetCorporationRelationship(_directEntity.CorpId) <= 5.00);
            }
        }

        private bool? _isPotentialCombatTarget = null;

        public bool IsPotentialCombatTarget
        {
            get
            {
                if (!RecursionCheck(nameof(IsPotentialCombatTarget)))
                    return false;

                if (_directEntity != null)
                {
                    if (!IsValid)
                    {
                        if (!ESCache.Instance.Paused && DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (!IsValid)");
                        _isPotentialCombatTarget = false;
                        return _isPotentialCombatTarget ?? false;
                    }

                    if (_isPotentialCombatTarget != null)
                        return (bool)_isPotentialCombatTarget;

                    if (ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Count > 0)
                    {
                        if (ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Contains(Id))
                        {
                            if (!ESCache.Instance.Paused && DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Contains(Id))");
                            _isPotentialCombatTarget = true;
                            return _isPotentialCombatTarget ?? true;
                        }
                    }

                    if (CategoryId == (int) CategoryID.Entity)
                    {
                        if (!IsOnGridWithMe)
                            return false;

                        if (Id == ESCache.Instance.ActiveShip.Entity.Id)
                            return false;

                        if (ESCache.Instance.InAbyssalDeadspace)
                        {
                            if (IsAbyssalBioAdaptiveCache) // || IsAbyssalDeadspaceTriglavianExtractionNode)
                            {
                                if (DebugConfig.DebugClearPocket || DebugConfig.DebugKillTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsAbyssalDeadspaceTriglavianBioAdaptiveCache)");
                                _isPotentialCombatTarget = true;
                                ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Add(Id);
                                return _isPotentialCombatTarget ?? true;
                            }

                            //
                            // you CANT include this because we dont always kill them all and we assume the room is done when all potentialcombattargets are dead!
                            //
                            //if (IsAbyssalDeadspaceTriglavianExtractionNode)
                            //{
                            //    if (DebugConfig.DebugClearPocket) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsAbyssalDeadspaceTriglavianBioAdaptiveCache)");
                            //    _isPotentialCombatTarget = true;
                            //    ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Add(Id);
                            //    return _isPotentialCombatTarget ?? true;
                            //}
                        }

                        bool ShootNeutrals = true;

                        if (IsPlayer && ESCache.Instance.IsPvpAllowed && (IsStandingsRed || IsStandingsNegative || (IsStandingsNeutral && ShootNeutrals)))
                        {
                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsPlayer ...");
                            if (DebugConfig.DebugIsPvPAllowed) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsPotentialCombatTarget [true]");
                            _isPotentialCombatTarget = true;
                            return _isPotentialCombatTarget ?? true;
                        }

                        if (IsPlayer && ESCache.Instance.InWormHoleSpace && ESCache.Instance.Weapons.Any() && (ESCache.Instance.ActiveShip.IsScrambled || ESCache.Instance.ActiveShip.IsWarpDisrupted) && IsWarpScramblingMe)
                        {
                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsPlayer ...");
                            if (DebugConfig.DebugIsPvPAllowed) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] IsPotentialCombatTarget [true]");
                            _isPotentialCombatTarget = true;
                            return _isPotentialCombatTarget ?? true;
                        }

                        if (IsPlayer && ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.HydraController))
                        {
                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsPlayer ...");
                            _isPotentialCombatTarget = false;
                            return _isPotentialCombatTarget ?? false;
                        }

                        if (IsIgnored)
                        {
                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsIgnored)");
                            _isPotentialCombatTarget = false;
                            return _isPotentialCombatTarget ?? false;
                        }

                        if (IsContainer)
                        {
                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsContainer)");
                            _isPotentialCombatTarget = false;
                            return _isPotentialCombatTarget ?? false;
                        }

                        if (IsFactionWarfareNPC)
                        {
                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsFactionWarfareNPC)");
                            _isPotentialCombatTarget = false;
                            return _isPotentialCombatTarget ?? false;
                        }

                        if (IsBadIdea)
                        {
                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsBadIdea)");
                            _isPotentialCombatTarget = false;
                            return _isPotentialCombatTarget ?? false;
                        }

                        if (IsSentry)
                        {
                            if (IsSentryThatShouldBeShot)
                            {
                                if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsSentryThatShouldBeShot) return true");
                                _isPotentialCombatTarget = true;
                                return _isPotentialCombatTarget ?? true;
                            }

                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsSentry) return false");
                            _isPotentialCombatTarget = false;
                            return _isPotentialCombatTarget ?? false;
                        }

                        if (IsMiscJunk)
                        {
                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsMiscJunk)");
                            _isPotentialCombatTarget = false;
                            return _isPotentialCombatTarget ?? false;
                        }

                        if (IsLargeCollidable)
                        {
                            if (IsDecloakedTransmissionRelay)
                            {
                                if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] true if (IsDecloakedTransmissionRelay)");
                                _isPotentialCombatTarget = true;
                                ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Add(Id);
                                return _isPotentialCombatTarget ?? true;
                            }

                            if (IsLargeCollidableWeAlwaysWantToBlowupLast)
                            {
                                if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] GroupID [" + GroupId + "] true if (IsLargeCollidableWeAlwaysWantToBlowupLast)");
                                _isPotentialCombatTarget = true;
                                ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Add(Id);
                                return _isPotentialCombatTarget ?? true;
                            }

                            if (IsLargeCollidableWeAlwaysWantToBlowupFirst)
                            {
                                if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] GroupID [" + GroupId + "] true if (IsLargeCollidableWeAlwaysWantToBlowupFirst)");
                                _isPotentialCombatTarget = true;
                                ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Add(Id);
                                return _isPotentialCombatTarget ?? true;
                            }

                            if (IsPrimaryWeaponPriorityTarget)
                            {
                                if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] true if (IsPrimaryWeaponPriorityTarget)");
                                _isPotentialCombatTarget = true;
                                ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Add(Id);
                                return _isPotentialCombatTarget ?? true;
                            }

                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] false  if (IsLargeCollidable)");
                            _isPotentialCombatTarget = false;
                            return _isPotentialCombatTarget ?? false;
                        }

                        if (IsNpcByGroupID || IsAttacking || IsNpc || TypeName.Contains("Pirate Capsule"))
                        {
                            if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] if (IsNpcByGroupID || IsAttacking || IsDecloakedTransmissionRelay || ((IsAbyssalDeadspaceTriglavianBioAdaptiveCache || IsAbyssalDeadspaceTriglavianExtractionNode) && ESCache.Instance.InAbyssalDeadspace) || IsLargeCollidableWeAlwaysWantToBlowupLast || IsLargeCollidableWeAlwaysWantToBlowupFirst)");
                            _isPotentialCombatTarget = true;
                            ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget.Add(Id);
                            return _isPotentialCombatTarget ?? true;
                        }

                        if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] !if (IsNpcByGroupID || IsAttacking || IsDecloakedTransmissionRelay || ((IsAbyssalDeadspaceTriglavianBioAdaptiveCache || IsAbyssalDeadspaceTriglavianExtractionNode) && ESCache.Instance.InAbyssalDeadspace) || IsLargeCollidableWeAlwaysWantToBlowupLast || IsLargeCollidableWeAlwaysWantToBlowupFirst)");
                        _isPotentialCombatTarget = false;
                        return _isPotentialCombatTarget ?? false;
                    }

                    //if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] !if (CategoryId == (int) CategoryID.Entity)");
                    _isPotentialCombatTarget = false;
                    return _isPotentialCombatTarget ?? false;
                }

                if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("[" + Name + "][" + Nearest1KDistance + "] !if (_directEntity != null && IsValid)");
                _isPotentialCombatTarget = false;
                return _isPotentialCombatTarget ?? false;
            }
        }

        public bool IsPotentialNinjaLooter
        {
            get
            {
                if (CategoryId == (int)CategoryID.Ship)
                {
                    if (GroupId == (int)Group.Titan)
                        return false;

                    if (GroupId == (int)Group.Carrier)
                        return false;

                    if (GroupId == (int)Group.Dreadnaught)
                        return false;

                    if (TypeId == (int)TypeID.Noctis)
                        return false;

                    if (IsBattleship)
                        return false;

                    if (IsBattlecruiser)
                        return false;

                    if (IsT2Cruiser && GroupId != (int)Group.ForceReconShip)
                        return false;

                    if (GroupId == (int)Group.Shuttle)
                        return false;

                    if (GroupId == (int)Group.Capsule)
                        return false;

                    return true;
                }

                return false;
            }
        }

        public bool IsPreferredDroneTarget
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsPreferredDroneTarget)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (Drones.DronesKillHighValueTargets)
                            return IsPreferredPrimaryWeaponTarget;

                        if (_isPreferredDroneTarget == null)
                        {
                            if (Drones.PreferredDroneTarget != null && Drones.PreferredDroneTarget.Id == _directEntity.Id)
                            {
                                _isPreferredDroneTarget = true;
                                return (bool)_isPreferredDroneTarget;
                            }

                            _isPreferredDroneTarget = false;
                            return (bool)_isPreferredDroneTarget;
                        }

                        return (bool)_isPreferredDroneTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsPreferredPrimaryWeaponTarget
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsPreferredPrimaryWeaponTarget)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isPreferredPrimaryWeaponTarget == null)
                        {
                            if (Combat.PreferredPrimaryWeaponTarget != null && Combat.PreferredPrimaryWeaponTarget.Id == Id)
                            {
                                _isPreferredPrimaryWeaponTarget = true;
                                return (bool)_isPreferredPrimaryWeaponTarget;
                            }

                            _isPreferredPrimaryWeaponTarget = false;
                            return (bool)_isPreferredPrimaryWeaponTarget;
                        }

                        return (bool)_isPreferredPrimaryWeaponTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsPrimaryWeaponKillPriority
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isPrimaryWeaponKillPriority == null)
                        {
                            if (Combat.PrimaryWeaponPriorityTargets.Any(e => e.Entity.Id == Id))
                            {
                                _isPrimaryWeaponKillPriority = true;
                                return (bool)_isPrimaryWeaponKillPriority;
                            }

                            _isPrimaryWeaponKillPriority = false;
                            return (bool)_isPrimaryWeaponKillPriority;
                        }

                        return (bool)_isPrimaryWeaponKillPriority;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsPrimaryWeaponPriorityTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (Combat.PrimaryWeaponPriorityTargets.Any(i => i.EntityID == Id))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsReadyForDronesToShoot
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsReadyForDronesToShoot)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isReadyForDronesToShoot == null)
                        {
                            if (!HasExploded && IsTarget && Distance < Drones.MaxDroneRange && !IsBadIdea && !IsWreck && Id != ESCache.Instance.MyShipEntity.Id)
                            {
                                _isReadyForDronesToShoot = true;
                                return (bool)_isReadyForDronesToShoot;
                            }

                            _isReadyForDronesToShoot = false;
                            return (bool)_isReadyForDronesToShoot;
                        }

                        return _isReadyForDronesToShoot ?? false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public DamageType DamageTypeToShootAtDrifterArmor = DamageType.EM;


        private AmmoType? _shortestRangeAmmo;

        public AmmoType ShortestRangeAmmo
        {
            get
            {
                try
                {
                    if (_shortestRangeAmmo == null)
                    {
                        _shortestRangeAmmo = DirectUIModule.DefinedAmmoTypes.Where(x => !x.OnlyUseAsOverrideAmmo).OrderBy(i => i.Range).FirstOrDefault();
                        return _shortestRangeAmmo;
                    }

                    return _shortestRangeAmmo;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return DirectUIModule.DefinedAmmoTypes.Where(x => !x.OnlyUseAsOverrideAmmo).FirstOrDefault();
                }
            }
        }

        public bool IsShortestRangeAmmoInRange
        {
            get
            {
                if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))
                {
                    return true;
                }

                if (ShortestRangeAmmo.Range > Distance)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsLongestRangeAmmoInRange
        {
            get
            {
                if (LongestRangeAmmo.Range > Distance)
                {
                    return true;
                }

                return false;
            }
        }

        private AmmoType? _longestRangeAmmo;

        public AmmoType LongestRangeAmmo
        {
            get
            {
                try
                {
                    if (_longestRangeAmmo == null)
                    {
                        _longestRangeAmmo = DirectUIModule.DefinedAmmoTypes.Where(x => !x.OnlyUseAsOverrideAmmo).OrderByDescending(i => i.Range).FirstOrDefault();
                        return _longestRangeAmmo;
                    }

                    return _longestRangeAmmo;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return DirectUIModule.DefinedAmmoTypes.Where(x => !x.OnlyUseAsOverrideAmmo).FirstOrDefault();
                }
            }
        }

        private AmmoType? _currentAmmo;

        public AmmoType CurrentAmmo
        {
            get
            {
                try
                {
                    if (_currentAmmo == null)
                    {
                        if (ESCache.Instance.Weapons.Any(i => i.ChargeQty == 0 && !i.IsActive && !i.IsReloadingAmmo && !i.InLimboState))
                        {
                            /**
                            if (!Combat.BoolReloadWeaponsAsap)
                            {
                                int WeaponNumber = 0;
                                foreach (ModuleCache Weapon in ESCache.Instance.Weapons)
                                {
                                    WeaponNumber++;
                                    Log.WriteLine("Weapon [" + WeaponNumber + "][" + Weapon.TypeName + "] Charges [" + Weapon.ChargeQty + "] IsActive [" + Weapon.IsActive + "] IsReloading [" + Weapon.IsReloadingAmmo + "] InLimboState [" + Weapon.InLimboState + "]");
                                }

                                Log.WriteLine("CurrentAmmo: BoolReloadWeaponsAsap = true");
                                Combat.BoolReloadWeaponsAsap = true;
                                return null;
                            }
                            **/
                        }

                        if (ESCache.Instance.Weapons.Any() && ESCache.Instance.Weapons.FirstOrDefault().Charge != null)
                        {
                            _currentAmmo = ESCache.Instance.Weapons.FirstOrDefault().Charge.AmmoType;
                            return _currentAmmo;
                        }

                        return null;
                    }

                    return _currentAmmo;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return DirectUIModule.DefinedAmmoTypes.FirstOrDefault();
                }
            }
        }

        public AmmoType? AmmoToUseAtThisRange
        {
            get
            {
                if (DirectUIModule.DefinedAmmoTypes.Where(x => !x.OnlyUseAsOverrideAmmo).Any())
                {
                    try
                    {
                        if (Combat.UsableAmmoInCargo.Any(i => i.AmmoType != null && i.AmmoType.DamageType == BestDamageTypes.FirstOrDefault()))
                        {
                            return Combat.UsableAmmoInCargo.OrderBy(x => x.AmmoType.Range).FirstOrDefault(i => i.AmmoType != null && i.AmmoType.DamageType == BestDamageTypes.FirstOrDefault()).AmmoType;
                        }

                        if (Combat.UsableAmmoInCargo.Any(i => i.AmmoType != null && i.AmmoType.DamageType == BestDamageTypes.FirstOrDefault()))
                        {
                            return Combat.UsableAmmoInCargo.OrderBy(x => x.AmmoType.Range).FirstOrDefault(i => i.AmmoType != null && i.AmmoType.DamageType == BestDamageTypes.Skip(1).FirstOrDefault()).AmmoType;
                        }

                        if (Combat.UsableAmmoInCargo.Any(i => i.AmmoType != null && i.AmmoType.DamageType == BestDamageTypes.FirstOrDefault()))
                        {
                            return Combat.UsableAmmoInCargo.OrderBy(x => x.AmmoType.Range).FirstOrDefault(i => i.AmmoType != null && i.AmmoType.DamageType == BestDamageTypes.Skip(2).FirstOrDefault()).AmmoType;
                        }

                        return LongestRangeAmmo;
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                        return DirectUIModule.DefinedAmmoTypes.FirstOrDefault(i => i.DamageType == MissionSettings.CurrentDamageType);
                    }
                }

                return null;
            }
        }

        public bool CanSwapAmmoNowToHitThisTarget
        {
            get
            {
                if (!RecursionCheck(nameof(CanSwapAmmoNowToHitThisTarget)))
                    return false;

                if (ESCache.Instance.Weapons.Any())
                {
                    if (CurrentAmmo == null)
                    {
                        if (DebugConfig.DebugCombat) Log.WriteLine("CanSwapAmmoNowToHitThisTarget: CurrentAmmo is null");
                    }

                    if (CurrentAmmo != null && LongestRangeAmmo != null && CurrentAmmo.TypeId == LongestRangeAmmo.TypeId)
                    {
                        if (LongestRangeAmmo.Range > Distance)
                        {
                            return true;
                        }

                        return false;
                    }

                    foreach (ModuleCache Weapon in ESCache.Instance.Weapons.Where(i => !i.IsEnergyWeapon))
                    {
                        if (Time.Instance.LastChangedAmmoTimeStamp.ContainsKey(Weapon.ItemId))
                        {
                            if (Time.Instance.LastChangedAmmoTimeStamp[Weapon.ItemId].AddSeconds(30) > DateTime.UtcNow)
                            {
                                return false;
                            }
                        }
                    }

                    if (!IsPossibleToShoot)
                        return false;

                    return true;
                }

                return true; //???
            }
        }

        public bool IsReadyToShoot
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsReadyToShoot)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isReadyToShoot == null)
                        {
                            /**
                            if (IsDecloakedTransmissionRelay && .98 > ShieldPct && ESCache.Instance.ActiveShip.IsDread)
                            {
                                //|| ESCache.Instance.ActiveShip.IsMarauder || ESCache.Instance.ActiveShip.IsBattleship
                                if (IsTarget)
                                {
                                    if (UnlockTarget())
                                    {
                                        Log.WriteLine("unlocking [" + TypeName + "] at [" + Nearest1KDistance + "]");
                                        _isReadyToShoot = false;
                                        return (bool)_isReadyToShoot;
                                    }
                                }

                                _isReadyToShoot = false;
                                return (bool)_isReadyToShoot;
                            }
                            **/

                            if (!HasExploded && IsTarget && IsInRangeOfWeapons && !IsWreck && !IsNPCDrone && !IsIgnored && !IsBadIdea && Id != ESCache.Instance.MyShipEntity.Id)
                            {
                                //if (ESCache.Instance.Weapons.Any(i => i.IsTurret && i.IsEnergyWeapon))
                                //{
                                //    Combat.ChangeAmmoIfNeeded();
                                //}
                                _isReadyToShoot = true;
                                if (DebugConfig.DebugKillTargets) Log.WriteLine("IsReadyToShoot: [" + _isReadyToShoot + "] [" + Name + "][" + Nearest1KDistance + "k] HasExploded [" + HasExploded + "] IsTarget [" + IsTarget + "] IsInRangeOfWeapons [" + IsInRangeOfWeapons + "] Distance [" + Math.Round(Distance, 0) + "] MaxWeaponRange [" + Combat.MaxWeaponRange + "] IsBadIdea [" + IsBadIdea + "]");
                                return (bool)_isReadyToShoot;
                            }

                            _isReadyToShoot = false;
                            if (DebugConfig.DebugKillTargets) Log.WriteLine("IsReadyToShoot: [" + _isReadyToShoot + "] [" + Name + "][" + Nearest1KDistance + "k] HasExploded [" + HasExploded + "] IsTarget [" + IsTarget + "] IsInRangeOfWeapons [" + IsInRangeOfWeapons + "] Distance [" + Math.Round(Distance, 0) + "] MaxWeaponRange [" + Combat.MaxWeaponRange + "] IsBadIdea [" + IsBadIdea + "]");
                            return (bool)_isReadyToShoot;
                        }

                        return _isReadyToShoot ?? false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        private bool? _isPossibleToShoot;

        public bool IsPossibleToShoot
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsPossibleToShoot)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isPossibleToShoot == null)
                        {
                            if (!HasExploded && IsTarget && IsInRangeOfAnyAmmoWeHaveWithUs && !IsWreck && !IsNPCDrone && !IsIgnored && !IsBadIdea && Id != ESCache.Instance.MyShipEntity.Id)
                            {
                                _isPossibleToShoot = true;
                                return (bool)_isPossibleToShoot;
                            }

                            _isPossibleToShoot = false;
                            return (bool)_isPossibleToShoot;
                        }

                        return _isPossibleToShoot ?? false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }



        private bool? _isPossibleToTarget;

        public bool IsPossibleToTarget
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsPossibleToTarget)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isPossibleToTarget == null)
                        {
                            if (!HasExploded && IsInRangeOfAnyAmmoWeHaveWithUs && !IsTarget && !IsTargeting && !IsWreck && !IsNPCDrone && !IsIgnored && !IsBadIdea && Id != ESCache.Instance.MyShipEntity.Id)
                            {
                                _isPossibleToTarget = true;
                                return (bool)_isPossibleToTarget;
                            }

                            _isPossibleToTarget = false;
                            return (bool)_isPossibleToTarget;
                        }

                        return _isPossibleToTarget ?? false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsReadyToTarget
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsReadyToTarget)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isReadyToTarget == null)
                        {
                            //
                            // Needs logging here?
                            //
                            if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("HasExploded [" + HasExploded + "] IsTarget [" + IsTarget + "] IsTargeting [" + IsTargeting + "] Distance [" + Math.Round(Distance, 0) + "m] MaxTargetRange [" + Combat.MaxTargetRange + "m] ");
                            if (!HasExploded && !IsTarget && !IsTargeting)
                            {
                                if (ESCache.Instance.InAbyssalDeadspace && Distance < Combat.MaxTargetRange && IsAbyssalDeadspaceTriglavianBioAdaptiveCache && (Combat.MaxWeaponRange > Distance || (Drones.UseDrones && Drones.DroneControlRange > Distance)))
                                {
                                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                                    {
                                        if (15000 > Distance)
                                        {
                                            _isReadyToTarget = true;
                                            return (bool)_isReadyToTarget;
                                        }

                                        _isReadyToTarget = false;
                                        return (bool)_isReadyToTarget;
                                    }
                                    _isReadyToTarget = true;
                                    if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("IsReadyToTarget [" + Name + "][" + Nearest1KDistance + "k][" + _isReadyToTarget + "]");
                                    return (bool)_isReadyToTarget;
                                }

                                if ((!IsAbyssalDeadspaceTriglavianBioAdaptiveCache && Distance < Combat.MaxTargetRange || Distance < 20000 || (IsPotentialNinjaLooter && Distance < Combat.MaxTargetRange)) && !HasInitiatedWarp && Id != ESCache.Instance.MyShipEntity.Id)
                                {
                                    if (ESCache.Instance.InWormHoleSpace && IsDecloakedTransmissionRelay && .98 > ShieldPct)
                                    {
                                        _isReadyToTarget = false;
                                        if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("IsReadyToTarget [" + Name + "][" + Nearest1KDistance + "k][" + _isReadyToTarget + "]!!.!");
                                        return (bool)_isReadyToTarget;
                                    }

                                    /**
                                if (ESCache.Instance.InAbyssalDeadspace && (IsAbyssalDeadspaceTriglavianBioAdaptiveCache || IsAbyssalDeadspaceTriglavianExtractionNode))
                                {
                                    if (ESCache.Instance.ActiveShip.IsFrigate)
                                    {
                                        if (ESCache.Instance.EntitiesNotSelf.Any(i => i.IsNpc))
                                        {
                                            _isReadyToTarget = false;
                                            if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("IsReadyToTarget [" + Name + "][" + Nearest1KDistance + "][" + _isReadyToTarget + "]!.!");
                                            return (bool)_isReadyToTarget;
                                        }

                                        _isReadyToTarget = true;
                                        if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("IsReadyToTarget [" + Name + "][" + Nearest1KDistance + "][" + _isReadyToTarget + "]!.!");
                                        return (bool)_isReadyToTarget;
                                    }

                                    if (ESCache.Instance.EntitiesNotSelf.Count(i => i.IsNpc && i.IsNPCBattleship) >= 5)
                                    {
                                        _isReadyToTarget = true;
                                        if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("IsReadyToTarget [" + Name + "][" + Nearest1KDistance + "][" + _isReadyToTarget + "]!!!");
                                        return (bool)_isReadyToTarget;
                                    }

                                    if (Salvage.TractorBeams.Count > 0)
                                    {
                                        if (Salvage.TractorBeamRange + 6000 > Distance)
                                        {
                                            _isReadyToTarget = true;
                                            if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("IsReadyToTarget [" + Name + "][" + Nearest1KDistance + "][" + _isReadyToTarget + "]!");
                                            return (bool)_isReadyToTarget;
                                        }

                                        _isReadyToTarget = false;
                                        if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("IsReadyToTarget [" + Name + "][" + Nearest1KDistance + "][" + _isReadyToTarget + "]!");
                                        return (bool)_isReadyToTarget;
                                    }
                                }
                                **/


                                    _isReadyToTarget = true;
                                    if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("IsReadyToTarget [" + Name + "][" + Nearest1KDistance + "k][" + _isReadyToTarget + "]");
                                    return (bool)_isReadyToTarget;
                                }
                            }

                            _isReadyToTarget = false;
                            if (DebugConfig.DebugIsReadyToTarget) Log.WriteLine("IsReadyToTarget [" + Name + "][" + Nearest1KDistance + "k][" + _isReadyToTarget + "]");
                            return (bool)_isReadyToTarget;
                        }

                        return (bool)_isReadyToTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsSensorDampeningMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("ewRemoteSensorDamp"))
                        {
                            if (!ESCache.Instance.ListOfDampeningEntities.Contains(Id)) ESCache.Instance.ListOfDampeningEntities.Add(Id);
                            return true;
                        }

                        if (ESCache.Instance.ListOfDampeningEntities.Contains(Id))
                            return true;

                        if (ESCache.Instance.InAbyssalDeadspace && (Name.Contains("Obfuscator") || Name.Contains("Blinding") || Name.Contains("Gazedimmer")))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool WeWantToPrioritizeECM
        {
            get
            {
                //
                // Reasons to prioritize this type of EWar
                //

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsTryingToJamMe) >= 1)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool WeWantToPrioritizeNeutralizers
        {
            get
            {
                //
                // Reasons to prioritize this type of EWar
                //

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (!ESCache.Instance.ActiveShip.IsActiveTanked)
                        return false;

                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasNeutralizers && i.IsNPCBattleship))
                    {
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasNeutralizers && i.IsNPCCruiser))
                    {
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasNeutralizers && i.IsNPCDestroyer))
                    {
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.NpcHasNeutralizers) >= 2)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool WeWantToPrioritizeRemoteRepair
        {
            get
            {
                //
                // Reasons to prioritize this type of EWar
                //

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.NpcHasRemoteRepair) >= 2)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool WeWantToPrioritizeSensorDamps
        {
            get
            {
                //
                // Reasons to prioritize this type of EWar
                //
                if (Drones.DronesKillHighValueTargets)
                {
                    //drones are on aggressive right?
                    return false;
                }

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsSensorDampeningMe) >= 3)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool WeWantToPrioritizeTargetPainters
        {
            get
            {
                //
                // Reasons to prioritize this type of EWar
                //

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsTargetPaintingMe) >= 4)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool WeWantToPrioritizeTrackingDistuptors
        {
            get
            {
                //
                // Reasons to prioritize this type of EWar
                //

                //this effects missiles and turrets! crazy!

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsTrackingDisruptingMe) >= 2)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool WeWantToPrioritizeWebs
        {
            get
            {
                //
                // Reasons to prioritize this type of EWar
                //

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (ESCache.Instance.ActiveShip.HasSpeedMod)
                    {
                        //drones are on aggressive right?
                        return true;
                    }

                    if (Drones.DronesKillHighValueTargets)
                    {
                        //drones are on aggressive right?
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsWebbingMe) >= 2)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool WeWantToPrioritizeScrams
        {
            get
            {
                //
                // Reasons to prioritize this type of EWar
                //

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (ESCache.Instance.ActiveShip.HasSpeedMod)
                    {
                        //drones are on aggressive right?
                        return true;
                    }

                    if (Drones.DronesKillHighValueTargets)
                    {
                        //drones are on aggressive right?
                        return true;
                    }

                    if (!ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsWarpScramblingMe))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return false;
            }
        }


        public bool IsSentryThatShouldBeShot
        {
            get
            {
                if (!IsSentry || (IsSentry && KillSentries) || (IsSentry && IsEwarTarget))
                    return false;

                return true;
            }
        }

        public bool IsSentry => _directEntity.IsSentry;

        public bool IsSmallWreck => _directEntity.IsSmallWreck;

        public bool IsSomethingICouldKillFasterIfIWereCloser
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (!IsInWebRange) return true;
                    if (!IsInOptimalRange) return true;
                    if (!IsInDroneRange && Drones.DronesKillHighValueTargets) return true;
                    if (Distance > Combat.MaxRange && !Drones.DronesKillHighValueTargets) return true;
                    return false;
                }

                return false;
            }
        }

        public bool HasCitadelWithin10k
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (ESCache.Instance.Citadels.Any(i => 10000 > i.DistanceTo(this)))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsCitadel
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= CategoryId == (int)CategoryID.Citadel;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsStargate
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= GroupId == (int)Group.Stargate;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsStation
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= CategoryId == (int)CategoryID.Station;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsT2Cruiser => _directEntity.IsT2Cruiser;

        public bool IsT2BattleCruiser => _directEntity.IsT2BattleCruiser;

        public bool IsTarget => _directEntity.IsTarget;

        public bool IsTargetedBy => _directEntity.IsTargetedBy;

        public bool IsTargeting => _directEntity.IsTargeting;

        public bool IsTargetingMeAndNotYetTargeted
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= (IsNpc || IsNpcByGroupID || IsAttacking)
                                  && CategoryId == (int)CategoryID.Entity
                                  && Distance < Combat.MaxTargetRange
                                  && !IsLargeCollidable && !IsTargeting && !IsTarget && IsTargetedBy && !IsContainer && !IsIgnored &&
                                  (!IsBadIdea || IsAttacking) && !IsEntityIShouldLeaveAlone && !IsFactionWarfareNPC && !IsStation && !IsCitadel;

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTargetPaintingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("ewTargetPaint"))
                        {
                            if (!ESCache.Instance.ListOfTargetPaintingEntities.Contains(Id)) ESCache.Instance.ListOfTargetPaintingEntities.Add(Id);
                            return true;
                        }

                        if (ESCache.Instance.ListOfTargetPaintingEntities.Contains(Id))
                            return true;

                        if (ESCache.Instance.InAbyssalDeadspace && (Name.Contains("Spotlighter") || Name.Contains("Harrowing") || Name.Contains("Illuminator")))
                            return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTargetWeCanShootButHaveNotYetTargeted
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= CategoryId == (int)CategoryID.Entity
                                  && !IsTarget
                                  && !IsTargeting
                                  && Distance < Combat.MaxRange
                                  && !IsIgnored
                                  && !IsStation
                                  && !IsCitadel;

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTooCloseTooFastTooSmallToHit
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_isTooCloseTooFastTooSmallToHit == null)
                            if (IsNPCFrigate || IsFrigate)
                            {
                                if (Combat.DoWeCurrentlyHaveTurretsMounted() && Drones.UseDrones && Drones.ActiveDroneCount > 0)
                                {
                                    if (!IsTrackable)
                                    {
                                        if (Combat.PotentialCombatTargets.Count > 0 && Combat.PotentialCombatTargets.Any(i => !i.IsNPCFrigate))
                                        {
                                            _isTooCloseTooFastTooSmallToHit = true;
                                            return (bool)_isTooCloseTooFastTooSmallToHit;
                                        }

                                        //
                                        // it may not be tracable, but its potentially the only thing left to shoot...
                                        //

                                        _isTooCloseTooFastTooSmallToHit = true;
                                        return (bool)_isTooCloseTooFastTooSmallToHit;
                                    }

                                    _isTooCloseTooFastTooSmallToHit = false;
                                    return (bool)_isTooCloseTooFastTooSmallToHit;
                                }

                                _isTooCloseTooFastTooSmallToHit = false;
                                return (bool)_isTooCloseTooFastTooSmallToHit;
                            }

                        return _isTooCloseTooFastTooSmallToHit ?? false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public double Health
        {
            get { return ShieldMaxHitPoints + ArmorMaxHitPoints + StructureMaxHitPoints; }
        }

        public bool AllowChangingAmmoBasedOnResists
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsNPCBattleship && Health > 1000)
                        return true;

                    if (IsLargeCollidable && Health > 1000)
                        return true;

                    return false;
                }

                return false;
            }
        }
        //todo: improve this to work for missiles (either hard coded NPC sizes to missile sizes, or better explosion velocity calcs)
        public bool IsTrackable
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsTrackable)))
                        return false;

                    if (_directEntity != null && IsValid)
                    {
                        if (_isTrackable == null)
                        {
                            if (ESCache.Instance.Weapons.Count > 0)
                            {
                                if (Combat.DoWeCurrentlyHaveTurretsMounted())
                                    try
                                    {
                                        //https://eve-search.com/thread/505590-1/page/all
                                        //Divide turret tracking by 1000, you'll hit frigates doing that many rad/s 50% of the time.
                                        //Multiply by 3.2, you'll hit cruisers doing that many rad/s 50% of the time.
                                        //Multiply that by 3.2(or divide original turret tracking by 100) and you'll hit battleships doing that many rad/s 50% of the time.
                                        //That's the rule of thumb you want to start with, it obviously changes a little if the sig sizes aren't exactly 40 / 125 / 400.
                                        ModuleCache myTurret = ESCache.Instance.Weapons.Find(i => i.IsTurret);
                                        if (myTurret != null)
                                        {
                                            if ((IsFrigate || IsNPCFrigate) && AngularVelocity < myTurret.TrackingSpeed / 1000)
                                            {
                                                _isTrackable = true;
                                                return (bool)_isTrackable;
                                            }

                                            if ((IsCruiser || IsNPCCruiser) && AngularVelocity < myTurret.TrackingSpeed / 1000 * 3.2)
                                            {
                                                _isTrackable = true;
                                                return (bool)_isTrackable;
                                            }

                                            if ((IsBattleship || IsBattlecruiser || IsNPCBattleship || IsNPCBattlecruiser) && AngularVelocity < myTurret.TrackingSpeed / 100)
                                            {
                                                _isTrackable = true;
                                                return (bool)_isTrackable;
                                            }

                                            if ((IsNpcCapitalEscalation || IsNPCCapitalShip) && AngularVelocity < myTurret.TrackingSpeed / 5) //todo: check this math?
                                            {
                                                _isTrackable = true;
                                                return (bool)_isTrackable;
                                            }

                                            if (IsWreck)
                                            {
                                                _isTrackable = true;
                                                return (bool)_isTrackable;
                                            }

                                            if (IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                                            {
                                                _isTrackable = true;
                                                return (bool)_isTrackable;
                                            }

                                            if (IsLargeCollidable)
                                            {
                                                _isTrackable = true;
                                                return (bool)_isTrackable;
                                            }

                                            _isTrackable = false;
                                            return (bool)_isTrackable;
                                        }

                                        _isTrackable = false;
                                        return (bool)_isTrackable;
                                    }
                                    catch (Exception exception)
                                    {
                                        Log.WriteLine("Exception [" + exception + "]");
                                        return false;
                                    }

                                return true;
                            }

                            _isTrackable = false;
                            return (bool)_isTrackable;
                        }

                        return (bool)_isTrackable;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTrackingDisruptingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("ewTrackingDisrupt"))
                        {
                            if (!ESCache.Instance.ListOfTrackingDisruptingEntities.Contains(Id)) ESCache.Instance.ListOfTrackingDisruptingEntities.Add(Id);
                            return true;
                        }

                        if (ESCache.Instance.ListOfTrackingDisruptingEntities.Contains(Id))
                            return true;

                        if (ESCache.Instance.InAbyssalDeadspace && (Name.Contains("Ghosting") || Name.Contains("Fogcaster") || Name.Contains("Confuser")))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsValid => _directEntity.IsValid;

        public bool IsWarpScramblingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (ESCache.Instance.InAbyssalDeadspace)
                        {
                            switch (Name)
                            {
                                //https://docs.google.com/spreadsheets/d/1SsYX2utsKnMxGBUA8cDuLsoE7XHy20lqIJJ6AFAocQU/edit#gid=244135671

                                case "Anchoring Damavik":
                                case "Anchoring Vila Damavik":
                                case "Ephialtes Spearfisher":
                                    return true;
                            }
                        }

                        return _directEntity.IsWarpScramblingMe;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsWebbingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_directEntity.Attacks.Contains("effects.ModifyTargetSpeed"))
                        {
                            if (!ESCache.Instance.ListofWebbingEntities.Contains(Id)) ESCache.Instance.ListofWebbingEntities.Add(Id);
                            return true;
                        }

                        if (ESCache.Instance.ListofWebbingEntities.Contains(Id))
                            return true;

                        if (ESCache.Instance.InAbyssalDeadspace)
                        {
                            switch (Name)
                            {
                                //https://docs.google.com/spreadsheets/d/1SsYX2utsKnMxGBUA8cDuLsoE7XHy20lqIJJ6AFAocQU/edit#gid=244135671
                                case "Benthic Abyssal Overmind":
                                case "Snarecaster Tessella": //Rogue Drone Frigate
                                case "Drifter Entanglement Cruiser": //Drifter Cruiser
                                case "Tangling Damavik":
                                case "Tangling Vila Damavik":
                                case "Tangling Leshak":
                                case "Lucid Warden":
                                case "Lucid Upholder":
                                case "Ephialtes Entangler":
                                case "Tangling Kikimora":
                                //case "Scylla Tyrannos":
                                    return true;
                            }
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsMissileDisruptingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (ESCache.Instance.InAbyssalDeadspace && Name.Contains("Fogcaster"))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsHighDps
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (ESCache.Instance.InAbyssalDeadspace)
                        {
                            if (IsNPCFrigate)
                            {
                                if (Name.Contains("Lance") || Name.Contains("Striking") || Name.Contains("Warding"))
                                    return true;
                            }

                            if (IsNPCCruiser)
                            {
                                if (Name.Contains("Vedmak") || Name.Contains("Lancer"))
                                    return true;
                            }

                            if (IsNPCBattleship)
                            {
                                if (Name.Contains("Leshak"))
                                    return true;
                            }
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsWreckReadyToBeNavigateOnGridTarget
        {
            get
            {
                if (!RecursionCheck(nameof(IsWreckReadyToBeNavigateOnGridTarget)))
                    return false;

                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (!IsWreck)
                    return false;

                if (IsWreckEmpty)
                    return false;

                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                {
                    if (Combat.PotentialCombatTargets.All(i => !i.IsNPCFrigate && !i.IsNPCDestroyer && !i.IsNPCCruiser && !i.IsNPCBattlecruiser && !i.IsNPCBattleship))
                        return true;

                    return false;
                }

                return true;
            }
        }
        public bool IsWreck
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (GroupId == (int)Group.Wreck)
                            return true;

                        if (Name.Contains("Cache Wreck") && !IsPlayer)
                            return true;

                        if (ESCache.Instance.InAbyssalDeadspace && IsContainer && Name.Contains(" Wreck"))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTractorActive
        {
            get
            {
                if (Salvage.TractorBeams.Any(i => i.IsActive && i.TargetId == Id))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsWreckEmpty
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (IsWreck)
                            return _directEntity.IsEmpty;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsAbyssalCacheWreck
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (IsWreck)
                        {
                            if (!ESCache.Instance.InAbyssalDeadspace)
                                return false;

                            if (string.IsNullOrEmpty(Name))
                                return false;

                            if (Name.ToLower().Contains("Biocombinative Cache Wreck".ToLower()))
                                return true;

                            if (Name.ToLower().Contains("Bioadaptive Cache Wreck".ToLower()))
                                return true;

                            if (Name.ToLower().Contains("Biocombinative Schematic Cache Wreck".ToLower())) //not in abyssaldeadspace? see: https://wiki.eveuniversity.org/Triglavian_Invasion_combat_sites
                                return true;

                            if (Name.ToLower().Contains("Cladistic Cache Wreck".ToLower())) //pvp cache wreck
                                return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool KillThisJammingNpc
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsTryingToJamMe)
                    {
                        if (Combat.AddECMsToPrimaryWeaponsPriorityTargetList)
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public bool KillThisNeutralizingNpc
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsNeutralizingMe)
                    {
                        if (Combat.AddNeutralizersToPrimaryWeaponsPriorityTargetList || ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        /**
        public bool IsPriorityWarpScrambler
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (Combat.PrimaryWeaponPriorityTargets.Any(pt => pt.EntityID == Id && pt.PrimaryWeaponPriority == PrimaryWeaponPriority.WarpScrambler))
                            return true;

                        if (Drones.DronePriorityTargets.Any(pt => pt.EntityID == Id && pt.DronePriority == DronePriority.WarpScrambler))
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }
        **/

        public bool KillThisSensorDampeningNpc
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsSensorDampeningMe)
                    {
                        if (Combat.AddDampenersToPrimaryWeaponsPriorityTargetList)
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public bool KillThisTargetPaintingNpc
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsTargetPaintingMe)
                    {
                        if (Combat.AddTargetPaintersToPrimaryWeaponsPriorityTargetList)
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public bool KillThisTrackingDisruptingNpc
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsTrackingDisruptingMe)
                    {
                        if (Combat.AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList)
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public bool KillThisWarpScramblingNpc
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsWarpScramblingMe)
                    {
                        if (Combat.AddWarpScramblersToPrimaryWeaponsPriorityTargetList)
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public bool KillThisWebbingNpc
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (IsWebbingMe)
                    {
                        if (Combat.AddWebifiersToPrimaryWeaponsPriorityTargetList)
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public DirectSolarSystem LeadsToSolarSystem
        {
            get
            {
                if (GroupId == (int)Group.Stargate)
                {
                    IEnumerable<DirectSolarSystem> NeighboringSystems = ESCache.Instance.DirectEve.Session.SolarSystem.GetNeighbours(1);
                    if (NeighboringSystems.Any(i => i.Name == Name))
                    {
                        DirectSolarSystem tempSolarSystem = NeighboringSystems.FirstOrDefault(i => i.Name == Name);
                        return tempSolarSystem;
                    }
                }

                return null;
            }
        }

        public bool WeCanKillThisNPCAndStillHaveEnoughDpsOnFieldToKillLooters
        {
            get
            {
                if (IsNPCFrigate && 8 >= Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate))
                    return false;

                if (IsNPCCruiser && 6 >= Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser))
                    return false;

                if (IsNPCBattlecruiser && 4 >= Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser))
                    return false;

                if (IsNPCBattleship && 2 >= Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))
                    return false;

                return true;
            }
        }

        public string MaskedId
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        int numofCharacters = _directEntity.Id.ToString(CultureInfo.InvariantCulture).Length;
                        if (numofCharacters >= 5)
                        {
                            string maskedID = _directEntity.Id.ToString(CultureInfo.InvariantCulture).Substring(numofCharacters - 4);
                            maskedID = "[MaskedID]" + maskedID;
                            return maskedID;
                        }

                        return "!0!";
                    }

                    return "!0!";
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return "!0!";
                }
            }
        }


        public double AmmoDamageCalc(DirectItem ammoToUseInDamageCalc)
        {
            if (ammoToUseInDamageCalc.CategoryId == (int)CategoryID.Charge)
            {
                if (ammoToUseInDamageCalc.IsMissile)
                {
                    double dmg = MissileDamageCalc(ammoToUseInDamageCalc);
                    return dmg;
                }

                if (ammoToUseInDamageCalc.IsProjectileAmmo)
                {
                    double dmg = ProjectileAmmoDamageCalc(ammoToUseInDamageCalc);
                    return dmg;
                }

                if (ammoToUseInDamageCalc.IsHybridAmmo)
                {
                    double dmg = HybridAmmoDamageCalc(ammoToUseInDamageCalc);
                    return dmg;
                }

                if (ammoToUseInDamageCalc.IsLaserAmmo)
                {
                    double dmg = LaserCrystalDamageCalc(ammoToUseInDamageCalc);
                    return dmg;
                }
            }

            return 0;
        }

        private double? _projectileAmmoDamageCalc = null;

        public double ProjectileAmmoDamageCalc(DirectItem projectileAmmoToUseInDamageCalc)
        {
            if (_projectileAmmoDamageCalc != null) return _projectileAmmoDamageCalc ?? 0;

            if (projectileAmmoToUseInDamageCalc.CategoryId == (int)CategoryID.Charge && projectileAmmoToUseInDamageCalc.IsProjectileAmmo)
            {
                // https://wiki.eveuniversity.org/Turrets
                double projectileAmmoBaseEmDamage = projectileAmmoToUseInDamageCalc.Attributes.TryGet<int>("emDamage");
                double projectileAmmoExplosiveDamage = projectileAmmoToUseInDamageCalc.Attributes.TryGet<int>("explosiveDamage");
                double projectileAmmoBaseKineticDamage = projectileAmmoToUseInDamageCalc.Attributes.TryGet<int>("kineticDamage");
                double projectileAmmoBaseThermalDamage = projectileAmmoToUseInDamageCalc.Attributes.TryGet<int>("thermalDamage");

                double projectileAmmoCalculatedEmDamage = projectileAmmoBaseEmDamage * CurrentEmResistance;
                double projectileAmmoCalculatedExplosiveDamage = projectileAmmoExplosiveDamage * CurrentExplosiveResistance;
                double projectileAmmoCalculatedKineticDamage = projectileAmmoBaseKineticDamage * CurrentKineticResistance;
                double projectileAmmoCalculatedThermalDamage = projectileAmmoBaseThermalDamage * CurrentThermalResistance;
                double projectileAmmoCalculatedTotalDamage = projectileAmmoCalculatedEmDamage + projectileAmmoCalculatedExplosiveDamage + projectileAmmoCalculatedKineticDamage + projectileAmmoCalculatedThermalDamage;
                _projectileAmmoDamageCalc = projectileAmmoCalculatedTotalDamage;
                return _projectileAmmoDamageCalc ?? 0;
            }

            return 0;
        }

        private double? _hybridAmmoDamageCalc = null;

        public double HybridAmmoDamageCalc(DirectItem hybridAmmoToUseInDamageCalc)
        {
            if (_hybridAmmoDamageCalc != null) return _hybridAmmoDamageCalc ?? 0;

            if (hybridAmmoToUseInDamageCalc.CategoryId == (int)CategoryID.Charge && hybridAmmoToUseInDamageCalc.IsHybridAmmo)
            {
                //https://wiki.eveuniversity.org/Turrets
                double hybridAmmoBaseEmDamage = hybridAmmoToUseInDamageCalc.Attributes.TryGet<int>("emDamage");
                double hybridAmmoExplosiveDamage = hybridAmmoToUseInDamageCalc.Attributes.TryGet<int>("explosiveDamage");
                double hybridAmmoBaseKineticDamage = hybridAmmoToUseInDamageCalc.Attributes.TryGet<int>("kineticDamage");
                double hybridAmmoBaseThermalDamage = hybridAmmoToUseInDamageCalc.Attributes.TryGet<int>("thermalDamage");

                double hybridAmmoCalculatedEmDamage = hybridAmmoBaseEmDamage * CurrentEmResistance;
                double hybridAmmoCalculatedExplosiveDamage = hybridAmmoExplosiveDamage * CurrentExplosiveResistance;
                double hybridAmmoCalculatedKineticDamage = hybridAmmoBaseKineticDamage * CurrentKineticResistance;
                double hybridAmmoCalculatedThermalDamage = hybridAmmoBaseThermalDamage * CurrentThermalResistance;
                double hybridAmmoCalculatedTotalDamage = hybridAmmoCalculatedEmDamage + hybridAmmoCalculatedExplosiveDamage + hybridAmmoCalculatedKineticDamage + hybridAmmoCalculatedThermalDamage;
                _hybridAmmoDamageCalc = hybridAmmoCalculatedTotalDamage;
                return _hybridAmmoDamageCalc ?? 0;
            }

            return 0;
        }

        private double? _laserCrystalDamageCalc = null;

        public double LaserCrystalDamageCalc(DirectItem laserCrystalToUseInDamageCalc)
        {
            if (_laserCrystalDamageCalc != null) return _laserCrystalDamageCalc ?? 0;

            if (laserCrystalToUseInDamageCalc.CategoryId == (int)CategoryID.Charge && laserCrystalToUseInDamageCalc.IsLaserAmmo)
            {
                //https://wiki.eveuniversity.org/Turrets
                double laserCrystalBaseEmDamage = laserCrystalToUseInDamageCalc.Attributes.TryGet<int>("emDamage");
                double laserCrystalExplosiveDamage = laserCrystalToUseInDamageCalc.Attributes.TryGet<int>("explosiveDamage");
                double laserCrystalBaseKineticDamage = laserCrystalToUseInDamageCalc.Attributes.TryGet<int>("kineticDamage");
                double laserCrystalBaseThermalDamage = laserCrystalToUseInDamageCalc.Attributes.TryGet<int>("thermalDamage");

                double laserCrystalCalculatedEmDamage = laserCrystalBaseEmDamage * CurrentEmResistance;
                double laserCrystalCalculatedExplosiveDamage = laserCrystalExplosiveDamage * CurrentExplosiveResistance;
                double laserCrystalCalculatedKineticDamage = laserCrystalBaseKineticDamage * CurrentKineticResistance;
                double laserCrystalCalculatedThermalDamage = laserCrystalBaseThermalDamage * CurrentThermalResistance;
                double laserCrystalCalculatedTotalDamage = laserCrystalCalculatedEmDamage + laserCrystalCalculatedExplosiveDamage + laserCrystalCalculatedKineticDamage + laserCrystalCalculatedThermalDamage;
                _laserCrystalDamageCalc = laserCrystalCalculatedTotalDamage;
                return _laserCrystalDamageCalc ?? 0;
            }

            return 0;
        }


        /// <summary>
        /// Calculate the damage done by a missile
        /// https://wiki.eveuniversity.org/Missile_mechanics#Missile_damage_formula
        /// </summary>
        /// <param name="missileToUseInDamageCalc">DirectItem of Missile to use for Damage Calculations</param>
        /// <returns>The final damage dealt by the missile impact</returns>
        public double MissileDamageCalc(DirectItem missileToUseInDamageCalc)
        {
            if (missileToUseInDamageCalc.CategoryId == (int) CategoryID.Charge && missileToUseInDamageCalc.IsMissile)
            {
                // To prevent doing damage greater than the missile's base damage, return the smallest out of:
                // 1 (full damage)
                // Sig Radius / Explosion Radius (signature radius is larger than the explosion radius)
                // The full calculation
                double missileBaseEmDamage = missileToUseInDamageCalc.Attributes.TryGet<int>("emDamage");
                double missileBaseExplosiveDamage = missileToUseInDamageCalc.Attributes.TryGet<int>("explosiveDamage");
                double missileBaseKineticDamage = missileToUseInDamageCalc.Attributes.TryGet<int>("kineticDamage");
                double missileBaseThermalDamage = missileToUseInDamageCalc.Attributes.TryGet<int>("thermalDamage");

                double missileDamageReductionFactor = missileToUseInDamageCalc.Attributes.TryGet<int>("aoeDamageReductionFactor");
                double missileExplosionRadius = missileToUseInDamageCalc.Attributes.TryGet<int>("aoeCloudSize");
                double missileExplosionVelocity = missileToUseInDamageCalc.Attributes.TryGet<int>("aoeVelocity");

                double missileCalculatedRawEmDamage = missileBaseEmDamage * Math.Min(1, Math.Min((double)_directEntity.EntitySignatureRadius * missileExplosionRadius, Math.Pow((double)_directEntity.EntitySignatureRadius / missileExplosionRadius * (missileExplosionVelocity / Velocity), missileDamageReductionFactor)));
                double missileCalculatedRawExplosiveDamage = missileBaseExplosiveDamage * Math.Min(1, Math.Min((double)_directEntity.EntitySignatureRadius * missileExplosionRadius, Math.Pow((double)_directEntity.EntitySignatureRadius / missileExplosionRadius * (missileExplosionVelocity / Velocity), missileDamageReductionFactor)));
                double missileCalculatedRawKineticDamage = missileBaseKineticDamage * Math.Min(1, Math.Min((double)_directEntity.EntitySignatureRadius * missileExplosionRadius, Math.Pow((double)_directEntity.EntitySignatureRadius / missileExplosionRadius * (missileExplosionVelocity / Velocity), missileDamageReductionFactor)));
                double missileCalculatedRawThermalDamage = missileBaseThermalDamage * Math.Min(1, Math.Min((double)_directEntity.EntitySignatureRadius * missileExplosionRadius, Math.Pow((double)_directEntity.EntitySignatureRadius / missileExplosionRadius * (missileExplosionVelocity / Velocity), missileDamageReductionFactor)));

                double missileCalculatedEmDamage = missileCalculatedRawEmDamage * CurrentEmResistance;
                double missileCalculatedExplosiveDamage = missileCalculatedRawExplosiveDamage * CurrentExplosiveResistance;
                double missileCalculatedKineticDamage = missileCalculatedRawKineticDamage * CurrentKineticResistance;
                double missileCalculatedThermalDamage = missileCalculatedRawThermalDamage * CurrentThermalResistance;
                double missileCalculatedTotalDamage = missileCalculatedEmDamage + missileCalculatedExplosiveDamage + missileCalculatedKineticDamage + missileCalculatedThermalDamage;
                return missileCalculatedTotalDamage;
            }

            return 0;
        }

        public double TurretChanceToHit(DirectUIModule myTurret)
        {
            if (myTurret.OptimalRange != null && myTurret.FallOff != null && myTurret.IsTurret)
            {
                // How likely it is to hit the target with how fast the target is moving in relation to the ship doing the shooting
                double trackingTerm = 0.5 * (((double)_directEntity.AngularVelocity * 40000) / ((double)myTurret.TrackingSpeed * (double)_directEntity.EntitySignatureRadius));

                // How likely it is to hit the target at the distance it is
                // 100% within optimal range
                // about 50% at optimal + under half of falloff
                // about 6.5% @ optimal + over half of falloff
                // about 0.2% @ optimal + falloff
                double rangeTerm = 0.5 * (Math.Max(0, Distance - (double)myTurret.OptimalRange) / (double)myTurret.FallOff);

                return trackingTerm * rangeTerm;
            }

            return 1;
        }

        public double CurrentEmResistance
        {
            get
            {
                if (ShieldMaxHitPoints > 0)
                    return ShieldResistanceEm;

                if (ArmorMaxHitPoints > 0)
                    return ArmorResistanceEm;

                if (StructureMaxHitPoints > 0)
                    return 0;

                return 0;
            }
        }

        public double CurrentExplosiveResistance
        {
            get
            {
                if (ShieldMaxHitPoints > 0)
                    return ShieldResistanceExplosive;

                if (ArmorMaxHitPoints > 0)
                    return ArmorResistanceExplosive;

                if (StructureMaxHitPoints > 0)
                    return 0;

                return 0;
            }
        }

        public double CurrentKineticResistance
        {
            get
            {
                if (ShieldMaxHitPoints > 0)
                    return ShieldResistanceKinetic;

                if (ArmorMaxHitPoints > 0)
                    return ArmorResistanceKinetic;

                if (StructureMaxHitPoints > 0)
                    return 0;

                return 0;
            }
        }

        public double CurrentThermalResistance
        {
            get
            {
                if (ShieldMaxHitPoints > 0)
                    return ShieldResistanceThermal;

                if (ArmorMaxHitPoints > 0)
                    return ArmorResistanceThermal;

                if (StructureMaxHitPoints > 0)
                    return 0;

                return 0;
            }
        }

        public int Mode => _directEntity.Mode;

        // 0 = entityIdle
        // 1 = Approaching or entityCombat
        // 2 = entityMining
        // 3 = Warping (is this correct? entityApproaching)
        // 4 = Orbiting (is this correct? entityDeparting)
        // 5 = entityDeparting2
        // 6 = entityPursuit
        // 7 = entityFleeing
        // 8 =
        // 9 = entityOperating
        // 10 = entityEngage
        // 18 = entitySalvaging

        //entityApproaching = 3
        //entityCombat = 1
        //entityDeparting = 4
        //entityDeparting2 = 5
        //entityEngage = 10
        //entityFleeing = 7
        //entityIdle = 0
        //entityMining = 2
        //entityOperating = 9
        //entityPursuit = 6
        //entitySalvaging = 18

        public string Name => _directEntity.Name;

        public bool boolNameContains(string stringToSearchFor)
        {
            if (Name.ContainsIgnoreCase(stringToSearchFor))
                return true;

            if (Name.ToLower() != TypeName.ToLower())
            {
                if (TypeName.ContainsIgnoreCase(stringToSearchFor))
                    return true;
            }

            return false;
        }

        public bool IsBurnerMainNPC
        {
            get
            {
                switch (Name)
                {
                    case "Burner Jaguar":
                    case "Burner Hawk":
                    case "Burner Enyo":
                    case "Burner Vengeance":
                    case "Burner Cruor":
                    case "Burner Daredevil":
                    case "Burner Dramiel":
                    case "Burner Succubus":
                    case "Burner Worm":
                        return true;
                }

                return false;
            }
        }

        public double Nearest1KDistance
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_nearest1KDistance == null)
                            if (Distance > 0 && Distance < 900000000)
                                _nearest1KDistance = Math.Round(Distance / 1000, 0);

                        return _nearest1KDistance ?? Distance;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double Nearest5kDistance
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_nearest5KDistance == null)
                            if (Distance > 0 && Distance < 900000000)
                                _nearest5KDistance = Math.Ceiling(Math.Round(Distance / 1000) / 5.0) * 5;

                        return _nearest5KDistance ?? Distance;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public bool NpcHasNeutralizers
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (_npcHasNeutralizers == null)
                        _npcHasNeutralizers = _directEntity.NpcHasNeutralizers;

                    if (IsNeutralizingMe)
                        _npcHasNeutralizers = IsNeutralizingMe;

                    return (bool)_npcHasNeutralizers;
                }

                return false;
            }
        }

        public bool NpcHasALotOfRemoteRepair
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    //remote shield rep
                    if (Name.Contains("Fieldweaver") || Name.Contains("Plateweaver") || Name.Contains("Preserver"))
                        return true;

                    if (Name.Contains("Renewing") || Name.Contains("Plateforger"))
                        return true;

                    if (IsNPCCruiser && Name.Contains("Rodiva"))
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool NpcHasRemoteRepair
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (NpcRemoteArmorRepairChance > 0 || _npcRemoteShieldRepairChance > 0)
                        return true;
                    //remote shield rep
                    if (Name.Contains("Fieldweaver") || Name.Contains("Plateweaver") || Name.Contains("Preserver"))
                        return true;
                    //remote armor rep
                    if (Name.Contains("Renewing") || Name.Contains("Plateforger"))
                        return true;
                    //remote armor rep
                    if (Name.Contains("Anchoring"))
                        return true;

                    if (IsNPCCruiser && Name.Contains("Rodiva"))
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool WillYellowBoxButPosesLittleThreatToDrones
        {
            get
            {
                if (_directEntity != null && IsValid)
                {
                    if (Name.ToLower().Contains("Blastgrip Tessera".ToLower()))
                        return false; //this NPC is a threat to drones

                    if (Name.ToLower().Contains("Sparkgrip Tessera".ToLower()))
                        return false; //this NPC is a threat to drones

                    if (Name.ToLower().Contains("Strikegrip Tessera".ToLower()))
                        return false; //this NPC is a threat to drones

                    if (NpcHasRemoteRepair)
                        return true;

                    if (Name.ToLower().Contains("Striking Damavik".ToLower()))
                        return true;

                    return false;
                }

                return false;
            }
        }

        public double NpcRemoteArmorRepairChance
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_npcRemoteArmorRepairChance != null)
                            return (double)_npcRemoteArmorRepairChance;

                        if (_directEntity.NpcRemoteArmorRepairChance > 0)
                        {
                            _npcRemoteArmorRepairChance = _directEntity.NpcRemoteArmorRepairChance;
                            return (double)_npcRemoteArmorRepairChance;
                        }

                        return 0;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double NpcRemoteShieldRepairChance
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_npcRemoteShieldRepairChance != null)
                            return (double)_npcRemoteShieldRepairChance;

                        if (_directEntity.NpcRemoteArmorRepairChance > 0)
                        {
                            _npcRemoteShieldRepairChance = _directEntity.NpcRemoteArmorRepairChance;
                            return (double)_npcRemoteShieldRepairChance;
                        }

                        return 0;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double OptimalRange => _directEntity.OptimalRange;

        public PrimaryWeaponPriority PrimaryWeaponPriorityLevel
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_primaryWeaponPriorityLevel == null)
                        {
                            if (Combat.PrimaryWeaponPriorityTargets.Any(pt => pt.EntityID == Id))
                            {
                                _primaryWeaponPriorityLevel = Combat.PrimaryWeaponPriorityTargets.Where(t => t.Entity.IsTarget && t.EntityID == Id)
                                    .Select(pt => pt.PrimaryWeaponPriority)
                                    .FirstOrDefault();
                                return (PrimaryWeaponPriority)_primaryWeaponPriorityLevel;
                            }

                            return PrimaryWeaponPriority.NotUsed;
                        }

                        return (PrimaryWeaponPriority)_primaryWeaponPriorityLevel;
                    }

                    return PrimaryWeaponPriority.NotUsed;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return PrimaryWeaponPriority.NotUsed;
                }
            }
        }

        public bool SalvagersAvailable
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        bool result = false;
                        result |= ESCache.Instance.Modules.Any(m => m.GroupId == (int)Group.Salvager && m.IsOnline);
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public double ShieldMaxHitPoints => _directEntity.MaxShield ?? 0; //: it should be taking into account abyssal effects
        public double ShieldCurrentHitPoints => (_directEntity.MaxShield * _directEntity.ShieldPct) ?? 0;
        public double ShieldPct => Math.Round(_directEntity.ShieldPct, 2);
        public double ShieldResistanceEm => _directEntity.ShieldResistanceEM ?? 0; //: it should be taking into account abyssal effects
        public double ShieldResistanceExplosive => _directEntity.ShieldResistanceExplosive ?? 0; //: it should be taking into account abyssal effects
        public double ShieldResistanceKinetic => _directEntity.ShieldResistanceKinetic ?? 0; //: it should be taking into account abyssal effects
        public double ShieldResistanceThermal => _directEntity.ShieldResistanceThermal ?? 0; //: it should be taking into account abyssal effects
        public double StructureMaxHitPoints => _directEntity.MaxStructure ?? 0;
        public double StructureCurrentHitPoints => (_directEntity.MaxStructure * _directEntity.StructurePct) ?? 0;
        public double StructurePct => Math.Round(_directEntity.StructurePct, 2);


        public int? TargetValue
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_targetValue == null)
                        {
                            ShipTargetValue value = null;

                            try
                            {
                                if (ESCache.Instance.ShipTargetValues != null && ESCache.Instance.ShipTargetValues.Any(v => v.GroupId == GroupId))
                                    value = ESCache.Instance.ShipTargetValues.Find(v => v.GroupId == GroupId);

                                if (Name.Contains("Burner Enyo") ||
                                    Name.Contains("Burner Hawk") ||
                                    Name.Contains("Burner Jaguar") ||
                                    Name.Contains("Burner Vengeance"))
                                {
                                    _targetValue = 4;
                                    return _targetValue;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine(ex.ToString());
                            }

                            if (value == null)
                            {
                                if (IsNPCBattleship)
                                    _targetValue = 4;
                                else if (IsNPCBattlecruiser)
                                    _targetValue = 3;
                                else if (IsNPCCruiser)
                                    _targetValue = 2;
                                else if (IsNPCFrigate)
                                    _targetValue = 0;

                                return _targetValue ?? -1;
                            }

                            _targetValue = value.TargetValue;
                            return _targetValue;
                        }

                        return _targetValue;
                    }

                    return -1;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return -1;
                }
            }
        }

        public double? TransversalVelocity => _directEntity.TransversalVelocity;
        public double? TriglavianDamage => _directEntity.TriglavianDamage;
        public double? TriglavianDPS => _directEntity.TriglavianDPS;
        public int TypeId => _directEntity.TypeId;
        public string TypeName => _directEntity.TypeName;
        public double Velocity => _directEntity.Velocity;

        public double WarpScrambleChance
        {
            get
            {
                try
                {
                    if (_directEntity != null && IsValid)
                    {
                        //if (ESCache.Instance.InAbyssalDeadspace)
                        //    return 0;

                        if (IsWarpScramblingMe)
                            return 1;

                        if (Name.ToLower().Contains("nullwarp".ToLower()))
                            return 1;

                        if (Name.ToLower().Contains("anchoring".ToLower()))
                            return 1;

                        if (Name.ToLower().Contains("spearfisher".ToLower()))
                            return 1;

                        if (_warpScrambleChance != null)
                            return (double)_warpScrambleChance;

                        if (_directEntity.Attacks.Contains("effects.WarpScramble") || _directEntity.WarpScrambleChance > 0)
                        {
                            _warpScrambleChance = _directEntity.WarpScrambleChance;
                            return (double)_warpScrambleChance;
                        }

                        return 0;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public bool WillTryToStayWithin40kOfItsTarget
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (_directEntity.IsAbyssalKaren)
                        return false;

                    if (Name.Contains("Leshak"))
                        return false;

                    if (Name.Contains("Marshal"))
                        return false;

                    if (Name.Contains("Enforcer"))
                        return false;
                }

                return true;
            }
        }

        #endregion Properties

        /**
        private bool? _isAtCorrectRangeForMyCurrentAmmo;

        public bool IsAtCorrectRangeForMyCurrentAmmo
        {
            get
            {
                try
                {
                    if (_isAtCorrectRangeForMyCurrentAmmo != null)
                    {
                        return (bool)_isAtCorrectRangeForMyCurrentAmmo;
                    }
                    //
                    // Do I even have more than one ammo in cargo?
                    //
                    if (MissionSettings.AmmoTypesToLoad.Count > 1)
                    {
                        foreach (KeyValuePair<AmmoType, DateTime> ammoType in MissionSettings.AmmoTypesToLoad)
                        {
                            if (ESCache.Instance.Weapons.FirstOrDefault() != null)
                            {
                                if (ESCache.Instance.Weapons.FirstOrDefault().Charge != null)
                                {
                                    if (ammoType.Key.TypeId == ESCache.Instance.Weapons.FirstOrDefault().Charge.TypeId)
                                    {
                                        //
                                        // In Range for this DefinedAmmoTypes?
                                        //
                                        if (ammoType.Key.Range > Distance)
                                        {
                                            if (MissionSettings.AmmoTypesToLoad.Any(am => am.Key.Range > Distance))
                                            {
                                                _isAtCorrectRangeForMyCurrentAmmo = false;
                                                return (bool)_isAtCorrectRangeForMyCurrentAmmo;
                                            }

                                            _isAtCorrectRangeForMyCurrentAmmo = true;
                                            return (bool)_isAtCorrectRangeForMyCurrentAmmo;
                                        }

                                        _isAtCorrectRangeForMyCurrentAmmo = false;
                                        return (bool)_isAtCorrectRangeForMyCurrentAmmo;
                                    }
                                }
                            }
                        }

                        //if we cant figure out if its in range or not, assume it is
                        _isAtCorrectRangeForMyCurrentAmmo = true;
                        return (bool)_isAtCorrectRangeForMyCurrentAmmo;
                    }

                    _isAtCorrectRangeForMyCurrentAmmo = true;
                    return (bool)_isAtCorrectRangeForMyCurrentAmmo;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return true;
                }
            }
        }
        **/

        #region Methods

        public bool IsVulnerableAgainstCurrentDamageType => BestDamageTypes.FirstOrDefault() == MissionSettings.CurrentDamageType;

        public bool CloseCargoWindows()
        {
            foreach (DirectWindow tempWindow in ESCache.Instance.DirectEve.Windows)
            {
                if(tempWindow.Name.Contains("Wreck"))
                {
                    Log.WriteLine("CloseCargoWindows: Attempting to close window [" + tempWindow.Name + "]");
                    const bool forceWindowClose = true;
                    tempWindow.Close(forceWindowClose);
                }
            }

            return true;
        }

        public bool ActivateAccelerationGate(bool Force = false)
        {
            try
            {
                if (!RecursionCheck(nameof(ActivateAccelerationGate)))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(2))
                    return false;

                if (DateTime.UtcNow > Time.Instance.NextActivateAccelerationGate)
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCacheCreated)
                            Log.WriteLine("The EntityCache instance that represents [" + _directEntity.Name + "][" +
                                          Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                          MaskedId + "] was created [" + _thisEntityCacheCreated + "]");

                        if (!ESCache.Instance.OkToInteractWithEveNow)
                        {
                            Log.WriteLine("EntityCache: Activate: !OkToInteractWithEveNow");
                            return false;
                        }

                        if (!SafeToInitiateMovementCommand(Force))
                            return false;

                        if (!SafeToInitiateWarpJumpActivateCommand("Activate"))
                            return false;

                        if (ESCache.Instance.InAbyssalDeadspace)
                        {
                            CloseCargoWindows();
                            if (Name.Contains("Transfer Conduit"))
                            {
                                Log.WriteLine("Attempting to activate: [" + Name + "] at [" + Math.Round(Distance, 0) + "m] MyVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s].");
                                if (_directEntity.ActivateAbyssalAccelerationGate())
                                {
                                    ESCache.Instance.ClearPerSystemCache();
                                    ESCache.Instance.OldAccelerationGateId = Id;
                                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(12);
                                    return true;
                                }

                                Log.WriteLine("Failed to activate gate at [" + Math.Round(Distance, 0) + "k]");
                                return false;
                            }

                            if (Name.Contains("Origin Conduit"))
                            {
                                Log.WriteLine("Attempting to activate: [" + Name + "] at [" + Math.Round(Distance, 0) + "m] MyVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s]!!!!");
                                if (TypeId == (int)TypeID.AbyssExitGate && _directEntity.ActivateAbyssalEndGate())
                                {
                                    ESCache.Instance.ClearPerSystemCache();
                                    Log.WriteLine("Exiting Abyssal Deadspace");
                                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(12);
                                    ESCache.Instance.OldAccelerationGateId = Id;
                                    return true;
                                }

                                if (TypeId == (int)TypeID.VoidSpaceExitGate && _directEntity.ActivateVoidSpaceEndGate())
                                {
                                    ESCache.Instance.ClearPerSystemCache();
                                    Log.WriteLine("Exiting VoidSpace");
                                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(12);
                                    ESCache.Instance.OldAccelerationGateId = Id;
                                    return true;
                                }

                                Log.WriteLine("Failed to activate gate at [" + Math.Round(Distance, 0) + "k]");
                                return false;
                            }
                        }

                        if (GroupId == (int)Group.AbyssalTrace)
                        {
                            if (Name.Contains("Abyssal Trace"))
                            {
                                Log.WriteLine("Attempting to activate AbyssalTrace [" + Name + "] at [" + Nearest1KDistance + "k].");
                                if (_directEntity.ActivateAbyssalEntranceAccelerationGate())
                                {
                                    return true;
                                }

                                Log.WriteLine("Failed to activate gate at [" + Math.Round(Distance, 0) + "k]");
                                return false;
                            }

                            return false;
                        }

                        Log.WriteLine("Attempting to activate: [" + Name + "] at [" + Math.Round(Distance, 0) + "m] MyVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s]!!");
                        if (_directEntity.Activate())
                        {
                            if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastInteractedWithEVE.AddSeconds(1))
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);

                            ESCache.Instance.ClearPerPocketCache();
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Gate Activated [" + Name + "]"));
                            if (ESCache.Instance.EveAccount.IsLeader && DirectEve.Interval(7000))
                            {
                                if (ESCache.Instance.EveAccount.LeaderLastEntityIdActivate != Id)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdActivate), Id);

                                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastActivate.AddSeconds(10))
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastActivate), DateTime.UtcNow);
                            }

                            return true;
                        }

                        Log.WriteLine("Failed to activate gate at [" + Math.Round(Distance, 0) + "]k");
                        return false;
                    }

                    Log.WriteLine("[" + Name + "] DirecEntity is null or is not valid");
                    return false;
                }

                Log.WriteLine("You have another [" + Math.Round(Time.Instance.NextActivateAccelerationGate.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                              "] sec before we should attempt to activate [" + Name + "], waiting.");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool ActuallyWarpTo(double range = 0)
        {
            try
            {
                if (!RecursionCheck(nameof(ActuallyWarpTo)))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(2))
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("ActuallyWarpTo: if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(2)) return false;");
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextWarpAction)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (_directEntity != null && IsValid)
                        {
                            if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCacheCreated)
                                Log.WriteLine("The EntityCache instance that represents [" + _directEntity.Name + "][" +
                                              Math.Round(_directEntity.Distance / 1000, 0) +
                                              "k][" + MaskedId + "] was created [" + _thisEntityCacheCreated + "]");

                            if (ESCache.Instance.ActiveShip.IsWarpScrambled || ESCache.Instance.ActiveShip.IsWarpDisrupted)
                            {
                                try
                                {
                                    if (ESCache.Instance.ClosestStargate.Distance > (double)Distances.WarptoDistance)
                                    {
                                        ESCache.Instance.ClosestStargate._directEntity.Approach();
                                        return false;
                                    }
                                }
                                catch (Exception){}

                                try
                                {
                                    if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
                                    {
                                        if (ESCache.Instance.ClosestStargate.Distance > (double)Distances.GateActivationRangeWhileCloaked)
                                        {
                                            ESCache.Instance.ClosestStargate._directEntity.Approach();
                                            return false;
                                        }
                                    }
                                    else if (ESCache.Instance.ClosestStargate.Distance > (double)Distances.GateActivationRange)
                                    {
                                        ESCache.Instance.ClosestStargate._directEntity.Approach();
                                        return false;
                                    }
                                }
                                catch (Exception){}

                                ESCache.Instance.ClosestStargate.Jump();
                                return false;
                            }

                            if (Distance < (long)Distances.HalfOfALightYearInAu)
                            {
                                if (Distance > (int)Distances.WarptoDistance)
                                {
                                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                                        return false;

                                    if (ESCache.Instance.ActiveShip.IsScrambled)
                                        return false;

                                    if (!ESCache.Instance.OkToInteractWithEveNow)
                                    {
                                        if (DebugConfig.DebugInteractWithEve) Log.WriteLine("EntityCache: WarpTo: !OkToInteractWithEveNow");
                                        return false;
                                    }

                                    //AlignTo();

                                    if (!HasInitiatedWarp && !ESCache.Instance.ActiveShip.IsWarpScrambled && _directEntity.WarpTo(range))
                                    {
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                                        Drones.DronesShouldBePulled = true;
                                        Time.Instance.WehaveMoved = DateTime.UtcNow;
                                        Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                                        Time.Instance.NextWarpAction = DateTime.UtcNow.AddSeconds(Time.Instance.WarptoDelay_seconds);

                                        if (ESCache.Instance.DirectEve.Session.SolarSystemId != null)
                                        {
                                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.SolarSystem), ESCache.Instance.DirectEve.GetLocationName((long)ESCache.Instance.DirectEve.Session.SolarSystemId));
                                            /**
                                            if (ESCache.Instance.EveAccount.IsLeader)
                                            {
                                                if (ESCache.Instance.EveAccount.LeaderIsInSystemId != (long)ESCache.Instance.DirectEve.Session.SolarSystemId ||
                                                    ESCache.Instance.EveAccount.LeaderIsInSystemName != ESCache.Instance.DirectEve.GetLocationName((long)ESCache.Instance.DirectEve.Session.SolarSystemId))
                                                {
                                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderIsInSystemName), ESCache.Instance.DirectEve.GetLocationName((long)ESCache.Instance.DirectEve.Session.SolarSystemId));
                                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderIsInSystemId), (long)ESCache.Instance.DirectEve.Session.SolarSystemId);
                                                }

                                                if (ESCache.Instance.EveAccount.LeaderLastEntityIdWarp != Id)
                                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdWarp), Id);

                                                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastWarp.AddSeconds(5))
                                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastWarp), DateTime.UtcNow);
                                            }
                                            **/
                                        }

                                        if (ESCache.Instance.MyShipEntity != null)
                                        {
                                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.ShipType), ESCache.Instance.MyShipEntity.TypeName);
                                        }

                                        return true;
                                    }

                                    return false;
                                }

                                Log.WriteLine("[" + Name + "] Distance [" + Math.Round(Distance / 1000, 0) +
                                              "k] is not greater then 150k away, WarpTo aborted!");
                                return false;
                            }

                            Log.WriteLine("[" + Name + "] Distance [" + Math.Round(Distance / 1000, 0) +
                                          "k] was greater than 5000AU away, we assume this an error!, WarpTo aborted!");
                            return false;
                        }

                        Log.WriteLine("[" + Name + "] DirecEntity is null or is not valid");
                        return false;
                    }

                    Log.WriteLine("We have not yet been in space at least 2 seconds, waiting!");
                    return false;
                }

                if (DebugConfig.DebugTraveler) Log.WriteLine("ActuallyWarpTo: if (DateTime.UtcNow > Time.Instance.NextWarpAction)");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool AlignTo(bool Force = false)
        {
            try
            {
                if (!RecursionCheck(nameof(AlignTo)))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(2))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastAlign.AddSeconds(15))
                    return true;

                if (DateTime.UtcNow > Time.Instance.NextAlign)
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCacheCreated)
                            Log.WriteLine("The EntityCache instance that represents [" + _directEntity.Name + "][" +
                                          Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                          MaskedId + "]was created [" + _thisEntityCacheCreated + "]");

                        if (!ESCache.Instance.OkToInteractWithEveNow)
                        {
                            if (DebugConfig.DebugInteractWithEve) Log.WriteLine("EntityCache: AlignTo: !OkToInteractWithEveNow");
                            return false;
                        }

                        if (SafeToInitiateMovementCommand(Force) && _directEntity.AlignTo())
                        {
                            Log.WriteLine("Aligning to [" + Name + "]");
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                            if (ESCache.Instance.EveAccount.IsLeader)
                            {
                                if (ESCache.Instance.EveAccount.LeaderLastEntityIdAlign != Id)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdAlign), Id);

                                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastAlign.AddSeconds(5))
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastAlign), DateTime.UtcNow);
                            }

                            Time.Instance.WehaveMoved = DateTime.UtcNow;
                            Time.Instance.NextAlign = DateTime.UtcNow.AddSeconds(Time.Instance.AlignDelay_seconds);
                            Time.Instance.LastAlign = DateTime.UtcNow;
                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool Approach()
        {
            try
            {
                if (!RecursionCheck(nameof(Approach)))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(2))
                    return false;

                if (DateTime.UtcNow > Time.Instance.NextApproachAction || ESCache.Instance.ActiveShip.FollowingEntity != this._directEntity)
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCacheCreated)
                            Log.WriteLine("The EntityCache instance that represents [" + _directEntity.Name + "][" +
                                          Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                          MaskedId + "] was created [" + _thisEntityCacheCreated + "]");

                        //if (Distance > 5000)
                        //{
                        //    if (DebugConfig.UseMoveToAStarWhenOrbitKeepAtRangeEtc && !DirectEntity.MoveToViaAStar(2000, forceRecreatePath: false, dest: _directEntity.DirectAbsolutePosition, ignoreAbyssEntites: true))
                        //    {
                        //        return false;
                        //    }
                        //}

                        if (!ESCache.Instance.OkToInteractWithEveNow)
                        {
                            if (DebugConfig.DebugInteractWithEve) Log.WriteLine("EntityCache: Approach: !OkToInteractWithEveNow");
                            return false;
                        }

                        if (DateTime.UtcNow < Time.Instance.LastActivateAbyssalActivationWindowAttempt.AddSeconds(10))
                            return false;

                        if (DateTime.UtcNow < Time.Instance.LastActivateKeyActivationWindowAttempt.AddSeconds(4))
                            return false;

                        if (SafeToInitiateMovementCommand(false))
                        {
                            if (IsStation || IsCitadel)
                            {
                                if (_directEntity.MoveTo())
                                {
                                    Log.WriteLine("MoveTo [" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "]");
                                    Defense.AlwaysActivateSpeedModForThisGridOnly = true;
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                                    Time.Instance.LastApproachAction = DateTime.UtcNow;
                                    ESCache.Instance.MyShipEntity._directEntity._followId = Id;
                                    Time.Instance.NextApproachAction = DateTime.UtcNow.AddSeconds(Time.Instance.ApproachDelay_seconds + ESCache.Instance.RandomNumber(1, 3));
                                    Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(Time.Instance.ApproachDelay_seconds);
                                    if (ESCache.Instance.EveAccount.IsLeader)
                                    {
                                        if (ESCache.Instance.EveAccount.LeaderLastEntityIdApproach != Id)
                                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdApproach), Id);

                                        if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastApproach.AddSeconds(5))
                                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastApproach), DateTime.UtcNow);
                                    }
                                }
                            }
                            else
                            {
                                //if (!IsApproachedByActiveShip && _directEntity.Approach())
                                if (_directEntity.Approach())
                                {
                                    Log.WriteLine("Approach: [" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "]");
                                    Time.Instance.LastApproachAction = DateTime.UtcNow;
                                    ESCache.Instance.MyShipEntity._directEntity._followId = Id;
                                    Time.Instance.NextApproachAction = DateTime.UtcNow.AddSeconds(Time.Instance.ApproachDelay_seconds + ESCache.Instance.RandomNumber(1, 5));
                                    Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(Time.Instance.ApproachDelay_seconds);
                                    if (ESCache.Instance.EveAccount.IsLeader)
                                    {
                                        if (ESCache.Instance.EveAccount.LeaderLastEntityIdApproach != Id)
                                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdApproach), Id);

                                        if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastApproach.AddSeconds(5))
                                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastApproach), DateTime.UtcNow);
                                    }

                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public void CheckForDamageAndSetNeedsRepair()
        {
            //If we NeeRepair is true we do not need to check anything, we will repair when in station
            if (ESCache.Instance.EveAccount.NeedRepair)
            {
                ESCache.Instance.NeedRepair = true;
                return;
            }

            foreach (ModuleCache module in ESCache.Instance.Modules)
            {
                if (module.DamagePercent > 0)
                {
                    ESCache.Instance.NeedRepair = true;
                    Log.WriteLine("We have a damaged module [" + module.TypeName + "] with DamagePercent [" + Math.Round(module.DamagePercent, 2) + "] Setting NeedRepair");
                    return;
                }
            }

            if (ESCache.Instance.ActiveShip.ArmorPercentage < 100 && ESCache.Instance.ActiveShip.IsShieldTanked)
            {
                ESCache.Instance.NeedRepair = true;
                Log.WriteLine("We have Armor Damage:  [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "] Setting NeedRepair");
                return;
            }

            if (ESCache.Instance.ActiveShip.StructurePercentage < 100)
            {
                ESCache.Instance.NeedRepair = true;
                Log.WriteLine("We have Structure Damage:  [" + Math.Round(ESCache.Instance.ActiveShip.StructurePercentage, 2) + "] Setting NeedRepair");
                return;
            }

            return;
        }



        public bool Dock(bool Force = false)
        {
            try
            {
                if (!RecursionCheck(nameof(Dock)))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(2))
                    return false;

                if (ESCache.Instance.EveAccount.DoNotSessionChange)
                {
                    Log.WriteLine("DoNotSessionChange: true - waiting for other EVE Accounts to completely login before session changing (avoid crashing...)");
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextDockAction)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (_directEntity != null && IsValid)
                        {
                            if (!ESCache.Instance.OkToInteractWithEveNow)
                            {
                                if (DebugConfig.DebugInteractWithEve) Log.WriteLine("EntityCache: Dock: !OkToInteractWithEveNow");
                                return false;
                            }

                            if (!SafeToInitiateMovementCommand(Force))
                                return false;

                            if (!SafeToInitiateWarpJumpActivateCommand("Dock"))
                                return false;

                            CheckForDamageAndSetNeedsRepair();

                            if (_directEntity.Dock())
                            {
                                Log.WriteLine("Docking [" + _directEntity.Name + "][" + _directEntity.Distance + "m]");
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                                if (ESCache.Instance.EveAccount.IsLeader)
                                {
                                    if (ESCache.Instance.EveAccount.LeaderLastEntityIdDock != Id)
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdDock), Id);

                                    if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastDock.AddSeconds(10))
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastDock), DateTime.UtcNow);
                                }

                                if (ESCache.Instance.MyShipEntity != null)
                                {
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.ShipType), ESCache.Instance.MyShipEntity.TypeName);
                                }

                                ESCache.Instance.ClearPerSystemCache();
                                Drones.ResetInSpaceSettingsWhenEnteringStation();
                                Time.Instance.WehaveMoved = DateTime.UtcNow.AddDays(-7);
                                Time.Instance.NextApproachAction = DateTime.UtcNow.AddSeconds(Time.Instance.DockingDelay_seconds);
                                Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerJumpedGateNextCommandDelay_seconds);
                            }
                        }
                        return false;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool Jump(bool Force = false)
        {
            try
            {
                if (!RecursionCheck(nameof(Jump)))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddMilliseconds(500))
                    return false;

                if (ESCache.Instance.EveAccount.DoNotSessionChange)
                {
                    Log.WriteLine("DoNotSessionChange: true - waiting for other EVE Accounts to completely login before session changing (avoid crashing...)");
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextJumpAction)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (_directEntity != null && IsValid)
                        {
                            if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCacheCreated)
                                Log.WriteLine("The EntityCache instance that represents [" + _directEntity.Name + "][" +
                                              Math.Round(_directEntity.Distance / 1000, 0) +
                                              "k][" + MaskedId + "] was created [" + _thisEntityCacheCreated + "]");

                            if (Distance <= (double)Distances.JumpRange)
                            {
                                if (!ESCache.Instance.OkToInteractWithEveNow)
                                {
                                    if (DebugConfig.DebugInteractWithEve) Log.WriteLine("Jump: !OkToInteractWithEveNow");
                                    return false;
                                }

                                if (!SafeToInitiateMovementCommand(Force))
                                    return false;

                                if (!SafeToInitiateWarpJumpActivateCommand("WarpTo"))
                                    return false;

                                if (_directEntity.Jump())
                                {
                                    State.CurrentInstaStationDockState = InstaStationDockState.JustArrivedInSystem;
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                                    if (ESCache.Instance.EveAccount.LeaderLastEntityIdJump != Id)
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdJump), Id);

                                    if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastJump.AddSeconds(7))
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastJump), DateTime.UtcNow);

                                    ESCache.Instance.ClearPerSystemCache();
                                    Time.Instance.WehaveMoved = DateTime.UtcNow.AddDays(-7);
                                    Time.Instance.NextJumpAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(10, 15));
                                    Time.Instance.LastJumpAction = DateTime.UtcNow;
                                    Time.Instance.NextTravelerAction = DateTime.UtcNow.AddMilliseconds(500);
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerJumpedGateNextCommandDelay_seconds);
                                    return true;
                                }
                            }

                            if (DebugConfig.DebugTraveler)
                                Log.WriteLine("we tried to jump through [" + Name + "] but it is [" + Math.Round(Distance / 1000, 2) + "k away][" + MaskedId +
                                              "]");
                            return false;
                        }

                        if (DebugConfig.DebugTraveler) Log.WriteLine("[" + Name + "] DirecEntity is null or is not valid");
                        return false;
                    }

                    if (DebugConfig.DebugTraveler) Log.WriteLine("We have not yet been in space for 2 seconds, waiting");
                    return false;
                }

                if (DebugConfig.DebugTraveler) Log.WriteLine("We just jumped. We should wait at least another [" + Math.Round(Time.Instance.NextJumpAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "] sec before trying to jump again.");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool KeepAtRange(int range, bool allowNegativeRanges = false, bool force = false)
        {
            try
            {
                if (!RecursionCheck(nameof(KeepAtRange)))
                    return false;

                //if (Distance > range + 5000)
                //{
                //    if (DebugConfig.UseMoveToAStarWhenOrbitKeepAtRangeEtc && !DirectEntity.MoveToViaAStar(2000, forceRecreatePath: false, dest: _directEntity.DirectAbsolutePosition, ignoreAbyssEntites: true))
                //    {
                //        return false;
                //    }
                //}

                if (DateTime.UtcNow > Time.Instance.NextApproachAction || force)
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCacheCreated)
                            Log.WriteLine("The EntityCache instance that represents [" + _directEntity.Name + "][" +
                                          Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                          MaskedId + "] was created [" + _thisEntityCacheCreated + "]");

                        if (!ESCache.Instance.OkToInteractWithEveNow && !force)
                        {
                            if (DebugConfig.DebugInteractWithEve) Log.WriteLine("EntityCache: KeepAtRange: !OkToInteractWithEveNow");
                            return false;
                        }

                        if (!allowNegativeRanges && range < 0)
                            range = 0;

                        if (IsApproachedByActiveShip && !force)
                            return true;

                        if (_directEntity.KeepAtRange(range, force))
                        {
                            Log.WriteLine("KeepAtRange [" + range + "][" + Name + "] Current Distance [" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "]");
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                            Time.Instance.LastApproachAction = DateTime.UtcNow;
                            Time.Instance.NextApproachAction = DateTime.UtcNow.AddSeconds(Time.Instance.ApproachDelay_seconds + ESCache.Instance.RandomNumber(0, 2));
                            if (ESCache.Instance.EveAccount.IsLeader)
                            {
                                if (ESCache.Instance.EveAccount.LeaderLastEntityIdKeepAtRange != Id)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdKeepAtRange), Id);

                                if (ESCache.Instance.EveAccount.LeaderLastKeepAtRangeDistance != range)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastKeepAtRangeDistance), range);

                                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastKeepAtRange.AddSeconds(5))
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastKeepAtRange), DateTime.UtcNow);
                            }

                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool LockTarget(string module)
        {
            try
            {
                if (!RecursionCheck(nameof(LockTarget)))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(5))
                    return false;

                if (DateTime.UtcNow < Time.Instance.NextTargetAction)
                    return false;

                if (ESCache.Instance.InsidePosForceField)
                    return false;

                if (_directEntity != null && IsValid)
                {
                    if (!IsTarget)
                    {
                        if (!HasExploded)
                        {
                            if (Distance + 1000 < Combat.MaxTargetRange)
                            {
                                if (ESCache.Instance.Targets.Count < ESCache.Instance.MaxLockedTargets)
                                {
                                    if (!IsTargeting)
                                    {
                                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == Id))
                                        {
                                            if (ESCache.Instance.MyShipEntity.Id == Id && Distance == 0)
                                            {
                                                Log.WriteLine("[" + module + "] You cannot target yourself! aborting targeting attempt");
                                                return false;
                                            }

                                            if (!ESCache.Instance.EveAccount.UseFleetMgr)
                                            {
                                                if (IsBadIdea && !IsAttacking && ESCache.Instance.SelectedController != "CombatDontMoveController")
                                                {
                                                    Log.WriteLine(
                                                        "[" + module + "] Attempted to target a player or concord entity! [" + Name + "] - aborting");
                                                    return false;
                                                }
                                            }

                                            if (Distance >= 250001 || Distance > Combat.MaxTargetRange)
                                            {
                                                Log.WriteLine("[" + module + "] tried to lock [" + Name + "] which is [" +
                                                              Math.Round(Distance / 1000, 2) +
                                                              "k] away. Do not try to lock things that you cant possibly target");
                                                return false;
                                            }

                                            foreach (EntityCache target in ESCache.Instance.EntitiesOnGrid.Where(e => e.IsTarget && ESCache.Instance.TargetingIDs.ContainsKey(e.Id)))
                                                ESCache.Instance.TargetingIDs.Remove(target.Id);

                                            if (ESCache.Instance.TargetingIDs.ContainsKey(Id))
                                            {
                                                DateTime lastTargeted = ESCache.Instance.TargetingIDs[Id];

                                                double seconds = DateTime.UtcNow.Subtract(lastTargeted).TotalSeconds;
                                                if (seconds < 5)
                                                {
                                                    Log.WriteLine("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) +
                                                                  "k][" + MaskedId +
                                                                  "][" + ESCache.Instance.Targets.Count + "] targets already, can reTarget in [" +
                                                                  Math.Round(5 - seconds, 0) + "]");
                                                    return false;
                                                }
                                            }

                                            if (!ESCache.Instance.OkToInteractWithEveNow)
                                            {
                                                if (DebugConfig.DebugInteractWithEve) Log.WriteLine("LockTarget: !OkToInteractWithEveNow");
                                                return false;
                                            }

                                            if (!IsValid)
                                            {
                                                Log.WriteLine("LockTarget: if (!IsValid)");
                                                return false;
                                            }

                                            // We can't target this entity yet!
                                            if (!ESCache.Instance.DirectEve.CanTarget(Id))
                                            {
                                                Log.WriteLine("LockTarget: if (!ESCache.Instance.DirectEve.CanTarget(Id))");
                                                return false;
                                            }

                                            if (ESCache.Instance.SelectedController == "AbyssalDeadspacveController" && Statistics.StartedPocket.AddSeconds(8) > DateTime.UtcNow)
                                                return false;

                                            if (_directEntity.LockTarget())
                                            {
                                                Combat.PotentialCombatTargetsCount_AtLastLockedTarget = Combat.CurrentPotentialCombatTargetsCount;
                                                Time.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                                                if (ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.CombatDontMoveController))
                                                    ESCache.Instance.TargetingIDs[Id] = DateTime.UtcNow;
                                                if (DirectEve.Interval(5000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Target Locked [" + TypeName + "] at [" + Nearest1KDistance + "k] ID[" + MaskedId + "]"));
                                                if (!Statistics.BountyValues.ContainsKey(Id))
                                                    try
                                                    {
                                                        double bounty = _directEntity.GetBounty();
                                                        if (bounty > 0)
                                                        {
                                                            Log.WriteLine("Added bounty [" + bounty + "] for [" + Name + "] at [" + Math.Round(Distance / 1000, 0) + "k] Id [" + MaskedId + "] - locking target");
                                                            Statistics.BountyValues.AddOrUpdate(Id, bounty);
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Log.WriteLine("Exception [" + ex + "]");
                                                    }
                                                return true;
                                            }

                                            Log.WriteLine("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" +
                                                          MaskedId + "][" +
                                                          ESCache.Instance.Targets.Count + "] LockedTargets already, LockTarget failed (unknown reason)");
                                            return false;
                                        }

                                        Log.WriteLine("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" +
                                                      MaskedId +
                                                      "][" +
                                                      ESCache.Instance.Targets.Count +
                                                      "] LockedTargets already, LockTarget failed: target was not in Entities List");
                                        return false;
                                    }

                                    Log.WriteLine("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId +
                                                  "][" +
                                                  ESCache.Instance.Targets.Count +
                                                  "] targets already, LockTarget aborted: target is already being targeted");
                                    return false;
                                }

                                Log.WriteLine("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId +
                                              "][" +
                                              ESCache.Instance.Targets.Count + "] targets already, we only have [" + ESCache.Instance.MaxLockedTargets +
                                              "] slots!");
                                return false;
                            }

                            Log.WriteLine("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + "][" +
                                          ESCache.Instance.Targets.Count + "] targets already, my targeting range is only [" +
                                          Math.Round(Combat.MaxTargetRange / 1000, 2) + "k]!");
                            return false;
                        }

                        Log.WriteLine("[" + module + "] tried to lock [" + Name + "][" + ESCache.Instance.Targets.Count +
                                      "] targets already, target is already dead!");
                        return false;
                    }

                    Log.WriteLine("[" + module + "] LockTarget request has been ignored for [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" +
                                  MaskedId + "][" +
                                  ESCache.Instance.Targets.Count + "] targets already, target is already locked!");
                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool MakeActiveTarget()
        {
            if (ESCache.Instance.SelectedController == "AbyssalDeadspaceController" && Statistics.StartedPocket.AddSeconds(8) > DateTime.UtcNow)
                return false;

            return _directEntity.MakeActiveTarget();
        }

        public bool OpenCargo()
        {
            try
            {
                if (!RecursionCheck(nameof(OpenCargo)))
                    return false;

                if (DateTime.UtcNow > Time.Instance.NextOpenCargoAction)
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (_directEntity.OpenCargo())
                        {
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                            Time.Instance.NextOpenCargoAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(1, 2));
                            Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool Orbit(int orbitRange, bool Force = false, string LogMessage = "")
        {
            try
            {
                if (!RecursionCheck(nameof(Orbit)))
                    return false;

                if (!DirectEve.HasFrameChanged())
                    return true;

                if (!ESCache.Instance.InSpace)
                    return false;

                if (ActionControl.PerformingLootActionNow && GroupId == (int)Group.AccelerationGate)
                    return false;

                if (ESCache.Instance.InAbyssalDeadspace && Drones.UseDrones && Statistics.StartedPocket.AddSeconds(8) > DateTime.UtcNow)
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(2))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastJumpAction.AddSeconds(4))
                    return false;

                if (ESCache.Instance.MyShipEntity.FollowId == Id)
                {
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (50000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (700 > ESCache.Instance.ActiveShip.Entity.Velocity && !DirectEve.Interval(6000, 6000, orbitRange.ToString() + Id))
                                return false;

                            if (1500 > ESCache.Instance.ActiveShip.Entity.Velocity && !DirectEve.Interval(4000, 4000, orbitRange.ToString() + Id))
                                return false;
                        }
                    }
                    else
                    {
                        if (700 > ESCache.Instance.ActiveShip.Entity.Velocity && !DirectEve.Interval(6000, 6000, orbitRange.ToString() + Id))
                            return false;

                        if (1500 > ESCache.Instance.ActiveShip.Entity.Velocity && !DirectEve.Interval(4000, 4000, orbitRange.ToString() + Id))
                            return false;
                    }
                }

                //if (Distance > orbitRange + 5000)
                //{
                //    if (DebugConfig.UseMoveToAStarWhenOrbitKeepAtRangeEtc && !DirectEntity.MoveToViaAStar(2000, forceRecreatePath: false, dest: _directEntity.DirectAbsolutePosition, ignoreAbyssEntites: true))
                //    {
                //        return false;
                //    }
                //}

                if (DirectEntity.IntervalForMovementCommands(4000, 7000, Id.ToString() + orbitRange) || (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.FollowingEntity != null && ESCache.Instance.ActiveShip.FollowingEntity.Id != this._directEntity.Id) || Force)
                {
                    //even if we FORCE we need to not spam commands crazy quickly!
                    //if (!DirectEntity.IntervalForMovementCommands(500, 800, Id.ToString() + orbitRange))
                    //    return true;

                    if (_directEntity != null && IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCacheCreated)
                            Log.WriteLine("The EntityCache instance that represents [" + _directEntity.Name + "][" +
                                          Math.Round(_directEntity.Distance, 0) + "m][" +
                                          MaskedId + "] was created [" + _thisEntityCacheCreated + "]");

                        if (!ESCache.Instance.OkToInteractWithEveNow && !Force)
                        {
                            if (DebugConfig.DebugInteractWithEve) Log.WriteLine("EntityCache: Orbit: !OkToInteractWithEveNow");
                            return false;
                        }

                        if (ESCache.Instance.InAbyssalDeadspace)
                        {
                            if (!Combat.PotentialCombatTargets.Any())
                            {
                                if (IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                                {
                                    Approach();
                                    return true;
                                }

                                if (IsWreck && !IsWreckEmpty)
                                {
                                    _directEntity.Approach();
                                    return true;
                                }

                                if (IsAccelerationGate)
                                {
                                    _directEntity.Approach();
                                    return true;
                                }
                            }
                        }

                        if (orbitRange <= 0) orbitRange = 500;
                        if (SafeToInitiateMovementCommand(Force) && _directEntity.Orbit(orbitRange)) //&& _directEntity.Orbit(orbitRange, false))
                        {
                            if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastInteractedWithEVE.AddSeconds(1))
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);

                            if (ESCache.Instance.EveAccount.IsLeader)
                            {
                                if (ESCache.Instance.EveAccount.LeaderLastEntityIdOrbit != Id)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastEntityIdOrbit), Id);

                                if (ESCache.Instance.EveAccount.LeaderLastOrbitDistance != orbitRange)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastOrbitDistance), orbitRange);

                                if (DateTime.UtcNow > ESCache.Instance.EveAccount.LeaderLastOrbit.AddSeconds(3))
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastOrbit), DateTime.UtcNow);
                            }
                            if (!string.IsNullOrEmpty(LogMessage))
                                Log.WriteLine(LogMessage);
                            //else
                            //    Log.WriteLine("Initiating Orbit [" + Name + "][" + Math.Round(Distance / 1000, 0) + "k] at [" + Math.Round((double)orbitRange / 1000, 0) + "k][" + MaskedId + "]!.!");

                            Time.Instance.NextOrbit = DateTime.UtcNow.AddSeconds(5 + ESCache.Instance.RandomNumber(1, 5));
                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        internal bool ApproachUsingMoveToAStar()
        {
            if (_directEntity.DirectAbsolutePosition != null)
            {
                if (DirectEntity.MoveToViaAStar(2000, distanceToTarget: 10000, forceRecreatePath: false, dest: _directEntity.DirectAbsolutePosition,
                        ignoreAbyssEntities: true,
                        ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                        ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                        ignoreWideAreaAutomataPylon: true,
                        ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                        ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                        ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost
                        ))
                {
                    //once we are within 10k use regular approach!
                    this.Approach();
                }

                return true;
            }

            return false;
        }

        public bool? OrbitAt10KLessThanTarget(int distanceToCheck, int finalOrbitRange)
        {
            if (!RecursionCheck(nameof(OrbitAt10KLessThanTarget)))
                return false;

            const bool forceReOrbit = true;
            distanceToCheck = Math.Max(distanceToCheck, finalOrbitRange);

            if (Distance > distanceToCheck)
            {
                if (Orbit(distanceToCheck - 10000, forceReOrbit))
                {
                    if (finalOrbitRange == distanceToCheck) return true;
                    return false;
                }

                return false;
            }

            return null;
        }

        public bool SafeToInitiateMovementCommand(bool Force)
        {
            try
            {
                if (!RecursionCheck(nameof(SafeToInitiateMovementCommand)))
                    return false;

                if (Force) return true;

                if (ESCache.Instance.AccelerationGates.Count > 0)
                {
                    if ((int)Distances.JumpRange > ESCache.Instance.AccelerationGates.FirstOrDefault().Distance)
                        return true;
                }

                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (ESCache.Instance.Modules.Any(i => i.IsMicroWarpDrive))
                        return true;

                    if (ESCache.Instance.ActiveShip.IsAssaultShip)
                        return true;

                    if (ESCache.Instance.ActiveShip.IsDestroyer)
                        return true;

                    if (ESCache.Instance.ActiveShip.IsFrigate)
                        return true;

                    if (Statistics.StartedPocket.AddSeconds(14) > DateTime.UtcNow)
                        return false;
                }

                if (DateTime.UtcNow < Time.Instance.LastActivateAbyssalActivationWindowAttempt.AddSeconds(20))
                    return false;

                if (Time.Instance.NextActivateAccelerationGate > DateTime.UtcNow)
                {
                    return false;
                }

                if (Time.Instance.NextJumpAction > DateTime.UtcNow)
                {
                    return false;
                }

                if (Time.Instance.NextDockAction > DateTime.UtcNow)
                {
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.LastInitiatedWarp.AddSeconds(6))
                {
                    Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.LastInitiatedWarp.AddSeconds(6)) waiting");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.LastAlign.AddSeconds(1))
                {
                    Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.LastAlign.AddSeconds(4)) waiting");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.LastApproachAction.AddSeconds(2))
                {
                    Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.LastApproachAction.AddSeconds(4)) waiting");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.NextActivateAccelerationGate)
                {
                    Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.NextActivateAction) waiting");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.NextOrbit.AddSeconds(-5))
                {
                    Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.NextOrbit) waiting");
                    return false;
                }

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                {
                    Log.WriteLine("SafeToInitiateMovementCommand: f (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)");
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public bool SafeToInitiateWarpJumpActivateCommand(string module)
        {
            try
            {
                if (!RecursionCheck(nameof(SafeToInitiateWarpJumpActivateCommand)))
                    return false;

                if (ESCache.Instance.EntitiesOnGrid.All(i => !i.IsWarpScramblingMe && i.WarpScrambleChance == 0))
                {
                    //
                    // If we are using drones and if drones are in space that are not incapacitated
                    //
                    if (Drones.UseDrones && Drones.ActiveDroneCount > 0 && Drones.ActiveDrones.All(i => i.StructurePct > .2) && Drones.WaitForDronesToReturn)
                    {
                        Log.WriteLine("[" + module + "] We Attempted to warp with drones in space: Recall Drones");
                        Drones.RecallDrones();
                        return false;
                    }

                    //
                    // we are not warp scrambled and we have no drones out
                    //
                    return true;
                }

                //
                // we are warp scrambled!
                //
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return true;
            }
        }

        public bool SpiralInProgressively(int finalOrbitRange)
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(2))
                    return false;

                int DistanceToCheck = 100000;
                bool? spiralResult = OrbitAt10KLessThanTarget(DistanceToCheck, finalOrbitRange);
                //
                // null means we arent within that range (yet)
                //
                if (spiralResult == null) return false;
                //
                // true means we have reached the desired final orbit range
                //
                if ((bool)spiralResult) return true;
                //
                // false means we arent yet at the final distance, but we may have orbited at an intermediary distance ok...
                //
                DistanceToCheck = 90000;
                ///repeat for closer distances
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool UnlockTarget()
        {
            try
            {
                if (!RecursionCheck(nameof(UnlockTarget)))
                    return false;

                if (_directEntity != null && IsValid)
                {
                    if (IsTarget)
                    {
                        if (_directEntity.UnlockTarget())
                        {
                            Time.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                            ESCache.Instance.TargetingIDs.Remove(Id);
                            if (DirectEve.Interval(5000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Unlocked Target [" + TypeName + "]"));
                            if (Statistics.BountyValues.ContainsKey(Id))
                                try
                                {
                                    double bounty = _directEntity.GetBounty();
                                    if (bounty > 0)
                                    {
                                        Log.WriteLine("Removed bounty [" + bounty + "] for [" + Name + "] at [" + Math.Round(Distance / 1000, 0) + "k] Id [" + MaskedId + "] - unlocking target");
                                        Statistics.BountyValues.Remove(Id);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteLine("Exception [" + ex + "]");
                                }
                        }

                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public bool WarpTo(double range = 0, bool Force = false)
        {
            try
            {
                if (!RecursionCheck(nameof(WarpTo)))
                    return false;

                if (ESCache.Instance.InSpace)
                {
                    if (_directEntity != null && IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCacheCreated)
                        {
                            Log.WriteLine("The EntityCache instance that represents [" + _directEntity.Name + "][" +
                                          Math.Round(_directEntity.Distance / 1000, 0) +
                                          "k][" + MaskedId + "] was created [" + _thisEntityCacheCreated + "]");
                            ControllerManager.Instance.SetPause(true);
                        }

                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsWarpScramblingMe))
                            return false;

                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsStation || i.IsCitadel))
                        {
                            CheckForDamageAndSetNeedsRepair();

                            if (ESCache.Instance.Modules.Any(i => !i.IsOnline))
                            {
                                Util.PlayNoticeSound();
                                string msg = "We attempted to warp away from a station with an offline module: docking instead!";
                                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                                Log.WriteLine(msg);

                                //MissionSettings.OfflineModulesFound = true;
                                ESCache.Instance.EntitiesOnGrid.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsStation || i.IsCitadel).Dock();
                                return false;
                            }
                            else MissionSettings.OfflineModulesFound = false;

                            if (ESCache.Instance.Modules.Any(i => i.DamagePercent > 0))
                            {
                                Util.PlayNoticeSound();
                                string msg = "We attempted to warp away from a station with an damaged module: docking instead!";
                                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                                Log.WriteLine(msg);
                                //MissionSettings.DamagedModulesFound = true;
                                ESCache.Instance.EntitiesOnGrid.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsStation || i.IsCitadel).Dock();
                                return false;
                            }
                            //else MissionSettings.DamagedModulesFound = false;
                        }

                        if (Distance < (long)Distances.HalfOfALightYearInAu)
                        {
                            if (Distance > (int)Distances.WarptoDistance)
                            {
                                if (!SafeToInitiateMovementCommand(Force))
                                    return false;

                                if (!SafeToInitiateWarpJumpActivateCommand("WarpTo"))
                                    return false;

                                if (DebugConfig.DebugWarpCloakyTrick)
                                {
                                    if (!AlignTo())
                                    {
                                        if (DebugConfig.DebugTraveler) Log.WriteLine("WarpTo: if (!AlignTo())");
                                        return false;
                                    }

                                    if (Defense.EntityThatMayDecloakMe != null && Defense.EntityThatMayDecloakMe.Distance >= (int)Distances.SafeToCloakDistance && (int)Defense.EntityThatMayDecloakMe.Distance != 0)
                                    {
                                        if (ESCache.Instance.Modules.Any(i => i.GroupId == (int)Group.CloakingDevice))
                                        {
                                            //
                                            // MWD
                                            //
                                            const bool forceSpeedModToActivate = true;
                                            const bool deactivsteSpeedModIfActive = true;
                                            Defense.ActivateSpeedMod(forceSpeedModToActivate, deactivsteSpeedModIfActive);

                                            if (ActuallyWarpTo())
                                            {
                                                if (DebugConfig.DebugTraveler) Log.WriteLine("WarpTo: if (WarpTo())");
                                                return true;
                                            }

                                            if (DebugConfig.DebugTraveler) Log.WriteLine("WarpTo: WarpTo() returned false");
                                            return true;
                                        }

                                        if (DebugConfig.DebugTraveler) Log.WriteLine("WarpTo: we have no cloaking device: just warp");

                                        if (ActuallyWarpTo())
                                        {
                                            if (DebugConfig.DebugTraveler) Log.WriteLine("WarpTo: if (WarpTo()).");
                                            return true;
                                        }
                                    }

                                    if (DebugConfig.DebugTraveler) Log.WriteLine("WarpTo: we are too close to [" + Defense.EntityThatMayDecloakMe.Name + "][" + Math.Round(Defense.EntityThatMayDecloakMe.Distance, 0) + "m] to even try to cloak. just warp.");
                                }

                                if (ActuallyWarpTo())
                                {
                                    if (DebugConfig.DebugTraveler) Log.WriteLine("WarpTo: [" + Name + "] if (WarpTo()).");
                                    return true;
                                }

                                Log.WriteLine("WarpTo: [" + Name + "].WarpTo() returned false.");
                                return false;
                            }

                            Log.WriteLine("[" + Name + "] Distance [" + Math.Round(Distance / 1000, 0) +
                                          "k] is not greater then 150k away, AlignCloakMWDDeCloakWarp aborted!");
                            return false;
                        }

                        Log.WriteLine("[" + Name + "] Distance [" + Math.Round(Distance / 1000, 0) +
                                      "k] was greater than 5000AU away, we assume this an error!, AlignCloakMWDDeCloakWarp aborted!");
                        return false;
                    }

                    Log.WriteLine("[" + Name + "] DirecEntity is null or is not valid");
                    return false;
                }

                Log.WriteLine("We have not yet been in space at least 2 seconds, waiting");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        private string _whatisFollowingMe = null;

        public string WhatisFollowingMe
        {
            get
            {
                try
                {
                    if (_whatisFollowingMe == null)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                        {
                            foreach (var PCT in Combat.PotentialCombatTargets)
                            {
                                if (PCT.FollowId == Id)
                                {
                                    if (_whatisFollowingMe == null)
                                        _whatisFollowingMe = string.Empty;

                                    _whatisFollowingMe += "[" + PCT._directEntity.FollowEntityName + "]";
                                }
                            }

                            foreach (var DeviantAutomataSuppressor in ESCache.Instance.Entities.Where(i => i.IsAbyssalDeadspaceSmallDeviantAutomataSuppressor || i.IsAbyssalDeadspaceMediumDeviantAutomataSuppressor))
                            {
                                if (DeviantAutomataSuppressor.FollowId == Id)
                                {
                                    if (_whatisFollowingMe == null)
                                        _whatisFollowingMe = string.Empty;

                                    _whatisFollowingMe += "[" + DeviantAutomataSuppressor._directEntity.FollowEntityName + "]";
                                }
                            }
                        }

                        return _directEntity.FollowEntityName;
                    }

                    return _whatisFollowingMe ?? string.Empty;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        #endregion Methods
    }

    public static class CombatMath
    {
        /// <summary>
        /// Calculate the damage done by a missile
        /// https://wiki.eveuniversity.org/Missile_mechanics#Missile_damage_formula
        /// </summary>
        /// <param name="missileBaseDamage">The base damage of the missile, of a particular damage type</param>
        /// <param name="missileDamageReductionFactor">The Damage Reduction Factor of the missile</param>
        /// <param name="missileExplosionRadius">THe Explosion Radius of the missile</param>
        /// <param name="missileExplosionVelocity">The Explosion Velocity of the missile</param>
        /// <param name="targetSigRadius">The Signature Radius of the target</param>
        /// <param name="targetVelocity">The Velocity of the target</param>
        /// <returns>The final damage dealt by the missile impact</returns>
        public static double MissileDamageCalc(
            double missileBaseDamage,
            double missileDamageReductionFactor,
            double missileExplosionRadius,
            double missileExplosionVelocity,
            double targetSigRadius,
            double targetVelocity
        )
        {
            // To prevent doing damage greater than the missile's base damage, return the smallest out of:
            // 1 (full damage)
            // Sig Radius / Explosion Radius (signature radius is larger than the explosion radius)
            // The full calculation
            return missileBaseDamage * Math.Min(
                       1,
                       Math.Min(
                           targetSigRadius * missileExplosionRadius,
                           Math.Pow((targetSigRadius / missileExplosionRadius) * (missileExplosionVelocity / targetVelocity), missileDamageReductionFactor)
                       )
                   );
        }

        /// <summary>
        /// Calculate the range of a missile.
        /// https://wiki.eveuniversity.org/Eve_Math#Missiles
        /// Note that EVE servers operate on 1-second intervals. A missile with a flight time of 12.3 seconds has
        /// a 70% chance of flying for 12 seconds and a 30% probability of flying for 13 seconds.
        /// </summary>
        /// <param name="missileFlightTime"></param>
        /// <param name="missileVelocity"></param>
        /// <returns></returns>
        public static double MissileFlightTimeCalc(double missileFlightTime, double missileVelocity)
        {
            return missileFlightTime * missileVelocity;
        }

        /// <summary>
        /// <para>Calculate the chance for a turret to hit something</para>
        /// <para>https://wiki.eveuniversity.org/Turret_Damage#Tracking</para>
        /// </summary>
        /// <param name="targetAngularVelocity">The angular velocity of the target, relative to the shooter</param>
        /// <param name="targetSigRadius">The target's signature radius</param>
        /// <param name="targetDistance">The target's distance from teh shooter</param>
        /// <param name="turretTrackingSpeed">The turret's tracking speed</param>
        /// <param name="turretOptimalRange">The turret's optimal range</param>
        /// <param name="turretFalloff">The turret's falloff range</param>
        /// <returns>The chance to hit (e.g. 0.5 = 50%)</returns>
        public static double TurretChanceToHit(
            double targetAngularVelocity,
            double targetSigRadius,
            double targetDistance,
            double turretTrackingSpeed,
            double turretOptimalRange,
            double turretFalloff)
        {
            // How likely it is to hit the target with how fast the target is moving in relation to the ship doing the shooting
            double trackingTerm = 0.5 * ((targetAngularVelocity * 40000) / (turretTrackingSpeed * targetSigRadius));

            // How likely it is to hit the target at the distance it is
            // 100% within optimal range
            // about 50% at optimal + under half of falloff
            // about 6.5% @ optimal + over half of falloff
            // about 0.2% @ optimal + falloff
            double rangeTerm = 0.5 * ((Math.Max(0, targetDistance - turretOptimalRange)) / turretFalloff);

            return trackingTerm * rangeTerm;
        }

        /// <summary>
        /// Get a rough estimate of Effective Hit Points; the ship's hit points (shield, armor, or hull) with your resistance applied.
        /// </summary>
        /// <example>1000 shields, 50% EM res., 60% Therm res. 60% Exp res., 70% Kin res., 60% averaged shield resistance.
        /// EffectiveHitPointsSimple(1000, 0.5, 0.6, 0.6, 0.7) = 1500 EHP</example>
        /// <param name="baseHP">The total hit points of shields, armor, or hull</param>
        /// <param name="resistEM">The EM resistance modifier</param>
        /// <param name="resistExp">The Explosive resistance modifier</param>
        /// <param name="resistTherm">The Thermal resistance modifier</param>
        /// <param name="resistKin">The Kinetic resistance modifier</param>
        /// <returns></returns>
        public static double EffectiveHitPoints(
            double baseHP,
            double resistEM,
            double resistExp,
            double resistTherm,
            double resistKin
        )
        {
            double averageResistance = (resistEM + resistExp + resistTherm + resistKin) / 4; //Get your average resistance
            return baseHP * (1 + averageResistance);
        }

        /// <summary>
        /// Get an estimate of Effective Hit Points; the ship's hit points (shield, armor, or hull) with your resistance applied.
        /// </summary>
        /// <param name="baseHP">The total hit points of shields, armor, or hull</param>
        /// <param name="resistEM">The EM resistance modifier</param>
        /// <param name="resistExp">The Explosive resistance modifier</param>
        /// <param name="resistTherm">The Thermal resistance modifier</param>
        /// <param name="resistKin">The Kinetic resistance modifier</param>
        /// <param name="attackEM">The percentage of incoming EM damage</param>
        /// <param name="attackExp">The percentage of incoming Explosive damage</param>
        /// <param name="attackTherm">The percentage of incoming Thermal damage</param>
        /// <param name="attackKin">The percentage of incoming Kinetic damage</param>
        /// <returns></returns>
        public static double EffectiveHitPoints(
            double baseHP,
            double resistEM,
            double resistExp,
            double resistTherm,
            double resistKin,
            double attackEM,
            double attackExp,
            double attackTherm,
            double attackKin
        )
        {
            return baseHP *
                   (1 + resistEM - attackEM) *
                   (1 + resistExp - attackExp) *
                   (1 + resistTherm - attackTherm) *
                   (1 + resistKin - attackKin);
        }

        // https://forums.eveonline.com/t/targeting-time-locking-time-calculation/91133
        // =40000/(ScanRes*ASINH(SigRadius)^2)
        /// <summary>
        /// Calculate how long in seconds it will take for a ship to lock a target, in seconds.
        /// Due to how EVE's servers operate in 'ticks', time is rounded to the next second (e.g. 3.36s becomes 4s).
        /// </summary>
        /// <param name="shipScanResolution">The targetting resolution of the ship doing the targetting, in millimetres</param>
        /// <param name="targetSignatureRadius">The signature resolution of thie ship being targetted, in metres</param>
        /// <returns></returns>
        public static int LockTime(
            int shipScanResolution,
            int targetSignatureRadius
        )
        {
            // The C# Math library doesn't have a function for ArcSinH, so we'll calculate it ourselves
            // If you're doing this equation in Excel or anothe language which has asinh, just use that
            // https://www.codeproject.com/Articles/86805/An-introduction-to-numerical-programming-in-C
            // asinh(x) = log(x + sqrt(x2 + 1))
            double sigRadiusAsinh = Math.Log(targetSignatureRadius + Math.Sqrt(Math.Pow(targetSignatureRadius, 2) + 1));

            return (int)Math.Ceiling(40000 / (shipScanResolution * Math.Pow(sigRadiusAsinh, 2)));
        }

        #region EWarAndLogistics

        // Most of the EWar math comes from
        // https://wiki.eveuniversity.org/Electronic_warfare

        // Most of the logistics information comes from
        // https://wiki.eveuniversity.org/Guide_to_Logistics
        // https://forums-archive.eveonline.com/message/6306658/

        /// <summary>
        /// <para>Get the probability of an ECM jammer sucessfully jamming a target, based on the target's distance & sensor power vs the jammer's range and strength.</para>
        /// <para>When an ECM jammer cycles, it "rolls the die" against this probability of success.</para>
        /// </summary>
        /// <param name="jammerStrength">The ECM jammer's strength against the target's sensor type, after bonuses and range effects are applied.</param>
        /// <param name="jammerOptimalRange">The jammer's optimal range.</param>
        /// <param name="jammerAccuracyFalloff">The jammer's effective accuracy falloff distance.</param>
        /// <param name="targetSensorStrength">The target's sensor strength, after bonuses are applied.</param>
        /// <param name="targetDistance">The target's distance from the jamming ship.</param>
        /// <returns>The probabilty of the ECM jammer successfully jamming the target, as a number between 0 and 1.</returns>
        public static double EcmChanceToJam(
            double jammerStrength,
            double jammerOptimalRange,
            double jammerAccuracyFalloff,
            double targetSensorStrength,
            double targetDistance
        )
        {
            // Jammer strength vs the target's sensor strength
            double strengthTerm = jammerStrength / targetSensorStrength;

            //How strong or weak the jammer is because of range
            double rangeTerm = EWarAndLogisticsEffectiveness(jammerOptimalRange, jammerAccuracyFalloff, targetDistance);

            return 1 * strengthTerm * rangeTerm;
        }

        /// <summary>
        /// <para>Calculate the effectiveness modifier, depending on the module's range and the targets distance.</para>
        /// <para>This applies to electronic warfare modules (e.g. target painters, tracking disruptors, sensor dampeners) as well as logistics modules (e.g. remote repair, sensor booter).</para>
        /// <para>Note that it uses a similar calculation to turret range and accuracy, however angular velocity and signature radius has no effect.</para>
        /// <para>For ECM jmmers, use <seealso cref="EcmChanceToJam"./></para>
        /// </summary>
        /// <param name="moduleOptimalRange">The module's effective optimal range.</param>
        /// <param name="moduleAccuracyFalloff">The module's effective accuracy falloff distance.</param>
        /// <param name="targetDistance">The targets distance from the ship.</param>
        /// <returns>The effectiveness of the module, as a number between 0 and 1.</returns>
        public static double EWarAndLogisticsEffectiveness(
            double moduleOptimalRange,
            double moduleAccuracyFalloff,
            double targetDistance
        )
        {
            //How strong or weak the module's effect is because of range
            // 100% within optimal range
            // about 50% at optimal + under half of falloff
            // about 6.5% @ optimal + over half of falloff
            // about 0.2% @ optimal + falloff
            return 0.5 * ((Math.Max(0, targetDistance - moduleOptimalRange)) / moduleAccuracyFalloff);
        }

        #endregion
    }
}