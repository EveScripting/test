extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Questor.Combat
{
    public static class Drones
    {
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

        public enum DroneState
        {
            Idle = 0,
            Attacking = 1,
            Mining = 2,
            Approaching = 3,
            Returning = 4, //Return to bay
            Returning2 = 5, //Return and orbit
            Operating = 6,
            Following = 7,
            Fleeing = 8,
            Repairing = 9,
            Salvaging = 10,
            Unknown = 99,
        }

        #region Constructors

        static Drones()
        {
        }

        #endregion Constructors

        #region Fields

        public static int DefaultDroneTypeID;
        public static bool IsRecoverLostDronesAlreadyProcessedInThisPocket;
        public static int MinimumNumberOfDronesBeforeWeGoBuyMore = 100;
        private static List<EntityCache> _activeDrones;
        private static double _activeDronesArmorPercentageOnLastPulse;
        private static double _activeDronesArmorTotalOnLastPulse;
        private static double _activeDronesShieldPercentageOnLastPulse;
        private static double _activeDronesShieldTotalOnLastPulse;
        private static double _activeDronesStructurePercentageOnLastPulse;
        private static double _activeDronesStructureTotalOnLastPulse;
        private static DirectContainer _droneBay;
        private static List<EntityCache> _dronePriorityEntities;
        private static bool _dronesKillHighValueTargets;
        private static int _droneTypeID;
        private static int _lastDroneCount { get; set; }
        private static DateTime _lastLaunch;
        private static DateTime _lastRecall;
        private static DateTime _lastRecallCommand;

        private static DateTime _launchTimeout;
        private static int _launchTries;
        private static double? _maxDroneRange;
        private static DateTime _nextDroneAction { get; set; } = DateTime.UtcNow;
        private static DateTime _nextWarpScrambledWarning = DateTime.MinValue;
        private static EntityCache _preferredDroneTarget;
        public static EntityCache _cachedDroneTarget { get; set; }
        private static int _recallCount;
        private static DateTime LastDroneFightCmd = DateTime.MinValue;
        public static DateTime LastDroneAssistCmd = DateTime.MinValue;


        #endregion Fields

        #region Properties

        private static int? _activeDroneCount = null;

        public static int ActiveDroneCount
        {
            get
            {
                if (_activeDroneCount != null)
                    return _activeDroneCount ?? 0;

                _activeDroneCount = ActiveDrones.Count;
                return _activeDroneCount ?? 0;
            }
        }

        public static int AllDronesInSpaceCount
        {
            get
            {
                if (AllDronesInSpace.Any())
                {
                    return AllDronesInSpace.Count();
                }

                return 0;
            }
        }

        private static IEnumerable<EntityCache> _allDronesInSpace = new List<EntityCache>();

        public static IEnumerable<EntityCache> AllDronesInSpace
        {
            get
            {
                if (ESCache.Instance.Entities.Count > 0)
                {
                    if (_allDronesInSpace == null)
                    {
                        if (ESCache.Instance.Entities.Any(i => i.CategoryId == (int)CategoryID.Drone && i.TypeId == DroneTypeID))
                        {
                            _allDronesInSpace = ESCache.Instance.EntitiesOnGrid.Where(i => i.CategoryId == (int)CategoryID.Drone && i.TypeId == DroneTypeID && i._directEntity.IsOwnedByMe);
                            if (_allDronesInSpace != null && _allDronesInSpace.Any())
                            {
                                return _allDronesInSpace;
                            }

                            return new List<EntityCache>();
                        }

                        return new List<EntityCache>();
                    }

                    return _allDronesInSpace ?? new List<EntityCache>();
                }

                return new List<EntityCache>();
            }
        }

        public static List<long> ActiveDroneIds
        {
            get
            {
                if (ActiveDrones.Any())
                {
                    List<long> _activeDroneIDs = new List<long>();
                    foreach (EntityCache ActiveDrone in ActiveDrones)
                    {
                        _activeDroneIDs.Add(ActiveDrone.Id);
                    }

                    return _activeDroneIDs;
                }

                return new List<long>();
            }
        }

        public static List<EntityCache> ActiveDrones
        {
            get
            {
                try
                {
                    if (_activeDrones == null)
                    {
                        if (ESCache.Instance.DirectEve.ActiveDrones.Count > 0)
                        {
                            _activeDrones = ESCache.Instance.DirectEve.ActiveDrones.Select(d => new EntityCache(d)).ToList();
                            if (_activeDrones != null)
                                return _activeDrones;

                            return new List<EntityCache>();
                        }

                        return new List<EntityCache>();
                    }

                    return _activeDrones ?? new List<EntityCache>();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<EntityCache>();
                }
            }
        }

        public static bool AddDampenersToDronePriorityTargetList { get; set; }
        public static bool AddECMsToDroneTargetList { get; set; }
        public static bool AddNeutralizersToDronePriorityTargetList { get; set; }
        public static bool AddTargetPaintersToDronePriorityTargetList { get; set; }
        public static bool AddTrackingDisruptorsToDronePriorityTargetList { get; set; }
        public static bool AddWarpScramblersToDronePriorityTargetList { get; set; }
        public static bool AddWebifiersToDronePriorityTargetList { get; set; }
        public static int BelowThisHealthLevelRemoveFromDroneBay { get; set; }
        public static int BuyAmmoDroneAmmount { get; set; }
        public static bool DefaultUseDrones { get; set; }

        public static DirectContainer DroneBay
        {
            get
            {
                try
                {
                    if (_droneBay != null) return _droneBay;

                    if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
                        if (ESCache.Instance.Windows.Count > 0)
                        {
                            _droneBay = ESCache.Instance.DirectEve.GetShipsDroneBay();
                            return _droneBay;
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

        private static bool areDronesIdle
        {
            get
            {
                try
                {
                    if (ActiveDrones.Any(i => i._directEntity.DroneState == (int)DroneState.Idle))
                        return true;

                    if (ActiveDrones.Any(e => e.FollowId == 0))
                        return true;

                    if (currentDroneTarget == null)
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


        private static bool areDronesInFight => ActiveDrones.Any(i => i._directEntity.DroneState == (int)DroneState.Attacking);

        private static bool areDronesReturning => ActiveDrones.Any(i => i._directEntity.DroneState == (int)DroneState.Returning || i._directEntity.DroneState == (int)DroneState.Returning2);

        private static long? currentDroneTarget => ActiveDrones?.FirstOrDefault()?.FollowId;


        public static float DroneControlRange
        {
            get
            {
                if (!ESCache.Instance.ActiveShip.HasDroneBay)
                    return 0;

                return ESCache.Instance.ActiveShip.GetDroneControlRange();
            }
        }

        public static bool DroneIsTooFarFromOldTarget
        {
            get
            {
                try
                {
                    if (EntityDronesAreShooting == null)
                        return true;

                    if (Drones.ActiveDrones == null || !Drones.ActiveDrones.Any())
                        return true;

                    Double DistanceFromDroneToCachedTarget = Drones.ActiveDrones.FirstOrDefault()._directEntity.DirectAbsolutePosition.GetDistance(EntityDronesAreShooting._directEntity.DirectAbsolutePosition);

                    if (DistanceFromDroneToCachedTarget == -1)
                        return false;

                    if (DistanceFromDroneToCachedTarget > 9000)
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
        public static int DroneMinimumArmorPct { get; set; }
        public static int DroneMinimumCapacitorPct { get; set; }
        public static int DroneMinimumShieldPct { get; set; }

        public static List<EntityCache> DronePriorityEntities
        {
            get
            {
                try
                {
                    if (_dronePriorityEntities == null)
                    {
                        if (DronePriorityTargets != null && DronePriorityTargets.Count > 0)
                        {
                            _dronePriorityEntities =
                                DronePriorityTargets.OrderByDescending(pt => pt.DronePriority).ThenBy(pt => pt.Entity.Nearest5kDistance).Select(pt => pt.Entity).ToList();
                            return _dronePriorityEntities ?? new List<EntityCache>();
                        }

                        return _dronePriorityEntities ?? new List<EntityCache>();
                    }

                    return _dronePriorityEntities ?? new List<EntityCache>();
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

        public static List<PriorityTarget> DronePriorityTargets
        {
            get
            {
                try
                {
                    if (_dronePriorityTargets != null && _dronePriorityTargets.Count > 0)
                    {
                        foreach (PriorityTarget dronePriorityTarget in _dronePriorityTargets)
                            if (ESCache.Instance.EntitiesOnGrid.All(i => i.Id != dronePriorityTarget.EntityID))
                            {
                                _dronePriorityTargets.Remove(dronePriorityTarget);
                                break;
                            }

                        return _dronePriorityTargets;
                    }

                    _dronePriorityTargets = new List<PriorityTarget>();
                    return _dronePriorityTargets;
                }
                catch (NullReferenceException)
                {
                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public static int DroneRecallArmorPct { get; set; }
        public static int DroneRecallCapacitorPct { get; set; }
        public static int DroneRecallShieldPct { get; set; }
        public static bool DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive { get; set; }

        public static bool DronesKillHighValueTargets
        {
            get
            {
                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GroupId == (int) Group.Dreadnaught)
                    return false;

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                    return true;

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                    return true;

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                    return true;

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Worm)
                    return true;

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Rattlesnake)
                    return true;

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsShipWithNoDroneBay)
                    return false;

                if (MissionSettings.MissionDronesKillHighValueTargets != null)
                    return (bool) MissionSettings.MissionDronesKillHighValueTargets;

                return _dronesKillHighValueTargets;
            }
            set => _dronesKillHighValueTargets = value;
        }

        public static int DroneTypeID
        {
            get
            {
                if (MissionSettings.MissionDroneTypeID != null && MissionSettings.MissionDroneTypeID != 0)
                {
                    _droneTypeID = (int) MissionSettings.MissionDroneTypeID;
                    return _droneTypeID;
                }

                if (MissionSettings.PocketDroneTypeID != null && MissionSettings.PocketDroneTypeID != 0)
                {
                    _droneTypeID = (int) MissionSettings.PocketDroneTypeID;
                    return _droneTypeID;
                }

                if (MissionSettings.FactionDroneTypeID != null && MissionSettings.FactionDroneTypeID != 0)
                {
                    _droneTypeID = (int) MissionSettings.FactionDroneTypeID;
                    return _droneTypeID;
                }

                _droneTypeID = DefaultDroneTypeID;
                return _droneTypeID;
            }
        }

        public static bool WaitForDronesToReturn
        {
            get
            {
                if (ESCache.Instance.MyShipEntity._directEntity.DirectAbsolutePosition.GetDistance(Drones.LastDronesNeedToBePulledPositionInSpace) > (double)Distances.OnGridWithMe)
                {
                    Log.WriteLine("WaitForDronesToReturn: [" + ESCache.Instance.MyShipEntity._directEntity.DirectAbsolutePosition.GetDistance(Drones.LastDronesNeedToBePulledPositionInSpace) + "]m > Distances.OnGridWithMe [" + Distances.OnGridWithMe + "]: Updating LastDronesNeedToBePulledPositionInSpace to be these coordinates");
                    Drones.LastDronesNeedToBePulledPositionInSpace = ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.PositionInSpace;
                    Time.Instance.LastDronesNeedToBePulledTimeStamp = DateTime.UtcNow;
                    return true;
                }

                if (200000 > ESCache.Instance.MyShipEntity._directEntity.DirectAbsolutePosition.GetDistance(Drones.LastDronesNeedToBePulledPositionInSpace))
                {
                    if (DateTime.UtcNow > Time.Instance.LastDronesNeedToBePulledTimeStamp.AddSeconds(30))
                    {
                        Log.WriteLine("WaitForDronesToReturn: if (DateTime.UtcNow > Time.Instance.LastDronesNeedToBePulledTimeStamp.AddSeconds(30)) return false");
                        return false;
                    }
                }

                //abyssal timer uncomfortably low
                if (10 > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                {
                    Log.WriteLine("WaitForDronesToReturn: if (10 > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) return false");
                    return false;
                }

                if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && ((ESCache.Instance.ActiveShip.IsArmorTanked && 35 > ESCache.Instance.MyShipEntity.ArmorPct) || (ESCache.Instance.ActiveShip.IsShieldTanked && 35 > ESCache.Instance.MyShipEntity.ShieldPct)))
                {
                    Log.WriteLine("WaitForDronesToReturn: if (AbyssalDeadspaceBehavior.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && (ESCache.Instance.ActiveShip.IsArmorTanked && 35 > ESCache.Instance.MyShipEntity.ArmorPct) || (ESCache.Instance.ActiveShip.IsShieldTanked && 35 > ESCache.Instance.MyShipEntity.ShieldPct)) return false");
                    return false;
                }

                return true;
            }
        }

        public static int FactionDroneTypeID { get; set; }
        public static bool DronesShouldBePulled { get; set; }

        public static long? _lastTargetIDDronesEngaged { get; set; }

        public static long? LastTargetIDDronesEngaged
        {
            get
            {
                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == _lastTargetIDDronesEngaged))
                    return _lastTargetIDDronesEngaged;

                _lastTargetIDDronesEngaged = null;
                return null;
            }
            set
            {
                _lastTargetIDDronesEngaged = value;
            }
        }

        public static int LongRangeDroneRecallArmorPct { get; set; }
        public static int LongRangeDroneRecallCapacitorPct { get; set; }
        public static int LongRangeDroneRecallShieldPct { get; set; }

        private static double? _maxDronesAllowedInSpace;
        public static double MaxDronesAllowedInSpace
        {
            get
            {
                if (_maxDronesAllowedInSpace == null)
                {
                    _maxDronesAllowedInSpace = Math.Floor((double)Math.Min(5, (ESCache.Instance.ActiveShip.DroneBandwidth / DroneInvType.Volume)));
                    return _maxDronesAllowedInSpace ?? 0;
                }

                return _maxDronesAllowedInSpace ?? 0;
            }
        }

        public static double MaxDroneRange
        {
            get
            {
                if (_maxDroneRange == null)
                {
                    _maxDroneRange = Math.Min(DroneControlRange, Combat.MaxTargetRange);
                    return (double) _maxDroneRange;
                }

                return (double) _maxDroneRange;
            }
        }

        public static EntityCache PreferredDroneTarget
        {
            get
            {
                if (_preferredDroneTarget == null)
                {
                    if (PreferredDroneTargetID != null)
                    {
                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == PreferredDroneTargetID))
                        {
                            _preferredDroneTarget = ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == PreferredDroneTargetID);
                            return _preferredDroneTarget;
                        }

                        return null;
                    }

                    return null;
                }

                return _preferredDroneTarget;
            }
            set
            {
                if (value == null)
                {
                    if (_preferredDroneTarget != null)
                    {
                        _preferredDroneTarget = null;
                        PreferredDroneTargetID = null;
                        Log.WriteLine("[ null ]");
                    }
                }
                else
                {
                    if (_preferredDroneTarget != null && _preferredDroneTarget.Id != value.Id)
                    {
                        _preferredDroneTarget = value;
                        PreferredDroneTargetID = value.Id;
                    }
                }
            }
        }

        public static long? PreferredDroneTargetID { get; set; }

        public static bool UseDrones
        {
            get
            {
                try
                {
                    //
                    // if we have the character set to not use drones we dont care what the mission XML wants us to do...
                    //
                    if ((!DefaultUseDrones && !ESCache.Instance.ActiveShip.IsShipWithDroneBonuses) || (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsShipWithNoDroneBay)) return false;

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController) ||
                        ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    {
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                            return false;

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                            return true;

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                            return true;

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Worm)
                            return true;

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Cormorant)
                            return false;

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Hawk)
                            return false;

                        //if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Arbritrator)
                        //    return true;
                    }

                    if (MissionSettings.PocketUseDrones != null)
                    {
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine("We are using PocketDrones setting [" + MissionSettings.PocketUseDrones + "]");
                        return (bool) MissionSettings.PocketUseDrones;
                    }

                    if (MissionSettings.MissionUseDrones != null)
                    {
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine("We are using MissionDrones setting [" + MissionSettings.MissionUseDrones + "]");
                        return (bool) MissionSettings.MissionUseDrones;
                    }

                    return DefaultUseDrones;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        private static List<PriorityTarget> _dronePriorityTargets { get; set; }

        private static bool OurDronesHaveAHugeBonusToHitPoints
        {
            get
            {
                if (ESCache.Instance.InSpace)
                {
                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Rattlesnake)
                        return true;

                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.TypeId == (int) TypeID.Gila)
                        return true;

                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                        return true;

                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Worm)
                        return true;

                    if (ActiveDroneCount > 0 && ActiveDrones.Any(drone => drone.TypeId == (int) TypeID.Gecko))
                        return true;

                    return false;
                }

                return false;
            }
        }

        public static void ClearPerPocketCache()
        {
            _shouldWeLaunchDrones = null;
            LastTargetIDDronesEngaged = null;
            IsRecoverLostDronesAlreadyProcessedInThisPocket = false;
            _droneRecallTimers = new Dictionary<long, DateTime>();
        }

        public static void ResetInSpaceSettingsWhenEnteringStation()
        {
            DronesShouldBePulled = false;
        }

        private static Dictionary<long, DateTime> _droneRecallTimers = new Dictionary<long, DateTime>();

        #endregion Properties

        #region Methods

        private static void TrackDroneRecalls()
        {
            var allDronesInSpaceIds = AllDronesInSpace.Select(d => d.Id).ToList();
            // we only allow any of the in space drones to be part of the dict
            foreach (var key in _droneRecallTimers.Keys.ToList())
            {
                if (!allDronesInSpaceIds.Any(e => e == key))
                {
                    _droneRecallTimers.Remove(key);
                }
            }

            // add returning drones to the dict
            foreach (var d in AllDronesInSpace.Where(e => e._directEntity.DroneState == (int)DroneState.Returning))
            {
                if (!_droneRecallTimers.ContainsKey(d.Id))
                {
                    _droneRecallTimers[d.Id] = DateTime.UtcNow;
                }
            }
        }

        private static int DroneReturningSinceSeconds(long droneId)
        {
            if (_droneRecallTimers.ContainsKey(droneId))
            {
                return Math.Abs((int)(DateTime.UtcNow - _droneRecallTimers[droneId]).TotalSeconds);
            }

            return 0;
        }

        private static int SpamDronesEngageDelayInMilliSeconds
        {
            get
            {
                if (SpamDronesEngage)
                    return ESCache.Instance.RandomNumber(4500, 5500);

                return ESCache.Instance.RandomNumber(1800, 2200);
            }
        }

        private static bool ReEngageDronesIfNeeded
        {
            get
            {
                try
                {
                    if (Combat.PotentialCombatTargets.Any(x => x.IsTarget) && Drones.AllDronesInSpace.Any() && Combat.PotentialCombatTargets.All(x => !x.IsTargeting) && areDronesIdle)
                    {
                        if (Combat.PotentialCombatTargets.Any(x => x.IsTarget) && Combat.PotentialCombatTargets.All(y => !y.IsActiveTarget))
                        {
                            if (_cachedDroneTarget == null)
                                PickDroneTarget();

                            if (_cachedDroneTarget == null)
                                _cachedDroneTarget.MakeActiveTarget();
                            else
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsTarget).MakeActiveTarget();
                        }

                        Log.WriteLine("ReEngageDronesIfNeeded: if (Combat.PotentialCombatTargets.Any(x => x.IsTarget) && ActiveDrones.Any(e => e.FollowId == 0))");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesEngage);
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private static bool SpamDronesEngage
        {
            get
            {
                if (Combat.PotentialCombatTargets.Count > 0)
                {
                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                        if (ESCache.Instance.InMission)
                            if (MissionSettings.MyMission != null && MissionSettings.MyMission.Name.Contains("Anomic"))
                                if (DateTime.UtcNow > LastDroneFightCmd.AddSeconds(22))
                                    return true;

                    if (ActiveDrones.Any(e => e.FollowId == 0))
                    {

                        return true;
                    }

                    if (DronesKillHighValueTargets)
                    {
                        if (ActiveDrones.Any(e => 0 == e.Velocity))
                            return true;

                        if (ActiveDrones.Any(x => 4000 > x.Distance))
                            return true;

                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsNPCDrone && i.Velocity != 0))
                            return true;

                        if (Combat.PotentialCombatTargets.Any(i => i.Name == "Harrowing Vedmak"))
                            return true;

                        if (Combat.PotentialCombatTargets.Any(i => i.Name == "Starving Vedmak"))
                            return true;

                        //if (Combat.PotentialCombatTargets.Any(i => i.Name == "Karybdis Tyrannos"))
                        //    return true;
                    }

                    return false;
                }

                return false;
            }
        }

        public static void AddDronePriorityTarget(EntityCache ewarEntity, DronePriority priority, bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                if (AddEwarTypeToPriorityTargetList && UseDrones)
                {
                    if (ewarEntity.IsIgnored || DronePriorityTargets.Any(p => p.EntityID == ewarEntity.Id))
                    {
                        if (DebugConfig.DebugAddDronePriorityTarget)
                            Log.WriteLine("if ((target.IsIgnored) || DronePriorityTargets.Any(p => p.Id == target.Id))");
                        return;
                    }

                    if (DronePriorityTargets.All(i => i.EntityID != ewarEntity.Id))
                    {
                        int DronePriorityTargetCount = 0;
                        if (DronePriorityTargets.Count > 0)
                            DronePriorityTargetCount = DronePriorityTargets.Count;
                        Log.WriteLine("Adding [" + ewarEntity.Name + "] Speed [" + Math.Round(ewarEntity.Velocity, 2) + " m/s] Distance [" +
                                      Math.Round(ewarEntity.Distance / 1000, 2) + "] [ID: " + ewarEntity.MaskedId + "] as a drone priority target [" +
                                      priority +
                                      "] we have [" + DronePriorityTargetCount + "] other DronePriorityTargets");
                        _dronePriorityTargets.Add(new PriorityTarget {Name = ewarEntity.Name, EntityID = ewarEntity.Id, DronePriority = priority});
                    }

                    return;
                }

                if (DebugConfig.DebugAddDronePriorityTarget)
                    Log.WriteLine("UseDrones is [" + UseDrones + "] AddWarpScramblersToDronePriorityTargetList is [" +
                                  AddWarpScramblersToDronePriorityTargetList + "] [" + ewarEntity.Name +
                                  "] was not added as a Drone PriorityTarget (why did we even try?)");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void AddDronePriorityTargets(IEnumerable<EntityCache> ewarEntities, DronePriority priority, string module,
            bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                if (ewarEntities.Any())
                    foreach (EntityCache ewarEntity in ewarEntities)
                        AddDronePriorityTarget(ewarEntity, priority, AddEwarTypeToPriorityTargetList);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void AddDronePriorityTargetsByName(string stringEntitiesToAdd)
        {
            try
            {
                IEnumerable<EntityCache> entitiesToAdd = ESCache.Instance.EntitiesByPartialName(stringEntitiesToAdd).ToList();
                if (entitiesToAdd.Any())
                {
                    foreach (EntityCache entityToAdd in entitiesToAdd)
                    {
                        Log.WriteLine("adding [" + entityToAdd.Name + "][" + Math.Round(entityToAdd.Distance / 1000, 0) + "k][" + entityToAdd.MaskedId +
                                      "] to the PWPT List");
                        AddDronePriorityTarget(entityToAdd, DronePriority.PriorityKillTarget);
                    }

                    return;
                }

                Log.WriteLine("[" + stringEntitiesToAdd + "] was not found on grid");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool ChangeDroneControllerState(DroneControllerState state, bool wait = true)
        {
            try
            {
                if (State.CurrentDroneControllerState != state)
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("New DroneControllerState [" + state + "]");
                    State.CurrentDroneControllerState = state;
                    if (wait)
                        _nextDroneAction = DateTime.UtcNow.AddMilliseconds(250);
                    else
                        ProcessState();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static EntityCache FindDronePriorityTarget(EntityCache currentTarget, DronePriority priorityType, bool AddECMTypeToDronePriorityTargetList,
            double Distance, bool FindAUnTargetedEntity = true)
        {
            if (AddECMTypeToDronePriorityTargetList)
            {
                try
                {
                    EntityCache target = null;
                    try
                    {
                        if (DronePriorityEntities.Any(pt => pt.DronePriorityLevel == priorityType))
                            target =
                                DronePriorityEntities.Where(
                                        pt =>
                                            ((FindAUnTargetedEntity || pt.IsReadyForDronesToShoot) && currentTarget != null && pt.Id == currentTarget.Id &&
                                            pt.Distance < Distance && pt.IsActiveDroneEwarType == priorityType)
                                            ||
                                            ((FindAUnTargetedEntity || pt.IsReadyForDronesToShoot) && pt.Distance < Distance && pt.IsActiveDroneEwarType == priorityType))
                                    .OrderByDescending(pt => pt.IsNPCFrigate)
                                    .ThenByDescending(pt => pt.IsLastTargetDronesWereShooting)
                                    .ThenByDescending(pt => pt.IsInDroneRange)
                                    .ThenBy(pt => pt.IsEntityIShouldKeepShootingWithDrones)
                                    .ThenBy(pt => pt.ShieldPct + pt.ArmorPct + pt.StructurePct)
                                    .ThenBy(pt => pt.Nearest5kDistance)
                                    .FirstOrDefault();
                    }
                    catch (NullReferenceException)
                    {
                    }

                    if (target != null)
                    {
                        if (!FindAUnTargetedEntity)
                        {
                            PreferredDroneTarget = target;
                            Time.Instance.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                            return target;
                        }

                        return target;
                    }

                    return null;
                }
                catch (NullReferenceException)
                {
                }

                return null;
            }

            return null;
        }

        public static void InvalidateCache()
        {
            try
            {
                _activeDroneCount = null;
                _allDronesInSpace = null;
                _droneTypeID = 0;
                _activeDrones = null;
                _droneBay = null;
                _dronePriorityEntities = null;
                _maxDronesAllowedInSpace = null;
                _maxDroneRange = null;
                _cachedDroneTarget = null;
                _entityToAssistDronesTo = null;
                _preferredDroneTarget = null;
                _pickDroneTarget_cached = null;

                if (_dronePriorityTargets != null && _dronePriorityTargets.Count > 0)
                    _dronePriorityTargets.ForEach(pt => pt.ClearCache());
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: Drones");
                AddDampenersToDronePriorityTargetList =
                    (bool?) CharacterSettingsXml.Element("addDampenersToDronePriorityTargetList") ??
                    (bool?) CommonSettingsXml.Element("addDampenersToDronePriorityTargetList") ?? true;
                AddECMsToDroneTargetList =
                    (bool?) CharacterSettingsXml.Element("addECMsToDroneTargetList") ??
                    (bool?) CommonSettingsXml.Element("addECMsToDroneTargetList") ?? true;
                AddNeutralizersToDronePriorityTargetList =
                    (bool?) CharacterSettingsXml.Element("addNeutralizersToDronePriorityTargetList") ??
                    (bool?) CommonSettingsXml.Element("addNeutralizersToDronePriorityTargetList") ?? true;
                AddTargetPaintersToDronePriorityTargetList =
                    (bool?) CharacterSettingsXml.Element("addTargetPaintersToDronePriorityTargetList") ??
                    (bool?) CommonSettingsXml.Element("addTargetPaintersToDronePriorityTargetList") ?? true;
                AddTrackingDisruptorsToDronePriorityTargetList =
                    (bool?) CharacterSettingsXml.Element("addTrackingDisruptorsToDronePriorityTargetList") ??
                    (bool?) CommonSettingsXml.Element("addTrackingDisruptorsToDronePriorityTargetList") ?? true;
                AddWarpScramblersToDronePriorityTargetList =
                    (bool?) CharacterSettingsXml.Element("addWarpScramblersToDronePriorityTargetList") ??
                    (bool?) CommonSettingsXml.Element("addWarpScramblersToDronePriorityTargetList") ?? true;
                AddWebifiersToDronePriorityTargetList =
                    (bool?) CharacterSettingsXml.Element("addWebifiersToDronePriorityTargetList") ??
                    (bool?) CommonSettingsXml.Element("addWebifiersToDronePriorityTargetList") ?? true;
                DefaultUseDrones =
                    (bool?) CharacterSettingsXml.Element("useDrones") ??
                    (bool?) CommonSettingsXml.Element("useDrones") ?? true;
                Log.WriteLine("Drones: useDrones [" + DefaultUseDrones + "]");
                DefaultDroneTypeID =
                    (int?) CharacterSettingsXml.Element("droneTypeId") ??
                    (int?) CommonSettingsXml.Element("droneTypeId") ?? 0;
                Log.WriteLine("Drones: droneTypeId [" + DefaultDroneTypeID + "]");
                BuyAmmoDroneAmmount =
                    (int?) CharacterSettingsXml.Element("buyAmmoDroneAmount") ??
                    (int?) CommonSettingsXml.Element("buyAmmoDroneAmount") ?? 200;
                Log.WriteLine("Drones: buyAmmoDroneAmount [" + BuyAmmoDroneAmmount + "]");
                MinimumNumberOfDronesBeforeWeGoBuyMore =
                    (int?) CharacterSettingsXml.Element("minimumNumberOfDronesBeforeWeGoBuyMore") ??
                    (int?) CommonSettingsXml.Element("minimumNumberOfDronesBeforeWeGoBuyMore") ?? 100;
                Log.WriteLine("Drones: minimumNumberOfDronesBeforeWeGoBuyMore [" + MinimumNumberOfDronesBeforeWeGoBuyMore + "]");
                //DroneControlRange =
                //    (int?) CharacterSettingsXml.Element("droneControlRange") ??
                //    (int?) CommonSettingsXml.Element("droneControlRange") ?? 25000;
                //Log.WriteLine("Drones: droneControlRange [" + DroneControlRange + "]");
                DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive =
                    (bool?) CharacterSettingsXml.Element("dronesDontNeedTargetsBecauseWehaveThemSetOnAggressive") ??
                    (bool?) CommonSettingsXml.Element("dronesDontNeedTargetsBecauseWehaveThemSetOnAggressive") ?? true;
                Log.WriteLine("Drones: dronesDontNeedTargetsBecauseWehaveThemSetOnAggressive [" + DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive + "]");
                DroneMinimumShieldPct =
                    (int?) CharacterSettingsXml.Element("droneMinimumShieldPct") ??
                    (int?) CommonSettingsXml.Element("droneMinimumShieldPct") ?? 50;
                DroneMinimumArmorPct =
                    (int?) CharacterSettingsXml.Element("droneMinimumArmorPct") ??
                    (int?) CommonSettingsXml.Element("droneMinimumArmorPct") ?? 50;
                DroneMinimumCapacitorPct =
                    (int?) CharacterSettingsXml.Element("droneMinimumCapacitorPct") ??
                    (int?) CommonSettingsXml.Element("droneMinimumCapacitorPct") ?? 0;
                DroneRecallShieldPct =
                    (int?) CharacterSettingsXml.Element("droneRecallShieldPct") ??
                    (int?) CommonSettingsXml.Element("droneRecallShieldPct") ?? 0;
                DroneRecallArmorPct =
                    (int?) CharacterSettingsXml.Element("droneRecallArmorPct") ??
                    (int?) CommonSettingsXml.Element("droneRecallArmorPct") ?? 0;
                DroneRecallCapacitorPct =
                    (int?) CharacterSettingsXml.Element("droneRecallCapacitorPct") ??
                    (int?) CommonSettingsXml.Element("droneRecallCapacitorPct") ?? 0;
                LongRangeDroneRecallShieldPct =
                    (int?) CharacterSettingsXml.Element("longRangeDroneRecallShieldPct") ??
                    (int?) CommonSettingsXml.Element("longRangeDroneRecallShieldPct") ?? 0;
                LongRangeDroneRecallArmorPct =
                    (int?) CharacterSettingsXml.Element("longRangeDroneRecallArmorPct") ??
                    (int?) CommonSettingsXml.Element("longRangeDroneRecallArmorPct") ?? 0;
                LongRangeDroneRecallCapacitorPct =
                    (int?) CharacterSettingsXml.Element("longRangeDroneRecallCapacitorPct") ??
                    (int?) CommonSettingsXml.Element("longRangeDroneRecallCapacitorPct") ?? 0;
                DronesKillHighValueTargets =
                    (bool?) CharacterSettingsXml.Element("dronesKillHighValueTargets") ??
                    (bool?) CommonSettingsXml.Element("dronesKillHighValueTargets") ?? false;
                //Log.WriteLine("LoadSettings: Drones: dronesKillHighValueTargets [" + DronesKillHighValueTargets + "]");
                BelowThisHealthLevelRemoveFromDroneBay =
                    (int?) CharacterSettingsXml.Element("belowThisHealthLevelRemoveFromDroneBay") ??
                    (int?) CommonSettingsXml.Element("belowThisHealthLevelRemoveFromDroneBay") ?? 150;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Error Loading Weapon and targeting Settings [" + exception + "]");
            }
        }

        public static void ShootTheBestIndividualNPCOfTheSameTypeWeChoose()
        {
            if (_cachedDroneTarget != null && Combat.PotentialCombatTargets.Count > 0)
            {
                if (Combat.PotentialCombatTargets.Any(i => i.IsEntityDronesAreShooting && i.IsInDroneRange && i.IsTarget && i.Name == _cachedDroneTarget.Name && !i.WeShouldFocusFire))
                {
                    _cachedDroneTarget = Combat.PotentialCombatTargets.Find(i => i.IsEntityDronesAreShooting && i.IsInDroneRange && i.IsTarget && i.Name == _cachedDroneTarget.Name && !i.WeShouldFocusFire);
                    return;
                }

                if (ActiveDroneCount > 0 && Combat.PotentialCombatTargets.Any(i => i.IsReadyForDronesToShoot && i.Name == _cachedDroneTarget.Name && !i.WeShouldFocusFire))
                {
                    _cachedDroneTarget = Combat.PotentialCombatTargets.Where(i => i.IsReadyForDronesToShoot && i.Name == _cachedDroneTarget.Name && !i.WeShouldFocusFire).OrderBy(x => x._directEntity.DirectAbsolutePosition.GetDistance(ActiveDrones.FirstOrDefault()._directEntity.DirectAbsolutePosition)).FirstOrDefault();
                    return;
                }
            }
        }

        public static EntityCache PickDroneTarget()
        {
            if (_cachedDroneTarget != null)
                return _cachedDroneTarget;

            if (ESCache.Instance.InWormHoleSpace)
            {
                if (ESCache.Instance.MyShipEntity.GroupId == (int)Group.Dreadnaught)
                {
                    return null;
                }

                //return PickPrimaryWeaponTargetWSpace.FirstOrDefault();
            }

            if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
            {
                if (DronesKillHighValueTargets)
                {
                    _cachedDroneTarget = PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets();
                    if (_cachedDroneTarget != null)
                    {

                        //we sort all NPCs with the same name by .IsLowestHealthNpcWithThisSameName within pickDroneTarget...
                        //ShootTheBestIndividualNPCOfTheSameTypeWeChoose();
                        PreferredDroneTargetID = _cachedDroneTarget.Id;
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine("DebugTargetCombatants: PickDroneTarget: Abyssal: DronesKillHIghValueTargets:  _pickDroneTarget [" + _cachedDroneTarget.Name + "] as KillTarget for drones");
                        return _cachedDroneTarget;
                    }

                    return null;
                }

                if (ESCache.Instance.ActiveShip.HasSpeedMod && NavigateOnGrid.SpeedTank)
                {
                    _cachedDroneTarget = PickDroneTarget_AbyssalDeadSpaceWhileSpeedTank();
                    if (_cachedDroneTarget != null)
                    {
                        PreferredDroneTargetID = _cachedDroneTarget.Id;
                        ShootTheBestIndividualNPCOfTheSameTypeWeChoose();
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine("DebugTargetCombatants: PickDroneTarget: Abyssal: DronesKillHIghValueTargets: SpeedTank:  _pickDroneTarget [" + _cachedDroneTarget.Name + "] as KillTarget for drones");
                        return _cachedDroneTarget;
                    }
                }

                _cachedDroneTarget = PickDroneTarget_AbyssalDeadSpace();
                if (_cachedDroneTarget != null)
                {
                    ShootTheBestIndividualNPCOfTheSameTypeWeChoose();
                    PreferredDroneTargetID = _cachedDroneTarget.Id;
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("DebugTargetCombatants: PickDroneTarget: Abyssal: DronesKillHIghValueTargets: _pickDroneTarget [" + _cachedDroneTarget.Name + "] as KillTarget for drones");
                    return _cachedDroneTarget;
                }

                return null;
            }

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) ||
                ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Nestor)
                {
                    return null;
                }

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Logistics)
                {
                    return null;
                }

                if (!ESCache.Instance.DockableLocations.All(i => i.IsOnGridWithMe) && !ESCache.Instance.Stargates.All(i => i.IsOnGridWithMe) && Combat.PotentialCombatTargets.Count > 0 && ESCache.Instance.Weapons.Count == 0 && (ESCache.Instance.ActiveShip.GroupId == (int)Group.CommandShip || ESCache.Instance.ActiveShip.GroupId == (int)Group.Battlecruiser))
                {
                    return null;
                }

                //
                // 1st or 2nd Room of Observatory Site
                //
                if (ESCache.Instance.AccelerationGates.Any(i => i.Name.Contains("Observatory")) || ESCache.Instance.Entities.Any(i => i.Name.Contains("Triglavian Stellar Accelerator")) || ESCache.Instance.Entities.Any(i => i.Name.Contains("Stellar Observatory")))
                {
                    return null;
                }

                //
                // All other Sites
                //
                //if (Drones.DronesKillHighValueTargets)
                //{
                //    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetAbyssalDeadSpaceDronesKillHighValueTargets();");
                //    _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetTriglavianInvasionDronesKillHighValueTargets.FirstOrDefault(i => !i.IsWreck);
                //    ShootNPCWeAreAlreadyEngagingIfSameName();
                //    return _pickPrimaryWeaponTarget;
                //}

                _cachedDroneTarget = PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets();
                PreferredDroneTargetID = _cachedDroneTarget.Id;
                ShootTheBestIndividualNPCOfTheSameTypeWeChoose();
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: PickDroneTarget: DronesKillHIghValueTargets: _pickDroneTarget [" + _cachedDroneTarget.Name + "] as KillTarget for drones");
                return _cachedDroneTarget;
            }

            if (State.CurrentHydraState == HydraState.Combat)
            {
                if (ESCache.Instance.Targets.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderIsAggressingTargetId))
                    _cachedDroneTarget = ESCache.Instance.Targets.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderIsAggressingTargetId);
                return _cachedDroneTarget;
            }

            if (ESCache.Instance.InMission)
                if (ESCache.Instance.MyShipEntity != null)
                {
                    if (ESCache.Instance.MyShipEntity.IsFrigate)
                    {
                        if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && MissionSettings.MyMission != null && MissionSettings.MyMission.Name.Contains("Anomic"))
                        {
                            _cachedDroneTarget = PickDroneTargetBasedOnTargetsForBurners();
                            ShootTheBestIndividualNPCOfTheSameTypeWeChoose();
                            return _cachedDroneTarget;
                        }

                        //if (DebugConfig.DebugKillTargets) Log.WriteLine("if (ESCache.Instance.InMission) if (ESCache.Instance.MyShipEntity.IsFrigate)");
                        //droneTarget = PickPrimaryWeaponTargetBasedOnTargetsForAFrigate.FirstOrDefault();
                        //if (droneTarget != null)
                        //    return droneTarget;
                        //
                        //return null;
                    }

                    if (ESCache.Instance.MyShipEntity.IsCruiser)
                    {
                        //if (DebugConfig.DebugKillTargets) Log.WriteLine("if (ESCache.Instance.InMission) if (ESCache.Instance.MyShipEntity.IsCruiser)");
                        //droneTarget = PickPrimaryWeaponTargetBasedOnTargetsForACruiser.FirstOrDefault();
                        //if (droneTarget != null)
                        //    return droneTarget;
                        //
                        //return null;
                    }

                    if (ESCache.Instance.MyShipEntity.IsBattleship)
                    {
                    //    if (DebugConfig.DebugKillTargets) Log.WriteLine("if (ESCache.Instance.InMission) if (ESCache.Instance.MyShipEntity.IsBattleship)");
                    //    droneTarget = PickPrimaryWeaponTargetBasedOnTargetsForABattleship.FirstOrDefault();
                    //    if (droneTarget != null)
                    //        return droneTarget;
                    //
                    //    return null;
                    }
                    //
                    //
                    // Default to picking targets for a battleship sized ship
                    //
                    //if (DebugConfig.DebugKillTargets) Log.WriteLine("if (ESCache.Instance.InMission) MyShipEntity class unknown");
                    //droneTarget = PickPrimaryWeaponTargetBasedOnTargetsForABattleship.FirstOrDefault();
                    //if (droneTarget != null)
                    //    return droneTarget;
                    //

                    //if (_cachedDroneTarget == null && ESCache.Instance.Targets.Count > 0)
                    //{
                    //    _cachedDroneTarget = ESCache.Instance.Targets.Where(i => !i.IsBadIdea).OrderBy(i => 100 > i.HealthPct).FirstOrDefault();
                    //    ShootTheBestIndividualNPCOfTheSameTypeWeChoose();
                    //    if (DebugConfig.DebugDrones) Log.WriteLine("droneTarget was still null: choosing the first non-badidea target [" + _cachedDroneTarget.Name + "][" + _cachedDroneTarget.Distance + "]");
                    //    return _cachedDroneTarget;
                    //}
                }

            if (DronesKillHighValueTargets)
            {
                _cachedDroneTarget = PickDroneTarget_DronesKillHighValueTargets();
                PreferredDroneTargetID = _cachedDroneTarget.Id;
                ShootTheBestIndividualNPCOfTheSameTypeWeChoose();
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: PickDroneTarget: DronesKillHIghValueTargets: _pickDroneTarget [" + _cachedDroneTarget.Name + "] as KillTarget for drones");
                return _cachedDroneTarget;
            }

            List<EntityCache> droneTargets = Combat.PotentialCombatTargets.Where(i => i.IsReadyForDronesToShoot && !i.IsIgnored)
                    .OrderByDescending(a => a.IsLowValueTarget && a.IsWarpScramblingMe && 100 > a.HealthPct)
                    .ThenByDescending(b => b.IsLowValueTarget && b.IsWarpScramblingMe)
                    .ThenByDescending(b => b.IsLowValueTarget && b.WarpScrambleChance > 0 && 100 > b.HealthPct)
                    .ThenByDescending(b => b.IsLowValueTarget && b.WarpScrambleChance > 0)
                    .ThenByDescending(c => c.IsLowValueTarget && c.IsPreferredDroneTarget && 100 > c.HealthPct)
                    .ThenByDescending(c => c.IsLowValueTarget && c.IsPreferredDroneTarget)
                    //.ThenByDescending(j => !j.IsTrigger)
                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsWebbingMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsWebbingMe)

                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsNeutralizingMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsNeutralizingMe)

                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTrackingDisruptingMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTrackingDisruptingMe)

                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTryingToJamMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTryingToJamMe)

                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsSensorDampeningMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsSensorDampeningMe)

                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTargetPaintingMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTargetPaintingMe)

                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsWebbingMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsWebbingMe)

                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsNeutralizingMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsNeutralizingMe)

                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTrackingDisruptingMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTrackingDisruptingMe)

                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTryingToJamMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTryingToJamMe)

                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsSensorDampeningMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsSensorDampeningMe)

                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTargetPaintingMe && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTargetPaintingMe)


                    .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && 100 > g.HealthPct)
                    .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && 100 > g.HealthPct)

                    .ThenByDescending(g => g.IsDestroyer || g.IsNPCDestroyer)
                    .ThenByDescending(g => g.IsFrigate || g.IsNPCFrigate)

                    .ThenByDescending(f => Combat.KillTarget != null && Combat.KillTarget.Id == f.Id)

                    .ThenByDescending(f => f.IsAttacking)
                    .ThenByDescending(e => e.IsTargetedBy)

                    .ThenByDescending(c => c.IsLargeCollidableWeAlwaysWantToBlowupFirst)
                .ThenByDescending(c => c.IsLargeCollidableWeAlwaysWantToBlowupLast)
                .ThenBy(p => p.HealthPct)
                .ThenBy(s => s.Nearest5kDistance).ToList();

            if (DebugConfig.DebugLogOrderOfDroneTargets)
                LogOrderOfDroneTargets(droneTargets);

            PreferredDroneTargetID = droneTargets.FirstOrDefault().Id;
            ShootTheBestIndividualNPCOfTheSameTypeWeChoose();
            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: PickDroneTarget: _pickDroneTarget [" + _cachedDroneTarget.Name + "] as KillTarget for drones");
            _cachedDroneTarget = droneTargets.FirstOrDefault();
            return _cachedDroneTarget;
        }

        public static EntityCache PickDroneTargetBasedOnTargetsForBurners()
        {
            if (_pickDroneTarget_cached == null)
            {
                _pickDroneTarget_cached = ESCache.Instance.Targets.Where(i => !i.IsWreck && !i.IsBadIdea && i.IsTarget && !i.IsNPCDrone)
                    .OrderByDescending(j => j.IsPrimaryWeaponPriorityTarget)
                    .ThenByDescending(j => j.IsDronePriorityTarget)
                    .ThenByDescending(j => j.IsBurnerMainNPC).FirstOrDefault() ;

                if (_pickDroneTarget_cached != null)
                {
                    return _pickDroneTarget_cached;
                }

                return null;
            }

            return null;
        }

        public static EntityCache PickDroneTarget_AbyssalDeadSpace()
        {
            List<EntityCache> droneTargets = Combat.PotentialCombatTargets.Where(i => i.IsReadyForDronesToShoot)
                .OrderByDescending(i => !i.Name.ToLower().Contains("karybdis tyrannos".ToLower())) //last
                .ThenByDescending(i => !i.Name.ToLower().Contains("hadal abyssal overmind".ToLower())) //last
                .ThenByDescending(i => !i.Name.ToLower().Contains("lucid deepwatcher".ToLower())) //last
                .ThenByDescending(i => i.Name.ToLower().Contains("thunderchild".ToLower()))//first
                .ThenByDescending(i => i.Name.ToLower().Contains("arrester marshal disparu troop".ToLower()))//first
                .ThenByDescending(i => i.Name.ToLower().Contains("drainer marshal disparu troop".ToLower()))//first
                .ThenByDescending(l => l.IsNPCBattlecruiser && !l.IsAttacking)
                .ThenByDescending(k => k.IsNPCBattlecruiser)
                .ThenByDescending(l => l.IsNPCFrigate && 100 > l.HealthPct)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWarpScramblingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWithinOptimalOfDrones)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsCloseToDrones)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking) //kill things shooting drones!
                .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted()).ThenByDescending(o => Combat._pickPrimaryWeaponTarget != null &&  Combat._pickPrimaryWeaponTarget != null && o != Combat._pickPrimaryWeaponTarget && o.IsNPCFrigate)
                .ThenByDescending(l => l.IsNPCFrigate)
                .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWithinOptimalOfDrones)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsCloseToDrones)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking) //kill things shooting drones!
                .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                .ThenByDescending(i => i.IsNPCCruiser && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                .ThenByDescending(i => i.IsNPCCruiser && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                .ThenByDescending(l => l.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && ESCache.Instance.Weapons.Count == 0)
                //.ThenByDescending(l => l.IsAbyssalDeadspaceTriglavianExtractionNode && ESCache.Instance.Weapons.Count == 0)
                .ThenByDescending(l => l.IsNPCCruiser)
                .ThenByDescending(l => l.IsNPCBattleship && 100 > l.HealthPct)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(j => j.IsNPCBattleship)
                .ThenByDescending(i => i.IsEntityIShouldKeepShooting).ToList();

            if (droneTargets != null && droneTargets.Count > 0)
            {
                if (DebugConfig.DebugLogOrderOfDroneTargets)
                    LogOrderOfDroneTargets(droneTargets);

                return droneTargets.FirstOrDefault();
            }

            return null;
        }

        public static EntityCache PickDroneTarget_AbyssalDeadSpaceWhileSpeedTank()
        {
            List<EntityCache> droneTargets = Combat.PotentialCombatTargets.Where(i => i.IsReadyForDronesToShoot)
                //.OrderByDescending(l => !l.WeShouldFocusFire && !l.IsKillTarget)
                .OrderByDescending(i => !i.Name.ToLower().Contains("karybdis tyrannos".ToLower())) //last
                .ThenByDescending(i => !i.Name.ToLower().Contains("hadal abyssal overmind".ToLower())) //last
                .ThenByDescending(i => !i.Name.ToLower().Contains("lucid deepwatcher".ToLower())) //last
                .ThenByDescending(i => i.Name.ToLower().Contains("thunderchild".ToLower()))//first
                .ThenByDescending(i => i.Name.ToLower().Contains("arrester marshal disparu troop".ToLower()))//first
                .ThenByDescending(i => i.Name.ToLower().Contains("drainer marshal disparu troop".ToLower()))//first
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800)
                .ThenByDescending(l => l.IsNPCBattleship && l.StructurePct < .90)
                .ThenByDescending(l => l.IsNPCBattleship && l.ArmorPct < .90)
                .ThenByDescending(l => l.IsNPCBattleship && l.ShieldPct < .90)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsHighDps)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(j => j.IsNPCBattleship)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCBattlecruiser && l.StructurePct < .90)
                .ThenByDescending(l => l.IsNPCBattlecruiser && l.ArmorPct < .90)
                .ThenByDescending(l => l.IsNPCBattlecruiser && l.ShieldPct < .90)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasRemoteRepair)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsSensorDampeningMe)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCBattlecruiser && l.IsHighDps)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCBattlecruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(k => k.IsNPCBattlecruiser)
                .ThenByDescending(k => k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                //.ThenByDescending(k => k.IsAbyssalDeadspaceTriglavianExtractionNode)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.StructurePct < .90)
                .ThenByDescending(l => l.IsNPCCruiser && l.ArmorPct < .90)
                .ThenByDescending(l => l.IsNPCCruiser && l.ShieldPct < .90)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsHighDps)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(l => l.IsNPCCruiser)
                .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && 100 > l.HealthPct && l.IsLastTargetDronesWereShooting) //kill things shooting drones!
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && 100 > l.HealthPct) //kill things shooting drones!
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && !l.NpcHasALotOfRemoteRepair) //kill things shooting drones!
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.StructurePct < .90)
                .ThenByDescending(l => l.IsNPCFrigate && l.ArmorPct < .90)
                .ThenByDescending(l => l.IsNPCFrigate && l.ShieldPct < .90)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsMissileDisruptingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsHighDps)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                .ThenBy(p => p.StructurePct)
                .ThenBy(q => q.ArmorPct)
                .ThenBy(r => r.ShieldPct).ToList();

            if (droneTargets != null && droneTargets.Count > 0)
            {
                if (DebugConfig.DebugLogOrderOfDroneTargets)
                    LogOrderOfDroneTargets(droneTargets);

                return droneTargets.FirstOrDefault();
            }

            return null;
        }

        private static EntityCache _pickDroneTarget_cached = null;

        public static EntityCache PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets()
        {
            if (_pickDroneTarget_cached != null)
                return _pickDroneTarget_cached;

            List<EntityCache> droneTargets = AbyssalSpawn.AbyssalPotentialCombatTargets_Drones.ToList();

            if (DebugConfig.DebugLogOrderOfDroneTargets)
                LogOrderOfDroneTargets(droneTargets);

            if (droneTargets != null && droneTargets.Any(i => i.IsLowestHealthNpcWithThisSameName))
            {
                droneTargets = droneTargets.Where(i => i.IsReadyForDronesToShoot && i.IsLowestHealthNpcWithThisSameName).ToList();
            }

            if (droneTargets != null && droneTargets.Count > 0)
                if (droneTargets.Any(i => i.IsEntityDronesAreShooting) && droneTargets.Any())
                    if (droneTargets.Find(i => i.IsEntityDronesAreShooting).Name == droneTargets.FirstOrDefault().Name)
                        return droneTargets.Find(i => i.IsEntityDronesAreShooting);

            _pickDroneTarget_cached = droneTargets.FirstOrDefault(i => i.IsReadyForDronesToShoot);
            return _pickDroneTarget_cached;
        }

        public static EntityCache PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_old()
        {
            if (_pickDroneTarget_cached != null)
                return _pickDroneTarget_cached;

            AbyssalSpawn.AbyssalSpawnType AbyssalDetectSpawnResult = AbyssalSpawn.DetectSpawn;

            if (AbyssalDetectSpawnResult != AbyssalSpawn.AbyssalSpawnType.Undecided)
            {
                /**
                switch (AbyssalDetectSpawnResult)
                {
                    case AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindBSSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_AbyssalOvermindSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.AllFrigateSpawn:
                    case AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn:
                    case AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn:
                    case AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_AllFrigateSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.HighAngleBattleCruiserSpawn:
                        break;

                    case AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn:
                        break;

                    case AbyssalSpawn.AbyssalSpawnType.ConcordBSSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_ConcordSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.CruiserSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_EphialtesCruiserSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_DevotedCruiserSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_EphialtesCruiserSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_KarybdisTyrannosSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_KikimoraDestroyerSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn:
                        break;

                    case AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn:
                        break;

                    case AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_EphialtesCruiserSpawn();
                        break;

                    case AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_VedmakSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_VedmakVilaCruiserSwarmerSpawn();

                    case AbyssalSpawn.AbyssalSpawnType.RodivaSpawn:
                        return PickDroneTarget_AbyssalDeadspace_DronesKillHighValueTargets_RodivaSpawn();
                }
                **/
            }

            List<EntityCache> droneTargets = Combat.PotentialCombatTargets.Where(i => i.IsReadyForDronesToShoot)
                .OrderByDescending(i => !i.Name.ToLower().Contains("hadal abyssal overmind".ToLower())) //last
                .ThenByDescending(i => !i.Name.ToLower().Contains("lucid deepwatcher".ToLower())) //last
                .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking && l.IsWebbingMe && DroneIsTooFarFromOldTarget)
                .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking && l.IsWarpScramblingMe && l.IsLowestHealthNpcWithThisSameName && DroneIsTooFarFromOldTarget)
                .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking && l.IsWarpScramblingMe && DroneIsTooFarFromOldTarget)
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && l.IsLowestHealthNpcWithThisSameName && DroneIsTooFarFromOldTarget)
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && DroneIsTooFarFromOldTarget)
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && DroneIsTooFarFromOldTarget)
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && l.IsLowestHealthNpcWithThisSameName && DroneIsTooFarFromOldTarget)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                //.ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                .ThenByDescending(l => l.IsNPCDestroyer && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && DroneIsTooFarFromOldTarget)
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && DroneIsTooFarFromOldTarget)
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && 100 > l.HealthPct && l.IsLastTargetDronesWereShooting && DroneIsTooFarFromOldTarget) //kill things shooting drones!
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsLowestHealthNpcWithThisSameName && DroneIsTooFarFromOldTarget) //kill things shooting drones!
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && !l.NpcHasALotOfRemoteRepair && DroneIsTooFarFromOldTarget) //kill things shooting drones!
                .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked && j.IsLowestHealthNpcWithThisSameName)
                .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked && !j.IsAttacking)
                .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsLastTargetDronesWereShooting)
                .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && !l.IsAttacking)
                .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire)
                .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && !l.IsAttacking)
                .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire)
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                .ThenByDescending(i => i.IsNPCCruiser && i.WarpScrambleChance >= 1 && !i.IsAttacking && Combat.PotentialCombatTargets.Any(x => x.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                .ThenByDescending(i => i.IsNPCCruiser && i.WarpScrambleChance >= 1 && Combat.PotentialCombatTargets.Any(x => x.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                .ThenByDescending(l => l.IsNPCCruiser && l.Name == "Scylla Tyrannos")
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking) //kill things shooting drones!
                .ThenByDescending(l => l.IsNPCBattleship && l.IsLowestHealthNpcWithThisSameName)
                //.ThenByDescending(l => l.IsNPCBattleship && l.IsWithinOptimalOfDrones)
                //.ThenByDescending(l => l.IsNPCBattleship && l.IsCloseToDrones)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(j => j.IsNPCBattleship)
                .ThenByDescending(l => l.IsNPCDestroyer && l.IsLowestHealthNpcWithThisSameName)
                .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking)
                .ThenByDescending(k => k.IsNPCDestroyer)
                .ThenByDescending(l => l.IsNPCBattlecruiser && l.IsLowestHealthNpcWithThisSameName)
                .ThenByDescending(l => l.IsNPCBattlecruiser && 100 > l.HealthPct)
                .ThenByDescending(l => l.IsNPCBattlecruiser && !l.IsAttacking)
                .ThenByDescending(k => k.IsNPCBattlecruiser)
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsLowestHealthNpcWithThisSameName)
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking) // dont kill all non-attacking frigates until we have dealt with BCs (see above)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsLowestHealthNpcWithThisSameName)
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking) //kill things shooting drones!
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWithinOptimalOfDrones)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsCloseToDrones)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                .ThenByDescending(i => i.IsNPCCruiser && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                .ThenByDescending(i => i.IsNPCCruiser && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                .ThenByDescending(l => l.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && ESCache.Instance.Weapons.Count == 0)
                //.ThenByDescending(l => l.IsAbyssalDeadspaceTriglavianExtractionNode && ESCache.Instance.Weapons.Count == 0)
                .ThenByDescending(l => l.IsNPCCruiser)
                .ThenByDescending(l => l.IsNPCFrigate && 100 > l.HealthPct && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsLowestHealthNpcWithThisSameName)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWithinOptimalOfDrones)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsCloseToDrones)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(l => l.IsNPCFrigate)
                .ThenByDescending(i => i.IsEntityIShouldKeepShooting).ToList();

            if (DebugConfig.DebugLogOrderOfDroneTargets)
                LogOrderOfDroneTargets(droneTargets);

            if (droneTargets != null && droneTargets.Any(i => i.IsLowestHealthNpcWithThisSameName))
            {
                droneTargets = droneTargets.Where(i => i.IsReadyForDronesToShoot && i.IsLowestHealthNpcWithThisSameName).ToList();
            }

            if (droneTargets != null && droneTargets.Count > 0)
                if (droneTargets.Any(i => i.IsEntityDronesAreShooting) && droneTargets.Any())
                    if (droneTargets.Find(i => i.IsEntityDronesAreShooting).Name == droneTargets.FirstOrDefault().Name)
                        return droneTargets.Find(i => i.IsEntityDronesAreShooting);

            _pickDroneTarget_cached = droneTargets.FirstOrDefault();
            return _pickDroneTarget_cached;
        }

        public static EntityCache PickDroneTarget_DronesKillHighValueTargets()
        {
            if (_pickDroneTarget_cached != null)
                return _pickDroneTarget_cached;

            List<EntityCache> droneTargets = Combat.PotentialCombatTargets.Where(i => i.IsReadyForDronesToShoot)
                .OrderByDescending(l => !l.WeShouldFocusFire && l.IsKillTarget)
                .ThenByDescending(a => a.IsWarpScramblingMe)
                .ThenByDescending(b => b.WarpScrambleChance)
                .ThenByDescending(d => d.IsPreferredPrimaryWeaponTarget)
                .ThenByDescending(e => e.IsLargeCollidableWeAlwaysWantToBlowupFirst)
                .ThenByDescending(e => e.IsTargetedBy)
                .ThenByDescending(f => f.IsAttacking)
                .ThenByDescending(h => h.IsHighValueTarget)
                .ThenByDescending(j => j.IsNPCBattleship)
                .ThenByDescending(k => k.IsNPCBattlecruiser)
                .ThenByDescending(l => l.IsNPCCruiser)
                .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                .ThenByDescending(o => Combat._pickPrimaryWeaponTarget != null && o != Combat._pickPrimaryWeaponTarget && o.IsNPCFrigate && !o.IsWarpScramblingMe)
                .ThenByDescending(e => e.IsLargeCollidableWeAlwaysWantToBlowupLast)
                .ThenBy(p => p.StructurePct)
                .ThenBy(q => q.ArmorPct)
                .ThenBy(r => r.ShieldPct)
                .ThenBy(s => s.Nearest5kDistance).ToList();

            if (DebugConfig.DebugLogOrderOfDroneTargets)
                LogOrderOfDroneTargets(droneTargets);

            _pickDroneTarget_cached = droneTargets.FirstOrDefault();
            return _pickDroneTarget_cached;
        }

        public static void ProcessState()
        {
            try
            {
                if (!OnEveryDroneProcessState()) return;

                switch (State.CurrentDroneControllerState)
                {
                    case DroneControllerState.WaitingForTargets:
                        if (!WaitingForTargetsDroneState()) break;
                        break;

                    case DroneControllerState.Launch:
                        if (!LaunchDronesState()) break;
                        break;

                    case DroneControllerState.Launching:
                        if (!LaunchingDronesState()) break;
                        break;

                    case DroneControllerState.OutOfDrones:
                        if (!OutOfDronesDronesState()) break;
                        break;

                    case DroneControllerState.Fighting:
                        if (!FightingDronesState()) break;
                        break;

                    case DroneControllerState.Recalling:
                        if (!RecallDrones()) break;

                        if (ActiveDroneCount == 0)
                        {
                            _lastRecall = DateTime.UtcNow;
                            _nextDroneAction = DateTime.UtcNow.AddSeconds(3);
                            _lastDroneCount = 0;
                            if (!UseDrones)
                            {
                                ChangeDroneControllerState(DroneControllerState.Idle);
                                break;
                            }

                            ChangeDroneControllerState(DroneControllerState.WaitingForTargets);
                            break;
                        }

                        break;

                    case DroneControllerState.Idle:
                        if (!IdleDroneState()) break;
                        break;
                }

                _activeDronesShieldTotalOnLastPulse = GetActiveDroneShieldTotal();
                _activeDronesArmorTotalOnLastPulse = GetActiveDroneArmorTotal();
                _activeDronesStructureTotalOnLastPulse = GetActiveDroneStructureTotal();
                _activeDronesShieldPercentageOnLastPulse = GetActiveDroneShieldPercentage();
                _activeDronesArmorPercentageOnLastPulse = GetActiveDroneArmorPercentage();
                _activeDronesStructurePercentageOnLastPulse = GetActiveDroneStructurePercentage();
                _lastDroneCount = ActiveDroneCount;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool RecallDrones()
        {
            if (ActiveDroneCount == 0)
            {
                _lastDroneCount = 0;
                return true;
            }

            var dronesReturningForLongerThan10Sec = AllDronesInSpace.Where(e => DroneReturningSinceSeconds(e.Id) > 10);

            if (dronesReturningForLongerThan10Sec.Any() && DirectEve.Interval(5000))
            {
                foreach (var drone in dronesReturningForLongerThan10Sec)
                {
                    Log.WriteLine($"--- Drone [{drone.TypeName}] Id {drone.Id} is being recalled for longer than 10 seconds.");
                }
            }

            if (DateTime.UtcNow.Subtract(_lastRecallCommand).TotalSeconds > Time.Instance.RecallDronesDelayBetweenRetries + ESCache.Instance.RandomNumber(0, 2))
            {
                //ESCache.Instance.DirectEve.ActiveShip.ReturnDronesToBay(dronesToRecall.Select(e => e.Id).ToList());

                if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnToBay))
                    try
                    {
                        double FarthestDroneDistance = 0;
                        if (ActiveDroneCount > 0)
                        {
                            FarthestDroneDistance = Math.Round(ActiveDrones.OrderByDescending(i => i.Distance).FirstOrDefault().Distance / 1000, 0);
                        }

                        Log.WriteLine("[" + ActiveDrones.Count + "] Drones Returning to Bay from [" + FarthestDroneDistance + "] k away");
                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                        _lastRecallCommand = DateTime.UtcNow;
                        return true;
                    }
                    catch (Exception)
                    {
                    }

                return false;
            }

            return true;
        }

        public static void RemovedDronePriorityTargetsByName(string stringEntitiesToRemove)
        {
            try
            {
                List<EntityCache> entitiesToRemove = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == stringEntitiesToRemove.ToLower()).ToList();
                if (entitiesToRemove.Count > 0)
                {
                    Log.WriteLine("removing [" + stringEntitiesToRemove + "] from the DPT List");
                    RemoveDronePriorityTargets(entitiesToRemove);
                    return;
                }

                Log.WriteLine("[" + stringEntitiesToRemove + "] was not found on grid");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool RemoveDronePriorityTargets(List<EntityCache> targets)
        {
            try
            {
                if (targets.Count > 0 && _dronePriorityTargets != null && _dronePriorityTargets.Count > 0 &&
                    _dronePriorityTargets.Any(pt => targets.Any(t => t.Id == pt.EntityID)))
                {
                    _dronePriorityTargets.RemoveAll(pt => targets.Any(t => t.Id == pt.EntityID));
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

        private static EntityCache _entityToAssistDronesTo { get; set; }

        private static EntityCache EntityToAssistDronesTo
        {
            get
            {
                if (_entityToAssistDronesTo == null)
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("if (entityToAssistDronesTo == null)");
                    if (ESCache.Instance.MyCorpMatesAsEntities.Count > 0)
                    {
                        if (ESCache.Instance.EveAccount.AssistMyDronesTo == ESCache.Instance.CharName)
                            return null;

                        if (DebugConfig.DebugDrones) Log.WriteLine("if (ESCache.Instance.MyCorpMatesAsEntities.Any())");
                        if (ESCache.Instance.MyCorpMatesAsEntities.Any(i => i.Name == ESCache.Instance.EveAccount.AssistMyDronesTo))
                        {
                            if (DebugConfig.DebugDrones) Log.WriteLine("if (ESCache.Instance.MyCorpMatesAsEntities.Any(i => i.Name == ESCache.Instance.EveAccount.AssistMyDronesTo))");
                            _entityToAssistDronesTo = ESCache.Instance.MyCorpMatesAsEntities.Find(i => i.Name == ESCache.Instance.EveAccount.AssistMyDronesTo);
                            if (_entityToAssistDronesTo != null)
                            {
                                if (DebugConfig.DebugDrones) Log.WriteLine("entityToAssistDronesTo [" + _entityToAssistDronesTo.Name + "][" + Math.Round(_entityToAssistDronesTo.Distance / 1000, 0) + "]");
                                return _entityToAssistDronesTo;
                            }

                            return null;
                        }

                        return null;
                    }

                    Log.WriteLine("ESCache.Instance.MyCorpMatesAsEntities == 0");
                    return null;
                }

                return _entityToAssistDronesTo;
            }
        }

        private static bool ShouldWeSendDronesToAssist()
        {
            try
            {
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) ||
                    ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                {
                    if (ActiveDrones.Any(i => 3000 > i.Distance))
                    {
                        if (DebugConfig.DebugDrones) Log.WriteLine("ShouldWeSendDronesToAssist: ActiveDrones are within 3k of my ship (assumed to be not assisting)");
                        if (EntityToAssistDronesTo == null)
                        {
                            Log.WriteLine("ShouldWeSendDronesToAssist: AssistMyDronesTo [" + ESCache.Instance.EveAccount.AssistMyDronesTo + "] is not yet on grid with us: waiting.");
                            LastDroneAssistCmd = DateTime.UtcNow;
                            return false;
                        }

                        if (DebugConfig.DebugDrones) Log.WriteLine("ShouldWeSendDronesToAssist: if (EntityToAssistDronesTo != null) ...");
                        if (DebugConfig.DebugDrones) Log.WriteLine("ShouldWeSendDronesToAssist: EntityToAssistDronesTo [" + EntityToAssistDronesTo.Name + "][" + Math.Round(EntityToAssistDronesTo.Distance/1000, 0) + "k]");
                        if (EntityToAssistDronesTo != null)
                        {
                            //
                            // AssistDrones Here
                            //

                            if (DebugConfig.DebugDrones) Log.WriteLine("ShouldWeSendDronesToAssist: if (EntityToAssistDronesTo != null)");
                            if (DateTime.UtcNow > LastDroneAssistCmd.AddSeconds(ESCache.Instance.RandomNumber(60, 120)))
                            {
                                Log.WriteLine("ShouldWeSendDronesToAssist: Attempt to Assist drones to [" + EntityToAssistDronesTo.Name + "]");

                                if (EntityToAssistDronesTo.CharacterId == null)
                                {
                                    Log.WriteLine("ShouldWeSendDronesToAssist: if (EntityToAssistDronesTo.CharacterId == null)");
                                    return true;
                                }

                                if (EntityToAssistDronesTo.CharacterId != null && EntityToAssistDronesTo._directEntity.SendDronesToAssist((long)EntityToAssistDronesTo.CharacterId))
                                {
                                    Log.WriteLine("ShouldWeSendDronesToAssist: Assisting Drones to [" + ESCache.Instance.EveAccount.AssistMyDronesTo + "] success");
                                    LastDroneAssistCmd = DateTime.UtcNow;
                                    return true;
                                }
                            }

                            return true;
                        }

                        if (DebugConfig.DebugDrones) Log.WriteLine("ShouldWeSendDronesToAssist: if (EntityToAssistDronesTo is null)!!!");
                        return true;
                    }

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

        //ActiveDrones.All(i => i.FollowId == PreferredDroneTargetID && (i.Mode == 1 || i.Mode == 6 || i.Mode == 10))
        private static EntityCache EntityDronesAreShooting
        {
            get
            {
                try
                {
                    if (ActiveDrones.Any(i => i.FollowId != 0))
                    {
                        long FollowIdToFind = ActiveDrones.FirstOrDefault().FollowId;

                        EntityCache _entityDronesAreShooting = Combat.PotentialCombatTargets.FirstOrDefault(i => i.Id == FollowIdToFind);
                        if (_entityDronesAreShooting != null && (_entityDronesAreShooting.Mode == 1 || _entityDronesAreShooting.Mode == 6 || _entityDronesAreShooting.Mode == 10))
                        {
                            return _entityDronesAreShooting ?? null;
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
        private static void EngageTarget()
        {
            try
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("Entering EngageTarget()");

                if (DebugConfig.DebugDrones && State.CurrentQuestorState != QuestorState.CombatMissionsBehavior)
                    Log.WriteLine("MaxDroneRange [" + Math.Round(MaxDroneRange, 0) + "] lowValueTargetTargeted [" + Combat.LowValueTargetsTargeted.Count +
                                  "] LVTT InDroneRange [" +
                                  Combat.LowValueTargetsTargeted.Count(i => i.Distance < MaxDroneRange) + "] highValueTargetTargeted [" +
                                  Combat.HighValueTargetsTargeted.Count + "] HVTT InDroneRange [" +
                                  Combat.HighValueTargetsTargeted.Count(i => i.Distance < MaxDroneRange) + "]");

                if (ShouldWeSendDronesToAssist()) return;

                //if (_pickDroneTarget != null && (!_pickDroneTarget.Exists || !_pickDroneTarget.IsTarget || !_pickDroneTarget.IsInDroneRange || !_pickDroneTarget.IsCloseToDrones || Time.Instance.LastJumpAction.AddSeconds(45) > DateTime.UtcNow))
                //    _pickDroneTarget = null;

                if (State.CurrentHydraState == HydraState.Combat)
                    if (ESCache.Instance.EveAccount.LeaderIsAggressingTargetId != 0)
                        foreach (EntityCache entity in ESCache.Instance.Targets)
                            if (entity.Id == ESCache.Instance.EveAccount.LeaderIsAggressingTargetId)
                            {
                                _cachedDroneTarget = entity;
                                break;
                            }

                if (_cachedDroneTarget == null || !_cachedDroneTarget.IsTarget)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("PreferredDroneTarget is null, picking a target using a simple rule set...MaxDroneRange [" + Math.Round(MaxDroneRange, 0) + "]");
                    if (ESCache.Instance.Targets.Any(i => i.IsReadyForDronesToShoot))
                        PickDroneTarget();
                }

                if (_cachedDroneTarget != null)
                {
                    if (_cachedDroneTarget.IsReadyForDronesToShoot)
                    {
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine(
                                "if (DroneToShoot != null && DroneToShoot.IsReadyToShoot && DroneToShoot.Distance < Cache.Instance.MaxDroneRange)");

                        // if there are not any NPC drones of high damage cruisers that may pull aggro (like Abyssal sites!) keep us from spamming engage drones too often!
                        if (!SpamDronesEngage)
                            if (LastTargetIDDronesEngaged != null)
                                if (LastTargetIDDronesEngaged == _cachedDroneTarget.Id &&
                                    ActiveDrones.All(i => i.FollowId == PreferredDroneTargetID && (i.Mode == 1 || i.Mode == 6 || i.Mode == 10)))
                                {
                                    if (DebugConfig.DebugDrones)
                                        Log.WriteLine("if (LastDroneTargetID [" + LastTargetIDDronesEngaged + "] == droneTarget.Id [" + _cachedDroneTarget.Id +
                                                      "] && Cache.Instance.ActiveDrones.Any(i => i.FollowId != Cache.Instance.PreferredDroneTargetID) [" +
                                                      ActiveDrones.Any(i => i.FollowId != PreferredDroneTargetID) + "]) - no need to send other drone commands right now.");

                                    if (!ActiveDrones.All(i => i._directEntity.DirectAbsolutePosition.GetDistance(_cachedDroneTarget._directEntity.DirectAbsolutePosition) > 10000))
                                    {
                                        if (DebugConfig.DebugDrones) Log.WriteLine("if (!ActiveDrones.All(i => i.DistanceFromEntity(droneTarget) > (i.OptimalRange * 1.5)))");
                                        return;
                                    }

                                    if (DebugConfig.DebugDrones) Log.WriteLine("if (ActiveDrones.Any(i => i.DistanceFromEntity(droneTarget) > (i.OptimalRange * 1.5)))");
                                }

                        try
                        {
                            int activedronenum = 0;
                            foreach (EntityCache activedrone in ActiveDrones)
                            {
                                activedronenum++;
                                if (activedrone.FollowId == 0)
                                {
                                    if (DebugConfig.DebugDrones) Log.WriteLine("activedrone [" + activedronenum + "] FollowID is 0");
                                    LastDroneFightCmd = DateTime.MinValue;
                                }
                                else if (activedrone.Mode == 0)
                                {
                                    if (DebugConfig.DebugDrones) Log.WriteLine("activedrone [" + activedronenum + "] Mode is [" + activedrone.Mode + "].");
                                    LastDroneFightCmd = DateTime.MinValue;
                                }
                                else if (activedrone.Mode != 1 && activedrone.Mode != 4 && activedrone.Mode != 6 && activedrone.Mode != 10)
                                {
                                    if (DebugConfig.DebugDrones) Log.WriteLine("activedrone [" + activedronenum + "] Mode is [" + activedrone.Mode + "]");
                                    LastDroneFightCmd = DateTime.MinValue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        if (DebugConfig.DebugDrones) Log.WriteLine("if(droneTarget.IsActiveTarget)");
                        // if there are not any NPC drones of high damage cruisers that may pull aggro (like Abyssal sites!) keep us from spamming engage drones too often!
                        if (LastTargetIDDronesEngaged == null || LastTargetIDDronesEngaged != _cachedDroneTarget.Id || SpamDronesEngage)
                        {
                            if (DebugConfig.DebugDrones) Log.WriteLine("if (LastTargetIDDronesEngaged == null || LastTargetIDDronesEngaged != droneTarget.Id)");
                            if (DateTime.UtcNow > LastDroneFightCmd.AddMilliseconds(SpamDronesEngageDelayInMilliSeconds))
                            {
                                if (DebugConfig.DebugDrones) Log.WriteLine("if (LastDroneFightCmd.AddSeconds(10) < DateTime.UtcNow)");

                                if (_cachedDroneTarget.Distance > MaxDroneRange)
                                    return;

                                //if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesEngage))
                                if (_cachedDroneTarget._directEntity.EngageTargetWithDrones(ActiveDroneIds))
                                {
                                    if (DronesKillHighValueTargets && _cachedDroneTarget != null && _cachedDroneTarget.Id != 0)
                                    {
                                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(ESCache.Instance.EveAccount.LastEntityIdEngaged), _cachedDroneTarget.Id);
                                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(ESCache.Instance.EveAccount.DateTimeLastEntityIdEngaged), DateTime.UtcNow);
                                    }

                                    Log.WriteLine("Engaging [ " + ActiveDrones.Count + " ] drones on [" + _cachedDroneTarget.Name + "][" + _cachedDroneTarget.TypeName + "] TypeId [" + _cachedDroneTarget.TypeId + "] GroupId [" + _cachedDroneTarget.GroupId + "][ID: " +
                                                    _cachedDroneTarget.MaskedId + "]" +
                                                    Math.Round(_cachedDroneTarget.Distance / 1000, 0) + "k away] IsTargetWithin10KOfOurDrones [" + _cachedDroneTarget.IsWithin10KOfOurDrones + "] IsTargetWithin15KOfOurDrones [" + _cachedDroneTarget.IsWithin15KOfOurDrones + "] IsTargetWithin20KOfOurDrones [" + _cachedDroneTarget.IsWithin20KOfOurDrones + "] EWAR [" + _cachedDroneTarget.stringEwarTypes + "]");
                                    LastDroneFightCmd = DateTime.UtcNow;
                                    LastTargetIDDronesEngaged = _cachedDroneTarget.Id;
                                }
                            }
                        }

                        return;
                    }

                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("DroneToShoot.IsReadyForDronesToShoot [" + _cachedDroneTarget.IsReadyForDronesToShoot + "] droneTarget.Distance [" + _cachedDroneTarget.Distance + "] < Cache.Instance.MaxDroneRange)");
                    return;
                }

                if (DebugConfig.DebugDrones)
                    Log.WriteLine("if (droneTarget != null)");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool FightingDronesState()
        {
            if (DebugConfig.DebugDrones)
                Log.WriteLine("Should we recall our drones? This is a possible list of reasons why we should");

            if (ActiveDroneCount == 0)
            {
                _lastDroneCount = 0;
                Log.WriteLine("Apparently we have lost all our drones");
                ChangeDroneControllerState(DroneControllerState.Idle);
                return false;
            }

            if (Combat.PotentialCombatTargets.Any(pt => pt.IsWarpScramblingMe))
            {
                EntityCache WarpScrambledBy = ESCache.Instance.Targets.OrderBy(d => d.Nearest5kDistance).ThenByDescending(i => i.IsWarpScramblingMe).FirstOrDefault();
                if (WarpScrambledBy != null && DateTime.UtcNow > _nextWarpScrambledWarning)
                {
                    _nextWarpScrambledWarning = DateTime.UtcNow.AddSeconds(20);
                    Log.WriteLine("We are scrambled by: [" + WarpScrambledBy.Name + "][" +
                                  Math.Round(WarpScrambledBy.Distance/ 1000, 0) + "k][" + WarpScrambledBy.Id +
                                  "]");
                }
            }

            if (ShouldWeRecallDrones())
            {
                Statistics.DroneRecalls++;
                //LastRecallDrones = DateTime.UtcNow;
                ChangeDroneControllerState(DroneControllerState.Recalling);
                return true;
            }

            if (DebugConfig.DebugDrones) Log.WriteLine("EngageTarget(); - before");

            EngageTarget();

            if (DebugConfig.DebugDrones) Log.WriteLine("EngageTarget(); - after");
            if (ActiveDroneCount < MaxDronesAllowedInSpace)
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("Drones: EngageTarget: We have [" + ActiveDroneCount + "] activeDrones and can have up to [" + MaxDronesAllowedInSpace + "] ShouldWeLaunchDrones?");
                if (!ShouldWeLaunchDrones()) return false;

                _launchTries = 0;
                _lastLaunch = DateTime.UtcNow;
                ChangeDroneControllerState(DroneControllerState.Launch, false);
                return true;
            }

            return true;
        }

        private static double GetActiveDroneArmorPercentage()
        {
            if (ActiveDroneCount == 0)
                return 0;

            return ActiveDrones.Sum(d => d.ArmorPct * 100);
        }

        private static double GetActiveDroneArmorTotal()
        {
            if (ActiveDroneCount == 0)
                return 0;

            if (ActiveDrones.Any(i => i.ArmorPct * 100 < 100))
                ESCache.Instance.NeedRepair = true;

            return ActiveDrones.Sum(d => d.ArmorMaxHitPoints);
        }

        private static double GetActiveDroneShieldPercentage()
        {
            if (ActiveDroneCount == 0)
                return 0;

            return ActiveDrones.Sum(d => d.ShieldPct * 100);
        }

        private static double GetActiveDroneShieldTotal()
        {
            if (ActiveDroneCount == 0)
                return 0;

            return ActiveDrones.Sum(d => d.ShieldMaxHitPoints);
        }

        private static double GetActiveDroneStructurePercentage()
        {
            if (ActiveDroneCount == 0)
                return 0;

            return ActiveDrones.Sum(d => d.StructurePct * 100);
        }

        private static double GetActiveDroneStructureTotal()
        {
            if (ActiveDroneCount == 0)
                return 0;

            if (ActiveDrones.Any(i => i.StructurePct * 100 < 100))
                ESCache.Instance.NeedRepair = true;

            return ActiveDrones.Sum(d => d.StructureMaxHitPoints);
        }

        private static bool IdleDroneState()
        {
            try
            {
                if (!ESCache.Instance.InSpace)
                    return false;

                if (ESCache.Instance.ActiveShip.Entity == null)
                    return false;

                if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
                    return false;

                if (!UseDrones)
                    return false;

                if (ESCache.Instance.InWarp)
                    return false;

                if (!ESCache.Instance.ActiveShip.GivenName.ToLower().Equals(Combat.CombatShipName.ToLower()))
                    return false;


                if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.HydraController))
                {
                    if (State.CurrentHydraState == HydraState.Combat)
                    {
                        ChangeDroneControllerState(DroneControllerState.WaitingForTargets, false);
                        return true;
                    }

                    return false;
                }

                if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                {
                    if (Drones.AllDronesInSpace.Any())
                    {
                        ChangeDroneControllerState(DroneControllerState.WaitingForTargets, false);
                        return true;
                    }

                    return false;
                }

                if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController))
                {
                    if (Drones.AllDronesInSpace.Any())
                    {
                        ChangeDroneControllerState(DroneControllerState.WaitingForTargets, false);
                        return true;
                    }

                    return false;
                }
                /**

                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    ChangeDroneControllerState(DroneControllerState.WaitingForTargets, false);
                    return true;
                }
                **/

                ChangeDroneControllerState(DroneControllerState.WaitingForTargets, false);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool LaunchDronesState()
        {
            if (!WeHaveDronesInDroneBay())
            {
                Log.WriteLine("No Drones in DroneBay?! We have [" + Drones.ActiveDrones.Count + "] drones in space.");
                //ChangeDroneState(DroneState.Idle, false);
                //return false;
            }

            if (SendCommandToLaunchDrones())
            {
                Log.WriteLine("LaunchAllDrones");
                _launchTimeout = DateTime.UtcNow;
                ChangeDroneControllerState(DroneControllerState.Launching, false);
                return true;
            }

            return false;
        }

        private static bool UseThermalDrones
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                        return true;
                }

                return false;
            }
        }

        private static Dictionary<int, string> ThermalDrones = new Dictionary<int, string>
        {
            //{ 1, "Hobgoblin II" },
            //{ 2, "Hobgoblin I" },
            //{ 1, "Hammerhead II" },
            //{ 2, "Hammerhead I" },
            //{ 1, "Ogre II" },
            //{ 2, "Ogre I" },
        };

        private static Dictionary<int, string> ExplosiveDrones = new Dictionary<int, string>
        {
            //{ 1, "Warrior II" },
            //{ 2, "Warrior I" },
            //{ 1, "Valkyrie II" },
            //{ 2, "Valkyrie I" },
            //{ 1, "Berserker II" },
            //{ 2, "Berserker I" },
        };

        private static Dictionary<int, string> KineticDrones = new Dictionary<int, string>
        {
            //{ 1, "Hornet II" },
            //{ 2, "Hornet I" },
            //{ 1, "Vespa II" },
            //{ 2, "Vespa I" },
            //{ 1, "Wasp II" },
            //{ 2, "Wasp I" },
        };

        private static Dictionary<int, string> EmDrones = new Dictionary<int, string>
        {
            //{ 1, "Acolyte II" },
            //{ 2, "Acolyte I" },
            //{ 1, "Infiltrator II" },
            //{ 2, "Infiltrator I" },
            //{ 1, "Praetor II" },
            //{ 2, "Praetor I" },
        };

        /**
        private static List<DirectItem> ThermalDronesInDroneBay
        {
            get
            {
                if (DroneBay != null)
                {
                    if (DroneBay.Items != null && DroneBay.Items.Any())
                    {
                        List<DirectItem> _thermalDronesInDroneBay = new List<DirectItem>();
                        _thermalDronesInDroneBay = DroneBay.Items.Where(droneItem => ThermalDrones.ContainsKey(droneItem.TypeId)).ToList();
                        if (_thermalDronesInDroneBay != null && _thermalDronesInDroneBay.Any())
                        {
                            return _thermalDronesInDroneBay;
                        }
                    }

                    return new List<DirectItem>();
                }

                return new List<DirectItem>();
            }
        }
        **/

        private static bool UseKineticDrones
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    //if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                    //    return true;
                }

                return false;
            }
        }

        private static bool UseExplosiveDrones
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    //if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                    //    return true;
                }

                return false;
            }
        }

        private static bool UseEmDrones
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    //if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                    //    return true;
                }

                return false;
            }
        }

        private static bool SendCommandToLaunchDrones()
        {
            if (ESCache.Instance.ActiveShip == null) return false;
            if (Time.Instance.NextActivateAccelerationGate > DateTime.UtcNow)
                return false;

            if (Time.Instance.LastJumpAction.AddSeconds(6) > DateTime.UtcNow)
                return false;

            //if (UseThermalDrones && ThermalDronesInDroneBay.Any())
            //{
            //    ESCache.Instance.ActiveShip.LaunchDrones(ThermalDronesInDroneBay)
            //    return true;
            //}

            //if (!ESCache.Instance.ActiveShip.LaunchAllDrones()) return false;
            if (!ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdLaunchFavoriteDrones)) return false;
            ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;

            return true;
        }

        private static bool FightToTheDeath
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                    return true;

                if (ESCache.Instance.InWormHoleSpace)
                    return true;

                return false;
            }
        }

        private static bool PullDronesForDamage
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace && OurDronesHaveAHugeBonusToHitPoints)
                {
                    if  (ESCache.Instance.MyShipEntity.IsCruiser || ESCache.Instance.MyShipEntity.IsBattleship)
                    {
                        return false;
                    }

                    if (ESCache.Instance.MyShipEntity.IsFrigate)
                    {
                        //pull drones unless we have a Non-tower NPC not aggressing
                        if (Combat.PotentialCombatTargets.Where(x => !x.IsAbyssalDeadspaceDeviantAutomataSuppressor).Any(i => !i.IsTargetedBy))
                            return true;

                        return false;
                    }

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                        return false;
                }

                return true;
            }
        }

        private static bool PullDronesForDamageWhenADroneDies
        {
            get
            {
                //if (ESCache.Instance.InAbyssalDeadspace && OurDronesHaveAHugeBonusToHitPoints && Combat.PotentialCombatTargets.Any(i => !i.NpcHasRemoteRepair && !i.IsAttacking))
                //{
                //    return false;
                //}

                return true;
            }
        }

        private static bool LaunchingDronesState()
        {
            Log.WriteLine("Entering Launching State...");
            if (ActiveDroneCount == 0)
            {
                _lastDroneCount = 0;
                Log.WriteLine("No Drones in space yet. waiting. _launchTries [" + _launchTries + "]");
                if (DateTime.UtcNow.Subtract(_launchTimeout).TotalSeconds >= 4)
                {
                    if (_launchTries < 5 || FightToTheDeath)
                    {
                        _launchTries++;
                        ChangeDroneControllerState(DroneControllerState.Launch);
                        return true;
                    }

                    ChangeDroneControllerState(DroneControllerState.OutOfDrones);
                }

                return true;
            }

            Log.WriteLine("[" + ActiveDrones.Count + "] Drones Launched");
            ChangeDroneControllerState(DroneControllerState.Fighting, false);
            return true;
        }

        private static void LogOrderOfDroneTargets(List<EntityCache> droneTargets)
        {
            int targetnum = 0;
            Log.WriteLine("----------------droneTargets------------------");
            foreach (EntityCache myDroneTarget in droneTargets)
            {
                targetnum++;
                Log.WriteLine(targetnum + ";" + myDroneTarget.Name + ";" + Math.Round(myDroneTarget.Distance / 1000, 0) + "k;" + myDroneTarget.IsNPCBattleship + ";BC;" + myDroneTarget.IsNPCBattlecruiser + ";C;" + myDroneTarget.IsNPCCruiser + ";F;" + myDroneTarget.IsNPCFrigate + ";isAttacking;" + myDroneTarget.IsAttacking + ";IsTargetedBy;" + myDroneTarget.IsTargetedBy + ";IsWarpScramblingMe;" + myDroneTarget.IsWarpScramblingMe + ";IsWebbingMe;" + myDroneTarget.IsWebbingMe + ";IsNeutralizingMe;" + myDroneTarget.IsNeutralizingMe + ";Health;" + myDroneTarget.HealthPct + ";ShieldPct;" + myDroneTarget.ShieldPct + ";ArmorPct;" + myDroneTarget.ArmorPct + ";StructurePct;" + myDroneTarget.StructurePct);
                if (myDroneTarget.Name.Contains("Damavik") && (myDroneTarget.IsWarpScramblingMe || myDroneTarget.IsWebbingMe))
                {
                    foreach (string attack in myDroneTarget._directEntity.Attacks)
                    {
                        Log.WriteLine("Attack from attacks [" + myDroneTarget.Name + "][" + myDroneTarget.MaskedId + "][" + myDroneTarget.Nearest1KDistance + "k][" + attack + "]");
                    }
                }
            }

            Log.WriteLine("----------------------------------------------");
        }

        private static bool OnEveryDroneProcessState()
        {
            try
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("Entering Drones.ProcessState: State.CurrentDroneControllerState [" + State.CurrentDroneControllerState + "] AllDronesInSpaceCount [" + Drones.AllDronesInSpaceCount + "]");

                if (ESCache.Instance.InStation || !ESCache.Instance.InSpace)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("InStation [" + ESCache.Instance.InStation + "] InSpace [" + ESCache.Instance.InSpace + "] - doing nothing");
                    ChangeDroneControllerState(DroneControllerState.Idle);
                    return false;
                }

                if (ESCache.Instance.MyShipEntity == null)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("if (ESCache.Instance.MyShipEntity == null) - doing nothing");
                    ChangeDroneControllerState(DroneControllerState.Idle);
                    return false;
                }

                if (ESCache.Instance.ActiveShip == null)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("if (ESCache.Instance.ActiveShip == null) - doing nothing");
                    ChangeDroneControllerState(DroneControllerState.Idle);
                    return false;
                }

                if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("ESCache.Instance.ActiveShip.Entity.IsCloaked [" + ESCache.Instance.ActiveShip.Entity.IsCloaked + "] - doing nothing");
                    ChangeDroneControllerState(DroneControllerState.Idle);
                    return false;
                }

                if (ESCache.Instance.ActiveShip.IsShipWithNoDroneBay)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("IsShipWithNoDroneBay [" + ESCache.Instance.ActiveShip.IsShipWithNoDroneBay + "] - doing nothing");
                    ChangeDroneControllerState(DroneControllerState.Idle);
                    return false;
                }

                if (ESCache.Instance.InsidePosForceField)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("ESCache.Instance.InsidePosForceField [" + ESCache.Instance.InsidePosForceField + "] - doing nothing");
                    ChangeDroneControllerState(DroneControllerState.Idle);
                    return false;
                }

                if (Time.Instance.LastJumpAction.AddSeconds(8) > DateTime.UtcNow)
                    return false;

                if (Time.Instance.LastDockAction.AddSeconds(8) > DateTime.UtcNow)
                    return false;

                if (Time.Instance.LastActivateFilamentAttempt.AddSeconds(8) > DateTime.UtcNow)
                    return false;

                if (Time.Instance.LastActivateKeyActivationWindowAttempt.AddSeconds(8) > DateTime.UtcNow)
                    return false;

                if (Time.Instance.LastActivateAccelerationGate.AddSeconds(8) > DateTime.UtcNow)
                    return false;

                if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.InWarp)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.InWarp) return false;");
                    return false;
                }

                //if (!UseDrones && ActiveDrones.Any())
                //{
                //    Log.WriteLine("UseDrones [" + UseDrones + "] - Recalling Drones");
                //    if (!RecallingDronesState()) return false;
                //    return false;
                //}

                if (!ESCache.Instance.InAbyssalDeadspace && ActiveDrones == null)
                {
                    Log.WriteLine("ActiveDrones == null");
                    ChangeDroneControllerState(DroneControllerState.Idle);
                    return false;
                }

                if (ESCache.Instance.ActiveShip.IsShipWithNoDroneBay)
                {
                    Log.WriteLine("IsShipWithNoDronesBay - Setting useDrones to false.");
                    ChangeDroneControllerState(DroneControllerState.Idle);
                    return false;
                }

                if (!UseDrones)
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("UseDrones [" + UseDrones + "].");
                    return false;
                }

                if (ESCache.Instance.InAbyssalDeadspace && Combat.PotentialCombatTargets.Any(i => i.IsTargetedBy && i.IsWarpScramblingMe))
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("We are still scrambled!");
                    DronesShouldBePulled = false;
                    return true;
                }

                if (ESCache.Instance.SelectedController == "AbyssalDeadspaceController")
                {
                    if (!ESCache.Instance.InAbyssalDeadspace)
                        return false;

                    if (Statistics.StartedPocket.AddSeconds(12) > DateTime.UtcNow)
                        return false;
                }

                if ((!ESCache.Instance.InAbyssalDeadspace && ActiveDroneCount == 0 && ESCache.Instance.InWarp) || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("No Active Drones in space and we are InWarp - doing nothing");
                    RemoveDronePriorityTargets(DronePriorityEntities);
                    ChangeDroneControllerState(DroneControllerState.Idle);
                    return false;
                }

                if (ActiveDrones.Any() && ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                {
                    ShouldWeSendDronesToAssist();

                    if (State.CurrentDroneControllerState != DroneControllerState.Fighting)
                    {
                        if (DebugConfig.DebugDrones) Log.WriteLine("Change DroneControllerState to Fighting!");
                        ChangeDroneControllerState(DroneControllerState.Fighting);
                        return true;
                    }

                    return true;
                }

                if (ActiveDroneCount == 0 && State.CurrentDroneControllerState != DroneControllerState.Launching && State.CurrentDroneControllerState != DroneControllerState.Launch)
                {
                    if (ReEngageDronesIfNeeded)
                    {
                        if (DebugConfig.DebugDrones)
                        {
                            Log.WriteLine("if (ReEngageDronesIfNeeded)");
                        }
                    }

                    if (!ShouldWeLaunchDrones())
                    {
                        if (DebugConfig.DebugDrones)
                        {
                            Log.WriteLine("if (!ShouldWeLaunchDrones()) return false;");
                        }

                        return false;
                    }

                    _launchTries = 0;
                    _lastLaunch = DateTime.UtcNow;
                    ChangeDroneControllerState(DroneControllerState.Launch, false);
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool OutOfDronesDronesState()
        {
            if (UseDrones &&
                State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
            {
                if (Statistics.OutOfDronesCount >= 3)
                {
                    Log.WriteLine("We are Out of Drones! AGAIN - Headed back to base to stay!");
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    if (MissionSettings.MyMission != null)
                        MissionSettings.MissionCompletionErrors = 10;
                    Statistics.OutOfDronesCount++;
                }

                Log.WriteLine("We are Out of Drones! - Headed back to base to Re-Arm");
                State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                Statistics.OutOfDronesCount++;
                return true;
            }

            return true;
        }

        private static DateTime _lastDronesInSpace;

        private static DateTime LastDronesInSpace
        {
            get
            {
                if (ActiveDroneCount > 0)
                    _lastDronesInSpace = DateTime.UtcNow;

                return _lastDronesInSpace;
            }
        }

        public static Vec3 LastDronesNeedToBePulledPositionInSpace= new Vec3(0,0,0);

        private static bool? _shouldWeLaunchDrones = null;

        private static bool ShouldWeLaunchDrones()
        {
            if (_shouldWeLaunchDrones != null)
                return (bool)_shouldWeLaunchDrones;

            if (LastDronesInSpace.AddSeconds(2) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("if (LastDronesInSpace.AddSeconds(2) > DateTime.UtcNow) return false;");
                return false;
            }

            if (ESCache.Instance.MyShipEntity.GroupId == (int)Group.Capsule)
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("if (LastDronesInSpace.AddSeconds(2) > DateTime.UtcNow) return false;");
                return false;
            }

            if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                return false;

            int PocketLaunchDroneDelay = 10;
            if (Combat.PotentialCombatTargets.Any() && Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship || i.BracketType == BracketType.NPC_Battleship) >= 4)
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("if (Combat.PotentialCombatTargets.Any() && Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship || i.BracketType == BracketType.NPC_Battleship) >= 4) then PocketLaunchDroneDelay = 2;");
                PocketLaunchDroneDelay = 2;
                Drones.DronesShouldBePulled = false;
            }

            if (Combat.PotentialCombatTargets.All(i => i.IsTargetedBy))
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("All NPCs have us targeted: no need for any delay launching drones");
                PocketLaunchDroneDelay = 0;
                Drones.DronesShouldBePulled = false;
            }

            if (PocketLaunchDroneDelay > Math.Round(Math.Abs(DateTime.UtcNow.Subtract(Statistics.StartedPocket).TotalSeconds)))
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("if (PocketLaunchDroneDelay > Math.Round(Math.Abs(DateTime.UtcNow.Subtract(Statistics.StartedPocket).TotalSeconds)))");
                return false;
            }

            if (UseDrones && !ESCache.Instance.InAbyssalDeadspace)
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("if (UseDrones && !ESCache.Instance.InAbyssalDeadspace)");
                if (State.CurrentPanicState != PanicState.Normal && State.CurrentPanicState != PanicState.Idle)
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("if (State.CurrentPanicState != PanicState.Normal && State.CurrentPanicState != PanicState.Idle) return true;");
                    return true;
                }
            }

            if (Time.Instance.NextActivateAccelerationGate > DateTime.UtcNow)
            {
                if (DebugConfig.DebugDrones)
                    Log.WriteLine("ShouldWeLaunchDrones: if (DateTime.UtcNow > Time.Instance.NextActivateAction)");
                return false;
            }

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.Undecided && Time.Instance.LastJumpAction.AddSeconds(22) > DateTime.UtcNow)
            {
                Log.WriteLine("ShouldWeLaunchDrones: if (DateTime.UtcNow > Time.Instance.LastJumpAction)");
                return false;
            }

            if (Combat.PotentialCombatTargets.All(pt => !pt.IsWarpScramblingMe))
            {
                if (!UseDrones)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("UseDrones is [" + UseDrones + "] Not Launching Drones");
                    return false;
                }

                /**
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && !i.WillYellowBoxButPosesLittleThreatToDrones && (!i.IsAttacking || !i.IsTargetedBy)))
                        {
                            if (DebugConfig.DebugDrones)
                                Log.WriteLine("ShouldWeLaunchDrones: if (Combat.PotentialCombatTargets.Where(i => i.IsNPCBattlecruiser && !i.WillYellowBoxButPosesLittleThreatToDrones).Any(i => !i.IsAttacking || !i.IsTargetedBy))");
                            return false;
                        }
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.WeShouldFocusFire))
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser && i.WeShouldFocusFire && (!i.IsAttacking || !i.IsTargetedBy)) > 1)
                        {
                            if (DebugConfig.DebugDrones)
                                Log.WriteLine("ShouldWeLaunchDrones: if (Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser && i.WeShouldFocusFire).Count(i => !i.IsAttacking || !i.IsTargetedBy) > 1)");
                            return false;
                        }
                    }

                    if (Combat.PotentialCombatTargets.Any() && !AbyssalDeadspaceBehavior.TriglavianConstructionSiteSpawnFoundDozenPlusBSs)
                    {
                        if (Combat.PotentialCombatTargets.Count(j => !j.WillYellowBoxButPosesLittleThreatToDrones && (!j.IsAttacking || !j.IsTargetedBy)) > 4)
                        {
                            if (DebugConfig.DebugDrones)
                                Log.WriteLine("ShouldWeLaunchDrones: if (Combat.PotentialCombatTargets.Where(j => !j.WillYellowBoxButPosesLittleThreatToDrones).Count(i => !i.IsAttacking || !i.IsTargetedBy) > 4)");
                            return false;
                        }
                    }
                }
                **/

                //if (MissionSettings.Agent != null && MissionSettings.Agent.IsValid && MissionSettings.Agent.IsMissionFinished) return false;

                //
                // The AOE explosion of the Pleasure Gardens in Damsel in Distress kills drones
                //
                if (ESCache.Instance.Targets.Any(i => i.Name.Contains("Pleasure Gardens") && .5 > i.StructurePct))
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("if (ESCache.Instance.Targets.Where(i => i.Name.Contains(Pleasure Gardens)).Any(i => .5 > i.StructurePct))");
                    return false;
                }

                try
                {
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.Contains("Patient Zero") && 60000 > i.Distance))
                    {
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine("if (ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.Contains(Patient Zero)).Any())");
                        return false;
                    }
                }
                catch (Exception ex) { }


                if (DronesShouldBePulled)
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("DronesShouldBePulled [" + DronesShouldBePulled + "] Not Launching Drones");
                    return false;
                }

                if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway && ESCache.Instance.AccelerationGates.Any(i => 15000 > i.Distance))
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("if (AbyssalDeadspaceBehavior.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && ESCache.Instance.AccelerationGates.Any(i => 15000 > i.Distance) && !Drones.ActiveDrones.Any())");
                    return false;
                }

                if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoBase && !ESCache.Instance.InAbyssalDeadspace)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("We are not scrambled and CurrentCombatMissionBehaviorState [" + State.CurrentCombatMissionBehaviorState + "] Not Launching Drones");
                    return false;
                }

                if (Combat.PotentialCombatTargets.Count == 0)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("We have no PotentialCombatTargets on grid");
                    return false;
                }

                if (Combat.PotentialCombatTargets.All(i => i.Distance > MaxDroneRange) && !ESCache.Instance.InAbyssalDeadspace)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("All PotentialCombatTargets are outside of your max drone range [" + Math.Round(MaxDroneRange / 1000, 0) + "k]");
                    return false;
                }

                if (State.CurrentHydraState == HydraState.Combat && ESCache.Instance.Targets.Count == 0 && ESCache.Instance.Targeting.Count == 0)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("if (State.CurrentHydraState == HydraState.Combat && !ESCache.Instance.Targets.Any() && !ESCache.Instance.Targeting.Any())");

                    return false;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) && ESCache.Instance.Targets.Count == 0 && ESCache.Instance.Targeting.Count == 0)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("if (ESCache.Instance.SelectedController == CombatDontMoveController &&  !ESCache.Instance.Targets.Any() && !ESCache.Instance.Targeting.Any()");

                    return false;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController) && ESCache.Instance.Targets.Count == 0 && ESCache.Instance.Targeting.Count == 0)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("if (ESCache.Instance.SelectedController == CombatDontMoveController &&  !ESCache.Instance.Targets.Any() && !ESCache.Instance.Targeting.Any()");

                    return false;
                }

                if (!Combat.Aggressed.Any(e => !e.IsSentry || (e.IsSentry && e.KillSentries) || (e.IsSentry && e.IsEwarTarget && e.Distance < MaxDroneRange)) &&
                    State.CurrentHydraState != HydraState.Combat && !ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.CombatDontMoveController) && ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.WspaceSiteController) && ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("We have nothing Aggressed; MaxDroneRange [" + MaxDroneRange + "] DroneControlrange [" + DroneControlRange +
                                      "] TargetingRange [" +
                                      Combat.MaxTargetRange + "]");
                    return false;
                }

                if (State.CurrentQuestorState != QuestorState.CombatMissionsBehavior && State.CurrentHydraState != HydraState.Combat && State.CurrentAbyssalDeadspaceBehaviorState != AbyssalDeadspaceBehaviorState.ExecuteMission && ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.CombatDontMoveController) && ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                    if (
                        !ESCache.Instance.EntitiesOnGrid.All(
                            e =>
                                ((!e.IsSentry && !e.IsBadIdea && e.CategoryId == (int) CategoryID.Entity && e.IsNpc && !e.IsContainer && !e.IsLargeCollidable) ||
                                 e.IsAttacking) && e.Distance < MaxDroneRange))
                    {
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine("QuestorState is [" + State.CurrentQuestorState + "] We have nothing to shoot;");
                        return false;
                    }

                if (!ESCache.Instance.InAbyssalDeadspace)
                {
                    if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp) return false;

                    //if (ESCache.Instance.AttemptingToWarp) return false;
                }

                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (!OurDronesHaveAHugeBonusToHitPoints && !ESCache.Instance.ActiveShip.IsFrigate && ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceDeviantAutomataSuppressor && 10000 > i.Distance))
                    {
                        Log.WriteLine("if (!OurDronesHaveAHugeBonusToHitPoints && ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceDeviantAutomataSuppressor && 40000 > i.Distance)) return false;");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count == 0)
                    {
                        if (DebugConfig.DebugDrones) Log.WriteLine("if (Combat.PotentialCombatTargets.Count == 0) return false;");
                        return false;
                    }

                    if (Time.Instance.NextActivateAccelerationGate.AddSeconds(17) > DateTime.UtcNow && Combat.PotentialCombatTargets.Count(i => !i.NpcHasRemoteRepair && !i.IsAttacking) >= 5)
                    {
                        if (DebugConfig.DebugDrones) Log.WriteLine("if (Time.Instance.NextActivateAction.AddSeconds(20) > DateTime.UtcNow && Combat.PotentialCombatTargets.Count(i => !i.NpcHasRemoteRepair && !i.IsAttacking) >= 5) return false;");
                        return false;
                    }
                }

                if (ActiveDroneCount == MaxDronesAllowedInSpace)
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("if (ActiveDroneCount == MaxDronesAllowedInSpace) return false;");
                    return false;
                }

                if (Combat.PotentialCombatTargets.All(i => i.IsTargetedBy) || ESCache.Instance.IsPVPGankLikely)
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("if (Combat.PotentialCombatTargets.All(i => i.IsTargetedBy)) return false;");
                    return true;
                }

                if (_lastLaunch < _lastRecall && _lastRecall.Subtract(_lastLaunch).TotalSeconds < 30)
                {
                    if (_lastRecall.AddSeconds(_recallCount + 1) < DateTime.UtcNow)
                    {
                        _recallCount++;

                        if (_recallCount > 4)
                            _recallCount = 4;

                        return true;
                    }

                    Log.WriteLine("Drones: ShouldWeLaunchDrones: We are still in _lastRecall delay.");
                    return false;
                }

                _recallCount = 0;
                return true;
            }

            return true;
        }

        private static bool ShouldWeRecallDrones()
        {
            try
            {
                int lowShieldWarning = LongRangeDroneRecallShieldPct;
                int lowArmorWarning = LongRangeDroneRecallArmorPct;
                int lowCapWarning = LongRangeDroneRecallCapacitorPct;

                if (ActiveDrones.Average(d => d.Distance) < MaxDroneRange / 2d)
                {
                    lowShieldWarning = DroneRecallShieldPct;
                    lowArmorWarning = DroneRecallArmorPct;
                    lowCapWarning = DroneRecallCapacitorPct;
                }

                if (!UseDrones)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count + " ] drones: UseDrones is [" + UseDrones + "]");
                    return true;
                }

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsWarpScramblingMe) && !ESCache.Instance.InAbyssalDeadspace)
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("Not recalling drones because we are scrambled");
                    return false;
                }

                if (State.CurrentHydraState != HydraState.Combat)
                    if (MissionSettings.IsMissionFinished) return true;

                if (ESCache.Instance.IsPVPGankLikely)
                {
                    if (DebugConfig.DebugDrones) Log.WriteLine("if (ESCache.Instance.IsPVPGankLikely) return false");
                    return false;
                }

                if (ESCache.Instance.InAbyssalDeadspace && (Combat.PotentialCombatTargets == null || Combat.PotentialCombatTargets.Count == 0))
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: No PotentialCombatTargets on grid");
                    return true;
                }

                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (DateTime.UtcNow > Time.Instance.LastActivateAccelerationGate.AddSeconds(30))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser && !i.WillYellowBoxButPosesLittleThreatToDrones && i.HealthPct > 10 && i.IsValid && (!i.IsAttacking || !i.IsTargetedBy)) >= AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalBCsToPullDrones)
                            {
                                Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: We have [" + AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalBCsToPullDrones + "]+ BCs (mean ones!) yellow boxing us");
                                return true;
                            }
                        }

                        //if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.WeShouldFocusFire))
                        //{
                        //if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser && i.WeShouldFocusFire && i.HealthPct > 10 && i.IsValid && (!i.IsAttacking || !i.IsTargetedBy)) > 1)
                        //{
                        //    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: We have 2+ Cruiser NPCs that WeShouldFocusFire on yellow boxing us");
                        //    return true;
                        //}
                        //}

                        if (Combat.PotentialCombatTargets.Any(i => i.Name == "Scylla Tyrannos"))
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.Name == "Scylla Tyrannos" && i.HealthPct > 10 && i.IsValid && (!i.IsAttacking || !i.IsTargetedBy)) >= AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones)
                            {
                                Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: We have [" + AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones + "]+ Scylla Tyrannos NPCs yellow boxing us");
                                return true;
                            }
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Ephialtes ")))
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Ephialtes ") && i.HealthPct > 10 && i.IsValid && (!i.IsAttacking || !i.IsTargetedBy)) >= AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones)
                            {
                                Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: We have [" + AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones + "]+ Ephialtes Cruiser NPCs yellow boxing us");
                                return true;
                            }
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && !i.NpcHasALotOfRemoteRepair && (i.IsWebbingMe || i.IsWarpScramblingMe) && i.HealthPct > 10 && i.IsValid && (!i.IsAttacking || !i.IsTargetedBy)) >= AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalFrigsToPullDrones)
                            {
                                if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.ConcordSpawn &&
                                    AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn &&
                                    AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                                {
                                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: We have [" + AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalFrigsToPullDrones + "]+ Frigate/Drone NPCs yellow boxing us");
                                    return true;
                                }
                            }
                        }

                        if (Combat.PotentialCombatTargets.Count > 0 && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs)
                        {
                            if (Combat.PotentialCombatTargets.Count(j => !j.WillYellowBoxButPosesLittleThreatToDrones && (!j.IsAttacking || !j.IsTargetedBy)) >= AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalNPCsToPullDrones)
                            {
                                if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.ConcordSpawn &&
                                    AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn &&
                                    AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                                {
                                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: We have [" + AbyssalDeadspaceBehavior.NumOfYellowBoxingAbyssalNPCsToPullDrones + "]+ NPCs yellow boxing us: and we have only been in pocket less than 20 sec: AbyssalSpawn.DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                                    return true;
                                }
                            }
                        }
                    }
                }

                //
                // The AOE explosion of the Pleasure Gardens in Damsel in Distress kills drones
                //
                if (ESCache.Instance.Targets.Any(i => i.Name.Contains("Pleasure Gardens") && .5 > i.StructurePct))
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: UseDrones is [" + UseDrones + "]");
                    return true;
                }

                int TargetedByInDroneRangeCount =
                    Combat.TargetedBy.Count(e => (!e.IsSentry || (e.IsSentry && e.KillSentries) || (e.IsSentry && e.IsEwarTarget)) && e.IsInDroneRange);
                if (TargetedByInDroneRangeCount == 0 && !ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy && i.IsWarpScramblingMe) && State.CurrentHydraState != HydraState.Combat && State.CurrentAbyssalDeadspaceBehaviorState != AbyssalDeadspaceBehaviorState.ExecuteMission && ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.CombatDontMoveController) && ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.WspaceSiteController) && ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                {
                    int TargtedByCount = 0;
                    if (Combat.TargetedBy.Count > 0)
                    {
                        TargtedByCount = Combat.TargetedBy.Count;
                        EntityCache __closestTargetedBy =
                            Combat.TargetedBy.OrderBy(i => i.Nearest5kDistance)
                                .FirstOrDefault(e => !e.IsSentry || (e.IsSentry && e.KillSentries) || (e.IsSentry && e.IsEwarTarget));
                        if (__closestTargetedBy != null)
                            Log.WriteLine("The closest target that is targeting ME is at [" + __closestTargetedBy.Distance + "]k");
                    }

                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: There are [" +
                                  Combat.PotentialCombatTargets.Count(e => e.IsInDroneRange) +
                                  "] PotentialCombatTargets not targeting us within My MaxDroneRange: [" + Math.Round(MaxDroneRange / 1000, 0) +
                                  "k] Targeting Range Is [" +
                                  Math.Round(Combat.MaxTargetRange / 1000, 0) + "k] We have [" + TargtedByCount + "] total things targeting us and [" +
                                  Combat.PotentialCombatTargets.Count +
                                  "] total PotentialCombatTargets");

                    if (DebugConfig.DebugDrones)
                        foreach (EntityCache PCTInDroneRange in Combat.PotentialCombatTargets.Where(i => i.IsInDroneRange && i.IsTargetedBy))
                            Log.WriteLine("Recalling Drones Details:  PCTInDroneRange [" + PCTInDroneRange.Name + "][" + PCTInDroneRange.MaskedId +
                                          "] at [" +
                                          Math.Round(PCTInDroneRange.Distance / 1000, 2) + "] not targeting us yet");

                    return true;
                }

                if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoBase && !ESCache.Instance.InAbyssalDeadspace)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: We changed states to Gotobase and we are not scrambled");
                    return true;
                }

                if (DronesShouldBePulled)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: DronesShouldBePulled [" + DronesShouldBePulled + "]");
                    return true;
                }

                if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: We are warping");
                    return true;
                }

                if (MissionSettings.MyMission != null && MissionSettings.MyMission.Name.ToLower().Contains("anomic team".ToLower()) && OurDronesHaveAHugeBonusToHitPoints && Combat.PotentialCombatTargets.Any(pt => (pt.Name.Contains("Enyo") || pt.Name.Contains("Vengeance") || pt.Name.Contains("Jaguar") || pt.Name.Contains("Hawk")) && !pt.IsAttacking && pt.IsTarget))
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: RecallDronesIfWeLoseAggroOnOurShip and we have a Potential Combat Target locked that is not aggressing us and will likely shoot drones soon!");
                    return true;
                }

                if (!DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive && ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.WspaceSiteController))
                {
                    if (ESCache.Instance.Targets == null || (ESCache.Instance.Targets.Count == 0 && State.CurrentHydraState == HydraState.Combat && !DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive))
                    {
                        Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: We have nothing targeted");
                        return true;
                    }
                }

                if (ActiveDrones.All(i => i.Distance > 500 && i.Velocity < 10 && 10 > i.HealthPct))
                {
                    Log.WriteLine("Not Recalling [ " + ActiveDroneCount + " ] drones: Drones are outside scoop range, less than 10% health and not moving! (should we abandon this drone?)");
                    return false;
                }

                if ((_activeDronesStructureTotalOnLastPulse > GetActiveDroneStructureTotal() + 5) && PullDronesForDamage)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: structure! [Old:" +
                                  _activeDronesStructureTotalOnLastPulse.ToString("N2") +
                                  "][New: " + GetActiveDroneStructureTotal().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesArmorTotalOnLastPulse > GetActiveDroneArmorTotal() + 5 && PullDronesForDamage)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: armor! [Old:" + _activeDronesArmorTotalOnLastPulse.ToString("N2") +
                                  "][New: " +
                                  GetActiveDroneArmorTotal().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesShieldTotalOnLastPulse > GetActiveDroneShieldTotal() + 5 && PullDronesForDamage)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: shields! [Old: " +
                                  _activeDronesShieldTotalOnLastPulse.ToString("N2") + "][New: " +
                                  GetActiveDroneShieldTotal().ToString("N2") + "]");
                    return true;
                }

                if ((_activeDronesStructurePercentageOnLastPulse > GetActiveDroneStructurePercentage() + 1) && PullDronesForDamage)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: structure! [Old:" +
                                  _activeDronesStructurePercentageOnLastPulse.ToString("N2") +
                                  "][New: " + GetActiveDroneStructurePercentage().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesArmorPercentageOnLastPulse > GetActiveDroneArmorPercentage() + 1 && PullDronesForDamage)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: armor! [Old:" +
                                  _activeDronesArmorPercentageOnLastPulse.ToString("N2") + "][New: " +
                                  GetActiveDroneArmorPercentage().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesShieldPercentageOnLastPulse > GetActiveDroneShieldPercentage() + 1 && PullDronesForDamage)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: shields! [Old: " +
                                  _activeDronesShieldPercentageOnLastPulse.ToString("N2") +
                                  "][New: " + GetActiveDroneShieldPercentage().ToString("N2") + "]");
                    return true;
                }

                //if (GetActiveDroneStructurePercentage() < 10)
                //{
                //    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: structure is very low! [" + GetActiveDroneShieldPercentage().ToString("N2") + "]");
                //    return true;
                //}

                if ((ActiveDroneCount < _lastDroneCount) && PullDronesForDamageWhenADroneDies)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones: We lost a drone! [Old:" + _lastDroneCount + "][New: " +
                                  ActiveDroneCount + "]");
                    return true;
                }

                if (!DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive && ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.WspaceSiteController))
                {
                    if (!ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (Combat.PotentialCombatTargets.Count > 0 && !Combat.PotentialCombatTargets.Any(i => i.IsTargeting || i.IsTarget))
                        {
                            Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones due to [" + ESCache.Instance.Targets.Count +
                                          "] targets being locked. Locking [" +
                                          ESCache.Instance.Targeting.Count + "] targets atm");
                            return true;
                        }
                    }
                }

                if (PullDronesForDamage && ESCache.Instance.ActiveShip.ArmorPercentage < lowArmorWarning && !ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy && i.IsWarpScramblingMe) && !ESCache.Instance.InAbyssalDeadspace)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones due to armor [" +
                                  Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) +
                                  "%] below [" + lowArmorWarning + "%] minimum");
                    return true;
                }

                if (PullDronesForDamage && ESCache.Instance.ActiveShip.ShieldPercentage < lowShieldWarning && !ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy && i.IsWarpScramblingMe) && !ESCache.Instance.InAbyssalDeadspace)
                {
                    Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones due to shield [" +
                                  Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) +
                                  "%] below [" + lowShieldWarning + "%] minimum");
                    return true;
                }

                if (ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.WSpaceScoutController) &&
                    ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.CombatDontMoveController)
                   )
                {
                    if (PullDronesForDamage && ESCache.Instance.ActiveShip.CapacitorPercentage < lowCapWarning && !ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy && i.IsWarpScramblingMe) && !ESCache.Instance.InAbyssalDeadspace)
                    {
                        Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones due to capacitor [" +
                                      Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) +
                                      "%] below [" + lowCapWarning + "%] minimum");
                        return true;
                    }
                }

                if (State.CurrentQuestorState == QuestorState.CombatMissionsBehavior && !ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy && i.IsWarpScramblingMe))
                {
                    if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoBase && !ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy && i.IsWarpScramblingMe) && !ESCache.Instance.InAbyssalDeadspace)
                    {
                        Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones due to gotobase state");
                        return true;
                    }

                    if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoMission && !ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy && i.IsWarpScramblingMe) && !ESCache.Instance.InAbyssalDeadspace)
                    {
                        Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones due to gotomission state");
                        return true;
                    }

                    if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Panic && !ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy && i.IsWarpScramblingMe) && !ESCache.Instance.InAbyssalDeadspace)
                    {
                        Log.WriteLine("Recalling [ " + ActiveDroneCount + " ] drones due to panic state");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool ShouldWeRecoverLostDrones()
        {
            if (State.CurrentQuestorState != QuestorState.CombatMissionsBehavior)
                return false;

            //if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
            //    return false;

            //if (!ESCache.Instance.InMission)
            //    return false;

            if (!UseDrones)
                return false;

            if (DronesShouldBePulled)
                return false;

            if (IsRecoverLostDronesAlreadyProcessedInThisPocket)
                return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) ||
                    ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
            {
                if (Combat.PotentialCombatTargets.Count == 0)
                    return true;

                if (ESCache.Instance.AccelerationGates.Count > 0)
                {
                    if (ESCache.Instance.AccelerationGates.Any(i => 2000 > i.Distance))
                        return true;
                }

                if (ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                    return true;
            }

            if (ESCache.Instance.Entities.Count > 0 && ESCache.Instance.Entities.Any(i => i.TypeId == DroneTypeID && i.Velocity == 0))
            {
                Log.WriteLine("Drones with typeID [" + DroneTypeID + "] were found on grid not moving.");
                if (ESCache.Instance.Entities.Any(i => i.TypeId == DroneTypeID && i.Velocity == 0 && i._directEntity.IsOwnedByMe))
                {
                    if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReconnectToDrones))
                    {
                        Log.WriteLine("Drones with typeID [" + DroneTypeID + "] were found on grid not moving and appear to be owned by us! Reconnecting to Drones");
                        IsRecoverLostDronesAlreadyProcessedInThisPocket = true;
                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                        return true;
                    }

                    return false;
                }

                return false;
            }

            return false;
        }

        private static bool WaitingForTargetsDroneState()
        {
            if (ActiveDroneCount > 0)
            {
                ChangeDroneControllerState(DroneControllerState.Fighting, false);
                return true;
            }

            if (ESCache.Instance.Targets.Any(i => !i.IsWreck) || (Combat.PotentialCombatTargets.Count > 0 && DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive))
            {
                if (!ShouldWeLaunchDrones()) return false;

                _launchTries = 0;
                _lastLaunch = DateTime.UtcNow;
                ChangeDroneControllerState(DroneControllerState.Launch, false);
                return true;
            }

            return true;
        }

        public static DirectInvType DroneInvType
        {
            get
            {
                return ESCache.Instance.DirectEve.GetInvType(DroneTypeID);
            }
        }

        public static int LostDrones
        {
            get
            {
                DirectInvType drone = ESCache.Instance.DirectEve.GetInvType(DroneTypeID);
                if (drone != null)
                {
                    if (DroneBay == null)
                    {
                        Log.WriteLine("if (Drones.DroneBay == null)");
                        return 0;
                    }

                    return (int)Math.Floor((DroneBay.Capacity - (double)DroneBay.UsedCapacity) / drone.Volume);
                }

                return 0;
            }
        }

        private static bool WeHaveDronesInDroneBay()
        {
            if (DroneBay != null && DroneBay.Items != null && DroneBay.Items.Count > 0)
                return true;

            return false;
        }

        #endregion Methods
    }
}