extern alias SC;

using EVESharpCore.Controllers;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Events;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py.D3DDetour;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EVESharpCore.Cache
{
    public partial class ESCache
    {
        #region Constructors

        private ESCache()
        {
            LastModuleTargetIDs = new Dictionary<long, long>();
            TargetingIDs = new Dictionary<long, DateTime>();
            _entitiesById = new Dictionary<long, EntityCache>();

            LootedContainers = new HashSet<long>();
        }

        #endregion Constructors

        #region Fields

        public DateTime NextSlotActivate = DateTime.UtcNow;

        private DirectContainer _fittedModules;
        private int? _weaponRange;
        private int? _miningRange;
        public HashSet<long> ListNeutralizingEntities = new HashSet<long>();
        public HashSet<long> ListofContainersToLoot = new HashSet<long>();
        public HashSet<long> ListOfDampeningEntities = new HashSet<long>();
        public HashSet<long> ListofEntitiesToEcm = new HashSet<long>();
        public HashSet<long> ListofEntitiesToTrackingDisrupt = new HashSet<long>();
        public HashSet<long> ListOfJammingEntities = new HashSet<long>();
        public HashSet<string> ListofMissionCompletionItemsToLoot = new HashSet<string>();
        public HashSet<long> ListOfTargetPaintingEntities = new HashSet<long>();
        public HashSet<long> ListOfTrackingDisruptingEntities = new HashSet<long>();
        public HashSet<long> ListofWebbingEntities = new HashSet<long>();
        public bool MissionBookmarkTimerSet;
        public DirectLocation MissionSolarSystem;
        public bool NormalNavigation = true;
        public string OrbitEntityNamed;
        public bool RouteIsAllHighSecBool;
        private readonly Dictionary<long, EntityCache> _entitiesById;
        private List<EntityCache> _abyssalBigObjects;
        private List<EntityCache> _abyssalDeadspaceBioluminescenceCloud;
        private List<EntityCache> _abyssalDeadspaceTachyonCausticCloud;
        private List<EntityCache> _abyssalDeadspaceDeviantAutomataSuppressor;
        private EntityCache _abyssalDeadspaceSmallDeviantAutomataSuppressor;
        private EntityCache _abyssalDeadspaceMediumDeviantAutomataSuppressor;
        private List<EntityCache> _abyssalDeadspaceFilamentCloud;
        private List<EntityCache> _abyssalDeadspaceMultibodyTrackingPylon;
        private List<EntityCache> _bigObjectsAndGates;
        private List<EntityCache> _containers;
        private DirectContainer _currentShipsCargo;
        private DirectContainer _currentShipsFleetHangar;
        private DirectContainer _currentShipsGeneralMiningHold;

        private DirectContainer _currentShipsAmmoHold;
        private DirectContainer _currentShipsGasHold;
        private DirectContainer _currentShipsIceHold;
        private DirectContainer _currentShipsModules;
        private DirectContainer _currentShipsOreHold;
        private DirectContainer _currentShipsMineralHold;
        private EveAccount _eveAccount;
        private EveSetting _eveSetting;
        private List<EntityCache> _gates;
        private bool? _inAbyssalDeadspace;
        private bool? _inMission;
        private bool? _insidePosForceField { get; set; }
        private int? _maxLockedTargets;
        public long? OldAccelerationGateId = null;
        private List<ModuleCache> _modules { get; set; }
        private DirectItem _myCurrentAmmoInWeapon;
        private List<DirectBookmark> _safeSpotBookmarks;
        private EntityCache _star;
        private EntityCache _stargate;
        private List<EntityCache> _stargates { get; set; }
        private List<EntityCache> _targeting;
        private List<EntityCache> _targets { get; set; }
        private List<EntityCache> _unlootedContainers;
        private List<EntityCache> _unlootedWrecksAndSecureCans;
        private List<ModuleCache> _weapons;
        private List<ModuleCache> _miningEquipment;
        private List<DirectWindow> _windows;
        public static bool LootAlreadyUnloaded { get; set; }

        private bool? needRepair = false;

        public bool NeedRepair
        {
            get
            {
                if (ESCache.Instance.EveAccount.NeedRepair)
                    return true;

                if (needRepair != null)
                    return needRepair.Value;

                if (DateTime.UtcNow > Time.Instance.LastAlwaysRepairResetNeedRepair.AddSeconds(15))
                {
                    if (ESCache.Instance.EveAccount.AlwaysRepair)
                    {
                        needRepair = true;
                        Time.Instance.LastAlwaysRepairResetNeedRepair = DateTime.UtcNow;
                        return needRepair ?? true;
                    }
                }

                return needRepair ?? true;
            }
            set
            {
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(ESCache.Instance.EveAccount.NeedRepair), value);
                needRepair = value;
            }
        }

        #endregion Fields

        #region Properties

        public static ESCache Instance { get; } = new ESCache();

        //ignoreTrackingPolyons
        public bool IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings
        {
            // Multibody Tracking Pylon: +60% or +80% tracking to all ships in its area of effect.
            // This can be helpful for fighting small, fast enemies.
            // It helps your drones as well as your ship.
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    return false;

                //if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                //    return true;

                //Add more situations where we have small targets in the spawn
                //if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains))
                //    return true;

                return false;
            }
        }

        //ignoreAutomataPylon
        //ignoreWideAreaAutomataPylon
        public bool IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles
        {
            // Deviant Automata Suppressor: Damages all missiles, drones, and rogue drone frigates within its area of effect.
            // The larger tower does about the same damage to drones as a single medium smartbomb; the smaller does more than double that.
            // Flying into the range of this Suppressor can help you take out pirate drones.
            // Watch out for your own drones.

            //Short-Range Deviant Suppressor will attack all drones, missiles and rogue drone frigates within 15 KM
            //Medium-Range Deviant Suppressor will attack all drones, missiles, and rogue drone frigates within 40 KM
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    return true;

                //if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                //    return true;

                //if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                //    return true;

                //Add more situations where we have rogue drones in the spawn but its not detected as a DroneFrigateSpawn
                //if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains))
                //    return true;


                return false;
            }
        }
        public bool IgnoreBioClouds_Blue_4xSignatureRadius
        {
            // Bioluminescence Cloud (light blue): +300% Signature Radius (4.0x signature radius multiplier).
            // Entering this cloud will make your ship an easier target to hit but it will also make all rats easier to hit.
            // If fighting small but accurate enemies like Damaviks, this cloud can actually be helpful, and you can lure the rats into it.
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                    return true;

                return false;
            }
        }

        //IgnoreFilaClouds
        public bool IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty
        {
            // Bioluminescence Cloud (light blue): +300% Signature Radius (4.0x signature radius multiplier).
            // Entering this cloud will make your ship an easier target to hit but it will also make all rats easier to hit.
            // If fighting small but accurate enemies like Damaviks, this cloud can actually be helpful, and you can lure the rats into it.
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        return false;

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        return false;

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Vedmak")) >= 3)
                        return false;

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 4)
                        return false;

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        return false;

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Devoted Knight")) >= 2)
                        return false;

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Kiki")) >= 5)
                        return false;

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                    return true;

                return false;
            }
        }
        //IgnoreTachClouds
        public bool IgnoreTachClouds_White_4xSpeedBoost
        {
            // Bioluminescence Cloud (light blue): +300% Signature Radius (4.0x signature radius multiplier).
            // Entering this cloud will make your ship an easier target to hit but it will also make all rats easier to hit.
            // If fighting small but accurate enemies like Damaviks, this cloud can actually be helpful, and you can lure the rats into it.
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    if (ESCache.Instance.AbyssalGate.Distance > 40000)
                        return true;

                    return false;
                }

                return false;
            }
        }
        public EntityCache AbyssalGate
        {
            get
            {
                try
                {
                    if (EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.AbyssEncounterGate || i.TypeId == (int)TypeID.AbyssExitGate))
                    {
                        return EntitiesOnGrid.FirstOrDefault(i => i.TypeId == (int)TypeID.AbyssEncounterGate || i.TypeId == (int)TypeID.AbyssExitGate) ?? null;
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

        public bool TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway { get; set; } = true;

        public bool? _triglavianConstructionSiteSpawnFoundDozenPlusBSs;

        public bool TriglavianConstructionSiteSpawnFoundDozenPlusBSs
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    //
                    // what type of filament are we using? If its not a tier 5 filament we should just assume this cant happen
                    // https://wiki.eveuniversity.org/Filaments
                    //

                    //if (!AbyssalDeadspaceFilamentName.ToLower().Contains("Chaotic".ToLower()))
                    //    return false;

                    if (_triglavianConstructionSiteSpawnFoundDozenPlusBSs == null)
                    {
                        if (ESCache.Instance.Entities.Any(i => i.TypeId == (int)TypeID.TriglavianConstruction01a) && ESCache.Instance.Entities.Count(i => i.Name.Contains("Leshak")) >= 8)
                        {
                            _triglavianConstructionSiteSpawnFoundDozenPlusBSs = true;
                            DebugConfig.DebugAbyssalDeadspaceBehavior = true;
                            DebugConfig.DebugCombat = true;
                            DebugConfig.DebugActivateWeapons = true;
                            DebugConfig.DebugKillTargets = true;

                            DebugConfig.DebugDisableCombat = true;
                            return _triglavianConstructionSiteSpawnFoundDozenPlusBSs ?? true;
                        }

                        DebugConfig.DebugAbyssalDeadspaceBehavior = false;
                        DebugConfig.DebugCombat = false;
                        DebugConfig.DebugActivateWeapons = false;
                        DebugConfig.DebugKillTargets = false;
                        DebugConfig.DebugDisableCombat = false;
                        _triglavianConstructionSiteSpawnFoundDozenPlusBSs = false;
                        return _triglavianConstructionSiteSpawnFoundDozenPlusBSs ?? false;
                    }

                    return _triglavianConstructionSiteSpawnFoundDozenPlusBSs ?? false;
                }

                _triglavianConstructionSiteSpawnFoundDozenPlusBSs = false;
                return _triglavianConstructionSiteSpawnFoundDozenPlusBSs ?? false;
            }
        }

        public EntityCache EntityWithHighestNeutingPotential
        {
            get
            {
                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasNeutralizers))
                    {
                        return Combat.PotentialCombatTargets.OrderByDescending(i => i._directEntity.GigaJouleNeutedPerSecond).FirstOrDefault();
                    }

                    return null;
                }

                return null;
            }
        }

        private List<DirectWorldPosition> _abyssalSphereCoordinates = new List<DirectWorldPosition>();

        public List<DirectWorldPosition> AbyssalSphereCoordinates
        {
            get
            {
                try
                {
                    if (_abyssalSphereCoordinates != null && _abyssalSphereCoordinates.Any())
                    {
                        return _abyssalSphereCoordinates;
                    }

                    _abyssalSphereCoordinates = ESCache.Instance.AbyssalCenter._directEntity.SphereCoordinates(30000, 20, 20);

                    if (_abyssalSphereCoordinates != null && _abyssalSphereCoordinates.Any())
                    {
                        int intCount = 0;
                        foreach (var abyssalSphereCoordinate in _abyssalSphereCoordinates)
                        {
                            intCount++;
                            if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("[" + intCount + "] abyssalSphereCoordinate: Distance [" + Math.Round(abyssalSphereCoordinate.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) / 1000, 0) + "]");
                            continue;
                        }

                        return _abyssalSphereCoordinates;
                    }

                    Log.WriteLine("! if (!_abyssalSphereCoordinates.Any()) !");
                    return new List<DirectWorldPosition>();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<DirectWorldPosition>();
                }
            }
        }

        private Dictionary<double, DirectWorldPosition> _sphereCoordDistances = new Dictionary<double, DirectWorldPosition>();

        public Dictionary<double, DirectWorldPosition> SphereCoordDistancesFrom(DirectEntity thisEntity)
        {
            try
            {
                if (thisEntity == null)
                    thisEntity = ESCache.Instance.ActiveShip.Entity;

                if (ESCache.Instance.AbyssalSphereCoordinates.Any())
                {
                    int intCountCoordPoint = 0;
                    if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (ESCache.Instance.AbyssalSphereCoordinates.Any())");
                    _sphereCoordDistances = new Dictionary<double, DirectWorldPosition>();
                    foreach (var vecCoordPoint in ESCache.Instance.AbyssalSphereCoordinates)
                    {
                        intCountCoordPoint++;
                        _sphereCoordDistances.AddOrUpdate(thisEntity.DirectAbsolutePosition.GetDistance(vecCoordPoint), vecCoordPoint);
                        continue;
                    }

                    if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("SphereCoordDistances [" + _sphereCoordDistances.Count() + "] found");
                    return _sphereCoordDistances ?? new Dictionary<double, DirectWorldPosition>();
                }
                else Log.WriteLine("! if (ESCache.Instance.AbyssalSphereCoordinates.Any()) !!");

                return new Dictionary<double, DirectWorldPosition>();
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                if (_sphereCoordDistances != null && _sphereCoordDistances.Any())
                {
                    return _sphereCoordDistances;
                }

                return new Dictionary<double, DirectWorldPosition>();
            }
        }

        private DirectWorldPosition _abyssalSpeedTankSpot { get; set; } = null;
        private DateTime _lastAbyssalSpeedTankSpot = DateTime.MinValue;

        internal DirectWorldPosition AbyssalSpeedTankSpot
        {
            get
            {
                try
                {
                    if (_lastAbyssalSpeedTankSpot.AddSeconds(8) > DateTime.UtcNow && _abyssalSpeedTankSpot != null && _abyssalSpeedTankSpot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) > 12000)
                        return _abyssalSpeedTankSpot;

                    _lastAbyssalSpeedTankSpot = DateTime.UtcNow;

                    DirectEntity _entityWeWantToAvoid = null;
                    if (Combat.PotentialCombatTargets.Any())
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNeutralizingMe || i.IsWebbingMe))
                        {
                            _entityWeWantToAvoid = Combat.PotentialCombatTargets.Where(x => x.IsNeutralizingMe || x.IsWebbingMe).OrderBy(i => i.Distance).FirstOrDefault()._directEntity;
                        }
                        else if (Combat.PotentialCombatTargets.Any())
                        {
                            _entityWeWantToAvoid = Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault()._directEntity;
                        }

                        if (_entityWeWantToAvoid == null)
                            _entityWeWantToAvoid = ESCache.Instance.ActiveShip.Entity;


                        Dictionary<double, DirectWorldPosition> cachedSphereCoordDistancesFromNpc = ESCache.Instance.SphereCoordDistancesFrom(_entityWeWantToAvoid);
                        Dictionary<double, DirectWorldPosition> cachedSphereCoordDistancesFromMe = ESCache.Instance.SphereCoordDistancesFrom(ESCache.Instance.ActiveShip.Entity);

                        /**
                        if (_entityWeWantToAvoid.Id != ESCache.Instance.ActiveShip.Entity.Id)
                        {
                            IEnumerable<KeyValuePair<double, DirectWorldPosition>> spotsFarFromEntityWeWantToAvoid = cachedSphereCoordDistancesFromNpc.Where(x => x.Key > 25000 && Combat.MaxRange > x.Key);
                            IEnumerable<KeyValuePair<double, DirectWorldPosition>> cachedSpotsWeCanUse = cachedSphereCoordDistancesFromMe.Where(Mykvp => Mykvp.Key > 10000 && cachedSphereCoordDistancesFromNpc.Any(NpcKvp => NpcKvp.Value.PositionInSpace == Mykvp.Value.PositionInSpace));
                            var cachedSpotWeShouldUse =
                            _abyssalSpeedTankSpot = cachedSpotsWeCanUse.OrderBy(x => x.Key).FirstOrDefault().Value;
                            return _abyssalSpeedTankSpot;
                        }

                        _abyssalSpeedTankSpot = cachedSphereCoordDistancesFromMe.Where(x => x.Value.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) > 10000).OrderBy(i => i.Key).FirstOrDefault().Value;

                        **/
                        if (cachedSphereCoordDistancesFromNpc.Any())
                        {
                            _abyssalSpeedTankSpot = cachedSphereCoordDistancesFromNpc.OrderByDescending(i => i.Key).FirstOrDefault().Value;
                            return _abyssalSpeedTankSpot ?? null;
                        }

                        return _abyssalSpeedTankSpot ?? null;
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

        public bool SellError { get; set; }



        private EntityCache _abyssalCenter;

        public EntityCache AbyssalCenter
        {
            get
            {
                try
                {
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (ESCache.Instance.Entities.Any())
                        {
                            if (ESCache.Instance.Entities.Any(i => i.IsAbyssalCenter))
                            {
                                _abyssalCenter = ESCache.Instance.Entities.FirstOrDefault(i => i.IsAbyssalCenter);
                                if (_abyssalCenter != null)
                                {
                                    try
                                    {
                                        //_abyssalCenter._directEntity.DrawSphere(DebugConfig.DebugAbyssalCenterDrawDistance);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.WriteLine("Exception [" + ex + "]");
                                    }

                                    return _abyssalCenter;
                                }

                                Log.WriteLine("!! false if (_abyssalCenter != null) false !!");
                                return null;
                            }

                            Log.WriteLine("!! false if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalCenter)) false !!");
                            //how?
                            return ESCache.Instance.AbyssalGate ?? null;
                        }

                        Log.WriteLine("!! false if (ESCache.Instance.EntitiesOnGrid.Any()) false !!");
                        return null;
                    }

                    Log.WriteLine("!! false if (ESCache.Instance.InAbyssalDeadspace) false !!");
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return ESCache.Instance.AbyssalGate ?? null;
                }
            }
        }

        private EntityCache _abyssalTrace;

        public EntityCache AbyssalTrace
        {
            get
            {
                if (_abyssalTrace == null)
                {
                    if (ESCache.Instance.EntitiesOnGrid.Any())
                    {
                        if (ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.GroupId == (int)Group.AbyssalTrace) != null)
                        {
                            _abyssalTrace = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.GroupId == (int)Group.AbyssalTrace);
                            try
                            {
                                //_abyssalTrace._directEntity.DrawSphere(250);
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }

                            return _abyssalTrace;
                        }

                        return null;
                    }

                    return null;
                }

                return _abyssalTrace;
            }
        }

        private EntityCache _abyssalMobileTractor { get; set; } = null;

        public EntityCache AbyssalMobileTractor
        {
            get
            {
                try
                {
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (ESCache.Instance.Entities.Any(i => i.GroupId == (int)Group.MobileTractor))
                        {
                            //should we make sure we own this MTU? in frigate or destroyer abyssals we might have another players MTU out!
                            _abyssalMobileTractor = ESCache.Instance.Entities.FirstOrDefault(i => i.GroupId == (int)Group.MobileTractor);
                            return _abyssalMobileTractor ?? null;
                        }

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
        }

        private EntityCache _playerSpawnLocation;

        public EntityCache PlayerSpawnLocation
        {
            get
            {
                if (_playerSpawnLocation == null)
                {
                    try
                    {
                        if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.EntitiesOnGrid.Any())
                        {
                            if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsPlayerSpawnLocation))
                            {
                                _playerSpawnLocation = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.IsPlayerSpawnLocation);
                                try
                                {
                                    //_playerSpawnLocation._directEntity.DrawSphere(500);
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteLine("Exception [" + ex + "]");
                                }

                                return _playerSpawnLocation;
                            }

                            return null;
                        }

                        return null;
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }
                }

                return _playerSpawnLocation;
            }
        }

        private DirectActiveShip _activeShip = null;

        public DirectActiveShip ActiveShip
        {
            get
            {
                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    return null;

                if (_activeShip != null)
                    return _activeShip;

                _activeShip = ESCache.Instance.DirectEve.ActiveShip;
                return _activeShip;
            }
        }

        private double? _unbonusedShieldBoosterDuration = 1;
        private double? _unbonusedShieldBoostAmount = 1;
        //private double? _unbonusedSignatureRadius = 1;
        //private double? _unbonusedVelocity = 1;
        //private double? _unbonusedInertia = 1;

        internal int SafeDistanceFromAbyssalCenter
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                {
                    if (ESCache.Instance.ActiveShip.Entity.Velocity > 2500)
                        return 54000;

                    if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                    {
                        return 54000;
                    }

                    return 57000;
                }

                return 60000;
            }
        }

        public double GetUnbonusedShieldBoosterDuration()
        {
            try
            {
                if (InStation)
                {
                    _unbonusedShieldBoosterDuration = null;
                    return _unbonusedShieldBoosterDuration ?? 0;
                }

                if (InSpace)
                {
                    if (_unbonusedShieldBoosterDuration == null)
                    {
                        if (ActiveShip.IsShieldTanked)
                        {
                            if (!ESCache.Instance.Modules.Any())
                                return 0;

                            if (ESCache.Instance.Modules.All(i => !i.IsShieldRepairModule))
                                return 0;

                            try
                            {
                                _unbonusedShieldBoosterDuration = ESCache.Instance.Modules.FirstOrDefault(i => i.IsShieldRepairModule).Duration;
                                Log.WriteLine("GetUnbonusedShieldBoosterDuration [" + _unbonusedShieldBoosterDuration + "]");
                                return _unbonusedShieldBoosterDuration ?? 0;
                            }
                            catch (Exception)
                            {
                                return 0;
                            }
                        }

                        _unbonusedShieldBoosterDuration = 1;
                        return _unbonusedShieldBoosterDuration ?? 0;
                    }

                    return _unbonusedShieldBoosterDuration ?? 0;
                }

                return _unbonusedShieldBoosterDuration ?? 0;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        public double GetUnbonusedShieldBoostAmount()
        {
            try
            {
                if (InStation)
                {
                    _unbonusedShieldBoostAmount = null;
                    return _unbonusedShieldBoostAmount ?? 0;
                }

                if (InSpace)
                {
                    if (_unbonusedShieldBoostAmount == null)
                    {
                        if (ActiveShip.IsShieldTanked)
                        {
                            try
                            {
                                if (!ESCache.Instance.Modules.Any())
                                    return 0;

                                if (ESCache.Instance.Modules.All(i => !i.IsShieldRepairModule))
                                    return 0;

                                _unbonusedShieldBoostAmount = ESCache.Instance.Modules.FirstOrDefault(i => i.IsShieldRepairModule)._module.ShieldBonus;
                                Log.WriteLine("GetUnbonusedShieldBoostAmount [" + Math.Round(_unbonusedShieldBoostAmount ?? 0, 0) + "]");
                                return _unbonusedShieldBoostAmount ?? 0;
                            }
                            catch (Exception)
                            {
                                return 0;
                            }
                        }

                        _unbonusedShieldBoostAmount = -1;
                        return _unbonusedShieldBoostAmount ?? -1;
                    }

                    return _unbonusedShieldBoostAmount ?? 0;
                }

                return _unbonusedShieldBoostAmount ?? 0;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        //public int UnbonusedSignatureRadius = 1;
        //public int UnbonusedVelocity = 1;
        //public int UnbonusedInertia = 1;

        public bool AfterMissionSalvaging { get; set; }
        public string CharName { get; set; }
        public DirectContainer ContainerInSpace { get; set; }
        public string CurrentPocketAction { get; set; }

        public DirectContainer CurrentShipsAmmoHold
        {
            get
            {
                try
                {
                    if (Time.Instance.NextJumpAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars || DebugConfig.DebugMiningBehavior) Log.WriteLine("CurrentShipsAmmoHold: if (Time.Instance.NextJumpAction > DateTime.UtcNow) return");
                        return null;
                    }

                    if (Time.Instance.NextDockAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars || DebugConfig.DebugMiningBehavior) Log.WriteLine("CurrentShipsOreHold: if (Time.Instance.NextDockAction > DateTime.UtcNow) return;");
                        return null;
                    }

                    if (!InSpace && !InStation) return null;

                    if (Instance.Windows.Count > 0)
                    {
                        if (_currentShipsAmmoHold == null)
                            _currentShipsAmmoHold = Instance.DirectEve.GetShipsAmmoHold();

                        return _currentShipsAmmoHold;
                    }

                    _currentShipsAmmoHold = null;
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public DirectContainer CurrentShipsCargo
        {
            get
            {
                try
                {
                    if (Time.Instance.NextJumpAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsCargo: if (Time.Instance.NextJumpAction > DateTime.UtcNow) return");
                        return null;
                    }

                    if (Time.Instance.NextDockAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsCargo: if (Time.Instance.NextDockAction > DateTime.UtcNow) return;");
                        return null;
                    }

                    if (Time.Instance.NextActivateAccelerationGate > DateTime.UtcNow && !ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsCargo: if (Time.Instance.NextActivateAction > DateTime.UtcNow) return;");
                        return null;
                    }

                    if (!InSpace && !InStation) return null;

                    if (Instance.Windows.Count > 0)
                    {
                        if (_currentShipsCargo == null)
                        {
                            if (Time.Instance.NextOpenCargoAction > DateTime.UtcNow)
                            {
                                if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsCargo: if (Time.Instance.NextOpenCargoAction > DateTime.UtcNow)");
                                return null;
                            }

                            _currentShipsCargo = Instance.DirectEve.GetShipsCargo();
                        }

                        return _currentShipsCargo;
                    }

                    _currentShipsCargo = null;
                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Unable to complete ReadyCargoHold [" + exception + "]");
                    return null;
                }
            }
        }

        public DirectContainer CurrentShipsFleetHangar
        {
            get
            {
                try
                {
                    if (Time.Instance.NextJumpAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsFleetHangar: if (Time.Instance.NextJumpAction > DateTime.UtcNow) return");
                        return null;
                    }

                    if (Time.Instance.NextDockAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsFleetHangar: if (Time.Instance.NextDockAction > DateTime.UtcNow) return;");
                        return null;
                    }

                    if (!InSpace && !InStation) return null;

                    if (Instance.Windows.Count > 0)
                    {
                        if (_currentShipsFleetHangar == null)
                            _currentShipsFleetHangar = Instance.DirectEve.GetShipsFleetHangar();

                        return _currentShipsFleetHangar;
                    }

                    _currentShipsFleetHangar = null;
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public DirectContainer CurrentShipsGeneralMiningHold
        {
            get
            {
                try
                {
                    if (Time.Instance.NextJumpAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsGeneralMiningHold: if (Time.Instance.NextJumpAction > DateTime.UtcNow) return");
                        return null;
                    }

                    if (Time.Instance.NextDockAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsGeneralMiningHold: if (Time.Instance.NextDockAction > DateTime.UtcNow) return;");
                        return null;
                    }

                    if (!InSpace && !InStation) return null;

                    if (Instance.Windows.Count > 0)
                    {

                        if (_currentShipsGeneralMiningHold == null)
                            _currentShipsGeneralMiningHold = Instance.DirectEve.GetShipsGeneralMiningHold();

                        return _currentShipsGeneralMiningHold;
                    }

                    _currentShipsGeneralMiningHold = null;
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public DirectContainer CurrentShipsModules
        {
            get
            {
                try
                {
                    if (Time.Instance.LastInWarp.AddSeconds(3) > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsModules: if (Time.Instance.LastInWarp.AddSeconds(10) > DateTime.UtcNow) return;");
                        return null;
                    }

                    if (Time.Instance.NextJumpAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsModules: if (Time.Instance.NextJumpAction > DateTime.UtcNow) return");
                        return null;
                    }

                    if (Time.Instance.NextDockAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars) Log.WriteLine("CurrentShipsModules: if (Time.Instance.NextDockAction > DateTime.UtcNow) return;");
                        return null;
                    }

                    if (!InSpace && !InStation) return null;

                    if (Instance.Windows.Count > 0)
                    {
                        if (_currentShipsModules == null)
                        {
                            _currentShipsModules = Instance.DirectEve.GetShipsCargo();
                            return _currentShipsModules;
                        }

                        return _currentShipsModules;
                    }

                    _currentShipsModules = null;
                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Unable to complete CurrentShipsModules [" + exception + "]");
                    return null;
                }
            }
        }

        //
        /// <summary>
        /// for Special Haulers, this is NOT the general mining hold that for instance a Hulk would have!
        /// </summary>
        public DirectContainer CurrentShipsOreHold
        {
            get
            {
                try
                {
                    if (Time.Instance.NextJumpAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars || DebugConfig.DebugMiningBehavior) Log.WriteLine("CurrentShipsOreHold: if (Time.Instance.NextJumpAction > DateTime.UtcNow) return");
                        return null;
                    }

                    if (Time.Instance.NextDockAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars || DebugConfig.DebugMiningBehavior) Log.WriteLine("CurrentShipsOreHold: if (Time.Instance.NextDockAction > DateTime.UtcNow) return;");
                        return null;
                    }

                    if (!InSpace && !InStation) return null;

                    if (Instance.Windows.Count > 0)
                    {
                        if (_currentShipsOreHold == null)
                            _currentShipsOreHold = Instance.DirectEve.GetShipsOreHold();

                        return _currentShipsOreHold;
                    }

                    _currentShipsOreHold = null;
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        /// <summary>
        /// This is a Mineral Hold - NOT a General Mining Hold: this type of Hold is only available on special haulers!
        /// </summary>
        public DirectContainer CurrentShipsMineralHold
        {
            get
            {
                try
                {
                    if (Time.Instance.NextJumpAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars || DebugConfig.DebugMiningBehavior) Log.WriteLine("CurrentShipsOreHold: if (Time.Instance.NextJumpAction > DateTime.UtcNow) return");
                        return null;
                    }

                    if (Time.Instance.NextDockAction > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugHangars || DebugConfig.DebugMiningBehavior) Log.WriteLine("CurrentShipsOreHold: if (Time.Instance.NextDockAction > DateTime.UtcNow) return;");
                        return null;
                    }

                    if (!InSpace && !InStation) return null;

                    if (Instance.Windows.Count > 0)
                    {
                        if (_currentShipsMineralHold == null)
                            _currentShipsMineralHold = Instance.DirectEve.GetShipsMineralHold();

                        return _currentShipsMineralHold;
                    }

                    _currentShipsMineralHold = null;
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public DirectEve DirectEve { get; set; }

        public EveAccount EveAccount
        {
            get
            {
                if (_eveAccount == null) //FIXME: This should probably refresh but it doesnt right now. info will get stale if it changes in the launcher. when we refreshed this every x sec it created problems: fps lag and logs being locked by the bot unable to open them without closing the bot
                    try
                    {
                        if (DirectEve.Interval(60000)) Console.WriteLine("WCFClient.Instance.GUID [" + WCFClient.Instance.GUID + "]");

                        if (string.IsNullOrEmpty(WCFClient.Instance.GUID))
                        {
                            if (DirectEve.Interval(60000)) Console.WriteLine("EveAccount: WCFClient.Instance.GUID IsNullOrEmpty?!?!");
                        }

                        if (DirectEve.Interval(60000)) Console.WriteLine("GetEveAccount using GUID");
                        _eveAccount = WCFClient.Instance.GetPipeProxy.GetEveAccount(WCFClient.Instance.GUID);

                        if (_eveAccount != null)
                        {
                            if (DirectEve.Interval(60000)) Console.WriteLine("Found EveAccount");
                        }

                        CancellationTokenSource eveAccountTokenSource = new CancellationTokenSource();
                        Task.Run(() =>
                        {
                            while (!eveAccountTokenSource.Token.IsCancellationRequested)
                            {
                                eveAccountTokenSource.Token.WaitHandle.WaitOne(2000);
                                try
                                {
                                    EveAccount r = _eveAccount;
                                    if (r != null)
                                        _eveAccount = r;
                                }
                                catch (Exception e)
                                {
                                    Log.WriteLine(e.ToString());
                                }
                            }
                        }, eveAccountTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        if (InStation)
                        {
                            ESCache.Instance.CloseEveReason = "Exception in EveAccount";
                            ESCache.Instance.BoolRestartEve = true;
                            Log.WriteLine("Exception [" + ex + "]");
                        }
                    }
                return _eveAccount;
            }
        }

        public EveSetting EveSetting
        {
            get
            {
                if (_eveSetting == null)
                    try
                    {
                        _eveSetting = WCFClient.Instance.GetPipeProxy.GetEVESettings();
                        CancellationTokenSource eveSettingTokenSource = new CancellationTokenSource();
                        Task.Run(() =>
                        {
                            while (!eveSettingTokenSource.Token.IsCancellationRequested)
                            {
                                eveSettingTokenSource.Token.WaitHandle.WaitOne(10000);
                                try
                                {
                                    EveSetting r = WCFClient.Instance.GetPipeProxy.GetEVESettings();
                                    if (r != null)
                                        _eveSetting = r;
                                }
                                catch (Exception e)
                                {
                                    Log.WriteLine(e.ToString());
                                }
                            }
                        }, eveSettingTokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(e.ToString());
                    }
                return _eveSetting;
            }
        }

        public DirectContainer FittedModules
        {
            get
            {
                try
                {
                    if (_fittedModules == null)
                        _fittedModules = Instance.DirectEve.GetShipsModules();

                    return _fittedModules;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public bool IsSafeToTravelIntoEmpireFromHere
        {
            get
            {
                //
                // Impossible
                //
                if (ESCache.Instance.DirectEve.Session.IsWspace) //no stargates
                    return false;

                //if (ESCache.Instance.DirectEve.Session.SolarSystem.IsTriglavianSystem) // no stargates
                //    return false;

                //
                // dangerous but possible
                //
                //if (ESCache.Instance.DirectEve.Session.SolarSystem.IsZeroZeroSpace)
                //    return false;

                //if (ESCache.Instance.DirectEve.Session.SolarSystem.IsLowSecuritySpace)
                //    return false;

                return true;
            }
        }
        public bool InWormHoleSpace
        {
            get
            {
                try
                {
                    if (InStation && !InSpace)
                        return false;

                    if (!InStation && !InSpace)
                        return false;

                    if (ESCache.Instance.InAbyssalDeadspace)
                        return false;

                    if (Stargates.Count > 0)
                        return false;

                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private List<EntityCache> _planets = new List<EntityCache>();

        public List<EntityCache> Planets
        {
            get
            {
                if (_planets != null)
                    return _planets ?? new List<EntityCache>();

                _planets = Entities.Where(i => i.IsPlanet).ToList();
                return _planets ?? new List<EntityCache>();
            }
        }

        private List<EntityCache> _celestials = new List<EntityCache>();

        public List<EntityCache> Celestials
        {
            get
            {
                if (_celestials != null)
                    return _celestials ?? new List<EntityCache>();

                _celestials = Entities.Where(i => i.IsCelestial).ToList();
                return _celestials ?? new List<EntityCache>();
            }
        }

        public EntityCache ClosestCelestial
        {
            get
            {
                if (Celestials.Count == 0)
                    return null;

                if (Celestials.Count > 0)
                    return Celestials.OrderBy(i => i.Distance).FirstOrDefault();

                return null;
            }
        }

        public EntityCache ClosestPlanet
        {
            get
            {
                if (Planets.Count == 0)
                    return null;

                if (Planets.Count > 0)
                    return Planets.OrderBy(i => i.Distance).FirstOrDefault();

                return null;
            }
        }

        private bool? _inAnomaly = null;

        public bool InAnomaly
        {
            get
            {
                if (_inAnomaly != null)
                    return _inAnomaly ?? false;

                if (InAbyssalDeadspace)
                {
                    _inAnomaly = false;
                    return _inAnomaly ?? false;
                }

                if (Stargates.Any(i => i.IsOnGridWithMe))
                {
                    _inAnomaly = false;
                    return _inAnomaly ?? false;
                }

                if (Entities.Any(i => i.IsStation && i.IsOnGridWithMe))
                {
                    _inAnomaly = false;
                    return _inAnomaly ?? false;
                }

                if (Entities.Any(i => i.IsCitadel && i.IsOnGridWithMe))
                {
                    _inAnomaly = false;
                    return _inAnomaly ?? false;
                }

                if (State.CurrentHighSecAnomalyBehaviorState == HighSecAnomalyBehaviorState.DoneWithCurrentAnomaly)
                {
                    _inAnomaly = false;
                    return _inAnomaly ?? false;
                }

                if (Entities.Any(i => i.TypeId == (int)TypeID.Anomaly))
                {
                    _inAnomaly = true;
                    return _inAnomaly ?? true;
                }

                _inAnomaly = false;
                return _inAnomaly ?? false;
            }
        }

        private bool? _inSite = null;

        public bool InSite
        {
            get
            {
                if (_inSite != null)
                    return _inSite ?? false;

                if (InAbyssalDeadspace)
                {
                    _inSite = false;
                    return _inSite ?? false;
                }

                if (Stargates.Any(i => i.IsOnGridWithMe))
                {
                    _inSite = false;
                    return _inSite ?? false;
                }

                if (Entities.Any(i => i.IsStation && i.IsOnGridWithMe))
                {
                    _inSite = false;
                    return _inSite ?? false;
                }

                if (Entities.Any(i => i.IsCitadel && i.IsOnGridWithMe))
                {
                    _inSite = false;
                    return _inSite ?? false;
                }

                if (State.CurrentHighSecAnomalyBehaviorState == HighSecAnomalyBehaviorState.DoneWithCurrentAnomaly)
                {
                    _inSite = false;
                    return _inSite ?? false;
                }

                if (Entities.Any(i => i.TypeId == (int)TypeID.Anomaly))
                {
                    _inSite = true;
                    return _inSite ?? true;
                }

                _inSite = false;
                return _inSite ?? false;
            }
        }

        public void TaskSetEveAccountAttribute(string attributeToUpdate, object valueOfAttribute)
        {
            if (attributeToUpdate == null)
            {
                //if (DebugConfig.DebugFpsLimits)
                //{
                    //if (DirectEve.Interval(90000)) Log.WriteLine("if (attributeToUpdate == null)");
                //}
            }

            if (valueOfAttribute == null)
            {
                //if (DebugConfig.DebugFpsLimits)
                //{
                    //if (DirectEve.Interval(90000)) Log.WriteLine("if (valueOfAttribute == null)");
                //}
            }

            Task.Run(() => WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, attributeToUpdate, valueOfAttribute));
        }

        public bool InAbyssalDeadspace
        {
            get
            {
                try
                {
                    if (DebugConfig.DebugFleetMgrPauseAndSetIsAbyssalDeadspaceTrue)
                    {
                        if (DirectEve.Interval(20000))
                        {
                            if (ESCache.Instance.InStation)
                            {
                                Log.WriteLine("DebugFleetMgrPauseAndSetIsAbyssalDeadspaceTrue [" + DebugConfig.DebugFleetMgrPauseAndSetIsAbyssalDeadspaceTrue + "] InStation! - Pausing!");
                                ControllerManager.Instance.SetPause(true);
                            }
                            else Log.WriteLine("DebugFleetMgrPauseAndSetIsAbyssalDeadspaceTrue [" + DebugConfig.DebugFleetMgrPauseAndSetIsAbyssalDeadspaceTrue + "] InSpace?!");
                        }

                        return true;
                    }

                    if (_inAbyssalDeadspace == null)
                    {
                        if (Instance.InSpace)
                        {
                            if (Instance.EntitiesNotSelf != null && Instance.EntitiesNotSelf.Count > 0)
                            {
                                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("InAbyssalDeadspace: We have [" + Instance.EntitiesNotSelf.Count + "] entities on grid: ");
                                if (Instance.Stargates.Count == 0)
                                {
                                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("InAbyssalDeadspace: We have no stargates: this must be either w-Space or AbyssalDeadspace");
                                    if (Instance.Star == null && ESCache.Instance.Entities.Any(i => i.IsAbyssalCenter))
                                    {
                                        //
                                        // this must be abyssal deadspace
                                        //
                                        if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("InAbyssalDeadspace: We have no star: this must be AbyssalDeadspace");
                                        _inAbyssalDeadspace = true;

                                        if (DirectEve.Interval(60000, 90000, _inAbyssalDeadspace.ToString()))
                                        {
                                            if (!EveAccount.IsInAbyss) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), _inAbyssalDeadspace);
                                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "InAbyssalDeadSpace [true]"));
                                        }

                                        return (bool)_inAbyssalDeadspace;
                                    }

                                    _inAbyssalDeadspace = false;
                                    if (DirectEve.Interval(60000, 90000, _inAbyssalDeadspace.ToString()))
                                    {
                                        if (EveAccount.IsInAbyss)
                                        {
                                            ESCache.Instance.TaskSetEveAccountAttribute("AbyssalPocketNumber", 0);
                                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), _inAbyssalDeadspace);
                                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "InAbyssalDeadSpace [false]"));
                                        }
                                    }

                                    return (bool)_inAbyssalDeadspace;
                                }

                                _inAbyssalDeadspace = false;
                                if (DirectEve.Interval(60000, 90000, _inAbyssalDeadspace.ToString()))
                                {
                                    if (EveAccount.IsInAbyss)
                                    {
                                        ESCache.Instance.TaskSetEveAccountAttribute("AbyssalPocketNumber", 0);
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), _inAbyssalDeadspace);
                                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "InAbyssalDeadSpace [false]"));
                                    }
                                }

                                return (bool)_inAbyssalDeadspace;
                            }

                            _inAbyssalDeadspace = false;
                            if (DirectEve.Interval(60000, 90000, _inAbyssalDeadspace.ToString()))
                            {
                                if (EveAccount.IsInAbyss)
                                {
                                    ESCache.Instance.TaskSetEveAccountAttribute("AbyssalPocketNumber", 0);
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), _inAbyssalDeadspace);
                                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "InAbyssalDeadSpace [false]"));
                                }
                            }

                            return (bool)_inAbyssalDeadspace;
                        }

                        _inAbyssalDeadspace = false;
                        if (DirectEve.Interval(60000, 90000, _inAbyssalDeadspace.ToString()))
                        {
                            if (EveAccount.IsInAbyss)
                            {
                                ESCache.Instance.TaskSetEveAccountAttribute("AbyssalPocketNumber", 0);
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), _inAbyssalDeadspace);
                                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "InAbyssalDeadSpace [false]"));
                            }
                        }

                        return (bool)_inAbyssalDeadspace;
                    }

                    return _inAbyssalDeadspace ?? false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public bool InMission
        {
            get
            {
                try
                {
                    if (Time.Instance.Started_DateTime.AddSeconds(45) > DateTime.UtcNow)
                        return false;

                    if (_inMission != null) return _inMission ?? false;

                    if (InWarp)
                        SetInMissionState(false);

                    if (InStation)
                        SetInMissionState(false);

                    if (DirectEve.Session.IsAbyssalDeadspace)
                        SetInMissionState(DirectEve.Session.IsAbyssalDeadspace);

                    if (Instance.SelectedController.Contains("Courier"))
                        SetInMissionState(false);

                    if (_inMission == null && ESCache.Instance.DockableLocations.Count > 0 && ESCache.Instance.ClosestDockableLocation.Distance < 8000000)
                        SetInMissionState(false);

                    if (_inMission == null && ESCache.Instance.Stargates.Count > 0 && ESCache.Instance.ClosestStargate.Distance < 1000000)
                        SetInMissionState(false);

                    if (_inMission == null && ESCache.Instance.Star != null && ESCache.Instance.Star.Distance < 50000)
                        SetInMissionState(false);

                    if (_inMission == null && Weapons.Count == 0)
                        SetInMissionState(false);

                    if (_inMission == null && ESCache.Instance.AccelerationGates.Count > 0)
                        SetInMissionState(true);

                    if (_inMission == null) SetInMissionState(true);
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.InMission), _inMission);
                    return _inMission ?? true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool InsidePosForceField
        {
            get
            {
                try
                {
                    if (_insidePosForceField == null)
                    {
                        _insidePosForceField = Instance.EntitiesOnGrid.Where(b => b.GroupId == (int)Group.ForceField && b.Distance <= 0).Any();
                        if (_insidePosForceField != null && (bool)_insidePosForceField)
                        {
                            if (DirectEve.Interval(30000, 30000, _insidePosForceField.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsSafeInPOS), true);
                            if (DirectEve.Interval(120000) & 50 > ESCache.Instance.ActiveShip.Entity.Velocity)
                            {
                                //Util.FlushMemIfThisProcessIsUsingTooMuchMemory(2048);
                            }
                        }
                        else if (EveAccount.IsSafeInPOS)
                            if (DirectEve.Interval(30000, 30000, _insidePosForceField.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsSafeInPOS), false);

                        return _insidePosForceField ?? false;
                    }

                    return _insidePosForceField ?? false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public Dictionary<long, long> LastModuleTargetIDs { get; }

        public HashSet<long> LootedContainers { get; }

        private bool? _paused = null;

        public bool Paused
        {
            get
            {
                try
                {
                    if (_paused != null)
                        return (bool)_paused;

                    _paused = false;
                    return _paused ?? false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
            set
            {
                _paused = value;
            }
        }

        private List<EntityCache> _myFleetMembersAsEntities { get; set; } = new List<EntityCache>();

        private bool? _isPvpAllowed { get; set; } = null;

        public bool IsPvpAllowed
        {
            get
            {
                if (_isPvpAllowed != null)
                    return (bool)_isPvpAllowed;

                if (ESCache.Instance.DirectEve.Session.SolarSystem.IsHighSecuritySpace && Settings.Instance.AllowPvpInHighSecuritySpace)
                {
                    if (ESCache.Instance.Wormholes.Any(i => i.IsOnGridWithMe && 16000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Stargates.Any(i => i.IsOnGridWithMe && 16000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Citadels.Any(i => i.IsOnGridWithMe && 10000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Stations.Any(i => i.IsOnGridWithMe && 10000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    //
                    //We dont allow PVP in high sec right now. with ALOT more testing maybe we can turn this on?
                    //
                    SetIsPvpAllowedState(false);
                    return (bool)_isPvpAllowed;
                }

                if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace && Settings.Instance.AllowPvpInAbyssalSpace)
                {
                    //
                    // maybe - depends
                    //
                    SetIsPvpAllowedState(true);
                    return (bool)_isPvpAllowed;
                }

                if (ESCache.Instance.DirectEve.Session.IsWspace && Settings.Instance.AllowPvpInWspace)
                {
                    if (ESCache.Instance.Wormholes.Any(i => i.IsOnGridWithMe && 16000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Citadels.Any(i => i.IsOnGridWithMe && 10000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Stations.Any(i => i.IsOnGridWithMe && 10000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    SetIsPvpAllowedState(true);
                    return (bool)_isPvpAllowed;
                }

                if (ESCache.Instance.DirectEve.Session.SolarSystem.IsZeroZeroSpace && Settings.Instance.AllowPvpInZeroZeroSpace)
                {
                    if (ESCache.Instance.Wormholes.Any(i => i.IsOnGridWithMe && 16000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Stargates.Any(i => i.IsOnGridWithMe && 16000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Citadels.Any(i => i.IsOnGridWithMe && 10000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Stations.Any(i => i.IsOnGridWithMe && 10000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    SetIsPvpAllowedState(true);
                    return (bool)_isPvpAllowed;
                }

                if (ESCache.Instance.DirectEve.Session.SolarSystem.IsLowSecuritySpace && Settings.Instance.AllowPvpInLowSecuritySpace)
                {
                    if (ESCache.Instance.Wormholes.Any(i => i.IsOnGridWithMe && 16000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Stargates.Any(i => i.IsOnGridWithMe && 16000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Citadels.Any(i => i.IsOnGridWithMe && 10000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    if (ESCache.Instance.Stations.Any(i => i.IsOnGridWithMe && 10000 > i.Distance))
                    {
                        SetIsPvpAllowedState(false);
                        return (bool)_isPvpAllowed;
                    }

                    SetIsPvpAllowedState(true);
                    return (bool)_isPvpAllowed;
                }

                return false;
            }
        }
        private bool _previouslyPvpAllowedState = false;

        private void SetIsPvpAllowedState(bool state)
        {
            if (_previouslyPvpAllowedState == state)
            {
                _isPvpAllowed = state;
                return;
            }

            _isPvpAllowed = state;
            _previouslyPvpAllowedState = state;
            if ((bool)_isPvpAllowed)
            {
                Log.WriteLine("IsPvpAllowed is now [" + _isPvpAllowed + "] IsHighSecuritySpace [" + ESCache.Instance.DirectEve.Session.SolarSystem.IsHighSecuritySpace + "] IsLowSecuritySpace [" + ESCache.Instance.DirectEve.Session.SolarSystem.IsLowSecuritySpace + "] IsZeroZeroSpace [" + ESCache.Instance.DirectEve.Session.SolarSystem.IsZeroZeroSpace + "] IsWspace [" + ESCache.Instance.DirectEve.Session.IsWspace + "]");
            }
            else
            {
                Log.WriteLine("IsPvpAllowed is now [" + _isPvpAllowed + "] IsHighSecuritySpace [" + ESCache.Instance.DirectEve.Session.SolarSystem.IsHighSecuritySpace + "] IsLowSecuritySpace [" + ESCache.Instance.DirectEve.Session.SolarSystem.IsLowSecuritySpace + "] IsZeroZeroSpace [" + ESCache.Instance.DirectEve.Session.SolarSystem.IsZeroZeroSpace + "] IsWspace [" + ESCache.Instance.DirectEve.Session.IsWspace + "]");
            }
        }

        private List<EntityCache> _myCorpMatesAsEntities;

        public List<EntityCache> MyCorpMatesAsEntities
        {
            get
            {
                if (!ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    //In station or not in space..., there are no entities here!
                    _myCorpMatesAsEntities = new List<EntityCache>();
                    return new List<EntityCache>();
                }

                if (_myCorpMatesAsEntities == null)
                {
                    _myCorpMatesAsEntities = new List<EntityCache>();
                    int intCorpmember = 0;
                    int intLocalmember = 0;
                    //DirectCharacter myCharacterInLocal = Instance.DirectEve.Session.CharactersInLocal.Find(i => i.CharacterId.ToString() == Instance.EveAccount.MyCharacterId);

                    // check for NPC corps and abort if this toon is in a NPC corp
                    //if (ESCache.Instance.DirectEve.Session.CharactersInLocal.Any(i => i.CorporationId == Instance.EveAccount.myCharacterId))
                    //    return;

                    if (ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher != null && ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher.Count > 0)
                    {
                        if (DebugConfig.DebugDrones) Log.WriteLine("ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher [" + ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher.Count() + "]");
                        foreach (string slaveCharacterName in ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher)
                        {
                            if (slaveCharacterName == ESCache.Instance.CharName)
                                continue;

                            foreach (EntityCache Player in ESCache.Instance.EntitiesOnGrid.Where(i => i.Id != ESCache.Instance.MyShipEntity.Id && i.IsPlayer))
                            {
                                if (Player.Name == ESCache.Instance.CharName)
                                    continue;

                                if (slaveCharacterName == Player.Name && !_myCorpMatesAsEntities.Contains(Player))
                                {
                                    if (DebugConfig.DebugDrones) Log.WriteLine("_myCorpMatesAsEntities add [" + Player.Name + "]");
                                    _myCorpMatesAsEntities.Add(Player);
                                }
                            }
                        }
                    }

                    if (DebugConfig.DebugDrones) Log.WriteLine("_myCorpMatesAsEntities [" + _myCorpMatesAsEntities.Count() + "]");

                    if (ESCache.Instance.DirectEve.Session.CharactersInLocal.Count == 0)
                        return _myCorpMatesAsEntities ?? new List<EntityCache>();

                    foreach (DirectCharacter localMember in Instance.DirectEve.Session.CharactersInLocal.Where(i => i.CharacterId.ToString() != Instance.EveAccount.MyCharacterId && MyShipEntity != null && MyShipEntity._directEntity.CharacterFromLocal != null && i.CorporationId == MyShipEntity._directEntity.CharacterFromLocal.CorporationId))
                    {
                        intLocalmember++;
                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("localmember [" + localMember.Name + "]");
                        foreach (EntityCache thisPlayer in ESCache.Instance.EntitiesOnGrid.Where(i => i.CategoryId == (int)CategoryID.Ship))
                        {
                            if (_myFleetMembersAsEntities != null && _myFleetMembersAsEntities.Count > 0 && _myFleetMembersAsEntities.Contains(thisPlayer)) continue;

                            intCorpmember++;
                            if (DebugConfig.DebugTargetCombatants) Log.WriteLine("PlayerEntity [" + thisPlayer + "][" + thisPlayer.Name + "] CategoryID [" + thisPlayer.CategoryId + "]");
                            if (thisPlayer.Name == localMember.Name)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (thisPlayer.Name == localMember.Name)");
                                _myCorpMatesAsEntities.Add(thisPlayer);
                            }

                            continue;
                        }
                    }

                    return _myCorpMatesAsEntities ?? new List<EntityCache>();
                }

                return _myCorpMatesAsEntities ?? new List<EntityCache>();
            }
        }

        private List<String> _myLeaderAndSlaveNamesFromLauncher;

        public List<String> MyLeaderAndSlaveNamesFromLauncher
        {
            get
            {
                if (_myLeaderAndSlaveNamesFromLauncher == null)
                {
                    _myLeaderAndSlaveNamesFromLauncher = new List<string>
                    {
                        ESCache.Instance.EveAccount.LeaderCharacterName
                    };

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName1))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName1);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName2))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName2);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName3))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName3);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName4))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName4);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName5))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName5);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName6))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName6);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName7))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName7);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName8))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName8);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName9))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName9);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName10))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName10);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName11))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName11);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName12))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName12);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName13))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName13);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName14))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName14);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName15))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName15);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName16))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName16);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName17))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName17);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName18))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName18);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName19))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName19);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName20))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName20);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName21))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName21);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName22))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName22);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName23))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName23);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName24))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName24);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName25))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName25);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName26))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName26);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName27))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName27);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName28))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName28);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName29))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName29);

                    if (_myLeaderAndSlaveNamesFromLauncher.Count > 0 && !_myLeaderAndSlaveNamesFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacterName30))
                        _myLeaderAndSlaveNamesFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacterName30);

                    return _myLeaderAndSlaveNamesFromLauncher ?? new List<string>();
                }

                return _myLeaderAndSlaveNamesFromLauncher ?? new List<string>();
            }
        }

        //
        // What # of DPS groups exist: separate NPCs into that number of groups by ID? determine what my group is assigned and prioritize what is in my group first (but definitely shoot other groups targets if thats all that is avail)
        //

        public bool PCTInDPSGroup1
        {
            get
            {
                double intDivideEntitiesIntoThisManyPiles = ESCache.Instance.DPSGroupCount;
                double EntityCount = 0;
                double NumberOfEntitiesPerPile = 0;



                //if (IsSmallPCTEntity)
                {
                    List<EntityCache> ListOfSmallPCTEntities = Combat.PotentialCombatTargets.Where(i => i.IsSmallPCTEntity).ToList();
                    EntityCount = ListOfSmallPCTEntities.Count();
                    NumberOfEntitiesPerPile = Math.Ceiling(EntityCount / intDivideEntitiesIntoThisManyPiles);


                }

                //if (IsMediumPCTEntity)
                {
                    List<EntityCache> ListOfMediumPCTEntities = Combat.PotentialCombatTargets.Where(i => i.IsMediumPCTEntity).ToList();
                    EntityCount = Combat.PotentialCombatTargets.Count(i => i.IsMediumPCTEntity);
                    NumberOfEntitiesPerPile = Math.Ceiling(EntityCount / intDivideEntitiesIntoThisManyPiles);
                }

                //if (IsLargePCTEntity)
                {
                    List<EntityCache> ListOfLargePCTEntities = Combat.PotentialCombatTargets.Where(i => i.IsLargePCTEntity).ToList();
                    EntityCount = Combat.PotentialCombatTargets.Count(i => i.IsLargePCTEntity);
                    NumberOfEntitiesPerPile = Math.Ceiling(EntityCount / intDivideEntitiesIntoThisManyPiles);
                }




                return false;
            }
        }


        private List<int> _myDPSGroupNumbersFromLauncher;

        public List<int> MyDPSGroupNamesFromLauncher
        {
            get
            {
                if (_myDPSGroupNumbersFromLauncher == null)
                {
                    _myDPSGroupNumbersFromLauncher = new List<int>
                    {
                        1
                    };

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter1DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter1DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter2DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter2DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter3DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter3DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter4DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter4DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter5DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter5DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter6DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter6DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter7DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter7DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter8DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter8DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter9DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter9DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter10DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter10DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter11DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter11DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter12DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter12DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter13DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter13DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter14DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter14DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter15DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter15DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter16DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter16DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter17DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter17DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter18DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter18DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter19DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter19DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter20DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter20DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter21DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter21DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter22DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter22DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter23DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter23DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter24DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter24DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter25DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter25DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter26DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter26DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter27DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter27DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter28DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter28DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter29DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter29DPSGroup);

                    if (_myDPSGroupNumbersFromLauncher.Count > 0 && !_myDPSGroupNumbersFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter30DPSGroup))
                        _myDPSGroupNumbersFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter30DPSGroup);

                    return _myDPSGroupNumbersFromLauncher.Distinct().ToList() ?? new List<int>();
                }

                return _myDPSGroupNumbersFromLauncher.Distinct().ToList() ?? new List<int>();
            }
        }

        private List<string> _myLeaderAndSlaveCharacterIDsFromLauncher;

        public List<string> MyLeaderAndSlaveCharacterIDsFromLauncher
        {
            get
            {
                if (_myLeaderAndSlaveCharacterIDsFromLauncher == null)
                {
                    _myLeaderAndSlaveCharacterIDsFromLauncher = new List<string>
                    {
                        ESCache.Instance.EveAccount.LeaderCharacterId
                    };

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter1ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter1ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter2ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter2ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter3ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter3ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter4ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter4ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter5ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter5ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter6ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter6ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter7ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter7ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter8ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter8ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter9ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter9ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter10ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter10ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter11ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter11ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter12ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter12ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter13ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter13ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter14ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter14ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter15ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter15ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter16ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter16ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter17ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter17ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter18ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter18ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter19ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter19ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter20ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter20ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter21ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter21ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter22ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter22ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter23ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter23ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter24ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter24ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter25ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter25ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter26ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter26ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter27ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter27ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter28ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter28ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter29ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter29ChracterId);

                    if (_myLeaderAndSlaveCharacterIDsFromLauncher.Count > 0 && !_myLeaderAndSlaveCharacterIDsFromLauncher.Contains(ESCache.Instance.EveAccount.SlaveCharacter30ChracterId))
                        _myLeaderAndSlaveCharacterIDsFromLauncher.Add(ESCache.Instance.EveAccount.SlaveCharacter30ChracterId);

                    return _myLeaderAndSlaveCharacterIDsFromLauncher ?? new List<string>();
                }

                return _myLeaderAndSlaveCharacterIDsFromLauncher ?? new List<string>();
            }
        }

        public int DPSGroupCount
        {
            get
            {
                if (MyDPSGroupNamesFromLauncher == null)
                {
                    return 0;
                }

                return MyDPSGroupNamesFromLauncher.Count();
            }
        }

        public int DPSGroupNumberOfLargeEntitiesToInclude
        {
            get
            {
                if (MyDPSGroupNamesFromLauncher == null)
                {
                    return 0;
                }

                return MyDPSGroupNamesFromLauncher.Count();
            }
        }

        public int DPSGroupNumberOfMediumEntitiesToInclude
        {
            get
            {
                if (MyDPSGroupNamesFromLauncher == null)
                {
                    return 0;
                }

                return MyDPSGroupNamesFromLauncher.Count();
            }
        }

        public int DPSGroupNumberOfSmallEntitiesToInclude
        {
            get
            {
                if (MyDPSGroupNamesFromLauncher == null)
                {
                    return 0;
                }

                return MyDPSGroupNamesFromLauncher.Count();
            }
        }

        public List<EntityCache> NPCSmallDPSGroupA
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCSmallDPSGroupB
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCSmallDPSGroupC
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCSmallDPSGroupD
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCSmallDPSGroupE
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCMediumDPSGroupA
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCMediumDPSGroupB
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCMediumDPSGroupC
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCMediumDPSGroupD
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCMediumDPSGroupE
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCLargeDPSGroupA
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCLargeDPSGroupB
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCLargeDPSGroupC
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCLargeDPSGroupD
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> NPCLargeDPSGroupE
        {
            get
            {
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> MyFleetMembersAsEntities
        {
            get
            {
                if (_myFleetMembersAsEntities == null)
                {
                    _myFleetMembersAsEntities = new List<EntityCache>();
                    int intFleetMember = 0;

                    if (ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher != null && ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher.Count > 0)
                    {
                        foreach (string slaveCharacterName in ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher)
                        {
                            if (slaveCharacterName == ESCache.Instance.CharName)
                                continue;

                            foreach (EntityCache player in ESCache.Instance.EntitiesOnGrid.Where(i => i.IsPlayer))
                            {
                                if (player.Name == ESCache.Instance.CharName)
                                    continue;

                                if (slaveCharacterName == player.Name && !_myFleetMembersAsEntities.Contains(player))
                                    _myFleetMembersAsEntities.Add(player);
                            }
                        }
                    }

                    if (!ESCache.Instance.DirectEve.Session.InFleet)
                        return _myFleetMembersAsEntities ?? new List<EntityCache>();

                    foreach (DirectFleetMember fleetMember in ESCache.Instance.DirectEve.GetFleetMembers)
                    {
                        intFleetMember++;
                        if (DebugConfig.DebugFleetMgr) Log.WriteLine("FleetMember [" + intFleetMember + "][" + fleetMember.Name + "] Job [" + fleetMember.Job + "] Role [" + fleetMember.Role + "]");
                        if (ESCache.Instance.EntitiesOnGrid.Any(i => fleetMember.Name == i.Name))
                        {
                            if (_myFleetMembersAsEntities.Contains(ESCache.Instance.EntitiesOnGrid.Find(i => i.IsPlayer && fleetMember.Name == i.Name))) continue;

                            if (DebugConfig.DebugFleetMgr) Log.WriteLine("FleetMember: if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsPlayer && fleetMember.Name == i.Name))");
                            _myFleetMembersAsEntities.Add(ESCache.Instance.EntitiesOnGrid.Find(i => i.IsPlayer && fleetMember.Name == i.Name));
                        }

                        continue;
                    }

                    return _myFleetMembersAsEntities ?? new List<EntityCache>();
                }

                return _myFleetMembersAsEntities ?? new List<EntityCache>();
            }
        }

        public long StationIDJitaP4M4 = 60003760;

        public List<ModuleCache> Modules
        {
            get
            {
                try
                {
                    if (_modules != null)
                        return _modules;

                    if (ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.IsPod)
                        return new List<ModuleCache>();

                    if (_modules == null)
                    {
                        if (Instance.DirectEve.Modules.Count > 0)
                        {
                            _modules = Instance.DirectEve.Modules.Select(m => new ModuleCache(m)).ToList();
                            if (DebugConfig.DebugModules)
                            {
                                foreach (ModuleCache myModule in _modules)
                                {
                                    Log.WriteLine("ID [" + myModule.ItemId + "] Name [" + myModule.TypeName + "] IsActive [" + myModule.IsActive + "] IsDeactivating [" + myModule.IsDeactivating + "] IsActivatable [" + myModule.IsActivatable + "] GroupId [" + myModule.GroupId + "]");
                                }
                            }

                            return _modules;
                        }
                    }

                    return new List<ModuleCache>();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return new List<ModuleCache>();
                }
            }
        }

        public DirectItem MyCurrentAmmoInWeapon
        {
            get
            {
                try
                {
                    if (_myCurrentAmmoInWeapon == null)
                    {
                        if (Instance.Weapons.Count > 0)
                        {
                            ModuleCache WeaponToCheckForAmmo = Instance.Weapons.FirstOrDefault();
                            if (WeaponToCheckForAmmo != null)
                            {
                                _myCurrentAmmoInWeapon = WeaponToCheckForAmmo.Charge;
                                return _myCurrentAmmoInWeapon;
                            }

                            return null;
                        }

                        return null;
                    }

                    return _myCurrentAmmoInWeapon;
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                    return null;
                }
            }
        }

        public bool MyShipIsHealthy
        {
            get
            {
                if (Instance.Modules.Any(i => i.IsArmorRepairModule))
                {
                    if (Instance.ActiveShip.ArmorPercentage > 75 && Instance.ActiveShip.CapacitorPercentage > 75)
                        return true;

                    return false;
                }

                if (Instance.Modules.Any(i => i.IsShieldRepairModule))
                {
                    if (Instance.ActiveShip.ShieldPercentage > 75 && Instance.ActiveShip.CapacitorPercentage > 75)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public EntityCache AbyssalMidGate => ESCache.Instance.Entities.FirstOrDefault(e => e.TypeId == 47685 && e.BracketType == BracketType.Warp_Gate);

        public EntityCache AbyssalEndGate => ESCache.Instance.Entities.FirstOrDefault(e => e.TypeId == 47686 && e.BracketType == BracketType.Warp_Gate);

        public EntityCache AbyssalNextGate => AbyssalMidGate ?? AbyssalEndGate ?? null;

        public float? MyWalletBalance { get; set; }

        public bool PauseAfterNextDock { get; set; }

        public bool DeactivateScheduleAndCloseEveAfterNextDock { get; set; }

        public List<ShipTargetValue> ShipTargetValues { get; private set; }

        public Dictionary<long, DateTime> TargetingIDs { get; }

        public static Dictionary<int, string> UnloadLootTheseItemsAreLootById { get; private set; }

        public float? Wealth { get; set; }

        public float WealthAtStartOfPocket { get; set; }

        public int WeaponRange
        {
            get
            {
                if (_weaponRange != null) return _weaponRange ?? 0;

                try
                {
                    _weaponRange = 0;
                    AmmoType ammoInGuns = null;

                    if (Instance.Weapons.Count > 0)
                    {
                        if (ESCache.Instance.Weapons.FirstOrDefault().Charge != null)
                        {
                            ammoInGuns = ESCache.Instance.Weapons.FirstOrDefault().Charge.AmmoType;
                        }
                        else
                        {
                            _weaponRange = Convert.ToInt32(Combat.MaxTargetRange);
                            return _weaponRange ?? 0;
                        }

                        if (ammoInGuns == null)
                        {
                            _weaponRange = Convert.ToInt32(Combat.MaxTargetRange);
                            return _weaponRange ?? 0;
                        }

                        double effectiveMaxRange = 0;

                        if (Combat.DoWeCurrentlyHaveTurretsMounted())
                        {
                            if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.Any(i => i.GroupId == (int)Group.PrecursorWeapon))
                            {
                                effectiveMaxRange = Math.Min(Instance.Weapons.FirstOrDefault().OptimalRange, Combat.MaxTargetRange);
                                double precursorAmmoRange = Math.Min(ammoInGuns.Range, Combat.MaxTargetRange);
                                _weaponRange = Math.Max((int)precursorAmmoRange, (int)effectiveMaxRange);
                                return _weaponRange ?? 0;
                            }

                            const int OptimalRangeMultiplier = 2;
                            effectiveMaxRange = Math.Min(Instance.Weapons.FirstOrDefault().FallOff * OptimalRangeMultiplier, Combat.MaxTargetRange);
                        }

                        double ammoRange = Math.Min(ammoInGuns.Range, Combat.MaxTargetRange);

                        _weaponRange = Math.Max((int)ammoRange, (int)effectiveMaxRange);
                    }

                    return _weaponRange ?? 0;
                }
                catch (Exception)
                {
                    if (Instance.ActiveShip != null)
                    {
                        _weaponRange = Convert.ToInt32(Combat.MaxTargetRange);
                        return _weaponRange ?? 0;
                    }

                    return 0;
                }
            }
        }

        public int MiningRange
        {
            get
            {
                if (_miningRange != null) return _miningRange ?? 0;

                try
                {
                    _miningRange = 0;
                    if (Instance.MiningEquipment.Count > 0)
                    {
                        _miningRange = (int?)MiningEquipment.Min(m => m.OptimalRange);
                    }

                    return _miningRange ?? 0;
                }
                catch (Exception)
                {
                    if (Instance.ActiveShip != null)
                    {
                        _miningRange = 10000;
                        return _miningRange ?? 0;
                    }

                    return 0;
                }
            }
        }

        private bool? _weHaveWeapons = null;

        public bool WeHaveWeapons
        {
            get
            {
                if (_weHaveWeapons != null)
                    return _weHaveWeapons ?? false;

                if (Weapons.Count() > 0)
                {
                    _weHaveWeapons = true;
                    return _weHaveWeapons ?? true;
                }

                return false;
            }
        }

        public List<ModuleCache> MiningEquipment
        {
            get
            {
                if (_miningEquipment == null)
                {
                    _miningEquipment = Modules.Where(m => m.IsOnline && (m.IsOreMiningModule || m.IsIceMiningModule)).ToList() ?? new List<ModuleCache>();
                    return _miningEquipment ?? new List<ModuleCache>();
                }

                return _miningEquipment ?? new List<ModuleCache>();
            }
        }

        public List<ModuleCache> Weapons
        {
            get
            {
                if (_weapons == null)
                {
                    _weapons = Modules.Where(m => m.IsOnline && m.IsWeapon && m._module.IsMasterOrIsNotGrouped).ToList() ?? new List<ModuleCache>();
                    return _weapons ?? new List<ModuleCache>();
                }

                return _weapons ?? new List<ModuleCache>();
            }
        }

        public double? WebRange
        {
            get
            {
                ModuleCache webifier = null;
                if (Instance.Modules.Any(i => i.GroupId == (int)Group.StasisWeb))
                {
                    webifier = Instance.Modules.Find(i => i.GroupId == (int)Group.StasisWeb && i.IsOnline);
                    if (webifier != null)
                        return webifier.MaxRange;

                    return null;
                }

                return null;
            }
        }

        public List<DirectWindow> Windows
        {
            get
            {
                try
                {
                    if (_windows == null)
                    {
                        if (DirectEve.Windows.Count > 0)
                        {
                            _windows = DirectEve.Windows;
                            return _windows;
                        }

                        return new List<DirectWindow>();
                    }

                    return _windows ?? new List<DirectWindow>();
                }
                catch (Exception)
                {
                    return new List<DirectWindow>();
                }
            }
        }

        public void ResetInStationSettingsWhenExitingStation()
        {
            Log.WriteLine("Exiting Station: LootAlreadyUnloaded is now false");
            LootAlreadyUnloaded = false;
            Statistics.ResetInStationSettingsWhenExitingStation();
            MissionSettings.ResetInStationSettingsWhenExitingStation();
        }

        #endregion Properties

        #region Methods

        public static bool LoadDirectEveInstance()
        {
            //
            // unused?
            //
            if (Instance.DirectEve == null)
            {
                Console.WriteLine("LoadDirectEveInstance");
                Instance.DirectEve = new DirectEve(new StandaloneFramework());
            }

            return Instance.DirectEve != null;
        }

        public DirectItem CheckCargoForItem(int typeIdToFind, int quantityToFind)
        {
            try
            {
                if (Instance.CurrentShipsCargo != null && Instance.CurrentShipsCargo.Items.Count > 0)
                {
                    DirectItem item = Instance.CurrentShipsCargo.Items.Find(i => i.TypeId == typeIdToFind && i.Quantity >= quantityToFind);
                    return item;
                }

                return null;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }

            return null;
        }

        public bool CheckIfRouteIsAllHighSec()
        {
            Instance.RouteIsAllHighSecBool = false;

            try
            {
                if (DirectEve.Navigation.GetDestinationPath() != null && DirectEve.Navigation.GetDestinationPath().Count > 0)
                {
                    List<long> currentPath = DirectEve.Navigation.GetDestinationPath();
                    if (currentPath == null || currentPath.Count == 0) return false;
                    if (currentPath[0] == 0) return false;
                    Log.WriteLine("CheckIfRouteIsAllHighSec: Route has [" + currentPath.Count + "] Systems");
                    int systemCount = 0;
                    foreach (long _system in currentPath)
                    {
                        systemCount++;
                        if (_system < 60000000)
                        {
                            DirectSolarSystem solarSystemInRoute = Instance.DirectEve.SolarSystems[(int)_system];
                            if (solarSystemInRoute != null)
                            {
                                if (solarSystemInRoute.IsHighSecuritySpace)
                                {
                                    Log.WriteLine("CheckIfRouteIsAllHighSec: [" + systemCount + "][" + solarSystemInRoute.Name + "] is [" + solarSystemInRoute.GetSecurity() + "] security - lowsec!");
                                    Instance.RouteIsAllHighSecBool = false;
                                    return true;
                                }

                                Log.WriteLine("CheckIfRouteIsAllHighSec: [" + systemCount + "][" + solarSystemInRoute.Name + "] is [" + solarSystemInRoute.GetSecurity() + "] security - highsec");
                                continue;
                            }

                            Log.WriteLine("Jump number [" + _system + "of" + currentPath.Count +
                                          "] in the route came back as null, we could not get the system name or sec level");
                        }
                    }

                    Log.WriteLine("CheckIfRouteIsAllHighSec: Route has [" + currentPath.Count + "] Systems 0 of which are lowsec");
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }

            Instance.RouteIsAllHighSecBool = true;
            return true;
        }

        private string _selectedController = String.Empty;

        public string SelectedController
        {
            get
            {
                if (!string.IsNullOrEmpty(_selectedController))
                    return _selectedController;

                _selectedController = ESCache.Instance.EveAccount.SelectedController;
                return _selectedController;
            }
        }

        public void ClearPerPocketCache()
        {
            try
            {
                if (DateTime.UtcNow > Time.Instance.NextClearPocketCache)
                {
                    Log.WriteLine("ClearPerPocketCache()");
                    DirectCache.ClearPerPocketCache();
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NeedHumanIntervention), false);
                    AbyssalDeadspaceBehavior.ClearPerPocketCache();
                    AbyssalController.ClearPerPocketCache();
                    AmmoManagementBehavior.ClearPerPocketCache();
                    Combat.ClearPerPocketCache();
                    Defense.ClearPerPocketCache();
                    DirectEntity.ClearPerPocketCache();
                    Drones.ClearPerPocketCache();
                    GatherShipsBehavior.ClearPerPocketCache();
                    HighSecAnomalyBehavior.ClearPerPocketCache();
                    MissionSettings.ClearPerPocketCache();
                    NavigateOnGrid.ClearPerPocketCache();
                    Panic.ClearPerPocketCache();
                    ReduceGraphicLoad.ClearPerPocketCache();
                    Salvage.ClearPerPocketCache();
                    State.CurrentInstaStationDockState = InstaStationDockState.Idle;
                    State.CurrentInstaStationUndockState = InstaStationUndockState.Idle;
                    //DirectEntity.InterSectingEntitiesForEachWorldPosition = new Dictionary<DirectWorldPosition, List<long>>();

                    ESCache.Instance.DictionaryCachedPerPocket.Clear();
                    ESCache.Instance.DictionaryCachedPerPocketLastAttackedDrones.Clear();
                    ListOfJammingEntities.Clear();
                    ListOfTrackingDisruptingEntities.Clear();
                    ListNeutralizingEntities.Clear();
                    ListOfTargetPaintingEntities.Clear();
                    ListOfDampeningEntities.Clear();
                    ListofWebbingEntities.Clear();
                    ListofContainersToLoot.Clear();
                    ListofMissionCompletionItemsToLoot.Clear();

                    _abyssalSphereCoordinates = null;
                    _cachedBookmarks = null;
                    _inAbyssalDeadspace = null;
                    _inMission = null;
                    _insidePosForceField = null;
                    _listOfUndockBookmarks = null;
                    _selectedController = string.Empty;
                    _weHaveWeapons = null;
                    _wormholes = null;

                    ActionControl.IgnoreTargets.Clear();
                    DictionaryIsHighValueTarget.Clear();
                    DictionaryIsLowValueTarget.Clear();
                    DictionaryIsEntityIShouldLeaveAlone.Clear();
                    Instance.LootedContainers.Clear();
                    TargetingIDs.Clear();

                    ESCache.Instance.ListEntityIDs_IsBadIdeaTrue = new List<long>();
                    ESCache.Instance.ListEntityIDs_IsBadIdeaFalse = new List<long>();
                    ESCache.Instance.ListEntityIDs_IsPotentialCombatTarget = new List<long>();
                    if (SelectedController == nameof(EveAccount.AvailableControllers.CourierMissionsController))
                        CourierMissionsController.ClearPerPocketCache();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
            finally
            {
                Time.Instance.NextClearPocketCache = DateTime.UtcNow.AddSeconds(10);
            }
        }

        public void ClearPerSystemCache()
        {
            GatherShipsBehavior.ClearPerSystemCache();
            //if (DirectEve.Interval(60000)) Util.FlushMemIfThisProcessIsUsingTooMuchMemory(1500);
            if (DirectEve.Interval(180000) && ESCache.Instance.ClosestWormhole == null) MemoryOptimizer.OptimizeMemory();
            DirectWorldPosition.OnSessionChange();

            if (DirectEve.Interval(10000)) Log.WriteLine("ClearPerSystemCache()");
            DirectCache.ClearPerSystemCache();
            Defense.ClearSystemSpecificSettings();
            SkillQueue.ClearSystemSpecificSettings();
            Instance.ClearPerPocketCache();
            ReduceGraphicLoad.ClearPerSystemCache();
            Scanner.ClearPerSystemCache();
            HighSecAnomalyBehavior.ClearPerSystemCache();
            AmmoManagementBehavior.ClearPerSystemCache();
            ExplortationNoWeaponsBehavior.ClearPerSystemCache();
        }

        public string CloseEveReason { get; set; } = "CloseEveReason: n/a";
        public bool BoolCloseEve = false;
        public bool BoolRestartEve = false;

        public bool CloseEve(bool restart, string reason)
        {
            //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AllowSimultaneousLogins), true);
            if (restart) WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.RestartOfEveClientNeeded), restart);
            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, reason));
            Log.WriteLine(reason);
            Util.PlayNoticeSound();
            Util.PlayNoticeSound();
            Util.PlayNoticeSound();
            Log.WriteLine("Closing EVESharp [" + reason + "]");
            Thread.Sleep(100);
            DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
            //Util.TaskKill(Process.GetCurrentProcess().Id, false);
            return false;
        }

        public bool RestartBot(string reason)
        {
            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.RestartOfBotNeeded), true);
            Log.WriteLine("Restarting bot [" + reason + "]");
            return false;
        }

        public void DisableThisInstance()
        {
            string msg = "Set [" + ESCache.Instance.EveAccount.MaskedCharacterName + "] disabled.";
            WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.UseScheduler), false);
        }

        public bool GroupWeapons(bool ignoreWarp = false)
        {
            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
            {
                if (DebugConfig.DebugActivateWeapons) Log.WriteLine("Dont group lasers, changing ammo is a problem if they are grouped. laser ammo isnt packaged, and thus does have a quantity!!");
                return true;
            }

            if (ESCache.Instance.Weapons.Any(i => i.IsEnergyWeapon))
            {
                if (DebugConfig.DebugActivateWeapons) Log.WriteLine("Dont group lasers, changing ammo is a problem if they are grouped. laser ammo isnt packaged, and thus does have a quantity!!");
                return true;
            }

            if (Instance.InSpace && (Instance.InWarp && !ignoreWarp) &&
                Time.Instance.LastGroupWeapons.AddSeconds(Time.Instance.Rnd.Next(15, 30)) < DateTime.UtcNow)
            {
                Time.Instance.LastGroupWeapons = DateTime.UtcNow;
                if (Instance.Weapons.Count > 1 &&
                    Instance.Weapons.All(w => w.IsOnline && !w.IsActive && !w.IsReloadingAmmo))
                    if (Instance.ActiveShip != null && Instance.ActiveShip.Entity != null)
                        if (Instance.ActiveShip.CanGroupAll())
                        {
                            Time.Instance.TryGroupWeapons = false; // will become true after docking
                            Instance.ActiveShip.GroupAllWeapons();
                            Log.WriteLine("Grouped weapons.");
                            return true;
                        }
            }

            return false;
        }

        public DateTime LastInteractedWithEVE { get; set; } = DateTime.UtcNow;

        private bool? _okToInteractWithEveNow { get; set; } = null;

        public bool OkToInteractWithEveNow
        {
            get
            {
                if (_okToInteractWithEveNow != null)
                    return _okToInteractWithEveNow ?? false;

                if (LastInteractedWithEVE > DateTime.UtcNow.AddMinutes(1))
                {
                    LastInteractedWithEVE = DateTime.UtcNow;
                    _okToInteractWithEveNow = false;
                    return _okToInteractWithEveNow ?? false;
                }

                if (DateTime.UtcNow > LastInteractedWithEVE.AddMilliseconds(300))
                {
                    _okToInteractWithEveNow = true;
                    return _okToInteractWithEveNow ?? false;
                }

                return _okToInteractWithEveNow ?? false;
            }
        }

        private bool? _replaceMissionsActions = null;

        public bool ReplaceMissionsActions
        {
            get
            {
                if (_replaceMissionsActions != null)
                    return _replaceMissionsActions ?? false;

                if (ESCache.Instance.EveAccount.ReplaceMissionsActions)
                {
                    _replaceMissionsActions = true;
                    return _replaceMissionsActions ?? true;
                }

                _replaceMissionsActions = false;
                return _replaceMissionsActions ?? false;
            }
        }

        public List<DirectBookmark> _cachedBookmarks = null;

        public List<DirectBookmark> CachedBookmarks
        {
            get
            {
                if (_cachedBookmarks != null)
                    return _cachedBookmarks;

                _cachedBookmarks = ESCache.Instance.DirectEve.Bookmarks;

                if (_cachedBookmarks == null)
                    _cachedBookmarks = new List<DirectBookmark>();

                return _cachedBookmarks;
            }
        }

        public void InvalidateCache()
        {
            try
            {
                if (DebugConfig.DebugCheckSessionValid) Log.WriteLine("InvalidateCache: Start");

                if (!ESCache.Instance.Paused && DirectEntity.intCountInValidEntities > 150)
                {
                    string msg = "DirectEntity: intCountInValidEntities was: " + DirectEntity.intCountInValidEntities;
                    ESCache.Instance.CloseEveReason = msg;
                    ESCache.Instance.BoolRestartEve = true;
                }

                if (BoolCloseEve)
                {
                    BoolCloseEve = false;
                    CloseEve(false, CloseEveReason);
                }

                if (BoolRestartEve)
                {
                    BoolRestartEve = false;
                    CloseEve(true, CloseEveReason);
                }

                GetUnbonusedShieldBoostAmount();
                GetUnbonusedShieldBoosterDuration();

                if (ESCache.Instance.LastInteractedWithEVE != ESCache.Instance.EveAccount.LastInteractedWithEVE)
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), LastInteractedWithEVE);

                try
                {
                    if (DirectEve.Interval(5000)) DirectEntity.InvalidateCache();
                    Settings.InvalidateCache();
                    HighSecAnomalyController.InvalidateCache();
                    AmmoManagementBehavior.InvalidateCache();
                    Arm.InvalidateCache();
                    Defense.InvalidateCache();
                    Drones.InvalidateCache();
                    Combat.InvalidateCache();
                    Salvage.InvalidateCache();
                    Scanner.InvalidateCache();
                    //AbyssalController.InvalidateCache();
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController)) MissionSettings.InvalidateCache();
                    NavigateOnGrid.InvalidateCache();
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HydraController)) HydraController.InvalidateCache();
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController)) AbyssalDeadspaceBehavior.InvalidateCache();
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.MiningController)) MiningBehavior.InvalidateCache();
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController)) AbyssalSpawn.InvalidateCache();
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController)) AbyssalSpawn.InvalidateCache();
                    InvalidateCache_Hangars();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                ESCache.Instance.DictionaryCachedPerFrame.Clear();
                if (_inMission != null && !(bool)_inMission)
                    _inMission = null;

                try
                {
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                }
                catch (Exception){}

                _inSpace = null;
                _inStation = null;
                if (Instance.InSpace && Instance.InStation) Log.WriteLine("Both InStation and InSpace were true. How?");
                _fittedModules = null;
                _abyssalCenter = null;
                _abyssalMobileTractor = null;
                _abyssalSpeedTankSpot = null;
                _abyssalTrace = null;
                _bigObjectsAndGates = null;
                _abyssalBigObjects = null;
                _abyssalDeadspaceDeviantAutomataSuppressor = null;
                _abyssalDeadspaceSmallDeviantAutomataSuppressor = null;
                _abyssalDeadspaceMediumDeviantAutomataSuppressor = null;
                _abyssalDeadspaceMultibodyTrackingPylon = null;
                _abyssalDeadspaceBioluminescenceCloud = null;
                _abyssalDeadspaceTachyonCausticCloud = null;
                _abyssalDeadspaceFilamentCloud = null;
                _activeShip = null;
                _asteroids = null;
                _celestials = null;
                _chargeEntities = null;
                _closestDockableLocation = null;
                _closestStargate = null;
                _currentShipsAmmoHold = null;
                _currentShipsCargo = null;
                _currentShipsFleetHangar = null;
                _currentShipsGasHold = null;
                _currentShipsGeneralMiningHold = null;
                _currentShipsIceHold = null;
                _currentShipsModules = null;
                _currentShipsMineralHold = null;
                _currentShipsOreHold = null;
                _containers = null;
                _dockableLocations = null;
                _entities = null;
                _entitiesNotSelf = null;
                _entitiesOnGrid = null;
                _entitiesById.Clear();
                 //_eveAccount = null;
                _fittingManagerWindow = null;
                _freeportCitadels = null;
                _gates = null;
                _inAbyssalDeadspace = null;
                _inAnomaly = null;
                _insidePosForceField = null;
                _inSite = null;
                _inWarp = null;
                _isPvpAllowed = null;
                _lpStore = null;
                _maxLockedTargets = null;
                _miningEquipment = null;
                _modules = null;
                _hostileMissilesInSpace = null;
                _miningRange = null;
                _myAmmoInSpace = null;
                _myCorpMatesAsEntities = null;
                _myCurrentAmmoInWeapon = null;
                _myFleetMembersAsEntities = null;
                _myShipEntity = null;
                _myLeaderAndSlaveNamesFromLauncher = null;
                _okToInteractWithEveNow = null;
                _planets = null;
                _playerSpawnLocation = null;
                _replaceMissionsActions = null;
                _safeSpotBookmarks = null;
                _selectedController = null;
                _star = null;
                _stargate = null;
                _stargates = null;
                _stations = null;
                _targets = null;
                _targeting = null;
                _totalTargetsAndTargeting = null;
                _triglavianConstructionSiteSpawnFoundDozenPlusBSs = null;
                _unlootedContainers = null;
                _unlootedWrecksAndSecureCans = null;
                _weapons = null;
                _weaponRange = null;
                _weHaveWeapons = null;
                _windows = null;
                _wrecks = null;
                //GetUnbonusedInertia();
                //GetUnbonusedSignatureRadius();
                //GetUnbonusedVelocity();
                if (DebugConfig.DebugCheckSessionValid) Log.WriteLine("InvalidateCache: Done");

            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        public void IterateShipTargetValues()
        {
            ShipTargetValues = new List<ShipTargetValue>();

            try
            {
                Log.WriteLine("IterateShipTargetValues - Loading [ShipTargetValuesXmlFile]");
                XDocument values = XDocument.Parse(Lookup.ShipTargetValues.GetShipTargetValuesXML);
                if (values.Root != null)
                    foreach (XElement value in values.Root.Elements("ship"))
                    {
                        ShipTargetValue stv = new ShipTargetValue(value);
                        ShipTargetValues.Add(stv);
                    }
            }
            catch (Exception exception)
            {
                Log.WriteLine("IterateShipTargetValues - Exception: [" + exception + "]");
            }
        }

        public void IterateUnloadLootTheseItemsAreLootItems()
        {
            UnloadLootTheseItemsAreLootById = new Dictionary<int, string>();

            try
            {
                MissionSettings.UnloadLootTheseItemsAreLootItems = XDocument.Parse(UnloadLootTheseItemsAreLootItems.GetUnloadLootTheseItemsAreLootItemsXML);
                if (MissionSettings.UnloadLootTheseItemsAreLootItems.Root != null)
                    foreach (XElement element in MissionSettings.UnloadLootTheseItemsAreLootItems.Root.Elements("invtype"))
                    {
                        int key = -1;
                        int.TryParse(element.Attribute("id").Value, out key);
                        string name = element.Attribute("name").Value;
                        UnloadLootTheseItemsAreLootById.AddOrUpdate(key, name);
                    }
            }
            catch (Exception exception)
            {
                Log.WriteLine("IterateUnloadLootTheseItemsAreLootItems - Exception: [" + exception + "]");
            }
        }

        public bool LocalSafe(int maxBad, double stand)
        {
            return true;

            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return false;

            if (ESCache.Instance.InAbyssalDeadspace || ESCache.Instance.InWormHoleSpace)
                return false;

            int number = 0;

            if (Instance.DirectEve.Session.LocalChatChannel == null)
            {
                Log.WriteLine($"local == null?");
                return true;
            }

            try
            {
                foreach (DirectCharacter localMember in Instance.DirectEve.Session.CharactersInLocal.Where(i => i.CharacterId.ToString() != Instance.EveAccount.MyCharacterId))
                {
                    float[] alliance =
                    {
                        DirectEve.Standings.GetPersonalRelationship(localMember.AllianceId),
                        DirectEve.Standings.GetCorporationRelationship(localMember.AllianceId),
                        DirectEve.Standings.GetAllianceRelationship(localMember.AllianceId)
                    };
                    float[] corporation =
                    {
                        DirectEve.Standings.GetPersonalRelationship(localMember.CorporationId),
                        DirectEve.Standings.GetCorporationRelationship(localMember.CorporationId),
                        DirectEve.Standings.GetAllianceRelationship(localMember.CorporationId)
                    };
                    float[] personal =
                    {
                        DirectEve.Standings.GetPersonalRelationship(localMember.CharacterId),
                        DirectEve.Standings.GetCorporationRelationship(localMember.CharacterId),
                        DirectEve.Standings.GetAllianceRelationship(localMember.CharacterId)
                    };

                    if (alliance.Min() <= stand || corporation.Min() <= stand || personal.Min() <= stand)
                    {
                        Log.WriteLine("Bad Standing Pilot Detected: [ " + localMember.Name + "] " + " [ " + number + " ] so far... of [ " + maxBad +
                                      " ] allowed");
                        number++;
                    }

                    if (number > maxBad)
                    {
                        Log.WriteLine("[" + number + "] Bad Standing pilots in local, We should stay in station");
                        return false;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }

            return true;
        }

        public int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        #endregion Methods
    }
}