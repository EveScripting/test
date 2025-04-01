//
// (c) duketwo 2022
//

extern alias SC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.EVE.DatabaseSchemas;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.SQLite;
using SC::SharedComponents.Utility;
using ServiceStack.OrmLite;
using SharpDX.Direct2D1;


namespace EVESharpCore.Controllers.Abyssal
{
    public partial class AbyssalController : AbyssalBaseController
    {

        internal DateTime _nextOverheatDisablePropMod = DateTime.MinValue;
        internal DateTime _nextOverheatDisableShieldHardner = DateTime.MinValue;
        internal DateTime _nextOverheatDisableShieldBooster = DateTime.MinValue;

        internal AbyssStatEntry _abyssStatEntry;
        internal DateTime _lastDrugUsage = DateTime.MinValue;

        internal double _currentStageMaximumEhp = 0;
        internal double _currentStageCurrentEhp = 0;

        private DateTime _lastAbyssLogState;



        private bool UpdateTimeLastNPCWasKilled()
        {
            try
            {
                if (AllRatsHaveBeenCleared && !Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    switch (CurrentAbyssalStage)
                    {
                        case AbyssalStage.Stage1:
                            if (DateTime.UtcNow > stage1TimeLastNPCWasKilled.AddMinutes(10))
                            {
                                stage1TimeLastNPCWasKilled = DateTime.UtcNow;
                                Log("_stage1TimeLastNPCWasKilled [" + stage1TimeLastNPCWasKilled.ToLongTimeString() + "]");
                            }

                            break;
                        case AbyssalStage.Stage2:
                            if (DateTime.UtcNow > stage2TimeLastNPCWasKilled.AddMinutes(10))
                            {
                                stage2TimeLastNPCWasKilled = DateTime.UtcNow;
                                Log("_stage2TimeLastNPCWasKilled [" + stage2TimeLastNPCWasKilled.ToLongTimeString() + "]");
                            }

                            break;
                        case AbyssalStage.Stage3:
                            if (DateTime.UtcNow > stage3TimeLastNPCWasKilled.AddMinutes(10))
                            {
                                stage3TimeLastNPCWasKilled = DateTime.UtcNow;
                                Log("_stage3TimeLastNPCWasKilled [" + stage3TimeLastNPCWasKilled.ToLongTimeString() + "]");
                            }

                            break;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        internal void LogPriorityCombatTargetInfo()
        {
            try
            {
                Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] DetectDungeon [" + AbyssalSpawn.DetectDungeon + "] AbyssalWeather [" + AbyssalSpawn.AbyssalWeather + "]");
                if (Combat.CurrentWeaponTarget() == null)
                {
                    Log("[0] CurrentTarget [ None ]");
                }
                else
                {
                    Log("[0] CurrentTarget [" + Combat.CurrentWeaponTarget().TypeName + "] Priority [" + Combat.CurrentWeaponTarget()._directEntity.AbyssalTargetPriority + "][" + Combat.CurrentWeaponTarget().Nearest1KDistance + "k] ID [" + Combat.CurrentWeaponTarget().MaskedId + "] S[" + Math.Round(Combat.CurrentWeaponTarget().ShieldPct * 100, 0) + "%] A[" + Math.Round(Combat.CurrentWeaponTarget().ArmorPct * 100, 0) + "%] H[" + Math.Round(Combat.CurrentWeaponTarget().StructurePct * 100, 0) + "%] Locked [" + Combat.CurrentWeaponTarget().IsTarget + "] IsAttacking [" + Combat.CurrentWeaponTarget()._directEntity.IsAttacking + "] IsTargetedBy [" + Combat.CurrentWeaponTarget()._directEntity.IsTargetedBy + "] IsYellowBoxing [" + Combat.CurrentWeaponTarget()._directEntity.IsYellowBoxing + "] IsInSpeedCloud [" + Combat.CurrentWeaponTarget()._directEntity.IsInSpeedCloud + "][" + Combat.CurrentWeaponTarget().stringEwarTypes + "] Shield Resists: EM [" + Combat.CurrentWeaponTarget().ShieldResistanceEm + "] TH [" + Combat.CurrentWeaponTarget().ShieldResistanceThermal + "] KI [" + Combat.CurrentWeaponTarget().ShieldResistanceKinetic + "] EX [" + Combat.CurrentWeaponTarget().ShieldResistanceExplosive + "] Armor Resists: EM [" + Combat.CurrentWeaponTarget().ArmorResistanceEm + "] TH [" + Combat.CurrentWeaponTarget().ArmorResistanceThermal + "] KI [" + Combat.CurrentWeaponTarget().ArmorResistanceKinetic + "] EX [" + Combat.CurrentWeaponTarget().ArmorResistanceExplosive + "]");
                }
                int intCount = 0;

                //locked targets
                foreach (var pct in sortedListOfTargets)
                {
                    intCount++;
                    Log("[" + intCount + "] Locked [" + pct.TypeName + "] Priority [" + pct._directEntity.AbyssalTargetPriority + "][" + pct.Nearest1KDistance + "k] Velocity [" + Math.Round(pct.Velocity, 0) + "m/s] Optimal [" + pct.OptimalRange + "] Falloff [" + pct._directEntity.AccuracyFalloff + "] IsInOptimalRangeOfMe [" + pct._directEntity.IsInNPCsOptimalRange + "] ID [" + pct.MaskedId + "] S[" + Math.Round(pct.ShieldPct * 100, 0) + "%] A[" + Math.Round(pct.ArmorPct * 100, 0) + "%] H[" + Math.Round(pct.StructurePct * 100, 0) + "%] S[" + Math.Round(pct.ShieldCurrentHitPoints, 0) + "] A[" + Math.Round(pct.ArmorCurrentHitPoints, 0) + "] H[" + Math.Round(pct.StructureCurrentHitPoints, 0) + "] Locked [" + pct.IsTarget + "] IsAttacking [" + pct._directEntity.IsAttacking + "] IsTargetedBy [" + pct._directEntity.IsTargetedBy + "] IsYellowBoxing [" + pct._directEntity.IsYellowBoxing + "] IsInSpeedCloud [" + pct._directEntity.IsInSpeedCloud + "][" + pct.stringEwarTypes + "] IsNPCFrigate [" + pct.IsNPCFrigate + "] IsNPCDestroyer [" + pct.IsNPCDestroyer + "] IsNPCCruiser [" + pct.IsNPCCruiser + "] IsNPCBattlecruiser [" + pct.IsNPCBattlecruiser + "] IsNPCBattleship [" + pct.IsNPCBattleship + "] IsTooCloseToSmallDeviantAutomataSuppressor [" + pct._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor + "] IsTooCloseToMediumDeviantAutomataSuppressor [" + pct._directEntity.IsTooCloseToMediumDeviantAutomataSuppressor + "] Shield Resists: EM [" + pct.ShieldResistanceEm + "] TH [" + pct.ShieldResistanceThermal + "] KI [" + pct.ShieldResistanceKinetic + "] EX [" + pct.ShieldResistanceExplosive + "] Armor Resists: EM [" + pct.ArmorResistanceEm + "] TH [" + pct.ArmorResistanceThermal + "] KI [" + pct.ArmorResistanceKinetic + "] EX [" + pct.ArmorResistanceExplosive + "]");
                }
                intCount = 0;
                foreach (var pct in Combat.PotentialCombatTargets.OrderBy(i => i._directEntity.AbyssalTargetPriority))
                {
                    intCount++;
                    Log("[" + intCount + "][" + pct.TypeName + "] Priority [" + pct._directEntity.AbyssalTargetPriority + "][" + pct.Nearest1KDistance + "k] Velocity [" + Math.Round(pct.Velocity, 0) + "m/s] Optimal [" + pct.OptimalRange + "] Falloff [" + pct._directEntity.AccuracyFalloff + "] IsInOptimalRangeOfMe [" + pct._directEntity.IsInNPCsOptimalRange + "] ID [" + pct.MaskedId + "] S[" + Math.Round(pct.ShieldPct * 100, 0) + "%] A[" + Math.Round(pct.ArmorPct * 100, 0) + "%] H[" + Math.Round(pct.StructurePct * 100, 0) + "%] S[" + Math.Round(pct.ShieldCurrentHitPoints, 0) + "] A[" + Math.Round(pct.ArmorCurrentHitPoints, 0) + "] H[" + Math.Round(pct.StructureCurrentHitPoints, 0) + "] Locked [" + pct.IsTarget + "] IsAttacking [" + pct._directEntity.IsAttacking + "] IsTargetedBy [" + pct._directEntity.IsTargetedBy + "] IsYellowBoxing [" + pct._directEntity.IsYellowBoxing + "] IsInSpeedCloud [" + pct._directEntity.IsInSpeedCloud + "][" + pct.stringEwarTypes + "] IsNPCFrigate [" + pct.IsNPCFrigate + "] IsNPCDestroyer [" + pct.IsNPCDestroyer + "] IsNPCCruiser [" + pct.IsNPCCruiser + "] IsNPCBattlecruiser [" + pct.IsNPCBattlecruiser + "] IsNPCBattleship [" + pct.IsNPCBattleship + "] IsTooCloseToSmallDeviantAutomataSuppressor [" + pct._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor + "] IsTooCloseToMediumDeviantAutomataSuppressor [" + pct._directEntity.IsTooCloseToMediumDeviantAutomataSuppressor + "]! Shield Resists: EM [" + pct.ShieldResistanceEm + "] TH [" + pct.ShieldResistanceThermal + "] KI [" + pct.ShieldResistanceKinetic + "] EX [" + pct.ShieldResistanceExplosive + "] Armor Resists: EM [" + pct.ArmorResistanceEm + "] TH [" + pct.ArmorResistanceThermal + "] KI [" + pct.ArmorResistanceKinetic + "] EX [" + pct.ArmorResistanceExplosive + "]");
                }
            }
            catch (Exception ex)
            {
                Log($"{ex}");
            }
        }

        internal void LogFleetMemberInfo()
        {
            try
            {
                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    int intCount = 0;
                    foreach (var fleetMember in ESCache.Instance.EntitiesNotSelf.Where(i => i.GroupId == (int)Group.AssaultShip))
                    {
                        intCount++;
                        Log("[" + intCount + "][" + fleetMember.Name + "][" + fleetMember.Nearest1KDistance + "k] Velocity [" + Math.Round(fleetMember.Velocity, 0) + "m/s] S[" + Math.Round(fleetMember.ShieldPct * 100, 0) + "%] A[" + Math.Round(fleetMember.ArmorPct * 100, 0) + "%] H[" + Math.Round(fleetMember.StructurePct * 100, 0) + "%] IsTarget [" + fleetMember.IsTarget + "]");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"{ex}");
            }
        }

        internal bool boolDoWeCareAboutAutomataTowers
        {
            get
            {
                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.StormBringer)
                    return false;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Ishtar)
                    return true;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila)
                    return true;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.VexorNavyIssue)
                    return true;

                if (ESCache.Instance.Weapons.Any(i => i.IsMissileLauncher && !i._module.IsVortonProjector))
                {
                    return true;
                }

                return false;
            }
        }

        internal void LogAutomataSuppressorInfo()
        {
            try
            {
                int intCount = 0;
                if (boolDoWeCareAboutAutomataTowers)
                {
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceMediumDeviantAutomataSuppressor || i.IsAbyssalDeadspaceSmallDeviantAutomataSuppressor))
                    {
                        foreach (var tower in ESCache.Instance.EntitiesOnGrid.Where(i => i.IsAbyssalDeadspaceMediumDeviantAutomataSuppressor || i.IsAbyssalDeadspaceSmallDeviantAutomataSuppressor))
                        {
                            double rangeOfTower = 40000;
                            if (tower.IsAbyssalDeadspaceSmallDeviantAutomataSuppressor)
                            {
                                rangeOfTower = 20000;
                            }

                            if (rangeOfTower > tower.Distance)
                            {
                                intCount++;
                                Log("[" + intCount + "][" + tower.TypeName + "] at [" + tower.Nearest1KDistance + "k] RangeOfTower [" + Math.Round(rangeOfTower / 1000, 0) + "] Small [" + tower.IsAbyssalDeadspaceSmallDeviantAutomataSuppressor + "] Medium [" + tower.IsAbyssalDeadspaceMediumDeviantAutomataSuppressor + "]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"{ex}");
            }
        }

        internal void LogTrackingTowerInfo()
        {
            try
            {
                int intCount = 0;
                if (true)
                {
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceMultibodyTrackingPylon))
                    {
                        foreach (var trackingPylon in ESCache.Instance.EntitiesOnGrid.Where(i => i.IsAbyssalDeadspaceMultibodyTrackingPylon))
                        {
                            //we just want to log when we get too close to these: we are worried about running into the pylon itself NOT being in range of it!
                            double RangeOfTrackingPylon = 15000;

                            bool HurtingOurTracking = false;

                            if (RangeOfTrackingPylon > trackingPylon.Distance)
                            {
                                intCount++;
                                Log("[" + intCount + "][" + trackingPylon.TypeName + "] at [" + trackingPylon.Nearest1KDistance + "k] RangeOfTrackingPylon [" + HurtingOurTracking + "]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"{ex}");
            }
        }

        internal void LogWeaponState()
        {
            try
            {
                if (ESCache.Instance.Weapons.Any())
                {
                    Log($"Weapons [" + ESCache.Instance.Weapons.Count() + "]");
                    if (allDronesInSpace.Any())
                    {
                        foreach (var thisweapon in ESCache.Instance.Weapons.OrderByDescending(i => i.IsActive))
                        {
                            try
                            {
                                Log("[" + thisweapon.TypeName + "] Id[" + thisweapon.ItemId +
                                "] IsActive [" + thisweapon.IsActive +
                                "] IsOverloaded [" + thisweapon.IsOverloaded +
                                "] IsInLimboState [" + thisweapon.IsInLimboState +
                                "] IsActivatable [" + thisweapon.IsActivatable +
                                "] IsVortonProjector [" + thisweapon._module.IsVortonProjector +
                                "] TargetEntity [" + thisweapon.TargetEntityName +
                                "]");
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        internal void LogDroneState()
        {
            try
            {
                Log($"Drones InSpace [" + allDronesInSpace.Count() + "] Drones InBay [" + alldronesInBay.Count() + "]");
                if (allDronesInSpace.Any())
                {
                    foreach (var drone in allDronesInSpace.OrderByDescending(i => i._directEntity.Volume))
                    {
                        try
                        {
                            Log("InSpace [" + drone.TypeName + "]" +
                            "S[" + Math.Round(drone.ShieldPct * 100, 0) +
                            "] A[" + Math.Round(drone.ArmorPct * 100, 0) +
                            "] H[" + Math.Round(drone.StructurePct * 100, 0) +
                            "]  @ [" + drone.Nearest1KDistance +
                            "k] Id[" + drone.MaskedId +
                            "] IsLocatedWithinSpeedCloud (3x speed) [" + drone._directEntity.IsLocatedWithinSpeedCloud +
                            "] IsLocatedWithinBioluminescenceCloud (300% signature radius) [" + drone._directEntity.IsLocatedWithinBioluminescenceCloud +
                            "] IsLocatedWithinFilamentCloud (shield boost penalty) [" + drone._directEntity.IsLocatedWithinFilamentCloud +
                            "] FollowEntity [" + drone._directEntity.FollowEntityName + "]");
                        }
                        catch (Exception ex)
                        {
                            Log("Exception [" + ex + "]");
                        }
                    }
                }

                if (alldronesInBay.Any())
                {
                    foreach (var drone in alldronesInBay.OrderByDescending(i => i.Volume))
                    {
                        try
                        {
                            Log("InBay [" + drone.TypeName + "] ItemID [" + drone.ItemId + "]");
                        }
                        catch (Exception ex)
                        {
                            Log("Exception [" + ex + "]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        /// <summary>
        /// Prints information about the current abyss state every 30 seconds
        /// </summary>
        internal void LogAbyssState(bool force = false)
        {
            try
            {
                if (!DirectEve.Me.IsInAbyssalSpace())
                    return;

                if (!force && _lastAbyssLogState.AddSeconds(15) > DateTime.UtcNow)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        //fixme - todo
                    }

                    return;
                }

                _lastAbyssLogState = DateTime.UtcNow;

                try
                {
                    var ship = ESCache.Instance.DirectEve.ActiveShip;
                    var de = ESCache.Instance.DirectEve;
                    var droneRecalls = _abyssStatEntry != null ? CurrentAbyssalStage == AbyssalStage.Stage1 ? _abyssStatEntry.DroneRecallsStage1 : CurrentAbyssalStage == AbyssalStage.Stage2 ? _abyssStatEntry.DroneRecallsStage2 : _abyssStatEntry.DroneRecallsStage3 : -1;

                    var _stringStage1TimeLastNPCWasKilled = string.Empty;
                    var _stringStage2TimeLastNPCWasKilled = string.Empty;
                    var _stringStage3TimeLastNPCWasKilled = string.Empty;

                    if (stage1TimeLastNPCWasKilled.AddMinutes(21) > DateTime.UtcNow)
                        _stringStage1TimeLastNPCWasKilled = stage1TimeLastNPCWasKilled.ToLongTimeString();

                    if (stage2TimeLastNPCWasKilled.AddMinutes(21) > DateTime.UtcNow)
                        _stringStage2TimeLastNPCWasKilled = stage2TimeLastNPCWasKilled.ToLongTimeString();

                    if (stage3TimeLastNPCWasKilled.AddMinutes(21) > DateTime.UtcNow)
                        _stringStage3TimeLastNPCWasKilled = stage3TimeLastNPCWasKilled.ToLongTimeString();

                    var activeShip = ESCache.Instance.ActiveShip.Entity;
                    Log($"Spawn:   _stage1DetectSpawn [" + _stage1DetectSpawn + "] _stage2DetectSpawn [" + _stage2DetectSpawn + "] _stage3DetectSpawn [" + _stage3DetectSpawn + "]");
                    try
                    {
                        Log($"LastNPC: _stage1TimeLastNPCWasKilled [" + _stringStage1TimeLastNPCWasKilled + "]");
                        Log($"_stage2TimeLastNPCWasKilled [" + _stringStage1TimeLastNPCWasKilled + "]");
                        Log($"_stage3TimeLastNPCWasKilled [" + _stringStage1TimeLastNPCWasKilled + "]");
                        Log($"Wasted: __stage1SecondsWastedAfterLastNPCWasKilled [" + Math.Round(__stage1SecondsWastedAfterLastNPCWasKilled ?? 0, 0) + "]");
                        Log($"__stage2SecondsWastedAfterLastNPCWasKilled [" + Math.Round(__stage2SecondsWastedAfterLastNPCWasKilled ?? 0, 0) + "]");
                        Log($"__stage3SecondsWastedAfterLastNPCWasKilled [" + Math.Round(__stage3SecondsWastedAfterLastNPCWasKilled ?? 0, 0) + "]");
                    }
                    catch (Exception ex)
                    {
                        Log("Exception [" + ex + "]");
                    }

                    Log($"DetectSpawn [{AbyssalSpawn.DetectSpawn}] DetectDungeon [{AbyssalSpawn.DetectDungeon}] CurrentAbyssalStage [" + CurrentAbyssalStage + "]");
                    Log($"----- Time     - Time [ {Math.Round(GetCurrentStageStageSeconds / 60, 1)} min] Collapse in: {(_abyssRemainingSeconds > 60 ? (int)_abyssRemainingSeconds / 60 : 0)}m{Math.Round(_abyssRemainingSeconds % 60, 0)}s");
                    Log($"----- Abyss    - Stage [{CurrentAbyssalStage}] GateDistance [{_nextGate.Nearest1KDistance}] PlayerSpawnLocation Distance [{ESCache.Instance.PlayerSpawnLocation.Nearest1KDistance}] AbyssalCenter Distance [{ESCache.Instance.AbyssalCenter.Nearest1KDistance}] EstimatedGridClearTime [{(GetEstimatedStageRemainingTimeToClearGrid() ?? 0):F}] TimeNeededToTheGate [{_secondsNeededToReachTheGate:F}] StageRemainingSeconds [{CurrentStageRemainingSecondsWithoutPreviousStages:F}] TotalRemainingSeconds [{ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds:F}] SecondsSinceLastSessionChange [{Time.Instance.SecondsSinceLastSessionChange:F}] IsAbyssGateOpen [{IsAbyssGateOpen}]");
                    //Log($"----- TargetInfo    - TargetPriorities [{string.Join(", ", GetSortedTargetList(_currentLockedTargets.Where(e => e.Distance < _maxDroneRange && e.GroupId != 2009)).Select(e => $"{e.TypeName}:{e.AbyssalTargetPriority}"))}]");
                    Log($"----- Ship     - Shield [{Math.Round(ship.ShieldPercentage, 0)}] Armor [{Math.Round(ship.ArmorPercentage, 0)}] Structure [{Math.Round(ship.StructurePercentage, 0)}] Capacitor [{Math.Round(ship.CapacitorPercentage, 0)}] Speed [{Math.Round(ship.Entity.Velocity, 0)}] TargetingRange [{Math.Round(_maxTargetRange, 0)}]");
                    if (Drones.UseDrones) Log($"----- Drones   - Strategy [{_roomStrategy}] DronesInBay [{alldronesInBay.Sum(e => e.Stacksize):D2}] DronesInSpace [{allDronesInSpace.Count:D2}] InSpaceBandwidth [{_currentlyUsedDroneBandwidth:F}] AmountDronesInSpeedCloud [{allDronesInSpace.Count(e => DirectEntity.AnyIntersectionAtThisPosition(e._directEntity.DirectAbsolutePosition, false, true, true, true, true, true, true).Any())}] HudStatusEffects [{string.Join(", ", de.Me.ActiveHudStatusEffects.Select(d => d.ToString()).ToArray())}]");
                    //Log($"----- DroneInfo- (Drone,State,FollowEnt,DistanceToFollowEnt) [{string.Join(", ", allDronesInSpace.Select(d => de.EntitiesById.ContainsKey(d.FollowId) ? $"({d.TypeName},{d._directEntity.DroneState},{de.EntitiesById[d.FollowId].TypeName},{de.EntitiesById[d.FollowId].DistanceTo(de.EntitiesById[d.Id])})" : $"({d.TypeName},{d._directEntity.DroneState},Unknown,0)").ToArray())}]");
                    Log($"----- Player   - Boosters [{string.Join(", ", de.Me.Boosters.Select(b => de.GetInvType(b.TypeID).TypeName).ToArray())}] InSpeedCloud [{ship.Entity.IsInSpeedCloud}] IsLocatedWithinBioluminescenceCloud [{ship.Entity.IsLocatedWithinBioluminescenceCloud}] IsLocatedWithinFilamentCloud [{ship.Entity.IsLocatedWithinFilamentCloud}] SideEffects [{string.Join(", ", de.Me.GetAllNegativeBoosterEffects().Select(d => d.ToString()).ToArray())}] AbyssDailyHours [{TimeSpan.FromSeconds(ESCache.Instance.EveAccount.AbyssSecondsDaily).TotalHours:F}] AStarErrors [{DirectEntity.AStarErrors}]");
                    Log($"----- Entities - EnemiesOnGrid [{Combat.PotentialCombatTargets.Count()}] AmountEnemiesOnGridInASpeedCloud [{Combat.PotentialCombatTargets.Count(e => e._directEntity.IsInSpeedCloud)}] GJ/s [{Combat.PotentialCombatTargets.Sum(e => e._directEntity.GigaJouleNeutedPerSecond):F}] Kikimoras [{Combat.PotentialCombatTargets.Count(e => e.TypeName.ToLower().Contains("kikimora"))}] Marshals [{_marshalsOnGridCount}] Karen [{Combat.PotentialCombatTargets.Any(e => e._directEntity.IsDrifterBSSpawn)}] Battleships [{Combat.PotentialCombatTargets.Count(e => e.IsNPCBattleship)}] Neuts [{_neutsOnGridCount}] StageHPMax/Remaining [{_currentStageMaximumEhp:F}|{_currentStageCurrentEhp:F}]");
                    //Log($"----- EntDists - {GetEnemiesAndTheirDistances()}");
                    Log($"----- Clouds   - Filament [{ESCache.Instance.intFilamentClouds}] Bioluminesence [{ESCache.Instance.intBioluminesenceClouds}] Tachyon [{ESCache.Instance.intTachyonClouds}]");
                    Log($"----- Towers   - AutomataSuppressors [{ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Count()}] TrackingPylons [{ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.Count()}]");

                    LogPriorityCombatTargetInfo();
                    LogAutomataSuppressorInfo();
                    LogTrackingTowerInfo();
                    LogFleetMemberInfo();
                    if (Drones.UseDrones) LogDroneState();
                    if (ESCache.Instance.Weapons.Any()) LogWeaponState();
                }
                catch (Exception ex)
                {
                    Log($"{ex}");
                }
            }
            catch (Exception ex)
            {
                Log($"{ex}");
            }
        }

        private String GetEnemiesAndTheirDistances()
        {
            var sb = new StringBuilder();
            var targetsOrderedByDist = Combat.PotentialCombatTargets.OrderBy(e => e.Distance);
            foreach (var target in targetsOrderedByDist)
            {
                if (target == targetsOrderedByDist.Last())
                    sb.Append($"{target.TypeName} [{target.Distance:F}]");
                else
                    sb.Append($"{target.TypeName} [{target.Distance:F}], ");
            }

            return sb.ToString();
        }

        public static double? __getEstimatedStageRemainingTimeToClearGrid = 0;
        public double _GetEstimatedStageRemainingTimeToClearGrid
        {
            get
            {
                if (__getEstimatedStageRemainingTimeToClearGrid == null)
                {
                    GetEstimatedStageRemainingTimeToClearGrid();
                    if (__getEstimatedStageRemainingTimeToClearGrid.HasValue)
                        return (double)__getEstimatedStageRemainingTimeToClearGrid;

                    return 0;
                }

                return (double)__getEstimatedStageRemainingTimeToClearGrid;
            }
            set
            {
                __getEstimatedStageRemainingTimeToClearGrid = value;
            }
        }

        private double? GetEstimatedStageRemainingTimeToClearGrid()
        {
            try
            {
                if (_currentStageMaximumEhp == _currentStageCurrentEhp)
                    return 1200 / 3; // assume stage maximum time at the start

                if ((long)Time.Instance.SecondsSinceLastSessionChange == 0)
                    return null;

                var dps = (_currentStageMaximumEhp - _currentStageCurrentEhp) / Time.Instance.SecondsSinceLastSessionChange;

                var timeToClearGridWithDrones = GetSecondsToKillWithActiveDrones();

                if ((long)dps == 0)
                {
                    if (timeToClearGridWithDrones > 0 && timeToClearGridWithDrones < 400)
                        return timeToClearGridWithDrones;
                    return null;
                }

                var remaining = _currentStageCurrentEhp / dps;

                if (timeToClearGridWithDrones > 0 && timeToClearGridWithDrones < 400)
                {
                    remaining = (remaining + timeToClearGridWithDrones) / 2;
                }

                return remaining;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return 0;
            }
        }
        private DateTime _lastStateWrite;

        private bool StatsUseCSV = true;

        public void WriteStats()
        {
            if (_lastStateWrite.AddMinutes(1) > DateTime.UtcNow)
            {
                Log($"We already did write stats in the past minute, skipping.");
                return;
            }
            _lastStateWrite = DateTime.UtcNow;

            if (_abyssStatEntry != null)
            {
                if (StatsUseCSV)
                {
                    WriteStatsToCSV();
                }
                else
                {
                    WriteStatsToDB();
                }

            }
        }

        public void WriteStatsToDB()
        {
            if (_lastStateWrite.AddMinutes(1) > DateTime.UtcNow)
            {
                Log($"We already did write stats in the past minute, skipping.");
                return;
            }
            _lastStateWrite = DateTime.UtcNow;

            if (_abyssStatEntry != null)
            {

                var newTotal = _abyssStatEntry.TotalSeconds + ESCache.Instance.EveAccount.AbyssSecondsDaily;
                Log($"New total daily runtime: {TimeSpan.FromSeconds(newTotal)}");
                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.AbyssSecondsDaily), newTotal);
                Log("WriteStatsToDB:");

                try
                {
                    using (var wc = WriteConn.Open())
                    {
                        try
                        {

                            Log("Writing stats to SQLIte DB.");
                            wc.DB.Insert(_abyssStatEntry);
                            Log("Finished writing stats to SQLIte DB.");
                        }
                        catch (Exception e)
                        {
                            Log($"SQLDB Write Exception: {e.ToString()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                }
            }
        }

        public void WriteStatsToCSV()
        {
            if (_lastStateWrite.AddMinutes(1) > DateTime.UtcNow)
            {
                Log($"We already did write stats in the past minute, skipping.");
                return;
            }
            _lastStateWrite = DateTime.UtcNow;

            if (_abyssStatEntry != null)
            {
                Log("WriteStatsToCSV: Disabled: FixMe: NotYetImplimented");
                //
                // Adapt this code from drone stats to output abyssal stats as csv?
                //
                /*
                try
                {
                    string dronestatslogheader = "Date;Mission;Number of lost drones;# of Recalls\r\n";
                    if (!File.Exists(DroneStatslogFile))
                        File.AppendAllText(DroneStatslogFile, dronestatslogheader);

                    string dronestatslogline = DateTimeForLogs.ToShortDateString() + ";";
                    dronestatslogline += DateTimeForLogs.ToShortTimeString() + ";";
                    dronestatslogline += MissionSettings.MissionNameforLogging + ";";
                    dronestatslogline += LostDrones + ";";
                    dronestatslogline += +DroneRecalls + ";\r\n";
                    File.AppendAllText(DroneStatslogFile, dronestatslogline);
                    Log.WriteLine(dronestatslogheader);
                    Log.WriteLine(dronestatslogline);
                    DroneLoggingCompleted = true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return true;
                }
                */
            }
        }

        private void CaptureHP()
        {
            int ms = 0;
            using (new DisposableStopwatch(t => ms = (int)t.TotalMilliseconds))
            {
                var ehp = 0.0d;
                foreach (var target in Combat.PotentialCombatTargets)
                {

                    var currEhp = 0d;
                    currEhp = target._directEntity.CurrentArmor.Value + target._directEntity.CurrentShield.Value + target._directEntity.CurrentStructure.Value;
                    ehp += currEhp;
                }


                _currentStageMaximumEhp = Math.Max(ehp, _currentStageMaximumEhp);
                _currentStageCurrentEhp = ehp;
            }

            if (Combat.PotentialCombatTargets.Any() && DirectEve.Interval(4000, 5000))
            {
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log($"EHP Calculation took: [{ms}] ms. TimeSinceLastSessionChange [{Math.Round(Time.Instance.SecondsSinceLastSessionChange, 0)}] " +
                    $"RemainingStageClearGrindSeconds [{Math.Round(GetEstimatedStageRemainingTimeToClearGrid() ?? -1, 0)}] ExpectedFinishDate [{DateTime.UtcNow.AddSeconds(GetEstimatedStageRemainingTimeToClearGrid() ?? 1000).ToLongTimeString()}] maxEHP [{_currentStageMaximumEhp}] currEHP [{_currentStageCurrentEhp}]");
            }
        }

        private DateTime? _lastDroneInOptimal = null;
        private double _dronesInOptimalStage;

        // to not add stats logic overhead within the code, we just call this on every frame while in abyss space.
        internal void ManageStats()
        {
            var dronesInOptimal = DronesInOptimalCount();

            if (_lastDroneInOptimal != null)
            {
                var duration = (DateTime.UtcNow - _lastDroneInOptimal.Value).TotalSeconds;

                if (dronesInOptimal > 0)
                {
                    _dronesInOptimalStage += duration * (double)dronesInOptimal / 5.0d;
                }

                _lastDroneInOptimal = DateTime.UtcNow;
            }
            else
            {
                _lastDroneInOptimal = DateTime.UtcNow;
            }

            if (!DirectEve.Interval(2000))
                return;

            if (!DirectEve.Me.IsInAbyssalSpace())
                return;

            if (AreWeResumingFromACrash)
            {
                if (DirectEve.Interval(30000))
                    Log($"We are resuming from a crash. Skipping stats.");
                return;
            }

            //ensure necessary containers are open
            var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
            if (shipsCargo == null)
                return;

            var droneBay = ESCache.Instance.DirectEve.GetShipsDroneBay();
            if (droneBay == null)
                return;

            if (_abyssStatEntry == null)
            {
                _abyssStatEntry = new AbyssStatEntry();
                _abyssStatEntry.LowestStructure1 = 1;
                _abyssStatEntry.LowestArmor1 = 1;
                _abyssStatEntry.LowestShield1 = 1;
                _abyssStatEntry.LowestStructure2 = 1;
                _abyssStatEntry.LowestArmor2 = 1;
                _abyssStatEntry.LowestShield2 = 1;
                _abyssStatEntry.LowestStructure3 = 1;
                _abyssStatEntry.LowestArmor3 = 1;
                _abyssStatEntry.LowestShield3 = 1;
                _abyssStatEntry.LowestCap = 1;
            }

            _abyssStatEntry.StartedDate = DateTime.UtcNow.AddSeconds(-(20 * 60 - _abyssRemainingSeconds));
            var largeDronesLeftInbay = _droneBayItemList.Where(x => x.Item3 == DroneSize.Large).Sum(x => GetAmountOfTypeIdLeftInDroneBay(x.Item1));
            var medDronesLeftInbay = _droneBayItemList.Where(x => x.Item3 == DroneSize.Medium).Sum(x => GetAmountOfTypeIdLeftInDroneBay(x.Item1));
            var smallDronesLeftInbay = _droneBayItemList.Where(x => x.Item3 == DroneSize.Small).Sum(x => GetAmountOfTypeIdLeftInDroneBay(x.Item1));

            var amountOfAllDronesInBay = largeDronesLeftInbay + medDronesLeftInbay + smallDronesLeftInbay;
            var amountOfAllDronesInBayAfterArm = _droneBayItemList.Sum(d => d.Item2);

            var analBurner = DirectEve.Modules.Where(e => e.GroupId == (int)Group.Afterburner);
            var shieldHardener = DirectEve.Modules.Where(e => e.GroupId == (int)Group.ShieldHardeners);
            var shieldBooster = DirectEve.Modules.Where(e => e.GroupId == (int)Group.ShieldBoosters);
            var modules = analBurner.Concat(shieldHardener).Concat(shieldBooster);

            var noDronesInSpaceAndNoLimboDrones = (_limboDeployingAbyssalDrones == null || !_limboDeployingAbyssalDrones.Any()) && !allDronesInSpace.Any();

            _abyssStatEntry.SingleGateAbyss = _singleRoomAbyssal;
            _abyssStatEntry.FilamentTypeId = (int)_filamentTypeId;

            _abyssStatEntry.LowestCap = Math.Round(Math.Min(ESCache.Instance.ActiveShip.CapacitorPercentage / 100, _abyssStatEntry.LowestCap), 2);
            if (_abyssStatEntry.AStarErrors != DirectEntity.AStarErrors)
            {
                Log($"AbyssStatEntry.AStarErrors changed it's value, current value [{DirectEntity.AStarErrors}]");
            }

            _abyssStatEntry.AStarErrors = DirectEntity.AStarErrors;

            switch (CurrentAbyssalStage)
            {
                case AbyssalStage.Stage1:

                    if (_abyssStatEntry.Room1Seconds > 0)
                        _abyssStatEntry.DronePercOptimal1 = Math.Round(_dronesInOptimalStage / _abyssStatEntry.Room1Seconds, 2);
                    _abyssStatEntry.DroneEngagesStage1 = _droneEngageCount;
                    _abyssStatEntry.Room1Hp = Math.Round(_currentStageMaximumEhp, 2);
                    _abyssStatEntry.LowestStructure1 = Math.Min(Math.Round(ESCache.Instance.ActiveShip.Entity.StructurePct, 2), _abyssStatEntry.LowestStructure1);
                    _abyssStatEntry.LowestArmor1 = Math.Min(Math.Round(ESCache.Instance.ActiveShip.Entity.ArmorPct, 2), _abyssStatEntry.LowestArmor1);
                    _abyssStatEntry.LowestShield1 = Math.Min(Math.Round(ESCache.Instance.ActiveShip.Entity.ShieldPct, 2), _abyssStatEntry.LowestShield1);
                    _abyssStatEntry.DroneRecallsStage1 = _droneRecallsStage;
                    _abyssStatEntry.Room1Seconds = (int)GetCurrentStageStageSeconds;

                    if (IsAbyssGateOpen)
                    {

                        if (!allDronesInSpace.Any())
                        {
                            _abyssStatEntry.LostDronesRoom1 = amountOfAllDronesInBayAfterArm - amountOfAllDronesInBay;
                        }

                        if (_abyssStatEntry.PenaltyStrength == default(double))
                        {
                            _abyssStatEntry.PenaltyStrength = Framework.Me.GetAbyssResistsDebuff()?.Item2 ?? 0;
                        }

                        //if (_alreadyLootedItemIds.Any() && String.IsNullOrEmpty(_abyssStatEntry.LootTableRoom1))
                        //{
                        //    _abyssStatEntry.LootTableRoom1 = string.Join(",", _alreadyLootedItems);
                        //}

                        if (!_singleRoomAbyssal)
                        {
                            _abyssStatEntry.ClearDoneGateDist1 = Math.Round(Math.Max(_nextGate.Distance, _abyssStatEntry.ClearDoneGateDist1), 2);
                        }
                        _abyssStatEntry.Room1CacheMiss = _remainingNonEmptyWrecksAndCacheCount;
                    }
                    else
                    {


                        _abyssStatEntry.OverheatRoom1 = _abyssStatEntry.OverheatRoom1 || modules.Any(m => m.IsOverloaded);
                        _abyssStatEntry.DrugsUsedRoom1 = _abyssStatEntry.DrugsUsedRoom1 || _lastDrugUsage.AddSeconds(4) > DateTime.UtcNow;
                        _abyssStatEntry.Room1Neuts = Math.Max(_abyssStatEntry.Room1Neuts, _neutsOnGridCount);
                        _abyssStatEntry.Room1NeutGJs = Math.Max(_abyssStatEntry.Room1NeutGJs, Combat.PotentialCombatTargets.Sum(e => e._directEntity.GigaJouleNeutedPerSecond));
                        if (Combat.PotentialCombatTargets.Any(e => !e.IsAbyssalBioAdaptiveCache && !e.IsAbyssalDeadspaceTriglavianExtractionNode) && ESCache.Instance.TotalTargetsAndTargeting.Count() >= 1 && string.IsNullOrEmpty(_abyssStatEntry.Room1Dump))
                        {
                            _abyssStatEntry.Room1Dump = GenerateGridDump(DirectEve.Entities.Where(e => e.IsNPCByBracketType || e.IsAbyssalBioAdaptiveCache || e.IsAbyssalDeadspaceTriglavianExtractionNode));
                            Log($"Generated Dump for room 1: {_abyssStatEntry.Room1Dump}");
                            _abyssStatEntry.PylonsClounds1 = GenerateGridDump(DirectEve.Entities.Where(e => e.IsAbyssSphereEntity), false);
                        }

                    }
                    break;
                case AbyssalStage.Stage2:

                    if (_abyssStatEntry.Room2Seconds > 0)
                        _abyssStatEntry.DronePercOptimal2 = Math.Round(_dronesInOptimalStage / _abyssStatEntry.Room2Seconds, 2);
                    _abyssStatEntry.DroneEngagesStage2 = _droneEngageCount;
                    _abyssStatEntry.Room2Hp = Math.Round(_currentStageMaximumEhp, 2);
                    _abyssStatEntry.LowestStructure2 = Math.Min(Math.Round(ESCache.Instance.ActiveShip.Entity.StructurePct, 2), _abyssStatEntry.LowestStructure2);
                    _abyssStatEntry.LowestArmor2 = Math.Min(Math.Round(ESCache.Instance.ActiveShip.Entity.ArmorPct, 2), _abyssStatEntry.LowestArmor2);
                    _abyssStatEntry.LowestShield2 = Math.Min(Math.Round(ESCache.Instance.ActiveShip.Entity.ShieldPct, 2), _abyssStatEntry.LowestShield2);

                    _abyssStatEntry.Room2Seconds = (int)GetCurrentStageStageSeconds;
                    _abyssStatEntry.DroneRecallsStage2 = _droneRecallsStage;
                    if (IsAbyssGateOpen)
                    {

                        if (!allDronesInSpace.Any())
                        {
                            _abyssStatEntry.LostDronesRoom2 = amountOfAllDronesInBayAfterArm - amountOfAllDronesInBay -
                                                              _abyssStatEntry.LostDronesRoom1;
                        }


                        if (_abyssStatEntry.PenaltyStrength == default(double))
                        {
                            _abyssStatEntry.PenaltyStrength = Framework.Me.GetAbyssResistsDebuff()?.Item2 ?? 0;
                        }

                        //if (_alreadyLootedItemIds.Any() && String.IsNullOrEmpty(_abyssStatEntry.LootTableRoom2))
                        //{
                        //    _abyssStatEntry.LootTableRoom2 = string.Join(",", _alreadyLootedItems);
                        //}

                        if (!_singleRoomAbyssal)
                        {
                            _abyssStatEntry.ClearDoneGateDist2 = Math.Round(Math.Max(_nextGate.Distance, _abyssStatEntry.ClearDoneGateDist2), 2);
                        }
                        _abyssStatEntry.Room2CacheMiss = _remainingNonEmptyWrecksAndCacheCount;
                    }
                    else
                    {
                        if (noDronesInSpaceAndNoLimboDrones)
                            _abyssStatEntry.LostDronesRoom2 = amountOfAllDronesInBayAfterArm - amountOfAllDronesInBay - _abyssStatEntry.LostDronesRoom1 - allDronesInSpace.Count;


                        _abyssStatEntry.OverheatRoom2 = _abyssStatEntry.OverheatRoom2 || modules.Any(m => m.IsOverloaded);
                        _abyssStatEntry.DrugsUsedRoom2 = _abyssStatEntry.DrugsUsedRoom2 || _lastDrugUsage.AddSeconds(4) > DateTime.UtcNow;
                        _abyssStatEntry.Room2Neuts = Math.Max(_abyssStatEntry.Room2Neuts, _neutsOnGridCount);
                        _abyssStatEntry.Room2NeutGJs = Math.Max(_abyssStatEntry.Room2NeutGJs, Combat.PotentialCombatTargets.Sum(e => e._directEntity.GigaJouleNeutedPerSecond));
                        if (Combat.PotentialCombatTargets.Any(e => !e.IsAbyssalBioAdaptiveCache && !e.IsAbyssalDeadspaceTriglavianExtractionNode) && ESCache.Instance.TotalTargetsAndTargeting.Count() >= 1 && string.IsNullOrEmpty(_abyssStatEntry.Room2Dump))
                        {
                            _abyssStatEntry.Room2Dump = GenerateGridDump(DirectEve.Entities.Where(e => e.IsNPCByBracketType || e.IsAbyssalBioAdaptiveCache || e.IsAbyssalDeadspaceTriglavianExtractionNode));
                            Log($"Generated Dump for room 2: {_abyssStatEntry.Room2Dump}");
                            _abyssStatEntry.PylonsClounds2 = GenerateGridDump(DirectEve.Entities.Where(e => e.IsAbyssSphereEntity), false);
                        }

                    }
                    break;
                case AbyssalStage.Stage3:

                    if (_abyssStatEntry.Room3Seconds > 0)
                        _abyssStatEntry.DronePercOptimal3 = Math.Round(_dronesInOptimalStage / _abyssStatEntry.Room3Seconds, 2);

                    _abyssStatEntry.DroneEngagesStage3 = _droneEngageCount;
                    _abyssStatEntry.Room3Hp = Math.Round(_currentStageMaximumEhp, 2);
                    _abyssStatEntry.LowestStructure3 = Math.Min(Math.Round(ESCache.Instance.ActiveShip.Entity.StructurePct, 2), _abyssStatEntry.LowestStructure3);
                    _abyssStatEntry.LowestArmor3 = Math.Min(Math.Round(ESCache.Instance.ActiveShip.Entity.ArmorPct, 2), _abyssStatEntry.LowestArmor3);
                    _abyssStatEntry.LowestShield3 = Math.Min(Math.Round(ESCache.Instance.ActiveShip.Entity.ShieldPct, 2), _abyssStatEntry.LowestShield3);

                    _abyssStatEntry.TotalSeconds = (int)(DateTime.UtcNow - _abyssStatEntry.StartedDate).TotalSeconds;
                    _abyssStatEntry.Room3Seconds = (int)GetCurrentStageStageSeconds;
                    _abyssStatEntry.DroneRecallsStage3 = _droneRecallsStage;

                    if (IsAbyssGateOpen)
                    {

                        if (!allDronesInSpace.Any())
                        {
                            _abyssStatEntry.LostDronesRoom3 = amountOfAllDronesInBayAfterArm - amountOfAllDronesInBay -
                                                              _abyssStatEntry.LostDronesRoom1 -
                                                              _abyssStatEntry.LostDronesRoom2;

                            _abyssStatEntry.LargeDronesLost =
                                _droneBayItemList.Where(x => x.Item3 == DroneSize.Large).Sum(x => x.Item2) - largeDronesLeftInbay;
                            _abyssStatEntry.MediumDronesLost = _droneBayItemList.Where(x => x.Item3 == DroneSize.Medium).Sum(x => x.Item2) - medDronesLeftInbay;
                            _abyssStatEntry.SmallDronesLost =
                                _droneBayItemList.Where(x => x.Item3 == DroneSize.Small).Sum(x => x.Item2) - smallDronesLeftInbay;
                        }

                        if (_abyssStatEntry.PenaltyStrength == default(double))
                        {
                            _abyssStatEntry.PenaltyStrength = Framework.Me.GetAbyssResistsDebuff()?.Item2 ?? 0;
                        }

                        //if (_alreadyLootedItemIds.Any() && String.IsNullOrEmpty(_abyssStatEntry.LootTableRoom3))
                        //{
                        //    _abyssStatEntry.LootTableRoom3 = string.Join(",", _alreadyLootedItems);
                        //}

                        if (!_singleRoomAbyssal)
                        {
                            _abyssStatEntry.ClearDoneGateDist3 = Math.Round(Math.Max(_nextGate.Distance, _abyssStatEntry.ClearDoneGateDist3), 2);
                        }
                        _abyssStatEntry.Room3CacheMiss = _remainingNonEmptyWrecksAndCacheCount;
                    }
                    else
                    {


                        _abyssStatEntry.OverheatRoom3 = _abyssStatEntry.OverheatRoom3 || modules.Any(m => m.IsOverloaded);
                        _abyssStatEntry.DrugsUsedRoom3 = _abyssStatEntry.DrugsUsedRoom3 || _lastDrugUsage.AddSeconds(4) > DateTime.UtcNow;
                        _abyssStatEntry.Room3Neuts = Math.Max(_abyssStatEntry.Room3Neuts, _neutsOnGridCount);
                        _abyssStatEntry.Room3NeutGJs = Math.Max(_abyssStatEntry.Room3NeutGJs, Combat.PotentialCombatTargets.Sum(e => e._directEntity.GigaJouleNeutedPerSecond));

                        if (Combat.PotentialCombatTargets.Any(e => !e.IsAbyssalBioAdaptiveCache && !e.IsAbyssalDeadspaceTriglavianExtractionNode) && ESCache.Instance.TotalTargetsAndTargeting.Count() >= 1 && string.IsNullOrEmpty(_abyssStatEntry.Room3Dump))
                        {
                            _abyssStatEntry.Room3Dump = GenerateGridDump(DirectEve.Entities.Where(e => e.IsNPCByBracketType || e.IsAbyssalBioAdaptiveCache || e.IsAbyssalDeadspaceTriglavianExtractionNode));
                            _abyssStatEntry.PylonsClounds3 = GenerateGridDump(DirectEve.Entities.Where(e => e.IsAbyssSphereEntity), false);
                            Log($"Generated Dump for room 3: {_abyssStatEntry.Room3Dump}");
                        }
                    }

                    break;
                default:
                    break;
            }

            if (DirectEve.Interval(5000))
            {
                var act = new Action(() =>
                {

                    try
                    {
                        var frm = this.Form as AbyssalControllerForm;
                        var dgv = frm.GetDataGridView1;
                        frm.Invoke(new Action(() =>
                        {
                            var scrollingIndex = 0;
                            var colIndex = 0;

                            if (dgv.RowCount != 0)
                            {
                                scrollingIndex = dgv.FirstDisplayedCell.RowIndex;
                                colIndex = dgv.FirstDisplayedScrollingColumnIndex;
                            }

                            dgv.DataSource = new List<AbyssStatEntry> { _abyssStatEntry };

                            if (dgv.RowCount != 0)
                            {
                                dgv.FirstDisplayedScrollingRowIndex = scrollingIndex;
                                dgv.FirstDisplayedScrollingColumnIndex = colIndex;
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Log(ex.ToString());
                    }


                });


                Task.Run(act);
            }
        }
    }
}
