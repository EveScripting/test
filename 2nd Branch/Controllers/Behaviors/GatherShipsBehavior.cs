extern alias SC;

using EVESharpCore.Logging;
using System;
using EVESharpCore.Questor.States;
using EVESharpCore.Framework;
using EVESharpCore.Cache;
using System.Linq;
using SC::SharedComponents.EVE;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using System.ServiceModel.Security;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Traveller;
using System.Runtime.Remoting.Messaging;
using System.Windows.Controls;

namespace EVESharpCore.Questor.Behaviors
{
    public class GatherShipsBehavior
    {
        #region Constructors

        public GatherShipsBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields
        #endregion Fields

        #region Methods

        public static bool ChangeGatherShipsBehaviorState(GatherShipsBehaviorState _StateToSet, bool wait = true, string LogMessage = null)
        {
            try
            {
                if (State.CurrentGatherShipsBehaviorState != _StateToSet)
                {
                    if (_StateToSet == GatherShipsBehaviorState.TravelToMarketSystem)
                        State.CurrentTravelerState = TravelerState.Idle;

                    if (_StateToSet == GatherShipsBehaviorState.TravelToToLocation)
                        State.CurrentTravelerState = TravelerState.Idle;

                    Log.WriteLine("New GatherShipsBehaviorState [" + _StateToSet + "]");
                    State.CurrentGatherShipsBehaviorState = _StateToSet;
                    if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void temp()
        {
            try
            {
                if (DirectEve.Interval(20000))
                {
                    Log.WriteLine("GatherShipsBehavior.ProcessState: [" + State.CurrentCombatMissionBehaviorState + "]");

                    if (ESCache.Instance.DirectEve.GetAssets().Any())
                    {
                        var CachedAssets = ESCache.Instance.DirectEve.GetAssets();
                        Log.WriteLine("Assets[" + CachedAssets.Count() + "]: results limited to 100 so we dont fill the log");
                        foreach (var item in CachedAssets.Take(100).OrderBy(i => i.SolarSystemName))
                        {
                            try
                            {
                                Log.WriteLine("  " + item.TypeName + " [" + item.Quantity + "] IsSingleton [" + item.IsSingleton + "][" + item.SolarSystemName + "][" + item.StationName + "]");
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }
                        }

                        var Assets_ShipsOnly = CachedAssets.Where(i => i.CategoryId == (int)CategoryID.Ship && i.SolarSystem != null && !i.SolarSystem.IsWormholeSystem);
                        Log.WriteLine("Assets: Ships [" + Assets_ShipsOnly.Count() + "]: results limited to 100 so we dont fill the log");
                        foreach (var item in Assets_ShipsOnly.Take(100).OrderBy(i => i.SolarSystem.Jumps))
                        {
                            try
                            {
                                Log.WriteLine("  " + item.TypeName + " [" + item.Quantity + "] IsSingleton [" + item.IsSingleton + "][" + item.SolarSystemName + "][" + item.StationName + "]");
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }
                        }

                        // Do something with the assets here...
                    }
                    else Log.WriteLine("Assets: No Assets Found");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool EveryPulse()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugHighSecCombatSignaturesBehavior) Log.WriteLine("EveryPulse: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)");
                return false;
            }

            Panic.ProcessState(Settings.Instance.HomeBookmarkName);

            if (State.CurrentPanicState == PanicState.StartPanicking || State.CurrentPanicState == PanicState.Panicking || State.CurrentPanicState == PanicState.Panic)
            {
                return false;
            }

            if (State.CurrentPanicState == PanicState.Resume)
            {
                if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    Panic.ChangePanicState(PanicState.Normal);
                    State.CurrentTravelerState = TravelerState.Idle;
                    return true;
                }

                if (DebugConfig.DebugGatherShipsBehavior) Log.WriteLine("EveryPulse: if (State.CurrentPanicState == PanicState.Resume)");
                return false;
            }

            return true;
        }

        public static bool DoWeHaveShipsToGather
        {
            get
            {
                try
                {
                    if (ESCache.Instance.DirectEve.GetAssets().Any())
                    {
                        var CachedAssets = ESCache.Instance.DirectEve.GetAssets();
                        var AssetsNotInJita_ShipsOnly = CachedAssets.Where(i => i.CategoryId == (int)CategoryID.Ship &&
                                                                                i.GroupId != (int)Group.Freighter &&
                                                                                i.GroupId != (int)Group.JumpFreighter &&
                                                                                i.GroupId != (int)Group.Marauder &&
                                                                                i.GroupId != (int)Group.Shuttle &&
                                                                                i.GroupId != (int)Group.RookieShip &&
                                                                                !i.IsSingleton && //this indicates this ship is not assembled
                                                                                i.SolarSystem != null &&
                                                                                !i.SolarSystem.IsWormholeSystem &&
                                                                                i.LocationId != ESCache.Instance.StationIDJitaP4M4 &&
                                                                                i.SolarSystem.IsHighSecuritySpace &&
                                                                                i.SolarSystem.CanBeReachedUsingHighSecOnly);
                        if (AssetsNotInJita_ShipsOnly.Any())
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public static void ClearPerSystemCache()
        {
            _nextShipToGather = null;
        }

        public static void ClearPerPocketCache()
        {
            _nextShipToGather = null;
        }

        public static DirectItem _nextShipToGather = null;
        public static DirectItem NextShipToGather
        {
            get
            {
                try
                {
                    if (_nextShipToGather != null)
                        return _nextShipToGather;

                    if (ESCache.Instance.DirectEve.GetAssets().Any())
                    {
                        var CachedAssets = ESCache.Instance.DirectEve.GetAssets();
                        var AssetsNotInJita_ShipsOnly = CachedAssets.Where(i => i.CategoryId == (int)CategoryID.Ship &&
                                                        i.GroupId != (int)Group.Freighter &&
                                                        i.GroupId != (int)Group.JumpFreighter &&
                                                        i.GroupId != (int)Group.Marauder &&
                                                        i.GroupId != (int)Group.Shuttle &&
                                                        i.GroupId != (int)Group.RookieShip &&
                                                        !i.IsSingleton && //this indicates this ship is not assembled
                                                        i.SolarSystem != null &&
                                                        !i.SolarSystem.IsWormholeSystem &&
                                                        i.LocationId != ESCache.Instance.StationIDJitaP4M4 &&
                                                        i.SolarSystem.IsHighSecuritySpace &&
                                                        i.SolarSystem.CanBeReachedUsingHighSecOnly);

                        if (AssetsNotInJita_ShipsOnly.Any())
                        {
                            _nextShipToGather = AssetsNotInJita_ShipsOnly.OrderBy(i => i.SolarSystem.Jumps).FirstOrDefault();
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public static void ProcessState()
        {
            try
            {
                if (ESCache.Instance.DirectEve.Session.IsWspace)
                {
                    if (DebugConfig.DebugGatherShipsBehavior) Log.WriteLine("ProcessState: We are in wspace, returning");
                    return;
                }

                if (!EveryPulse()) return;

                if (DebugConfig.DebugGatherShipsBehavior) Log.WriteLine("State.CurrentGatherShipsBehaviorState is [" + State.CurrentGatherItemsBehaviorState + "]");

                switch (State.CurrentGatherShipsBehaviorState)
                {
                    case GatherShipsBehaviorState.Idle:
                        if (DoWeHaveShipsToGather)
                        {
                            ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.Start);
                            return;
                        }

                        break;

                    case GatherShipsBehaviorState.Start:
                        if (ESCache.Instance.InWormHoleSpace)
                            return;

                        if (ESCache.Instance.DirectEve.Session.SolarSystem.IsZeroZeroSpace)
                            return;

                        if (ESCache.Instance.DirectEve.Session.SolarSystem.IsLowSecuritySpace)
                            return;

                        if (ESCache.Instance.InSpace && !ESCache.Instance.ActiveShip.IsPod)
                        {
                            ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.TravelToMarketSystem);
                            return;
                        }

                        if (ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.IsPod)
                        {
                            ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.PickNextShipToGrab);
                            return;
                        }

                        if (ESCache.Instance.InStation)
                        {
                            ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.LeaveShip);
                            return;
                        }

                        Log.WriteLine("session change?");
                        break;

                    case GatherShipsBehaviorState.TravelToToLocation:
                        TravelToToLocationState();
                        break;

                    case GatherShipsBehaviorState.LeaveShip:
                        LeaveShipState();
                        break;

                    case GatherShipsBehaviorState.PickNextShipToGrab:
                        PickNextShipToGrabState();
                        break;

                    case GatherShipsBehaviorState.ActivateNextShipToMove:
                        ActivateNextShipToMoveState();
                        break;

                    case GatherShipsBehaviorState.TravelToMarketSystem:
                        TravelToMarketSystemState();
                        break;

                    case GatherShipsBehaviorState.Default:
                        ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }


        }

        private static void ActivateNextShipToMoveState()
        {
            try
            {
                if (Time.Instance.LastDockAction.AddSeconds(7) > DateTime.UtcNow)
                {
                    Log.WriteLine("docking...");
                    return;
                }

                if (CachedNextShipToGather == null)
                {
                    Log.WriteLine("ActivateNextShipToMoveState: CachedNextShipToGather is null: how?");
                    return;
                }
                else  if (CachedNextShipToGather != null)
                {
                    if (ESCache.Instance.ActiveShip.TypeId == CachedNextShipToGather.TypeId)
                    {
                        Log.WriteLine("ActivateNextShipToMoveState: We are in the ship we want to move: [" + CachedNextShipToGather.TypeName + "]");
                        CachedNextShipToGather = null;
                        ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.TravelToMarketSystem);
                        return;
                    }

                    if (ESCache.Instance.ShipHangar.Items.Any())
                    {
                        if (ESCache.Instance.ShipHangar.Items.Any(i => i.ItemId == CachedNextShipToGather.ItemId))
                        {
                            var ShipToBoard = ESCache.Instance.ShipHangar.Items.FirstOrDefault(i => i.ItemId == CachedNextShipToGather.ItemId);
                            if (ShipToBoard != null)
                            {
                                Log.WriteLine("ShipToBoard != null");
                                if (DirectEve.Interval(3000))
                                {
                                    if (!ShipToBoard.IsSingleton)
                                    {
                                        Log.WriteLine("ShipToBoard is packaged and needs to be assembled");
                                        if (DirectEve.Interval(8000))
                                        {
                                            Log.WriteLine("Assembling [" + ShipToBoard.TypeName + "] Ship!");
                                            ShipToBoard.AssembleShip();
                                            return;
                                        }

                                        return;
                                    }
                                    else Log.WriteLine("ShipToBoard is assembled");

                                    if (DirectEve.Interval(8000))
                                    {
                                        //Ship should be Assembled at this point!
                                        ShipToBoard.ActivateShip();
                                        return;
                                    }
                                }

                                return;
                            }

                            return;
                        }
                        else Log.WriteLine("if (ESCache.Instance.ShipHangar.Items.Any(i => i.ItemId == CachedNextShipToGather.ItemId)) is false");
                    }
                }

                ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.PickNextShipToGrab);
                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return;
            }
        }

        private static void TravelToMarketSystemState()
        {
            try
            {
                try
                {
                    Traveler.TravelToStationId(ESCache.Instance.StationIDJitaP4M4);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    Log.WriteLine("Arrived at destination");
                    Traveler.Destination = null;
                    ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.Idle);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void TravelToToLocationState()
        {
            try
            {
                try
                {
                    Traveler.TravelToStationId(CachedNextShipToGather.LocationId);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                if (Time.Instance.LastDockAction.AddSeconds(9) > DateTime.UtcNow)
                {
                    Log.WriteLine("docking...");
                    return;
                }

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    //fixme?
                    if (!DirectEve.Interval(5000))
                    {
                        Log.WriteLine("if (!DirectEve.Interval(5000))");
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    Traveler.Destination = null;
                    ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.ActivateNextShipToMove);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static DirectItem CachedNextShipToGather = null;

        private static void PickNextShipToGrabState()
        {
            try
            {
                if (ESCache.Instance.InStation)
                {
                    if (Time.Instance.LastUndockAction.AddSeconds(10) > DateTime.UtcNow)
                    {
                        Log.WriteLine("waiting for undock");
                        return;
                    }

                    if (NextShipToGather != null)
                    {
                        CachedNextShipToGather = NextShipToGather;
                        if (CachedNextShipToGather.LocationId == ESCache.Instance.DirectEve.Session.LocationId)
                        {
                            Log.WriteLine("PickNextShipToGrabState: We are at the location of the ship we want to grab: [" + CachedNextShipToGather.TypeName + "]");
                            if (ESCache.Instance.ActiveShip.ItemId == CachedNextShipToGather.ItemId)
                            {
                                Log.WriteLine("PickNextShipToGrabState: We are in the ship we want to move: [" + CachedNextShipToGather.TypeName + "]");
                                ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.TravelToMarketSystem);
                                return;
                            }

                            ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.ActivateNextShipToMove);
                            return;
                        }

                        bool AllowPodToUndock = true;
                        TravelerDestination.Undock(AllowPodToUndock);
                        return;
                    }

                    Log.WriteLine("PickNextShipToGrabState: No more ships to grab");
                    return;
                }
                else if (ESCache.Instance.InSpace)
                {
                    if (CachedNextShipToGather != null)
                    {
                        ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.TravelToToLocation);
                        return;
                    }
                    else Log.WriteLine("PickNextShipToGrabState: CachedNextShipToGather is null");

                    return;
                }

                //session change in progress?
                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void LeaveShipState()
        {
            try
            {
                if (ESCache.Instance.ActiveShip.IsPod)
                {
                    CachedNextShipToGather = null;
                    Log.WriteLine("LeaveShipState: We are in a pod");
                    ChangeGatherShipsBehaviorState(GatherShipsBehaviorState.PickNextShipToGrab);
                    return;
                }

                if (DirectEve.Interval(7000)) ESCache.Instance.ActiveShip.LeaveShip();
                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: start");
            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return;
        }

        #endregion Methods
    }
}