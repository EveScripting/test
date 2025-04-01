extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Security.Tokens;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Questor.Combat
{
    public static partial class Combat
    {
        #region Fields

        private static HashSet<long> ListLeaderTargets;

        #endregion Fields

        #region Methods

        private static DateTime DebugInfoLogged = DateTime.UtcNow;

        private static void BuildListLeaderTargets()
        {
            ListLeaderTargets = new HashSet<long>();
            if (ESCache.Instance.EveAccount.LeaderIsTargetingId1 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId1 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId1, "LeaderIsTargetingId1");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId1 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId1 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId1);
            }

            if (ESCache.Instance.EveAccount.LeaderIsTargetingId2 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId2 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId2, "LeaderIsTargetingId2");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId2 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId2 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId2);
            }

            if (ESCache.Instance.EveAccount.LeaderIsTargetingId3 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId3 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId3, "LeaderIsTargetingId3");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId3 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId3 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId3);
            }

            if (ESCache.Instance.EveAccount.LeaderIsTargetingId4 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId4 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId4, "LeaderIsTargetingId4");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId4 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId4 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId4);
            }

            if (ESCache.Instance.EveAccount.LeaderIsTargetingId5 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId5 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId5, "LeaderIsTargetingId5");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId5 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId5 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId5);
            }

            if (ESCache.Instance.EveAccount.LeaderIsTargetingId6 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId6 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId6, "LeaderIsTargetingId6");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId6 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId6 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId6);
            }

            if (ESCache.Instance.EveAccount.LeaderIsTargetingId7 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId7 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId7, "LeaderIsTargetingId7");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId7 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId7 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId7);
            }

            if (ESCache.Instance.EveAccount.LeaderIsTargetingId8 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId8 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId8, "LeaderIsTargetingId8");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId8 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId8 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId8);
            }

            if (ESCache.Instance.EveAccount.LeaderIsTargetingId9 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId9 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId9, "LeaderIsTargetingId9");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId9 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId9 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId9);
            }

            if (ESCache.Instance.EveAccount.LeaderIsTargetingId10 != null && ESCache.Instance.EveAccount.LeaderIsTargetingId10 > 0)
            {
                CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId10, "LeaderIsTargetingId10");
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("BuildListLeaderTargets: LeaderIsTargetingId10 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId10 + "]");
                ListLeaderTargets.Add((long)ESCache.Instance.EveAccount.LeaderIsTargetingId10);
            }
        }

        private static void CheckForEntityWithEntityId(long? leaderIsTargetingIdNumber, string targetNum)
        {
            if (leaderIsTargetingIdNumber == null)
                return;

            if (leaderIsTargetingIdNumber == 0)
                return;

            foreach (EntityCache entity in ESCache.Instance.EntitiesOnGrid)
                if (entity.Id == leaderIsTargetingIdNumber)
                    return;

            const long emptyValue = 0;
            if (DebugConfig.DebugTargetCombatants) Log.WriteLine("Hydra: CheckForEntityWithEntityID [" + leaderIsTargetingIdNumber + "] Not found. Removing [ LeaderIsTargetingId" + targetNum + " ]");
            ESCache.Instance.TaskSetEveAccountAttribute("LeaderIsTargetingId" + targetNum, emptyValue);
            if (DebugConfig.DebugTargetCombatants) Log.WriteLine("Hydra: CheckForEntityWithEntityID [" + leaderIsTargetingIdNumber + "] Not found.. Checking LeaderIsTargetingId1 is [" + ESCache.Instance.EveAccount.LeaderIsTargetingId1 + "]");
        }

        private static void DetermineMaxTargetsICanLock()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                int? totalTargetingSlotsWeNeed = maxHighValueTargets + maxLowValueTargets + Salvage.GlobalMaximumWreckTargets;
                if (totalTargetingSlotsWeNeed != null && ESCache.Instance.ActiveShip.MaxLockedTargetsWithShipAndSkills > totalTargetingSlotsWeNeed)
                    switch (ESCache.Instance.ActiveShip.MaxLockedTargetsWithShipAndSkills)
                    {
                        case 1:
                        case 2:
                            //If we have max targets of 2 and we are in a cruiser or battleship something is very wrong!
                            maxHighValueTargets = 1;
                            maxLowValueTargets = 1;
                            Salvage.GlobalMaximumWreckTargets = 1;

                            if (!ESCache.Instance.PauseAfterNextDock && ESCache.Instance.Weapons.Any() && (ESCache.Instance.MyShipEntity.IsCruiser || ESCache.Instance.MyShipEntity.IsBattlecruiser || ESCache.Instance.MyShipEntity.IsBattleship))
                            {
                                Log.WriteLine("DetermineMaxTargetsICanLock -  MaxLockedTargets [" + ESCache.Instance.ActiveShip.MaxLockedTargetsWithShipAndSkills + "] this has to be bad: pausing at next dock");
                                ESCache.Instance.PauseAfterNextDock = true;
                                break;
                            }

                            break;

                        case 3:
                            maxHighValueTargets = 1;
                            maxLowValueTargets = 1;
                            Salvage.GlobalMaximumWreckTargets = 1;
                            break;

                        case 4:
                            maxHighValueTargets = 2;
                            maxLowValueTargets = 1;
                            Salvage.GlobalMaximumWreckTargets = 1;
                            break;

                        case 5:
                            maxHighValueTargets = 2;
                            maxLowValueTargets = 2;
                            Salvage.GlobalMaximumWreckTargets = 1;
                            break;

                        case 6:
                            maxHighValueTargets = 3;
                            maxLowValueTargets = 2;
                            Salvage.GlobalMaximumWreckTargets = 1;
                            break;

                        case 7:
                            maxHighValueTargets = 3;
                            maxLowValueTargets = 3;
                            Salvage.GlobalMaximumWreckTargets = 1;
                            break;

                        case 8:
                            maxHighValueTargets = 3;
                            maxLowValueTargets = 3;
                            Salvage.GlobalMaximumWreckTargets = 2;
                            break;

                        case 9:
                            maxHighValueTargets = 4;
                            maxLowValueTargets = 3;
                            Salvage.GlobalMaximumWreckTargets = 2;
                            break;

                        case 10:
                            maxHighValueTargets = 4;
                            maxLowValueTargets = 4;
                            Salvage.GlobalMaximumWreckTargets = 2;
                            break;

                        case 11:
                            maxHighValueTargets = 5;
                            maxLowValueTargets = 4;
                            Salvage.GlobalMaximumWreckTargets = 2;
                            break;

                        case 12:
                            maxHighValueTargets = 5;
                            maxLowValueTargets = 5;
                            Salvage.GlobalMaximumWreckTargets = 2;
                            break;
                    }
            }
        }

        private static bool PushTargetInfo()
        {
            int targetNum = 0;
            foreach (EntityCache target in ESCache.Instance.EntitiesOnGrid.Where(i => !i.IsLargeCollidable && !i.IsOreOrIce && !i.IsStation && !i.IsCitadel && (i.IsTarget || i.IsTargeting)))
            {
                targetNum++;
                if (targetNum > 10) break;
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("LeaderIsTargetingId" + targetNum + " [" + target.Name + "][" + target.Id + "][" + Math.Round(target.Distance / 1000, 0) + "k]");
                ESCache.Instance.TaskSetEveAccountAttribute("LeaderIsTargetingId" + targetNum, target.Id);
            }

            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId1, "1");
            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId2, "2");
            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId3, "3");
            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId4, "4");
            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId5, "5");
            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId6, "6");
            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId7, "7");
            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId8, "8");
            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId9, "9");
            CheckForEntityWithEntityId(ESCache.Instance.EveAccount.LeaderIsTargetingId10, "10");
            return false;
        }

        private static bool EcmJammingChecks()
        {
            if (ESCache.Instance.MaxLockedTargets == 0 || Combat.TargetedBy.Any(t => t._directEntity.IsJammingMe))
            {
                if (!_isJammed)
                    Log.WriteLine("We are jammed and can not target anything");

                _isJammed = true;
                if (ESCache.Instance.Weapons.Count > 0)
                {
                    if (ESCache.Instance.Weapons.Any(i => i.WeaponNeedsAmmo && !i.IsCivilianWeapon && i.WeaponNeedsToBeReloadedNow && !i.IsReloadingAmmo))
                        if (DirectEve.Interval(10000))
                            ReloadAll();
                }

                return true;
            }

            if (_isJammed)
            {
                ESCache.Instance.TargetingIDs.Clear();
                Log.WriteLine("We are no longer jammed, reTargeting");
            }

            _isJammed = false;
            return true;
        }

        private static void DebugLogTargetInfo()
        {
            if (DebugConfig.DebugCombatMissionsBehavior && DateTime.UtcNow > DebugInfoLogged.AddSeconds(300))
            {
                int potentialCombatTargetNumber = 0;
                foreach (EntityCache potentialCombatTarget in PotentialCombatTargets)
                {
                    Log.WriteLine("[" + potentialCombatTargetNumber + "][" + potentialCombatTarget.Name + "][" + Math.Round(potentialCombatTarget.Distance / 1000, 0) + "k]");
                    potentialCombatTargetNumber++;

                    Log.WriteLine("[" + potentialCombatTarget.Name + ";EntityAttackRange;" + potentialCombatTarget._directEntity.EntityAttackRange);

                    Log.WriteLine("[" + potentialCombatTarget.Name + ";EmRawTurretDamage;" + potentialCombatTarget._directEntity.NpcEmRawTurretDamage);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";ExplosiveRawTurretDamage;" + potentialCombatTarget._directEntity.ExplosiveRawTurretDamage);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";KineticTurretDamage;" + potentialCombatTarget._directEntity.NpcKineticTurretDamage);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";ThermalRawTurretDamage;" + potentialCombatTarget._directEntity.NpcThermalRawTurretDamage);

                    Log.WriteLine("[" + potentialCombatTarget.Name + ";EmRawTurretDps;" + potentialCombatTarget._directEntity.NpcEmRawTurretDps);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";NpcExplosiveRawTurretDps;" + potentialCombatTarget._directEntity.NpcExplosiveRawTurretDps);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";NpcKineticRawTurretDps;" + potentialCombatTarget._directEntity.NpcKineticRawTurretDps);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";NpcThermalRawTurretDps;" + potentialCombatTarget._directEntity.NpcThermalRawTurretDps);

                    Log.WriteLine("[" + potentialCombatTarget.Name + ";EmRawMissileDamage;" + potentialCombatTarget._directEntity.NpcEmRawMissileDamage);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";NpcExplosiveRawMissileDamage;" + potentialCombatTarget._directEntity.NpcExplosiveRawMissileDamage);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";KineticRawMissileDamage;" + potentialCombatTarget._directEntity.NpcKineticRawMissileDamage);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";ThermalRawMissileDamage;" + potentialCombatTarget._directEntity.NpcThermalRawMissileDamage);

                    Log.WriteLine("[" + potentialCombatTarget.Name + ";NpcEmRawMissileDps;" + potentialCombatTarget._directEntity.NpcEmRawMissileDps);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";NpcExplosiveRawMissileDps;" + potentialCombatTarget._directEntity.NpcExplosiveRawMissileDps);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";NpcKineticRawMissileDps;" + potentialCombatTarget._directEntity.NpcKineticRawMissileDps);
                    Log.WriteLine("[" + potentialCombatTarget.Name + ";NpcThermalRawMissileDps);" + potentialCombatTarget._directEntity.NpcThermalRawMissileDps);

                    Log.WriteLine("-------------------------");
                    if (potentialCombatTargetNumber >= 20) break;
                }

                DebugInfoLogged = DateTime.UtcNow;
            }
        }

        public static int PotentialCombatTargetsCount_AtLastLockedTarget = 0;

        public static int CurrentPotentialCombatTargetsCount => PotentialCombatTargets.Count;

        private static bool UnlockTargetsThatAreOutOfRange()
        {
            //Remove any target that is out of range(lower of Weapon Range or targeting range, definitely matters if damped)
            if (ESCache.Instance.InWormHoleSpace && ESCache.Instance.Targets.Count > 0 && ESCache.Instance.Targets.Count > 4 && Time.Instance.NextUnlockTargetOutOfRange < DateTime.UtcNow)
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: if (ESCache.Instance.InWormHoleSpace && ESCache.Instance.Targets.Any() && ESCache.Instance.Targets.Count() > 4 && Time.Instance.NextUnlockTargetOutOfRange < DateTime.UtcNow)");

                Time.Instance.NextUnlockTargetOutOfRange = DateTime.UtcNow.AddSeconds(Time.Instance.Rnd.Next(5, 8));
                if (CurrentPotentialCombatTargetsCount > PotentialCombatTargetsCount_AtLastLockedTarget)
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("DebugTargetCombatants: if (CurrentPotentialCombatTargetsCount > PotentialCombatTargetsCount_AtLastLockedTarget)");
                    foreach (EntityCache target in ESCache.Instance.Targets.Where(target => target.IsTarget && !target.IsReadyToTarget && !target.IsNPCBattleship && !target.IsNpcCapitalEscalation && !target.IsPlayer))
                    {
                        Log.WriteLine("unlocking [" + target.TypeName + "] at [" + target.Nearest1KDistance + "] !IsReadyToTarget");
                        target.UnlockTarget();
                    }
                }
            }

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.MiningController))
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.MiningController))");

                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: if (CurrentPotentialCombatTargetsCount > PotentialCombatTargetsCount_AtLastLockedTarget)");
                foreach (EntityCache target in ESCache.Instance.Targets.Where(target => target.IsTarget && !target.IsInRangeOfWeapons))
                {
                    Log.WriteLine("unlocking [" + target.TypeName + "] at [" + target.Nearest1KDistance + "] !IsInRangeOfWeapons");
                    target.UnlockTarget();
                }

                return true;
            }

            if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.Targets.Any(i => i.IsWreck && i.IsWreckEmpty))
            {
                if (ESCache.Instance.Targets.FirstOrDefault(i => i.IsWreck && i.IsWreckEmpty).UnlockTarget())
                {
                    Log.WriteLine("unlocking [" + ESCache.Instance.Targets.FirstOrDefault(i => i.IsWreck && i.IsWreckEmpty).TypeName + "] at [" + ESCache.Instance.Targets.FirstOrDefault(i => i.IsWreck && i.IsWreckEmpty).Nearest1KDistance + "]...");
                }
                return false;
            }

            if (ESCache.Instance.Targets.Count > 1 && Time.Instance.NextUnlockTargetOutOfRange < DateTime.UtcNow)
            {
                Time.Instance.NextUnlockTargetOutOfRange = DateTime.UtcNow.AddSeconds(Time.Instance.Rnd.Next(5, 8));

                foreach (EntityCache target in ESCache.Instance.Targets.Where(target => target.IsTarget && !target.IsInRangeOfWeapons))
                {
                    Log.WriteLine("unlocking [" + target.TypeName + "] at [" + target.Nearest1KDistance + "] !IsInRangeOfWeapons");
                    target.UnlockTarget();
                }

                if (ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.MiningController))
                {
                    if (!UnlockLowValueTarget("[lowValue]OutOfRange or Ignored", true)) return false;
                    if (!UnlockHighValueTarget("[highValue]OutOfRange or Ignored", true)) return false;
                }
            }

            return true;
        }

        /**
        private static bool WormholeSpaceTargeting()
        {
            if (ESCache.Instance.InWormHoleSpace)
            {
                if (ESCache.Instance.MyShipEntity.GroupId == (int)GroupID.Dreadnaught)
                {
                    //
                    // determine if guns are high angle guns or not (how?)
                    //
                    if (ESCache.Instance.Weapons.Any() && ESCache.Instance.Weapons.Any(weapon => weapon.IsHighAngleWeapon))
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: Targeting_WormHoleAnomaly_Dread_HAW();");
                        Targeting_WormHoleAnomaly_Dread_HAW();
                        return true;
                    }

                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("DebugTargetCombatants: Targeting_WormHoleAnomaly_Dread();");
                    Targeting_WormHoleAnomaly_Dread();
                    return true;
                }

                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: Targeting_WormHoleAnomaly();");
                Targeting_WormHoleAnomaly();
                return true;
            }

            return false;
        }
        **/


        private static bool MiningTargeting()
        {
            if (ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.MiningController))
                return false;

            if (MiningBehavior.MinableAsteroids.Any())
            {
                if (DebugConfig.DebugMiningBehavior)
                    Log.WriteLine("DebugMiningBehavior: MiningTargeting();");
                //Targeting_MiningTargetsToLock = null;
                //#Todo fixme
                if (MiningBehavior.Targeting_MiningTargetsToLock != null && MiningBehavior.Targeting_MiningTargetsToLock.Any())
                {
                    Log.WriteLine("DebugMiningBehavior: Targeting_MiningTargetsToLock [" + MiningBehavior.Targeting_MiningTargetsToLock.Count() + "]");
                    TargetTheseEntities(MiningBehavior.Targeting_MiningTargetsToLock, "MiningTargeting");
                }

                return true;
            }

            return true;
        }

        private static bool AbyssalDeadspaceTargeting()
        {
            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 2)
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("DebugTargetCombatants: if (Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 2)");
                    return true;
                }

                if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs)
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("DebugTargetCombatants: Targeting_AbyssalConstructionYard();");
                    Targeting_AbyssalConstructionYard();
                    return true;
                }

                //if (ESCache.Instance.EntitiesOnGrid.Count > 0 && ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToTarget))
                //{
                //    if (DebugConfig.DebugTargetCombatants)
                //        Log.WriteLine("DebugTargetCombatants: if (ESCache.Instance.EntitiesOnGrid.Count > 0 && ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToTarget))");
                //    Targeting_AbyssalTargetsToLock = ESCache.Instance.EntitiesOnGrid.Where(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToTarget).OrderBy(x => x.Distance);
                //    TargetTheseEntities(Targeting_AbyssalTargetsToLock, "Targeting_Abyssal");
                //    return true;
                //}

                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: Targeting_Abyssal();");
                Targeting_AbyssalTargetsToLock = AbyssalSpawn.AbyssalPotentialCombatTargets_Targets;
                if (Targeting_AbyssalTargetsToLock != null && Targeting_AbyssalTargetsToLock.Any())
                {
                    TargetTheseEntities(Targeting_AbyssalTargetsToLock, "Targeting_Abyssal");
                }

                return true;
            }

            return false;
        }

        private static bool CombatDontMoveControllerTargeting()
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) ||
                ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController)) //ESCache.Instance.DirectEve.Me.IsInvasionActive
            {
                if (Salvage.Salvagers.Count >= 3) //Dedicated salvaging ship?
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (!ESCache.Instance.Weapons.Any() && Salvage.Salvagers.Any() && Salvage.Salvagers.Count() >= 3) Target wrecks!");
                    Salvage.TargetWrecks(ESCache.Instance.UnlootedContainers);
                    return true;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Nestor)
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Nestor)");
                    Targeting_TriglavianNestor();
                    return true;
                }

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Logistics || ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Augoror || ESCache.Instance.Modules.Count(i => i.IsHighSlotModule && (i.IsRemoteArmorRepairModule || i.IsRemoteShieldRepairModule || i.IsRemoteEnergyTransferModule)) > 3)
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (ESCache.Instance.ActiveShip.GroupId == (int)GroupID.Logistics)");
                    Targeting_TriglavianLogistics();
                    return true;
                }
                if (!ESCache.Instance.DockableLocations.All(i => i.IsOnGridWithMe) && !ESCache.Instance.Stargates.All(i => i.IsOnGridWithMe) && PotentialCombatTargets.Count > 0 && (ESCache.Instance.ActiveShip.GroupId == (int)Group.CommandShip || ESCache.Instance.ActiveShip.GroupId == (int)Group.Battlecruiser))
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (!ESCache.Instance.Stations.All(i => i.IsOnGridWithMe) && !ESCache.Instance.Stargates.All(i => i.IsOnGridWithMe) && PotentialCombatTargets.Any() && !ESCache.Instance.Weapons.Any() && (ESCache.Instance.ActiveShip.GroupId == (int)GroupID.CommandShip || ESCache.Instance.ActiveShip.GroupId == (int)GroupID.Battlecruiser))");
                    Targeting_TriglavianAntiLooter();
                    return true;
                }

                if (ESCache.Instance.InWormHoleSpace && ESCache.Instance.ActiveShip.IsDread || ESCache.Instance.ActiveShip.IsMarauder || ESCache.Instance.ActiveShip.IsBattleship || ESCache.Instance.ActiveShip.IsBattleCruiser)
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (ESCache.Instance.InWormHoleSpace && ESCache.Instance.ActiveShip.IsDread || ESCache.Instance.ActiveShip.IsMarauder || ESCache.Instance.ActiveShip.IsBattleship)");
                    if (ESCache.Instance.Weapons.Any(i => i.IsHighAngleWeapon || ESCache.Instance.ActiveShip.IsMarauder || ESCache.Instance.ActiveShip.IsBattleship || ESCache.Instance.ActiveShip.IsBattleCruiser))
                    {
                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (ESCache.Instance.Weapons.Any(i => i.IsHighAngleWeapon))");
                        Targeting_WormHoleAnomaly_Dread_HighAngle();
                        return true;
                    }

                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (ESCache.Instance.InWormHoleSpace && ESCache.Instance.ActiveShip.GroupId == (int)GroupID.Dreadnaught)");
                    Targeting_WormHoleAnomaly_Dread_BigGun();
                    return true;
                }

                //
                // 1st of Second room
                //
                //if (ESCache.Instance.AccelerationGates.Any(i => i.Name.Contains("Observatory")) || ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.Contains("Triglavian Stellar Accelerator")) || ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.Contains("Stellar Observatory")))
                //{
                //    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (ESCache.Instance.AccelerationGates.Any(i => i.Name.Contains(Observatory)) || ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.Contains(Triglavian Stellar Accelerator)) || ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.Contains(Stellar Observatory)))");
                //    //Targeting_TriglavianInvasionObservatory();
                //    return true;
                //}

                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer || ESCache.Instance.ActiveShip.IsAssaultShip)
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("DebugTargetCombatants: Targeting_TriglavianInvasion();");
                    Targeting_FrigateAbyssal();
                    return true;
                }

                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: Targeting_TriglavianInvasion();");
                Targeting_TriglavianInvasion();
                return true;
            }

            return false;
        }

        public static bool TargetCombatants()
        {
            try
            {
                if (!CombatIsAppropriateHere("Target"))
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (!CombatIsAppropriateHere())");
                    return false;
                }

                DetermineMaxTargetsICanLock();

                if (State.CurrentHydraState == HydraState.Combat)
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (State.CurrentHydraState == HydraState.Combat)");
                    if (TargetSpecificEntities()) return true;
                    return false;
                }

                if (State.CurrentHydraState == HydraState.Leader)
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (State.CurrentHydraState == HydraState.Leader)");
                    if (PushTargetInfo()) return true;
                    return false;
                }

                if (ESCache.Instance.EveAccount.BotUsesHydra)
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (ESCache.Instance.EveAccount.BotUsesHydra)");
                    PushTargetInfo();
                }

                DebugLogTargetInfo();

                if (!EcmJammingChecks())
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (!EcmJammingChecks()) return false");
                    return false;
                }

                if (!UnlockTargetsThatAreOutOfRange())
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (!UnlockTargetsThatAreOutOfRange()) return false");
                    return false;
                }

                //if (WormholeSpaceTargeting())
                //{
                //    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (!UnlockTargetsThatAreOutOfRange())");
                //    return false;
                //}

                if (MiningTargeting())
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (MiningTargeting()) return true");
                    return true;
                }

                if (AbyssalDeadspaceTargeting())
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (AbyssalDeadspaceTargeting()) return true");
                    return true;
                }

                if (CombatDontMoveControllerTargeting())
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetCombatants: if (CombatDontMoveControllerTargeting()) return true");
                    return true;
                }

                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                {
                    if (ESCache.Instance.InMission)
                    {
                        if (MissionSettings.MyMission != null && MissionSettings.MyMission.Name.Contains("Anomic"))
                        {
                            Targeting_BurnerMissions();
                            return true;
                        }
                    }
                }

                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: Targeting_Defaults();");
                if (!Targeting_Defaults()) return false;

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static void Targeting_WormHoleAnomaly_Dread_BigGun()
        {
            IEnumerable<EntityCache> potentialCombatTargetsToLock = PotentialCombatTargets.Where(e => e.IsReadyToTarget && !e.IsPod && (e.IsNpcCapitalEscalation || (ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Zirnitra && e.IsNPCBattleship) || e.Name.Contains("Arithmos Tyrannos"))) //|| e.IsDecloakedTransmissionRelay))
                .OrderByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.HealthPct < 100)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable)
                .ThenByDescending(j => j.IsNPCBattleship && j.HealthPct < 100)
                .ThenByDescending(j => j.IsNPCBattleship)
                .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                .ThenByDescending(j => j.IsNpcCapitalEscalation && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                .ThenByDescending(j => j.IsNpcCapitalEscalation && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                .ThenByDescending(j => j.IsDecloakedTransmissionRelay)
                .ThenByDescending(j => j.Name.Contains("Arithmos Tyrannos"))
                .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange && j.HealthPct < 100)
                .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange)
                .ThenBy(p => p.StructurePct)
                .ThenBy(q => q.ArmorPct)
                .ThenBy(r => r.ShieldPct);

            TargetTheseEntities(potentialCombatTargetsToLock, "Targeting_WormHoleAnomaly_Dread_BigGun");
        }

        private static void Targeting_WormHoleAnomaly_Dread_HighAngle()
        {
            IEnumerable<EntityCache> potentialCombatTargetsToLock = Combat.PotentialCombatTargets.Where(i => i.IsReadyToTarget && !i.IsPod && !i.IsNPCFrigate && !i.IsSentry)
                            .OrderByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser)
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable)
                            .ThenByDescending(j => j.IsPlayer && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsPlayer)
                            //.ThenByDescending(j => j.IsSentry && j.IsInOptimalRange && j.HealthPct < 100)
                            //.ThenByDescending(j => j.IsSentry && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable)
                            .ThenByDescending(j => j.IsNPCCruiser && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCCruiser)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable)
                            .ThenByDescending(j => j.IsNPCBattleship && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship)
                            .ThenByDescending(j => j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsTrackable)
                            .ThenByDescending(j => j.HealthPct < 100)
                            .ThenBy(p => p.StructurePct)
                            .ThenBy(q => q.ArmorPct)
                            .ThenBy(r => r.ShieldPct);

            TargetTheseEntities(potentialCombatTargetsToLock, "Targeting_WormHoleAnomaly_Dread_HighAngle");
        }


        private static void Targeting_BurnerMissions()
        {
            IEnumerable<EntityCache> potentialCombatTargetsToLock = PotentialCombatTargets.Where(e => !e.IsTarget && !e.IsTargeting && (e.IsBurnerMainNPC || e.IsKillTarget || e.IsDroneKillTarget || e.IsPreferredPrimaryWeaponTarget || e.IsPreferredDroneTarget))
                .OrderByDescending(j => j.IsPreferredPrimaryWeaponTarget)
                .ThenByDescending(j => j.IsPreferredDroneTarget)
                .ThenByDescending(j => j.IsBurnerMainNPC)
                .ThenBy(p => p.StructurePct)
                .ThenBy(q => q.ArmorPct)
                .ThenBy(r => r.ShieldPct);

            TargetTheseEntities(potentialCombatTargetsToLock, "Targeting_BurnerMissions");
        }

        private static void Targeting_WormHoleAnomaly()
        {
            IOrderedEnumerable<EntityCache> potentialCombatTargetsToLock = PotentialCombatTargets.Where(e => e.IsReadyToTarget)
                .OrderByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                .ThenByDescending(j => j.IsNPCBattlecruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600)
                .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(j => j.IsNPCBattleship)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasRemoteRepair)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsSensorDampeningMe)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsWebbingMe)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasNeutralizers)
                .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCBattlecruiser && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(k => k.IsNPCBattlecruiser)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(l => l.IsNPCCruiser)
                .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                .ThenByDescending(j => j.IsNPCFrigate && Drones.DronesKillHighValueTargets && j.IsNeutralizingMe && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                .ThenBy(p => p.StructurePct)
                .ThenBy(q => q.ArmorPct)
                .ThenBy(r => r.ShieldPct);

            TargetTheseEntities(potentialCombatTargetsToLock, "Targeting_WormHoleAnomaly");
        }


        private static IEnumerable<EntityCache> Targeting_AbyssalTargetsToLock { get; set; } = null;

        private static IEnumerable<EntityCache> Targeting_WSpaceDreadTargetsToLock { get; set; } = null;

        private static IEnumerable<EntityCache> Targeting_TriglavianInvasionTargetsToLock { get; set; } = null;
        private static IEnumerable<EntityCache> Targeting_TriglavianInvasionNestorTargetsToLock { get; set; } = null;
        private static IEnumerable<EntityCache> Targeting_TriglavianInvasionLogisticsTargetsToLock { get; set; } = null;
        private static IEnumerable<EntityCache> Targeting_TriglavianInvasionAntiLooterTargetsToLock { get; set; } = null;

        private static void Targeting_TriglavianInvasionObservatory()
        {
            if (Targeting_TriglavianInvasionTargetsToLock == null)
            {
                Targeting_TriglavianInvasionTargetsToLock = PotentialCombatTargets.Where(e => e.IsReadyToTarget && e.WeCanKillThisNPCAndStillHaveEnoughDpsOnFieldToKillLooters)
                .OrderByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                .ThenByDescending(l => l.IsNPCBattlecruiser && !l.IsAttacking)
                .ThenByDescending(k => k.IsNPCBattlecruiser)
                .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule) && !j.IsAttacking)
                .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && !j.IsAttacking)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                .ThenByDescending(j => j.IsNPCCruiser && j.WeShouldFocusFire)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsWithinOptimalOfDrones)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsCloseToDrones)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(j => j.IsNPCBattleship)
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(l => l.IsNPCCruiser)
                .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWithinOptimalOfDrones)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsCloseToDrones)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                .ThenByDescending(j => j.IsNPCFrigate && j.IsHighDps)
                .ThenByDescending(i => i.IsNPCFrigate && i.IsTrackable)
                .ThenByDescending(i => i.IsNPCFrigate && i.IsInOptimalRange)
                .ThenBy(p => p.StructurePct)
                .ThenBy(q => q.ArmorPct)
                .ThenBy(r => r.ShieldPct);
            }

            TargetTheseEntities(Targeting_TriglavianInvasionTargetsToLock, "Targeting_TriglavianInvasionObservatory");
        }

        private static void Targeting_WSpaceDread()
        {
            if (Targeting_WSpaceDreadTargetsToLock == null)
            {
                Targeting_WSpaceDreadTargetsToLock = PotentialCombatTargets.Where(e => e.IsReadyToTarget)
                    .OrderByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(l => l.IsNPCBattlecruiser && !l.IsAttacking)
                    .ThenByDescending(k => k.IsNPCBattlecruiser)
                    .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule) && !j.IsAttacking)
                    .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                    .ThenByDescending(j => j.IsNPCCruiser && j.WeShouldFocusFire)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsWithinOptimalOfDrones)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsCloseToDrones)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(j => j.IsNPCBattleship)
                    .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(l => l.IsNPCCruiser)
                    .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                    .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsWithinOptimalOfDrones)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsCloseToDrones)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                    .ThenByDescending(j => j.IsNPCFrigate && j.IsHighDps)
                    .ThenByDescending(i => i.IsNPCFrigate && i.IsTrackable)
                    .ThenByDescending(i => i.IsNPCFrigate && i.IsInOptimalRange)
                    .ThenBy(p => p.StructurePct)
                    .ThenBy(q => q.ArmorPct)
                    .ThenBy(r => r.ShieldPct);
            }

            TargetTheseEntities(Targeting_WSpaceDreadTargetsToLock, "Targeting_WSpaceDread");
        }

        private static void Targeting_TriglavianInvasion()
        {
            if (Targeting_TriglavianInvasionTargetsToLock == null)
            {
                Targeting_TriglavianInvasionTargetsToLock = PotentialCombatTargets.Where(e => e.IsReadyToTarget)
                    .OrderByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(l => l.IsNPCBattlecruiser && !l.IsAttacking)
                    .ThenByDescending(k => k.IsNPCBattlecruiser)
                    .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule) && !j.IsAttacking)
                    .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && !j.IsAttacking)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                    .ThenByDescending(j => j.IsNPCCruiser && j.WeShouldFocusFire)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsWithinOptimalOfDrones)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsCloseToDrones)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(j => j.IsNPCBattleship)
                    .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(l => l.IsNPCCruiser)
                    .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                    .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                    .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsWithinOptimalOfDrones)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsCloseToDrones)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                    .ThenByDescending(j => j.IsNPCFrigate && j.IsHighDps)
                    .ThenByDescending(i => i.IsNPCFrigate && i.IsTrackable)
                    .ThenByDescending(i => i.IsNPCFrigate && i.IsInOptimalRange)
                    .ThenBy(p => p.StructurePct)
                    .ThenBy(q => q.ArmorPct)
                    .ThenBy(r => r.ShieldPct);
            }

            TargetTheseEntities(Targeting_TriglavianInvasionTargetsToLock, "Targeting_TriglavianInvasion");
        }

        private static void Targeting_TriglavianNestor()
        {
            //if (!ESCache.Instance.DirectEve.Session.InFleet)
            //    return;

            if (Targeting_TriglavianInvasionNestorTargetsToLock == null)
            {
                Targeting_TriglavianInvasionNestorTargetsToLock = ESCache.Instance.MyCorpMatesAsEntities.Where(e => e.IsReadyToTarget &&
                                                                                                                    e.Id != ESCache.Instance.MyShipEntity.Id &&
                                                                                                                    (e.TypeId == (int)TypeID.Nestor || e.IsInMyRepairGroup))
                            .OrderByDescending(j => j.TypeId == (int)TypeID.Nestor)
                            .ThenByDescending(j => j.IsInMyRepairGroup)
                            .ThenByDescending(j => j.GroupId == (int)Group.Logistics)
                            .ThenByDescending(j => j.TypeId == (int)TypeID.Augoror)
                            .ThenByDescending(j => j.IsBattlecruiser)
                            .ThenByDescending(j => j.IsBattleship);

                if (DebugConfig.DebugTargetCombatants)
                {
                    Log.WriteLine("Targeting_TriglavianNestor: InFleet [" + ESCache.Instance.DirectEve.Session.InFleet + "]  ChatChannelToPullFleetInvitesFrom [" + ESCache.Instance.EveAccount.ChatChannelToPullFleetInvitesFrom + "])");
                    int intCorpMate = 0;
                    Log.WriteLine(" - - - - - Start List of MyCorpMateAsEntity - - - - ");
                    foreach (EntityCache MyCorpMateAsEntity in ESCache.Instance.MyCorpMatesAsEntities)
                    {
                        intCorpMate++;
                        Log.WriteLine("MyCorpMateAsEntity [" + intCorpMate + "][" + MyCorpMateAsEntity.Name + "][" + Math.Round(MyCorpMateAsEntity.Distance / 1000, 0) + "k] IsInMyRepairGroup [" + MyCorpMateAsEntity.IsInMyRepairGroup + "]");
                    }
                    Log.WriteLine(" - - - - - End List of MyCorpMateAsEntity - - - - ");

                    int intFleetMember = 0;
                    Log.WriteLine(" - - - - - Start List of MyFleetMembersAsEntities - - - - ");
                    foreach (EntityCache MyFleetMemberAsEntity in ESCache.Instance.MyFleetMembersAsEntities)
                    {
                        intFleetMember++;
                        Log.WriteLine("MyFleetMemberAsEntity [" + intFleetMember + "][" + MyFleetMemberAsEntity.Name + "][" + Math.Round(MyFleetMemberAsEntity.Distance / 1000, 0) + "k] IsInMyRepairGroup [" + MyFleetMemberAsEntity.IsInMyRepairGroup + "]");
                    }
                    Log.WriteLine(" - - - - - End List of MyFleetMembersAsEntities - - - - ");

                    Log.WriteLine("Leader and Slave Info from launcher (all non-leader toons in the same fleet as this leader)");
                    Log.WriteLine("LeaderCharacterName  [" + ESCache.Instance.EveAccount.LeaderCharacterName + "][" + ESCache.Instance.EveAccount.RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName1  [" + ESCache.Instance.EveAccount.SlaveCharacterName1 + "][" + ESCache.Instance.EveAccount.SlaveCharacter1RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName2  [" + ESCache.Instance.EveAccount.SlaveCharacterName2 + "][" + ESCache.Instance.EveAccount.SlaveCharacter2RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName3  [" + ESCache.Instance.EveAccount.SlaveCharacterName3 + "][" + ESCache.Instance.EveAccount.SlaveCharacter3RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName4  [" + ESCache.Instance.EveAccount.SlaveCharacterName4 + "][" + ESCache.Instance.EveAccount.SlaveCharacter4RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName5  [" + ESCache.Instance.EveAccount.SlaveCharacterName5 + "][" + ESCache.Instance.EveAccount.SlaveCharacter5RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName6  [" + ESCache.Instance.EveAccount.SlaveCharacterName6 + "][" + ESCache.Instance.EveAccount.SlaveCharacter6RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName7  [" + ESCache.Instance.EveAccount.SlaveCharacterName7 + "][" + ESCache.Instance.EveAccount.SlaveCharacter7RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName8  [" + ESCache.Instance.EveAccount.SlaveCharacterName8 + "][" + ESCache.Instance.EveAccount.SlaveCharacter8RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName9  [" + ESCache.Instance.EveAccount.SlaveCharacterName9 + "][" + ESCache.Instance.EveAccount.SlaveCharacter9RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName10 [" + ESCache.Instance.EveAccount.SlaveCharacterName10 + "][" + ESCache.Instance.EveAccount.SlaveCharacter10RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName11 [" + ESCache.Instance.EveAccount.SlaveCharacterName11 + "][" + ESCache.Instance.EveAccount.SlaveCharacter11RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName12 [" + ESCache.Instance.EveAccount.SlaveCharacterName12 + "][" + ESCache.Instance.EveAccount.SlaveCharacter12RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName13 [" + ESCache.Instance.EveAccount.SlaveCharacterName13 + "][" + ESCache.Instance.EveAccount.SlaveCharacter13RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName14 [" + ESCache.Instance.EveAccount.SlaveCharacterName14 + "][" + ESCache.Instance.EveAccount.SlaveCharacter14RepairGroup + "]");
                    Log.WriteLine("SlaveCharacterName15 [" + ESCache.Instance.EveAccount.SlaveCharacterName15 + "][" + ESCache.Instance.EveAccount.SlaveCharacter15RepairGroup + "]");
                }
            }

            TargetTheseEntities(Targeting_TriglavianInvasionNestorTargetsToLock, "Targeting_TriglavianNestor");
        }

        private static void Targeting_TriglavianAntiLooter()
        {
            if (Targeting_TriglavianInvasionAntiLooterTargetsToLock == null)
            {
                Targeting_TriglavianInvasionAntiLooterTargetsToLock = ESCache.Instance.Entities.Where(e => e.IsReadyToTarget && e.IsPotentialNinjaLooter)
                            .OrderByDescending(j => j.IsFrigate)
                            .ThenByDescending(j => j.IsCruiser);
            }

            TargetTheseEntities(Targeting_TriglavianInvasionAntiLooterTargetsToLock, "Targeting_TriglavianAntiLooter");
        }

        private static void Targeting_TriglavianLogistics()
        {
            //if (!ESCache.Instance.DirectEve.Session.InFleet)
            //    return;

            if (Targeting_TriglavianInvasionLogisticsTargetsToLock == null)
            {
                IEnumerable<EntityCache> LockTheseFriendlies = null;
                if (ESCache.Instance.MyCorpMatesAsEntities.Count > 0)
                    LockTheseFriendlies = ESCache.Instance.MyCorpMatesAsEntities;
                if (LockTheseFriendlies == null)
                    if (ESCache.Instance.MyFleetMembersAsEntities.Count > 0)
                        LockTheseFriendlies = ESCache.Instance.MyFleetMembersAsEntities;

                Targeting_TriglavianInvasionLogisticsTargetsToLock = LockTheseFriendlies.Where(e => e.IsReadyToTarget &&
                                                                                                    e.Id != ESCache.Instance.MyShipEntity.Id)
                            //(e.TypeId == (int)TypeID.Nestor || e.TypeId == (int)TypeID.Augoror || e.GroupId == (int)Group.Logistics))
                            .OrderByDescending(j => j.IsInMyRepairGroup);
                            //.ThenByDescending(j => j.TypeId == (int)TypeID.Nestor)
                            //.ThenByDescending(j => j.GroupId == (int)Group.Logistics)
                            //.ThenByDescending(j => j.TypeId == (int)TypeID.Augoror)
                            //.ThenByDescending(j => j.IsBattlecruiser);
                            //.ThenByDescending(j => j.IsBattleship);
            }

            TargetTheseEntities(Targeting_TriglavianInvasionLogisticsTargetsToLock, "Targeting_TriglavianLogistics");
        }

        private static void Targeting_FrigateAbyssal()
        {
            IOrderedEnumerable<EntityCache> potentialCombatTargetsToLock = PotentialCombatTargets.Where(e => e.IsReadyToTarget)
                .OrderByDescending(l => l.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && l.IsAbyssalDeadspaceTriglavianBioAdaptiveCacheReadyToTarget)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                .ThenByDescending(j => j.IsNPCFrigate && j.IsHighDps)
                .ThenByDescending(i => i.IsNPCFrigate && i.IsTrackable)
                .ThenByDescending(i => i.IsNPCFrigate && i.IsInOptimalRange)
                .ThenByDescending(j => j.IsNPCCruiser && j.WeShouldFocusFire)
                .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(l => l.IsNPCCruiser)
                .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule) && !j.IsAttacking)
                .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && !j.IsAttacking)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                .ThenByDescending(k => k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                //.ThenByDescending(k => k.IsAbyssalDeadspaceTriglavianExtractionNode)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsWithinOptimalOfDrones)
                .ThenByDescending(l => l.IsNPCCruiser && l.IsCloseToDrones)
                .ThenByDescending(l => l.IsNPCBattlecruiser && !l.IsAttacking)
                .ThenByDescending(k => k.IsNPCBattlecruiser)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                .ThenByDescending(l => l.IsNPCBattleship && l.IsWithinOptimalOfDrones)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsCloseToDrones)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                .ThenByDescending(j => j.IsNPCBattleship)
                .ThenBy(p => p.StructurePct)
                .ThenBy(q => q.ArmorPct)
                .ThenBy(r => r.ShieldPct);

            TargetTheseEntities(potentialCombatTargetsToLock, "Targeting_FrigateAbyssal");
        }

        private static void Targeting_AbyssalConstructionYard()
        {
            try
            {
                IOrderedEnumerable<EntityCache> potentialCombatTargetsToLock = PotentialCombatTargets.Where(e => e.IsReadyToTarget)
                    //.OrderByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .OrderByDescending(k => k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                    //.ThenByDescending(k => k.IsAbyssalDeadspaceTriglavianExtractionNode)
                    .ThenByDescending(j => j.IsNPCFrigate && Drones.DronesKillHighValueTargets && j.IsNeutralizingMe && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                    .ThenBy(p => p.StructurePct)
                    .ThenBy(q => q.ArmorPct)
                    .ThenBy(r => r.ShieldPct);

                TargetTheseEntities(potentialCombatTargetsToLock, "Targeting_AbyssalConstructionYard");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool Targeting_Defaults()
        {
            if (TargetingSlotsAreAvailable)
            {
                if (!PotentialCombatTargets.Any())
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (!PotentialCombatTargets.Any())");

                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (TargetingSlotsAreAvailable)");
                if (PotentialCombatTargets.Any(i => i.IsTargetedBy))
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (PotentialCombatTargets.Any(i => i.IsTargetedBy))");
                    IOrderedEnumerable<EntityCache> tempTargetsToLock = null;

                    if (TargetingSlotsAreAvailableForHighValueTargets)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (TargetingSlotsAreAvailableForHighValueTargets)");
                        tempTargetsToLock = PickWhatToLockNextBasedOnTargetsForBattleship;
                        if (tempTargetsToLock.Any())
                        {
                            LockOptimalTargets(tempTargetsToLock, true, maxHighValueTargets ?? 3, "Targeting (Large)");
                        }
                    }

                    if (TargetingSlotsAreAvailableForLowValueTargets)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (TargetingSlotsAreAvailableForLowValueTargets)");
                        tempTargetsToLock = PickWhatToLockNextBasedOnTargetsForLowValueTargets;
                        if (tempTargetsToLock.Any())
                        {
                            LockOptimalTargets(tempTargetsToLock, true, maxLowValueTargets ?? 2, "Targeting (Small)");
                        }
                    }
                }
                else if (PotentialCombatTargets.All(i => !i.IsTarget && !i.IsTargeting))
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("else if (PotentialCombatTargets.All(i => !i.IsTarget && !i.IsTargeting))");
                    if (Time.Instance.LastInWarp.AddSeconds(15) > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (Time.Instance.LastInWarp.AddSeconds(15) > DateTime.UtcNow)");
                        return true;
                    }

                    LockOptimalTargets(PotentialCombatTargets.Where(i => !i.IsTarget && !i.IsTargeting).OrderBy(j => j.Distance), true, maxLowValueTargets ?? 2, "Targeting (Lock closest target)");
                }
                else if (PotentialCombatTargets.Any(i => !i.IsTarget && !i.IsTargeting))
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("else if (PotentialCombatTargets.Any(i => !i.IsTarget && !i.IsTargeting))");
                    if (Time.Instance.LastInWarp.AddSeconds(15) > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (Time.Instance.LastInWarp.AddSeconds(15) > DateTime.UtcNow)");
                        return true;
                    }

                    LockOptimalTargets(PotentialCombatTargets.Where(i => !i.IsTarget && !i.IsTargeting).OrderBy(j => j.Distance).ThenBy(x => x.Velocity != 0), true, maxLowValueTargets ?? 2, "Targeting (Lock closest moving target)");
                }
            }

            return true;
        }

        private static void TargetTheseEntities(IEnumerable<EntityCache> Targeting_TargetsToLock, string TargetingRoutineDescription)
        {
            if (DebugConfig.DebugTargetCombatants)
            {
                if (Targeting_TargetsToLock != null && Targeting_TargetsToLock.Any(i => i.IsReadyToTarget))
                {
                    int targetnum = 0;
                    Log.WriteLine("DebugTargetCombatants: Lock Targets in this order:");
                    foreach (EntityCache potentialCombatTargetToLock in Targeting_TargetsToLock.Where(i => i.IsReadyToTarget))
                    {
                        targetnum++;
                        Log.WriteLine("[" + targetnum + "][" + potentialCombatTargetToLock.Name + "] at [" + potentialCombatTargetToLock.Nearest1KDistance + "k] MaskedID [" + potentialCombatTargetToLock.MaskedId + "] IsTarget [" + potentialCombatTargetToLock.IsTarget + "] IsTargeting [" + potentialCombatTargetToLock.IsTargeting + "]");
                    }
                    Log.WriteLine("DebugTargetCombatants: End Of List");
                }
                else
                {
                    if (Targeting_TargetsToLock == null)
                    {
                        Log.WriteLine("DebugTargetCombatants: !! if (Targeting_TargetsToLock == null) !!");
                    }
                    else
                    {
                        if (!Targeting_TargetsToLock.Any())
                        {
                            Log.WriteLine("DebugTargetCombatants: !! if (!Targeting_TargetsToLock.Any()) !!!");
                        }

                        if (!Targeting_TargetsToLock.Any(i => i.IsReadyToTarget))
                        {
                            Log.WriteLine("DebugTargetCombatants: !! Targeting_TargetsToLock [" + Targeting_TargetsToLock.Count() + "] if (!Targeting_TargetsToLock.Any(i => i.IsReadyToTarget)) !!!");
                        }
                    }
                }
            }

            if (Targeting_TargetsToLock == null || !Targeting_TargetsToLock.Any(i => i.IsReadyToTarget))
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: if (Targeting_TargetsToLock == null || !Targeting_TargetsToLock.Any(i => i.IsReadyToTarget))");
                return;
            }

            int TotalLockedTargets = 0;
            if (ESCache.Instance.TotalTargetsAndTargeting.Count > 0)
                TotalLockedTargets = ESCache.Instance.TotalTargetsAndTargeting.Count;

            if (Targeting_TargetsToLock.Any() && TotalLockedTargets < ESCache.Instance.MaxLockedTargets)
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: MyMaxTargetRange [" + MaxTargetRange + "] [" + Targeting_TargetsToLock.Count() +
                                  "] IsNotYetTargetingMeAndNotYetTargeted targets");

                /**
                foreach (EntityCache LockedTarget in ESCache.Instance.Targets.Where(i => i.IsNpc && i.Velocity > 0 && !i.IsWreck && !i.IsDroneKillTarget && !i.IsKillTarget))
                {
                    if (Targeting_TargetsToLock.Any(i => i.Id == LockedTarget.Id))
                    {
                        continue;
                    }

                    Log.WriteLine("----------");
                    int intTargetToLock = 0;
                    foreach (var Targeting_TargetToLock in Targeting_TargetsToLock)
                    {
                        intTargetToLock++;
                        Log.WriteLine("Targeting_TargetToLock: #[" + intTargetToLock + "][" + Targeting_TargetToLock.Name + "][" + Targeting_TargetToLock.Nearest1KDistance + "] ID [" + Targeting_TargetToLock.MaskedId + "] IsTarget [" + Targeting_TargetToLock.IsTarget + "] IsTargeting [" + Targeting_TargetToLock.IsTargeting + "]");
                    }

                    Log.WriteLine("----------");

                    Log.WriteLine("TargetCombatants: [" + LockedTarget.Name + "][" + LockedTarget.Nearest1KDistance + "][" + LockedTarget.Id + "] was locked but not found in Targeting_TargetsToLock - Unlocking!");
                    LockedTarget.UnlockTarget();
                }
                **/



                foreach (EntityCache targetToLock in Targeting_TargetsToLock.Where(i => i.IsReadyToTarget))
                {
                    if (targetToLock != null)
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants [" + targetToLock.Name + "][" + Math.Round(targetToLock.Distance / 1000, 0) + "k] IsReadyToTarget [" + targetToLock.IsReadyToTarget + "]");

                        //if (targetToLock.IsDecloakedTransmissionRelay)
                        //{
                        //    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("DebugTargetCombatants: [" + targetToLock.Name + "][" + Math.Round(targetToLock.Distance / 1000, 0) + "k] IsDecloakedTransmissionRelay");
                        //    continue;
                        //}

                        if (targetToLock.IsReadyToTarget
                        && targetToLock.LockTarget(TargetingRoutineDescription))
                        {
                            Log.WriteLine("Targeting [" + targetToLock.Name + "][GroupID: " +
                                          targetToLock.GroupId +
                                          "][TypeID: " + targetToLock.TypeId + "][" + targetToLock.MaskedId + "][" +
                                          Math.Round(targetToLock.Distance / 1000, 0) + "k away] HasInitiatedWarp [" + targetToLock.HasInitiatedWarp + "]");
                            if (ESCache.Instance.TotalTargetsAndTargeting.Count > 0 &&
                                ESCache.Instance.TotalTargetsAndTargeting.Count >= ESCache.Instance.MaxLockedTargets)
                                Time.Instance.NextTargetAction = DateTime.UtcNow.AddSeconds(Time.Instance.TargetsAreFullDelay_seconds);

                            return;
                        }
                    }
                }
            }
            else
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: 0 IsNotYetTargetingMeAndNotYetTargeted targets");
            }
        }

        private static IOrderedEnumerable<EntityCache> PickWhatToLockNextBasedOnTargetsForBattleship
        {
            get
            {
                if (_pickWhatToLockNextBasedOnTargetsForABattleship == null)
                {
                    if (DebugConfig.DebugKillTargets) Log.WriteLine("PickWhatToLockNextBasedOnTargetsForBattleship: IsPossibleToShoot # [" + Combat.PotentialCombatTargets.Count(i => i.IsPossibleToTarget) + "]");
                    if (DebugConfig.DebugKillTargets) Log.WriteLine("PickWhatToLockNextBasedOnTargetsForBattleship: IsReadyToShoot # [" + Combat.PotentialCombatTargets.Count(i => i.IsReadyToTarget) + "]");

                    _pickWhatToLockNextBasedOnTargetsForABattleship = PotentialCombatTargets.Where(i => !i.IsContainer && !i.IsBadIdea && !i.IsIgnored && i.IsPossibleToTarget && (i.IsHighValueTarget || (i.IsLargeCollidable && (i.IsPrimaryWeaponKillPriority || i.IsDronePriorityTarget))))
                        .OrderByDescending(j => j.IsWarpScramblingMe)
                        //.ThenByDescending(j => !j.IsTrigger)
                        .ThenByDescending(i => i.IsLargeCollidableWeAlwaysWantToBlowupFirst)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTrackable && ESCache.Instance.ActiveShip.HasSpeedMod && g.IsWebbingMe && 100 > g.HealthPct)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTrackable && ESCache.Instance.ActiveShip.HasSpeedMod && g.IsWebbingMe)
                        .ThenByDescending(j => j.IsPrimaryWeaponPriorityTarget)
                        //.ThenByDescending(i => i.IsCorrectSizeForMyWeapons && i.IsWarpScramblingMe && !i.IsLargeCollidable)
                        //.ThenByDescending(i => i.IsCorrectSizeForMyWeapons)
                        //.ThenByDescending(i => i.IsEntityIShouldKeepShooting)

                        //
                        // Kill Aggro first
                        //
                        .ThenByDescending(i => i.IsNPCBattleship && 100 > i.HealthPct && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisNeutralizingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisWarpScramblingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisSensorDampeningNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTrackingDisruptingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTargetPaintingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking && i.IsTrackable && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && ESCache.Instance.Weapons.Any(i => i.IsTurret) && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking && i.IsShortestRangeAmmoInRange)

                        .ThenByDescending(i => i.IsNPCBattleship && 100 > i.HealthPct && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisNeutralizingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisWarpScramblingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisSensorDampeningNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTrackingDisruptingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTargetPaintingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking)

                        .ThenByDescending(i => i.IsNPCBattlecruiser && 100 > i.HealthPct && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisNeutralizingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisWarpScramblingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisSensorDampeningNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTrackingDisruptingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTargetPaintingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsShortestRangeAmmoInRange)

                        .ThenByDescending(i => i.IsNPCBattlecruiser && 100 > i.HealthPct && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisNeutralizingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisWarpScramblingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisSensorDampeningNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTrackingDisruptingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTargetPaintingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking)

                        .ThenByDescending(i => i.IsNPCCruiser && 100 > i.HealthPct && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisNeutralizingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWarpScramblingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisSensorDampeningNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTrackingDisruptingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTargetPaintingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking && i.IsShortestRangeAmmoInRange)

                        .ThenByDescending(i => i.IsNPCCruiser && 100 > i.HealthPct && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisNeutralizingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWarpScramblingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisSensorDampeningNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTrackingDisruptingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTargetPaintingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking)

                        //If the PotentialCombatTarget is repote repairing we should prioritize that target! but we do not

                        .ThenByDescending(i => i.IsNPCBattleship && 100 > i.HealthPct && i.IsTargetedBy)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsTargetedBy)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 100 > i.HealthPct && i.IsTargetedBy)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsTargetedBy)
                        .ThenByDescending(i => i.IsNPCCruiser && 100 > i.HealthPct && i.IsTargetedBy)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsTargetedBy)

                        //
                        // These are not aggo'd yet - kick the ant hill
                        //
                        .ThenByDescending(i => i.IsNPCBattleship && 5000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 10000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 15000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 20000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 25000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 30000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 35000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 40000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 45000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 50000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 55000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 60000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 65000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && 70000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattleship && i.Distance > 70000 && Combat.TargetedByCount == 0)

                        .ThenByDescending(i => i.IsNPCBattlecruiser && 5000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 10000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 15000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 20000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 25000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 30000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 35000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 40000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 45000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 50000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 55000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 60000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 65000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 70000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.Distance > 70000 && Combat.TargetedByCount == 0)

                        .ThenByDescending(i => i.IsNPCCruiser && 5000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 10000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 15000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 20000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 25000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 30000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 35000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 40000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 45000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 50000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 55000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 60000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 65000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && 70000 > i.Distance && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsNPCCruiser && i.Distance > 70000 && Combat.TargetedByCount == 0)


                        .ThenByDescending(i => i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsAttacking)
                        .ThenByDescending(i => i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && Combat.TargetedByCount == 0)
                        .ThenByDescending(i => i.IsTargetedBy)
                        //.ThenByDescending(i => i.IsAttacking && i.IsPlayer && i.IsBattlecruiser)
                        //.ThenByDescending(i => i.IsAttacking && i.IsPlayer && i.IsBattleship)
                        //.ThenByDescending(i => i.IsAttacking && i.IsPlayer && i.IsCruiser)

                        .ThenByDescending(i => i.IsLargeCollidableWeAlwaysWantToBlowupLast);

                    if (DebugConfig.DebugLogOrderOfKillTargets)
                        LogOrderOfKillTargets(_pickWhatToLockNextBasedOnTargetsForABattleship);

                    return _pickWhatToLockNextBasedOnTargetsForABattleship;
                }

                return _pickWhatToLockNextBasedOnTargetsForABattleship;
            }
        }

        private static IOrderedEnumerable<EntityCache> PickWhatToLockNextBasedOnTargetsForLowValueTargets
        {
            get
            {
                if (_pickWhatToLockNextBasedOnTargetsForLowValueTargets == null)
                {
                    _pickWhatToLockNextBasedOnTargetsForLowValueTargets = PotentialCombatTargets.Where(i => !i.IsContainer && !i.IsBadIdea && !i.IsIgnored && i.IsInDroneRange && i.IsReadyToTarget && !i.IsHighValueTarget)
                        .OrderByDescending(a => a.IsWarpScramblingMe && 100 > a.HealthPct)
                        .ThenByDescending(b => b.IsWarpScramblingMe)
                        .ThenByDescending(b => b.WarpScrambleChance > 0 && 100 > b.HealthPct && b.IsTargetedBy)
                        .ThenByDescending(b => b.WarpScrambleChance > 0 && b.IsTargetedBy)
                        .ThenByDescending(c => c.IsPreferredDroneTarget && 100 > c.HealthPct)
                        .ThenByDescending(c => c.IsPreferredDroneTarget)
                        //.ThenByDescending(j => !j.IsTrigger)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsWebbingMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsWebbingMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsNeutralizingMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsNeutralizingMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTrackingDisruptingMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTrackingDisruptingMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTryingToJamMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTryingToJamMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsSensorDampeningMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsSensorDampeningMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTargetPaintingMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && g.IsTargetPaintingMe && g.IsTargetedBy)

                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsWebbingMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsWebbingMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsNeutralizingMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsNeutralizingMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTrackingDisruptingMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTrackingDisruptingMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTryingToJamMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTryingToJamMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsSensorDampeningMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsSensorDampeningMe && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTargetPaintingMe && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && g.IsTargetPaintingMe && g.IsTargetedBy)

                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && 100 > g.HealthPct && g.IsTargetedBy)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && 100 > g.HealthPct && g.IsTargetedBy)

                        .ThenByDescending(g => g.IsDestroyer || g.IsNPCDestroyer && g.IsTargetedBy)
                        .ThenByDescending(g => g.IsFrigate || g.IsNPCFrigate && g.IsTargetedBy)

                        .ThenByDescending(g => (g.IsDestroyer || g.IsNPCDestroyer) && Combat.TargetedByCount == 0)
                        .ThenByDescending(g => (g.IsFrigate || g.IsNPCFrigate) && Combat.TargetedByCount == 0)

                        .ThenByDescending(f => f.IsAttacking)
                        .ThenByDescending(e => e.IsTargetedBy);

                    if (DebugConfig.DebugLogOrderOfKillTargets)
                        LogOrderOfKillTargets(_pickWhatToLockNextBasedOnTargetsForLowValueTargets);

                    return _pickWhatToLockNextBasedOnTargetsForLowValueTargets;
                }

                return _pickWhatToLockNextBasedOnTargetsForLowValueTargets;
            }
        }

        private static bool TargetingSlotsAreAvailable
        {
            get
            {
                try
                {
                    int totalLockedTargets = 0;
                    if (ESCache.Instance.TotalTargetsAndTargeting.Count > 0)
                        totalLockedTargets = ESCache.Instance.TotalTargetsAndTargeting.Count;

                    if (ESCache.Instance.MaxLockedTargets > totalLockedTargets)
                        return true;

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private static bool TargetingSlotsAreAvailableForLowValueTargets
        {
            get
            {
                try
                {
                    if (!TargetingSlotsAreAvailable)
                        return false;

                    int totalLockedLowValueTargets = 0;
                    if (ESCache.Instance.TotalTargetsAndTargeting.Count > 0)
                        totalLockedLowValueTargets = ESCache.Instance.TotalTargetsAndTargeting.Count(i => i.IsLowValueTarget);

                    if (maxLowValueTargets > totalLockedLowValueTargets)
                        return true;

                    if (!PotentialCombatTargets.Any(i => i.IsHighValueTarget))
                    {
                        if (maxLowValueTargets + 1 > totalLockedLowValueTargets)
                            return true;
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

        private static bool TargetingSlotsAreAvailableForHighValueTargets
        {
            get
            {
                try
                {
                    if (!TargetingSlotsAreAvailable)
                        return false;

                    int totalLockedHighValueTargets = 0;
                    if (ESCache.Instance.TotalTargetsAndTargeting.Count > 0)
                        totalLockedHighValueTargets = ESCache.Instance.TotalTargetsAndTargeting.Count(i => i.IsHighValueTarget);

                    if (maxLowValueTargets > totalLockedHighValueTargets)
                        return true;

                    //if (!PotentialCombatTargets.Any(i => i.DictionaryIsLowValueTarget))
                    //{
                    //    if (maxHighValueTargets + 1 > totalLockedHighValueTargets)
                    //        return true;
                    //}

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private static bool DebugLogOrderTargetsWillBeTargeted(IOrderedEnumerable<EntityCache> potentialTargetsToLock)
        {
            try
            {
                if (DebugConfig.DebugTargetCombatants)
                {
                    int targetnum = 0;
                    Log.WriteLine("DebugTargetCombatants: Lock Targets in this order:");
                    foreach (EntityCache potentialTargetToLock in potentialTargetsToLock)
                    {
                        targetnum++;
                        Log.WriteLine("[" + targetnum + "][" + potentialTargetToLock.Name + "][" + Math.Round(potentialTargetToLock.Distance / 1000, 0) + "] Attacking [" + potentialTargetToLock.IsAttacking + "]");
                    }
                    Log.WriteLine("DebugTargetCombatants: End Of List");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        private static bool LockOptimalTargets(IOrderedEnumerable<EntityCache> potentialCombatTargetsToLock, bool doWeHaveEnoughOfTheCorrectTargetingSlotsAvailable, int maxCorrectlySizedTargetingSlots, string module)
        {
            try
            {
                if (potentialCombatTargetsToLock.Any())
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (potentialCombatTargetsToLock.Any())");
                    if (doWeHaveEnoughOfTheCorrectTargetingSlotsAvailable)
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: [" + potentialCombatTargetsToLock.Count() +
                                          "] IsNotYetTargetingMeAndNotYetTargeted targets");

                        foreach (EntityCache potentialCombatTargetToLock in potentialCombatTargetsToLock.Take(maxCorrectlySizedTargetingSlots))
                            if (potentialCombatTargetToLock != null
                                && potentialCombatTargetToLock.Distance < MaxTargetRange
                                && potentialCombatTargetToLock.IsReadyToTarget
                                && potentialCombatTargetToLock.LockTarget(module))
                            {
                                Log.WriteLine("Targeting [" + potentialCombatTargetToLock.Name + "][GroupID: " +
                                              potentialCombatTargetToLock.GroupId +
                                              "][TypeID: " + potentialCombatTargetToLock.TypeId + "][" + potentialCombatTargetToLock.MaskedId + "][" +
                                              Math.Round(potentialCombatTargetToLock.Distance / 1000, 0) + "k away]");

                                if (ESCache.Instance.TotalTargetsAndTargeting.Count >= ESCache.Instance.MaxLockedTargets)
                                {
                                    Time.Instance.NextTargetAction = DateTime.UtcNow.AddSeconds(Time.Instance.TargetsAreFullDelay_seconds);
                                }

                                return false;
                            }

                        return true;
                    }

                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public static bool TargetSpecificEntities()
        {
            if (DebugConfig.DebugTargetCombatants) Log.WriteLine("TargetSpecificEntities");
            BuildListLeaderTargets();

            if (ListLeaderTargets != null && ListLeaderTargets.Count > 0)
            {
                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (ListLeaderTargets != null && ListLeaderTargets.Any())");
                foreach (EntityCache unlockTarget in ESCache.Instance.Targets.Where(i => i.IsPlayer && ListLeaderTargets.All(a => a != i.Id)))
                {
                    Log.WriteLine("unlocktarget [" + unlockTarget.Name + "]!.!");
                    if (unlockTarget.UnlockTarget())
                        return false;
                }

                if (ESCache.Instance.Targets.Count >= ESCache.Instance.MaxLockedTargets)
                {
                    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("if (ESCache.Instance.Targets.Count() >= ESCache.Instance.MaxLockedTargets)");
                    return false;
                }

                foreach (long entityId in ListLeaderTargets)
                    foreach (EntityCache lockTarget in ESCache.Instance.EntitiesOnGrid.Where(i => !i.IsTarget && !i.IsTargeting && i.Distance < ESCache.Instance.ActiveShip.MaxTargetRange))
                        if (lockTarget.Id == entityId)
                        {
                            if (lockTarget.LockTarget("Lock"))
                                Log.WriteLine("LeaderTargets: Locking [" + lockTarget + "] at [" + Math.Round(lockTarget.Distance / 1000, 0) + "k]");

                            return false;
                        }

                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("no targets to lock.");
                return false;
            }

            if (ESCache.Instance.Targets.Count > 0)
                foreach (EntityCache target in ESCache.Instance.Targets)
                {
                    target.UnlockTarget();
                    return false;
                }

            if (DebugConfig.DebugTargetCombatants) Log.WriteLine("ListLeaderTargets is empty.");
            return false;
        }

        #endregion Methods
    }
}