extern alias SC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.BackgroundTasks
{
    public static class NavigateOnGrid
    {
        #region Fields

        public static DateTime AvoidBumpingThingsTimeStamp = Time.Instance.Started_DateTime;
        public static bool AvoidBumpingThingsWarningSent;
        public static DateTime LastWarpScrambled = DateTime.UtcNow;
        public static int SafeDistanceFromStructureMultiplier = 1;
        public static long? StationIdToGoto;
        private static DateTime _nextWarpScrambledWarning = DateTime.UtcNow;
        private static int? _orbitDistance;
        private static int? _orbitDistanceToUse;
        private static EntityCache _stationToGoTo;
        public static DateTime NextAvoidBumpingThings { get; set; } = DateTime.UtcNow;
        public static DateTime NextNavigateIntoRange { get; set; } = DateTime.UtcNow;

        #endregion Fields

        #region Properties

        private static bool? _abyssalLargeAvoidBumpingThingsBool;

        private static bool? _abyssalMediumAvoidBumpingThingsBool;

        private static bool? _abyssalSmallAvoidBumpingThingsBool;

        private static int _intWeAreMovingSlowlyAgainAbyssalLarge;
        private static int _intWeAreMovingSlowlyAgainAbyssalMedium;
        private static int _intWeAreMovingSlowlyAgainAbyssalSmall;
        private static DateTime _nextAvoidBumpingThingsReset = DateTime.UtcNow;

        public static bool GlobalAvoidBumpingThingsBool { get; set; }

        public static int OptimalRange
        {
            get
            {
                try
                {
                    if (MissionSettings.MissionOptimalRange != null)
                        return (int) MissionSettings.MissionOptimalRange;

                    //
                    // This must mean we have drones only?!
                    //
                    if (Drones.UseDrones && Drones.DronesKillHighValueTargets)
                    {
                        _optimalRange = (int) Drones.MaxDroneRange;
                        _optimalRange = (int) Math.Min(Drones.MaxDroneRange, ESCache.Instance.ActiveShip.MaxTargetRange);
                        if (ESCache.Instance.WebRange != null)
                            _optimalRange = (int) Math.Min((double) _optimalRange, (double) ESCache.Instance.WebRange);

                        _optimalRange = (int)Math.Min((double)_optimalRange, (double)ESCache.Instance.WeaponRange);
                        return _optimalRange ?? 10000;
                    }

                    if (ESCache.Instance.Weapons.Count > 0)
                    {
                        ModuleCache weapon = ESCache.Instance.Weapons.FirstOrDefault();
                        if (weapon != null)
                        {
                            if (weapon.OptimalRange != 0)
                            {
                                if (Combat.Combat.DoWeCurrentlyProjectilesMounted())
                                {
                                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                                        return 800;

                                    _optimalRange = (int) weapon.OptimalRange + (int) (weapon.FallOff * .50);
                                    _optimalRange = (int) Math.Min((double) _optimalRange, ESCache.Instance.ActiveShip.MaxTargetRange);
                                    if (ESCache.Instance.WebRange != null)
                                        _optimalRange = (int) Math.Min((double) _optimalRange, (double) ESCache.Instance.WebRange);

                                    _optimalRange = (int)Math.Min((double)_optimalRange, (double)ESCache.Instance.WeaponRange);
                                    return _optimalRange ?? 10000;
                                }

                                //
                                // any type of turret: hybrid or lasers in this case
                                //
                                if (Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                                {
                                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                                        return 800;

                                    _optimalRange = (int) weapon.OptimalRange + (int) (weapon.FallOff * .50);
                                    _optimalRange = (int) Math.Min((double) _optimalRange, ESCache.Instance.ActiveShip.MaxTargetRange);
                                    if (ESCache.Instance.WebRange != null)
                                        _optimalRange = (int) Math.Min((double) _optimalRange, (double) ESCache.Instance.WebRange);

                                    _optimalRange = (int)Math.Min((double)_optimalRange, (double)ESCache.Instance.WeaponRange);
                                    return _optimalRange ?? 10000;
                                }
                            }

                            //
                            // If we have any weapons this means we have missiles: return 0 for missiles
                            //
                            return 0;
                        }

                        return 0;
                    }

                    return _optimalRange ?? 0;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 10000;
                }
            }
        }

        public static int OrbitDistance
        {
            get => _orbitDistance ?? 2000;
            set => _orbitDistance = value;
        }

        public static int OrbitDistanceToUse
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    AbyssalSpawn.AbyssalSpawnType AbyssalDetectSpawnResult = AbyssalSpawn.DetectSpawn;

                    if (AbyssalDetectSpawnResult != AbyssalSpawn.AbyssalSpawnType.Undecided)
                    {
                        switch (AbyssalDetectSpawnResult)
                        {
                            case AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn:
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                    return 2000;
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn:
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                    return 2000;
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn:
                                if (ESCache.Instance.ActiveShip.HasSpeedMod && Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                                {
                                    if (ESCache.Instance.ActiveShip.IsFrigate)
                                        return 16000;

                                    return 20000;
                                }

                                break;

                            case AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn:
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                    return 2000;
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.ConcordSpawn:
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                    return 500;
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.CruiserSpawn:
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                    return 500;
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.DevotedHunter:
                                if (Combat.Combat.PotentialCombatTargets.Any())
                                {
                                    if (ESCache.Instance.ActiveShip.IsFrigate)
                                    {
                                        //if (ESCache.Instance.Weapons.Any(i => i.IsEnergyWeapon))
                                        //{
                                        //    return 22000;
                                        //}

                                        return Math.Min(17000, ESCache.Instance.Weapons.FirstOrDefault(i => i.ChargeQty != 0).Charge.DefinedAsAmmoType.Range);
                                    }

                                    return Math.Min(30000, ESCache.Instance.Weapons.FirstOrDefault(i => i.ChargeQty != 0).Charge.DefinedAsAmmoType.Range);
                                }

                                break;

                            case AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn:
                                if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                {
                                    if (ESCache.Instance.ActiveShip.IsFrigate)
                                        return 500;

                                    return 50000;
                                }

                                break;

                            case AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn:
                                if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                {
                                    if (ESCache.Instance.ActiveShip.IsFrigate)
                                        return 2000;

                                    return 10000;
                                }

                                break;

                            case AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn:
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                    return 2000;
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn:
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                    return 2000;
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn:
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                    return 2000;
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn:
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                    return 2000;
                                break;
                        }
                    }
                }

                _orbitDistanceToUse = OrbitDistance;

                if (MissionSettings.MissionOrbitDistance != null)
                    _orbitDistanceToUse = (int) MissionSettings.MissionOrbitDistance;

                if (_orbitDistanceToUse == 0)
                    _orbitDistanceToUse = 1000;

                if (!ESCache.Instance.InAbyssalDeadspace)
                    if (_orbitDistanceToUse > ESCache.Instance.WeaponRange)
                        _orbitDistanceToUse = Math.Min(4000, ESCache.Instance.WeaponRange - 1000);

                if (Combat.Combat.PotentialCombatTargets.Count == 0)
                    _orbitDistanceToUse = 500;

                return _orbitDistanceToUse ?? 2000;
            }
        }

        public static bool GlobalOrbitStructure { get; set; }

        public static bool OrbitStructure
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (ActionControl.PerformingLootActionNow)
                    {
                        return false;
                    }

                    if (ESCache.Instance.MyShipEntity.IsFrigate && Combat.Combat.PotentialCombatTargets.Count > 0)
                    {
                        return false;
                    }

                    if (SpeedTank && Combat.Combat.PotentialCombatTargets.Count > 0)
                    {
                        return false;
                    }

                    return true;
                }

                return GlobalOrbitStructure;
            }
        }

        public static bool SpeedTank
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    AbyssalSpawn.AbyssalSpawnType AbyssalDetectSpawnResult = AbyssalSpawn.DetectSpawn;

                    if (AbyssalDetectSpawnResult != AbyssalSpawn.AbyssalSpawnType.Undecided)
                    {
                        switch (AbyssalDetectSpawnResult)
                        {
                            case AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn:
                                if (ESCache.Instance.ActiveShip.HasSpeedMod && Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 1)
                                {
                                    if (25 > ESCache.Instance.MyShipEntity.Velocity)
                                    {
                                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);
                                    }

                                    if (SpeedTankGlobalSetting != null)
                                        return (bool)SpeedTankGlobalSetting;

                                    return false;
                                }
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn:
                                if (ESCache.Instance.ActiveShip.HasSpeedMod && Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 1)
                                {
                                    if (25 > ESCache.Instance.MyShipEntity.Velocity)
                                    {
                                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);
                                    }

                                    if (SpeedTankGlobalSetting != null)
                                        return (bool)SpeedTankGlobalSetting;

                                    return false;
                                }
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn:
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.ConcordSpawn:
                                //speed tank if there is more than 1 Battleship
                                if (ESCache.Instance.ActiveShip.HasSpeedMod && Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                {
                                    if (25 > ESCache.Instance.MyShipEntity.Velocity)
                                    {
                                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);
                                    }

                                    if (SpeedTankGlobalSetting != null)
                                        return (bool)SpeedTankGlobalSetting;

                                    return false;
                                }
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.CruiserSpawn:
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn:
                                if (ESCache.Instance.ActiveShip.HasSpeedMod && Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                {
                                    if (25 > ESCache.Instance.MyShipEntity.Velocity)
                                    {
                                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);
                                    }

                                    if (SpeedTankGlobalSetting != null)
                                        return (bool)SpeedTankGlobalSetting;

                                    return false;
                                }

                                break;

                            case AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn:
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn:
                                break;

                            case AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn:
                                if (ESCache.Instance.ActiveShip.HasSpeedMod && Combat.Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Vedmak") && !i.Name.Contains("Vila")) >= 2)
                                {
                                    if (25 > ESCache.Instance.MyShipEntity.Velocity)
                                    {
                                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);
                                    }

                                    if (SpeedTankGlobalSetting != null)
                                        return (bool)SpeedTankGlobalSetting;

                                    return false;
                                }

                                break;

                            case AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn:
                                break;
                        }
                    }
                }

                if (SpeedTankMissionSetting != null)
                    return (bool)SpeedTankMissionSetting;

                if (SpeedTankGlobalSetting != null)
                    return (bool)SpeedTankGlobalSetting;

                return false;
            }
        }

        public static bool? SpeedTankGlobalSetting { get; set; }

        public static bool? SpeedTankMissionSetting { get; set; }

        public static EntityCache StationToGoTo
        {
            get
            {
                try
                {
                    if (StationIdToGoto == null)
                    {
                        if (DebugConfig.DebugTraveler) Log.WriteLine("stationToGoTo: if (stationIDToGoto == null)");
                        return null;
                    }

                    if (ESCache.Instance.DockableLocations.Count == 0)
                    {
                        if (DebugConfig.DebugTraveler) Log.WriteLine("stationToGoTo: No Stations");
                        return null;
                    }

                    if (ESCache.Instance.DockableLocations.Count > 0)
                    {
                        if (ESCache.Instance.DockableLocations.Any(i => i.Id == StationIdToGoto))
                        {
                            if (DebugConfig.DebugTraveler) Log.WriteLine("stationToGoTo: if (ESCache.Instance.DockableLocations.Any(i => i.Id == stationIDToGoto))");
                            _stationToGoTo = ESCache.Instance.DockableLocations.Find(i => i.Id == StationIdToGoto);
                            return _stationToGoTo;
                        }

                        if (DebugConfig.DebugTraveler) Log.WriteLine("stationToGoTo: if (ESCache.Instance.DockableLocations.All(i => i.Id != stationIDToGoto))");
                        return null;
                    }

                    if (DebugConfig.DebugTraveler) Log.WriteLine("stationToGoTo: No DockableLocations?!");
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public static int TooCloseToStructure
        {
            get
            {
                if (ESCache.Instance.Entities.Any(i => i.IsLargeCollidable && i.Name == "Asteroid Mining Post"))
                    return 10000;

                if (!ESCache.Instance.InAbyssalDeadspace && MissionSettings.MissionTooCloseToStructure != null)
                    return (int) MissionSettings.MissionTooCloseToStructure;

                if (ESCache.Instance.InAnomaly)
                    return 6000;

                return (int) Distances.TooCloseToStructure;
            }
        }

        private static int? _optimalRange { get; set; }

        private static int SafeDistanceFromStructure
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace && MissionSettings.MissionSafeDistanceFromStructure != null)
                    return (int) MissionSettings.MissionSafeDistanceFromStructure;

                return (int) Distances.SafeDistancefromStructure;
            }
        }

        public static bool AbyssalLargeAvoidBumpingThingsBool(bool avoidBumpingThingsOnlyIfGoingSlow)
        {
            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (!avoidBumpingThingsOnlyIfGoingSlow)
                    return true;

                if (DateTime.UtcNow > _nextAvoidBumpingThingsReset)
                {
                    AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
                    lastTooCloseToEntity = DateTime.UtcNow.AddHours(-5);
                    SafeDistanceFromStructureMultiplier = 1;
                    AvoidBumpingThingsWarningSent = false;
                    _intWeAreMovingSlowlyAgainAbyssalLarge = 0;
                    _abyssalLargeAvoidBumpingThingsBool = null;
                    return false;
                }

                if (_abyssalLargeAvoidBumpingThingsBool == null)
                {
                    if (WeAreMovingVerySlowly)
                    {
                        Log.WriteLine("Abyssal_AvoidBumpingIntoThings: We are moving less than Max Velocity [" + Math.Round(ESCache.Instance.ActiveShip.MaxVelocity, 0) + "m/s] * .30 [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "m/s]: _intWeAreMovingSlowlyAgainAbyssalMedium [" + _intWeAreMovingSlowlyAgainAbyssalMedium + "]");
                        _intWeAreMovingSlowlyAgainAbyssalLarge++;
                        if (_intWeAreMovingSlowlyAgainAbyssalLarge >= 2)
                        {
                            _nextAvoidBumpingThingsReset = DateTime.UtcNow.AddSeconds(60);
                            _abyssalLargeAvoidBumpingThingsBool = true;
                            return (bool) _abyssalLargeAvoidBumpingThingsBool;
                        }
                    }

                    return false;
                }

                return (bool) _abyssalLargeAvoidBumpingThingsBool;
            }

            return GlobalAvoidBumpingThingsBool;
        }

        public static bool AbyssalMediumAvoidBumpingThingsBool(bool avoidBumpingThingsOnlyIfGoingSlow)
        {
            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (!avoidBumpingThingsOnlyIfGoingSlow)
                    return true;

                if (_abyssalMediumAvoidBumpingThingsBool != null && (bool) _abyssalMediumAvoidBumpingThingsBool && DateTime.UtcNow > _nextAvoidBumpingThingsReset)
                {
                    Log.WriteLine("AbyssalMediumAvoidBumpingThingsBool: It has been 60 sec: resetting _abyssalMediumAvoidBumpingThingsBool to false");
                    AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
                    lastTooCloseToEntity = DateTime.UtcNow.AddHours(-5);
                    SafeDistanceFromStructureMultiplier = 1;
                    AvoidBumpingThingsWarningSent = false;
                    _intWeAreMovingSlowlyAgainAbyssalMedium = 0;
                    _abyssalMediumAvoidBumpingThingsBool = null;
                    return false;
                }

                if (_abyssalMediumAvoidBumpingThingsBool == null)
                {
                    if (WeAreMovingVerySlowly)
                    {
                        _intWeAreMovingSlowlyAgainAbyssalMedium++;
                        if (_intWeAreMovingSlowlyAgainAbyssalMedium >= 2)
                        {
                            Log.WriteLine("AbyssalMediumAvoidBumpingThingsBool: Is now true: _intWeAreMovingSlowlyAgainAbyssalMedium [" + _intWeAreMovingSlowlyAgainAbyssalMedium + "]");
                            _nextAvoidBumpingThingsReset = DateTime.UtcNow.AddSeconds(60);
                            _abyssalMediumAvoidBumpingThingsBool = true;
                            return (bool) _abyssalMediumAvoidBumpingThingsBool;
                        }

                        Log.WriteLine("AbyssalMediumAvoidBumpingThingsBool: We are moving less than Max Velocity [" + Math.Round(ESCache.Instance.ActiveShip.MaxVelocity, 0) + "m/s] * .30 [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "m/s]: _intWeAreMovingSlowlyAgainAbyssalMedium [" + _intWeAreMovingSlowlyAgainAbyssalMedium + "]");
                        return false;
                    }

                    return false;
                }

                return (bool) _abyssalMediumAvoidBumpingThingsBool;
            }

            return GlobalAvoidBumpingThingsBool;
        }

        public static bool AbyssalSmallAvoidBumpingThingsBool(bool avoidBumpingThingsOnlyIfGoingSlow)
        {
            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (!avoidBumpingThingsOnlyIfGoingSlow)
                    return true;

                if (DateTime.UtcNow > _nextAvoidBumpingThingsReset)
                {
                    AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
                    lastTooCloseToEntity = DateTime.UtcNow.AddHours(-5);
                    SafeDistanceFromStructureMultiplier = 1;
                    AvoidBumpingThingsWarningSent = false;
                    _intWeAreMovingSlowlyAgainAbyssalSmall = 0;
                    _abyssalSmallAvoidBumpingThingsBool = null;
                    return false;
                }

                if (_abyssalSmallAvoidBumpingThingsBool == null)
                {
                    if (WeAreMovingVerySlowly)
                    {
                        Log.WriteLine("Abyssal_AvoidBumpingIntoThings: We are moving less than Max Velocity [" + Math.Round(ESCache.Instance.ActiveShip.MaxVelocity, 0) + "m/s] * .30 [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "m/s]: _intWeAreMovingSlowlyAgainAbyssalMedium [" + _intWeAreMovingSlowlyAgainAbyssalMedium + "]");
                        _intWeAreMovingSlowlyAgainAbyssalSmall++;
                        if (_intWeAreMovingSlowlyAgainAbyssalSmall >= 2)
                        {
                            _nextAvoidBumpingThingsReset = DateTime.UtcNow.AddSeconds(60);
                            _abyssalSmallAvoidBumpingThingsBool = true;
                            return (bool) _abyssalSmallAvoidBumpingThingsBool;
                        }
                    }

                    return false;
                }

                return (bool) _abyssalSmallAvoidBumpingThingsBool;
            }

            return GlobalAvoidBumpingThingsBool;
        }

        public static bool AvoidBumpingThingsBool()
        {
            if (ESCache.Instance.InMission && MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
            {
                if (MissionSettings.MyMission != null && MissionSettings.MyMission.Agent.Level == 1)
                {
                    switch (MissionSettings.MyMission.Name)
                    {
                        case "Cash Flow for Capsuleers (4 of 10)":
                            return false;
                    }

                    return true;
                }

                if (MissionSettings.MyMission != null && MissionSettings.MyMission.Agent.Level == 2)
                    return true;

                if (MissionSettings.MyMission != null && MissionSettings.MyMission.Agent.Level == 3)
                    return true;

                if (MissionSettings.MyMission != null && MissionSettings.MyMission.Agent.Level == 4)
                {
                    switch (MissionSettings.MyMission.Name)
                    {
                        case "Cargo Delivery":
                        case "The Damsel In Distress":
                            return false;
                    }

                    if (MissionSettings.MyMission.Name.Contains("Anomic"))
                        return false;
                }

                if (MissionSettings.MyMission != null && MissionSettings.MyMission.Name.ToLower() == "Worlds Collide".ToLower())
                    if (ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.ToLower().Contains("Damaged Heron".ToLower())))
                        return false;
            }

            return GlobalAvoidBumpingThingsBool;
        }

        public static void ClearPerPocketCache()
        {
            AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
            SafeDistanceFromStructureMultiplier = 1;
            _chooseNavigateOnGridTargetIds = null;
        }

        #endregion Properties

        #region Methods

        private static List<long> _navigateOnGridTargets { get; set; } = new List<long>();
        private static readonly bool AvoidBumpingThingsOnlyIfGoingSlow = true;
        private static DateTime lastTooCloseToEntity = DateTime.UtcNow;
        private static DateTime NextLogWhereAmIOnGrid = DateTime.UtcNow;

        private static Stopwatch ChooseNavigateOnGridTargetsStopwatch = new Stopwatch();

        private static List<long> _chooseNavigateOnGridTargetIds { get; set; } = null;

        public static List<long> ChooseNavigateOnGridTargetIds
        {
            get
            {
                ChooseNavigateOnGridTargetsStopwatch.Restart();

                if (_chooseNavigateOnGridTargetIds != null)
                    return _chooseNavigateOnGridTargetIds;

                try
                {
                    if (Combat.Combat.PotentialCombatTargets.Count > 0)
                    {
                        if (ESCache.Instance.InWormHoleSpace)
                        {
                            if (ESCache.Instance.ActiveShip.GroupId == (int) Group.Dreadnaught)
                            {
                                //do not even try to move in a dread, siege or not its just a bad idea.
                                return new List<long>();
                            }

                            //do not move regardless (maybe we want this to orbit the anchor?
                            return new List<long>();
                        }

                        if (ESCache.Instance.InAbyssalDeadspace)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("ChooseNavigateOnGridTargets: if (ESCache.Instance.InAbyssalDeadspace)");
                            if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("ChooseNavigateOnGridTargets: if (AbyssalDeadspaceBehavior.TriglavianConstructionSiteSpawnFoundDozenPlusBSs)");
                                _chooseNavigateOnGridTargetIds = AbyssalDeadspace_14BsSpawn_NavigateOnGridTargets;
                                if (_chooseNavigateOnGridTargetIds.Count > 0)
                                    return _chooseNavigateOnGridTargetIds;
                            }

                            if (ESCache.Instance.MyShipEntity.IsFrigate)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("ChooseNavigateOnGridTargets: if (ESCache.Instance.MyShipEntity.IsFrigate)");
                                _chooseNavigateOnGridTargetIds =  AbyssalDeadspace_FrigateNavigateOnGridTargets;
                                if (_chooseNavigateOnGridTargetIds.Count > 0)
                                    return _chooseNavigateOnGridTargetIds;

                                return new List<long>();
                            }

                            _chooseNavigateOnGridTargetIds = AbyssalDeadspace_NavigateOnGridTargets;
                            if (_chooseNavigateOnGridTargetIds.Count > 0)
                                return _chooseNavigateOnGridTargetIds;

                            return new List<long>();
                        }

                        if (ESCache.Instance.InMission)
                            if (ESCache.Instance.MyShipEntity != null)
                            {
                                if (ESCache.Instance.MyShipEntity.IsFrigate)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.InMission) if (ESCache.Instance.MyShipEntity.IsFrigate)");
                                    _chooseNavigateOnGridTargetIds = PickNavigateOnGridTarget_BasedOnTargetsForAFrigate;
                                    if (_chooseNavigateOnGridTargetIds.Count > 0)
                                        return _chooseNavigateOnGridTargetIds;

                                    return new List<long>();
                                }

                                if (ESCache.Instance.MyShipEntity.IsCruiser)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.InMission) if (ESCache.Instance.MyShipEntity.IsCruiser)");

                                    _chooseNavigateOnGridTargetIds = PickNavigateOnGridTarget_BasedOnTargetsForACruiser;
                                    if (_chooseNavigateOnGridTargetIds.Count > 0)
                                        return _chooseNavigateOnGridTargetIds;

                                    return new List<long>();
                                }

                                if (ESCache.Instance.MyShipEntity.IsBattleship)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.InMission) if (ESCache.Instance.MyShipEntity.IsBattleship)");
                                    _chooseNavigateOnGridTargetIds = PickNavigateOnGridTarget_BasedOnTargetsForABattleship;
                                    if (_chooseNavigateOnGridTargetIds.Count > 0)
                                        return _chooseNavigateOnGridTargetIds;

                                    return new List<long>();
                                }

                                //
                                // Default to picking targets for a battleship sized ship
                                //
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.InMission) MyShipEntity class unknown");
                                _chooseNavigateOnGridTargetIds = PickNavigateOnGridTarget_BasedOnTargetsForABattleship;
                                if (_chooseNavigateOnGridTargetIds.Count > 0)
                                    return _chooseNavigateOnGridTargetIds;

                                return new List<long>();
                            }

                        return new List<long>();
                    }

                    List<long> ListOfAccelerationGates = new List<long>();
                    if (ESCache.Instance.AccelerationGates.Any())
                    {
                        foreach (var AccelerationGate in ESCache.Instance.AccelerationGates.OrderBy(i => i.Distance))
                        {
                            ListOfAccelerationGates.Add(AccelerationGate.Id);
                        }
                    }

                    return ListOfAccelerationGates;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<long>();
                }
                finally
                {
                    ChooseNavigateOnGridTargetsStopwatch.Stop();
                    //if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("ChooseNavigateOnGridTargets Took [" + Util.ElapsedMicroSeconds(ChooseNavigateOnGridTargetsStopwatch) + "]");
                }
            }
        }

        private static Stopwatch PickNavigateOnGridTargetStopWatch = new Stopwatch();

        public static List<long> PickNavigateOnGridTarget_BasedOnTargetsForABattleship
        {
            get
            {
                PickNavigateOnGridTargetStopWatch.Restart();

                if (_navigateOnGridTargets != null)
                    return _navigateOnGridTargets;

                _navigateOnGridTargets = Combat.Combat.PotentialCombatTargets.Where(i => !i.IsContainer && !i.IsBadIdea && !i.IsNPCDrone)
                    .OrderByDescending(k => k.IsPreferredPrimaryWeaponTarget)
                    .ThenByDescending(i => i.IsTargetedBy)
                    .ThenByDescending(i => i.IsAttacking)
                    .ThenByDescending(i => i.IsNPCBattleship && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCBattleship && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCBattleship && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser)
                    .ThenByDescending(i => i.IsNPCCruiser && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCCruiser && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCCruiser && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWebbingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCFrigate && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCFrigate && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWebbingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate).Select(i => i.Id).ToList();

                if (DebugConfig.DebugLogOrderOfNavigateOnGridTargets)
                    LogOrderOfNavigateOnGridTargets(_navigateOnGridTargets);

                PickNavigateOnGridTargetStopWatch.Stop();

                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("PickNavigateOnGridTarget_BasedOnTargetsForABattleship Took [" +  Util.ElapsedMicroSeconds(PickNavigateOnGridTargetStopWatch) + "]");
                return _navigateOnGridTargets;
            }
        }

        public static List<long> PickNavigateOnGridTarget_BasedOnTargetsForACruiser
        {
            get
            {
                PickNavigateOnGridTargetStopWatch.Restart();

                if (_navigateOnGridTargets != null)
                    return _navigateOnGridTargets;

                _navigateOnGridTargets = Combat.Combat.PotentialCombatTargets.Where(i => !i.IsContainer && !i.IsBadIdea && !i.IsNPCDrone)
                    .OrderByDescending(k => k.IsPreferredPrimaryWeaponTarget)
                    .ThenByDescending(i => i.IsTargetedBy)
                    .ThenByDescending(i => i.IsAttacking)
                    .ThenByDescending(i => i.IsNPCFrigate && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCFrigate && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCFrigate && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisJammingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWebbingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate)
                    .ThenByDescending(i => i.IsNPCCruiser && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCCruiser && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCCruiser && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisJammingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWebbingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisJammingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser)
                    .ThenByDescending(i => i.IsNPCBattleship && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCBattleship && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCBattleship && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisJammingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship).Select(i => i.Id).ToList();

                if (DebugConfig.DebugLogOrderOfNavigateOnGridTargets)
                    LogOrderOfNavigateOnGridTargets(_navigateOnGridTargets);

                PickNavigateOnGridTargetStopWatch.Stop();
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("PickNavigateOnGridTarget_BasedOnTargetsForACruiser Took [" + Util.ElapsedMicroSeconds(PickNavigateOnGridTargetStopWatch) + "]");
                return _navigateOnGridTargets;
            }
        }

        public static List<long> PickNavigateOnGridTarget_BasedOnTargetsForAFrigate
        {
            get
            {
                PickNavigateOnGridTargetStopWatch.Restart();

                if (_navigateOnGridTargets != null)
                    return _navigateOnGridTargets;

                _navigateOnGridTargets = Combat.Combat.PotentialCombatTargets.Where(i => !i.IsContainer && !i.IsBadIdea && !i.IsNPCDrone).Concat(ESCache.Instance.Wrecks.Where(i => ESCache.Instance.MyShipEntity.ShieldPct > 80 && i.IsWreckReadyToBeNavigateOnGridTarget))
                    .OrderBy(i => i.Distance)
                    .ThenByDescending(i => ESCache.Instance.InAnomaly && i.IsWreck && !i.IsWreckEmpty && i.IsPossibleToDropFactionModules)
                    .ThenByDescending(i => ESCache.Instance.InAnomaly && i.IsWreck && !i.IsWreckEmpty && i.IsLargeWreck)
                    .ThenByDescending(i => ESCache.Instance.InAnomaly && i.IsWreck && !i.IsWreckEmpty && i.IsMediumWreck)
                    .ThenByDescending(i => ESCache.Instance.InAnomaly && i.IsWreck && !i.IsWreckEmpty)
                    .ThenByDescending(k => k.IsPreferredPrimaryWeaponTarget)
                    .ThenByDescending(i => Combat.Combat.KillSentries && i.IsSentry && i.IsTargetedBy)
                    .ThenByDescending(i => i.IsTargetedBy)
                    .ThenByDescending(i => i.IsAttacking)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisJammingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCFrigate && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCFrigate && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWebbingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCFrigate)
                    .ThenByDescending(i => i.IsNPCCruiser && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCCruiser && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCCruiser && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisJammingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWebbingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCCruiser)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisJammingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCBattlecruiser)
                    .ThenByDescending(i => i.IsNPCBattleship && i.StructurePct < .90)
                    .ThenByDescending(i => i.IsNPCBattleship && i.ArmorPct < .90)
                    .ThenByDescending(i => i.IsNPCBattleship && i.ShieldPct < .90)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisNeutralizingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisWarpScramblingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisJammingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisSensorDampeningNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTrackingDisruptingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTargetPaintingNpc)
                    .ThenByDescending(i => i.IsNPCBattleship).Select(i => i.Id).ToList();

                if (DebugConfig.DebugLogOrderOfNavigateOnGridTargets)
                    LogOrderOfNavigateOnGridTargets(_navigateOnGridTargets);

                PickNavigateOnGridTargetStopWatch.Stop();
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("PickNavigateOnGridTarget_BasedOnTargetsForAFrigate Took [" + Util.ElapsedMicroSeconds(PickNavigateOnGridTargetStopWatch) + "]");
                return _navigateOnGridTargets;
            }
        }

        private static bool WeAreMovingVerySlowly
        {
            get
            {
                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.MyShipEntity != null)
                    if (ESCache.Instance.ActiveShip.MaxVelocity * .3 > ESCache.Instance.MyShipEntity.Velocity)
                        return true;

                return false;
            }
        }

        private static List<long> AbyssalDeadspace_14BsSpawn_NavigateOnGridTargets
        {
            get
            {
                if (_navigateOnGridTargets != null)
                    return _navigateOnGridTargets;

                _navigateOnGridTargets = ESCache.Instance.Entities.Where(i => (i.IsPotentialCombatTarget || i.IsAccelerationGate || (i.IsWreck && !i.IsWreckEmpty) || i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache) && !i.IsBadIdea && !i.IsNPCDrone)
                    .OrderByDescending(l => l.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway)
                    .ThenByDescending(l => l.IsWreck && !l.IsWreckEmpty && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway)
                    .ThenByDescending(l => l.IsAccelerationGate).Select(i => i.Id).ToList();

                if (DebugConfig.DebugLogOrderOfNavigateOnGridTargets)
                    LogOrderOfNavigateOnGridTargets(_navigateOnGridTargets);

                return _navigateOnGridTargets;
            }
        }

        private static List<long> AbyssalDeadspace_EverythingIsInRange
        {
            get
            {
                if (_navigateOnGridTargets != null)
                    return _navigateOnGridTargets;

                _navigateOnGridTargets = ESCache.Instance.EntitiesOnGrid.Where(i => i.Velocity == 0 && !i.Name.Contains("Lucid") && !i.Name.Contains("Tyrannos") && !i.IsNPCBattleship && !i.IsNPCCruiser && !i.IsNPCFrigate && (i.IsAbyssalAccelerationGateReadyToBeNavigateOnGridTarget || i.IsWreckReadyToBeNavigateOnGridTarget || i.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToBeNavigateOnGridTarget) && !i.IsBadIdea && !i.IsNPCDrone)
                    .OrderByDescending(l => (Salvage.TractorBeams.Count == 0 || l.Distance > Salvage.TractorBeamRange) || (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway) && l.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                    .ThenByDescending(l => (Salvage.TractorBeams.Count == 0 || l.Distance > Salvage.TractorBeamRange) || (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway) && l.IsWreck && !l.IsWreckEmpty)
                    .ThenByDescending(l => l.IsAccelerationGate).Select(i => i.Id).ToList();

                if (DebugConfig.DebugLogOrderOfNavigateOnGridTargets || DebugConfig.DebugNavigateOnGrid)
                    LogOrderOfNavigateOnGridTargets(_navigateOnGridTargets);

                return _navigateOnGridTargets;
            }
        }

        private static List<long> AbyssalDeadspace_NavigateOnGridTargets
        {
            get
            {
                if (_navigateOnGridTargets != null)
                    return _navigateOnGridTargets;

                _navigateOnGridTargets = ESCache.Instance.Entities.Where(i => (i.IsPotentialCombatTarget || i.IsAbyssalAccelerationGateReadyToBeNavigateOnGridTarget || i.IsWreckReadyToBeNavigateOnGridTarget || i.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToBeNavigateOnGridTarget) && !i.IsBadIdea && !i.IsNPCDrone)
                    .OrderByDescending(i => Drones.DronesKillHighValueTargets && i.Distance > Drones.MaxDroneRange && !Drones.DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive)
                    .ThenByDescending(i => !Drones.DronesKillHighValueTargets && i.Distance > Combat.Combat.MaxRange)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCBattlecruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCFrigate && Drones.DronesKillHighValueTargets && j.IsNeutralizingMe && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                    .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600)
                    .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(j => j.IsNPCBattleship)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasRemoteRepair)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsSensorDampeningMe)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsWebbingMe)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasNeutralizers)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCBattlecruiser && l.IsTrackingDisruptingMe && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(k => k.IsNPCBattlecruiser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.Combat.DoWeCurrentlyHaveTurretsMounted() && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule) && n.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.Combat.DoWeCurrentlyHaveTurretsMounted() && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(i => i.IsEntityIShouldKeepShooting && i.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                    .ThenByDescending(l => l.IsWreckReadyToBeNavigateOnGridTarget)
                    .ThenByDescending(l => l.IsAccelerationGate).Select(i => i.Id).ToList();

                if (DebugConfig.DebugLogOrderOfNavigateOnGridTargets)
                    LogOrderOfNavigateOnGridTargets(_navigateOnGridTargets);

                if (Combat.Combat.PotentialCombatTargets.Count == 0)
                    return new List<long>();

                return _navigateOnGridTargets;
            }
        }

        private static bool AllowOrbitingDeviantAutomataSuppressor
        {
            get
            {
                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceDeviantAutomataSuppressor))
                {
                    EntityCache AbyssalDeadspaceDeviantAutomataSuppressor = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.IsAbyssalDeadspaceDeviantAutomataSuppressor);
                    if (35000 > AbyssalDeadspaceDeviantAutomataSuppressor.Distance)
                    {
                        if (Combat.Combat.PotentialCombatTargets.Count(x => x.Name.Contains(" Tessella")) >= 3)
                        {
                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                            {
                                return true;
                            }

                            return false;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }
        private static List<long> AbyssalDeadspace_FrigateNavigateOnGridTargets
        {
            get
            {
                if (_navigateOnGridTargets != null)
                    return _navigateOnGridTargets;

                _navigateOnGridTargets = ESCache.Instance.Entities.Where(i => (i.IsPotentialCombatTarget || i.IsAbyssalAccelerationGateReadyToBeNavigateOnGridTarget || i.IsWreckReadyToBeNavigateOnGridTarget || i.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToBeNavigateOnGridTarget) && !i.IsBadIdea && !i.IsNPCDrone)
                    .OrderByDescending(i => i.IsAbyssalDeadspaceDeviantAutomataSuppressor && AllowOrbitingDeviantAutomataSuppressor)
                    //.ThenByDescending(i => i.IsAbyssalDeadspaceMultibodyTrackingPylon && ESCache.Instance.Weapons.Any(x => x.IsTurret) && AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                    .ThenByDescending(i => Drones.DronesKillHighValueTargets && i.Distance > Drones.MaxDroneRange && !Drones.DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive)
                    .ThenByDescending(i => !Drones.DronesKillHighValueTargets && i.Distance > Combat.Combat.MaxRange)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCBattlecruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCFrigate && Drones.DronesKillHighValueTargets && j.IsNeutralizingMe && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                    .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600)
                    .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule) && n.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.Combat.DoWeCurrentlyHaveTurretsMounted() && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCFrigate)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.Combat.DoWeCurrentlyHaveTurretsMounted() && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsNPCCruiser)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasRemoteRepair)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsSensorDampeningMe)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsWebbingMe)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasNeutralizers)
                    .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCBattlecruiser && l.IsTrackingDisruptingMe && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(k => k.IsNPCBattlecruiser)
                    .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(j => j.IsNPCBattleship)
                    .ThenByDescending(i => i.IsEntityIShouldKeepShooting && i.IsSomethingICouldKillFasterIfIWereCloser)
                    .ThenByDescending(l => l.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                    .ThenByDescending(l => l.IsWreckReadyToBeNavigateOnGridTarget)
                    .ThenByDescending(l => l.IsAccelerationGate).Select(i => i.Id).ToList();

                if (DebugConfig.DebugLogOrderOfNavigateOnGridTargets)
                    LogOrderOfNavigateOnGridTargets(_navigateOnGridTargets);

                if (Combat.Combat.PotentialCombatTargets.Count == 0)
                    return new List<long>();

                return _navigateOnGridTargets;
            }
        }

        public static bool Abyssal_AvoidBumpingIntoThings(EntityCache navigateOnGridTarget)
        {
            if (ESCache.Instance.InAbyssalDeadspace)
            {
                try
                {
                    if (!OrbitBioAdaptiveCacheOrGateIfWeStrayTooFarAway()) return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                try
                {
                    //
                    // always avoid Asteroids and such
                    //
                    EntityCache bigObjectToAvoid = null;

                    if (ESCache.Instance.AbyssalBigObjects.Any(i => i.Name.ToLower().Contains("Large Asteroid Environment".ToLower())))
                    {
                        bigObjectToAvoid = ESCache.Instance.AbyssalBigObjects.Where(i => i.Name.ToLower().Contains("Large Asteroid Environment".ToLower()) && 22500 > i.Distance).OrderBy(i => i.Distance).FirstOrDefault();
                        if (bigObjectToAvoid != null)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("Abyssal_AvoidBumpingIntoThings: [" + bigObjectToAvoid.Name + "] found on grid [" + Math.Round(bigObjectToAvoid.Distance / 1000, 0) + "]k away");
                            if (!AvoidBumpingThings(bigObjectToAvoid, "NavigateOnGrid: NavigateIntoRange", 22500, AbyssalLargeAvoidBumpingThingsBool(AvoidBumpingThingsOnlyIfGoingSlow))) return false;
                            return true;
                        }
                    }

                    if (ESCache.Instance.AbyssalBigObjects.Any(i => i.Name.ToLower().Contains("Medium Asteroid Environment".ToLower())))
                    {
                        bigObjectToAvoid = ESCache.Instance.AbyssalBigObjects.Where(i => i.Name.ToLower().Contains("Medium Asteroid Environment".ToLower()) && 20500 > i.Distance).OrderBy(i => i.Distance).FirstOrDefault();
                        if (bigObjectToAvoid != null)
                        {
                            bool tempBool = AbyssalMediumAvoidBumpingThingsBool(AvoidBumpingThingsOnlyIfGoingSlow);
                            Log.WriteLine("Abyssal_AvoidBumpingIntoThings: myVelocity [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "] m/s [" + bigObjectToAvoid.Name + "] found on grid [" + Math.Round(bigObjectToAvoid.Distance / 1000, 0) + "]k away which is too close: AvoidBumpingThingsBool [" + tempBool + "]");
                            if (!AvoidBumpingThings(bigObjectToAvoid, "NavigateOnGrid: NavigateIntoRange", 20500, tempBool)) return false;
                            return true;
                        }
                    }

                    if (ESCache.Instance.AbyssalBigObjects.Any(i => i.Name.ToLower().Contains("Small Asteroid Environment".ToLower())))
                    {
                        bigObjectToAvoid = ESCache.Instance.AbyssalBigObjects.Where(i => i.Name.ToLower().Contains("Small Asteroid Environment".ToLower()) && 6500 > i.Distance && WeAreMovingVerySlowly).OrderBy(i => i.Distance).FirstOrDefault();
                        if (bigObjectToAvoid != null)
                        {
                            bool tempBool = AbyssalSmallAvoidBumpingThingsBool(AvoidBumpingThingsOnlyIfGoingSlow);
                            Log.WriteLine("Abyssal_AvoidBumpingIntoThings: [" + bigObjectToAvoid.Name + "] found on grid [" + Math.Round(bigObjectToAvoid.Distance / 1000, 0) + "]k away which is too close: AvoidBumpingThingsBool [" + tempBool + "]");
                            if (!AvoidBumpingThings(bigObjectToAvoid, "NavigateOnGrid: NavigateIntoRange", 6500, tempBool)) return false;
                            return true;
                        }
                    }

                    if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsTarget || i.IsTargeting))
                    {
                        //clouds... what are the ranges for each size cloud? we do not currently account for that!
                        int TooClose = 5000;
                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidTachyonClouds && ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any())
                        {
                            TooClose = Math.Max((int) ESCache.Instance.AbyssalDeadspaceTachyonClouds.FirstOrDefault().OptimalRange, 20000);
                            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("AvoidBumpingThings: AbyssalDeadspacetachyonCloud [" + ESCache.Instance.AbyssalDeadspaceTachyonClouds.FirstOrDefault().Distance + "]k  TooClose [" + TooClose + "]");
                            if (!AvoidBumpingThings(ESCache.Instance.AbyssalDeadspaceTachyonClouds.FirstOrDefault(), "NavigateOnGrid: NavigateIntoRange avoid Clouds", TooClose, AvoidBumpingThingsBool())) return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidBioluminescenceClouds && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any())
                        {
                            TooClose = Math.Max((int) ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.FirstOrDefault().OptimalRange, 20000);
                            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("AvoidBumpingThings: AbyssalDeadspaceBioluminesenceCloud [" + ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.FirstOrDefault().Distance + "]k  TooClose [" + TooClose + "]");
                            if (!AvoidBumpingThings(ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.FirstOrDefault(), "NavigateOnGrid: NavigateIntoRange avoid Clouds", TooClose, AvoidBumpingThingsBool())) return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidFilamentClouds && ESCache.Instance.AbyssalDeadspaceFilamentCloud.Any())
                        {
                            TooClose = Math.Max((int) ESCache.Instance.AbyssalDeadspaceFilamentCloud.FirstOrDefault().OptimalRange, 20000);
                            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("AvoidBumpingThings: AbyssalDeadspaceFilamentCloud [" + ESCache.Instance.AbyssalDeadspaceFilamentCloud.FirstOrDefault().Distance + "]k  TooClose [" + TooClose + "]");
                            if (!AvoidBumpingThings(ESCache.Instance.AbyssalDeadspaceFilamentCloud.FirstOrDefault(), "NavigateOnGrid: NavigateIntoRange avoid Clouds", TooClose, AvoidBumpingThingsBool())) return false;
                        }

                        //towers...
                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers) if (!AvoidBumpingThings(ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.FirstOrDefault(), "NavigateOnGrid: NavigateIntoRange avoid Aoe Weapons", (int) ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.FirstOrDefault().OptimalRange, false)) return false; //these have a 15k in tiers 1-3 and a 40k range in tier 4 and 5
                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceAvoidMultibodyTrackingPylonTowers) if (!AvoidBumpingThings(ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.FirstOrDefault(), "NavigateOnGrid: NavigateIntoRange avoid Aoe Weapons", (int) ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.FirstOrDefault().OptimalRange, false)) return false; //these have a 15k in tiers 1-3 and a 40k range in tier 4 and 5
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return true;
                }
            }

            return true;
        }

        public static void AbyssalLogWhereAmIOnGrid()
        {
            try
            {
                if (!ESCache.Instance.InAbyssalDeadspace) return;
                if (NextLogWhereAmIOnGrid > DateTime.UtcNow) return;

                EntityCache TriglavianGate = ESCache.Instance.AccelerationGates.Find(i => i.Name.Contains("Triglavian"));
                EntityCache BioadaptiveCacheOrWreck = null;
                int numBattleshipsLeft = 0;
                int numBattlecruisersLeft = 0;
                int numCruisersLeft = 0;
                int numDestroyersLeft = 0;
                int numFrigatesLeft = 0;
                int numPotentialCombatTargetsLeft = 0;

                if (Combat.Combat.PotentialCombatTargets.Count > 0)
                {
                    numPotentialCombatTargetsLeft = Combat.Combat.PotentialCombatTargets.Count;
                    numBattleshipsLeft = Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship);
                    numBattlecruisersLeft = Combat.Combat.PotentialCombatTargets.Count(i => i.IsBattlecruiser);
                    numCruisersLeft = Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser);
                    numDestroyersLeft = Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer);
                    numFrigatesLeft = Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate);
                }

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache))
                    BioadaptiveCacheOrWreck = ESCache.Instance.EntitiesOnGrid.Find(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache);

                if (ESCache.Instance.Wrecks.Count > 0)
                    BioadaptiveCacheOrWreck = ESCache.Instance.Wrecks.FirstOrDefault();

                if (BioadaptiveCacheOrWreck != null)
                {
                    NextLogWhereAmIOnGrid = DateTime.UtcNow.AddSeconds(15);
                    EntityCache ClosestEntity = ESCache.Instance.EntitiesNotSelf.OrderBy(i => i.Distance).FirstOrDefault();
                    Log.WriteLine("TimeInPocket [" + Math.Round(Math.Abs(DateTime.UtcNow.Subtract(Statistics.StartedPocket).TotalMinutes), 0) + "min] Triglavian Gate [" + Math.Round(TriglavianGate.Distance / 1000, 0) + "k] BioAdaptiveCacheorWreck [" + Math.Round(BioadaptiveCacheOrWreck.Distance / 1000, 0) + "k] speed [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + " m/s] BS [" + numBattleshipsLeft + "] BC [" + numBattlecruisersLeft + "] Cruisers [" + numCruisersLeft + "] Destroyers [" + numDestroyersLeft + "] Frigates [" + numFrigatesLeft + "] PotentialCombatTargets [" + numPotentialCombatTargetsLeft + "] DronesInSpace [" + Drones.ActiveDroneCount + "] Targets [" + ESCache.Instance.Targets.Count + "] IsAttacking [" + Combat.Combat.PotentialCombatTargets.Count(i => i.IsAttacking) + "] Closest Entity [" + ClosestEntity.Name + "] @ [" + Math.Round(ClosestEntity.Distance / 1000, 0) + "k] AbyssalTimerInSec [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds, 0) + "]");
                    LogMyCurrentHealth();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void LogMyCurrentHealth(string module = "")
        {
            if (ESCache.Instance.ActiveShip != null) Log.WriteLine("MyHealth: [" + module + "] S[" + ESCache.Instance.ActiveShip.ShieldPercentage + "] A[" + ESCache.Instance.ActiveShip.ArmorPercentage + "] H[" + ESCache.Instance.ActiveShip.StructurePercentage + "] C[" + ESCache.Instance.ActiveShip.CapacitorPercentage + "]");
            return;
        }

        public static bool AvoidBumpingThings(EntityCache thisBigObject, string module, int tooCloseToEntity, bool avoidBumpingThingsBool)
        {
            try
            {
                if (!ESCache.Instance.InSpace || ESCache.Instance.InWarp || ESCache.Instance.ActiveShip.Entity.IsCloaked)
                    return true;

                if (DateTime.UtcNow < NextAvoidBumpingThings)
                    return false;

                bool tempAvoidBumpingThingsBool = false;
                tempAvoidBumpingThingsBool = avoidBumpingThingsBool;
                if (tempAvoidBumpingThingsBool)
                {
                    //we cant move in bastion mode, do not try
                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile) return false;

                    if (!ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.Distance < 9000)
                            return false;

                        if (ESCache.Instance.ClosestDockableLocation != null && ESCache.Instance.ClosestDockableLocation.Distance < 11000)
                            return false;
                    }

                    if (thisBigObject != null)
                    {
                        //
                        // if we are "too close" to the bigObject move away... (is orbit the best thing to do here?)
                        //
                        if (lastTooCloseToEntity.AddSeconds(90) > DateTime.UtcNow && thisBigObject.Distance >= tooCloseToEntity)
                        {
                            //we are no longer "too close" and can proceed.
                            if (AvoidBumpingThingsWarningSent)
                                if (!ESCache.Instance.InAbyssalDeadspace)
                                    StopMyShip("We are no longer too close to structure");

                            Log.WriteLine("AvoidBumpingThings: We are further than tooCloseToEntity[" + tooCloseToEntity + "] from [" + thisBigObject.Name + "] @ [" + Math.Round(thisBigObject.Distance / 1000, 0) + "k]");
                            AvoidBumpingThingsReset();
                            return true;
                        }

                        if (tooCloseToEntity > thisBigObject.Distance)
                        {
                            Log.WriteLine("AvoidBumpingThings: [" + thisBigObject.Name + "] is Distance [" + Math.Round(thisBigObject.Distance / 1000, 0) + "k] away which is less than [tooCloseToEntity][" + Math.Round((double) tooCloseToEntity / 1000, 0) + "k]: we are too close!");
                            lastTooCloseToEntity = DateTime.UtcNow;

                            if (ESCache.Instance.InAbyssalDeadspace && !ESCache.Instance.ActiveShip.IsFrigate && !ESCache.Instance.ActiveShip.IsAssaultShip && !ESCache.Instance.ActiveShip.IsDestroyer)
                            {
                                EntityCache AccelGate = ESCache.Instance.AccelerationGates.FirstOrDefault();
                                int OrbitToGetUnstuckDistance = (int)AccelGate.Distance + 3000;
                                if (AccelGate.Orbit(OrbitToGetUnstuckDistance))
                                {
                                    Log.WriteLine("[" + module + "] Initiating Orbit [" + AccelGate.Name + "][" + Math.Round(AccelGate.Distance / 1000, 0) + "k] @ OrbitToGetUnstuckDistance [" + Math.Round((AccelGate.Distance + 2000) / 1000, 0) + "] SafeDistanceFromStructure [" + SafeDistanceFromStructure + "] tooCloseToEntity - 5000 [" + (tooCloseToEntity - 5000) + "]");
                                    NextAvoidBumpingThings = DateTime.UtcNow.AddSeconds(35);
                                    return false;
                                }

                                return false;
                            }

                            if (DateTime.UtcNow > AvoidBumpingThingsTimeStamp.AddSeconds(30) && !ESCache.Instance.InAbyssalDeadspace)
                            {
                                if (SafeDistanceFromStructureMultiplier <= 1)
                                {
                                    AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
                                    SafeDistanceFromStructureMultiplier++;
                                }

                                if (DateTime.UtcNow > AvoidBumpingThingsTimeStamp.AddMinutes(5) && !AvoidBumpingThingsWarningSent)
                                {
                                    Log.WriteLine("AvoidBumpingThings: We are stuck on a object and have been trying to orbit away from it for over 5 min");
                                    AvoidBumpingThingsWarningSent = true;
                                }

                                if (DateTime.UtcNow > AvoidBumpingThingsTimeStamp.AddMinutes(15))
                                    if (Combat.Combat.PotentialCombatTargets.Count == 0 ||
                                        Combat.Combat.PotentialCombatTargets.All(i => i.Distance > ESCache.Instance.WeaponRange))
                                    {
                                        const string msg = "AvoidBumpingThings: We have been stuck on an object for over 15 min and have nothing left to shoot";
                                        ESCache.Instance.CloseEveReason = msg;
                                        ESCache.Instance.BoolRestartEve = true;
                                        return false;
                                    }
                                    else
                                    {
                                        Log.WriteLine("AvoidBumpingThings: We are stuck on a object and have been trying to orbit away from it for over 15 min: waiting for combat to clear NPCs");
                                    }
                            }

                            int distanceForAvoidBumpingThingsToOrbit = Math.Max(SafeDistanceFromStructure, tooCloseToEntity + 3000);
                            if (thisBigObject.Orbit(distanceForAvoidBumpingThingsToOrbit, false, "[" + module + "] Initiating Orbit of [" + thisBigObject.Name + "][" + Math.Round(thisBigObject.Distance / 1000, 0) + "k] orbiting at [" + distanceForAvoidBumpingThingsToOrbit + "] SafeDistanceFromStructure [" + SafeDistanceFromStructure + "] tooCloseToEntity [" + tooCloseToEntity + "]"))
                            {
                                NextAvoidBumpingThings = DateTime.UtcNow.AddSeconds(15);
                                return false;
                            }

                            return false;
                            //we are still too close, do not continue through the rest until we are not "too close" anymore
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("Debug AvoidBumpingThings: [" + thisBigObject.Name + "] is Distance [" + Math.Round(thisBigObject.Distance / 1000, 0) + "k] [tooCloseToEntity][" + Math.Round((double) tooCloseToEntity / 1000, 0) + "k]");
                        AvoidBumpingThingsReset();
                        return true;
                    }

                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("AvoidBumpingThings: AvoidBumpingThingsBool [" + tempAvoidBumpingThingsBool + "] AvoidBumpingThingsOnlyIfGoingSlow [" + AvoidBumpingThingsOnlyIfGoingSlow + "]");
                AvoidBumpingThingsReset();
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public static bool GlobalAvoidBumpingIntoThings(EntityCache navigateOnGridTarget)
        {
            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (!Abyssal_AvoidBumpingIntoThings(navigateOnGridTarget)) return false;

                if (BioAdaptiveCacheOrWreckOrAbyssalGate == null)
                    return true;

                if (Combat.Combat.PotentialCombatTargets.All(i => i.IsInRangeOfWeapons || (Drones.UseDrones && i.IsInDroneRange)) && ESCache.Instance.ActiveShip.FollowingEntity == null && BioAdaptiveCacheOrWreckOrAbyssalGate != null)
                {
                    BioAdaptiveCacheOrWreckOrAbyssalGate.Orbit(500, false, "Orbit BioAdaptiveCache or Gate [" + BioAdaptiveCacheOrWreckOrAbyssalGate.Name + "][" + Math.Round(BioAdaptiveCacheOrWreckOrAbyssalGate.Distance / 1000, 0) + "k away]@[500m] ActiveShip.FollowingEntity == null");
                    return true;
                }

                return true;
            }

            Mission_AvoidBumpingIntoThings();
            return true;
        }

        private static DateTime _lastClearPer5SecondCache = DateTime.UtcNow;
        public static DateTime LastPositionTimestamp = DateTime.UtcNow;
        public static DirectWorldPosition PositionVec3TakenEvery5SecondsWas { get; set; }

        private static void ClearPer5SecondCache()
        {
            if (ESCache.Instance.InStation) return;
            if (!ESCache.Instance.InSpace) return;
            if (ESCache.Instance.ActiveShip == null) return;
            if (ESCache.Instance.ActiveShip.Entity == null) return;

            if (DateTime.UtcNow > _lastClearPer5SecondCache.AddSeconds(5) || PositionVec3TakenEvery5SecondsWas == null)
            {
                //Log.WriteLine("if (DateTime.UtcNow > _lastClearPer5SecondCache.AddSeconds(5) || PositionVec3TakenEvery5SecondsWas == null)");

                if (ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition == null)
                    Log.WriteLine("if (ESCache.Instance.ActiveShip.Entity.DirectWorldPosition == null)");

                if (ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition != null)
                {
                    //Log.WriteLine("if (ESCache.Instance.ActiveShip.Entity.DirectWorldPosition != null)");
                    _lastClearPer5SecondCache = DateTime.UtcNow;
                    LastPositionTimestamp = DateTime.UtcNow;
                    PositionVec3TakenEvery5SecondsWas = ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition;
                    //ESCache.Instance.ActiveShip.Entity.RemoveDrawnSphereByName("t1");

                    try
                    {
                        if (ESCache.Instance.ClosestStation != null)
                        {
                            if (ESCache.Instance.ClosestStation._directEntity.DirectAbsolutePosition == null)
                                Log.WriteLine("if (ESCache.Instance.ClosestStation._directEntity.DirectWorldPosition == null)");

                            if (ESCache.Instance.ActiveShip == null)
                                Log.WriteLine("if (ESCache.Instance.ActiveShip == null)");

                            if (ESCache.Instance.ActiveShip.Entity == null)
                                Log.WriteLine("if (ESCache.Instance.ActiveShip.Entity == null)");

                            if (ESCache.Instance.ClosestStation._directEntity == null)
                                Log.WriteLine("if (ESCache.Instance.ClosestStation._directEntity == null)");

                            //Log.WriteLine("Nearest Station [" + ESCache.Instance.ClosestStation.Name + "] X [" + ESCache.Instance.ClosestStation._directEntity.DirectWorldPosition.XCoordinate + "] Y [" + ESCache.Instance.ClosestStation._directEntity.DirectWorldPosition.YCoordinate + "] Z [" + ESCache.Instance.ClosestStation._directEntity.DirectWorldPosition.ZCoordinate + "]");
                            //Log.WriteLine("Distance Calculated [" + ESCache.Instance.ActiveShip.Entity.DirectWorldPosition.GetDistance(ESCache.Instance.ClosestStation._directEntity.DirectWorldPosition) + "] Distance [" + ESCache.Instance.ClosestStation._directEntity.Distance + "]");
                        }

                        //Log.WriteLine("if (ESCache.Instance.ClosestStation == null)");
                        //Log.WriteLine("Entities Count [" + ESCache.Instance.Entities.Count + "]");
                        //Log.WriteLine("EntitiesOnGrid Count [" + ESCache.Instance.EntitiesOnGrid.Count + "]");
                        //Log.WriteLine("Stations Count [" + ESCache.Instance.Stations.Count + "]");
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    //Log.WriteLine("...");
                }

                //Log.WriteLine("......");

                if (DebugConfig.DebugWspaceSiteBehavior)
                {
                    //Log.WriteLine("PositionVec3TakenEvery5SecondsWas X[" + (Vec3)PositionVec3TakenEvery5SecondsWas.X + "]");
                    //Log.WriteLine("PositionVec3TakenEvery5SecondsWas Y[" + (Vec3)PositionVec3TakenEvery5SecondsWas.Y + "]");
                    //Log.WriteLine("PositionVec3TakenEvery5SecondsWas Z[" + (Vec3)PositionVec3TakenEvery5SecondsWas.Z + "]");
                    //double temp = ESCache.Instance.ActiveShip.DistanceToPositionVec3TakenEvery5Seconds ?? -1;
                    //Log.WriteLine("PositionVec3TakenEvery5SecondsWas [" + Math.Round(temp, 0)  + "] k away");
                    //ESCache.Instance.ActiveShip.Entity.DrawSphereAtCurrentCoordinates("PV3T5S", 500);
                }
            }
        }

        public static void InvalidateCache()
        {
            _bioAdaptiveCacheOrWreckOrAbyssalGate = null;
            _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes = null;
            _chooseNavigateOnGridTargetIds = null;
            _determineWhichWrecksToApproach = null;
            _stationToGoTo = null;
            _navigateOnGridTargets = null;
            _orbitThisTarget = null;
            ClearPer5SecondCache();
            if (ESCache.Instance.InSpace)
            {
                if (ESCache.Instance.ActiveShip == null)
                {
                    Log.WriteLine("ESCache.Instance.ActiveShip == null ---");
                }
                else if (ESCache.Instance.ActiveShip.Entity == null)
                {
                    Log.WriteLine("ESCache.Instance.ActiveShip.Entity == null ---");
                }
                else if (ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition == null)
                {
                    Log.WriteLine("ESCache.Instance.ActiveShip.Entity.DirectWorldPosition == null) ---");
                }
                else
                {
                    //Log.WriteLine("ESCache.Instance.ActiveShip.Entity.Position [" + ESCache.Instance.ActiveShip.Entity.DirectWorldPosition.PositionInSpace.X + "][" + ESCache.Instance.ActiveShip.Entity.DirectWorldPosition.PositionInSpace.Y + "][" + ESCache.Instance.ActiveShip.Entity.DirectWorldPosition.PositionInSpace.Z + "] ");
                }
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("NavigateOnGrid");
                GlobalAvoidBumpingThingsBool =
                    (bool?) CharacterSettingsXml.Element("avoidBumpingThings") ??
                    (bool?) CommonSettingsXml.Element("avoidBumpingThings") ?? true;
                Log.WriteLine("NavigateOnGrid: avoidBumpingThings [" + GlobalAvoidBumpingThingsBool + "]");
                SpeedTankGlobalSetting =
                    (bool?) CharacterSettingsXml.Element("speedTank") ??
                    (bool?) CommonSettingsXml.Element("speedTank") ?? false;
                Log.WriteLine("NavigateOnGrid: speedTank [" + SpeedTankGlobalSetting + "]");
                OrbitDistance =
                    (int?) CharacterSettingsXml.Element("orbitDistance") ??
                    (int?) CommonSettingsXml.Element("orbitDistance") ?? 0;
                Log.WriteLine("NavigateOnGrid: orbitDistance [" + OrbitDistance + "]");
                GlobalOrbitStructure =
                    (bool?) CharacterSettingsXml.Element("orbitStructure") ??
                    (bool?) CommonSettingsXml.Element("orbitStructure") ?? true;
                Log.WriteLine("NavigateOnGrid: orbitStructure [" + GlobalOrbitStructure + "]");
            }
            catch (Exception exception)
            {
                Log.WriteLine("Error Loading Speed and Movement Settings [" + exception + "]");
            }
        }

        public static void LogWhereAmIOnGrid()
        {
            try
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    AbyssalLogWhereAmIOnGrid();
                    return;
                }

                MissionLogWhereAmIOnGrid();
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool Mission_AvoidBumpingIntoThings()
        {
            if (!ESCache.Instance.InMission) return true;
            if (ESCache.Instance.Stargates.Any(i => i.IsOnGridWithMe)) return true;
            if (ESCache.Instance.DockableLocations.Any(i => i.IsOnGridWithMe)) return true;

            //
            // Avoid Asteroids and such all the time
            //
            EntityCache bigObjectToAvoid = ESCache.Instance.BigObjects.OrderBy(i => i.Distance).FirstOrDefault();
            if (!AvoidBumpingThings(bigObjectToAvoid, "NavigateOnGrid: NavigateIntoRange", TooCloseToStructure, AvoidBumpingThingsBool())) return false;
            return true;
        }

        public static void MissionLogWhereAmIOnGrid()
        {
            if (ESCache.Instance.InMission)
            {
                int numBattleshipsLeft = 0;
                int numBattlecruisersLeft = 0;
                int numCruisersLeft = 0;
                int numFrigatesLeft = 0;
                int numPotentialCombatTargetsLeft = 0;

                if (Combat.Combat.PotentialCombatTargets.Count > 0)
                {
                    numPotentialCombatTargetsLeft = Combat.Combat.PotentialCombatTargets.Count;
                    numBattleshipsLeft = Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship);
                    numBattlecruisersLeft = Combat.Combat.PotentialCombatTargets.Count(i => i.IsBattlecruiser);
                    numCruisersLeft = Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser);
                    numFrigatesLeft = Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate);
                }

                if (DateTime.UtcNow > NextLogWhereAmIOnGrid)
                {
                    NextLogWhereAmIOnGrid = DateTime.UtcNow.AddMinutes(1);
                    Log.WriteLine("TimeInPocket [" + Math.Round(Math.Abs(DateTime.UtcNow.Subtract(Statistics.StartedPocket).TotalMinutes), 0) + "min] speed [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + " m/s] BS [" + numBattleshipsLeft + "] BC [" + numBattlecruisersLeft + "] Cruisers [" + numCruisersLeft + "] Frigates [" + numFrigatesLeft + "] PotentialCombatTargets [" + numPotentialCombatTargetsLeft + "] DronesInSpace [" + Drones.ActiveDroneCount + "] Targets [" + ESCache.Instance.Targets.Count + "]");
                }
            }
        }

        public static void NavigateInWSpace_Dread()
        {
            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateInWSpace_Dread");

            if (ESCache.Instance.Wormholes.Any(i => i.IsOnGridWithMe))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.Wormholes.Any(i => i.IsOnGridWithMe))");
                //
                // handle navigating when near a wormhole! approach? run? cloak?
                //
                return;
            }

            if (ESCache.Instance.Citadels.Any(i => i.IsOnGridWithMe))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.Citadels.Any(i => i.IsOnGridWithMe))");
                //handle navigating when near a Citadel!
                return;
            }

            if (ESCache.Instance.ActiveShip.IsImmobile)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.ActiveShip.IsImmobile)");
                return;
            }

            if (Time.Instance.LastInWarp.AddSeconds(60) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (Time.Instance.LastInWarp.AddSeconds(60) > DateTime.UtcNow)");
                if (ESCache.Instance.ActiveShip.Entity.Velocity > 7)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.ActiveShip.Entity.Velocity > 10)");
                    if (ESCache.Instance.ActiveShip.IsMovingAwayFromLastInWarpPoint != null && (bool)ESCache.Instance.ActiveShip.IsMovingAwayFromLastInWarpPoint)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.ActiveShip.IsMovingAwayFromLastInWarpPoint != null && (bool)ESCache.Instance.ActiveShip.IsMovingAwayFromLastInWarpPoint)");
                        if (ESCache.Instance.LastInWarpPositionWas == null)
                            return;

                        Log.WriteLine("Velocity is [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s] Approaching LastInWarpPositionWas [" + Math.Round((double)ESCache.Instance.ActiveShip.DistanceToLastInWarpPosition / 1000, 0) + "k ] away - to slow down");
                        ESCache.Instance.ActiveShip.MoveTo(ESCache.Instance.LastInWarpPositionWas);
                        return;
                    }

                    if (ESCache.Instance.ActiveShip.IsMovingTowardLastInWarpPoint != null && (bool)ESCache.Instance.ActiveShip.IsMovingTowardLastInWarpPoint)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (ESCache.Instance.ActiveShip.IsMovingTowardLastInWarpPoint != null && (bool)ESCache.Instance.ActiveShip.IsMovingTowardLastInWarpPoint)");
                        if (PositionVec3TakenEvery5SecondsWas == null)
                            return;

                        Log.WriteLine("Velocity is [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s] Approaching PositionVec3TakenEvery5SecondsWas [" + Math.Round((double)ESCache.Instance.ActiveShip.DistanceToPositionVec3TakenEvery5Seconds / 1000, 0) + "k ] away - to slow down");
                        ESCache.Instance.ActiveShip.MoveTo(PositionVec3TakenEvery5SecondsWas);
                        return;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("return;");
                    return;
                }

                if (!ESCache.Instance.ActiveShip.IsImmobile) StopMyShip("Dread still moving at [" + ESCache.Instance.ActiveShip.Entity.Velocity + "m/s]");
            }

            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("Its been over 60 sec since we warped in, do nothing");
        }

        public static void NavigateInWSpace()
        {
            if (!ESCache.Instance.InSpace) return;
            if (ESCache.Instance.InWarp) return;
            if (!DebugConfig.DebugWspaceSiteBehavior)
            {
                if (!ESCache.Instance.InWormHoleSpace) return; //If in regular space do not navigate on grid at all!
            }

            if (ESCache.Instance.ActiveShip.IsDread || DebugConfig.DebugWspaceSiteBehavior)
            {
                NavigateInWSpace_Dread();
                return;
            }

            return; //This assumes dread, if your in another ship, do nothing for now
        }

        public static bool NavigateInAbyssalDeadspace()
        {
            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (SpeedTank)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateInAbyssalDeadspace: if (SpeedTank)");
                    NavigateInAbyssalDeadspaceSpeedtank();
                    return true;
                }

                NavigateInAbyssalDeadspaceSlowBoat();
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateInAbyssalDeadspace: if (!ESCache.Instance.InAbyssalDeadspace)");
            return false;
        }

        public static bool NavigateInFactionWarfareComplex()
        {
            if (SpeedTank)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateInAbyssalDeadspace: if (SpeedTank)");
                NavigateOnGridUsingSpeedTank(ChooseNavigateOnGridTargetIds);
                return true;
            }

            NavigateInAbyssalDeadspaceSlowBoat();
            return true;
        }

        public static bool NavigateInAbyssalDeadspaceSlowBoat()
        {
            if (ChooseNavigateOnGridTargetIds.Count > 0)
            {
                List<EntityCache> ChooseNavigateOnGridTargets = new List<EntityCache>();
                foreach (long NavigateOnGridEntityId in ChooseNavigateOnGridTargetIds)
                {
                    foreach (EntityCache EntityOnGrid in ESCache.Instance.EntitiesOnGrid.Where(i => i.Id == NavigateOnGridEntityId))
                    {
                        ChooseNavigateOnGridTargets.Add(EntityOnGrid);
                        break;
                    }

                    continue;
                }

                if (ChooseNavigateOnGridTargets.Any(i => (i.IsValid && !Drones.DronesKillHighValueTargets && !i.IsReadyToShoot) || (Drones.DronesKillHighValueTargets && !i.IsReadyForDronesToShoot && !Drones.DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive) || !i.IsInWebRange))
                {
                    //
                    // we have SOMETHING that isnt in range
                    //

                    //if optimalrange is set - use it to determine engagement range
                    if (OptimalRange != 0)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (OptimalRange != 0): NavigateOnGridUsingOptimalRange");
                        NavigateOnGridUsingOptimalRange(ChooseNavigateOnGridTargetIds);
                    }
                    else //if optimalrange is not set use MaxRange (shorter of weapons range and targeting range)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (OptimalRange == 0): NavigateOnGridUsingMaxRange");
                        NavigateOnGridUsingMaxRange(ChooseNavigateOnGridTargetIds);
                    }

                    return true;
                }
            }

            //
            // Everything is in range!
            //

            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateInAbyssalDeadspace: Everything is in range");

            if (OrbitThisTarget == null)
            {
                Log.WriteLine("NavigateInAbyssalDeadspaceSlowBoat: target was null!!");
                return true;
            }

            Log.WriteLine("NavigateInAbyssalDeadspaceSlowBoat: Orbit [" + OrbitThisTarget.Name + "] at [500m].");
            OrbitThisTarget.Orbit(500);
            return true;
        }

        public static bool NavigateInAbyssalDeadspaceSpeedtank()
        {
            if (ChooseNavigateOnGridTargetIds.Count > 0)
            {
                List<EntityCache> ChooseNavigateOnGridTargets = new List<EntityCache>();
                foreach (long NavigateOnGridEntityId in ChooseNavigateOnGridTargetIds)
                {
                    foreach (EntityCache EntityOnGrid in ESCache.Instance.EntitiesOnGrid.Where(i => i.Id == NavigateOnGridEntityId))
                    {
                        ChooseNavigateOnGridTargets.Add(EntityOnGrid);
                        break;
                    }

                    continue;
                }

                if ((ChooseNavigateOnGridTargets.Count > 0 && ESCache.Instance.MyShipEntity.IsFrigate) || (ChooseNavigateOnGridTargets.Any(i => (!Drones.DronesKillHighValueTargets && !i.IsReadyToShoot) || (Drones.DronesKillHighValueTargets && !i.IsReadyForDronesToShoot && !Drones.DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive) || !i.IsInWebRange)))
                {
                    //
                    // we have SOMETHING that isnt in range
                    //
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("we have SOMETHING that isnt in range");
                    //if optimalrange is set - use it to determine engagement range
                    NavigateOnGridUsingSpeedTank(ChooseNavigateOnGridTargetIds);
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("..!...");
            }

            //
            // Everything is in range!
            //

            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateInAbyssalDeadspace: Everything is in range: AbyssalDeadspace_EverythingIsInRange.Count [" + AbyssalDeadspace_EverythingIsInRange.Count() + "]");

            EntityCache correctTargetToBeApproaching = null;
            foreach (long NavigateOnGridEntityId in AbyssalDeadspace_EverythingIsInRange)
            {
                if (correctTargetToBeApproaching != null)
                    break;

                foreach (EntityCache EntityOnGrid in ESCache.Instance.EntitiesOnGrid.Where(i => i.Id == NavigateOnGridEntityId))
                {
                    correctTargetToBeApproaching = EntityOnGrid;
                    break;
                }

                continue;
            }

            if (correctTargetToBeApproaching == null)
            {
                Log.WriteLine("NavigateOnGridUsingSpeedTank: target was null!/!");
                return true;
            }

            Log.WriteLine("NavigateInAbyssalDeadspace: Orbit [" + correctTargetToBeApproaching.Name + "] at [500m]");
            correctTargetToBeApproaching.Orbit(500);
            return true;
        }

        private static Stopwatch NavigateIntoRangeStopwatch = new Stopwatch();

        //
        // Goals
        // Navigate the ship on the grid so that:
        // 1) we respect BastionAndSiegeModules Mode if active and do not try to move while bastion is active
        // 2) You are always avoiding bumping into large objects in space like LCOs / Gates / etc.
        // 3) You are speed tanking or Not Speed tanking depending on the speedtank setting
        // 4) You are moving into:
        //        a) targeting range
        //        b) weapons range * 0.8
        //        c) optimal range (if you have guns fitted)
        //
        public static void NavigateIntoRange(List<long> NavigateIntoRangeTargetIds, string module, bool moveMyShip)
        {
            try
            {
                EntityCache target =  ESCache.Instance.EntitiesOnGrid.Find(i => NavigateIntoRangeTargetIds.Contains(i.Id));
                NavigateIntoRangeStopwatch.Restart();
                /**
                if (AbyssalDeadspaceBehavior.TriglavianConstructionSiteSpawnFoundDozenPlusBSs)
                {
                    if (!DebugConfig.DebugNavigateOnGrid)
                    {
                        DebugConfig.DebugNavigateOnGrid = true;
                        Log.WriteLine("TriglavianConstructionSiteFound: Enabling DebugNavigateOngrid [" + DebugConfig.DebugNavigateOnGrid + "]");
                    }
                }
                else
                {
                    if (DebugConfig.DebugNavigateOnGrid)
                    {
                        Log.WriteLine("Disabling DebugNavigateOngrid [" + DebugConfig.DebugNavigateOnGrid + "]");
                        DebugConfig.DebugNavigateOnGrid = false;
                    }
                }
                **/

                NavigateIntoRangeStopwatch.Stop();
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange 1: Took [" + Util.ElapsedMicroSeconds(NavigateIntoRangeStopwatch) + "]");
                NavigateIntoRangeStopwatch.Restart();

                if (!ESCache.Instance.InSpace || ESCache.Instance.InWarp || (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity.HasInitiatedWarp) || !moveMyShip)
                    return;

                if (DateTime.UtcNow < NextNavigateIntoRange)
                    return;

                if (ESCache.Instance.SelectedController == "AbyssalDeadspaceController")
                {
                    if (!ESCache.Instance.InAbyssalDeadspace)
                        return;

                    if (Statistics.StartedPocket.AddSeconds(12) > DateTime.UtcNow)
                        return;
                }

                if (ESCache.Instance.ActiveShip.Entity.Velocity > 0)
                {
                    NextNavigateIntoRange = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(200, 550));
                }
                else NextNavigateIntoRange = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(4, 5));

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                    return;

                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange Started");

                NavigateIntoRangeStopwatch.Stop();
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange 2: Took [" + Util.ElapsedMicroSeconds(NavigateIntoRangeStopwatch) + "]");
                NavigateIntoRangeStopwatch.Restart();

                LogWhereAmIOnGrid();

                NavigateIntoRangeStopwatch.Stop();
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange LogWhereAmIOnGrid: Took [" + Util.ElapsedMicroSeconds(NavigateIntoRangeStopwatch) + "]");
                NavigateIntoRangeStopwatch.Restart();

                if (!GlobalAvoidBumpingIntoThings(target))
                {
                    Log.WriteLine("NavigteOnGrid: NavigateIntoRange: Waiting to proceed until AvoidBumpingThings returns true;");
                    return;
                }

                if (ESCache.Instance.InWormHoleSpace)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigteOnGrid: NavigateIntoRange: NavigateInWSpace: Do not move");
                    return;
                }

                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigteOnGrid: NavigateIntoRange: NavigateInAbyssalDeadspace");
                    if (!NavigateInAbyssalDeadspace()) return;
                    return;
                }

                if (ActionControl.PerformingLootActionNow)
                    return;

                if (target == null)
                {
                    Log.WriteLine($"Target is null.");
                    return;
                }

                if (!target.IsValid)
                {
                    Log.WriteLine($"Target is not valid.");
                    return;
                }

                NavigateIntoRangeStopwatch.Stop();
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange 3: Took [" + Util.ElapsedMicroSeconds(NavigateIntoRangeStopwatch) + "]");
                NavigateIntoRangeStopwatch.Restart();

                if (!Combat.Combat.PotentialCombatTargets.Any())
                {
                    if (ESCache.Instance.AccelerationGates.Any(i => i.Distance > (double)Distances.GateActivationRange))
                    {
                        ESCache.Instance.AccelerationGates.FirstOrDefault().Orbit(100);
                        return;
                    }

                    return;
                }

                if (SpeedTank)
                {
                    NavigateIntoRangeStopwatch.Restart();

                    List<long> ListOfNavigateOnGridTargetIds = new List<long>
                    {
                        target.Id
                    };

                    NavigateOnGridUsingSpeedTank(ListOfNavigateOnGridTargetIds);

                    NavigateIntoRangeStopwatch.Stop();
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGridUsingSpeedTank: Took [" + Util.ElapsedMicroSeconds(NavigateIntoRangeStopwatch) + "]");
                    NavigateIntoRangeStopwatch.Restart();
                }
                else
                //if we are not speed tanking then check optimalrange setting, if that is not set use the less of targeting range and weapons range to dictate engagement range
                {
                    if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                    {
                        if (MissionSettings.MyMission != null && !string.IsNullOrEmpty(MissionSettings.MyMission.Name) && MissionSettings.MyMission.Name.Contains("Anomic"))
                        {
                            target.KeepAtRange(OptimalRange);
                            return;
                        }

                        //if (ESCache.Instance.MyShipEntity.IsFrigate)
                        //{
                        //    target.KeepAtRange(OrbitDistanceToUse);
                        //    return;
                        //}

                        //if optimalrange is set - use it to determine engagement range
                        if (ESCache.Instance.Weapons.Any(i => i.IsTurret))
                        {
                            NavigateIntoRangeStopwatch.Restart();
                            List<long> ListNavigateOnGridTargetIds = new List<long>
                            {
                                target.Id
                            };

                            NavigateOnGridUsingOptimalRange(ListNavigateOnGridTargetIds);
                            NavigateIntoRangeStopwatch.Stop();
                            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGridUsingOptimalRange: Took [" + Util.ElapsedMicroSeconds(NavigateIntoRangeStopwatch) + "]");
                        }
                        else //if optimalrange is not set use MaxRange (shorter of weapons range and targeting range)
                        {
                            NavigateIntoRangeStopwatch.Restart();
                            List<long> ListNavigateOnGridTargetIds = new List<long>
                            {
                                target.Id
                            };

                            NavigateOnGridUsingMaxRange(ListNavigateOnGridTargetIds);
                            NavigateIntoRangeStopwatch.Stop();
                            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGridUsingMaxRange: Took [" + Util.ElapsedMicroSeconds(NavigateIntoRangeStopwatch) + "]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool NavigateToTarget(EntityCache target, int distanceFromTarget)
        {
            if (!ESCache.Instance.InSpace || ESCache.Instance.InWarp || (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity.HasInitiatedWarp))
                return false;

            if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive && Traveler.Cloak != null && Traveler.Cloak._module.ReactivationDelay > 0)
                return false;

            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile) return false;

            if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugTraveler) Log.WriteLine("NavigateToTarget: target [" + target.Name + "] distance [" + target.Nearest5kDistance + "] GroupID [" + target.GroupId + "]");

            LogWhereAmIOnGrid();
            if (!GlobalAvoidBumpingIntoThings(target))
            {
                Log.WriteLine("NavigateToTarget: Waiting to proceed until AvoidBumpingThings returns true;");
                return false;
            }

            // if we are inside warpto range you need to approach (you cant warp from here)
            if (target.Distance < (int) Distances.WarptoDistance)
            {
                if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugTraveler) Log.WriteLine("NavigateToTarget: if (target.Distance < (int) Distances.WarptoDistance)");

                /**
                if (false && orbit && target.GroupId != (int) Group.Station && target.GroupId != (int) Group.Stargate)
                {
                    if (target.Distance < distanceFromTarget)
                        return true;

                    if (DateTime.UtcNow > Time.Instance.NextOrbit)
                    {
                        //we cant move in bastion mode, do not try
                        List<ModuleCache> bastionModules = null;
                        bastionModules = ESCache.Instance.Modules.Where(m => m.GroupId == (int) Group.BastionAndSiegeModules && m.IsOnline).ToList();
                        if (bastionModules.Any(i => i.UseScheduler)) return false;

                        //Log.WriteLine("StartOrbiting: Target in range");
                        if (!target.IsApproachedByActiveShip && !target.IsOrbitedByActiveShip || target.Distance > distanceFromTarget)
                        {
                            //Log.WriteLine("We are not approaching nor orbiting");
                            target.Orbit(distanceFromTarget - 1500);
                            return false;
                        }
                    }
                }
                **/

                if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateToTarget: if (DateTime.UtcNow > Time.Instance.NextApproachAction)");

                    if (target.Distance < distanceFromTarget)
                        return true;

                    //if (target.KeepAtRange(distanceFromTarget - 1500))
                    if (ESCache.Instance.InAbyssalDeadspace && Combat.Combat.PotentialCombatTargets.Any())
                    {
                        if (!target.Orbit(500)) return false;
                        return false;
                    }

                    if (!target._directEntity.Approach()) return false;
                    return false;
                }

                return false;

                //
                // do nothing here. If we havent approached or orbited its because we are waiting before spamming the commands again.
                //
            }
            if (target.Distance > (int) Distances.WarptoDistance)
            {
                if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugTraveler) Log.WriteLine("NavigateToTarget: if (target.Distance > (int) Distances.WarptoDistance)");

                if (ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.Velocity == 0 && Defense.CovertOpsCloak == null)
                {
                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugTraveler) Log.WriteLine("NavigateToTarget: target.AlignTo()");
                    target.AlignTo();
                    return false;
                }

                if (Drones.DronePriorityEntities.Any(pt => pt.IsWarpScramblingMe) ||
                    Combat.Combat.PrimaryWeaponPriorityEntities.Any(pt => pt.IsWarpScramblingMe))
                {
                    EntityCache WarpScrambledBy = Drones.DronePriorityEntities.Find(pt => pt.IsWarpScramblingMe) ??
                                                  Combat.Combat.PrimaryWeaponPriorityEntities.Find(pt => pt.IsWarpScramblingMe);
                    if (WarpScrambledBy != null && DateTime.UtcNow > _nextWarpScrambledWarning)
                    {
                        _nextWarpScrambledWarning = DateTime.UtcNow.AddSeconds(20);
                        Log.WriteLine("We are scrambled by: [" + WarpScrambledBy.Name + "][" +
                                      Math.Round(WarpScrambledBy.Distance / 1000, 0) + "k][" + WarpScrambledBy.Id +
                                      "]");
                        LastWarpScrambled = DateTime.UtcNow;
                    }

                    return false;
                }

                if (target.WarpTo())
                {
                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugTraveler) Log.WriteLine("NavigateToTarget: if (target.WarpTo())");
                    return false;
                }

                if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugTraveler) Log.WriteLine("NavigateToTarget: if (!target.WarpTo())");
            }

            return false;
        }

        private static EntityCache _bioAdaptiveCacheOrWreckOrAbyssalGate = null;

        public static EntityCache BioAdaptiveCacheOrWreckOrAbyssalGate
        {
            get
            {
                if (_bioAdaptiveCacheOrWreckOrAbyssalGate != null)
                {
                    return _bioAdaptiveCacheOrWreckOrAbyssalGate;
                }
                //EntityCache target = null;
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToBeNavigateOnGridTarget))
                    {
                        if (Salvage.TractorBeams.Count == 0 || (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway))
                            _bioAdaptiveCacheOrWreckOrAbyssalGate = ESCache.Instance.EntitiesOnGrid.Find(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache);
                    }
                    else if (ESCache.Instance.Wrecks.Any(i => i.IsWreckReadyToBeNavigateOnGridTarget))
                    {
                        if (Salvage.TractorBeams.Count == 0 || (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway))
                            _bioAdaptiveCacheOrWreckOrAbyssalGate = ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty);
                    }
                    else if (ESCache.Instance.AbyssalGate != null && ESCache.Instance.AbyssalGate.IsAbyssalAccelerationGateReadyToBeNavigateOnGridTarget)
                    {
                        _bioAdaptiveCacheOrWreckOrAbyssalGate = ESCache.Instance.AbyssalGate;
                    }

                    if (_bioAdaptiveCacheOrWreckOrAbyssalGate == null)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("OrbitBioAdaptiveCacheOrGateIfWeStrayTooFarAway: if (target == null)");
                        return null;
                    }

                    return _bioAdaptiveCacheOrWreckOrAbyssalGate;
                }

                return null;
            }
        }

        private static EntityCache _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes { get; set; } = null;

        public static EntityCache BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes
        {
            get
            {
                if (_bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes != null)
                {
                    return _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes;
                }

                //EntityCache target = null;
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache))
                    {
                        if (Salvage.TractorBeams.Count == 0 || (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway))
                        {
                            _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes = ESCache.Instance.EntitiesOnGrid.Find(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache);
                            return _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes ?? ESCache.Instance.AbyssalGate;
                        }

                        _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes = ESCache.Instance.AbyssalGate;
                        return _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes ?? ESCache.Instance.AbyssalGate;
                    }
                    else if (ESCache.Instance.Wrecks.Any(i => i.IsWreck && !i.IsWreckEmpty))
                    {
                        if (Salvage.TractorBeams.Count == 0 || (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway))
                        {
                            _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes = ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty);
                            return _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes ?? ESCache.Instance.AbyssalGate;
                        }

                        _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes = ESCache.Instance.AbyssalGate;
                        return _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes ?? ESCache.Instance.AbyssalGate;
                    }
                    else if (ESCache.Instance.AbyssalGate != null)
                    {
                        _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes = ESCache.Instance.AbyssalGate;
                        return _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes ?? ESCache.Instance.AbyssalGate;
                    }

                    if (_bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes == null)
                    {
                        Log.WriteLine("OrbitBioAdaptiveCacheOrGateIfWeStrayTooFarAway: if (target == null)");
                        return null;
                    }

                    return _bioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes;
                }

                return null;
            }
        }

        public static bool OrbitBioAdaptiveCacheOrGateIfWeStrayTooFarAway()
        {
            if (!ESCache.Instance.InSpace)
                return true;

            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile) return false;

            if (BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes == null)
                return true;

            //double AbyssalDangerDistanceFromCenter = 32000;
            //if (SpeedTank) AbyssalDangerDistanceFromCenter = 29000;

            try
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    try
                    {
                        if (!IsOurShipWithintheAbyssBounds())
                        {
                            BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes.Orbit(500, false, "Orbit BioAdaptiveCache or Gate [" + BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes.Name + "][" + Math.Round(BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes.Distance / 1000, 0) + "k away]@[500m] we are outside the abyssal boundry!");
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        //ignore this exception
                    }

                    try
                    {
                        if (ESCache.Instance.ActiveShip.MaxVelocity > 4000)
                        {
                            BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes.Orbit(500, false, "Orbit BioAdaptiveCache or Gate [" + BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes.Name + "][" + Math.Round(BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes.Distance / 1000, 0) + "k away]@[500m] we are outside the abyssal boundry!");
                            Log.WriteLine("ActiveShip MaxVelocity [" + ESCache.Instance.ActiveShip.MaxVelocity + "] IsLocatedWithinFilamentCloud [" + ESCache.Instance.ActiveShip.Entity.IsLocatedWithinFilamentCloud + "]");
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        //swallow exception
                    }

                    if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway)
                    {
                        BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes.Orbit(500, false, "Orbit BioAdaptiveCache or Gate [" + BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes.Name + "][" + Math.Round(BioAdaptiveCacheOrWreckOrAbyssalGateForBoundryPurposes.Distance / 1000, 0) + "k away]@[500m] TriglavianConstructionSiteSpawnFoundDozenPlusBSs");
                        return true;
                    }

                    //if (ESCache.Instance.ActiveShip.FollowingEntity == null)
                    //{
                    //    target.Orbit(500, false, "Orbit BioAdaptiveCache or Gate [" + target.Name + "][" + Math.Round(target.Distance / 1000, 0) + "k away]@[500m] ActiveShip.FollowingEntity == null");
                    //    return true;
                    //}
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            return true;
        }

        private static EntityCache _orbitThisTarget;

        public static EntityCache OrbitThisTarget
        {
            get
            {
                //
                // this is not yet in use: work in progress: to replace the mess that is the structure var in OrbitGateorTarget
                //

                if (_orbitThisTarget == null)
                {
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Where(e => e.Velocity == 0 && (e.IsAbyssalAccelerationGateReadyToBeNavigateOnGridTarget || e.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToBeNavigateOnGridTarget || e.IsWreckReadyToBeNavigateOnGridTarget))
                        .OrderByDescending(j => ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway && (j.IsWreck && !j.IsWreckEmpty))
                        .ThenByDescending(j => Salvage.TractorBeams.Count == 0 && (j.IsWreck && !j.IsWreckEmpty))
                        .ThenByDescending(j => ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway && j.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                        .ThenByDescending(k => Salvage.TractorBeams.Count == 0  && k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                        //.ThenByDescending(i => i.IsMobileTractor)
                        .ThenByDescending(i => i.IsAccelerationGate)
                        .ThenByDescending(i => i.Distance)
                        .FirstOrDefault();

                        return _orbitThisTarget;
                    }

                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && ESCache.Instance.InMission && MissionSettings.MyMission != null)
                    {
                        switch (MissionSettings.MyMission.Name.ToLower())
                        {
                            case "dread pirate scarlet":
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: [" + MissionSettings.MyMission.Name + "] detected");
                                if (_orbitThisTarget == null && ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAccelerationGate))
                                {
                                    _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Where(i => i.IsAccelerationGate).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }

                                Defense.AlwaysActivateSpeedModForThisGridOnly = true;
                                break;

                            case "duo of death":
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: [" + MissionSettings.MyMission.Name + "] detected");
                                if (Combat.Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Gist Seraphim")))
                                {
                                    _orbitThisTarget = Combat.Combat.PotentialCombatTargets.Where(i => i.Name.Contains("Gist Seraphim")).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }

                                Defense.AlwaysActivateSpeedModForThisGridOnly = true;
                                break;

                            case "smuggler interception":
                            case "unauthorized military presence":
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: [" + MissionSettings.MyMission.Name + "] detected");
                                if (_orbitThisTarget == null)
                                {
                                    _orbitThisTarget = Combat.Combat.PotentialCombatTargets.Where(i => i.Name.Contains("Personnel Transport")).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }

                                if (_orbitThisTarget == null)
                                {
                                    _orbitThisTarget = ESCache.Instance.UnlootedWrecksAndSecureCans.Where(i => i.Name.Contains("Personnel Transport Wreck")).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }

                                break;

                            case "the damsel in distress":
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: [" + MissionSettings.MyMission.Name + "] detected");
                                if (_orbitThisTarget == null && ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.ToLower().Contains("Pleasure Gardens".ToLower())))
                                {
                                    _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower().Contains("Pleasure Gardens".ToLower())).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }
                                if (_orbitThisTarget == null)
                                {
                                    _orbitThisTarget = ESCache.Instance.UnlootedWrecksAndSecureCans.Where(i => i.Name.ToLower().Contains("Cargo Container".ToLower())).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }
                                break;

                            case "the right hand of zazzmatazz":
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: [" + MissionSettings.MyMission.Name + "] detected");
                                if (ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.ToLower().Contains("Outpost Headquarters".ToLower())))
                                {
                                    _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower().Contains("Outpost Headquarters".ToLower())).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }
                                if (_orbitThisTarget == null)
                                {
                                    _orbitThisTarget = ESCache.Instance.UnlootedWrecksAndSecureCans.Where(i => i.Name.ToLower().Contains("Outpost Headquarters Wreck".ToLower())).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }
                                Defense.AlwaysActivateSpeedModForThisGridOnly = true;
                                break;

                            case "the rogue slave trader (1 of 2)":
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: [" + MissionSettings.MyMission.Name + "] detected");
                                if (ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.ToLower().Contains("Slave Pen".ToLower())))
                                {
                                    _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower().Contains("Slave Pen".ToLower())).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }

                                if (_orbitThisTarget == null)
                                {
                                    _orbitThisTarget = ESCache.Instance.UnlootedWrecksAndSecureCans.Where(i => i.Name.ToLower().Contains("Cargo Container".ToLower())).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }
                                break;

                            case "In the Midst of Deadspace (1 of 5)":
                            case "In the Midst of Deadspace (3 of 5)":
                            case "In the Midst of Deadspace (4 of 5)":
                            case "In the Midst of Deadspace (5 of 5)":
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: [" + MissionSettings.MyMission.Name + "] detected");
                                _orbitThisTarget = null;
                                if (ESCache.Instance.EntitiesOnGrid != null && DetermineWhichWrecksToApproach.Any(i => i.IsLargeWreck))
                                {
                                    _orbitThisTarget = DetermineWhichWrecksToApproach.Where(i => i.IsLargeWreck).OrderBy(i => i.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }

                                if (_orbitThisTarget == null)
                                {
                                    _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Find(i => i.IsLargeCollidableWeAlwaysWantToBlowupFirst);
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }

                                if (_orbitThisTarget == null)
                                {
                                    _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Find(i => i.IsLargeCollidableWeAlwaysWantToBlowupLast);
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }
                                break;

                            case "silence the informant":
                            case "rogue drone harassment":
                                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: [" + MissionSettings.MyMission.Name + "] detected");
                                if (ESCache.Instance.EntitiesNotSelf.All(i => !i.IsMobileTractor))
                                {
                                    if (Combat.Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Elite Drone Parasite")))
                                    {
                                        _orbitThisTarget = Combat.Combat.PotentialCombatTargets.Where(i => i.Name.Contains("Elite Drone Parasite")).OrderBy(t => t.Distance).FirstOrDefault();
                                        if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                    }

                                    if (_orbitThisTarget == null)
                                    {
                                        _orbitThisTarget = ESCache.Instance.UnlootedWrecksAndSecureCans.Where(i => i.Name.Contains("Elite Drone Parasite Wreck")).OrderBy(t => t.Distance).FirstOrDefault();
                                        if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                    }
                                }

                                if (_orbitThisTarget == null)
                                {
                                    _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Where(i => i.IsAccelerationGate).OrderBy(t => t.Distance).FirstOrDefault();
                                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                                }

                                break;
                        }

                        if (_orbitThisTarget == null)
                        {
                            _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Where(i => i.IsWreck && !i.IsWreckEmpty && i.HaveLootRights).OrderBy(t => t.Distance).FirstOrDefault();
                            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateOnGrid: OrbitGateOrTarget: structure [" + _orbitThisTarget.Name + "]");
                        }
                    }

                    _orbitThisTarget = ESCache.Instance.EntitiesOnGrid.Where(e => e.Velocity == 0 && e.IsReadyToTarget && (e.IsAbyssalAccelerationGateReadyToBeNavigateOnGridTarget || e.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToBeNavigateOnGridTarget))
                        .OrderByDescending(i => !string.IsNullOrEmpty(ESCache.Instance.OrbitEntityNamed) && i.Name.Contains(ESCache.Instance.OrbitEntityNamed))
                        .ThenByDescending(i => Salvage.LootEverything && i.IsWreck && !i.IsWreckEmpty && i.HaveLootRights)
                        //.ThenByDescending(i => i.IsMobileTractor)
                        .ThenByDescending(i => i.IsAccelerationGate)
                        .ThenByDescending(i => i.Distance)
                        .FirstOrDefault();

                    return _orbitThisTarget;
                }

                return _orbitThisTarget;
            }
        }


        public static void OrbitGateorTarget(EntityCache target)
        {
            if (!ESCache.Instance.InSpace || ESCache.Instance.InWarp || (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity.HasInitiatedWarp))
                return;

            if (DateTime.UtcNow > Time.Instance.NextOrbit)
            {
                //we cant move in bastion mode, do not try
                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile) return;

                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("OrbitGateorTarget Started");

                if (target.Distance < Combat.Combat.MaxRange - 5000)
                {
                    if (DebugConfig.DebugNavigateOnGrid)
                        Log.WriteLine("if (target.Distance < Combat.MaxRange - 5000)");

                    //Logging.Log("CombatMissionCtrl." + _pocketActions[_currentAction] ,"StartOrbiting: Target in range");
                    if (!target.IsOrbitedByActiveShip && !target.IsApproachedByActiveShip)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log.WriteLine("We are not approaching nor orbiting");

                        //
                        // Prefer to orbit the last structure defined in
                        // Cache.Instance.OrbitEntityNamed
                        //
                        if (OrbitStructure)
                        {
                            //
                            // if we have a structure to orbit do so
                            //
                            if (OrbitThisTarget != null)
                            {
                                if (!Combat.Combat.PotentialCombatTargets.Any())
                                {
                                    if (target._directEntity.Approach())
                                    {
                                        Log.WriteLine("Initiating Approach [" + target.Name + "][ID: " + target.MaskedId + "] OrbitStructure == true");
                                        return;
                                    }

                                    return;
                                }

                                if (OrbitThisTarget.Distance > 50000 && ESCache.Instance.ActiveShip.MaxVelocity > 600)
                                    if (OrbitThisTarget.Orbit(30000, false, "Initiating Orbit [" + OrbitThisTarget.Name + "][" + Math.Round(OrbitThisTarget.Distance / 1000, 0) + "k] at [" + Math.Round((double) OrbitDistanceToUse / 1000, 0) + "k][" + OrbitThisTarget.MaskedId + "]."))
                                        return;

                                if (OrbitThisTarget.Distance > 40000 && ESCache.Instance.ActiveShip.MaxVelocity > 600)
                                    if (OrbitThisTarget.Orbit(20000, false, "Initiating Orbit [" + OrbitThisTarget.Name + "][" + Math.Round(OrbitThisTarget.Distance / 1000, 0) + "k] at [" + Math.Round((double) OrbitDistanceToUse / 1000, 0) + "k][" + OrbitThisTarget.MaskedId + "]!"))
                                        return;

                                if (OrbitThisTarget.Distance > 30000 && ESCache.Instance.ActiveShip.MaxVelocity > 600)
                                    if (OrbitThisTarget.Orbit(15000, false, "Initiating Orbit [" + OrbitThisTarget.Name + "][" + Math.Round(OrbitThisTarget.Distance / 1000, 0) + "k] at [" + Math.Round((double) OrbitDistanceToUse / 1000, 0) + "k][" + OrbitThisTarget.MaskedId + "].!"))
                                        return;

                                if (OrbitThisTarget.Distance > 20000 && ESCache.Instance.ActiveShip.MaxVelocity > 600)
                                    if (OrbitThisTarget.Orbit(10000, false, "Initiating Orbit [" + OrbitThisTarget.Name + "][" + Math.Round(OrbitThisTarget.Distance / 1000, 0) + "k] at [" + Math.Round((double) OrbitDistanceToUse / 1000, 0) + "k][" + OrbitThisTarget.MaskedId + "]!."))
                                        return;

                                if (OrbitThisTarget.Orbit(500, false, "Initiating Orbit [" + OrbitThisTarget.Name + "][" + Math.Round(OrbitThisTarget.Distance / 1000, 0) + "k] at [" + Math.Round((double) OrbitDistanceToUse / 1000, 0) + "k][" + OrbitThisTarget.MaskedId + "]!!"))
                                    return;

                                return;
                            }
                        }

                        //
                        // OrbitStructure is false
                        //
                        if (SpeedTank)
                        {
                            if (!Combat.Combat.PotentialCombatTargets.Any())
                            {
                                if (target._directEntity.Approach())
                                {
                                    Log.WriteLine("Initiating Approach [" + target.Name + "][ID: " + target.MaskedId + "] speedtank == true");
                                    return;
                                }

                                return;
                            }

                            if (target.Orbit(OrbitDistanceToUse, false, "Initiating Orbit [" + target.Name + "] at [" + Math.Round((double) OrbitDistanceToUse / 1000, 0) + "k][ID: " + target.MaskedId + "]!"))
                                return;

                            return;
                        }

                        //
                        // OrbitStructure is false
                        // SpeedTank is false
                        //
                        if (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 300 && (target.IsNPCFrigate || target.IsFrigate)) //this will spam a bit until we know what "mode" our active ship is when aligning
                            if (Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                                if (ESCache.Instance.Star.AlignTo())
                                    Log.WriteLine("Aligning to the Star so we might possibly hit [" + target.Name + "][ID: " + target.MaskedId +
                                                  "][ActiveShip.Entity.Mode:[" + ESCache.Instance.ActiveShip.Entity.Mode + "]");

                        if (!Combat.Combat.PotentialCombatTargets.Any())
                        {
                            if (target.Approach())
                            {
                                Log.WriteLine("Initiating Approach [" + target.Name + "][ID: " + target.MaskedId + "] speedtank == false && orbitstructure == false");
                                return;
                            }

                            return;
                        }

                        if (target.Orbit(OrbitDistanceToUse, false, "Initiating Orbit [" + target.Name + "] at [" + Math.Round((double) OrbitDistanceToUse / 1000, 0) + "k][ID: " + target.MaskedId + "] speedtank == false && orbitstructure == false"))
                            return;
                    }
                }
                else
                {
                    if (target.Orbit(OrbitDistanceToUse, false, "Target [" + target.Name + "]@[" + Math.Round(target.Distance / 1000, 0) + "k] outside MaxRange [" + Math.Round(Combat.Combat.MaxRange / 1000, 0) + "k]. orbiting @ [" + OrbitDistanceToUse + "]...."))
                        return;
                }
            }
        }

        private static List<EntityCache> _determineWhichWrecksToApproach;

        public static List<EntityCache> DetermineWhichWrecksToApproach
        {
            get
            {
                try
                {
                    if (_determineWhichWrecksToApproach != null)
                        return _determineWhichWrecksToApproach;

                    _determineWhichWrecksToApproach = LargeWrecksToApproach.Concat(MediumWrecksToApproach.Concat(SmallWrecksToApproach)).ToList();
                    return _determineWhichWrecksToApproach;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<EntityCache>();
                }
            }
        }

        public static List<EntityCache> LargeWrecksToApproach
        {
            get
            {
                List<EntityCache> _largeWrecksToApproach = new List<EntityCache>();
                _largeWrecksToApproach = ESCache.Instance.UnlootedContainers.Where(i => !i.IsInTractorRange && i.IsLargeWreck && !i.IsWreckEmpty && i.HaveLootRights)
                .OrderBy(i => i.Distance).ToList();

                return _largeWrecksToApproach;
            }
        }

        public static List<EntityCache> MediumWrecksToApproach
        {
            get
            {
                List<EntityCache> _mediumWrecksToApproach = new List<EntityCache>();
                _mediumWrecksToApproach = ESCache.Instance.UnlootedContainers.Where(i => !i.IsInTractorRange && !i.IsLargeWreck && !i.IsSmallWreck && !i.IsWreckEmpty && i.HaveLootRights)
                .OrderBy(i => i.Distance).ToList();

                return _mediumWrecksToApproach;
            }
        }

        public static List<EntityCache> SmallWrecksToApproach
        {
            get
            {
                List<EntityCache> _smallWrecksToApproach = new List<EntityCache>();
                _smallWrecksToApproach = ESCache.Instance.UnlootedContainers.Where(i => !i.IsInTractorRange && !i.IsLargeWreck && !i.IsMediumWreck && !i.IsWreckEmpty && i.HaveLootRights)
                .OrderBy(i => i.Distance).ToList();

                return _smallWrecksToApproach;
            }
        }

        public static void Reset()
        {
            StationIdToGoto = null;
        }

        public static bool StopMyShip(string reason)
        {
            if (DateTime.UtcNow > Time.Instance.NextStopAction)
            {
                if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip))
                {
                    Log.WriteLine("NavigateOnGrid: StopMyShip: [" + reason + "]");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                    if (ESCache.Instance.EveAccount.IsLeader)
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderLastStoppedShip), DateTime.UtcNow);

                    Time.Instance.NextStopAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(4, 6));
                    return true;
                }

                return false;
            }

            return false;
        }

        internal static bool PerformFinalDestinationTask(long stationId, string stationName)
        {
            try
            {
                if (TravelerDestination.NextTravelerDestinationAction > DateTime.UtcNow)
                    return false;

                StationIdToGoto = stationId;

                if ((ESCache.Instance.InStation && ESCache.Instance.DirectEve.Session.StationId == stationId) ||
                    ESCache.Instance.DirectEve.Session.Structureid == stationId)
                {
                    Log.WriteLine("Arrived in station.");
                    return true;
                }

                if (ESCache.Instance.InStation)
                {
                    // We are in a station, but not the correct station!
                    if (DateTime.UtcNow > Time.Instance.NextUndockAction)
                    {
                        if (TravelerDestination.Undock())
                            return false;

                        return false;
                    }

                    // We are not there yet
                    return false;
                }

                if (!ESCache.Instance.InSpace)
                    return false;

                TravelerDestination.UndockAttempts = 0;

                if (ESCache.Instance.DockableLocations.Count == 0)
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("PerformFinalDestinationTask: No Stations?!");
                    return false;
                }

                if (DebugConfig.DebugTraveler) Log.WriteLine("PerformFinalDestinationTask: Looking for station named [ " + stationName + " ] ID [" + stationId + "]");

                if (StationToGoTo == null)
                {
                    if (DebugConfig.DebugTraveler)
                        Log.WriteLine("if (stationToGoTo == null)");

                    return false;
                }

                if (StationToGoTo.Distance <= (int) Distances.DockingRange)
                {
                    if (StationToGoTo.Dock())
                    {
                        Log.WriteLine("Dock at [" + StationToGoTo.Name + "] which is [" + Math.Round(StationToGoTo.Distance / 1000, 0) +
                                      "k away]");
                        TravelerDestination.NextTravelerDestinationAction = DateTime.UtcNow.AddSeconds(15);
                        return false; //we do not return true until we actually appear in the destination (station in this case)
                    }

                    return false;
                }

                if (StationToGoTo.Distance < (int) Distances.WarptoDistance)
                {
                    if (StationToGoTo.Approach())
                        Log.WriteLine("Approaching [" + StationToGoTo.Name + "] which is [" + Math.Round(StationToGoTo.Distance / 1000, 0) + "k away]");

                    return false;
                }

                EntityCache bigObject = ESCache.Instance.BigObjects.FirstOrDefault();
                AvoidBumpingThings(bigObject, "NavigateOnGrid: PerformFinalDestinationTask", SafeDistanceFromStructure, AvoidBumpingThingsBool());

                if (!AvoidBumpingThingsBool() || (bigObject != null && bigObject.Distance > 2000) || bigObject == null)
                {
                    try
                    {
                        if (Settings.Instance.UseDockBookmarks)
                        {
                            if (DebugConfig.DebugTraveler) Log.WriteLine("UseDockBookmarks [" + Settings.Instance.UseDockBookmarks + "] State.CurrentInstaStationDockState [" + State.CurrentInstaStationDockState + "]");
                            if (State.CurrentInstaStationDockState != InstaStationDockState.Done &&
                                State.CurrentInstaStationDockState != InstaStationDockState.WaitForTraveler)
                            {
                                InstaStationDock.ProcessState();
                                if (State.CurrentInstaStationDockState != InstaStationDockState.Done &&
                                    State.CurrentInstaStationDockState != InstaStationDockState.WaitForTraveler)
                                    return false;
                            }
                        }

                        if (StationToGoTo != null && StationToGoTo.WarpTo())
                        {
                            if (DebugConfig.DebugTraveler) Log.WriteLine("UseDockBookmarks [" + Settings.Instance.UseDockBookmarks + "]");
                            if (DebugConfig.DebugTraveler) Log.WriteLine("if (station.WarpTo())");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }
                }

                if (DebugConfig.DebugTraveler) Log.WriteLine("BigObject.Distance less than 2000m?");
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static void AvoidBumpingThingsReset()
        {
            AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
            lastTooCloseToEntity = DateTime.UtcNow.AddHours(-5);
            SafeDistanceFromStructureMultiplier = 1;
            AvoidBumpingThingsWarningSent = false;
        }

        private static void LogOrderOfNavigateOnGridTargets(List<long> navOnGridTargetIds)
        {
            int targetnum = 0;
            Log.WriteLine("----------------[ navongridtargets ]------------------");
            if (navOnGridTargetIds != null && navOnGridTargetIds.Count > 0)
                foreach (long myNavOnGridTargetId in navOnGridTargetIds)
                {
                    EntityCache myNavigateOnGridEntityCacheTarget = ESCache.Instance.EntitiesOnGrid.Find(i => i.Id == myNavOnGridTargetId);
                    if (myNavigateOnGridEntityCacheTarget == null)
                        continue;

                    targetnum++;
                    Log.WriteLine(targetnum + ";" + myNavigateOnGridEntityCacheTarget.Name + ";" + Math.Round(myNavigateOnGridEntityCacheTarget.Distance / 1000, 0) + "k;" + myNavigateOnGridEntityCacheTarget.IsBattleship + ";BC;" + myNavigateOnGridEntityCacheTarget.IsBattlecruiser + ";C;" + myNavigateOnGridEntityCacheTarget.IsCruiser + ";F;" + myNavigateOnGridEntityCacheTarget.IsFrigate + ";isAttacking;" + myNavigateOnGridEntityCacheTarget.IsAttacking + ";IsTargetedBy;" + myNavigateOnGridEntityCacheTarget.IsTargetedBy + ";IsWarpScramblingMe;" + myNavigateOnGridEntityCacheTarget.IsWarpScramblingMe + ";IsNeutralizingMe;" + myNavigateOnGridEntityCacheTarget.IsNeutralizingMe + ";Health;" + myNavigateOnGridEntityCacheTarget.HealthPct + ";ShieldPct;" + myNavigateOnGridEntityCacheTarget.ShieldPct + ";ArmorPct;" + myNavigateOnGridEntityCacheTarget.ArmorPct + ";StructurePct;" + myNavigateOnGridEntityCacheTarget.StructurePct);
                }

            Log.WriteLine("----------------------------------------------");
        }

        private static void NavigateOnGridUsingMaxRange(List<long> ListOfNavigateOnGridEntityIds)
        {
            EntityCache target = null;
            foreach (long NavigateOnGridEntityId in ListOfNavigateOnGridEntityIds)
            {
                if (target != null)
                    break;

                foreach (EntityCache EntityOnGrid in ESCache.Instance.EntitiesOnGrid.Where(i => i.Id == NavigateOnGridEntityId))
                {
                    target = EntityOnGrid;
                    break;
                }

                continue;
            }

            if (target == null)
            {
                Log.WriteLine("NavigateOnGridUsingMaxRange: target was null!");
                return;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("Debug: NavigateIntoRange: OptimalRange == 0 using MaxRange [" + Combat.Combat.MaxRange + "] target is [" + target.Name + "][" + target.Distance + "] what weapon systems cause this? drones only?");

            if (target.Distance > Combat.Combat.MaxRange)
                if (ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != target.Id ||
                    (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 50))
                {
                    if (target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                    {
                        OrbitGateorTarget(target);
                        return;
                    }

                    if (target.Approach())
                        Log.WriteLine("Target [" + target.Name + "][ID: " + target.MaskedId + "] is outside MaxRange [" +
                                        Math.Round(target.Distance / 1000, 0) + "k ] Approach");

                    return;
                }

            if (target.Distance <= Combat.Combat.MaxRange - 5000)
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if ((Drones.DronesKillHighValueTargets && Salvage.TractorBeams.Any() && Combat.Combat.PotentialCombatTargets.All(i => i.Distance < Drones.DroneControlRange)) || (!Drones.DronesKillHighValueTargets && Salvage.TractorBeams.Count > 0 && Combat.Combat.PotentialCombatTargets.All(i => i.Distance < Combat.Combat.MaxRange) && ESCache.Instance.AccelerationGates.Count > 0 && ESCache.Instance.AccelerationGates.FirstOrDefault()._directEntity.DirectAbsolutePosition.GetDistance(target._directEntity.DirectAbsolutePosition) + 1000 < OptimalRange))
                    {
                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        {
                            if (!ESCache.Instance.DirectEve.Me.IsOmegaClone && Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship) && ESCache.Instance.ActiveShip.HasSpeedMod)
                            {
                                if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                {
                                    Combat.Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(OrbitDistanceToUse);
                                    return;
                                }

                                return;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn ||
                            AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn ||
                            AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                        {
                            if (!ESCache.Instance.DirectEve.Me.IsOmegaClone && Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship) && ESCache.Instance.ActiveShip.HasSpeedMod)
                            {
                                if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                {
                                    Combat.Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(OrbitDistanceToUse);
                                    return;
                                }
                            }
                        }

                        if (ESCache.Instance.FollowingEntity != null && ESCache.Instance.FollowingEntity.Velocity != 0 && ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity != 0 && ESCache.Instance.AccelerationGates.Count > 0)
                        {
                            if (Combat.Combat.PotentialCombatTargets.Any())
                            {
                                if (DateTime.UtcNow > Time.Instance.NextOrbit)
                                {
                                    ESCache.Instance.AccelerationGates.FirstOrDefault().Orbit(500);
                                    Time.Instance.NextOrbit = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(10, 15));
                                    return;
                                }

                                return;
                            }

                            ESCache.Instance.AccelerationGates.FirstOrDefault().Approach();
                            return;
                        }
                    }
                }

                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange: target is NPC Frigate [" + target.Name + "][" + target.Distance + "]");
                OrbitGateorTarget(target);
            }
        }

        private static void NavigateOnGridUsingOptimalRange(List<long> ListOfNavigateOnGridEntityIds)
        {
            EntityCache target = null;
            foreach (long NavigateOnGridEntityId in ListOfNavigateOnGridEntityIds)
            {
                if (target != null)
                    break;

                if (ESCache.Instance.InAbyssalDeadspace &&  ESCache.Instance.ActiveShip.IsFrigate)
                {
                    if (ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache))
                    {
                        var bioadaptivecache = ESCache.Instance.Entities.FirstOrDefault(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache);
                        if (ESCache.Instance.ActiveShip.Entity.FollowId != bioadaptivecache.Id)
                        {
                            ESCache.Instance.Entities.FirstOrDefault(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache).Approach();
                            return;
                        }
                    }
                }

                foreach (EntityCache EntityOnGrid in ESCache.Instance.EntitiesOnGrid.Where(i => i.Id == NavigateOnGridEntityId))
                {
                    target = EntityOnGrid;
                    break;
                }

                continue;
            }

            if (target == null)
            {
                Log.WriteLine("NavigateOnGridUsingMaxRange: target was null!");
                return;
            }

            if (DebugConfig.DebugNavigateOnGrid)
                Log.WriteLine("NavigateIntoRange: OptimalRange [ " + OptimalRange + "] Current Distance to [" + target.Name + "] is [" +
                              Math.Round(target.Distance / 1000, 0) + "]");

            if (!target.IsInOptimalRange || !target.IsInWebRange)
                if (ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != target.Id ||
                    (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 50))
                {
                    if (!ESCache.Instance.InAbyssalDeadspace && target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log.WriteLine("NavigateIntoRange: target is NPC Frigate [" + target.Name + "][" +
                                          Math.Round(target.Distance / 1000, 0) + "]");

                        OrbitGateorTarget(target);
                        return;
                    }

                    int tempOrbitDistance = 1000;
                    int tempKeepAtRangeDistance = Math.Min(8000, OptimalRange);

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                        tempKeepAtRangeDistance = 500;

                    if (target.IsAccelerationGate)
                    {
                        tempOrbitDistance = 1000;
                        tempKeepAtRangeDistance = 1000;
                    }

                    if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship || i.TriglavianDamage > 0))
                    {
                        if (target.Orbit(tempOrbitDistance, false, "Using Optimal Range: Orbiting target [" + target.Name + "][ID: " + target.MaskedId + "][" + Math.Round(target.Distance / 1000, 0) + "k away]"))
                            return;
                    }
                    else
                    {
                        if (target.KeepAtRange(tempKeepAtRangeDistance))
                            Log.WriteLine("Using Optimal Range: KeepAtRange target [" + target.Name + "][ID: " + target.MaskedId + "][" +
                                          Math.Round(target.Distance / 1000, 0) + "k away] myOptimalRange [" + Math.Round((double) OptimalRange / 1000, 0) + "]");
                    }

                    return;
                }

            if (target.IsInOptimalRange)
                if (target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                {
                    if (ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != target.Id ||
                        (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 50))
                        if (target.KeepAtRange(OptimalRange))
                        {
                            Log.WriteLine("Target is NPC Frigate and we got Turrets. Keeping target at Range to hit it.");
                            Log.WriteLine("Initiating KeepAtRange [" + target.Name + "] at [" + Math.Round((double) OptimalRange / 1000, 0) +
                                          "k][ID: " + target.MaskedId + "]");
                        }
                }
                else if (ESCache.Instance.FollowingEntity != null && ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity != 0)
                {
                    //
                    // Approaching something
                    //
                    if (target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted()) return;

                    if (ESCache.Instance.FollowingEntity.Velocity != 0)
                    {
                        if ((Drones.DronesKillHighValueTargets && Salvage.TractorBeams.Count > 0 && Combat.Combat.PotentialCombatTargets.All(i => i.Distance < Drones.DroneControlRange)) || (!Drones.DronesKillHighValueTargets && Salvage.TractorBeams.Count > 0 && Combat.Combat.PotentialCombatTargets.All(i => i.Distance < Combat.Combat.MaxRange) && ESCache.Instance.AccelerationGates.Count > 0 && ESCache.Instance.AccelerationGates.FirstOrDefault()._directEntity.DirectAbsolutePosition.GetDistance(target._directEntity.DirectAbsolutePosition) + 1000 < OptimalRange))
                        {
                            if (DateTime.UtcNow > Time.Instance.NextOrbit)
                            {
                                ESCache.Instance.AccelerationGates.FirstOrDefault().Orbit(500);
                                Time.Instance.NextOrbit = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(10, 15));
                                return;
                            }

                            return;
                        }

                        if (!StopMyShip("Using Optimal Range: Stop ship, target at [" + Math.Round(target.Distance / 1000, 0) +
                                        "k away] Approaching [" + ESCache.Instance.FollowingEntity.Name + "]@[" + Math.Round(ESCache.Instance.FollowingEntity.Distance / 1000, 0) + "k][" + ESCache.Instance.FollowingEntity.Velocity + "m/s] and is inside optimal")) return;
                    }
                }
                else if (ESCache.Instance.FollowingEntity == null && ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity != 0)
                {
                    //
                    // moving but not approaching anything, where are we going?!
                    //
                    if ((Drones.DronesKillHighValueTargets && Salvage.TractorBeams.Count > 0 && Combat.Combat.PotentialCombatTargets.All(i => i.Distance < Drones.DroneControlRange)) || (!Drones.DronesKillHighValueTargets && Salvage.TractorBeams.Count > 0 && Combat.Combat.PotentialCombatTargets.All(i => i.Distance < Combat.Combat.MaxRange) && ESCache.Instance.AccelerationGates.Count > 0 && ESCache.Instance.AccelerationGates.FirstOrDefault()._directEntity.DirectAbsolutePosition.GetDistance(target._directEntity.DirectAbsolutePosition) + 1000 < OptimalRange))
                    {
                        if (DateTime.UtcNow > Time.Instance.NextOrbit)
                        {
                            ESCache.Instance.AccelerationGates.FirstOrDefault().Orbit(500);
                            Time.Instance.NextOrbit = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(10, 15));
                            return;
                        }

                        return;
                    }

                    if (Combat.Combat.KillTarget != null)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log.WriteLine("NavigateIntoRange: if (Combat.Combat.KillTarget != null) orbit");

                        Combat.Combat.KillTarget.Orbit(OrbitDistanceToUse);
                        return;
                    }

                    if (Combat.Combat.PotentialCombatTargets.Any())
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log.WriteLine("NavigateIntoRange: if (Combat.Combat.PotentialCombatTargets.Any()) orbit");
                        Combat.Combat.PotentialCombatTargets.OrderBy(i => i._directEntity.DirectAbsolutePosition.GetDistance(ESCache.Instance.AccelerationGates.FirstOrDefault()._directEntity.DirectAbsolutePosition)).FirstOrDefault().Orbit(OrbitDistanceToUse);
                        return;
                    }

                    EntityCache AbyssalGate = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.TypeId == (int)TypeID.AbyssEncounterGate || (int)i.TypeId == (int)TypeID.AbyssExitGate);

                    if (!Combat.Combat.PotentialCombatTargets.Any())
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log.WriteLine("NavigateIntoRange: if (!Combat.Combat.PotentialCombatTargets.Any()) approach gate");
                        AbyssalGate._directEntity.MoveToViaAStar();
                        return;
                    }

                    if (!StopMyShip("Using Optimal Range: Stop ship, target at [" + Math.Round(target.Distance / 1000, 0) +
                                    "k away] is inside optimal and we were motoring off into space")) return;
                }
        }

        public static bool IsSpotWithinAbyssalBounds(DirectWorldPosition p, long offset = 0)
        {
            if (!ESCache.Instance.InAbyssalDeadspace)
                return false;

            if (offset == 0)
                return ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <= DirectEntity.AbyssBoundarySizeSquared;

            return ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <= (DirectEntity.AbyssBoundarySize + offset) * (DirectEntity.AbyssBoundarySize + offset);
        }

        public static bool IsOurShipWithintheAbyssBounds(int offset = 0) => IsSpotWithinAbyssalBounds(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition, offset);

        private static void NavigateOnGridUsingSpeedTank(List<long> ListOfNavigateOnGridEntityIds)
        {

            if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.ActiveShip.IsFrigate)
            {
                if (ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache))
                {
                    var bioadaptivecache = ESCache.Instance.Entities.FirstOrDefault(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache);
                    if (ESCache.Instance.ActiveShip.Entity.FollowId != bioadaptivecache.Id)
                    {
                        ESCache.Instance.Entities.FirstOrDefault(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache).Orbit(500);
                        return;
                    }
                }
            }

            EntityCache target = null;
            foreach (long NavigateOnGridEntityId in ListOfNavigateOnGridEntityIds)
            {
                if (target != null)
                    break;

                foreach (EntityCache EntityOnGrid in Combat.Combat.PotentialCombatTargets.Where(i => i.Id == NavigateOnGridEntityId))
                {
                    target = EntityOnGrid;
                    break;
                }

                continue;
            }

            if (target == null)
            {
                if (ESCache.Instance.ActiveShip.IsFrigate && ESCache.Instance.Targets.Any(i => !i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && !i.IsWreck))
                {
                    target = ESCache.Instance.Targets.OrderBy(x => x.HealthPct).FirstOrDefault(i => !i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && !i.IsWreck);
                }

                if (target == null)
                {
                    if (Combat.Combat.PotentialCombatTargets.Any(i => !i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache))
                    {
                        Log.WriteLine("NavigateOnGridUsingSpeedTank: target was null-!-");
                        Log.WriteLine("NavigateOnGridUsingSpeedTank: List of Potential Combat Targets:");
                        foreach (var thisPCT in Combat.Combat.PotentialCombatTargets)
                        {
                            Log.WriteLine("[" + thisPCT.Name + "][" + thisPCT.Nearest1KDistance + "] ID [" + thisPCT.Id + "]");
                        }

                        Log.WriteLine("NavigateOnGridUsingSpeedTank: List of NavigateOnGrid Targets: EntityIDs:");
                        foreach (var ThisEntityID in ListOfNavigateOnGridEntityIds)
                        {
                            Log.WriteLine("[" + ThisEntityID + "]");
                        }
                    }

                    return;
                }
            }

            if (!ESCache.Instance.ActiveShip.IsFrigate && target.IsWreckReadyToBeNavigateOnGridTarget)
            {
                if (target.Orbit(500))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange: SpeedTank: if (target.IsWreck) target.Orbit(500)");
                }

                return;
            }

            if (!ESCache.Instance.ActiveShip.IsFrigate && target.IsAbyssalDeadspaceDeviantAutomataSuppressor && ESCache.Instance.ActiveShip.ShieldPercentage > 75)
            {
                if (target.Orbit(2000))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange: SpeedTank: if (target.IsWreck) target.Orbit(500)");
                }

                return;
            }

            if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.ActiveShip.IsFrigate)
            {
                if (!target.IsTrackable || target.Name.Contains("Tessella") || target.Name.Contains("StrikeNeedle"))
                {
                    Log.WriteLine("NavigateIntoRange: SpeedTank: Target [" + target.Name + "] IsTrackable [" + target.IsTrackable + "] Transversal [" + Math.Round((double)target.TransversalVelocity, 0) + "m]");

                    if (ESCache.Instance.AbyssalGate.Distance > 10000 && ESCache.Instance.ActiveShip.Entity.FollowId != ESCache.Instance.AbyssalGate.Id)
                    {
                        Log.WriteLine("NavigateIntoRange: SpeedTank: Target [" + target.Name + "] IsTrackable [" + target.IsTrackable + "] Change between target.orbit and gate.keepatrange to randomize our transversal - Approach");
                        ESCache.Instance.AbyssalGate._directEntity.Approach();
                        return;
                    }

                    if (5000 > ESCache.Instance.AbyssalGate.Distance && ESCache.Instance.ActiveShip.Entity.FollowId != target.Id)
                    {
                        Log.WriteLine("NavigateIntoRange: SpeedTank: Target [" + target.Name + "] IsTrackable [" + target.IsTrackable + "] Change between target.orbit and gate.keepatrange to randomize our transversal - KeepAtRange");
                        target.KeepAtRange(40000);
                        return;
                    }

                    return;
                }

                if (target.Orbit(OrbitDistanceToUse))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange: SpeedTank: orbit::");
                }

                return;
            }

            if (target.Orbit(OrbitDistanceToUse))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange: SpeedTank: orbit");
            }

            return;
        }

        #endregion Methods
    }
}