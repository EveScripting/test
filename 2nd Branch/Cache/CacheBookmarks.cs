extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;

namespace EVESharpCore.Cache
{
    public sealed partial class ESCache
    {
        #region Fields

        private static DirectBookmark _undockBookmarkInLocal;
        private IEnumerable<DirectBookmark> _listOfUndockBookmarks;

        #endregion Fields

        #region Properties

        public IEnumerable<DirectBookmark> SafeSpotBookmarks
        {
            get
            {
                try
                {
                    if (_safeSpotBookmarks == null)
                        if (Instance.BookmarksByLabel(Settings.Instance.SafeSpotBookmarkPrefix).Count > 0)
                            _safeSpotBookmarks = Instance.BookmarksByLabel(Settings.Instance.SafeSpotBookmarkPrefix).Where(i => i.BookmarkType == BookmarkType.Coordinate).ToList();

                    if (_safeSpotBookmarks != null && _safeSpotBookmarks.Count > 0)
                        return _safeSpotBookmarks;

                    return new List<DirectBookmark>();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                }

                return new List<DirectBookmark>();
            }
        }

        public DirectBookmark UndockBookmark
        {
            get
            {
                try
                {
                    if (_undockBookmarkInLocal == null)
                    {
                        if (_listOfUndockBookmarks == null)
                            if (Settings.Instance.UndockBookmarkPrefix != "")
                                _listOfUndockBookmarks = Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix).Where(i => i.BookmarkType == BookmarkType.Coordinate);
                        if (_listOfUndockBookmarks != null && _listOfUndockBookmarks.Any())
                        {
                            _listOfUndockBookmarks = _listOfUndockBookmarks.Where(i => i.IsInCurrentSystem).ToList();
                            _undockBookmarkInLocal =
                                _listOfUndockBookmarks.OrderBy(i => Instance.DistanceFromMe(i.X ?? 0, i.Y ?? 0, i.Z ?? 0))
                                    .FirstOrDefault(b => Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distances.NextPocketDistance);
                            if (_undockBookmarkInLocal != null)
                                return _undockBookmarkInLocal;

                            return null;
                        }

                        return null;
                    }

                    return _undockBookmarkInLocal;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("[" + exception + "]");
                    return null;
                }
            }
            internal set => _undockBookmarkInLocal = value;
        }

        public bool IsPVPGankLikely
        {
            get
            {
                if (ESCache.Instance.InWarp) return false;
                if (ESCache.Instance.Stargates.Any(i => (double)Distances.JumpRange > i.Distance)) return false;
                if (ESCache.Instance.Stations.Any(i => 6000 > i.Distance)) return false;
                if (ESCache.Instance.EntitiesOnGrid.All(i => !i.IsPlayer)) return false;
                if (ESCache.Instance.InAbyssalDeadspace) return false; //really?!

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsPlayer && i.IsTargetedBy && i.IsAttacking)) return true; //what about fireworks?! 0 damage but shows as aggression, right?

                return false;
            }
        }

        public bool IsPVPGankHappening
        {
            get
            {
                if (IsPVPGankLikely && (100 > ESCache.Instance.ActiveShip.ShieldPercentage))
                    return true;

                return false;
            }
        }

        #endregion Properties

        #region Methods

        public DirectBookmark BookmarkById(long bookmarkId)
        {
            return Instance.DirectEve.Bookmarks.Find(b => b.BookmarkId == bookmarkId);
        }

        public List<DirectBookmark> BookmarksByLabel(string label)
        {
            try
            {
                //
                // does this work in w-space? .solarsystem might be null in that case
                //
                if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Count > 0)
                {
                    List<DirectBookmark> tempBookmarks = ESCache.Instance.CachedBookmarks.Where(b => !string.IsNullOrEmpty(label) &&  b.LocationId != null && !string.IsNullOrEmpty(b.Title) && b.Title.ToLower().StartsWith(label.ToLower())).ToList() ?? new List<DirectBookmark>();
                    if (tempBookmarks.Count > 0)
                    {
                        tempBookmarks = tempBookmarks.OrderBy(f => f.LocationId).ToList() ?? new List<DirectBookmark>();
                        tempBookmarks = tempBookmarks.OrderBy(f => f.LocationId).ThenBy(i => Instance.DistanceFromMe(i.X ?? 0, i.Y ?? 0, i.Z ?? 0)).ToList() ?? new List<DirectBookmark>();
                        return tempBookmarks;
                    }

                    return new List<DirectBookmark>();
                }

                return new List<DirectBookmark>();
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return new List<DirectBookmark>();
            }
        }

        public List<DirectBookmark> BookmarksThatContain(string label)
        {
            try
            {
                if (string.IsNullOrEmpty(label))
                {
                    Log.WriteLine("BookmarksThatContain: label was blank, how?");
                    return new List<DirectBookmark>();
                }

                if (ESCache.Instance.CachedBookmarks != null && (ESCache.Instance.CachedBookmarks.Any()))
                {
                    return ESCache.Instance.CachedBookmarks.Where(b => !string.IsNullOrEmpty(b.Title) && b.Title.ToLower().Contains(label.ToLower()))
                                .OrderBy(f => f.LocationId)
                                .ToList();
                }

                return new List<DirectBookmark>();
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return new List<DirectBookmark>();
            }
        }

        #endregion Methods
    }
}