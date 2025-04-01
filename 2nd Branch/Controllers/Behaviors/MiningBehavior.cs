extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SC::SharedComponents.Extensions;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Behaviors
{
    public class MiningBehavior
    {
        #region Constructors

        public MiningBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        #endregion Fields

        #region Properties
        public static string MiningBehaviorHomeBookmarkName { get; set; }

        public static string MiningOreBookmarkPrefix { get; set; } = "OreSite";

        public static string MiningGasBookmarkPrefix { get; set; } = "GasSite";

        public static bool AllowMiningInAsteroidBelts { get; set; }
        public static bool AllowMiningInMiningAnomolies { get; set; }
        public static bool AllowMiningInIceAnomolies { get; set; }
        public static bool AllowMiningInIceSignatures { get; set; }
        public static bool AllowMiningAtBookmarks { get; set; } = true;

        public static bool AllowMiningInMiningSignatures { get; set; }


        #endregion Properties

        #region Methods

        public static bool ChangeMiningBehaviorState(MiningBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentMiningBehaviorState != _StateToSet)
                {
                    if (_StateToSet == MiningBehaviorState.GotoHomeBookmark)
                    {
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                    }

                    Log.WriteLine("New MiningBehaviorState [" + _StateToSet + "]");
                    State.CurrentMiningBehaviorState = _StateToSet;
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
            Log.WriteLine("LoadSettings: MiningBehavior");
            MiningBehaviorHomeBookmarkName =
                (string)CharacterSettingsXml.Element("MiningHomeBookmark") ?? (string)CharacterSettingsXml.Element("MiningHomeBookmark") ??
                (string)CommonSettingsXml.Element("MiningHomeBookmark") ?? (string)CommonSettingsXml.Element("MiningHomeBookmark") ?? "MiningHomeBookmark";
            Log.WriteLine("MiningBehavior: MiningBehaviorHomeBookmarkName [" + MiningBehaviorHomeBookmarkName + "]");
            AllowMiningInAsteroidBelts =
                (bool?)CharacterSettingsXml.Element("allowMiningInAsteroidBelts") ??
                (bool?)CommonSettingsXml.Element("allowMiningInAsteroidBelts") ?? false;
            Log.WriteLine("MiningBehavior: allowMiningInAsteroidBelts [" + AllowMiningInAsteroidBelts + "]");
            AllowMiningInMiningAnomolies =
                (bool?)CharacterSettingsXml.Element("allowMiningInMiningAnomolies") ??
                (bool?)CommonSettingsXml.Element("allowMiningInMiningAnomolies") ?? true;
            Log.WriteLine("MiningBehavior: allowMiningInMiningAnomolies [" + AllowMiningInMiningAnomolies + "]");
            AllowMiningInMiningSignatures =
                (bool?)CharacterSettingsXml.Element("allowMiningInMiningSignatures") ??
                (bool?)CommonSettingsXml.Element("allowMiningInMiningSignatures") ?? true;
            Log.WriteLine("MiningBehavior: allowMiningInMiningSignatures [" + AllowMiningInMiningSignatures + "]");
            AllowMiningInIceAnomolies =
                (bool?)CharacterSettingsXml.Element("allowMiningInIceAnomolies") ??
                (bool?)CommonSettingsXml.Element("allowMiningInIceAnomolies") ?? true;
            Log.WriteLine("MiningBehavior: allowMiningInIceAnomolies [" + AllowMiningInIceAnomolies + "]");
            AllowMiningInIceSignatures =
                (bool?)CharacterSettingsXml.Element("allowMiningInIceSignatures") ??
                (bool?)CommonSettingsXml.Element("allowMiningInIceSignatures") ?? true;
            Log.WriteLine("MiningBehavior: allowMiningInIceSignatures [" + AllowMiningInIceSignatures + "]");
            //
            //https://wiki.eveuniversity.org/Asteroids_and_ore
            //
            MineVeldspar =
                (bool?)CharacterSettingsXml.Element("mineVeldspar") ??
                (bool?)CommonSettingsXml.Element("mineVeldspar") ?? true;
            Log.WriteLine("MiningBehavior: mineVeldspar [" + MineVeldspar + "]");
            MineScordite =
                (bool?)CharacterSettingsXml.Element("mineScordite") ??
                (bool?)CommonSettingsXml.Element("mineScordite") ?? true;
            Log.WriteLine("MiningBehavior: mineScordite [" + MineScordite + "]");
            MinePyroxeres =
                (bool?)CharacterSettingsXml.Element("minePyroxeres") ??
                (bool?)CommonSettingsXml.Element("minePyroxeres") ?? true;
            Log.WriteLine("MiningBehavior: minePyroxeres [" + MinePyroxeres + "]");
            MinePlagioclase =
                (bool?)CharacterSettingsXml.Element("minePlagioclase") ??
                (bool?)CommonSettingsXml.Element("minePlagioclase") ?? true;
            Log.WriteLine("MiningBehavior: minePlagioclase [" + MinePlagioclase + "]");
            MineOmber =
                (bool?)CharacterSettingsXml.Element("mineOmber") ??
                (bool?)CommonSettingsXml.Element("mineOmber") ?? true;
            Log.WriteLine("MiningBehavior: mineOmber [" + MineOmber + "]");
            MineKernite =
                (bool?)CharacterSettingsXml.Element("mineKernite") ??
                (bool?)CommonSettingsXml.Element("mineKernite") ?? true;
            Log.WriteLine("MiningBehavior: mineKernite [" + MineKernite + "]");
            MineJaspet =
                (bool?)CharacterSettingsXml.Element("mineJaspet") ??
                (bool?)CommonSettingsXml.Element("mineJaspet") ?? true;
            Log.WriteLine("MiningBehavior: mineJaspet [" + MineJaspet + "]");
            MineHemorphite =
                (bool?)CharacterSettingsXml.Element("mineHemorphite") ??
                (bool?)CommonSettingsXml.Element("mineHemorphite") ?? true;
            Log.WriteLine("MiningBehavior: mineHemorphite [" + MineHemorphite + "]");
            MineHedbergite =
                (bool?)CharacterSettingsXml.Element("mineHedbergite") ??
                (bool?)CommonSettingsXml.Element("mineHedbergite") ?? true;
            Log.WriteLine("MiningBehavior: mineHedbergite [" + MineHedbergite + "]");
            MineGneiss =
                (bool?)CharacterSettingsXml.Element("mineGneiss") ??
                (bool?)CommonSettingsXml.Element("mineGneiss") ?? true;
            Log.WriteLine("MiningBehavior: mineGneiss [" + MineGneiss + "]");
            MineDarkOchre =
                (bool?)CharacterSettingsXml.Element("mineDarkOchre") ??
                (bool?)CommonSettingsXml.Element("mineDarkOchre") ?? true;
            Log.WriteLine("MiningBehavior: mineDarkOchre [" + MineDarkOchre + "]");
            MineCrokite =
                (bool?)CharacterSettingsXml.Element("mineCrokite") ??
                (bool?)CommonSettingsXml.Element("mineCrokite") ?? true;
            Log.WriteLine("MiningBehavior: mineCrokite [" + MineCrokite + "]");
            MineSpodumain =
                (bool?)CharacterSettingsXml.Element("mineSpodumain") ??
                (bool?)CommonSettingsXml.Element("mineSpodumain") ?? true;
            Log.WriteLine("MiningBehavior: mineSpodumain [" + MineSpodumain + "]");
            MineArkonor =
                (bool?)CharacterSettingsXml.Element("mineArkonor") ??
                (bool?)CommonSettingsXml.Element("mineArkonor") ?? true;
            Log.WriteLine("MiningBehavior: mineArkonor [" + MineArkonor + "]");
            MineBistot =
                (bool?)CharacterSettingsXml.Element("mineBistot") ??
                (bool?)CommonSettingsXml.Element("mineBistot") ?? true;
            Log.WriteLine("MiningBehavior: mineBistot [" + MineBistot + "]");
            MineMercoxit =
                (bool?)CharacterSettingsXml.Element("mineMercoxit") ??
                (bool?)CommonSettingsXml.Element("mineMercoxit") ?? false;
            Log.WriteLine("MiningBehavior: mineMercoxit [" + MineMercoxit + "]");
        }

        private static void OreM3MinedPerMiningModulePerCycle()
        {
            //
            // calculate
            //
        }

        private static void HandleOreWeHaveAlreadyMined()
        {
            //
            // Are we full?
            //
            //
            // other options here should be make a jetcan, call a hauler?, dump into a can or corp hangar in a transport ship maybe?
            //
            if (ESCache.Instance.InStation)
            {
                Log.WriteLine("HandleOreWeHaveAlreadyMined: We are in station: UnloadLoot");
                ChangeMiningBehaviorState(MiningBehaviorState.UnloadLoot);
                return;
            }

            if (ESCache.Instance.CurrentShipsGeneralMiningHold == null)
            {
                Log.WriteLine("HandleOreWeHaveAlreadyMined: if (ESCache.Instance.CurrentShipsOreHold == null)");
                return;
            }

            if (ESCache.Instance.CurrentShipsGeneralMiningHold.UsedCapacityPercentage > 90 || 50 > ESCache.Instance.CurrentShipsGeneralMiningHold.FreeCapacity)
            {
                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("HandleOreWeHaveAlreadyMined: We are full: [" + ESCache.Instance.CurrentShipsGeneralMiningHold.FreeCapacity + "] which is [" + ESCache.Instance.CurrentShipsGeneralMiningHold.UsedCapacityPercentage + "]% limit is 90%");
                //go home to dump ore
                ChangeMiningBehaviorState(MiningBehaviorState.MakeBookmarkForNextOreRoid);
                return;
            }

            if (DebugConfig.DebugMiningBehavior) Log.WriteLine("HandleOreWeHaveAlreadyMined: We still have room: [" + ESCache.Instance.CurrentShipsGeneralMiningHold.FreeCapacity + "] which is only [" + ESCache.Instance.CurrentShipsGeneralMiningHold.UsedCapacityPercentage + "]% limit is 90%");
            return;
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("State.CurrentMiningBehaviorState is [" + State.CurrentMiningBehaviorState + "]");

                switch (State.CurrentMiningBehaviorState)
                {
                    case MiningBehaviorState.Idle:
                        MissionSettings.MissionSafeDistanceFromStructure = 22000;
                        IdleCMBState();
                        break;

                    case MiningBehaviorState.Start:
                        StartCMBState();
                        break;

                    case MiningBehaviorState.Switch:
                        SwitchCMBState();
                        break;

                    case MiningBehaviorState.Arm:
                        ArmCMBState();
                        break;

                    case MiningBehaviorState.LocalWatch:
                        LocalWatchCMBState();
                        break;

                    case MiningBehaviorState.WaitingforBadGuytoGoAway:
                        WaitingFoBadGuyToGoAway();
                        break;

                    case MiningBehaviorState.WarpOutStation:
                        WarpOutBookmarkCMBState();
                        break;

                    case MiningBehaviorState.FindAsteroidToMine:
                        HandleOreWeHaveAlreadyMined();
                        FindAsteroidToMineState();
                        break;

                    case MiningBehaviorState.GotoHomeBookmark:
                        GotoHomeBookmarkState();
                        break;

                    case MiningBehaviorState.MakeBookmarkForNextOreRoid:
                        MakeBookmarkForNextOreRoidState();
                        break;

                    case MiningBehaviorState.UnloadLoot:
                        UnloadLootCMBState();
                        break;

                    case MiningBehaviorState.Traveler:
                        TravelerCMBState();
                        break;

                    case MiningBehaviorState.Default:
                        ChangeMiningBehaviorState(MiningBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }


        //https://www.fuzzwork.co.uk/ore/

        private static EntityCache _nextAvailableAsteroidBeltInLocal { get; set; }

        private static EntityCache NextAvailableAsteroidBeltInLocal
        {
            get
            {
                if (_nextAvailableAsteroidBeltInLocal == null)
                {
                    if (AvailableAsteroidBeltsInLocal.Any())
                    {
                        if (DebugConfig.DebugMiningBehavior) Log.WriteLine("AvailableAsteroidBeltsInLocal [" + AvailableAsteroidBeltsInLocal.Count() + "]");
                        _nextAvailableAsteroidBeltInLocal = AvailableAsteroidBeltsInLocal.OrderBy(i => i.Distance).FirstOrDefault();
                        if (_nextAvailableAsteroidBeltInLocal != null)
                        {
                            if (DebugConfig.DebugMiningBehavior) Log.WriteLine("_nextAvailableAsteroidBeltInLocal [" + _nextAvailableAsteroidBeltInLocal.Name + "][" + _nextAvailableAsteroidBeltInLocal.Nearest1KDistance + "]");
                            return _nextAvailableAsteroidBeltInLocal;
                        }

                        return null;
                    }

                    return null;
                }

                return _nextAvailableAsteroidBeltInLocal;
            }
        }

        private static List<DirectSystemScanResult> _availableOreSitesInLocal = null;

        private static List<DirectSystemScanResult> AvailableOreSitesInLocal
        {
            get
            {
                if (ESCache.Instance.InWarp)
                    return new List<DirectSystemScanResult>();

                if (_availableOreSitesInLocal == null)
                {
                    if (Scanner.SystemScanResults.Any())
                    {
                        _availableOreSitesInLocal = Scanner.SystemScanResults.Where(i =>
                                                                                !i.IsSiteWithNoOre &&
                                                                                !i.IsSiteWithPvP &&
                                                                                !i.IsSiteWithOtherMiners &&
                                                                                !i.IsSiteWithNPCs)
                                                                                .ToList();

                        return _availableOreSitesInLocal;
                    }

                    return new List<DirectSystemScanResult>();
                }

                return _availableOreSitesInLocal;
            }
        }

        //SitesWithNoIce
        private static List<DirectSystemScanResult> _availableIceSitesInLocal = null;

        private static List<DirectSystemScanResult> AvailableIceSitesInLocal
        {
            get
            {
                if (ESCache.Instance.InWarp)
                    return new List<DirectSystemScanResult>();

                if (_availableIceSitesInLocal == null)
                {
                    if (Scanner.SystemScanResults.Any())
                    {
                        _availableIceSitesInLocal = Scanner.SystemScanResults.Where(i =>
                                                                                !i.IsSiteWithNoIce &&
                                                                                !i.IsSiteWithPvP &&
                                                                                !i.IsSiteWithOtherMiners &&
                                                                                !i.IsSiteWithNPCs)
                                                                                .ToList();

                        return _availableIceSitesInLocal;
                    }

                    return new List<DirectSystemScanResult>();
                }

                return _availableIceSitesInLocal;
            }
        }


        private static IEnumerable<EntityCache> _availableAsteroidBeltsInLocal = null;

        private static IEnumerable<EntityCache> AvailableAsteroidBeltsInLocal
        {
            get
            {
                if (ESCache.Instance.InWarp)
                    return new List<EntityCache>();

                if (_availableAsteroidBeltsInLocal == null)
                {
                    if (ESCache.Instance.Entities.Any())
                    {
                        if (ESCache.Instance.Entities.Any(i => i.GroupId == (int)Group.AsteroidBelt))
                        {
                            _availableAsteroidBeltsInLocal = AsteroidBeltsInLocal.Where(i => !Scanner.AsteroidBeltsWithNoOre.ContainsKey(i.Id) &&
                                                                                !Scanner.AsteroidBeltsWithPvP.ContainsKey(i.Id) &&
                                                                                !Scanner.AsteroidBeltsWithOtherMiners.ContainsKey(i.Id) &&
                                                                                !Scanner.AsteroidBeltsWithNPCs.ContainsKey(i.Id));

                            return _availableAsteroidBeltsInLocal;
                        }

                        return new List<EntityCache>();
                    }

                    return new List<EntityCache>();
                }

                return _availableAsteroidBeltsInLocal;
            }
        }

        private static IEnumerable<EntityCache> _asteroidBeltsInLocal = null;

        private static IEnumerable<EntityCache> AsteroidBeltsInLocal
        {
            get
            {
                if (ESCache.Instance.InWarp)
                    return new List<EntityCache>();

                if (_asteroidBeltsInLocal == null)
                {
                    if (ESCache.Instance.Entities.Any())
                    {
                        if (ESCache.Instance.Entities.Any(i => i.GroupId == (int)Group.AsteroidBelt))
                        {
                            _asteroidBeltsInLocal = ESCache.Instance.Entities.Where(i => i.GroupId == (int)Group.AsteroidBelt);
                            return _asteroidBeltsInLocal;
                        }

                        return new List<EntityCache>();
                    }

                    return new List<EntityCache>();
                }

                return _asteroidBeltsInLocal;
            }
        }

        private static IEnumerable<EntityCache> _minableAsteroids = null;

        public static IEnumerable<EntityCache> MinableAsteroids
        {
            get
            {
                try
                {
                    if (ESCache.Instance.InWarp)
                        return new List<EntityCache>();

                    if (_minableAsteroids == null)
                    {
                        if (ESCache.Instance.Entities.Any())
                        {
                            try
                            {
                                List<Ore> VeldsparTypeRoids = new List<Ore>();
                                VeldsparTypeRoids.Add(OreTypes.Veldspar);
                                VeldsparTypeRoids.Add(OreTypes.ConcentratedVeldspar);
                                VeldsparTypeRoids.Add(OreTypes.DenseVeldspar);
                                VeldsparTypeRoids.Add(OreTypes.StableVeldspar);

                                if (VeldsparTypeRoids != null && VeldsparTypeRoids.Any())
                                {
                                    //if (DirectEve.Interval(10000)) Log.WriteLine("VeldsparTypeRoids Count [" + VeldsparTypeRoids.Count + "]");
                                    int iCount = 0;
                                    foreach (var veldsparTypeRoid in VeldsparTypeRoids)
                                    {
                                        iCount++;
                                        //Log.WriteLine("veldsparTypeRoid [" + iCount + "][" + veldsparTypeRoid.Name + "]");
                                    }

                                    foreach (var entityOnGrid in ESCache.Instance.EntitiesOnGrid.Where(i => i.CategoryId == (int)CategoryID.Asteroid))
                                    {
                                        //Log.WriteLine("entityOnGrid.TypeId [" + entityOnGrid.TypeId + "] typename [" + entityOnGrid.TypeName + "]");
                                        if (entityOnGrid.TypeId == (int)OreTypes.Veldspar.TypeId)
                                        {
                                            //if (DirectEve.Interval(10000, 10000, entityOnGrid.Id.ToString())) Log.WriteLine("Found [" + entityOnGrid.TypeName + "] at [" + entityOnGrid.Nearest1KDistance + "]");
                                        }

                                    }

                                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.CategoryId == (int)CategoryID.Asteroid && VeldsparTypeRoids.Any(x => x.TypeId == i.TypeId)))
                                    {
                                        _minableAsteroids = ESCache.Instance.EntitiesOnGrid.Where(i => i.CategoryId == (int)CategoryID.Asteroid && VeldsparTypeRoids.Any(x => x.TypeId == i.TypeId));

                                        if (_minableAsteroids != null)
                                        {
                                            return _minableAsteroids;
                                        }

                                        if (DebugConfig.DebugMiningBehavior) Log.WriteLine("Veldspar: if (_minableAsteroids == null)");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }

                            try
                            {
                                List<Ore> ScorditeTypeRoids = new List<Ore>();
                                ScorditeTypeRoids.Add(OreTypes.Scordite);
                                ScorditeTypeRoids.Add(OreTypes.CondensedScordite);
                                ScorditeTypeRoids.Add(OreTypes.GlossyScordite);
                                ScorditeTypeRoids.Add(OreTypes.MassiveScordite);

                                if (ScorditeTypeRoids != null && ScorditeTypeRoids.Any())
                                {
                                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.CategoryId == (int)CategoryID.Asteroid && ScorditeTypeRoids.Any(x => x.TypeId == i.TypeId)))
                                    {
                                        _minableAsteroids = ESCache.Instance.EntitiesOnGrid.Where(i => i.CategoryId == (int)CategoryID.Asteroid && ScorditeTypeRoids.Any(x => x.TypeId == i.TypeId));
                                        if (_minableAsteroids != null)
                                        {
                                            return _minableAsteroids;
                                        }

                                        if (DebugConfig.DebugMiningBehavior) Log.WriteLine("Scordite: if (_minableAsteroids == null)");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }

                            return new List<EntityCache>();
                        }

                        return new List<EntityCache>();
                    }

                    return _minableAsteroids;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<EntityCache>();
                }
            }
        }

        public static IEnumerable<EntityCache> MiningTargets_Targets
        {
            get
            {
                //#ToDo Fix me
                /**
                AbyssalSpawn.AbyssalSpawnType AbyssalDetectSpawnResult = AbyssalSpawn.DetectSpawn;

                if (AbyssalDetectSpawnResult != AbyssalSpawn.AbyssalSpawnType.Undecided)
                {
                    switch (AbyssalDetectSpawnResult)
                    {
                        case AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindBSSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: AbyssalOvermindBSSpawn");
                                return AbyssalPotentialCombatTargets_AbyssalOvermindSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.AllFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: AllFrigateSpawn");
                                return AbyssalPotentialCombatTargets_AllFrigateSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.HighAngleBattleCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: HighAngleBattleCruiserSpawn");
                                return AbyssalPotentialCombatTargets_HighAngleBattlecruiserSpawn(false, false);
                            }


                        case AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: HighAngleBattleCruiserSpawn");
                                return AbyssalPotentialCombatTargets_LeshakSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.ConcordBSSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: ConcordBSSpawn");
                                return AbyssalPotentialCombatTargets_ConcordSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.CruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: CruiserSpawn");
                                return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: DevotedCruiserSpawn");
                                return AbyssalPotentialCombatTargets_DevotedCruiserSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: EphialtesCruiserSpawn");
                                return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: KarybdisTyrannosSpawn");
                                return AbyssalPotentialCombatTargets_KarybdisTyrannosSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: KikimoraDestroyerSpawn");
                                return AbyssalPotentialCombatTargets_KikimoraDestroyerSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: LeshakBSSpawn");
                                return AbyssalPotentialCombatTargets_LeshakSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: LucidDeepwatcherBSSpawn");
                                return AbyssalPotentialCombatTargets_UndetectedSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: LucidWatchmanCruiserSpawn");
                                return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: VedmakCruiserSpawn");
                                return AbyssalPotentialCombatTargets_VedmakSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: VedmakVilaCruiserSwarmerSpawn");
                                return AbyssalPotentialCombatTargets_VedmakVilaCruiserSwarmerSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.RodivaSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: RodivaSpawn");
                                return AbyssalPotentialCombatTargets_RodivaSpawn(false, false);
                            }
                    }
                }
                **/
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: UndetectedSpawn");
                return null;
            }
        }

        //Ore value info
        //https://harvest.poisonreverse.net/
        //https://www.fuzzwork.co.uk/ore/
        //https://ore.cerlestes.de/ore
        //
        private static bool MineVeldspar { get; set;}

        private static bool MineScordite { get; set; }

        private static bool MinePyroxeres { get; set; }
        private static bool MinePlagioclase { get; set; }
        private static bool MineOmber { get; set; }
        private static bool MineKernite { get; set; }
        private static bool MineJaspet { get; set; }
        private static bool MineHemorphite { get; set; }
        private static bool MineHedbergite { get; set; }
        private static bool MineGneiss { get; set; }
        private static bool MineDarkOchre { get; set; }
        private static bool MineCrokite { get; set; }
        private static bool MineSpodumain { get; set; }
        private static bool MineBistot { get; set; }
        private static bool MineArkonor { get; set; }
        private static bool MineMercoxit { get; set; }
        private static bool MineIce { get; set; }

        public static IEnumerable<EntityCache> Targeting_MiningTargetsToLock
        {
            get
            {
                if (ESCache.Instance.Asteroids.Any())
                {
                    if (!ESCache.Instance.Modules.Any(i => i.IsOreMiningModule || i.IsIceMiningModule))
                    {
                        if (DebugConfig.DebugMiningBehavior && DirectEve.Interval(10000)) Log.WriteLine("if (!ESCache.Instance.Modules.Any(i => i.IsMiningModule))");
                        return new List<EntityCache>();
                    }

                    if (ESCache.Instance.Asteroids.All(e => !e.IsReadyToTarget))
                    {
                        if (DebugConfig.DebugMiningBehavior && DirectEve.Interval(10000)) Log.WriteLine("if (ESCache.Instance.Asteroids.All(e => !e.IsReadyToTarget))");
                        return new List<EntityCache>();
                    }

                    if (DebugConfig.DebugMiningBehavior && DirectEve.Interval(10000))  Log.WriteLine("ESCache.Instance.Asteroids Count [" + ESCache.Instance.Asteroids.Count() + "]");

                    return ESCache.Instance.Asteroids.Where(e => e.IsReadyToTarget)
                        .OrderByDescending(a => a.IsBistot && MineBistot)
                        //.ThenByDescending(j => j.IsMercoxit && MineMercoxit)
                        .ThenByDescending(b => b.IsArkonor && MineArkonor)
                        .ThenByDescending(c => c.IsSpodumain && MineSpodumain)
                        .ThenByDescending(d => d.IsCrokite && MineCrokite)
                        .ThenByDescending(e => e.IsDarkOchre && MineDarkOchre)
                        .ThenByDescending(f => f.IsGneiss && MineGneiss)
                        .ThenByDescending(g => g.IsHedbergite && MineHedbergite)
                        .ThenByDescending(h => h.IsHemorphite && MineHemorphite)
                        .ThenByDescending(i => i.IsJaspet && MineJaspet)
                        .ThenByDescending(j => j.IsKernite && MineKernite)
                        .ThenByDescending(k => k.IsOmber && MineOmber)
                        .ThenByDescending(l => l.IsPlagioclase && MinePlagioclase)
                        .ThenByDescending(m => m.IsPyroxeres && MinePyroxeres)
                        .ThenByDescending(n => n.IsScordite && MineScordite)
                        .ThenByDescending(o => o.IsVeldspar && MineVeldspar);
                }

                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("!if (ESCache.Instance.Asteroids.Any())");
                return new List<EntityCache>();
            }
        }

        private static void FindAsteroidToMineState()
        {
            if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
            {
                if (ESCache.Instance.ActiveShip.GivenName == Settings.Instance.MiningShipName)
                {
                    Log.WriteLine("Docked");
                    TravelerDestination.Undock();
                    return;
                }

                Log.WriteLine("Docked and not in our mining ship: we should switch ships here");
                ChangeMiningBehaviorState(MiningBehaviorState.Switch);
                return;
            }

            if (IsThisAGoodPlaceToMine())
            {
                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("FindAsteroidToMineState: IsThisAGoodPlaceToMine");
                if (MinableAsteroids.Any())
                {
                    if (!ESCache.Instance.DirectEve.Bookmarks.Any(i => i.IsOnGridWithMe))
                    {
                        if (DebugConfig.DebugMiningBehavior) Log.WriteLine("FindAsteroidToMineState: if (!ESCache.Instance.DirectEve.Bookmarks.Any(i => i.IsOnGridWithMe))");
                        if (DebugConfig.DebugMiningBehavior) Log.WriteLine("FindAsteroidToMineState: ChangeMiningBehaviorState(MiningBehaviorState.MakeBookmarkForNextOreRoid);");
                        ChangeMiningBehaviorState(MiningBehaviorState.MakeBookmarkForNextOreRoid);
                        return;
                    }

                    if (DebugConfig.DebugMiningBehavior) Log.WriteLine("FindAsteroidToMineState: if (MinableAsteroids.Any())");
                    if (MinableAsteroids.OrderBy(i => i.Distance).FirstOrDefault().Distance > 8000)
                    {
                        if (DebugConfig.DebugMiningBehavior) Log.WriteLine("FindAsteroidToMineState: if (MinableAsteroids.OrderBy(i => i.Distance).Any(myAsteroid => 40000 > myAsteroid.Distance))");
                        NavigateOnGrid.NavigateToTarget(MinableAsteroids.OrderBy(i => i.Distance).FirstOrDefault(), 2000);
                        return;
                    }

                    if (DebugConfig.DebugMiningBehavior) Log.WriteLine("FindAsteroidToMineState: MineRoid();");
                    MineRoid();
                    return;
                }

                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("FindAsteroidToMineState: !if (MinableAsteroids.Any())");
                GotoNextAsteroidBeltOrSite();
                return;
            }

            if (DebugConfig.DebugMiningBehavior) Log.WriteLine("FindAsteroidToMineState: !if (IsThisAGoodPlaceToMine())");
            GotoNextAsteroidBeltOrSite();
            return;
        }

        private static EntityCache ClosestMinableAsteroid
        {
            get
            {
                try
                {
                    if (MinableAsteroids.Any(x => x.IsOnGridWithMe && MiningModules.All(module => module.LastTargetId != x.Id)))
                    {
                        return MinableAsteroids.OrderBy(i => i.Distance).FirstOrDefault();
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

        private static IEnumerable<ModuleCache> MiningModules
        {
            get
            {
                if (ESCache.Instance.Modules.Any(i => (i.IsOreMiningModule || i.IsIceMiningModule) && i.IsOnline))
                {
                    return ESCache.Instance.Modules.Where(i => (i.IsOreMiningModule || i.IsIceMiningModule) && i.IsOnline);
                }

                return new List<ModuleCache>();
            }
        }

        private static double MiningRange
        {
            get
            {
                if (MiningModules.Any())
                {
                    return MiningModules.FirstOrDefault().OptimalRange;
                }

                return 5000;
            }
        }

        private static bool MineRoid()
        {
            if (MinableAsteroids.Any())
            {
                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("if (MinableAsteroids.Any())");
                if (ClosestMinableAsteroid != null)
                {
                    if (DebugConfig.DebugMiningBehavior) Log.WriteLine("if (ClosestMinableAsteroid != null)");
                    //if (ClosestMinableAsteroid.Distance > MiningRange)
                    //{
                    //    NavigateOnGrid.NavigateToTarget(ClosestMinableAsteroid, 5000);
                    //}
                    int intCount = 0;
                    foreach (ModuleCache miningModule in MiningModules)
                    {
                        intCount++;
                        if (DebugConfig.DebugMiningBehavior) Log.WriteLine("[" + intCount + "][" + miningModule.TypeName + "] isActive [" + miningModule.IsActive + "] ID [" + miningModule.ItemId + "]");
                    }

                    EntityCache ClosestLockedAsteroid = ESCache.Instance.Targets.Where(i => i.CategoryId == (int)CategoryID.Asteroid).OrderBy(x => x.Distance).FirstOrDefault();
                    if (ClosestLockedAsteroid != null)
                    {
                        if (MiningModules.Any(i => !i.IsActive && !i.IsInLimboState && i.OptimalRange > ClosestLockedAsteroid.Distance))
                        {
                            if (DebugConfig.DebugMiningBehavior) Log.WriteLine("if (MiningModules.Any(i => !i.IsActive && !i.IsInLimboState && i.OptimalRange > ClosestMinableAsteroid.Distance))");
                            if (MiningModules.FirstOrDefault(i => !i.IsActive && !i.IsInLimboState).Activate(ClosestMinableAsteroid))
                            {
                                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("if (MiningModules.FirstOrDefault(i => !i.IsActive && !i.IsInLimboState).Activate(ClosestMinableAsteroid))");
                                return true;
                            }

                            if (DebugConfig.DebugMiningBehavior) Log.WriteLine("All mining modules are active?!");
                            return true;
                        }

                        if (DebugConfig.DebugMiningBehavior) Log.WriteLine("!if (MiningModules.Any(i => !i.IsActive && !i.IsInLimboState && i.OptimalRange > ClosestMinableAsteroid.Distance))");
                        return true;
                    }

                    if (DebugConfig.DebugMiningBehavior) Log.WriteLine("if (ClosestLockedAsteroid != null)");
                    return false;
                }

                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("if (ClosestMinableAsteroid == null)");
                return false;
            }

            if (DebugConfig.DebugMiningBehavior) Log.WriteLine("!if (MinableAsteroids.Any())");
            return false;
        }

        private static bool IsThisAGoodPlaceToMine()
        {
            if (ESCache.Instance.ClosestCitadel != null && ESCache.Instance.ClosestCitadel.IsOnGridWithMe)
                return false;

            if (ESCache.Instance.ClosestStation != null && ESCache.Instance.ClosestStation.IsOnGridWithMe)
                return false;

            if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe)
                return false;

            if (ESCache.Instance.ClosestWormhole != null && ESCache.Instance.ClosestWormhole.IsOnGridWithMe)
                return false;

            if (ESCache.Instance.ClosestPlanet != null && ESCache.Instance.ClosestPlanet.IsOnGridWithMe)
                return false;

            if (ESCache.Instance.Moons != null && ESCache.Instance.Moons.Any(i => i.IsOnGridWithMe))
                return false;

            if (MinableAsteroids.Any())
            {
                try
                {
                    //
                    // Detect other miners
                    //
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => 50000 > i.Distance && i.IsMiner && i.Id != ESCache.Instance.ActiveShip.ItemId))
                    {
                        //belts
                        if (AvailableAsteroidBeltsInLocal.Any(i => (int)Distances.OnGridWithMe > i.Distance))
                        {
                            Scanner.AsteroidBeltsWithOtherMiners.AddOrUpdate(AvailableAsteroidBeltsInLocal.FirstOrDefault(i => (int)Distances.OnGridWithMe > i.Distance).Id, DateTime.UtcNow);
                            return false;
                        }

                        //sites
                        if (Scanner.SystemScanResults.Any(i => i.IsOnGridWithMe))
                        {
                            var thisMiningSite = AvailableOreSitesInLocal.FirstOrDefault(i => i.IsOnGridWithMe);
                            Scanner.SitesWithOtherMiners.AddOrUpdate(thisMiningSite.Id, DateTime.UtcNow);
                            return false;
                        }

                        return false;
                    }

                    //
                    // Detect PvP
                    //
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => 50000 > i.Distance && i.IsPlayer && !i.IsMiner && i.Id != ESCache.Instance.ActiveShip.ItemId))
                    {
                        //belts
                        if (AvailableAsteroidBeltsInLocal.Any(i => (int)Distances.OnGridWithMe > i.Distance))
                        {
                            Scanner.AsteroidBeltsWithPvP.AddOrUpdate(AvailableAsteroidBeltsInLocal.FirstOrDefault(i => (int)Distances.OnGridWithMe > i.Distance).Id, DateTime.UtcNow);
                            return false;
                        }

                        //sites
                        if (Scanner.SystemScanResults.Any(i => i.IsOnGridWithMe))
                        {
                            var thisMiningSite = AvailableOreSitesInLocal.FirstOrDefault(i => i.IsOnGridWithMe);
                            Scanner.SitesWithPvP.AddOrUpdate(thisMiningSite.Id, DateTime.UtcNow);
                            return true;
                        }

                        return false;
                    }

                    //
                    // Detect NPCs we cant kill? Frigates should be fine, right?
                    //
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsNPCCruiser || i.IsNPCBattlecruiser || i.IsNPCBattleship || i.IsNPCDestroyer || i.IsNPCCapitalShip))
                    {
                        //belts
                        if (AvailableAsteroidBeltsInLocal.Any(i => (int)Distances.OnGridWithMe > i.Distance))
                        {
                            Scanner.AsteroidBeltsWithNPCs.AddOrUpdate(AvailableAsteroidBeltsInLocal.FirstOrDefault(i => (int)Distances.OnGridWithMe > i.Distance).Id, DateTime.UtcNow);
                            return false;
                        }

                        //sites
                        if (Scanner.SystemScanResults.Any(i => i.IsOnGridWithMe))
                        {
                            var thisMiningSite = AvailableOreSitesInLocal.FirstOrDefault(i => i.IsOnGridWithMe);
                            Scanner.SitesWithNPCs.AddOrUpdate(thisMiningSite.Id, DateTime.UtcNow);
                            return false;
                        }

                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return true;
            }

            if (AvailableAsteroidBeltsInLocal.Any(i => i.IsOnGridWithMe))
            {
                var thisAsteroidBelt = AvailableAsteroidBeltsInLocal.FirstOrDefault(i => i.IsOnGridWithMe);
                if (!Scanner.AsteroidBeltsWithNoOre.Any(i => i.Key == thisAsteroidBelt.Id))
                {
                    Scanner.AsteroidBeltsWithNoOre.AddOrUpdate(thisAsteroidBelt.Id, DateTime.UtcNow);
                }
            }
            else if (Scanner.SystemScanResults.Any(i => i.IsOnGridWithMe))
            {
                var thisMiningSite = AvailableOreSitesInLocal.FirstOrDefault(i => i.IsOnGridWithMe);
                if (!Scanner.SitesWithNoOre.Any(i => i.Key == thisMiningSite.Id))
                {
                    Scanner.SitesWithNoOre.AddOrUpdate(thisMiningSite.Id, DateTime.UtcNow);
                }
            }

            return false;
        }

        private static bool TravelToMiningBookmarksIfAny()
        {
            if (!string.IsNullOrEmpty(MiningGasBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> miningBookmarks = ESCache.Instance.BookmarksByLabel(MiningGasBookmarkPrefix ?? "");
                if (miningBookmarks == null || !miningBookmarks.Any())
                {
                    miningBookmarks = ESCache.Instance.BookmarksByLabel(MiningOreBookmarkPrefix ?? "");
                }

                if (miningBookmarks != null && miningBookmarks.Any(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.Distance < 10000000 && b.Distance > (int)Distances.WarptoDistance))
                {
                    DirectBookmark miningBookmark = miningBookmarks.FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.Distance < 10000000 && b.Distance > (int)Distances.WarptoDistance);

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (miningBookmark == null)
                    {
                        Log.WriteLine("No Bookmark found with MiningGasBookmarkPrefix [" + MiningGasBookmarkPrefix + "] or MiningOreBookmarkPrefix [" + MiningOreBookmarkPrefix + "] in the name");
                        State.CurrentTravelerState = TravelerState.Idle;
                        if (ESCache.Instance.InStation) TravelerDestination.Undock();
                        else ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
                    }
                    else if (miningBookmark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + miningBookmark.Title);
                            Traveler.Destination = new BookmarkDestination(miningBookmark);
                        }

                        Traveler.ProcessState();
                        if (State.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            State.CurrentTravelerState = TravelerState.Idle;
                            ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmark in System!");
                        State.CurrentTravelerState = TravelerState.Idle;
                        if (ESCache.Instance.InStation) TravelerDestination.Undock();
                    }

                    return false;
                }
            }

            if (!ESCache.Instance.InWarp && ESCache.Instance.Entities.Any(i => i.IsMinable))
            {
                Log.WriteLine("if (!ESCache.Instance.InWarp && ESCache.Instance.Entities.Any(i => i.IsAsteroid || i.isGasCloud))");
                State.CurrentTravelerState = TravelerState.Idle;
                ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
                Traveler.Destination = null;
                return false;
            }

            Log.WriteLine("No Bookmarks contain [" + MiningGasBookmarkPrefix + "] or [" + MiningOreBookmarkPrefix + "] in local");
            return true;
        }

        private static bool TravelToOreSiteIfAny()
        {
            if (Scanner.SystemScanResults.Any(i => i.IsOreSite))
            {
                if (ESCache.Instance.InWarp)
                    return false;

                if (IsThisAGoodPlaceToMine())
                {

                }

                return false;
            }

            Log.WriteLine("No Ore Anoms or Sigs in local");
            return true;
        }

        private static bool TravelToIceSiteIfAny()
        {
            return false;
            if (!string.IsNullOrEmpty(MiningGasBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> miningBookmarks = ESCache.Instance.BookmarksByLabel(MiningGasBookmarkPrefix ?? "");
                if (miningBookmarks == null || !miningBookmarks.Any())
                {
                    miningBookmarks = ESCache.Instance.BookmarksByLabel(MiningOreBookmarkPrefix ?? "");
                }

                if (miningBookmarks != null && miningBookmarks.Any(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.Distance < 10000000 && b.Distance > (int)Distances.WarptoDistance))
                {
                    DirectBookmark miningBookmark = miningBookmarks.FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.Distance < 10000000 && b.Distance > (int)Distances.WarptoDistance);

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (miningBookmark == null)
                    {
                        Log.WriteLine("No Bookmark found with MiningGasBookmarkPrefix [" + MiningGasBookmarkPrefix + "] or MiningOreBookmarkPrefix [" + MiningOreBookmarkPrefix + "] in the name");
                        State.CurrentTravelerState = TravelerState.Idle;
                        if (ESCache.Instance.InStation) TravelerDestination.Undock();
                        else ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
                    }
                    else if (miningBookmark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + miningBookmark.Title);
                            Traveler.Destination = new BookmarkDestination(miningBookmark);
                        }

                        Traveler.ProcessState();
                        if (State.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            State.CurrentTravelerState = TravelerState.Idle;
                            ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmark in System!");
                        State.CurrentTravelerState = TravelerState.Idle;
                        if (ESCache.Instance.InStation) TravelerDestination.Undock();
                    }

                    return false;
                }
            }

            if (!ESCache.Instance.InWarp && ESCache.Instance.Entities.Any(i => i.IsMinable))
            {
                Log.WriteLine("if (!ESCache.Instance.InWarp && ESCache.Instance.Entities.Any(i => i.IsAsteroid || i.isGasCloud))");
                State.CurrentTravelerState = TravelerState.Idle;
                ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
                Traveler.Destination = null;
                return false;
            }

            Log.WriteLine("No Bookmarks contain [" + MiningGasBookmarkPrefix + "] or [" + MiningOreBookmarkPrefix + "] in local");
            return true;
        }

        private static bool GotoNextAsteroidBeltOrSite()
        {
            if (ESCache.Instance.InWarp)
                return false;

            if (AllowMiningAtBookmarks) //Any Anomolies in local? Any mining anomolies?)
            {
                if (!TravelToMiningBookmarksIfAny()) return false;
            }

            /**
            if (DirectEve.Interval(20000)) Log.WriteLine("AllowMiningInMiningSignatures [" + AllowMiningInMiningSignatures + "]");

            if (AllowMiningInMiningSignatures && ESCache.Instance.Modules.Any(i => i.IsOreMiningModule)) //Any Signatures in local? Any mining signatures?)
            {
                if (DirectEve.Interval(20000))
                {
                    Log.WriteLine("Signature OreSites [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && !i.IsAnomaly) + "]");
                    if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsOreSite && !i.IsAnomaly))
                    {
                        Log.WriteLine("Signature OreSites: !IsSiteWithNoOre [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && !i.IsAnomaly && !i.IsSiteWithNoOre) + "]");
                        Log.WriteLine("Signature OreSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && !i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners) + "]");
                        Log.WriteLine("Signature OreSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners !IsSiteWithPvP[" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && !i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP) + "]");
                    }
                }
                //
                // insert logic here to find Ore sigs
                //
                if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsOreSite && !i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP))
                {
                    //Gas Sites
                    //https://docs.google.com/spreadsheets/d/1ew-27FWLQZ8y5kudmpUbM9SAbZma2WmDuOVuAoO-XNM
                    //Ore Sites
                    //https://wiki.eveuniversity.org/Wormhole_sites#Ore_sites
                    DirectSystemScanResult nextMiningSite = Scanner.SystemScanResults.Where(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP)
                        .OrderByDescending(i => i.TypeName.Contains("Exceptional Core Deposit")) //C5 and C6
                        .ThenByDescending(i => i.TypeName.Contains("Infrequent Core Deposit")) //C5 and C6
                        .ThenByDescending(i => i.TypeName.Contains("Isolated Core Deposit")) //C5 and C6
                        .ThenByDescending(i => i.TypeName.Contains("Rarified Core Deposit")) //C5 and C6
                        .ThenByDescending(i => i.TypeName.Contains("Uncommon Core Deposit")) //C5 and C6
                        .ThenByDescending(i => i.TypeName.Contains("Unusual Core Deposit")) //C5 and C6
                        .ThenByDescending(i => i.TypeName.Contains("Colossal Asteroid Cluster")) //0.0
                        .ThenByDescending(i => i.TypeName.Contains("Enormous Asteroid Cluster")) // 0.0
                        .ThenByDescending(i => i.TypeName.Contains("Large Asteroid Cluster")) // 0.0
                        .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster")) // 0.0
                        .ThenByDescending(i => i.TypeName.Contains("Small Asteroid Cluster")) // 0.0
                        .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster"))
                        .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster"))
                        .ThenByDescending(i => i.TypeName.Contains("Common Perimeter Deposit")) //All Classes of W-Space
                        .ThenByDescending(i => i.TypeName.Contains("Average Frontier Deposit")) //All Classes of W-Space
                        .ThenByDescending(i => i.TypeName.Contains("Unexceptional Frontier Deposit")) //All Classes of W-Space
                        .ThenByDescending(i => i.TypeName.Contains("Ordinary Perimeter Deposit")) //All Classes of W-Space
                        .ThenByDescending(i => i.TypeName.Contains("Common Perimeter Deposit")) //All Classes of W-Space
                        .ThenByDescending(i => i.TypeName.Contains("Medium Jaspet Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Large Gneiss Deposits")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Average Gneiss Deposits")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Small Gneiss Deposits")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Large Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Average Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Small Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Large Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Average Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Small Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Large Crokite and Dark Ochre Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Average Crokite and Dark Ochre Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Small Crokite and Dark Ochre Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Large Dark Ochre and Gneiss Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Average Dark Ochre and Gneiss Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Small Dark Ochre and Gneiss Deposit")) //Low sec
                        .ThenByDescending(i => i.TypeName.Contains("Remnants")) //High sec - Veldspar
                        .FirstOrDefault();

                    if (nextMiningSite != null)
                    {
                        if (!nextMiningSite.IsOnGridWithMe)
                        {
                            Log.WriteLine("if (!nextMiningSite.IsOnGridWithMe)");
                            if (nextMiningSite.WarpTo())
                            {
                                return false;
                            }

                            return false;
                        }
                        else Log.WriteLine("nextMiningSite [" + nextMiningSite.TypeName + "] IsOnGridWithMe !!!");
                    }
                    else Log.WriteLine("nextMiningSite == null");
                }
            }
            **/

            if (DirectEve.Interval(20000)) Log.WriteLine("AllowMiningInMiningAnomolies [" + AllowMiningInMiningAnomolies + "]");

            if (AllowMiningInMiningAnomolies) //Any Anomolies in local? Any mining anomolies?)
            {
                if (DirectEve.Interval(20000))
                {
                    Log.WriteLine("Anomaly OreSites [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly) + "] Mining Modules Available [" + ESCache.Instance.Modules.Count(i => i.IsOreMiningModule) + "]");
                    if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsIceSite && !i.IsAnomaly))
                    {
                        Log.WriteLine("Anomaly OreSites: !IsSiteWithNoOre [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre) + "]");
                        Log.WriteLine("Anomaly OreSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners) + "]");
                        Log.WriteLine("Anomaly OreSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners !IsSiteWithPvP[" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP) + "]");
                    }
                }

                if (ESCache.Instance.Modules.Any(i => i.IsOreMiningModule))
                {
                    //
                    // insert logic here to find Ore anoms
                    //
                    if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly))
                    {
                        if (DirectEve.Interval(20000))
                        {
                            Log.WriteLine("Anomaly OreSites [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly) + "]");
                            if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsOreSite && !i.IsAnomaly))
                            {
                                Log.WriteLine("Anomaly OreSites: !IsSiteWithNoOre [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre) + "]");
                                Log.WriteLine("Anomaly OreSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners) + "]");
                                Log.WriteLine("Anomaly OreSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners !IsSiteWithPvP[" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP) + "]");
                            }
                        }

                        //Gas Sites
                        //https://docs.google.com/spreadsheets/d/1ew-27FWLQZ8y5kudmpUbM9SAbZma2WmDuOVuAoO-XNM
                        //Ore Sites
                        //https://wiki.eveuniversity.org/Wormhole_sites#Ore_sites
                        DirectSystemScanResult nextMiningSite = Scanner.SystemScanResults.Where(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP)
                            .OrderByDescending(i => i.TypeName.Contains("Exceptional Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Infrequent Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Isolated Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Rarified Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Uncommon Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Unusual Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Colossal Asteroid Cluster")) //0.0
                            .ThenByDescending(i => i.TypeName.Contains("Enormous Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Large Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Small Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster"))
                            .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster"))
                            .ThenByDescending(i => i.TypeName.Contains("Common Perimeter Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Average Frontier Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Unexceptional Frontier Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Ordinary Perimeter Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Common Perimeter Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Medium Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Gneiss Deposits")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Gneiss Deposits")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Gneiss Deposits")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Crokite and Dark Ochre Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Crokite and Dark Ochre Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Crokite and Dark Ochre Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Remnants")) //High sec - Veldspar
                            .FirstOrDefault();

                        if (nextMiningSite != null)
                        {
                            if (!nextMiningSite.IsOnGridWithMe)
                            {
                                Log.WriteLine("if (!nextMiningSite.IsOnGridWithMe)");
                                if (nextMiningSite.WarpTo())
                                {
                                    return false;
                                }

                                return false;
                            }
                            else Log.WriteLine("nextMiningSite [" + nextMiningSite.TypeName + "] IsOnGridWithMe !!!");
                        }
                        else Log.WriteLine("nextMiningSite == null");
                    }
                }
            }

            if (DirectEve.Interval(20000)) Log.WriteLine("AllowMiningInIceSignatures [" + AllowMiningInIceSignatures + "]");

            if (AllowMiningInIceSignatures) //Any Signatures in local? Any mining signatures?)
            {
                if (DirectEve.Interval(20000))
                {
                    Log.WriteLine("Signature IceSites [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsSignature) + "] IceMining Modules Available [" + ESCache.Instance.Modules.Count(i => i.IsIceMiningModule) + "]");
                    if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsIceSite && !i.IsAnomaly))
                    {
                        Log.WriteLine("Signature IceSites: !IsSiteWithNoOre [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsSignature && !i.IsSiteWithNoOre) + "]");
                        Log.WriteLine("Signature IceSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsSignature && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners) + "]");
                        Log.WriteLine("Signature IceSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners !IsSiteWithPvP[" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsSignature && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP) + "]");
                    }
                }

                if (ESCache.Instance.Modules.Any(i => i.IsIceMiningModule))
                {
                    //
                    // insert logic here to find Ice sigs
                    //
                    if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsIceSite && !i.IsAnomaly && !i.IsSiteWithNoIce && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP))
                    {
                        //Gas Sites
                        //https://docs.google.com/spreadsheets/d/1ew-27FWLQZ8y5kudmpUbM9SAbZma2WmDuOVuAoO-XNM
                        //Ore Sites
                        //https://wiki.eveuniversity.org/Wormhole_sites#Ore_sites
                        DirectSystemScanResult nextIceSite = Scanner.SystemScanResults.Where(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP)
                            .OrderByDescending(i => i.TypeName.Contains("Exceptional Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Infrequent Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Isolated Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Rarified Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Uncommon Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Unusual Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Colossal Asteroid Cluster")) //0.0
                            .ThenByDescending(i => i.TypeName.Contains("Enormous Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Large Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Small Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster"))
                            .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster"))
                            .ThenByDescending(i => i.TypeName.Contains("Common Perimeter Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Average Frontier Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Unexceptional Frontier Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Ordinary Perimeter Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Common Perimeter Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Medium Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Gneiss Deposits")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Gneiss Deposits")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Gneiss Deposits")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Crokite and Dark Ochre Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Crokite and Dark Ochre Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Crokite and Dark Ochre Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Remnants")) //High sec - Veldspar
                            .FirstOrDefault();

                        if (nextIceSite != null)
                        {
                            if (!nextIceSite.IsOnGridWithMe)
                            {
                                Log.WriteLine("if (!nextIceSite.IsOnGridWithMe)");
                                if (nextIceSite.WarpTo())
                                {
                                    return false;
                                }

                                return false;
                            }
                            else Log.WriteLine("nextIceSite [" + nextIceSite.TypeName + "] IsOnGridWithMe !!!");
                        }
                        else Log.WriteLine("nextIceSite == null");
                    }
                }
            }

            if (DirectEve.Interval(20000)) Log.WriteLine("AllowMiningInIceAnomolies [" + AllowMiningInIceAnomolies + "]");

            if (AllowMiningInIceAnomolies) //Any Anomolies in local? Any mining anomolies?)
            {
                if (DirectEve.Interval(20000))
                {
                    Log.WriteLine("Anomaly IceSites [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsAnomaly) + "] IceMining Modules Available [" + ESCache.Instance.Modules.Count(i => i.IsIceMiningModule) + "]");
                    if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsIceSite && !i.IsAnomaly))
                    {
                        Log.WriteLine("Anomaly IceSites: !IsSiteWithNoOre [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsAnomaly && !i.IsSiteWithNoOre) + "]");
                        Log.WriteLine("Anomaly IceSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners) + "]");
                        Log.WriteLine("Anomaly IceSites: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners !IsSiteWithPvP[" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP) + "]");
                    }
                }

                if (ESCache.Instance.Modules.Any(i => i.IsIceMiningModule))
                {
                    //
                    // insert logic here to find Ice anoms
                    //
                    if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsIceSite && i.IsAnomaly && !i.IsSiteWithNoIce && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP))
                    {
                        //Gas Sites
                        //https://docs.google.com/spreadsheets/d/1ew-27FWLQZ8y5kudmpUbM9SAbZma2WmDuOVuAoO-XNM
                        //Ore Sites
                        //https://wiki.eveuniversity.org/Wormhole_sites#Ore_sites
                        DirectSystemScanResult nextIceSite = Scanner.SystemScanResults.Where(i => i.IsPointResult && i.IsOreSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP)
                            .OrderByDescending(i => i.TypeName.Contains("Exceptional Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Infrequent Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Isolated Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Rarified Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Uncommon Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Unusual Core Deposit")) //C5 and C6
                            .ThenByDescending(i => i.TypeName.Contains("Colossal Asteroid Cluster")) //0.0
                            .ThenByDescending(i => i.TypeName.Contains("Enormous Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Large Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Small Asteroid Cluster")) // 0.0
                            .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster"))
                            .ThenByDescending(i => i.TypeName.Contains("Medium Asteroid Cluster"))
                            .ThenByDescending(i => i.TypeName.Contains("Common Perimeter Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Average Frontier Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Unexceptional Frontier Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Ordinary Perimeter Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Common Perimeter Deposit")) //All Classes of W-Space
                            .ThenByDescending(i => i.TypeName.Contains("Medium Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Gneiss Deposits")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Gneiss Deposits")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Gneiss Deposits")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Hedbergite, Hemorphite and Jaspet Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Crokite, Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Crokite and Dark Ochre Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Crokite and Dark Ochre Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Crokite and Dark Ochre Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Large Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Average Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Small Dark Ochre and Gneiss Deposit")) //Low sec
                            .ThenByDescending(i => i.TypeName.Contains("Remnants")) //High sec - Veldspar
                            .FirstOrDefault();

                        if (nextIceSite != null)
                        {
                            if (!nextIceSite.IsOnGridWithMe)
                            {
                                Log.WriteLine("if (!nextIceSite.IsOnGridWithMe)");
                                if (nextIceSite.WarpTo())
                                {
                                    return false;
                                }

                                return false;
                            }
                            else Log.WriteLine("nextIceSite [" + nextIceSite.TypeName + "] IsOnGridWithMe !!!");
                        }
                        else Log.WriteLine("nextIceSite == null");
                    }
                }
            }

            if (DirectEve.Interval(20000)) Log.WriteLine("AllowMiningInAsteroidBelts [" + AllowMiningInAsteroidBelts + "]");

            if (AllowMiningInAsteroidBelts)
            {
                if (DirectEve.Interval(20000))
                {
                    Log.WriteLine("AsteroidBelts [" + ESCache.Instance.AsteroidBelts.Count() + "] Mining Modules Available [" + ESCache.Instance.Modules.Count(i => i.IsOreMiningModule) + "]");
                    //if (Scanner.SystemScanResults.Any(i => i.IsPointResult && i.IsIceSite && !i.IsAnomaly))
                    //{
                    //    Log.WriteLine("AsteroidBelts: !IsSiteWithNoOre [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsAnomaly && !i.IsSiteWithNoOre) + "]");
                    //    Log.WriteLine("AsteroidBelts: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners [" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners) + "]");
                    //    Log.WriteLine("AsteroidBelts: !IsSiteWithNoOre !IsSiteWithNPCs !IsSiteWithOtherMiners !IsSiteWithPvP[" + Scanner.SystemScanResults.Count(i => i.IsPointResult && i.IsIceSite && i.IsAnomaly && !i.IsSiteWithNoOre && !i.IsSiteWithNPCs && !i.IsSiteWithOtherMiners && !i.IsSiteWithPvP) + "]");
                    //}
                }


                if (ESCache.Instance.Modules.Any(i => i.IsOreMiningModule))
                {
                    if (NextAvailableAsteroidBeltInLocal != null)
                    {
                        Log.WriteLine("if (AllowMiningInAsteroidBelts && NextAvailableAsteroidBeltInLocal != null)");
                        if (NextAvailableAsteroidBeltInLocal.Distance > (int)Distances.WarptoDistance)
                        {
                            Log.WriteLine("NextAvailableAsteroidBeltInLocal.Distance [" + Math.Round(NextAvailableAsteroidBeltInLocal.Distance / 1000, 0) + "k] > WarptoDistance [" + (int)Distances.WarptoDistance + "]");
                            NavigateOnGrid.NavigateToTarget(NextAvailableAsteroidBeltInLocal, 0);
                            return false;
                        }

                        Log.WriteLine("NextAvailableAsteroidBeltInLocal.Distance [" + Math.Round(NextAvailableAsteroidBeltInLocal.Distance / 1000, 0) + "k]");
                        return true;
                    }
                }
            }

            //
            // do we want to travel to another system to find a mining site?
            //
            Log.WriteLine("if (NextAvailableAsteroidBeltInLocal == null)");
            ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
            return false;
        }

        private static int MiningPrerequisiteCheckRetries = 0;

        private static DateTime LastMiningPrerequisiteCheck = DateTime.UtcNow;

        private static bool MiningPrerequisiteCheck()
        {
            if (DateTime.UtcNow < LastMiningPrerequisiteCheck.AddMinutes(10))
            {
                ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                MiningPrerequisiteCheckRetries++;
                return false;
            }

            if (MiningPrerequisiteCheckRetries > 3)
            {
                Log.WriteLine("MiningPrerequisiteCheck: if (AbyssalSitePrerequisiteCheckRetries > 10): go home");
                ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                MiningPrerequisiteCheckRetries++;
                return false;
            }

            if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
            {
                if (ESCache.Instance.CurrentShipsCargo.Items.All(i => i.GroupId != (int) Group.AbyssalDeadspaceFilament))
                {
                    Log.WriteLine("MiningPrerequisiteCheck: We have no filaments in our cargo: go home");
                    ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                    MiningPrerequisiteCheckRetries++;
                    return false;
                }

                DirectItem abyssalFilament = ESCache.Instance.CurrentShipsCargo.Items.Find(i => i.GroupId == (int) Group.AbyssalDeadspaceFilament);
                if (abyssalFilament != null && !abyssalFilament.IsSafeToUseAbyssalKeyHere)
                {
                    Log.WriteLine("MiningPrerequisiteCheck: We have a filament but it is not safe to activate it here! go home and pause");
                    ESCache.Instance.PauseAfterNextDock = true;
                    ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                    MiningPrerequisiteCheckRetries++;
                    return false;
                }

                if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.All(i => !i.IsEnergyWeapon))
                {
                    if (ESCache.Instance.CurrentShipsCargo.UsedCapacity > ESCache.Instance.CurrentShipsCargo.Capacity * .8)
                    {
                        Log.WriteLine("MiningPrerequisiteCheck: Less than 80% of our cargo space left: go home");
                        ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                        MiningPrerequisiteCheckRetries++;
                        return false;
                    }

                    if (ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.CategoryId == (int)CategoryID.Charge))
                    {
                        foreach (DirectItem ammoItem in ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.CategoryId == (int)CategoryID.Charge))
                        {
                            Log.WriteLine("MiningPrerequisiteCheck: CargoHoldItem: [" + ammoItem.TypeName + "] TypeId [" + ammoItem.TypeId + "] Quantity [" + ammoItem.Quantity + "]");

                            if (ammoItem.Quantity < 2500 && ESCache.Instance.Weapons.All(i => i.GroupId == (int)Group.ProjectileWeapon ||
                                                                                              i.GroupId == (int)Group.HybridWeapon ||
                                                                                              i.IsMissileLauncher))
                            {
                                Log.WriteLine("MiningPrerequisiteCheck: Less than 2500 units, go back to base");
                                ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                                MiningPrerequisiteCheckRetries++;
                                return false;
                            }

                            if (ammoItem.Quantity < 500 && ESCache.Instance.Weapons.All(i => i.GroupId == (int)Group.PrecursorWeapon))
                            {
                                Log.WriteLine("MiningPrerequisiteCheck: Less than 500 units of Precursor weapon ammo, go back to base");
                                ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                                MiningPrerequisiteCheckRetries++;
                                return false;
                            }
                        }

                        Log.WriteLine("MiningPrerequisiteCheck: We have enough ammo.");
                        MiningPrerequisiteCheckRetries = 0;
                        return true;
                        /**
                        if (Drones.UseDrones)
                        {
                            //if (Drones.DroneBay != null && Drones.DroneBay.Capacity > 0)
                            //{
                            //    if (Drones.DroneBay.Capacity - Drones.DroneBay.UsedCapacity >= 25)
                            //    {
                            //        Log.WriteLine("AbyssalSitePrerequisiteCheck: if (Drones.DroneBay.Capacity - Drones.DroneBay.UsedCapacity >= 25)");
                            //        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark);
                            //        return false;
                            //    }
                            //
                            Log.WriteLine("AbyssalSitePrerequisiteCheck: We assume we have enough drones!?!");
                            LastAbyssalSitePrerequisiteCheck = DateTime.UtcNow;
                            AbyssalSitePrerequisiteCheckRetries = 0;
                            return true;
                            //}

                            //AbyssalSitePrerequisiteCheckRetries++;
                            //return false;
                        }
                        Log.WriteLine("AbyssalSitePrerequisiteCheck: UseDrones is false");
                        AbyssalSitePrerequisiteCheckRetries = 0;
                        return true;
                        **/
                    }

                    Log.WriteLine("MiningPrerequisiteCheck: We have no ammo left in the cargo!");
                    ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                    MiningPrerequisiteCheckRetries++;
                    return false;
                }

                MiningPrerequisiteCheckRetries = 0;
                return true;
            }

            Log.WriteLine("MiningPrerequisiteCheck: We have no items in our cargo!");
            ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
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
                ChangeMiningBehaviorState(MiningBehaviorState.LocalWatch, true);
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
                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastAmmoBuyAttempt), DateTime.UtcNow);
                        ControllerManager.Instance.AddController(new BuyItemsController());
                        return false;
                    }

            return true;
        }

        public static void ClearPerPocketCache()
        {
            /// WeHaveBeenInPocketTooLong_WarningSent = false;
            return;
        }

        private static void ProcessAlerts()
        {
            return;
        }

        private static bool EveryPulse()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("MiningBehavior: CMBEveryPulse: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)");
                return false;
            }

            if (Settings.Instance.FinishWhenNotSafe && State.CurrentAbyssalDeadspaceBehaviorState != AbyssalDeadspaceBehaviorState.GotoHomeBookmark)
                if (ESCache.Instance.InSpace &&
                    !ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    Log.WriteLine("Going back to base");
                    ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark, true);
                }

            if (ESCache.Instance.InWormHoleSpace)
            {
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("MiningBehavior: CMBEveryPulse: if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)");
                return true;
            }

            Panic.ProcessState(MiningBehaviorHomeBookmarkName);

            if (State.CurrentPanicState == PanicState.Resume)
            {
                if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    State.CurrentPanicState = PanicState.Normal;
                    State.CurrentTravelerState = TravelerState.Idle;
                    ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                    return true;
                }

                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("MiningBehavior: CMBEveryPulse: if (State.CurrentPanicState == PanicState.Resume)");
                return false;
            }

            if (DoIWantToBeMiningRightNow())
            {
                ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
            }

            /**
            if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
            {
                if (State.CurrentMiningBehaviorState != MiningBehaviorState.Start &&
                    State.CurrentMiningBehaviorState != MiningBehaviorState.WarpOutStation &&
                    State.CurrentMiningBehaviorState != MiningBehaviorState.Arm &&
                    State.CurrentMiningBehaviorState != MiningBehaviorState.UnloadLoot &&
                    State.CurrentMiningBehaviorState != MiningBehaviorState.LocalWatch &&
                    State.CurrentMiningBehaviorState != MiningBehaviorState.Switch &&
                    State.CurrentMiningBehaviorState != MiningBehaviorState.WaitingforBadGuytoGoAway &&
                    State.CurrentMiningBehaviorState != MiningBehaviorState.Error)
                {
                    Log.WriteLine("InStation and State.CurrentMiningBehaviorState [" + State.CurrentMiningBehaviorState + "] Forcing change to Start");
                    ChangeMiningBehaviorState(MiningBehaviorState.Start);
                }
            }
            **/


            return true;
        }

        private static bool DoIWantToBeMiningRightNow()
        {
            if (Time.Instance.Started_DateTime.AddMinutes(1) > DateTime.UtcNow)
                return false;

            if (!ESCache.Instance.InSpace)
                return false;

            if (ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                return false;

            if (ESCache.Instance.Entities.Any(i => i.CategoryId == (int)CategoryID.Asteroid) && ESCache.Instance.Modules.Any(module => module.IsOreMiningModule) && ESCache.Instance.Entities.Any(asteroid => asteroid.GroupId == (int)Group.Veldspar && 10000 > asteroid.Distance))
                return true;

            return false;
        }

        private static bool MakeBookmarkForNextOreRoidState()
        {
            if (!ESCache.Instance.CachedBookmarks.Any(i => i.IsOnGridWithMe))
            {
                //Log.WriteLine("InWarp: if (!DirectEve.Bookmarks.Any(i => i.IsInCurrentSystem) || DirectEve.Bookmarks.Where(x => x.IsInCurrentSystem).All(i => i.DistanceInAU != null && i.DistanceInAU > 1))");
                if (ESCache.Instance.EntitiesOnGrid.Where(i => i.IsOreOrIce).Any())
                {
                    IEnumerable<EntityCache> ListOfOreOrIceSoretedByValue = ESCache.Instance.EntitiesOnGrid
                        .Where(i => (i.IsBistot && MineBistot) ||
                        //i.IsMercoxit && MineMercoxit ||
                        (i.IsArkonor && MineArkonor) ||
                        (i.IsSpodumain && MineSpodumain) ||
                        (i.IsCrokite && MineCrokite) ||
                        (i.IsDarkOchre && MineDarkOchre) ||
                        (i.IsGneiss && MineGneiss) ||
                        (i.IsHedbergite && MineHedbergite) ||
                        (i.IsHemorphite && MineHemorphite) ||
                        (i.IsJaspet && MineJaspet) ||
                        (i.IsKernite && MineKernite) ||
                        (i.IsOmber && MineOmber) ||
                        (i.IsPlagioclase && MinePlagioclase) ||
                        (i.IsPyroxeres && MinePyroxeres) ||
                        (i.IsScordite && MineScordite) ||
                        (i.IsVeldspar && MineVeldspar))
                        .OrderByDescending(x => x._directEntity.AveragePrice())
                        .ToList();

                    EntityCache nextOreRoid = null;
                    if (ListOfOreOrIceSoretedByValue.Any(x => x.Distance > (double)Distances.WarptoDistance))
                    {
                        nextOreRoid = ListOfOreOrIceSoretedByValue.FirstOrDefault(x => x.Distance > (double)Distances.WarptoDistance);
                    }
                    else
                    {
                        nextOreRoid = ListOfOreOrIceSoretedByValue.FirstOrDefault();
                    }

                    ESCache.Instance.DirectEve.BookmarkEntity(nextOreRoid._directEntity, MiningOreBookmarkPrefix + " -- #" + new Random().Next(100, 999).ToString(), null);
                    return true;
                }

                if (DateTime.UtcNow > Time.Instance.LastBookmarkAction.AddSeconds(10))
                {
                    Log.WriteLine("We have no bookmarks on this grid! Make a bookmark here!");
                    //make bookmark
                    ESCache.Instance.DirectEve.BookmarkCurrentLocation(MiningGasBookmarkPrefix + " -- #" + new Random().Next(100, 999).ToString(), null);
                    ESCache.Instance.ClearPerPocketCache(); //clears cache on CachedBookmarks so that it pulls new data which should include the new bookmark!
                    return true;
                }
            }

            ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
            return true;
        }

        private static void GotoHomeBookmarkState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "AbyssalDeadspaceBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: Traveler.TravelToBookmarkName(" + MiningBehaviorHomeBookmarkName + ");");

            Traveler.TravelToBookmarkName(MiningBehaviorHomeBookmarkName);

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("HomeBookmark is defined as [" + MiningBehaviorHomeBookmarkName + "] and should be a bookmark of a station or citadel we can dock at: why are we still in space?!");
                    return;
                }

                if (DebugConfig.DebugMiningBehavior) Log.WriteLine("if (State.CurrentTravelerState == TravelerState.AtDestination) ChangeMiningBehaviorState(MiningBehaviorState.Start, true);");
                Traveler.Destination = null;
                ChangeMiningBehaviorState(MiningBehaviorState.Start, true);
            }
        }

        private static void IdleCMBState()
        {
            State.CurrentArmState = ArmState.Idle;
            State.CurrentDroneControllerState = DroneControllerState.Idle;
            State.CurrentSalvageState = SalvageState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            Traveler.Destination = null;

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

            ChangeMiningBehaviorState(MiningBehaviorState.Start);
            return;
        }

        private static DateTime LastDictionaryUpdate = DateTime.UtcNow;

        public static void InvalidateCache()
        {
            try
            {
                if (!ESCache.Instance.InSpace)
                    return;

                //if (ESCache.Instance.ActiveShip != null && !ESCache.Instance.ActiveShip.IsMiningShip && !ESCache.Instance.ActiveShip.IsHaulingShip)
                //{
                //    if (DebugConfig.DebugMiningBehavior) Log.WriteLine("InvalidateCache: if (ESCache.Instance.ActiveShip != null && !ESCache.Instance.ActiveShip.IsMiningShip && !ESCache.Instance.ActiveShip.IsHaulingShip)");
                //    return;
                //}

                _minableAsteroids = null;
                _asteroidBeltsInLocal = null;
                _availableAsteroidBeltsInLocal = null;
                _nextAvailableAsteroidBeltInLocal = null;

                if (DateTime.UtcNow > LastDictionaryUpdate.AddMinutes(1))
                {
                    try
                    {
                        LastDictionaryUpdate = DateTime.UtcNow;
                        Scanner.AsteroidBeltsWithNoOre = Scanner.AsteroidBeltsWithNoOre.Where(i => DateTime.UtcNow > i.Value.AddHours(5)).ToDictionary(x => x.Key, x => x.Value);
                        Scanner.AsteroidBeltsWithPvP = Scanner.AsteroidBeltsWithPvP.Where(i => DateTime.UtcNow > i.Value.AddHours(2)).ToDictionary(x => x.Key, x => x.Value);
                        Scanner.AsteroidBeltsWithOtherMiners = Scanner.AsteroidBeltsWithOtherMiners.Where(i => DateTime.UtcNow > i.Value.AddMinutes(45)).ToDictionary(x => x.Key, x => x.Value);
                        Scanner.AsteroidBeltsWithNPCs = Scanner.AsteroidBeltsWithNPCs.Where(i => DateTime.UtcNow > i.Value.AddMinutes(25)).ToDictionary(x => x.Key, x => x.Value);
                        Scanner.SitesWeAreMiningIn = Scanner.SitesWeAreMiningIn.Where(i => DateTime.UtcNow > i.Value.AddMinutes(45)).ToDictionary(x => x.Key, x => x.Value);

                        Scanner.SitesWithNoOre = Scanner.SitesWithNoOre.Where(i => DateTime.UtcNow > i.Value.AddHours(5)).ToDictionary(x => x.Key, x => x.Value);
                        Scanner.SitesWithNPCs = Scanner.SitesWithNPCs.Where(i => DateTime.UtcNow > i.Value.AddMinutes(25)).ToDictionary(x => x.Key, x => x.Value);
                        Scanner.SitesWithOtherMiners = Scanner.SitesWithOtherMiners.Where(i => DateTime.UtcNow > i.Value.AddMinutes(45)).ToDictionary(x => x.Key, x => x.Value);
                        Scanner.SitesWithPvP = Scanner.SitesWithPvP.Where(i => DateTime.UtcNow > i.Value.AddHours(2)).ToDictionary(x => x.Key, x => x.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void LocalWatchCMBState()
        {
            if (Settings.Instance.UseLocalWatch && !ESCache.Instance.InWormHoleSpace)
            {
                Time.Instance.LastLocalWatchAction = DateTime.UtcNow;

                if (DebugConfig.DebugArm) Log.WriteLine("Starting: Is LocalSafe check...");
                if (!ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    if (DirectEve.Interval(30000)) Log.WriteLine("Bad standings pilots in local!");
                    return;
                }
            }

            if (ESCache.Instance.DirectEve.Me.PVPTimerExist && !ESCache.Instance.InWormHoleSpace)
            {
                Log.WriteLine("AbyssalSitePrerequisiteCheck: We have pvp timer: waiting");
                return;
            }

            if (ESCache.Instance.InStation)
            {
                TravelerDestination.Undock();
                return;
            }

            ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
            return;
        }

        private static bool ResetStatesToDefaults()
        {
            Log.WriteLine("AbyssalDeadspaceBehavior.ResetStatesToDefaults: start");
            State.CurrentAbyssalDeadspaceBehaviorState = AbyssalDeadspaceBehaviorState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Traveler.Destination = null;
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return true;
        }

        internal static bool AreWeDockedInHomeSystem()
        {
            try
            {
                var hbm = ESCache.Instance.DirectEve.Bookmarks.OrderByDescending(e => e.IsInCurrentSystem).FirstOrDefault(b => b.Title == MiningBehaviorHomeBookmarkName);
                if (hbm != null)
                {
                    return hbm.DockedAtBookmark();
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool? ShouldWeGoHome
        {
            get
            {
                if (!ESCache.Instance.InStation && !ESCache.Instance.InSpace)
                    return null;

                //if (ESCache.Instance.DirectEve.Me.IsInvuln)
                //    return null;

                if (ESCache.Instance.InStation)
                {
                    if (ESCache.Instance.InWormHoleSpace)
                    {
                        if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: InStation: InWormHoleSpace: so return [false]");
                        return false;
                    }

                    if (AreWeDockedInHomeSystem())
                    {
                        if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: InStation: AreWeDockedInHomeSystem: yes: We are already home so return [false]");
                        return false;
                    }

                    if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: InStation: we are not in the correct station [true]");
                    return true;
                }

                if (ESCache.Instance.InSpace)
                {
                    if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: InSpace");
                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GivenName != Settings.Instance.MiningShipName)
                    {
                        if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GivenName != Settings.Instance.MiningShipName) [true]");
                        return true;
                    }

                    if (ESCache.Instance.CurrentShipsGeneralMiningHold == null)
                    {
                        if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: if (ESCache.Instance.CurrentShipsOreHold == null)");
                        return null;
                    }

                    if (ESCache.Instance.CurrentShipsGeneralMiningHold.UsedCapacityPercentage > 80)
                    {
                        if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: if (ESCache.Instance.CurrentShipsOreHold.UsedCapacityPercentage > 80) [true]");
                        return true;
                    }

                    if (ESCache.Instance.InWormHoleSpace)
                    {
                        var ListOfHomeBookmarks = ESCache.Instance.BookmarksThatContain(MiningBehaviorHomeBookmarkName);
                        if (ListOfHomeBookmarks != null && ListOfHomeBookmarks.Any(i => i.IsInCurrentSystem))
                        {
                            if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: WH?");
                            return true;
                        }
                    }

                    //if (!ESCache.Instance.BookmarksThatContain("MineHere").Any(i => i.IsInCurrentSystem))
                    //{
                    //    if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: if (!ESCache.Instance.BookmarksThatContain(\"MineHere\").Any(i => i.IsInCurrentSystem)) [true]");
                    //    return true;
                    //}

                    if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: [false]");
                    return false;
                }

                if (DirectEve.Interval(4000)) Log.WriteLine("ShouldWeGoHome: [false]!");
                return false;
            }
        }

        private static void StartCMBState()
        {

            if (Time.Instance.IsItDuringDowntimeNow)
            {
                Log.WriteLine("MiningController: Start: Downtime is less than 15 minutes from now: Pausing");
                ControllerManager.Instance.SetPause(true);
                return;
            }

            if (ShouldWeGoHome == null)
                return;

            if ((bool)ShouldWeGoHome)
            {
                Log.WriteLine("MiningController: Start: ShouldWeGoHome returned true");
                ChangeMiningBehaviorState(MiningBehaviorState.GotoHomeBookmark);
                return;
            }

            if (ESCache.Instance.ActiveShip.GivenName == Settings.Instance.MiningShipName)
            {
                Log.WriteLine("MiningController: Start: if (ESCache.Instance.ActiveShip.GivenName == Settings.Instance.MiningShipName): FindAsteroidToMine");
                ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
                return;
            }
        }

        private static void SwitchCMBState()
        {
            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                Arm.ChangeArmState(ArmState.ActivateMiningShip, true, null);
            }

            if (DebugConfig.DebugArm) Log.WriteLine("CombatMissionBehavior.Switch is Entering Arm.Processstate");
            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Done");
                Arm.SwitchShipsOnly = false;
                Arm.ChangeArmState(ArmState.Idle, true, null);
                ChangeMiningBehaviorState(MiningBehaviorState.UnloadLoot);
            }
        }

        private static void TravelerCMBState()
        {
            try
            {
                if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                {
                    if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                List<long> destination = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();
                if (destination == null || destination.Count == 0)
                {
                    Log.WriteLine("No destination?");
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

                if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                    destination[0] = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.LastOrDefault())
                {
                    if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Count > 0)
                    {
                        IEnumerable<DirectBookmark> bookmarks = ESCache.Instance.CachedBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.Any())
                        {
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                            return;
                        }

                        Log.WriteLine("Destination: [" + ESCache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                        long lastSolarSystemInRoute = destination.LastOrDefault();

                        Log.WriteLine("Destination: [" + lastSolarSystemInRoute + "]");
                        Traveler.Destination = new SolarSystemDestination(destination.LastOrDefault());
                        return;
                    }

                    return;
                }

                Traveler.ProcessState();

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
                    {
                        Log.WriteLine("an error has occurred");
                        ChangeMiningBehaviorState(MiningBehaviorState.Error, true);
                        return;
                    }

                    if (ESCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeMiningBehaviorState(MiningBehaviorState.Error, true);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeMiningBehaviorState(MiningBehaviorState.Idle, true);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void UnloadLootCMBState()
        {
            try
            {
                if (!ESCache.Instance.InStation)
                {
                    ChangeMiningBehaviorState(MiningBehaviorState.Idle);
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

                    ChangeMiningBehaviorState(MiningBehaviorState.Arm, true);
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

            ChangeMiningBehaviorState(MiningBehaviorState.LocalWatch);
        }

        private static void WarpOutBookmarkCMBState()
        {
            if (!string.IsNullOrEmpty(Settings.Instance.UndockBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> warpOutBookmarks = ESCache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "");
                if (warpOutBookmarks != null && warpOutBookmarks.Any())
                {
                    DirectBookmark warpOutBookmark = warpOutBookmarks.FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.Distance < 10000000 && b.Distance > (int)Distances.WarptoDistance);

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Log.WriteLine("No Bookmark found with UndockBookmarkPrefix [" + Settings.Instance.UndockBookmarkPrefix + "] in the name");
                        State.CurrentTravelerState = TravelerState.Idle;
                        if (ESCache.Instance.InStation) TravelerDestination.Undock();
                        else ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
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
                            ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmark in System!");
                        State.CurrentTravelerState = TravelerState.Idle;
                        if (ESCache.Instance.InStation) TravelerDestination.Undock();
                    }

                    return;
                }
            }

            Log.WriteLine("No Bookmark in System.");
            ChangeMiningBehaviorState(MiningBehaviorState.FindAsteroidToMine);
            return;
        }

        #endregion Methods
    }
}