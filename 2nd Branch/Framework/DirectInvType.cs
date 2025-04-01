// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Security;
using EVESharpCore.Cache;
using EVESharpCore.Questor.Combat;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum BracketType
    {
        Unknown,
        Agents_in_Space,
        Assembly_Array,
        Asteroid_Belt,
        Asteroid_Billboard,
        Asteroid_Large,
        Asteroid_Medium,
        Asteroid_Small,
        Battlecruiser,
        Battleship,
        Biomass,
        Bomb,
        Capsule,
        Capture_Point,
        Cargo_container,
        Cargo_Container_NPC,
        Carrier,
        Celestial_Agent_Site_Beacon,
        Celestial_Beacon_II,
        Command_Node_Beacon,
        Compression_Array,
        Control_Tower,
        Corporate_Hangar_Array,
        Cruiser,
        Cynosural_Field,
        Cynosural_Generator_Array,
        Cynosural_System_Jammer,
        Destroyer,
        Destructible_Station_Service,
        Dreadnought,
        Drone,
        Drone_EW,
        Drone_Logistics,
        Drone_Mining,
        Drone_Salvaging,
        Drone_Sentry,
        Encounter_Surveillance_System,
        Entity,
        Extra_Large_Engineering_Complex,
        Extra_Large_Structure,
        Fighter_Squadron,
        Force_Auxiliary,
        Force_Field,
        Freighter,
        Frigate,
        FW_Infrastructure_Hub,
        Harvestable_Cloud,
        Ice_Field,
        Ice_Large,
        Ice_Small,
        Industrial_Command_Ship,
        Industrial_Ship,
        Infrastructure_Hub,
        Jump_Portal_Array,
        Laboratory,
        Large_Collidable_Structure,
        Large_Engineering_Complex,
        Large_Refinery,
        Large_Structure,
        Medium_Engineering_Complex,
        Medium_Refinery,
        Medium_Structure,
        Mining_Barge,
        Mining_Frigate,
        Mobile_Cynosural_Inhibitor,
        Mobile_Depot,
        Mobile_Jump_Disruptor,
        Mobile_Micro_Jump_Unit,
        Mobile_Power_Core,
        Mobile_Scan_Inhibitor,
        Mobile_Shield_Generator,
        Mobile_Siphon_Unit,
        Mobile_Storage,
        Mobile_Tractor_Unit,
        Mobile_Warp_Disruptor,
        Moon,
        Moon_Asteroid,
        Moon_Asteroid_Jackpot,
        Moon_Mining,
        Navy_Concord_Customs,
        NO_BRACKET,
        NPC_Battlecruiser,
        NPC_Battleship,
        NPC_Carrier,
        NPC_Cruiser,
        NPC_Destroyer,
        NPC_Dreadnought,
        NPC_Drone,
        NPC_Drone_EW,
        NPC_Extra_Large_Engineering_Complex,
        NPC_Fighter,
        NPC_Fighter_Bomber,
        NPC_Force_Auxiliary,
        NPC_Freighter,
        NPC_Frigate,
        NPC_Industrial,
        NPC_Industrial_Command_Ship,
        NPC_Mining_Barge,
        NPC_Mining_Frigate,
        NPC_Rookie_Ship,
        NPC_Shuttle,
        NPC_Super_Carrier,
        NPC_Titan,
        Personal_Hangar_Array,
        Planet,
        Planetary_Customs_Office,
        Planetary_Customs_Office_NPC,
        Platform,
        Reactor,
        Reprocessing_Array,
        Rookie_ship,
        Satellite_Beacon,
        Scanner_Array,
        Scanner_Probe,
        Sentry_Gun,
        Ship_Maintenance_Array,
        Shuttle,
        Silo,
        Sovereignty_Blockade_Unit,
        Starbase_Electronic_Warfare_Battery,
        Starbase_Energy_Neutralizing_Battery,
        Starbase_Hybrid_Battery,
        Starbase_Laser_Battery,
        Starbase_Missile_Battery,
        Starbase_Projectile_Battery,
        Starbase_Sensor_Dampening_Battery,
        Starbase_Shield_Hardening_Array,
        Starbase_Stasis_Webification_Battery,
        Starbase_Warp_Scrambling_Battery,
        Stargate,
        Station,
        Structure,
        Sun,
        Super_Carrier,
        Territorial_Claim_Unit,
        Titan,
        Warp_Gate,
        Wormhole,
        Wreck,
        Wreck_NPC,
        XL_Ship_maintenance_Array
    }

    public class DirectInvType : DirectObject
    {
        #region Fields

        /// <summary>
        ///     TypeId; Bracketname dict
        /// </summary>
        private static Dictionary<int, string> _bracketNameDictionary = new Dictionary<int, string>();

        private static Dictionary<int, string> _bracketTexturePathDictionary = new Dictionary<int, string>();
        private static Dictionary<int, BracketType> _bracketTypeDictionary = new Dictionary<int, BracketType>();
        private static Dictionary<int, DirectInvType> invTypeCache = new Dictionary<int, DirectInvType>();

        private Dictionary<string, object> _attrdictionary;
        //private double? _averagePrice;
        private double? _basePrice;
        private double? _capacity;

        private int? _categoryId;

        //private string _categoryName;
        private double? _chanceOfDuplicating;

        private int? _dataId;
        private string _description;

        //private List<PyObject> _dmgAttributes;

        //private double? _emTurretEffectiveDps;

        //private double? _emMissileEffectiveDps;
        private int? _graphicId;
        private int? _groupId;
        private string _groupName;
        private int? _iconId;

        //private double? _kineticTurretEffectiveDps;

        private int? _marketGroupId;

        //private double? _kineticMissileEffectiveDps;
        private double? _mass;

        //private double? _emTurretEffectiveDamage;

        //private double? _explosiveEffectiveDps;

        //private double? _explosiveMissileEffectiveDps;
        private int? _portionSize;

        private bool? _published;

        private int? _raceId;
        private double? _radius;
        private int? _soundId;
        private double? _maxArmor;
        private double? _maxShield;
        private double? _maxStructure;
        //private double? _thermalTurretEffectiveDps;

        //private double? _thermalMissileEffectiveDps;
        private double? _signatureRadius;
        private string _typeName;
        private double? _volume;

        // These refer to NPC attributes
        // ingame this refers to "speed" for some nasty reason
        private double? rateOfFire;
        private double? missileLaunchDuration;
        private double? optimalRange;
        private double? optimalSigRadius;
        private double? damageModifier;
        private double? maxtargetingRange;
        private double? damageEm;
        private double? damageExplosive;
        private double? damagekinetic;
        private double? missileEntityAoeVelocityMultiplier;
        private double? aoeVelocity;
        private double? aoeDamageReductionFactor;

        private double? entityMissileTypeID;
        private double? damageThermal;
        private double? accuracyFalloff;
        private double? turretTracking;
        private double? damageMultiplierBonusMax;
        private double? missileDamageMultiplier;
        private int? maxLockedTargets;
        //private double? disintergratorDamageMultiplierPerCycle;
        //private double? disintergratorMaxDamageMultiplier;

        #endregion Fields

        #region Constructors

        public DirectInvType(DirectEve directEve, int typeId)
            : base(directEve)
        {
            TypeId = typeId;
        }

        internal DirectInvType(DirectEve directEve)
            : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        private string _categoryName;

        public double BasePrice
        {
            get
            {
                if (!_basePrice.HasValue)
                {
                    if (DirectCache.DictionaryCachedBasePrices.ContainsKey(TypeId))
                    {
                        if (DirectCache.DictionaryCachedBasePrices.TryGetValue(TypeId, out double tempBasePrice))
                        {
                            _basePrice = tempBasePrice;
                            return _basePrice.Value;
                        }
                    }

                    _basePrice = (double)PyInvType.Attribute("basePrice");
                    DirectCache.DictionaryCachedBasePrices.AddOrUpdate(TypeId, (double)_basePrice);
                    return _basePrice.Value;
                }

                return _basePrice.Value;
            }
        }

        /// <summary>
        /// This works also with mutated drones.
        /// </summary>
        /// <returns></returns>
        public Dictionary<DirectDamageType, float> GetDroneDPS()
        {

            long id = 0;

            if (this.GetType() == typeof(DirectItem))
                id = ((DirectItem)this).ItemId;
            else if (this.GetType() == typeof(DirectEntity))
                id = ((DirectEntity)this).Id;
            else
            {
                throw new NotImplementedException();
            }

            var result = new Dictionary<DirectDamageType, float>();

            // Get the attributes for each damage type
            var emDmg = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeEmDamage.ToInt());
            var exploDmg = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeExplosiveDamage.ToInt());
            var thermDmg = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeThermalDamage.ToInt());
            var kinDmg = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeKineticDamage.ToInt());

            // Get the attribute for damage multi and duration
            var multi = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeDamageMultiplier.ToInt());
            var rof = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeRateOfFire.ToInt());

            //DirectEve.Log($"emDmg {emDmg} exploDmg {exploDmg} thermDmg {thermDmg} kinDmg {kinDmg} multi {multi} rof {rof}");
            // Calculate drone dps => droneDps = damage * damageMultiplier / duration
            if (rof > 0d)
            {
                result[DirectDamageType.EM] = 1000 * ((emDmg * multi) / rof);
                result[DirectDamageType.EXPLO] = 1000 * ((exploDmg * multi) / rof);
                result[DirectDamageType.THERMAL] = 1000 * ((thermDmg * multi) / rof);
                result[DirectDamageType.KINETIC] = 1000 * ((kinDmg * multi) / rof);
            }
            else
            {
                result[DirectDamageType.EM] = 0;
                result[DirectDamageType.EXPLO] = 0;
                result[DirectDamageType.THERMAL] = 0;
                result[DirectDamageType.KINETIC] = 0;
            }

            return result;
        }

        public BracketType BracketType
        {
            get
            {
                if (DirectCache.BracketTypeDictionary.TryGetValue(TypeId, out var bracketType)) return bracketType;

                BracketType r = default(BracketType);
                if (Enum.TryParse<BracketType>(GetBracketName().Replace(" ", "_"), out var type)) r = type;

                DirectCache.BracketTypeDictionary[TypeId] = r;
                return r;
            }
        }

        public double Capacity
        {
            get
            {
                if (!_capacity.HasValue)
                    _capacity = (double) PyInvType.Attribute("capacity");

                return _capacity.Value;
            }
        }

        public int CategoryId
        {
            get
            {
                if (!_categoryId.HasValue)
                    _categoryId = (int) PyInvGroup.Attribute("categoryID");

                return _categoryId.Value;
            }
        }

        public string CategoryName
        {
            get
            {
                if (string.IsNullOrEmpty(_categoryName))
                {
                    _categoryName = (string) PySharp.Import("evetypes")
                        .Attribute("localizationUtils")
                        .Call("GetLocalizedCategoryName", (int) PyInvCategory.Attribute("categoryNameID"), "en-us");
                }
                return _categoryName;
            }
        }

        public double ChanceOfDuplicating
        {
            get
            {
                if (!_chanceOfDuplicating.HasValue)
                    _chanceOfDuplicating = (double) PyInvType.Attribute("chanceOfDuplicating");

                return _chanceOfDuplicating.Value;
            }
        }

        public AmmoType? AmmoType
        {
            get
            {
                if (DirectUIModule.DefinedAmmoTypes.Any(x => x.TypeId == TypeId))
                {
                    return DirectUIModule.DefinedAmmoTypes.FirstOrDefault(x => x.TypeId == TypeId);
                }

                return null;
            }
        }

        public bool ThisAmmoIsCorrectDamageTypeAmmoForKillTarget
        {
            get
            {
                if (CategoryId != (int)CategoryID.Charge)
                    return false;

                if (Combat.KillTarget != null && Combat.KillTarget.BestDamageTypes.Any() && DefinedDamageTypeForThisItem == Combat.KillTarget.BestDamageTypes.FirstOrDefault())
                {
                    return true;
                }

                if (DefinedDamageTypeForThisItem == MissionSettings.CurrentDamageType)
                {
                    return true;
                }

                return false;
            }
        }

        public DamageType? DefinedDamageTypeForThisItem
        {
            get
            {
                if (AmmoType != null)
                {
                    return AmmoType.DamageType;
                }

                return null;
            }
        }

        public int DataId
        {
            get
            {
                if (!_dataId.HasValue)
                    _dataId = (int) PyInvType.Attribute("dataID");

                return _dataId.Value;
            }
        }

        public string Description
        {
            get
            {
                if (string.IsNullOrEmpty(_description))
                    _description = (string) PyInvType.Attribute("description");

                return _description;
            }
        }

        private static Dictionary<int, double> _cachedPrices = new Dictionary<int, double>();

        private static System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@"(?:\""sell\"".*\""wavg\"":(?<Wavg>[\d\.]+))", System.Text.RegularExpressions.RegexOptions.Compiled);

        public double AveragePrice()
        {
            var avgPrice = -1d;
            if (_cachedPrices.TryGetValue(this.TypeId, out double price))
                return price;

            try
            {
                return Util.MeasureTime(() =>
                {
                    double val = (double)PySharp.Import("inventorycommon.typeHelpers").Call("GetAveragePrice", this.TypeId);
                    return _cachedPrices[this.TypeId] = val;
                });

            }
            catch (Exception)
            {
                avgPrice = (double)PySharp.Import("inventorycommon.typeHelpers").Call("GetAveragePrice", this.TypeId);
                Console.WriteLine($"Info: Returning average price fallback value. TypeId [{this.TypeId}] AvgPrice [{avgPrice}]");
            }
            return avgPrice;
        }


        //     quote = sm.GetService('marketQuote')
        // averagePrice = quote.GetAveragePrice(typeID)
        //public double GetAveragePrice
        //{
        //    get
        //    {
        //        if (_averagePrice == null) _averagePrice = (double) DirectEve.GetLocalSvc("marketQuote").Call("GetAveragePrice", TypeId);
        //
        //        return _averagePrice == null ? 0 : (double) _averagePrice;
        //    }
        //}

        public int GraphicId
        {
            get
            {
                if (!_graphicId.HasValue)
                    _graphicId = (int) PyInvType.Attribute("graphicID");

                return _graphicId.Value;
            }
        }

        public int GroupId
        {
            get
            {
                if (!_groupId.HasValue)
                    _groupId = (int) PyInvType.Attribute("groupID");

                return _groupId.Value;
            }
        }

        public bool IsAbyssalLootToSell
        {
            get
            {
                if (GroupId == (int)Group.Mutaplasmids)
                    return true;

                if (CategoryId == (int)CategoryID.Skill)
                    return true;

                if (CategoryId == (int)CategoryID.Blueprint)
                    return true;

                if (TypeName.Contains("Dark Filament"))
                    return true;

                if (TypeName.Contains("Electrical Filament"))
                    return true;

                if (TypeName.Contains("Exotic Filament"))
                    return true;

                if (TypeName.Contains("Firestorm Filament"))
                    return true;

                if (TypeName.Contains("Gamma Filament"))
                    return true;

                if (TypeName.Contains("Zero-Point Condensate"))
                    return true;

                if (TypeName.Contains("Crystalline Isogen-10"))
                    return true;

                return false;
            }
        }

        public bool IsContainerUsedToSortItemsInStations
        {
            get
            {
                if (GroupId == (int)Group.CargoContainer)
                    return true;

                if (GroupId == (int)Group.SecureContainer)
                    return true;

                if (GroupId == (int)Group.AuditLogSecureContainer)
                    return true;

                if (GroupId == (int)Group.FreightContainer)
                    return true;

                return false;
            }
        }

        private double? _armorResistanceEm;

        private double? _armorResistanceExplosive;

        private double? _armorResistanceKinetic;

        private double? _armorResistanceThermal;

        public double? ArmorResistanceEM
        {
            get
            {
                if (!_armorResistanceEm.HasValue)
                {
                    _armorResistanceEm = Math.Round(1.0d - TryGet<float>("armorEmDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EM)
                    {
                        _armorResistanceEm = Math.Round(1.0d - (1.0d - _armorResistanceEm.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_armorResistanceEm < 0d)
                             _armorResistanceEm = 0d;
                    }
                }
                return _armorResistanceEm;
            }
        }

        private double? _falloff;

        public double? Falloff
        {
            get
            {
                if (_falloff == null)
                    _falloff = (float)PyInvType.Attribute("falloff");
                return _falloff.Value;
            }
        }

        public double? ArmorResistanceExplosive
        {
            get
            {
                if (!_armorResistanceExplosive.HasValue)
                {
                    _armorResistanceExplosive = Math.Round(1.0d - TryGet<float>("armorExplosiveDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EXPLO)
                    {
                        _armorResistanceExplosive = Math.Round(1.0d - (1.0d - _armorResistanceExplosive.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_armorResistanceExplosive < 0d)
                            _armorResistanceExplosive = 0d;
                    }
                }
                return _armorResistanceExplosive;
            }
        }

        public double? ArmorResistanceKinetic
        {
            get
            {
                if (!_armorResistanceKinetic.HasValue)
                {
                    _armorResistanceKinetic = Math.Round(1.0d - TryGet<float>("armorKineticDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.KINETIC)
                    {
                        _armorResistanceKinetic = Math.Round(1.0d - (1.0d - _armorResistanceKinetic.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_armorResistanceKinetic < 0d)
                            _armorResistanceKinetic = 0d;
                    }
                }
                return _armorResistanceKinetic;
            }
        }

        public double? ArmorResistanceThermal
        {
            get
            {
                if (!_armorResistanceThermal.HasValue)
                {
                    _armorResistanceThermal = Math.Round(1.0d - TryGet<float>("armorThermalDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.THERMAL)
                    {
                        _armorResistanceThermal = Math.Round(1.0d - (1.0d - _armorResistanceThermal.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_armorResistanceThermal < 0d)
                            _armorResistanceThermal = 0d;
                    }
                }
                return _armorResistanceThermal;
            }
        }

        private double? _shieldResistanceEm;
        private double? _shieldResistanceExplosive;
        private double? _shieldResistanceKinetic;
        private double? _shieldResistanceThermal;
        private double? _structureResistanceEm;
        private double? _structureResistanceExplosive;
        private double? _structureResistanceKinetic;
        private double? _structureResistanceThermal;


        public double? ShieldResistanceEM
        {
            get
            {
                if (!_shieldResistanceEm.HasValue)
                {
                    _shieldResistanceEm = Math.Round(1.0d - TryGet<float>("shieldEmDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EM)
                    {
                        _shieldResistanceEm = Math.Round(1.0d - (1.0d - _shieldResistanceEm.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_shieldResistanceEm < 0d)
                            _shieldResistanceEm = 0d;
                    }
                }
                return _shieldResistanceEm;
            }
        }

        public double? ShieldResistanceExplosive
        {
            get
            {
                if (!_shieldResistanceExplosive.HasValue)
                {
                    _shieldResistanceExplosive = Math.Round(1.0d - TryGet<float>("shieldExplosiveDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EXPLO)
                    {
                        _shieldResistanceExplosive = Math.Round(1.0d - (1.0d - _shieldResistanceExplosive.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_shieldResistanceExplosive < 0d)
                            _shieldResistanceExplosive = 0d;
                    }
                }

                return _shieldResistanceExplosive;
            }
        }

        public double? ShieldResistanceKinetic
        {
            get
            {
                if (!_shieldResistanceKinetic.HasValue)
                {
                    _shieldResistanceKinetic = Math.Round(1.0d - TryGet<float>("shieldKineticDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.KINETIC)
                    {
                        _shieldResistanceKinetic = Math.Round(1.0d - (1.0d - _shieldResistanceKinetic.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_shieldResistanceKinetic < 0d)
                            _shieldResistanceKinetic = 0d;
                    }
                }
                return _shieldResistanceKinetic;
            }
        }

        public double? ShieldResistanceThermal
        {
            get
            {
                if (!_shieldResistanceThermal.HasValue)
                {

                    _shieldResistanceThermal = Math.Round(1.0d - TryGet<float>("shieldThermalDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.THERMAL)
                    {
                        _shieldResistanceThermal = Math.Round(1.0d - (1.0d - _shieldResistanceThermal.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                    }

                    if (_structureResistanceThermal < 0d || _structureResistanceThermal >= 1.0d)
                        _shieldResistanceThermal = 0d;
                }
                return _shieldResistanceThermal;
            }
        }

        public double? StructureResistanceEM
        {
            get
            {
                if (!_structureResistanceEm.HasValue)
                {
                    _structureResistanceEm = Math.Round(1.0d - TryGet<float>("emDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EM)
                    {
                        _structureResistanceEm = Math.Round(1.0d - (1.0d - _structureResistanceEm.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                    }

                    if (_structureResistanceEm < 0d || _structureResistanceEm >= 1.0d)
                        _structureResistanceEm = 0d;
                }
                return _structureResistanceEm;
            }
        }

        public double? StructureResistanceExplosion
        {
            get
            {
                if (!_structureResistanceExplosive.HasValue)
                {
                    _structureResistanceExplosive = Math.Round(1.0d - TryGet<float>("explosiveDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EXPLO)
                    {
                        _structureResistanceExplosive = Math.Round(1.0d - (1.0d - _structureResistanceExplosive.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                    }

                    if (_structureResistanceExplosive < 0d || _structureResistanceExplosive >= 1.0d)
                        _structureResistanceExplosive = 0d;
                }

                return _structureResistanceExplosive;
            }
        }

        public double? StructureResistanceKinetic
        {
            get
            {
                if (!_structureResistanceKinetic.HasValue)
                {
                    _structureResistanceKinetic = Math.Round(1.0d - TryGet<float>("kineticDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.KINETIC)
                    {
                        _structureResistanceKinetic = Math.Round(1.0d - (1.0d - _structureResistanceKinetic.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                    }

                    if (_structureResistanceKinetic < 0d || _structureResistanceKinetic >= 1.0d)
                        _structureResistanceKinetic = 0d;
                }
                return _structureResistanceKinetic;
            }
        }

        public double? StructureResistanceThermal
        {
            get
            {
                if (!_structureResistanceThermal.HasValue)
                {
                    _structureResistanceThermal = Math.Round(1.0d - TryGet<float>("thermalDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.THERMAL)
                    {
                        _structureResistanceThermal = Math.Round(1.0d - (1.0d - _structureResistanceThermal.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_structureResistanceThermal < 0d)
                            _structureResistanceThermal = 0d;
                    }
                }
                return _structureResistanceThermal;
            }
        }
        private bool? _isMissile;

        public bool IsMissile
        {
            get
            {
                if (CategoryId != (int)CategoryID.Charge) return false;

                if (_isMissile == null)
                {
                    if (GroupId != 0)
                    {
                        switch (GroupId)
                        {
                            case (int)Group.Rockets:
                            case (int)Group.LightMissiles:
                            case (int)Group.HeavyMissiles:
                            case (int)Group.HeavyAssaultMissiles:
                            case (int)Group.CruiseMissiles:
                            case (int)Group.Torpedoes:
                            {
                                _isMissile = true;
                                return (bool)_isMissile;
                            }
                        }

                        _isMissile = false;
                        return (bool)_isMissile;
                    }

                    _isMissile = false;
                    return (bool)_isMissile;
                }

                return (bool)_isMissile;
            }
        }

        private bool? _isProjectileAmmo;

        public bool IsProjectileAmmo
        {
            get
            {
                if (CategoryId != (int)CategoryID.Charge) return false;

                if (_isProjectileAmmo == null)
                {
                    if (GroupId != 0)
                    {
                        switch (GroupId)
                        {
                            case (int)Group.AdvancedArtilleryAmmo:
                            case (int)Group.AdvancedAutoCannonAmmo:
                            case (int)Group.ProjectileAmmo:
                                {
                                    _isProjectileAmmo = true;
                                    return (bool)_isProjectileAmmo;
                                }
                        }

                        _isProjectileAmmo = false;
                        return (bool)_isProjectileAmmo;
                    }

                    _isProjectileAmmo = false;
                    return (bool)_isProjectileAmmo;
                }

                return (bool)_isProjectileAmmo;
            }
        }

        private bool? _isHybridAmmo;

        public bool IsHybridAmmo
        {
            get
            {
                if (CategoryId != (int)CategoryID.Charge) return false;

                if (_isHybridAmmo == null)
                {
                    if (GroupId != 0)
                    {
                        switch (GroupId)
                        {
                            case (int)Group.HybridCharge:
                            case (int)Group.AdvancedRailgunCharge:
                            case (int)Group.AdvancedBlasterCharge:
                                {
                                    _isHybridAmmo = true;
                                    return (bool)_isHybridAmmo;
                                }
                        }

                        _isHybridAmmo = false;
                        return (bool)_isHybridAmmo;
                    }

                    _isHybridAmmo = false;
                    return (bool)_isHybridAmmo;
                }

                return (bool)_isHybridAmmo;
            }
        }

        private bool? _isLaserAmmo;

        public bool IsLaserAmmo
        {
            get
            {
                if (CategoryId != (int)CategoryID.Charge) return false;

                if (_isLaserAmmo == null)
                {
                    if (GroupId != 0)
                    {
                        switch (GroupId)
                        {
                            case (int)Group.FrequencyCrystal:
                            case (int)Group.AdvancedBeamLaserCrystal:
                            case (int)Group.AdvancedPulseLaserCrystal:
                                {
                                    _isLaserAmmo = true;
                                    return (bool)_isLaserAmmo;
                                }
                        }

                        _isLaserAmmo = false;
                        return (bool)_isLaserAmmo;
                    }

                    _isLaserAmmo = false;
                    return (bool)_isLaserAmmo;
                }

                return (bool)_isLaserAmmo;
            }
        }


        public string GroupName
        {
            get
            {
                if (string.IsNullOrEmpty(_groupName))
                    _groupName = (string) PySharp.Import("evetypes")
                        .Attribute("localizationUtils")
                        .Call("GetLocalizedGroupName", (int) PyInvGroup.Attribute("groupNameID"), "en-us");
                return _groupName;
            }
        }

        public int IconId
        {
            get
            {
                if (!_iconId.HasValue)
                    _iconId = (int) PyInvType.Attribute("iconID");

                return _iconId.Value;
            }
        }

        public int MarketGroupId
        {
            get
            {
                if (!_marketGroupId.HasValue)
                    _marketGroupId = (int) PyInvType.Attribute("marketGroupID");

                return _marketGroupId.Value;
            }
        }

        public double Mass
        {
            get
            {
                if (!_mass.HasValue)
                    _mass = (double) PyInvType.Attribute("mass");

                return _mass.Value;
            }
        }

        public int PortionSize
        {
            get
            {
                if (!_portionSize.HasValue)
                    _portionSize = (int) PyInvType.Attribute("portionSize");

                return _portionSize.Value;
            }
        }

        public bool Published
        {
            get
            {
                if (!_published.HasValue)
                    _published = (bool) PyInvType.Attribute("published");

                return _published.Value;
            }
        }

        public int RaceId
        {
            get
            {
                if (!_raceId.HasValue)
                    _raceId = (int) PyInvType.Attribute("raceID");

                return _raceId.Value;
            }
        }

        public double Radius
        {
            get
            {
                if (!_radius.HasValue)
                    _radius = (float)PyInvType.Attribute("radius");

                return _radius.Value;
            }
        }

        public double SignatureRadius
        {
            get
            {
                if (!_signatureRadius.HasValue)
                    _signatureRadius = new int?((int)TryGet<float>("signatureRadius")) ?? 0;
                return _signatureRadius.Value;
            }
        }

        public int SoundId
        {
            get
            {
                if (!_soundId.HasValue)
                    _soundId = (int) PyInvType.Attribute("soundID");

                return _soundId.Value;
            }
        }

        public double? MaxArmor
        {
            get
            {
                if (!_maxArmor.HasValue)
                {
                    _maxArmor = TryGet<float>("armorHP");
                    if (DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.weatherInfernal))
                        _maxArmor *= 1.5;
                }

                return _maxArmor;
            }
        }

        public double? MaxShield
        {
            get
            {
                if (!_maxShield.HasValue)
                {
                    _maxShield = TryGet<float>("shieldCapacity");
                    if (DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.weatherXenonGas))
                        _maxShield *= 1.5;
                }

                return _maxShield;
            }
        }

        public double? MaxStructure
        {
            get
            {
                if (!_maxStructure.HasValue)
                    _maxStructure = TryGet<float>("hp");
                return _maxStructure;
            }
        }

        private float? _maxRange;

        public float? MaxRange
        {
            get
            {
                if (!_maxRange.HasValue)
                    _maxRange = TryGet<float>("maxRange");
                return _maxRange;
            }
        }

        public int TypeId { get; internal set; }

        public string TypeName
        {
            get
            {
                if (string.IsNullOrEmpty(_typeName))
                    _typeName = (string) PySharp.Import("evetypes")
                        .Attribute("localizationUtils")
                        .Call("GetLocalizedTypeName", (int) PyInvType.Attribute("typeNameID"), "en-us");

                return _typeName;
            }
        }

        public bool IsAllowedInThisHoldType(string NameOfHold)
        {
            if (NameOfHold == "CurrentCargoHold")
                return true;
            if (NameOfHold == "CurrentShipsAmmoHold")
                return IsAllowedInAmmoHold;
            if (NameOfHold == "CurrentShipsGasHold")
                return IsAllowedInGasHold;
            if (NameOfHold == "CurrentShipsGeneralMiningHold")
                return IsAllowedInGeneralMiningHold;
            if (NameOfHold == "CurrentShipsIceHold")
                return IsAllowedInIceHold;
            if (NameOfHold == "CurrentShipsMineralHold")
                return IsAllowedInMineralHold;
            if (NameOfHold == "CurrentShipsOreHold")
                return IsAllowedInOreHold;

            return true;
        }

        public bool IsAllowedInAmmoHold
        {
            get
            {
                if (CategoryId == (int)CategoryID.Charge)
                    return true;

                return false;
            }
        }
        public bool IsAllowedInOreHold
        {
            get
            {
                if (CategoryId == (int)CategoryID.Asteroid)
                    return true;

                if (GroupId == (int)Group.Veldspar)
                    return true;

                if (GroupId == (int)Group.Scordite)
                    return true;

                return false;
            }
        }

        public bool IsAllowedInGeneralMiningHold
        {
            get
            {
                if (GroupId == (int)Group.Ice)
                    return false;

                if (CategoryId == (int)CategoryID.Asteroid)
                    return true;

                if (GroupId == (int)Group.Veldspar)
                    return true;

                if (GroupId == (int)Group.Scordite)
                    return true;

                return false;
            }
        }

        public bool IsAllowedInIceHold
        {
            get
            {
                if (GroupId == (int)Group.Ice)
                    return true;

                return false;
            }
        }

        public bool IsAllowedInMineralHold
        {
            get
            {
                if (GroupId == (int)Group.Minerals)
                    return true;

                return false;
            }
        }

        public bool IsAllowedInGasHold
        {
            get
            {
                if (GroupId == (int)Group.GasCloud)
                    return true;

                return false;
            }
        }

        public double Volume
        {
            get
            {
                if (!_volume.HasValue)
                    _volume = (double) PyInvType.Attribute("volume");

                return _volume.Value;
            }
        }

        public double MissileLaunchDuration => missileLaunchDuration ??= TryGet<double>("missileLaunchDuration");
        public double RateOfFire => rateOfFire ??= TryGet<double>("speed");
        public double OptimalRange => optimalRange ??= TryGet<double>("maxRange");
        public double OptimalSigRadius => optimalSigRadius ??= TryGet<double>("optimalSigRadius");

        public double TurretDamageMultiplier => damageModifier ??= TryGet<double>("damageMultiplier");

        public double DamageModifier => damageModifier ??= TryGet<double>("damageMultiplier");
        public double MaxtargetingRange => maxtargetingRange ??= TryGet<double>("maxTargetRange");
        public double DamageEm => damageEm ??= TryGet<double>("emDamage");
        public double DamageExplosive => damageExplosive ??= TryGet<double>("explosiveDamage");
        public double Damagekinetic => damagekinetic ??= TryGet<double>("kineticDamage");

        public double AoeDamageReductionFactor => aoeDamageReductionFactor ??= TryGet<double>("aoeDamageReductionFactor");


        public double MissileEntityAoeVelocityMultiplier => missileEntityAoeVelocityMultiplier ??= TryGet<double>("missileEntityAoeVelocityMultiplier");

        public double ExplosionVelocity => aoeVelocity ??= TryGet<double>("aoeVelocity");

        public double EntityMissileTypeID => entityMissileTypeID ??= TryGet<double>("entityMissileTypeID");
        public double DamageThermal => damageThermal ??= TryGet<double>("thermalDamage");
        public double AccuracyFalloff => accuracyFalloff ??= TryGet<double>("falloff");
        public double TurretTracking => turretTracking ??= TryGet<double>("trackingSpeed");
        public double DamageMultiplierBonusMax => damageMultiplierBonusMax ??= TryGet<double>("damageMultiplierBonusMax");
        public double MissileDamageMultiplier => missileDamageMultiplier ??= TryGet<double>("missileDamageMultiplier");

        public int MaxLockedTargets => maxLockedTargets ??= TryGet<int>("maxLockedTargets");

        // Have to find the ingame names for these
        //public double DisintergratorDamageMultiplierPerCycle => disintergratorDamageMultiplierPerCycle ??= (double)PyInvType.Attribute("thermalDamage");
        //public double DisintergratorMaxDamageMultiplier => disintergratorMaxDamageMultiplier ??= (double)PyInvType.Attribute("thermalDamage");


        internal PyObject PyInvCategory => PySharp.Import("evetypes").Call("GetCategory", this.CategoryId);

        internal PyObject PyInvGroup => PySharp.Import("evetypes").Call("GetGroup", this.GroupId);

        internal PyObject PyInvType => PySharp.Import("evetypes").Call("GetType", this.TypeId);

        #endregion Properties

        #region Methods

        private static Dictionary<int, string> _attributeNamesById;
        private Dictionary<int, string> GetAttributeNamesById()
        {
            if (_attributeNamesById == null)
            {
                _attributeNamesById = new Dictionary<int, string>();
                var attributeNameById = DirectEve.PySharp.Import("dogma.data").Call("get_attribute_names_by_id").ToDictionary<int>();
                foreach (var k in attributeNameById)
                {
                    _attributeNamesById.AddOrUpdate(k.Key, k.Value.ToUnicodeString());
                }
            }
            return _attributeNamesById;
        }

        private Dictionary<int, DirectDgmEffect> _dgmEffects;

        private static Dictionary<int, Dictionary<int, DirectDgmEffect>> _dgmEffectsCache = new Dictionary<int, Dictionary<int, DirectDgmEffect>>();
        public Dictionary<int, DirectDgmEffect> GetDmgEffects()
        {
            if (_dgmEffects == null && _dgmEffectsCache.TryGetValue(this.TypeId, out var val))
                _dgmEffects = val;

            if (_dgmEffects == null)
            {
                var ret = new Dictionary<int, DirectDgmEffect>();
                var dogmaIM = DirectEve.GetLocalSvc("clientDogmaStaticSvc");
                if (dogmaIM.IsValid)
                {
                    var effectsForThisType = dogmaIM.Call("TypeGetEffects", this.TypeId).ToDictionary<int>();
                    //var hasTypeEffects = dogmaData.Call("has_type_effects", this.TypeId).ToBool();
                    foreach (var eff in effectsForThisType)
                    {
                        var effect = dogmaIM.Call("GetEffect", eff.Key);
                        ret[eff.Key] = new DirectDgmEffect(DirectEve, effect);
                    }
                    _dgmEffects = ret;
                    _dgmEffectsCache[this.TypeId] = _dgmEffects;
                }
            }
            return _dgmEffects;
        }

        private Dictionary<string, DirectDgmEffect> _dgmEffectsByGuid;

        private static Dictionary<int, Dictionary<string, DirectDgmEffect>> _dgmEffectsByGuidCache = new Dictionary<int, Dictionary<string, DirectDgmEffect>>();
        public Dictionary<string, DirectDgmEffect> GetDmgEffectsByGuid()
        {
            if (_dgmEffectsByGuid == null && _dgmEffectsByGuidCache.TryGetValue(this.TypeId, out var val))
                _dgmEffectsByGuid = val;

            if (_dgmEffectsByGuid == null)
            {
                var ret = new Dictionary<string, DirectDgmEffect>();
                var dogmaIM = DirectEve.GetLocalSvc("clientDogmaStaticSvc");
                if (dogmaIM.IsValid)
                {
                    var effectsForThisType = dogmaIM.Call("TypeGetEffects", this.TypeId).ToDictionary<int>();
                    foreach (var eff in effectsForThisType)
                    {
                        var effect = dogmaIM.Call("GetEffect", eff.Key);
                        var dgmEffect = new DirectDgmEffect(DirectEve, effect);
                        if (!String.IsNullOrWhiteSpace(dgmEffect.Guid))
                        {
                            ret[dgmEffect.Guid] = dgmEffect;
                        }
                    }
                    _dgmEffectsByGuid = ret;
                    _dgmEffectsByGuidCache[this.TypeId] = _dgmEffectsByGuid;
                }
            }
            return _dgmEffectsByGuid;
        }

        private DateTime? _getBoosterConsumbableUntil = null;

        public DateTime GetBoosterConsumbableUntil()
        {

            if (_getBoosterConsumbableUntil.HasValue)
                return _getBoosterConsumbableUntil.Value;

            var ret = DateTime.MaxValue;

                var s = "boosterLastInjectionDatetime";
                if (this.GetAttributesInvType().ContainsKey(s))
                {
                    var boosterLastInjectionDatetime = this.TryGet<double>("boosterLastInjectionDatetime");
                    if (boosterLastInjectionDatetime > 0)
                    {
                        var result = PyObject.PY_EPOCH_TIME_TIME.AddDays(boosterLastInjectionDatetime);
                        _getBoosterConsumbableUntil = result;
                        return result;
                    }
                }

            _getBoosterConsumbableUntil = ret;
            return ret;
        }

        public Dictionary<string, object> GetAttributesInvType()
        {
            if (invTypeCache.TryGetValue(this.TypeId, out var cachedInvType) && cachedInvType._attrdictionary != null)
            {
                return cachedInvType._attrdictionary;
            }

            if (_attrdictionary == null)
            {
                _attrdictionary = new Dictionary<string, object>();
                var _dmgAttribute = DirectEve.PySharp.Import("dogma.data").Call("get_type_attributes_by_id", this.TypeId).ToDictionary<int>();
                var a = (int)DirectEve.Const["attributeCapacity"];
                var b = (int)DirectEve.Const["attributeVolume"];
                if (!_dmgAttribute.ContainsKey(a))
                    _dmgAttribute.AddOrUpdate(a, PySharp.PyNone);
                if (!_dmgAttribute.ContainsKey(b))
                    _dmgAttribute.AddOrUpdate(b, PySharp.PyNone);
                var attributeNamesbyId = GetAttributeNamesById();
                var attributeDataTypeTypeMirror = (int)DirectEve.Const["attributeDataTypeTypeMirror"];
                foreach (var k in _dmgAttribute)
                {
                    var attribute = DirectEve.PySharp.Import("dogma.data").Call("get_attribute", k.Key);
                    if (!attribute.IsValid)
                    {
                        DirectEve.Log($"ERROR: dogma.data.get_attribute return an invalid object.");
                        break;
                    }
                    var dataType = attribute.Attribute("dataType").ToInt();
                    if (dataType == attributeDataTypeTypeMirror)
                    {
                        if (attributeNamesbyId.TryGetValue(k.Key, out var key))
                        {
                            var value = DirectEve.PySharp.Import("evetypes").Call("GetAttributeForType", this.TypeId, key).GetValue(out var newVal, out var type);
                            _attrdictionary.Add(key, newVal);
                        }
                    }
                    else
                    {
                        if (attributeNamesbyId.TryGetValue(k.Key, out var key))
                        {
                            var val = k.Value.Attribute("value").GetValue(out var newVal, out var type);
                            _attrdictionary.Add(key, newVal);
                        }
                    }
                }
            }

            var exists = DirectEve.PySharp.Import("evetypes").Call("Exists", this.TypeId).ToBool();
            if (exists)
            {
                var invType = new DirectInvType(DirectEve, this.TypeId);
                invType._attrdictionary = _attrdictionary;
                invTypeCache[this.TypeId] = invType;
            }
            //invTypeCache[this.TypeId] = this; // add to cache
            return _attrdictionary;
        }

        /// <summary>
        ///     Retrieves the bracket data
        /// </summary>
        /// <returns></returns>
        public PyObject GetBracketData()
        {
            var bracketSvc = DirectEve.GetLocalSvc("bracket");

            int GetBracketId()
            {
                if (bracketSvc.IsValid)
                {
                    var bracketDataByTypeID = bracketSvc.Attribute("bracketDataByTypeID");
                    if (bracketDataByTypeID.IsValid)
                    {
                        var bd = bracketDataByTypeID.Call("get", TypeId, PySharp.PyNone);
                        if (bd.IsValid)
                            return bd.ToInt();
                    }

                    var bracketDataByGroupID = bracketSvc.Attribute("bracketDataByGroupID");
                    if (bracketDataByGroupID.IsValid)
                    {
                        var bd = bracketDataByGroupID.Call("get", GroupId, PySharp.PyNone);
                        if (bd.IsValid)
                            return bd.ToInt();
                    }

                    var bracketDataByCategoryID = bracketSvc.Attribute("bracketDataByCategoryID");
                    if (bracketDataByCategoryID.IsValid)
                    {
                        var bd = bracketDataByCategoryID.Call("get", CategoryId, PySharp.PyNone);
                        if (bd.IsValid)
                            return bd.ToInt();
                    }
                }

                return 0;
            }

            var bracketId = GetBracketId();
            if (bracketId != 0)
            {
                var bracketData = bracketSvc.Call("GetBrackeDatatByID", bracketId);
                return bracketData;
            }

            return PySharp.PyZero;
        }

        /// <summary>
        ///     Retrieves the bracket name, 'NPC Battleship' for example
        /// </summary>
        /// <returns></returns>
        public string GetBracketName()
        {
            if (DirectCache.BracketNameDictionary.TryGetValue(TypeId, out var name))
                return name;

            name = string.Empty;
            var bd = GetBracketData();
            if (bd.IsValid) name = (string) bd.Attribute("name");
            if (GroupId == 446)
            {
                DirectCache.BracketNameDictionary[TypeId] = BracketType.Navy_Concord_Customs.ToString();
            }
            else
            {
                DirectCache.BracketNameDictionary[TypeId] = name;
            }
            return DirectCache.BracketNameDictionary[TypeId];
        }

        public string GetBracketTexturePath()
        {
            if (DirectCache.BracketTexturePathDictionary.TryGetValue(TypeId, out var texturePath))
                return texturePath;

            texturePath = string.Empty;
            var bd = GetBracketData();
            if (bd.IsValid) texturePath = (string) bd.Attribute("texturePath");
            if (string.IsNullOrEmpty(texturePath))
                texturePath = string.Empty;
            DirectCache.BracketTexturePathDictionary[TypeId] = texturePath;
            return texturePath;
        }

        public virtual T TryGet<T>(string keyname)
        {
            object obj = null;
            if (GetAttributesInvType().ContainsKey(keyname))
            {
                var item = GetAttributesInvType()[keyname];
                if (item != null)
                {
                    if (typeof(T) == typeof(bool))
                    {
                        obj = (int) item;
                        return (T) obj;
                    }
                    if (typeof(T) == typeof(string))
                    {
                        obj = (string) item;
                        return (T) obj;
                    }
                    if (typeof(T) == typeof(int))
                    {
                        obj = (int) item;
                        return (T) obj;
                    }
                    if (typeof(T) == typeof(long))
                    {
                        obj = (long) item;
                        return (T) obj;
                    }
                    if (typeof(T) == typeof(float))
                    {
                        obj = (float) item;
                        return (T) obj;
                    }
                    if (typeof(T) == typeof(double))
                    {
                        obj = Convert.ToDouble(item);
                        return (T) obj;
                    }
                    if (typeof(T) == typeof(DateTime))
                    {
                        obj = Convert.ToDateTime(item);
                        return (T) obj;
                    }
                }
            }
            return default(T);
        }

        internal static DirectInvType GetInvType(DirectEve directEve, int typeId)
        {
            if (DirectCache.InvTypeCache.TryGetValue(typeId, out DirectInvType cachedInvType))
            {
                return cachedInvType;
            }
            else
            {
                var exists = directEve.PySharp.Import("evetypes").Call("Exists", typeId).ToBool();
                DirectInvType ret = null;
                if (exists)
                {
                    ret = new DirectInvType(directEve, typeId);
                    DirectCache.InvTypeCache[typeId] = ret;
                }

                return ret;
            }
        }

        /// <summary>
        /// Per second
        /// </summary>
        public double FlatArmorLocalRepairAmount
        {
            get
            {
                var amount = TryGet<float>("behaviorArmorRepairerAmount");
                var duration = TryGet<float>("behaviorArmorRepairerDuration"); // milliseconds

                if (duration <= 0)
                    return 0;

                if (amount <= 0)
                    return 0;

                return amount / (duration / 1000);
            }
        }
        /// <summary>
        /// Per second
        /// </summary>
        public double FlatShieldLocalRepairAmount
        {
            get
            {
                var amount = TryGet<float>("behaviorShieldBoosterAmount");
                var duration = TryGet<float>("behaviorShieldBoosterDuration"); // milliseconds

                if (duration <= 0)
                    return 0;

                if (amount <= 0)
                    return 0;

                return amount / (duration / 1000);
            }
        }

        public double FlatShieldArmorLocalRepairAmountCombined
        {
            get
            {
                return FlatArmorLocalRepairAmount + FlatShieldLocalRepairAmount;
            }
        }


        //public static Dictionary<string, int> GetInvTypeNames(DirectEve directEve)
        //{
        //    var result = new Dictionary<string, int>();
        //    var pyDict = directEve.PySharp.Import("evetypes").Attribute("storages").Attribute("TypeStorage").Attribute("_storage").ToDictionary<int>();
        //    foreach (var pair in pyDict) result[new DirectInvType(directEve, pair.Key).TypeName] = pair.Key;
        //    return result;
        //}

        #endregion Methods
    }
}