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
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum BookmarkType
    {
        Station,
        Citadel,
        Solar_System,
        Coordinate,
    }

    public class DirectBookmark : DirectInvType
    {
        #region Fields

        /// <summary>
        ///     Entity cache
        /// </summary>
        private DirectEntity _entity;

        public const int BOOKMARK_EXPIRY_NONE = 0;
        public const int BOOKMARK_EXPIRY_3HOURS = 1;
        public const int BOOKMARK_EXPIRY_2DAYS = 2;

        #endregion Fields

        #region Constructors

        internal DirectBookmark(DirectEve directEve, PyObject pyBookmark)
            : base(directEve)
        {
            PyBookmark = pyBookmark;
            BookmarkId = (long?)pyBookmark.Attribute("bookmarkID");
            CreatedOn = (DateTime?)pyBookmark.Attribute("created");
            ItemId = (long?)pyBookmark.Attribute("itemID");
            LocationId = (long?)pyBookmark.Attribute("locationID");

            FolderId = (long?)pyBookmark.Attribute("folderID");
            Title = (string)pyBookmark.Attribute("memo");
            if (!string.IsNullOrEmpty(Title) && Title.Contains("\t"))
            {
                Memo = Title.Substring(Title.IndexOf("\t") + 1);
                Title = Title.Substring(0, Title.IndexOf("\t"));
            }
            Note = (string)pyBookmark.Attribute("note");
            OwnerId = (int?)pyBookmark.Attribute("ownerID");
            TypeId = (int)pyBookmark.Attribute("typeID");
            X = (double?)pyBookmark.Attribute("x");
            Y = (double?)pyBookmark.Attribute("y");
            Z = (double?)pyBookmark.Attribute("z");

            if (Enum.TryParse<BookmarkType>(GroupName.Replace(" ", "_"), out var result))
            {
                BookmarkType = result;
                if (BookmarkType != BookmarkType.Citadel && BookmarkType != BookmarkType.Station && X.HasValue && Y.HasValue && Z.HasValue)
                    BookmarkType = BookmarkType.Coordinate;
            }
        }

        #endregion Constructors

        #region Properties

        public long? BookmarkId { get; internal set; }
        public BookmarkType BookmarkType { get; }
        public DateTime? CreatedOn { get; internal set; }

        public Vec3 Coordinates
        {
            get
            {
                if (X == null || Y == null || Z == null) return new Vec3(0, 0, 0);
                return new Vec3((double)X, (double)Y, (double)Z);
            }
        }

        public bool DockedAtBookmark()
        {
            if ((BookmarkType == BookmarkType.Station || BookmarkType == BookmarkType.Citadel)
                && ItemId.HasValue
                && (ItemId == ESCache.Instance.DirectEve.Session.StationId
                    || ItemId == ESCache.Instance.DirectEve.Session.Structureid))
            {
                return true;
            }
            return false;
        }

        public DateTime ThisDirectBookmarkInstanceDate = DateTime.UtcNow;

        public bool IsOnGridWithMe
        {
            get
            {
                if ((double)Distances.OnGridWithMe > Distance)
                {
                    return true;
                }

                return false;
            }
        }

        public double? Distance
        {
            get
            {
                try
                {
                    if (DirectEve.Session.IsInSpace)
                    {
                        double? deltaX = X - DirectEve.ActiveShip.Entity.XCoordinate;
                        double? deltaY = Y - DirectEve.ActiveShip.Entity.YCoordinate;
                        double? deltaZ = Z - DirectEve.ActiveShip.Entity.ZCoordinate;
                        if (deltaX != null && deltaY != null && deltaZ != null)
                            return Math.Sqrt(((double)deltaX * (double)deltaX) + ((double)deltaY * (double)deltaY) + ((double)deltaZ * (double)deltaZ));

                        return null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return 0;
                }
            }
        }

        public double? DistanceInAU
        {
            get
            {
                if (Distance == null)
                    return null;

                return (double)Distance.Value / (double)Distances.OneAu;
            }
        }

        private List<DirectEntity> _celestials = new List<DirectEntity>();

        public List<DirectEntity> Celestials
        {
            get
            {
                //if (_celestials != null)
                //{
                //    return _celestials;
                //}

                _celestials = DirectEve.Entities.Where(i => i.IsCelestial).ToList();
                if (_celestials == null)
                {
                    if (DirectEve.Session.IsKSpace) Logging.Log.WriteLine("Celestials returned null");
                    return new List<DirectEntity>();
                }

                if (DirectEve.Session.IsKSpace && !_celestials.Any()) Logging.Log.WriteLine("Celestials returned none? How?");
                return _celestials;
            }
        }

        private DirectEntity _closestCelestial = null;

        public DirectEntity ClosestCelestial
        {
            get
            {
                if (Celestials.Count == 0)
                {
                    if (DirectEve.Session.IsKSpace) Logging.Log.WriteLine("ClosestPlanet: if (Planets.Count == 0)");
                    return null;
                }

                if (Celestials.Count > 0)
                    _closestCelestial = Celestials.Where(x => x.Distance != null && x.Distance != 0).OrderBy(i => i.Distance).FirstOrDefault();

                if (DirectEve.Session.IsKSpace && _closestCelestial == null) Logging.Log.WriteLine("ClosestPlanet: returned null!");
                return _closestCelestial;
            }
        }

        /// <summary>
        ///     The entity associated with this bookmark
        /// </summary>
        /// <remarks>
        ///     This property will be null if no entity can be found
        /// </remarks>
        public DirectEntity Entity => _entity ?? (_entity = DirectEve.GetEntityById(ItemId ?? -1));

        public long? FolderId { get; internal set; }

        public bool IsInCurrentSystem
        {
            get
            {
                //
                // this is necessary to avoid a crash, we dont use this feature (yet) in wspace anyway; if we need it we will nee to solve the other bug that was causing eve to freeze
                //
                if (DirectEve.Session.IsWspace)
                {
                    if (Distance != null)
                    {
                        if ((long)Distances.FourHundredAu > Distance)
                        {
                            return true;
                        }
                    }

                    if (DirectEve.Session.SolarSystemId == null)
                        return true;

                    if (DirectEve.Session.SolarSystemId == SolarSystemId)
                        return true;

                    return false;
                }

                if (SolarSystem == null)
                    return false;

                return ItemId == DirectEve.Session.LocationId || LocationId == DirectEve.Session.SolarSystemId || ItemId == DirectEve.Session.SolarSystemId || (!ESCache.Instance.InWormHoleSpace && SolarSystem.Id == DirectEve.Session.SolarSystem.Id);
            }
        }

        /// <summary>
        ///     If this is a bookmark of a station, this is the StationId
        /// </summary>
        public long? ItemId { get; internal set; }

        /// <summary>
        ///     Matches SolarSystemId
        /// </summary>
        public long? LocationId { get; internal set; }

        public string Memo { get; internal set; }

        public string Note { get; internal set; }

        public int? OwnerId { get; internal set; }

        public DirectSolarSystem SolarSystem
        {
            get
            {
                try
                {
                    if (SolarSystemId != null)
                    {
                        if (DirectEve.Session.IsWspace)
                            return null;

                        DirectSolarSystem tempSolarsystem = DirectEve.SolarSystems[(int)SolarSystemId];
                        if (tempSolarsystem != null)
                            return tempSolarsystem;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public long? StationId
        {
            get
            {
                if (LocationId > 60000000 && LocationId < 64000000)
                    return LocationId;

                return null;
            }
        }

        public string Title { get; internal set; }

        public double? X { get; internal set; }

        public double? Y { get; internal set; }

        public double? Z { get; internal set; }
        public Vec3 Pos => new Vec3(X ?? 0, Y ?? 0, Z ?? 0);
        public DirectWorldPosition WorldPosition => new DirectWorldPosition(X ?? 0, Y ?? 0, Z ?? 0);
        internal PyObject PyBookmark { get; set; }

        public bool IsKSpace
        {
            get
            {
                //
                //https://gist.github.com/a-tal/5ff5199fdbeb745b77cb633b7f4400bb
                //32,000,000 	33,000,000 	Abyssal systems
                //
                if (SolarSystemId >= 30000000 && SolarSystemId <= 30999999)
                {
                    if (DirectEve.Interval(30000, 30000, ItemId.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), false);
                    return true;
                }

                return false;
            }
        }

        public bool IsWSpace
        {
            get
            {
                //
                //https://gist.github.com/a-tal/5ff5199fdbeb745b77cb633b7f4400bb
                //32,000,000 	33,000,000 	Abyssal systems
                //
                if (SolarSystemId >= 31000000 && SolarSystemId <= 31999999)
                {
                    if (DirectEve.Interval(30000, 30000, ItemId.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), false);
                    return true;
                }

                return false;
            }
        }

        public bool IsAbyssalSpace
        {
            get
            {
                //
                //https://gist.github.com/a-tal/5ff5199fdbeb745b77cb633b7f4400bb
                //32,000,000 	33,000,000 	Abyssal systems
                //
                if (SolarSystemId >= 32000000 && SolarSystemId <= 32999999)
                {
                    if (DirectEve.Interval(30000, 30000, ItemId.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), false);
                    return true;
                }

                return false;
            }
        }

        public bool IsVoidSystem
        {
            get
            {
                //
                //https://gist.github.com/a-tal/5ff5199fdbeb745b77cb633b7f4400bb
                //32,000,000 	33,000,000 	Abyssal systems
                //
                if (SolarSystemId >= 34000000 && SolarSystemId <= 34999999)
                {
                    if (DirectEve.Interval(30000, 30000, ItemId.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), false);
                    return true;
                }

                return false;
            }
        }

        private long? SolarSystemId
        {
            get
            {
                try
                {
                    //if (DirectEve.Session.IsWspace)
                    //    return null;

                    if (LocationId > 30000000 && LocationId < 33000000)
                        return LocationId;

                    //DirectEve.Log("Bookmark named [" + Title + "] returned locationID [" + LocationId + "]");
                    return null;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        #endregion Properties

        #region Methods

        public DateTime NextApproachAction { get; set; }

        public DateTime NextWarpAction { get; set; }

        public bool Approach()
        {

        	if (!IsInCurrentSystem)
            {
                DirectEve.Log("ERROR: The bookmark is not in the current system!");
                return false;
            }

            if (DirectEve.ActiveShip.IsImmobile)
                return false;

            if (DateTime.UtcNow > NextApproachAction)
            {
                NextApproachAction = DateTime.UtcNow.AddSeconds(15);
                PyObject approachLocation = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions").Attribute("ApproachLocation");
                return DirectEve.ThreadedCall(approachLocation, PyBookmark);
            }

            return false;
        }

        public bool Delete()
        {
            if (!BookmarkId.HasValue)
                return false;

            return DirectEve.ThreadedLocalSvcCall("addressbook", "DeleteBookmarks", new List<PyObject> { PyBookmark.Attribute("bookmarkID") });
        }

        public double? DistanceFromEntity(DirectEntity otherEntityToMeasureFrom)
        {
            try
            {
                if (otherEntityToMeasureFrom == null)
                    return null;

                if (X == null || Y == null || Z == null)
                    return null;

                if (X == 0 || Y == 0 || Z == 0)
                    return null;

                if (otherEntityToMeasureFrom.XCoordinate == 0 || otherEntityToMeasureFrom.YCoordinate == 0 || otherEntityToMeasureFrom.ZCoordinate == 0)
                    return null;

                double deltaX = (double)X - otherEntityToMeasureFrom.DirectAbsolutePosition.XCoordinate;
                double deltaY = (double)Y - otherEntityToMeasureFrom.DirectAbsolutePosition.YCoordinate;
                double deltaZ = (double)Z - otherEntityToMeasureFrom.DirectAbsolutePosition.ZCoordinate;

                return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ));
            }
            catch (Exception ex)
            {
                DirectEve.Log("Exception [" + ex + "]");
                return 0;
            }
        }

        public double DistanceTo(DirectStation station)
        {
            if (BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;

            return Math.Round(Math.Sqrt(((station.X - X.Value) * (station.X - X.Value)) + ((station.Y - Y.Value) * (station.Y - Y.Value)) + ((station.Z - Z.Value) * (station.Z - Z.Value))), 2);
        }

        public double? DistanceTo(DirectEntity entity)
        {
            if (BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;

            if (entity.DirectAbsolutePosition == null)
                return null;

            return Math.Round(Math.Sqrt((double)(((entity.DirectAbsolutePosition.XCoordinate - X.Value) * (entity.DirectAbsolutePosition.XCoordinate - X.Value)) + ((entity.YCoordinate - Y.Value) * (entity.YCoordinate - Y.Value)) + ((entity.ZCoordinate - Z.Value) * (entity.DirectAbsolutePosition.ZCoordinate - Z.Value)))), 2);
        }

        public double DistanceTo(Vec3 vec3)
        {
            if (this.BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;

            return Math.Round(Math.Sqrt((vec3.X - X.Value) * (vec3.X - X.Value) + (vec3.Y - Y.Value) * (vec3.Y - Y.Value) + (vec3.Z - Z.Value) * (vec3.Z - Z.Value)), 2);
        }

        public double DistanceTo(DirectBookmark entity)
        {
            if (this.BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;

            if (entity.BookmarkType != BookmarkType.Coordinate)
                return double.MaxValue;

            return Math.Round(Math.Sqrt((double)((entity.X - X.Value) * (entity.X - X.Value) + (entity.Y - Y.Value) * (entity.Y - Y.Value) + (entity.Z - Z.Value) * (entity.Z - Z.Value))), 2);
        }

        public void CheckForDamageAndSetNeedsRepair()
        {
            foreach (DirectUIModule module in DirectEve.Modules)
            {
                if (module.HeatDamagePercent > 0)
                {
                    ESCache.Instance.NeedRepair = true;
                }
            }

            if (DirectEve.ActiveShip.ArmorPercentage < 100)
            {
                ESCache.Instance.NeedRepair = true;
            }

            if (DirectEve.ActiveShip.StructurePercentage < 100)
            {
                ESCache.Instance.NeedRepair = true;
            }

            return;
        }
        public bool WarpTo()
        {
            try
            {
                //if (DateTime.UtcNow < LastInWarp.AddSeconds(2))
                //    return false;

                if (DateTime.UtcNow > NextWarpAction)
                {
                    if (DirectEve.Session.IsInSpace)
                    {
                        if (Distance == null)
                        {
                            DirectEve.Log("WarpTo: Distance == null: Bad Bookmark?! Bookmark [" + Title + "]");
                            return false;
                        }

                        if (DirectEve.Session.IsWspace)
                        {
                            if (DirectEve.Session.SolarSystemId != SolarSystemId)
                            {
                                DirectEve.Log("You can only warp to bookmarks that are in system with you! [" + Title + "]");
                                return false;
                            }
                        }
                        else if (DirectEve.Session.SolarSystem != null && DirectEve.Session.SolarSystem.Id != SolarSystem.Id)
                        {
                            DirectEve.Log("You can only warp to bookmarks that are in system with you! [" + Title + "] is in [" + SolarSystem.Name + "] and you are in [" + DirectEve.Session.SolarSystem.Name + "]");
                            return false;
                        }

                        if (DirectEve.Entities.Any(i => (double)Distances.OnGridWithMe > i.Distance && (i.IsStation || i.IsCitadel)))
                        {
                            CheckForDamageAndSetNeedsRepair();

                            if (DirectEve.Modules.Any(i => !i.IsOnline) && !ESCache.Instance.Paused)
                            {
                                DirectEve.Log("We attempted to warp away from a station with an offline module: docking instead!");
                                //MissionSettings.OfflineModulesFound = true;
                                DirectEve.Entities.Where(i => (double)Distances.OnGridWithMe > i.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.IsStation || i.IsCitadel).Dock();
                                return false;
                            }
                            else Cache.MissionSettings.OfflineModulesFound = false;

                            if (DirectEve.Modules.Any(i => i.HeatDamagePercent > 0) && !ESCache.Instance.Paused)
                            {
                                ESCache.Instance.NeedRepair = true;
                                DirectEve.Log("We attempted to warp away from a station with a damaged module: docking instead!");
                                //MissionSettings.DamagedModulesFound = true;
                                DirectEve.Entities.Where(i => (double)Distances.OnGridWithMe > i.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.IsStation || i.IsCitadel).Dock();
                                return false;
                            }

                            if (!DirectEve.Modules.Any() && ESCache.Instance.ActiveShip.GivenName == Combat.CombatShipName && !ESCache.Instance.Paused)
                            {
                                ESCache.Instance.NeedRepair = true;
                                DirectEve.Log("We attempted to warp away from a station with nothing fitted?: docking instead! PauseAfterNextDock [true]");
                                ESCache.Instance.PauseAfterNextDock = true;
                                DirectEve.Entities.Where(i => (double)Distances.OnGridWithMe > i.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.IsStation || i.IsCitadel).Dock();
                                return false;
                            }

                            if (!ESCache.Instance.Weapons.Any() && ESCache.Instance.ActiveShip.GivenName == Combat.CombatShipName && !ESCache.Instance.Paused)
                            {
                                ESCache.Instance.NeedRepair = true;
                                DirectEve.Log("We attempted to warp away from a station with no weapons fitted?: docking instead! PauseAfterNextDock [true]");
                                ESCache.Instance.PauseAfterNextDock = true;
                                DirectEve.Entities.Where(i => (double)Distances.OnGridWithMe > i.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.IsStation || i.IsCitadel).Dock();
                                return false;
                            }
                            //else MissionSettings.DamagedModulesFound = false;
                        }

                        if (Distance != null && Distance < (long)Distances.HalfOfALightYearInAu)
                        {
                            if (Distance != null && Distance > (int)Distances.WarptoDistance)
                            {
                                if (WarpTo(0))
                                {
                                    Logging.Log.WriteLine("Warping to bookmark [" + Title + "] at Distance [" + Math.Round((double)Distance / 1000, 0) + "k]");
                                    NextWarpAction = DateTime.UtcNow.AddSeconds(4);
                                    if (DirectEve.Session.SolarSystemId != null)
                                        ESCache.Instance.TaskSetEveAccountAttribute("SolarSystem", DirectEve.GetLocationName((long)DirectEve.Session.SolarSystemId));

                                    if (DirectEve.ActiveShip != null)
                                        ESCache.Instance.TaskSetEveAccountAttribute("ShipType", DirectEve.ActiveShip.TypeName);

                                    return true;
                                }

                                return false;
                            }

                            DirectEve.Log("[" + Title + "] at Distance [" + Math.Round((double)Distance / 1000, 0) +
                                          "k] is not greater then 150k away, WarpTo aborted!!");
                            return false;
                        }

                        DirectEve.Log("[" + Title + "] Distance [" + Math.Round((double)Distance / 1000, 0) +
                                      "k] was greater than 5000AU away, we assume this an error!, WarpTo aborted!");
                        return false;
                    }

                    DirectEve.Log("We have not yet been in space at least 2 seconds, waiting!.!");
                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                DirectEve.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public bool WarpTo(double distance)
        {
            if (DirectEve.Interval(4000, 5000))
            {
                PyObject warpToBookmark = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions").Attribute("WarpToBookmark");
                return DirectEve.ThreadedCall(warpToBookmark, PyBookmark, distance);
            }
            return false;
        }
        //def ACLBookmarkLocation(self, itemID, folderID, name, comment, itemTypeID, expiry, subfolderID = None):
        internal static bool BookmarkLocation(DirectEve directEve, long itemId, long folderId, string name, int typeId, int expiry = 0, string comment = "")
        {
            if (Time.Instance.LastBookmarkAction.AddSeconds(10) > DateTime.UtcNow)
                return false;

            if (expiry < 0 || expiry > 2)
                return false;

            var folders = GetFolders(directEve);

            if (!folders.Where(f => f.IsActive).Any(f => f.Id.Equals(folderId)))
                return false;

            var bookmarkLocation = directEve.GetLocalSvc("bookmarkSvc").Attribute("ACLBookmarkLocation");
            if (directEve.ThreadedCall(bookmarkLocation, itemId, folderId, name, comment, typeId, expiry))
            {
                Time.Instance.LastBookmarkAction = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        //CreateBookmarkFolder(self, isPersonal, folderName, description, adminGroupID, manageGroupID, useGroupID, viewGroupID)
        // self.CreateBookmarkFolder(True, name, '', None, None, None, None)
        internal static bool CreatePersonalBookmarkFolder(DirectEve directEve, string name, string description = "")
        {
            return directEve.ThreadedLocalSvcCall("bookmarkSvc", "CreateBookmarkFolder", true, name, description, PySharp.PyNone, PySharp.PyNone, PySharp.PyNone, PySharp.PyNone);
        }

        internal static List<DirectBookmark> GetBookmarks(DirectEve directEve)
        {
            //
            // If you query this too many times (how many?) it will cause odd behavior (crash?)
            //
            // List the bookmarks from cache
            Dictionary<long, PyObject> bookmarks = directEve.GetLocalSvc("bookmarkSvc").Attribute("bookmarkCache").ToDictionary<long>();
            return bookmarks.Values.Select(pyBookmark => new DirectBookmark(directEve, pyBookmark)).ToList();
        }

        internal static List<DirectBookmarkFolder> GetFolders(DirectEve directEve)
        {
            // List the bookmark folders from cache
            var folders = directEve.GetLocalSvc("bookmarkSvc").Attribute("foldersNew").ToDictionary<long>();
            return folders.Values.Select(pyFolder => new DirectBookmarkFolder(directEve, pyFolder)).ToList();
        }

        internal static DateTime? GetLastBookmarksUpdate(DirectEve directEve)
        {
            // Get the bookmark-last-update-time
            return (DateTime?)directEve.GetLocalSvc("bookmarkSvc").Attribute("lastUpdateTime");
        }

        internal static bool RefreshBookmarks(DirectEve directEve)
        {
            // If the bookmarks need to be refreshed, then this will do it
            return directEve.ThreadedLocalSvcCall("bookmarkSvc", "GetBookmarks");
        }

        //internal static bool RefreshPnPWindow(DirectEve directEve)
        //{
        //    return directEve.ThreadedLocalSvcCall("bookmarkSvc", "RefreshWindow");
        //    ;
        //}

        #endregion Methods
    }
}