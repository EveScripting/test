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
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using SC::SharedComponents.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum ScanGroup
    {
        Starbase = 0,
        Scrap = 1,
        Fighter = 2,
        Signature = 3,
        Ship = 4,
        Structure = 5,
        Drone = 6,
        Celestial = 7,
        Anomaly = 8,
        Charge = 9,
        NPC = 10,
        Orbital = 11,
        Deployable = 12,
        Sovereignty = 13,
        FilamentTrace = 14,
    }

    /**
    public Dictionary<int, string> EXPLORATION_SITE_TYPES
    {
        siteTypeOre = "OreSite",
        siteTypeGas = "GasSite",
        siteTypeRelic = "RelicSite",
        siteTypeData = "DataSite",
        siteTypeWormhole = "Wormhole",
        siteTypeCombat = "Combat"
    }
    **/

    //Define a new class called DirectScanResultTrackingIdentifier
    //Add a new property called ID of type string
    //Add a new property called ScanGroupID of type int
    //Add a new property called SolarSystem of type DirectSolarSystem

    public class DirectSystemScanResult : DirectObject
    {
        #region Fields

        internal PyObject PyResult;

        #endregion Fields

        //'itemID': 1026895148307L,
        //'typeID': None,
        //'isIdentified': False,
        //'scanGroupID': 4,
        //'factionID': None,
        //'difficulty': None,
        //'data': (-76322780977.72708, -25978895447.96276, 430583048001.9394),
        //'certainty': 0.08137081820865469,
        //'prevCertainty': 0.08137081886993291,
        //'pos': (-76322780977.72708, -25978895447.96276, 430583048001.9394),
        //'groupID': None,
        //'strengthAttributeID': None,
        //'isPerfect': False,
        //'dungeonNameID': None,
        //'GetDistance': <bound method Result._GetDistance of <Result : HPK-944 - 4, None, None>>,
        //'id': 'HPK-943'}>

        #region Constructors

        internal DirectSystemScanResult(DirectEve directEve, PyObject pyResult)
            : base(directEve)
        {
            PyResult = pyResult;
            Id = (string) pyResult.Attribute("id");
            ScanGroupID = (int) pyResult.Attribute("scanGroupID");
            //StrengthAttributeID = (string)pyResult.Attribute("strengthAttributeID");
            //if (!string.IsNullOrEmpty(StrengthAttributeID))
            //{
            //    Logging.Log.WriteLine("strengthAttributeID is [" + StrengthAttributeID.ToString() + "]");
            //}

            TypeID = (int) pyResult.Attribute("typeID");
            GroupID = (int) pyResult.Attribute("groupID");
            ScanGroup = (ScanGroup) ScanGroupID;
            IsPerfectResult = (bool) pyResult.Attribute("isPerfect");
            //GroupName = (string) pyResult.Attribute("groupName").ToUnicodeString();
            //TypeName = (string) pyResult.Attribute("typeName").ToUnicodeString();
            SignalStrength = (double) pyResult.Attribute("certainty");
            PreviousSignalStrength = (double) pyResult.Attribute("prevCertainty");
            Deviation = (double) pyResult.Attribute("deviation");
            var pos = pyResult.Attribute("pos");
            var data = pyResult.Attribute("data");

            Pos = new Vec3((double)pos.GetItemAt(0), (double)pos.GetItemAt(1), (double)pos.GetItemAt(2));
            // Data can also be a float
            Data = data.GetPyType() == PyType.TupleType ? new Vec3((double)data.GetItemAt(0), (double)data.GetItemAt(1), (double)data.GetItemAt(2)) : new Vec3(0, 0, 0);
            IsPointResult = (string) PyResult.Attribute("data").Attribute("__class__").Attribute("__name__") == "tuple";
            IsSphereResult = (string) PyResult.Attribute("data").Attribute("__class__").Attribute("__name__") == "float";
            IsMultiPointResult = (string) PyResult.Attribute("data").Attribute("__class__").Attribute("__name__") == "list";
            //isCircleResult
            if (IsPerfectResult)
            {
                X = (double?)pyResult.Attribute("data").Attribute("x");
                Y = (double?)pyResult.Attribute("data").Attribute("y");
                Z = (double?)pyResult.Attribute("data").Attribute("z");
            }
            //IsCircleResult = !IsPointResult && !IsSpereResult;
            //if (IsPointResult)
            //{
            //    X = (double?) pyResult.Attribute("data").Attribute("x");
            //    Y = (double?) pyResult.Attribute("data").Attribute("y");
            //    Z = (double?) pyResult.Attribute("data").Attribute("z");
            //}
            //else if (IsCircleResult)
            //{
            //    X = (double?) pyResult.Attribute("data").Attribute("point").Attribute("x");
            //    Y = (double?) pyResult.Attribute("data").Attribute("point").Attribute("y");
            //    Z = (double?) pyResult.Attribute("data").Attribute("point").Attribute("z");
            //}

            // If SphereResult: X,Y,Z is probe location

            //if (X.HasValue && Y.HasValue && Z.HasValue)
            //{
            //    var myship = directEve.ActiveShip.Entity;
            //    Distance = Math.Sqrt((X.Value - myship.X) * (X.Value - myship.X) + (Y.Value - myship.Y) * (Y.Value - myship.Y) +
            //                         (Z.Value - myship.Z) * (Z.Value - myship.Z));
            //}
            //GroupName = GetGroupName();
            TypeName = GetTypeName();
            Faction = GetFaction();
            if (directEve.Session.SolarSystem != null)
            {
                SolarSystem = directEve.Session.SolarSystem;
                SolarSystemId = directEve.Session.SolarSystem.Id;
            }

            ScanResultTime = DateTime.UtcNow;
        }

        #endregion Constructors

        #region Properties

        public DateTime ScanResultTime { get; internal set; }
        public DirectSolarSystem SolarSystem { get; internal set; }
        public long SolarSystemId { get; internal set; }

        public Faction Faction;

        public Faction GetFaction()
        {
            if (!string.IsNullOrEmpty(TypeName))
            {
                if (TypeName.ToLower().Contains("Angel".ToLower()))
                    return DirectNpcInfo.AngelCartelFaction;

                if (TypeName.ToLower().Contains("Blood".ToLower()))
                    return DirectNpcInfo.BloodRaiderCovenantFaction;

                if (TypeName.ToLower().Contains("Guristas".ToLower()))
                    return DirectNpcInfo.GuristasPiratesFaction;

                if (TypeName.ToLower().Contains("Sasha".ToLower()))
                    return DirectNpcInfo.SanshasNationFaction;

                if (TypeName.ToLower().Contains("Serpentis".ToLower()))
                    return DirectNpcInfo.SerpentisFaction;

                if (TypeName.ToLower().Contains("Rogue".ToLower()))
                    return DirectNpcInfo.RogueDronesFaction;

                if (TypeName.ToLower().Contains("Conduit".ToLower()))
                    return DirectNpcInfo.TriglavianFaction;
            }

            return null;
        }

        public string GetTypeName()
        {
            if (ScanGroup == ScanGroup.Signature || ScanGroup == ScanGroup.Anomaly)
            {
                if (PyResult.Attribute("dungeonNameID").IsValid)
                {
                    return DirectEve.GetLocalizationMessageById(PyResult.Attribute("dungeonNameID").ToInt());
                }
            }
            //if (PyResult.Attribute("typeID").IsValid)
            //{
            //    var typeId = PyResult.Attribute("typeID").ToInt();
            //    DirectEve.Log(typeId.ToString());
            //    //return DirectEve.GetInvType(typeId).TypeName;
            //}
            return string.Empty;
        }

        public bool IsSiteWithNoOre
        {
            get
            {
                if (Scanner.SitesWithNoOre.ContainsKey(Id))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsSiteWithNoIce
        {
            get
            {
                return false;
            }
        }

        public bool IsSiteWithNPCs
        {
            get
            {
                if (Scanner.SitesWithNPCs.ContainsKey(Id))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsSiteWithOtherMiners
        {
            get
            {
                if (Scanner.SitesWithOtherMiners.ContainsKey(Id))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsSiteWithPvP
        {
            get
            {
                if (Scanner.SitesWithPvP.ContainsKey(Id))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsAnomaly
        {
            get
            {
                if (ScanGroup == ScanGroup.Anomaly)
                    return true;

                return false;
            }
        }

        public bool IsSignature
        {
            get
            {
                if (ScanGroup == ScanGroup.Signature)
                    return true;

                return false;
            }
        }

        public bool IsCombatSite
        {
            get
            {
                if (IsSignature || IsAnomaly)
                    if (GroupName.Contains("Combat"))
                        return true;

                return false;
            }
        }

        public bool IsDeepFlowSite
        {
            get
            {
                if (IsSignature)
                    if (GroupName.Contains("Combat"))
                        if (GetTypeName().Contains("Deepflow Rift"))
                            return true;

                return false;
            }
        }

        public string GetGroupName()
        {
            if (ScanGroup == ScanGroup.Signature || ScanGroup == ScanGroup.Anomaly)
            {
                if (PyResult.Attribute("strengthAttributeID").IsValid)
                {
                    var i = PyResult.Attribute("strengthAttributeID").ToInt();
                    if (DebugConfig.DebugProbeScanner) Logging.Log.WriteLine("Id [" + Id + "] ScanGroup [" + ScanGroup + "] strengthAttributeID [" + i + "]");
                    var d = DirectEve.Const["EXPLORATION_SITE_TYPES"].ToDictionary<int>();
                    if (DebugConfig.DebugProbeScanner)
                    {
                        foreach (var entry_d in d)
                        {
                            Logging.Log.WriteLine("entry_d Key [" + entry_d.Key + "] Value [" + entry_d.Value + "]");
                        }
                    }
                    if (d.ContainsKey(i))
                    {
                        return DirectEve.GetLocalizationMessageByLabel(d[i].ToUnicodeString());
                    }
                }
                else if (DebugConfig.DebugProbeScanner) Logging.Log.WriteLine("!if (PyResult.Attribute(\"strengthAttributeID\").IsValid)");
            }
            //if (PyResult.Attribute("groupID").IsValid)
            //{
            //    var groupId = PyResult.Attribute("groupID").ToInt();
            //    // TODO: finish (evetypes.GetGroupNameByGroup)
            //}
            return string.Empty;
        }

        public Vec3 Data { get; internal set; }

        public string TypeName { get; internal set; }

        public double? X { get; internal set; }
        public double? Y { get; internal set; }
        public double? Z { get; internal set; }
        public double Deviation { get; internal set; }

        public int GroupID { get; internal set; }
        public string Id { get; internal set; }

        public bool IsOnGridWithMe
        {
            get
            {
                if (IsPerfectResult)
                {
                    if (Distance != null)
                    {
                        if ((double)Distances.OnGridWithMe >= Distance.Value)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public double? Distance
        {
            get
            {
                if (IsPerfectResult)
                {
                    if (X.HasValue && Y.HasValue && Z.HasValue)
                    {
                        if (ESCache.Instance.ActiveShip != null)
                        {
                            return ESCache.Instance.ActiveShip.Entity.DistanceTo(Pos);
                        }

                        return null;
                    }

                    return null;
                }

                return null;
            }
        }

        public bool IsPerfectResult { get; internal set; }
        public bool IsPointResult { get; internal set; }
        public bool IsSphereResult { get; internal set; }
        public bool IsMultiPointResult { get; internal set; }
        public Vec3 Pos { get; internal set; }
        public double PreviousSignalStrength { get; internal set; }
        public ScanGroup ScanGroup { get; internal set; }

        public Dictionary<long, double> _distanceToProbes = new Dictionary<long, double>();

        public Dictionary<long, double> DistanceToProbes
        {
            get
            {
                if (!DirectEve.HasFrameChanged())
                    return _distanceToProbes;

                _distanceToProbes = new Dictionary<long, double>();
                if (Scanner.GetProbes().Any())
                {
                    foreach (var probe in Scanner.GetProbes())
                    {
                        var _distanceInMeters = DirectWorldPosition.GetDistance(Pos, probe.Pos);
                        _distanceToProbes.Add(probe.ProbeId, _distanceInMeters);
                    }

                    return _distanceToProbes;
                }

                return new Dictionary<long, double>();
            }
        }

        public Dictionary<long, double> _distanceToPlanets = new Dictionary<long, double>();

        public Dictionary<long, double> DistanceToPlanets
        {
            get
            {
                if (!DirectEve.HasFrameChanged())
                    return _distanceToPlanets;

                _distanceToPlanets = new Dictionary<long, double>();
                if (ESCache.Instance.Planets.Any())
                {
                    foreach (var planet in ESCache.Instance.Planets)
                    {
                        var _distanceInMeters = DirectWorldPosition.GetDistance(Pos, planet._directEntity.DirectAbsolutePosition.PositionInSpace);
                        _distanceToPlanets.Add(planet.Id, _distanceInMeters);
                    }

                    return _distanceToPlanets;
                }

                return new Dictionary<long, double>();
            }
        }

        public long ClosestPlanetIdThatWehaventYetScannedFrom
        {
            get
            {
                if (DistanceToPlanets.Any())
                {
                    return DistanceToPlanets.Where(x => !Scanner.PlanetsWeHaveScannedFrom.ContainsKey(x.Key)).OrderBy(i => i.Value).FirstOrDefault().Key;
                }

                return 0;
            }
        }

        public Vec3 vec3NextPlanetToScanFrom
        {
            get
            {
                foreach (EntityCache planet in ESCache.Instance.Planets.OrderByDescending(i => i.Id == ClosestPlanetIdThatWehaventYetScannedFrom))
                {
                    if (!Scanner.PlanetsWeHaveScannedFrom.ContainsKey(planet.Id))
                    {
                        Log.WriteLine("Add [" + planet.Name + "] to the list of planets we have scanned from.");
                        Scanner.PlanetsWeHaveScannedFrom.AddOrUpdate(planet.Id, true);
                        if (ESCache.Instance.Planets.Any(i => (double)Distances.OneAu > i.Distance))
                        {
                            foreach (EntityCache closePlanet in ESCache.Instance.Planets.Where(i => (double)Distances.OneAu > i.Distance))
                            {
                                if (!Scanner.PlanetsWeHaveScannedFrom.ContainsKey(closePlanet.Id))
                                {
                                    Log.WriteLine("Add [" + closePlanet.Name + "] to the list of planets we have scanned from because it is close to [" + planet.Name + "]");
                                    Scanner.PlanetsWeHaveScannedFrom.AddOrUpdate(closePlanet.Id, true);
                                }
                            }
                        }

                        Scanner.strNextPlanetToScanFrom = planet.Name;
                        Scanner.PlanetsLeftToScanFrom();
                        Log.WriteLine("NextPlanetToScanFrom is [" + Scanner.strNextPlanetToScanFrom + "][" + planet.Id + "]");
                        Vec3 planetCoord = new Vec3(planet._directEntity.DirectAbsolutePosition.XCoordinate, planet._directEntity.DirectAbsolutePosition.YCoordinate, planet._directEntity.DirectAbsolutePosition.ZCoordinate);
                        return planetCoord;
                    }
                }

                Log.WriteLine("NextPlanetToScanFrom is you ship!? no planets left to scan from: Clearing list");
                Scanner.PlanetsWeHaveScannedFrom = new Dictionary<long, bool>();
                return ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.PositionInSpace;
            }
        }

        public bool IsIgnoredScanGroup
        {
            get
            {
                if (ScanGroup == ScanGroup.Celestial)
                    return true;

                if (ScanGroup == ScanGroup.Charge)
                    return true;

                if (ScanGroup == ScanGroup.Deployable)
                    return true;

                if (ScanGroup == ScanGroup.Drone)
                    return true;

                if (ScanGroup == ScanGroup.Fighter)
                    return true;

                if (ScanGroup == ScanGroup.NPC)
                    return true;

                if (ScanGroup == ScanGroup.Orbital)
                    return true;

                if (ScanGroup == ScanGroup.Scrap)
                    return true;

                if (ScanGroup == ScanGroup.Ship)
                    return true;

                if (ScanGroup == ScanGroup.Sovereignty)
                    return true;

                if (ScanGroup == ScanGroup.Starbase)
                    return true;

                if (ScanGroup == ScanGroup.Structure)
                    return true;

                return false;
            }
        }

        public bool IsUnknownSite
        {
            get
            {
                if (string.IsNullOrEmpty(GroupName))
                    return true;

                return false;
            }
        }

        public bool IsGasSite
        {
            get
            {
                if (GroupName.ToLower().Contains("Gas".ToLower()))
                    return true;

                return false;
            }
        }

        public bool IsOreSite
        {
            get
            {
                if (GroupName.ToLower().Contains("Ore".ToLower()))
                    return true;

                return false;
            }
        }

        public bool IsIceSite
        {
            //https://wiki.eveuniversity.org/Ice_harvesting
            get
            {
                if (TypeName.ToLower().Contains("Clear Icicle Belt".ToLower()) ||
                    TypeName.ToLower().Contains("White Glaze Belt".ToLower()) ||
                    TypeName.ToLower().Contains("Blue Ice Belt".ToLower()) ||
                    TypeName.ToLower().Contains("Glacial Mass Belt".ToLower()) ||
                    TypeName.ToLower().Contains("Enriched Clear Icicle Belt".ToLower()) ||
                    TypeName.ToLower().Contains("Pristine White Glaze Belt".ToLower()) ||
                    TypeName.ToLower().Contains("Thick Blue Ice Belt".ToLower()) ||
                    TypeName.ToLower().Contains("Smooth Glacial Mass Belt".ToLower()) ||
                    TypeName.ToLower().Contains("Shattered Ice Field".ToLower())
                    )
                    return true;

                return false;
            }
        }

        public bool IsRelicSite
        {
            get
            {
                if (GroupName.ToLower().Contains("Relic".ToLower()))
                    return true;

                return false;
            }
        }

        public bool IsDataSite
        {
            get
            {
                if (GroupName.ToLower().Contains("Data".ToLower()))
                    return true;

                return false;
            }
        }

        public bool IsHomefrontSite
        {
            get
            {
                if (GroupName.ToLower().Contains("Homefront".ToLower()))
                    return true;

                return false;
            }
        }

        public bool IsWormhole
        {
            get
            {
                if (GroupName.ToLower().Contains("Wormhole".ToLower()))
                    return true;

                return false;
            }
        }

        public int ScanGroupID { get; internal set; }

        public string _groupName = string.Empty;

        public string GroupName
        {
            get
            {
                if (!string.IsNullOrEmpty(_groupName))
                    return _groupName;

                if (Scanner.ProbeScannerWindowScanResults.Any())
                {
                    if (DebugConfig.DebugProbeScanner) Logging.Log.WriteLine("DirectSystemScanResult: ESCache.Instance.probeScannerWindow.ProbeScannerWindowScanResults is not empty");
                    int intCount = 0;
                    foreach (var ProbeScannerWindowScanResult in Scanner.ProbeScannerWindowScanResults)
                    {
                        intCount++;
                        if (DebugConfig.DebugProbeScanner) Logging.Log.WriteLine("DirectSystemScanResult: ProbeScannerWindowScanResult[" + intCount + "]");
                        if (!string.IsNullOrEmpty(ProbeScannerWindowScanResult.ID))
                        {
                            if (DebugConfig.DebugProbeScanner) Log.WriteLine("DirectSystemScanResult: ProbeScannerWindowScanResult.ID [" + ProbeScannerWindowScanResult.ID + "]");
                            if (ProbeScannerWindowScanResult.ID.ToLower() == Id.ToLower())
                            {
                                if (DebugConfig.DebugProbeScanner) Log.WriteLine("DirectSystemScanResult: ProbeScannerWindowScanResult.ID [" + ProbeScannerWindowScanResult.ID + "] matches Id [" + Id + "]");
                                if (!string.IsNullOrEmpty(ProbeScannerWindowScanResult.GroupName))
                                {
                                    if (DebugConfig.DebugProbeScanner) Log.WriteLine("DirectSystemScanResult: ProbeScannerWindowScanResult.GroupName [" + ProbeScannerWindowScanResult.GroupName + "]");
                                    _groupName = ProbeScannerWindowScanResult.GroupName;
                                    return _groupName;
                                }
                                else if (DebugConfig.DebugProbeScanner) Log.WriteLine("DirectSystemScanResult: ProbeScannerWindowScanResult.GroupName is null or empty");
                            }
                        }
                    }
                }

                return string.Empty;
            }
        }
        public double SignalStrength { get; internal set; }

        public string StrengthAttributeID { get; internal set; }
        public int TypeID { get; internal set; }

        #endregion Properties

        //public bool IsCircleResult { get; internal set; }

        #region Methods

        //def ACLBookmarkScanResult(self, locationID, name, comment, resultID, folderID, expiry, subfolderID = None):
        // BOOKMARK_EXPIRY_NONE = 0
        // BOOKMARK_EXPIRY_3HOURS = 1
        // BOOKMARK_EXPIRY_2DAYS = 2

        public bool BookmarkScanResult(string name, string folderName, string comment = "")
        {
            var folder = DirectEve.BookmarkFolders.FirstOrDefault(f => f.Name == folderName);

            if (folder == null)
            {
                DirectEve.Log($"Bookmarkfolder [{folderName}] not found.");
                return false;
            }

            return DirectEve.ThreadedLocalSvcCall("bookmarkSvc", "ACLBookmarkScanResult",
                DirectEve.Session.SolarSystemId.Value, name, comment, Id, folder.Id, 0);
        }

        public bool WarpTo()
        {
            if (SignalStrength == 1)
                return DirectEve.ThreadedLocalSvcCall("menu", "WarpToScanResult", Id);
            return false;
        }

        public bool WarpFleetTo()
        {
            if (SignalStrength == 1)
                return DirectEve.ThreadedLocalSvcCall("menu", "WarpFleetToScanResult", Id);
            return false;
        }

        public bool IgnoreResult()
        {
            return true;
            //return DirectEve.ThreadedLocalSvcCall("menu", "IngoreResult", Id);
            //return DirectEve.ThreadedLocalSvcCall("menu", "IgnoreResult", Id);
            //PyObject scanSvc = DirectEve.GetLocalSvc("scanSvc");
            //if (scanSvc.IsValid)
            //{
            //    //return DirectEve.ThreadedLocalSvcCall("scanSvc", "IgnoreResult", .....);
            //}
        }

        #endregion Methods
    }
}