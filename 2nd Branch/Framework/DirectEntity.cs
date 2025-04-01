extern alias SC;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Events;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.FastPriorityQueue;
using SC::SharedComponents.Utility;
using SC::SharedComponents.Extensions;
//using ServiceStack.Text;
using SharpDX.DXGI;
using EVESharpCore.Cache;
using System.Runtime;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Behaviors;
using System.Diagnostics;
using EVESharpCore.Questor.BackgroundTasks;
using SC::SharedComponents.IPC;
using EVESharpCore.Questor.Traveller;
using EVESharpCore.Framework.Lookup;
using System.Windows;

namespace EVESharpCore.Framework
{
    extern alias SC;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Numerics;
    using static EVESharpCore.Framework.DirectSceneManager;
    using static System.Net.Mime.MediaTypeNames;

    public partial class DirectEntity : DirectInvType
    {
        //
        // use this value to determine when we need to forcefully restart the client! Put the check where?
        // Defense? Traveler?
        // If this happens the client is VERY broken! We will die to this if we dont restart eve: immediately!
        //
        public static int intCountInValidEntities = 0;

        #region Fields

        public readonly DateTime _thisEntityCreated = DateTime.UtcNow;

        private int? _allianceId;

        private double? _angularVelocity;

        private double? _armorPct;

        private List<DamageType> _bestDamageTypes;

        private int? _charId;

        private int? _corpId;

        private double? _currentrmor;

        private double? _currentShield;

        private double? _currentStructure;

        private double? _distance;

        private double? _emEHP;

        private double? _entityArmorDelayChanceLarge;

        private double? _entityArmorRepairAmount;

        private double? _entityArmorRepairDuration;

        private double? _entityAttackRange;

        private int? _entityMissileTypeId;

        private double? _entityShieldBoostAmount;

        private double? _entityShieldBoostDelayChance;

        private double? _entityShieldBoostDuration;

        private double? _entityShieldRechargeRate;

        private double? _expEHP;

        private double? _explosiveDamage;

        public long? _followId;

        private string _givenName;

        private bool? _hasExploded;

        private bool? _hasReleased;

        private bool? _isAbyssalDeadspaceTriglavianBioAdaptiveCache;

        private bool? _isCloaked;

        private bool? _isDockable;

        private bool? _isEmpty;

        private double? _kinEHP;

        private int? _mode;

        private string _name;

        private double? _energyNeutralizerAmount;

        //private double? _npcArmorUniformity;
        private double? _npcDamageMultiplier;

        private double? _npcEmMissileDamage;

        private double? _npcEmMissileDps;

        private double? _npcEmTurretDamage;

        private double? _npcEmTurretDps;

        private double? _npcExplosiveDps;

        private double? _npcExplosiveMissileDamage;

        private double? _npcExplosiveMissileDps;

        private double? _npcKineticMissileDamage;

        private double? _npcKineticMissileDps;

        private double? _npcKineticTurretDamage;

        private double? _npcKineticTurretDps;

        private DirectItem _npcMissileAmmoType;

        private double? _npcMissileDamageMultiplier;

        private double? _npcMissileEntityAoeCloudSizeMultiplier;

        //private double? _npcMissileDps;
        private double? _npcMissileEntityAoeVelocityMultiplier;

        private double? _npcMissileEntityFlightTime;

        private double? _npcMissileEntityFlightTimeMultiplier;

        private double? _npcMissileEntityVelocity;

        private double? _npcMissileEntityVelocityMultiplier;

        private double? _npcMissileRateOfFire;

        private double? _npcRateOfFire;

        private double? _npcRemoteArmorRepairChance;

        private double? _npcRemoteShieldRepairChance;

        private double? _npcShieldRechargeRate;

        private double? _npcShieldUniformity;

        //private double? _npcShieldResistanceThermal;
        private double? _npcThermalMissileDamage;

        //private double? _npcShieldResistanceKinetic;
        private double? _npcThermalMissileDps;

        //private double? _npcShieldResistanceExplosive;
        private double? _npcThermalTurretDamage;

        //private double? _npcShieldResistanceEm;
        private double? _npcThermalTurretDps;

        private double? _npcTurretDps;

        private int? _ownerId;

        private double? _radius;

        private double? _rawEhp;

        private double? _shieldPct;

        private double? _signatureRadius;

        private double? _structurePct;

        private double? _structureUniformity;

        private double? _trackingSpeed;

        private double? _transversalVelocity;

        private double? _triglavianDamage;

        private double? _triglavianDps;

        private double? _trmEHP;

        private double? _velocity;

        private double? _vx;

        private double? _vy;

        private double? _vz;

        private double? _warpScrambleChance;

        private double? _wormholeAge;

        private double? _wormholeSize;

        private double? _x;

        private double? _y;

        private double? _z;
        private double? _gotoX;
        private double? _gotoY;
        private double? _gotoZ;
        private double? _estimatedPixelDiameterWithChildren;
        private Vec3? _ballPos;
        private string _dna;

        private PyObject? _pyDynamicItem;
        private Dictionary<DirectEntityFlag, bool> _entityFlags;
        private PyObject _ballpark;
        private PyObject ballpark
        {
            get
            {
                if (_ballpark != null && _ballpark.IsValid)
                {
                    return _ballpark;
                }

                _ballpark = DirectEve.GetLocalSvc("michelle").Call("GetBallpark");
                return _ballpark;
            }
        }

        private PyObject _slimItem;

        private PyObject slimItem
        {
            get
            {
                if (_slimItem != null && _slimItem.IsValid)
                    return _slimItem;

                if (ballpark.IsValid)
                {
                    _slimItem = ballpark.Call("GetInvItem", Id);
                    return _slimItem;
                }

                return null;
            }
        }

        private PyObject _ball;

        private PyObject ball
        {
            get
            {
                if (_ball != null && _ball.IsValid)
                    return _ball;

                if (ballpark.IsValid)
                {
                    _ball = ballpark.Call("GetBall", Id);
                    return _ball;
                }

                return null;
            }
        }

        #endregion Fields

        #region Constructors

        internal DirectEntity(DirectEve directEve, PyObject ballpark, PyObject ball, PyObject slimItem, long id) : base(directEve)
        {
            InfoGatheredTimeStamp = DateTime.UtcNow;

            _entityFlags = new Dictionary<DirectEntityFlag, bool>();

            Id = id;
            TypeId = (int)slimItem.Attribute("typeID");

            Attacks = new List<string>();
            ElectronicWarfare = new List<string>();

            _x = (double)ball.Attribute("x");
            _y = (double)ball.Attribute("y");
            _z = (double)ball.Attribute("z");
            _name = (string)PySharp.Import("eve.client.script.ui.util.uix").Call("GetSlimItemName", slimItem);
            _distance = (double)ball.Attribute("surfaceDist");
        }

        #endregion Constructors

        #region Properties

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

        public bool HasFlagSet(DirectEntityFlag flag)
        {
            if (_entityFlags.ContainsKey(flag) && _entityFlags[flag])
                return true;

            var svc = DirectEve.GetLocalSvc("stateSvc");
            if (svc.IsValid)
            {
                var states = svc["states"];
                if (states.IsValid)
                {
                    var dict = states.ToDictionary<int>();
                    if (dict.TryGetValue((int)flag, out var pyObj))
                    {
                        var innerDict = pyObj.ToDictionary<long>();
                        if (innerDict.TryGetValue(this.Id, out var innerPyObj))
                        {
                            if (innerPyObj.ToBool())
                            {
                                _entityFlags[flag] = true;
                                //Console.WriteLine($"Set flag [{flag}] to [true] for ent [{this.Id}] TypeName {this.TypeName}");
                                return true;
                            }
                            else
                            {
                                _entityFlags[flag] = false;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public double BandwidthUsed
        {
            get
            {
                if (CategoryId == (int)CategoryID.Drone)
                {
                    return this.TryGet<double>("droneBandwidthUsed");
                }

                return 0;
            }
        }

        public string DNA
        {
            get
            {
                if (_dna == null)
                    _dna = (string)_ball.Attribute("model").Attribute("dna");
                return _dna;
            }
        }

        public int SlimFilamentTypeId => (int)_slimItem.Attribute("abyssFilamentTypeID");

        /// <summary>
        ///     SOLO = 1, COOP = 2, TWO_PLAYER = 3
        /// </summary>
        public int SlimFilamentGameModeId => (int)_slimItem.Attribute("gameModeID");

        public float SlimSignatureRadius => (float)_slimItem.Attribute("signatureRadius");

        // actual mass with items, no static value from inv type
        public float BallMass => (float)_ball.Attribute("mass");


        public double GetSecondsToKill(Dictionary<DirectDamageType, float> damagePairs, out double effectiveDps)
        {
            return GetSecondsToKill(damagePairs, new List<DirectEntity>() { this }, out effectiveDps);
        }
        /// <summary>
        /// How long does it take to kill that specific entity will all current drones in space
        /// </summary>
        /// <returns></returns>
        public double GetSecondsToKillWithActiveDrones()
        {
            Dictionary<DirectDamageType, float> dict = new Dictionary<DirectDamageType, float>();
            foreach (var drone in DirectEve.ActiveDrones.Where(e => e.DroneState != 4))
            {
                foreach (var kv in drone.GetDroneDPS())
                {
                    if (dict.ContainsKey(kv.Key))
                    {
                        dict[kv.Key] += kv.Value;
                    }
                    else
                    {
                        dict.Add(kv.Key, kv.Value);
                    }
                }
            }
            return GetSecondsToKill(dict, out _);
        }
        /// <summary>
        /// How long does it take to kill that specific entity with the list of given drones
        /// </summary>
        /// <param name="drones"></param>
        /// <returns></returns>
        public double GetSecondsToKillWithActiveDrones(List<EntityCache> drones)
        {
            Dictionary<DirectDamageType, float> dict = new Dictionary<DirectDamageType, float>();
            foreach (var drone in Drones.ActiveDrones.Where(e => e._directEntity.DroneState != 4))
            {
                if (!drones.Contains(drone))
                    continue;

                foreach (var kv in drone._directEntity.GetDroneDPS())
                {
                    if (dict.ContainsKey(kv.Key))
                    {
                        dict[kv.Key] += kv.Value;
                    }
                    else
                    {
                        dict.Add(kv.Key, kv.Value);
                    }
                }
            }
            return GetSecondsToKill(dict, out _);
        }
        public double CalculateEffectiveDPS(Dictionary<DirectDamageType, float> damagePairs)
        {
            var effDps = 0d;
            var secs = GetSecondsToKill(damagePairs, new List<DirectEntity>() { this }, out effDps);
            return effDps;
        }

        public static double CalculateEffectiveDPS(Dictionary<DirectDamageType, float> damagePairs, List<DirectEntity> entities)
        {
            var effDps = 0d;
            var secs = GetSecondsToKill(damagePairs, entities, out effDps);
            return effDps;
        }

        public static double GetSecondsToKill(Dictionary<DirectDamageType, float> damagePairs, List<DirectEntity> entities, out double effectiveDps)
        {
            // This will return Shield, Armor, Structure base HP combined if resists can't be read
            double effectiveHealthEM = 0;
            double effectiveHealthKinetic = 0;
            double effectiveHealthExplosive = 0;
            double effectiveHealthThermal = 0;

            double combinedHealth = 0;

            effectiveDps = 0;

            foreach (var ent in entities)
            {
                effectiveHealthEM += ent.EmEHP.Value;
                effectiveHealthKinetic += ent.KinEHP.Value;
                effectiveHealthExplosive += ent.ExpEHP.Value;
                effectiveHealthThermal += ent.TrmEHP.Value;
                combinedHealth += (ent.CurrentShield ?? 0) + (ent.CurrentArmor ?? 0) + (ent.CurrentStructure ?? 0);
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

                        if (Double.IsInfinity(effectiveHealthEM) || Double.IsNaN(effectiveHealthEM))
                            continue;

                        effectiveHealthEM *= percentage;
                        totalEffEHp += effectiveHealthEM;
                        break;
                    case DirectDamageType.KINETIC:

                        if (Double.IsInfinity(effectiveHealthKinetic) || Double.IsNaN(effectiveHealthKinetic))
                            continue;

                        effectiveHealthKinetic *= percentage;
                        totalEffEHp += effectiveHealthKinetic;
                        break;
                    case DirectDamageType.EXPLO:

                        if (Double.IsInfinity(effectiveHealthExplosive) || Double.IsNaN(effectiveHealthExplosive))
                            continue;

                        effectiveHealthExplosive *= percentage;
                        totalEffEHp += effectiveHealthExplosive;
                        break;
                    case DirectDamageType.THERMAL:

                        if (Double.IsInfinity(effectiveHealthThermal) || Double.IsNaN(effectiveHealthThermal))
                            continue;

                        effectiveHealthThermal *= percentage;
                        totalEffEHp += effectiveHealthThermal;
                        break;
                }
            }

            double secondsToKill = totalEffEHp / totalDps;
            if (combinedHealth > 0 && totalEffEHp > 0)
            {
                var ratio = combinedHealth / (double)totalEffEHp;
                effectiveDps = totalDps * ratio;
            }

            return secondsToKill;
        }
        public DateTime InfoGatheredTimeStamp;

        public bool IsActiveDrone
        {
            get
            {
                if (DirectEve.ActiveDrones.Any(x => x.Id == Id))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsBeingFollowedOrAttackedByPotentialCombatTarget
        {
            get
            {
                if (Combat.PotentialCombatTargets.Any(i => i.FollowId == Id))
                {
                    return true;
                }

                return false;
            }
        }


        private bool? _isAbyssGateOpen;

        public bool IsAbyssGateOpen()
        {
            if (this.TypeId != 47685 && this.TypeId != 47686)
                return false;

            if (_isAbyssGateOpen == null)
            {
                _isAbyssGateOpen = slimItem["isAbyssGateOpen"].ToBool();
            }
            return _isAbyssGateOpen.Value;
        }

        public PyObject DynamicItem
        {
            get
            {
                _pyDynamicItem ??= DirectEve.GetLocalSvc("dynamicItemSvc")["dynamicItemCache"].DictionaryItem(this.Id);
                return _pyDynamicItem;
            }
        }


        public bool IsDynamicItem
        {
            get
            {
                var evetypes = PySharp.Import("evetypes");
                return evetypes.Call("IsDynamicType", this.TypeId).ToBool();

                //return this.TryGet<bool>("isDynamicType", true);
            }
        }

        public DirectInvType OrignalDynamicItem
        {
            get
            {
                if (IsDynamicItem)
                {
                    var sourceTypeID = DynamicItem["sourceTypeID"].ToInt();
                    return DirectEve.GetInvType(sourceTypeID);
                }
                return null;
            }
        }

        public override T TryGet<T>(string keyname)
        {

            if (IsDynamicItem)
            {
                var sourceTypeID = DynamicItem["sourceTypeID"].ToInt();
                var value = DirectEve.GetInvType(sourceTypeID).TryGet<T>(keyname);
                return value;
            }

            return base.TryGet<T>(keyname);
        }

        #region Abyss types
        public bool IsLargeBioCloud => this.TypeId == 47441;
        public bool IsMedBioCloud => this.TypeId == 47440;
        public bool IsSmallBioCloud => this.TypeId == 47439;

        /// <summary>
        /// +300% Signature Radius (4.0x signature radius multiplier).
        /// </summary>
        public bool IsBioCloud => IsLargeBioCloud || IsMedBioCloud || IsSmallBioCloud;

        public bool IsLargeTachCloud
        {
            get
            {
                if (TypeId == (int)TypeID.LargeTachyonCloud)
                {
                    try
                    {
                        //DrawSphere(25000);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    return true;
                }

                return false;
            }
        }

        public bool IsMedTachCloud
        {
            get
            {
                if (TypeId == (int)TypeID.MediumTachyonCloud)
                {
                    //close?
                    try
                    {
                        //DrawSphere(20000);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    return true;
                }

                return false;
            }
        }
        public bool IsSmallTachCloud
        {
            get
            {
                if (TypeId == (int)TypeID.SmallTachyonCloud)
                {
                    try
                    {
                        //DrawSphere(12000);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// +300% Velocity (x4.0 velocity), -50% Inertia Modifier.
        /// </summary>
        public bool IsTachCloud => IsLargeTachCloud || IsMedTachCloud || IsSmallTachCloud;

        public bool IsLargeFilaCloud => this.TypeId == 47473;
        public bool IsMedFilaCloud => this.TypeId == 47472;
        public bool IsSmallFilaCloud => this.TypeId == 47620;

        /// <summary>
        /// Penalty to Shield Booster boosting (-40%) and reduction to shield booster duration (-40%)
        /// </summary>
        public bool IsFilaCloud => IsLargeFilaCloud || IsMedFilaCloud || IsSmallFilaCloud;

        public bool IsMediumRangeAutomataPylon => this.TypeId == 47438;

        public bool IsShortRangeAutomataPylon => this.TypeId == 47437;

        /// <summary>
        /// A Triglavian area-denial structure equipped with a medium-range point-defense system that will target all drones, missiles, and rogue drone frigates within its field of fire.
        /// </summary>
        public bool IsAutomataPylon => IsMediumRangeAutomataPylon || IsShortRangeAutomataPylon;

        public bool IsMediumRangeTrackingPylon => this.TypeId == 47470;

        public bool IsShortRangeTrackingPylon => this.TypeId == 47469;
        /// <summary>
        /// Tracking Pylon: +60% or +80% tracking to all ships in its area of effect.
        /// </summary>
        public bool IsTrackingPylon => IsMediumRangeTrackingPylon || IsShortRangeTrackingPylon;

        /// <summary>
        /// As of 2022 this pylon is currently unused.
        /// +20% velocity and +10% damage for all local rogue drones, -30% velocity and -15% damage for all local capsuleer drones
        /// </summary>
        public bool IsWideAreaAutomataPylon => this.TypeId == 48254;


        /// <summary>
        /// IsWideAreaAutomataPylon || IsTrackingPylon || IsAutomataPylon || IsFilaCould || IsTachCloud || IsBioCloud
        /// </summary>
        public bool IsAbyssSphereEntity => IsWideAreaAutomataPylon || IsTrackingPylon || IsAutomataPylon || IsFilaCloud || IsTachCloud || IsBioCloud;
        /// <summary>
        /// IsEntityWeWantToAvoidInAbyssals = IsTrackingPylon || IsAutomataPylon || IsWideAreaAutomataPylon || IsFilaCould || IsBioCloud
        /// </summary>
        //public bool IsEntityWeWantToAvoidInAbyssals => IsTrackingPylon || IsAutomataPylon || IsWideAreaAutomataPylon || IsFilaCould || IsBioCloud;

        public bool IsEntityWeWantToAvoidInAbyssals
        {
            get
            {
                if (!RecursionCheck(nameof(IsEntityWeWantToAvoidInAbyssals)))
                    return false;

                //FixME
                if (IsTrackingPylon || IsAutomataPylon || IsWideAreaAutomataPylon || IsBioCloud)
                    return true;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController) && Combat.PotentialCombatTargets.Any())
                {
                    if (IsTrackingPylon || IsAutomataPylon || IsWideAreaAutomataPylon || IsBioCloud)
                        return true;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController) && Combat.PotentialCombatTargets.Any(i => i.IsTarget || i.IsTargeting))
                {
                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidTachyonClouds)
                    {
                        if (IsTachCloud)
                            return true;
                    }

                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidBioluminescenceClouds)
                    {
                        if (IsBioCloud)
                            return true;
                    }

                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidFilamentClouds)
                    {
                        if (IsFilaCloud)
                            return true;

                    }

                    //towers...
                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers)
                    {
                        if (IsAutomataPylon)
                            return true;
                    }

                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidMultibodyTrackingPylonTowers)
                    {
                        if (IsTrackingPylon)
                            return true;
                    }
                }

                return false;
            }
        }

        private double? _gigaJouleNeutedPerSecond;

        public double GigaJouleNeutedPerSecond
        {
            get
            {
                if (_gigaJouleNeutedPerSecond.HasValue)
                    return _gigaJouleNeutedPerSecond.Value;

                if (!IsNeutingEntity)
                    return 0d;

                var neutAmount = this.TryGet<double>("energyNeutralizerAmount"); //basically 4x of behaviorEnergyNeutralizerDischarge - How do we know if this is right?
                var behaviorEnergyNeutralizerDischarge = this.TryGet<double>("behaviorEnergyNeutralizerDischarge");
                var behaviorEnergyNeutralizerDuration = this.TryGet<double>("behaviorEnergyNeutralizerDuration");

                var behaviorEnergyNeutralizerRange = this.TryGet<double>("behaviorEnergyNeutralizerRange");
                var behaviorEnergyNeutralizerFalloff = this.TryGet<double>("behaviorEnergyNeutralizerFalloff");

                if (behaviorEnergyNeutralizerDuration != 0)
                {
                    _gigaJouleNeutedPerSecond = neutAmount / (behaviorEnergyNeutralizerDuration / 1000);

                    if (Distance > behaviorEnergyNeutralizerFalloff + behaviorEnergyNeutralizerRange)
                    {
                        _gigaJouleNeutedPerSecond = _gigaJouleNeutedPerSecond / 2;
                    }

                    return _gigaJouleNeutedPerSecond.Value;
                }

                _gigaJouleNeutedPerSecond = 0;
                return _gigaJouleNeutedPerSecond.Value;
            }
        }

        public bool IsLocalArmorRepairingEntity => GetDmgEffects().ContainsKey(2197) ||
                                            GetDmgEffectsByGuid().ContainsKey("effects.ArmorRepair");

        public bool IsLocalShieldRepairingEntity => GetDmgEffects().ContainsKey(6990) ||
                                                     GetDmgEffectsByGuid().ContainsKey("effects.ShieldBoosting");

        public double LocalShieldRepairingAmountPerSecond
        {
            get
            {
                if (!IsLocalShieldRepairingEntity)
                    return 0;

                var amount = this.TryGet<double>("behaviorShieldBoosterAmount");
                var duration = this.TryGet<double>("behaviorShieldBoosterDuration");

                if (amount == 0d)
                    return 0;

                return amount / duration;
            }
        }

        public double LocalArmorRepairingAmountPerSecond
        {
            get
            {
                if (!IsLocalArmorRepairingEntity)
                    return 0d;

                var amount = this.TryGet<double>("entityArmorRepairAmount");
                var duration = this.TryGet<double>("entityArmorRepairDuration");

                if (duration == 0d)
                    return 0;

                return amount / duration;
            }
        }

        public bool HasLocalArmorRepair
        {
            get
            {
                var amount = this.TryGet<double>("behaviorArmorRepairAmount");
                var duration = this.TryGet<double>("behaviorArmorRepairDuration");
                return amount > 0d && duration > 0d;

            }
        }


        public bool NPCHasVortonProjectorGuns => GetAttributesInvType().ContainsKey("VortonArcTargets");

        #endregion Abyss types

        private double? _radiusOverride;

        public double? RadiusOverride
        {
            get
            {
                if (_radiusOverride == null)
                {
                    // pylons ... clouds
                    if (this.GroupId == 1971 || this.GroupId == 1981 || this.GroupId == (int)Group.SentryGun) // http://games.chruker.dk/eve_online/inventory.php?group_id=1971 | https://everef.net/groups/1981 | https://everef.net/groups/99 (Sentry Gun -- Debug)
                    {
                        var offset = 1000;

                        if (IsTachCloud)
                            offset = 1500;

                        _radiusOverride = this.MaxRange + this.Radius + offset; // add 4k radius to not always move along the edges of the spheres (clouds, towers.. )

                        // sentry gun override
                        if (this.GroupId == (int)Group.SentryGun)
                            _radiusOverride = +this.Radius + 29000;

                        return _radiusOverride;
                    }

                    var ret = ModelBoundingSphereRadius > BallRadius ? ModelBoundingSphereRadius : BallRadius;
                    _radiusOverride = ret;
                }

                return _radiusOverride;
            }
        }

        /// <summary>
        /// Returns the average distance to the list of entities
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public double DistanceTo(IEnumerable<DirectEntity> entities)
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

        public double DistanceTo(DirectWorldPosition directWorldPosition)
        {
            return Math.Round(Math.Sqrt((directWorldPosition.XCoordinate - XCoordinate.Value) * (directWorldPosition.XCoordinate - XCoordinate.Value) + (directWorldPosition.YCoordinate - YCoordinate.Value) * (directWorldPosition.YCoordinate - YCoordinate.Value) + (directWorldPosition.ZCoordinate - ZCoordinate.Value) * (directWorldPosition.ZCoordinate - ZCoordinate.Value)), 2);
        }

        public double DistanceTo(Vec3 vec3)
        {
            return Math.Round(Math.Sqrt((vec3.X - XCoordinate.Value) * (vec3.X - XCoordinate.Value) + (vec3.Y - YCoordinate.Value) * (vec3.Y - YCoordinate.Value) + (vec3.Z - ZCoordinate.Value) * (vec3.Z - ZCoordinate.Value)), 2);
        }

        public bool IsInNPCsOptimalRange => Distance <= OptimalRange + AccuracyFalloff / 2;
        public bool IsInOptimalRangeTo(DirectEntity ent) => ent.DistanceTo(this) <= OptimalRange + AccuracyFalloff / 2;


        private DirectWorldPosition _directAbsolutePosition;
        public DirectWorldPosition DirectAbsolutePosition
        {
            get
            {
                if (_directAbsolutePosition != null)
                    return _directAbsolutePosition;

                if (!IsXYZCoordValid)
                {
                    //if (XCoordinate == null) Log.WriteLine("Name [" + Name + "] XCoordinate == null");
                    //if (YCoordinate == null) Log.WriteLine("Name [" + Name + "] YCoordinate == null");
                    //if (ZCoordinate == null) Log.WriteLine("Name [" + Name + "] ZCoordinate == null");
                    return null;
                }

                _directAbsolutePosition = new DirectWorldPosition((double)XCoordinate, (double)YCoordinate, (double)ZCoordinate, DirectEve);
                //DirectEve.DictEntitiesPositionInfoCachedAcrossFrames.AddOrUpdate(Id, _directWorldPosition.PositionInSpace);
                return _directAbsolutePosition;
            }
        }
        /// <summary>
        /// // Only collidable entities have a world pos, will return (-1,-1,-1) if there is none
        /// This is relative to current ship. Current ship is always at (0,0,0)
        /// </summary>
        private Vec3? _worldPos = null;
        public Vec3? WorldPos
        {
            get
            {
                if (_worldPos == null)
                {
                    var res = Ball["model"]["worldPosition"].ToList();
                    if (res.Count > 0)
                        _worldPos = new Vec3(res[0].ToDouble(), res[1].ToDouble(), res[2].ToDouble());
                    else
                        _worldPos = new Vec3(-1, -1, -1);
                }
                return _worldPos;
            }
        }


        // This is the relative position to the player. X,Y,Z are absolute positions
        public Vec3 BallPos
        {
            get
            {
                if (_ballPos == null)
                {
                    var camUtil = DirectEve.PySharp.Import("eve.client.script.ui.camera.cameraUtil");
                    var res = camUtil.Call("_GetBallPosition", this.Ball).ToList();
                    _ballPos = new Vec3(res[0].ToDouble(), res[1].ToDouble(), res[2].ToDouble());
                }
                return _ballPos.Value;
            }
        }

        public List<DirectWorldPosition> _sphereCoordinates = new List<DirectWorldPosition>();

        public List<DirectWorldPosition> SphereCoordinates(double radius = 30000, int numCircles = 20, int numPointsPerCircle = 20) //radius is in meters
        {
            try
            {
                if (_sphereCoordinates != null && _sphereCoordinates.Any())
                    return _sphereCoordinates;

                for (int i = 0; i < numCircles; i++)
                {
                    double latitude = Math.PI * (i + 1) / (numCircles + 1);  // Calculate the latitude angle
                    List<DirectWorldPosition> _tempCircleCoordinates = CircleCoordinates(latitude, radius, numPointsPerCircle);
                    _sphereCoordinates.AddRange(_tempCircleCoordinates);
                }

                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine($"Sphere Coord found: [" + _sphereCoordinates.Count() + "]");
                return _sphereCoordinates;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return new List<DirectWorldPosition>();
            }
        }

        public double? _circleCoordinatesRadius = null;
        public double? _circleCoordinatesLatitude = null;
        public int? _circleCoordinatesNumPoints = null;

        public List<DirectWorldPosition> _circleCoordinates = new List<DirectWorldPosition>();

        public List<DirectWorldPosition> CircleCoordinates(double latitude , double radius = 30000, int numPoints = 30) //radius is in meters
        {
            try
            {
                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("CircleCoordinates: latitude[" + latitude + "] radius[" + radius + "] numPoints[" + numPoints + "]");

                if (_circleCoordinates != null && _circleCoordinates.Any() && _circleCoordinates.Count() == numPoints && _circleCoordinatesRadius == radius && _circleCoordinatesNumPoints == numPoints)
                    return _circleCoordinates;

                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("_circleCoordinates not being used: recalculating");

                if (_circleCoordinates != null && _circleCoordinates.Any() && _circleCoordinates.Count() != numPoints)
                {
                    if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("_circleCoordinates.Count() [" + _circleCoordinates.Count() + "] != numPoints [" + numPoints + "]");
                    _circleCoordinates = new List<DirectWorldPosition>();
                }

                if (_circleCoordinatesLatitude != null && latitude != _circleCoordinatesLatitude)
                {
                    _circleCoordinatesLatitude = latitude;
                    _circleCoordinates = new List<DirectWorldPosition>();
                }

                if (_circleCoordinatesRadius != null && _circleCoordinatesRadius != radius)
                {
                    _circleCoordinatesRadius = radius;
                    _circleCoordinates = new List<DirectWorldPosition>();
                }

                if (_circleCoordinatesNumPoints != null && _circleCoordinatesNumPoints != numPoints)
                {
                    _circleCoordinatesNumPoints = numPoints;
                    _circleCoordinates = new List<DirectWorldPosition>();
                }

                if (XCoordinate == null || XCoordinate == 0)
                {
                    Log.WriteLine("[" + TypeName + "] XCoordinate [" + XCoordinate + "] YCoordinate [" + YCoordinate + "] ZCoordinate [" + ZCoordinate + "]");
                }

                if (YCoordinate == null || YCoordinate == 0)
                {
                    Log.WriteLine("[" + TypeName + "] XCoordinate [" + XCoordinate + "] YCoordinate [" + YCoordinate + "] ZCoordinate [" + ZCoordinate + "]");
                }

                if (ZCoordinate == null || ZCoordinate == 0)
                {
                    Log.WriteLine("[" + TypeName + "] XCoordinate [" + XCoordinate + "] YCoordinate [" + YCoordinate + "] ZCoordinate [" + ZCoordinate + "]");
                }


                for (int i = 0; i < numPoints; i++)
                {
                    try
                    {
                        if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("i [" + i + "] for (int i = 0; i < numPoints; i++)");
                        double longitude = 2 * Math.PI * i / numPoints;  // Calculate the longitude angle

                        // Calculate the X and Y coordinates for the current point
                        double x = (double)XCoordinate + (int)(radius * Math.Cos(latitude) * Math.Cos(longitude));
                        double y = (double)YCoordinate + (int)(radius * Math.Cos(latitude) * Math.Sin(longitude));
                        double z = (double)ZCoordinate + (int)(radius * Math.Sin(latitude));
                        var thisVec3 = new Vec3(x, y, z);
                        var thisPosition = new DirectWorldPosition(thisVec3, DirectEve);
                        var distanceToThisPosition = thisPosition.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition);
                        if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("i[" + i + "]: Distance [" + Math.Round(distanceToThisPosition / 1000, 0) + "k] X[" + x + "] Y[" + y + "] Z[" + z + "]");
                        if (_circleCoordinates.Any(i => i.XCoordinate == x && i.YCoordinate == y && i.ZCoordinate == z))
                        {
                            if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("_circleCoordinates already contains i[" + i + "]: X[" + x + "] Y[" + y + "] Z[" + z + "]");
                        }
                        else if(!AnyIntersectionAtThisPosition(thisPosition,
                            false, //entities
                            true,  //tracking pylons
                            true,  //automata pylons
                            true,  //wide area automata pylons
                            false,  //fila clouds
                            false,  //bio clouds
                            false  //tack clouds
                            ).Any())
                        {
                                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("_circleCoordinates Add i[" + i + "]: X[" + x + "] Y[" + y + "] Z[" + z + "]");
                                _circleCoordinates.Add(new DirectWorldPosition(x, y, z));
                        }
                        else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("AnyIntersectionAtThisPosition TRUE: cant use this spot i[" + i + "]: X[" + x + "] Y[" + y + "] Z[" + z + "]");

                        if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("i[" + i + "] _circleCoordinates.Count() [" + _circleCoordinates.Count() + "]");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                        if (_circleCoordinates != null && _circleCoordinates.Any())
                        {
                            if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine($"Circle Coord found: [" + _circleCoordinates.Count() + "]!!!");
                            continue;
                        }

                        if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine($"Circle Coord found: [" + _circleCoordinates.Count() + "] none!");
                        continue;
                    }
                }

                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine($"Circle Coord found: [" + _circleCoordinates.Count() + "].");
                return _circleCoordinates ?? new List<DirectWorldPosition>();
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                if (_circleCoordinates != null && _circleCoordinates.Any())
                {
                    if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine($"Circle Coord found: [" + _circleCoordinates.Count() + "]!.!");
                    return _circleCoordinates;
                }

                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine($"Circle Coord found: [" + _circleCoordinates.Count() + "] none!!");
                return new List<DirectWorldPosition>();
            }
        }

        //public void ShowBoxes(bool val)
        //{
        //    this.Ball.SetAttribute("showBoxes", val);
        //}

        // a = next wp
        // b =  final dest


        // moves to the entity via ongrid pathfinding
        // returns true if the destination is near, false if moving
        // use higher stepSize with higher speeds
        private static List<DirectWorldPosition> _currentPath;
        private static DirectWorldPosition _currentDestination;
        private static DateTime _lastPathFind;


        public bool MoveToViaAStar()
        {
            return MoveToViaAStar(ignoreAbyssEntities: true, dest: this.DirectAbsolutePosition);
        }

        public static bool MoveToViaAStar(DirectWorldPosition dest = null)
        {
            return MoveToViaAStar(ignoreAbyssEntities: true, dest: dest);
        }

        public static bool MoveToViaAStar(int stepSize = 5000, int distanceToTarget = 9000,
            int distToNextWaypoint = 5000,
            bool drawPath = true, bool disableMoving = false, bool forceRecreatePath = false,
            DirectWorldPosition dest = null)
        {
            return MoveToViaAStar(stepSize: stepSize, distanceToTarget: distanceToTarget,
                distToNextWaypoint: distToNextWaypoint, drawPath: drawPath, disableMoving: disableMoving,
                forceRecreatePath: forceRecreatePath, dest: dest, ignoreAbyssEntities: true);
        }

        public static bool MoveToViaAStar(int stepSize = 5000, int distanceToTarget = 9000,
            int distToNextWaypoint = 5000,
            bool drawPath = true,
            bool disableMoving = false,
            bool forceRecreatePath = false,
            DirectWorldPosition dest = null,
            bool ignoreAbyssEntities = false,
            bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false,
            bool ignoreWideAreaAutomataPylon = false,
            bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false,
            bool ignoreTachClouds = false,
            //DirectEntity destinationEntity = null,
            bool optimizedPath = true)
        {
            if (!DirectEve.HasFrameChanged())
                return false;

            if (!DirectEve.Interval(500))
                return false;

            if (!ESCache.Instance.DirectEve.Session.IsInSpace)
                return false;

            if (ESCache.Instance.DirectEve.Me.IsWarpingByMode)
                return false;

            var activeShip = ESCache.Instance.DirectEve.ActiveShip.Entity;

            var distanceInMeters = Math.Round(ESCache.Instance.DirectEve.ActiveShip.Entity.DirectAbsolutePosition.GetDistance(dest), 0);
            var distInKm = Math.Round(distanceInMeters / 1000, 0);

            if (DirectSceneManager.LastRedrawSceneColliders.AddSeconds(15) < DateTime.UtcNow)
            {
                try
                {
                    ESCache.Instance.DirectEve.SceneManager.RedrawSceneColliders(ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }

            if (!DirectEve.Interval(1500, 1500, distInKm.ToString())) return true;

            //if (_currentPath.Count == 0 && dist < distanceToTarget)
            if (distanceInMeters < distanceToTarget)
            {
                _currentDestination = null;
                _currentPath = null;
                //DirectEve.Log($"Destination reached.");
                if (drawPath)
                {
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                }

                if ((DebugConfig.DebugMoveToViaAStar || DebugConfig.DebugNavigateOnGrid) && DirectEve.Interval(10000))
                {
                    ESCache.Instance.DirectEve.Log($"Destination reached. Distance [{distInKm}] km");
                }

                return true;
            }

            if (_currentDestination != dest ||
                (_lastPathFind.AddSeconds(3) < DateTime.UtcNow && (_currentPath == null || !_currentPath.Any())) ||
                _currentPath == null ||
                forceRecreatePath)
            {
                _lastPathFind = DateTime.UtcNow;
                _currentDestination = dest;
                List<DirectWorldPosition> path = null;
                int ms = 0;

                using (new DisposableStopwatch(t => ms = (int)t.TotalMilliseconds))
                {
                    path = ESCache.Instance.DirectEve.ActiveShip.Entity.CalculatePathTo(_currentDestination, stepSize,
                        ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon,
                        ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, optimizedPath);
                }

                _currentPath = path;
                var pathCount = _currentPath.Count;
                if (pathCount == 0)
                {
                    if (!disableMoving)
                    {
                        if (DirectEve.Interval(10000))
                            ESCache.Instance.DirectEve.Log($"Warning: No path found: Approaching instead");
                        ESCache.Instance.DirectEve.ActiveShip.MoveTo(dest);
                    }

                    return false;
                }

                if (DebugConfig.DebugMoveToViaAStar) ESCache.Instance.DirectEve.Log($"Path calculation finished. Took [{ms}] ms. Count [{pathCount}]");
            }

            if (_currentPath == null)
            {
                if (!disableMoving)
                {
                    if (DirectEve.Interval(10000))
                        ESCache.Instance.DirectEve.Log($"Warning: CurrentPath == null.");
                    ESCache.Instance.DirectEve.ActiveShip.MoveTo(dest);
                }

                return false;
            }


            if (_currentPath.Count == 0)
            {
                if (!disableMoving)
                {
                    ESCache.Instance.DirectEve.Log($"No valid path found, moving to destination.");
                    ESCache.Instance.DirectEve.ActiveShip.MoveTo(dest);
                }

                return false;
            }

            if (ESCache.Instance.AbyssalCenter != null && ESCache.Instance.DirectEve.Me.IsInAbyssalSpace() && _currentPath.Count > 0 && activeShip.DirectAbsolutePosition.GetDistanceSquared(ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition) > DirectEntity.AbyssBoundarySizeSquared)
            {
                if (_currentPath.Any(p => p.GetDistanceSquared(ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition) > 82000L * 82000L))
                {
                    _currentPath = new List<DirectWorldPosition>() { dest };
                    ESCache.Instance.DirectEve.Log($"Cleared path, we found waypoints further outside than 7k of the abyss boundary sphere.");
                    return false;
                }
            }

            var current = _currentPath.FirstOrDefault();

            if (current.GetDistance(activeShip.DirectAbsolutePosition) < distToNextWaypoint)
            {
                if (DebugConfig.DebugMoveToViaAStar && DirectEve.Interval(10000))
                    ESCache.Instance.DirectEve.Log($"Removed a waypoint. {current}. Remaining [{_currentPath.Count}]");
                _currentPath.Remove(current);
                current = _currentPath.FirstOrDefault();
            }

            if (_currentPath != null && _currentPath.Count > 0 && drawPath)
            {
                ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                DrawWayPoints(_currentPath);
            }

            // check if all of the waypoints is a direct path to the destination
            //bool approachTo = false;

            //if (destinationEntity != null && _currentPath.Count > 0)
            if (_currentPath.Count > 0)
            {
                var cnt = _currentPath.Count;
                var flagCnt = _currentPath.Count(e => e.DirectPathFlag);
                if (flagCnt + 1 >= cnt) // add 1 because the start pos has no flag set
                {
                    if (DebugConfig.DebugMoveToViaAStar && DirectEve.Interval(5000))
                        ESCache.Instance.DirectEve.Log(
                            $"There are no obstacles between us and the final target, using approach instead of moveTo.");
                    //approachTo = true;
                }
                // ESCache.Instance.DirectEve.Log(
                //     $"---- _currentPathCount [{_currentPath.Count}] _currentPathCountFlag [{_currentPath.Count(e => e.DirectPathFlag)}]");
            }

            if (current != null)
            {
                if (DirectEve.Interval(800, 900))
                {
                    if (!disableMoving)
                    {
                        if ((DebugConfig.DebugMoveToViaAStar || DebugConfig.DebugNavigateOnGrid) && DirectEve.Interval(5000))
                            ESCache.Instance.DirectEve.Log(
                                $"Moving to next waypoint. Current wp [{current}] Distance to next waypoint [{Math.Round((current.GetDistance(activeShip.DirectAbsolutePosition) / 1000), 2)}] km");

                        //if (approachTo && destinationEntity != null &&
                        //    !destinationEntity.IsApproachedOrKeptAtRangeByActiveShip && destinationEntity.Distance < 150_000)
                        //{
                        //    ESCache.Instance.DirectEve.Log($"Approach to [{destinationEntity.Name}]");
                        //    destinationEntity.Approach();
                        //    return true;
                        //}

                        ESCache.Instance.DirectEve.ActiveShip.MoveTo(current);
                        return false;
                    }
                }
            }

            return false;
        }

        public static void DrawWayPoints(List<DirectWorldPosition> path)
        {
            var me = ESCache.Instance.DirectEve.ActiveShip.Entity;
            if (me != null)
            {
                var meWorldPos = me.DirectAbsolutePosition;
                //DirectEve.SceneManager.ClearDebugLines();
                var prev = me.BallPos;
                foreach (var waypoint in path)
                {
                    var wpPos = meWorldPos.GetDirectionalVectorTo(waypoint);
                    ESCache.Instance.DirectEve.SceneManager.DrawLine(prev, wpPos);
                    prev = wpPos;
                }
            }
        }


        /// <summary>
        ///
        /// UNLOAD_COLLISION_INFO = 0
        /// SHOW_COLLISION_DATA = 1
        /// SHOW_DESTINY_BALL = 2
        /// SHOW_MODEL_SPHERE = 3
        /// SHOW_BOUNDING_SPHERE = 4
        /// </summary>
        /// <param name="mode"></param>
        public void ShowDestinyBalls(int mode)
        {
            var id = DirectEve.ActiveShip.Entity.Id;
            var eve = DirectEve.PySharp.Import("eve");
            var modelDebugFunctions = eve["client"]["script"]["ui"]["services"]["menuSvcExtras"]["modelDebugFunctions"];
            if (modelDebugFunctions.IsValid && modelDebugFunctions["ShowDestinyBalls"].IsValid)
            {
                modelDebugFunctions.Call("ShowDestinyBalls", this.Id, mode);
                //DirectEve.ThreadedCall(modelDebugFunctions["ShowDestinyBalls"], this.Id, mode);
            }
            else
            {
                DirectEve.Log("Warning: modelDebugFunctions not valid!");
            }
        }

        // Note: This opens a window. Window operations are logged. Don't cry later
        public void ShowInBlueViewer()
        {
            var qatools = DirectEve.PySharp.Import("eveclientqatools");
            var blueviewer = qatools["blueobjectviewer"];
            var model = this.Ball.Call("GetModel");
            var call = blueviewer["Show"];
            if (model.IsValid && blueviewer.IsValid && call.IsValid)
            {
                DirectEve.Log($"Opening blue viewer with modal of [{this.Id}]");
                DirectEve.ThreadedCall(call, model);
            }
        }

        private int? _miniBallAmount;
        public int MiniBallAmount => _miniBallAmount ??= MiniBalls.Count;

        private int? _miniBoxesAmount;
        public int MiniBoxesAmount => _miniBoxesAmount ??= Ball["miniBoxes"].Size();

        public bool IsMassive => Ball["isMassive"].ToBool();

        public bool IsInvulnerable => !IsMassive;

        private int? _miniCapsuleAmount;
        public int MiniCapsulesAmount => _miniCapsuleAmount ??= Ball["miniCapsules"].Size();

        private bool? _hasAnyColliders;
        private static Dictionary<long, bool> _hasAnyCollidersCacheByTypeId = new Dictionary<long, bool>();

        public List<IGeometry> GetAllColliders => MiniBalls.Cast<IGeometry>().Concat(MiniBoxes).Concat(MiniCapsules).ToList();

        public bool HasAnyNonTraversableColliders
        {
            get
            {
                if (_hasAnyCollidersCacheByTypeId.TryGetValue(this.Id, out var res))
                {
                    _hasAnyColliders = res;
                }

                if (_hasAnyColliders == null)
                {
                    var miniBallAmount = MiniBalls.Count(e => !e.Traversable);
                    _hasAnyColliders = MiniBallAmount > 0 || MiniBoxesAmount > 0 || MiniCapsulesAmount > 0;
                    _hasAnyCollidersCacheByTypeId[this.TypeId] = _hasAnyColliders.Value;
                }

                return _hasAnyColliders.Value;
            }
        }

        public static void OnSessionChange()
        {
            ResetColliderCaches();
        }

        public static int AStarErrors = 0;

        public static void ResetColliderCaches()
        {
            _miniBallsCacheByTypeId = new Dictionary<string, List<DirectMiniBall>>();
            _miniBoxesCacheByTypeId = new Dictionary<string, List<DirectMiniBox>>();
            _miniCapsulesCacheByTypeId = new Dictionary<string, List<DirectMiniCapsule>>();
            _boundingSphereRadiusCache = new Dictionary<long, double>();
            _hasAnyCollidersCacheByTypeId = new Dictionary<long, bool>();
        }

        private static Dictionary<long, double> _boundingSphereRadiusCache = new Dictionary<long, double>();
        public double BoundingSphereRadius()
        {
            try
            {
                if (_boundingSphereRadiusCache.TryGetValue(this.Id, out var max))
                    return max;

                foreach (var ball in MiniBalls)
                {
                    var val = ball.Center.Magnitude + ball.MaxBoundingRadius;
                    if (val > max)
                        max = val;
                }

                foreach (var capsule in MiniCapsules)
                {
                    var val = capsule.Center.Magnitude + capsule.MaxBoundingRadius;
                    if (val > max)
                        max = val;
                }

                foreach (var rect in MiniBoxes)
                {
                    var val = rect.Center.Magnitude + rect.MaxBoundingRadius;
                    if (val > max)
                        max = val;
                }

                //Console.WriteLine($"Ent [{this.Id}] TypeName [{this.TypeName}] Max [{max}]");

                _boundingSphereRadiusCache[this.Id] = max;
                return max;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        private List<DirectMiniBall> _miniBalls;

        private static Dictionary<string, List<DirectMiniBall>> _miniBallsCacheByTypeId =
            new Dictionary<string, List<DirectMiniBall>>();

        public List<DirectMiniBall> MiniBalls
        {
            get
            {
                if (_miniBallsCacheByTypeId.TryGetValue("" + this.PositionInSpace + this.Id,
                        out var res)) // TODO: create a on session change event and clear those there, also is there a better way to cache those?
                {
                    _miniBalls = res;
                }

                if (_miniBalls == null)
                {
                    var list = this.Ball["miniBalls"].ToList();
                    _miniBalls = new List<DirectMiniBall>();
                    foreach (var obj in list)
                    {
                        var mb = new DirectMiniBall(obj);
                        _miniBalls.Add(mb);
                    }

                    if (this.IsAbyssSphereEntity)
                    {
                        var mb = new DirectMiniBall(0, 0, 0, (float)this.RadiusOverride.Value, true);
                        _miniBalls.Add(mb);
                    }

                    _miniBallsCacheByTypeId["" + this.PositionInSpace + this.Id] = _miniBalls;
                }

                return _miniBalls;
            }
        }


        private List<DirectMiniBox> _miniBoxes;

        private static Dictionary<string, List<DirectMiniBox>> _miniBoxesCacheByTypeId =
            new Dictionary<string, List<DirectMiniBox>>();

        public List<DirectMiniBox> MiniBoxes
        {
            get
            {
                if (_miniBoxesCacheByTypeId.TryGetValue("" + this.PositionInSpace + this.Id, out var res))
                {
                    _miniBoxes = res;
                }

                if (_miniBoxes == null)
                {
                    var list = this.Ball["miniBoxes"].ToList();
                    _miniBoxes = new List<DirectMiniBox>();
                    foreach (var obj in list)
                    {
                        var mb = new DirectMiniBox(obj);
                        _miniBoxes.Add(mb);
                    }
                    _miniBoxesCacheByTypeId["" + this.PositionInSpace + this.Id] = _miniBoxes;
                }
                return _miniBoxes;
            }
        }


        private List<DirectMiniCapsule> _miniCapsules;

        private static Dictionary<string, List<DirectMiniCapsule>> _miniCapsulesCacheByTypeId =
            new Dictionary<string, List<DirectMiniCapsule>>();

        public List<DirectMiniCapsule> MiniCapsules
        {
            get
            {
                if (_miniCapsulesCacheByTypeId.TryGetValue("" + this.PositionInSpace + this.Id, out var res))
                {
                    _miniCapsules = res;
                }

                if (_miniCapsules == null)
                {
                    var list = this.Ball["miniCapsules"].ToList();
                    _miniCapsules = new List<DirectMiniCapsule>();
                    foreach (var obj in list)
                    {
                        var mb = new DirectMiniCapsule(obj);
                        _miniCapsules.Add(mb);
                    }
                    _miniCapsulesCacheByTypeId["" + this.PositionInSpace + this.Id] = _miniCapsules;
                }
                return _miniCapsules;
            }
        }

        public void DrawSphere(float? radius = null, bool forceRedraw = false)
        {
            try
            {
                if (Settings.Instance.Disable3D)
                    return;

                if (radius == null)
                        radius = (float)this.RadiusOverride;


                var name = this.Id.ToString();

                if (!forceRedraw)
                {
                    if (DirectEve.SceneManager.DefaultSceneObjectsDict.ContainsKey(name + "_CCPEndorsedRenderObject"))
                    {
                        return;
                    }
                }

                var x = 0f;
                var y = 0f;
                var z = 0f;

                SphereType sphereType;

                if (this.IsBioCloud)
                    sphereType = SphereType.Jumprangebubble; // correct size
                else if (this.IsTachCloud)
                    sphereType = SphereType.Scanbubblehitsphere; // correct size
                else if (this.IsFilaCloud)
                    sphereType = SphereType.Scanconesphere; // correct size
                else
                    sphereType = SphereType.Miniball;

                DirectEve.SceneManager.DrawSphere(radius.Value, name, (float)x, (float)y, (float)z, this.Ball, sphereType);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public void DrawSphereOverHere(Vec3 vec3, float? radius = 5000, SphereType sphereType = SphereType.Miniball)
        {
            try
            {
                if (Settings.Instance.Disable3D)
                    return;

                var name = this.Id.ToString();

                DirectEve.SceneManager.DrawSphere(radius.Value, name, (float)vec3.X, (float)vec3.Y, (float)vec3.Z, this.Ball, sphereType);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public void DrawBalls()
        {
            try
            {
            if (Settings.Instance.Disable3D)
                return;

            if (this.MiniBallAmount > 0)
            {
                var mbs = this.Ball["miniBalls"].ToList();
                int n = 0;
                foreach (var mb in mbs)
                {
                    var x = mb["x"].ToFloat();
                    var y = mb["y"].ToFloat();
                    var z = mb["z"].ToFloat();
                    var radius = mb["radius"].ToFloat();
                    var name = this.Id + "_" + n;
                    DirectEve.SceneManager.DrawSphere(radius, name, x, y, z, this.Ball);
                    n++;
                }
            }
        }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public void DisplayAllBallAxes()
        {
            if (Settings.Instance.Disable3D)
                return;

            if (this.MiniBallAmount > 0)
            {
                var mbs = this.Ball["miniBalls"].ToList();
                int n = 0;
                foreach (var mb in mbs)
                {
                    var x = mb["x"].ToFloat();
                    var y = mb["y"].ToFloat();
                    var z = mb["z"].ToFloat();
                    var radius = mb["radius"].ToFloat();
                    var name = this.Id + "_" + n;
                    var pos = new DirectWorldPosition(x + this.WorldPos.Value.X, y + this.WorldPos.Value.Y, z + this.WorldPos.Value.Z).GetVector();
                    DirectEve.SceneManager.DrawLine(pos, new DirectWorldPosition(pos.X + 15000, pos.Y, pos.Z).GetVector());
                    DirectEve.SceneManager.DrawLine(pos, new DirectWorldPosition(pos.X, pos.Y + 15000, pos.Z).GetVector());
                    DirectEve.SceneManager.DrawLine(pos, new DirectWorldPosition(pos.X, pos.Y, pos.Z + 15000).GetVector());
                    n++;
                }
            }
        }

        public void ShowBallEdges()
        {
            try
            {
                if (this.MiniBallAmount > 0)
                {
                    foreach (var mb in this.MiniBalls)
                    {
                        var pos = new DirectWorldPosition(mb.X + this.WorldPos.Value.X, mb.Y + this.WorldPos.Value.Y, mb.Z + this.WorldPos.Value.Z).GetVector();
                        for (int i = 0; i < mb.Radius + mb.Radius * 0.3; i = i + 100)
                        {
                            var pNew = new DirectWorldPosition(pos.X, pos.Y, pos.Z + i);
                            if (mb.IsPointWithin(pNew.GetVector(), this.WorldPos.Value))
                            {
                                DirectEve.SceneManager.DrawLine(pNew.GetVector(), new DirectWorldPosition(pNew.XCoordinate + 15000, pNew.YCoordinate, pNew.ZCoordinate).GetVector(), 0, 1, 0, 1); // green
                            }
                            else
                            {
                                DirectEve.SceneManager.DrawLine(pNew.GetVector(), new DirectWorldPosition(pNew.XCoordinate + 15000, pNew.YCoordinate, pNew.ZCoordinate).GetVector(), 1, 0, 0, 1); // yellow
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public void DrawBoxes()
        {
            try
            {
            if (this.MiniBoxesAmount > 0)
            {
                var boxes = this.MiniBoxes;
                foreach (var mb in boxes)
                {
                    DirectEve.SceneManager.DrawBox(mb, this.Ball);
                }
            }
        }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public void DrawCapsules()
        {
            try
            {
            if (Settings.Instance.Disable3D)
                return;

            if (this.MiniCapsulesAmount > 0)
            {
                foreach (var caps in this.MiniCapsules)
                {
                    DirectEve.SceneManager.DrawCapsule(caps, this.Ball);
                }
            }
        }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public void DrawBoxesWithLines()
        {
            if (Settings.Instance.Disable3D)
                return;

            if (this.MiniBoxesAmount > 0)
            {
                var boxes = this.MiniBoxes;

                foreach (var mb in boxes)
                {
                    if (this.WorldPos.HasValue)
                    {
                        var pos = this.DirectAbsolutePosition.GetVector() - DirectEve.ActiveShip.Entity.DirectAbsolutePosition.GetVector();
                        //var pos = this.WorldPos.Value;

                        DirectEve.SceneManager.DrawLine(mb.P8 + pos, mb.P4 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P8 + pos, mb.P7 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P7 + pos, mb.P3 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P3 + pos, mb.P4 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P4 + pos, mb.P2 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P2 + pos, mb.P6 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P6 + pos, mb.P5 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P5 + pos, mb.P1 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P1 + pos, mb.P2 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P6 + pos, mb.P8 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P7 + pos, mb.P5 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P1 + pos, mb.P3 + pos, 0, 1, 1, 1);

                        //    p6 +--------+ p2
                        //      /        /|
                        //     /        / |
                        // p5 +--------+p1|
                        //    |        |  |
                        //    |   p8   |  + p4
                        //    |        | /
                        //    |        |/
                        // p7 +--------+ p3
                    }
                }
            }
        }

        public void RemoveDrawnSphere()
        {
            try
            {
                var name = this.Id.ToString();
                DirectEve.SceneManager.RemoveDrawnObject(name);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private DirectEntity _abyssalCenter;
        private DirectEntity AbyssalCenter => _abyssalCenter ??= DirectEve.Entities.FirstOrDefault(e => e.TypeId == (int)TypeID.AbyssalCenter);


        private bool? _isInSpeedCloud = null;

        public bool IsInSpeedCloud
        {
            get
            {
                if (!_isInSpeedCloud.HasValue)
                {
                    if (!DirectEve.Entities.Any(e => e.IsTachCloud))
                        return false;

                    _isInSpeedCloud = AnyIntersectionAtThisPosition(DirectAbsolutePosition, false, true, true, true, true, true, false).Any(e => e.IsTachCloud);
                }
                return _isInSpeedCloud.Value;
            }
        }

        public List<DirectEntity> GetIntersectionEntities
        {
            get
            {
                return AnyIntersectionAtThisPosition(DirectAbsolutePosition);
            }
        }

        private static List<DirectEntity> _colliderEntities = null;
        public static List<DirectEntity> ColliderEntities
        {
            get
            {
                if (_colliderEntities == null || DirectEve.HasFrameChanged())
                {
                    _colliderEntities = ESCache.Instance.DirectEve.Entities.Where(e => e.Distance < 3_000_000 && (e.HasAnyNonTraversableColliders || e.IsAbyssSphereEntity)).ToList();
                }
                return _colliderEntities;
            }
        }

        public bool IsGoingTooFastForOurDronesToKill
        {
            get
            {
                if (Velocity > 3500)
                {
                    return true;
                }

                return false;
            }
        }

        //IsTrackingPylon || IsAutomataPylon || IsWideAreaAutomataPylon || IsFilaCould || IsBioCloud || IsTach;

        private (float, bool) Cost(DirectWorldPosition current, DirectWorldPosition next, DirectWorldPosition destination, bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false, List<DirectEntity> ingoredEntities = null)
        {
            // Calculate the distance from current to the next waypoint
            float distCurrentNext = (float)current.GetDistanceSquared(next).Value;

            //// TODO: Should we use a multiple of the distance if we encountered this waypoint before to discourage using the same path waypoint multiple times?
            if (next.Visits > 0)
                distCurrentNext *= next.Visits;

            // Use raycasting to check if there is a line of sight between the current and the next waypoint
            var colliders = DirectRayCasting.IsLineOfSightFree(current.GetVector(), next.GetVector(), ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, ignoredEntities: ingoredEntities);
            if (AbyssalCenter != null && AbyssalCenter.DirectAbsolutePosition.GetDistanceSquared(next) >= AbyssBoundarySizeSquared && AbyssalCenter.DirectAbsolutePosition.GetDistanceSquared(destination) <= AbyssBoundarySizeSquared)
                            {
                var factor = 10000.0f;
                var distNext = AbyssalCenter.DirectAbsolutePosition.GetDistanceSquared(next);
                var distCurrent = AbyssalCenter.DirectAbsolutePosition.GetDistanceSquared(current);

                if (distCurrentNext > distCurrent)
                {
                    factor *= 5;
                }

                distCurrentNext *= (float)factor;
                return (distCurrentNext, true);
            }

            if (!colliders.Item1)
            {
                double factor = 1000.0f;
                var cols = colliders.Item2;

                if (cols.Any(e => e.Value.Any(c => !c.Traversable)))
                {
                    return (float.MaxValue, false);
                }

                //Scale the distance by the number of colliders hit and by the distance to the center of the colliding unit
                if (cols.Count == 1)
                {
                    var colCenter = new DirectWorldPosition(cols.First().Value.First().Center);
                    if (colCenter.GetDistanceSquared(destination) > cols.First().Value.First().MaxBoundingRadiusSquared)
                    {
                        foreach (var ent in cols)
                        {
                            foreach (var col in ent.Value)
                            {
                                // if we are in a sphere, choose the shortest way out
                                var center = col.Center;
                                var radiusSq = col.Radius * col.Radius;
                                var distToCenter = (float)next.GetDistanceSquared(new DirectWorldPosition(center + ent.Key.DirectAbsolutePosition.GetVector())).Value;
                                //Console.WriteLine($"Distance to center: {distToCenter} radius {radius}");
                                factor = 800.0f + 200.0d * (radiusSq - distToCenter) / radiusSq;
                                distCurrentNext *= (float)factor;
                                break;
                            }
                        }
                        return (distCurrentNext, true);
                    }
                }
                                // Penalize overlapping colliders even harder
                if (cols.Count > 1)
                    factor *= cols.Count;

                distCurrentNext *= (float)factor;
            }

            return (distCurrentNext, true);
        }

        // To ensure admissibility (... ,triangle inequality) we only use the distance to the final destination as heuristic
        public float Heuristic(DirectWorldPosition current, DirectWorldPosition next, DirectWorldPosition destination)
        {
            // Calculate the distance from next waypoint to destination waypoint
            var d = (float)next.GetDistanceSquared(destination).Value;
            return d;
        }

        public static List<DirectEntity> AnyIntersectionAtThisPosition(DirectWorldPosition worldPos,
            bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false, IEnumerable<DirectEntity> excludeEnts = null)
        {
            try
            {
                var intersectingEntsRet = new List<DirectEntity>();
                var ret = DirectRayCasting.IsLineOfSightFree(worldPos.GetVector(), worldPos.GetVector(), ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, false, excludeEnts?.ToList() ?? new List<DirectEntity>());
                if (ret.Item1)
                {
                    return intersectingEntsRet;
                }
                else
                {
                    return ret.Item2.Keys.ToList();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return new List<DirectEntity>();
            }
        }


        private static DateTime _lastColliderSpheresDraw = DateTime.MinValue;

        public static long AbyssBoundarySize
        {
            get
            {
                try
                {
                    if (ESCache.Instance.DirectEve.Session.IsInDockableLocation)
                        return 70000;

                    if (!ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)
                        return 70000;

                    if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                    {
                        if (ESCache.Instance.Modules.Any(i => i._module.IsMicroWarpDrive))
                        {
                            if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any())
                            {
                                return 40000;
                            }

                            return 44000;
                        }

                        if (ESCache.Instance.Modules.Any(i => i._module.IsAfterburner))
                            return 62000;
                    }

                    return 70000;
                }
                catch (Exception)
                {
                    return 70000;
                }
            }
        }

        public static long AbyssBoundarySizeSquared = AbyssBoundarySize * AbyssBoundarySize;

        public List<DirectWorldPosition> CalculatePathTo(DirectWorldPosition destination, int stepSize = 5000,
            bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false, bool optimizedPath = false)
            {
                var dest = destination;
                var path = new List<DirectWorldPosition>();

                //var intereSectingEntsStart = AnyIntersectionAtThisPosition(start);
                var intersectingEntsDest = AnyIntersectionAtThisPosition(dest);
                //var intersectingEntsStartDest = intereSectingEntsStart.Concat(intersectingEntsDest).Distinct().ToList();

                //if (intereSectingEntsStart.Any())


                var ignoredEnts = intersectingEntsDest;

                if (intersectingEntsDest.Any())
                {
                    DirectEve.Log($"INFO: Destination position is intersecting with [{intersectingEntsDest.Count()}] entities. Entities [{String.Join(", ", intersectingEntsDest.Select(e => e.TypeName))}]");
                }

                var isLineOfSightFreeDestination = DirectRayCasting.IsLineOfSightFree(this.DirectAbsolutePosition.GetVector(), dest.GetVector(), ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, ignoredEntities: ignoredEnts);

                if (isLineOfSightFreeDestination.Item1)
                {
                    //Console.WriteLine($"Direct Path found, returning direct path.");
                    destination.DirectPathFlag = true;
                    this.DirectAbsolutePosition.DirectPathFlag = true;
                    path.Add(this.DirectAbsolutePosition);
                    path.Add(destination);
                    return path;
                }

                if (destination == null || destination == this.DirectAbsolutePosition || DirectEve.Session.IsInDockableLocation)
                {
                    return new List<DirectWorldPosition>() { this.DirectAbsolutePosition };
                }

                var start = this.DirectAbsolutePosition;
                var cameFrom = new Dictionary<DirectWorldPosition, DirectWorldPosition>();
                var costSoFar = new Dictionary<DirectWorldPosition, float>();
                var frontier = new SimplePriorityQueue<DirectWorldPosition>();

                // round start and dest to whole integers
                start = new DirectWorldPosition((long)start.XCoordinate, (long)start.YCoordinate, (long)start.ZCoordinate);
                dest = new DirectWorldPosition((long)dest.XCoordinate, (long)dest.YCoordinate, (long)dest.ZCoordinate);
                destination = dest;

                frontier.Enqueue(start, 0);
                cameFrom[start] = start;
                costSoFar[start] = 0;
                var amount = 0;

                HashSet<DirectWorldPosition> neighbours = new HashSet<DirectWorldPosition>();
                int duplicateNeighbours = 0;

                while (frontier.Count > 0)
                {
                    var current = frontier.Dequeue();
                    current.Visits++;
                    amount++;
                    var dist = current.GetDistanceSquared(destination);

                    if (amount > 3000)
                    {
                        AStarErrors++;
                        DirectEve.Log($"WARNING: Hit limit. Dist [{dist}] AStarError [{AStarErrors}]");
                        break;
                    }

                    if (dist <= Math.Pow((stepSize + 50), 2))
                    {
                        destination = current;
                        break;
                    }

                    foreach (var next in current.GenerateNeighbours(stepSize, destination))
                    {
                        var n = next;
                        if (neighbours.TryGetValue(next, out var nx))
                        {
                            n = nx;
                            duplicateNeighbours++;
                        }
                        else
                        {
                            neighbours.Add(next);
                        }

                        var cost = Cost(current, next, destination, ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, ignoredEnts);
                        var newCost = costSoFar[current] + 1 + cost.Item1;
                        if (cost.Item2 && (!costSoFar.ContainsKey(next) || newCost < costSoFar[next]))
                        {
                            var heur = Heuristic(current, next, destination);
                            var priority = newCost + heur;
                            costSoFar[next] = newCost;
                            frontier.Enqueue(next, priority);
                            cameFrom[next] = current;
                        }
                    }
                }


                var e = destination;
                while (e != start)
                {
                    path.Add(e);
                    if (!cameFrom.ContainsKey(e))
                    {
                        path.Clear();
                        break;
                    }

                    e = cameFrom[e];
                }

                if (path.Contains(destination))
                    path.Add(start);

                path.Reverse();

                if (path.Any())
                    path.Add(dest);


                if (optimizedPath && path.Any())
                {
                    var optimizedPathStartIndex = 0;
                    var optimizedPathList = new List<DirectWorldPosition>();
                    optimizedPathList.Add(path.First());
                    var isFreeCnt = 0;
                    for (int i = 1; i < path.Count - 1; i++)
                    {
                        var current = path[optimizedPathStartIndex];
                        var target = path[i + 1];
                        // Check to see if we can skip the current index in navigation
                        var isFree = DirectRayCasting.IsLineOfSightFree(current.GetVector(), target.GetVector(), ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, ignoredEntities: ignoredEnts);
                        // Not able to navigate, take until this index
                        if (!isFree.Item1)
                        {
                            optimizedPathList.Add(path[i]);
                            optimizedPathStartIndex = i;
                            i++;
                            isFreeCnt++;
                        }
                    }

                    if (optimizedPathList.Last() != path.Last())
                    {
                        optimizedPathList.Add(path.Last());
                    }

                    if (isFreeCnt == 0)
                    {
                        optimizedPathList.All(p => p.DirectPathFlag = true);
                    }

                    return optimizedPathList;
                }

            return path;
        }

        public double EstimatedPixelDiameterWithChildren
        {
            get
            {
                _estimatedPixelDiameterWithChildren ??=
                    (double)_ball.Attribute("model").Attribute("estimatedPixelDiameterWithChildren");

                return _estimatedPixelDiameterWithChildren.Value;
            }
        }

        public static IEnumerable<EntityCache> _listOfEntitiesThatDontMove = null;

        public static IEnumerable<EntityCache> ListOfEntitiesThatDontMove
        {
            get
            {
                if (_listOfEntitiesThatDontMove != null)
                {
                    return _listOfEntitiesThatDontMove;
                }

                _listOfEntitiesThatDontMove = ESCache.Instance.EntitiesNotSelf.Where(e => e.CategoryId != (int)CategoryID.Charge && e.Velocity == 0 && e.Distance < 40000 && (e._directEntity.HasAnyNonTraversableColliders || e._directEntity.IsEntityWeWantToAvoidInAbyssals));
                return _listOfEntitiesThatDontMove ?? new List<EntityCache>();
            }
        }
        public double GotoX
        {
            get
            {
                if (!_gotoX.HasValue)
                    _gotoX = (double)ball.Attribute("gotoX");

                return _gotoX.Value;
            }
        }

        private double? _maxVelocity;

        public double MaxVelocity
        {
            get
            {
                if (!_maxVelocity.HasValue)
                    _maxVelocity = (double)_ball.Attribute("maxVelocity");

                return _maxVelocity.Value;
            }
        }

        private double? _speedFraction;
        public double SpeedFraction
        {
            get
            {
                if (!_speedFraction.HasValue)
                    _speedFraction = (double)_ball.Attribute("speedFraction");

                return _speedFraction.Value;
            }
        }

        public double GotoY
        {
            get
            {
                if (!_gotoY.HasValue)
                    _gotoY = (double)ball.Attribute("gotoY");

                return _gotoY.Value;
            }
        }


        public double GotoZ
        {
            get
            {
                if (!_gotoZ.HasValue)
                    _gotoZ = (double)ball.Attribute("gotoZ");

                return _gotoZ.Value;
            }
        }

        public int? AllianceId
        {
            get
            {
                if (!_allianceId.HasValue)
                    _allianceId = (int)Ball.Attribute("allianceID");

                return _allianceId.Value;
            }
        }

        public string AllianceTicker => DirectEve.GetOwner(AllianceId ?? -1).ShortName;

        public double AngularVelocity
        {
            get
            {
                if (IsValid)
                {
                    if (_angularVelocity == null)
                        _angularVelocity = TransversalVelocity / Math.Max(1, Distance);

                    return _angularVelocity.Value;
                }

                return 0;
            }
        }

        public double ArmorPct
        {
            get
            {
                if (IsValid)
                {
                    if (_armorPct != null)
                        return (double)_armorPct;

                    if (!_armorPct.HasValue)
                        GetDamageState();

                    return _armorPct ?? 0;
                }

                return 0;
            }
        }

        public void DrawLineTo(DirectEntity to)
        {
            Vec3 start = new Vec3((float)this.BallPos.X, (float)this.BallPos.Y, (float)this.BallPos.Z);
            Vec3 end = new Vec3((float)to.BallPos.X, (float)to.BallPos.Y, (float)to.BallPos.Z);
            DirectEve.SceneManager.DrawLine(start, end);
        }

        public List<string> Attacks { get; private set; }
        public PyObject Ball => ball;

        public List<DamageType> BestDamageTypes
        {
            get
            {
                if (IsValid)
                {
                    if (_bestDamageTypes == null)
                    {
                        try
                        {
                            _bestDamageTypes = new List<DamageType>();
                            ulong emEhp = double.IsInfinity(EmEHP.Value) || double.IsNaN(EmEHP.Value) ? ulong.MaxValue : (ulong)EmEHP;
                            ulong expEhp = double.IsInfinity(ExpEHP.Value) || double.IsNaN(ExpEHP.Value) ? ulong.MaxValue : (ulong)ExpEHP;
                            ulong kinEhp = double.IsInfinity(KinEHP.Value) || double.IsNaN(KinEHP.Value) ? ulong.MaxValue : (ulong)KinEHP;
                            ulong trmEhp = double.IsInfinity(TrmEHP.Value) || double.IsNaN(TrmEHP.Value) ? ulong.MaxValue : (ulong)TrmEHP;

                            Dictionary<DamageType, ulong> dict = new Dictionary<DamageType, ulong>
                            {
                                { DamageType.EM, emEhp },
                                { DamageType.Explosive, expEhp },
                                { DamageType.Kinetic, kinEhp },
                                { DamageType.Thermal, trmEhp }
                            };
                            //_bestDamageType = dict.FirstOrDefault(e => e.Value == Math.Min(Math.Min(Math.Min(emEHP, expEHP), kinEHP), trmEHP)).Key;
                            _bestDamageTypes = dict.OrderBy(e => e.Value).Select(e => e.Key).ToList();
                        }
                        catch (Exception ex)
                        {
                            Logging.Log.WriteLine("Exception [" + ex + "] Entity [" + Name + "] Defaulting to EM as bestdamagetype (wrong?)");
                            Dictionary<DamageType, ulong> dict = new Dictionary<DamageType, ulong>
                            {
                                { DamageType.EM, 1000 }
                            };

                            _bestDamageTypes = dict.OrderBy(e => e.Value).Select(e => e.Key).ToList();
                        }
                    }

                    return _bestDamageTypes;
                }

                return new List<DamageType>();
            }
        }

        public int CharId
        {
            get
            {
                if (IsNpc) return 0;

                if (!_charId.HasValue)
                    _charId = (int)slimItem.Attribute("charID");

                return _charId.Value;
            }
        }

        /// <summary>
        /// Tries to calculate the entity which this entity is warping to
        /// </summary>
        public DirectEntity WarpDestinationEntity
        {
            get
            {
                if (!this.IsWarpingByMode)
                {
                    return null;
                }

                if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                {
                    Log.WriteLine("WarpDestinationEntity: if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)");
                    return null;
                }

                var dist = double.MaxValue;
                var entities = DirectEve.Entities.Where(e =>
                    e.GroupId == (int)Group.Stargate || e.GroupId == (int)Group.Station || e.GroupId == (int)Group.Moon ||
                    e.GroupId == (int)Group.Planet);

                DirectEntity result = null;
                foreach (var e in entities)
                {

                    //var d = Math.Round(Math.Sqrt((GotoX - e.X) * (GotoX - e.X) + (GotoY - e.Y) * (GotoY - e.Y) + (GotoZ - e.Z) * (GotoZ - e.Z)), 2);
                    var x = this.GotoX - e.XCoordinate;
                    var y = this.GotoY - e.YCoordinate;
                    var z = this.GotoZ - e.ZCoordinate;

                    var d = Math.Sqrt((double)x * (double)x + (double)y * (double)y + (double)z * (double)z);
                    if (d < dist)
                    {
                        dist = d;
                        result = e;
                    }
                }
                return result;
            }
        }

        public int CorpId
        {
            get
            {
                if (IsNpc) return 0;

                if (!_corpId.HasValue)
                    _corpId = (int)slimItem.Attribute("corpID");

                return _corpId.Value;
            }
        }

        public string CorpTicker => DirectEve.GetOwner(CorpId).ShortName;

        public double? CurrentArmor
        {
            get
            {
                if (IsValid)
                {
                    if (!_currentrmor.HasValue)
                        _currentrmor = MaxArmor * ArmorPct;
                    return _currentrmor ?? 0;
                }

                return 0;
            }
        }

        public double? CurrentShield
        {
            get
            {
                if (IsValid)
                {
                    if (!_currentShield.HasValue)
                        _currentShield = MaxShield * ShieldPct;
                    return _currentShield ?? 0;
                }

                return 0;
            }
        }

        public double? CurrentStructure
        {
            get
            {
                if (IsValid)
                {
                    if (!_currentStructure.HasValue)
                        _currentStructure = MaxStructure * StructurePct;
                    return _currentStructure ?? 0;
                }

                return 0;
            }
        }

        public double Distance
        {
            get
            {
                if (!_distance.HasValue)
                {
                    if (!IsValid)
                    {
                        if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                        {
                            Log.WriteLine("Distance: if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)");
                            return -1;
                        }

                        if (DirectEve.ActiveShip.Entity.XCoordinate == null || DirectEve.ActiveShip.Entity.YCoordinate == null || DirectEve.ActiveShip.Entity.ZCoordinate == null)
                        {
                            Log.WriteLine("Distance: if (DirectEve.ActiveShip.Entity.XCoordinate == null || DirectEve.ActiveShip.Entity.YCoordinate == null || DirectEve.ActiveShip.Entity.ZCoordinate == null");
                            return -1;
                        }

                        if (DirectEve.ActiveShip != null)
                        {
                            double deltaX = (double)XCoordinate - (double)DirectEve.ActiveShip.Entity.XCoordinate;
                            double deltaY = (double)YCoordinate - (double)DirectEve.ActiveShip.Entity.YCoordinate;
                            double deltaZ = (double)ZCoordinate - (double)DirectEve.ActiveShip.Entity.ZCoordinate;

                            _distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ));
                            return (double)_distance;
                        }

                        //Logging.Log.WriteLine("DirectEntity [" + Id + "] if (!IsValid) Distance will be 0");
                        return 0;
                    }

                    _distance = (double)Ball.Attribute("surfaceDist");
                }

                return _distance.Value;
            }
        }

        public double DistanceInAU
        {
            get
            {
                return Math.Round(Distance / (double)Distances.OneAu, 2);
            }
        }

        public double DistanceTo(DirectEntity ent)
        {
            return ent.DirectAbsolutePosition.GetDistance(this.DirectAbsolutePosition) - (double)BallRadius -
                   (double)ent.BallRadius;
        }

        public List<string> ElectronicWarfare { get; }

        public double? EmEHP => _emEHP ??= GetEmEHP();

        private double? GetEmEHP()
        {
            var shield = (1 / (1 - ShieldResistanceEM)) * CurrentShield ?? 0;
            var armor = (1 / (1 - ArmorResistanceEM)) * CurrentArmor ?? 0;
            var struc = (1 / (1 - StructureResistanceEM)) * CurrentStructure ?? 0;

            if (double.IsNaN(shield))
                shield = 0;

            if (double.IsNaN(armor))
                armor = 0;

            if (double.IsNaN(struc))
                struc = 0;

            return shield + armor + struc;
        }

        public double? ExpEHP => _expEHP ??= GetExpEHP();

        private double? GetExpEHP()
        {
            var shield = (1 / (1 - ShieldResistanceExplosive)) * CurrentShield ?? 0;
            var armor = (1 / (1 - ArmorResistanceExplosive)) * CurrentArmor ?? 0;
            var struc = (1 / (1 - StructureResistanceExplosion)) * CurrentStructure ?? 0;

            if (double.IsNaN(shield))
                shield = 0;

            if (double.IsNaN(armor))
                armor = 0;

            if (double.IsNaN(struc))
                struc = 0;

            return shield + armor + struc;
        }

        public double? TrmEHP => _trmEHP ??= GetTrmEHP();

        private double? GetTrmEHP()
        {
            var shield = (1 / (1 - ShieldResistanceThermal)) * CurrentShield ?? 0;
            var armor = (1 / (1 - ArmorResistanceThermal)) * CurrentArmor ?? 0;
            var struc = (1 / (1 - StructureResistanceThermal)) * CurrentStructure ?? 0;

            if (double.IsNaN(shield))
                shield = 0;

            if (double.IsNaN(armor))
                armor = 0;

            if (double.IsNaN(struc))
                struc = 0;

            return shield + armor + struc;
        }

         private double? GetKinEHP()
        {
            var shield = (1 / (1 - ShieldResistanceKinetic)) * CurrentShield ?? 0;
            var armor = (1 / (1 - ArmorResistanceKinetic)) * CurrentArmor ?? 0;
            var struc = (1 / (1 - StructureResistanceKinetic)) * CurrentStructure ?? 0;

            if (double.IsNaN(shield))
                shield = 0;

            if (double.IsNaN(armor))
                armor = 0;

            if (double.IsNaN(struc))
                struc = 0;

            return shield + armor + struc;
        }

        public double? KinEHP => _kinEHP ??= GetKinEHP();

        public bool IsBoarded => IsPlayer;

        private static Dictionary<long, (long, DateTime)> _followIdCacheDrones =
            new Dictionary<long, (long, DateTime)>();

        public long FollowId
        {
            get
            {
                if (_followIdCacheDrones.TryGetValue(this.Id, out var f) && f.Item2.AddSeconds(3) >= DateTime.UtcNow)
                {
                    return f.Item1;
                }

                if (!_followId.HasValue)
                    _followId = (long)_ball.Attribute("followId");

                return _followId.Value;
            }
        }

        private DirectEntity _followEntity;

        public DirectEntity FollowEntity
        {
            get
            {
                if (_followEntity != null)
                    return _followEntity;

                if (DirectEve.EntitiesById.TryGetValue(FollowId, out var ent))
                {
                    _followEntity = ent;
                }

                return _followEntity;
            }
        }

        public string FollowEntityName
        {
            get
            {
                if (FollowEntity != null)
                    return FollowEntity.TypeName;

                return "None";
            }
        }

        public string FollowEntityDistanceNearest1k
        {
            get
            {
                if (FollowEntity != null)
                    return Math.Round(FollowEntity.DistanceTo(this) / 1000, 0).ToString();

                return "0";
            }
        }

        /// <summary>
        /// Ensure you have access!
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetFleetHangarContainer()
        {
            var emptyCont = new DirectContainer(DirectEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            // check if it's a ship
            if (this.CategoryId != (int)CategoryID.Ship)
            {
                DirectEve.Log("Target is not a ship?");
                return emptyCont;
            }

            if (Distance > 2500)
            {
                DirectEve.Log($"Entity is too far away to access the fleet hangar container. Dist [{Distance}] Max Dist is [2500]");
                return emptyCont;
            }

            if (!DirectEve.Session.ShipId.HasValue)
                return emptyCont;


            // check if the target has a fleet hangar
            var godma = DirectEve.GetLocalSvc("godma");
            var hasFleetHangar = godma.Call("GetType", this.TypeId)["hasFleetHangars"].ToBool();
            if (!hasFleetHangar)
            {
                DirectEve.Log("Target ship has no fleet hangar.");
                return emptyCont;
            }

            var ofh = DirectEve.PySharp.Import("eve.client.script.ui.services.menuSvcExtras.openFunctions")["OpenFleetHangar"];

            if (!ofh.IsValid)
                DirectEve.Log($"Error: eve.client.script.ui.services.menuSvcExtras.openFunctions.OpenFleetHangar is not valid.");

            if (this.Id <= 0)
            {
                DirectEve.Log("Error: Id is <= 0.");
                return emptyCont;
            }

            DirectEve.ThreadedCall(ofh, this.Id);

            var inventory = DirectContainer.GetInventory(DirectEve, "GetInventoryFromId", this.Id);
            return new DirectContainer(DirectEve, inventory, DirectEve.Const.FlagFleetHangar, this.Id);
        }

        /// <summary>
        /// Ensure you have access!
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipMaintenaceBayContainer()
        {
            var emptyCont = new DirectContainer(DirectEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            // check if it's a ship
            if (this.CategoryId != (int)CategoryID.Ship)
            {
                DirectEve.Log("Target is not a ship?");
                return emptyCont;
            }

            if (Distance > 2500)
            {
                DirectEve.Log($"Entity is too far away to access the ship maintenance container. Dist [{Distance}] Max Dist is [2500]");
                return emptyCont;
            }

            if (!DirectEve.Session.ShipId.HasValue)
                return emptyCont;

            // check if the target has a fleet hangar
            var godma = DirectEve.GetLocalSvc("godma");
            var hasSMA = godma.Call("GetType", this.TypeId)["hasShipMaintenanceBay"].ToBool();
            if (!hasSMA)
            {
                DirectEve.Log("Target ship has no ship maint bay.");
                return emptyCont;
            }

            var ofs = DirectEve.PySharp.Import("eve.client.script.ui.services.menuSvcExtras.openFunctions")["OpenShipMaintenanceBayShip"];

            if (!ofs.IsValid)
                DirectEve.Log($"Error: eve.client.script.ui.services.menuSvcExtras.openFunctions.OpenShipMaintenanceBayShip is not valid.");

            if (this.Id <= 0)
            {
                DirectEve.Log("Error: Id is <= 0.");
                return emptyCont;
            }

            DirectEve.ThreadedCall(ofs, this.Id, "");
            var inventory = DirectContainer.GetInventory(DirectEve, "GetInventoryFromId", this.Id);
            return new DirectContainer(DirectEve, inventory, DirectEve.Const.FlagShipHangar, this.Id);
        }


        /// <summary>
        /// This opens a modal
        /// Use ONLY on targets which are in same fleet AND/OR in same corp and ensure that the rights are set correctly, we do not check for that here
        /// </summary>
        public void StoreCurrentShipInShipMaintenanceBay()
        {

            // TODO: CHECK IF TARGET IS IN FLEET OR CORP

            // check if it's a ship
            if (this.CategoryId != (int)CategoryID.Ship)
            {
                DirectEve.Log("Target is not a ship?");
                return;
            }

            // check if the target has a SMB
            var godma = DirectEve.GetLocalSvc("godma");
            var hasSMA = godma.Call("GetType", this.TypeId)["hasShipMaintenanceBay"].ToBool();
            if (!hasSMA)
            {
                DirectEve.Log("Target ship has no maintenance bay.");
                return;
            }

            if (DirectEve.ActiveShip.GroupId == (int)Group.Capsule)
            {
                DirectEve.Log("You can't store a capsule in a SMB.");
                return;
            }

            if (Distance > 2500)
            {
                DirectEve.Log($"Entity is too far away to store the ship. Dist [{Distance}] Max Dist is [2500]");
                return;
            }

            var menuSvc = DirectEve.GetLocalSvc("menu");

            if (!menuSvc.IsValid)
            {
                DirectEve.Log("Menu svc ref is not valid.");
                return;
            }

            var currentShipId = DirectEve.Session.ShipId;

            if (currentShipId <= 0)
            {
                DirectEve.Log("Current ship id is <= 0");
                return;
            }

            if (this.Id <= 0)
            {
                DirectEve.Log("Dest ship id is <= 0");
            }

            DirectEve.ThreadedCall(menuSvc["StoreVessel"], this.Id, currentShipId);
        }


        public bool EngageTargetWithDrones(List<long> droneIds)
        {
            if (!IsTarget)
                return false;

            var activeDrones = DirectEve.ActiveDrones.ToList();
            foreach (var id in droneIds)
            {
                if (activeDrones.All(e => e.Id != id))
                    return false;
            }

            foreach (var id in droneIds)
            {
                _followIdCacheDrones[id] = (this.Id, DateTime.UtcNow);
            }

            if (!DirectEve.Interval(900, 1500, Id.ToString()))
                return false;

            if (!IsActiveTarget)
                MakeActiveTarget(false);

            var ret = DirectEve.ThreadedLocalSvcCall("menu", "EngageTarget", droneIds);
            return ret;
        }

        public bool EngageTargetWithDrones(IEnumerable<DirectEntity> drones)
        {
            var ids = drones.Select(e => e.Id).ToList();
            if (ids.Any())
                return EngageTargetWithDrones(ids);
            return false;
        }

        //fixme - hard coded == bad
        public double? MaxArmor_HardCoded
        {
            get
            {
                if (!RecursionCheck(nameof(MaxArmor)))
                    return -1;

                if (IsNpc)
                {
                    if (DirectEve.Session.IsAbyssalDeadspace || GroupId == (int)Group.InvadingPrecursorEntities)
                    {
                        //
                        // http://games.chruker.dk/eve_online/inventory.php?group_id=1982
                        //
                        if (TypeId == (int)TypeID.BlindingLeshak || TypeId == (int)TypeID.RenewingLeshak || TypeId == (int)TypeID.StarvingLeshak || TypeId == (int)TypeID.StrikingLeshak)
                            return 13000;

                        if (IsNPCBattleship) //Drone BSs, etc
                            return 10000;

                        if (IsNPCBattlecruiser)
                            return 1300;

                        if (TypeId == (int)TypeID.HarrowingVedmak || TypeId == (int)TypeID.StarvingVedmak) //(Name.Contains("Vedmak"))
                            return 7000;

                        if (Name.Contains("Rodiva"))
                            return 7000;

                        if (IsNPCDestroyer)
                            return 2900;

                        if (IsNPCCruiser)
                            return 1500;

                        if (IsNPCFrigate)
                            return 2200;

                        return 10000;
                    }

                    if (DirectEve.Session.IsWspace)
                    {
                        if (IsNpcCapitalEscalation)
                            return 100000;

                        //return 10000;
                    }

                    if (IsNPCCapitalShip)
                        return 120000;

                    if (IsNPCBattleship) // http://games.chruker.dk/eve_online/inventory.php?group_id=552
                        return 4500;

                    if (IsNPCBattlecruiser) //http://games.chruker.dk/eve_online/inventory.php?group_id=793
                        return 1200;

                    if (IsNPCCruiser) // http://games.chruker.dk/eve_online/inventory.php?group_id=551
                        return 1500;

                    if (IsNPCFrigate)
                        return 150;
                }

                return 10000;
            }
        }

        public double? MaxShield_HardCoded
        {
            get
            {
                if (!RecursionCheck(nameof(MaxShield)))
                    return -1;
                //if (!_maxShield.HasValue)
                //    _maxShield = (float)Ball.Attribute("shieldCapacity");
                //return _maxShield;

                if (IsNpc)
                {
                    if (DirectEve.Session.IsAbyssalDeadspace || GroupId == (int)Group.InvadingPrecursorEntities)
                    {
                        if (TypeId == (int)TypeID.BlindingLeshak || TypeId == (int)TypeID.RenewingLeshak || TypeId == (int)TypeID.StarvingLeshak || TypeId == (int)TypeID.StrikingLeshak) //(Name.Contains("Leshak"))
                            return 5000;

                        if (IsNPCBattleship)
                            return 10000;

                        if (IsNPCBattlecruiser) //http://games.chruker.dk/eve_online/inventory.php?group_id=793
                            return 1300;

                        if (TypeId == (int)TypeID.HarrowingVedmak || TypeId == (int)TypeID.StarvingVedmak) //(Name.Contains("Vedmak"))
                            return 2000;

                        if (Name.Contains("Rodiva"))
                            return 2000;

                        if (IsNPCCruiser) // http://games.chruker.dk/eve_online/inventory.php?group_id=551
                            return 1000;

                        if (IsNPCDestroyer)
                            return 1100;

                        if (IsNPCFrigate)
                            return 500;

                        return 10000;
                    }

                    if (DirectEve.Session.IsWspace)
                    {
                        if (IsNpcCapitalEscalation)
                            return 100000;

                        return 10000;
                    }

                    if (IsNPCCapitalShip) // http://games.chruker.dk/eve_online/inventory.php?group_id=1681
                        return 140000;

                    if (IsNPCBattleship) // http://games.chruker.dk/eve_online/inventory.php?group_id=552
                        return 5000;

                    if (IsNPCBattlecruiser) //http://games.chruker.dk/eve_online/inventory.php?group_id=793
                        return 1700;

                    if (IsNPCCruiser) // http://games.chruker.dk/eve_online/inventory.php?group_id=551
                        return 2000;

                    if (IsNPCFrigate)
                        return 200;
                }

                return 10000;
            }
        }

        public double? MaxStructure_HardCoded
        {
            get
            {
                if (!RecursionCheck(nameof(MaxStructure)))
                    return -1;

                //if (!_maxStructure.HasValue)
                //    _maxStructure = (float)Ball.Attribute("hp");
                //return _maxStructure;

                if (IsNpc)
                {
                    if (DirectEve.Session.IsAbyssalDeadspace || GroupId == (int)Group.InvadingPrecursorEntities)
                    {
                        if (IsNPCBattleship)
                            return 10000;

                        if (IsNPCBattlecruiser) //http://games.chruker.dk/eve_online/inventory.php?group_id=793
                            return 1300;

                        if (Name.Contains("Vedmak"))
                            return 3000;

                        if (Name.Contains("Rodiva"))
                            return 3000;

                        if (IsNPCCruiser) // http://games.chruker.dk/eve_online/inventory.php?group_id=551
                            return 1000;

                        if (IsNPCDestroyer)
                            return 1300;

                        if (IsNPCFrigate)
                            return 650;

                        return 10000;
                    }

                    if (DirectEve.Session.IsWspace)
                    {
                        if (IsNpcCapitalEscalation)
                            return 100000;

                        return 10000;
                    }

                    if (IsNPCCapitalShip) // http://games.chruker.dk/eve_online/inventory.php?group_id=1681
                        return 100000;

                    if (IsNPCBattleship) // http://games.chruker.dk/eve_online/inventory.php?group_id=552
                        return 5000;

                    if (IsNPCBattlecruiser) //http://games.chruker.dk/eve_online/inventory.php?group_id=793
                        return 1300;

                    if (IsNPCCruiser) // http://games.chruker.dk/eve_online/inventory.php?group_id=551
                        return 1000;

                    if (IsNPCFrigate)
                        return 150;
                }

                return 1000;
            }
        }

        public double ArmorEffectiveHitpointsViaEM
        {
            get
            {
                return 0; // ArmorResistanceEm * armor
                //return ArmorResistanceEM * ArmorMaxHitPoints;
            }
        }

        public double ArmorEffectiveHitpointsViaExplosive
        {
            get
            {
                return 0; // ArmorResistanceExplosive * ArmorMaxHitPoints;
            }
        }

        public double ArmorEffectiveHitpointsViaKinetic
        {
            get
            {
                return 0; //ArmorResistanceKinetic * ArmorMaxHitPoints;
            }
        }

        public double ArmorEffectiveHitpointsViaThermal
        {
            get
            {
                return 0; //ArmorResistanceThermal * ArmorMaxHitPoints;
            }
        }

        public bool EntitiesMissilesAreInEffectiveRangeOfMe
        {
            get
            {
                try
                {
                    if (IsValid)
                    {
                        if (!IsNpc) return false;

                        if ((NpcMissileEntityFlightTimeMultiplier * NpcMissileEntityFlightTime) + (NpcMissileEntityVelocityMultiplier * NpcMissileEntityVelocity) > Distance)
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    //Log.WriteLine("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        public bool EntitiesTurretsAreInEffectiveRangeOfMe
        {
            get
            {
                if (IsValid)
                {
                    if (!IsNpc) return false;

                    if (OptimalRange + Falloff >= Distance)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public double? EntityArmorRepairAmount
        {
            get
            {
                if (IsValid)
                {
                    if (!IsNpc) return 0;

                    if (_entityArmorRepairAmount == null)
                        _entityArmorRepairAmount = (int)Ball.Attribute("entityArmorRepairAmount");

                    return _entityArmorRepairAmount.Value;
                }

                return 0;
            }
        }

        public double? EntityArmorRepairDelayChanceLarge
        {
            get
            {
                if (IsValid)
                {
                    if (!IsNpc) return 0;

                    if (_entityArmorRepairAmount == null)
                        _entityArmorRepairAmount = (int)Ball.Attribute("entityArmorRepairAmount");

                    if (!IsNpc) return 0;

                    if (_entityArmorDelayChanceLarge == null)
                        _entityArmorDelayChanceLarge = (int)Ball.Attribute("entityArmorRepairDelayChanceLarge");
                    return _entityArmorDelayChanceLarge.Value;
                }

                return 0;
            }
        }

        public double? EntityArmorRepairDuration
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_entityArmorRepairDuration == null)
                    _entityArmorRepairDuration = (int)Ball.Attribute("entityArmorRepairDuration");
                return _entityArmorRepairDuration.Value;
            }
        }

        public double? EntityAttackRange
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_entityAttackRange == null)
                    _entityAttackRange = (int)Ball.Attribute("entityAttackRange");

                return _entityAttackRange.Value;
            }
        }

        //
        // ToDo: fix me to calc damage types (kinetic, therm, em, exp) and thus dps of each type, etc.
        // for example for Zor: http://games.chruker.dk/eve_online/item.php?type_id=12256&debug=1
        //
        public int? EntityMissileTypeId
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_entityMissileTypeId == null)
                    _entityMissileTypeId = (int)Ball.Attribute("entityMissileTypeID");
                return _entityMissileTypeId.Value;
            }
        }

        public double? EntityShieldBoostAmount
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_entityShieldBoostAmount == null)
                    _entityShieldBoostAmount = (float)Ball.Attribute("entityShieldBoostAmount");

                return _entityShieldBoostAmount.Value;
            }
        }

        public double? EntityShieldBoostDelayChanceLarge
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_entityShieldBoostDelayChance == null)
                    _entityShieldBoostDelayChance = (int)Ball.Attribute("entityShieldBoostDelayChanceLarge");

                return _entityShieldBoostDelayChance.Value;
            }
        }

        public double? EntityShieldBoostDuration
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_entityShieldBoostDuration == null)
                    _entityShieldBoostDuration = (int)Ball.Attribute("entityShieldBoostDuration");

                return _entityShieldBoostDuration.Value;
            }
        }

        public double? EntityShieldRechargeRate
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_entityShieldRechargeRate == null)
                    _entityShieldRechargeRate = (int)Ball.Attribute("entityShieldRechargeRate");

                return _entityShieldRechargeRate.Value;
            }
        }

        public double? EntitySignatureRadius
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_signatureRadius == null)
                    _signatureRadius = (float)Ball.Attribute("signatureRadius");

                if (_signatureRadius != null && !double.IsNaN(_signatureRadius.Value) && !double.IsInfinity(_signatureRadius.Value))
                    return _signatureRadius.Value;

                return 0;
            }
        }

        public double? ExplosiveRawTurretDamage
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_explosiveDamage == null)
                {
                    _explosiveDamage = (float)Ball.Attribute("explosiveDamage");
                    if (_explosiveDamage.HasValue)
                        return _explosiveDamage.Value;

                    return 0;
                }

                return _explosiveDamage.Value;
            }
        }

        private int? _factionId = null;

        public int FactionId
        {
            get
            {
                if (!_factionId.HasValue)
                    _factionId = (int)slimItem.Attribute("factionID");

                return _factionId.Value;
            }
        }

        private Faction _faction = null;

        public Faction Faction
        {
            get
            {
                if (_faction != null)
                    return _faction ?? DirectNpcInfo.DefaultFaction;

                if (FactionId != -1)
                {
                    DirectNpcInfo.FactionIdsToFactions.TryGetValue(FactionId.ToString(), out _faction);
                    return _faction ?? DirectNpcInfo.DefaultFaction;
                }

                return DirectNpcInfo.DefaultFaction;
            }
        }

        public string GivenName
        {
            get
            {
                if (_givenName == null)
                    _givenName = DirectEve.GetLocationName(Id);

                return _givenName;
            }
        }

        public bool HasExploded
        {
            get
            {
                if (!_hasExploded.HasValue)
                    _hasExploded = Ball.Attribute("exploded").ToBool();

                return _hasExploded.Value;
            }
        }

        public bool HasReleased
        {
            get
            {
                if (!_hasReleased.HasValue)
                    _hasReleased = Ball.Attribute("released").ToBool();

                return _hasReleased.Value;
            }
        }

        public long Id { get; internal set; }

        public bool IsActiveTarget { get; internal set; }

        public bool IsAligning => Mode == 0;

        private bool IsAnchorableObject
        {
            get
            {
                if (GroupId == (int)Group.MobileWarpDisruptor) return true;
                return false;
            }
        }

        private bool IsAnchorableStructure
        {
            get
            {
                if (GroupId == (int)Group.POSControlTower) return true;
                return false;
            }
        }

        public bool IsApproachedOrKeptAtRangeByActiveShip => DirectEve.ActiveShip != null &&
                                                                             DirectEve.ActiveShip.Entity != null &&
                                                             DirectEve.ActiveShip.Entity.FollowId == Id
                                                             && DirectEve.ActiveShip.Entity.IsApproachingOrKeptAtRange
                                                             && DirectEve.GetEntityById(Id) != null;

        public bool IsApproachingOrKeptAtRange => Mode == 1;

        public bool IsAttacking => HasFlagSet(DirectEntityFlag.threatAttackingMe);

        public bool IsAbyssalDeadspaceTriglavianBioAdaptiveCache
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsAbyssalDeadspaceTriglavianBioAdaptiveCache)))
                        return false;

                    if (IsValid)
                    {
                        if (_isAbyssalDeadspaceTriglavianBioAdaptiveCache == null)
                        {
                            _isAbyssalDeadspaceTriglavianBioAdaptiveCache = false;

                            if (TypeId == (int)TypeID.AbyssalDeadspaceTriglavianBioAdaptiveCache && DirectEve.Session.IsAbyssalDeadspace)
                                _isAbyssalDeadspaceTriglavianBioAdaptiveCache = true;

                            if (TypeId == (int)TypeID.AbyssalDeadspaceTriglavianBioCombinativeCache && DirectEve.Session.IsAbyssalDeadspace)
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

        private bool? _isAbyssalDeadspaceTriglavianExtractionNode;

        public bool IsAbyssalDeadspaceTriglavianExtractionNode
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsAbyssalDeadspaceTriglavianExtractionNode)))
                        return false;

                    if (IsValid)
                    {
                        if (_isAbyssalDeadspaceTriglavianExtractionNode == null)
                        {
                            _isAbyssalDeadspaceTriglavianExtractionNode = false;

                            //If we are in a frigate assume we never want to kill these...
                            if (ESCache.Instance.ActiveShip.Entity.IsFrigate)
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

                        return _isAbyssalDeadspaceTriglavianExtractionNode ?? false;
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
        public bool IsCloaked
        {
            get
            {
                if (!IsValid) return false;
                if (!_isCloaked.HasValue)
                    _isCloaked = (int)Ball.Attribute("isCloaked") != 0;

                return _isCloaked.Value;
            }
        }

        public bool IsDockable
        {
            get
            {
                try
                {
                    if (_isDockable != null)
                        return (bool)_isDockable;
                    //
                    // we can dock with all stations now right? Any 0.0 situation would be a citadel iirc...
                    //
                    if (CategoryId == (int)CategoryID.Station)
                        return true;

                    if (CategoryId == (int)CategoryID.Citadel)
                    {
                        if (!_isDockable.HasValue)
                        {
                            bool? tempIsDockable = null;
                            try
                            {
                                tempIsDockable = (bool)DirectEve.GetLocalSvc("structureProximityTracker").Call("IsStructureDockable", Id);
                                //Logging.Log.WriteLine("IsDockable: Name [" + Name + "] IsDockable [" + tempIsDockable + "]");
                            }
                            catch (Exception ex)
                            {
                                Logging.Log.WriteLine("Exception [" + ex + "]");
                                return false;
                            }

                            _isDockable = tempIsDockable;
                            return _isDockable ?? false;
                        }

                        return _isDockable ?? false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Logging.Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                if (!_isEmpty.HasValue)
                    _isEmpty = (bool?)slimItem.Attribute("isEmpty") ?? true;

                return _isEmpty.Value;
            }
        }

        public bool IsItWorthTheTimeToChangeToBestAmmo
        {
            get
            {
                //
                // reload time,
                // Current DPS with resists,
                // Total Damage Lost to reload,
                // New DPS with resists,
                // Total time it would take to make up for lost DPS
                //

                return false;
            }
        }

        public bool IsTryingToJamMe { get; set; }

        public bool IsJammingMe { get; private set; }

        public bool IsKeepingAtRange => IsApproachingOrKeptAtRange;

        private bool IsNeutralizingMe { get; set; }

        public bool IsNpc => IsNPCByBracketType || (TypeName == "Pirate Capsule");

        public bool IsOrbitedByActiveShip => DirectEve.ActiveShip.Entity.FollowId == Id
                                             && DirectEve.ActiveShip.Entity.IsOrbiting
                                             && DirectEve.GetEntityById(Id) != null;

        public bool IsOrbiting => Mode == 4;

        public bool IsPlayer
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsPlayer)))
                        return false;

                    //if (DirectEve.Session.IsAbyssalDeadspace && Combat.PotentialCombatTargets.Any(i => i.Id == Id)) //Can we detect the proving grounds by the gates available?
                    //    return false;

                    if (GroupId == (int)Group.AbyssalSpaceshipEntities)
                        return false;

                    if (GroupId == (int)Group.Drifters_Battleship)
                        return false;

                    if (GroupId == (int)Group.DrifterResponseBattleship)
                        return false;

                    if (GroupId == (int)Group.DrifterReinforcements)
                        return false;

                    if (GroupId == (int)Group.AbyssalDeadspaceDroneEntities)
                        return false;

                    if (GroupId == (int)Group.InvadingPrecursorEntities)
                        return false;

                    if (CharId > 0)
                        return true;

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private bool IsSensorDampeningMe { get; set; }

        public bool IsTarget { get; internal set; }

        public bool IsTargetedBy { get; internal set; }

        public bool IsTargeting { get; internal set; }

        private bool IsTargetPaintingMe { get; set; }

        private bool IsTrackingDisruptingMe { get; set; }

        /// <summary>
        ///     Is it a valid entity?
        /// </summary>
        public bool IsValid
        {
            get
            {
                try
                {
                    if (DateTime.UtcNow.AddSeconds(-15) > _thisEntityCreated)
                    {
                        Log.WriteLine("The DirectEntity instance that represents [" + Name + "][" +
                                      Math.Round(Distance / 1000, 0) + "k][" +
                                      Id + "] was created [" + _thisEntityCreated + "]");
                        //Log.WriteLine("S[" + ShieldPct + "]A[" + ArmorPct + "]H[" + StructurePct + "]");
                        intCountInValidEntities++;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }


                if (!ball.IsValid)
                {
                    if (DebugConfig.DebugWspaceSiteBehavior)
                    {
                        //if (TypeId == (int)TypeID.Venture)
                        //{
                        //Log.WriteLine("Start Ball.LogObject()");
                        //Log.WriteLine($"{Ball.LogObject()}");
                        //Log.WriteLine("Done Ball.LogObject()");
                        //Log.WriteLine("Start _slimItem.LogObject()");
                        //Log.WriteLine($"{_slimItem.LogObject()}");
                        //Log.WriteLine("Done _slimItem.LogObject()");
                        Log.WriteLine("[" + Name + "] ID [" + Id + "] Distance [" + Distance + "] if (!Ball.IsValid)");
                        //}
                    }

                    return false;
                }

                if (!slimItem.IsValid)
                {
                    Log.WriteLine("[" + Name + "][" + Id + "] if (!slimItem.IsValid) _thisEntityCreated [" + _thisEntityCreated + "]");
                    return false;
                }

                if (!DirectEve.Session.IsInSpace)
                    return false;

                if (Id <= 0)
                {
                    Log.WriteLine("[" + Name + "][" + Id + "] if (Id <= 0)");
                    return false;
                }

                if (HasReleased)
                {
                    Log.WriteLine("[" + Name + "][" + Id + "] if (HasReleased)");
                    return false;
                }

                if (HasExploded)
                {
                    Log.WriteLine("[" + Name + "][" + Id + "] if (HasExploded)");
                    return false;
                }

                if (DirectEve.IsTargetBeingRemoved(Id))
                {
                    //Log.WriteLine("[" + Name + "][" + Id + "] if (DirectEve.IsTargetBeingRemoved(Id);");
                    return false;
                }

                return true;
            }
        }

        public bool IsWarpingByMode => Mode == 3;

        public bool IsWarpScramblingMe { get; private set; }

        public bool IsWarpDisruptingMe { get; private set; }

        public bool IsWarpScramblingOrDisruptingMe => IsWarpScramblingMe || IsWarpDisruptingMe;

        public bool IsWebbingMe { get; private set; }

        public bool IsWreck
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsWreck)))
                        return false;

                    if (IsValid)
                    {
                        if (GroupId == (int)Group.Wreck)
                            return true;

                        if (BracketType == BracketType.Wreck_NPC)
                            return true;

                        //if (BracketType == BracketType.Wreck)
                        //    return true;

                        //if (Name.Contains("Cache Wreck") && !IsPlayer)
                        //    return true;

                        //if (DirectEve.Session.IsAbyssalDeadspace && IsContainer && Name.Contains(" Wreck"))
                        //    return true;

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

        public bool IsWreckEmpty
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsWreckEmpty)))
                        return false;

                    if (IsValid)
                    {
                        if (IsWreck)
                            return IsEmpty;

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


        public void Dump()
        {
            DirectEve.Log($"{ball.LogObject()}");
            DirectEve.Log($"{slimItem.LogObject()}");
            DirectEve.Log($"{ball.Attribute("model").LogObject()}");
        }

        public double? MissileRange
        {
            get
            {
                try
                {
                    if (!IsNpc) return 0;

                    double? tempMissileRange = (NpcMissileEntityFlightTimeMultiplier * NpcMissileEntityFlightTime) + (NpcMissileEntityVelocityMultiplier * NpcMissileEntityVelocity);
                    if (tempMissileRange.HasValue && tempMissileRange.Value > 0)
                        return tempMissileRange;

                    return null;
                }
                catch (Exception)
                {
                    //Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        // align: mode 0 / no following entity
        // stop: mode 2 / no following entity
        // warp: mode 3 / ??
        // approach: 1 / has a following entity
        // keep at range: 1 / has a following entity
        // orbit: 4 / has a following entity

        public int Mode
        {
            get
            {
                if (!IsValid) return 0;
                if (!_mode.HasValue)
                    _mode = (int)Ball.Attribute("mode");

                return _mode.Value;
            }
        }

        //DRONE_STATES = {entities.STATE_IDLE: 'UI/Inflight/Drone/Idle',
        //entities.STATE_COMBAT: 'UI/Inflight/Drone/Fighting',
        //entities.STATE_MINING: 'UI/Inflight/Drone/Mining',
        //entities.STATE_APPROACHING: 'UI/Inflight/Drone/Approaching',
        //entities.STATE_DEPARTING: 'UI/Inflight/Drone/ReturningToShip',
        //entities.STATE_DEPARTING_2: 'UI/Inflight/Drone/ReturningToShip',
        //entities.STATE_OPERATING: 'UI/Inflight/Drone/Operating',
        //entities.STATE_PURSUIT: 'UI/Inflight/Drone/Following',
        //entities.STATE_FLEEING: 'UI/Inflight/Drone/Fleeing',
        //entities.STATE_ENGAGE: 'UI/Inflight/Drone/Repairing',
        //entities.STATE_SALVAGING: 'UI/Inflight/Drone/Salvaging',

        private int? _droneState;

        /// <summary>
        /// 0 = idle, 1 = attacking, 4 = returning to bay
        /// </summary>
        public int? DroneState
        {
            get
            {
                if (_droneState == null)
                {
                    var stateByDroneId = DirectEve.GetLocalSvc("michelle")["_Michelle__bp"]["stateByDroneID"];
                    if (stateByDroneId.IsValid)
                    {
                        var item = stateByDroneId.DictionaryItem(this.Id);
                        if (item.IsValid)
                        {
                            _droneState = item["activityState"].ToInt();
                        }
                    }
                }
                return _droneState;
            }
        }

        //private string? _droneStatename;
        public string? DroneStateName
        {
            get
            {
                if (DroneState != null)
                {
                    if (DroneState == 0)
                    {
                        return Drones.DroneState.Idle.ToString();
                    }

                    if (DroneState == 1)
                    {
                        return Drones.DroneState.Attacking.ToString();
                    }

                    if (DroneState == 2)
                    {
                        return Drones.DroneState.Mining.ToString();
                    }

                    if (DroneState == 3)
                    {
                        return Drones.DroneState.Approaching.ToString();
                    }

                    if (DroneState == 4)
                    {
                        return Drones.DroneState.Returning.ToString();
                    }

                    if (DroneState == 5)
                    {
                        return Drones.DroneState.Operating.ToString();
                    }

                    if (DroneState == 6)
                    {
                        return Drones.DroneState.Following.ToString();
                    }

                    if (DroneState == 7)
                    {
                        return Drones.DroneState.Fleeing.ToString();
                    }

                    if (DroneState == 8)
                    {
                        return Drones.DroneState.Repairing.ToString();
                    }

                    if (DroneState == 9)
                    {
                        return Drones.DroneState.Returning2.ToString();
                    }

                    if (DroneState == 10)
                    {
                        return Drones.DroneState.Salvaging.ToString();
                    }

                    if (DroneState > 10)
                    {
                        return Drones.DroneState.Unknown.ToString();
                    }

                    return "Unknown";
                }

                return "Unknown";
            }
        }

        private float? _modelBoundingSphereCenterX;
        private float? _modelBoundingSphereCenterY;
        private float? _modelBoundingSphereCenterZ;

        private float? _modelBoundingSphereRadius;

        private Vec3? _modelBoundingSphereCenter;

        public Vec3 ModelBoundingSphereCenter => _modelBoundingSphereCenter ??= new Vec3(ModelBoundingSphereCenterX.Value,
            ModelBoundingSphereCenterY.Value, ModelBoundingSphereCenterZ.Value);

        public float? ModelBoundingSphereCenterX => _modelBoundingSphereCenterX ??=
            Ball["model"]["boundingSphereCenter"].GetItemAt(0).ToFloat();

        public float? ModelBoundingSphereCenterY => _modelBoundingSphereCenterY ??=
            Ball["model"]["boundingSphereCenter"].GetItemAt(1).ToFloat();

        public float? ModelBoundingSphereCenterZ => _modelBoundingSphereCenterZ ??=
            Ball["model"]["boundingSphereCenter"].GetItemAt(2).ToFloat();

        public float? ModelBoundingSphereRadius => _modelBoundingSphereRadius ??=
            Ball["model"]["boundingSphereRadius"].ToFloat();

        private Vec3? _screenPos;

        private float? _modelScale;
        public float ModelScale => _modelScale ??= Ball["model"]["modelScale"].ToFloat();

        private float? _ballRadius;
        public float BallRadius => _ballRadius ??= Ball["radius"].ToFloat();

        public bool Display => Ball["model"]["display"].ToBool();

        public void SetDisplay(bool val)
        {
            Ball["model"].SetAttribute("display", val);
        }

        public Vec3? ScreenPos
        {
            get
            {
                if (_screenPos == null)
                {

                    var ballPos = DirectEve.SceneManager.CamUtil.Call("_GetBallPosition", this.Ball);
                    if (!ballPos.IsValid)
                    {
                        DirectEve.Log("BallPos not valid.");
                        return null;
                    }

                    if (!DirectEve.SceneManager.IsSeenByCamera(ballPos))
                        return null;

                    var viewPort = PySharp.Import("trinity")["device"]["viewport"];
                    if (!viewPort.IsValid)
                    {
                        DirectEve.Log("Trinity.device.viewport not valid.");
                        return null;
                    }
                    var viewPortTuple = PyObject.CreateTuple(PySharp, viewPort["x"].ToInt(), viewPort["y"].ToInt(),
                        viewPort["width"].ToInt(), viewPort["height"].ToInt(), viewPort["minZ"].ToInt(),
                        viewPort["maxZ"].ToInt());

                    if (!viewPortTuple.IsValid)
                    {
                        DirectEve.Log("ViewPortTuple is not valid.");
                        return null;
                    }

                    var geo2 = DirectEve.SceneManager.Geo2;
                    if (!geo2.IsValid)
                        return null;

                    var cam = DirectEve.SceneManager.Camera;
                    var matrixIdent = DirectEve.SceneManager.MatrixIdentity;

                    var sm = DirectEve.SceneManager;

                    //DirectEve.Log(ballPos.LogObject());
                    //DirectEve.Log(viewPortTuple.LogObject());
                    //DirectEve.Log(sm.ProjectionMatrix.LogObject());
                    //DirectEve.Log(sm.ViewMatrix.LogObject());
                    //DirectEve.Log(sm.MatrixIdentity.LogObject());

                    var res = geo2.Call("Vec3Project", ballPos, viewPortTuple, sm.ProjectionMatrix, sm.ViewMatrix, sm.MatrixIdentity);

                    if (!res.IsValid)
                    {
                        DirectEve.Log("Result not valid.");
                        return null;
                    }

                    var r = res.ToList();

                    _screenPos = new Vec3(r[0].ToInt(), r[1].ToInt(), r[2].ToInt());
                }
                return _screenPos.Value;
            }
        }

        public string Name
        {
            get
            {
                if (_name != null)
                    return _name;

                if (!IsValid) return string.Empty;

                if (string.IsNullOrEmpty(_name))
                    _name = (string)PySharp.Import("eve.client.script.ui.util.uix").Call("GetSlimItemName", slimItem);

                return _name;
            }
        }

        private double? NpcDamageMultiplier
        {
            get
            {
                if (!IsValid) return 1;
                if (!IsNpc) return 1;

                if (_npcDamageMultiplier == null)
                    _npcDamageMultiplier = (float)Ball.Attribute("damageMultiplier");

                if (_npcDamageMultiplier.HasValue)
                    return _npcDamageMultiplier;

                return 1;
            }
        }

        public double? NpcEffectiveDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;
                //
                // What about triglavianDPS?
                //
                return NpcEffectiveMissileDps + NpcEffectiveTurretDps;
            }
        }

        private double? NpcEffectiveMissileDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                return NpcEmEffectiveMissileDps + NpcThermalEffectiveMissileDps + NpcKineticEffectiveMissileDps + NpcExplosiveEffectiveMissileDps;
            }
        }

        public double? NpcEffectiveTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                return NpcEmEffectiveTurretDps + NpcThermalEffectiveTurretDps + NpcKineticEffectiveTurretDps + NpcExplosiveEffectiveTurretDps;
            }
        }

        private double? NpcEmEffectiveMissileDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;
                return NpcEmRawMissileDps * DirectEve.ActiveShip.Entity.ShieldResistanceEM;
            }
        }

        private double? NpcEmEffectiveTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;
                return NpcEmRawTurretDps * DirectEve.ActiveShip.Entity.ShieldResistanceEM;
            }
        }

        public double? NpcEmRawMissileDamage
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcEmMissileDamage == null)
                {
                    _npcEmMissileDamage = (int)Ball.Attribute("emDamage");
                    if (_npcEmMissileDamage.HasValue)
                        return _npcEmMissileDamage.Value;

                    return 0;
                }

                return _npcEmMissileDamage;
            }
        }

        public double? NpcEmRawMissileDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcEmMissileDps == null)
                {
                    if (NpcEmRawMissileDamage != null && NpcMissileDamageMultiplier != null && NpcMissileRateOfFire != null)
                    {
                        _npcEmMissileDps = (double)NpcEmRawMissileDamage * (double)NpcMissileDamageMultiplier / (double)NpcMissileRateOfFire;
                        if (_npcEmMissileDps != null && !double.IsNaN(_npcEmMissileDps.Value) && !double.IsInfinity(_npcEmMissileDps.Value))
                            return _npcEmMissileDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _npcEmMissileDps;
            }
        }

        public double? NpcEmRawTurretDamage
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcEmTurretDamage == null)
                {
                    _npcEmTurretDamage = (int)Ball.Attribute("emDamage");

                    if (_npcEmTurretDamage.HasValue)
                        return _npcEmTurretDamage.Value;

                    return 0;
                }

                return _npcEmTurretDamage.Value;
            }
        }

        public double? NpcEmRawTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcEmTurretDps == null)
                {
                    if (NpcEmRawMissileDamage != null && NpcDamageMultiplier != null && NpcRateOfFire != null)
                    {
                        _npcEmTurretDps = (double)NpcEmRawMissileDamage * (double)NpcDamageMultiplier / (double)NpcRateOfFire;
                        return _npcEmTurretDps.Value;
                    }

                    return 0;
                }

                return _npcEmTurretDps;
            }
        }

        private double? NpcExplosiveEffectiveMissileDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;
                return NpcExplosiveRawMissileDps * DirectEve.ActiveShip.Entity.ShieldResistanceExplosive;
            }
        }

        private double? NpcExplosiveEffectiveTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;
                return NpcExplosiveRawTurretDps * DirectEve.ActiveShip.Entity.ShieldResistanceExplosive;
            }
        }

        public double? NpcExplosiveRawMissileDamage
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcExplosiveMissileDamage == null)
                {
                    _npcExplosiveMissileDamage = (double)Ball.Attribute("explosiveDamage");

                    if (_npcExplosiveMissileDamage.HasValue)
                        return _npcExplosiveMissileDamage.Value;

                    return 0;
                }

                return _npcExplosiveMissileDamage;
            }
        }

        public double? NpcExplosiveRawMissileDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcExplosiveMissileDps == null)
                {
                    if (NpcExplosiveRawMissileDamage != null && NpcExplosiveRawMissileDamage != 0 && NpcMissileDamageMultiplier != null && NpcMissileRateOfFire != null)
                    {
                        _npcExplosiveMissileDps = (double)NpcExplosiveRawMissileDamage * (double)NpcMissileDamageMultiplier / (double)NpcMissileRateOfFire;
                        if (_npcExplosiveMissileDps != null && !double.IsNaN(_npcExplosiveMissileDps.Value) && !double.IsInfinity(_npcExplosiveMissileDps.Value))
                            return _npcExplosiveMissileDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _npcExplosiveMissileDps;
            }
        }

        public double? NpcExplosiveRawTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcExplosiveDps == null)
                {
                    if (ExplosiveRawTurretDamage != null && NpcDamageMultiplier != null && NpcRateOfFire != null)
                    {
                        _npcExplosiveDps = (double)ExplosiveRawTurretDamage * (double)NpcDamageMultiplier / (double)NpcRateOfFire;
                        if (_npcExplosiveDps != null && !double.IsNaN(_npcExplosiveDps.Value) && !double.IsInfinity(_npcExplosiveDps.Value))
                            return _npcExplosiveDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _npcExplosiveDps;
            }
        }

        public double EnergyNeutralizerAmount
        {
            get
            {
                if (_energyNeutralizerAmount.HasValue)
                {
                    return (double)_energyNeutralizerAmount;
                }

                return 0;
            }
        }

        public bool NpcHasNeutralizers
        {
            get
            {
                if (!IsValid) return false;
                if (!IsNpc) return false;

                if (!_energyNeutralizerAmount.HasValue)
                    _energyNeutralizerAmount = (double)Ball.Attribute("energyNeutralizerAmount");

                if (Name.ToLower().Contains("Starving".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Nullcharge".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Firewatcher".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Sentinel".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Illuminator".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Dissipator".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Devoted Knight".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Devoted Smith".ToLower()))
                    return true;

                if (_energyNeutralizerAmount.Value > 0)
                    return true;

                return false;
            }
        }

        private double? NpcKineticEffectiveMissileDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;
                return NpcKineticRawMissileDps * DirectEve.ActiveShip.Entity.ShieldResistanceKinetic;
            }
        }

        private double? NpcKineticEffectiveTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;
                return NpcKineticRawTurretDps * DirectEve.ActiveShip.Entity.ShieldResistanceKinetic;
            }
        }

        public double? NpcKineticRawMissileDamage
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcKineticMissileDamage == null)
                {
                    _npcKineticMissileDamage = (double)Ball.Attribute("kineticDamage");
                    if (_npcKineticMissileDamage.HasValue)
                        return _npcKineticMissileDamage.Value;

                    return 0;
                }

                return _npcKineticMissileDamage.Value;
            }
        }

        public double? NpcKineticRawMissileDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcKineticMissileDps == null)
                {
                    if (NpcKineticRawMissileDamage != null && NpcKineticRawMissileDamage != 0 && NpcMissileDamageMultiplier != null && NpcMissileRateOfFire != null)
                    {
                        _npcKineticMissileDps = (double)NpcKineticRawMissileDamage * (double)NpcMissileDamageMultiplier / (double)NpcMissileRateOfFire;
                        if (_npcKineticMissileDps != null && !double.IsNaN(_npcKineticMissileDps.Value) && !double.IsInfinity(_npcKineticMissileDps.Value))
                            return _npcKineticMissileDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _npcKineticMissileDps;
            }
        }

        public double? NpcKineticRawTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcKineticTurretDps == null)
                {
                    if (NpcKineticTurretDamage != null && NpcDamageMultiplier != null && NpcRateOfFire != null)
                    {
                        _npcKineticTurretDps = (double)NpcKineticTurretDamage * (double)NpcDamageMultiplier / (double)NpcRateOfFire;
                        if (_npcKineticTurretDps != null && !double.IsNaN(_npcKineticTurretDps.Value) && !double.IsInfinity(_npcKineticTurretDps.Value))
                            return _npcKineticTurretDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _npcKineticTurretDps;
            }
        }

        public double? NpcKineticTurretDamage
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcKineticTurretDamage == null)
                {
                    _npcKineticTurretDamage = (float)Ball.Attribute("kineticDamage");

                    if (_npcKineticTurretDamage.HasValue)
                        return _npcKineticTurretDamage.Value;

                    return 0;
                }

                return _npcKineticTurretDamage.Value;
            }
        }

        private DirectItem NpcMissileAmmoType
        {
            get
            {
                if (_npcMissileAmmoType == null)
                {
                    if (EntityMissileTypeId != null)
                    {
                        _npcMissileAmmoType = new DirectItem(DirectEve)
                        {
                            TypeId = (int)EntityMissileTypeId
                        };

                        if (_npcMissileAmmoType.TypeId != 0)
                            return _npcMissileAmmoType;

                        return null;
                    }

                    return null;
                }

                return _npcMissileAmmoType;
            }
        }

        private double? NpcMissileDamageMultiplier
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcMissileDamageMultiplier == null)
                    _npcMissileDamageMultiplier = (float)Ball.Attribute("missileDamageMultiplier");

                return _npcMissileDamageMultiplier.Value;
            }
        }

        public double? NpcMissileEntityAoeCloudSizeMultiplier
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcMissileEntityAoeCloudSizeMultiplier == null)
                    _npcMissileEntityAoeCloudSizeMultiplier = (float)Ball.Attribute("missileEntityAoeCloudSizeMultiplier");

                return _npcMissileEntityAoeCloudSizeMultiplier.Value;
            }
        }

        public double? NpcMissileEntityAoeVelocityMultiplier
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcMissileEntityAoeVelocityMultiplier == null)
                    _npcMissileEntityAoeVelocityMultiplier = (float)Ball.Attribute("missileEntityAoeVelocityMultiplier");

                return _npcMissileEntityAoeVelocityMultiplier.Value;
            }
        }

        public double? NpcMissileEntityFlightTime
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcMissileEntityFlightTime == null)
                {
                    if (NpcMissileAmmoType != null && NpcMissileAmmoType.TypeId != 0)
                    {
                        //this is correct - the explosionDelay attribute is the missiles flight time (can you say wtf?!)
                        _npcMissileEntityFlightTime = NpcMissileAmmoType.Attributes.TryGet<double>("explosionDelay");
                        return _npcMissileEntityFlightTime.Value;
                    }

                    return null;
                }

                return _npcMissileEntityFlightTime.Value;
            }
        }

        private double? NpcMissileEntityFlightTimeMultiplier
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcMissileEntityFlightTimeMultiplier == null)
                    _npcMissileEntityFlightTimeMultiplier = (float)Ball.Attribute("missileEntityFlightTimeMultiplier");

                if (_npcMissileEntityFlightTimeMultiplier != null && !double.IsNaN(_npcMissileEntityFlightTimeMultiplier.Value) && !double.IsInfinity(_npcMissileEntityFlightTimeMultiplier.Value))
                    return _npcMissileEntityFlightTimeMultiplier.Value;

                return 0;
            }
        }

        public double? NpcMissileEntityVelocity
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcMissileEntityVelocity == null)
                    _npcMissileEntityVelocity = NpcMissileAmmoType.Attributes.TryGet<double>("maxVelocity");

                return _npcMissileEntityVelocity.Value;
            }
        }

        private double? NpcMissileEntityVelocityMultiplier
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcMissileEntityVelocityMultiplier == null)
                    _npcMissileEntityVelocityMultiplier = (float)Ball.Attribute("missileEntityVelocityMultiplier");

                return _npcMissileEntityVelocityMultiplier.Value;
            }
        }

        private double? NpcMissileRateOfFire
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcMissileRateOfFire == null)
                    _npcMissileRateOfFire = (float)Ball.Attribute("missileLaunchDuration");

                return _npcMissileRateOfFire.Value;
            }
        }

        private double? NpcRateOfFire
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcRateOfFire == null)
                    _npcRateOfFire = (float)Ball.Attribute("speed");

                return _npcRateOfFire.Value / 1000;
            }
        }

        public double NpcRawTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcTurretDps == null)
                {
                    _npcTurretDps = NpcEmRawTurretDps ?? 0 + NpcExplosiveRawTurretDps ?? 0 + NpcKineticRawTurretDps ?? 0 + NpcThermalRawTurretDps ?? 0;
                    if (_npcTurretDps != null && !double.IsNaN(_npcTurretDps.Value) && !double.IsInfinity(_npcTurretDps.Value))
                        return _npcTurretDps.Value;

                    return 0;
                }

                return (double)_npcTurretDps;
            }
        }

        public double NpcRemoteArmorRepairChance
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (!_npcRemoteArmorRepairChance.HasValue)
                    _npcRemoteArmorRepairChance = (float)Ball.Attribute("npcRemoteArmorRepairChance");

                if (_npcRemoteArmorRepairChance == null)
                    _npcRemoteArmorRepairChance = (float)Ball.Attribute("behaviorRemoteArmorRepairDischarge");

                return _npcRemoteArmorRepairChance.Value;
            }
        }

        public double NpcRemoteShieldRepairChance
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (!_npcRemoteShieldRepairChance.HasValue)
                    _npcRemoteShieldRepairChance = (float)Ball.Attribute("npcRemoteShieldRepairChance");

                if (_npcRemoteShieldRepairChance == null)
                    _npcRemoteShieldRepairChance = (float)Ball.Attribute("behaviorRemoteShieldRepairDischarge");

                return _npcRemoteShieldRepairChance.Value;
            }
        }

        public double? NpcShieldRechargeRate
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcShieldRechargeRate == null)
                    _npcShieldRechargeRate = (float)Ball.Attribute("shieldRechargeRate");

                if (_npcShieldRechargeRate != null && !double.IsNaN(_npcShieldRechargeRate.Value) && !double.IsInfinity(_npcShieldRechargeRate.Value))
                    return _npcShieldRechargeRate.Value;

                return 0;
            }
        }

        public double? NpcShieldUnifirmity
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcShieldUniformity == null)
                    _npcShieldUniformity = (float)Ball.Attribute("shieldUnifirmity");

                if (_npcShieldUniformity != null && !double.IsNaN(_npcShieldUniformity.Value) && !double.IsInfinity(_npcShieldUniformity.Value))
                    return _npcShieldUniformity.Value;

                return 0;
            }
        }

        private double? NpcThermalEffectiveMissileDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                return NpcThermalRawMissileDps * DirectEve.ActiveShip.Entity.ShieldResistanceThermal;
            }
        }

        private double? NpcThermalEffectiveTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                return NpcThermalRawTurretDps * DirectEve.ActiveShip.Entity.ShieldResistanceThermal;
            }
        }

        public double? NpcThermalRawMissileDamage
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcThermalMissileDamage == null)
                {
                    _npcThermalMissileDamage = (float)Ball.Attribute("npcRemoteArmorRepairChance");

                    if (_npcThermalMissileDamage.HasValue)
                        return _npcThermalMissileDamage.Value;

                    return 0;
                }

                return _npcThermalMissileDamage;
            }
        }

        public double? NpcThermalRawMissileDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcThermalMissileDps == null)
                {
                    if (NpcThermalRawMissileDamage != null && NpcThermalRawMissileDamage != 0 && NpcMissileDamageMultiplier != null && NpcMissileRateOfFire != null)
                    {
                        _npcThermalMissileDps = (double)NpcThermalRawMissileDamage * (double)NpcMissileDamageMultiplier / (double)NpcMissileRateOfFire;
                        if (_npcThermalMissileDps != null && !double.IsNaN(_npcThermalMissileDps.Value) && !double.IsInfinity(_npcThermalMissileDps.Value))
                            return _npcThermalMissileDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _npcThermalMissileDps;
            }
        }

        public double? NpcThermalRawTurretDamage
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcThermalTurretDamage == null)
                {
                    _npcThermalTurretDamage = (float)Ball.Attribute("thermalDamage");
                    if (_npcThermalTurretDamage.HasValue)
                        return _npcThermalTurretDamage.Value;

                    return 0;
                }

                return _npcThermalTurretDamage.Value;
            }
        }

        public double? NpcThermalRawTurretDps
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_npcThermalTurretDps == null)
                {
                    if (NpcThermalRawTurretDamage != null && NpcDamageMultiplier != null && NpcRateOfFire != null)
                    {
                        _npcThermalTurretDps = (double)NpcThermalRawTurretDamage * (double)NpcDamageMultiplier / (double)NpcRateOfFire;
                        if (_npcThermalTurretDps != null && !double.IsNaN(_npcThermalTurretDps.Value) && !double.IsInfinity(_npcThermalTurretDps.Value))
                            return _npcThermalTurretDps.Value;

                        return 0;
                    }

                    return 0;
                }

                return _npcThermalTurretDps;
            }
        }

        public bool IsMyActiveDrone
        {
            get
            {
                if (CategoryId == (int)CategoryID.Drone)
                {
                    if (IsOwnedByMe)
                    {
                        if (Velocity > 0)
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        private DirectOwner _ownerOfThisEntity;

        private DirectOwner OwnerOfThisEntity
        {
            get
            {
                try
                {
                    if (_ownerOfThisEntity == null)
                    {
                        _ownerOfThisEntity = DirectEve.GetOwner(OwnerId);
                        return _ownerOfThisEntity;
                    }

                    return _ownerOfThisEntity;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public bool IsOwnedByMe
        {
            get
            {
                if (OwnerId == DirectEve.Session.CharacterId)
                    return true;

                return false;
            }
        }

        public int NumOfPCTInVontonArcRange
        {
            get
            {
                //If owned by someone in an NPC corp assume it is NOT safe to loot, etc.
                if (Combat.PotentialCombatTargets.Any())
                {
                    return Combat.PotentialCombatTargets.Count(i => 10000 > i._directEntity.DistanceTo(this));
                }

                return 0;
            }
        }

        public bool IsOwnedByMyCorp
        {
            get
            {
                //If owned by someone in an NPC corp assume it is NOT safe to loot, etc.
                if (!IsOwnedByMe && DirectNpcInfo.NpcCorpIdsToNames.ContainsKey(CorpId.ToString()))
                    return false;

                if (DirectEve.Session.CorporationId != null)
                {
                    if (CorpId == DirectEve.Session.CorporationId)
                        return true;

                    if (OwnerOfThisEntity.CorpId == DirectEve.Session.CorporationId)
                        return true;
                }

                return false;
            }
        }

        public int OwnerId
        {
            get
            {
                if (!_ownerId.HasValue)
                    _ownerId = (int)slimItem.Attribute("ownerID");

                return _ownerId.Value;
            }
        }

        /**
        public Vec3? PointInSpaceAwayNSEWUD
        {
            get
            {
                if (PointInSpaceDirectlyWest == null)
                    return null;

                if (PointInSpaceDirectlyEast == null)
                    return null;

                if (PointInSpaceDirectlyUp == null)
                    return null;

                if (PointInSpaceDirectlyDown == null)
                    return null;

                if (DirectWorldPosition == null)
                    return null;

                Vec3 tempPointInSpace = DirectWorldPosition.PositionInSpace;
                if (WeAreWestOfThisEntity != null && (bool)WeAreWestOfThisEntity)
                    tempPointInSpace = (Vec3)PointInSpaceDirectlyWest;

                if (WeAreEastOfThisEntity != null && (bool)WeAreEastOfThisEntity)
                    tempPointInSpace = (Vec3)PointInSpaceDirectlyEast;

                if (WeAreAboveThisEntity != null && (bool)WeAreAboveThisEntity)
                    tempPointInSpace = new Vec3(tempPointInSpace.X + (Vec3)PointInSpaceDirectlyUp.X, tempPointInSpace.Y + (Vec3)PointInSpaceDirectlyUp.Y, tempPointInSpace.Z + (Vec3)PointInSpaceDirectlyUp.Z);

                if (WeAreBelowThisEntity != null && (bool)WeAreBelowThisEntity)
                    tempPointInSpace = new Vec3(tempPointInSpace.X + (Vec3)PointInSpaceDirectlyDown.X, tempPointInSpace.Y + (Vec3)PointInSpaceDirectlyDown.Y, tempPointInSpace.Z + (Vec3)PointInSpaceDirectlyDown.Z);

                return tempPointInSpace;
            }
        }
        **/

        //
        // coord - 1 coord == 1 meter
        // X - up and down
        // Y - Left to Right
        // Z - forwards and backwards
        //

        /**
        public Vec3 KiteDamageToTheRight
        {
            get
            {
                var MaxDistanceToAllow = 50000;
                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    MaxDistanceToAllow = 35000;
                }

                var HowFarRightToMove = Math.Min(Distance + 5000, MaxDistanceToAllow);
                var newYCoord = YCoordinate + HowFarRightToMove;

                return new Vec3(XCoordinate, newYCoord, ZCoordinate + 30000);
            }
        }

        public Vec3 KiteDamageToTheLeft
        {
            get
            {
                var MaxDistanceToAllow = 50000;
                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    MaxDistanceToAllow = 35000;
                }

                var HowFarLeftToMove = Math.Min(Distance + 5000, MaxDistanceToAllow);
                var newYCoord = YCoordinate - HowFarLeftToMove;
                Vec3 temp = new Vec3(XCoordinate, newYCoord, ZCoordinate + 30000);
                return temp;
            }
        }
        **/

        public Vec3? PointInSpaceDirectlyDown
        {
            get
            {
                if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                    return null;

                return new Vec3((double)XCoordinate + 10000000, (double)YCoordinate, (double)ZCoordinate);
            }
        }

        public Vec3? PointInSpaceDirectlyEast
        {
            get
            {
                if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                    return null;

                return new Vec3((double)XCoordinate, (double)YCoordinate - 10000000, (double)ZCoordinate);
            }
        }

        public void DrawHighTransversalPointInSpaceEast()
        {
            if (HighTransversalPointInSpaceEast != null)
            {
                DrawSphereOverHere((Vec3)HighTransversalPointInSpaceEast, 1000, SphereType.Scanbubblehitsphere);
                return;
            }

            return;
        }

        public Vec3? HighTransversalPointInSpaceEast
        {
            get
            {
                if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                    return null;

                return new Vec3((double)XCoordinate, (double)YCoordinate - Distance, (double)ZCoordinate);
            }
        }

        public Vec3? PointInSpaceDirectlyNorth
        {
            get
            {
                if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                    return null;

                return new Vec3((double)XCoordinate, (double)YCoordinate, (double)ZCoordinate + 10000000);
            }
        }

        public Vec3? PointInSpaceDirectlySouth
        {
            get
            {
                if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                    return null;

                return new Vec3((double)XCoordinate, (double)YCoordinate, (double)ZCoordinate - 10000000);
            }
        }

        public Vec3? PointInSpaceDirectlyUp
        {
            get
            {
                if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                    return null;

                return new Vec3((double)XCoordinate - 10000000, (double)YCoordinate, (double)ZCoordinate);
            }
        }

        public Vec3? PointInSpaceDirectlyWest
        {
            get
            {
                if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                    return null;

                return new Vec3((double)XCoordinate, (double)YCoordinate + 10000000, (double)ZCoordinate);
            }
        }

        public Vec3? HighTransversalPointInSpaceWest
        {
            get
            {
                if (XCoordinate == null || YCoordinate == null || ZCoordinate == null)
                    return null;

                return new Vec3((double)XCoordinate, (double)YCoordinate + Distance, (double)ZCoordinate);
            }
        }

        public new double Radius
        {
            get
            {
                try
                {
                    if (!IsValid) return 0;

                    if (!_radius.HasValue)
                        _radius = (double)Ball.Attribute("radius");

                    if (!_radius.HasValue || _radius == 0)
                        _radius = (double)Ball.Attribute("Radius");

                    return _radius.Value;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return 0;
                }
            }
        }

        public double? RawEhp => _rawEhp ?? (_rawEhp = CurrentShield + CurrentArmor + CurrentStructure);

        public bool ScoopToCargoHold
        {
            get
            {
                if (!IsValid)
                    return false;

                if (DirectEve.ActiveShip.Entity == null)
                    return false;

                if (DirectEve.ActiveShip.Entity.GroupId == (int)Group.Capsule)
                    return false;

                if (DirectEve.GetShipsCargo() == null)
                    return false;

                if (GroupId == (int)Group.MobileTractor)
                {
                    // regular = 100
                    // pakrat = 125
                    // magpie = 100
                    int m3 = 100;
                    if (TypeId == (int)TypeID.PackratMobileTractorUnit)
                        m3 = 125;

                    if (m3 > DirectEve.GetShipsCargo().FreeCapacity)
                        return false;
                }

                if (GroupId != (int)Group.MobileTractor && GroupId != (int)Group.MobileDepot)
                    return false;

                if (GroupId == (int)Group.MobileTractor || GroupId == (int)Group.MobileDepot)
                {
                    //
                    // https://wiki.eveuniversity.org/Anchoring#Mobile_Tractor_Unit
                    // Deployment time is only 10 seconds
                    //
                    if (Time.Instance.LastLaunchForSelf.AddSeconds(11) > DateTime.UtcNow)
                        return false;

                    if (Distance > (double)Distances.ScoopRange)
                        return false;

                    var call = DirectEve.GetLocalSvc("menu")["Scoop"];
                    if (call.IsValid)
                    {
                        if (!DirectEve.Interval(4000, 6000, Id.ToString()))
                            return false;

                        DirectEve.ThreadedCall(call, Id, TypeId);
                    }
                    return true;
                }

                DirectEve.Log("Couldnt launch for self. Probably wrong type.");
                return false;

            }
        }

        public bool ScoopToFighterBay
        {
            get
            {
                if (!DirectEve.Session.IsInSpace)
                    return false;

                if (!IsValid)
                    return false;

                if (GroupId != (int)Group.MobileTractor && GroupId != (int)Group.MobileDepot)
                    return false;

                if (!DirectEve.Interval(4000, 6000))
                    return false;

                return DirectEve.ThreadedLocalSvcCall("menu", "ScoopToFighterBay", Id, TypeId);
            }
        }

        public bool ScoopToFleetHangar
        {
            get
            {
                if (!IsValid)
                    return false;

                if (GroupId != (int)Group.MobileTractor && GroupId != (int)Group.MobileDepot)
                    return false;

                if (!DirectEve.Interval(4000, 6000))
                    return false;

                return DirectEve.ThreadedLocalSvcCall("menu", "ScoopToFleetHangar", Id, TypeId);
            }
        }

        private DirectCharacter _characterFromLocal = null;

        public DirectCharacter CharacterFromLocal
        {
            get
            {
                if (DirectEve.Session.IsInSpace || DirectEve.Session.IsInDockableLocation)
                {
                    //try not to do this in crowded systems
                    if (DirectEve.Session.CharactersInLocal.Count > 70)
                        return null;

                    foreach (DirectCharacter localMember in DirectEve.Session.CharactersInLocal)
                    {
                        if (localMember.Name == Name)
                        {
                            _characterFromLocal = localMember;
                            return _characterFromLocal;
                        }
                    }

                    return null;
                }

                return null;
            }
        }

        public bool SendDronesToAssist(long CharacterIdToAssistTo)
        {
            if (!DirectEve.Session.InFleet)
                return false;

            if (DirectEve.ActiveDrones.Count == 0)
                return false;

            List<long> droneIds = new List<long>();
            foreach (DirectEntity activeDrone in DirectEve.ActiveDrones)
            {
                droneIds.Add(activeDrone.Id);
            }

            PyObject AssistDrone = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.droneFunctions").Attribute("Assist");

            if (AssistDrone.IsValid)
            {
                if (!DirectEve.Interval(1000, 3000))
                    return false;

                return DirectEve.ThreadedCall(AssistDrone, CharacterIdToAssistTo, droneIds);
            }

            return false;
        }

        public bool SendDronesToAssist()
        {
            if (!DirectEve.Session.InFleet)
                return false;

            if (DirectEve.ActiveDrones.Count == 0)
                return false;

            if (CharacterFromLocal == null)
                return false;

            List<long> droneIds = new List<long>();
            foreach (DirectEntity activeDrone in DirectEve.ActiveDrones)
            {
                droneIds.Add(activeDrone.Id);
            }

            PyObject AssistDrone = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.droneFunctions").Attribute("Assist");

            if (AssistDrone.IsValid)
            {
                if (!DirectEve.Interval(1000, 3000))
                    return false;

                return DirectEve.ThreadedCall(AssistDrone, CharacterFromLocal.CharacterId, droneIds);
            }

            return false;
        }

        public bool ShieldArmorHullAllAt0
        {
            get
            {
                try
                {
                    if (IsTarget && (_shieldPct == null || _shieldPct == 0) && (_armorPct == null || _armorPct == 0) && (_structurePct == null || _structurePct == 0))
                    {
                        return true;
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

        public double ShieldPct
        {
            get
            {
                if (_shieldPct != null)
                    return (double)_shieldPct;

                if (!IsValid) return 0;

                if (!_shieldPct.HasValue)
                    GetDamageState();

                return _shieldPct ?? 0;
            }
        }

        public double StructurePct
        {
            get
            {
                if (_structurePct != null)
                    return (double)_structurePct;

                if (!IsValid) return 0;

                if (!_structurePct.HasValue)
                    GetDamageState();

                return _structurePct ?? 0;
            }
        }

        public double? StructureUnifirmity
        {
            get
            {
                if (_structureUniformity != null)
                    return (double)_structureUniformity;

                if (!IsValid) return 0;

                if (_structureUniformity == null)
                    _structureUniformity = (float)Ball.Attribute("structureUnifirmity");

                if (_structureUniformity != null && !double.IsNaN(_structureUniformity.Value) && !double.IsInfinity(_structureUniformity.Value))
                    return _structureUniformity.Value;

                return 0;
            }
        }

        public double? TrackingSpeed
        {
            get
            {
                if (!IsValid) return null;

                if (_trackingSpeed == null)
                    _trackingSpeed = (float)Ball.Attribute("trackingSpeed");

                if (_trackingSpeed != null && !double.IsNaN(_trackingSpeed.Value) && !double.IsInfinity(_trackingSpeed.Value))
                    return _trackingSpeed.Value;

                return 0;
            }
        }

        public double? TransversalVelocity
        {
            get
            {
                if (!IsValid) return 0;
                if (!IsNpc) return 0;

                if (_transversalVelocity == null)
                {
                    DirectEntity myBall = DirectEve.ActiveShip.Entity;
                    List<double> CombinedVelocity = new List<double> { Vx - myBall.Vx, Vy - myBall.Vy, Vz - myBall.Vz };
                    if (XCoordinate != null && YCoordinate != null && ZCoordinate != null)
                    {
                        List<double> Radius_ = new List<double> { (double)XCoordinate - (double)myBall.XCoordinate, (double)YCoordinate - (double)myBall.YCoordinate, (double)ZCoordinate - (double)myBall.ZCoordinate };
                        List<double> RadiusVectorNormalized = Radius_.Select(i => i / Math.Sqrt((Radius_[0] * Radius_[0]) + (Radius_[1] * Radius_[1]) + (Radius_[2] * Radius_[2])))
                            .ToList();
                        double RadialVelocity = (CombinedVelocity[0] * RadiusVectorNormalized[0]) + (CombinedVelocity[1] * RadiusVectorNormalized[1]) +
                                                (CombinedVelocity[2] * RadiusVectorNormalized[2]);
                        List<double> ScaledRadiusVector = RadiusVectorNormalized.Select(i => i * RadialVelocity).ToList();
                        _transversalVelocity =
                            Math.Sqrt(((CombinedVelocity[0] - ScaledRadiusVector[0]) * (CombinedVelocity[0] - ScaledRadiusVector[0])) +
                                      ((CombinedVelocity[1] - ScaledRadiusVector[1]) * (CombinedVelocity[1] - ScaledRadiusVector[1])) +
                                      ((CombinedVelocity[2] - ScaledRadiusVector[2]) * (CombinedVelocity[2] - ScaledRadiusVector[2])));
                        return _transversalVelocity.Value;
                    }

                    return null;
                }

                return _transversalVelocity.Value;
            }
        }

        public double? TriglavianDamage
        {
            get
            {
                if (!IsValid) return 0;

                if (_triglavianDamage == null)
                    _triglavianDamage = (float)Ball.Attribute("damage");

                if (_triglavianDamage.HasValue)
                    return _triglavianDamage;

                return null;
            }
        }

        public double? TriglavianDPS
        {
            get
            {
                if (!IsValid) return 0;

                if (TriglavianDamage != null && TriglavianDamage != 0)
                    _triglavianDps = TriglavianDamage / 5; //Where can we retrieve the accurate rate of fire for these guns?!

                if (_triglavianDps > 0)
                    return _triglavianDps;

                return 0;
            }
        }

        public Vec3? PositionInSpace
        {
            get
            {
                if (IsXYZCoordValid)
                {
                    return new Vec3((double)XCoordinate, (double)YCoordinate, (double)ZCoordinate);
                }

                return null;
            }
        }

        public double Velocity
        {
            get
            {
                if (!IsValid) return 0;

                if (_velocity == null)
                {
                    _velocity = (double)Ball.Call("GetVectorDotAt", PySharp.Import("blue").Attribute("os").Call("GetSimTime")).Call("Length");

                    if (Id == DirectEve.ActiveShip.ItemId)
                    {
                        DirectEve.ActiveShip.AdjustSpeedIfGoingTooFast(_velocity.Value);
                    }
                }

                return Math.Round(_velocity.Value, 0);
            }
        }

        /// <summary>
        /// This is the current direction vector of the ship
        /// </summary>
        /// <returns></returns>
        public Vec3? GetDirectionVector()
        {
            var simTime = PySharp.Import("blue").Attribute("os").Call("GetSimTime");
            if (simTime.IsValid)
            {
                var ret = _ball.Call("GetQuaternionAt", simTime);
                if (ret.IsValid)
                {
                    Quaternion q = new Quaternion(ret["x"].ToFloat(), ret["y"].ToFloat(), ret["z"].ToFloat(), ret["w"].ToFloat());
                    Vec3 v3 = new Vec3(0, 0, 1);
                    var res = q * v3;
                    return res;
                }
            }
            return null;
        }

        public void SetDisplayName(string html)
        {
            var bracket = ESCache.Instance.DirectEve.PySharp.Import("__builtin__")["sm"]["services"].DictionaryItem("bracket")["brackets"].DictionaryItem(Id);
            if (bracket.IsValid)
            {
                bracket.SetAttribute<string>("_displayName", html);
                //var name = bracket["_displayName"].ToUnicodeString();
                //bracket.SetAttribute<bool>("overrideLabel", false);
                //bracket.Call("SetOrder", 0);
                //bracket.Call("ShowLabel");
                //Log($"DisplayName [{bracket.LogObject()}]");
                //bracket.SetAttribute<bool>("overrideLabel", true);
                //bracket.SetAttribute<bool>("showLabel", true);
            }
        }

        /// <summary>
        /// This is the directional vector the ship aligns to
        /// </summary>
        /// <returns></returns>
        public Vec3 GetDirectionVectorFinal()
        {
            var x = this.GotoX - (double)XCoordinate;
            var y = this.GotoY - (double)YCoordinate;
            var z = this.GotoZ - (double)ZCoordinate;
            return new Vec3(x, y, z).Normalize();
        }

        private double Vx
        {
            get
            {
                if (!IsValid) return 0;

                if (!_vx.HasValue)
                    _vx = (double)Ball.Attribute("vx");

                return _vx.Value;
            }
        }

        private double Vy
        {
            get
            {
                if (!IsValid) return 0;

                if (!_vy.HasValue)
                    _vy = (double)Ball.Attribute("vy");

                return _vy.Value;
            }
        }

        private double Vz
        {
            get
            {
                if (!IsValid) return 0;

                if (!_vz.HasValue)
                    _vz = (double)Ball.Attribute("vz");

                return _vz.Value;
            }
        }

        public double WarpScrambleChance
        {
            get
            {
                if (!IsValid) return 0;

                if (!_warpScrambleChance.HasValue)
                    _warpScrambleChance = (float)Ball.Attribute("entityWarpScrambleChance");

                if (_warpScrambleChance != null && !double.IsNaN(_warpScrambleChance.Value) && !double.IsInfinity(_warpScrambleChance.Value))
                    return _warpScrambleChance.Value;

                return 0;
            }
        }

        private bool? WeAreAboveThisEntity
        {
            get
            {
                if (!IsValid) return false;

                if (DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.Id == Id) //this is US return null
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition.XCoordinate > DirectAbsolutePosition.XCoordinate)
                    return true;

                return false;
            }
        }

        private bool? WeAreBelowThisEntity
        {
            get
            {
                if (!IsValid) return false;

                if (DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.Id == Id) //this is US return null
                    return null;

                if (DirectEve.ActiveShip.Entity.XCoordinate > DirectAbsolutePosition.XCoordinate)
                    return false;

                return true;
            }
        }

        private bool? WeAreEastOfThisEntity
        {
            get
            {
                if (!IsValid) return false;

                if (DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.Id == Id) //this is US return null
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition.YCoordinate > DirectAbsolutePosition.YCoordinate)
                    return true;

                return false;
            }
        }

        public bool? WeAreNorthOfThisEntity
        {
            get
            {
                if (!IsValid) return false;

                if (DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.Id == Id) //this is US return null
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition.ZCoordinate > DirectAbsolutePosition.ZCoordinate)
                    return true;

                return false;
            }
        }

        public bool? WeAreSouthOfThisEntity
        {
            get
            {
                if (!IsValid) return false;

                if (DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.Id == Id) //this is US return null
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition.ZCoordinate > DirectAbsolutePosition.ZCoordinate)
                    return false;

                return true;
            }
        }

        private bool? WeAreWestOfThisEntity
        {
            get
            {
                if (!IsValid) return false;

                if (DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition == null)
                    return null;

                if (DirectEve.ActiveShip.Entity.DirectAbsolutePosition.YCoordinate > DirectAbsolutePosition.YCoordinate)
                    return false;

                return true;
            }
        }

        public double? WormholeAge
        {
            get
            {
                if (_wormholeAge == null)
                    _wormholeAge = (double)slimItem.Attribute("wormholeAge");

                return _wormholeAge.Value;
            }
        }

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
        public double? WormholeSize
        {
            get
            {
                if (_wormholeSize == null)
                    _wormholeSize = (double)slimItem.Attribute("wormholeSize");

                return _wormholeSize.Value;
            }
        }

        public bool IsXYZCoordValid
        {
            get
            {
                if (DirectEve.Session.IsAbyssalDeadspace)
                    return true;

                if (XCoordinate == null || !XCoordinate.HasValue || XCoordinate == 0)
                    return false;

                if (YCoordinate == null || !YCoordinate.HasValue || YCoordinate == 0)
                    return false;

                if (ZCoordinate == null || !ZCoordinate.HasValue || ZCoordinate == 0)
                    return false;

                return true;
            }
        }

        public double? XCoordinate
        {
            get
            {
                if (!_x.HasValue)
                {
                    if (!IsValid)
                    {
                        if (DebugConfig.DebugWspaceSiteBehavior)
                        {
                            if (TypeId == (int)TypeID.Venture)
                            {
                                Log.WriteLine("XCoordinate: if (!IsValid)");
                                Log.WriteLine("InfoGatheredTimeStamp: [" + InfoGatheredTimeStamp + "]");
                                Log.WriteLine("Start Ball.LogObject()");
                                Log.WriteLine($"{Ball.LogObject()}");
                                Log.WriteLine("Done Ball.LogObject()");
                                //Log.WriteLine("Start _slimItem.LogObject()");
                                //Log.WriteLine($"{_slimItem.LogObject()}");
                                //Log.WriteLine("Done _slimItem.LogObject()");
                            }
                        }

                        return null;
                    }

                    _x = (double)Ball.Attribute("x");
                }

                if (_x == 0)
                {
                    if (DirectEve.DictEntitiesXPositionInfoCachedAcrossFrames.ContainsKey(Id))
                    {
                        double tempXPositionInSpace = DirectEve.DictEntitiesXPositionInfoCachedAcrossFrames[Id];
                        _x = tempXPositionInSpace;
                        return _x;
                    }

                    return null;
                }

                /**
                if (DebugConfig.DebugWspaceSiteBehavior)
                {
                    if (TypeId == (int)TypeID.Venture)
                    {
                        Log.WriteLine("Start Ball.LogObject()");
                        Log.WriteLine($"{Ball.LogObject()}");
                        Log.WriteLine("Done Ball.LogObject()");
                        Log.WriteLine("Start _slimItem.LogObject()");
                        Log.WriteLine($"{_slimItem.LogObject()}");
                        Log.WriteLine("Done _slimItem.LogObject()");
                    }
                }
                **/
                if (_x.HasValue) DirectEve.DictEntitiesXPositionInfoCachedAcrossFrames.AddOrUpdate(Id, (double)_x);
                return _x ?? null;
            }
        }

        public double? YCoordinate
        {
            get
            {


                if (!_y.HasValue)
                {
                    if (!IsValid)
                    {
                        if (DebugConfig.DebugWspaceSiteBehavior)
                        {
                            if (TypeId == (int)TypeID.Venture)
                            {
                                Log.WriteLine("YCoordinate: if (!IsValid) return null;");
                            }
                        }
                        return null;
                    }

                    _y = (double)Ball.Attribute("y");
                }

                if (_y == 0)
                {
                    if (DirectEve.DictEntitiesYPositionInfoCachedAcrossFrames.ContainsKey(Id))
                    {
                        double tempPositionInSpace = DirectEve.DictEntitiesYPositionInfoCachedAcrossFrames[Id];
                        _y = tempPositionInSpace;
                        return _y;
                    }

                    return null;
                }

                if (_y.HasValue) DirectEve.DictEntitiesYPositionInfoCachedAcrossFrames.AddOrUpdate(Id, (double)_y);
                return _y ?? null;
            }
        }

        public double? ZCoordinate
        {
            get
            {
                if (!_z.HasValue)
                {
                    if (!IsValid)
                    {
                        if (DebugConfig.DebugWspaceSiteBehavior)
                        {
                            if (TypeId == (int)TypeID.Venture)
                            {
                                Log.WriteLine("ZCoordinate: if (!IsValid) return null;");
                            }
                        }
                        return null;
                    }

                    _z = (double)Ball.Attribute("z");
                }

                if (_z == 0)
                {
                    if (DirectEve.DictEntitiesZPositionInfoCachedAcrossFrames.ContainsKey(Id))
                    {
                        double tempPositionInSpace = DirectEve.DictEntitiesZPositionInfoCachedAcrossFrames[Id];
                        _z = tempPositionInSpace;
                        return _y;
                    }

                    return null;
                }

                if (_z.HasValue) DirectEve.DictEntitiesZPositionInfoCachedAcrossFrames.AddOrUpdate(Id, (double)_z);
                return _z ?? null;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Abandons all wrecks. Make sure to only call this on a wreck.
        /// </summary>
        /// <returns>false if entity is not a wreck</returns>
        public bool AbandonAllWrecks()
        {
            if (GroupId != (int)DirectEve.Const.GroupWreck)
                return false;

            var AbandonAllLoot = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("AbandonAllLoot");

            if (AbandonAllLoot.IsValid)
            {
                if (!DirectEve.Interval(5000, 7000))
                    return false;

                return DirectEve.ThreadedCall(AbandonAllLoot, Id);
            }

            return false;
        }

        /// <summary>
        ///     Activate (Acceleration Gates only)
        /// </summary>
        /// <returns></returns>
        public bool Activate()
        {
            if (!RecursionCheck(nameof(Activate)))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (GroupId != (int)Group.AccelerationGate)
            {
                Log.WriteLine("Activation failed! [" + Name + "] GroupId [" + GroupId + "] TypeId [" + TypeId + "][" + TypeName + "] - this is not an acceleration gate!");
                return false;
            }

            if (DirectEve.ActiveShip.Entity.IsWarpingByMode)
                return false;

            if (Distance > (double)Distances.CloseToGateActivationRange)
                return false;

            if (DirectEve.ActiveShip.IsImmobile)
                return false;

            if (DirectEve.ActiveShip.IsScrambled)
                return false;

            DirectEve.ActiveShip.AdjustSpeedIfGoingTooFast(DirectEve.ActiveShip.Entity.Velocity);

            PyObject DockOrJumpOrActivateGate = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                .Attribute("DockOrJumpOrActivateGate");

            if (DockOrJumpOrActivateGate.IsValid)
            {
                if (!DirectEve.Interval(15000, 16000, Id.ToString()))
                    return false;

                if (DirectEve.ThreadedCall(DockOrJumpOrActivateGate, Id))
                {
                    Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                    Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(16);
                    Time.Instance.LastActivateAccelerationGate = DateTime.UtcNow;
                    ESCache.Instance.ClearPerPocketCache();
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Activating [" + Name + "]"));
                    DirectSession.SetSessionNextSessionReady();
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Activate Abyssal Gate
        /// </summary>
        /// <returns></returns>
        public bool ActivateAbyssalAccelerationGate()
        {
            //if (!RecursionCheck(nameof(ActivateAbyssalAccelerationGate)))
            //    return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (TypeId != (int)TypeID.AbyssEncounterGate)
            {
                Log.WriteLine("ActivateAbyssalAccelerationGate: [" + Name + "] TypeId [" + TypeId + "] was expected to be [" + TypeID.AbyssEncounterGate + "]: Failed");
                return false;
            }

            if (Distance > (double)Distances.CloseToGateActivationRange)
                return false;

            DirectEve.ActiveShip.AdjustSpeedIfGoingTooFast(DirectEve.ActiveShip.Entity.Velocity);

            var call = DirectEve.GetLocalSvc("menu")["ActivateAbyssalAccelerationGate"];
            if (call.IsValid)
            {
                if (!DirectEve.Interval(15000, 16000))
                    return false;

                if (DirectEve.ThreadedLocalSvcCall("menu", "ActivateAbyssalAccelerationGate", Id))
                {
                    Log.WriteLine("ActivateAbyssalAccelerationGate: Jumping to next abyssal pocket");
                    Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                    Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(16);
                    Time.Instance.LastActivateAccelerationGate = DateTime.UtcNow;
                    Time.Instance.NextJumpAction = DateTime.UtcNow.AddSeconds(13);
                    ESCache.Instance.ClearPerSystemCache();
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping through [" + Name + "]"));
                    DirectSession.SetSessionNextSessionReady(7000, 9000);
                    return true;
                }

                return false;
            }

            Log.WriteLine("ActivateAbyssalAccelerationGate: failed");
            return false;
        }

        /// <summary>
        ///     Activate Abyssal Gate
        /// </summary>
        /// <returns></returns>
        public bool ActivateAbyssalEndGate()
        {
            if (!RecursionCheck(nameof(ActivateAbyssalEndGate)))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (TypeId != (int)TypeID.AbyssExitGate)
            {
                Log.WriteLine("ActivateAbyssalEndGate: Name [" + Name + "] TypeId [" + TypeId + "] was expected to be [" + TypeID.AbyssExitGate + "]: Failed");
                return false;
            }

            if (Distance > (double)Distances.CloseToGateActivationRange)
                return false;

            DirectEve.ActiveShip.AdjustSpeedIfGoingTooFast(DirectEve.ActiveShip.Entity.Velocity);

            if (!DirectEve.Interval(15000, 16000, Id.ToString()))
                return false;

            var call = DirectEve.GetLocalSvc("menu")["ActivateAbyssalEndGate"];
            if (call.IsValid)
            {
                if (DirectEve.ThreadedLocalSvcCall("menu", "ActivateAbyssalEndGate", Id))
                {
                    Log.WriteLine("ActivateAbyssalEndGate: Jumping back into regular space");
                    Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                    Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(16);
                    Time.Instance.LastActivateAccelerationGate = DateTime.UtcNow;
                    Time.Instance.NextJumpAction = DateTime.UtcNow.AddSeconds(13);
                    ESCache.Instance.ClearPerSystemCache();
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping through [" + Name + "] EndGate"));
                    DirectSession.SetSessionNextSessionReady(7000, 9000);
                    return true;
                }

                return false;
            }

            Log.WriteLine("ActivateAbyssalEndGate: failed");
            return false;
        }

        public bool ActivateVoidSpaceEndGate()
        {
            if (!RecursionCheck(nameof(ActivateVoidSpaceEndGate)))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (TypeId != (int)TypeID.VoidSpaceExitGate)
            {
                Log.WriteLine("ActivateVoidSpaceEndGate: Name [" + Name + "] TypeId [" + TypeId + "] was expected to be [" + TypeID.VoidSpaceExitGate + "]: Failed");
                return false;
            }

            if (Distance > (double)Distances.CloseToGateActivationRange)
                return false;

            DirectEve.ActiveShip.AdjustSpeedIfGoingTooFast(DirectEve.ActiveShip.Entity.Velocity);

            if (!DirectEve.Interval(15000, 16000, Id.ToString()))
                return false;

            var call = DirectEve.GetLocalSvc("menu")["ActivateVoidSpaceEndGate"];
            if (call.IsValid)
            {
                if (DirectEve.ThreadedLocalSvcCall("menu", "ActivateVoidSpaceEndGate", Id))
                {
                    Log.WriteLine("ActivateVoidSpaceEndGate: Jumping back into regular space");
                    Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                    Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(16);
                    Time.Instance.LastActivateAccelerationGate = DateTime.UtcNow;
                    Time.Instance.NextJumpAction = DateTime.UtcNow.AddSeconds(13);
                    ESCache.Instance.ClearPerSystemCache();
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping through [" + Name + "] EndGate"));
                    DirectSession.SetSessionNextSessionReady(7000, 9000);
                    return true;
                }

                return false;
            }

            Log.WriteLine("ActivateAbyssalEndGate: failed");
            return false;
        }

        public static bool IsSpotWithinAbyssalBounds(DirectWorldPosition p, long offset = 0)
        {
            try
            {
                if (!ESCache.Instance.DirectEve.Me.IsInAbyssalSpace())
                    return false;

                if (offset == 0)
                {
                    if (ESCache.Instance.AbyssalCenter != null)
                    {
                        if (ESCache.Instance.AbyssalCenter._directEntity != null)
                        {
                            if (ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition != null)
                            {
                                return ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <= DirectEntity.AbyssBoundarySizeSquared;
                            }
                            else Log.WriteLine("if (ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition == null)");
                        }
                        else Log.WriteLine("if (ESCache.Instance.AbyssalCenter._directEntity == null)");
                    }
                    else Log.WriteLine("if (ESCache.Instance.AbyssalCenter == null)");
                }

                if (ESCache.Instance.AbyssalCenter != null)
                {
                    return ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <= (DirectEntity.AbyssBoundarySize + offset) * (DirectEntity.AbyssBoundarySize + offset);
                }
                else Log.WriteLine("if (ESCache.Instance.AbyssalCenter == null)!!");

                Log.WriteLine("returning true?!?)");
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public bool IsWithinAbyssBounds(int offset = 0) => IsSpotWithinAbyssalBounds(DirectAbsolutePosition, offset);

        //
        // Abyssal Frigate fleet gate?
        //
        public bool ActivateAbyssalEntranceAccelerationGate()
        {
            if (!RecursionCheck(nameof(ActivateAbyssalEntranceAccelerationGate)))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (BracketType != BracketType.Warp_Gate || !Name.ToLower().Contains("abyssal".ToLower()))
                return false;

            if (Distance > (double)Distances.CloseToGateActivationRange)
                return false;

            //DirectEve.ActiveShip.AdjustSpeedIfGoingTooFast(DirectEve.ActiveShip.Entity.Velocity);

            var call = DirectEve.GetLocalSvc("menu")["ActivateAbyssalEntranceAccelerationGate"];
            if (call.IsValid)
            {
                if (!DirectEve.Interval(6000, 7000, Id.ToString()))
                    return false;

                if (DirectEve.ThreadedLocalSvcCall("menu", "ActivateAbyssalEntranceAccelerationGate", Id))
                {
                    if (DirectEve.ActiveShip.IsCruiser)
                    {
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping through [" + Name + "]"));
                        DirectSession.SetSessionNextSessionReady(7000, 9000);
                        ESCache.Instance.ClearPerSystemCache();
                        return true;
                    }

                    return true;
                }

                return false;
            }

            return false;
        }

        public bool ActivateAbyssalPvPGate()
        {
            if (!RecursionCheck(nameof(ActivateAbyssalPvPGate)))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (TypeId != (int)TypeID.AbyssPvPGate)
                return false;

            if (Distance > (double)Distances.CloseToGateActivationRange)
                return false;

            DirectEve.ActiveShip.AdjustSpeedIfGoingTooFast(DirectEve.ActiveShip.Entity.Velocity);

            var call = DirectEve.GetLocalSvc("menu")["ActivateAbyssalPvPGate"];
            if (call.IsValid)
            {
                if (!DirectEve.Interval(15000, 16000, Id.ToString()))
                    return false;

                if (DirectEve.ThreadedLocalSvcCall("menu", "ActivateAbyssalPvPGate", Id))
                {
                    ESCache.Instance.ClearPerSystemCache();
                    Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                    Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(16);
                    Time.Instance.LastActivateAccelerationGate = DateTime.UtcNow;
                    Time.Instance.NextJumpAction = DateTime.UtcNow.AddSeconds(13);
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping through [" + Name + "] PVPGate"));
                    DirectSession.SetSessionNextSessionReady();
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Warp to target
        /// </summary>
        /// <returns></returns>
        public bool AlignTo()
        {
            if (!RecursionCheck(nameof(AlignTo)))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (DirectEve.ActiveShip.Entity.Id == Id)
                return false;

            if (DirectEve.ActiveShip.Entity.IsWarpingByMode)
                return false;

            var call = DirectEve.GetLocalSvc("menu")["AlignTo"];

            if (call.IsValid)
            {
                if (!IntervalForMovementCommands(4000, 6000, Id.ToString()))
                    return false;

                if (DirectEve.ThreadedLocalSvcCall("menu", "AlignTo", Id))
                {
                    Log.WriteLine("AlignTo [" + TypeName + "] called: We are in a [" + ESCache.Instance.ActiveShip.Entity.TypeName + "]");
                }
            }

            return false;
        }

        public bool AnchorObject()
        {
            if (!DirectEve.Session.IsInSpace)
                return false;

            if (Distance >= 5000)
                return false;

            if (!IsValid)
                return false;

            if (!IsAnchorableObject)
                return false;

            if (!DirectEve.Interval(4000, 6000))
                return false;

            var call = DirectEve.GetLocalSvc("menu")["AnchorObject"];
            if (call.IsValid)
            {
                if (DirectEve.ThreadedLocalSvcCall("menu", "AnchorObject", Id))
                    return true;
            }

            return false;
        }

        public bool AnchorStructure()
        {
            if (!DirectEve.Session.IsInSpace)
                return false;

            if (Distance >= 5000)
                return false;

            if (!IsValid)
                return false;

            if (!IsAnchorableStructure)
                return false;

            var call = DirectEve.GetLocalSvc("menu")["AnchorStructure"];

            if (call.IsValid)
            {
                if (!DirectEve.Interval(4000, 6000))
                    return false;

                if (DirectEve.ThreadedLocalSvcCall("menu", "AnchorStructure", Id))
                    return true;
            }

            return false;
        }

        public bool? _isAbyssalBioAdaptiveCache;

        public bool IsAbyssalBioAdaptiveCache
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsAbyssalBioAdaptiveCache)))
                        return false;

                    if (IsValid)
                    {
                        if (_isAbyssalBioAdaptiveCache == null)
                        {
                            bool result = false;
                            result |= GroupId == (int)Group.AbyssalBioAdaptiveCache && ESCache.Instance.InAbyssalDeadspace;
                            _isAbyssalBioAdaptiveCache = result;
                            if (DebugConfig.DebugEntityCache)
                                Log.WriteLine("Adding [" + Name + "] to EntityIsAbyssalDeadspaceAoeTowerWeapon as [" + _isAbyssalBioAdaptiveCache + "]");
                            return (bool)_isAbyssalBioAdaptiveCache;
                        }

                        return _isAbyssalBioAdaptiveCache ?? false;
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

        /// <summary>
        ///     Approach target
        /// </summary>
        /// <returns></returns>
        public bool Approach()
        {
            if (!RecursionCheck("Approach"))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (DirectEve.ActiveShip.Entity.Id == Id)
                return false;

            if (DirectEve.ActiveShip.Entity.IsWarpingByMode)
                return false;

            if (Distance > (double)Distances.OnGridWithMe)
                return false;

            //if (IsApproachedOrKeptAtRangeByActiveShip)
            //    return true;

            if (DirectEve.ActiveShip.IsImmobile)
                return false;

            DirectEve.ActiveShip.AdjustSpeedIfGoingTooFast(DirectEve.ActiveShip.Entity.Velocity);

            var Approach = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions").Attribute("Approach");
            if (Approach.IsValid)
            {
                if (!IntervalForMovementCommands(3000, 6000, Id.ToString()))
                    return false;

                if (DirectEve.ThreadedCall(Approach, Id))
                {
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Board this ship
        /// </summary>
        /// <returns>false if entity is player or out of range</returns>
        public bool BoardShip()
        {
            if (!IsValid)
                return false;

            if (IsPlayer)
                return false;

            if (Distance > 6500)
                return false;

            var Board = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("Board");

            if (Board.IsValid)
            {
                if (!DirectEve.Interval(4000, 6000))
                    return false;

                return DirectEve.ThreadedCall(Board, Id);
            }

            return false;
        }

        /**
        public double? NpcArmorResistanceEm
        {
            get
            {
                if (!IsNpc) return 0;

                if (!_npcArmorResistanceEm.HasValue)
                    _npcArmorResistanceEm = Math.Round(1.0d - (int) Ball.Attribute("armorEmDamageResonance"), 2);

                return _npcArmorResistanceEm.Value;
            }
        }

        public double? NpcArmorResistanceExplosive
        {
            get
            {
                if (!IsNpc) return 0;

                if (!_npcArmorResistanceExplosion.HasValue)
                    _npcArmorResistanceExplosion = Math.Round(1.0d - (int) Ball.Attribute("armorExplosiveDamageResonance"), 2);

                return _npcArmorResistanceExplosion.Value;
            }
        }

        public double? NpcArmorResistanceKinetic
        {
            get
            {
                if (!IsNpc) return 0;

                if (!_npcArmorResistanceKinetic.HasValue)
                    _npcArmorResistanceKinetic = Math.Round(1.0d - (float) Ball.Attribute("armorKineticDamageResonance"), 2);

                return _npcArmorResistanceKinetic;
            }
        }

        public double? NpcArmorResistanceThermal
        {
            get
            {
                if (!IsNpc) return 0;

                if (!_npcArmorResistanceThermal.HasValue)
                    _npcArmorResistanceThermal = Math.Round(1.0d - (float) Ball.Attribute("armorThermalDamageResonance"), 2);

                return _npcArmorResistanceThermal;
            }
        }
        **/
        /**
        public double? NpcShieldResistanceEM
        {
            get
            {
                if (!IsNpc) return 0;

                if (!_npcShieldResistanceEm.HasValue)
                    _npcShieldResistanceEm = Math.Round(1.0d - (float) Ball.Attribute("shieldEmDamageResonance"), 2);
                return _npcShieldResistanceEm;
            }
        }

        public double? NpcShieldResistanceExplosive
        {
            get
            {
                if (!IsNpc) return 0;

                if (!_npcShieldResistanceExplosive.HasValue)
                    _npcShieldResistanceExplosive = Math.Round(1.0d - (double) Ball.Attribute("shieldExplosiveDamageResonance"), 2);
                return _npcShieldResistanceExplosive;
            }
        }

        public double? NpcShieldResistanceKinetic
        {
            get
            {
                if (!IsNpc) return 0;

                if (!_npcShieldResistanceKinetic.HasValue)
                    _npcShieldResistanceKinetic = Math.Round(1.0d - (double) Ball.Attribute("shieldKineticDamageResonance"), 2);
                return _npcShieldResistanceKinetic;
            }
        }

        public double? NpcShieldResistanceThermal
        {
            get
            {
                if (!IsNpc) return 0;

                if (!_npcShieldResistanceThermal.HasValue)
                    _npcShieldResistanceThermal = Math.Round(1.0d - (double) Ball.Attribute("shieldThermalDamageResonance"), 2);
                return _npcShieldResistanceThermal;
            }
        }
        **/
        /**
        public double? EntityFactionLoss
        {
            get
            {
                if (!IsNpc) return 0;

                if (_entityFactionLoss == null)
                    _entityFactionLoss = (float)Ball.Attribute("entityFactionLoss");

                return _entityFactionLoss.Value;
            }
        }
        **/
        /// <summary>
        ///     Warp to target and dock
        /// </summary>
        /// <returns></returns>
        public bool Dock()
        {
            if (!RecursionCheck(nameof(Dock)))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (GroupId != (int)Group.Station && GroupId != (int)Group.Citadel)
                return false;

            if (DirectEve.ActiveShip.Entity.IsWarpingByMode)
                return false;

            if (Distance > (double)Distances.OnGridWithMe)
                return false;

            //if (DirectEve.ActiveShip.IsImmobile) //Can we dock in bastion?
            //    return false;

            if (DirectEve.Me.WeaponsTimerExists)
                return false;

            PyObject DockOrJumpOrActivateGate = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                .Attribute("DockOrJumpOrActivateGate");

            if (DockOrJumpOrActivateGate.IsValid)
            {
                if (!DirectEve.Interval(7000, 11000))
                    return false;

                if (DirectEve.ThreadedCall(DockOrJumpOrActivateGate, Id))
                {
                    Time.Instance.NextDockAction = DateTime.UtcNow.AddSeconds(Time.Instance.DockingDelay_seconds);
                    Time.Instance.LastDockAction = DateTime.UtcNow;
                    ESCache.Instance.ClearPerPocketCache();
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Docking [" + Name + "]"));
                    DirectSession.SetSessionNextSessionReady(10000, 11000);
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                    return true;
                }

                return false;
            }

            return false;
        }

        public double GetBounty()
        {
            var bountyRow = DirectEve.GetLocalSvc("godma")
                .Call("GetType", TypeId)
                .Attribute("displayAttributes")
                .ToList()
                .Find(i => i.Attribute("attributeID").ToInt() == (int)DirectEve.Const.AttributeEntityKillBounty);
            if (bountyRow == null || !bountyRow.IsValid)
                return 0;

            return (double)bountyRow.Attribute("value");
        }

        public string GetResistInfo()
        {
            return $"Name [{Name}] ShieldHitPoints [{MaxShield}] " +
                   $" ArmorHitPoints [{MaxArmor}]" +
                   $" StructureHitPoints[{MaxStructure}]" +
                   $" Shield-Res-EM/EXP/KIN/TRM [{ShieldResistanceEM}%," +
                   $" {ShieldResistanceExplosive}%," +
                   $" {ShieldResistanceKinetic}%," +
                   $" {ShieldResistanceThermal}%]" +
                   $" Armor-Res-EM/EXP/KIN/TRM [{ArmorResistanceEM}%," +
                   $" {ArmorResistanceExplosive}%," +
                   $" {ArmorResistanceKinetic}%," +
                   $" {ArmorResistanceThermal}%]";
        }

        public bool IsInRangeOfWeapons
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsInRangeOfWeapons)))
                        return false;

                    if (IsValid)
                    {
                        if (Combat.MaxWeaponRange > 0)
                        {
                            if (Combat.MaxWeaponRange > Distance)
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

        /// <summary>
        ///     Jump (Stargates only)
        /// </summary>
        /// <returns></returns>
        public bool Jump()
        {
            if (!RecursionCheck(nameof(Jump)))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (Distance >= (double)Distances.JumpRange)
                return false;

            if (!IsValid)
                return false;

            if (DirectEve.Me.WeaponsTimerExists)
                return false;

            var DockOrJumpOrActivateGate = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                .Attribute("DockOrJumpOrActivateGate");

            if (DockOrJumpOrActivateGate.IsValid)
            {
                if (!DirectEve.Interval(4000, 6000))
                    return false;

                if (DirectEve.ThreadedCall(DockOrJumpOrActivateGate, Id))
                {
                    ESCache.Instance.ClearPerSystemCache();
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping into [" + Name + "]"));
                    DirectSession.SetSessionNextSessionReady();
                    //ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Jump Wormhole (Wormholes only)
        /// </summary>
        /// <returns></returns>
        public bool JumpWormhole()
        {
            if (!RecursionCheck(nameof(JumpWormhole)))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (Distance >= 5000)
                return false;

            if (!IsValid)
                return false;

            var call = DirectEve.GetLocalSvc("menu")["EnterWormhole"];
            if (call.IsValid)
            {
                if (!DirectEve.Interval(4000, 6000))
                    return false;

                if (DirectEve.ThreadedLocalSvcCall("menu", "EnterWormhole", Id))
                {
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping [" + Name + "] WormHole"));
                    DirectSession.SetSessionNextSessionReady();
                    //ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                    return true;
                }

                return false;
            }

            return false;
        }

        //private int? _prevKeepAtRangeDist;

        private bool? _isReadyToShoot;
        public bool IsReadyToShoot
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsReadyToShoot)))
                        return false;

                    if (IsValid)
                    {
                        if (_isReadyToShoot == null)
                        {
                            //if (IsDecloakedTransmissionRelay && .98 > ShieldPct && ESCache.Instance.ActiveShip.IsDread)
                            //{
                            //    UnlockTarget();
                            //    _isReadyToShoot = false;
                            //    return (bool)_isReadyToShoot;
                            //}

                            if (!HasExploded && IsTarget && IsInRangeOfWeapons && !IsWreck && !IsNPCDrone && !IsBadIdea && Id != ESCache.Instance.MyShipEntity.Id) //!IsIgnored
                            {
                                _isReadyToShoot = true;
                                return (bool)_isReadyToShoot;
                            }

                            _isReadyToShoot = false;
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

        private bool? _isReadyToTarget;

        public bool IsReadyToTarget
        {
            get
            {
                try
                {
                    if (!RecursionCheck(nameof(IsReadyToTarget)))
                        return false;

                    if (IsValid)
                    {
                        if (_isReadyToTarget == null)
                        {
                            if (!HasExploded && !IsTarget && !IsTargeting && (Distance < Combat.MaxTargetRange || Distance < 20000) && !IsWarpingByMode && Id != ESCache.Instance.MyShipEntity.Id)
                            {
                                if (IsDecloakedTransmissionRelay && .98 > ShieldPct)
                                {
                                    _isReadyToTarget = false;
                                    return (bool)_isReadyToTarget;
                                }

                                if (IsAbyssalDeadspaceTriglavianBioAdaptiveCache || IsAbyssalDeadspaceTriglavianExtractionNode)
                                {
                                    //if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship || i.BracketType == BracketType.NPC_Battleship) >= 5)
                                    //    return true;

                                    _isReadyToTarget = true;
                                    return (bool)_isReadyToTarget;
                                }

                                _isReadyToTarget = true;
                                return (bool)_isReadyToTarget;
                            }

                            _isReadyToTarget = false;
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

        public bool IsValidForKeepAtRange
        {
            get
            {
                if (GroupId == (int)Group.MiningDrone)
                    return false;

                if (GroupId == (int)Group.CargoContainer)
                    return false;

                if (GroupId == (int)Group.SecureContainer)
                    return false;

                if (GroupId == (int)Group.AuditLogSecureContainer)
                    return false;

                if (GroupId == (int)Group.Station)
                    return false;

                if (GroupId == (int)Group.Stargate)
                    return false;

                if (GroupId == (int)Group.FreightContainer)
                    return false;

                if (GroupId == (int)Group.Wreck)
                    return false;

                return true;
            }
        }

        /// <summary>
        ///     KeepAtRange target
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool KeepAtRange(int range, bool force = false)
        {
            if (!RecursionCheck("KeepAtRange"))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (!IsValidForKeepAtRange)
                return false;

            if (DirectEve.ActiveShip.Entity.Id == Id)
                return false;

            if (DirectEve.ActiveShip.Entity.IsWarpingByMode)
                return false;

            if (Distance > (double)Distances.OnGridWithMe)
                return false;

            if (DirectEve.ActiveShip.IsImmobile)
                return false;

            if (!SafeToInitiateMovementCommand(force))
                return false;

            if (IsApproachedOrKeptAtRangeByActiveShip && DirectEve.LastKeepAtRangeDistance.HasValue && DirectEve.LastKeepAtRangeDistance == range)
                return true;

            DirectEve.LastKeepAtRangeDistance = range;

            if (range < 50) // min keep at range
                range = 50;


            var KeepAtRange = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions").Attribute("KeepAtRange");
            if (KeepAtRange.IsValid)
            {
                if (!IntervalForMovementCommands(8000, 10000, Id.ToString() + range) && !force)
                    return false;

                Log.WriteLine("KeepAtRange called.");
                if (DirectEve.ThreadedCall(KeepAtRange, Id, range))
                {
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Lock target
        /// </summary>
        /// <returns></returns>
        public bool LockTarget()
        {
            if (!RecursionCheck(nameof(LockTarget)))
                return false;

            if (!IsValid) return false;

            // It's already a target!
            if (IsTarget || IsTargeting)
                return false;

            if (Distance > (double)Distances.OnGridWithMe)
                return false;

            // We can't target this entity yet!
            if (!DirectEve.CanTarget(Id))
                return false;

            // Set target timer
            DirectEve.SetTargetTimer(Id);

            var call = DirectEve.GetLocalSvc("menu")["LockTarget"];
            if (call.IsValid)
            {
                //if (!DirectEve.Interval(100, 200))
                //    return false;

                if (DirectEve.ThreadedLocalSvcCall("menu", "LockTarget", Id))
                {
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.LOCK_TARGET, "Targeting [" + TypeName + "] at [" + Math.Round(Distance / 1000, 0) + "k]"));
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Make this your active target
        /// </summary>
        /// <returns></returns>
        public bool MakeActiveTarget(bool threaded = true)
        {
            if (!RecursionCheck(nameof(MakeActiveTarget)))
                return false;

            if (!IsTarget)
                return false;

            if (DirectEve.IsTargetBeingRemoved(Id))
                return false;

            if (HasExploded)
                return false;

            if (HasReleased)
                return false;

            if (IsActiveTarget)
                return true;

            if (!IsValid)
                return false;

            // Even though we uthread the thing, expect it to be instant
            var currentActiveTarget = DirectEve.Entities.Find(t => t.IsActiveTarget);
            if (currentActiveTarget != null)
                currentActiveTarget.IsActiveTarget = false;

            if (!DirectEve.Interval(100, 200))
                return false;

            // Switch active targets
            if (threaded)
            {
                // Switch active targets
                var activeTarget = PySharp.Import("eve.client.script.parklife.states").Attribute("activeTarget");
                if (activeTarget.IsValid)
                {
                    IsActiveTarget = DirectEve.ThreadedLocalSvcCall("stateSvc", "SetState", Id, activeTarget, 1);
                    return IsActiveTarget;
                }

                return false;
            }
            else
            {
                // Switch active targets
                var activeTarget = PySharp.Import("eve.client.script.parklife.states").Attribute("activeTarget");
                var stateSvc = DirectEve.GetLocalSvc("stateSvc");
                if (activeTarget.IsValid && stateSvc.IsValid)
                {
                    stateSvc.Call("SetState", Id, activeTarget, 1);
                    IsActiveTarget = true;
                    return true;
                }

                return false;
            }
        }

        public bool MoveTo()
        {
            if (!RecursionCheck(nameof(MoveTo)))
                return false;

            if (!IsValid) return false;

            if (!DirectEve.Interval(4000, 6000, Id.ToString()))
                return false;

            if (!SafeToInitiateMovementCommand(false))
                return false;

            return DirectEve.ActiveShip.MoveTo(this);
        }

        /// <summary>
        ///     Open cargo window (only valid on containers in space, or own ship)
        /// </summary>
        /// <returns></returns>
        public bool OpenCargo()
        {
            if (IsValid)
            {
                var call = DirectEve.GetLocalSvc("menu")["OpenCargo"];
                if (call.IsValid)
                {
                    if (!DirectEve.Interval(1500, 2000))
                        return false;

                    if (DirectEve.ThreadedLocalSvcCall("menu", "OpenCargo", Id))
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }

            return false;
        }


        public bool Scoop()
        {
            if (GroupId == (int)Group.MobileTractor || GroupId == (int)Group.MobileDepot)
            {
                var call = DirectEve.GetLocalSvc("menu")["Scoop"];
                if (call.IsValid)
                {
                    if (!DirectEve.Interval(2000, 3000))
                        return false;

                    if (DirectEve.ThreadedCall(call, Id, TypeId))
                        return true;

                    return false;
                }

                return false;
            }

            Log.WriteLine("Couldnt launch for self. TypeName [" + Name + "] TypeID [" + TypeId + "] GroupID [" + GroupId + "]");
            return false;
        }



        /// <summary>
        ///     Orbit target at 5000m
        /// </summary>
        /// <returns></returns>
        public bool Orbit(bool Force = false)
        {
            if (!IsValid) return false;

            return Orbit(5000, Force);
        }

        public bool SafeToInitiateMovementCommand(bool Force)
        {
            try
            {
                if (Force) return true;

                if (DirectEve.Session.IsAbyssalDeadspace && Statistics.StartedPocket.AddSeconds(14) > DateTime.UtcNow)
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastActivateAbyssalActivationWindowAttempt.AddSeconds(10))
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInitiatedWarp.AddSeconds(6))
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.LastInitiatedWarp.AddSeconds(6)) waiting");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.LastAlign.AddSeconds(1))
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.LastAlign.AddSeconds(4)) waiting");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.LastApproachAction.AddSeconds(2))
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.LastApproachAction.AddSeconds(4)) waiting");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.NextActivateAccelerationGate)
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.NextActivateAction) waiting");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.NextOrbit.AddSeconds(-5))
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("SafeToInitiateMovementCommand: if (DateTime.UtcNow < Time.Instance.NextOrbit) waiting");
                    return false;
                }

                if (DirectEve.ActiveShip != null && DirectEve.ActiveShip.IsImmobile)
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("SafeToInitiateMovementCommand: f (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)");
                    return false;
                }

                if (DirectEve.Session.InJump)
                    return false;

                DirectEve.ActiveShip.AdjustSpeedIfGoingTooFast(DirectEve.ActiveShip.Entity.Velocity);

                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }


        //private int? _prevOrbitDist;

        /// <summary>
        ///     Orbit target
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool Orbit(int range, bool Force = false)
        {
            if (!RecursionCheck("Orbit"))
                return false;

            if (!IsValid) return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (DirectEve.ActiveShip.Entity.Id == Id)
                return false;

            if (DirectEve.ActiveShip.Entity.IsWarpingByMode)
                return false;

            if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(2))
                return false;

            if (DateTime.UtcNow < Time.Instance.LastJumpAction.AddSeconds(4))
                return false;

            if (DateTime.UtcNow < Time.Instance.LastActivateAbyssalActivationWindowAttempt.AddSeconds(10))
                return false;

            if (DateTime.UtcNow < Time.Instance.LastActivateKeyActivationWindowAttempt.AddSeconds(4))
                return false;

            if (Distance > (double)Distances.OnGridWithMe)
                return false;

            if (DirectEve.ActiveShip.IsImmobile)
                return false;

            if (!SafeToInitiateMovementCommand(false))
                return false;

            var Orbit = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions").Attribute("Orbit");
            if (Orbit.IsValid)
            {
                int minDelay = 1500;
                int maxDelay = 2000;
                if (!DirectEve.Session.IsAbyssalDeadspace)
                {
                    minDelay = 3000;
                    maxDelay = 4000;
                }

                if (!IntervalForMovementCommands(minDelay, maxDelay, "orbit" + Id.ToString() + range) && !Force)
                    return false;

                //if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("Orbit called [" + Name + "][" + Math.Round(Distance / 1000, 0) + "k] at [" + Math.Round((double)range / 1000, 0) + "k][" + Id + "]!.!");
                if (DirectEve.ThreadedCall(Orbit, Id, range))
                {
                    Log.WriteLine("Orbit [" + Name + "][" + Math.Round(Distance / 1000, 1) + "k] at [" + Math.Round((double)range, 0) + "m] MaskedID [" + MaskedId + "] success");
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                }
            }

            return false;
        }

        public string MaskedId
        {
            get
            {
                try
                {
                    if (IsValid)
                    {
                        int numofCharacters = Id.ToString(CultureInfo.InvariantCulture).Length;
                        if (numofCharacters >= 5)
                        {
                            string maskedID = Id.ToString(CultureInfo.InvariantCulture).Substring(numofCharacters - 4);
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

        public static bool IntervalForMovementCommands(int delayMs, int delayMsMax = 0, string uniqueName = null, bool IgnoreCurrentFrameExec = false, [CallerLineNumber] int ln = 0, [CallerMemberName] string caller = null, [CallerFilePath] string callerFilePath = null)
        {
            caller = uniqueName ?? caller + ln.ToString() + callerFilePath;
            if (caller == uniqueName)
                caller = uniqueName + ln.ToString() + callerFilePath;

            if (!DirectEve.HasFrameChanged(caller))
                return false;

            if (IgnoreCurrentFrameExec && DirectEve.IgnoreCurrentFrameExecution(caller))
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var delay = delayMsMax == 0 ? delayMs : _random.Next(delayMs, delayMsMax);

            if (_intervalForMovementCommandsDict.TryGetValue(caller, out var dt) && dt > now)
                return false;

            //Clear the dictionary if returning true so that we dont cache old Orbit, Approach, KeepAtRange info after doing one of the others...
            //switching back and forth should not result in a delay!
            _intervalForMovementCommandsDict = new Dictionary<string, DateTime>();
            _intervalForMovementCommandsDict[caller] = now.AddMilliseconds(delay);
            return true;
        }

        private static Dictionary<string, DateTime> _intervalForMovementCommandsDict = new Dictionary<string, DateTime>();
        private static Random _random = new Random();

        public bool SendDroneToEngageActiveTarget()
        {
            if (!IsValid)
                return false;

            if (CategoryId != (int)CategoryID.Drone)
                return false;

            if (DirectEve.ActiveDrones.Count == 0)
                return false;

            if (DirectEve.ActiveDrones.Any(i => i.Id == Id))
            {
                // this engages the drone on whatever the ActiveTarget is (locked and selected target in the hud)
                // this is only dealing with one drone at a time!
                PyObject pySendDroneToEngageActiveTarget = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.droneFunctions").Attribute("EngageTarget");
                if (pySendDroneToEngageActiveTarget.IsValid)
                {
                    if (!DirectEve.Interval(1000, 2000, Id.ToString()))
                        return false;

                    return DirectEve.ThreadedCall(pySendDroneToEngageActiveTarget, Id);
                }

                return false;
            }

            return false;
        }

        public bool SendDroneToMineRepeatedlyActiveTarget()
        {
            if (!IsValid)
                return false;

            if (CategoryId != (int)CategoryID.Drone)
                return false;

            if (DirectEve.ActiveDrones.Count == 0)
                return false;

            if (DirectEve.ActiveDrones.Any(i => i.Id == Id))
            {
                // this engages the drone on whatever the ActiveTarget is (locked and selected target in the hud)
                // this is only dealing with one drone at a time!
                PyObject pySendDroneToMineRepeatedlyActiveTarget = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.droneFunctions").Attribute("MineRepeatedly");
                if (pySendDroneToMineRepeatedlyActiveTarget.IsValid)
                {
                    if (!DirectEve.Interval(1000, 2000))
                        return false;

                    return DirectEve.ThreadedCall(pySendDroneToMineRepeatedlyActiveTarget, Id);
                }

                return false;
            }

            return false;
        }

        public bool SendDroneToSalvageActiveTarget()
        {
            if (!IsValid)
                return false;

            if (CategoryId != (int)CategoryID.Drone)
                return false;

            if (DirectEve.ActiveDrones.Count == 0)
                return false;

            if (DirectEve.ActiveDrones.Any(i => i.Id == Id))
            {
                // this engages the drone on whatever the ActiveTarget is (locked and selected target in the hud)
                // this is only dealing with one drone at a time!
                PyObject pySendDroneToSalvageActiveTarget = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.droneFunctions").Attribute("Salvage");
                if (pySendDroneToSalvageActiveTarget.IsValid)
                {
                    if (!DirectEve.Interval(1000, 2000))
                        return false;

                    return DirectEve.ThreadedCall(pySendDroneToSalvageActiveTarget, Id);
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Unlock target
        /// </summary>
        /// <returns></returns>
        public bool UnlockTarget()
        {
            if (!IsValid) return false;
            // Its not a target, why are you unlocking?!?!
            if (!IsTarget)
                return false;

            // Clear target information
            var call = DirectEve.GetLocalSvc("menu")["UnlockTarget"];
            if (call.IsValid)
            {
                DirectEve.ClearTargetTimer(Id);
                return DirectEve.ThreadedLocalSvcCall("menu", "UnlockTarget", Id);
            }

            return false;
        }

        /// <summary>
        ///     Warp fleet to target, make sure you have the fleetposition to warp the fleet
        /// </summary>
        /// <returns></returns>
        public bool WarpFleetTo()
        {
            if (!IsValid) return false;

            if (!DirectEve.Session.InFleet)
                return false;

            if (!SafeToInitiateMovementCommand(false))
                return false;

            if (DirectEve.ActiveShip.IsImmobile)
                return false;

            if (DirectEve.ActiveShip.IsScrambled)
                return false;

            var myDirectFleetMember = DirectEve.GetFleetMembers.Find(i => i.CharacterId == DirectEve.Session.CharacterId);
            if (myDirectFleetMember.Role == DirectFleetMember.FleetRole.Member)
                return false;

            var call = DirectEve.GetLocalSvc("menu")["WarpFleet"];
            if (call.IsValid)
            {
                if (!DirectEve.Interval(3000, 4000))
                    return false;

                if (DirectEve.ThreadedLocalSvcCall("menu", "WarpFleet", Id))
                {
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Warp fleet to target at range, make sure you have the fleetposition to warp the fleet
        /// </summary>
        /// <returns></returns>
        public bool WarpFleetTo(double range)
        {
            if (!IsValid) return false;

            if (!DirectEve.Session.InFleet)
                return false;

            if (!SafeToInitiateMovementCommand(false))
                return false;

            if (DirectEve.ActiveShip.IsImmobile)
                return false;

            if (DirectEve.ActiveShip.IsScrambled)
                return false;

            DirectFleetMember myDirectFleetMember = DirectEve.GetFleetMembers.Find(i => i.CharacterId == DirectEve.Session.CharacterId);
            if (myDirectFleetMember.Role == DirectFleetMember.FleetRole.Member)
                return false;

            var call = DirectEve.GetLocalSvc("menu")["WarpFleet"];
            if (call.IsValid)
            {
                if (!DirectEve.Interval(4000, 6000))
                    return false;

                if (DirectEve.ThreadedLocalSvcCall("menu", "WarpFleet", Id, range))
                {
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                    return true;
                }

                return false;
            }

            return false;
        }

        public bool IsCitadel
        {
            get
            {
                try
                {
                    if (IsValid)
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
                    if (IsValid)
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

        public bool IsPlanet
        {
            get
            {
                try
                {
                    if (IsValid)
                    {
                        if (GroupId == (int)Group.Planet)
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

        public bool IsStation
        {
            get
            {
                try
                {
                    if (IsValid)
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
        public bool IsJammingEntity => GetDmgEffects().ContainsKey(6695) || GetDmgEffectsByGuid().ContainsKey("effects.ElectronicAttributeModifyTarget");
        public bool IsNeutingEntity => GetDmgEffects().ContainsKey(6756) || GetDmgEffectsByGuid().ContainsKey("effects.EnergyDestabilization");
        public bool IsWarpScramblingEntity => GetDmgEffects().ContainsKey(6745) || GetDmgEffectsByGuid().ContainsKey("effects.WarpScramble");
        public bool IsWarpDisruptingEntity => GetDmgEffects().ContainsKey(6744) || GetDmgEffectsByGuid().ContainsKey("effects.WarpDisrupt");
        public bool IsWebbingEntity => GetDmgEffects().ContainsKey(6743) || GetDmgEffectsByGuid().ContainsKey("effects.ModifyTargetSpeed");
        public bool IsTargetPaintingEntity => GetDmgEffects().ContainsKey(6754) || GetDmgEffectsByGuid().ContainsKey("effects.TargetPaint");
        public bool IsRemoteArmorRepairingEntity => GetDmgEffects().ContainsKey(6741) || GetDmgEffectsByGuid().ContainsKey("effects.RemoteArmourRepair");
        public bool IsRemoteShieldRepairingEntity => GetDmgEffects().ContainsKey(6742) || GetDmgEffectsByGuid().ContainsKey("effects.ShieldTransfer");
        public bool IsRemoteRepairEntity => IsRemoteArmorRepairingEntity || IsRemoteShieldRepairingEntity;
        public bool IsSensorDampeningEntity => GetDmgEffects().ContainsKey(6755) || GetDmgEffectsByGuid().ContainsKey("effects.SensorDampening");
        public bool IsTrackingDisruptingEntity => GetDmgEffects().ContainsKey(6747) || GetDmgEffectsByGuid().ContainsKey("effects.TrackingDisruption");
        public bool IsGuidanceDisruptingEntity => GetDmgEffects().ContainsKey(6746) || GetDmgEffectsByGuid().ContainsKey("effects.ElectronicAttributeModifyTarget");
        public bool IsHeavyDeepsIntegrating => GetDmgEffects().ContainsKey(6995); // targetDisintegratorAttack

        public bool IsInSeigeModeEntity => GetDmgEffectsByGuid().ContainsKey("effects.SiegeMode");

        public bool IsInTriageModeEntity => GetDmgEffectsByGuid().ContainsKey("effects.TriageMode");

        //Filament Cloud (orange): Penalty to Shield Booster boosting (-40%) and reduction to shield booster duration (-40%). If using a conventional (not Ancillary) shield booster, in effect this does not weaken your shield booster, but rather increases its capacitor cost per second by 66%. If you rely on a shield booster to survive, you should avoid entering these clouds.
        public bool IsLocatedWithinFilamentCloud => GetDmgEffects().ContainsKey(7058);
        //Bioluminescence Cloud (light blue): +300% Signature Radius (4.0x signature radius multiplier). Entering this cloud will make your ship an easier target to hit but it will also make all rats easier to hit. If fighting small but accurate enemies like Damaviks, this cloud can actually be helpful, and you can lure the rats into it.
        public bool IsLocatedWithinBioluminescenceCloud => GetDmgEffects().ContainsKey(7050);

        //Tachyon Cloud(white): +300% Velocity(x4.0 velocity), -50% Inertia Modifier.Be very careful entering this cloud with an active MWD, as the inertia reduction will cause you to accelerate very quickly (x4 velocity, x0.5 inertia, x8 acceleration), potentially slingshotting you outside the pocket boundary for a very quick death.These clouds will also increase enemy velocities, causing them to either close range very quickly, or suddenly pull away.
        public bool IsLocatedWithinCausticCloud => GetDmgEffects().ContainsKey(7051);
        public bool IsLocatedWithinSpeedCloud => IsLocatedWithinCausticCloud;
        public bool IsTooCloseToSpeedCloud
        {
            get
            {
                if (IsLocatedWithinSpeedCloud)
                    return true;

                if (TypeId == (int)TypeID.SmallFilamentCloud)
                {
                    if ((double)Distances.CloseToSmallSpeedCloud > Distance)
                        return true;
                }

                if (TypeId == (int)TypeID.MediumFilamentCloud)
                {
                    if ((double)Distances.CloseToMediumSpeedCloud > Distance)
                        return true;
                }

                if (TypeId == (int)TypeID.LargeFilamentCloud)
                {
                    if ((double)Distances.CloseToLargeSpeedCloud > Distance)
                        return true;
                }

                return false;
            }
        }

        public bool IsTooCloseToSmallDeviantAutomataSuppressor //kills drones and missiles!
        {
            get
            {
                if (ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor != null)
                {
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        return false; //vorton projectors are not effected by Automata Suppressors!
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        return false; //vorton projectors are not effected by Automata Suppressors!
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                    {
                        return false; //Gila should be okay?
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Worm)
                    {
                        return false; //Worm should be okay?
                    }

                    if ((double)Distances.CloseToSmallDeviantAutomataSuppressor + 3000 > ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor._directEntity.DistanceTo(this))
                        return true;
                }

                return false;
            }
        }

        public bool IsTooCloseToMediumDeviantAutomataSuppressor //kills drones and missiles!
        {
            get
            {
                if (ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor != null)
                {
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        return false; //vorton projectors are not effected by Automata Suppressors!
                    }

                    if ((double)Distances.CloseToSmallDeviantAutomataSuppressor > ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor._directEntity.DistanceTo(this))
                        return true;
                }

                return false;
            }
        }

        //
        //see buffbarConst.py
        //public bool IsLocatedWithinTachyonCloud => GetDmgEffects().ContainsKey(6995);//not used, probably want caustic cloud
        public bool IsLocatedWithinRangeOfPulsePlatformTrackingPylon => GetDmgEffects().ContainsKey(7053);
        public bool IsLocatedWithinRangeOfPointDefenseBattery => GetDmgEffects().ContainsKey(7057);


        /**
            effectFighterAbilityAttackTurret = 6430
            effectFighterAbilityAttackMissile = 6465
            effectFighterAbilityEvasiveManeuvers = 6439
            effectFighterAbilityAfterburner = 6440
            effectFighterAbilityMicroWarpDrive = 6441
            effectFighterAbilityMicroJumpDrive = 6442
            effectFighterAbilityMissiles = 6431
            effectFighterAbilityECM = 6437
            effectFighterAbilityEnergyNeutralizer = 6434
            effectFighterAbilityStasisWebifier = 6435
            effectFighterAbilityWarpDisruption = 6436
            effectFighterAbilityTackle = 6464
            effectFighterAbilityLaunchBomb = 6485
            effectFighterDecreaseTargetSpeed = 6418
            effectFighterTargetPaint = 6419
            effectFighterDamageMultiply = 6420
            effectFighterMicroJumpDrive = 6421
            effectFighterAbilityKamikaze = 6554
         **/

        /// <summary>
        ///     Warp to target at range
        /// </summary>
        /// <returns></returns>
        public bool WarpTo(double range = 0)
        {
            if (!RecursionCheck("WarpTo"))
                return false;

            if (!IsValid)
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (DirectEve.ActiveShip.IsImmobile)
                return false;

            if (DirectEve.ActiveShip.IsScrambled)
                return false;

            if (!SafeToInitiateMovementCommand(false))
                return false;

            if (DirectEve.ActiveShip.Entity.Id == Id)
                return false;

            if (DirectEve.ActiveShip.Entity.IsWarpingByMode)
                return false;

            if (Distance > (long)Distances.HalfOfALightYearInAu)
                return false;

            if (Distance <= (int)Distances.WarptoDistance)
                return false;

            if (!Defense.ActivateCovopsCloak())
                return false;

            //if (ESCache.Instance.ActiveShip.GivenName == Settings.Instance.TransportShipName ||
            //    ESCache.Instance.ActiveShip.GivenName == Settings.Instance.TravelShipName)
            //{
            //    if (!Defense.ActivateRegularCloakWarpCloakyTrick())
            //        return false;
            //}

            var WarpToItem = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions").Attribute("WarpToItem");
            if (WarpToItem.IsValid)
            {
                if (!DirectEve.Interval(4000, 6000))
                    return false;

                if (range == 0)
                {
                    if (DirectEve.ThreadedCall(WarpToItem, Id))
                    {
                        //DirectEvent
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.WARP, "Warping to [" + Name + "]"));
                        ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                        return true;
                    }

                    return false;
                }

                if (DirectEve.ThreadedCall(WarpToItem, Id, range))
                {
                    Log.WriteLine("Warping to [" + Name + "][" + Math.Round(Distance / 1000 / 149598000, 2) + " AU away]");
                    //DirectEvent
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.WARP, "Warping to [" + Name + "]"));
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                    return true;
                }

                return false;
            }

            return false;
        }

        private List<float> _warpRanges = new List<float>() {
                            //10_000,
                            20_000,
                            30_000,
                            50_000,
                            70_000,
                            100_000,
                        };

        public bool WarpToAtRandomRange()
        {
            var randomRange = ListExtensions.Random(_warpRanges);
            if (randomRange < 0 || randomRange > 100_000)
                randomRange = 0;

            var res = WarpTo(randomRange);

            if (res)
                DirectEve.Log($"Warping at range [{randomRange}]");

            return res;
        }

        internal static long GetBallparkCount(DirectEve directEve)
        {
            //var Ballpark = directEve.GetLocalSvc("michelle").Call("GetBallpark");
            //if (Ballpark.IsValid && Ballpark.Attribute("balls").IsValid) return Ballpark.Attribute("balls").Call("keys").Size();
            var michelle = directEve.GetLocalSvc("michelle");
            var ballpark = michelle["_Michelle__bp"];
            var bpReady = michelle["bpReady"].ToBool();
            if (ballpark.IsValid && bpReady && ballpark.Attribute("balls").IsValid)
                return ballpark.Attribute("balls").Call("keys").Size();

            return 0;
        }

        private bool? _isTargetingOurDrones;

        /// <summary>
        /// Any entity has the follow id of our active drones
        /// </summary>
        public bool HasTheFollowIdOfAnyOfOurActiveDrones
        {
            get
            {
                if (_isTargetingOurDrones == null)
                {
                    var droneIds = DirectEve.ActiveDrones.Select(e => e.Id);
                    var followId = this.FollowId;
                    _isTargetingOurDrones = droneIds.Contains(followId);
                }

                return _isTargetingOurDrones.Value;
            }
        }

        public bool IsYellowBoxing
        {
            get
            {
                if (Time.Instance.LastJumpAction.AddSeconds(7) > DateTime.UtcNow)
                    return false;

                if (!IsAttacking)
                {
                    if (Drones.UseDrones && Drones.AllDronesInSpaceCount > 0)
                    {
                        ESCache.Instance.DictionaryCachedPerPocketLastAttackedDrones.AddOrUpdate(Id, DateTime.UtcNow);
                        return true;
                    }

                    return true;
                }

                if (!IsTargetedBy)
                {
                    if (Drones.UseDrones && Drones.AllDronesInSpaceCount > 0)
                    {
                        ESCache.Instance.DictionaryCachedPerPocketLastAttackedDrones.AddOrUpdate(Id, DateTime.UtcNow);
                        return true;
                    }

                    return true;
                }


                return false;
            }
        }

        internal static Dictionary<long, DirectEntity> GetEntities(DirectEve directEve)
        {
            if (DebugConfig.DebugEntities) Log.WriteLine("GetEntities");
            var pySharp = directEve.PySharp;
            var entitiesById = new Dictionary<long, DirectEntity>();

            // Used by various loops, etc
            var Ballpark = directEve.GetLocalSvc("michelle").Call("GetBallpark");
            var balls = Ballpark.Attribute("balls").Call("keys").ToList<long>();
            var target = directEve.GetLocalSvc("target");
            var targetsBeingRemoved = target.Attribute("deadShipsBeingRemoved");

            if (!targetsBeingRemoved.IsValid)
            {
                Log.WriteLine($"Target.deadShipsBeingRemoved is not valid!");
                return entitiesById;
            }

            var targetsBeingRemovedDict = targetsBeingRemoved.ToList<long>().ToDictionary(x => x, y => true);
            foreach (var id in balls)
            {
                if (id < 0)
                    continue;

                // Get slim item
                var slimItem = Ballpark.Call("GetInvItem", id);

                // Get ball
                var _ball = Ballpark.Call("GetBall", id);

                // Create the entity
                if (slimItem.IsValid && _ball.IsValid
                    && !targetsBeingRemovedDict.ContainsKey(id)
                    && !directEve.GetTargetsBeingRemoved().ContainsKey(id)
                    && !(bool)_ball.Attribute("exploded")
                    && !(bool)_ball.Attribute("released")
                    && _ball.Attribute("ballpark").IsValid)
                {
                    entitiesById[id] = new DirectEntity(directEve, Ballpark, _ball, slimItem, id);
                    if (!DebugConfig.DebugEntitiesSetDisplayFalse)
                    {
                        entitiesById[id].SetDisplay(true);
                    }
                    else
                        {
                        // this hides the entities, may increase performance
                        entitiesById[id].SetDisplay(false);
                        }
                    }
            }

            // Mark active target
            var activeTarget = pySharp.Import("eve.client.script.parklife.states").Attribute("activeTarget");
            var activeTargetId = (long)directEve.GetLocalSvc("stateSvc").Call("GetExclState", activeTarget);
            if (entitiesById.TryGetValue(activeTargetId, out var entity))
                entity.IsActiveTarget = true;


            var targets = target.Attribute("targetsByID").ToDictionary().Keys;
            foreach (var targetId in targets)
            {
                if (!entitiesById.TryGetValue((long)targetId, out entity))
                    continue;

                entity.IsTarget = true;
            }

            var targeting = target.Attribute("targeting").ToDictionary<long>().Keys;
            foreach (var targetId in targeting)
            {
                if (!entitiesById.TryGetValue(targetId, out entity))
                    continue;

                entity.IsTargeting = true;
            }

            var targetedBy = target.Attribute("targetedBy").ToList<long>();
            foreach (var targetId in targetedBy)
            {
                if (!entitiesById.TryGetValue(targetId, out entity))
                    continue;

                entity.IsTargetedBy = true;
            }

            var attackers = directEve.GetLocalSvc("tactical").Attribute("attackers").ToDictionary<long>();
            foreach (var attacker in attackers)
            {
                if (!entitiesById.TryGetValue(attacker.Key, out entity))
                    continue;

                if (entity.IsTargetedBy)
                {
                    //entity.IsAttacking = true;

                    var attacks = attacker.Value.ToList();
                    foreach (var attack in attacks.Select(a => (string)a.GetItemAt(1)))
                    {
                        entity.IsWarpScramblingMe |= attack == "effects.WarpScramble";
                        entity.IsWebbingMe |= attack == "effects.ModifyTargetSpeed";
                        entity.IsWarpDisruptingMe |= attack == "effects.WarpDisrupt";
                        entity.Attacks.Add(attack);
                    }
                }
            }

            var tempJammers = directEve.GetLocalSvc("tactical").Attribute("jammers").ToDictionary<long>();
            var jammers = tempJammers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            foreach (var jammer in jammers)
            {
                if (!entitiesById.TryGetValue(jammer.Key, out entity))
                    continue;

                var ews = jammer.Value.ToDictionary<string>().Keys;
                foreach (var ew in ews)
                {
                    entity.IsNeutralizingMe |= ew == "ewEnergyNeut";
                    entity.IsTryingToJamMe |= ew == "electronic";
                    entity.IsSensorDampeningMe |= ew == "ewRemoteSensorDamp";
                    entity.IsTargetPaintingMe |= ew == "ewTargetPaint";
                    entity.IsTrackingDisruptingMe |= ew == "ewTrackingDisrupt";
                    entity.ElectronicWarfare.Add(ew);
                }
            }

            // Find active jammers
            var godma = directEve.GetLocalSvc("godma");
            if (godma.IsValid)
            {
                var activeJams = godma.Attribute("activeJams").ToList();
                if (activeJams.Any())
                {
                    foreach (var jam in activeJams)
                    {
                        var jamAttr = jam.ToList();
                        if (jamAttr[3].ToUnicodeString() == "electronic")
                        {
                            var sourceEntityId = jamAttr[0].ToLong();
                            if (!entitiesById.TryGetValue((long)sourceEntityId, out entity))
                                continue;
                            entity.IsJammingMe = true;
                        }
                    }
                }
            }

            return entitiesById;
        }


        /// <summary>
        /// range 0 .. 1.0
        /// </summary>
        internal void GetDamageState()
        {
            _shieldPct = 0;
            _armorPct = 0;
            _structurePct = 0;

            // Get damage state properties
            var damageState = ballpark.Call("GetDamageState", Id).ToList();
            if ((damageState.Count == 3) ^ (damageState.Count == 5))
            {
                _shieldPct = (double)damageState[0];
                _armorPct = (double)damageState[1];
                _structurePct = (double)damageState[2];
            }

            if(DirectEve._entityHealthPercOverrides.TryGetValue(this.Id, out var res))
            {
                _shieldPct = res.Item1;
                _armorPct = res.Item2;
                _structurePct = res.Item3;
            }
        }

        public (double EM, double Explosive, double Kinetic, double Thermal) GetCurrentDPSFromTurrets()
        {
            var invType = ESCache.Instance.DirectEve.GetInvType(this.TypeId);

            if (invType == null)
                return (0, 0, 0, 0);

            if (invType.RateOfFire <= 0)
                return (0, 0, 0, 0);

            var myShip = DirectEve.ActiveShip.Entity;

            if (myShip == null)
                return (0, 0, 0, 0);


            var mySig = DirectEve.ActiveShip.GetSignatureRadius();

            var optimalSigRadius = invType.OptimalSigRadius == 0 ? 40_000 : invType.OptimalSigRadius;
            var falloff = invType.AccuracyFalloff == 0 ? 1 : invType.AccuracyFalloff;
            var tracking = invType.TurretTracking == 0 ? 1000 : invType.TurretTracking;

            double angularTerm = Math.Pow(((this.AngularVelocity * optimalSigRadius) / (tracking * mySig)), 2);
            double distanceTerm = Math.Pow(Math.Max(0, this.Distance - invType.OptimalRange) / falloff, 2);

            //Console.WriteLine($"angularTerm {angularTerm} distanceTerm {distanceTerm}");

            double hitChance = Math.Pow(0.5d, (angularTerm + distanceTerm));

            //Console.WriteLine($"TypeName {this.TypeName}  mySig {mySig} HitChance {hitChance} this.Distance {this.Distance} AngularVelocity {this.AngularVelocity} TurretTracking {invType.TurretTracking} OptimalRange {invType.OptimalRange} AccuracyFalloff {invType.AccuracyFalloff}");

            var normalizedTurretDamage = 0.5 *
                                        Math.Min(
                                            Math.Pow(hitChance, 2) + 0.98 * hitChance + 0.0501,
                                            6 * hitChance
                                        );

            // Desintegrator bonus // TODO: we need to be able to retrieve the "live" value
            if (DamageMultiplierBonusMax > 0)
                normalizedTurretDamage *= 1 + DamageMultiplierBonusMax;

            var dmgMulti = invType.TurretDamageMultiplier;

            var rateOfFire = invType.RateOfFire;

            var emDmg = invType.DamageEm;
            var expDmg = invType.DamageExplosive;
            var kinDmg = invType.Damagekinetic;
            var theDmg = invType.DamageThermal;


            var em = (emDmg * dmgMulti);
            var exp = (expDmg * dmgMulti);
            var kin = (kinDmg * dmgMulti);
            var the = (theDmg * dmgMulti);

            var emDps = normalizedTurretDamage * (em / (rateOfFire / 1000));
            var expDps = normalizedTurretDamage * (exp / (rateOfFire / 1000));
            var kinDps = normalizedTurretDamage * (kin / (rateOfFire / 1000));
            var theDps = normalizedTurretDamage * (the / (rateOfFire / 1000));

            return (emDps, expDps, kinDps, theDps);
        }

        public (double EM, double Explosive, double Kinetic, double Thermal) GetCurrentDPSFromMissiles()
        {

            if (MissileEntityAoeVelocityMultiplier <= 0)
                return (0, 0, 0, 0);

            var missileInvType = ESCache.Instance.DirectEve.GetInvType((int)EntityMissileTypeID);

            if (missileInvType == null)
                return (0, 0, 0, 0);

            var myShip = DirectEve.ActiveShip.Entity;

            if (myShip == null)
                return (0, 0, 0, 0);

            // Damage formula D * min(1, S/E, (SVM/EVT)^drf)
            // D = base damage
            // S = signature radius of target
            // E = explosion radius of missile
            // SVM = signature radius of target * explosion velocity of missile
            // EVT = (explosion radius of missile * explosion velocity of the target)^drf
            // drf = damage reduction factor (1 for rockets, 0.5 for light missiles, 0.25 for heavy missiles, 0.125 for cruise missiles, 0.0625 for torpedoes)

            var d = missileInvType.DamageEm + missileInvType.DamageExplosive + missileInvType.Damagekinetic +
                              missileInvType.DamageThermal;

            var missileRateOfFire = MissileLaunchDuration / 1000;
            var missleDamageMulti = MissileDamageMultiplier;

            var emDamage = (missileInvType.DamageEm * missleDamageMulti) / missileRateOfFire;
            var explosiveDamage = (missileInvType.DamageExplosive * missleDamageMulti) / missileRateOfFire;
            var kineticDamage = (missileInvType.Damagekinetic * missleDamageMulti) / missileRateOfFire;
            var thermalDamage = (missileInvType.DamageThermal * missleDamageMulti) / missileRateOfFire;

            var drf = missileInvType.AoeDamageReductionFactor;
            var targetSigRadius = DirectEve.ActiveShip.GetSignatureRadius();

            emDamage = emDamage * Math.Min(Math.Min(1, targetSigRadius / missileInvType.Radius), Math.Pow((targetSigRadius * missileInvType.ExplosionVelocity) / (missileInvType.Radius * MissileEntityAoeVelocityMultiplier), drf));
            explosiveDamage = explosiveDamage * Math.Min(Math.Min(1, targetSigRadius / missileInvType.Radius), Math.Pow((targetSigRadius * missileInvType.ExplosionVelocity) / (missileInvType.Radius * MissileEntityAoeVelocityMultiplier), drf));
            kineticDamage = kineticDamage * Math.Min(Math.Min(1, targetSigRadius / missileInvType.Radius), Math.Pow((targetSigRadius * missileInvType.ExplosionVelocity) / (missileInvType.Radius * MissileEntityAoeVelocityMultiplier), drf));
            thermalDamage = thermalDamage * Math.Min(Math.Min(1, targetSigRadius / missileInvType.Radius), Math.Pow((targetSigRadius * missileInvType.ExplosionVelocity) / (missileInvType.Radius * MissileEntityAoeVelocityMultiplier), drf));

            return (emDamage, explosiveDamage, kineticDamage, thermalDamage);

        }
        public (double EM, double Explosive, double Kinetic, double Thermal) GetCurrentDPSFrom()
        {
            var turretDps = GetCurrentDPSFromTurrets();
            var missileDps = GetCurrentDPSFromMissiles();
            // return sum of both bot round to two decimals
            var roundedDps = (
               Math.Round(turretDps.EM + missileDps.EM, 2),
               Math.Round(turretDps.Explosive + missileDps.Explosive, 2),
               Math.Round(turretDps.Kinetic + missileDps.Kinetic, 2),
               Math.Round(turretDps.Thermal + missileDps.Thermal, 2)
            );
            return roundedDps;
        }


        public (double EM, double Explosive, double Kinetic, double Thermal) GetMaxDPSFrom()
        {
            var turretDps = GetMaxDPSFromTurrets();
            var missileDps = GetMaxDPSFromMissiles();
            // return sum of both bot round to two decimals

            var roundedDps = (
             Math.Round(turretDps.EM + missileDps.EM, 2),
             Math.Round(turretDps.Explosive + missileDps.Explosive, 2),
             Math.Round(turretDps.Kinetic + missileDps.Kinetic, 2),
             Math.Round(turretDps.Thermal + missileDps.Thermal, 2)
             );
            return roundedDps;
        }

        public (double EM, double Explosive, double Kinetic, double Thermal) GetMaxDPSFromMissiles()
        {

            if (MissileEntityAoeVelocityMultiplier <= 0)
                return (0, 0, 0, 0);

            var invType = ESCache.Instance.DirectEve.GetInvType((int)EntityMissileTypeID);

            if (invType == null)
                return (0, 0, 0, 0);

            var missileRateOfFire = MissileLaunchDuration / 1000;
            var missleDamageMulti = MissileDamageMultiplier;

            var emDamage = (invType.DamageEm * missleDamageMulti) / missileRateOfFire;
            var explosiveDamage = (invType.DamageExplosive * missleDamageMulti) / missileRateOfFire;
            var kineticDamage = (invType.Damagekinetic * missleDamageMulti) / missileRateOfFire;
            var thermalDamage = (invType.DamageThermal * missleDamageMulti) / missileRateOfFire;

            return (emDamage, explosiveDamage, kineticDamage, thermalDamage);
        }

        public (double EM, double Explosive, double Kinetic, double Thermal) GetMaxDPSFromTurrets()
        {
            var invType = ESCache.Instance.DirectEve.GetInvType(this.TypeId);

            if (invType == null)
                return (0, 0, 0, 0);

            if (invType.RateOfFire <= 0)
                return (0, 0, 0, 0);

            var myShip = ESCache.Instance.MyShipEntity._directEntity;

            if (myShip == null)
                return (0, 0, 0, 0);

            // Turret calculations
            var hitChance = 1.0;

            var normalizedTurretDamage = 0.5 *
                                         Math.Min(
                                             Math.Pow(hitChance, 2) + 0.98 * hitChance + 0.0501,
                                             6 * hitChance
                                         );

            // Desintegrator bonus // TODO: we need to be able to retrieve the "live" value
            if (DamageMultiplierBonusMax > 0)
                normalizedTurretDamage *= 1 + DamageMultiplierBonusMax;

            var dmgMulti = invType.TurretDamageMultiplier;

            var rateOfFire = invType.RateOfFire;

            var emDmg = invType.DamageEm;
            var expDmg = invType.DamageExplosive;
            var kinDmg = invType.Damagekinetic;
            var theDmg = invType.DamageThermal;

            var em = (emDmg * dmgMulti);
            var exp = (expDmg * dmgMulti);
            var kin = (kinDmg * dmgMulti);
            var the = (theDmg * dmgMulti);

            var emDps = normalizedTurretDamage * (em / (rateOfFire / 1000));
            var expDps = normalizedTurretDamage * (exp / (rateOfFire / 1000));
            var kinDps = normalizedTurretDamage * (kin / (rateOfFire / 1000));
            var theDps = normalizedTurretDamage * (the / (rateOfFire / 1000));
            // return them but round two intergers
            return (emDps, expDps, kinDps, theDps);
        }

        public double DamageTo()
        {
            // TODO: I will come back to this, requires knowing all our damage methods
            return 0.0;
        }

        public double turretHitChance
        {
            get
            {
                return 0.5 * (
                Math.Pow((AngularVelocity * 40000) / (TurretTracking * SignatureRadius), 2)
                + Math.Pow(Math.Max(0, Distance - OptimalRange) / AccuracyFalloff, 2)
                );
            }
        }

        public double normalizedTurretDamage
        {
            get
            {
                return 0.5 * Math.Min(Math.Pow(turretHitChance, 2) + 0.98 * turretHitChance + 0.0501, 6 * turretHitChance);
            }
        }

        public double normalizedTurretDps
        {
            get
            {
                return normalizedTurretDamage / (RateOfFire / 1000);
            }
        }

        private double? _totalDamage = null;
        public int totalDamage
        {
            get
            {
                if (_totalDamage == null)
                {
                    double _totalRawDamage = DamageEm + DamageExplosive + Damagekinetic + DamageThermal;
                    _totalDamage = _totalRawDamage * DamageModifier;
                    return (int)_totalDamage;
                }

                return (int)_totalDamage;
            }
        }

        //
        // this is only correct for turrets, needs to add missiles, trig weapons?!
        //
        public (double EM, double Explosive, double Kinetic, double Thermal) TurretDamageFrom()
        {
            // If we are outside it's lock range assume no damage
            //if (Distance > invType.MaxtargetingRange) return (0, 0, 0, 0);

            var emDamage = DamageEm / totalDamage * normalizedTurretDps;
            var explosiveDamage = DamageExplosive / totalDamage * normalizedTurretDps;
            var kineticDamage = Damagekinetic / totalDamage * normalizedTurretDps;
            var thermalDamage = DamageThermal / totalDamage * normalizedTurretDps;

            return (emDamage, explosiveDamage, kineticDamage, thermalDamage);
        }

        #endregion Methods
    }
}