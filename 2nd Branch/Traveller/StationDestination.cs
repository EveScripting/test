using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using System;
using System.Linq;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Traveller
{
    public class StationDestination2 : TravelerDestination
    {
        #region Fields

        private DateTime _nextStationAction;

        #endregion Fields

        #region Constructors

        public StationDestination2(long stationId)
        {
            DirectLocation station = ESCache.Instance.DirectEve.Navigation.GetLocation(stationId);
            if (station == null || !station.ItemId.HasValue || !station.SolarSystemId.HasValue)
            {
                Log.WriteLine("Invalid station id [" + stationId + "]");

                SolarSystemId = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                StationId = -1;
                StationName = "";
                return;
            }

            Log.WriteLine("StationDestination: Destination set to [" + station.Name + "]");

            StationId = stationId;
            StationName = station.Name;
            SolarSystemId = station.SolarSystemId.Value;
            //Logging.Log(station.SolarSystemId.Value + " " + stationId + " " + station.Name);
        }

        public StationDestination2(long solarSystemId, long stationId, string stationName)
        {
            Log.WriteLine("StationDestination: Destination set to [" + stationName + "]");
            //Logging.Log(solarSystemId + " " + stationId + " " + stationName);

            SolarSystemId = solarSystemId;
            StationId = stationId;
            StationName = stationName;
        }

        #endregion Constructors

        #region Properties

        public long StationId { get; set; }

        public string StationName { get; set; }

        #endregion Properties

        #region Methods

        public override bool PerformFinalDestinationTask()
        {
            return PerformFinalDestinationTask(StationId, StationName, ref _nextStationAction);
        }

        internal static bool PerformFinalDestinationTask(long stationId, string stationName, ref DateTime nextAction)
        {
            if (nextAction > DateTime.UtcNow)
                return false;

            NavigateOnGrid.StationIdToGoto = stationId;
            if (ESCache.Instance.InStation && ESCache.Instance.DirectEve.Session.StationId == stationId)
            {
                Log.WriteLine("Arrived in station");
                return true;
            }

            if (ESCache.Instance.InStation)
            {
                // We are in a station, but not the correct station!
                if (Time.Instance.NextUndockAction < DateTime.UtcNow)
                {
                    Log.WriteLine("We're docked in the wrong station, undocking");
                    if (Undock())
                    {
                        Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(4000, 6000));
                        nextAction = DateTime.UtcNow.AddSeconds(7);
                    }

                    return false;
                }

                // We are not there yet
                return false;
            }

            if (!ESCache.Instance.InSpace)
                return false;

            UndockAttempts = 0;

            EntityCache station = ESCache.Instance.EntityByName(stationName);
            if (station == null)
                return false;

            if (station.Distance <= (int)Distances.DockingRange)
            {
                if (station.Dock())
                {
                    Log.WriteLine("Dock at [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                    nextAction = DateTime.UtcNow.AddSeconds(15);
                }

                return false;
            }

            if (station.Distance < (int)Distances.WarptoDistance)
            {
                if (station.Approach())
                {
                    Log.WriteLine("Approaching [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                    nextAction = DateTime.UtcNow.AddSeconds(30);
                }

                return false;
            }

            EntityCache BigObject = ESCache.Instance.BigObjects.FirstOrDefault();
            NavigateOnGrid.AvoidBumpingThings(BigObject, "NavigateOnGrid: PerformFinalDestinationTask", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());

            if (BigObject != null && BigObject.Distance > 2000 || BigObject == null || !NavigateOnGrid.AvoidBumpingThingsBool())
            {
                if (ESCache.Instance.BookmarksThatContain(Settings.Instance.StationDockBookmarkPrefix).Any(bookmark => bookmark.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && bookmark.DistanceFromEntity(station._directEntity) != null && bookmark.DistanceFromEntity(station._directEntity) != null && bookmark.DistanceFromEntity(station._directEntity) < 5000))
                {
                    DirectBookmark stationDockBookmark = ESCache.Instance.BookmarksThatContain(Settings.Instance.StationDockBookmarkPrefix).FirstOrDefault(bookmark => bookmark.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && bookmark.DistanceFromEntity(station._directEntity) < 5000);
                    if (stationDockBookmark != null && stationDockBookmark.WarpTo())
                    {
                        Log.WriteLine("Warp to [" + stationDockBookmark.Title + "] bookmark for [" + station.Name + "][" +
                                      Math.Round(station.Distance / 1000 / 149598000, 2) + " AU away]");
                        return false;
                    }
                }

                if (station.WarpTo())
                    return false;
            }

            return false;
        }

        #endregion Methods
    }
}