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
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectSession : DirectObject
    {
        #region Fields

        private static DateTime _nextSession;
        private static Random _rnd = new Random();

        public static event EventHandler<EventArgs> OnSessionReadyEvent = delegate { };

        //def InSpace():
        //    return bool(session.solarsystemid) and bool(session.shipid) and session.structureid in (session.shipid, None)
        //def InShip():
        //    return bool(session.shipid) and bool(session.shipid != session.structureid)
        //def InShipInSpace():
        //    return bool(session.solarsystemid) and bool(session.shipid) and not bool(session.structureid)
        //def IsDocked():
        //    return bool(session.stationid2) or IsDockedInStructure()
        //def InStructure():
        //    return bool(session.structureid)
        //def IsDockedInStructure():
        //    return bool(session.structureid) and bool(session.structureid != session.shipid)

        private readonly bool DebugCheckSessionReady = false;
        private bool? _inDockableLocation;
        private bool? _inSpace;
        private static bool _IsReady;

        #endregion Fields

        #region Constructors

        internal DirectSession(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public long? AllianceId => (long?)Session.Attribute("allianceid");
        public DirectOwner Character => DirectEve.GetOwner(CharacterId ?? -1);
        public long? CharacterId => (long?)Session.Attribute("charid");
        public long? ConstellationId => (long?)Session.Attribute("constellationid");

        public DirectConstellation Constellation
        {
            get
            {
                if (ConstellationId == null) return null;

                if (ESCache.Instance.DirectEve.Constellations.Any(n => n.Value.Id == ConstellationId))
                {
                    DirectConstellation tempConstellation = ESCache.Instance.DirectEve.Constellations.FirstOrDefault(n => n.Value.Id == ConstellationId).Value;
                    return tempConstellation;
                }

                return null;
            }
        }

        public long? CorporationId => (long?)Session.Attribute("corpid");
        public long? FleetId => (long?)Session.Attribute("fleetid");

        public bool InFleet
        {
            get
            {
                if (FleetId == null)
                    return false;

                return true;
            }
        }

        public DirectChatWindow LocalChatChannel
        {
            get
            {
                if (!IsInSpace && !IsInDockableLocation)
                    return null;

                if (IsAbyssalDeadspace || IsWspace)
                    return null;

                return DirectEve.ChatWindows.Find(w => w.Name.StartsWith("chatchannel_local"));
            }
        }

        private List<DirectCharacter> _charactersInLocal;
        public List<DirectCharacter> CharactersInLocal
        {
            get
            {
                if (_charactersInLocal != null)
                    return _charactersInLocal;

                if (!IsInSpace && !IsInDockableLocation)
                    return new List<DirectCharacter>();

                if (IsAbyssalDeadspace || IsWspace)
                    return new List<DirectCharacter>();

                if (LocalChatChannel != null && LocalChatChannel.Members.Any(i => i.CharacterId != CharacterId))
                {
                    _charactersInLocal = LocalChatChannel.Members.Where(i => i.CharacterId != CharacterId).ToList();
                    return _charactersInLocal;
                }

                return new List<DirectCharacter>();
            }
        }

        public bool IsAbyssalDeadspace
        {
            get
            {
                //
                //https://gist.github.com/a-tal/5ff5199fdbeb745b77cb633b7f4400bb
                //32,000,000 	33,000,000 	Abyssal systems
                //
                if (SolarSystemId >= 32000000 && SolarSystemId <= 33000000)
                {
                    if (DirectEve.Interval(30000, 30000, true.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), true);
                    return true;
                }

                if (DirectEve.Interval(30000, 30000, false.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), false);
                return false;
            }
        }

        public bool IsVoidSpace
        {
            get
            {
                //
                //34,000,000 	34,999,999 	Void systems
                //
                if (SolarSystemId >= 34000000 && SolarSystemId <= 34999999)
                {
                    //if (DirectEve.Interval(30000, 30000, ItemId.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), false);
                    return true;
                }

                return false;
            }
        }

        public bool IsKSpace
        {
            get
            {
                //
                //https://gist.github.com/a-tal/5ff5199fdbeb745b77cb633b7f4400bb
                //32,000,000 	33,000,000 	Abyssal systems
                //
                if (SolarSystemId >= 30000000 && SolarSystemId <= 31000000)
                {
                    if (DirectEve.Interval(30000, 30000, true.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), true);
                    return true;
                }

                if (DirectEve.Interval(30000, 30000, true.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), false);
                return false;
            }
        }

        public bool IsInDockableLocation
        {
            get
            {
                if (!IsReady)
                    return false;

                if (_inDockableLocation != null)
                    return (bool)_inDockableLocation;

                _inDockableLocation = InDockableLocation;
                return _inDockableLocation.Value;
            }
        }

        public bool IsInSpace => IsReady && (_inSpace ?? (_inSpace = InSpace).Value);

        public bool InJump
        {
            get
            {
                bool boolIsSubwayEnabled = (bool)DirectEve.GetLocalSvc("subway").Attribute("ENABLED");
                bool boolIsSubwayInJump = (bool)DirectEve.GetLocalSvc("subway").Call("InJump");

                if (!IsInDockableLocation && boolIsSubwayInJump)
                {
                    Time.Instance.LastInJump = DateTime.UtcNow;
                    Task.Run(() =>
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastSessionReady),
                            DateTime.UtcNow);
                    });

                    DirectEve.DictEntitiesXPositionInfoCachedAcrossFrames = new Dictionary<long, double>();
                    DirectEve.DictEntitiesYPositionInfoCachedAcrossFrames = new Dictionary<long, double>();
                    DirectEve.DictEntitiesZPositionInfoCachedAcrossFrames = new Dictionary<long, double>();
                    return true;
                }

                if (Time.Instance.LastInJump.AddSeconds(2) > DateTime.UtcNow)
                    return true;

                return false;
            }
        }

        public bool IsKnownSpace
        {
            get
            {
                //
                //https://gist.github.com/a-tal/5ff5199fdbeb745b77cb633b7f4400bb
                //30,000,000 	31,000,000 	NEW-EDEN Solar systems
                //
                if (SolarSystemId >= 30000000 && SolarSystemId <= 31000000)
                    return true;

                return false;
            }
        }

        // holds the value of session ready, will be updated once a frame
        public bool IsReady
        {
            get
            {
                if (_nextSession > DateTime.UtcNow)
                    return false;

                return _IsReady;
            }

            // will be called once a frame, due that fact we can set the next session timer here if the value is false,
            // that way we achieve of having a delay of GetNextSessionTimer value after last session = false
            private set => _IsReady = value;
        }

        public bool IsWspace
        {
            get
            {
                try
                {
                    //
                    //https://gist.github.com/a-tal/5ff5199fdbeb745b77cb633b7f4400bb
                    // 31,000,000 	32,000,000 	Wormhole Solar systems
                    //
                    if (SolarSystemId >= 31000000 && SolarSystemId <= 32000000)
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

        public int? LocationId => (int?)Session.Attribute("locationid");

        public long? RegionId => (long?)Session.Attribute("regionid");

        public DirectRegion Region
        {
            get
            {
                if (RegionId == null) return null;

                if (ESCache.Instance.DirectEve.Regions.Any(n => n.Value.Id == RegionId))
                {
                    DirectRegion tempRegion = ESCache.Instance.DirectEve.Regions.FirstOrDefault(n => n.Value.Id == RegionId).Value;
                    return tempRegion;
                }

                return null;
            }
        }

        public long? ShipId => (long?)Session.Attribute("shipid");

        public DirectSolarSystem SolarSystem
        {
            get
            {
                if (SolarSystemId != null && ESCache.Instance.DirectEve.Session != null && !ESCache.Instance.DirectEve.Session.IsWspace)
                    return DirectEve.SolarSystems.Values.FirstOrDefault(k => k.Id.Equals(DirectEve.Session.SolarSystemId));

                return null;
            }
        }

        public string SolarSystemName
        {
            get
            {
                if (SolarSystemId != null && ESCache.Instance.DirectEve.Session != null && !ESCache.Instance.DirectEve.Session.IsWspace && SolarSystem != null)
                    return SolarSystem.Name;

                return string.Empty;
            }
        }

        public int? SolarSystemId => (int?)Session.Attribute("solarsystemid2");
        public int? StationId => (int?)Session.Attribute("stationid");

        public long? Structureid => (long?)Session.Attribute("structureid");

        public bool HasStructureId => Session.Attribute("structureid").IsValid;

        public bool HasStationId => Session.Attribute("stationid").IsValid;

        public DirectStation Station
        {
            get
            {
                if (DirectEve.Stations.Count > 0 && IsInDockableLocation && StationId != null)
                {
                    DirectStation _station = null;
                    DirectEve.Stations.TryGetValue((int)StationId, out _station);
                    if (_station != null) return _station;
                    return null;
                }

                return null;
            }
        }

        public int UserType => (int)Session.Attribute("userType");

        private static DateTime GetNextSessionTimer =>
            DateTime.UtcNow.AddMilliseconds(_rnd.Next(5000, 5300));

        private bool __inDockableLocation => (LocationId.HasValue && LocationId == StationId) || Structureid.HasValue;
        private bool __inSpace => LocationId.HasValue && LocationId == SolarSystemId && !Structureid.HasValue;

        private bool InDockableLocation
        {
            get
            {
                try
                {
                    if (!IsReady)
                    {
                        if (DebugConfig.DebugInStation) Log.WriteLine("IsStation: False if (!IsReady)");
                        return false;
                    }

                    if (__inSpace)
                    {
                        if (DebugConfig.DebugInStation) Log.WriteLine("IsStation: false: if (__inSpace)");
                        return false;
                    }

                    if (!__inDockableLocation)
                    {
                        if (DebugConfig.DebugInStation) Log.WriteLine("IsStation: false: if (!__inDockableLocation)");
                        return false;
                    }

                    if (DirectEve.AnyEntities())
                    {
                        if (DebugConfig.DebugInStation) Log.WriteLine("IsStation: false: if (DirectEve.AnyEntities())");
                        return false;
                    }

                    if (DirectEve.ActiveShip == null)
                    {
                        if (DebugConfig.DebugInStation) Log.WriteLine("IsStation: false: if (DirectEve.ActiveShip == null)");
                        return false;
                    }

                    if (DirectEve.ActiveShip.Entity != null)
                        return false;

                    if (DirectEve.Interval(2000))
                        Task.Run(() =>
                        {
                            try
                            {
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.IsDocked), true);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }

                        });

                    DirectEve.DictEntitiesXPositionInfoCachedAcrossFrames = new Dictionary<long, double>();
                    DirectEve.DictEntitiesYPositionInfoCachedAcrossFrames = new Dictionary<long, double>();
                    DirectEve.DictEntitiesZPositionInfoCachedAcrossFrames = new Dictionary<long, double>();
                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

        private bool InSpace
        {
            get
            {
                try
                {
                    if (!IsReady)
                        return false;

                    if (!__inSpace)
                        return false;

                    if (__inDockableLocation) // in station
                        return false;

                    if (!DirectEve.AnyEntities())
                        return false;

                    if (DirectEve.ActiveShip == null)
                        return false;

                    if (DirectEve.ActiveShip.Entity == null)
                        return false;

                    if (DirectEve.Interval(2000))
                        Task.Run(() =>
                        {
                            try
                            {
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.IsDocked), false);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        });

                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

        private PyObject Session => PySharp.Import("__builtin__").Attribute("eve").Attribute("session");

        #endregion Properties

        #region Methods

        // makes the session invalid until GetNextSessionTimer value. make sure to call this on for example (undock / dock / jump / switch ship)
        // and ensure the execution flow returns ( stop the current frame execution ) at this point.
        // else the client might hang if there are any methods called between the session change
        public static void SetSessionNextSessionReady(int min = 5000, int max = 5300)
        {
            _nextSession = DateTime.UtcNow.AddMilliseconds(_rnd.Next(min, max));
        }

        public static DateTime LastSessionChange { get; set; } = DateTime.UtcNow;

        // being called once a frame by the controller manager, should not be called elsewhere
        public void SetSessionReady()
        {
            var prevValue = _IsReady;
            IsReady = CheckSessionReady();

            if (prevValue != _IsReady)
            {
                LastSessionChange = DateTime.UtcNow;
                DirectEve.Log($"Session value changed. Previous session value [{prevValue}]. Current value [{_IsReady}].");

                if (_IsReady)
                {
                    // TODO: fire session change event here  [done]
                    OnSessionReadyEvent(this, EventArgs.Empty);
                    DirectEntity.OnSessionChange();
                    DirectUIModule.OnSessionChange();
                    DirectWorldPosition.OnSessionChange();
                }
            }
        }

        // being called once a frame by the controller manager, should not be called elsewhere
        private bool CheckSessionReady()
        {
            if (!DirectEve.HasFrameChanged())
                return _IsReady;

            if (DateTime.UtcNow < Time.Instance.NextCheckSessionReady)
                return false;

            var inSpace = __inSpace;
            var inDockableLocation = __inDockableLocation;
            var michelle = DirectEve.GetLocalSvc("michelle", false, false);
            var undockingSvc = DirectEve.GetLocalSvc("undocking", false, false);
            var godma = DirectEve.GetLocalSvc("godma");
            var dockingHeroNotification = DirectEve.GetLocalSvc("dockingHeroNotification");


            if (!michelle.IsValid)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (!michelle.IsValid) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (inSpace)
            {
                var ballparkReady = michelle["bpReady"].ToBool();

                if (!ballparkReady)
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (!ballpark.IsValid) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }

                var michelleBallpark = michelle.Attribute("_Michelle__bp");

                if (!michelleBallpark.IsValid)
                {
                    if (DebugCheckSessionReady)
                    {
                        Log.WriteLine("_Michelle__bpis invalid.");
                    }

                    return false;
                }

                var remoteBallpark = michelleBallpark["remoteBallpark"];

                if (!remoteBallpark.IsValid)
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (!remoteBallpark.IsValid) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }

                var bindParams = remoteBallpark.Attribute("_Moniker__bindParams");
                if (!bindParams.IsValid)
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (!bindParams.IsValid) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }

                if (bindParams.ToInt() != SolarSystemId)
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (bindParams.ToInt() != SolarSystemId) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }

                var bpSolarSystemId = michelleBallpark.Attribute("solarsystemID");
                if (bpSolarSystemId.IsValid && bpSolarSystemId.ToInt() != SolarSystemId)
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (bpSolarSystemId.IsValid && bpSolarSystemId.ToInt() != SolarSystemId) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }
            }

            if (inDockableLocation)
                if (undockingSvc.Attribute("exitingDockableLocation").ToBool())
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (undockingSvc.Attribute(exitingDockableLocation).ToBool()) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }


            if (godma.IsValid)
            {
                var priming = godma["stateManager"]["priming"];
                if (priming.IsValid && priming.ToBool())
                {
                    if (DebugCheckSessionReady) Log.WriteLine("godma['stateManager']['priming'].ToBool()");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }
            }

            if (dockingHeroNotification.IsValid)
            {
                var data = dockingHeroNotification["_active_notification_cancellation_tokens"]["data"];
                if (data.IsValid && data.ToList().Count > 0)
                {
                    if (DebugCheckSessionReady) Log.WriteLine("dockingHeroNotification[\"_active_notification_cancellation_tokens\"][\"data\"] is valid and has more than 0 items.");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }
            }

            if (ShipId == null)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (ShipId == null) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (!Session.IsValid)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (!Session.IsValid) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (!Session.Attribute("locationid").IsValid)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (!Session.Attribute(locationid).IsValid) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (!Session.Attribute("solarsystemid2").IsValid)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (!Session.Attribute(solarsystemid2).IsValid) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (Session.Attribute("changing").IsValid)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (Session.Attribute(changing).IsValid) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if ((bool)Session.Attribute("mutating"))
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (Session.Attribute(mutating) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (!(bool)Session.Attribute("rwlock").Call("IsCool"))
                if (DirectEve.Windows == null || DirectEve.Windows.Count == 0)
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (!(bool)Session.Attribute(rwlock).Call(IsCool))]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }

            if (DirectEve.GetLocalSvc("jumpQueue", false, false).Attribute("jumpQueue").IsValid)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (DirectEve.GetLocalSvc(jumpQueue, false, false).Attribute(jumpQueue).IsValid) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (Session.Attribute("nextSessionChange").IsValid) // next session change is always +10 sec after a session change
            {
                var nextSessionChange = Session.Attribute("nextSessionChange").ToDateTime();
                nextSessionChange = nextSessionChange.AddSeconds(-5);
                if (nextSessionChange >= DateTime.UtcNow)
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (nextSessionChange >= Now) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }
            }

            var station = DirectEve.GetLocalSvc("station", false, false);
            if (station.IsValid)
            {
                if ((bool)station.Attribute("activatingShip"))
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if ((bool) station.Attribute(activatingShip)) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }

                if ((bool)station.Attribute("loading"))
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if ((bool) station.Attribute(loading)) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }

                if ((bool)station.Attribute("leavingShip"))
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if ((bool) station.Attribute(leavingShip)) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }
            }

            var loading = (bool)DirectEve.Layers.LoadingLayer
                .Attribute("display");
            if (loading)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (loading) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            var anyEnt = DirectEve.AnyEntities();

            if (inDockableLocation && anyEnt)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (inStation && anyEnt) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (inSpace)
                if (!anyEnt)
                {
                    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (inSpace) if (!anyEnt) ]");
                    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                    return false;
                }

            if (DirectEve.Me.IsJumpCloakActive && DirectEve.Me.JumpCloakRemainingSeconds >= 57)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (DirectEve.Me.IsJumpCloakActive && DirectEve.Me.JumpCloakRemainingSeconds >= 58) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (DirectEve.Me.IsInvuln && DirectEve.Me.InvulnRemainingSeconds() >= 27)

            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (DirectEve.Me.IsInvulnUndock && DirectEve.Me.IsInvulnUndockRemainingSeconds >= 28) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (DirectEve.Windows == null || DirectEve.Windows.Count == 0)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (DirectEve.Windows != null || !DirectEve.Windows.Any()) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            if (DirectEve.GetLocalSvc("sceneManager")["primaryJob"]["scene"]["objects"].ToList().Count < 3)
            {
                if (DebugCheckSessionReady)
                {
                    Console.WriteLine("sceneManager.primaryJob.scene.object amount is below 3.");
                }
                return false;
            }

            var wnd = DirectEve.Windows.FirstOrDefault(w => w.Guid == "form.LobbyWnd" || w.WindowId == "overview"); // both can't be active at a time

            if (wnd == null)
            {
                if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (wnd == null) ]");
                Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                return false;
            }

            var display = wnd.PyWindow.Attribute("_display").ToBool();
            //var alignmentDirty = wnd.PyWindow.Attribute("_alignmentDirty").ToBool();
            //var childAlignmentDirty = wnd.PyWindow.Attribute("_childrenAlignmentDirty").ToBool();
            //var displayDirty = wnd.PyWindow.Attribute("_displayDirty").ToBool();

            if (!DirectEve.NewEdenStore.IsStoreOpen)
            {
                //if (alignmentDirty || childAlignmentDirty || !display)
                //{
                //    if (DebugCheckSessionReady) Log.WriteLine("IsReady is false [ if (displayDirty || !display) ]");
                //    Time.Instance.NextCheckSessionReady = DateTime.UtcNow.AddSeconds(1);
                //    return false;
                //}
                if (!display)
                {
                    if (DebugCheckSessionReady)
                    {
                        Log.WriteLine("if (!display)");
                    }

                    return false;
                }
            }

            if (InJump && !DebugConfig.DebugDisableInJumpChecking)
            {
                if (DirectEve.Interval(2000)) Log.WriteLine("InJump is true: GetLocalSvc(subway).Call(InJump)");
                return false;
            }

            return true;
        }

        #endregion Methods
    }
}
