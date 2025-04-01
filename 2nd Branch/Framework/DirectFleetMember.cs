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
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectFleetMember : DirectObject
    {
        #region Constructors

        internal DirectFleetMember(DirectEve directEve, PyObject memberObject)
            : base(directEve)
        {
            CharacterId = (int) memberObject.Attribute("charID");
            SquadID = (long) memberObject.Attribute("squadID");
            WingID = (long) memberObject.Attribute("wingID");
            //Skills = new List<int>
            //{
            //    (int) memberObject.Attribute("skills").ToList()[0],
            //    (int) memberObject.Attribute("skills").ToList()[1],
            //    (int) memberObject.Attribute("skills").ToList()[2]
            //};

            if ((int) memberObject.Attribute("job") == (int) directEve.Const.FleetJobCreator)
                Job = JobRole.Boss;
            else
                Job = JobRole.RegularMember;

            if ((int) memberObject.Attribute("role") == (int) directEve.Const.FleetRoleLeader)
                Role = FleetRole.FleetCommander;
            else if ((int) memberObject.Attribute("role") == (int) directEve.Const.FleetRoleWingCmdr)
                Role = FleetRole.WingCommander;
            else if ((int) memberObject.Attribute("role") == (int) directEve.Const.FleetRoleSquadCmdr)
                Role = FleetRole.SquadCommander;
            else if ((int) memberObject.Attribute("role") == (int) directEve.Const.FleetRoleMember)
                Role = FleetRole.Member;

            ShipTypeID = (int?) memberObject.Attribute("shipTypeID");
            SolarSystemID = (int) memberObject.Attribute("solarSystemID");
        }

        #endregion Constructors

        #region Methods

        public bool WarpToMember(double distance = 0)
        {
            return DirectEve.ThreadedLocalSvcCall("menu", "WarpToMember", CharacterId, distance);
        }

        public bool WarpSquadToMember(double distance = 0)
        {
            return DirectEve.ThreadedLocalSvcCall("menu", "WarpSquadToMember", CharacterId, distance);
        }

        public bool WarpWingToMember(double distance = 0)
        {
            return DirectEve.ThreadedLocalSvcCall("menu", "WarpWingToMember", CharacterId, distance);
        }

        public bool WarpFleetToMember(double distance = 0)
        {
            return DirectEve.ThreadedLocalSvcCall("menu", "WarpFleetToMember", CharacterId, distance);
        }

        public bool AddToWatchlist()
        {
            if (!DirectEve.Session.IsInSpace && !DirectEve.Session.IsInDockableLocation)
            {
                //Log("if (!IsInSpace && !IsInDockableLocation)");
                return false;
            }

            if (!DirectEve.Session.InFleet)
            {
                //Log("FormNewFleet: Session.FleetId is [" + DirectEve.Session.FleetId + "]");
                return false;
            }

            PyObject fleetsvc = DirectEve.GetLocalSvc("fleet");
            if (fleetsvc == null || !fleetsvc.IsValid)
            {
                //Log("FormNewFleet: if (fleetsvc == null || !fleetsvc.IsValid)");
                return false;
            }

            if (DirectEve.ThreadedCall(fleetsvc.Attribute("AddFavorite"), CharacterId))
                return true;

            return false;
        }

        /**
        public bool SendDronesToAssist()
        {
            if (!DirectEve.Session.InFleet)
                return false;

            if (!DirectEve.ActiveDrones.Any())
                return false;

            List<long> droneIds = new List<long>();
            foreach (DirectEntity activeDrone in DirectEve.ActiveDrones)
            {
                droneIds.Add(activeDrone.Id);
            }

            PyObject AssistDrone = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.droneFunctions").Attribute("Assist");
            return DirectEve.ThreadedCall(AssistDrone, CharacterId, droneIds);
        }
        **/
        /**
        public bool SendDronesToGuard()
        {
            if (!DirectEve.Session.InFleet)
                return false;

            if (!DirectEve.ActiveDrones.Any())
                return false;

            List<long> droneIds = new List<long>();
            foreach (DirectEntity activeDrone in DirectEve.ActiveDrones)
            {
                droneIds.Add(activeDrone.Id);
            }

            PyObject GuardDrone = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.droneFunctions").Attribute("Guard");
            return DirectEve.ThreadedCall(GuardDrone, CharacterId, droneIds);
        }
        **/

        public DirectCharacter Character
        {
            get
            {
                try
                {
                    if (!DirectEve.IsInFleet)
                        return null;

                    if (DirectEve.ChatWindows.Any())
                    {
                        if (DirectEve.ChatWindows.Any(i => i.DisplayName == "Fleet"))
                        {
                            if (DirectEve.ChatWindows.FirstOrDefault(e => e.DisplayName == "Fleet").Members.Any())
                            {
                                foreach (var fleetChatMember in DirectEve.ChatWindows.FirstOrDefault(e => e.DisplayName == "Fleet").Members)
                                {
                                    if (fleetChatMember.CharacterId == CharacterId)
                                        return fleetChatMember;
                                }

                                return null;
                            }

                            return null;
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    //Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public bool ClearWatchlist()
        {
            if (!DirectEve.Session.IsInSpace && !DirectEve.Session.IsInDockableLocation)
            {
                //Log("if (!IsInSpace && !IsInDockableLocation)");
                return false;
            }

            if (!DirectEve.Session.InFleet)
            {
                //Log("FormNewFleet: Session.FleetId is [" + DirectEve.Session.FleetId + "]");
                return false;
            }

            PyObject fleetsvc = DirectEve.GetLocalSvc("fleet");
            if (fleetsvc == null || !fleetsvc.IsValid)
            {
                //Log("FormNewFleet: if (fleetsvc == null || !fleetsvc.IsValid)");
                return false;
            }

            if (DirectEve.ThreadedCall(fleetsvc.Attribute("RemoveAllFavorites"), DirectEve.Session.CharacterId))
                return true;

            return false;
        }

        #endregion Methods

        #region Enums

        public enum FleetRole
        {
            FleetCommander,
            WingCommander,
            SquadCommander,
            Member
        }

        public enum JobRole
        {
            Boss,
            RegularMember
        }

        #endregion Enums

        #region Properties

        public int CharacterId { get; internal set; }
        public JobRole Job { get; internal set; }
        public string Name => DirectEve.GetOwner(CharacterId).Name;
        public FleetRole Role { get; internal set; }
        public int? ShipTypeID { get; internal set; }
        public List<int> Skills { get; internal set; }
        public long SolarSystemID { get; internal set; }
        public long SquadID { get; internal set; }
        public long WingID { get; internal set; }

        // TODO: We need to check if the ship is actual boarded by a character too! (Done - isPlayer tells)
        public DirectEntity Entity => DirectEve.Entities.FirstOrDefault(e => e.OwnerId == this.CharacterId && e.IsPlayer);


        #endregion Properties
    }
}