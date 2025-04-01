extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectUIModule : DirectItem
    {
        #region Fields

        public bool _isDeactivating;
        private static Dictionary<long, DateTime> _lastModuleAction = new Dictionary<long, DateTime>();

        // CCP bug, sometimes repairing doesn't update the clients game state, but it does on the servers side.
        // So we keep track of the repair amounts until the module is in the state of "isBeingRepaired", then we clear that dict.
        // We return that the module has no module damage until isBeingRepaired == true or next session change.
        private static Dictionary<long, long> _repairAttempts = new Dictionary<long, long>();

        private bool _isActive;

        private bool
            _isReloadingAmmo; // this works because it's created every frame again, by setting is directly within the reloading routine other code can't reload twice per frame

        //private List<DirectItem> _matchingAmmo;
        private PyObject _pyModule;

        private long? _targetId = null;

        #endregion Fields

        #region Constructors

        internal DirectUIModule(DirectEve directEve, PyObject pyModule) : base(directEve)
        {
            _pyModule = pyModule;
        }

        #endregion Constructors

        #region Properties

        //todo:
        //attributes to look at
        //attributeDisallowInEmpireSpace
        //attributeDisallowInHighSec
        //attributeDisallowRepeatingActivation
        //attributeDisallowWhenInvulnerable
        //attributeDisallowCloaking
        //attributeDrawback
        //attributeEntityAttackRange
        //attributeEntityChaseMaxDistance
        //attributeEntityFactionLoss
        //attributeEntityMaxWanderRange
        //attributeFitsToShipType
        //attributeGateMaxJumpMass
        //attributeGateScrambleStatus
        //attributeHeatAbsorbtionRateHi = 1182
        //attributeHeatAbsorbtionRateLow = 1184
        //attributeHeatAbsorbtionRateMed = 1183
        //attributeHeatAttenuationHi = 1259
        //attributeHeatAttenuationLow = 1262
        //attributeHeatAttenuationMed = 1261
        //attributeHeatCapacityHi = 1178
        //attributeHeatCapacityLow = 1200
        //attributeHeatCapacityMed = 1199
        //attributeHeatDamage = 1211
        //attributeHeatDissipationRateHi = 1179
        //attributeHeatDissipationRateLow = 1198
        //a/ttributeHeatDissipationRateMed = 1196
        //attributeHeatGenerationMultiplier = 1224
        //attributeHeatAbsorbtionRateModifier = 1180
        //attributeHeatHi = 1175
        //attributeHeatLow = 1177
        //attributeHeatMed = 1176
        //attributeIsIncapacitated
        //attributeIsPointTargeted
        //attributeLootRespawnTime ???
        //attributeMass = 4
        //attributeMassAddition = 796
        //attributeMassBonusPercentage = 1131
        //attributeMaxAttackTargets
        //attributeMaxLockedTargets
        //attributeMaxTargetRange
        //attributeModuleReactivationDelay
        //attributeReloadTime
        //attributeTargetLockSilently
        //attributeWarpBubbleImmune
        //attributeWarpScrambleStatus
        //attributeCompressibleItemsTypeList
        //attributeActivationRequiresActiveIndustrialCore
        //attributeWormholeMassRegeneration = 1384
        //attributeWormholeMaxJumpMass = 1385
        //attributeWormholeMaxStableMass = 1383
        //attributeWormholeMaxStableTime = 1382
        //attributeWormholeTargetSystemClass = 1381
        //attributeWormholeTargetDistribution = 1457
        //attributeBehaviorMiningMaxRange
        //attributeBehaviorMiningAmount
        //attributeBehaviorMiningDuration
        //attributeBehaviorMiningDischarge
        //attributeCanActivateInGateCloak
        //attributeTierDifficulty
        //attributeHackable


        private const int ModuleReactivationDelay = 400;
        private double? _emMissileDamage;
        private double? _emMissileDps;
        private double? _emTurretDamage;
        private double? _emTurretDps;
        private double? _explosiveDamage;
        private double? _explosiveDps;
        private double? _explosiveMissileDamage;
        private double? _explosiveMissileDps;
        private double? _kineticMissileDamage;
        private double? _kineticMissileDps;
        private double? _kineticTurretDamage;
        private double? _kineticTurretDps;
        private AmmoType _chargeAmmoType;
        private DirectItem _missileDirectItem;
        private double? _missileDps;
        private double? _missileLaunchDuration;
        private double? _thermalMissileDamage;
        private double? _thermalMissileDps;
        private double? _thermalTurretDamage;
        private double? _thermalTurretDps;
        private double? _turretDps;

        private const int MODULE_REACTIVATION_DELAY = 400;

        public static List<AmmoType> _definedAmmoTypes = new List<AmmoType>();

        public static List<AmmoType> DefinedAmmoTypes
        {
            get
            {
                try
                {
                    if (_definedAmmoTypes.Count > 0)
                        return _definedAmmoTypes;

                    //
                    // Load AmmoTypes from MissionXml
                    //
                    if (MissionSettings.MissionXml != null && MissionSettings.MissionXml.Root != null)
                    {
                        XElement missionSpecificAmmoTypes = MissionSettings.MissionXml.Root.Element("ammoTypes");
                        if (missionSpecificAmmoTypes != null)
                        {
                            if (DebugConfig.DebugReloadorChangeAmmo) Log.WriteLine("Clearing DefinedAmmoTypes");
                            _definedAmmoTypes = new List<AmmoType>();
                            foreach (XElement individualAmmoType in missionSpecificAmmoTypes.Elements("ammoType"))
                            {
                                AmmoType individualAmmoTypeToAdd = new AmmoType(individualAmmoType);
                                if (DebugConfig.DebugReloadorChangeAmmo) Log.WriteLine("Adding MissionSpecific DefinedAmmoTypes [" + individualAmmoTypeToAdd.Description + "] TypeId [" + individualAmmoTypeToAdd.TypeId + "][" + individualAmmoTypeToAdd.DamageType + "] Range [" + individualAmmoTypeToAdd.Range + "] Quantity [" + individualAmmoTypeToAdd.Quantity + "]");
                                _definedAmmoTypes.Add(individualAmmoTypeToAdd);
                            }

                            if (_definedAmmoTypes.Select(a => a.DamageType).Distinct().Count() >= 4)
                            {
                                if (_definedAmmoTypes.All(a => a.DamageType != DamageType.EM)) Log.WriteLine("Missing EM damage type!");
                                if (_definedAmmoTypes.All(a => a.DamageType != DamageType.Thermal)) Log.WriteLine("Missing Thermal damage type!");
                                if (_definedAmmoTypes.All(a => a.DamageType != DamageType.Kinetic)) Log.WriteLine("Missing Kinetic damage type!");
                                if (_definedAmmoTypes.All(a => a.DamageType != DamageType.Explosive))  Log.WriteLine("Missing Explosive damage type!");

                                Log.WriteLine("You are required to specify all 4 damage types in your mission xml file if you include an ammotypes section!");
                            }

                            return _definedAmmoTypes;
                        }
                    }

                    //
                    // Load AmmoTypes from CharacterXml
                    //
                    if (Settings.CharacterSettingsXml != null)
                    {
                        _definedAmmoTypes = new List<AmmoType>();
                        XElement ammoTypes = Settings.CharacterSettingsXml.Element("ammoTypes") ?? Settings.CommonSettingsXml.Element("ammoTypes");
                        if (ammoTypes != null)
                        {
                            if (DebugConfig.DebugReloadorChangeAmmo) Log.WriteLine("Clearing DefinedAmmoTypes");
                            _definedAmmoTypes = new List<AmmoType>();
                            foreach (XElement individualAmmoType in ammoTypes.Elements("ammoType"))
                            {
                                AmmoType individualAmmoTypeToAdd = new AmmoType(individualAmmoType);
                                if (DebugConfig.DebugReloadorChangeAmmo) Log.WriteLine("Adding DefinedAmmoTypes [" + individualAmmoTypeToAdd.Description + "] TypeId [" + individualAmmoTypeToAdd.TypeId + "][" + individualAmmoTypeToAdd.DamageType + "] Range [" + individualAmmoTypeToAdd.Range + "] Quantity [" + individualAmmoTypeToAdd.Quantity + "]");
                                _definedAmmoTypes.Add(individualAmmoTypeToAdd);
                            }

                            if (_definedAmmoTypes.Select(a => a.DamageType).Distinct().Count() != 4)
                            {
                                if (_definedAmmoTypes.All(a => a.DamageType != DamageType.EM)) Log.WriteLine("Missing EM damage type!");
                                if (_definedAmmoTypes.All(a => a.DamageType != DamageType.Thermal)) Log.WriteLine("Missing Thermal damage type!");
                                if (_definedAmmoTypes.All(a => a.DamageType != DamageType.Kinetic)) Log.WriteLine("Missing Kinetic damage type!");
                                if (_definedAmmoTypes.All(a => a.DamageType != DamageType.Explosive)) Log.WriteLine("Missing Explosive damage type!");

                                Log.WriteLine("You are required to specify all 4 damage types!");
                            }

                            return _definedAmmoTypes;
                        }

                        return new List<AmmoType>();
                    }

                    return new List<AmmoType>();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Ammo Settings [" + exception + "]");
                    return _definedAmmoTypes;
                }
            }
        }

        public double? ArmorTransferRange => Attributes.TryGet<double>("maxRange");
        public bool AutoReload { get; internal set; }
        public bool AutoRepeat { get; internal set; }
        public bool Blinking { get; internal set; }
        public double? CapacitorNeed => Attributes.TryGet<double>("capacitorNeed");
        public DirectItem Charge { get; internal set; }

        public AmmoType ChargeAmmoType
        {
            get
            {
                if (!IsWeapon) return null;

                if (_chargeAmmoType == null)
                {
                    if (Charge != null)
                    {
                        foreach (AmmoType definedAmmoType in DefinedAmmoTypes)
                            if (definedAmmoType.TypeId == Charge.TypeId)
                            {
                                _chargeAmmoType = definedAmmoType;
                                return _chargeAmmoType;
                            }

                        return null;
                    }

                    return null;
                }

                return _chargeAmmoType;
            }
        }

        public int ChargeQty
        {
            get
            {
                if (Charge != null)
                {
                    //
                    // Laser crystals are not packaged and thus wont have a Quantity!
                    //
                    if (Charge.IsLaserAmmo)
                    {
                        return 1;
                    }

                    return Charge.Quantity;
                }
                else
                {
                    return 0;
                }
            }
        }

        public double ChargeRange
        {
            get
            {
                if (ChargeAmmoType != null)
                    return ChargeAmmoType.Range;

                return 0;
            }
        }

        public double HeatDamage { get; internal set; }

        public double? DamageMultiplier => Attributes.TryGet<double>("damageMultiplier");

        public double HeatDamagePercent
        {
            get
            {
                if (Hp != 0)
                {
                    return HeatDamage / Hp * 100;
                }

                return 0;
            }
        }

        private double? _shieldBonus;

        public double? ShieldBonus
        {
            get
            {
                if (!_shieldBonus.HasValue)
                    _shieldBonus = Attributes.TryGet<float>("shieldBonus");
                return _shieldBonus ?? 0;
            }
        }

        public bool DisableAutoReload => !IsInLimboState && !IsActive && AutoReload && SetAutoReload(false);
        public double? Duration => Attributes.TryGet<double>("duration");
        public bool EffectActivating => _pyModule.Attribute("effect_activating").ToBool();
        public int? EffectCategory => this?.DefEffect.Attribute("effectCategory").ToInt() ?? null;
        public int? EffectId => this?.DefEffect.Attribute("effectID").ToInt() ?? null;
        public string EffectName => this?.DefEffect.Attribute("effectName").ToUnicodeString() ?? null;

        public double? EmRawMissileDamage
        {
            get
            {
                if (_emMissileDamage == null)
                {
                    if (MissileDirectItem != null && MissileDirectItem.TypeId != 0)
                    {
                        _emMissileDamage = MissileDirectItem.Attributes.TryGet<double>("emDamage");
                        if (_emMissileDamage.HasValue)
                            return _emMissileDamage.Value;

                        return 0;
                    }

                    return 0;
                }

                return _kineticMissileDamage;
            }
        }

        public double? EmRawMissileDps
        {
            //
            // https://wiki.eveuniversity.org/Missile_mechanics
            //
            get
            {
                if (_emMissileDps == null)
                {
                    if (EmRawMissileDamage != null && MissileRateOfFire != null)
                    {
                        _emMissileDps = (double) EmRawMissileDamage / (double) MissileRateOfFire;
                        if (_emMissileDps != null && !double.IsNaN(_emMissileDps.Value) && !double.IsInfinity(_emMissileDps.Value))
                            return _emMissileDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _emMissileDps;
            }
        }

        public double? EmRawTurretDamage
        {
            get
            {
                if (_emTurretDamage == null)
                {
                    _emTurretDamage = (float)PyInvType.Attribute("emDamage");
                    if (_emTurretDamage.HasValue)
                        return _emTurretDamage.Value;

                    return 0;
                }

                return _emTurretDamage.Value;
            }
        }

        public double? EmRawTurretDps
        {
            get
            {
                if (_emTurretDps == null)
                {
                    if (EmRawTurretDamage != null && DamageMultiplier != null && RateOfFire != null)
                    {
                        _emTurretDps = (double) EmRawTurretDamage * (double) DamageMultiplier / (double) RateOfFire;
                        return _emTurretDps.Value;
                    }

                    return 0;
                }

                return _emTurretDps;
            }
        }

        public double? ExplosiveRawMissileDamage
        {
            get
            {
                if (_explosiveMissileDamage == null)
                {
                    if (MissileDirectItem != null && MissileDirectItem.TypeId != 0)
                    {
                        _explosiveMissileDamage = MissileDirectItem.Attributes.TryGet<double>("explosiveDamage");
                        if (_explosiveMissileDamage.HasValue)
                            return _explosiveMissileDamage.Value;

                        return 0;
                    }

                    return 0;
                }

                return _explosiveMissileDamage;
            }
        }

        public double? ExplosiveRawMissileDps
        {
            get
            {
                if (_explosiveMissileDps == null)
                {
                    if (ExplosiveRawMissileDamage != null && ExplosiveRawMissileDamage != 0 && MissileRateOfFire != null)
                    {
                        _explosiveMissileDps = (double) ExplosiveRawMissileDamage / (double) MissileRateOfFire;
                        if (_explosiveMissileDps != null && !double.IsNaN(_explosiveMissileDps.Value) && !double.IsInfinity(_explosiveMissileDps.Value))
                            return _explosiveMissileDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _explosiveMissileDps;
            }
        }

        public double? ExplosiveRawTurretDamage
        {
            get
            {
                if (_explosiveDamage == null)
                {
                    _explosiveDamage = Attributes.TryGet<double>("explosiveDamage");
                    if (_explosiveDamage.HasValue)
                        return _explosiveDamage.Value;

                    return 0;
                }

                return _explosiveDamage.Value;
            }
        }

        public double? ExplosiveRawTurretDps
        {
            get
            {
                if (_explosiveDps == null)
                {
                    if (ExplosiveRawTurretDamage != null && DamageMultiplier != null && RateOfFire != null)
                    {
                        _explosiveDps = (double) ExplosiveRawTurretDamage * (double) DamageMultiplier / (double) RateOfFire;
                        if (_explosiveDps != null && !double.IsNaN(_explosiveDps.Value) && !double.IsInfinity(_explosiveDps.Value))
                            return _explosiveDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _explosiveDps;
            }
        }

        public double? FallOff => Attributes.TryGet<double>("falloff");

        public double? FalloffEffectiveness => Attributes.TryGet<double>("falloffEffectiveness");
        public PyObject GetPyModule => _pyModule;

        public double Hp { get; internal set; }

        public bool IsActivatable => DefEffect != null && DefEffect.IsValid;
        public bool IsPassiveModule
        {
            get
            {
                //
                // https://everef.net/categories/7
                //
                if (GroupId == (int)Group.DamageControl)
                    return true;

                if (GroupId == (int)Group.CapacitorBattery)
                    return true;

                if (GroupId == (int)Group.ArmorPlate)
                    return true;

                if (GroupId == (int)Group.CapRecharger)
                    return true;

                if (GroupId == (int)Group.CapacitorFluxCoil)
                    return true;

                if (GroupId == (int)Group.CPUEnhancer)
                    return true;

                if (GroupId == (int)Group.DroneControlRange)
                    return true;

                if (GroupId == (int)Group.DroneDamageAmplifier)
                    return true;

                if (GroupId == (int)Group.Gyrostabilizer)
                    return true;

                if (GroupId == (int)Group.HeatSink)
                    return true;

                if (GroupId == (int)Group.MagneticFieldStabilizer)
                    return true;

                if (GroupId == (int)Group.VortronProjectorUpgrade)
                    return true;

                if (GroupId == (int)Group.InertialStabilizer)
                    return true;

                if (GroupId == (int)Group.Nanofiber)
                    return true;

                if (GroupId == (int)Group.PowerDiagnosticUnit)
                    return true;

                if (GroupId == (int)Group.ReactorControlUnit)
                    return true;

                if (GroupId == (int)Group.Shieldrecharger)
                    return true;

                if (GroupId == (int)Group.ShieldBoostAmplifier)
                    return true;

                if (GroupId == (int)Group.CapacitorInjector)
                    return true;

                //
                // TODO: wont this return true for modules that currently cant be activated but ARE usually activatable? As in modules that are in a limbo state?
                //
                if (!IsActivatable)
                    return true;

                return false;
            }
        }
        public bool IsActive => IsActivatable && (DefEffect.Attribute("isActive").ToBool() || _isActive);
        public bool IsBeingRepaired { get; internal set; }
        private bool? _isMaster;
        public bool IsMaster => _isMaster ??= _pyModule.Attribute("isMaster").ToBool();

        public bool IsMasterOrIsNotGrouped
        {
            get
            {
                if (IsMaster) return true;
                if (ESCache.Instance.DirectEve.Modules.Where(x => x.IsWeapon).All(i => !i.IsMaster))
                    return true;

                //Assume that if we have any weapons stacked that they are ALL stacked by type ID (you cant stack diff weapon types in the same stack!)
                if (ESCache.Instance.DirectEve.Modules.Where(x => x.IsWeapon).Any(i => i.IsMaster && i.TypeId != TypeId))
                    return true;

                return false;
            }
        }

        public int SlaveCount => _pyModule.Attribute("slaves").ToList().Count;
        public bool IsDeactivating => (IsActivatable && DefEffect.Attribute("isDeactivating").ToBool()) || _isDeactivating;
        public bool? IsEffectOffensive => this?.DefEffect.Attribute("isOffensive").ToBool() ?? null;
        public bool IsEnergyWeapon => GroupId == (int)Group.EnergyWeapon;

        public bool IsEwarModule => GroupId == (int)Group.WarpDisruptor
                                    || GroupId == (int)Group.StasisWeb
                                    || GroupId == (int)Group.TargetPainter
                                    || GroupId == (int)Group.TrackingDisruptor
                                    || GroupId == (int)Group.SensorDampener
                                    || GroupId == (int)Group.Ecm
                                    || GroupId == (int)Group.Neutralizer;



        public bool IsInLimboState
        {
            get
            {
                if (IsPassiveModule)
                {
                    if (!IsOnline
                        || IsBeingRepaired
                        || !DirectEve.Session.IsInSpace
                        || DirectEve.Session.IsInDockableLocation
                        || ReactivationDelay > 0)
                    {
                        return true;
                    }

                    return false;
                }

                if (!IsActivatable
                    || !IsOnline
                    || IsDeactivating
                    || IsReloadingAmmo
                    || IsBeingRepaired
                    || !DirectEve.Session.IsInSpace
                    || DirectEve.Session.IsInDockableLocation
                    || EffectActivating
                    || DirectEve.IsEffectActivating(this)
                    || ReactivationDelay > 0
                    || Time.Instance.LastDockAction.AddSeconds(5) > DateTime.UtcNow
                    || Time.Instance.LastJumpAction.AddSeconds(5) > DateTime.UtcNow)
                {
                    return true;
                }

                return false;
            }
        }

// use for time critical module operations
        public bool IsLimboStateWithoutEffectActivating => !IsActivatable
                                      || !IsOnline
                                      || IsDeactivating
                                      || IsReloadingAmmo
                                      || IsBeingRepaired
                                      || !DirectEve.Session.IsInSpace
                                      || DirectEve.Session.IsInDockableLocation
                                      || EffectActivating
                                      || ReactivationDelay > 0;

        public bool IsInLimboStatePassiveModules => !IsOnline
                                 || IsBeingRepaired
                                 || !DirectEve.Session.IsInSpace
                                 || DirectEve.Session.IsInDockableLocation
                                 || ReactivationDelay > 0;

        public bool IsAfterburner
        {
            get
            {
                if (GroupId == (int)Group.Afterburner)
                {
                    if (TypeName.Contains("1"))
                        return true;

                    if (TypeName.Contains("10"))
                        return true;

                    if (TypeName.Contains("100"))
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsMicroWarpDrive
        {
            get
            {
                if (GroupId == (int)Group.Afterburner)
                {
                    if (TypeName.Contains("5"))
                        return true;

                    if (TypeName.Contains("50"))
                        return true;

                    if (TypeName.Contains("500"))
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsMissileLauncher => GroupId == (int)Group.AssaultMissileLaunchers
                                         || GroupId == (int)Group.CitadelCruiseLaunchers
                                         || GroupId == (int)Group.CitadelTorpLaunchers
                                         || GroupId == (int)Group.CruiseMissileLaunchers
                                         || GroupId == (int)Group.DefenderMissileLaunchers
                                         || GroupId == (int)Group.HeavyAssaultMissileLaunchers
                                         || GroupId == (int)Group.HeavyMissileLaunchers
                                         || GroupId == (int)Group.LightMissileLaunchers
                                         || GroupId == (int)Group.RapidHeavyMissileLaunchers
                                         || GroupId == (int)Group.RapidLightMissileLaunchers
                                         || GroupId == (int)Group.RocketLaunchers
                                         || GroupId == (int)Group.StandardMissileLaunchers
                                         || GroupId == (int)Group.TorpedoLaunchers
                                         || GroupId == (int)Group.VortonProjector; //not a missile launcher, but range and such works the same?!

        public bool IsOnline { get; internal set; }
        public bool IsOverloaded { get; internal set; }
        public bool IsPendingOverloading { get; internal set; }
        public bool IsPendingStopOverloading { get; internal set; }
        public bool IsOverloadLimboState => IsPendingOverloading || IsPendingStopOverloading;
        public bool IsReloadingAmmo => _pyModule.IsValid && _pyModule.Attribute("reloadingAmmo").ToBool() || _isReloadingAmmo;

        public bool IsTurret => GroupId == (int)Group.EnergyWeapon
                                || GroupId == (int)Group.ProjectileWeapon
                                || GroupId == (int)Group.PrecursorWeapon
                                || GroupId == (int)Group.HybridWeapon
                                || IsCivilianWeapon;

        public bool IsVortonProjector => GroupId == (int)Group.VortonProjector;

        public bool IsCivilianWeapon
        {
            get
            {
                if (TypeId == (int)TypeID.CivilianGatlingAutocannon)
                    return true;

                if (TypeId == (int)TypeID.CivilianGatlingPulseLaser)
                    return true;

                if (TypeId == (int)TypeID.CivilianGatlingRailgun)
                    return true;

                if (TypeId == (int)TypeID.CivilianLightElectronBlaster)
                    return true;

                return false;
            }
        }

        //
        // Smartbombs arent considered weapons? they should probably stay a special case...
        //
        public bool IsWeapon => IsMissileLauncher || IsTurret || IsVortonProjector;

        public double? KineticRawMissileDamage
        {
            get
            {
                if (_kineticMissileDamage == null)
                {
                    if (MissileDirectItem != null && MissileDirectItem.TypeId != 0)
                    {
                        _kineticMissileDamage = MissileDirectItem.Attributes.TryGet<double>("kineticDamage");
                        if (_kineticMissileDamage.HasValue)
                            return _kineticMissileDamage.Value;

                        return 0;
                    }

                    return 0;
                }

                return _kineticMissileDamage;
            }
        }

        public double? KineticRawMissileDps
        {
            get
            {
                if (_kineticMissileDps == null)
                {
                    if (KineticRawMissileDamage != null && KineticRawMissileDamage != 0 && MissileRateOfFire != null)
                    {
                        _kineticMissileDps = (double) KineticRawMissileDamage / (double) MissileRateOfFire;
                        if (_kineticMissileDps != null && !double.IsNaN(_kineticMissileDps.Value) && !double.IsInfinity(_kineticMissileDps.Value))
                            return _kineticMissileDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _kineticMissileDps;
            }
        }

        public double? KineticRawTurretDamage
        {
            get
            {
                if (_kineticTurretDamage == null)
                {
                    _kineticTurretDamage = (float)PyInvType.Attribute("kineticDamage");
                    if (_kineticTurretDamage.HasValue)
                        return _kineticTurretDamage.Value;

                    return 0;
                }

                return _kineticTurretDamage.Value;
            }
        }

        public double? KineticRawTurretDps
        {
            get
            {
                if (_kineticTurretDps == null)
                {
                    if (KineticRawTurretDamage != null && DamageMultiplier != null && RateOfFire != null)
                    {
                        _kineticTurretDps = (double) KineticRawTurretDamage * (double) DamageMultiplier / (double) RateOfFire;
                        if (_kineticTurretDps != null && !double.IsNaN(_kineticTurretDps.Value) && !double.IsInfinity(_kineticTurretDps.Value))
                            return _kineticTurretDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _kineticTurretDps;
            }
        }

        public int MaxCharges
        {
            get
            {
                if (GroupId == (int)Group.NOS)
                    return 0;

                if (GroupId == (int)Group.Neutralizer)
                    return 0;

                if (GroupId == (int)Group.TractorBeam)
                    return 0;

                if (GroupId == (int)Group.Salvager)
                    return 0;

                //if (TypeName.Contains("Quad 3500mm"))
                //    return 40;

                if (TypeName.Contains("Ultratidal Entropic Disintegrator"))
                    return 500;

                if (TypeName.Contains("Quad 800mm"))
                    return 80;

                if (TypeName.Contains("2500mm Repeating Cannon"))
                    return 80;

                if (TypeName.Contains("Polarized 800mm Repeating Cannon"))
                    return 360;

                if (TypeName.Contains("Polarized 200mm AutoCannon"))
                    return 360;

                if (TypeName.Contains("800mm Repeating Cannon") && !TypeName.Contains("Quad"))
                    return 120;

                if (Capacity == 0)
                {
                    Log.WriteLine("TypeName [" + TypeName + "] if (Capacity == 0) MaxCharges = 0");
                    return 0;
                }

                var chargeTouse = Charge ?? Combat.UsableAmmoInCargo.FirstOrDefault();

                if (chargeTouse != null && chargeTouse.Volume > 0)
                    return Convert.ToInt32(Capacity / chargeTouse.Volume);

                /*if (MatchingAmmo.Count > 0)
                    return (int) (Capacity/MatchingAmmo[0].Volume);*/

                Log.WriteLine("TypeName [" + TypeName + "] MaxCharges = 0!.!");
                return 0;
            }
        }

        public DirectItem MissileDirectItem
        {
            get
            {
                if (_missileDirectItem != null)
                {
                    if (Charge != null)
                    {
                        if (Charge.IsMissile)
                        {
                            _missileDirectItem = Charge;
                            if (_missileDirectItem.TypeId != 0)
                                return _missileDirectItem;

                            return null;
                        }

                        return null;
                    }

                    return null;
                }

                return _missileDirectItem;
            }
        }

        public double? MissileLaunchDuration
        {
            get
            {
                if (_missileLaunchDuration == null)
                    _missileLaunchDuration = (float)PyInvType.Attribute("missileLaunchDuration");

                return _missileLaunchDuration.Value;
            }
        }

        //missileLaunchDuration
        public double? MissileRateOfFire => MissileLaunchDuration;

        public new double? OptimalRange => Attributes.TryGet<double>("maxRange");

        public double? PowerTransferRange => Attributes.TryGet<double>("powerTransferRange");

        public bool RampActive => GetPyModule.Attribute("ramp_active").ToBool();

        new public double? RateOfFire => Speed; //Should this be renamed to indicate it is with skills? the raw attribute without skills is named the same: is that logical?

        public double RawMissileDps
        {
            get
            {
                if (_missileDps == null)
                {
                    _missileDps = EmRawMissileDps ?? 0 + ExplosiveRawMissileDps ?? 0 + KineticRawMissileDps ?? 0 + ThermalRawMissileDps ?? 0;
                    if (!double.IsNaN(_missileDps.Value) && !double.IsInfinity(_missileDps.Value))
                        return _missileDps.Value;

                    return 0;
                }

                return (double) _missileDps;
            }
        }

        public double RawTurretDps
        {
            get
            {
                if (_turretDps == null)
                {
                    _turretDps = EmRawTurretDps ?? 0 + ExplosiveRawTurretDps ?? 0 + KineticRawTurretDps ?? 0 + ThermalRawTurretDps ?? 0;
                    if (!double.IsNaN(_turretDps.Value) && !double.IsInfinity(_turretDps.Value))
                        return _turretDps.Value;

                    return 0;
                }

                return (double) _turretDps;
            }
        }

        public double? ShieldTransferRange => Attributes.TryGet<double>("shieldTransferRange");

        public double? Speed => Attributes.TryGet<double>("speed");

        // target id is either 0 (activatable with target possible) or -1 (activateable without a target possible) or targetid
        public long? TargetId => _targetId ?? (_targetId = DefEffect.Attribute("targetID").ToLong());

        public double? ThermalRawMissileDamage
        {
            get
            {
                if (_thermalMissileDamage == null)
                {
                    if (MissileDirectItem != null && MissileDirectItem.TypeId != 0)
                    {
                        _thermalMissileDamage = MissileDirectItem.Attributes.TryGet<double>("thermalDamage");
                        if (_thermalMissileDamage.HasValue)
                            return _thermalMissileDamage.Value;

                        return 0;
                    }

                    return 0;
                }

                return _thermalMissileDamage;
            }
        }

        public double? ThermalRawMissileDps
        {
            get
            {
                if (_thermalMissileDps == null)
                {
                    if (ThermalRawMissileDamage != null && ThermalRawMissileDamage != 0 && MissileRateOfFire != null)
                    {
                        _thermalMissileDps = (double) ThermalRawMissileDamage / (double) MissileRateOfFire;
                        if (_thermalMissileDps != null && !double.IsNaN(_thermalMissileDps.Value) && !double.IsInfinity(_thermalMissileDps.Value))
                            return _thermalMissileDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _thermalMissileDps;
            }
        }

        public double? ThermalRawTurretDamage
        {
            get
            {
                if (_thermalTurretDamage == null)
                {
                    _thermalTurretDamage = (float)PyInvType.Attribute("thermalDamage");
                    if (_thermalTurretDamage.HasValue)
                        return _thermalTurretDamage.Value;

                    return 0;
                }

                return _thermalTurretDamage.Value;
            }
        }

        public double? ThermalRawTurretDps
        {
            get
            {
                if (_thermalTurretDps == null)
                {
                    if (ThermalRawTurretDamage != null && DamageMultiplier != null && RateOfFire != null)
                    {
                        _thermalTurretDps = (double) ThermalRawTurretDamage * (double) DamageMultiplier / (double) RateOfFire;
                        if (_thermalTurretDps != null && !double.IsNaN(_thermalTurretDps.Value) && !double.IsInfinity(_thermalTurretDps.Value))
                            return _thermalTurretDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _thermalTurretDps;
            }
        }

        public double? TrackingSpeed => Attributes.TryGet<double>("trackingSpeed");

        public static void OnSessionChange()
        {
            _repairAttempts = new Dictionary<long, long>();
        }

        //public double? OptimalRange => Attributes.TryGet<double>("maxRange");
        //public bool RampActive => _pyModule.Attribute("ramp_active").ToBool();

        // always use the targetId we have set while activate, else the target id of a previous target would've been used
        //public long? TargetId => _targetId.HasValue ? _targetId : (_targetId = DefEffect.Attribute("targetID").ToLong());

        private PyObject DefEffect { get; set; }

        public int OverloadState { get; set; }

        #endregion Properties

        /*public List<DirectItem> MatchingAmmo
        {
            get
            {
                if (_matchingAmmo == null)
                {
                    _matchingAmmo = new List<DirectItem>();

                    var pyCharges = _pyModule.Call("GetMatchingAmmo", TypeId).ToList();
                    foreach (var pyCharge in pyCharges)
                    {
                        var charge = new DirectItem(DirectEve);
                        charge.PyItem = pyCharge;
                        _matchingAmmo.Add(charge);
                    }
                }

                return _matchingAmmo;
            }
        }*/

        #region Methods

        public bool IsShieldRepairModule
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.ShieldBoosters;
                    result |= GroupId == (int)Group.AncillaryShieldBooster;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsShieldTankModule
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.ShieldHardeners;
                    result |= GroupId == (int)Group.ShieldExtender;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsArmorRepairModule
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.ArmorRepairer;
                    result |= GroupId == (int)Group.AncillaryArmorBooster;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsArmorTankModule
        {
            get
            {
                try
                {
                    if (IsArmorRepairModule) return true;

                    bool result = false;
                    result |= GroupId == (int)Group.ArmorHardeners;
                    result |= GroupId == (int)Group.ArmorPlate;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsGasCloudHarvester
        {
            get
            {
                if (GroupId == (int)Group.GasCloudHarvester)
                    return true;

                return false;
            }
        }

        public bool IsMiningMercoxitMiningLaser
        {
            get
            {
                if (TypeId == (int)TypeID.OREDeepCoreMiningLaser)
                    return true;

                return false;
            }
        }

        public bool IsMiningLaser
        {
            get
            {
                if (GroupId == (int)Group.MiningLaser)
                    return true;

                if (GroupId == (int)Group.StripMiners)
                    return true;

                return false;
            }
        }

        public bool IsRemoteArmorRepairModule
        {
            get
            {
                try
                {
                    if (GroupId == (int)Group.RemoteArmorRepairer)
                        return true;

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsRemoteShieldRepairModule
        {
            get
            {
                try
                {
                    if (GroupId == (int)Group.RemoteShieldRepairer)
                        return true;

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsRemoteHullRepairModule
        {
            get
            {
                try
                {
                    if (GroupId == (int)Group.RemoteHullRepairer)
                        return true;

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsRemoteEnergyTransferModule
        {
            get
            {
                try
                {
                    if (GroupId == (int)Group.RemoteEnergyTransfer)
                        return true;

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        //public HashSet<int> ChargeCompatibleGroups
        //{
        //    get
        //    {
        //        if (_chargeCompatibleGroups != null)
        //            return _chargeCompatibleGroups;

        //        _chargeCompatibleGroups = new HashSet<int>();

        //        PyObject pyObj = PySharp.Import("__builtin__").Attribute("cfg").Attribute("__chargecompatiblegroups__");
        //        if (pyObj.IsValid)
        //        {
        //            int size = pyObj.Size();
        //            for (int i = 0; i < size; i++)
        //                _chargeCompatibleGroups.Add(pyObj.Item(i).ToInt());
        //        }
        //        return _chargeCompatibleGroups;
        //    }
        //}

        public double ReactivationDelay
        {
            get
            {
                var dictEntry = _pyModule.Attribute("stateManager").Attribute("lastStopTimesByItemID").DictionaryItem(this.ItemId);
                if (dictEntry.IsValid)
                {
                    var delayedUntil = dictEntry.GetItemAt(0).ToDateTime().AddMilliseconds(dictEntry.GetItemAt(1).ToFloat());
                    if (delayedUntil > DateTime.UtcNow)
                    {
                        return (delayedUntil - DateTime.UtcNow).TotalMilliseconds;
                    }
                }
                return 0d;
            }
        }

        public bool IsCovertOpsCloak
        {
            get
            {
                if (TypeId == (int)TypeID.CovertOpsCloakingDevice)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsCloak
        {
            get
            {
                if (GroupId == (int)Group.CloakingDevice)
                {
                    return true;
                }

                return false;
            }
        }

        public bool ActivateCovertOpsCloak
        {
            get
            {
                if (ESCache.Instance.InStation || !ESCache.Instance.InSpace)
                    return false;

                if (!IsCloak || !IsCovertOpsCloak)
                    return true; //if we dont even have a cloak, just return true and move on...

                if (ESCache.Instance.Stargates.Any(i => (double)Distances.SafeToCloakDistance > i.Distance))
                    return true; //if we are too close to something to cloak, just return true and move on!

                if (IsActive)
                {
                    Traveler.BoolRunEveryFrame = false;
                    if (DebugConfig.DebugWarpCloakyTrick) Log.WriteLine("ActivateCovertOpsCloak: IsActive");
                    return true;
                }

                if (IsInLimboState)
                {
                    Log.WriteLine("ActivateCovertOpsCloak: IsInLimboState");
                    Traveler.BoolRunEveryFrame = true;
                    if (ESCache.Instance.DirectEve.Me.IsInvuln)
                    {
                        Log.WriteLine("ActivateCovertOpsCloak IsInvuln: wait");
                        return false;
                    }

                    if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive)
                    {
                        Log.WriteLine("ActivateCovertOpsCloak: IsJumpCloakActive: wait");
                        return false;
                    }

                    return false;
                }

                if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive || ESCache.Instance.DirectEve.Me.IsInvuln)
                {
                    Log.WriteLine("IsJumpCloakActive [" + ESCache.Instance.DirectEve.Me.IsJumpCloakActive + "] IsInvuln [" + ESCache.Instance.DirectEve.Me.IsInvuln + "]");
                    Traveler.BoolRunEveryFrame = true;
                    if (ESCache.Instance.DirectEve.Session.SolarSystem != null &&
                        (ESCache.Instance.DirectEve.Session.SolarSystem.IsWormholeSystem ||
                         ESCache.Instance.DirectEve.Session.SolarSystem.IsZeroZeroSpace ||
                         ESCache.Instance.DirectEve.Session.SolarSystem.IsLowSecuritySpace)
                        )
                    {
                        if (ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && !e.IsHauler && !e.IsPod && !e.IsShuttle && e.Distance < 1400000))
                        {
                            if (ReactivationDelay > 0)
                            {
                                Log.WriteLine("ActivateCovertOpsCloak: if (ReactivationDelay > 0)");
                                return false;
                            }
                        }
                    }
                    else
                    {
                        if (ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && e.PlayerInAShipThatCanGank && e.Distance < 1400000))
                        {
                            if (ReactivationDelay > 0)
                            {
                                Log.WriteLine("ActivateCovertOpsCloak: if (ReactivationDelay > 0)");
                                return false;
                            }
                        }
                    }

                    if (DebugConfig.DebugDefense) Log.WriteLine("if (ESCache.Instance.DirectEve.Me.IsJumpActivationTimerActive) - move to break invul!");
                    //break invul here?
                    Traveler.BoolRunEveryFrame = true;
                    ESCache.Instance.ActiveShip.MoveToRandomDirection();
                    return false;
                }

                if (Combat.PotentialCombatTargets.Any(i => i.IsTargetedBy))
                {
                    Log.WriteLine("ActivateCovertOpsCloak: We cant cloak because we are targeted!");
                    return true; //if we cant cloak, return true so we move on
                }

                if (ESCache.Instance.ActiveShip.Entity == null)
                {
                    Log.WriteLine("ActivateCovertOpsCloak: ESCache.Instance.ActiveShip.Entity == null");
                    return false;
                }

                if (ESCache.Instance.EntitiesNotSelf.Any(e => e.Distance <= (int)Distances.SafeToCloakDistance))
                {
                    EntityCache ent = ESCache.Instance.EntitiesNotSelf.Find(e => e.Distance <= (int)Distances.SafeToCloakDistance);
                    if (ent != null && ent.IsValid)
                    {
                        Log.WriteLine("ActivateCovertOpsCloak: Cannot activate cloak: [" + ent.TypeName + "] at [" + Math.Round(ent.Distance, 0) + "m] within [" + Distances.SafeToCloakDistance + "]m");
                        return true;
                    }
                }

                if (Click())
                {
                    Traveler.BoolRunEveryFrame = false;
                    Log.WriteLine("ActivateCovertOpsCloak: Click()");
                    return true;
                }

                return false;
            }
        }

        public bool Activate(long targetId)
        {
            try
            {
                if (!DirectEve.Session.IsInSpace)
                    return false;

                //if (!DirectEve.Interval(500, 1000))
                //    return false;

                if (IsActive)
                    return false;

                if (IsInLimboState)
                    return false;

                //if (DisableAutoReload)
                //    return false;

                if (this.ChargeQty > this.MaxCharges)
                {
                    DirectEve.Log("this.ChargeQty [" + ChargeQty + "] > this.MaxCharges [" + MaxCharges + "]");
                    return false;
                }

                DirectEve.EntitiesById.TryGetValue(targetId, out var ent);
                if (ent != null)
                {
                    if (!ent.IsValid)
                        return false;

                    if (!DirectEve.IsTargetStillValid(targetId))
                        return false;

                    if (DirectEve.IsTargetBeingRemoved(targetId))
                        return false;

                    if (ent.IsEwarImmune && IsEwarModule)
                        return false;

                    if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(Activate)))
                        return false;

                    if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
                        return false;

                    _isActive = true;
                    _lastModuleAction[ItemId] = DateTime.UtcNow;
                    _targetId = targetId;
                    return DirectEve.ThreadedCall(_pyModule.Attribute("ActivateEffect"), _pyModule.Attribute("def_effect"), targetId);
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DirectEve.Activate Exception [" + ex + "]");
                return false;
            }
        }

        /// <summary>
        ///     Cancels the repairing of DirectUIModule in space
        /// </summary>
        /// <returns></returns>
        public bool CancelRepair()
        {
            if (!DirectEve.Interval(1500, 2200, ItemId.ToString()))
                return false;

            _lastModuleAction[ItemId] = DateTime.UtcNow;
            return DirectEve.ThreadedCall(_pyModule.Attribute("CancelRepair"));
        }

        public bool ChangeAmmo(DirectItem charge)
        {
            if (!DirectEve.Session.IsInSpace)
                return false;

            if (IsCivilianWeapon)
                return true;

            if (!CanBeReloaded)
                return true;

            if (IsMaster && charge.Stacksize < SlaveCount)
            {
                if (DirectEve.Interval(15000, 15000, ItemId.ToString()))
                {
                    DirectEve.Log($"Stacksize [{charge.Stacksize}] of charge is too small for the amount of grouped weapons. SlaveCount [{SlaveCount}].");
                }

                return false;
            }

            if (charge.ItemId <= 0)
                return false;

            if (IsInLimboState)
                return false;

            if (charge.TypeId <= 0)
                return false;

            if (charge.Stacksize <= 0)
                return false;

            if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(ChangeAmmo)))
                return false;

            if (!charge.PyItem.IsValid)
            {
                DirectEve.Log("Charge.pyItem is not valid!");
                return false;
            }

            if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
                return false;

            _lastModuleAction[ItemId] = DateTime.UtcNow;

            var reloadInfo = _pyModule.Call("GetChargeReloadInfo");

            if (!reloadInfo.IsValid)
            {
                DirectEve.Log("GetChargeReloadInfo is not valid! Error.");
                return false;
            }

            var reloadInfoList = reloadInfo.ToList();

            if (!reloadInfoList.Any())
            {
                DirectEve.Log("ReloadInfoList is empty! Error.");
                return false;
            }

            if (charge.TypeId == (int) reloadInfoList[0])
            {
                return ReloadAmmo(charge, true);
            }
            else
            {

                //self.icon.LoadIconByTypeID(charge.typeID)
                //self.charge = charge
                //self.stateManager.ChangeAmmoTypeForModule(self.moduleinfo.itemID, charge.typeID)
                //self.id = charge.itemID

                _pyModule["icon"].Call("LoadIconByTypeID", charge.TypeId);
                _pyModule.SetAttribute("charge", charge.PyItem);
                _pyModule.Attribute("stateManager").Call("ChangeAmmoTypeForModule", ItemId, charge.TypeId);
                _pyModule.SetAttribute("id", charge.ItemId);
                _pyModule.Call("UpdateChargeQuantity", charge.PyItem);
                //var setCharge = _pyModule.Attribute("SetCharge");
                //if (!setCharge.IsValid)
                //{
                //    DirectEve.Log($"SetCharge is not valid!");
                //    return false;
                //}

                //_pyModule.Call("SetCharge", charge.PyItem);
                //DirectEve.Log("Calling SetCharge()");

                return ReloadAmmo(charge, true);
            }
        }

        public bool Click(int moduleReactivationDelay = MODULE_REACTIVATION_DELAY, bool ignoreLimbo = false)
        {
            //if (!DirectEve.Interval(100, 200))
            //    return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (IsInLimboState && !ignoreLimbo)
            {
                //DirectEve.Log("IsInLimbo");
                return false;
            }

            // allow only one action per frame
            if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(Click)))
            {
                //DirectEve.Log("FrameHasNotChanged");
                return false;
            }

            // module interval
            if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
            {
                //DirectEve.Log("Mod reactivation");
                return false;
            }

            // prevent any action if the target is in the death blink animation
            if (TargetId.HasValue && DirectEve.IsTargetBeingRemoved(TargetId.Value))
                    return false;

            // temp fix, deactivating modules after being jammed causes exceptions
            if (IsActive && (IsEffectOffensive.HasValue && IsEffectOffensive.Value || EffectCategory.HasValue && EffectCategory.Value == 2))
            {
                if (Math.Min(DirectEve.Me.MaxLockedTargets, DirectEve.ActiveShip.MaxLockedTargetsWithShipAndSkills) == 0)
                {
                    DirectEve.Log($"Blocked module [{this.TypeId}] deactivation, we are jammed.");
                    return false;
                }
            }

            // don't allow prop mods with bastion
            if (DirectEve.ActiveShip.IsImmobile && (int)Group.Afterburner == this.GroupId && !this.IsActive)
            {
                return false;
            }

            // set is deactivating instantly for this frame
            if (IsActive)
                _isDeactivating = true;
            else
                _isActive = true;

            if (this.GroupId == 330)
            {
                    if (ESCache.Instance.InStation || !ESCache.Instance.InSpace)
                    {
                        return false;
                    }

                    if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive)
                    {
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.Entity == null)
                    {
                        return false;
                    }

                    if (ESCache.Instance.EntitiesNotSelf.Any(e => e.GroupId != 227 && e.Distance <= (int)Distances.SafeToCloakDistance)) // 227 = Inventory Groups.Celestial.Cloud
                    {
                        var ent = ESCache.Instance.EntitiesNotSelf.FirstOrDefault(e => e.Distance <= (int)Distances.SafeToCloakDistance);
                        if (ent != null && ent.IsValid)
                        {
                            DirectEve.Log($"Can't activate cloak because there is another entity within [{(int)Distances.SafeToCloakDistance}]m. Entity {ent.TypeName}");
                            return false;
                        }
                    }

                    // cloak avoid storm 56050 56049
                    if (DirectEve.Entities.Any(e => e.TypeId == 56050 || e.TypeId == 56049))
                    {
                        DirectEve.IntervalLog(10000, message: "Can't cloak because of electrical storm.");
                        return false;
                    }
            }

            //DirectEve.Log("Calling click!");
            _lastModuleAction[ItemId] = DateTime.UtcNow;
            return DirectEve.ThreadedCall(_pyModule.Attribute("Click"));
        }

        public bool Deactivate()
        {
            if (IsInLimboState)
                return false;

            if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(Deactivate))) return false;

            if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
                return false;

            _lastModuleAction[ItemId] = DateTime.UtcNow;
            DirectEve.AddEffectTimer(this);

            return DirectEve.ThreadedCall(_pyModule.Attribute("DeactivateEffect"), _pyModule.Attribute("def_effect"));
        }

        public bool OfflineModule()
        {
            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!DirectEve.Interval(500, 1000))
                return false;

            if (!IsOnline)
                return true;

            if (IsInLimboState)
                return false;

            if (DirectEve.ThreadedCall(GetPyModule.Attribute("ChangeOnline"), 0))
                return true;

            return false;
        }

        public bool OnlineModule()
        {
            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!DirectEve.Interval(500, 1000))
                return false;

            if (IsOnline)
                return true;

            if (DirectEve.ThreadedCall(GetPyModule.Attribute("ChangeOnline"), 1))
                return true;

            return false;
        }

        /// <summary>
        ///     Repairs a DirectUIModule in space with nanite paste
        /// </summary>
        /// <returns></returns>
        public bool Repair()
        {
            if (!DirectEve.Interval(2200, 3000, ItemId.ToString()))
                return false;

        	if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(950) > DateTime.UtcNow)
                return false;

            if (IsInLimboStatePassiveModules)
                return false;

            _lastModuleAction[ItemId] = DateTime.UtcNow;

            if (IsBeingRepaired)
                return true;

            if (HeatDamage == 0)
                return true;

            if (_repairAttempts.ContainsKey(ItemId))
            {
                _repairAttempts[ItemId] = _repairAttempts[ItemId] + 1;
            }
            else
            {
                _repairAttempts[ItemId] = 1;
            }

            return DirectEve.ThreadedCall(_pyModule.Attribute("RepairModule"));
        }

        public bool SetAutoReload(bool on)
        {
            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!DirectEve.Interval(500, 1000))
                return false;

            if (AutoReload == on)
                return true;

            if (IsInLimboState)
                return false;

            if (IsActive)
                return false;

            return DirectEve.ThreadedCall(_pyModule.Attribute("SetAutoReload"), on);
        }

        /// <summary>
        ///     Toggles overload of the DirectUIModule. If it's not allowed it will fail silently.
        /// </summary>
        /// <returns></returns>
        public bool ToggleOverload()
        {
            if (IsOverloadLimboState)
                return false;

            if (!DirectEve.Interval(900, 1500, ItemId.ToString()))
                return false;

            return DirectEve.ThreadedCall(_pyModule.Attribute("ToggleOverload"));
        }

        private static HashSet<int> _chargeCompatibleGroups;

        public bool WaitingForActiveTarget => _pyModule["waitingForActiveTarget"].ToBool();

        public HashSet<int> ChargeCompatibleGroups
        {
            get
            {
                if (_chargeCompatibleGroups != null)
                {
                    return _chargeCompatibleGroups;
                }

                _chargeCompatibleGroups = new HashSet<int>();

                var pyObj = PySharp.Import("__builtin__").Attribute("cfg").Attribute("__chargecompatiblegroups__");
                if (pyObj.IsValid)
                {
                    var size = pyObj.Size();
                    for (var i = 0; i < size; i++)
                    {
                        _chargeCompatibleGroups.Add(pyObj.GetItemAt(i).ToInt());
                    }
                }
                return _chargeCompatibleGroups;
            }
        }

        public bool CanBeReloaded => ChargeCompatibleGroups.Contains(GroupId);

        //public void UnloadToCargo()
        //{
        //    if (!DirectEve.Session.IsInSpace)
        //        return;
        //
        //    if (!DirectEve.Interval(500, 1000))
        //        return;
        //
        //    if (IsReloadingAmmo)
        //        return;
        //
        //    if (Charge == null)
        //        return;
        //
        //    if (Charge.ItemId <= 0)
        //        return;
        //
        //    if (Charge.TypeId <= 0)
        //        return;
        //
        //    DirectEve.ThreadedCall(_pyModule.Attribute("UnloadToCargo"), Charge.ItemId);
        //}

        internal static List<DirectUIModule> GetModules(DirectEve directEve)
        {
            if (!directEve.Session.IsInSpace)
                return new List<DirectUIModule>();

            var modules = new List<DirectUIModule>();

            //var pySharp = directEve.PySharp;
            //var carbonui = pySharp.Import("carbonui");

            var pyModules = directEve.Layers.ShipUILayer
                .Attribute("slotsContainer")
                .Attribute("modulesByID")
                .ToDictionary<long>();

            foreach (var pyModule in pyModules)
            {
                var module = new DirectUIModule(directEve, pyModule.Value)
                {
                    PyItem = pyModule.Value.Attribute("moduleinfo"),
                    ItemId = pyModule.Key,
                    IsOnline = (bool)pyModule.Value.Attribute("online"),
                    HeatDamage = _repairAttempts.ContainsKey(pyModule.Key) && _repairAttempts[pyModule.Key] > 4 ? 0 : (double)pyModule.Value.Attribute("moduleinfo").Attribute("damage"),
                    Hp = (double)pyModule.Value.Attribute("moduleinfo").Attribute("hp"),
                    IsBeingRepaired = (bool)pyModule.Value.Attribute("isBeingRepaired"),
                    AutoReload = (bool)pyModule.Value.Attribute("autoreload"),
                    //AutoRepeat = (bool)pyModule.Value.Attribute("autorepeat"),
                    DefEffect = pyModule.Value.Attribute("def_effect"),
                    //WaitingForActiveTarget = (double)pyModule.Value.Attribute("waitingForActiveTarget"),
                    //Blinking = (bool)pyModule.Value.Attribute("blinking"),
                    //IsActivatable = effect.IsValid,
                    //IsActive = (bool)effect.Attribute("isActive"),
                    //IsDeactivating = (bool)effect.Attribute("isDeactivating"),
                    //TargetId = (long?)effect.Attribute("targetID"),
                };

                module.OverloadState = pyModule.Value.Attribute("stateManager").Call("GetOverloadState", module.ItemId).ToInt();
                module.IsOverloaded = module.OverloadState == 1;
                module.IsPendingOverloading = module.OverloadState == 2;
                module.IsPendingStopOverloading = module.OverloadState == 3;
                module.IsBeingRepaired = (bool)pyModule.Value.Attribute("isBeingRepaired");
                if (module.IsBeingRepaired && _repairAttempts.ContainsKey(module.ItemId))
                {
                    _repairAttempts.Remove(module.ItemId);
                }
                module.AutoReload = (bool)pyModule.Value.Attribute("autoreload");
                module.DefEffect = pyModule.Value.Attribute("def_effect");
                //module.IsActivatable = effect.IsValid;
                //module.IsActive = (bool)effect.Attribute("isActive");
                //module.IsDeactivating = (bool)effect.Attribute("isDeactivating");
                //module.TargetId = (long?)effect.Attribute("targetID");

                var pyCharge = pyModule.Value.Attribute("charge");
                if (pyCharge.IsValid)
                {
                    module.Charge = new DirectItem(directEve);
                    module.Charge.PyItem = pyCharge;
                }

                modules.Add(module);

                if (DebugConfig.DebugModules)
                {
                    int intAttribute = 0;
                    var attributes = module.Attributes.GetAttributes();
                    Log.WriteLine("Module is [" + module.TypeName + "] TypeID [" + module.TypeId + "] GroupID [" + module.GroupId + "]");
                    foreach (KeyValuePair<string, Type> a in attributes)
                    {
                        intAttribute++;
                        Log.WriteLine("Module Attribute [" + intAttribute + "] Key[" + a.Key + "] Value [" + a.Value.ToString() + "]");
                    }
                }
            }

            return modules;
        }
        /// <summary>
        /// (IsValid, ActivationTime, ModuleCycleDurationMilliseconds, MillisecondsLeftUntilNextCycle)
        /// </summary>
        /// <returns></returns>
        public (bool, DateTime, int, int) GetEffectTiming()
        {
            try
            {
                var res = this._pyModule.Call("GetEffectTiming");
                if (res != null && res.GetPyType() == PyType.TupleType)
                {
                    var outerTuple = res.ToList();
                    if (outerTuple.Count > 0 && outerTuple[0].GetPyType() == PyType.TupleType)
                    {
                        var innterTuple = outerTuple[0].ToList();
                        if (innterTuple.Count > 1)
                        {
                            var activationTime = innterTuple[1].ToDateTime();
                            var moduleDurationMilliseconds = outerTuple[1].ToInt();
                            var isValid = true;
                            if (moduleDurationMilliseconds <= 0)
                                isValid = false;

                            var millisecondsLeftUntilNextCycle = (activationTime.AddMilliseconds(moduleDurationMilliseconds) - DateTime.UtcNow).TotalMilliseconds;
                            if (millisecondsLeftUntilNextCycle <= 0)
                                isValid = false;

                            //DirectEve.Log($"isValid {isValid} activationTime {activationTime} moduleDurationMilliseconds {moduleDurationMilliseconds} millisecondsLeftUntilNextCycle {millisecondsLeftUntilNextCycle}");

                            return (isValid, activationTime, moduleDurationMilliseconds, (int)millisecondsLeftUntilNextCycle);
                        }
                    }
                    //DirectEve.Log($"{res.LogObject()}");
                }
                return (false, DateTime.UtcNow, 0, 0);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return (false, DateTime.UtcNow, 0, 0);
            }
        }

        public int? EffectDurationMilliseconds
        {
            get
            {
                try
                {
                    var dura = DefEffect.Attribute("duration");
                    if (dura.IsValid)
                    {
                        return dura.ToInt();
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

        public DateTime? EffectStartedWhen
        {
            get
            {
                try
                {
                    var pySharp = DirectEve.PySharp;
                    var carbonui = pySharp.Import("carbonui");
                    var rampTimers = carbonui["uicore"]["uicore"]["layer"]["shipui"]["sr"]["rampTimers"];

                    if (!rampTimers.IsValid)
                        return null;

                    var rampTimersDict = rampTimers.ToDictionary<long>();
                    if (!rampTimersDict.ContainsKey(this.ItemId))
                        return null;

                    var rampTimer = rampTimersDict[this.ItemId];
                    if (!rampTimer.IsValid)
                        return null;

                    var tuple = rampTimer.ToList();

                    if (tuple.Count <= 1)
                        return null;

                    return tuple[1].ToDateTime();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public int? MillisecondsUntilNextCycle
        {
            get
            {
                try
                {
                    if (!EffectStartedWhen.HasValue)
                        return null;

                    if (!EffectDurationMilliseconds.HasValue)
                        return null;

                    var millisecondsLeftUntilNextCycle = (EffectStartedWhen.Value.AddMilliseconds(EffectDurationMilliseconds.Value) - DateTime.UtcNow).TotalMilliseconds;

                    if (millisecondsLeftUntilNextCycle <= 0)
                    {
                        return null;
                    }

                    return (int)millisecondsLeftUntilNextCycle;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }
        public bool ReloadAmmo(DirectItem newCharge, bool ignoreModuleAction = false, DirectEntity entity = null)
        {
            if (!DirectEve.Session.IsInSpace)
                return false;

            if (IsCivilianWeapon)
                return false;

            if (IsInLimboState)
                return false;

            if (newCharge.ItemId <= 0)
                return false;

            if (newCharge.TypeId <= 0)
                return false;

            if (ItemId <= 0)
                return false;

            if (IsDeactivating)
                return false;

            if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(ReloadAmmo)))
                return false;

            if (!DirectEve.DWM.ActivateWindow(typeof(DirectDesktopWindow), true))
                return false;

            if (!ignoreModuleAction)
            {
                if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
                    return false;

                _lastModuleAction[ItemId] = DateTime.UtcNow;
            }

            _isReloadingAmmo = true;

            string potentialEntityToChangeAmmoFor = string.Empty;
            if (entity != null)
            {
                potentialEntityToChangeAmmoFor = " so we can hit [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 0) + "k]";
            }

            string ExistingChargeName = string.Empty;

            if (Charge != null)
                ExistingChargeName = Charge.TypeName;
            Log.WriteLine("Reloading/Changing [" + ItemId + "][" + TypeName + "] from [" + ExistingChargeName + "] to [" + newCharge.TypeName + "]" + potentialEntityToChangeAmmoFor);
            DirectEve.AddEffectTimer(this);
            DirectEve.ThreadedCall(DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Attribute("LoadAmmoTypeToModule"), ItemId, newCharge.TypeId);
            return true;
        }

        #endregion Methods
    }
}

//		public bool Activate()
//		{
//			try
//			{
//
//				if (LastActivatedModule.ContainsKey(this.ItemId) && LastActivatedModule[this.ItemId].AddMilliseconds(500) > DateTime.UtcNow)
//				{
//					return false;
//				}
//
//				LastActivatedModule[this.ItemId] = DateTime.UtcNow;
//				return DirectEve.ThreadedCall(_pyModule.Attribute("ActivateEffect"), _pyModule.Attribute("def_effect"));
//
//			}
//			catch (Exception ex)
//			{
//
//				Console.WriteLine("DirectEve.Activate Exception [" + ex + "]");
//				return false;
//			}
//
//		}