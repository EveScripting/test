extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Events;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using System.Diagnostics;
using SC::SharedComponents.Utility;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.BackgroundTasks
{
    public static class Defense
    {
        #region Constructors

        static Defense()
        {
            Interlocked.Increment(ref DefenseInstances);
        }

        #endregion Constructors

        #region Fields

        private static bool attemptOverloadHighSlot1;
        private static bool attemptOverloadHighSlot2;
        private static bool attemptOverloadHighSlot3;
        private static bool attemptOverloadHighSlot4;
        private static bool attemptOverloadHighSlot5;
        private static bool attemptOverloadHighSlot6;
        private static bool attemptOverloadHighSlot7;
        private static bool attemptOverloadHighSlot8;
        private static bool attemptOverloadLowSlot1;
        private static bool attemptOverloadLowSlot2;
        private static bool attemptOverloadLowSlot3;
        private static bool attemptOverloadLowSlot4;
        private static bool attemptOverloadLowSlot5;
        private static bool attemptOverloadLowSlot6;
        private static bool attemptOverloadLowSlot7;
        private static bool attemptOverloadLowSlot8;
        private static bool attemptOverloadMidSlot1;
        private static bool attemptOverloadMidSlot2;
        private static bool attemptOverloadMidSlot3;
        private static bool attemptOverloadMidSlot4;
        private static bool attemptOverloadMidSlot5;
        private static bool attemptOverloadMidSlot6;
        private static bool attemptOverloadMidSlot7;
        private static bool attemptOverloadMidSlot8;
        private static bool attemptOverloadRackHigh;
        private static bool attemptOverloadRackLow;
        private static bool attemptOverloadRackMedium;
        private static int DefenseInstances;

        private static DateTime LastActivateSpeedMod;
        private static DateTime LastCloaked;

        private static readonly Dictionary<long, DateTime> NextScriptReload = new Dictionary<long, DateTime>();
        private static int _attemptsToOnlineOfflineModules;
        private static int _deactivateOverloadHighSlot1Seconds;
        private static int _deactivateOverloadHighSlot2Seconds;
        private static int _deactivateOverloadHighSlot3Seconds;
        private static int _deactivateOverloadHighSlot4Seconds;
        private static int _deactivateOverloadHighSlot5Seconds;
        private static int _deactivateOverloadHighSlot6Seconds;
        private static int _deactivateOverloadHighSlot7Seconds;
        private static int _deactivateOverloadHighSlot8Seconds;
        private static int _deactivateOverloadLowSlot1Seconds;
        private static int _deactivateOverloadLowSlot2Seconds;
        private static int _deactivateOverloadLowSlot3Seconds;
        private static int _deactivateOverloadLowSlot4Seconds;
        private static int _deactivateOverloadLowSlot5Seconds;
        private static int _deactivateOverloadLowSlot6Seconds;
        private static int _deactivateOverloadLowSlot7Seconds;
        private static int _deactivateOverloadLowSlot8Seconds;
        private static int _deactivateOverloadMidSlot1Seconds;
        private static int _deactivateOverloadMidSlot2Seconds;
        private static int _deactivateOverloadMidSlot3Seconds;
        private static int _deactivateOverloadMidSlot4Seconds;
        private static int _deactivateOverloadMidSlot5Seconds;
        private static int _deactivateOverloadMidSlot6Seconds;
        private static int _deactivateOverloadMidSlot7Seconds;
        private static int _deactivateOverloadMidSlot8Seconds;
        private static DateTime _lastOverloadHighSlot1 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadHighSlot2 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadHighSlot3 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadHighSlot4 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadHighSlot5 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadHighSlot6 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadHighSlot7 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadHighSlot8 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadLowSlot1 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadLowSlot2 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadLowSlot3 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadLowSlot4 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadLowSlot5 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadLowSlot6 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadLowSlot7 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadLowSlot8 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadMidSlot1 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadMidSlot2 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadMidSlot3 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadMidSlot4 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadMidSlot5 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadMidSlot6 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadMidSlot7 = DateTime.UtcNow.AddMinutes(-5);
        private static DateTime _lastOverloadMidSlot8 = DateTime.UtcNow.AddMinutes(-5);
        private static int _sensorBoosterScriptAttempts;
        private static int _sensorDampenerScriptAttempts;
        private static int _trackingComputerScriptAttempts;
        private static int _trackingDisruptorScriptAttempts;
        private static int _trackingLinkScriptAttempts;
        public static bool GlobalAllowOverLoadOfHardeners;
        public static bool GlobalAllowOverLoadOfReps;
        public static bool GlobalAllowOverLoadOfSpeedMod;
        public static int GlobalRepsOverloadDamageAllowed = 50;
        private static bool overloadHighSlot1Completed;
        private static bool overloadHighSlot2Completed;
        private static bool overloadHighSlot3Completed;
        private static bool overloadHighSlot4Completed;
        private static bool overloadHighSlot5Completed;
        private static bool overloadHighSlot6Completed;
        private static bool overloadHighSlot7Completed;
        private static bool overloadHighSlot8Completed;
        private static bool overloadLowSlot1Completed;
        private static bool overloadLowSlot2Completed;
        private static bool overloadLowSlot3Completed;
        private static bool overloadLowSlot4Completed;
        private static bool overloadLowSlot5Completed;
        private static bool overloadLowSlot6Completed;
        private static bool overloadLowSlot7Completed;
        private static bool overloadLowSlot8Completed;
        private static bool overloadMidSlot1Completed;
        private static bool overloadMidSlot2Completed;
        private static bool overloadMidSlot3Completed;
        private static bool overloadMidSlot4Completed;
        private static bool overloadMidSlot5Completed;
        private static bool overloadMidSlot6Completed;
        private static bool overloadMidSlot7Completed;

        private static bool overloadMidSlot8Completed;

        private static bool overloadRackHighCompleted;

        private static bool overloadRackLowCompleted;
        private static bool overloadRackMediumCompleted;

        public static HashSet<long> BoosterTypesToLoadIntoCargo = new HashSet<long>();

        public static int ActivateThisCombatBoosterTypeIdWhenShieldsAreLow;
        public static int ActivateThisCombatBoosterTypeIdWhenArmorIsLow;
        public static int GlobalActivateCombatBoosterWhenShieldsAreBelowThisPercentage;
        public static int ActivateCombatBoosterWhenShieldsAreBelowThisPercentage
        {
            get
            {
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                    {
                        return 101;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                {
                    if (Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 8)
                    {
                        return 101;
                    }
                }

                return GlobalActivateCombatBoosterWhenShieldsAreBelowThisPercentage;
            }
        }
        public static int ActivateCombatBoosterWhenArmorIsBelowThisPercentage;

        #endregion Fields

        #region Properties

        private static int OverloadRepairModulesAtThisPercGlobalSetting = 40;

        private static int OverloadHardenerModulesAtThisPercGlobalSetting = 40;

        private static int? ActivateAssaultDamageControlModulesAtThisPercMissionSetting { get; set; }

        private static int? ActivateAssaultDamageControlModulesAtThisPercGlobalSetting { get; set; }

        private static int ActivateAssaultDamageControlModulesAtThisPerc
        {
            get
            {
                try
                {
                    if (ActivateAssaultDamageControlModulesAtThisPercMissionSetting != null)
                        return (int)ActivateAssaultDamageControlModulesAtThisPercMissionSetting;

                    if (ActivateAssaultDamageControlModulesAtThisPercGlobalSetting != null)
                    {
                        try
                        {
                            return (int)ActivateAssaultDamageControlModulesAtThisPercGlobalSetting;
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                            return (int)ActivateAssaultDamageControlModulesAtThisPercGlobalSetting;
                        }
                    }

                    return 70;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 70;
                }
            }
        }

        private static int ActivateRepairModulesAtThisPerc
        {
            get
            {
                try
                {
                    if (ActivateRepairModulesAtThisPercMissionSetting != null)
                        return (int) ActivateRepairModulesAtThisPercMissionSetting;

                    if (ESCache.Instance.IsPVPGankLikely)
                    {
                        //Overload when taking any damage!
                        return 99;
                    }

                    if (ActivateRepairModulesAtThisPercGlobalSetting != null)
                    {
                        try
                        {
                            //if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.ActiveShip.IsLocatedWithinFilamentCloud && 40 > ESCache.Instance.ActiveShip.CapacitorPercentage && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule))
                            if (ESCache.Instance.InAbyssalDeadspace && (ESCache.Instance.MyShipEntity.TypeId == (int)TypeID.Gila || ESCache.Instance.MyShipEntity.TypeId == (int)TypeID.Ishtar || ESCache.Instance.MyShipEntity.TypeId == (int)TypeID.Vagabond || ESCache.Instance.MyShipEntity.TypeId == (int)TypeID.StormBringer) && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule))
                            {
                                if (40 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                                {
                                    return 50;
                                }

                                if (ESCache.Instance.ActiveShip.Entity.IsLocatedWithinFilamentCloud)
                                {
                                    if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                                    {
                                        return 51;
                                    }

                                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                                    {
                                        if (Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 4)
                                        {
                                            return (int)ActivateRepairModulesAtThisPercGlobalSetting;
                                        }

                                        return 51;
                                    }

                                }

                                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                                {
                                    return 90;
                                }

                                //if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordBSSpawn && Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                                //{
                                //    return 101;
                                //}
                            }


                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                            return (int)ActivateRepairModulesAtThisPercGlobalSetting;
                        }

                        return (int)ActivateRepairModulesAtThisPercGlobalSetting;
                    }

                    return 70;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 70;
                }
            }
        }

        private static int? ActivateRepairModulesAtThisPercGlobalSetting { get; set; }

        private static int? ActivateRepairModulesAtThisPercMissionSetting { get; set; }

        private static int ActivateSecondRepairModulesAtThisPerc
        {
            get
            {
                try
                {
                    if (ActivateSecondRepairModulesAtThisPercMissionSetting != null)
                        return (int) ActivateSecondRepairModulesAtThisPercMissionSetting;

                    if (ActivateSecondRepairModulesAtThisPercGlobalSetting != null)
                    {
                        if (Combat.Combat.PotentialCombatTargets.Count > 0)
                        {
                            if (ActivateSecondRepairModulesAtThisPercGlobalSetting > 100)
                                return (int)ActivateSecondRepairModulesAtThisPercGlobalSetting;
                        }

                        return (int)ActivateSecondRepairModulesAtThisPercGlobalSetting;
                    }

                    return 50;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 50;
                }
            }
        }

        private static int? ActivateSecondRepairModulesAtThisPercGlobalSetting { get; set; }

        private static int? ActivateSecondRepairModulesAtThisPercMissionSetting { get; set; }

        private static bool? _alwaysActivateSpeedMod { get; set; } = null;

        private static bool AlwaysActivateSpeedMod
        {
            get
            {
                if (ESCache.Instance.MyShipEntity.Velocity == 0)
                    return false;

                if (_alwaysActivateSpeedMod != null)
                    return _alwaysActivateSpeedMod ?? false;

                if (!ESCache.Instance.ActiveShip.HasSpeedMod)
                {
                    _alwaysActivateSpeedMod = false;
                    return _alwaysActivateSpeedMod ?? false;
                }

                if (AlwaysActivateSpeedModForThisGridOnly)
                {
                    _alwaysActivateSpeedMod = true;
                    return _alwaysActivateSpeedMod ?? true;
                }

                //if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.SalvageGridController))
                //{
                //    _alwaysActivateSpeedMod = true;
                //    return _alwaysActivateSpeedMod ?? true;
                //}

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController))
                {
                    _alwaysActivateSpeedMod = true;
                    return _alwaysActivateSpeedMod ?? true;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                {
                    _alwaysActivateSpeedMod = true;
                    return _alwaysActivateSpeedMod ?? true;
                }

                if (MissionSettings.MissionAlwaysActivateSpeedMod)
                {
                    _alwaysActivateSpeedMod = true;
                    return _alwaysActivateSpeedMod ?? true;
                }

                if (ESCache.Instance.InMission && (Combat.Combat.PotentialCombatTargets.Count == 0 || !ActionControl.PerformingClearPocketNow))
                {
                    if (ESCache.Instance.AccelerationGates.Any(i => i.Distance > 3000))
                        if (ESCache.Instance.AccelerationGates.Find(i => i.Distance > 3000).IsApproachedByActiveShip)
                        {
                            _alwaysActivateSpeedMod = true;
                            return _alwaysActivateSpeedMod ?? true;
                        }
                }

                if (ESCache.Instance.InAbyssalDeadspace && Combat.Combat.PotentialCombatTargets.Count <= 2)
                {
                    _alwaysActivateSpeedMod = false;
                    return _alwaysActivateSpeedMod ?? false;
                }

                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if ((ESCache.Instance.ActiveShip.IsShieldTanked && ESCache.Instance.ActiveShip.ShieldPercentage > 50) || (ESCache.Instance.ActiveShip.IsArmorTanked && ESCache.Instance.ActiveShip.ArmorPercentage > 50))
                        {
                            if (Combat.Combat.PotentialCombatTargets.All(i => !i.IsNPCBattleship && !i.IsNPCBattlecruiser))
                            {
                                if (Combat.Combat.KillTarget != null && (Combat.Combat.KillTarget.IsInDroneRange || Combat.Combat.KillTarget.IsInRangeOfWeapons) && !Combat.Combat.KillTarget.IsTrackable)
                                {
                                    //
                                    // slow down?
                                    //
                                    _alwaysActivateSpeedMod = false;
                                    return _alwaysActivateSpeedMod ?? false;
                                }

                                _alwaysActivateSpeedMod = true;
                                return _alwaysActivateSpeedMod ?? true;
                            }

                            _alwaysActivateSpeedMod = true;
                            return _alwaysActivateSpeedMod ?? true;
                        }

                        _alwaysActivateSpeedMod = true;
                        return _alwaysActivateSpeedMod ?? true;
                    }

                    _alwaysActivateSpeedMod = true;
                    return _alwaysActivateSpeedMod ?? true;
                }

                if (ESCache.Instance.InMission)
                {
                    if (MissionSettings.MyMission != null)
                    {
                        switch (MissionSettings.MyMission.Name.ToLower())
                        {
                            case "Recon (1 of 3)":
                            case "Recon (2 of 3)":
                            case "Recon (3 of 3)":
                            case "Duo of Death":
                            case "The Rogue Slave Trader (1 of 2)":
                            case "Pirate Invasion":
                            case "The Right Hand of Zazzmatazz":
                            {
                                _alwaysActivateSpeedMod = true;
                                return _alwaysActivateSpeedMod ?? true;
                            }

                            case "In the Midst of Deadspace (1 of 5)":
                            case "In the Midst of Deadspace (2 of 5)":
                            case "In the Midst of Deadspace (3 of 5)":
                            case "In the Midst of Deadspace (4 of 5)":
                            case "In the Midst of Deadspace (5 of 5)":
                            case "The Score":
                            case "Silence the Informant":
                            case "Worlds Collide":
                                if (Combat.Combat.PotentialCombatTargets.Count == 0)
                                {
                                    _alwaysActivateSpeedMod = true;
                                    return _alwaysActivateSpeedMod ?? true;
                                }

                                break;
                        }
                    }

                    if (ESCache.Instance.AccelerationGates.Count > 0 && ESCache.Instance.AccelerationGates.FirstOrDefault().IsApproachedByActiveShip && ESCache.Instance.AccelerationGates.FirstOrDefault().Distance > (double)Distances.GateActivationRange)
                    {
                        _alwaysActivateSpeedMod = true;
                        return _alwaysActivateSpeedMod ?? true;
                    }

                    if (ESCache.Instance.Wrecks.Count > 0 && ESCache.Instance.Wrecks.Any(i => i.IsApproachedByActiveShip) && ESCache.Instance.Wrecks.Find(i => i.IsApproachedByActiveShip).Distance > (double)Distances.ScoopRange)
                    {
                        _alwaysActivateSpeedMod = true;
                        return _alwaysActivateSpeedMod ?? true;
                    }

                    if (Combat.Combat.PotentialCombatTargets.Count == 0 && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                    {
                        _alwaysActivateSpeedMod = true;
                        return _alwaysActivateSpeedMod ?? true;
                    }

                    if (!ESCache.Instance.Entities.Any(i => i.IsLargeCollidable) && ESCache.Instance.Entities.Any(i => i.IsLargeCollidable && i.IsApproachedByActiveShip))
                    {
                        _alwaysActivateSpeedMod = true;
                        return _alwaysActivateSpeedMod ?? true;
                    }
                }

                if (ESCache.Instance.Stations.Any(i => 10000 > i.Distance) && Time.Instance.LastInWarp.AddSeconds(30) > DateTime.UtcNow)
                {
                    _alwaysActivateSpeedMod = true;
                    return _alwaysActivateSpeedMod ?? true;
                }

                _alwaysActivateSpeedMod = false;
                return _alwaysActivateSpeedMod ?? false;
            }
        }

        public static bool AlwaysActivateSpeedModForThisGridOnly { get; set; }

        public static bool CovertOps
        {
            get
            {
                try
                {
                    if (ESCache.Instance.InStation) return false;

                    if (ESCache.Instance.InSpace)
                    {
                        if (ESCache.Instance.Modules.Any(i => i.GroupId == (int) Group.CloakingDevice && i.TypeId == (int) TypeID.CovertOpsCloakingDevice))
                            return true;

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

        private static int DeactivateRepairModulesAtThisPerc
        {
            get
            {
                try
                {
                    if (DeactivateRepairModulesAtThisPercMissionSetting != null)
                        return (int)DeactivateRepairModulesAtThisPercMissionSetting;

                    if (DeactivateRepairModulesAtThisPercGlobalSetting != null)
                    {
                        if (Combat.Combat.PotentialCombatTargets.Count > 0)
                        {
                            if (ActivateRepairModulesAtThisPerc >= 101)
                                return 101;

                            //
                            // this will effectively turn off the repair module when fully repped if ActivateRepairModulesAtThisPerc is not set to perma rep (low cap, etc?)
                            //
                            if ((int)DeactivateRepairModulesAtThisPercGlobalSetting > 100 && ActivateRepairModulesAtThisPerc < 100)
                                return 85;

                            return (int)DeactivateRepairModulesAtThisPercGlobalSetting;
                        }

                        /*
                        if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.MyShipEntity.TypeId == (int)TypeID.Gila && 40 > ESCache.Instance.ActiveShip.CapacitorPercentage && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule))
                        {
                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordBSSpawn)
                                return (int)DeactivateRepairModulesAtThisPercGlobalSetting;

                            return 100;
                        }
                        */

                        return 100;
                    }

                    return 70;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 70;
                }
            }
        }

        private static int? DeactivateRepairModulesAtThisPercGlobalSetting { get; set; }

        private static int? DeactivateRepairModulesAtThisPercMissionSetting { get; set; }

        public static EntityCache EntityThatMayDecloakMe
        {
            get
            {
                EntityCache entityThatMayDecloakMe = ESCache.Instance.EntitiesNotSelf.Where(
                        t => t.IsBadIdea || t.IsContainer || t.IsNpc || t.IsPlayer || t.IsStation || t.IsCitadel)
                    .OrderBy(t => t.Distance)
                    .FirstOrDefault();
                return entityThatMayDecloakMe ?? null;
            }
        }

        private static int GlobalInjectCapPerc { get; set; }

        private static int InjectCapPerc
        {
            get
            {
                if (MissionSettings.MissionInjectCapPerc != null && MissionSettings.MissionInjectCapPerc != 0)
                    return (int) MissionSettings.MissionInjectCapPerc;

                if (GlobalInjectCapPerc != 0)
                    return GlobalInjectCapPerc;

                return 10;
            }
        }

        private static int MinimumPropulsionModuleCapacitor { get; set; }

        private static int MinimumPropulsionModuleDistance { get; set; }
        private static bool DreadRepairWhileInWarp { get; set; }
        public static int OverloadRepairModulesAtThisPerc
        {
            get
            {
                try
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                    {
                        if (Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                        {
                            return 75;
                        }
                    }

                    if (ESCache.Instance.IsPVPGankLikely)
                    {
                        //Always Overload!
                        return 100;
                    }

                    return OverloadRepairModulesAtThisPercGlobalSetting;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 30;
                }
            }
        }

        public static int OverloadHardenerModulesAtThisPerc
        {
            get
            {
                try
                {
                    if (ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.AbyssalController) &&
                        ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    {
                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                        {
                            if (Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                            {
                                //Always Overload!
                                return 100;
                            }
                        }
                    }

                    if (ESCache.Instance.IsPVPGankLikely && ESCache.Instance.EveAccount.SelectedController != nameof(EveAccount.AvailableControllers.WspaceSiteController))
                    {
                        //Always Overload!
                        return 100;
                    }

                    OverloadHardenerModulesAtThisPercGlobalSetting = OverloadRepairModulesAtThisPercGlobalSetting;
                    return OverloadHardenerModulesAtThisPercGlobalSetting;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 30;
                }
            }
        }

        public static int ToggleOffOverloadRepairModulesAtThisPerc
        {
            get
            {
                try
                {
                    //if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordBSSpawn)
                    //{
                    //    if (Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                    //    {
                    //        //Always Overload!
                    //        return 101;
                    //    }
                    //}

                    return 80;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 80;
                }
            }
        }

        public static int ToggleOffOverloadHardenerModulesAtThisPerc
        {
            get
            {
                try
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                    {
                        if (Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                        {
                            //Always Overload!
                            return 101;
                        }
                    }

                    if (ESCache.Instance.IsPVPGankLikely)
                    {
                        //Always Overload!
                        return 101;
                    }

                    return 80;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 80;
                }
            }
        }

        private static bool AllowOverLoadOfReps
        {
            get
            {
                if (MissionSettings.MissionAllowOverLoadOfReps)
                    return true;

                if (GlobalAllowOverLoadOfReps)
                    return true;

                return false;
            }
        }

        private static bool AllowOverLoadOfSpeedMod
        {
            get
            {
                //if (MissionSettings.MissionAllowOverLoadOfSpeedMod)
                //    return true;

                if (GlobalAllowOverLoadOfSpeedMod)
                    return true;

                return false;
            }
        }


        private static int ModuleNumber { get; set; }

        #endregion Properties

        #region Methods

        private static bool PodWarningSent;

        public static void InvalidateCache()
        {
            _covertOpsCloak = null;
        }

        private static DirectUIModule _covertOpsCloak = null;

        public static DirectUIModule CovertOpsCloak
        {
            get
            {
                if (_covertOpsCloak != null)
                    return _covertOpsCloak ?? null;

                if (ESCache.Instance.InStation)
                    return null;

                if (!ESCache.Instance.InSpace)
                    return null;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsCovertOpsCloak && i.IsOnline))
                {
                    _covertOpsCloak = ESCache.Instance.DirectEve.Modules.FirstOrDefault(i => i.IsCovertOpsCloak && i.IsOnline);
                    return _covertOpsCloak ?? null;
                }

                return null;
            }
        }

        private static DirectUIModule _regularCloak = null;

        public static DirectUIModule RegularCloak
        {
            get
            {
                if (_regularCloak != null)
                    return _regularCloak ?? null;

                if (ESCache.Instance.InStation)
                    return null;

                if (!ESCache.Instance.InSpace)
                    return null;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsCloak && !i.IsCovertOpsCloak && i.IsOnline))
                {
                    _regularCloak = ESCache.Instance.DirectEve.Modules.FirstOrDefault(i => i.IsCloak && !i.IsCovertOpsCloak && i.IsOnline);
                    return _regularCloak ?? null;
                }

                return null;
            }
        }

        public static bool ActivateRegularCloakWarpCloakyTrick()
        {
            if (CovertOpsCloak != null) //CovOps cloak would be activated elsewhere!
                return true;

            if (RegularCloak == null) //we must not have a cloak
                return true;

            if (!DirectEve.HasFrameChanged())
                return false;

            ModuleNumber = 0;

            if (!RegularCloak.IsActivatable)
            {
                Log.WriteLine("if (!RegularCloak.IsActivatable)");
                Traveler.BoolRunEveryFrame = true;
                return false;
            }

            ModuleNumber++;
            if (DebugConfig.DebugDefense)
                Log.WriteLine("[" + ModuleNumber + "][" + RegularCloak.TypeName + "] TypeID [" + RegularCloak.TypeId +
                                "] GroupId [" +
                                RegularCloak.GroupId + "] Activatable [" + RegularCloak.IsActivatable + "] Found");

            if (RegularCloak.IsActive)
            {
                Log.WriteLine("[" + ModuleNumber + "][" + RegularCloak.TypeName + "] is active");
                Traveler.BoolRunEveryFrame = false;

                foreach (ModuleCache speedMod in ESCache.Instance.Modules.Where(i => i.GroupId == (int) Group.Afterburner))
                {
                    if (LastActivateSpeedMod.AddMilliseconds(speedMod.Duration * .75) > DateTime.UtcNow || speedMod.InLimboState)
                    {
                        Log.WriteLine("ActivateRegularCloak: if (LastActivateSpeedMod.AddMilliseconds(speedMod.Duration * .75) > DateTime.UtcNow || speedMod.InLimboState)");
                        return false;
                    }

                    if (RegularCloak.Click())
                    {
                        Time.Instance.NextActivateModules = DateTime.UtcNow;
                        Log.WriteLine("[" + ModuleNumber + "][" + RegularCloak.TypeName + "] deactivated");
                        return true;
                    }
                }

                return true;
            }

            if (RegularCloak.IsLimboStateWithoutEffectActivating)
            {
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("[" + ModuleNumber + "][" + RegularCloak.TypeName + "] is in IsLimboStateWithoutEffectActivating");
                //return;
            }

            if (ESCache.Instance.DirectEve.Me.IsJumpActivationTimerActive)
            {
                if (DebugConfig.DebugDefense) Log.WriteLine("if (ESCache.Instance.DirectEve.Me.IsJumpActivationTimerActive) - move to break invul!");
                ESCache.Instance.ActiveShip.MoveToRandomDirection();
                return false;
            }

            if (ESCache.Instance.DirectEve.Me.IsInvuln)
            {
                Log.WriteLine("if (ESCache.Instance.DirectEve.Me.IsInvuln)");
                return false; //Invuln - do nothing
            }

            if (EntityThatMayDecloakMe != null && EntityThatMayDecloakMe.Distance <= (int)Distances.SafeToCloakDistance)
            {
                Log.WriteLine("if (EntityThatMayDecloakMe != null && EntityThatMayDecloakMe.Distance <= (int)Distances.SafeToCloakDistance)");
                if ((int)EntityThatMayDecloakMe.Distance != 0)
                {
                    Log.WriteLine("if ((int)EntityThatMayDecloakMe.Distance != 0)");
                    return true;
                }
            }

            if (LastCloaked.AddSeconds(1) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("Cloak is currently inactive, but we have been cloaked within the last 1 seconds. Cloak is likely still on cooldown. waiting.");
                return false;
            }

            if (RegularCloak.Click())
            {
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("[" + ModuleNumber + "][" + RegularCloak.TypeName + "] activated");
                return true;
            }

            return false;
        }

        public static bool ActivateCovopsCloak()
        {
            if (CovertOpsCloak == null)
                return true;

            if (!DirectEve.HasFrameChanged())
                return false;

            ModuleNumber = 0;
            ModuleNumber++;
            if (DebugConfig.DebugDefense)
                Log.WriteLine("[" + ModuleNumber + "][" + CovertOpsCloak.TypeName + "] TypeID [" + CovertOpsCloak.TypeId +
                                "] GroupId [" +
                                CovertOpsCloak.GroupId + "] Activatable [" + CovertOpsCloak.IsActivatable + "] Found");

            if (CovertOpsCloak.IsActive)
            {
                Traveler.BoolRunEveryFrame = false;
                if (DirectEve.Interval(10000)) Log.WriteLine("[" + ModuleNumber + "][" + CovertOpsCloak.TypeName + "] is active: return [true]");
                return true;
            }
            else
            {
                if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive)
                    Traveler.BoolRunEveryFrame = true;
            }

            if (!CovertOpsCloak.IsActivatable)
            {
                Log.WriteLine("[" + ModuleNumber + "][" + CovertOpsCloak.TypeName + "] if (!CovertOpsCloak.IsActivatable) return [false]");
                Traveler.BoolRunEveryFrame = true;
                return false;
            }

            if (CovertOpsCloak.IsLimboStateWithoutEffectActivating)
            {
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("[" + ModuleNumber + "][" + CovertOpsCloak.TypeName + "] is IsLimboStateWithoutEffectActivating");
                //return;
            }

            if (EntityThatMayDecloakMe != null && EntityThatMayDecloakMe.Distance <= (int)Distances.SafeToCloakDistance)
            {
                Log.WriteLine("if (EntityThatMayDecloakMe != null && EntityThatMayDecloakMe.Distance <= (int)Distances.SafeToCloakDistance)");
                if ((int)EntityThatMayDecloakMe.Distance != 0)
                {
                    Log.WriteLine("EntityThatMayDecloakMe [" + EntityThatMayDecloakMe.TypeName + "] Distance [" + Math.Round(EntityThatMayDecloakMe.Distance, 0) + "m]");
                    return true;
                }
            }

            if (LastCloaked.AddSeconds(1) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("Cloak is currently inactive, but we have been cloaked within the last 1 seconds. Cloak is likely still on cooldown. waiting.");
                return false;
            }

            if (CovertOpsCloak.ActivateCovertOpsCloak)
            {
                Log.WriteLine("[" + ModuleNumber + "][" + CovertOpsCloak.TypeName + "] ActivateCovertOpsCloak [ true ]");
                return true;
            }
            else if (DebugConfig.DebugDefense)
            {
                Log.WriteLine("[" + ModuleNumber + "][" + CovertOpsCloak.TypeName + "] ActivateCovertOpsCloak [ false ]");
            }

            return false;
        }

        private static Stopwatch DefenseActivateSpeedModStopWatch = new Stopwatch();

        private static bool GlobalAllowDefenseToUseSpeedMod = true;

        private static bool AllowDefenseToUseSpeedMod
        {
            get
            {
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController))
                {
                    if (ESCache.Instance.WeHaveWeapons)
                        return false;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                {
                    if (ESCache.Instance.WeHaveWeapons)
                        return false;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                {
                    if (Math.Abs(DateTime.UtcNow.Subtract(Statistics.StartedPocket).TotalMinutes) > 4)
                        return false;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                {
                    //level 1 agent?
                    if (MissionSettings.MyMission != null && MissionSettings.MyMission.Agent.Level == 1)
                    {
                        if (Math.Abs(DateTime.UtcNow.Subtract(Statistics.StartedPocket).TotalMinutes) > 6)
                            return false;
                    }
                }

                return GlobalAllowDefenseToUseSpeedMod;
            }
        }

        public static void ActivateSpeedMod(bool force = false, bool ifActiveDeactivate = false)
        {
            DefenseActivateSpeedModStopWatch.Restart();

            if (LastActivateSpeedMod.AddSeconds(7) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugSpeedMod)
                    Log.WriteLine("DebugSpeedMod: SpeedMod was activated less than 7 seconds ago");

                return;
            }

            if (!AllowDefenseToUseSpeedMod)
                return;

            if (ESCache.Instance.MyShipEntity.Velocity == 0 && ESCache.Instance.Modules.Any(i => i.GroupId == (int)Group.Afterburner && !i.IsActive))
                return;

            DefenseActivateSpeedModStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod 1a Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
            DefenseActivateSpeedModStopWatch.Restart();

            DefenseActivateSpeedModStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod 1b Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
            DefenseActivateSpeedModStopWatch.Restart();

            if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(3) && Combat.Combat.PotentialCombatTargets.Count == 0 && !AlwaysActivateSpeedMod)
            {
                if (DebugConfig.DebugSpeedMod)
                    Log.WriteLine("DebugSpeedMod: if (State.CurrentSlaveState == SlaveState.SlaveBehavior)");

                return;
            }

            DefenseActivateSpeedModStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod 1c Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
            DefenseActivateSpeedModStopWatch.Restart();

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HydraController))
            {
                if (DebugConfig.DebugSpeedMod)
                    Log.WriteLine("DebugSpeedMod: if (State.CurrentSlaveState == SlaveState.SlaveBehavior)");

                return;
            }

            DefenseActivateSpeedModStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod 2 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
            DefenseActivateSpeedModStopWatch.Restart();

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController))
            {
                if (DebugConfig.DebugSpeedMod)
                    Log.WriteLine("DebugSpeedMod: if (ESCache.Instance.SelectedController == CombatDontMoveController)");

                return;
            }

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
            {
                if (DebugConfig.DebugSpeedMod)
                    Log.WriteLine("DebugSpeedMod: if (ESCache.Instance.SelectedController == WspaceSiteController)");

                return;
            }

            DefenseActivateSpeedModStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod 3 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
            DefenseActivateSpeedModStopWatch.Restart();

            if (ESCache.Instance.Wormholes.Count > 0)
            {
                if (DebugConfig.DebugSpeedMod)
                    Log.WriteLine("DebugSpeedMod: if (ESCache.Instance.Wormholes.Any()) - never touch the speed mod when we are near a wormhole! assume manual");

                return;
            }

            DefenseActivateSpeedModStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod 4 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
            DefenseActivateSpeedModStopWatch.Restart();

            ModuleNumber = 0;
            foreach (ModuleCache speedMod in ESCache.Instance.Modules.Where(i => i.GroupId == (int) Group.Afterburner))
            {
                DefenseActivateSpeedModStopWatch.Restart();

                ModuleNumber++;

                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(speedMod.ItemId))
                {
                    if (DebugConfig.DebugSpeedMod)
                        Log.WriteLine("[" + ModuleNumber + "][" + speedMod.TypeName + "] was last activated [" +
                                      Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastActivatedTimeStamp[speedMod.ItemId]).TotalSeconds, 0) +
                                      "] sec ago");
                    if (Time.Instance.LastActivatedTimeStamp[speedMod.ItemId].AddMilliseconds(Time.Instance.AfterburnerDelay_milliseconds) > DateTime.UtcNow)
                        return;
                }

                DefenseActivateSpeedModStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 5 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                DefenseActivateSpeedModStopWatch.Restart();

                if (speedMod.InLimboState)
                {
                    if (DebugConfig.DebugSpeedMod)
                        Log.WriteLine("[" + ModuleNumber + "][" + speedMod.TypeName + "] isActive [" + speedMod.IsActive + "]");
                    return;
                }

                DefenseActivateSpeedModStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 6 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                DefenseActivateSpeedModStopWatch.Restart();

                if (DebugConfig.DebugSpeedMod)
                    Log.WriteLine("[" + ModuleNumber + "][" + speedMod.TypeName + "] isActive [" + speedMod.IsActive + "]");

                if (speedMod.IsActive)
                {
                    bool deactivate = false;

                    try
                    {
                        if (DateTime.UtcNow.AddSeconds(3) < Time.Instance.LastInWarp)
                        {
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("if (DateTime.UtcNow.AddSeconds(3) < Time.Instance.LastInWarp) deactivate = true");
                            deactivate = true;
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] isActive 7 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.Distance < 25000 && LastCloaked.AddSeconds(1) > DateTime.UtcNow)
                        {
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.Distance < 25000 && LastCloaked.AddSeconds(1) > DateTime.UtcNow) deactivate = true");
                            deactivate = true;
                        }

                        try
                        {
                            if (ESCache.Instance.AbyssalTrace != null && 10000 > ESCache.Instance.AbyssalTrace.Distance)
                            {
                                if (DebugConfig.DebugSpeedMod)
                                    Log.WriteLine("if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.Distance < 25000 && LastCloaked.AddSeconds(1) > DateTime.UtcNow) deactivate = true");
                                deactivate = true;
                            }

                            if (ESCache.Instance.EntitiesOnGrid.Any() && ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.Name.Contains("Abyssal Filament")) != null && 10000 > ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.Name.Contains("Abyssal Filament")).Distance)
                            {
                                if (DebugConfig.DebugSpeedMod)
                                    Log.WriteLine("if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.Distance < 25000 && LastCloaked.AddSeconds(1) > DateTime.UtcNow) deactivate = true");
                                deactivate = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        try
                        {
                            if (ESCache.Instance.InAbyssalDeadspace && !Combat.Combat.PotentialCombatTargets.Any() && ESCache.Instance.Modules.Any(i => i.IsMicroWarpDrive))
                            {
                                if (DebugConfig.DebugSpeedMod)
                                    Log.WriteLine("Activate: if (ESCache.Instance.InAbyssalDeadspace && !Combat.Combat.PotentialCombatTargets.Any())");

                                if (!ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache))
                                {
                                    if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                                    {
                                        if (5000 > ESCache.Instance.Wrecks.OrderBy(x => x.Distance).FirstOrDefault(y => !y.IsWreckEmpty).Distance)
                                        {
                                            if (DebugConfig.DebugSpeedMod)
                                                Log.WriteLine("Activate: We are within 6k of a non-empty wreck");
                                            deactivate = true;
                                        }

                                        if (DebugConfig.DebugSpeedMod)
                                            Log.WriteLine("Activate: if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))");
                                    }
                                    else
                                    {
                                        EntityCache AbyssalGate = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.TypeId == (int)TypeID.AbyssEncounterGate || (int)i.TypeId == (int)TypeID.AbyssExitGate);

                                        if (AbyssalGate != null && 5000 > AbyssalGate.Distance)
                                        {
                                            if (DebugConfig.DebugSpeedMod)
                                                Log.WriteLine("Activate: if (AbyssalGate != null && 6000 > AbyssalGate.Distance)");
                                            deactivate = true;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] isActive 8 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                            return;

                        if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                        {
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("BastionMode is active, we cannot move, deactivating speed module");
                            deactivate = true;
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] isActive 9 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 10) //not moving?
                        {
                            deactivate = true;
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("[" + ModuleNumber + "][" + speedMod.TypeName + "] We are not moving at the moment. Deactivate [" +
                                              deactivate +
                                              "]");
                        }

                        if (!ESCache.Instance.MyShipEntity.IsApproaching && !ESCache.Instance.MyShipEntity.IsOrbiting && !ESCache.Instance.InAbyssalDeadspace)
                        {
                            deactivate = true;
                            Log.WriteLine("[" + ModuleNumber + "][" + speedMod.TypeName + "] We are not approaching or orbiting anything: Deactivate [" + deactivate + "] AlwaysActivateSpeedMod [" + AlwaysActivateSpeedMod + "]");
                        }

                        if (Combat.Combat.PotentialCombatTargets.Count == 0 && DateTime.UtcNow > Statistics.StartedPocket.AddMinutes(20) &&
                            ESCache.Instance.ActiveShip.GivenName.ToLower() == Combat.Combat.CombatShipName.ToLower() && ESCache.Instance.FollowingEntity == null)
                        {
                            deactivate = true;
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("[" + ModuleNumber + "][" + speedMod.TypeName +
                                              "] Nothing on grid is attacking and it has been more than 20 minutes since we landed in this pocket. Deactivate [" +
                                              deactivate + "] AlwaysActivateSpeedMod [" + AlwaysActivateSpeedMod + "]");
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] isActive 12 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if ((ESCache.Instance.MyShipEntity.IsApproaching || ESCache.Instance.MyShipEntity.IsOrbiting) && ESCache.Instance.FollowingEntity != null)
                            if (ESCache.Instance.FollowingEntity.Distance < MinimumPropulsionModuleDistance || ESCache.Instance.MyShipEntity.Velocity / ESCache.Instance.ActiveShip.MaxVelocity < .5)
                            {
                                deactivate = true;
                                Log.WriteLine("[" + ModuleNumber + "][" + speedMod.TypeName + "] We are approaching or orbiting... and [" +
                                                  Math.Round(ESCache.Instance.FollowingEntity.Distance / 1000, 0) + "] is within [" +
                                                  Math.Round((double) MinimumPropulsionModuleDistance / 1000, 0) + "] Deactivate [" + deactivate + "]");
                            }

                        if (ESCache.Instance.ActiveShip.CapacitorPercentage < MinimumPropulsionModuleCapacitor)
                        {
                            deactivate = true;
                            Log.WriteLine("[" + ModuleNumber + "][" + speedMod.TypeName + "] Capacitor is at [" +
                                              Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) +
                                              "] which is below MinimumPropulsionModuleCapacitor [" + MinimumPropulsionModuleCapacitor + "] Deactivate [" +
                                              deactivate +
                                              "] AlwaysActivateSpeedMod [" + AlwaysActivateSpeedMod + "]");
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] isActive 16 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (AlwaysActivateSpeedMod)
                        {
                            deactivate = false;
                            if (DebugConfig.DebugSpeedMod) Log.WriteLine("if (AlwaysActivateSpeedMod): Useful for burner missions: deactivate == false");
                            //
                            // useful for burner missions
                            //
                            if (ESCache.Instance.Modules.Any(i => i.GroupId == (int) Group.WarpDisruptor && i.IsActive))
                            {
                                if (DebugConfig.DebugSpeedMod)
                                    Log.WriteLine("if (ESCache.Instance.Modules.Any(i => i.GroupId == (int) Group.WarpDisruptor && i.IsActive)) deactivate = true");
                                deactivate = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    if (ifActiveDeactivate)
                    {
                        if (speedMod.Click())
                        {
                            Log.WriteLine("[" + ModuleNumber + "] [" + speedMod.TypeName + "] Deactivated");
                            return;
                        }

                        return;
                    }

                    if (deactivate)
                        if (speedMod.Click())
                        {
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("[" + ModuleNumber + "] [" + speedMod.TypeName + "] Deactivated");
                            return;
                        }
                }

                DefenseActivateSpeedModStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 25 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                DefenseActivateSpeedModStopWatch.Restart();

                if (!speedMod.IsActive && !speedMod.InLimboState)
                {
                    bool activate = false;

                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                    {
                        if (DebugConfig.DebugSpeedMod)
                            Log.WriteLine("BastionMode is active, we cannot move, do not attempt to activate speed module");
                        activate = false;
                        return;
                    }

                    if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.AbyssalTrace != null)
                    {
                        if (15000 > ESCache.Instance.AbyssalTrace.Distance)
                        {
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("We are near an abyssal trace, assume we dont want the speed mod on right now");
                            activate = false;
                            return;
                        }
                    }

                    try
                    {
                        if (ESCache.Instance.InAbyssalDeadspace)
                        {
                            if (!Combat.Combat.PotentialCombatTargets.Any() && ESCache.Instance.Modules.Any(i => i.IsMicroWarpDrive))
                            {
                                if (DebugConfig.DebugSpeedMod)
                                    Log.WriteLine("Activate: if (ESCache.Instance.InAbyssalDeadspace && !Combat.Combat.PotentialCombatTargets.Any())");

                                if (!ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache))
                                {
                                    if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                                    {
                                        if (5000 > ESCache.Instance.Wrecks.OrderBy(x => x.Distance).FirstOrDefault(y => !y.IsWreckEmpty).Distance)
                                        {
                                            if (DebugConfig.DebugSpeedMod)
                                                Log.WriteLine("Activate: We are within 6k of a non-empty wreck");
                                            activate = false;
                                        }

                                        if (DebugConfig.DebugSpeedMod)
                                            Log.WriteLine("Activate: if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))");
                                    }
                                    else
                                    {
                                        EntityCache AbyssalGate = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.TypeId == (int)TypeID.AbyssEncounterGate || (int)i.TypeId == (int)TypeID.AbyssExitGate);

                                        if (AbyssalGate != null && 5000 > AbyssalGate.Distance)
                                        {
                                            if (DebugConfig.DebugSpeedMod)
                                                Log.WriteLine("Activate: if (AbyssalGate != null && 6000 > AbyssalGate.Distance)");
                                            activate = false;
                                            return;
                                        }
                                    }
                                }
                            }

                            if (Combat.Combat.PotentialCombatTargets.Any())
                            {
                                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                                {
                                    //
                                    // only exception for frigate is the Attacker Pacifier Disparu Troop - it is quite fast so do not disable the speed mod for that one NPC. Others need it off
                                    //
                                    if (Combat.Combat.PotentialCombatTargets.All(i => !i.Name.Contains("Attacker Pacifier Disparu Troop")))
                                    {
                                        if (DebugConfig.DebugSpeedMod || DebugConfig.DebugNavigateOnGrid) Log.WriteLine("AvtivateSpeedMod: if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn) do not allow speed mod for this spawn");
                                        activate = false;
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    DefenseActivateSpeedModStopWatch.Stop();
                    if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 28 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                    DefenseActivateSpeedModStopWatch.Restart();

                    if (!ESCache.Instance.InMission && ESCache.Instance.ClosestStargate != null && 24000 > ESCache.Instance.ClosestStargate.Distance)
                    {
                        if (DebugConfig.DebugSpeedMod)
                            Log.WriteLine("if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.Distance < 25000 && LastCloaked.AddSeconds(1) > DateTime.UtcNow) deactivate = true");
                        activate = false;
                        return;
                    }

                    DefenseActivateSpeedModStopWatch.Stop();
                    if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 29 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                    DefenseActivateSpeedModStopWatch.Restart();

                    if (ESCache.Instance.InAbyssalDeadspace && Combat.Combat.PotentialCombatTargets.Count == 0 && ESCache.Instance.AccelerationGates.Any(i => 7000 > i.Distance) && ESCache.Instance.Wrecks.All(i => i.IsWreckEmpty))
                    {
                        //
                        // do not activate speed mod if we are in abyssaldeadspace, near the gate and there are no targets left
                        //
                        activate = false;
                        return;
                    }

                    DefenseActivateSpeedModStopWatch.Stop();
                    if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 30 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                    DefenseActivateSpeedModStopWatch.Restart();

                    if (ESCache.Instance.ClosestDockableLocation != null && 50000 > ESCache.Instance.ClosestDockableLocation.Distance && DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(30))
                    {
                        if (DebugConfig.DebugSpeedMod)
                            Log.WriteLine("We are very close to a station and are likely docking: use speed mod");
                        activate = true;
                    }

                    DefenseActivateSpeedModStopWatch.Stop();
                    if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 31 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                    DefenseActivateSpeedModStopWatch.Restart();

                    if (AlwaysActivateSpeedMod)
                    {
                        if (ESCache.Instance.InMission && MissionSettings.SelectedControllerUsesCombatMissionsBehavior && MissionSettings.AgentToPullNextRegularMissionFrom.Level == 4 && (ESCache.Instance.EntitiesNotSelf != null && ESCache.Instance.EntitiesNotSelf.Any(i => i.Velocity > 0)))
                        {
                            activate = true;
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("[" + ModuleNumber + "] AlwaysActivateSpeedMod [" + AlwaysActivateSpeedMod + "] Activate [" + activate + "]");

                            //
                            // useful for burner missions
                            //
                            if (ESCache.Instance.Modules.Any(i => i.GroupId == (int) Group.WarpDisruptor && i.IsActive))
                                activate = false;
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 35 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (ESCache.Instance.MyShipEntity.Velocity == 0)
                        {
                            activate = false;
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("[" + ModuleNumber + "] Velocity [ 0 ] Activate [" + activate + "]");
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 36 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (AlwaysActivateSpeedMod)
                        {
                            activate = true;
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("[" + ModuleNumber + "] AlwaysActivateSpeedModForThisGridOnly [" + AlwaysActivateSpeedModForThisGridOnly + "] Activate [" + activate + "]");
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 37 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (speedMod.IsMicroWarpDrive && ESCache.Instance.ActiveShip.IsWarpScrambled)
                        {
                            if (DebugConfig.DebugSpeedMod) Log.WriteLine("if (speedMod.IsMicroWarpDrive && ESCache.Instance.ActiveShip.IsWarpScrambled) return");
                            return;
                        }

                        if (activate)
                        {
                            if (speedMod.Click())
                                return;

                            return;
                        }

                        return;
                    }

                    if ((ESCache.Instance.MyShipEntity.IsApproaching || ESCache.Instance.MyShipEntity.IsOrbiting || (NavigateOnGrid.SpeedTank && ESCache.Instance.MyShipEntity.IsOrbiting)) && ESCache.Instance.FollowingEntity != null && ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity > 20)
                    {
                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 40 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (DebugConfig.DebugSpeedMod)
                            Log.WriteLine("[" + ModuleNumber + "] We are approaching or orbiting");
                        if (ESCache.Instance.FollowingEntity.Distance > MinimumPropulsionModuleDistance && ESCache.Instance.MyShipEntity.Velocity / ESCache.Instance.ActiveShip.MaxVelocity > .5)
                        {
                            activate = true;
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("[" + ModuleNumber + "] SpeedTank is [" + NavigateOnGrid.SpeedTank +
                                              "] We are approaching or orbiting and [" +
                                              Math.Round(ESCache.Instance.FollowingEntity.Distance / 1000, 0) +
                                              "k] is within MinimumPropulsionModuleDistance [" +
                                              Math.Round((double) MinimumPropulsionModuleDistance / 1000, 2) + "] Activate [" + activate + "]");
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 41 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (AlwaysActivateSpeedMod)
                        {
                            activate = true;
                            if (DebugConfig.DebugSpeedMod)
                                Log.WriteLine("[" + ModuleNumber + "] We are approaching or orbiting: Activate [" + activate + "]");
                        }

                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 42 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();
                    }

                    if (ESCache.Instance.ActiveShip.CapacitorPercentage < MinimumPropulsionModuleCapacitor)
                    {
                        activate = false;
                        if (DebugConfig.DebugSpeedMod)
                            Log.WriteLine("[" + ModuleNumber + "] CapacitorPercentage is [" + ESCache.Instance.ActiveShip.CapacitorPercentage +
                                          "] which is less than MinimumPropulsionModuleCapacitor [" + MinimumPropulsionModuleCapacitor + "] Activate [" +
                                          activate + "]");
                    }

                    DefenseActivateSpeedModStopWatch.Stop();
                    if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 45 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                    DefenseActivateSpeedModStopWatch.Restart();

                    if (DateTime.UtcNow.AddSeconds(3) < Time.Instance.LastInWarp)
                    {
                        activate = false;
                        if (DebugConfig.DebugSpeedMod) Log.WriteLine("[" + ModuleNumber + "][" + speedMod.TypeName + "] We were just in warp: Do not activate [" + activate + "]");
                    }

                    if (force)
                    {
                        DefenseActivateSpeedModStopWatch.Stop();
                        if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 50 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                        DefenseActivateSpeedModStopWatch.Restart();

                        if (speedMod.Click())
                        {
                            DefenseActivateSpeedModStopWatch.Stop();
                            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 55 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                            DefenseActivateSpeedModStopWatch.Restart();

                            Time.Instance.NextActivateModules = DateTime.UtcNow;
                            LastActivateSpeedMod = DateTime.UtcNow;
                            return;
                        }

                        return;
                    }

                    if (activate)
                    {
                        if (speedMod.Click())
                        {
                            DefenseActivateSpeedModStopWatch.Stop();
                            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 59 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
                            DefenseActivateSpeedModStopWatch.Restart();

                            LastActivateSpeedMod = DateTime.UtcNow;
                            return;
                        }

                        return;
                    }
                }
            }

            DefenseActivateSpeedModStopWatch.Stop();
            if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod ModuleNumber [" + ModuleNumber + "] 65 Took [" + Util.ElapsedMicroSeconds(DefenseActivateSpeedModStopWatch) + "]");
            DefenseActivateSpeedModStopWatch.Restart();

            return;
        }

        public static void ClearPerPocketCache()
        {
            PodWarningSent = false;
            AlwaysActivateSpeedModForThisGridOnly = false;
            _alwaysActivateSpeedMod = null;
            CheckedForOfflineModules = false;
        }

        public static void ClearPerMissionSettings()
        {
            Defense.ActivateRepairModulesAtThisPercMissionSetting = null;
            Defense.ActivateSecondRepairModulesAtThisPercMissionSetting = null;
            Defense.DeactivateRepairModulesAtThisPercMissionSetting = null;
            Defense.attemptOverloadRackHigh = false;
            Defense.attemptOverloadRackMedium = false;
            Defense.attemptOverloadRackLow = false;
            Defense.attemptOverloadHighSlot1 = false;
            Defense.attemptOverloadHighSlot2 = false;
            Defense.attemptOverloadHighSlot3 = false;
            Defense.attemptOverloadHighSlot4 = false;
            Defense.attemptOverloadHighSlot5 = false;
            Defense.attemptOverloadHighSlot6 = false;
            Defense.attemptOverloadHighSlot7 = false;
            Defense.attemptOverloadHighSlot8 = false;
            Defense.attemptOverloadMidSlot1 = false;
            Defense.attemptOverloadMidSlot2 = false;
            Defense.attemptOverloadMidSlot3 = false;
            Defense.attemptOverloadMidSlot4 = false;
            Defense.attemptOverloadMidSlot5 = false;
            Defense.attemptOverloadMidSlot6 = false;
            Defense.attemptOverloadMidSlot7 = false;
            Defense.attemptOverloadMidSlot8 = false;
            Defense.attemptOverloadLowSlot1 = false;
            Defense.attemptOverloadLowSlot2 = false;
            Defense.attemptOverloadLowSlot3 = false;
            Defense.attemptOverloadLowSlot4 = false;
            Defense.attemptOverloadLowSlot5 = false;
            Defense.attemptOverloadLowSlot6 = false;
            Defense.attemptOverloadLowSlot7 = false;
            Defense.attemptOverloadLowSlot8 = false;
        }

        public static void ClearSystemSpecificSettings()
        {
            try
            {
                overloadRackHighCompleted = false;
                overloadRackMediumCompleted = false;
                overloadRackLowCompleted = false;

                overloadHighSlot1Completed = false;
                overloadHighSlot2Completed = false;
                overloadHighSlot3Completed = false;
                overloadHighSlot4Completed = false;
                overloadHighSlot5Completed = false;
                overloadHighSlot6Completed = false;
                overloadHighSlot7Completed = false;
                overloadHighSlot8Completed = false;

                overloadMidSlot1Completed = false;
                overloadMidSlot2Completed = false;
                overloadMidSlot3Completed = false;
                overloadMidSlot4Completed = false;
                overloadMidSlot5Completed = false;
                overloadMidSlot6Completed = false;
                overloadMidSlot7Completed = false;
                overloadMidSlot8Completed = false;

                overloadLowSlot1Completed = false;
                overloadLowSlot2Completed = false;
                overloadLowSlot3Completed = false;
                overloadLowSlot4Completed = false;
                overloadLowSlot5Completed = false;
                overloadLowSlot6Completed = false;
                overloadLowSlot7Completed = false;
                overloadLowSlot8Completed = false;
                _attemptsToOnlineOfflineModules = 0;
                Time.Instance.LastAlign = DateTime.UtcNow.AddMinutes(-15);
                AlwaysActivateSpeedModForThisGridOnly = false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool DeactivateCloak()
        {
            if (DateTime.UtcNow < Time.Instance.NextActivateModules)
                return false;

            ModuleNumber = 0;
            foreach (ModuleCache cloak in ESCache.Instance.Modules.Where(i => i.GroupId == (int) Group.CloakingDevice))
            {
                if (!cloak.IsActivatable)
                    continue;

                ModuleNumber++;
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("[" + ModuleNumber + "][" + cloak.TypeName + "] TypeID [" + cloak.TypeId +
                                  "] GroupId [" +
                                  cloak.GroupId + "] Activatable [" + cloak.IsActivatable + "] Found");

                if (cloak.InLimboState)
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

        public static void LoadMissionXmlData(XDocument missionXml)
        {
            try
            {
                if (missionXml.Root != null)
                {
                    ActivateRepairModulesAtThisPercMissionSetting = (int?) missionXml.Root.Element("activateRepairModulesAtThisPerc") ?? null;
                    ActivateSecondRepairModulesAtThisPercMissionSetting = (int?) missionXml.Root.Element("activateSecondRepairModulesAtThisPerc") ?? null;
                    DeactivateRepairModulesAtThisPercMissionSetting = (int?) missionXml.Root.Element("deactivateRepairModulesAtThisPerc") ?? null;
                    attemptOverloadRackHigh = (bool?) missionXml.Root.Element("attemptOverloadRackHigh") ?? false;
                    attemptOverloadRackMedium = (bool?) missionXml.Root.Element("attemptOverloadRackMedium") ?? false;
                    attemptOverloadRackLow = (bool?) missionXml.Root.Element("attemptOverloadRackLow") ?? false;
                    attemptOverloadHighSlot1 = (bool?) missionXml.Root.Element("attemptOverloadHighSlot1") ?? false;
                    attemptOverloadHighSlot2 = (bool?) missionXml.Root.Element("attemptOverloadHighSlot2") ?? false;
                    attemptOverloadHighSlot3 = (bool?) missionXml.Root.Element("attemptOverloadHighSlot3") ?? false;
                    attemptOverloadHighSlot4 = (bool?) missionXml.Root.Element("attemptOverloadHighSlot4") ?? false;
                    attemptOverloadHighSlot5 = (bool?) missionXml.Root.Element("attemptOverloadHighSlot5") ?? false;
                    attemptOverloadHighSlot6 = (bool?) missionXml.Root.Element("attemptOverloadHighSlot6") ?? false;
                    attemptOverloadHighSlot7 = (bool?) missionXml.Root.Element("attemptOverloadHighSlot7") ?? false;
                    attemptOverloadHighSlot8 = (bool?) missionXml.Root.Element("attemptOverloadHighSlot8") ?? false;
                    attemptOverloadMidSlot1 = (bool?) missionXml.Root.Element("attemptOverloadMidSlot1") ?? false;
                    attemptOverloadMidSlot2 = (bool?) missionXml.Root.Element("attemptOverloadMidSlot2") ?? false;
                    attemptOverloadMidSlot3 = (bool?) missionXml.Root.Element("attemptOverloadMidSlot3") ?? false;
                    attemptOverloadMidSlot4 = (bool?) missionXml.Root.Element("attemptOverloadMidSlot4") ?? false;
                    attemptOverloadMidSlot5 = (bool?) missionXml.Root.Element("attemptOverloadMidSlot5") ?? false;
                    attemptOverloadMidSlot6 = (bool?) missionXml.Root.Element("attemptOverloadMidSlot6") ?? false;
                    attemptOverloadMidSlot7 = (bool?) missionXml.Root.Element("attemptOverloadMidSlot7") ?? false;
                    attemptOverloadMidSlot8 = (bool?) missionXml.Root.Element("attemptOverloadMidSlot8") ?? false;
                    attemptOverloadLowSlot1 = (bool?) missionXml.Root.Element("attemptOverloadLowSlot1") ?? false;
                    attemptOverloadLowSlot2 = (bool?) missionXml.Root.Element("attemptOverloadLowSlot2") ?? false;
                    attemptOverloadLowSlot3 = (bool?) missionXml.Root.Element("attemptOverloadLowSlot3") ?? false;
                    attemptOverloadLowSlot4 = (bool?) missionXml.Root.Element("attemptOverloadLowSlot4") ?? false;
                    attemptOverloadLowSlot5 = (bool?) missionXml.Root.Element("attemptOverloadLowSlot5") ?? false;
                    attemptOverloadLowSlot6 = (bool?) missionXml.Root.Element("attemptOverloadLowSlot6") ?? false;
                    attemptOverloadLowSlot7 = (bool?) missionXml.Root.Element("attemptOverloadLowSlot7") ?? false;
                    attemptOverloadLowSlot8 = (bool?) missionXml.Root.Element("attemptOverloadLowSlot8") ?? false;

                    _deactivateOverloadHighSlot1Seconds = (int?) missionXml.Root.Element("deactivateOverloadHighSlot1Seconds") ?? 0;
                    _deactivateOverloadHighSlot2Seconds = (int?) missionXml.Root.Element("deactivateOverloadHighSlot2Seconds") ?? 0;
                    _deactivateOverloadHighSlot3Seconds = (int?) missionXml.Root.Element("deactivateOverloadHighSlot3Seconds") ?? 0;
                    _deactivateOverloadHighSlot4Seconds = (int?) missionXml.Root.Element("deactivateOverloadHighSlot4Seconds") ?? 0;
                    _deactivateOverloadHighSlot5Seconds = (int?) missionXml.Root.Element("deactivateOverloadHighSlot5Seconds") ?? 0;
                    _deactivateOverloadHighSlot6Seconds = (int?) missionXml.Root.Element("deactivateOverloadHighSlot6Seconds") ?? 0;
                    _deactivateOverloadHighSlot7Seconds = (int?) missionXml.Root.Element("deactivateOverloadHighSlot7Seconds") ?? 0;
                    _deactivateOverloadHighSlot8Seconds = (int?) missionXml.Root.Element("deactivateOverloadHighSlot8Seconds") ?? 0;

                    _deactivateOverloadMidSlot1Seconds = (int?) missionXml.Root.Element("deactivateOverloadMidSlot1Seconds") ?? 0;
                    _deactivateOverloadMidSlot2Seconds = (int?) missionXml.Root.Element("deactivateOverloadMidSlot2Seconds") ?? 0;
                    _deactivateOverloadMidSlot3Seconds = (int?) missionXml.Root.Element("deactivateOverloadMidSlot3Seconds") ?? 0;
                    _deactivateOverloadMidSlot4Seconds = (int?) missionXml.Root.Element("deactivateOverloadMidSlot4Seconds") ?? 0;
                    _deactivateOverloadMidSlot5Seconds = (int?) missionXml.Root.Element("deactivateOverloadMidSlot5Seconds") ?? 0;
                    _deactivateOverloadMidSlot6Seconds = (int?) missionXml.Root.Element("deactivateOverloadMidSlot6Seconds") ?? 0;
                    _deactivateOverloadMidSlot7Seconds = (int?) missionXml.Root.Element("deactivateOverloadMidSlot7Seconds") ?? 0;
                    _deactivateOverloadMidSlot8Seconds = (int?) missionXml.Root.Element("deactivateOverloadMidSlot8Seconds") ?? 0;

                    _deactivateOverloadLowSlot1Seconds = (int?) missionXml.Root.Element("deactivateOverloadLowSlot1Seconds") ?? 0;
                    _deactivateOverloadLowSlot2Seconds = (int?) missionXml.Root.Element("deactivateOverloadLowSlot2Seconds") ?? 0;
                    _deactivateOverloadLowSlot3Seconds = (int?) missionXml.Root.Element("deactivateOverloadLowSlot3Seconds") ?? 0;
                    _deactivateOverloadLowSlot4Seconds = (int?) missionXml.Root.Element("deactivateOverloadLowSlot4Seconds") ?? 0;
                    _deactivateOverloadLowSlot5Seconds = (int?) missionXml.Root.Element("deactivateOverloadLowSlot5Seconds") ?? 0;
                    _deactivateOverloadLowSlot6Seconds = (int?) missionXml.Root.Element("deactivateOverloadLowSlot6Seconds") ?? 0;
                    _deactivateOverloadLowSlot7Seconds = (int?) missionXml.Root.Element("deactivateOverloadLowSlot7Seconds") ?? 0;
                    _deactivateOverloadLowSlot8Seconds = (int?) missionXml.Root.Element("deactivateOverloadLowSlot8Seconds") ?? 0;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: Defense");
                MinimumPropulsionModuleDistance =
                    (int?) CharacterSettingsXml.Element("minimumPropulsionModuleDistance") ??
                    (int?) CommonSettingsXml.Element("minimumPropulsionModuleDistance") ?? 5000;
                Log.WriteLine("Defense: minimumPropulsionModuleDistance [" + MinimumPropulsionModuleDistance + "]");
                DreadRepairWhileInWarp =
                    (bool?)CharacterSettingsXml.Element("dreadRepairWhileInWarp") ??
                    (bool?)CommonSettingsXml.Element("dreadRepairWhileInWarp") ?? true;
                Log.WriteLine("Defense: dreadRepairWhileInWarp [" + DreadRepairWhileInWarp + "]");
                MinimumPropulsionModuleCapacitor =
                    (int?) CharacterSettingsXml.Element("minimumPropulsionModuleCapacitor") ??
                    (int?) CommonSettingsXml.Element("minimumPropulsionModuleCapacitor") ?? 55;
                Log.WriteLine("Defense: minimumPropulsionModuleCapacitor [" + MinimumPropulsionModuleCapacitor + "]");
                ActivateAssaultDamageControlModulesAtThisPercGlobalSetting =
                    (int?)CharacterSettingsXml.Element("activateAssaultDamageControlModulesAtThisPerc") ??
                    (int?)CommonSettingsXml.Element("activateAssaultDamageControlModulesAtThisPerc") ?? 30;
                Log.WriteLine("Defense: activateAssaultDamageControlModulesAtThisPerc [" + ActivateAssaultDamageControlModulesAtThisPercGlobalSetting + "]");
                ActivateRepairModulesAtThisPercGlobalSetting =
                    (int?)CharacterSettingsXml.Element("activateRepairModules") ??
                    (int?)CommonSettingsXml.Element("activateRepairModules") ?? 65;
                Log.WriteLine("Defense: activateRepairModules [" + ActivateRepairModulesAtThisPercGlobalSetting + "]");
                OverloadRepairModulesAtThisPercGlobalSetting =
                    (int?) CharacterSettingsXml.Element("overloadRepairModulesAtThisPerc") ??
                    (int?) CommonSettingsXml.Element("overloadRepairModulesAtThisPerc") ?? 40;
                Log.WriteLine("Defense: overloadRepairModulesAtThisPerc [" + OverloadRepairModulesAtThisPercGlobalSetting + "]");
                OverloadHardenerModulesAtThisPercGlobalSetting =
                    (int?)CharacterSettingsXml.Element("overloadHardenerModulesAtThisPerc") ??
                    (int?)CommonSettingsXml.Element("overloadHardenerModulesAtThisPerc") ?? 40;
                Log.WriteLine("Defense: overloadHardenerModulesAtThisPerc [" + OverloadHardenerModulesAtThisPercGlobalSetting + "]");
                ActivateSecondRepairModulesAtThisPercGlobalSetting =
                    (int?) CharacterSettingsXml.Element("activateSecondRepairModules") ??
                    (int?) CommonSettingsXml.Element("activateSecondRepairModules") ?? 50;
                Log.WriteLine("Defense: activateSecondRepairModules [" + ActivateSecondRepairModulesAtThisPercGlobalSetting + "]");
                DeactivateRepairModulesAtThisPercGlobalSetting =
                    (int?) CharacterSettingsXml.Element("deactivateRepairModules") ??
                    (int?) CommonSettingsXml.Element("deactivateRepairModules") ?? 95;
                Log.WriteLine("Defense: deactivateRepairModules [" + DeactivateRepairModulesAtThisPercGlobalSetting + "]");
                GlobalInjectCapPerc =
                    (int?) CharacterSettingsXml.Element("injectCapPerc") ??
                    (int?) CharacterSettingsXml.Element("injectcapperc") ??
                    (int?) CommonSettingsXml.Element("injectCapPerc") ??
                    (int?) CommonSettingsXml.Element("injectcapperc") ?? 60;
                Log.WriteLine("Defense: injectCapPerc [" + GlobalInjectCapPerc + "]");
                GlobalAllowOverLoadOfReps =
                    (bool?) CharacterSettingsXml.Element("allowOverLoadOfReps") ??
                    (bool?) CommonSettingsXml.Element("allowOverLoadOfReps") ?? false;
                Log.WriteLine("Defense: allowOverLoadOfReps [" + GlobalAllowOverLoadOfReps + "]");
                GlobalAllowOverLoadOfSpeedMod =
                    (bool?)CharacterSettingsXml.Element("allowOverLoadOfSpeedMod") ??
                    (bool?)CommonSettingsXml.Element("allowOverLoadOfSpeedMod") ?? false;
                Log.WriteLine("Defense: allowOverLoadOfSpeedMod [" + GlobalAllowOverLoadOfSpeedMod + "]");
                GlobalRepsOverloadDamageAllowed =
                    (int?) CharacterSettingsXml.Element("repsOverloadDamageAllowed") ??
                    (int?) CommonSettingsXml.Element("repsOverloadDamageAllowed") ?? 50;
                Log.WriteLine("Defense: repsOverloadDamageAllowed [" + GlobalRepsOverloadDamageAllowed + "]");
                GlobalAllowOverLoadOfHardeners =
                    (bool?) CharacterSettingsXml.Element("allowOverloadOfHardeners") ??
                    (bool?) CommonSettingsXml.Element("allowOverloadOfHardeners") ?? false;
                Log.WriteLine("Defense: allowOverloadOfHardeners [" + GlobalAllowOverLoadOfHardeners + "]");

                GlobalAllowDefenseToUseSpeedMod =
                    (bool ?) CharacterSettingsXml.Element("allowDefenseToUseSpeedMod") ??
                    (bool?)CommonSettingsXml.Element("allowDefenseToUseSpeedMod") ?? true;
                Log.WriteLine("Defense: allowDefenseToUseSpeedMod [" + GlobalAllowDefenseToUseSpeedMod + "]");

                BoosterTypesToLoadIntoCargo = new HashSet<long>();
                XElement boostersToLoadIntoCargoXml = CharacterSettingsXml.Element("boosterTypesToLoadIntoCargo") ?? CommonSettingsXml.Element("boosterTypesToLoadIntoCargo");

                if (boostersToLoadIntoCargoXml != null)
                    foreach (XElement boosterToLoadIntoCargo in boostersToLoadIntoCargoXml.Elements("boosterType"))
                    {
                        try
                        {
                            long booster = int.Parse(boosterToLoadIntoCargo.Value);
                            DirectInvType boosterInvType = ESCache.Instance.DirectEve.GetInvType(int.Parse(boosterToLoadIntoCargo.Value));
                            Log.WriteLine("Adding booster [" + boosterInvType.TypeName + "] to the list of boosters that will be loaded into cargo for use in space (if needed)");
                            BoosterTypesToLoadIntoCargo.Add(booster);
                        }
                        catch (Exception){ }
                    }

                ActivateThisCombatBoosterTypeIdWhenShieldsAreLow =
                    (int?)CharacterSettingsXml.Element("ActivateThisCombatBoosterTypeIdWhenShieldsAreLow") ??
                     (int?)CommonSettingsXml.Element("ActivateThisCombatBoosterTypeIdWhenShieldsAreLow") ?? 0;
                Log.WriteLine("Defense: ActivateThisCombatBoosterTypeIdWhenShieldsAreLow [" + ActivateThisCombatBoosterTypeIdWhenShieldsAreLow + "]");

                ActivateThisCombatBoosterTypeIdWhenArmorIsLow =
                    (int?)CharacterSettingsXml.Element("ActivateThisCombatBoosterTypeIdWhenArmorIsLow") ??
                     (int?)CommonSettingsXml.Element("ActivateThisCombatBoosterTypeIdWhenArmorIsLow") ?? 0;
                Log.WriteLine("Defense: ActivateThisCombatBoosterTypeIdWhenArmorIsLow [" + ActivateThisCombatBoosterTypeIdWhenArmorIsLow + "]");

                GlobalActivateCombatBoosterWhenShieldsAreBelowThisPercentage =
                    (int?)CharacterSettingsXml.Element("ActivateCombatBoosterWhenShieldsAreBelowThisPercentage") ??
                    (int?)CommonSettingsXml.Element("ActivateCombatBoosterWhenShieldsAreBelowThisPercentage") ?? 30;
                Log.WriteLine("Defense: ActivateCombatBoosterWhenShieldsAreBelowThisPercentage [" + GlobalActivateCombatBoosterWhenShieldsAreBelowThisPercentage + "]");

            }
            catch (Exception ex)
            {
                Log.WriteLine("Error Loading Defense Settings [" + ex + "]");
            }
        }

        private static Stopwatch DefenseStopWatch = new Stopwatch();

        private static bool CheckedForOfflineModules = false;
        public static void ProcessState()
        {
            DefenseStopWatch.Restart();

            try
            {
                if (!ESCache.Instance.DirectEve.Session.IsReady)
                    return;

                if (DebugConfig.DebugDefense)
                    Log.WriteLine("DebugDefense: Defense ProcessState");

                if (ESCache.Instance.InStation)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("DebugDefense: We are in a station.");
                    _trackingLinkScriptAttempts = 0;
                    _sensorBoosterScriptAttempts = 0;
                    _sensorDampenerScriptAttempts = 0;
                    _trackingComputerScriptAttempts = 0;
                    _trackingDisruptorScriptAttempts = 0;
                    if (ESCache.Instance.EveAccount.AmountExceptionsCurrentSession > 40)
                    {
                        Log.WriteLine("DebugDefense: AmountExceptionsCurrentSession is [" + ESCache.Instance.EveAccount.AmountExceptionsCurrentSession + "] > 10 - you may want to look at the SharpLogLite Log and verify there are no issues to fix");
                    }

                    return;
                }

                if (Time.Instance.LastJumpAction.AddSeconds(8) > DateTime.UtcNow)
                    return;

                if (Time.Instance.LastDockAction.AddSeconds(8) > DateTime.UtcNow)
                    return;

                if (Time.Instance.LastActivateFilamentAttempt.AddSeconds(8) > DateTime.UtcNow)
                    return;

                if (Time.Instance.LastActivateKeyActivationWindowAttempt.AddSeconds(8) > DateTime.UtcNow)
                    return;

                if (Time.Instance.LastActivateAccelerationGate.AddSeconds(8) > DateTime.UtcNow)
                    return;

                if (ESCache.Instance.EveAccount.AmountExceptionsCurrentSession > 40)
                {
                    Log.WriteLine("DebugDefense: AmountExceptionsCurrentSession is: [" + ESCache.Instance.EveAccount.AmountExceptionsCurrentSession + "] > 10 - you may want to look at the SharpLogLite Log and verify there are no issues to fix");
                }

                if (ESCache.Instance.InsidePosForceField)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("DebugDefense: We are in a POS Forcefield.");
                    _trackingLinkScriptAttempts = 0;
                    _sensorBoosterScriptAttempts = 0;
                    _sensorDampenerScriptAttempts = 0;
                    _trackingComputerScriptAttempts = 0;
                    _trackingDisruptorScriptAttempts = 0;
                    return;
                }

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense: 1 Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (ESCache.Instance.DockableLocations.Any(i => i.IsOnGridWithMe))
                    if (Time.Instance.LastOfflineModuleCheck.AddSeconds(10) < DateTime.UtcNow && ESCache.Instance.InSpace
                        && ESCache.Instance.Modules.Any(m => !m.IsOnline)
                        && !ESCache.Instance.Paused
                        && !ESCache.Instance.InMission)
                    {
                        if ((_attemptsToOnlineOfflineModules < 4 && ESCache.Instance.ActiveShip.CapacitorPercentage < 95) || !ESCache.Instance.Modules.Find(m => !m.IsOnline).OnlineModule)
                        {
                            Time.Instance.LastOnlineAModule = DateTime.UtcNow;
                            Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                            ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                            if (!CheckedForOfflineModules)
                            {
                                _attemptsToOnlineOfflineModules++;
                            }

                            CheckedForOfflineModules = true;
                            return;
                        }

                        if (Time.Instance.LastOnlineAModule.AddSeconds(6) > DateTime.UtcNow)
                        {
                            //Try to keep us from warping until the module actually onlines
                            Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                            ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                            return;
                        }

                        if (_attemptsToOnlineOfflineModules >= 4)
                        {
                            Log.WriteLine("Offline modules found, if (_attemptsToOnlineOfflineModules >= 4) - Dock! and PauseAfterNextDock [true]");
                            ESCache.Instance.DockableLocations.OrderBy(i => i.Distance).FirstOrDefault(i => i.IsOnGridWithMe).Dock();
                            ESCache.Instance.PauseAfterNextDock = true;
                        }

                        if (ESCache.Instance.EveAccount.IsLeader && ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Id != 0)
                            if (ESCache.Instance.EveAccount.LeaderEntityId != ESCache.Instance.MyShipEntity.Id)
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LeaderEntityId), ESCache.Instance.MyShipEntity.Id);

                        Time.Instance.LastOfflineModuleCheck = DateTime.UtcNow;

                        foreach (ModuleCache mod in ESCache.Instance.Modules.Where(m => !m.IsOnline))
                            Log.WriteLine("Offline module: [" + mod.TypeName + "] Damage% [" + mod.DamagePercent + "]");

                        Log.WriteLine("Offline modules found, going back to base trying to fit again");
                        MissionSettings.CurrentFit = string.Empty;
                        MissionSettings.OfflineModulesFound = true;
                        Traveler.Destination = null;
                        Traveler.ChangeTravelerState(TravelerState.Idle);

                        if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                        {
                            State.CurrentQuestorState = QuestorState.Start;
                            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                            MissionSettings.StrCurrentAgentName = null;
                            if (MissionSettings.AgentToPullNextRegularMissionFrom == null)
                            {
                                Log.WriteLine("Offline Modules Found!: Agent Error! if (MissionSettings.AgentToPullNextRegularMissionFrom == null)");
                                State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                            }
                        }

                        if (ESCache.Instance.SelectedController == "AbyssalDeadspaceController" && State.CurrentAbyssalDeadspaceBehaviorState != States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark)
                        {
                            Log.WriteLine("Offline Modules Found! Go Home.");
                            State.CurrentAbyssalDeadspaceBehaviorState = States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark;
                            return;
                        }

                        return;
                    }

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense: 2 Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (ESCache.Instance.DockableLocations.Any(i => i.IsOnGridWithMe))
                    if (Time.Instance.LastDamagedModuleCheck.AddSeconds(10) < DateTime.UtcNow && ESCache.Instance.InSpace
                        && ESCache.Instance.Modules.Any(m => m.DamagePercent > 0)
                        && !ESCache.Instance.Paused
                        && !ESCache.Instance.InMission
                        && State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.GotoBase
                        && State.CurrentAbyssalDeadspaceBehaviorState != States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark)
                    {
                        foreach (ModuleCache mod in ESCache.Instance.Modules.Where(m => m.DamagePercent > 1))
                            Log.WriteLine("Damaged module: [" + mod.TypeName + "] Damage% [" + Math.Round(mod.DamagePercent, 1) + "]");

                        Log.WriteLine("Damaged modules found, going back to base trying to fit again");
                        MissionSettings.CurrentFit = string.Empty;
                        MissionSettings.DamagedModulesFound = true;
                        ESCache.Instance.NeedRepair = true;
                        Traveler.Destination = null;
                        Traveler.ChangeTravelerState(TravelerState.Idle);

                        if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                        {
                            State.CurrentQuestorState = QuestorState.Start;
                            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                            MissionSettings.StrCurrentAgentName = null;
                            if (MissionSettings.AgentToPullNextRegularMissionFrom == null)
                            {
                                Log.WriteLine("Offline Modules Found!: Agent Error! if (MissionSettings.AgentToPullNextRegularMissionFrom == null)");
                                State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                            }
                        }

                        if (ESCache.Instance.SelectedController == "AbyssalDeadspaceController" && State.CurrentAbyssalDeadspaceBehaviorState != States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark)
                        {
                            Log.WriteLine("Damaged Modules Found! Go Home.");
                            State.CurrentAbyssalDeadspaceBehaviorState = States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark;
                            return;
                        }

                        /**
                        if (ESCache.Instance.)
                        {
                            State.CurrentQuestorState = QuestorState.Start;
                            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                            MissionSettings.strCurrentAgentName = null;
                            if (MissionSettings.AgentToPullNextRegularMissionFrom == null)
                            {
                                Log.WriteLine("Offline Modules Found!: Agent Error! if (MissionSettings.AgentToPullNextRegularMissionFrom == null)");
                                State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                            }
                        }
                        **/
                        return;
                    }

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense: 3 Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (!ESCache.Instance.InSpace)
                {
                    if (DebugConfig.DebugDefense) Log.WriteLine("we are not in space (yet?)");
                    return;
                }

                try
                {
                    //This runs DetectSpawn every processstate so that we will cache the detectspawn result when we first enter a pocket
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.NotInAbyssalDeadspace)
                        if (DebugConfig.DebugDefense) Log.WriteLine("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.NotInAbyssalDeadspace)");

                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense: 4a Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                {
                    if (!PodWarningSent)
                    {
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.CAPSULE, "Character is in a capsule."));
                        Log.WriteLine("We are in a pod, no defense required...");
                        PodWarningSent = true;
                        return;
                    }

                    return;
                }

                if (ESCache.Instance.SelectedController == "AbyssalDeadspaceController")
                {
                    if (!ESCache.Instance.InAbyssalDeadspace && !ESCache.Instance.Stargates.Any())
                        return;

                    if (Statistics.StartedPocket.AddSeconds(12) > DateTime.UtcNow)
                        return;
                }

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense: 4b Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
                {
                    if (DebugConfig.DebugDefense) Log.WriteLine("we are cloaked... no defense needed.");
                    LastCloaked = DateTime.UtcNow;
                    return;
                }

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense: 4d Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (Time.Instance.LastDockAction.AddSeconds(5) > DateTime.UtcNow)
                    return;

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense: 4e Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense)
                    Log.WriteLine("Starting ActivateRepairModules() Current Health Stats: Shields: [" +
                                  ESCache.Instance.ActiveShip.ShieldPercentage +
                                  "%] Armor: [" + ESCache.Instance.ActiveShip.ArmorPercentage + "%] Cap: [" +
                                  ESCache.Instance.ActiveShip.CapacitorPercentage + "%] We are TargetedBy [" + Combat.Combat.TargetedByCount + "] entities");

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense: 4g Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting ActivateCovopsCloak();");
                ActivateCovopsCloak();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense: ActivateCovopsCloak Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense OverloadReps Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting RecordDefenseStatistics();");
                RecordDefenseStatistics();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense RecordDefenseStatistics Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting OverloadHardeners();");
                OverloadHardeners();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting OverloadReps();");
                OverloadReps();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting OverloadSpeedMod();");
                OverloadSpeedMod();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting ActivateHardeners();");
                ActivateHardeners();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateHardeners Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting ActivateShieldRepairAndCapInjectorModules();");
                ActivateShieldRepairAndCapInjectorModules();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateShieldRepairAndCapInjectorModules Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting ActivateArmorRepairModules();");
                ActivateArmorRepairModules();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting ActivateAssaultDamageControlModules();");
                ActivateAssaultDamageControlModules();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateArmorRepairModules Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense) Log.WriteLine("Starting ActivateOnce();");
                ActivateOnce();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateOnce Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (ESCache.Instance.ActiveShip.CapacitorPercentage < 10 && Combat.Combat.TargetedBy.Count == 0 &&
                    ESCache.Instance.Modules.Where(i => i.GroupId == (int)Group.ShieldBoosters ||
                                                        i.GroupId == (int)Group.AncillaryShieldBooster ||
                                                        i.GroupId == (int)Group.CapacitorInjector ||
                                                        i.GroupId == (int)Group.ArmorRepairer)
                        .All(x => !x.IsActive))
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("Cap is SO low that we should not care about hardeners/boosters as we are not being targeted anyhow)");
                    return;
                }

                if (ESCache.Instance.InWarp)
                {
                    if (Settings.Instance.KeepWeaponsGrouped) ESCache.Instance.GroupWeapons();
                    _trackingLinkScriptAttempts = 0;
                    _sensorBoosterScriptAttempts = 0;
                    _sensorDampenerScriptAttempts = 0;
                    _trackingComputerScriptAttempts = 0;
                    _trackingDisruptorScriptAttempts = 0;
                    return;
                }

                //if (DebugConfig.DebugDefense || DebugConfig.DebugSpeedMod)
                //    Log.WriteLine("Starting OverloadSpeedMod();");
                //Combat.Combat.OverloadSpeedMod();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense OverloadSpeedMod Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense || DebugConfig.DebugSpeedMod)
                    Log.WriteLine("Starting ActivateSpeedMod();");
                ActivateSpeedMod();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                    Log.WriteLine("Starting ActivateSmartBombs();");
                ActivateSmartBombs();

                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense ActivateSpeedMod Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();

                if (DebugConfig.DebugDefense)
                    Log.WriteLine("Starting ActivateCombatBoosters();");
                ActivateCombatBoosters();
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
            finally
            {
                DefenseStopWatch.Stop();
                if (DebugConfig.DebugDefensePerformance) Log.WriteLine("Defense Took [" + Util.ElapsedMicroSeconds(DefenseStopWatch) + "]");
                DefenseStopWatch.Restart();
            }
        }

        private static bool ActivateCombatBoosters()
        {
            try
            {
                //Dont activate boosters when hauling stuff ffs!
                if (Combat.Combat.CombatShipName != ESCache.Instance.ActiveShip.GivenName)
                    return true;

                if (ESCache.Instance.CurrentShipsCargo == null)
                    return false;

                if (ActivateThisCombatBoosterTypeIdWhenShieldsAreLow != 0 && ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == ActivateThisCombatBoosterTypeIdWhenShieldsAreLow))
                {
                    if (ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule) && ActivateCombatBoosterWhenShieldsAreBelowThisPercentage > ESCache.Instance.ActiveShip.ShieldPercentage)
                    {
                        if (Arm.CheckTheseBoosters(BoosterTypesToLoadIntoCargo, ESCache.Instance.CurrentShipsCargo)) return true;
                        return true;
                    }
                }

                if (ActivateThisCombatBoosterTypeIdWhenArmorIsLow != 0 && ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == ActivateThisCombatBoosterTypeIdWhenArmorIsLow))
                {
                    if (ESCache.Instance.Modules.Any(i => i.IsArmorRepairModule) && ActivateCombatBoosterWhenArmorIsBelowThisPercentage > ESCache.Instance.ActiveShip.ArmorPercentage)
                    {
                        if (Arm.CheckTheseBoosters(BoosterTypesToLoadIntoCargo, ESCache.Instance.CurrentShipsCargo)) return true;
                    }
                }

                /**
                if (ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == ActivateThisBoosterWhenCapacitorIsLow))
                {
                    if (ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule) && ActivateBoosterWhenCapacitorIsBelowPercentage > ESCache.Instance.ActiveShip.CapacitorPercentage)
                    {
                        if (Arm.CheckTheseBoosters(BoosterTypesToLoadIntoCargo, ESCache.Instance.CurrentShipsCargo)) return true;
                    }
                }
                **/

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static void ActivateArmorRepairModules()
        {
            if (DateTime.UtcNow < Time.Instance.NextRepModuleAction)
            {
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("if (DateTime.UtcNow < Time.Instance.NextRepModuleAction [" +
                                  Time.Instance.NextRepModuleAction.Subtract(DateTime.UtcNow).TotalSeconds +
                                  " Sec from now])");
                return;
            }

            ModuleNumber = 0;
            foreach (ModuleCache armorRepairModule in ESCache.Instance.Modules.Where(i => i.IsArmorRepairModule && i.IsOnline && !i._module.IsBeingRepaired)
                .OrderByDescending(a => a._module.Attributes.TryGet<int>("armorDamageAmount")))
            {
                ModuleNumber++;
                if (armorRepairModule.InLimboState)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + armorRepairModule.TypeName + "][" + armorRepairModule._module.ItemId + "][" + armorRepairModule.IsActive + "] is InLimboState, continue");
                    continue;
                }

                double perc;
                double cap;
                cap = ESCache.Instance.ActiveShip.CapacitorPercentage;

                if (armorRepairModule.GroupId == (int)Group.ArmorRepairer)
                {
                    perc = ESCache.Instance.ActiveShip.ArmorPercentage;
                    if (DebugConfig.DebugDefenseSimulateLowArmor)
                    {
                        Log.WriteLine("DebugDefenseSimulateLowShield: ShouldWeDeactivate: Pretending Armor is at 70%: really it is [" + perc + "]");
                        perc = 70;
                    }
                    if (DebugConfig.DebugDefenseSimulateReallyLowArmor)
                    {
                        Log.WriteLine("DebugDefenseSimulateLowShield: ShouldWeDeactivate: Pretending Armor is at 70%: really it is [" + perc + "]");
                        perc = 10;
                    }
                }
                else
                    continue;

                bool inCombat = ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy) || Combat.Combat.PotentialCombatTargets.Count > 0;

                if (perc >= DeactivateRepairModulesAtThisPerc)
                {
                    if (!armorRepairModule.IsActive)
                        continue;

                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Attempting to Click to Deactivate [" +
                                      ModuleNumber +
                                      "][" + armorRepairModule.TypeName + "][" + armorRepairModule._module.ItemId + "]");
                    if (armorRepairModule.Click())
                    {
                        Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                        if (ESCache.Instance.ActiveShip.IsDread)
                            Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(2000);

                        Statistics.RepairCycleTimeThisPocket += (int)DateTime.UtcNow.Subtract(Time.Instance.StartedBoosting).TotalSeconds;
                        Statistics.RepairCycleTimeThisMission += (int)DateTime.UtcNow.Subtract(Time.Instance.StartedBoosting).TotalSeconds;
                        perc = ESCache.Instance.ActiveShip.ArmorPercentage;
                        if (DebugConfig.DebugDefenseSimulateLowArmor)
                        {
                            Log.WriteLine("DebugDefenseSimulateLowShield: Deactivated: Pretending Armor is at 70%: really it is [" + perc + "]");
                            perc = 70;
                        }
                        if (DebugConfig.DebugDefenseSimulateReallyLowArmor)
                        {
                            Log.WriteLine("DebugDefenseSimulateLowShield: Deactivated: Pretending Armor is at 70%: really it is [" + perc + "]");
                            perc = 10;
                        }

                        Log.WriteLine("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Armor Repairer: deactivated [" + armorRepairModule._module.ItemId + "]");
                        return;
                    }
                }

                if ((inCombat && perc < ActivateRepairModulesAtThisPerc) || (!inCombat && perc < DeactivateRepairModulesAtThisPerc && cap > Panic.SafeCapacitorPct))
                {
                    if (armorRepairModule.IsActive)
                        continue;

                    if (ESCache.Instance.UnlootedContainers != null && Statistics.WrecksThisPocket != ESCache.Instance.UnlootedContainers.Count)
                        Statistics.WrecksThisPocket = ESCache.Instance.UnlootedContainers.Count;

                    if (ESCache.Instance.ActiveShip.Capacitor == 0)
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("Capacitor [" + ESCache.Instance.ActiveShip.Capacitor + "] == 0");
                        continue;
                    }

                    if (ESCache.Instance.ActiveShip.Capacitor == 0 || (armorRepairModule.CapacitorNeed != null && armorRepairModule.CapacitorNeed > ESCache.Instance.ActiveShip.Capacitor))
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("Capacitor [" + ESCache.Instance.ActiveShip.Capacitor + "] is less than [" + armorRepairModule.CapacitorNeed + "] needed to activate [" + armorRepairModule.TypeName + "]");
                        continue;
                    }

                    if (ESCache.Instance.ActiveShip.CapacitorPercentage == 0 || ESCache.Instance.ActiveShip.CapacitorPercentage < 3)
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("if (Cache.Instance.ActiveShip.CapacitorPercentage [" + ESCache.Instance.ActiveShip.CapacitorPercentage +
                                          "] < 3)");
                        continue;
                    }

                    if (ESCache.Instance.Modules.Where(i => i.IsArmorRepairModule).All(x => !x.IsActive))
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("SingleRep: Perc: [" + Math.Round(perc, 0) + "%] < [" + ActivateSecondRepairModulesAtThisPerc + "] myCap: [" + Math.Round(cap, 0) + "%] Attempting to Click [" + armorRepairModule.TypeName + "][" + armorRepairModule._module.ItemId + "]");

                        if (armorRepairModule.Click())
                        {
                            Time.Instance.StartedBoosting = DateTime.UtcNow;
                            Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                            if (ESCache.Instance.ActiveShip.IsDread)
                                Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(2000);


                            if (ESCache.Instance.ActiveShip.ArmorPercentage < 100)
                                ESCache.Instance.NeedRepair = true;

                            perc = ESCache.Instance.ActiveShip.ArmorPercentage;
                            if (DebugConfig.DebugDefenseSimulateLowArmor)
                            {
                                Log.WriteLine("DebugDefenseSimulateLowShield: Activated: Pretending Armor is at 70%: really it is [" + perc + "]");
                                perc = 70;
                            }
                            if (DebugConfig.DebugDefenseSimulateReallyLowArmor)
                            {
                                Log.WriteLine("DebugDefenseSimulateLowShield: Activated: Pretending Armor is at 70%: really it is [" + perc + "]");
                                perc = 10;
                            }

                            Log.WriteLine("Tank % [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Armor Repairer: activated [" + armorRepairModule._module.ItemId + "]");
                            int aggressiveEntities = ESCache.Instance.EntitiesOnGrid.Count(e => e.IsAttacking && e.IsPlayer);
                            if (aggressiveEntities == 0 && ESCache.Instance.EntitiesOnGrid.Any(e => e.IsStation || e.IsCitadel))
                            {
                                Time.Instance.NextDockAction = DateTime.UtcNow.AddSeconds(1);
                                Log.WriteLine("Repairing Armor outside station with no aggro (yet): delaying docking for a few seconds");
                            }

                            return;
                        }
                    }
                    else if (perc < ActivateSecondRepairModulesAtThisPerc) //If we have 1 armor rep already going
                    {
                        if (ESCache.Instance.ActiveShip.IsDread && !DreadRepairWhileInWarp)
                        {
                            if (DebugConfig.DebugDefense)
                                Log.WriteLine("SecondRep: Perc: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] ESCache.Instance.ActiveShip.IsDread [" + ESCache.Instance.ActiveShip.IsDread + "]");
                            if (ESCache.Instance.InWarp)
                            {
                                if (DebugConfig.DebugDefense)
                                {
                                    Log.WriteLine("Perc: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] ESCache.Instance.InWarp [" + ESCache.Instance.InWarp + "] Not activating armor repair module");
                                }

                                return; //no need to continue to loop through all the guns this logic tree is the same for all guns.
                            }
                        }

                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("SecondRep: Perc: [" + Math.Round(perc, 0) + "%] < [" + ActivateSecondRepairModulesAtThisPerc + "] myCap: [" + Math.Round(cap, 0) + "%] Attempting to Click to Activate [" +
                                          ModuleNumber +
                                          "][" + armorRepairModule.TypeName + "].");
                        if (armorRepairModule.Click())
                        {
                            Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                            return;
                        }
                    }
                }


            }
        }

        private static void ActivateAssaultDamageControlModules()
        {
            if (DateTime.UtcNow < Time.Instance.NextAssaultDamageControlModuleAction)
            {
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("if (DateTime.UtcNow < Time.Instance.NextAssaultDamageControlModuleAction [" +
                                  Time.Instance.NextAssaultDamageControlModuleAction.Subtract(DateTime.UtcNow).TotalSeconds +
                                  " Sec from now])");
                return;
            }

            //
            // ...
            //
            ModuleNumber = 0;
            foreach (ModuleCache assultDamageControlModule in ESCache.Instance.Modules.Where(i => i.GroupId == (int)Group.AssaultDamageControl)
                .Where(x => x.IsOnline).OrderByDescending(a => a._module.Attributes.TryGet<int>("armorDamageAmount")))
            {
                ModuleNumber++;
                if (assultDamageControlModule.InLimboState)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + assultDamageControlModule.TypeName + "] is InLimboState, continue");
                    continue;
                }

                double perc;
                double cap;
                cap = ESCache.Instance.ActiveShip.CapacitorPercentage;

                if (ESCache.Instance.ActiveShip.IsArmorTanked)
                    perc = ESCache.Instance.ActiveShip.ArmorPercentage;
                else
                    perc = ESCache.Instance.ActiveShip.ShieldPercentage;

                bool inCombat = ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy) || Combat.Combat.PotentialCombatTargets.Count > 0;
                if (!assultDamageControlModule.IsActive &&
                    (inCombat && perc < ActivateAssaultDamageControlModulesAtThisPerc))
                {
                    if (ESCache.Instance.ActiveShip.Capacitor == 0)
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("Capacitor [" + ESCache.Instance.ActiveShip.Capacitor + "] == 0");
                        continue;
                    }

                    if (ESCache.Instance.ActiveShip.Capacitor == 0 || (assultDamageControlModule.CapacitorNeed != null && assultDamageControlModule.CapacitorNeed > ESCache.Instance.ActiveShip.Capacitor))
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("Capacitor [" + ESCache.Instance.ActiveShip.Capacitor + "] is less than [" + assultDamageControlModule.CapacitorNeed + "] needed to activate [" + assultDamageControlModule.TypeName + "]");
                        continue;
                    }

                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("Perc: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Attempting to Click [" +
                                        ModuleNumber +
                                        "][" + assultDamageControlModule.TypeName + "]");
                    if (assultDamageControlModule.Click())
                    {
                        Time.Instance.NextAssaultDamageControlModuleAction = DateTime.UtcNow.AddSeconds(20);

                        Log.WriteLine("Tank % [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] AssultDamageControl: [" +
                                        ModuleNumber + "] activated");
                        return;
                    }
                }
            }
        }


        private static void ActivateHardeners()
        {
            if (DateTime.UtcNow < Time.Instance.NextActivateModules)
            {
                if (DebugConfig.DebugDefense) Log.WriteLine("if (DateTime.UtcNow < Time.Instance.NextActivateModules)");

                return;
            }

            ModuleNumber = 0;
            foreach (ModuleCache hardener in ESCache.Instance.Modules.Where(i => i.IsShieldHardener || i.IsArmorHardener || (i.IsShieldHardener && Defense.ActivateRepairModulesAtThisPerc > 100 && Combat.Combat.PotentialCombatTargets.Any())))
            {
                if (!hardener.IsActivatable)
                {
                    if (DebugConfig.DebugDefense) Log.WriteLine("if (!hardener.IsActivatable)");
                    continue;
                }

                ModuleNumber++;

                if (DebugConfig.DebugDefense)
                    Log.WriteLine("[" + ModuleNumber + "][" + hardener.TypeName + "] TypeID [" + hardener.TypeId +
                                  "] GroupId [" +
                                  hardener.GroupId + "] Activatable [" + hardener.IsActivatable + "] Found");

                if (hardener.IsActive)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + hardener.TypeName + "] is already active");
                    continue;
                }

                if (hardener.InLimboState)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + hardener.TypeName +
                                      "] is in LimboState (likely being activated or decativated already)");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.Capacitor < 45)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + hardener.TypeName +
                                      "] You have less then 45 UNITS of cap: do not make it worse by turning on the hardeners");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.CapacitorPercentage < 3)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + hardener.TypeName +
                                      "] You have less then 3% of cap: do not make it worse by turning on the hardeners");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.Capacitor < 400 && Combat.Combat.TargetedBy.Count == 0
                    && !string.IsNullOrEmpty(Combat.Combat.CombatShipName)
                    && ESCache.Instance.ActiveShip.GivenName.ToLower() == Combat.Combat.CombatShipName.ToLower())
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + hardener.TypeName +
                                      "] You have less then 400 units total cap and nothing is targeting you yet, no need for hardeners yet.");
                    continue;
                }

                if (hardener.Click())
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + hardener.TypeName + "] activated");
                    continue;
                }
            }
        }

        private static void ActivateOnce()
        {
            if (DateTime.UtcNow < Time.Instance.NextActivateModules)
                return;

            ModuleNumber = 0;
            foreach (ModuleCache ActivateOncePerSessionModulewScript in ESCache.Instance.Modules.Where(i => i.GroupId == (int) Group.TrackingDisruptor ||
                                                                                                            i.GroupId == (int) Group.TrackingComputer ||
                                                                                                            i.GroupId == (int) Group.TrackingLink ||
                                                                                                            i.GroupId == (int) Group.SensorBooster ||
                                                                                                            i.GroupId == (int) Group.MissileGuidanceComputer ||
                                                                                                            i.GroupId == (int) Group.SensorDampener ||
                                                                                                            i.GroupId == (int) Group.CapacitorInjector ||
                                                                                                            i.GroupId == (int) Group.AncillaryShieldBooster))
            {
                if (!ActivateOncePerSessionModulewScript.IsActivatable)
                    continue;

                if (ActivateOncePerSessionModulewScript.IsPendingOverloading)
                    continue;

                if (ActivateOncePerSessionModulewScript.ChargeQty < ActivateOncePerSessionModulewScript.MaxCharges)
                {
                    if (DebugConfig.DebugLoadScripts)
                        Log.WriteLine("Found Activatable Module with no charge[typeID:" + ActivateOncePerSessionModulewScript.TypeId + "]");
                    DirectItem scriptToLoad;

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.TrackingDisruptor && _trackingDisruptorScriptAttempts < 10)
                    {
                        _trackingDisruptorScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Log.WriteLine("TrackingDisruptor Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(Settings.Instance.TrackingDisruptorScript, 1);

                        if (scriptToLoad != null)
                        {
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    if (DebugConfig.DebugLoadScripts) Log.WriteLine("Deactivate TrackingDisruptor");
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ModuleNumber++;
                                return;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                return;
                            }

                            return;
                        }

                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.TrackingComputer && _trackingComputerScriptAttempts < 10)
                    {
                        _trackingComputerScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Log.WriteLine("TrackingComputer Found");
                        DirectItem TrackingComputerScript = ESCache.Instance.CheckCargoForItem(Settings.Instance.TrackingComputerScript, 1);

                        EntityCache EntityTrackingDisruptingMe = Combat.Combat.TargetedBy.Find(t => t.IsTrackingDisruptingMe);
                        if (EntityTrackingDisruptingMe != null || TrackingComputerScript == null)
                            TrackingComputerScript = ESCache.Instance.CheckCargoForItem((int) TypeID.OptimalRangeScript, 1);

                        scriptToLoad = TrackingComputerScript;
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts) Log.WriteLine("Script Found for TrackingComputer");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    if (DebugConfig.DebugLoadScripts) Log.WriteLine("DeActivate TrackingComputer");
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ModuleNumber++;
                                return;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }

                            return;
                        }

                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.TrackingLink && _trackingLinkScriptAttempts < 10)
                    {
                        _trackingLinkScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Log.WriteLine("TrackingLink Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(Settings.Instance.TrackingLinkScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts) Log.WriteLine("Script Found for TrackingLink");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ModuleNumber++;
                                return;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }

                            return;
                        }

                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.SensorBooster && _sensorBoosterScriptAttempts < 10)
                    {
                        _sensorBoosterScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Log.WriteLine("SensorBooster Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(Settings.Instance.SensorBoosterScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts) Log.WriteLine("Script Found for SensorBooster");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ModuleNumber++;
                                return;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }

                            return;
                        }

                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.SensorDampener && _sensorDampenerScriptAttempts < 10)
                    {
                        _sensorDampenerScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Log.WriteLine("SensorDampener Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(Settings.Instance.SensorDampenerScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts) Log.WriteLine("Script Found for SensorDampener");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ModuleNumber++;
                                return;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }

                            return;
                        }

                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.AncillaryShieldBooster)
                    {
                        if (DebugConfig.DebugLoadScripts) Log.WriteLine("ancillaryShieldBooster Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(Settings.Instance.AncillaryShieldBoosterScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts)
                                Log.WriteLine("CapBoosterCharges Found for ancillaryShieldBooster");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                    return;
                                }

                            bool inCombat = Combat.Combat.TargetedBy.Count > 0;
                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline ||
                                (inCombat && ActivateOncePerSessionModulewScript.ChargeQty > 0))
                            {
                                ModuleNumber++;
                                return;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }

                            return;
                        }

                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.CapacitorInjector)
                    {
                        if (DebugConfig.DebugLoadScripts) Log.WriteLine("capacitorInjector Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(Settings.Instance.CapacitorInjectorScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts)
                                Log.WriteLine("CapBoosterCharges Found for capacitorInjector");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                    return;
                                }

                            bool inCombat = Combat.Combat.TargetedBy.Count > 0;
                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline ||
                                (inCombat && ActivateOncePerSessionModulewScript.ChargeQty > 0))
                            {
                                ModuleNumber++;
                                return;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }
                        }
                        else if (ActivateOncePerSessionModulewScript.ChargeQty == 0)
                        {
                            Combat.Combat.ChangeCombatState(CombatState.OutOfAmmo, "ReloadCapBooster: ran out of cap booster with typeid: [ " + Settings.Instance.CapacitorInjectorScript + " ]");
                            continue;
                        }

                        ModuleNumber++;
                        continue;
                    }
                }

                ModuleNumber++;
            }

            ModuleNumber = 0;
            foreach (ModuleCache ActivateOncePerSessionModule in ESCache.Instance.Modules.Where(i => i.GroupId == (int) Group.SensorBooster ||
                                                                                                     i.GroupId == (int) Group.TrackingComputer ||
                                                                                                     i.GroupId == (int) Group.MissileGuidanceComputer ||
                                                                                                     i.GroupId == (int) Group.ECCM ||
                                                                                                     i.GroupId == (int) Group.DroneTrackingLink))
            {
                if (!ActivateOncePerSessionModule.IsActivatable)
                    continue;

                ModuleNumber++;

                if (DebugConfig.DebugDefense)
                    Log.WriteLine("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName + "] TypeID [" + ActivateOncePerSessionModule.TypeId +
                                  "] GroupId [" +
                                  ActivateOncePerSessionModule.GroupId + "] Activatable [" + ActivateOncePerSessionModule.IsActivatable + "] Found");

                if (ActivateOncePerSessionModule.IsActive)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName + "] is already active");
                    continue;
                }

                if (ActivateOncePerSessionModule.InLimboState)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName +
                                      "] is in LimboState (likely being activated or decativated already)");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.Capacitor < 45)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName +
                                      "] You have less then 45 UNITS of cap: do not make it worse by turning on the hardeners");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.CapacitorPercentage < 3)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName +
                                      "] You have less then 3% of cap: do not make it worse by turning on the hardeners");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.Capacitor < 400 && Combat.Combat.TargetedBy.Count == 0 &&
                    ESCache.Instance.ActiveShip.GivenName.ToLower() == Combat.Combat.CombatShipName.ToLower())
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName +
                                      "] You have less then 400 units total cap and nothing is targeting you yet, no need for hardeners yet.");
                    continue;
                }

                if (ActivateOncePerSessionModule.Click())
                {
                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName + "] activated");
                    return;
                }
            }

            ModuleNumber = 0;
        }

        private static void ActivateShieldRepairAndCapInjectorModules()
        {
            if (DateTime.UtcNow < Time.Instance.NextRepModuleAction)
            {
                if (DebugConfig.DebugDefense)
                    Log.WriteLine("if (DateTime.UtcNow < Time.Instance.NextRepModuleAction [" +
                                  Time.Instance.NextRepModuleAction.Subtract(DateTime.UtcNow).TotalSeconds +
                                  " Sec from now])");
                return;
            }

            ModuleNumber = 0;
            foreach (ModuleCache repairModule in ESCache.Instance.Modules.Where(i => i.GroupId == (int) Group.ShieldBoosters ||
                                                                                     i.GroupId == (int) Group.AncillaryShieldBooster ||
                                                                                     i.GroupId == (int) Group.CapacitorInjector)
                .Where(x => x.IsOnline))
            {
                ModuleNumber++;
                if (repairModule.InLimboState)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("[" + ModuleNumber + "][" + repairModule.TypeName + "] is InLimboState, continue");
                    continue;
                }

                double perc;
                double cap;
                cap = ESCache.Instance.ActiveShip.CapacitorPercentage;
                perc = ESCache.Instance.ActiveShip.ShieldPercentage;
                if (DebugConfig.DebugDefenseSimulateLowShield)
                {
                    Log.WriteLine("DebugDefenseSimulateLowShield: Pretending Shields are at 10%: really they are at [" + perc + "]");
                    perc = 10;
                }

                bool inCombat = ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy) || Combat.Combat.PotentialCombatTargets.Count > 0;
                if (!repairModule.IsActive && inCombat && cap < InjectCapPerc && repairModule.GroupId == (int) Group.CapacitorInjector &&
                    repairModule.ChargeQty > 0)
                    if (repairModule.Click())
                    {
                        Log.WriteLine("Cap: [" + Math.Round(cap, 0) + "%] Capacitor Booster: [" + ModuleNumber + "] activated");
                        return;
                    }

                if (!repairModule.IsActive &&
                    ((inCombat && perc < ActivateRepairModulesAtThisPerc) ||
                     (!inCombat && perc < DeactivateRepairModulesAtThisPerc && cap > Panic.SafeCapacitorPct)))
                {
                    if (ESCache.Instance.UnlootedContainers != null && Statistics.WrecksThisPocket != ESCache.Instance.UnlootedContainers.Count)
                        Statistics.WrecksThisPocket = ESCache.Instance.UnlootedContainers.Count;

                    if (repairModule.GroupId == (int) Group.AncillaryShieldBooster)
                        if (repairModule.ChargeQty > 0)
                            if (repairModule.Click())
                            {
                                Log.WriteLine("Perc: [" + Math.Round(perc, 0) + "%] Ancillary Shield Booster: [" + ModuleNumber + "] activated");
                                Time.Instance.StartedBoosting = DateTime.UtcNow;
                                Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                return;
                            }

                    if (ESCache.Instance.ActiveShip.Capacitor == 0)
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("Capacitor [" + ESCache.Instance.ActiveShip.Capacitor + "] == 0");
                        continue;
                    }

                    if (ESCache.Instance.ActiveShip.Capacitor == 0 || (repairModule.CapacitorNeed != null && repairModule.CapacitorNeed > ESCache.Instance.ActiveShip.Capacitor))
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("Capacitor [" + ESCache.Instance.ActiveShip.Capacitor + "] is less than [" + repairModule.CapacitorNeed + "] needed to activate [" + repairModule.TypeName + "]");
                        continue;
                    }

                    if (ESCache.Instance.ActiveShip.CapacitorPercentage == 0 || ESCache.Instance.ActiveShip.CapacitorPercentage < 3)
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("if (Cache.Instance.ActiveShip.CapacitorPercentage [" + ESCache.Instance.ActiveShip.CapacitorPercentage +
                                          "] < 3)");
                        continue;
                    }

                    if (ESCache.Instance.ActiveShip.IsDread && !DreadRepairWhileInWarp)
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("Perc: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] ESCache.Instance.ActiveShip.IsDread [" + ESCache.Instance.ActiveShip.IsDread + "]");
                        if (ESCache.Instance.InWarp)
                        {
                            if (DebugConfig.DebugDefense)
                            {
                                Log.WriteLine("Perc: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] ESCache.Instance.InWarp [" + ESCache.Instance.InWarp + "] Not activating armor repair module");
                            }

                            return; //no need to continue to loop through all the guns. this logic tree is the same for all guns.
                        }
                    }

                    if (repairModule.GroupId == (int) Group.ShieldBoosters)
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine("Perc: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Attempting to Click to Activate [" +
                                          ModuleNumber +
                                          "][" + repairModule.TypeName + "]");

                        if (repairModule.Click())
                        {
                            Time.Instance.StartedBoosting = DateTime.UtcNow;
                            Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);

                            if (ESCache.Instance.ActiveShip.ArmorPercentage < 100)
                                ESCache.Instance.NeedRepair = true;

                            if (repairModule.GroupId == (int) Group.ShieldBoosters || repairModule.GroupId == (int) Group.AncillaryShieldBooster)
                            {
                                perc = ESCache.Instance.ActiveShip.ShieldPercentage;
                                if (DebugConfig.DebugDefenseSimulateLowShield)
                                {
                                    Log.WriteLine("DebugDefenseSimulateLowShield: Pretending Shields are at 10%: really they are at [" + perc + "]");
                                    perc = 10;
                                }

                                Log.WriteLine("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Shield Booster: [" +
                                              ModuleNumber + "] activated");
                            }

                            return;
                        }
                    }
                }

                if (repairModule.IsActive && (perc >= DeactivateRepairModulesAtThisPerc || repairModule.GroupId == (int) Group.CapacitorInjector))
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Attempting to Click to Deactivate [" +
                                      ModuleNumber +
                                      "][" + repairModule.TypeName + "]");
                    if (repairModule.Click())
                        try
                        {
                            Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                            Statistics.RepairCycleTimeThisPocket += (int)DateTime.UtcNow.Subtract(Time.Instance.StartedBoosting).TotalSeconds;
                            Statistics.RepairCycleTimeThisMission += (int)DateTime.UtcNow.Subtract(Time.Instance.StartedBoosting).TotalSeconds;
                            if (repairModule.GroupId == (int) Group.ShieldBoosters || repairModule.GroupId == (int) Group.CapacitorInjector)
                            {
                                perc = ESCache.Instance.ActiveShip.ShieldPercentage;
                                if (DebugConfig.DebugDefenseSimulateLowShield)
                                {
                                    Log.WriteLine("DebugDefenseSimulateLowShield: Pretending Shields are at 10%: really they are at [" + perc + "]");
                                    perc = 10;
                                }

                                Log.WriteLine("Tank %: [" + Math.Round(perc, 0) + "%]  Cap: [" + Math.Round(cap, 0) + "%] Shield Booster: [" + ModuleNumber +
                                              "] deactivated [" +
                                              Math.Round(Time.Instance.NextRepModuleAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                                              "] sec reactivation delay - DeactivateRepairModulesAtThisPerc [" + DeactivateRepairModulesAtThisPerc + "] ActivateRepairModulesAtThisPerc [" + ActivateRepairModulesAtThisPerc + "]");
                            }

                            return;
                        }
                        catch (Exception)
                        {
                            return;
                        }
                }
            }
        }

        private static void ActivateSmartBombs()
        {
            if (!DebugConfig.DebugSmartBombs)
                return;

            if (ESCache.Instance.Paused && !DebugConfig.DebugSmartBombs)
                return;

            if (DebugConfig.DebugSmartBombs) Log.WriteLine("ActivateSmartBombs");

            ModuleNumber = 0;
            foreach (ModuleCache SmartBombModule in ESCache.Instance.Modules.Where(i => i.GroupId == (int)Group.SmartBomb))
            {
                if (DateTime.UtcNow < Time.Instance.NextActivateModules)
                    return;

                if (!SmartBombModule.IsActivatable)
                    continue;

                ModuleNumber++;

                if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                    Log.WriteLine("[" + ModuleNumber + "][" + SmartBombModule.TypeName + "] TypeID [" + SmartBombModule.TypeId +
                                  "] GroupId [" +
                                  SmartBombModule.GroupId + "] Activatable [" + SmartBombModule.IsActivatable + "] Found");

                if (SmartBombModule.IsActive)
                {
                    if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                        Log.WriteLine("[" + ModuleNumber + "][" + SmartBombModule.TypeName + "] is already active");
                    continue;
                }

                if (SmartBombModule.InLimboState)
                {
                    if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                        Log.WriteLine("[" + ModuleNumber + "][" + SmartBombModule.TypeName +
                                      "] is in LimboState (likely being activated or decativated already)");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.Capacitor < 45)
                {
                    if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                        Log.WriteLine("[" + ModuleNumber + "][" + SmartBombModule.TypeName +
                                      "] You have less then 45 UNITS of cap: do not make it worse by turning on a smartbomb");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.CapacitorPercentage < 3)
                {
                    if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                        Log.WriteLine("[" + ModuleNumber + "][" + SmartBombModule.TypeName +
                                      "] You have less then 3% of cap: do not make it worse by turning on a smartbomb");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.Capacitor < 400 && Combat.Combat.TargetedBy.Count == 0
                    && !string.IsNullOrEmpty(Combat.Combat.CombatShipName)
                    && ESCache.Instance.ActiveShip.GivenName.ToLower() == Combat.Combat.CombatShipName.ToLower())
                {
                    if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                        Log.WriteLine("[" + ModuleNumber + "][" + SmartBombModule.TypeName +
                                      "] You have less then 400 units total cap and nothing is targeting you yet, no need for smartbombs yet.");
                    continue;
                }

                if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.EntitiesOnGrid.Any(i => i.Id != ESCache.Instance.MyShipEntity.Id && (i.IsPlayer || i.IsStation || i.IsCitadel || i.IsStargate || i.IsEntityIShouldLeaveAlone || i.IsBadIdea) && 40000 > i.Distance))
                {
                    if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                    {
                        Log.WriteLine("[" + ModuleNumber + "][" + SmartBombModule.TypeName + "] if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.EntitiesOnGrid.Any(i => i.Id != ESCache.Instance.MyShipEntity.Id && (i.IsPlayer || i.IsStation || i.IsCitadel || i.IsStargate || i.IsEntityIShouldLeaveAlone || i.IsBadIdea) && 40000 > i.Distance))");
                    }

                    continue;
                }

                if (Combat.Drones.ActiveDrones.Any(i => 6000 > i.Distance))
                {
                    if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                    {
                        Log.WriteLine("[" + ModuleNumber + "][" + SmartBombModule.TypeName + "] if (Combat.Drones.ActiveDrones.Any(i => 6000 > i.Distance))");
                    }

                    continue;
                }

                if (DebugConfig.DebugSmartBombs && ESCache.Instance.HostileMissilesInSpace.Any())
                {
                    foreach (EntityCache HostileMissileInSpace in ESCache.Instance.HostileMissilesInSpace)
                    {
                        Log.WriteLine("DebugSmartBombs: HostileMissileInSpace [" + Math.Round(HostileMissileInSpace.Distance / 1000, 2) + "]k Velocity [" + HostileMissileInSpace.Velocity + "] Health [" + HostileMissileInSpace.Health + "] GroupID [" + HostileMissileInSpace.GroupId + "] TypeName [" + HostileMissileInSpace.TypeName + "] Owner [" + HostileMissileInSpace._directEntity.OwnerId + "]");
                    }
                }

                if (ESCache.Instance.HostileMissilesInSpace.Any(i => i.GroupId == (int)Group.HeavyMissiles && 24000 > i.Distance))
                {
                    if (SmartBombModule.Click())
                    {
                        Time.Instance.NextActivateSmartBomb = DateTime.UtcNow.AddMilliseconds(1000);
                        if (DebugConfig.DebugDefense || DebugConfig.DebugSmartBombs)
                            Log.WriteLine("[" + ModuleNumber + "][" + SmartBombModule.TypeName + "] activated");
                        return;
                    }
                }
            }
        }

        private static bool LoadthisScript(DirectItem scriptToLoad, ModuleCache module)
        {
            if (scriptToLoad != null)
            {
                if (module.IsReloadingAmmo || module.IsActive || module.IsDeactivating || module.InLimboState ||
                    !module.IsOnline)
                    return false;

                if (module.Charge != null && module.Charge.TypeId == scriptToLoad.TypeId && module.ChargeQty == module.MaxCharges)
                {
                    Log.WriteLine("module is already loaded with the script we wanted");
                    NextScriptReload[module.ItemId] = DateTime.UtcNow.AddSeconds(15);
                    return false;
                }

                if (NextScriptReload.ContainsKey(module.ItemId) && NextScriptReload[module.ItemId] > DateTime.UtcNow)
                {
                    Log.WriteLine("module was reloaded recently... skipping");
                    return false;
                }

                if (module.Charge != null && module.Charge.TypeId == scriptToLoad.TypeId)
                {
                    if (DateTime.UtcNow.Subtract(Time.Instance.LastLoggingAction).TotalSeconds > 10)
                        Time.Instance.LastLoggingAction = DateTime.UtcNow;

                    if (module.ReloadAmmo(scriptToLoad, 0, 0))
                    {
                        NextScriptReload[module.ItemId] = DateTime.UtcNow.AddSeconds(15);
                        Log.WriteLine("Reloading [" + module.TypeId + "] with [" + scriptToLoad.TypeName + "][TypeID: " + scriptToLoad.TypeId + "]");
                        return true;
                    }

                    return false;
                }

                if (DateTime.UtcNow.Subtract(Time.Instance.LastLoggingAction).TotalSeconds > 10)
                    Time.Instance.LastLoggingAction = DateTime.UtcNow;

                if (module.ChangeAmmo(scriptToLoad, 0, 0))
                {
                    NextScriptReload[module.ItemId] = DateTime.UtcNow.AddSeconds(15);
                    Log.WriteLine("Changing [" + module.TypeId + "] with [" + scriptToLoad.TypeName + "][TypeID: " + scriptToLoad.TypeId + "]");
                    return true;
                }

                return false;
            }
            Log.WriteLine("script to load was NULL!");
            return false;
        }

        private static void OverloadIndividualHighSlots()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(60))
                    return;

                //
                // Activate Rack Overload
                //
                if (ESCache.Instance.InMission && Combat.Combat.PotentialCombatTargets.Count > 0 && State.CurrentCombatMissionCtrlState == ActionControlState.ExecutePocketActions)
                {
                    if (attemptOverloadHighSlot1 && !overloadHighSlot1Completed && !overloadRackHighCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot1))
                        {
                            _lastOverloadHighSlot1 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading High Slot 1 [ CmdOverloadHighPowerSlot1 ]");
                            overloadHighSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadHighSlot2 && !overloadHighSlot2Completed && !overloadRackHighCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot2))
                        {
                            _lastOverloadHighSlot2 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading High Slot 2 [ CmdOverloadHighPowerSlot2 ]");
                            overloadHighSlot2Completed = true;
                            return;
                        }

                    if (attemptOverloadHighSlot3 && !overloadHighSlot3Completed && !overloadRackHighCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot3))
                        {
                            _lastOverloadHighSlot3 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading High Slot 3 [ CmdOverloadHighPowerSlot3 ]");
                            overloadHighSlot3Completed = true;
                            return;
                        }

                    if (attemptOverloadHighSlot4 && !overloadHighSlot4Completed && !overloadRackHighCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot4))
                        {
                            _lastOverloadHighSlot4 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading High Slot 4 [ CmdOverloadHighPowerSlot4 ]");
                            overloadHighSlot4Completed = true;
                            return;
                        }

                    if (attemptOverloadHighSlot5 && !overloadHighSlot5Completed && !overloadRackHighCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot5))
                        {
                            _lastOverloadHighSlot5 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading High Slot 5 [ CmdOverloadHighPowerSlot5 ]");
                            overloadHighSlot5Completed = true;
                            return;
                        }

                    if (attemptOverloadHighSlot6 && !overloadHighSlot6Completed && !overloadRackHighCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot6))
                        {
                            _lastOverloadHighSlot6 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading High Slot 6 [ CmdOverloadHighPowerSlot6 ]");
                            overloadHighSlot5Completed = true;
                            return;
                        }

                    if (attemptOverloadHighSlot7 && !overloadHighSlot7Completed && !overloadRackHighCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot7))
                        {
                            _lastOverloadHighSlot7 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading High Slot 7 [ CmdOverloadHighPowerSlot7 ]");
                            overloadHighSlot5Completed = true;
                            return;
                        }

                    if (attemptOverloadHighSlot8 && !overloadHighSlot8Completed && !overloadRackHighCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot8))
                        {
                            _lastOverloadHighSlot8 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading High Slot 8 [ CmdOverloadHighPowerSlot8 ]");
                            overloadHighSlot5Completed = true;
                            return;
                        }
                }

                //
                // Deactivate Rack Overload (why? damage a module in that rack?)
                //

                if (attemptOverloadHighSlot1 && overloadHighSlot1Completed)
                    if (_deactivateOverloadHighSlot1Seconds > 0 && DateTime.UtcNow > _lastOverloadHighSlot1.AddSeconds(_deactivateOverloadHighSlot1Seconds))
                    {
                        attemptOverloadHighSlot1 = false;
                        Log.WriteLine("Defense: Deactivating Overload High Slot 1 [ CmdOverloadHighPowerSlot1 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot1);
                    }

                if (attemptOverloadHighSlot2 && overloadHighSlot2Completed)
                    if (_deactivateOverloadHighSlot2Seconds > 0 && DateTime.UtcNow > _lastOverloadHighSlot2.AddSeconds(_deactivateOverloadHighSlot2Seconds))
                    {
                        attemptOverloadHighSlot2 = false;
                        Log.WriteLine("Defense: Deactivating Overload High Slot 2 [ CmdOverloadHighPowerSlot2 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot2);
                    }

                if (attemptOverloadHighSlot3 && overloadHighSlot3Completed)
                    if (_deactivateOverloadHighSlot3Seconds > 0 && DateTime.UtcNow > _lastOverloadHighSlot3.AddSeconds(_deactivateOverloadHighSlot3Seconds))
                    {
                        attemptOverloadHighSlot3 = false;
                        Log.WriteLine("Defense: Deactivating Overload High Slot 3 [ CmdOverloadHighPowerSlot3 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot3);
                    }

                if (attemptOverloadHighSlot4 && overloadHighSlot4Completed)
                    if (_deactivateOverloadHighSlot4Seconds > 0 && DateTime.UtcNow > _lastOverloadHighSlot4.AddSeconds(_deactivateOverloadHighSlot4Seconds))
                    {
                        attemptOverloadHighSlot4 = false;
                        Log.WriteLine("Defense: Deactivating Overload High Slot 4 [ CmdOverloadHighPowerSlot4 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot4);
                    }

                if (attemptOverloadHighSlot5 && overloadHighSlot5Completed)
                    if (_deactivateOverloadHighSlot5Seconds > 0 && DateTime.UtcNow > _lastOverloadHighSlot5.AddSeconds(_deactivateOverloadHighSlot5Seconds))
                    {
                        attemptOverloadHighSlot5 = false;
                        Log.WriteLine("Defense: Deactivating Overload High Slot 5 [ CmdOverloadHighPowerSlot5 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot5);
                    }

                if (attemptOverloadHighSlot6 && overloadHighSlot6Completed)
                    if (_deactivateOverloadHighSlot6Seconds > 0 && DateTime.UtcNow > _lastOverloadHighSlot6.AddSeconds(_deactivateOverloadHighSlot6Seconds))
                    {
                        attemptOverloadHighSlot6 = false;
                        Log.WriteLine("Defense: Deactivating Overload High Slot 6 [ CmdOverloadHighPowerSlot6 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot6);
                    }

                if (attemptOverloadHighSlot7 && overloadHighSlot7Completed)
                    if (_deactivateOverloadHighSlot7Seconds > 0 && DateTime.UtcNow > _lastOverloadHighSlot7.AddSeconds(_deactivateOverloadHighSlot7Seconds))
                    {
                        attemptOverloadHighSlot7 = false;
                        Log.WriteLine("Defense: Deactivating Overload High Slot 7 [ CmdOverloadHighPowerSlot7 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot7);
                    }

                if (attemptOverloadHighSlot8 && overloadHighSlot8Completed)
                    if (_deactivateOverloadHighSlot8Seconds > 0 && DateTime.UtcNow > _lastOverloadHighSlot8.AddSeconds(_deactivateOverloadHighSlot8Seconds))
                    {
                        attemptOverloadHighSlot8 = false;
                        Log.WriteLine("Defense: Deactivating Overload High Slot 8 [ CmdOverloadHighPowerSlot8 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerSlot8);
                    }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void OverloadIndividualLowSlots()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(60))
                    return;

                //
                // Activate Rack Overload
                //
                if (ESCache.Instance.InMission && Combat.Combat.PotentialCombatTargets.Count > 0 && State.CurrentCombatMissionCtrlState == ActionControlState.ExecutePocketActions)
                {
                    if (attemptOverloadLowSlot1 && !overloadLowSlot1Completed && !overloadRackLowCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot1))
                        {
                            _lastOverloadLowSlot1 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Low Slot 1 [ CmdOverloadLowPowerSlot1 ]");
                            overloadLowSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadLowSlot2 && !overloadLowSlot2Completed && !overloadRackLowCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot2))
                        {
                            _lastOverloadLowSlot2 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Low Slot 2 [ CmdOverloadLowPowerSlot2 ]");
                            overloadLowSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadLowSlot3 && !overloadLowSlot3Completed && !overloadRackLowCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot3))
                        {
                            _lastOverloadLowSlot3 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Low Slot 3 [ CmdOverloadLowPowerSlot3 ]");
                            overloadLowSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadLowSlot4 && !overloadLowSlot4Completed && !overloadRackLowCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot4))
                        {
                            _lastOverloadLowSlot4 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Low Slot 4 [ CmdOverloadLowPowerSlot4 ]");
                            overloadLowSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadLowSlot5 && !overloadLowSlot5Completed && !overloadRackLowCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot5))
                        {
                            _lastOverloadLowSlot5 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Low Slot 5 [ CmdOverloadLowPowerSlot5 ]");
                            overloadLowSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadLowSlot6 && !overloadLowSlot6Completed && !overloadRackLowCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot6))
                        {
                            _lastOverloadLowSlot6 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Low Slot 6 [ CmdOverloadLowPowerSlot6 ]");
                            overloadLowSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadLowSlot7 && !overloadLowSlot7Completed && !overloadRackLowCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot7))
                        {
                            _lastOverloadLowSlot7 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Low Slot 7 [ CmdOverloadLowPowerSlot7 ]");
                            overloadLowSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadLowSlot8 && !overloadLowSlot8Completed && !overloadRackLowCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot8))
                        {
                            _lastOverloadLowSlot8 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Low Slot 8 [ CmdOverloadLowPowerSlot8 ]");
                            overloadLowSlot1Completed = true;
                            return;
                        }
                }

                //
                // Deactivate Rack Overload (why? damage a module in that rack?)
                //

                if (attemptOverloadLowSlot1 && overloadLowSlot1Completed)
                    if (_deactivateOverloadLowSlot1Seconds > 0 && DateTime.UtcNow > _lastOverloadLowSlot1.AddSeconds(_deactivateOverloadLowSlot1Seconds))
                    {
                        attemptOverloadLowSlot1 = false;
                        Log.WriteLine("Defense: Deactivating Overload Low Slot 1 [ CmdOverloadLowPowerSlot1 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot1);
                    }

                if (attemptOverloadLowSlot2 && overloadLowSlot2Completed)
                    if (_deactivateOverloadLowSlot2Seconds > 0 && DateTime.UtcNow > _lastOverloadLowSlot2.AddSeconds(_deactivateOverloadLowSlot2Seconds))
                    {
                        attemptOverloadLowSlot2 = false;
                        Log.WriteLine("Defense: Deactivating Overload Low Slot 2 [ CmdOverloadLowPowerSlot2 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot2);
                    }

                if (attemptOverloadLowSlot3 && overloadLowSlot3Completed)
                    if (_deactivateOverloadLowSlot3Seconds > 0 && DateTime.UtcNow > _lastOverloadLowSlot3.AddSeconds(_deactivateOverloadLowSlot3Seconds))
                    {
                        attemptOverloadLowSlot3 = false;
                        Log.WriteLine("Defense: Deactivating Overload Low Slot 3 [ CmdOverloadLowPowerSlot3 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot3);
                    }

                if (attemptOverloadLowSlot4 && overloadLowSlot4Completed)
                    if (_deactivateOverloadLowSlot4Seconds > 0 && DateTime.UtcNow > _lastOverloadLowSlot4.AddSeconds(_deactivateOverloadLowSlot4Seconds))
                    {
                        attemptOverloadLowSlot4 = false;
                        Log.WriteLine("Defense: Deactivating Overload Low Slot 4 [ CmdOverloadLowPowerSlot4 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot4);
                    }

                if (attemptOverloadLowSlot5 && overloadLowSlot5Completed)
                    if (_deactivateOverloadLowSlot5Seconds > 0 && DateTime.UtcNow > _lastOverloadLowSlot5.AddSeconds(_deactivateOverloadLowSlot5Seconds))
                    {
                        attemptOverloadLowSlot5 = false;
                        Log.WriteLine("Defense: Deactivating Overload Low Slot 5 [ CmdOverloadLowPowerSlot5 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot5);
                    }

                if (attemptOverloadLowSlot6 && overloadLowSlot6Completed)
                    if (_deactivateOverloadLowSlot6Seconds > 0 && DateTime.UtcNow > _lastOverloadLowSlot6.AddSeconds(_deactivateOverloadLowSlot6Seconds))
                    {
                        attemptOverloadLowSlot6 = false;
                        Log.WriteLine("Defense: Deactivating Overload Low Slot 6 [ CmdOverloadLowPowerSlot6 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot6);
                    }

                if (attemptOverloadLowSlot7 && overloadLowSlot7Completed)
                    if (_deactivateOverloadLowSlot7Seconds > 0 && DateTime.UtcNow > _lastOverloadLowSlot7.AddSeconds(_deactivateOverloadLowSlot7Seconds))
                    {
                        attemptOverloadLowSlot7 = false;
                        Log.WriteLine("Defense: Deactivating Overload Low Slot 7 [ CmdOverloadLowPowerSlot7 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot7);
                    }

                if (attemptOverloadLowSlot8 && overloadLowSlot8Completed)
                    if (_deactivateOverloadLowSlot8Seconds > 0 && DateTime.UtcNow > _lastOverloadLowSlot8.AddSeconds(_deactivateOverloadLowSlot8Seconds))
                    {
                        attemptOverloadLowSlot8 = false;
                        Log.WriteLine("Defense: Deactivating Overload Low Slot 8 [ CmdOverloadLowPowerSlot8 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerSlot8);
                    }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void OverloadIndividualMidSlots()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(60))
                    return;

                //
                // Activate Rack Overload
                //
                if (ESCache.Instance.InMission && Combat.Combat.PotentialCombatTargets.Count > 0 && State.CurrentCombatMissionCtrlState == ActionControlState.ExecutePocketActions)
                {
                    if (attemptOverloadMidSlot1 && !overloadMidSlot1Completed && !overloadRackMediumCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot1))
                        {
                            _lastOverloadMidSlot1 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Mid Slot 1 [ CmdOverloadMediumPowerSlot1 ]");
                            overloadMidSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadMidSlot2 && !overloadMidSlot2Completed && !overloadRackMediumCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot2))
                        {
                            _lastOverloadMidSlot2 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Mid Slot 2 [ CmdOverloadMediumPowerSlot2 ]");
                            overloadMidSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadMidSlot3 && !overloadMidSlot3Completed && !overloadRackMediumCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot3))
                        {
                            _lastOverloadMidSlot3 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Mid Slot 3 [ CmdOverloadMediumPowerSlot3 ]");
                            overloadMidSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadMidSlot4 && !overloadMidSlot4Completed && !overloadRackMediumCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot4))
                        {
                            _lastOverloadMidSlot4 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Mid Slot 4 [ CmdOverloadMediumPowerSlot4 ]");
                            overloadMidSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadMidSlot5 && !overloadMidSlot5Completed && !overloadRackMediumCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot5))
                        {
                            _lastOverloadMidSlot5 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Mid Slot 5 [ CmdOverloadMediumPowerSlot5 ]");
                            overloadMidSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadMidSlot6 && !overloadMidSlot6Completed && !overloadRackMediumCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot6))
                        {
                            _lastOverloadMidSlot6 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Mid Slot 6 [ CmdOverloadMediumPowerSlot6 ]");
                            overloadMidSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadMidSlot7 && !overloadMidSlot7Completed && !overloadRackMediumCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot7))
                        {
                            _lastOverloadMidSlot7 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Mid Slot 7 [ CmdOverloadMediumPowerSlot7 ]");
                            overloadMidSlot1Completed = true;
                            return;
                        }

                    if (attemptOverloadMidSlot8 && !overloadMidSlot8Completed && !overloadRackMediumCompleted)
                        if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot8))
                        {
                            _lastOverloadMidSlot8 = DateTime.UtcNow;
                            Log.WriteLine("Defense: Overloading Mid Slot 8 [ CmdOverloadMediumPowerSlot8 ]");
                            overloadMidSlot1Completed = true;
                            return;
                        }
                }

                //
                // Deactivate Rack Overload (why? damage a module in that rack?)
                //

                if (attemptOverloadMidSlot1 && overloadMidSlot1Completed)
                    if (_deactivateOverloadMidSlot1Seconds > 0 && DateTime.UtcNow > _lastOverloadMidSlot1.AddSeconds(_deactivateOverloadMidSlot1Seconds))
                    {
                        attemptOverloadMidSlot1 = false;
                        Log.WriteLine("Defense: Deactivating Overload Mid Slot 1 [ CmdOverloadMidPowerSlot1 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot1);
                    }

                if (attemptOverloadMidSlot2 && overloadMidSlot2Completed)
                    if (_deactivateOverloadMidSlot2Seconds > 0 && DateTime.UtcNow > _lastOverloadMidSlot2.AddSeconds(_deactivateOverloadMidSlot2Seconds))
                    {
                        attemptOverloadMidSlot2 = false;
                        Log.WriteLine("Defense: Deactivating Overload Mid Slot 2 [ CmdOverloadMidPowerSlot2 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot2);
                    }

                if (attemptOverloadMidSlot3 && overloadMidSlot3Completed)
                    if (_deactivateOverloadMidSlot3Seconds > 0 && DateTime.UtcNow > _lastOverloadMidSlot3.AddSeconds(_deactivateOverloadMidSlot3Seconds))
                    {
                        attemptOverloadMidSlot3 = false;
                        Log.WriteLine("Defense: Deactivating Overload Mid Slot 3 [ CmdOverloadMidPowerSlot3 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot3);
                    }

                if (attemptOverloadMidSlot4 && overloadMidSlot4Completed)
                    if (_deactivateOverloadMidSlot4Seconds > 0 && DateTime.UtcNow > _lastOverloadMidSlot4.AddSeconds(_deactivateOverloadMidSlot4Seconds))
                    {
                        attemptOverloadMidSlot4 = false;
                        Log.WriteLine("Defense: Deactivating Overload Mid Slot 4 [ CmdOverloadMidPowerSlot4 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot4);
                    }

                if (attemptOverloadMidSlot5 && overloadMidSlot5Completed)
                    if (_deactivateOverloadMidSlot5Seconds > 0 && DateTime.UtcNow > _lastOverloadMidSlot5.AddSeconds(_deactivateOverloadMidSlot5Seconds))
                    {
                        attemptOverloadMidSlot5 = false;
                        Log.WriteLine("Defense: Deactivating Overload Mid Slot 5 [ CmdOverloadMidPowerSlot5 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot5);
                    }

                if (attemptOverloadMidSlot6 && overloadMidSlot6Completed)
                    if (_deactivateOverloadMidSlot6Seconds > 0 && DateTime.UtcNow > _lastOverloadMidSlot6.AddSeconds(_deactivateOverloadMidSlot6Seconds))
                    {
                        attemptOverloadMidSlot6 = false;
                        Log.WriteLine("Defense: Deactivating Overload Mid Slot 6 [ CmdOverloadMidPowerSlot6 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot6);
                    }

                if (attemptOverloadMidSlot7 && overloadMidSlot7Completed)
                    if (_deactivateOverloadMidSlot7Seconds > 0 && DateTime.UtcNow > _lastOverloadMidSlot7.AddSeconds(_deactivateOverloadMidSlot7Seconds))
                    {
                        attemptOverloadMidSlot7 = false;
                        Log.WriteLine("Defense: Deactivating Overload Mid Slot 7 [ CmdOverloadMidPowerSlot7 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot7);
                    }

                if (attemptOverloadMidSlot8 && overloadMidSlot8Completed)
                    if (_deactivateOverloadMidSlot8Seconds > 0 && DateTime.UtcNow > _lastOverloadMidSlot8.AddSeconds(_deactivateOverloadMidSlot8Seconds))
                    {
                        attemptOverloadMidSlot8 = false;
                        Log.WriteLine("Defense: Deactivating Overload Mid Slot 8 [ CmdOverloadMidPowerSlot8 ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerSlot8);
                    }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void OverloadRacks()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(60))
                    return;

                //
                // Activate Rack Overload
                //
                if (attemptOverloadRackHigh && !overloadRackHighCompleted)
                {
                    Log.WriteLine("Defense: if (attemptOverloadRackHigh && !overloadRackHighCompleted)");
                    bool? readyToOverloadRackHigh = null;
                    foreach (ModuleCache module in ESCache.Instance.Modules.Where(i => i.IsHighSlotModule))
                        //
                        // reasons to not overload this rack of modules...
                        //
                        // module.Damage vs module.Hp
                        // modules have to be not yet overloaded, if they are already overloaded do not toggle overload off here!
                        //
                        readyToOverloadRackHigh = true;

                    if (readyToOverloadRackHigh != null && (bool) readyToOverloadRackHigh)
                    {
                        Log.WriteLine("Defense: Overloading Rack of High Slots [ CmdOverloadHighPowerRack ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadHighPowerRack);
                        overloadRackHighCompleted = true;
                        return;
                    }
                }

                if (attemptOverloadRackMedium && !overloadRackMediumCompleted)
                {
                    Log.WriteLine("Defense: if (attemptOverloadRackMedium && !overloadRackMediumCompleted)");
                    bool? readyToOverloadRackMedium = null;
                    foreach (ModuleCache module in ESCache.Instance.Modules.Where(i => i.IsMidSlotModule))
                        //
                        // reasons to not overload this rack of modules...
                        //
                        // module.Damage vs module.Hp
                        // modules have to be not yet overloaded, if they are already overloaded do not toggle overload off here!
                        //
                        readyToOverloadRackMedium = true;

                    if (readyToOverloadRackMedium != null && (bool) readyToOverloadRackMedium)
                    {
                        Log.WriteLine("Defense: Overloading Rack of Mid Slots [ CmdOverloadMediumPowerRack ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadMediumPowerRack);
                        overloadRackMediumCompleted = true;
                        return;
                    }
                }

                if (attemptOverloadRackLow && !overloadRackLowCompleted)
                {
                    Log.WriteLine("Defense: if (attemptOverloadRackLow && !overloadRackLowCompleted)");
                    bool? readyToOverloadRackLow = null;
                    foreach (ModuleCache module in ESCache.Instance.Modules.Where(i => i.IsMidSlotModule))
                        //
                        // reasons to not overload this rack of modules...
                        //
                        // module.Damage vs module.Hp
                        // modules have to be not yet overloaded, if they are already overloaded do not toggle overload off here!
                        //
                        readyToOverloadRackLow = true;

                    if (readyToOverloadRackLow != null && (bool) readyToOverloadRackLow)
                    {
                        Log.WriteLine("Defense: Overloading Rack of Mid Slots [ CmdOverloadLowPowerRack ]");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdOverloadLowPowerRack);
                        overloadRackLowCompleted = true;
                    }
                }

                //
                // Deactivate Rack Overload (why? damage a module in that rack?)
                //
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool OverloadedModuleNoLongerNeedsOverload(ModuleCache overloadedModule)
        {
            if (overloadedModule.DamagePercent > GlobalRepsOverloadDamageAllowed)
            {
                if (DebugConfig.DebugOverLoadReps) Log.WriteLine("if (overloadedModule.DamagePercent > GlobalRepsOverloadDamageAllowed)");
                return true;
            }

            if (overloadedModule.IsShieldRepairModule && ESCache.Instance.ActiveShip.ShieldPercentage > ToggleOffOverloadRepairModulesAtThisPerc)
            {
                if (DebugConfig.DebugOverLoadReps) Log.WriteLine("if (overloadedModule.IsShieldRepairModule && ESCache.Instance.ActiveShip.ShieldPercentage > ToggleOffOverloadRepairModulesAtThisPerc [" + +ToggleOffOverloadRepairModulesAtThisPerc + "])");
                return true;
            }

            if (overloadedModule.IsArmorRepairModule && ESCache.Instance.ActiveShip.ArmorPercentage > ToggleOffOverloadRepairModulesAtThisPerc)
            {
                if (DebugConfig.DebugOverLoadReps) Log.WriteLine("if (overloadedModule.IsArmorRepairModule && ESCache.Instance.ActiveShip.ArmorPercentage > ToggleOffOverloadRepairModulesAtThisPerc [" + ToggleOffOverloadRepairModulesAtThisPerc + "])");
                return true;
            }

            if (overloadedModule.IsShieldHardener && ESCache.Instance.ActiveShip.ShieldPercentage > ToggleOffOverloadHardenerModulesAtThisPerc)
            {
                if (DebugConfig.DebugOverLoadReps) Log.WriteLine("if (overloadedModule.IsShieldHardener && ESCache.Instance.ActiveShip.ShieldPercentage > ToggleOffOverloadRepairModulesAtThisPerc [" + +ToggleOffOverloadRepairModulesAtThisPerc + "])");
                return true;
            }

            if (overloadedModule.IsArmorHardener && ESCache.Instance.ActiveShip.ArmorPercentage > ToggleOffOverloadHardenerModulesAtThisPerc)
            {
                if (DebugConfig.DebugOverLoadReps) Log.WriteLine("if (overloadedModule.IsArmorHardener && ESCache.Instance.ActiveShip.ArmorPercentage > ToggleOffOverloadRepairModulesAtThisPerc [" + +ToggleOffOverloadRepairModulesAtThisPerc + "])");
                return true;
            }

            return false;
        }

        private static bool IsSafeToOverloadRepairModules
        {
            get
            {
                //
                // use shield to determine when to overload
                //
                if (ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule))
                {
                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsShieldHardener))");
                    if (ESCache.Instance.ActiveShip.ShieldPercentage > OverloadRepairModulesAtThisPerc)
                    {
                        if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.ActiveShip.ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] > OverloadRepairModulesAtThisPerc [" + OverloadRepairModulesAtThisPerc + "])");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.ShieldPercentage + ESCache.Instance.ActiveShip.ArmorPercentage + ESCache.Instance.ActiveShip.StructurePercentage == 0)
                    {
                        if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.ActiveShip.TotalHealth == 0)");
                        return false;
                    }

                    if (ESCache.Instance.Modules.Any(i => i.DamagePercent > GlobalRepsOverloadDamageAllowed))
                    {
                        if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.Modules.All(i => !i.IsActive))");
                        return false;
                    }

                    if (ESCache.Instance.Modules.All(i => !i.IsActive))
                    {
                        if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.Modules.All(i => !i.IsActive))");
                        return false;
                    }

                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("IsSafeToOverload: Shields: return true");
                    return true;
                }

                //
                // use armor to determine when to overload
                //

                if (ESCache.Instance.Modules.Any(i => i.IsArmorRepairModule))
                {
                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.Modules.Any(i => i.IsArmorRepairModule || i.IsArmorHardener))");
                    if (ESCache.Instance.ActiveShip.ArmorPercentage > OverloadRepairModulesAtThisPerc)
                    {
                        if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.ActiveShip.ArmorPercentage [" + ESCache.Instance.ActiveShip.ArmorPercentage + "] > OverloadRepairModulesAtThisPerc [" + OverloadRepairModulesAtThisPerc + "])");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.ShieldPercentage + ESCache.Instance.ActiveShip.ArmorPercentage + ESCache.Instance.ActiveShip.StructurePercentage == 0)
                    {
                        if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.ActiveShip.TotalHealth == 0)");
                        return false;
                    }

                    if (ESCache.Instance.Modules.All(i => !i.IsActive))
                    {
                        if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.Modules.All(i => !i.IsActive))");
                        return false;
                    }

                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("IsSafeToOverload: Armor: return true");
                    return true;
                }

                if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("!if (ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsShieldHardener))");
                return false;
            }
        }

        private static void OverloadHardeners()
        {
            try
            {
                try
                {
                    if (!GlobalAllowOverLoadOfHardeners || (ESCache.Instance.ActiveShip.IsShieldTanked && 35 > ESCache.Instance.ActiveShip.ShieldPercentage))
                        return;

                    List<ModuleCache> modulesToTryToOverload = ESCache.Instance.Modules.Where(i => i.IsShieldHardener || i.IsArmorHardener).ToList();
                    //
                    // for Anomic Team missions do not overheat until after we are out of warp for x seconds.
                    // this allows us to settle in to shooting the correct target before overheating
                    //
                    if (ESCache.Instance.InMission && MissionSettings.MyMission != null && MissionSettings.MyMission.Name.Contains("Anomic Team"))
                        if (Time.Instance.LastInWarp.AddSeconds(45) > DateTime.UtcNow)
                        {
                            if (DebugConfig.DebugOverLoadHardeners) Log.WriteLine("DebugOverLoadhardeners: We are doing an Anomic Team mission and are within 60 sec of dropping out of warp. Do not yet overload.");
                            return;
                        }

                    //
                    // DeActivate Overload as needed
                    //
                    int _repNumber = 0;
                    foreach (ModuleCache overloadedModule in modulesToTryToOverload.Where(i => i.UnOverloadDesirable && i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading))
                    {
                        _repNumber++;

                        try
                        {
                            if (overloadedModule.ToggleOverload())
                            {
                                Log.WriteLine("OverLoadhardeners: deactivate overload: [" + overloadedModule.TypeName + "][" + _repNumber + "] has [" + overloadedModule.DamagePercent + "%] damage: deactivating overload on hardener");
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        return;
                    }

                    //
                    // Activate Overload as needed
                    //
                    _repNumber = 0;
                    foreach (ModuleCache moduleToOverload in modulesToTryToOverload.Where(i => i.OverloadDesirable && !i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading && !i._module.IsBeingRepaired))
                    {
                        _repNumber++;

                        Log.WriteLine("OverLoadhardeners: Overload?:  OverloadDesirable [" + moduleToOverload.OverloadDesirable + "] IsOverloaded [" + moduleToOverload.IsOverloaded + "] IsPendingStopOverloading [" + moduleToOverload.IsPendingStopOverloading + "] IsPendingOverloading [" + moduleToOverload.IsPendingOverloading + "]");

                        try
                        {
                            if (moduleToOverload.ToggleOverload())
                            {
                                Log.WriteLine("OverLoadhardeners: Activating Overload on [" + moduleToOverload.TypeName + "] Module Damage [" + Math.Round(moduleToOverload.DamagePercent, 0) + "] Shield% [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "] Armor% [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "] Cap% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "] hardener");
                                ESCache.Instance.NeedRepair = true;
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        return;

                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }
        private static void OverloadReps()
        {
            try
            {
                try
                {
                    if (!AllowOverLoadOfReps)
                        return;

                    List<ModuleCache> modulesToTryToOverload = ESCache.Instance.Modules.Where(i => i.IsArmorRepairModule || i.IsShieldRepairModule).ToList();
                    // for Anomic Team missions do not overheat until after we are out of warp for x seconds.
                    // this allows us to settle in to shooting the correct target before overheating
                    //
                    if (ESCache.Instance.InMission && MissionSettings.MyMission != null && MissionSettings.MyMission.Name.Contains("Anomic Team"))
                        if (Time.Instance.LastInWarp.AddSeconds(45) > DateTime.UtcNow)
                        {
                            if (DebugConfig.DebugOverLoadReps) Log.WriteLine("DebugOverLoadReps: We are doing an Anomic Team mission and are within 60 sec of dropping out of warp. Do not yet overload.");
                            return;
                        }

                    //
                    // DeActivate Overload as needed
                    //
                    int _repNumber = 0;
                    foreach (ModuleCache overloadedModule in modulesToTryToOverload.Where(i => i.UnOverloadDesirable && i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading))
                    {
                        _repNumber++;

                        try
                        {
                            if (overloadedModule.ToggleOverload())
                            {
                                Log.WriteLine("OverLoadReps: deactivate overload: [" + overloadedModule.TypeName + "][" + _repNumber + "] has [" + overloadedModule.DamagePercent + "%] damage: deactivating overload on rep");
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        return;
                    }

                    //
                    // Activate Overload as needed
                    //
                    _repNumber = 0;
                    foreach (ModuleCache moduleToOverload in modulesToTryToOverload.Where(i => i.OverloadDesirable && !i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading && !i._module.IsBeingRepaired))
                    {
                        _repNumber++;

                        Log.WriteLine("OverLoadReps: OverloadDesirable [" + moduleToOverload.OverloadDesirable + "] IsOverloaded [" + moduleToOverload.IsOverloaded + "] IsPendingStopOverloading [" + moduleToOverload.IsPendingStopOverloading + "] IsPendingOverloading [" + moduleToOverload.IsPendingOverloading + "]");

                        try
                        {
                            if (moduleToOverload.ToggleOverload())
                            {
                                Log.WriteLine("OverLoadReps: Activating Overload on [" + moduleToOverload.TypeName + "] Module Damage [" + moduleToOverload.DamagePercent + "] Shield% [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] Armor% [" + ESCache.Instance.ActiveShip.ArmorPercentage + "] Cap% [" + ESCache.Instance.ActiveShip.CapacitorPercentage + "] rep module");
                                ESCache.Instance.NeedRepair = true;

                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void OverloadSpeedMod()
        {
            try
            {
                try
                {
                    if (!AllowOverLoadOfSpeedMod)
                        return;

                    List<ModuleCache> modulesToTryToOverload = ESCache.Instance.Modules.Where(i => i._module.IsMicroWarpDrive || i._module.IsAfterburner).ToList();

                    //
                    // DeActivate Overload as needed
                    //
                    int _repNumber = 0;
                    foreach (ModuleCache overloadedModule in modulesToTryToOverload.Where(i => i.UnOverloadDesirable && i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading))
                    {
                        _repNumber++;

                        try
                        {
                            if (overloadedModule.ToggleOverload())
                            {
                                Log.WriteLine("OverLoadSpedMod: deactivate overload: [" + overloadedModule.TypeName + "][" + _repNumber + "] has [" + overloadedModule.DamagePercent + "%] damage: deactivating overload on rep");
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        return;
                    }

                    //
                    // Activate Overload as needed
                    //
                    _repNumber = 0;
                    foreach (ModuleCache moduleToOverload in modulesToTryToOverload.Where(i => i.OverloadDesirable && !i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading && !i._module.IsBeingRepaired))
                    {
                        _repNumber++;

                        Log.WriteLine("OverloadSpeedMod: OverloadDesirable [" + moduleToOverload.OverloadDesirable + "] IsOverloaded [" + moduleToOverload.IsOverloaded + "] IsPendingStopOverloading [" + moduleToOverload.IsPendingStopOverloading + "] IsPendingOverloading [" + moduleToOverload.IsPendingOverloading + "]");

                        try
                        {
                            if (moduleToOverload.ToggleOverload())
                            {
                                Log.WriteLine("OverloadSpeedMod: Activating Overload on [" + moduleToOverload.TypeName + "] Module Damage [" + moduleToOverload.DamagePercent + "] Shield% [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] Armor% [" + ESCache.Instance.ActiveShip.ArmorPercentage + "] Cap% [" + ESCache.Instance.ActiveShip.CapacitorPercentage + "] rep module");
                                ESCache.Instance.NeedRepair = true;
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }


        private static void RecordDefenseStatistics()
        {
            try
            {
                if (ESCache.Instance.ActiveShip.ShieldPercentage < Statistics.LowestShieldPercentageThisPocket)
                {
                    Statistics.LowestShieldPercentageThisPocket = ESCache.Instance.ActiveShip.ShieldPercentage;
                    Statistics.LowestShieldPercentageThisMission = ESCache.Instance.ActiveShip.ShieldPercentage;
                }

                if (ESCache.Instance.ActiveShip.ArmorPercentage < Statistics.LowestArmorPercentageThisPocket)
                {
                    Statistics.LowestArmorPercentageThisPocket = ESCache.Instance.ActiveShip.ArmorPercentage;
                    Statistics.LowestArmorPercentageThisMission = ESCache.Instance.ActiveShip.ArmorPercentage;
                }

                if (ESCache.Instance.ActiveShip.CapacitorPercentage < Statistics.LowestCapacitorPercentageThisPocket)
                {
                    Statistics.LowestCapacitorPercentageThisPocket = ESCache.Instance.ActiveShip.CapacitorPercentage;
                    Statistics.LowestCapacitorPercentageThisMission = ESCache.Instance.ActiveShip.CapacitorPercentage;
                }

                try
                {
                    if (ESCache.Instance.InSpace && !ESCache.Instance.NeedRepair)
                    {
                        if (ESCache.Instance.ActiveShip != null)
                        {
                            if (ESCache.Instance.ActiveShip.ArmorPercentage < 100)
                            {
                                ESCache.Instance.NeedRepair = true;
                            }

                            if (ESCache.Instance.ActiveShip.StructurePercentage < 100)
                            {
                                ESCache.Instance.NeedRepair = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }
}