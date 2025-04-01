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
using SC::SharedComponents.Extensions;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using SC::SharedComponents.EVE;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Cache;
using EVESharpCore.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Framework
{
    public class DirectActiveShip : DirectItem
    {
        #region Fields

        /// <summary>
        ///     Entity cache
        /// </summary>
        private DirectEntity _entity;

        private DirectEntity _followingEntity;

        private long? _itemId;

        #endregion Fields

        #region Constructors

        internal DirectActiveShip(DirectEve directEve)
            : base(directEve)
        {
            _directEve = directEve;
            PyItem = directEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Call("GetShip");

            if (!PyItem.IsValid) DirectEve.Log("Warning: DirectActiveShip - GetShip returned null.");
        }

        DirectEve _directEve = null;

        private static long _cnt;

        /// <summary>
        /// shield, armor, structure values of past 20 pulses ( 10 seconds ). Each range:  0 ... 1.0
        /// </summary>
        public static Tuple<double, double, double>[] PastTwentyPulsesShieldArmorStrucValues = null;

        public static void SimulateLowShields()
        {
            PastTwentyPulsesShieldArmorStrucValues = new Tuple<double, double, double>[20];
            for (int i = 0; i < PastTwentyPulsesShieldArmorStrucValues.Length; i++)
            {
                PastTwentyPulsesShieldArmorStrucValues[i] = new Tuple<double, double, double>(0.1, 1, 1);
            }
        }

        public static void UpdateShieldArmorStrucValues(DirectEve de)
        {
            if (PastTwentyPulsesShieldArmorStrucValues == null)
            {
                PastTwentyPulsesShieldArmorStrucValues = new Tuple<double, double, double>[20];
                for (int i = 0; i < PastTwentyPulsesShieldArmorStrucValues.Length; i++)
                {
                    PastTwentyPulsesShieldArmorStrucValues[i] = new Tuple<double, double, double>(1, 1, 1);
                }
            }

            if (de.Session.IsReady && de.Session.IsInSpace)
            {
                if (de.ActiveShip.Entity != null)
                {
                    var ent = de.ActiveShip.Entity;
                    PastTwentyPulsesShieldArmorStrucValues[_cnt % 20] = new Tuple<double, double, double>(ent.ShieldPct, ent.ArmorPct, ent.StructurePct);
                    _cnt++;
                }
            }
        }

        #endregion Constructors

        #region Properties

        //things to look into
        //__builtin__.sm.services[systemWideEffectSvc].systemWideEffectsOnShip._systemWideEffects
        //__builtin__.sm.services[clientDogmaIM].dogmaLocation
        //FitItem
        //FitItemToLocation
        //FitItemToSelf
        //

        public bool IsPod
        {
            get
            {
                bool result = false;
                result |= GroupId == (int)Group.Capsule;
                if (result)
                    if (DirectEve.Interval(30000, 30000, result.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.ShipType), ESCache.Instance.ActiveShip.TypeName);

                return result;
            }
        }

        public bool IsMoving
        {
            get
            {
                if (5 > Entity.Velocity)
                    return false;

                if (NavigateOnGrid.LastPositionTimestamp.AddMilliseconds(200) > DateTime.UtcNow) //Wait for us to move away from the spot where we took the coord
                    return false;

                return true;
            }
        }

        public bool IsSpeedTankedShortRangeShip
        {
            get
            {
                if (HasAfterburner || HasMicroWarpDrive)
                {
                    if (_directEve.Session.IsAbyssalDeadspace)
                    {
                        if (TypeId == (int)TypeID.Retribution)
                            return true;

                        if (TypeId == (int)TypeID.Vagabond)
                            return true;
                    }
                }

                return false;
            }
        }

        public bool IsFrigOrDestroyerWithDroneBonuses
        {
            get
            {
                if (TypeId == (int)TypeID.Worm)
                    return true;

                if (TypeId == (int)TypeID.Tristan)
                    return true;

                if (TypeId == (int)TypeID.Dragoon)
                    return true;

                if (TypeId == (int)TypeID.Algos)
                    return true;


                return false;
            }
        }

        public bool IsCruiserWithDroneBonuses
        {
            get
            {
                if (TypeId == (int)TypeID.Ishtar)
                    return true;

                if (TypeId == (int)TypeID.Gila)
                    return true;

                if (TypeId == (int)TypeID.VexorNavyIssue)
                    return true;

                return false;
            }
        }

        public bool IsShipWithDroneBonuses
        {
            get
            {
                if (IsFrigOrDestroyerWithDroneBonuses)
                    return true;

                if (IsCruiserWithDroneBonuses)
                    return true;

                return false;
            }
        }

        public double? DistanceToPositionVec3TakenEvery5Seconds
        {
            get
            {
                if (!DirectEve.Session.IsInSpace) return null;
                if (NavigateOnGrid.PositionVec3TakenEvery5SecondsWas == null)
                    return null;

                double? _distanceToPositionVec3TakenEvery5Seconds = Entity.DirectAbsolutePosition.GetDistance(NavigateOnGrid.PositionVec3TakenEvery5SecondsWas);
                return _distanceToPositionVec3TakenEvery5Seconds ?? null;
            }
        }

        public double? DistanceToLastInWarpPosition
        {
            get
            {
                if (!DirectEve.Session.IsInSpace) return null;
                if (ESCache.Instance.LastInWarpPositionWas == null)
                    return null;

                double? _distanceToPositionVec3TakenEvery5Seconds = Entity.DirectAbsolutePosition.GetDistance(ESCache.Instance.LastInWarpPositionWas);
                return _distanceToPositionVec3TakenEvery5Seconds ?? null;
            }
        }

        public double? DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition
        {
            get
            {
                if (!DirectEve.Session.IsInSpace) return null;
                if (NavigateOnGrid.PositionVec3TakenEvery5SecondsWas == null)
                    return null;

                if (ESCache.Instance.LastInWarpPositionWas == null)
                    return null;

                double? _distanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition = DirectWorldPosition.GetDistance(NavigateOnGrid.PositionVec3TakenEvery5SecondsWas, ESCache.Instance.LastInWarpPositionWas);
                return _distanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition ?? null;
            }
        }

        public bool? IsMovingAwayFromLastInWarpPoint
        {
            get
            {
                // DistanceToLastInWarpPosition(LASTINWARP) == 2AU
                // DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition (away!) == 2AU + 100meters
                // DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition (toward!) == 2AU - 100meters
                // DistanceToPositionVec3TakenEvery5Seconds(XXX) = 100 meters
                //
                //     XXX ---- SHIP ----------------LASTINWARP
                //         ---- SHIP ---XXX----------LASTINWARP

                if (!IsMoving) return false;

                if (DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition == null)
                    return null;

                if (DistanceToLastInWarpPosition == null)
                    return null;

                if (DistanceToLastInWarpPosition >  DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition)
                {
                    Log.WriteLine("                                     DistanceToLastInWarpPosition [" + DistanceToLastInWarpPosition + "]");
                    Log.WriteLine("GreaterThan");
                    Log.WriteLine("DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition [" + DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition + "]");
                    Log.WriteLine("...");
                    Log.WriteLine("                         DistanceToPositionVec3TakenEvery5Seconds [" + DistanceToPositionVec3TakenEvery5Seconds + "]");
                    Log.WriteLine("IsMovingTowardLastInWarpPoint [" + IsMovingTowardLastInWarpPoint + "] IsMovingAwayFromLastInWarpPoint [ true ]");
                    return true;
                }

                return false;
            }
        }

        public bool? IsMovingTowardLastInWarpPoint
        {
            get
            {
                // DistanceToLastInWarpPosition(LASTINWARP) == 2AU
                // DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition (away!) == 2AU + 100meters
                // DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition (toward!) == 2AU - 100meters
                // DistanceToPositionVec3TakenEvery5Seconds(XXX) = 100 meters
                //
                //     XXX ---- SHIP ----------------LASTINWARP
                //         ---- SHIP ---XXX----------LASTINWARP

                if (!IsMoving) return false;

                if (DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition == null)
                    return null;

                if (DistanceToLastInWarpPosition == null)
                    return null;

                if (DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition > DistanceToLastInWarpPosition)
                {
                    Log.WriteLine("DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition [" + DistanceFromPositionVec3TakenEvery5SecondsWasToLastInWarpPosition + "]");
                    Log.WriteLine("GreaterThan");
                    Log.WriteLine("                                     DistanceToLastInWarpPosition [" + DistanceToLastInWarpPosition + "]");
                    Log.WriteLine("...");
                    Log.WriteLine("DistanceToPositionVec3TakenEvery5Seconds [" + DistanceToPositionVec3TakenEvery5Seconds + "]");
                    Log.WriteLine("IsMovingTowardLastInWarpPoint [ true ] IsMovingAwayFromLastInWarpPoint [" + IsMovingAwayFromLastInWarpPoint + "]");
                    return true;
                }


                return false;
            }
        }

        public void AdjustSpeedIfGoingTooFast(double myVelocity)
        {
            try
            {
                if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(AdjustSpeedIfGoingTooFast)))
                    return;

                if (!DirectEve.Interval(100, 200))
                    return;

                if (DirectEve.Session.InJump)
                    return;

                if (Entity.IsWarpingByMode)
                    return;

                if (Entity.Velocity > 20000)
                    return;

                if (Entity.IsDread)
                    return;

                if (DirectEve.Session.IsWspace)
                    return;

                if (ESCache.Instance.Wormholes.Any(i => i.IsOnGridWithMe))
                    return;

                if (ESCache.Instance.Stargates.Any(i => i.IsOnGridWithMe))
                    return;

                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    if (DirectEve.ActiveShip.Entity.IsLocatedWithinSpeedCloud || DirectEve.ActiveShip.Entity.IsTooCloseToSpeedCloud || ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud)
                    {
                        try
                        {
                            if (DebugConfig.DebugSetSpeed && DirectEve.ActiveShip.Entity.IsLocatedWithinSpeedCloud) Log.WriteLine("if (DirectEve.ActiveShip.Entity.IsLocatedWithinSpeedCloud)");
                            if (DebugConfig.DebugSetSpeed && !DirectEve.ActiveShip.Entity.IsLocatedWithinSpeedCloud) Log.WriteLine("IsAbyssalDeadspaceTachyonCloud && IsTooCloseToSpeedCloud");
                            if (DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                            {
                                if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (DirectEve.Modules.Any(i => i.IsMicroWarpDrive && i.IsActive))");
                                if (Combat.PotentialCombatTargets.Any())
                                {
                                    if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (Combat.PotentialCombatTargets.Any())");

                                    if (ESCache.Instance.ActiveShip.Entity.IsTooCloseToSpeedCloud)
                                    {
                                        if (myVelocity > 2000)
                                        {
                                            Log.WriteLine("IsAbyssalDeadspaceTachyonCloud is very close: slowing down");
                                            if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(.5f);
                                            return;
                                        }
                                    }

                                    if (myVelocity > 9000) // div by 5 = 1800
                                    {
                                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (myVelocity > 9000)");
                                        if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(.20f);
                                        return;
                                    }

                                    if (myVelocity > 6000) // div by 4 = 1500
                                    {
                                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (myVelocity > 6000)");
                                        if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(.25f);
                                        return;
                                    }

                                    if (myVelocity > 4000) // div by 3 = 1700
                                    {
                                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (myVelocity > 4000)");
                                        if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(.333f);
                                        return;
                                    }

                                    if (myVelocity > 3000) //div by 2 = 1500
                                    {
                                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (myVelocity > 3000)");
                                        if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(.5f);
                                        return;
                                    }

                                    return;
                                }

                                if (myVelocity < 950 && FollowingEntity != null)
                                {
                                    if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (myVelocity < 950 && FollowingEntity != null)");
                                    if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(1f);
                                    return;
                                }
                            }

                            //Afterburner
                            if (DirectEve.Modules.Any(i => i.IsAfterburner && !i.IsMicroWarpDrive))
                            {
                                if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (DirectEve.Modules.Any(i => i.IsAfterburner && !i.IsMicroWarpDrive && i.IsActive))");
                                if (!Combat.PotentialCombatTargets.Any())
                                {
                                    //if (ESCache.Instance.AbyssalMobileTractor != null && 5000 > ESCache.Instance.AbyssalMobileTractor.Distance)
                                    //{
                                    //    if (.5 >= ESCache.Instance.ActiveShip.Entity.SpeedFraction)
                                    //    {
                                            //if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(.40f);
                                            //return;
                                    //    }
                                    //}

                                    if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (!Combat.PotentialCombatTargets.Any())");
                                    if (ESCache.Instance.AccelerationGates.Any(i => 5000 > i.Distance && i._directEntity.IsAbyssGateOpen()))
                                    {
                                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (ESCache.Instance.AccelerationGates.Any(i => 5000 > i.Distance))");
                                        if (Drones.ActiveDrones.Any())
                                        {
                                            if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (Drones.ActiveDrones.Any())");
                                            if (myVelocity > 300 && DirectEve.Interval(8000, 12000)) SetSpeedFraction(0.1f); //10th
                                            return;
                                        }

                                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (!Drones.ActiveDrones.Any())");
                                        if (.5 > ESCache.Instance.ActiveShip.Entity.SpeedFraction)
                                        {
                                            if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (.5 > ESCache.Instance.ActiveShip.Entity.SpeedFraction)");
                                            if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(1.0f);
                                            return;
                                        }

                                        return;
                                    }
                                }

                                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                                {
                                    if (Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) == 1)
                                    {
                                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i._directEntity.IsInSpeedCloud && i.DistanceTo(ESCache.Instance.AccelerationGates.FirstOrDefault()) > 25000))
                                        {
                                            if (ESCache.Instance.AccelerationGates.FirstOrDefault().Distance > 25000)
                                            {
                                                if (myVelocity > 1000)
                                                {
                                                    Log.WriteLine("KarybdisTyrannosSpawn: Gate > 25k: if (myVelocity > 1000) - .15");
                                                    if (DirectEve.Interval(2000, 2000, myVelocity.ToString())) SetSpeedFraction(.15f);
                                                    return;
                                                }

                                                if (myVelocity > 500)
                                                {
                                                    Log.WriteLine("KarybdisTyrannosSpawn: Gate > 25k: if (myVelocity > 500) - .33)");
                                                    if (DirectEve.Interval(2000, 2000, myVelocity.ToString())) SetSpeedFraction(.33f);
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (myVelocity > 2200)
                                {
                                    if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (myVelocity > 2200)");
                                    if (DirectEve.Interval(2000, 2000, myVelocity.ToString())) SetSpeedFraction(.5f);
                                    return;
                                }

                                if (ESCache.Instance.ActiveShip.IsCruiser && myVelocity > 1500)
                                {
                                    if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (ESCache.Instance.ActiveShip.IsCruiser && myVelocity > 900)");
                                    if (DirectEve.Interval(2000, 2000, myVelocity.ToString())) SetSpeedFraction(.5f);
                                    return;
                                }

                                //if (myVelocity < MaxVelocity - 100)
                                //{
                                //    if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (DirectEve.Interval(10000, 15000) && myVelocity < MaxVelocity - 100)");
                                //    if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(.4f);
                                //    return;
                                //}

                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Log.WriteLine("Exception [" + ex + "]!");
                        }
                    }
                    else
                    {
                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("not in a speed cloud");
                        try
                        {
                            if (myVelocity > 8000) //1600
                            {
                                if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (myVelocity > 8000)");
                                if (DirectEve.Interval(1000)) SetSpeedFraction(.20f);
                                return;
                            }
                            else if (myVelocity > 6000) //1500
                            {
                                if (DebugConfig.DebugSetSpeed) Log.WriteLine("else if (myVelocity > 6000)");
                                if (DirectEve.Interval(1000)) SetSpeedFraction(.25f);
                                return;
                            }
                            else if (myVelocity > 4000) //1600
                            {
                                if (DebugConfig.DebugSetSpeed) Log.WriteLine("else if (myVelocity > 4000)");
                                if (DirectEve.Interval(1000)) SetSpeedFraction(.40f);
                                return;
                            }
                            else if (myVelocity > 3000) //2000
                            {
                                if (DebugConfig.DebugSetSpeed) Log.WriteLine("else if (myVelocity > 3000)");
                                if (DirectEve.Interval(1000)) SetSpeedFraction(.666f);
                                return;
                            }

                            if (!Combat.PotentialCombatTargets.Any())
                            {
                                if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (!Combat.PotentialCombatTargets.Any())");
                                if (!DirectEve.Entities.Any(y => y.IsAbyssalDeadspaceTriglavianBioAdaptiveCache) && !DirectEve.Entities.Any(x => x.IsWreck && !x.IsWreckEmpty) && DirectEve.Entities.Any(i => i.IsAccelerationGate && 3000 > i.Distance))
                                {
                                    if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (!DirectEve.Entities.Any(y => y.IsAbyssalDeadspaceTriglavianBioAdaptiveCache) && !DirectEve.Entities.Any(x => x.IsWreck && !x.IsWreckEmpty) && DirectEve.Entities.Any(i => i.IsAccelerationGate && 8000 > i.Distance))");
                                    if (myVelocity > 1400 && DirectEve.Interval(8000, 12000)) SetSpeedFraction(.75f);
                                    return;
                                }
                                else if (DirectEve.Entities.Any(i => i.IsWreck && !i.IsWreckEmpty && 3000 > i.Distance))
                                {
                                    if (DebugConfig.DebugSetSpeed) Log.WriteLine("else if (DirectEve.Entities.Any(i => i.IsWreck && !i.IsWreckEmpty && 8000 > i.Distance && i.Velocity > 0))");
                                    if (FollowingEntity != null && FollowingEntity.IsWreck)
                                    {
                                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (FollowingEntity != null && FollowingEntity.IsWreck)");
                                        if (myVelocity > 1400 && DirectEve.Interval(8000, 12000)) SetSpeedFraction(.75f);
                                        return;
                                    }

                                    if (DirectEve.Modules.Any(i => i.GroupId == (int)Group.TractorBeam && i.TargetId != 0))
                                    {
                                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (DirectEve.Modules.Any(i => i.GroupId == (int)Group.TractorBeam && i.TargetId != 0))");
                                        if (myVelocity > 500 && DirectEve.Interval(8000, 12000)) SetSpeedFraction(0.1f);//10th
                                        return;
                                    }

                                    if (DirectEve.Modules.All(i => i.GroupId != (int)Group.TractorBeam))
                                    {
                                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (DirectEve.Modules.All(i => i.GroupId != (int)Group.TractorBeam))");
                                        if (myVelocity > 1400 && DirectEve.Interval(8000, 12000)) SetSpeedFraction(.6f);
                                        return;
                                    }
                                }
                                //else if (Combat.KillTarget != null && !Combat.KillTarget.IsTrackable && (IsFrigate || IsDestroyer || IsAssaultShip))
                                //{
                                    //int _intrandom = RandomNumber(1, 20);
                                    //if (_intrandom > 10)
                                    //{
                                    //    if (myVelocity < MaxVelocity - 100)
                                    //        SetSpeed(9999);
                                    //
                                    //    return;
                                    //}

                                    //if (DirectEve.Modules.Any(i => (i.IsShieldRepairModule && ShieldPercentage > 50) || i.IsArmorRepairModule && ArmorPercentage > 50))
                                    //{
                                    //    SetSpeed(50 / MaxVelocity);
                                    //    return;
                                    //}
                                    //else
                                    //{
                                    //    SetSpeed(9999);
                                    //    return;
                                    //}
                                    //
                                    //return;
                                //}
                                else if (.90 > ESCache.Instance.ActiveShip.Entity.SpeedFraction)
                                {
                                    if (DebugConfig.DebugSetSpeed) Log.WriteLine("else if (.8 > ESCache.Instance.ActiveShip.Entity.SpeedFraction)");
                                    if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(1.0f); //full
                                    return;
                                }
                            }
                            else if (.90 > ESCache.Instance.ActiveShip.Entity.SpeedFraction)
                            {
                                if (DebugConfig.DebugSetSpeed) Log.WriteLine("else if (.8 > ESCache.Instance.ActiveShip.Entity.SpeedFraction)");
                                if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(1.0f); //full
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Log.WriteLine("Exception [" + ex + "].");
                            return;
                        }
                    }
                }
                else
                {
                    if (DebugConfig.DebugSetSpeed) Log.WriteLine("In K-Space");
                    DirectEntity AbyssalTrace = DirectEve.Entities.FirstOrDefault(i => i.GroupId == (int)Group.AbyssalTrace);

                    if (AbyssalTrace != null)
                    {
                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (AbyssalTrace != null)");
                        if (3500 > AbyssalTrace.Distance)
                        {
                            if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (3500 > AbyssalTrace.Distance)");
                            SetSpeedFraction(.4f);
                            return;
                        }

                        if (AbyssalTrace.Distance > 5000)
                        {
                            if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (AbyssalTrace.Distance > 5000)");
                            SetSpeedFraction(.4f);
                            return;
                        }
                    }

                    //
                    // for low skilled pilots...
                    //
                    //if (2000000 > ESCache.Instance.EveAccount.TotalSkillPoints)
                    //{

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                    {
                        if (ESCache.Instance.InMission && ESCache.Instance.Weapons.Any(i => 170 > i.TrackingSpeed && i.IsTurret) && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer))
                        {
                            if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (ESCache.Instance.InMission && ESCache.Instance.Weapons.Any(i => 170 > i.TrackingSpeed && i.IsTurret) && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer))");
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.IsInRangeOfWeapons) && ESCache.Instance.ActiveShip.ShieldPercentage > 30)
                            {
                                if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.IsInRangeOfWeapons && !i.IsTrackable && TypeId != (int)TypeID.Worm) && ESCache.Instance.ActiveShip.ShieldPercentage > 80)");
                                if (DirectEve.Interval(2000, 2800)) SetSpeedFraction(.10f);
                            }
                        }
                    }

                    if (ESCache.Instance.InMission && ESCache.Instance.Weapons.Any(i => 170 > i.TrackingSpeed && i.IsTurret) && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer))
                    {
                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (ESCache.Instance.InMission && ESCache.Instance.Weapons.Any(i => 170 > i.TrackingSpeed && i.IsTurret) && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer))");
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.IsInRangeOfWeapons && !i.IsTrackable && TypeId != (int)TypeID.Worm) && ESCache.Instance.ActiveShip.ShieldPercentage > 80)
                        {
                            if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.IsInRangeOfWeapons && !i.IsTrackable && TypeId != (int)TypeID.Worm) && ESCache.Instance.ActiveShip.ShieldPercentage > 80)");
                            if (DirectEve.Interval(2000, 2800)) SetSpeedFraction(.15f);
                        }
                    }
                    //}

                    if (.5 > ESCache.Instance.ActiveShip.Entity.SpeedFraction)
                    {
                        if (DebugConfig.DebugSetSpeed) Log.WriteLine("if (.5 > ESCache.Instance.ActiveShip.Entity.SpeedFraction)");
                        if (DirectEve.Interval(8000, 12000)) SetSpeedFraction(1.0f); //full
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
                return;
            }
        }

        /// <summary>
        ///     Your current amount of armor
        /// </summary>
        public double Armor => MaxArmor - Attributes.TryGet<double>("armorDamage");

        private double? _armorPercentage = null;

        /// <summary>
        ///     Armor percentage
        /// </summary>
        public double ArmorPercentage
        {
            get
            {
                if (_armorPercentage != null)
                    return _armorPercentage ?? 0;

                _armorPercentage = Math.Round(Math.Abs(Armor / MaxArmor * 100), 1);
                return _armorPercentage ?? 0;
            }
        }
        /// <summary>
        ///     Your current amount of capacitor
        /// </summary>
        public double Capacitor => Attributes.TryGet<double>("charge");

        /// <summary>
        ///     Capacitor percentage
        /// </summary>
        public double CapacitorPercentage => Math.Round(Capacitor / MaxCapacitor * 100, 1);

        /// <summary>
        ///     DroneBandwidth
        /// </summary>
        public int DroneBandwidth => (int)Attributes.TryGet<double>("droneBandwidth");

        /// <summary>
        ///     DroneCapacity
        /// </summary>
        public int DroneCapacity => (int)Attributes.TryGet<double>("droneCapacity");

        // other checks -> treeData.py
        //    if bool (godmaSM.GetType(typeID).hasShipMaintenanceBay):
        //shipData.append(TreeDataShipMaintenanceBay(parent=self, clsName='ShipMaintenanceBay', itemID=itemID))
        //if bool (godmaSM.GetType(typeID).hasFleetHangars):
        //shipData.append(TreeDataFleetHangar(parent=self, clsName='ShipFleetHangar', itemID=itemID))
        //if bool (godmaSM.GetType(typeID).specialFuelBayCapacity):
        //shipData.append(TreeDataInv(parent=self, clsName='ShipFuelBay', itemID=itemID))
        //if bool (godmaSM.GetType(typeID).specialOreHoldCapacity):

        /// <summary>
        ///     The entity associated with your ship
        /// </summary>
        /// <remarks>
        ///     Only works in space, return's null if no entity can be found
        /// </remarks>
        public DirectEntity Entity => _entity ?? (_entity = DirectEve.GetEntityById(DirectEve.Session.ShipId ?? -1));

        public DirectEntity FollowingEntity => _followingEntity ?? (_followingEntity = DirectEve.GetEntityById(Entity != null ? Entity.FollowId : -1));

        /// <summary>
        /// AmmoHold is only available on certain special haulers
        /// </summary>
        public bool HasAmmoHold
        {
            get
            {
                if (TypeId == (int)TypeID.Hoarder)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Command Center Hold
        /// </summary>
        public bool HasCommandCenterHold
        {
            get
            {
                //if (TypeId == (int)TypeID.Venture)
                //    return true;

                if (TypeId == (int)TypeID.Primae)
                    return true;

                return false;
            }
        }

        public bool HasDroneBay => DroneCapacity > 0;

        /// <summary>
        /// Fleet Hangar: Has to be set to allow fleet or members of the fleet will not be able to access it!
        /// </summary>
        public bool HasFleetHangar
        {
            get
            {
                if (TypeId == (int)TypeID.Bustard)
                    return true;

                if (TypeId == (int)TypeID.Impel)
                    return true;

                if (TypeId == (int)TypeID.Mastodon)
                    return true;

                if (TypeId == (int)TypeID.Occator)
                    return true;

                if (TypeId == (int)TypeID.Orca)
                    return true;

                if (TypeId == (int)TypeID.Porpoise)
                    return true;

                if (TypeId == (int)TypeID.Rorqual)
                    return true;

                if (TypeId == (int)TypeID.Squall)
                    return true;

                if (TypeId == (int)TypeID.Torrent)
                    return true;

                if (GroupId == (int)Group.Carrier)
                    return true;

                if (GroupId == (int)Group.Dreadnaught)
                    return true;

                //GroupId == 941 || //Industrial Command Ships - Orca
                //GroupId == 883 || //Capital Industrial Ships - Rorqual
                //GroupId == 1538) //Force Auxiliary

                return false;
            }
        }



        /// <summary>
        /// Frigate Escape Bay: Can hold 1 frigate
        /// </summary>
        public bool HasFrigateEscapeBay
        {
            get
            {
                if (GroupId == (int)Group.Battleship)
                    return true;

                if (GroupId == (int)Group.BlackOps)
                    return true;

                if (GroupId == (int)Group.Marauder)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Holds Isotopes: usually for jump capable ships
        /// </summary>
        public bool HasFuelBay
        {
            get
            {
                if (GroupId == (int)Group.BlackOps)
                    return true;

                if (GroupId == (int)Group.Carrier)
                    return true;

                if (GroupId == (int)Group.Dreadnaught)
                    return true;

                if (GroupId == (int)Group.JumpFreighter)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Gas Hold is only available on special ships used for gas harvesting!
        /// </summary>
        public bool HasGasHold
        {
            get
            {
                //if (TypeId == (int)TypeID.Venture)
                //    return true;

                if (TypeId == (int)TypeID.Hoarder)
                    return true;

                if (TypeId == (int)TypeID.Primae)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// MiningHold: Most mining ships have one of these. Ore, Ice, Minerals(?) are allowed in it
        /// </summary>
        public bool HasGeneralMiningHold
        {
            get
            {
                if (TypeId == (int)TypeID.Endurance)
                    return true;

                if (TypeId == (int)TypeID.Miasmos)
                    return true;

                if (TypeId == (int)TypeID.Orca)
                    return true;

                if (TypeId == (int)TypeID.Porpoise)
                    return true;

                if (TypeId == (int)TypeID.Prospect) //Expedition Frigate
                    return true;

                if (GroupId == (int)Group.MiningBarge)
                    return true;

                if (GroupId == (int)Group.Exhumer)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Ice Hold: Only Ice is allowed in it: Only special haulers have one of these!
        /// </summary>
        public bool HasIceHold
        {
            get
            {
                if (TypeId == (int)TypeID.Kryos)
                    return true;

                if (TypeId == (int)TypeID.Primae)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Infrastructure Hold: Only special Ships have one of these
        /// </summary>
        public bool HasInfrastructureHold
        {
            get
            {
                if (TypeId == (int)TypeID.Avalanche)
                    return true;

                if (TypeId == (int)TypeID.Squall)
                    return true;

                if (TypeId == (int)TypeID.Deluge)
                    return true;

                if (TypeId == (int)TypeID.Torrent)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Mineral Hold: this is NOT a general mining hold and ONLY holds minerals. Only special haulers have one of these!
        /// </summary>
        public bool HasMineralHold
        {
            get
            {
                if (TypeId == (int)TypeID.Kryos)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Mobile Depot Hold: Only special ships have one of these!
        /// </summary>
        public bool HasMobileDepotHold
        {
            get
            {
                if (TypeId == (int)TypeID.Metamorphosis)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// OreHold: this is NOT a general mining hold and ONLY holds minerals. Only special haulers have one of these!
        /// </summary>
        public bool HasOreHold
        {
            get
            {
                if (TypeId == (int)TypeID.Miasmos)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Planetary Commodities Hold: Only special haulers have one of these!
        /// </summary>
        public bool HasPlanetaryCommoditiesHold
        {
            get
            {
                if (TypeId == (int)TypeID.Epithal)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Salvage Hold: Only special ships have one of these! Do no ships have one if these?!
        /// </summary>
        public bool HasSalvageHold
        {
            get
            {
                //if (TypeId == (int)TypeID.Noctis)
                //    return true;

                return false;
            }
        }

        public bool HasShipMaintenanceBay
        {
            get
            {
                if (TypeId == (int)TypeID.Bowhead)
                    return true;

                if (GroupId == (int)Group.Carrier)
                    return true;

                return false;
            }
        }

        /// <summary>
        ///     Inertia Modifier (also called agility)
        /// </summary>
        public double InertiaModifier => Attributes.TryGet<double>("agility");
        //private DirectUIModule _bastionUIModule;
        //private bool _checkedForBastion;
        public bool IsShieldTanked
        {
            get
            {
                //default to true
                if (DirectEve.Session.IsInSpace)
                {
                    if (IsArmorTanked) return false;

                    if (DirectEve.Modules != null)
                    {
                        if (DirectEve.Modules.Any(i => i.IsShieldTankModule || i.IsShieldRepairModule))
                            return true;

                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        public bool IsApproaching
        {
            get
            {
                if (Time.Instance.LastApproachAction.AddMilliseconds(2500) > DateTime.UtcNow)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsArmorTanked
        {
            get
            {
                //default to true
                if (DirectEve.Session.IsInSpace)
                {
                    if (DirectEve.Modules != null)
                    {
                        if (DirectEve.Modules.Any(i => i.IsArmorTankModule || i.IsArmorRepairModule))
                            return true;

                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        public bool HasSpeedMod
        {
            get
            {
                if (DirectEve.Modules.Any(i => i.GroupId == (int)Group.Afterburner))
                    return true;

                return false;
            }
        }

        public bool HasAfterburner
        {
            get
            {
                if (DirectEve.Modules.Any(i => i.IsAfterburner))
                    return true;

                return false;
            }
        }

        public bool HasMicroWarpDrive
        {
            get
            {
                if (DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                    return true;

                return false;
            }
        }

        public bool HasTurrets
        {
            get
            {
                if (DirectEve.Modules.Any(i => i.IsTurret))
                    return true;

                return false;
            }
        }

        public bool HasMiningLasersForMercoxit
        {
            get
            {
                if (DirectEve.Modules.Any(i => i.IsMiningMercoxitMiningLaser))
                    return true;

                return false;
            }
        }

        public bool HasMiningLasers
        {
            get
            {
                if (DirectEve.Modules.Any(i => i.IsMiningLaser))
                    return true;

                return false;
            }
        }

        public bool HasGasHarvesters
        {
            get
            {
                if (DirectEve.Modules.Any(i => i.IsGasCloudHarvester))
                    return true;

                return false;
            }
        }

        public bool IsActiveTanked
        {
            get
            {
                try
                {
                    if (DirectEve.Session.IsInSpace)
                    {
                        if (DirectEve.Modules != null)
                        {
                            if (DirectEve.Modules.Any(i => i.IsArmorRepairModule))
                                return true;

                            if (DirectEve.Modules.Any(i => i.IsShieldRepairModule))
                                return true;

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Logging.Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        //private double? _capacitor = null;
        /// <summary>
        ///     Your current amount of capacitor
        /// </summary>

        //private double? _capacitorPercentage = null;
        /// <summary>
        ///     Capacitor percentage
        /// </summary>


        // other checks -> treeData.py
        //    if bool (godmaSM.GetType(typeID).hasShipMaintenanceBay):
        //shipData.append(TreeDataShipMaintenanceBay(parent=self, clsName='ShipMaintenanceBay', itemID=itemID))
        //if bool (godmaSM.GetType(typeID).hasFleetHangars):
        //shipData.append(TreeDataFleetHangar(parent=self, clsName='ShipFleetHangar', itemID=itemID))
        //if bool (godmaSM.GetType(typeID).specialFuelBayCapacity):
        //shipData.append(TreeDataInv(parent=self, clsName='ShipFuelBay', itemID=itemID))
        //if bool (godmaSM.GetType(typeID).specialOreHoldCapacity):

        public bool IsLocatedWithinFilamentCloud => (DirectEve.Entities.FirstOrDefault(i => i.Id == DirectEve.ActiveShip.ItemId).IsLocatedWithinFilamentCloud) || DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.aoeFilamentCloud) || DebugConfig.DebugPretendWeAreInAFilamentCloud;
        public bool IsLocatedWithinBioluminescenceCloud => DirectEve.Entities.FirstOrDefault(i => i.Id == DirectEve.ActiveShip.ItemId).IsLocatedWithinBioluminescenceCloud;
        public bool IsLocatedWithinCausticCloud => DirectEve.Entities.FirstOrDefault(i => i.Id == DirectEve.ActiveShip.ItemId).IsLocatedWithinCausticCloud;
        public bool IsLocatedWithinSpeedCloud => DirectEve.Entities.FirstOrDefault(i => i.Id == DirectEve.ActiveShip.ItemId).IsLocatedWithinSpeedCloud; // || DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.aoe);

        public double? MaxGigaJoulePerSecondTank
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsActiveTanked)
                {
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                    {
                        return 999; //dont run from neuts, we are too slow to actually get away!
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                    {
                        return 999; //dont run from neuts, we are too slow to actually get away!
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        return 70;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                    {
                        return 999; //dont run from neutsshould we though!?!
                    }

                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Cruiser)
                    {
                        return 67;
                    }

                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Frigate || ESCache.Instance.ActiveShip.GroupId == (int)Group.Destroyer)
                    {
                        //fixme
                        return 1;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        return 4.5;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Hawk)
                    {
                        return 4.5;
                    }

                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.AssaultShip)
                    {
                        return 4.5;
                    }

                    return 67;
                }

                return 999;
            }
        }

        public bool CapConservationModeNeeded
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (IsLocatedWithinFilamentCloud && ESCache.Instance.ActiveShip.IsShieldTanked)
                    {
                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any() && Combat.PotentialCombatTargets.Where(i => i.IsReadyToTarget && i.IsNeutralizingMe && i.IsTargetedBy).Sum(e => e._directEntity.GigaJouleNeutedPerSecond) >= MaxGigaJoulePerSecondTank)
                                return true;

                            if (50 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                                return true;

                            return false;
                        }

                        if (Combat.PotentialCombatTargets.Any() && Combat.PotentialCombatTargets.Where(i => i.IsReadyToTarget && i.IsNeutralizingMe && i.IsTargetedBy).Sum(e => e._directEntity.GigaJouleNeutedPerSecond) >= MaxGigaJoulePerSecondTank)
                            return true;

                        if (50 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                            return true;

                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any() && Combat.PotentialCombatTargets.Where(i => i.IsReadyToTarget && i.IsNeutralizingMe && i.IsTargetedBy).Sum(e => e._directEntity.GigaJouleNeutedPerSecond) >= MaxGigaJoulePerSecondTank)
                        return true;

                    if (30 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                        return true;
                }

                return false;
            }
        }

        //
        public bool IsLocatedWithinRangeOfPulsePlatformTrackingPylon => GetDmgEffects().ContainsKey(7053);
        public bool IsLocatedWithinRangeOfPointDefenseBattery => GetDmgEffects().ContainsKey(7057);

        //public bool IsLocatedWithinRangeOfPulsePlatformTrackingPylon => DirectEve.Entities.FirstOrDefault(i => i.Id == DirectEve.ActiveShip.ItemId).IsLocatedWithinRangeOfPulsePlatformTrackingPylon;

        //public bool IsLocatedWithinRangeOfPointDefenseBattery => DirectEve.Entities.FirstOrDefault(i => i.Id == DirectEve.ActiveShip.ItemId).IsLocatedWithinRangeOfPointDefenseBattery;

        //effectWeatherCausticToxin = 7059
        //effectWeatherDarkness = 7060
        //effectWeatherInfernal = 7062
        //effectWeatherXenonGas = 7063
        //effectWeatherElectricStorm = 7061
        //effectWarpDisruptSphere
        //effectWarpDisrupt
        //effectWarpScrambleForEntity
        //effectEssWarpScramble
        //effectConcordWarpScramble
        //effectShipModuleFocusedWarpDisruptionScript
        //effectShipModuleFocusedWarpScramblingScript

        public bool IsImmobile
        {
            get
            {
                List<DirectUIModule> bastionAndSiegeModules = DirectEve.Modules.Where(m => m.GroupId == (int)Group.BastionAndSiegeModules && m.IsOnline).ToList();
                if (bastionAndSiegeModules.Any(i => i.IsActive))
                    return true;

                return false;
            }
        }

        public bool IsScrambled
        {
            get
            {
                if (DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.warpScramblerMWD))
                {
                    return true;
                }

                if (DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.warpScrambler))
                {
                    return true;
                }

                if (DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.focusedWarpScrambler))
                {
                    return true;
                }

                if (DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.LinkedToESSReserveBank))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsWebbed
        {
            get
            {
                if (DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.webify))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsWarpDisrupted
        {
            get
            {
                if (IsScrambled)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsFrigate
        {
            get
            {
                if (DirectEve.Session.IsInSpace)
                {
                    if (Entity.IsFrigate)
                        return true;

                    return false;
                }

                if (GroupId == (int)Group.Frigate)
                    return true;

                return false;
            }
        }

        public bool IsAssaultShip
        {
            get
            {
                if (DirectEve.Session.IsInSpace)
                {
                    if (GroupId == (int)Group.AssaultShip)
                        return true;

                    return false;
                }

                if (GroupId == (int)Group.AssaultShip)
                    return true;

                return false;
            }
        }

        public bool IsBattleCruiser
        {
            get
            {
                if (DirectEve.Session.IsInSpace)
                {
                    if (Entity.IsBattlecruiser || Entity.IsT2BattleCruiser)
                        return true;

                    return false;
                }

                if (GroupId == (int)Group.Battlecruiser)
                    return true;

                return false;
            }
        }

        public bool IsBattleship
        {
            get
            {
                if (DirectEve.Session.IsInSpace)
                {
                    if (Entity.IsBattleship)
                        return true;

                    return false;
                }

                if (GroupId == (int)Group.Battleship)
                    return true;

                return false;
            }
        }

        public bool IsDestroyer
        {
            get
            {
                if (DirectEve.Session.IsInSpace)
                {
                    if (Entity.IsDestroyer)
                        return true;

                    return false;
                }

                if (GroupId == (int)Group.Destroyer)
                    return true;

                return false;
            }
        }

        public bool IsCruiser
        {
            get
            {
                if (DirectEve.Session.IsInSpace)
                {
                    if (Entity.IsCruiser || Entity.IsT2Cruiser)
                        return true;

                    return false;
                }

                if (GroupId == (int)Group.Cruiser)
                    return true;

                return false;
            }
        }

        public bool IsDread
        {
            get
            {
                if (DirectEve.Session.IsInSpace)
                {
                    if (Entity.IsDread)
                        return true;

                    return false;
                }

                if (GroupId == (int)Group.Dreadnaught)
                    return true;

                return false;
            }
        }

        public bool IsMarauder
        {
            get
            {
                if (DirectEve.Session.IsInSpace)
                {
                    if (Entity.IsMarauder)
                        return true;

                    return false;
                }

                if (GroupId == (int)Group.Marauder)
                    return true;

                return false;
            }
        }

        public bool IsShipWithNoDroneBay
        {
            get
            {
                try
                {
                    //
                    // if we are in abyssaldeadspace just use UseDrones to determine if we should use drones or not
                    // in regular space we might be in various ships, in abyssaldeadspace we should always be in the combatship
                    //

                    if (TypeId == (int)TypeID.Hawk)
                        return false;

                    if (HasDroneBay)
                        return false;

                    if (GroupId == (int)Group.Dreadnaught)
                        return true;

                    if (GroupId == (int)Group.Capsule)
                        return true;

                    if (DroneCapacity < 5)
                        return true;

                    return false;
                }
                catch (Exception exception)
                {
                    DirectEve.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public new long ItemId
        {
            get
            {
                if (!_itemId.HasValue)
                    _itemId = (long)DirectEve.Session.ShipId;

                return _itemId.Value;
            }
            internal set => _itemId = value;
        }

        public bool ManageMyOwnDroneTargets
        {
            get
            {
                //if this is false the drone behavior will try to assist any available drones to another fleet member
                if (DirectEve.Entities.Any(i => i.CategoryId == (int)CategoryID.Ship && 80000 > i.Distance && i.IsPlayer))
                {
                    if (TypeId == (int)TypeID.Augoror)
                        return false;

                    if (TypeId == (int)TypeID.Nestor)
                        return false;

                    if (TypeId == (int)TypeID.Noctis)
                        return false;

                    if (GroupId == (int)Group.Logistics)
                        return false;
                }

                return true;
            }
        }

        public bool IsLogisticsOnly
        {
            get
            {
                //if this is false the drone behavior will try to assist any available drones to another fleet member
                if (!DirectEve.Weapons.Any())
                {
                    if (TypeId == (int)TypeID.Nestor)
                        return true;

                    if (GroupId == (int)Group.Logistics)
                        return true;
                }

                return false;
            }
        }

        public bool IsWarpScrambled
        {
            get
            {
                if (DirectEve.Modules.Any(i => GroupId == (int)Group.WarpCoreStabilizer))
                    return false; //should we do the math?

                if (DirectEve.ActiveShip.Entity.GroupId == (int)Group.Industrial) // GroupId == (int)GroupID.WarpCoreStabilizer))
                    return false; //should we do the math?

                if (DirectEve.ActiveShip.Entity.GroupId == (int)Group.TransportShip) // GroupId == (int)GroupID.WarpCoreStabilizer))
                    return false; //should we do the math?

                if (DirectEve.ActiveShip.Entity.GroupId == (int)Group.BlockadeRunner) // GroupId == (int)GroupID.WarpCoreStabilizer))
                    return false; //should we do the math?

                if (DirectEve.ActiveShip.IsScrambled)
                    return true;

                //
                // Add various mobile bubbles?
                //

                return false;
            }
        }

        /// <summary>
        ///     The maximum amount of armor
        /// </summary>
        public double MaxArmor => Attributes.TryGet<double>("armorHP");

        /// <summary>
        ///     The maximum amount of capacitor
        /// </summary>
        public double MaxCapacitor => Attributes.TryGet<double>("capacitorCapacity");

        /// <summary>
        ///     Maximum locked targets
        /// </summary>
        /// <remarks>
        ///     Skills may cause you to lock less targets!
        /// </remarks>
        public int MaxLockedTargetsWithShipAndSkills => (int)Attributes.TryGet<double>("maxLockedTargets");

        /// <summary>
        ///     The maxmimum amount of shields
        /// </summary>
        public double MaxShield => Attributes.TryGet<double>("shieldCapacity");

        /// <summary>
        ///     The maximum amount of structure
        /// </summary>
        public double MaxStructure => Attributes.TryGet<double>("hp");

        /// <summary>
        ///     The maximum target range
        /// </summary>
        public double MaxTargetRange => Attributes.TryGet<double>("maxTargetRange");

        /// <summary>
        ///     Maximum velocity
        /// </summary>
        public double MaxVelocity => Attributes.TryGet<double>("maxVelocity");

        private int _numOfRemoteArmorRepairers;

        public int NumOfRemoteArmorRepairers
        {
            get
            {
                //default to true
                if (DirectEve.Session.IsInSpace)
                {
                    if (DirectEve.Modules != null)
                    {
                        if (DirectEve.Modules.Any(i => i.IsRemoteArmorRepairModule))
                        {
                            _numOfRemoteArmorRepairers = DirectEve.Modules.Count(i => i.IsRemoteArmorRepairModule);
                            //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NumOfLargeRemoteArmorTransferModules), _numOfRemoteArmorRepairers);
                            return _numOfRemoteArmorRepairers;
                        }

                        return 0;
                    }

                    return 0;
                }

                return 0;
            }
        }

        private int _numOfRemoteShieldRepairers;

        public int NumOfRemoteShieldRepairers
        {
            get
            {
                //default to true
                if (DirectEve.Session.IsInSpace)
                {
                    if (DirectEve.Modules != null)
                    {
                        if (DirectEve.Modules.Any(i => i.IsRemoteShieldRepairModule))
                        {
                            _numOfRemoteShieldRepairers = DirectEve.Modules.Count(i => i.IsRemoteShieldRepairModule);
                            //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NumOfLargeRemoteShieldTransferModules), _numOfRemoteShieldRepairers);
                            return _numOfRemoteShieldRepairers;
                        }

                        return 0;
                    }

                    return 0;
                }

                return 0;
            }
        }

        private int _numOfRemoteHullRepairers;

        public int NumOfRemoteHullRepairers
        {
            get
            {
                //default to true
                if (DirectEve.Session.IsInSpace)
                {
                    if (DirectEve.Modules != null)
                    {
                        if (DirectEve.Modules.Any(i => i.IsRemoteHullRepairModule))
                        {
                            _numOfRemoteHullRepairers = DirectEve.Modules.Count(i => i.IsRemoteHullRepairModule);
                            //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.num), _numOfRemoteHullRepairers);
                            return _numOfRemoteHullRepairers;
                        }

                        return 0;
                    }

                    return 0;
                }

                return 0;
            }
        }

        private int _numOfRemoteEnergyTransferModules;

        public int NumOfRemoteEnergyTransferModules
        {
            get
            {
                //default to true
                if (DirectEve.Session.IsInSpace)
                {
                    if (DirectEve.Modules != null)
                    {
                        if (DirectEve.Modules.Any(i => i.IsRemoteEnergyTransferModule))
                        {
                            _numOfRemoteEnergyTransferModules = DirectEve.Modules.Count(i => i.IsRemoteEnergyTransferModule);
                            //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NumOfLargeRemoteEnergyTransferModules), _numOfRemoteEnergyTransferModules);
                            return _numOfRemoteEnergyTransferModules;
                        }

                        return 0;
                    }

                    return 0;
                }

                return 0;
            }
        }

        /// <summary>
        ///     Your current amount of shields
        /// </summary>
        public double Shield => Attributes.TryGet<double>("shieldCharge");

        /// <summary>
        ///     Shield percentage
        /// </summary>
        private double? _shieldPercentage = null;

        public double ShieldPercentage
        {
            get
            {
                if (_shieldPercentage != null)
                    return _shieldPercentage ?? 0;

                _shieldPercentage = Math.Round(Math.Abs(Shield / MaxShield * 100), 1);
                return _shieldPercentage ?? 0;
            }
        }

        /// <summary>
        ///     Your current amount of structure
        /// </summary>
        public double Structure => MaxStructure - Attributes.TryGet<double>("damage");

        /// <summary>
        ///     Structure percentage
        /// </summary>

        private double? _structurePercentage = null;

        public double StructurePercentage
        {
            get
            {
                if (_structurePercentage != null)
                    return _structurePercentage ?? 0;

                _structurePercentage = Math.Round(Math.Abs(Structure / MaxStructure * 100), 1);
                return _structurePercentage ?? 0;
            }
        }

        #endregion Properties

        #region Methods

        public bool CanGroupAll()
        {
            var dogmaLocation = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation");
            if (dogmaLocation.IsValid)
            {
                if ((bool)dogmaLocation.Call("CanGroupAll", DirectEve.Session.ShipId))
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        public float GetDroneControlRange()
        {
            return DirectEve.GetLiveAttribute<float>(DirectEve.Session.CharacterId.Value, DirectEve.Const.AttributeDroneControlDistance.ToInt());
        }

        public float GetShipsAgility(bool live = true)
        {
            if (!live)
                return this.TryGet<float>("agility");


            return DirectEve.GetLiveAttribute<float>(this.ItemId, DirectEve.Const.AttributeAgility.ToInt());
        }

        public float GetShipsMass(bool live = true)
        {
            if (!live)
                return this.TryGet<float>("mass");

            //DirectEve.Log($"id [{this.ItemId}] attribute id [{DirectEve.Const.AttributeMass.ToInt()}] ShipId [{DirectEve.Session.ShipId}]");
            return DirectEve.GetLiveAttribute<float>(this.ItemId, DirectEve.Const.AttributeMass.ToInt());
        }

        private double? _getLiveSignatureRadius;

        public double GetSignatureRadius(bool live = true)
        {
            if (!_getLiveSignatureRadius.HasValue && live)
            {
                _getLiveSignatureRadius = DirectEve.GetLiveAttribute<float>(this.ItemId, DirectEve.Const.AttributeSignatureRadius.ToInt());
            }

            if (!live)
                _getLiveSignatureRadius = this.SignatureRadius;

            return _getLiveSignatureRadius.Value;
        }


        public float GetMaxVelocityBase(bool live = true)
        {
            if (!live)
                return this.TryGet<float>("maxVelocity");

            //DirectEve.Log($"id [{this.ItemId}] attribute id [{DirectEve.Const.AttributeMass.ToInt()}] ShipId [{DirectEve.Session.ShipId}]");
            return DirectEve.GetLiveAttribute<float>(this.ItemId, DirectEve.Const.AttributeMaxVelocity.ToInt());
        }

        public float GetMaxVelocityWithPropMod()
        {
            var baseValue = GetMaxVelocityBase();
            var speedFactorAddtion = 0f;
            var thrust = 0f;
            var massAddition = 0f;
            if (DirectEve.Modules.Any(m => m.GroupId == (int)Group.Afterburner && m.IsOnline))
            {
                foreach (var mod in DirectEve.Modules.Where(m => m.GroupId == (int)Group.Afterburner && m.IsOnline))
                {
                    var mA = DirectEve.GetLiveAttribute<float>(mod.ItemId, (int)DirectEve.Const.AttributeSpeedFactor);
                    if (mA > speedFactorAddtion)
                    {
                        speedFactorAddtion = mA;
                        thrust = DirectEve.GetLiveAttribute<float>(mod.ItemId, (int)DirectEve.Const.AttributeSpeedBoostFactor);
                        massAddition = mod.TryGet<float>("massAddition");
                    }
                }
                float veloRatio = 1f + thrust * (float)speedFactorAddtion * 0.01f / (GetShipsMass() + massAddition);
                return baseValue * veloRatio;
            }
            return baseValue;
        }

        public double GetSecondsToWarp(double warpPerc = 0.75d)
        {
            var agility = GetShipsAgility();
            var mass = GetShipsMass();
            var secondsToWarp = agility * mass * Math.Pow(10, -6) * -Math.Log(1 - warpPerc / 1);
            return secondsToWarp;
        }

        public double GetSecondsToWarpWithPropMod(double warpPerc = 0.75d)
        {
            var massAddition = 0d;
            if (DirectEve.Modules.Any(m => m.GroupId == (int)Group.Afterburner && m.IsOnline))
            {
                foreach (var mod in DirectEve.Modules.Where(m => m.GroupId == (int)Group.Afterburner && m.IsOnline))
                {
                    var mA = mod.TryGet<float>("massAddition");
                    if (mA > massAddition)
                    {
                        massAddition = mA;
                    }
                }
                var agility = GetShipsAgility();
                var mass = GetShipsMass() + massAddition;
                var secondsToWarp = agility * mass * Math.Pow(10, -6) * -Math.Log(1 - warpPerc / 1);

                return secondsToWarp;
            }
            return GetSecondsToWarp(warpPerc);
        }

        private int ModuleNumber = 0;

        public bool DeactivateCloak()
        {
            if (DateTime.UtcNow < Time.Instance.NextActivateModules)
                return false;

            ModuleNumber = 0;
            foreach (DirectUIModule cloak in _directEve.Modules.Where(i => i.GroupId == (int)Group.CloakingDevice))
            {
                if (!cloak.IsActivatable)
                    continue;

                ModuleNumber++;
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("[" + ModuleNumber + "][" + cloak.TypeName + "] TypeID [" + cloak.TypeId +
                                  "] GroupId [" +
                                  cloak.GroupId + "] Activatable [" + cloak.IsActivatable + "] Found");

                if (cloak.IsInLimboState)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + cloak.TypeName +
                                      "] is in LimboState (likely being activated or decativated already)");
                    return false;
                }

                if (cloak.IsActive)
                {
                    if (cloak.Click())
                    {
                        Time.Instance.NextActivateModules = DateTime.UtcNow;
                        Log.WriteLine("[" + ModuleNumber + "][" + cloak.TypeName + "] deactivated so we can do something that requires being decloaked.");
                        return true;
                    }

                    return false;
                }

                return true;
            }

            return true;
        }

        /// <summary>
        /// 0 ... 1.0 range
        /// </summary>
        /// <returns></returns>
        public float LowHeatRackState()
        {
            var heatStates = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Call("GetCurrentShipHeatStates").ToDictionary<int>();
            var attr = DirectEve.Const["attributeHeatLow"].ToInt();
            if (heatStates.ContainsKey(attr))
            {
                return heatStates[attr].ToFloat();
            }

            return 0;
        }

        /// <summary>
        /// 0 ... 1.0 range
        /// </summary>
        /// <returns></returns>
        public bool SetSpeedFraction(float fraction)
        {
            if (Entity == null)
                return false;

            if (!DirectEve.Interval(1900, 3500))
                return false;

            if (IsImmobile)
                return false;

            double diff = 0.03d;

            if (Math.Abs(fraction - Entity.SpeedFraction) >= diff)
            {
                var cmd = DirectEve.GetLocalSvc("cmd");
                if (cmd.HasAttrString("SetSpeedFraction"))
                {
                    DirectEve.ThreadedCall(cmd["SetSpeedFraction"], fraction);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 0 ... 1.0 range
        /// </summary>
        /// <returns></returns>
        public float MedHeatRackState()
        {
            var heatStates = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Call("GetCurrentShipHeatStates").ToDictionary<int>();
            var attr = DirectEve.Const["attributeHeatMed"].ToInt();
            if (heatStates.ContainsKey(attr))
            {
                return heatStates[attr].ToFloat();
            }

            return 0;
        }

        /// <summary>
        /// 0 ... 1.0 range
        /// </summary>
        /// <returns></returns>
        public float HighHeatRackState()
        {
            var heatStates = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Call("GetCurrentShipHeatStates").ToDictionary<int>();
            var attr = DirectEve.Const["attributeHeatHi"].ToInt();
            if (heatStates.ContainsKey(attr))
            {
                return heatStates[attr].ToFloat();
            }

            return 0;
        }

        public bool IsHaulingShip
        {
            get
            {
                //if (!IsValidShipToUse)
                //    return false;

                if (GroupId == (int)Group.JumpFreighter)
                    return false;

                if (GroupId == (int)Group.Freighter)
                    return false;

                if (!string.IsNullOrEmpty(GivenName) && !string.IsNullOrEmpty(Settings.Instance.TransportShipName) && GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower())
                    return true;

                if (GroupId == (int)Group.TransportShip)
                    return true;

                //if (GroupId == (int)Group.hauler)
                //    return true;

                return false;
            }
        }

        public bool SelfDestructShip()
        {
            if (IsImmobile)
                return false;

            if (DirectEve.ThreadedLocalSvcCall("menu", "SelfDestructShip", ItemId))
            {
                return true;
            };

            return false;
        }

        /// <summary>
        ///     Eject from your current ship
        /// </summary>
        /// <returns></returns>
        public bool EjectFromShip()
        {
            if (ESCache.Instance.InStation)
            {
                Log.WriteLine("Use LeaveShip when in station not EjectFromShip!");
                return false;
            }

            if (IsImmobile)
                return false;

            var Eject = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("Eject");
            if (Eject.IsValid)
            {
                return DirectEve.ThreadedCall(Eject);
            }

            return false;
        }

        public enum ShipConfigOption
        {
            FleetHangar_AllowFleetAccess,
            FleetHangar_AllowCorpAccess,
            SMB_AllowFleetAccess,
            SMB_AllowCorpAccess
        }

        public bool? GetShipConfigOption(ShipConfigOption option)
        {
            var shipConfigSvc = DirectEve.GetLocalSvc("shipConfig");


            if (!shipConfigSvc.IsValid || !shipConfigSvc.HasAttrString("config"))
            {
                DirectEve.Log("Error: ShipConfigSvc has no attribute 'config'.");
                return null;
            }

            var config = DirectEve.GetLocalSvc("shipConfig")["config"].ToDictionary<string>();

            if (config.ContainsKey(option.ToString()))
            {
                return config[option.ToString()].ToBool();
            }

            return null;
        }
        /// <summary>
        /// Remote call!
        /// </summary>
        /// <param name="option"></param>
        public bool ToggleShipConfigOption(ShipConfigOption option)
        {
            var shipConfigSvc = DirectEve.GetLocalSvc("shipConfig");

            if (!shipConfigSvc.IsValid)
            {
                DirectEve.Log("Error: ShipConfig Svc is not valid!");
                return false;
            }

            if (!DirectEve.Interval(1500, 2000))
                return false;

            switch (option)
            {
                case ShipConfigOption.FleetHangar_AllowFleetAccess:
                    DirectEve.ThreadedCall(shipConfigSvc["ToggleFleetHangarFleetAccess"]);
                    break;
                case ShipConfigOption.FleetHangar_AllowCorpAccess:
                    DirectEve.ThreadedCall(shipConfigSvc["ToggleFleetHangarCorpAccess"]);
                    break;
                case ShipConfigOption.SMB_AllowFleetAccess:
                    DirectEve.ThreadedCall(shipConfigSvc["ToggleShipMaintenanceBayFleetAccess"]);
                    break;
                case ShipConfigOption.SMB_AllowCorpAccess:
                    DirectEve.ThreadedCall(shipConfigSvc["ToggleShipMaintenanceBayCorpAccess"]);
                    break;
            }

            return true;
        }


        /// <summary>
        ///     Groups all weapons if possible
        /// </summary>
        /// <returns>Fails if it's not allowed to group (because there is nothing to group)</returns>
        /// <remarks>Only works in space</remarks>
        public bool GroupAllWeapons()
        {
            if (!CanGroupAll())
                 return false;

            var dogmaLocation = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation");
            if (dogmaLocation.IsValid)
            {
                return DirectEve.ThreadedCall(dogmaLocation.Attribute("LinkAllWeapons"), DirectEve.Session.ShipId.Value);
            }

            return false;
        }

        internal static Dictionary<long, DateTime> _droneReturnModeCache = new Dictionary<long, DateTime>();

        public bool ReturnDronesToBay(List<long> droneIds)
        {
            var activeDrones = DirectEve.ActiveDrones.ToList();
            droneIds = droneIds.Distinct().ToList();

            foreach (var id in droneIds.ToList())
            {
                if (!activeDrones.Any(e => e.Id == id))
                {
                    droneIds.Remove(id);
                    continue;
                }

                var d = activeDrones.FirstOrDefault(e => e.Id == id);

                if (d?.DroneState == (int)Drones.DroneState.Returning)
                    droneIds.Remove(id);
            }

            if (!droneIds.Any())
                return false;

            if (!DirectEve.Interval(1200, 1900))
                return false;

            foreach (var id in droneIds)
            {
                _droneReturnModeCache[id] = DateTime.UtcNow;
            }

            var ret = DirectEve.ThreadedLocalSvcCall("menu", "ReturnToDroneBay", droneIds);
            return ret;
        }


        public int GetRemainingDroneBandwidth()
        {
            var droneBay = DirectEve.GetShipsDroneBay();

            if (droneBay == null)
                return 0;

            //var maxDrones = DirectEve.Me.MaxActiveDrones;
            var currentShipsDroneBandwidth = DirectEve.ActiveShip.DroneBandwidth;
            var activeDrones = DirectEve.ActiveDrones;

            var activeDronesBandwidth = activeDrones.Sum(d => (int)d.TryGet<double>("droneBandwidthUsed"));
            activeDronesBandwidth = activeDronesBandwidth < 0 ? 0 : activeDronesBandwidth;

            var remainingDronesBandwidth = currentShipsDroneBandwidth - activeDronesBandwidth;
            remainingDronesBandwidth = remainingDronesBandwidth < 0 ? 0 : remainingDronesBandwidth;

            return remainingDronesBandwidth;
        }

        public Vec3? MoveToRandomDirection()
        {
            if (!DirectEve.Interval(1500, 2500))
                return null;

            var minimum = 0.1d;
            var maximum = 0.9d;
            var x = (_rnd.NextDouble() * (maximum - minimum) + minimum) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            var y = (_rnd.NextDouble() * (maximum - minimum) + minimum) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            var z = (_rnd.NextDouble() * (maximum - minimum) + minimum) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            Vec3 dir = new Vec3(x, y, z).Normalize();
            MoveTo(x, y, z);
            return dir;
        }

        public int DroneBandwidthUsed
        {
            get
            {
                DirectItem droneDirectItem = new DirectItem(DirectEve);
                int _droneBandwidthUsed = 0;
                if (DirectEve.ActiveDrones.Count > 0)
                {
                    droneDirectItem.TypeId = DirectEve.ActiveDrones.FirstOrDefault().TypeId;
                    _droneBandwidthUsed = DirectEve.ActiveDrones.Sum(d => (int)droneDirectItem.Attributes.TryGet<double>("droneBandwidthUsed"));
                    _droneBandwidthUsed = _droneBandwidthUsed < 0 ? 0 : _droneBandwidthUsed;
                }

                return _droneBandwidthUsed;
            }
        }

        public int DroneBandwidthLeft
        {
            get
            {
                if (DroneBandwidth > 0)
                {
                    return DroneBandwidth - DroneBandwidthUsed;
                }

                return 0;
            }
        }

        /// <summary>
        ///     Launch all drones
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Only works in space
        /// </remarks>
        public bool LaunchDrones(int? typeId = null)
        {
            var droneBay = DirectEve.GetShipsDroneBay();

            if (droneBay == null)
                return false;

            if (!DirectEve.HasFrameChanged())
                return false;

            var maxDrones = DirectEve.Me.MaxActiveDrones;
            var currentShipsDroneBandwidth = DirectEve.ActiveShip.DroneBandwidth;
            var activeDrones = DirectEve.ActiveDrones;

            var activeDronesBandwidth = activeDrones.Sum(d => (int)d.TryGet<double>("droneBandwidthUsed"));
            activeDronesBandwidth = activeDronesBandwidth < 0 ? 0 : activeDronesBandwidth;

            var remainingDronesBandwidth = currentShipsDroneBandwidth - activeDronesBandwidth;
            remainingDronesBandwidth = remainingDronesBandwidth < 0 ? 0 : remainingDronesBandwidth;

            var remainingDrones = maxDrones - activeDrones.Count;
            remainingDrones = remainingDrones < 0 ? 0 : remainingDrones;

            if (!droneBay.Items.Any())
                return false;

            if (activeDrones.Count >= 5)
                return false;

            //			DirectEve.Log("remainingDrones: " + remainingDrones);
            //			DirectEve.Log("remainingDronesBandwidth: " + remainingDronesBandwidth);

            var dronesToLaunch = new List<DirectItem>();

            var drones = typeId == null
                ? droneBay.Items.OrderByDescending(d => d.Stacksize)
                : droneBay.Items.Where(d => d.TypeId == typeId).OrderByDescending(d => d.Stacksize);

            if (drones.Count() >= remainingDrones)
                foreach (var d in drones.RandomPermutation())
                {
                    var bandwidth = (int)d.TryGet<double>("droneBandwidthUsed");

                    if (remainingDronesBandwidth - bandwidth >= 0 && remainingDrones - 1 >= 0)
                    {
                        remainingDrones--;
                        remainingDronesBandwidth = remainingDronesBandwidth - bandwidth;
                        dronesToLaunch.Add(d);
                    }
                    else
                    {
                        break;
                    }
                }
            else
                dronesToLaunch = typeId == null ? droneBay.Items.OrderByDescending(d => d.Stacksize).ToList() :
                    droneBay.Items.Where(d => d.TypeId ==  typeId).OrderByDescending(d => d.Stacksize).ToList();

            if (dronesToLaunch.Any(d => d.Stacksize > 1))
                dronesToLaunch = dronesToLaunch.Where(d => d.Stacksize > 1).ToList();

            dronesToLaunch = dronesToLaunch.RandomPermutation().ToList();

            return LaunchDrones(dronesToLaunch);
        }

        /// <summary>
        ///     Launch a specific list of drones
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Only works in space
        /// </remarks>
        public bool LaunchDrones(IEnumerable<DirectItem> drones)
        {
            if (!drones.Any())
                return false;

            if (!DirectEve.Interval(4000, 4500))
                return false;

            drones = drones.Where(e => e.ItemId > 0).OrderBy(e => e.ItemId).ToList();

            var invItems = drones.Where(d => d.PyItem.IsValid).Select(d => d.PyItem);
            return DirectEve.ThreadedLocalSvcCall("menu", "LaunchDrones", invItems);
        }


        private static Random _rnd = new Random();

        private static int _preventedSameDirectionCount = 0;
        private int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        /// <summary>
        /// This is a directional vector!
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public bool MoveTo(double x, double y, double z, bool doNotRandomize = false, bool ignoreInterval = false)
        {
            var unitVecInput = new Vec3(x, y, z).Normalize();

            x = unitVecInput.X;
            y = unitVecInput.Y;
            z = unitVecInput.Z;

            double minPerc = 0.01d;
            double maxPerc = 0.03d;

            if (doNotRandomize)
            {
                minPerc = 0.001d;
                maxPerc = 0.003d;
            }

            var currentDirectionVector = Entity.GetDirectionVectorFinal();

            //DirectEve.SceneManager.ClearDebugLines();
            DirectEve.SceneManager.DrawLineGradient(new Vec3(0, 0, 0), currentDirectionVector.Scale(15000), new System.Numerics.Vector4(1, 0, 1, 1), new System.Numerics.Vector4(0, 0.5f, 0.5f, 0.5f));

            if (currentDirectionVector != null)
            {

                //DirectEve.Log($"currentDirectionVector {currentDirectionVector}");
                //DirectEve.Log($"XAbs [{Math.Abs(x - currentDirectionVector.X)}] YAbs [{Math.Abs(y - currentDirectionVector.Y)}] ZAbs [{Math.Abs(z - currentDirectionVector.Z)}]");
                if (
                    _preventedSameDirectionCount <= 3
                    && Math.Abs(x - currentDirectionVector.X) <= maxPerc
                    && Math.Abs(y - currentDirectionVector.Y) <= maxPerc
                    && Math.Abs(z - currentDirectionVector.Z) <= maxPerc
                    )
                {
                    if (DirectEve.Interval(3000, 6000))
                        _preventedSameDirectionCount++;
                    if (DirectEve.Interval(5000))
                    {
                        Log.WriteLine($"-- MoveTo skpping. We are already moving into that direction. _preventedSameDirectionCount {_preventedSameDirectionCount}");

                    }
                    return false;
                }
            }
            else
            {
                DirectEve.Log($"Warning: currentDirectionVector == null");
            }
            _preventedSameDirectionCount = 0;

            if (Math.Abs(x) < minPerc)
                x = (_rnd.NextDouble() * (maxPerc - minPerc) + minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1); // x = 0.01 ... 0.03

            if (Math.Abs(y) < minPerc)
                y = (_rnd.NextDouble() * (maxPerc - minPerc) + minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);  // y = 0.01 ... 0.03

            if (Math.Abs(z) < minPerc)
                z = (_rnd.NextDouble() * (maxPerc - minPerc) + minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1); // z = 0.01 ... 0.03

            x = x + (_rnd.NextDouble() * (x * maxPerc - x * minPerc) + x * minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            y = y + (_rnd.NextDouble() * (y * maxPerc - y * minPerc) + y * minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            z = z + (_rnd.NextDouble() * (z * maxPerc - z * minPerc) + z * minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);

            if (IsImmobile)
                return false;

            if (!ignoreInterval && !DirectEve.Interval(1500, 3100))
                return false;

            var unitVec = new Vec3(x, y, z).Normalize();

            if (!unitVec.IsUnitVector(0.00001d))
            {
                Console.WriteLine("Error: MoveTo -- Is not a unit vector");
                DirectEve.Log("Error: MoveTo -- Is not a unit vector");
                return false;
            }

            Log.WriteLine($"-- Framework MoveTo [{unitVec}].");

            if(doNotRandomize)
                return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("michelle").Call("GetRemotePark").Attribute("CmdGotoDirection"), unitVecInput.X, unitVecInput.Y, unitVecInput.Z);

            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("michelle").Call("GetRemotePark").Attribute("CmdGotoDirection"), unitVec.X, unitVec.Y, unitVec.Z);
        }

        public bool MoveTo(Vec3 thisVec3)
        {
            var a = DirectEve.ActiveShip.Entity;

            if (!a.IsXYZCoordValid)
            {
                return false;
            }

            return MoveTo((double)thisVec3.X - (double)a.XCoordinate, (double)thisVec3.Y - (double)a.YCoordinate, (double)thisVec3.Z - (double)a.ZCoordinate);
        }

        public bool MoveTo(DirectWorldPosition CoordTo)
        {
            var a = DirectEve.ActiveShip.Entity;

            if (!a.IsXYZCoordValid)
            {
                return false;
            }

            return MoveTo((double)CoordTo.XCoordinate - (double)a.XCoordinate, (double)CoordTo.YCoordinate - (double)a.YCoordinate, (double)CoordTo.ZCoordinate - (double)a.ZCoordinate);
        }

        public bool MoveTo(DirectEntity b)
        {
            var a = DirectEve.ActiveShip.Entity;

            if (!a.IsXYZCoordValid)
            {
                return false;
            }

            if (MoveTo((double)b.XCoordinate - (double)a.XCoordinate, (double)b.YCoordinate - (double)a.YCoordinate, (double)b.ZCoordinate - (double)a.ZCoordinate))
            {
                ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                return true;
            }

            return false;
        }

        /**
        public bool MoveTo(DirectWorldPosition coord)
        {
            if (!DirectEve.Interval(3000, 4000))
                return false;

            ////Create unit length
            double length = Math.Sqrt((coord.XCoordinate * coord.XCoordinate) + (coord.YCoordinate * coord.YCoordinate) + (coord.ZCoordinate * coord.ZCoordinate));
            if (length == 0)
                return false;

            double X = coord.XCoordinate / length;
            double Y = coord.YCoordinate / length;
            double Z = coord.ZCoordinate / length;

            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("michelle").Call("GetRemotePark").Attribute("CmdGotoDirection"), X, Y, Z);
        }
        **/

        public bool SetSpeed(double SpeedToSet) //1 is full speed, .5 would be 1/2 speed
        {
            try
            {
                if (!DirectEve.Interval(1900, 3200))
                    return false;

                if (SpeedToSet > 1)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("if (SpeedToSet[" + SpeedToSet + "] > 1) SpeedToSet = 1;");
                    SpeedToSet = 1;
                }

                if (SpeedToSet == 0)
                {
                    if (DebugConfig.DebugNavigateOnGrid)Log.WriteLine("if (SpeedToSet == 0) SpeedToSet = 1;");
                    SpeedToSet = 1;
                }

                if (Entity.Velocity == SpeedToSet)
                    return true;

                if (((SpeedToSet / Entity.Velocity) > .85) && ((SpeedToSet / Entity.Velocity) < 1.15))
                    return true;

                if (DirectEve.LastSetSpeedSpeedToSet == SpeedToSet && Entity.Velocity > 0)
                    return true;

                var pySharp = DirectEve.PySharp;
                var carbonui = pySharp.Import("carbonui");

                //var pyShipUI = carbonui.Attribute("uicore")
                //    .Attribute("uicore")
                //    .Attribute("layer")
                //    .Attribute("shipui");

                DirectEve.Layers.ShipUILayer.Call("SetSpeed", SpeedToSet);
                Logging.Log.WriteLine("Ship Speed set to [" + Math.Round(SpeedToSet * MaxVelocity, 0) + " m/s] MaxVelocity is [" + Math.Round(MaxVelocity, 0) + "]");
                DirectEve.LastSetSpeedSpeedToSet = SpeedToSet;
                Time.Instance.LastSetSpeed = DateTime.UtcNow;
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        /// <summary>
        ///     Strips active ship, use only in station!
        /// </summary>
        /// <returns></returns>
        public bool StripFitting()
        {
            string calledFrom = "StripFitting";
            if (!DirectEve.NoLockedItemsOrWaitAndClearLocks(calledFrom)) return false;
            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("menu").Attribute("invCache").Call("GetInventoryFromId", ItemId).Attribute("StripFitting"));
        }

        /// <summary>
        ///     Ungroups all weapons
        /// </summary>
        /// <returns>
        ///     Fails if anything can still be grouped. Execute GroupAllWeapons first if not everything is grouped, this is
        ///     done to mimic client behavior.
        /// </returns>
        /// <remarks>Only works in space</remarks>
        public bool UngroupAllWeapons()
        {
            var dogmaLocation = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation");
            var canGroupAll = (bool)dogmaLocation.Call("CanGroupAll", DirectEve.Session.ShipId.Value);
            if (canGroupAll)
                return false;

            return DirectEve.ThreadedCall(dogmaLocation.Attribute("UnlinkAllWeapons"), DirectEve.Session.ShipId.Value);
        }

        #endregion Methods
    }
}