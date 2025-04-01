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

using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectAgentMissionBookmark : DirectBookmark
    {
        #region Constructors

        internal DirectAgentMissionBookmark(DirectEve directEve, PyObject pyBookmark) : base(directEve, pyBookmark)
        {
            AgentId = (long?) pyBookmark.Attribute("agentID");
            IsDeadspace = (bool?) pyBookmark.Attribute("deadspace");
            Flag = (int?) pyBookmark.Attribute("flag");
            LocationNumber = (int?) pyBookmark.Attribute("locationNumber");
            LocationType = (string) pyBookmark.Attribute("locationType");
            Title = (string) pyBookmark.Attribute("hint");
            SolarSystemId = (long?) pyBookmark.Attribute("solarsystemID");
        }

        #endregion Constructors

        #region Fields

        public long? AgentId;
        public int? Flag;
        public bool? IsDeadspace;
        public int? LocationNumber;
        public string LocationType;

        public bool IsInStationWithAgent
        {
            get
            {
                if (AgentId == null) return false;
                DirectAgent tempAgent = DirectEve.GetAgentById((long) AgentId);
                if (tempAgent != null)
                {
                    if (tempAgent.SolarSystemId == SolarSystemId)
                    {
                        if ((IsDeadspace == null || !(bool) IsDeadspace) && IsInCurrentSystem && tempAgent.Station.Coordinates == Coordinates)
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public bool IsInStationWithMe
        {
            get
            {
                if (DirectEve.Session.SolarSystemId == SolarSystemId)
                {
                    if ((IsDeadspace == null || !(bool) IsDeadspace) && IsInCurrentSystem && DirectEve.Session.Station != null && DirectEve.Session.Station.Coordinates == Coordinates)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool isStationLocation => ItemId.HasValue && DirectEve.Stations.TryGetValue((int) ItemId.Value, out var _);

        public long? SolarSystemId { get; set; }

        public DirectStation Station
        {
            get
            {
                if (isStationLocation && ItemId.HasValue)
                {
                    DirectStation station = null;
                    DirectEve.Stations.TryGetValue((int) ItemId.Value, out station);
                    return station ?? null;
                }

                return null;
            }
        }

        #endregion Fields
    }
}