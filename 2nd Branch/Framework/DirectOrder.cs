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
using System;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectOrder : DirectObject
    {
        #region Fields

        internal PyObject PyOrder;

        #endregion Fields

        #region Constructors

        //https://wiki.eveuniversity.org/Trading
        internal DirectOrder(DirectEve directEve, PyObject pyOrder) : base(directEve)
        {

            //Debug
            if (false)
            {
                if (DirectEve.Interval(10000))
                {
                    Console.WriteLine($"{pyOrder.LogObject()}");
                }
            }
            // __columns__ : ['price', 'volRemaining', 'typeID', 'range', 'orderID', 'volEntered', 'minVolume', 'bid', 'issueDate', 'duration', 'stationID', 'regionID', 'solarSystemID', 'jumps']
            PyOrder = pyOrder;
            OrderId = -1;
            /// ConstellationId = (int)pyOrder.Attribute("constellationID");
            Price = (double) pyOrder.Attribute("price");
            VolumeRemaining = (int) pyOrder.Attribute("volRemaining");
            TypeId = (int) pyOrder.Attribute("typeID");
            InvType = DirectEve.GetInvType(TypeId);
            TypeName = InvType.TypeName;
            if ((int)pyOrder.Attribute("range") == (int)DirectEve.Const.RangeSolarSystem)
                Range = DirectOrderRange.SolarSystem;
            else if ((int)pyOrder.Attribute("range") == (int)DirectEve.Const.RangeConstellation)
                Range = DirectOrderRange.Constellation;
            else if ((int)pyOrder.Attribute("range") == (int)DirectEve.Const.RangeRegion)
                Range = DirectOrderRange.Region;
            else if ((int)pyOrder.Attribute("range") == (int)DirectEve.Const.RangeStation)
                Range = DirectOrderRange.Station;
            else
                RangeAbsolute = (int)pyOrder.Attribute("range");

            OrderId = (long)pyOrder.Attribute("orderID");
            VolumeEntered = (int)pyOrder.Attribute("volEntered");
            MinimumVolume = (int)pyOrder.Attribute("minVolume");
            IsBid = (bool)pyOrder.Attribute("bid");
            IssuedOn = (DateTime)pyOrder.Attribute("issued");
            Duration = (int)pyOrder.Attribute("duration");
            StationId = (int)pyOrder.Attribute("stationID");
            RegionId = (int)pyOrder.Attribute("regionID");
            SolarSystemId = (int)pyOrder.Attribute("solarSystemID");
            Jumps = (int)pyOrder.Attribute("jumps");
        }

        #endregion Constructors

        #region Properties

        public int Duration { get; set; }
        public DirectInvType InvType { get; set; }
        public bool IsBid { get; set; }

        public bool IsInRangeOfMe
        {
            get
            {
                if (SolarSystem == null)
                    return false;

                if (SolarSystemId != DirectEve.Session.SolarSystemId && RangeAbsolute > 0)
                {
                    DirectSolarSystem fromSystem = DirectEve.SolarSystems.Values.FirstOrDefault(k => k.Id.Equals(DirectEve.Session.SolarSystemId));
                    int jumps = fromSystem.CalculatePathTo(SolarSystem, null, false, false).Item1.Count;
                    if (RangeAbsolute > jumps)
                        return false;

                    return true;
                }

                switch (Range)
                {
                    case DirectOrderRange.Constellation:
                        if (SolarSystem.ConstellationId == DirectEve.Session.ConstellationId)
                            return true;

                        return false;

                    case DirectOrderRange.Region:
                        if (RegionId == DirectEve.Session.RegionId)
                            return true;

                        return false;

                    case DirectOrderRange.SolarSystem:
                        if (SolarSystemId == DirectEve.Session.SolarSystemId)
                            return true;

                        return false;

                    case DirectOrderRange.Station:
                        if (StationId == DirectEve.Session.StationId)
                            return true;

                        return false;
                }

                return false;
            }
        }

        public DateTime IssuedOn { get; set; }
        public int Jumps { get; set; }
        public int MinimumVolume { get; set; }
        public long OrderId { get; set; }
        public double Price { get; set; }
        public DirectOrderRange Range { get; set; }
        public int RangeAbsolute { get; set; }
        public int RegionId { get; set; }

        public DirectSolarSystem SolarSystem
        {
            get
            {
                if (SolarSystemId != 0)
                {
                    DirectSolarSystem _solarsystem = DirectEve.SolarSystems.FirstOrDefault(i => i.Key == SolarSystemId).Value;
                    return _solarsystem;
                }

                return null;
            }
        }

        public int SolarSystemId { get; set; }
        public int StationId { get; set; }
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public int VolumeEntered { get; set; }
        public int VolumeRemaining { get; set; }

        public bool IsInCitadel
        {
            get
            {
                //
                //https://gist.github.com/a-tal/5ff5199fdbeb745b77cb633b7f4400bb
                // 60,000,000 	61,000,000 	Stations created by CCP
                // 61,000,000 	64,000,000 	Stations created from outposts
                //
                if (StationId >= 60000000 && StationId <= 64000000)
                    return false;

                return true;
            }
        }

        //NPC Station
        //Broker's fee percentage = 3% − (0.3% × Broker Relations level) − (0.03% × faction standing) − (0.02% × corporation standing)
        //Upwell Structure
        //Broker's fee percentage = 0.5% + Owner %
        public long BrokersFee
        {
            get
            {
                return 100;
            }
        }

        public long RelistFee
        {
            get
            {
                return 0;
            }
        }

        #endregion Properties

        #region Methods

        public bool Buy(int quantity, DirectOrderRange range)
        {
            PyObject pyRange = DirectEve.GetRange(range);
            return DirectEve.ThreadedLocalSvcCall("marketQuote", "BuyStuff", StationId, TypeId, Price, quantity, pyRange);
        }

        // def CancelOrder(self, orderID, regionID):
        public bool CancelOrder()
        {
            if (OrderId == -1 || !PyOrder.IsValid)
            {
                DirectEve.Log("Trying to cancel a invalid order");
                return false;
            }

            return DirectEve.ThreadedLocalSvcCall("marketQuote", "CancelOrder", OrderId, RegionId);
        }

        //def ModifyOrder(self, order, newPrice):
        public bool ModifyOrder(double newPrice)
        {

            if (OrderId == -1 || !PyOrder.IsValid)
            {
                DirectEve.Log("Trying to modify a invalid order");
                return false;
            }

            return DirectEve.ThreadedLocalSvcCall("marketQuote", "ModifyOrder", PyOrder, newPrice);
        }

        public override String ToString()
        {
            return String.Format("OrderId: {0}, Price: {1}, VolumeRemaining: {2}, TypeId: {3}, Range: {4}, VolumeEntered: {5}, MinimumVolume: {6}, IsBid: {7}, IssuedOn: {8}, Duration: {9}, StationId: {10}, RegionId: {11}, SolarSystemId: {12}, Jumps: {13}",
                OrderId, Price, VolumeRemaining, TypeId, Range, VolumeEntered, MinimumVolume, IsBid, IssuedOn, Duration, StationId, RegionId, SolarSystemId, Jumps);
        }

        #endregion Methods
    }
}