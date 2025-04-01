// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;

namespace EVESharpCore.Traveller
{
    public class SolarSystemDestination2 : TravelerDestination
    {
        #region Fields

        private DateTime _nextAction;

        #endregion Fields

        #region Constructors

        public SolarSystemDestination2(long solarSystemId)
        {
            Log.WriteLine("Destination set to solar system id [" + solarSystemId + "]");
            SolarSystemId = solarSystemId;
        }

        #endregion Constructors

        #region Methods

        public override bool PerformFinalDestinationTask()
        {
            // The destination is the solar system, not the station in the solar system.
            if (ESCache.Instance.InStation)
            {
                if (_nextAction < DateTime.UtcNow)
                    if (Undock())
                    {
                        Log.WriteLine("Exiting station!");
                        Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(4000, 6000));
                        _nextAction = DateTime.UtcNow.AddSeconds(10);
                        return false;
                    }

                // We are not there yet
                return false;
            }

            UndockAttempts = 0;

            // The task was to get to the solar system, we are there :)
            Log.WriteLine("Arrived in system");
            return true;
        }

        #endregion Methods
    }
}