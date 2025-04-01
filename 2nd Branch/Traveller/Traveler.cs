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

using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Controllers;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Traveller
{
    public static class Traveler
    {
        #region Constructors

        static Traveler()
        {
            Time.Instance.NextTravelerAction = DateTime.MinValue;
        }

        #endregion Constructors

        #region Properties

        public static TravelerDestination Destination
        {
            get => _destination;
            set
            {
                _destination = value;
                MyTravelToBookmark = null;
                if (value == null)
                {
                    if (DirectEve.Interval(5000)) Log.WriteLine("Destination is now null");
                    ControllerManager.Instance.GetController<ActionQueueController>().RemoveAllActions();
                    State.CurrentTravelerState = TravelerState.AtDestination;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TravelerDestinationSystemId), 0);
                }
                else
                {
                    //if (Destination != null && Destination.SolarSystem != null) Log.WriteLine("Destination is now [" + Destination.SolarSystem.Name + "]");
                    State.CurrentTravelerState = TravelerState.Idle;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TravelerDestinationSystemId), Destination.SolarSystemId);
                }
            }
        }

        internal static void ResetTraveler()
        {
            Destination = null;
            State.CurrentTravelerState = TravelerState.Idle;
        }

        #endregion Properties

        #region Fields

        public static DirectLocation _location;
        public static bool IgnoreSuspectTimer;
        private static TravelerDestination _destination { get; set; }
        //private static long? _destinationId;
        private static List<long> _destinationRoute;
        private static bool _instaBMUsed;
        private static int _locationErrors;
        private static string _locationName;
        private static DateTime _nextGetLocation;
        private static IEnumerable<DirectBookmark> myHomeBookmarks;
        private static bool _startedInStation;
        private static Dictionary<int, DateTime> _dynamicSystemsToAvoid = new Dictionary<int, DateTime>();
        private static TimeSpan _dynamicSystemsToAvoidCacheTime = TimeSpan.FromMinutes(15);
        private static bool _avoidGateCamps = true;
        private static bool _avoidBubbles = true;
        private static bool _avoidSmartbombs = true;
        public static bool IgnoreDestinationChecks { get; set; } = true;

        public static event EventHandler<EventArgs> OnSettingsChanged;

        private static bool _allowLowSec;

        public static bool AllowLowSec
        {
            get => _allowLowSec;
            set
            {
                //var changed = _allowLowSec != value;
                _allowLowSec = value;

                //if (changed)
                //    OnSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public static bool AllowNullSec { get; set; }

        public static bool AvoidGateCamps
        {
            get => _avoidGateCamps;
            set
            {
                _avoidGateCamps = value;
                if (!value)
                {
                    _dynamicSystemsToAvoid.Clear();
                }
            }
        }

        public static bool AvoidBubbles
        {
            get => _avoidBubbles;
            set
            {
                _avoidBubbles = value;
                if (!value)
                {
                    _dynamicSystemsToAvoid.Clear();
                }
            }
        }
        public static bool AvoidSmartbombs
        {
            get => _avoidSmartbombs;
            set
            {
                _avoidSmartbombs = value;
                if (!value)
                {
                    _dynamicSystemsToAvoid.Clear();
                }
            }
        }

        #endregion Fields

        #region Properties

        public static DirectBookmark UndockBookmark => ESCache.Instance.DirectEve.Bookmarks
            .Where(b => !string.IsNullOrEmpty(b.Title) && b.LocationId != null && b.Title.ToLower().StartsWith(Settings.Instance.UndockBookmarkPrefix.ToLower())).ToList()
            .FirstOrDefault(b => b.IsInCurrentSystem && ESCache.Instance.DirectEve.Me.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distances.OnGridWithMe * 3);

        #endregion Properties

        #region Methods

        private static ActionQueueAction _covertCloakActionQueueAction;
        private static int _attemptsToRetrieveInsurgencySystem = 0;

        public static DirectBookmark MyTravelToBookmark { get; set; } = null;

        private static bool PlayNotificationSounds => true;

        public static void ProcessState(double finalWarpDistance = 0, bool randomFinalWarpdDistance = false)
        {
            // Only pulse state changes every 1000ms
            if (DateTime.UtcNow < Time.Instance.NextTravelerAction)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine("TravelerState.Traveling: if (DateTime.UtcNow > Time.Instance.NextTravelerAction)");
                return;
            }

            // we could move to the station, but if we are outside of docking range, we are probably dead anyway if we can't dock up instantly
            if (ESCache.Instance.InSpace && ESCache.Instance.Stations.Any(s => s.Distance <= (int)Distances.DockingRange) && (ESCache.Instance.Entities.Any(e => e.IsPlayer && e.IsAttacking) || ESCache.Instance.Entities.Count(e => e.IsTargetedBy && e.IsPlayer) > 1))
            {
                var station = ESCache.Instance.Stations.FirstOrDefault(s => s.Distance <= (int)Distances.DockingRange);
                if (ESCache.Instance.InWarp)
                {
                    Log.WriteLine($"We are outside of a station and being aggressed by another player or targeted by more than 2. Trying to stop the ship and dock.");
                    if (DirectEve.Interval(500, 1000))
                    {
                        ESCache.Instance.DirectEve.ExecuteCommand(EVESharpCore.Framework.DirectCmd.CmdStopShip);
                    }
                    return;
                }
                else
                {
                    Log.WriteLine("Docking attempt due the fact we are being aggressed or targeted by more than 2 players.");
                    station.Dock();
                    ChangeTravelerState(TravelerState.Error);
                    return;
                }
            }

            if (ESCache.Instance.ActiveShip.GroupId != (int)Group.BlockadeRunner && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Sunesis) // Blockade runner or sunesis
            {
                Traveler.AllowLowSec = false;
            }

            if (ESCache.Instance.InSpace)
            {
                var scrambled = ESCache.Instance.ActiveShip.IsWarpScrambled;
                if (scrambled)
                {
                    Log.WriteLine($"We are warp scrambled. Processing combat/drones.");
                    DefendOnTravel();
                    return;
                }

                var bastion = ESCache.Instance.DirectEve.Modules.FirstOrDefault(m => m.TypeId == 33400);
                if (bastion != null)
                {
                    if (bastion.IsActive)
                    {
                        // deactivate
                        if (!bastion.IsInLimboState && DirectEve.Interval(900, 1400))
                        {
                            Logging.Log.WriteLine($"Deactivating bastion module (travel).");
                            bastion.Click();
                        }
                    }
                }
            }
            if (DebugConfig.DebugTraveler) Log.WriteLine("TravelerState.Traveling: CurrentTravelerState [" + State.CurrentTravelerState + "]");
            switch (State.CurrentTravelerState)
            {
                case TravelerState.Idle:
                    _startedInStation = false;
                    _instaBMUsed = false;
                    if (ESCache.Instance.InStation)
                        _startedInStation = true;
                    _attemptsToRetrieveInsurgencySystem = 0;
                    ChangeTravelerState(TravelerState.CalculatePath);
                    break;

                case TravelerState.CalculatePath:

                    Log.WriteLine($"Traveler calculating path. AllowLowSec [{AllowLowSec}] AllowNullSec [{AllowNullSec}]");
                    if (Destination == null)
                    {
                        ChangeTravelerState(TravelerState.Error);
                        break;
                    }

                    var systemsToAvoid = new HashSet<DirectSolarSystem>();
                    _destinationRoute = ESCache.Instance.DirectEve.Me.CurrentSolarSystem.CalculatePathTo(ESCache.Instance.DirectEve.SolarSystems[(int)Destination.SolarSystemId], systemsToAvoid, AllowLowSec, AllowNullSec).Item1.Select(s => (long)s.Id).ToList();

                    if (_destinationRoute.Count == 0)
                    {
                        // TODO: handle me
                        Log.WriteLine("Error: _destinationRoute.Count == 0");
                        ChangeTravelerState(TravelerState.Error);
                        return;
                    }

                    if (TravelerUsePathAvoidance)
                    {
                        if (TravelerSystemNamesToAvoid.Any())
                        {
                            foreach (var systemName in TravelerSystemNamesToAvoid)
                            {
                                if (String.IsNullOrEmpty(systemName))
                                    continue;

                                //remove any spaces from systemName
                                var systemId = ESCache.Instance.DirectEve.GetSolarSystemIdByName(systemName.Trim());
                                if (systemId > 0)
                                {

                                    if ((int)Destination.SolarSystemId == systemId && IgnoreDestinationChecks)
                                    {
                                        Log.WriteLine($"Destination is in system to avoid [{systemName}]. Skipping.");
                                        continue;
                                    }

                                    if (ESCache.Instance.DirectEve.SolarSystems.TryGetValue(systemId, out var ss))
                                    {
                                        Log.WriteLine($"Avoiding solarsystem [{ss.Name}]");
                                        systemsToAvoid.Add(ss);
                                    }
                                }
                            }
                        }

                        if (TravelerAvoidInsurgencySystems)
                        {
                            if (_attemptsToRetrieveInsurgencySystem <= 3)
                            {
                                // For now avoid all insurgency systems
                                List<DirectSolarSystem> insurgencySystemList = new List<DirectSolarSystem>();
                                insurgencySystemList = ESCache.Instance.DirectEve.GetInsurgencyInfestedSystems();
                                if (insurgencySystemList.Count <= 0)
                                {
                                    _attemptsToRetrieveInsurgencySystem++;
                                    return;
                                }

                                foreach (var sys in insurgencySystemList)
                                {

                                    if ((int)Destination.SolarSystemId == sys.Id && IgnoreDestinationChecks)
                                    {
                                        Log.WriteLine($"Destination is in system to avoid [{sys.Name}]. Skipping.");
                                        continue;
                                    }

                                    Log.WriteLine($"Avoiding solarsystem [{sys.Name}]");
                                    systemsToAvoid.Add(sys);
                                }
                            }
                            else
                            {
                                Log.WriteLine($"Failed to retrieve insurgency systems. Skipping.");
                            }
                        }

                        foreach (var dynamicSystemsToAvoidEntry in _dynamicSystemsToAvoid.ToList())
                        {
                            // Remove old entries based on _dynamicSystemsToAvoidCacheTime ago
                            if (DateTime.UtcNow.Subtract(dynamicSystemsToAvoidEntry.Value) > _dynamicSystemsToAvoidCacheTime)
                            {
                                _dynamicSystemsToAvoid.Remove(dynamicSystemsToAvoidEntry.Key);
                                continue;
                            }

                            if (ESCache.Instance.DirectEve.SolarSystems.TryGetValue(dynamicSystemsToAvoidEntry.Key, out var ss))
                            {

                                if ((int)Destination.SolarSystemId == ss.Id && IgnoreDestinationChecks)
                                {
                                    Log.WriteLine($"Destination is in system to avoid [{ss.Name}]. Skipping.");
                                    continue;
                                }

                                Log.WriteLine($"Avoiding solarsystem [{ss.Name}] due to dynamic avoidance settings cache.");
                                systemsToAvoid.Add(ss);
                            }
                        }

                        // Log all solar system names in systemsToAvoid
                        Log.WriteLine("-------------------");
                        int k = 0;
                        foreach (var ss in systemsToAvoid)
                        {
                            Log.WriteLine($"[{k}] Avoiding solarsystem [{ss.Name}]");
                            k++;
                        }
                        Log.WriteLine("-------------------");
                        // print all the flags
                        Log.WriteLine($"AllowLow [{AllowLowSec}] AllowNull [{AllowNullSec}] IGNORE [{IgnoreDestinationChecks}] GC [{AvoidGateCamps}] BUBB [{AvoidBubbles}] SMART [{AvoidSmartbombs}]");
                        Log.WriteLine("-------------------");

                        if (AvoidBubbles || AvoidSmartbombs || AvoidGateCamps)
                        {
                            try
                            {
                                var pipeProxy = WCFClient.Instance.GetPipeProxy;
                                var gateCampInfo =
                                    pipeProxy.GetGateCampInfo(_destinationRoute.Select(x => (int)x).ToArray(), _dynamicSystemsToAvoidCacheTime);

                                Log.WriteLine($"Got gatecamp info for [{gateCampInfo.Count}] systems.");

                                var reroute = false;
                                foreach (var destination in _destinationRoute)
                                {
                                    if (!gateCampInfo.TryGetValue((int)destination, out var solarSystemEntry))
                                    {
                                        // System has no information associated with it
                                        continue;
                                    }

                                    if (!ESCache.Instance.DirectEve.SolarSystems.TryGetValue((int)destination, out var _gateCampedSolarSystem))
                                    {
                                        Log.WriteLine($"Failed to get solarsystem [{destination}]");
                                        ChangeTravelerState(TravelerState.Error);
                                        return;
                                    }

                                    Log.WriteLine("gateCampedSolarSystem [" + _gateCampedSolarSystem.Name + "][" + _gateCampedSolarSystem.GetSecurity() + "] JumpsAway [" + _gateCampedSolarSystem.Jumps + "]");

                                    if (_gateCampedSolarSystem.IsHighSecuritySpace)
                                        continue;


                                    if ((int)Destination.SolarSystemId == _gateCampedSolarSystem.Id && IgnoreDestinationChecks)
                                    {
                                        Log.WriteLine($"IgnoreDestinationChecks was set to true. Ignoring destinations checks for [{_gateCampedSolarSystem.Name}].");
                                        continue;
                                    }

                                    var hasBubbleShips = solarSystemEntry
                                        ?.Kills
                                        ?.GateKills
                                        ?.Values
                                        ?.Any(v => v.Checks.Dictors || v.Checks.Hictors) ?? false;
                                    if (hasBubbleShips && AvoidBubbles)
                                    {
                                        Log.WriteLine($"Avoiding solarsystem [{_gateCampedSolarSystem.Name}] due to bubbles.");
                                        _dynamicSystemsToAvoid[(int)destination] = DateTime.UtcNow;
                                        reroute = true;
                                        continue;
                                    }

                                    var hasSmartbombShips = solarSystemEntry
                                        ?.Kills
                                        ?.GateKills
                                        ?.Values
                                        ?.Any(v => v.Checks.Smartbombs) ?? false;
                                    if (hasSmartbombShips && AvoidSmartbombs)
                                    {
                                        Log.WriteLine($"Avoiding solarsystem [{_gateCampedSolarSystem.Name}] due to smartbombs.");
                                        _dynamicSystemsToAvoid[(int)destination] = DateTime.UtcNow;
                                        reroute = true;
                                        continue;
                                    }

                                    var hasGateCamp = solarSystemEntry
                                        ?.Kills
                                        ?.GateKillCountLastHour > 0;
                                    if (hasGateCamp && AvoidGateCamps)
                                    {
                                        Log.WriteLine($"Avoiding solarsystem [{_gateCampedSolarSystem.Name}] due to gatecamp.");
                                        _dynamicSystemsToAvoid[(int)destination] = DateTime.UtcNow;
                                        reroute = true;
                                        continue;
                                    }

                                    Log.WriteLine($"Solarsystem [{destination}] seems to be safe to travel through.");
                                }

                                if (reroute)
                                {
                                    Log.WriteLine("Rerouting on next tick due to dynamic avoidance settings.");
                                    ChangeTravelerState(TravelerState.CalculatePath);
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                Log.WriteLine("If you need to you can set TravelerUsePathAvoidance to false to avoid trying to get gatecamp info");
                                Log.WriteLine("Error getting gatecamp info: " + e);
                                //TravelerUsePathAvoidance = false;
                                //ChangeTravelerState(TravelerState.CalculatePath);
                                ChangeTravelerState(TravelerState.Error);
                                return;
                            }
                        }
                    }

                    if (_destinationRoute.Any())
                    {
                        Log.WriteLine("Calculated path: [" + _destinationRoute.Count() + "]");
                        int i = 0;
                        foreach (var waypoint in _destinationRoute)
                        {
                            var wp = ESCache.Instance.DirectEve.SolarSystems[(int)waypoint];
                            Log.WriteLine($"[{i}] Name [{wp.Name}] Security [{Math.Round(wp.GetSecurity(), 2)}]");
                            i++;
                        }
                    }
                    else Log.WriteLine("!_destinationRoute.Any()");

                    //if (Logging.DebugTraveler) Logging.Log("Traveler", "Destination is set: processing...", Logging.Teal);

                    ChangeTravelerState(TravelerState.Traveling);
                    break;


                case TravelerState.Traveling:
                    _attemptsToRetrieveInsurgencySystem = 0;
                    if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    {
                        if (DebugConfig.DebugTraveler) Log.WriteLine("TravelerState.Traveling: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation) return");
                        Time.Instance.NextTravelerAction = DateTime.UtcNow;
                        return;
                    }

                    if (ESCache.Instance.ActiveShip != null)
                    {
                        ActivateCovertOpsCloak();
                    }

                    //if we are in warp, do nothing, as nothing can actually be done until we are out of warp anyway.
                    if (ESCache.Instance.InWarp)
                    {
                        if (DebugConfig.DebugTraveler) Log.WriteLine("TravelerState.Traveling: if (ESCache.Instance.InWarp) return");
                        Time.Instance.NextTravelerAction = DateTime.UtcNow;
                        return;
                    }

                    if (_startedInStation && ESCache.Instance.InSpace && !_instaBMUsed)
                    {
                        UseInstaBookmark();
                        _instaBMUsed = true;
                        return;
                    }

                    if (Destination == null)
                    {
                        Log.WriteLine("TravelerState.Traveling: if (Destination == null) State.CurrentTravelerState = TravelerState.Error;");
                        ChangeTravelerState(TravelerState.Error);
                        break;
                    }

                    if (Destination.SolarSystemId != ESCache.Instance.DirectEve.Session.SolarSystemId)
                    {
                        if (DebugConfig.DebugTraveler) Log.WriteLine("TravelerState.Traveling: NavigateToBookmarkSystem(Destination.SolarSystemId);");
                        NavigateToBookmarkSystem(Destination.SolarSystemId);
                    }
                    else if (Destination.PerformFinalDestinationTask())
                    {
                        _destinationRoute = null;
                        _location = null;
                        _locationName = string.Empty;
                        _locationErrors = 0;
                        ChangeTravelerState(TravelerState.AtDestination);
                    }

                    //Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(4);
                    break;

                case TravelerState.AtDestination:
                    AllowLowSec = false;
                    //do nothing when at destination
                    //Traveler sits in AtDestination when it has nothing to do, NOT in idle.
                    break;

                default:
                    break;
            }
        }

        internal static void HandleNotifications()
        {
            try
            {
                if (!DirectEve.HasFrameChanged())
                    return;

                //
                //if (IsTravelerAtDestination)
                {
                    if (DirectEve.Interval(10000))
                    {
                        Log.WriteLine("Notification!: At Destination");
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool ChangeTravelerState(TravelerState state)
        {
            try
            {
                if (State.CurrentTravelerState != state)
                {
                    Log.WriteLine("New TravelerState [" + state + "]");
                    State.CurrentTravelerState = state;
                    if (State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        ResetStatesToDefaults(null);
                        return true;
                    }

                    if (Time.Instance.NextTravelerAction > DateTime.UtcNow)
                    {
                        if (State.CurrentTravelerState != TravelerState.Traveling)
                            return true;

                        ProcessState();
                        return true;
                    }

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

        public static bool DefendOnTravel()
        {
            try
            {
                bool canWarp = true;
                if (ESCache.Instance.InWarp) return false;
                //
                // defending yourself is more important that the traveling part... so it comes first.
                //
                if (ESCache.Instance.InSpace)
                    if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsCloak && i.IsActive))
                    {
                        if (DebugConfig.DebugGotobase) Log.WriteLine("Travel: _combat.ProcessState()");

                        if (Combat.TargetedBy.Any(t => t.IsWarpScramblingMe))
                        {
                            Drones.DronesShouldBePulled = false;
                            if (DebugConfig.DebugGotobase) Log.WriteLine("Travel: we are scrambled");
                            canWarp = false;
                        }
                    }

                if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                {
                    if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                return canWarp;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        private static List<string> TravelerSystemNamesToAvoid = new List<string>();
        private static bool TravelerUsePathAvoidance = false;
        private static bool TravelerAvoidInsurgencySystems = false;

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: Traveler");
                IgnoreSuspectTimer =
                    (bool?)CharacterSettingsXml.Element("ignoreSuspectTimer") ??
                    (bool?)CommonSettingsXml.Element("ignoreSuspectTimer") ?? false;
                Log.WriteLine("Traveler: ignoreSuspectTimer [" + IgnoreSuspectTimer + "]");
                TravelerSystemNamesToAvoid = CharacterSettingsXml.Element("TravelerSystemsToAvoid")?
                    .Elements("TravelerSystemToAvoid")
                    .Where(x => !x.IsEmpty)
                    .Select(x => (string)x)
                    .ToList() ?? new List<string>();
                if (TravelerSystemNamesToAvoid.Any())
                {
                    Log.WriteLine("Traveler: TravelerSystemNamesToAvoid [" + TravelerSystemNamesToAvoid.Count() + "]");
                    int intCount = 0;
                    foreach (var TravelerSystemNameToAvoid in TravelerSystemNamesToAvoid)
                    {
                        intCount++;
                        Log.WriteLine("Traveler: TravelerSystemNameToAvoid [" + intCount + "][" + TravelerSystemNameToAvoid + "]");
                    }
                }

                TravelerUsePathAvoidance =
                    (bool?)CharacterSettingsXml.Element("TravelerUsePathAvoidance") ??
                    (bool?)CommonSettingsXml.Element("TravelerUsePathAvoidance") ?? false;
                Log.WriteLine("Traveler: TravelerUsePathAvoidance [" + TravelerUsePathAvoidance + "]");
                TravelerAvoidInsurgencySystems =
                    (bool?)CharacterSettingsXml.Element("TravelerAvoidInsurgencySystems") ??
                    (bool?)CommonSettingsXml.Element("TravelerAvoidInsurgencySystems") ?? false;
                Log.WriteLine("Traveler: TravelerAvoidInsurgencySystems [" + TravelerAvoidInsurgencySystems + "]");
                IgnoreDestinationChecks =
                    (bool?)CharacterSettingsXml.Element("TravelerIgnoreDestinationChecks") ??
                    (bool?)CommonSettingsXml.Element("TravelerIgnoreDestinationChecks") ?? true;
                Log.WriteLine("Traveler: IgnoreDestinationChecks [" + IgnoreDestinationChecks + "]");
                AvoidBubbles =
                    (bool?)CharacterSettingsXml.Element("TravelerAvoidBubbles") ??
                    (bool?)CommonSettingsXml.Element("TravelerAvoidBubbles") ?? true;
                AvoidGateCamps =
                    (bool?)CharacterSettingsXml.Element("TravelerAvoidGateCamps") ??
                    (bool?)CommonSettingsXml.Element("TravelerAvoidGateCamps") ?? true;
                AvoidSmartbombs =
                    (bool?)CharacterSettingsXml.Element("TravelerAvoidSmartbombs") ??
                    (bool?)CommonSettingsXml.Element("TravelerAvoidSmartbombs") ?? true;
                AllowNullSec =
                    (bool?)CharacterSettingsXml.Element("TravelerAllowNullSec") ??
                    (bool?)CommonSettingsXml.Element("TravelerAllowNullSec") ?? false;
                AllowLowSec =
                    (bool?)CharacterSettingsXml.Element("TravelerAllowLowSec") ??
                    (bool?)CommonSettingsXml.Element("TravelerAllowLowSec") ?? false;

            }
            catch (Exception exception)
            {
                Log.WriteLine("Error Loading Weapon and targeting Settings [" + exception + "]");
            }
        }

        private static void ActivateCovertOpsCloak()
        {
            if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsCovertOpsCloak))
            {
                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsCovertOpsCloak && !i.IsActive))
                {
                    if (ESCache.Instance.DirectEve.Modules.FirstOrDefault(i => i.IsCovertOpsCloak).ActivateCovertOpsCloak)
                        Log.WriteLine("Cloak Activated");

                    return;
                }

                Traveler.BoolRunEveryFrame = false;
                return;

            }
        }



        /// <summary>
        ///     Set destination to a solar system
        /// </summary>
        public static bool SetStationDestination(long stationId)
        {
            NavigateOnGrid.StationIdToGoto = stationId;
            _location = ESCache.Instance.DirectEve.Navigation.GetLocation(stationId);
            if (DebugConfig.DebugTraveler)
                Log.WriteLine("Location = [" + _location.Name + "]");
            if (_location != null && _location.IsValid)
            {
                _locationErrors = 0;
                if (DebugConfig.DebugTraveler)
                    Log.WriteLine("Setting destination to [" + _location.Name + "]");
                try
                {
                    _location.SetDestination();
                }
                catch (Exception)
                {
                    Log.WriteLine("Set destination to [" + _location + "] failed ");
                }

                return true;
            }

            Log.WriteLine("Error setting station destination [" + stationId + "]");
            _locationErrors++;
            if (_locationErrors > 20)
                return false;
            return false;
        }

        public static void TravelHome(DirectAgent myAgent)
        {
            TravelToAgentsStation(myAgent);
        }

        public static void TravelToAgentsStation(DirectAgent myAgent)
        {
            try
            {
                if (DebugConfig.DebugGotobase)
                {
                    Log.WriteLine("TravelToAgentsStation: myAgent.StationId [" + myAgent.StationId + "]");
                    Log.WriteLine("TravelToAgentsStation: myAgent.SolarSystemId [" + myAgent.SolarSystemId + "]");
                }

                if (myAgent.StationId > 0)
                {
                    if (DebugConfig.DebugGotobase) Log.WriteLine("TravelToAgentsStation: if (DestinationId > 0)");

                    TravelToStationId(myAgent.StationId);
                    return;
                }

                Log.WriteLine("DestinationId [" + myAgent.StationId + "]");
            }
            catch (Exception)
            {
            }
        }

        public static void TravelToBookmark(DirectBookmark bookmark, double finalWarpDistance = 0, bool randomFinalWarpdDistance = false)
        {
            try
            {
                if (DebugConfig.DebugGotobase || DebugConfig.DebugTraveler) Log.WriteLine("bookmark [" + bookmark.Title + "]");
                var bm = ESCache.Instance.DirectEve.Bookmarks.FirstOrDefault(b => b.BookmarkId == bookmark.BookmarkId.Value);

                if (bookmark == null) Log.WriteLine("if (bookmark == null)");

                if (Destination == null)
                {
                    Log.WriteLine("Bookmark title [" + bm.Title + "] Setting Destination");
                    Destination = new BookmarkDestination(bookmark);
                    ChangeTravelerState(TravelerState.Idle);
                    return;
                }

                if (DebugConfig.DebugGotobase || DebugConfig.DebugTraveler)
                    if (Destination != null)
                        Log.WriteLine("TravelToBookmark: if (Destination != null)");

                _processAtDestinationActions();
                ProcessState(finalWarpDistance, randomFinalWarpdDistance);
                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void TravelToSetWaypoint()
        {
            try
            {
                var path = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();
                path.RemoveAll(i => i == 0);
                if (path.Count == 0)
                {
                    if (DirectEve.Interval(1000)) Log.WriteLine("No path set.");
                    ControllerManager.Instance.GetController<ActionQueueController>().RemoveAllActions();
                    Traveler.Destination = null;
                    State.CurrentTravelerState = TravelerState.AtDestination;
                    return;
                }

                if (path.Any())
                {

                    var dest = path.Last();
                    var location = ESCache.Instance.DirectEve.Navigation.GetLocation(dest);
                    var isStationLocation = location.ItemId.HasValue && ESCache.Instance.DirectEve.Stations.TryGetValue((int)location.ItemId.Value, out var _);

                    if (!location.SolarSystemId.HasValue)
                    {
                        Log.WriteLine("Location has no solarsystem id.");
                        return;
                    }

                    if (DebugConfig.DebugTraveler) Log.WriteLine($"Location SolarSystemId {location.SolarSystemId} isStationLocation {isStationLocation}");

                    // if we can't warp because we are scrambled, prevent next actions
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsWarpScramblingMe))
                        return;

                    if (_destination == null)
                    {
                        if (isStationLocation)
                        {
                            _destination = new StationDestination(location.ItemId.Value);
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TravelerDestinationSystemId), Destination.SolarSystemId);
                        }
                        else if (location.IsStructureLocation)
                        {
                            _destination = new DockableLocationDestination(location.LocationId);
                        }
                        else
                        {
                            _destination = new SolarSystemDestination(location.SolarSystemId.Value);
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TravelerDestinationSystemId), Destination.SolarSystemId);
                        }
                        ChangeTravelerState(TravelerState.Idle);
                        return;
                    }
                }

				if (_destination != null)
				{
				    _processAtDestinationActions();
                    ProcessState();
				}
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public static bool UseInstaBookmark()
        {
            try
            {
                if (ESCache.Instance.InWarp) return false;

                if (ESCache.Instance.InStation)
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                    return false;
                }

                if (ESCache.Instance.InSpace)
                {
                    if (UndockBookmark != null)
                    {
                        if (UndockBookmark.LocationId == ESCache.Instance.DirectEve.Session.LocationId)
                        {
                            var distance = ESCache.Instance.DirectEve.Me.DistanceFromMe(UndockBookmark.X ?? 0, UndockBookmark.Y ?? 0,
                                UndockBookmark.Z ?? 0);
                            if (distance < (int)Distances.WarptoDistance)
                            {
                                Log.WriteLine("Arrived at undock bookmark [" + UndockBookmark.Title +
                                              "]");
                                return true;
                            }

                            if (distance >= (int)Distances.WarptoDistance)
                            {
                                if (UndockBookmark.WarpTo())
                                {
                                    Log.WriteLine("Warping to undock bookmark [" + UndockBookmark.Title +
                                                  "][" + Math.Round(distance / 1000 / 149598000, 2) + " AU away]");
                                    //if (!Combat.ReloadAll(Cache.Instance.EntitiesNotSelf.OrderBy(t => t.Distance).FirstOrDefault(t => t.Distance < (double)Distance.OnGridWithMe))) return false;
                                    Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(10);
                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }

                        if (DebugConfig.DebugUndockBookmarks)
                            Log.WriteLine("Bookmark Named [" + UndockBookmark.Title +
                                          "] was somehow picked as an UndockBookmark but it is not in local with us! continuing without it.");
                        return true;
                    }

                    // if we just undocked from jita 4/4 warp to planet 5 to avoid bumping

                    if (ESCache.Instance.Stations.Any(s => s.Id == 60003760 && s.Distance < 1000))
                    {


                        var jita5Plnaet = ESCache.Instance.Entities.FirstOrDefault(e =>
                            e.GroupId == (int)Group.Planet && e.Name.Equals("Jita V"));

                        List<float> warpRanges = new List<float>() {
                            //10_000,
                            20_000,
                            30_000,
                            50_000,
                            70_000,
                            100_000,
                        };

                        if (jita5Plnaet != null)
                        {
                            var randomRange = ListExtensions.Random(warpRanges);
                            if (randomRange < 0 || randomRange > 100_000)
                                randomRange = 0;

                            Log.WriteLine($"We just undocked from Jita 4/4, warping to planet V at range [{randomRange}] to prevent bumps/ganks. (Instant warp)");
                            jita5Plnaet.WarpTo(ListExtensions.Random(warpRanges));

                        }
                        return true;
                    }

                    if (DebugConfig.DebugUndockBookmarks)
                        Log.WriteLine("No undock bookmarks in local matching our undockPrefix [" +
                                      Settings.Instance.UndockBookmarkPrefix +
                                      "] continuing without it.");
                    return true;
                }

                if (DebugConfig.DebugUndockBookmarks)
                    Log.WriteLine("InSpace [" + ESCache.Instance.InSpace + "]: waiting until we have been undocked or in system a few seconds");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public static bool TravelToBookmarkName(string bookmarkName)
        {
            bool travel = false;

            if (ESCache.Instance.BookmarksByLabel(bookmarkName).Count > 0)
            {
                myHomeBookmarks = ESCache.Instance.BookmarksByLabel(bookmarkName).OrderBy(i => Guid.NewGuid()).ToList();

                if (myHomeBookmarks != null && myHomeBookmarks.Any())
                {
                    if (MyTravelToBookmark == null) MyTravelToBookmark = myHomeBookmarks.Where(b =>  b.IsInCurrentSystem).OrderBy(i => Guid.NewGuid()).FirstOrDefault();
                    if (MyTravelToBookmark == null && !ESCache.Instance.DirectEve.Session.IsWspace) MyTravelToBookmark = myHomeBookmarks.OrderBy(i => i.SolarSystem.JumpsHighSecOnly).FirstOrDefault();
                    if (MyTravelToBookmark != null && MyTravelToBookmark.LocationId != null)
                    {
                        TravelToBookmark(MyTravelToBookmark);
                        travel = true;
                        return travel;
                    }
                }

                Log.WriteLine("bookmark not found! We were Looking for bookmark starting with [" + bookmarkName + "] found none.");
            }

            Log.WriteLine("bookmark not found! We were Looking for bookmark starting with [" + bookmarkName + "] found none");
            return travel;
        }

        public static bool TravelToMissionBookmark(DirectAgentMission myMission, string title)
        {
            try
            {
                if (myMission == null)
                {
                    Log.WriteLine("TravelToMissionBookmark: myMission is null");
                    return false;
                }

                if (myMission.State == MissionState.Offered && (myMission.Bookmarks == null || myMission.Bookmarks.Count == 0))
                {
                    ResetStatesToDefaults(myMission);
                    Log.WriteLine("TravelToMissionBookmark [" + myMission.Name + "] is int State [" + myMission.State + "] and should not yet have any bookmarks: logic error?");
                    //ESCache.Instance.CloseQuestor("No Bookmarks found for mission [" + myMission.Name + "] recycling the eve client");
                    CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                    return false;
                }

                if (myMission.Bookmarks == null || myMission.Bookmarks.Count == 0)
                {
                    ResetStatesToDefaults(myMission);
                    Log.WriteLine("TravelToMissionBookmark [" + myMission.Name + "] is int State [" + myMission.State + "] does not yet have any bookmarks and probably should");
                    //ESCache.Instance.CloseQuestor("No Bookmarks found for mission [" + myMission.Name + "] recycling the eve client");
                    return false;
                }

                if (!myMission.Bookmarks.Any(i => i.Title.Contains(title)))
                {
                    ResetStatesToDefaults(myMission);
                    Log.WriteLine("The mission [" + myMission.Name + "] does not yet have any bookmarks containing [" + title + "]: waiting");
                    return false;
                }

                MissionBookmarkDestination missionDestination = Destination as MissionBookmarkDestination;
                if (Destination == null || missionDestination == null || missionDestination.AgentId != myMission.AgentId || !missionDestination.Title.ToLower().StartsWith(title.ToLower()))
                {
                    DirectAgentMissionBookmark tempAgentMissionBookmark = myMission.Bookmarks.Find(i => i.Title.Contains(title));
                    if (tempAgentMissionBookmark != null)
                    {
                        Log.WriteLine("TravelToMissionBookmark: Setting Destination to [" + tempAgentMissionBookmark.Title + "]");
                        Destination = new MissionBookmarkDestination(tempAgentMissionBookmark);
                    }
                }

                ProcessState();

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (missionDestination != null)
                    {
                        Log.WriteLine("Arrived at RegularMission Bookmark Destination [ " + missionDestination.Title + " ]");
                        Destination = null;
                        missionDestination = null;
                        return true;
                    }

                    Log.WriteLine("destination is null"); //how would this occur exactly?
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



        public static void TravelToStationId(long destinationId)
        {
            try
            {
                try
                {
                    if (Destination == null)
                    {
                        Log.WriteLine("StationDestination: [" + destinationId + "]");
                        if (destinationId > 0)
                        {
                            Destination = new StationDestination(destinationId);
                            ChangeTravelerState(TravelerState.Idle);
                            return;
                        }

                        return;
                    }

                    try
                    {
                        if (((StationDestination)Destination).StationId != destinationId)
                        {
                            Log.WriteLine("StationDestination: [" + destinationId + "]");
                            if (destinationId > 0)
                            {
                                Destination = new StationDestination(destinationId);
                                ChangeTravelerState(TravelerState.Idle);
                                return;
                            }

                            return;
                        }
                    }
                    catch (Exception)
                    {
                        _destination = null;
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TravelerDestinationSystemId), 0);
                    }
                }
                catch (Exception ex)
                {
                    _destination = null;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TravelerDestinationSystemId), 0);
                    Log.WriteLine("Exception [" + ex + "]");
                }

                if (DebugConfig.DebugGotobase)
                    if (Destination != null)
                        Log.WriteLine("Traveler.Destination.SolarSystemId [" + Destination.SolarSystemId + "]");
                if (DebugConfig.DebugGotobase) Log.WriteLine("_processAtDestinationActions();");
                _processAtDestinationActions();
                if (DebugConfig.DebugGotobase) Log.WriteLine("Entering Traveler.ProcessState();");
                ProcessState();
                if (DebugConfig.DebugGotobase) Log.WriteLine("Exiting Traveler.ProcessState();");
            }
            catch (Exception ex)
            {
                _destination = null;
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TravelerDestinationSystemId), 0);
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void TravelToSystemId(long destinationId)
        {
            try
            {
                using (ProcessLock pLock = (ProcessLock)CrossProcessLockFactory.CreateCrossProcessLock(1, "TravelToSystemId"))
                {
                    if (_destination == null || ((SolarSystemDestination)_destination != null && ((SolarSystemDestination)_destination).SolarSystemId != destinationId))
                    {
                        Log.WriteLine("SolarSystemDestination: [" + destinationId + "]");
                        _destination = new SolarSystemDestination(destinationId);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TravelerDestinationSystemId), Destination.SolarSystemId);
                        ChangeTravelerState(TravelerState.Idle);
                        return;
                    }

                    if (DebugConfig.DebugGotobase)
                        if (Destination != null)
                            Log.WriteLine("Traveler.Destination.SolarSystemDestination [" + Destination.SolarSystemId + "]");
                    if (DebugConfig.DebugGotobase) Log.WriteLine("_processAtDestinationActions();");
                    _processAtDestinationActions();
                    if (DebugConfig.DebugGotobase) Log.WriteLine("Entering Traveler.ProcessState();");
                    ProcessState();
                    if (DebugConfig.DebugGotobase) Log.WriteLine("Exiting Traveler.ProcessState();");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void _processAtDestinationActions()
        {
            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
                {
                    Log.WriteLine("an error has occurred");
                    if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Traveler)
                        State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;

                    return;
                }

                if (ESCache.Instance.InSpace)
                {
                    if (State.CurrentHydraState == HydraState.Combat)
                    {
                        Log.WriteLine("Arrived at destination (in space)");
                        return;
                    }

                    Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                    ControllerManager.Instance.SetPause(true);
                    return;
                }

                if (DebugConfig.DebugTraveler) Log.WriteLine("Arrived at destination...");
                if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Traveler)
                {
                    if (State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.Storyline && State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.StorylineReturnToBase)
                        State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                }
            }
        }

        public static ModuleCache Cloak
        {
            get
            {
                if (ESCache.Instance.Modules.Count > 0 && ESCache.Instance.Modules.Any(i => i.TypeId == (int)TypeID.CovertOpsCloakingDevice))
                {
                    return ESCache.Instance.Modules.Find(i => i.TypeId == (int)TypeID.CovertOpsCloakingDevice);
                }

                return null;
            }
        }

        public static bool BoolRunEveryFrame = false;

        private static void ActivateCovertOpsCloak([CallerMemberName] string caller = "")
        {
            if (ESCache.Instance.InStation || !ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine($"ESCache.Instance.InDockableLocation || !ESCache.Instance.InSpace)");
                return;
            }

            var cloak = ESCache.Instance.Modules.FirstOrDefault(i => i.TypeId == 11578);
            if (cloak == null)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine($"Cloak == null");
                return;
            }

            if (!caller.Equals(nameof(AddActivateCovertOpsCloakAfterJumpAction)) && _covertCloakActionQueueAction != null)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine($"Blocked. Callermember: {caller}");
                return;
            }

            if (cloak._module.IsLimboStateWithoutEffectActivating || cloak.IsActive)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine("(cloak.IsInLimboState || cloak.IsActive)");
                return;
            }

            if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine("QCache.Instance.DirectEve.Me.IsJumpCloakActive");
                return;
            }

            if (ESCache.Instance.ActiveShip.Entity == null)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine("QCache.Instance.ActiveShip.Entity == null");
                return;
            }

            if (ESCache.Instance.EntitiesNotSelf.Any(e => e.GroupId != 227 && e.Distance <= (int)Distances.SafeToCloakDistance)) // 227 = Inventory Groups.Celestial.Cloud
            {
                var ent = ESCache.Instance.EntitiesNotSelf.FirstOrDefault(e => e.Distance <= (int)Distances.SafeToCloakDistance);
                if (ent != null && ent.IsValid)
                {
                    Log.WriteLine($"Can't activate cloak because there is another entity within [{(int)Distances.SafeToCloakDistance}]m. Entity {ent.TypeName}");
                    return;
                }
            }

            if (cloak._module.Click(ESCache.Instance.RandomNumber(90, 150), true))
            {
                Log.WriteLine("Activating covert ops cloak.");
            }
        }

        private static void AddActivateCovertOpsCloakAfterJumpAction()
        {
            var cloakActionTimeout = DateTime.UtcNow.AddSeconds(45);
            var _rnd = new Random();
            var nextPulse = DateTime.UtcNow;
            ActionQueueAction covOpsAction = new ActionQueueAction(() => // create new action to delay the click for 40ms
            {
                ActivateCovertOpsCloak(nameof(AddActivateCovertOpsCloakAfterJumpAction));
            }, true).Initialize();
            _covertCloakActionQueueAction = new ActionQueueAction(() =>
            {

                //Log.WriteLine("Pulse");
                if (nextPulse > DateTime.UtcNow)
                {
                    _covertCloakActionQueueAction.QueueAction();
                    return;
                }

                nextPulse = DateTime.UtcNow.AddMilliseconds(_rnd.Next(90, 150));

                if (cloakActionTimeout < DateTime.UtcNow)
                {
                    _covertCloakActionQueueAction = null;
                    if (DebugConfig.DebugTraveler) Log.WriteLine("Cov ops cloak action timed out.");
                    return;
                }

                var covOpsCloak = ESCache.Instance.Modules.FirstOrDefault(i => i.TypeId == 11578);

                if (covOpsCloak.IsActive && ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.Velocity > 80000)
                {
                    if (ESCache.Instance.ActiveShip.TypeId == 34590)
                    {
                        DeactivateCloak();
                    }
                    _covertCloakActionQueueAction = null;
                    Log.WriteLine("Stopping action, cloak is active.");
                    return;
                }

                if (ESCache.Instance.ActiveShip.Entity == null || ESCache.Instance.DirectEve.Me.IsJumpCloakActive)
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("ESCache.Instance.ActiveShip.Entity == null || ESCache.Instance.DirectEve.Me.IsJumpCloakActive");
                    _covertCloakActionQueueAction.QueueAction();
                    return;
                }

                if ((!ESCache.Instance.DirectEve.Me.IsJumpCloakActive || ESCache.Instance.ActiveShip.Entity.Velocity > 0) && !covOpsCloak._module.IsLimboStateWithoutEffectActivating && !covOpsCloak.IsActive)
                {
                    Log.WriteLine($"Adding AddActivateCovertOpsCloakAfterJumpAction. JumpCloakActive [{ESCache.Instance.DirectEve.Me.IsJumpCloakActive}] Velocity [{ESCache.Instance.ActiveShip.Entity.Velocity}] covOpsCloak.IsLimboStateWithoutEffectActivating [{covOpsCloak._module.IsLimboStateWithoutEffectActivating}] [{covOpsCloak.IsActive}]");
                    covOpsAction.QueueAction(_rnd.Next(90, 100));
                    //covOpsAction.QueueAction();
                }
                else
                {
                    if (DebugConfig.DebugTraveler)
                        Log.WriteLine($"ESCache.Instance.DirectEve.Me.IsJumpCloakActive [{ESCache.Instance.DirectEve.Me.IsJumpCloakActive}] ActiveShip.Entity.Velocity [{ESCache.Instance.ActiveShip.Entity.Velocity > 0}] CovOpsLimbo [{covOpsCloak.IsInLimboState}] CovOpsActive [{covOpsCloak.IsActive}] CovOpsReactivationDelay [{covOpsCloak._module.ReactivationDelay}] EffectActivating [{covOpsCloak._module.EffectActivating}] DirectEve.IsEffectActivating(covOpsCloak) [{ESCache.Instance.DirectEve.IsEffectActivating(covOpsCloak._module)}]");
                }

                _covertCloakActionQueueAction.QueueAction();
            }, true);
            _covertCloakActionQueueAction.Initialize().QueueAction(5000);
        }

        private static void DeactivateCloak()
        {
            var cloak = ESCache.Instance.Modules.FirstOrDefault(m => m.TypeId == 11578);
            if (cloak != null && cloak.IsActive)
            {
                if (cloak.Click())
                {
                    Log.WriteLine($"Deactivating cloak.");
                }
            }
        }

        private static bool ReadyToCloak()
        {
            if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive)
            {
                if (!Defense.CovertOpsCloak.IsInLimboState)
                {
                    if (!Defense.CovertOpsCloak.IsActive)
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }

            return false;
        }

        private static void SetEveClientDestination(long solarSystemId)
        {
            var evePath = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();


            // [PLACEBO] just set a path within eve.. may look more legit // do we really need that tho? kinda wasted resources
            if (evePath == null || evePath.Count == 0 || evePath.All(d => d != solarSystemId))
            {
                if (DateTime.UtcNow < _nextGetLocation)
                    if (evePath.Count == 0)
                        Log.WriteLine("We have no destination set in EVE");
                    else if (evePath.All(d => d != solarSystemId))
                        Log.WriteLine("The EVE Client destination is not currently set to solarsystemId [" + solarSystemId + "]");

                // We do not have the destination set
                if (DateTime.UtcNow > _nextGetLocation || _location == null)
                {
                    Log.WriteLine("Getting Location of solarSystemId [" + solarSystemId + "]");
                    _nextGetLocation = DateTime.UtcNow.AddSeconds(10);
                    _location = ESCache.Instance.DirectEve.Navigation.GetLocation(solarSystemId);
                    ChangeTravelerState(State.CurrentTravelerState);
                    return;
                }

                if (_location != null && _location.IsValid)  //&& _location.CanGetThereFromHereViaTraveler)
                {
                    _locationErrors = 0;
                    Log.WriteLine("Setting EVE Client destination to [" + _location.Name + "]");
                    try
                    {
                        _location.SetDestination();
                        ChangeTravelerState(State.CurrentTravelerState);
                        return;
                    }
                    catch (Exception)
                    {
                        Log.WriteLine("Set destination to [" + _location + "] failed ");
                        return;
                    }
                }

                Log.WriteLine("Error setting solar system destination [" + solarSystemId + "] CanGetThereFromHereViaTraveler [" + _location.CanGetThereFromHereViaTraveler + "]");
                _locationErrors++;
                if (_locationErrors > 20)
                {
                    State.CurrentTravelerState = TravelerState.Error;
                    return;
                }

                return;
            }
        }

        /// <summary>
        ///     Navigate to a solar system
        /// </summary>
        /// <param name="solarSystemId"></param>
        private static void NavigateToBookmarkSystem(long solarSystemId)
        {
            if (DebugConfig.DebugTraveler) Log.WriteLine("NavigateToBookmarkSystem: [" + DateTime.UtcNow.ToShortTimeString() + "] solarSystemID [" + solarSystemId + "] _destinationRoute.Count() [" + _destinationRoute.Count() + "]");
            SetEveClientDestination(solarSystemId);

            _locationErrors = 0;
            if (!ESCache.Instance.InSpace)
            {
                if (ESCache.Instance.InStation)
                    if (TravelerDestination.Undock())
                        Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(4000, 6000));

                // We are not yet in space, wait for it
                return;
            }

            TravelerDestination.UndockAttempts = 0;
            //if (Logging.DebugTraveler) Logging.Log("Traveler", "Destination is set: processing...");

            //
            // Check if we are docking and if so use (or make!) dock bookmark as needed
            //
            if (Settings.Instance.UseDockBookmarks)
                if (State.CurrentInstaStationDockState != InstaStationDockState.Done &&
                    State.CurrentInstaStationDockState != InstaStationDockState.WaitForTraveler)
                {
                    InstaStationDock.ProcessState();
                    if (State.CurrentInstaStationDockState != InstaStationDockState.Done &&
                        State.CurrentInstaStationDockState != InstaStationDockState.WaitForTraveler)
                        return;
                }

            //
            // Check if we are undocking and if so use (or make!) undock bookmark as needed
            //
            if (Settings.Instance.UseUndockBookmarks)
                if (State.CurrentInstaStationUndockState != InstaStationUndockState.Done &&
                    State.CurrentInstaStationUndockState != InstaStationUndockState.WaitForTraveler)
                {
                    InstaStationUnDock.ProcessState();
                    if (State.CurrentInstaStationUndockState != InstaStationUndockState.Done &&
                        State.CurrentInstaStationUndockState != InstaStationUndockState.WaitForTraveler)
                        return;
                }


            // Find the next waypoint

            var currentIndex = _destinationRoute.IndexOf(ESCache.Instance.DirectEve.Me.CurrentSolarSystem.Id);
            var waypoint = _destinationRoute[currentIndex + 1];


            //if (Logging.DebugTraveler) Logging.Log("Traveler", "NavigateToBookmarkSystem: getting next way-points locationName");
            _locationName = ESCache.Instance.DirectEve.Navigation.GetLocationName(waypoint);
            if (DebugConfig.DebugTraveler)
                Log.WriteLine("Next Waypoint is: [" + _locationName + "]");

            var solarSystemInRoute = ESCache.Instance.DirectEve.SolarSystems[(int)waypoint];

            if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
            {
                if (solarSystemInRoute != null && !solarSystemInRoute.IsHighSecuritySpace && ESCache.Instance.ActiveShip.GroupId != (int)Group.AssaultShip && ESCache.Instance.ActiveShip.GroupId != (int)Group.Shuttle && ESCache.Instance.ActiveShip.GroupId != (int)Group.Frigate && ESCache.Instance.ActiveShip.GroupId != (int)Group.Interceptor && ESCache.Instance.ActiveShip.GroupId != (int)Group.TransportShip && ESCache.Instance.ActiveShip.GroupId != (int)Group.ForceReconShip && ESCache.Instance.ActiveShip.GroupId != (int)Group.StealthBomber && !MissionSettings.AllowNonStorylineCourierMissionsInLowSec)
                {
                    Log.WriteLine("Next Waypoint is: [" + _locationName +
                                  "] which is LOW SEC! This should never happen. Turning off AutoStart and going home. PauseAfterNextDock [true]");
                    ESCache.Instance.PauseAfterNextDock = true;
                    ESCache.Instance.DeactivateScheduleAndCloseEveAfterNextDock = true;
                    if (State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                        State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    return;
                }
            }

            // Find the stargate associated with it

            if (!ESCache.Instance.Stargates.Any())
            {
                // not found, that cant be true?!?!?!?!
                Log.WriteLine("Error [" + _locationName + "] not found, most likely lag waiting [" + Time.Instance.TravelerNoStargatesFoundRetryDelay_seconds +
                              "] seconds.");
                Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerNoStargatesFoundRetryDelay_seconds);
                return;
            }

            // Warp to, approach or jump the stargate
            var nextStargate = ESCache.Instance.Stargates.Where(e => e.Name.ToLower() == _locationName.ToLower()).ToList()
                .FirstOrDefault();
            if (nextStargate != null)
            {
                if (!ESCache.Instance.ActiveShip.Entity.IsCloaked && nextStargate.Distance < (int)Distances.JumpRange)
                {
                    if (ESCache.Instance.InWarp) return;

                    if (nextStargate.Jump())
                    {
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Keep alive."));
                        Log.WriteLine("Jumping to [" + _locationName + "]");
                        if (Defense.CovertOpsCloak != null && Defense.CovertOpsCloak.IsOnline)
                        {
                            Log.WriteLine("Covert ops cloak found, adding cloak action.");
                            AddActivateCovertOpsCloakAfterJumpAction();
                        }

                        return;
                    }

                    return;
                }

                if (nextStargate.Distance != 0)
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("if (NavigateOnGrid.NavigateToTarget(" + nextStargate.Name + ", Traveler, false, 0)) return;");
                    if (NavigateOnGrid.NavigateToTarget(nextStargate, 0))
                        return;
                }
            }
        }

        private static bool ResetStatesToDefaults(DirectAgentMission myMission)
        {
            Log.WriteLine("Traveler: ResetStatesToDefaults");
            if (State.CurrentStorylineState != StorylineState.PreAcceptMission)
                if (myMission != null && !myMission.Name.Contains("Materials for War"))
                {
                    //CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle);
                }

            _destination = null;
            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.TravelerDestinationSystemId), 0);
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            if (ESCache.Instance.InStation)
            {
                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                {
                    if (State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.Storyline &&
                    State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.CourierMission)
                    {
                        CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                    }
                }
            }
            //State.CurrentTravelerState = TravelerState.AtDestination;
            NavigateOnGrid.Reset();
            return true;
        }

        #endregion Methods
    }
}