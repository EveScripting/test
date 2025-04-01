extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Utility;
using EVESharpCore.Framework.Lookup;
using System.Diagnostics;

namespace EVESharpCore.Cache
{
    extern alias SC;

    public partial class ESCache
    {
        #region Fields

        private List<DirectSolarSystem> _solarSystems;
        private List<EntityCache> _totalTargetsAndTargeting;
        private Dictionary<long, bool> DictionaryIsEntityIShouldLeaveAlone = new Dictionary<long, bool>();
        public Dictionary<long, bool> DictionaryIsHighValueTarget = new Dictionary<long, bool>();
        public Dictionary<long, bool> DictionaryIsLowValueTarget = new Dictionary<long, bool>();
        private List<EntityCache> _chargeEntities;
        private List<EntityCache> _entities;
        private List<EntityCache> _entitiesActivelyBeingLocked;
        private List<EntityCache> _entitiesNotSelf;
        private List<EntityCache> _entitiesOnGrid;
        private bool? _inSpace { get; set; } = false;
        private bool? _inStation { get; set; } = false;
        private List<EntityCache> _myAmmoInSpace;
        private EntityCache _myShipEntity;
        private List<EntityCache> _wrecks;
        private bool? _inWarp { get; set; }
        private bool? _previouslyInWarp { get; set; }
        private bool? _previouslyInMission { get; set; }

        #endregion Fields

        #region Properties

        public List<EntityCache> AbyssalBigObjects
        {
            get
            {
                return _abyssalBigObjects ?? (_abyssalBigObjects = Instance.EntitiesOnGrid.Where(e => e.IsLargeCollidable
                                                                                                      && e.Distance < (double)Distances.OnGridWithMe)
                           .OrderBy(t => t.Distance)
                           .ToList());
            }
        }

        public int intFilamentClouds
        {
            get
            {
                if (ESCache.Instance.AbyssalDeadspaceFilamentCloud.Any())
                {
                    return ESCache.Instance.AbyssalDeadspaceFilamentCloud.Count();
                }

                return 0;
            }
        }

        public int intBioluminesenceClouds
        {
            get
            {
                if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any())
                {
                    return ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Count();
                }

                return 0;
            }
        }

        public int intTachyonClouds
        {
            get
            {
                if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any())
                {
                    return ESCache.Instance.AbyssalDeadspaceTachyonClouds.Count();
                }

                return 0;
            }
        }

        //Bioluminescence Cloud (light blue): +300% Signature Radius (4.0x signature radius multiplier). Entering this cloud will make your ship an easier target to hit but it will also make all rats easier to hit. If fighting small but accurate enemies like Damaviks, this cloud can actually be helpful, and you can lure the rats into it.
        public List<EntityCache> ListAbyssalDeadspaceBioluminesenceClouds
        {
            get
            {
                return _abyssalDeadspaceBioluminescenceCloud ?? (_abyssalDeadspaceBioluminescenceCloud = Instance.EntitiesOnGrid.Where(e =>
                               e.IsAbyssalDeadspaceBioluminesenceCloud
                               && e.Distance < (double)Distances.OnGridWithMe
                               && e.TypeId != (int)TypeID.Beacon)
                           .OrderBy(t => t.Distance)
                           .ToList());
            }
        }

        //Tachyon Cloud (white): 300% Velocity!
        public List<EntityCache> AbyssalDeadspaceTachyonClouds
        {
            get
            {
                return _abyssalDeadspaceTachyonCausticCloud ?? (_abyssalDeadspaceTachyonCausticCloud = Instance.EntitiesOnGrid.Where(e =>
                               e.IsAbyssalDeadspaceTachyonCloud
                               && e.Distance < (double)Distances.OnGridWithMe
                               && e.TypeId != (int)TypeID.Beacon)
                           .OrderBy(t => t.Distance)
                           .ToList());
            }
        }

        public List<EntityCache> AbyssalDeadspaceDeviantAutomataSuppressor
        {
            get
            {
                return _abyssalDeadspaceDeviantAutomataSuppressor ?? (_abyssalDeadspaceDeviantAutomataSuppressor = Instance.EntitiesOnGrid.Where(e =>
                               e.IsAbyssalDeadspaceDeviantAutomataSuppressor
                               && e.Distance < (double)Distances.OnGridWithMe
                               && e.TypeId != (int)TypeID.Beacon)
                           .OrderBy(t => t.Distance)
                           .ToList());
            }
        }

        public EntityCache AbyssalDeadspaceSmallDeviantAutomataSuppressor
        {
            get
            {
                return _abyssalDeadspaceSmallDeviantAutomataSuppressor ?? (_abyssalDeadspaceSmallDeviantAutomataSuppressor = Instance.EntitiesOnGrid.Where(e =>
                               e.IsAbyssalDeadspaceSmallDeviantAutomataSuppressor
                               && e.Distance < (double)Distances.OnGridWithMe
                               && e.TypeId != (int)TypeID.Beacon)
                           .OrderBy(t => t.Distance)
                           .FirstOrDefault());
            }
        }

        public EntityCache AbyssalDeadspaceMediumDeviantAutomataSuppressor
        {
            get
            {
                return _abyssalDeadspaceMediumDeviantAutomataSuppressor ?? (_abyssalDeadspaceMediumDeviantAutomataSuppressor = Instance.EntitiesOnGrid.Where(e =>
                               e.IsAbyssalDeadspaceMediumDeviantAutomataSuppressor
                               && e.Distance < (double)Distances.OnGridWithMe
                               && e.TypeId != (int)TypeID.Beacon)
                           .OrderBy(t => t.Distance)
                           .FirstOrDefault());
            }
        }

        //Filament Cloud (orange): Penalty to Shield Booster boosting (-40%) and reduction to shield booster duration (-40%). If using a conventional (not Ancillary) shield booster, in effect this does not weaken your shield booster, but rather increases its capacitor cost per second by 66%. If you rely on a shield booster to survive, you should avoid entering these clouds.
        public List<EntityCache> AbyssalDeadspaceFilamentCloud
        {
            get
            {
                return _abyssalDeadspaceFilamentCloud ?? (_abyssalDeadspaceFilamentCloud = Instance.EntitiesOnGrid.Where(e =>
                               e.IsAbyssalDeadspaceFilamentCloud
                               && e.Distance < (double)Distances.OnGridWithMe
                               && e.TypeId != (int)TypeID.Beacon)
                           .OrderBy(t => t.Distance)
                           .ToList());
            }
        }

        public List<EntityCache> AbyssalDeadspaceMultibodyTrackingPylon
        {
            get
            {
                return _abyssalDeadspaceMultibodyTrackingPylon ?? (_abyssalDeadspaceMultibodyTrackingPylon = Instance.EntitiesOnGrid.Where(e =>
                               e.IsAbyssalDeadspaceMultibodyTrackingPylon
                               && e.Distance < (double)Distances.OnGridWithMe
                               && e.TypeId != (int)TypeID.Beacon)
                           .OrderBy(t => t.Distance)
                           .ToList());
            }
        }

        public List<EntityCache> AccelerationGates
        {
            get
            {
                if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                return _gates ?? (_gates = Instance.EntitiesOnGrid.Where(e =>
                               e.Distance < (double)Distances.OnGridWithMe &&
                               e.IsAccelerationGate &&
                               e.Name != "Triglavian Proving Conduit")
                           .OrderBy(t => t.Distance)
                           .ToList()) ?? new List<EntityCache>();
            }
        }

        private List<EntityCache> _asteroids;
        public List<EntityCache> Asteroids
        {
            get
            {
                if (!ESCache.Instance.InSpace) return new List<EntityCache>();
                if (_asteroids == null)
                {
                    _asteroids = Instance.EntitiesOnGrid.Where(e =>
                                                                e.IsAsteroid &&
                                                                e.Distance < (double)Distances.OnGridWithMe)
                           .OrderBy(t => t.Distance)
                           .ToList();

                    return _asteroids;
                }

                return _asteroids;
            }
        }

        public List<EntityCache> BigObjects
        {
            get
            {
                if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                return _bigObjectsAndGates ?? (_bigObjectsAndGates = Instance.EntitiesOnGrid.Where(e =>
                               (e.IsLargeCollidable ||
                                e.CategoryId == (int)CategoryID.Asteroid ||
                                e.GroupId == (int)Group.SpawnContainer ||
                                e.Name.ToLower() == "ENV_Triglavian_Construction_01a".ToLower())
                               && e.Distance < (double)Distances.OnGridWithMe
                               && e.GroupId != 1975
                               && e.TypeId != (int)TypeID.Beacon)
                           .OrderBy(t => t.Distance)
                           .ToList());
            }
        }

        public List<EntityCache> ChargeEntities
        {
            get
            {
                try
                {
                    if (_chargeEntities == null)
                    {
                        _chargeEntities =
                            Instance.DirectEve.Entities.Where(e => e.IsValid && !e.HasExploded && !e.HasReleased && e.CategoryId == (int)CategoryID.Charge)
                                .Select(i => new EntityCache(i))
                                .ToList();
                        return _chargeEntities;
                    }

                    return _chargeEntities;
                }
                catch (NullReferenceException)
                {
                }

                return new List<EntityCache>();
            }
        }

        //
        // these are not necessarily dockable! for dockable citadels use: freeportCitadels!
        //
        public List<EntityCache> Citadels => Entities.Where(e => e.IsCitadel).OrderBy(i => i.Distance).ToList();

        public List<EntityCache> Moons => Entities.Where(e => e.IsMoon).OrderBy(i => i.Distance).ToList();

        public List<EntityCache> AsteroidBelts => Entities.Where(e => e.IsAsteroidBelt).OrderBy(i => i.Distance).ToList();

        public EntityCache RandomCelestialWithoutaCitadel
        {
            get
            {
                List<EntityCache> _celestialInScanRangeWithoutaPOSOrCitadel = new List<EntityCache>();
                _celestialInScanRangeWithoutaPOSOrCitadel.AddRange(Moons.Where(e => !e.IsOnGridWithMe && !e.HasCitadelWithin10k));
                if (!_celestialInScanRangeWithoutaPOSOrCitadel.Any())
                {
                    _celestialInScanRangeWithoutaPOSOrCitadel.AddRange(Planets.Where(e => !e.IsOnGridWithMe && !e.HasCitadelWithin10k));
                    if (!_celestialInScanRangeWithoutaPOSOrCitadel.Any())
                    {
                        _celestialInScanRangeWithoutaPOSOrCitadel.AddRange(AsteroidBelts.Where(e => !e.IsOnGridWithMe && !e.HasCitadelWithin10k));
                    }
                }

                return _celestialInScanRangeWithoutaPOSOrCitadel.OrderBy(i => new Guid()).FirstOrDefault();
            }
        }


        public EntityCache _closestStargate = null;

        public EntityCache ClosestStargate
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return null;

                    if (Instance.InSpace)
                    {
                        if (_closestStargate != null)
                            return _closestStargate;

                        if (Instance.Entities != null && Instance.Entities.Count > 0)
                        {
                            if (Instance.Stargates.Count > 0)
                                return Instance.Stargates.OrderBy(s => s.Distance).FirstOrDefault() ?? null;

                            return null;
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public EntityCache ClosestWormhole
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return null;

                    if (Instance.InSpace)
                    {
                        if (Instance.EntitiesOnGrid != null && Instance.EntitiesOnGrid.Count > 0)
                        {
                            if (Instance.Wormholes != null && Instance.Wormholes.Count > 0)
                                return Instance.Wormholes.OrderBy(s => s.Distance).FirstOrDefault() ?? null;

                            return null;
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public EntityCache ClosestPlayer
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return null;

                    if (Instance.InSpace)
                    {
                        if (Instance.EntitiesOnGrid != null && Instance.EntitiesOnGrid.Count > 0)
                        {
                            return Instance.EntitiesNotSelf.OrderBy(s => s.Distance).FirstOrDefault(i => i.IsPlayer) ?? null;
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        private EntityCache _closestDockableLocation { get; set; } = null;

        public EntityCache ClosestDockableLocation
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return null;

                    if (_closestDockableLocation != null)
                        return _closestDockableLocation;

                    if (DockableLocations != null && DockableLocations.Count > 0)
                    {
                        _closestDockableLocation = DockableLocations.FirstOrDefault();
                        return _closestDockableLocation;
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public EntityCache ClosestStation
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return null;

                    if (Stations != null && Stations.Count > 0)
                        return Stations.OrderBy(s => s.Distance).FirstOrDefault() ?? Instance.Entities.OrderByDescending(s => s.Distance).FirstOrDefault();

                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public EntityCache ClosestCitadel
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return null;

                    if (Citadels != null && Citadels.Count > 0)
                        return Citadels.OrderBy(s => s.Distance).FirstOrDefault() ?? null;

                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public List<EntityCache> Containers
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                    return _containers ?? (_containers = Instance.EntitiesOnGrid.Where(e =>
                                   e.IsContainer &&
                                   (e.HaveLootRights || Salvage.AllowSalvagerToSteal) &&
                                   e.Name != "Abandoned Container")
                               .OrderBy(i => i.IsWreck).ThenBy(i => i.IsWreckEmpty) //Containers first, then wrecks
                               .ToList());
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return new List<EntityCache>();
                }
            }
        }

        public List<EntityCache> ContainersIgnoringLootRights
        {
            get
            {
                return _containers ?? (_containers = Instance.EntitiesOnGrid.Where(e =>
                               e.IsContainer &&
                               e.Name != "Abandoned Container")
                           .ToList());
            }
        }

        public List<EntityCache> Entities
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                    if (_entities == null)
                    {
                        _entities =
                            DirectEve.Entities.Where(e => e.IsValid && !e.HasExploded && !e.HasReleased && e.CategoryId != (int)CategoryID.Charge)
                                .Select(i => new EntityCache(i))
                                .ToList();
                        if (DebugConfig.DebugCheckSessionValid) Log.WriteLine("_entities [" + _entities.Count() + "]");
                        int intCount = 0;
                        foreach (EntityCache x in _entities.Where(i => i._thisEntityCacheCreated < DateTime.UtcNow).OrderBy(i => i._thisEntityCacheCreated))
                        {
                            intCount++;
                            if (intCount > 10) break;
                            if (DebugConfig.DebugCheckSessionValid) Log.WriteLine("[" + x.Name + "][" + x.Nearest1KDistance + "][" + x.Id + "][" + x._thisEntityCacheCreated + "]");
                        }

                        if (DebugConfig.DebugEntityCache)
                        {
                            foreach (EntityCache ent in _entities)
                            {
                                Log.WriteLine("ID [" + ent.Id + "] Name [" + ent.Name + "] Distance [" + ent.Nearest1KDistance + "] TypeID [" + ent.TypeId + "] GroupID [" + ent.GroupId + "]");
                            }
                        }

                        return _entities;
                    }

                    return _entities;
                }
                catch (NullReferenceException)
                {
                }

                return new List<EntityCache>();
            }
        }

        public List<EntityCache> EntitiesActivelyBeingLocked
        {
            get
            {
                if (!InSpace)
                    return new List<EntityCache>();

                if (Instance.EntitiesNotSelf.Count > 0)
                {
                    if (_entitiesActivelyBeingLocked == null)
                    {
                        _entitiesActivelyBeingLocked = Instance.EntitiesNotSelf.Where(i => i.IsTargeting).ToList();
                        if (_entitiesActivelyBeingLocked.Count > 0)
                            return _entitiesActivelyBeingLocked;

                        return new List<EntityCache>();
                    }

                    return _entitiesActivelyBeingLocked;
                }

                return new List<EntityCache>();
            }
        }

        public List<EntityCache> EntitiesNotSelf
        {
            get
            {
                if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                if (_entitiesNotSelf == null)
                {
                    _entitiesNotSelf =
                        Instance.EntitiesOnGrid.Where(i => i.Id != Instance.ActiveShip.ItemId).ToList();
                    if (_entitiesNotSelf.Count > 0)
                        return _entitiesNotSelf;

                    return new List<EntityCache>();
                }

                return _entitiesNotSelf;
            }
        }

        public List<EntityCache> EntitiesOnGrid
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                    if (_entitiesOnGrid == null)
                    {
                        _entitiesOnGrid = Instance.Entities.Where(e => e.Id != ESCache.Instance.ActiveShip.ItemId && e.IsOnGridWithMe).ToList();
                        return _entitiesOnGrid ?? new List<EntityCache>();
                    }

                    if (_entitiesOnGrid.Count > 0)
                        return _entitiesOnGrid ?? new List<EntityCache>();

                    return new List<EntityCache>();
                }
                catch (NullReferenceException)
                {
                    return new List<EntityCache>();
                }
            }
        }

        // the entity we are following (approach, orbit, keep at range)
        public EntityCache FollowingEntity => Instance.ActiveShip.Entity != null && Instance.ActiveShip.FollowingEntity != null ? new EntityCache(Instance.ActiveShip.FollowingEntity) : null;

        public bool InSpace
        {
            get
            {
                try
                {
                    if (_inSpace == null)
                    {
                        if (DateTime.UtcNow < Time.Instance.Started_DateTime.AddSeconds(3))
                        {
                            if (DebugConfig.DebugInSpace && DirectEve.Interval(3000)) Log.WriteLine("InSpace: if (DateTime.UtcNow < Time.Instance.Started_DateTime.AddSeconds(3)) return false");
                            return false;
                        }

                        _inSpace = false;

                        try
                        {
                            if (DirectEve.Session != null && DirectEve.Session.IsInSpace)
                            {
                                if (DebugConfig.DebugInSpace) Log.WriteLine("InSpace: DirectEve.Session.IsInSpace [" + DirectEve.Session.IsInSpace + "]");
                                if (DirectEve.Interval(7000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "InSpace"));

                                if (Instance.DirectEve.Session.CorporationId != null)
                                {
                                    if (Instance.EveAccount.MyCorpId != Instance.DirectEve.Session.CorporationId.ToString())
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MyCorpId), Instance.DirectEve.Session.CorporationId.ToString());

                                    if (Instance.EveAccount.MyCorp != Instance.DirectEve.Me.CorpName)
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MyCorp), Instance.DirectEve.Me.CorpName);
                                }

                                _inSpace = true;
                            }

                            if (Instance.EveAccount != null)
                            {
                                if (Instance.EveAccount.IsDocked && (bool)_inSpace)
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsDocked), false);

                                /**
                                if (Instance.EveAccount.IsLeader)
                                {
                                    if (Instance.EveAccount.LeaderInSpace != _inSpace)
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderInSpace), _inSpace);

                                    if (ESCache.Instance.Stargates.Count > 0)
                                    {
                                        string systemName = Instance.DirectEve.SolarSystems.FirstOrDefault(i => i.Key == Instance.DirectEve.Session.SolarSystemId).Value.Name;
                                        if (Instance.EveAccount.LeaderIsInSystemName != systemName)
                                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderIsInSystemName), systemName);
                                    }

                                    if (Instance.EveAccount.LeaderIsInSystemId != Instance.DirectEve.Session.SolarSystemId)
                                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderIsInSystemId), Instance.DirectEve.Session.SolarSystemId);
                                }
                                **/

                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                            return false;
                        }
                    }

                    return (bool)_inSpace;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public bool InStation
        {
            get
            {
                try
                {
                    if (_inStation != null) return (bool)_inStation;

                    if (DateTime.UtcNow < Time.Instance.Started_DateTime.AddSeconds(3))
                        return false;

                    _inStation = false;
                    long whereAmIDocked = 0;

                    if (DirectEve.Session.IsInDockableLocation)
                    {
                        if (DirectEve.Session.StationId.HasValue)
                            whereAmIDocked = (int)DirectEve.Session.StationId;
                        else if (DirectEve.Session.Structureid.HasValue)
                            whereAmIDocked = (long)DirectEve.Session.Structureid;

                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), false);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.InMission), false);
                        Traveler.MyTravelToBookmark = null;
                        //if (Instance.DirectEve.Session != null)
                        //{
                        //    if (Instance.EveAccount.MyCorpId != Instance.DirectEve.Session.CorporationId.ToString())
                        //        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MyCorpId), Instance.DirectEve.Session.CorporationId.ToString());
                        //
                        //    if (Instance.EveAccount.MyCorp != Instance.DirectEve.Me.CorpName)
                        //        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MyCorp), Instance.DirectEve.Me.CorpName);
                        //}

                        _inStation = true;
                        if (DirectEve.Interval(7000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "InStation"));
                    }

                    if (Instance.EveAccount.IsLeader && Instance.EveAccount.LeaderInStation != _inStation)
                    {
                        if (Instance.EveAccount.LeaderInStation) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderInStation), _inStation);
                        if (Instance.EveAccount.LeaderInStationId != 0) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderInStationId), whereAmIDocked);
                    }

                    if (_inStation != null)
                    {
                        if (!Instance.EveAccount.IsDocked && (bool)_inStation)
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsDocked), _inStation);

                        if (Instance.EveAccount.IsDocked && !(bool)_inStation)
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsDocked), _inStation);
                    }

                    if (_inStation == true && DateTime.UtcNow > ESCache.Instance.EveAccount.LastQuestorStarted.AddMinutes(5))
                    {
                        if (DirectEve.HasFrameChanged() && DirectEve.Interval(9000))
                        {
                            var ramAllocation = Process.GetCurrentProcess().WorkingSet64;
                            var allocationInMB = ramAllocation / (1024 * 1024);

                            if (allocationInMB > (1024 * 1.5))
                            {
                                if (DirectEve.Interval(15000))
                                {
                                    Log.WriteLine("InStation: allocationInMB [" + allocationInMB + "] > [" + 1024 * 1.5 + "]: FlushMem!");
                                    Util.FlushMem();
                                }

                                return _inStation ?? true;
                            }
                        }
                    }

                    return _inStation ?? false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

        public bool AllowCreateBookmarks
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                    return false;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                    return false;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                    return false;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    return false;

                if (ESCache.Instance.EveAccount.UseFleetMgr && ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Hawk)
                    return true;

                if (ESCache.Instance.Modules.Any(i => i.GroupId == (int)Group.ProbeLauncher))
                    return true;

                return false;
            }
        }

        public bool CheckForAndMakeBookmarksAsNeeded()
        {
            try
            {
                try
                {
                    if (!DirectEve.HasFrameChanged())
                        return true;

                    if (!AllowCreateBookmarks || ESCache.Instance.Paused)
                        return true;

                    if (ESCache.Instance.ActiveShip.GivenName != Combat.CombatShipName && ESCache.Instance.Modules.Any(i => i.GroupId != (int)Group.ProbeLauncher))
                        return true;

                    if (ESCache.Instance.DirectEve.Session.IsWspace)
                        return true;
                    //0.0
                    if (ESCache.Instance.DirectEve.Session.SolarSystem.IsZeroZeroSpace && ESCache.Instance.DirectEve.Session.IsKnownSpace)
                        return true;

                    if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)
                        return true;
                }
                catch (Exception ex)
                {
                    return true;
                }

                if (!DirectEve.Interval(3000))
                    return true;

                if (ESCache.Instance.Celestials.Any() && ESCache.Instance.ClosestCelestial != null && DirectEve.Session.IsKSpace)
                {
                    //Log.WriteLine("InWarp: Celestials [" + ESCache.Instance.Celestials.Count() + "]");
                    //Log.WriteLine("InWarp: ClosestCelestial [" + ClosestCelestial.Name + "][" + ESCache.Instance.ClosestCelestial.DistanceInAU + "] AU");
                    if (ESCache.Instance.ClosestCelestial.DistanceInAU > 5)
                    {
                        //Log.WriteLine("InWarp: ClosestCelestial [" + ClosestCelestial.Name + "][" + ESCache.Instance.ClosestCelestial.DistanceInAU + "] AU > 5AU");

                        if (ESCache.Instance.CachedBookmarks.Any(i => i.IsInCurrentSystem))
                        {
                            int intCount = 0;

                            try
                            {
                                //Log.WriteLine("List of all bookmarks we have in this solarsystem:");
                                foreach (var bookmark in ESCache.Instance.CachedBookmarks.Where(i => i.IsInCurrentSystem))
                                {
                                    try
                                    {
                                        intCount++;
                                        //Log.WriteLine("[" + intCount + "] BM [" + bookmark.Title + "][" + bookmark.ThisDirectBookmarkInstanceDate.ToShortTimeString() + "] IsInCurrentSystem [" + bookmark.IsInCurrentSystem + "] NearestCelestial [" + bookmark.ClosestCelestial.Name + "] NearestCelestialDistanceInAU [" + bookmark.ClosestCelestial.DistanceInAU + "][" + bookmark.DistanceInAU ?? "Null" + "AU]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.WriteLine("Exception [" + ex + "]");
                                    }

                                }

                                intCount = 0;
                                //Log.WriteLine("List of all bookmarks we have in this solarsystem: within 1AU (if any) [" + DirectEve.Bookmarks.Count(i => i.IsInCurrentSystem && i.DistanceInAU != null && 1 > i.DistanceInAU) + "]");
                                foreach (var bookmark in ESCache.Instance.CachedBookmarks.Where(i => i.IsInCurrentSystem && i.DistanceInAU != null && 1 > i.DistanceInAU))
                                {
                                    try
                                    {
                                        intCount++;
                                        //Log.WriteLine("[" + intCount + "] BM [" + bookmark.Title + "][" + bookmark.ThisDirectBookmarkInstanceDate.ToShortTimeString() + "] IsInCurrentSystem [" + bookmark.IsInCurrentSystem + "] NearestCelestial [" + bookmark.ClosestCelestial.Name + "] NearestCelestialDistanceInAU [" + bookmark.ClosestCelestial.DistanceInAU + "][" + bookmark.DistanceInAU ?? "Null" + "AU]");
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

                        //Check for other bookmarks within 5 AU, if none make one.
                        if (!ESCache.Instance.CachedBookmarks.Any(i => i.IsInCurrentSystem) || ESCache.Instance.CachedBookmarks.Where(x => x.IsInCurrentSystem && x.DistanceInAU != null).All(i => i.DistanceInAU > 5 && ESCache.Instance.ClosestCelestial.DistanceInAU > 25) || ((5 > ESCache.Instance.CachedBookmarks.Where(x => x.IsInCurrentSystem && x.Title.Contains("AU ] from [")).Count()) && ESCache.Instance.CachedBookmarks.Where(x => x.IsInCurrentSystem && x.DistanceInAU != null).All(i => i.DistanceInAU > 5)))
                        {
                            //Log.WriteLine("InWarp: if (!DirectEve.Bookmarks.Any(i => i.IsInCurrentSystem) || DirectEve.Bookmarks.Where(x => x.IsInCurrentSystem).All(i => i.DistanceInAU != null && i.DistanceInAU > 1))");
                            if (DateTime.UtcNow > Time.Instance.LastBookmarkAction.AddSeconds(10))
                            {
                                Log.WriteLine("InWarp: We have no bookmarks with 1AU of this spot and we are more than 4AU from any Celestial! Make a bookmark here!");
                                //make bookmark
                                ESCache.Instance.DirectEve.BookmarkCurrentLocation("Safespot: [" + ESCache.Instance.ClosestCelestial.DistanceInAU + " AU ] from [" + ESCache.Instance.ClosestCelestial.Name + "] -- #" + new Random().Next(100, 999).ToString(), null);
                                ESCache.Instance.ClearPerPocketCache(); //clears cache on CachedBookmarks so that it pulls new data which should include the new bookmark!
                            }
                            else
                            {
                                Log.WriteLine("InWarp: !if (DateTime.UtcNow > Time.Instance.LastBookmarkAction.AddSeconds(10)): We already made a bookmark.");
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public bool InWarp
        {
            get
            {
                try
                {
                    if (_inWarp != null) return _inWarp ?? false;

                    if (Instance.InSpace && !Instance.InStation)
                    {
                        if (Instance.ActiveShip != null)
                        {
                            if (Instance.ActiveShip.Entity != null)
                            {
                                if (ESCache.Instance.InAbyssalDeadspace)
                                {
                                    SetInWarpState(false);
                                    return false;
                                }

                                if (Instance.ActiveShip.Entity.Mode == 3)
                                {
                                    int atThisVelocityWeConsiderOurselvesInWarp = 10000;
                                    atThisVelocityWeConsiderOurselvesInWarp = (int)Instance.ActiveShip.MaxVelocity + 1;

                                    if (Instance.ActiveShip.Entity.Velocity > atThisVelocityWeConsiderOurselvesInWarp)
                                    {
                                        if (Instance.Weapons.Count > 0)
                                        {
                                            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController) || ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                                            {
                                                //Combat.ReloadAllWeaponsWithSameAmmoUsingCmdReloadAmmo();
                                            }
                                            else if (DirectEve.Interval(10000)) Combat.ReloadAll();
                                        }

                                        if (Combat.PrimaryWeaponPriorityEntities.Count > 0)
                                            Combat.RemovePrimaryWeaponPriorityTargets(Combat.PrimaryWeaponPriorityEntities.ToList());

                                        if (Drones.UseDrones && Drones.DronePriorityEntities.Count > 0)
                                            Drones.RemoveDronePriorityTargets(Drones.DronePriorityEntities.ToList());

                                        if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastInWarp.AddSeconds(1))
                                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInWarp), DateTime.UtcNow);
                                        Time.Instance.LastInWarp = DateTime.UtcNow;
                                        SetInWarpState(true);
                                        CheckForAndMakeBookmarksAsNeeded();
                                        if (Instance.EveAccount.IsLeader && !Instance.EveAccount.LeaderInWarp) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderInWarp), _inWarp);
                                        if (DirectEve.Interval(7000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "InWarp"));
                                        return _inWarp ?? false;
                                    }

                                    if (Time.Instance.LastInWarp.AddSeconds(5) > DateTime.UtcNow)
                                        return true;
                                }

                                SetInWarpState(false);
                                if (Instance.EveAccount.IsLeader && !Instance.EveAccount.LeaderInWarp) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderInWarp), _inWarp);
                                return _inWarp ?? false;
                            }

                            SetInWarpState(false);
                            if (Instance.EveAccount.IsLeader && !Instance.EveAccount.LeaderInWarp) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderInWarp), _inWarp);
                            return _inWarp ?? false;
                        }

                        SetInWarpState(false);
                        if (Instance.EveAccount.IsLeader && !Instance.EveAccount.LeaderInWarp) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderInWarp), _inWarp);
                        return _inWarp ?? false;
                    }

                    SetInWarpState(false);
                    if (Instance.EveAccount.IsLeader && !Instance.EveAccount.LeaderInWarp) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderInWarp), _inWarp);
                    return _inWarp ?? false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("InWarp check failed, exception [" + exception + "]");
                }

                return false;
            }
        }

        public int? MaxLockedTargets
        {
            get
            {
                try
                {
                    if (_maxLockedTargets == null)
                    {
                        if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                        {
                            return 2;
                        }

                        _maxLockedTargets = Math.Min(Instance.DirectEve.Me.MaxLockedTargets, Instance.ActiveShip.MaxLockedTargetsWithShipAndSkills);
                        return (int)_maxLockedTargets;
                    }

                    return (int)_maxLockedTargets;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }


        public List<EntityCache> _hostileMissilesInSpace;

        public List<EntityCache> HostileMissilesInSpace
        {
            get
            {
                if (_hostileMissilesInSpace == null)
                {

                    _hostileMissilesInSpace =
                        Instance.DirectEve.Entities.Where(e => e.IsValid && !e.HasExploded && !e.HasReleased && e.IsMissile && !e.IsOwnedByMe) // !e.IsOwnedByMyCorp)
                        .Select(i => new EntityCache(i))
                        .ToList();

                    if (_hostileMissilesInSpace.Count > 0)
                        return _hostileMissilesInSpace;

                    return new List<EntityCache>();
                }

                return _hostileMissilesInSpace;
            }
        }

        public List<EntityCache> MyAmmoInSpace
        {
            get
            {
                if (_myAmmoInSpace == null)
                {
                    if (MyCurrentAmmoInWeapon != null)
                    {
                        _myAmmoInSpace =
                            Instance.Entities.Where(e => e.Distance > 3000 && e.IsOnGridWithMe && e.TypeId == MyCurrentAmmoInWeapon.TypeId && e.Velocity > 50)
                                .ToList();
                        if (_myAmmoInSpace.Count > 0)
                            return _myAmmoInSpace;

                        return new List<EntityCache>();
                    }

                    return new List<EntityCache>();
                }

                return _myAmmoInSpace;
            }
        }

        public EntityCache MyShipEntity
        {
            get
            {
                try
                {
                    if (_myShipEntity == null)
                    {
                        if (!ESCache.Instance.InSpace)
                        {
                            Log.WriteLine("if (ESCache.Instance.ActiveShip == null)");
                            return null;
                        }

                        if (ESCache.Instance.ActiveShip == null)
                        {
                            Log.WriteLine("if (ESCache..Instance.ActiveShip == null)");
                            return null;
                        }

                        _myShipEntity = ESCache.Instance.Entities.Find(e => e.Id == ESCache.Instance.ActiveShip.Entity.Id);
                        return _myShipEntity;
                    }

                    return _myShipEntity;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        private List<EntityCache> _freeportCitadels = null;

        public List<EntityCache> FreeportCitadels
        {
            get
            {
                try
                {
                    if (_freeportCitadels != null)
                        return _freeportCitadels ?? new List<EntityCache>();

                    _freeportCitadels = Citadels.Where(e => e._directEntity.IsDockable).OrderBy(i => i.Distance).ToList();
                    return _freeportCitadels ?? new List<EntityCache>();
                }
                catch (Exception)
                {
                    return new List<EntityCache>();
                }
            }
        }

        public List<DirectSolarSystem> SolarSystems
        {
            get
            {
                try
                {
                    if (Instance.InSpace || Instance.InStation)
                    {
                        if (_solarSystems == null || _solarSystems.Count == 0 || _solarSystems.Count < 5400)
                        {
                            if (Instance.DirectEve.SolarSystems.Count > 0)
                            {
                                if (Instance.DirectEve.SolarSystems.Values.Count > 0)
                                    _solarSystems = Instance.DirectEve.SolarSystems.Values.OrderBy(s => s.Name).ToList();

                                return new List<DirectSolarSystem>();
                            }

                            return new List<DirectSolarSystem>();
                        }

                        return _solarSystems;
                    }

                    return new List<DirectSolarSystem>();
                }
                catch (NullReferenceException)
                {
                    return new List<DirectSolarSystem>();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return new List<DirectSolarSystem>();
                }
            }
        }

        public EntityCache Star
        {
            get { return _star ?? (_star = Entities.Find(e => e.CategoryId == (int)CategoryID.Celestial && e.GroupId == (int)Group.Star)); }
        }

        public List<EntityCache> Stargates
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                    if (_stargates == null)
                    {
                        if (Instance.Entities != null && Instance.Entities.Count > 0)
                        {
                            _stargates = Instance.Entities.Where(e => e.GroupId == (int)Group.Stargate).ToList();
                            return _stargates ?? new List<EntityCache>();
                        }

                        return new List<EntityCache>();
                    }

                    return _stargates;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return new List<EntityCache>();
                }
            }
        }

        private List<EntityCache> _dockableLocations = null;

        public List<EntityCache> DockableLocations
        {
            get
            {
                if (_dockableLocations != null)
                    return _dockableLocations ?? new List<EntityCache>();

                _dockableLocations = Entities.Where(e => e.IsStation || (e.IsCitadel && e._directEntity.IsDockable)).OrderBy(i => i.Distance).ToList() ?? new List<EntityCache>();
                return _dockableLocations ?? new List<EntityCache>();
            }
        }

        private List<EntityCache> _stations = new List<EntityCache>();

        public List<EntityCache> Stations
        {
            get
            {
                if (_stations != null)
                    return _stations ?? new List<EntityCache>();

                _stations = Entities.Where(e => e.IsStation).OrderBy(i => i.Distance).ToList() ?? new List<EntityCache>();
                return _stations;
            }
        }

        private List<EntityCache> _wormholes;

        public List<EntityCache> Wormholes
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                    if (_wormholes == null)
                    {
                        if (Instance.EntitiesOnGrid != null && Instance.EntitiesOnGrid.Count > 0)
                        {
                            _wormholes = Instance.EntitiesOnGrid.Where(e => e.GroupId == (int) Group.Wormhole).ToList();
                            return _wormholes ?? new List<EntityCache>();
                        }

                        return new List<EntityCache>();
                    }

                    return _wormholes ?? new List<EntityCache>();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return new List<EntityCache>();
                }
            }
        }

        public List<EntityCache> Targeting
        {
            get
            {
                if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                if (_targeting == null)
                    _targeting = Instance.EntitiesOnGrid.Where(e => e.IsValid && e.IsTargeting || Instance.TargetingIDs.ContainsKey(e.Id)).ToList();

                if (_targeting.Count > 0)
                    return _targeting;

                return new List<EntityCache>();
            }
        }

        public int TargetingSlotsNotBeingUsedBySalvager
        {
            get
            {
                if (Salvage.MaximumWreckTargets > 0 && Instance.MaxLockedTargets >= 5)
                    return Instance.MaxLockedTargets ?? 2 - Salvage.MaximumWreckTargets;

                return Instance.MaxLockedTargets ?? 2;
            }
        }

        public List<EntityCache> Targets
        {
            //todo: add a dictionary of entityIDs and last target datetime stamp so that we can use that list elsewhere to delay targeting
            get
            {
                if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                if (_targets == null)
                {
                    _targets = EntitiesOnGrid.Where(e => e.IsValid && e.IsTarget).ToList();
                    return _targets ?? new List<EntityCache>();
                }

                return _targets ?? new List<EntityCache>();
            }
        }

        public List<EntityCache> TotalTargetsAndTargeting
        {
            get
            {
                if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                if (_totalTargetsAndTargeting == null)
                {
                    _totalTargetsAndTargeting = Targets.Concat(Targeting.Where(i => !i.IsTarget)).ToList();
                    return _totalTargetsAndTargeting;
                }

                return _totalTargetsAndTargeting;
            }
        }

        public List<EntityCache> UnlootedContainers
        {
            get
            {
                if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                return _unlootedContainers ?? (_unlootedContainers = Instance.EntitiesOnGrid.Where(e =>
                               e.IsContainer &&
                               (e.HaveLootRights || Salvage.AllowSalvagerToSteal || e.IsWreckEmpty) &&
                               !LootedContainers.Contains(e.Id))
                           .OrderBy(
                               e => e.Distance)
                           .ToList());
            }
        }

        public List<EntityCache> UnlootedWrecksAndSecureCans
        {
            get
            {
                if (!ESCache.Instance.InSpace) return new List<EntityCache>();

                return _unlootedWrecksAndSecureCans ?? (_unlootedWrecksAndSecureCans = Instance.EntitiesOnGrid.Where(e =>
                               (e.GroupId == (int)Group.Wreck ||
                               e.GroupId == (int)Group.SecureContainer ||
                               e.GroupId == (int)Group.AuditLogSecureContainer ||
                               e.GroupId == (int)Group.FreightContainer))
                           .OrderBy(e => e.Distance)
                           .ToList());
            }
        }

        public List<EntityCache> Wrecks
        {
            get { return _wrecks ?? (_wrecks = Instance.EntitiesOnGrid.Where(e => e.GroupId == (int)Group.Wreck).ToList() ?? new List<EntityCache>());}
        }

        public DirectWorldPosition LastInWarpPositionWas = null;

        private void SetInWarpState(bool state)
        {
            if (_previouslyInWarp == state)
            {
                _inWarp = state;
                return;
            }

            _inWarp = state;
            _previouslyInWarp = state;
            if ((bool) _inWarp)
            {
                Log.WriteLine("InWarp is now [" + _inWarp + "]");
            }
            else
            {
                ESCache.Instance.ClearPerPocketCache();
                Log.WriteLine("Exiting Warp: InWarp is now [" + _inWarp + "]");
            }

            if (ESCache.Instance.InSpace)
            {
                if (ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition != null)
                {
                    LastInWarpPositionWas = ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition;
                    //LastInWarpPositionWas = ESCache.Instance.ClosestPlanet._directEntity.DirectWorldPosition;

                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("LastInWarpPositionWas: Set to: X [" + LastInWarpPositionWas.XCoordinate + "] Y [" + LastInWarpPositionWas.YCoordinate + "] Z [" + LastInWarpPositionWas.ZCoordinate + "]");
                    //if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("LastInWarpPositionWas: Set to: ClosestPlanet [" + ESCache.Instance.ClosestPlanet.Name + "]");
                }
            }
        }

        private void SetInMissionState(bool state)
        {
            if (_previouslyInMission == state)
            {
                _inMission = state;
                return;
            }

            _inMission = state;
            _previouslyInMission = state;
            if (ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.AbyssalController))
            {
                Log.WriteLine("InMission is now [" + _inMission + "]");
            }
        }

        #endregion Properties

        #region Methods

        public double DistanceFromMe(double x, double y, double z)
        {
            try
            {
                if (Instance.ActiveShip.Entity == null)
                    return -1;

                if (Instance.ActiveShip.Entity.DirectAbsolutePosition == null)
                    return -1;

                if (x == 0 || y == 0 || z == 0)
                    return -1;

                double curX = Instance.ActiveShip.Entity.DirectAbsolutePosition.XCoordinate;
                double curY = Instance.ActiveShip.Entity.DirectAbsolutePosition.YCoordinate;
                double curZ = Instance.ActiveShip.Entity.DirectAbsolutePosition.ZCoordinate;

                return Math.Round(Math.Sqrt(((curX - x) * (curX - x)) + ((curY - y) * (curY - y)) + ((curZ - z) * (curZ - z))), 2);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        public double DistanceFromMe(Vec3 thisPositionInSpace)
        {
            try
            {
                if (Instance.ActiveShip.Entity == null)
                    return -1;

                if (Instance.ActiveShip.Entity.DirectAbsolutePosition == null)
                    return -1;

                double x = thisPositionInSpace.X;
                double y = thisPositionInSpace.Y;
                double z = thisPositionInSpace.Z;

                if (x == 0 || y == 0 || z == 0)
                    return -1;

                double curX = Instance.ActiveShip.Entity.DirectAbsolutePosition.XCoordinate;
                double curY = Instance.ActiveShip.Entity.DirectAbsolutePosition.YCoordinate;
                double curZ = Instance.ActiveShip.Entity.DirectAbsolutePosition.ZCoordinate;

                return Math.Round(Math.Sqrt(((curX - x) * (curX - x)) + ((curY - y) * (curY - y)) + ((curZ - z) * (curZ - z))), 2);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return 0;
            }
        }

        public List<EntityCache> EntitiesByPartialName(string nameToSearchFor)
        {
            try
            {
                if (Instance.Entities != null && Instance.Entities.Count > 0)
                {
                    List<EntityCache> _entitiesByPartialName = Instance.Entities.Where(e => e.Name.ToLower().Contains(nameToSearchFor.ToLower())).ToList();
                    if (_entitiesByPartialName.Count == 0)
                        _entitiesByPartialName = Instance.Entities.Where(e => e.Name.ToLower() == nameToSearchFor.ToLower()).ToList();

                    if (_entitiesByPartialName.Count == 0)
                        _entitiesByPartialName = null;

                    return _entitiesByPartialName;
                }

                return new List<EntityCache>();
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return new List<EntityCache>();
            }
        }

        public List<EntityCache> EntitiesThatContainTheName(string label)
        {
            try
            {
                return Instance.Entities.Where(e => !string.IsNullOrEmpty(e.Name) && e.Name.ToLower().Contains(label.ToLower())).ToList();
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return new List<EntityCache>();
            }
        }

        public EntityCache EntityById(long id)
        {
            try
            {
                if (_entitiesById.ContainsKey(id))
                    return _entitiesById[id];

                EntityCache entity = Instance.EntitiesOnGrid.Find(e => e.Id == id);
                _entitiesById[id] = entity;
                return entity;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return null;
            }
        }

        public EntityCache EntityByName(string name)
        {
            return Instance.Entities.Find(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public bool GateInGrid()
        {
            try
            {
                if (Instance.AccelerationGates.Count == 0)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public Func<EntityCache, int> OrderByLowestHealth()
        {
            try
            {
                return t => (int)(t.ShieldPct + t.ArmorPct + t.StructurePct);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return null;
            }
        }

        public EntityCache StargateByName(string locationName)
        {
            return _stargate ??
                    (_stargate =
                        Instance.Stargates.Find(i => i.Name.ToLower() == locationName.ToLower()));
        }

        private void RemoveTargetsFromBountyListBeforeWarping()
        {
            try
            {
                if (Instance.EntitiesOnGrid != null && Instance.EntitiesOnGrid.Count > 0 && Statistics.BountyValues != null && Statistics.BountyValues.Count > 0)
                    foreach (EntityCache e in Instance.EntitiesOnGrid.Where(e => Statistics.BountyValues.TryGetValue(e.Id, out var val) && val > 0))
                    {
                        foreach (KeyValuePair<long, double> BountyValue in Statistics.BountyValues)
                        {
                            if (e.Id == BountyValue.Key)
                                Statistics.BountyValues.Remove(e.Id);
                        }
                    }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public List<long> ListEntityIDs_IsBadIdeaTrue = new List<long>();
        public List<long> ListEntityIDs_IsBadIdeaFalse = new List<long>();
        public List<long> ListEntityIDs_IsPotentialCombatTarget = new List<long>();

        public Dictionary<Tuple<long, string>, object> DictionaryCachedPerFrame = new Dictionary<Tuple<long, string>, object>();
        public Dictionary<Tuple<long, string>, object> DictionaryCachedPerPocket = new Dictionary<Tuple<long, string>, object>();
        public Dictionary<long, DateTime> DictionaryCachedPerPocketLastAttackedDrones = new Dictionary<long, DateTime>();

        #endregion Methods
    }
}