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
using SC::SharedComponents.Py;
using System;
using System.Linq;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Framework
{
    public class DirectCharacter : DirectObject
    {
        #region Constructors

        internal DirectCharacter(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public long AllianceId { get; internal set; }
        public long CharacterId { get; internal set; }
        public long CorporationId { get; internal set; }
        public string Name => DirectEve.GetOwner(CharacterId).Name;
        public long WarFactionId { get; internal set; }

        public bool IsInFleetWithMe
        {
            get
            {
                if (DirectEve.Session.InFleet)
                {
                    int intFleetMember = 0;
                    foreach (DirectFleetMember FleetMember in DirectEve.GetFleetMembers)
                    {
                        intFleetMember++;
                        if (FleetMember.CharacterId == CharacterId)
                            return true;

                        if (DebugConfig.DebugFleetMgr) Log.WriteLine("[" + intFleetMember + "] FleetMember [" + FleetMember.Name + "] FleetMember.CharacterId [" + FleetMember.CharacterId + "] != DirectCharacter [" + Name + "] CharacterId [" + CharacterId + "]");
                    }


                    return false;
                }

                return false;
            }
        }

        private bool? _isInLocalWithMe = null;

        public bool IsInLocalWithMe
        {
            get
            {
                if (_isInLocalWithMe != null)
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (_isInLocalWithMe != null) return [" + _isInLocalWithMe + "];");
                    return _isInLocalWithMe ?? false;
                }

                var local = DirectEve.ChatWindows.FirstOrDefault(w => w.Name.StartsWith("chatchannel_local"));

                if (local == null)
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (local == null) return false;");
                    return false;
                }

                //if in wspace we cant see local members and thus have to assume they are in local! Can we ask the launcher?!
                if (DirectEve.Session.IsWspace)
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (DirectEve.Session.IsWspace) return true;");
                    return true;
                }

                if (local.Members != null && !local.Members.Any())
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (local.Members != null && local.Members.Any()) return false;");
                    return false;
                }

                if (local.Members.Any(i => i.CharacterId == CharacterId))
                {
                    _isInLocalWithMe = true;
                    return _isInLocalWithMe ?? true;
                }

                _isInLocalWithMe = false;
                return _isInLocalWithMe ?? false;
            }
        }


        private bool? _isInStationWithMe = null;

        public bool IsInStationWithMe
        {
            get
            {
                if (_isInStationWithMe != null)
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (_isInLocalWithMe != null) return [" + _isInStationWithMe + "];");
                    return _isInStationWithMe ?? false;
                }

                //if in space we cant see characters in station and thus have to assume they are not there?
                if (DirectEve.Session.IsInSpace)
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (DirectEve.Session.IsInSpace) return false;");
                    return false;
                }

                if (!DirectEve.Session.IsInDockableLocation)
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (!DirectEve.Session.IsInDockableLocation) return false;");
                    return false;
                }

                if (!DirectEve.GetStationGuests.Any())
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (!DirectEve.GetStationGuests.Any(i => i == CharacterId))");
                    return false;
                }

                if (!DirectEve.GetStationGuests.Any(i => i == CharacterId))
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (!DirectEve.GetStationGuests.Any(i => i == CharacterId))");
                    return false;
                }

                _isInStationWithMe = true;
                return _isInStationWithMe ?? true;
            }
        }

        #endregion Properties


        public bool InviteToFleet()
        {
            if (Time.Instance.LastFleetInvite.ContainsKey(CharacterId))
            {
                if (DateTime.UtcNow < Time.Instance.LastFleetInvite[CharacterId].AddSeconds(ESCache.Instance.RandomNumber(1, 3)))
                {
                    Log.WriteLine("Fleet Invite for [" + CharacterId + "] has been sent less than 4 minutes, skipping");
                    return false;
                }
            }

            if (Time.Instance.LastFleetMemberTimeStamp.ContainsKey(CharacterId))
            {
                if (DateTime.UtcNow < Time.Instance.LastFleetMemberTimeStamp[CharacterId].AddSeconds(3))
                {
                    Log.WriteLine("CharacterId [" + CharacterId + "] was just in the fleet less than 30 sec ago, waiting before re-inviting");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.LastFleetMemberTimeStamp[CharacterId].AddSeconds(ESCache.Instance.RandomNumber(20, 30)))
                {
                    Log.WriteLine("CharacterId [" + CharacterId + "] was just in the fleet less than 30 sec ago, waiting before re-inviting");
                    return false;
                }
            }

            Time.Instance.LastFleetInvite.AddOrUpdate(CharacterId, DateTime.UtcNow);
            //InviteToFleet == FormFleetWith Menu Option
            if (!DirectEve.Interval(4000, 6000, CharacterId.ToString()))
                return false;

            Log.WriteLine("InviteToFleet: Fleet Invite sent to [" + Name + "] CharacterId [" + CharacterId + "]");
            if (DirectEve.ThreadedLocalSvcCall("menu", "InviteToFleet", CharacterId))
                return true;

            return false;
        }
    }
}