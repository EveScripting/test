// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System;
using System.Xml.Linq;
using EVESharpCore.Logging;

namespace EVESharpCore.Lookup
{
    public class FactionFitting
    {
        #region Constructors

        public FactionFitting()
        {
        }

        public FactionFitting(XElement factionfitting)
        {
            try
            {
                FactionName = (string)factionfitting.Attribute("faction") ?? "";
                FittingName = (string)factionfitting.Attribute("fitting") ?? "default";
                DroneTypeID = (int?)factionfitting.Attribute("dronetype") ??
                              (int?)factionfitting.Attribute("drone") ??
                              (int?)factionfitting.Attribute("dronetype");
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception: [" + exception + "]");
            }
        }

        #endregion Constructors

        #region Properties

        public int? DroneTypeID { get; }
        public string FactionName { get; }
        public string FittingName { get; }

        #endregion Properties
    }

    public class MissionFitting
    {
        #region Constructors

        public MissionFitting()
        {
        }

        public MissionFitting(XElement missionfitting)
        {
            try
            {
                MissionName = (string)missionfitting.Attribute("mission") ?? "";
                FactionName = (string)missionfitting.Attribute("faction") ?? "Default";
                FittingName = (string)missionfitting.Attribute("fitting") ?? "";
                Ship = (string)missionfitting.Attribute("ship") ?? (string)missionfitting.Attribute("shipName") ?? "";
                DroneTypeID = (int?)missionfitting.Attribute("droneTypeID") ??
                              (int?)missionfitting.Attribute("drone") ??
                              (int?)missionfitting.Attribute("dronetype");
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception: [" + exception + "]");
            }
        }

        #endregion Constructors

        #region Properties

        public int? DroneTypeID { get; }
        public string FactionName { get; }
        public string FittingName { get; }
        public string MissionName { get; }
        public string Ship { get; }

        #endregion Properties
    }
}