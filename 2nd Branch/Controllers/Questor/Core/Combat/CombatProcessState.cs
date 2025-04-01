extern alias SC;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Utility;
using SC::SharedComponents.Events;
using static EVESharpCore.Lookup.AbyssalSpawn;

namespace EVESharpCore.Questor.Combat
{
    public static partial class Combat
    {
        #region Fields

        private static EntityCache _killTarget;

        public static EntityCache KillTarget
        {
            get
            {
                try
                {
                    if (_killTarget == null)
                    {
                        _killTarget = PickPrimaryWeaponTarget();
                    }

                    return _killTarget;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        #endregion Fields

        #region Methods

        private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetAbyssalDeadSpace;
        private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetTriglavianInvasion;

        private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetWspaceDreadnaught;
        private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetWspace;
        private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard;
        private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank;
        private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetBasedOnTargetsForABattleship;
        private static IOrderedEnumerable<EntityCache> _pickWhatToLockNextBasedOnTargetsForABattleship;
        private static IOrderedEnumerable<EntityCache> _pickWhatToLockNextBasedOnTargetsForLowValueTargets;
        private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetBasedOnTargetsForAFrigate;
        private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetBasedOnTargetsForBurners;
        //private static IOrderedEnumerable<EntityCache> _pickPrimaryWeaponTargetFleetAbyssalDeadSpace;

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetWSpaceDreadnaught_BigGuns
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetWspaceDreadnaught == null)
                    {
                        _pickPrimaryWeaponTargetWspaceDreadnaught = Combat.PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                            .OrderByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable)
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            //.ThenByDescending(j => j.IsDecloakedTransmissionRelay)
                            .ThenByDescending(j => j.Name.Contains("Arithmos Tyrannos"))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattleship && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.IsInOptimalRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.IsTrackable && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsBattlecruiser && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.IsInOptimalRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.IsTrackable && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsPlayer && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange)
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

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetWspaceDreadnaught);

                        return _pickPrimaryWeaponTargetWspaceDreadnaught;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetWspaceDreadnaught;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetWSpaceDreadnaught_HighAngle
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetWspaceDreadnaught == null)
                    {
                        _pickPrimaryWeaponTargetWspaceDreadnaught = Combat.PotentialCombatTargets.Where(i => i.IsReadyToShoot && !i.IsNpcCapitalEscalation)
                            .OrderByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
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
                            .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange)
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

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetWspaceDreadnaught);

                        return _pickPrimaryWeaponTargetWspaceDreadnaught;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetWspaceDreadnaught;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetWSpaceMarauder
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetWspaceDreadnaught == null)
                    {
                        _pickPrimaryWeaponTargetWspaceDreadnaught = Combat.PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                            .OrderByDescending(j => j.IsPlayer && j.IsBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
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
                            .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable)
                            .ThenByDescending(j => j.IsNPCCruiser && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable)
                            .ThenByDescending(j => j.IsNPCBattleship && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship)
                            .ThenByDescending(j => j.IsNPCCruiser)
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable)
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNpcCapitalEscalation)
                            .ThenByDescending(j => j.Name.Contains("Arithmos Tyrannos"))
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

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetWspaceDreadnaught);

                        return _pickPrimaryWeaponTargetWspaceDreadnaught;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetWspaceDreadnaught;
            }
        }



        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetWSpace
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetWspace == null)
                    {
                        _pickPrimaryWeaponTargetWspace = Combat.PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                            .OrderByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.IsInOptimalRange && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.IsTrackable && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && j.HealthPct < 100 && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.IsNpcCapitalEscalation && ESCache.Instance.Modules.All(module => !module.IsHighAngleWeapon))
                            .ThenByDescending(j => j.Name.Contains("Arithmos Tyrannos"))
                            .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsSentry && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTrackable)
                            .ThenByDescending(j => j.IsNPCBattleship && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCBattleship)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsTrackable)
                            .ThenByDescending(j => j.IsNPCCruiser && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCCruiser)
                            .ThenByDescending(j => j.IsNPCFrigate && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100 && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCFrigate && j.IsTrackable && j.IsInOptimalRange && j.IsInWebRange)
                            .ThenByDescending(j => j.IsNPCFrigate && j.IsTrackable && j.IsInOptimalRange && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCFrigate && j.IsTrackable && j.IsInOptimalRange)
                            .ThenByDescending(j => j.IsNPCFrigate && j.IsTrackable && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCFrigate && j.IsTrackable)
                            .ThenByDescending(j => j.IsNPCFrigate && j.HealthPct < 100)
                            .ThenByDescending(j => j.IsNPCFrigate)
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

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetWspace);

                        return _pickPrimaryWeaponTargetWspace;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetWspace;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetTriglavianInvasion
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetTriglavianInvasion == null)
                    {
                        _pickPrimaryWeaponTargetTriglavianInvasion = PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                            .OrderBy(j => j.IsNPCCapitalShip)
                            .ThenByDescending(j => !j.IsEntityEngagedByMyOtherToons)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && 100 > k.HealthPct && k.IsDroneKillTarget)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && 100 > k.HealthPct)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && !k.IsAttacking)
                            .ThenByDescending(k => k.IsNPCBattlecruiser)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsWithinOptimalOfDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsCloseToDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && !l.IsAttacking)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsWithinOptimalOfDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsCloseToDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire)
                            .ThenByDescending(l => l.IsNPCFrigate && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(l => l.IsNPCFrigate)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsMissileDisruptingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair && !l.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps && !j.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && !l.IsDroneKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser)
                            .ThenByDescending(j => j.IsNPCBattleship && 100 > j.HealthPct && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.WeShouldFocusFire && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsHighDps && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe && !j.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && !l.IsDroneKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(j => j.IsNPCBattleship && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship)
                            .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                            .ThenBy(p => p.StructurePct)
                            .ThenBy(q => q.ArmorPct)
                            .ThenBy(r => r.ShieldPct);

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetTriglavianInvasion);

                        return _pickPrimaryWeaponTargetTriglavianInvasion;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetTriglavianInvasion;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetTriglavianInvasionObservatory
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetTriglavianInvasion == null)
                    {
                        _pickPrimaryWeaponTargetTriglavianInvasion = PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                            .OrderBy(j => j.IsNPCCapitalShip)
                            .ThenByDescending(i => i.Name.ToLower().Contains("zirnitra".ToLower()))
                            .ThenByDescending(i => i.Name.ToLower().Contains("Revelation".ToLower()))
                            .ThenByDescending(i => i.Name.ToLower().Contains("Moros".ToLower()))
                            .ThenByDescending(i => i.Name.ToLower().Contains("Naglafar".ToLower()))
                            .ThenByDescending(i => i.Name.ToLower().Contains("Phoenix".ToLower()))
                            .ThenByDescending(i => i.Name.ToLower().Contains("Triglavian Stellar Accelerator".ToLower()))
                            .ThenByDescending(j => !j.IsEntityEngagedByMyOtherToons)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && 100 > k.HealthPct && k.IsDroneKillTarget)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && 100 > k.HealthPct)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && !k.IsAttacking)
                            .ThenByDescending(k => k.IsNPCBattlecruiser)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsWithinOptimalOfDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsCloseToDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && !l.IsAttacking)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsWithinOptimalOfDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsCloseToDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire)
                            .ThenByDescending(l => l.IsNPCFrigate && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(l => l.IsNPCFrigate)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsMissileDisruptingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair && !l.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps && !j.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && !l.IsDroneKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser)
                            .ThenByDescending(j => j.IsNPCBattleship && 100 > j.HealthPct && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.WeShouldFocusFire && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsHighDps && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe && !j.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && !l.IsDroneKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(j => j.IsNPCBattleship && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship)
                            .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                            .ThenBy(p => p.StructurePct)
                            .ThenBy(q => q.ArmorPct)
                            .ThenBy(r => r.ShieldPct);

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetTriglavianInvasion);

                        return _pickPrimaryWeaponTargetTriglavianInvasion;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetTriglavianInvasion;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetAbyssalDeadSpace
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetAbyssalDeadSpace == null)
                    {
                        _pickPrimaryWeaponTargetAbyssalDeadSpace = PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                            .OrderByDescending(k => k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                            .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                            //.ThenByDescending(j => j.IsNPCBattleship && !Drones.DronesKillHighValueTargets && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                            //.ThenByDescending(j => j.IsNPCBattlecruiser && !Drones.DronesKillHighValueTargets && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && 100 > l.HealthPct && l.IsLastTargetDronesWereShooting) //kill things shooting drones!
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && 100 > l.HealthPct) //kill things shooting drones!
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && !l.NpcHasALotOfRemoteRepair) //kill things shooting drones!
                            .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                            .ThenByDescending(j => j.IsNPCCruiser && j.WeShouldFocusFire && 100 > j.HealthPct)
                            .ThenByDescending(j => j.IsNPCCruiser && j.WeShouldFocusFire && j.NpcHasRemoteRepair)
                            .ThenByDescending(j => j.IsNPCCruiser && j.WeShouldFocusFire)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && 100 > k.HealthPct && k.IsDroneKillTarget)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && 100 > k.HealthPct)
                            .ThenByDescending(k => k.IsNPCBattlecruiser)
                            .ThenByDescending(l => !l.WeShouldFocusFire && !l.IsDroneKillTarget)
                            //.ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                            .ThenByDescending(l => l.IsNPCFrigate && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsMissileDisruptingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                            .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                            .ThenByDescending(j => j.IsNPCFrigate && j.IsHighDps)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted()).ThenByDescending(o => o != Combat._killTarget && o.IsNPCFrigate)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsMissileDisruptingMe && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(l => l.IsNPCCruiser)
                            .ThenByDescending(i => !i.Name.ToLower().Contains("karybdis tyrannos".ToLower()))
                            .ThenByDescending(j => j.IsNPCBattleship && 100 > j.HealthPct)
                            .ThenByDescending(j => j.IsNPCBattleship && j.WeShouldFocusFire)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsHighDps)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(j => j.IsNPCBattleship)
                            .ThenByDescending(i => i.IsEntityIShouldKeepShooting);

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetAbyssalDeadSpace);

                        return _pickPrimaryWeaponTargetAbyssalDeadSpace;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetAbyssalDeadSpace;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard == null)
                    {
                        _pickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard = PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                            .OrderByDescending(k => k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsWithinOptimalOfDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsCloseToDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsWithinOptimalOfDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsCloseToDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire)
                            .ThenByDescending(l => l.IsNPCFrigate && 100 > l.HealthPct && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsMissileDisruptingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && !l.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCFrigate && j.IsHighDps && !j.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && !l.IsDroneKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted()).ThenByDescending(o => o != Combat._killTarget && o.IsNPCFrigate)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsMissileDisruptingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair && !l.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps && !j.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && !l.IsDroneKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser)
                            .ThenByDescending(j => j.IsNPCBattleship && 100 > j.HealthPct && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.WeShouldFocusFire && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsHighDps && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe && !j.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && !l.IsDroneKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(j => j.IsNPCBattleship && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship)
                            .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                            .ThenBy(p => p.StructurePct)
                            .ThenBy(q => q.ArmorPct)
                            .ThenBy(r => r.ShieldPct);

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard);

                        return _pickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetAbyssalDeadSpaceVortonProjector
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetAbyssalDeadSpace == null)
                    {
                        _pickPrimaryWeaponTargetAbyssalDeadSpace = PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                            .OrderByDescending(k => k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                            .ThenByDescending(i => !i.Name.ToLower().Contains("karybdis tyrannos".ToLower()))
                            .ThenByDescending(i => !i.Name.ToLower().Contains("hadal abyssal overmind".ToLower()))
                            .ThenByDescending(i => !i.Name.ToLower().Contains("lucid deepwatcher".ToLower()))
                            .ThenByDescending(i => !i.Name.ToLower().Contains("thunderchild disparu troop".ToLower()))
                            .ThenByDescending(i => !i.Name.ToLower().Contains("arrester marshal disparu troop".ToLower()))
                            .ThenByDescending(i => !i.Name.ToLower().Contains("drainer marshal disparu troop".ToLower()))
                            .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking && l.IsWebbingMe && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking && l.IsWarpScramblingMe && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking && l.IsWarpScramblingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe)
                            .ThenByDescending(i => i.IsNPCCruiser && i.IsWarpScramblingMe && !i.IsAttacking && Combat.PotentialCombatTargets.Any(x => x.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                            .ThenByDescending(i => i.IsNPCCruiser && i.IsWarpScramblingMe && Combat.PotentialCombatTargets.Any(x => x.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                            .ThenByDescending(k => k.IsNPCDestroyer && 100 > k.HealthPct && k.IsDroneKillTarget)
                            .ThenByDescending(k => k.IsNPCDestroyer && 100 > k.HealthPct)
                            .ThenByDescending(k => k.IsNPCDestroyer && !k.IsAttacking)
                            .ThenByDescending(k => k.IsNPCDestroyer)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && 100 > k.HealthPct && k.IsDroneKillTarget)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && 100 > k.HealthPct)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && !k.IsAttacking)
                            .ThenByDescending(k => k.IsNPCBattlecruiser)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsWithinOptimalOfDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire && l.IsCloseToDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && l.WeShouldFocusFire)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && !l.IsAttacking)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsWithinOptimalOfDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && l.IsCloseToDrones)
                            .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire)
                            .ThenByDescending(l => l.IsNPCFrigate && 100 > l.HealthPct)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(l => l.IsNPCFrigate)
                            .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsMissileDisruptingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair && !l.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps && !j.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && !l.IsDroneKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(l => l.IsNPCCruiser && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCCruiser)
                            .ThenByDescending(j => j.IsNPCBattleship && 100 > j.HealthPct && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.WeShouldFocusFire && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsHighDps && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe && !j.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && !l.IsDroneKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(j => j.IsNPCBattleship && !j.IsDroneKillTarget)
                            .ThenByDescending(j => j.IsNPCBattleship)
                            .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                            .ThenBy(p => p.StructurePct)
                            .ThenBy(q => q.ArmorPct)
                            .ThenBy(r => r.ShieldPct);

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetAbyssalDeadSpace);

                        return _pickPrimaryWeaponTargetAbyssalDeadSpace;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetAbyssalDeadSpace;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank
        {
            get
            {
                try
                {
                    if (_pickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank == null)
                    {
                        _pickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank = Combat.PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                            .OrderByDescending(l => !l.WeShouldFocusFire && !l.IsDroneKillTarget)
                            .ThenByDescending(l => l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                            .ThenByDescending(j => j.IsNPCBattlecruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                            .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                            .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                            .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                            .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                            .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                            .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600 && j.NpcHasRemoteRepair)
                            .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600)
                            .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800 && j.NpcHasRemoteRepair)
                            .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                            .ThenByDescending(l => l.IsNPCBattleship && l.StructurePct < .90)
                            .ThenByDescending(l => l.IsNPCBattleship && l.ArmorPct < .90)
                            .ThenByDescending(l => l.IsNPCBattleship && l.ShieldPct < .90)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                            .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsHighDps)
                            .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(j => j.IsNPCBattleship)
                            .ThenByDescending(l => l.IsNPCBattlecruiser && l.StructurePct < .90)
                            .ThenByDescending(l => l.IsNPCBattlecruiser && l.ArmorPct < .90)
                            .ThenByDescending(l => l.IsNPCBattlecruiser && l.ShieldPct < .90)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasRemoteRepair)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsSensorDampeningMe)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsWebbingMe)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasNeutralizers)
                            .ThenByDescending(j => j.IsNPCBattlecruiser && j.IsHighDps)
                            .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCBattlecruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(k => k.IsNPCBattlecruiser)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsMissileDisruptingMe)
                            .ThenByDescending(k => k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                            .ThenByDescending(l => l.IsNPCCruiser && l.StructurePct < .90 && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && l.ArmorPct < .90 && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && l.ShieldPct < .90 && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && l.StructurePct < .90)
                            .ThenByDescending(l => l.IsNPCCruiser && l.ArmorPct < .90)
                            .ThenByDescending(l => l.IsNPCCruiser && l.ShieldPct < .90)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                            .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                            .ThenByDescending(l => l.IsNPCCruiser)
                            .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                            .ThenByDescending(l => l.IsNPCFrigate && l.StructurePct < .90 && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.ArmorPct < .90 && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.ShieldPct < .90 && l.IsWebbingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.StructurePct < .90)
                            .ThenByDescending(l => l.IsNPCFrigate && l.ArmorPct < .90)
                            .ThenByDescending(l => l.IsNPCFrigate && l.ShieldPct < .90)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                            .ThenByDescending(j => j.IsNPCFrigate && j.IsHighDps)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                            .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted()).ThenByDescending(o => o != Combat._killTarget && o.IsNPCFrigate)
                            .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                            .ThenBy(p => p.StructurePct)
                            .ThenBy(q => q.ArmorPct)
                            .ThenBy(r => r.ShieldPct);

                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank);

                        return _pickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return _pickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetBasedOnTargetsForABattleship
        {
            get
            {
                if (_pickPrimaryWeaponTargetBasedOnTargetsForABattleship == null)
                {
                    if (DebugConfig.DebugKillTargets) Log.WriteLine("PickPrimaryWeaponTargetBasedOnTargetsForABattleship: IsPossibleToShoot # [" + Combat.PotentialCombatTargets.Count(i => i.IsPossibleToShoot) + "]");
                    if (DebugConfig.DebugKillTargets) Log.WriteLine("PickPrimaryWeaponTargetBasedOnTargetsForABattleship: IsReadyToShoot # [" + Combat.PotentialCombatTargets.Count(i => i.IsReadyToShoot) + "]");
                    _pickPrimaryWeaponTargetBasedOnTargetsForABattleship = Combat.PotentialCombatTargets.Where(i => i.IsPossibleToShoot)
                        .OrderByDescending(j => j.IsWarpScramblingMe && ((Drones.DronesKillHighValueTargets && !j.IsHighValueTarget) || j.IsHighValueTarget))
                        //.ThenByDescending(j => !j.IsTrigger)
                        .ThenByDescending(j => j.IsPrimaryWeaponPriorityTarget && !Drones.DronesKillHighValueTargets)
                        .ThenByDescending(j => j.IsLargeCollidableWeAlwaysWantToBlowupFirst)
                        //.ThenByDescending(i => i.IsAttacking && i.IsPlayer && i.IsBattlecruiser)
                        //.ThenByDescending(i => i.IsAttacking && i.IsPlayer && i.IsBattleship)
                        //.ThenByDescending(i => i.IsAttacking && i.IsPlayer && i.IsCruiser)
                        //.ThenByDescending(i => i.IsCorrectSizeForMyWeapons && i.IsWarpScramblingMe && !i.IsLargeCollidable)
                        //.ThenByDescending(i => i.IsCorrectSizeForMyWeapons)
                        //.ThenByDescending(i => i.IsEntityIShouldKeepShooting)

                        .ThenByDescending(i => i.IsNPCBattleship && 100 > i.HealthPct && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisNeutralizingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisWarpScramblingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisSensorDampeningNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTrackingDisruptingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTargetPaintingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking && i.IsTrackable && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
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
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking)


                        .ThenByDescending(i => i.IsNPCBattlecruiser && 100 > i.HealthPct && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisNeutralizingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisWarpScramblingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisSensorDampeningNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTrackingDisruptingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTargetPaintingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsTrackable && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsShortestRangeAmmoInRange)

                        .ThenByDescending(i => i.IsNPCBattlecruiser && 100 > i.HealthPct && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisNeutralizingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisWarpScramblingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisSensorDampeningNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTrackingDisruptingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTargetPaintingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking)

                        .ThenByDescending(i => i.IsNPCCruiser && 100 > i.HealthPct && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisNeutralizingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWarpScramblingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisSensorDampeningNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTrackingDisruptingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTargetPaintingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking && i.IsTrackable && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
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
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking)

                        .ThenByDescending(i => i.IsNPCBattleship && i.IsTargetedBy && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsTargetedBy && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCBattleship && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsTargetedBy)

                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsTargetedBy && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsTargetedBy && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsTargetedBy)

                        .ThenByDescending(i => i.IsNPCCruiser && i.IsTargetedBy && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsTargetedBy && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCCruiser && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsTargetedBy)

                        .ThenByDescending(i => i.IsNPCBattleship && 5000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 10000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 15000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 20000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 25000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 30000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 35000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 40000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 45000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 50000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 55000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 60000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 65000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && 70000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattleship && i.Distance > 70000)

                        .ThenByDescending(i => i.IsNPCBattlecruiser && 5000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 10000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 15000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 20000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 25000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 30000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 35000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 40000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 45000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 50000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 55000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 60000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 65000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && 70000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.Distance > 70000)

                        .ThenByDescending(i => i.IsNPCCruiser && 5000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 10000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 15000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 20000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 25000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 30000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 35000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 40000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 45000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 50000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 55000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 60000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 65000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && 70000 > i.Distance)
                        .ThenByDescending(i => i.IsNPCCruiser && i.Distance > 70000)

                        .ThenByDescending(i => i.IsNPCDestroyer && 100 > i.HealthPct && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisNeutralizingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisWarpScramblingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisSensorDampeningNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisTrackingDisruptingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisTargetPaintingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.IsAttacking && i.IsTrackable && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.IsAttacking && i.IsShortestRangeAmmoInRange)

                        .ThenByDescending(i => i.IsNPCDestroyer && 100 > i.HealthPct && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisNeutralizingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisWarpScramblingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisSensorDampeningNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisTrackingDisruptingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.KillThisTargetPaintingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.IsAttacking && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCDestroyer && i.IsAttacking)

                        .ThenByDescending(i => i.IsNPCFrigate && 100 > i.HealthPct && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisNeutralizingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWarpScramblingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisSensorDampeningNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTrackingDisruptingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTargetPaintingNpc && i.IsAttacking && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsAttacking && i.IsTrackable && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets && i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsAttacking && i.IsShortestRangeAmmoInRange)

                        .ThenByDescending(i => i.IsNPCFrigate && 100 > i.HealthPct && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisNeutralizingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWarpScramblingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisSensorDampeningNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTrackingDisruptingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTargetPaintingNpc && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsAttacking && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsAttacking && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsAttacking && i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsAttacking)

                        .ThenByDescending(i => i.IsNPCDestroyer)
                        .ThenByDescending(i => i.IsNPCFrigate)

                        .ThenByDescending(i => 100 > i.HealthPct && i.IsLastTargetPrimaryWeaponsWereShooting)
                        .ThenByDescending(i => 100 > i.HealthPct)
                        .ThenByDescending(i => i.IsShortestRangeAmmoInRange)
                        .ThenByDescending(i => i.CanSwapAmmoNowToHitThisTarget)
                        .ThenByDescending(j => j.IsLargeCollidableWeAlwaysWantToBlowupLast);

                    if (DebugConfig.DebugLogOrderOfKillTargets)
                        LogOrderOfKillTargets(_pickPrimaryWeaponTargetBasedOnTargetsForABattleship);

                    return _pickPrimaryWeaponTargetBasedOnTargetsForABattleship;
                }

                return _pickPrimaryWeaponTargetBasedOnTargetsForABattleship;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetBasedOnTargetsForACruiser => PickPrimaryWeaponTargetBasedOnTargetsForABattleship;

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetBasedOnTargetsForAFrigate
        {
            get
            {
                if (_pickPrimaryWeaponTargetBasedOnTargetsForAFrigate == null)
                {
                    _pickPrimaryWeaponTargetBasedOnTargetsForAFrigate = Combat.PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                        .OrderByDescending(j => j.IsWarpScramblingMe && ((Drones.DronesKillHighValueTargets && j.IsLowValueTarget) || !j.IsLowValueTarget))
                        .ThenByDescending(i => i.IsPrimaryWeaponPriorityTarget && !Drones.DronesKillHighValueTargets && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsPrimaryWeaponPriorityTarget && !Drones.DronesKillHighValueTargets && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsPrimaryWeaponPriorityTarget && !Drones.DronesKillHighValueTargets && i.ShieldPct < .90)
                        .ThenByDescending(j => j.IsPrimaryWeaponPriorityTarget && !Drones.DronesKillHighValueTargets)
                        .ThenByDescending(i => i.IsDronePriorityTarget && !Drones.DronesKillHighValueTargets && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsDronePriorityTarget && !Drones.DronesKillHighValueTargets && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsDronePriorityTarget && !Drones.DronesKillHighValueTargets && i.ShieldPct < .90)
                        .ThenByDescending(j => j.IsDronePriorityTarget && Drones.DronesKillHighValueTargets)
                        .ThenByDescending(i => !i.Name.Contains("Officer"))
                        .ThenByDescending(i => !i._directEntity.IsOfficerOrOverseer)
                        .ThenByDescending(i => KillSentries && i.IsSentry && i.IsTargetedBy)
                        .ThenByDescending(i => i.IsTrackable && i.IsInOptimalRange && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsTrackable && i.IsInOptimalRange && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsTrackable && i.IsInOptimalRange && i.ShieldPct < .90)
                        .ThenByDescending(i => i.IsTrackable && i.IsInOptimalRange)
                        .ThenByDescending(i => i.IsTargetedBy && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsTargetedBy && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsTargetedBy && i.ShieldPct < .90)
                        .ThenByDescending(i => i.IsTargetedBy)
                        .ThenByDescending(i => i.IsAttacking && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsAttacking && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsAttacking && i.ShieldPct < .90)
                        .ThenByDescending(i => i.IsAttacking)
                        .ThenByDescending(i => i.IsTrackable && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsTrackable && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsTrackable && i.ShieldPct < .90)
                        .ThenByDescending(i => i.IsTrackable)
                        .ThenByDescending(i => i.IsInOptimalRange && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsInOptimalRange && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsInOptimalRange && i.ShieldPct < .90)
                        .ThenByDescending(i => i.IsInOptimalRange)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisNeutralizingNpc)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWarpScramblingNpc)
                        .ThenByDescending(i => i.IsNPCFrigate && i.StructurePct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate && i.ArmorPct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate && i.ShieldPct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisWebbingNpc)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisSensorDampeningNpc)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTrackingDisruptingNpc)
                        .ThenByDescending(i => i.IsNPCFrigate && i.KillThisTargetPaintingNpc)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCFrigate)
                        .ThenByDescending(i => i.IsNPCFrigate && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsNPCFrigate && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsNPCFrigate && i.ShieldPct < .90)
                        .ThenByDescending(i => i.IsNPCCruiser && i.StructurePct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser && i.ArmorPct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser && i.ShieldPct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisNeutralizingNpc)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWarpScramblingNpc)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisWebbingNpc)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisSensorDampeningNpc)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTrackingDisruptingNpc)
                        .ThenByDescending(i => i.IsNPCCruiser && i.KillThisTargetPaintingNpc)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCCruiser)
                        .ThenByDescending(i => i.IsNPCCruiser && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsNPCCruiser && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsNPCCruiser && i.ShieldPct < .90)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.StructurePct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.ArmorPct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.ShieldPct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisNeutralizingNpc)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisWarpScramblingNpc)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisSensorDampeningNpc)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTrackingDisruptingNpc)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.KillThisTargetPaintingNpc)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattlecruiser)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.ShieldPct < .90)
                        .ThenByDescending(i => i.IsNPCBattleship && i.StructurePct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship && i.ArmorPct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship && i.ShieldPct < .90 && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisNeutralizingNpc)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisWarpScramblingNpc)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisSensorDampeningNpc)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTrackingDisruptingNpc)
                        .ThenByDescending(i => i.IsNPCBattleship && i.KillThisTargetPaintingNpc)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsAttacking)
                        .ThenByDescending(i => i.IsNPCBattleship)
                        .ThenByDescending(i => i.IsNPCBattleship && i.StructurePct < .90)
                        .ThenByDescending(i => i.IsNPCBattleship && i.ArmorPct < .90)
                        .ThenByDescending(i => i.IsNPCBattleship && i.ShieldPct < .90)
                        .ThenByDescending(i => i.KillThisNeutralizingNpc)
                        .ThenBy(i => i.StructureMaxHitPoints)
                        .ThenBy(i => i.ArmorMaxHitPoints)
                        .ThenBy(i => i.ShieldMaxHitPoints);

                    if (DebugConfig.DebugLogOrderOfKillTargets)
                        LogOrderOfKillTargets(_pickPrimaryWeaponTargetBasedOnTargetsForAFrigate);

                    return _pickPrimaryWeaponTargetBasedOnTargetsForAFrigate;
                }

                return _pickPrimaryWeaponTargetBasedOnTargetsForAFrigate;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetBasedOnTargetsForABurners
        {
            get
            {
                if (_pickPrimaryWeaponTargetBasedOnTargetsForBurners == null)
                {
                    _pickPrimaryWeaponTargetBasedOnTargetsForBurners = ESCache.Instance.Targets.Where(i => !i.IsWreck && !i.IsBadIdea && i.IsTarget && !i.IsNPCDrone)
                        .OrderByDescending(j => j.IsPrimaryWeaponPriorityTarget)
                        .ThenByDescending(j => j.IsDronePriorityTarget)
                        .ThenByDescending(j => j.IsBurnerMainNPC);

                    if (_pickPrimaryWeaponTargetBasedOnTargetsForBurners != null)
                    {
                        if (DebugConfig.DebugLogOrderOfKillTargets)
                            LogOrderOfKillTargets(_pickPrimaryWeaponTargetBasedOnTargetsForBurners);

                        return _pickPrimaryWeaponTargetBasedOnTargetsForBurners;
                    }

                    return null;
                }

                return null;
            }
        }

        public static IOrderedEnumerable<EntityCache> PickPrimaryWeaponTargetFleetAbyssalDeadSpace
        {
            get
            {
                if (_pickPrimaryWeaponTargetAbyssalDeadSpace == null)
                {
                    _pickPrimaryWeaponTargetAbyssalDeadSpace = Combat.PotentialCombatTargets.Where(i => i.IsReadyToShoot)
                        .OrderByDescending(k => !k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                        .ThenByDescending(j => j.IsNPCFrigate && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                        .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                        .ThenByDescending(j => j.IsNPCBattlecruiser && j.NpcHasNeutralizers && j.IsAttacking && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                        .ThenByDescending(j => j.IsNPCBattlecruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                        .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && j.IsAttacking && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                        .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                        .ThenByDescending(j => j.IsNPCFrigate && Drones.DronesKillHighValueTargets && j.IsNeutralizingMe && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                        .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                        .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                        .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                        .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                        .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600 && j.NpcHasRemoteRepair)
                        .ThenByDescending(j => j.IsNPCBattlecruiser && j.TriglavianDamage != null && j.TriglavianDamage > 600)
                        .ThenByDescending(k => k.IsAbyssalPrecursorCache)
                        .ThenByDescending(l => l.IsNPCFrigate && l.StructurePct < .90)
                        .ThenByDescending(l => l.IsNPCFrigate && l.ArmorPct < .90)
                        .ThenByDescending(l => l.IsNPCFrigate && l.ShieldPct < .90)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                        .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                        .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsTrackable)
                        .ThenByDescending(i => i.IsNPCFrigate && i.IsInOptimalRange)
                        .ThenByDescending(l => l.IsNPCCruiser && l.StructurePct < .90)
                        .ThenByDescending(l => l.IsNPCCruiser && l.ArmorPct < .90)
                        .ThenByDescending(l => l.IsNPCCruiser && l.ShieldPct < .90)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsTrackable)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsInOptimalRange)
                        .ThenByDescending(k => k.IsNPCCruiser)
                        .ThenByDescending(j => j.IsNPCBattleship && j.IsAttacking && j.TriglavianDamage != null && j.TriglavianDamage > 800 && j.NpcHasRemoteRepair)
                        .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800 && j.NpcHasRemoteRepair)
                        .ThenByDescending(j => j.IsNPCBattleship && j.IsAttacking && j.TriglavianDamage != null && j.TriglavianDamage > 800)
                        .ThenByDescending(j => j.IsNPCBattleship && j.TriglavianDamage != null && j.TriglavianDamage > 800)
                        .ThenByDescending(l => l.IsNPCBattleship && l.StructurePct < .90)
                        .ThenByDescending(l => l.IsNPCBattleship && l.ArmorPct < .90)
                        .ThenByDescending(l => l.IsNPCBattleship && l.ShieldPct < .90)
                        .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                        .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                        .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                        .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                        .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsTrackable)
                        .ThenByDescending(i => i.IsNPCBattleship && i.IsInOptimalRange)
                        .ThenByDescending(j => j.IsNPCBattleship)
                        .ThenByDescending(l => l.IsNPCBattlecruiser && l.StructurePct < .90)
                        .ThenByDescending(l => l.IsNPCBattlecruiser && l.ArmorPct < .90)
                        .ThenByDescending(l => l.IsNPCBattlecruiser && l.ShieldPct < .90)
                        .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasRemoteRepair)
                        .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsSensorDampeningMe)
                        .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsWebbingMe)
                        .ThenByDescending(k => k.IsNPCBattlecruiser && k.NpcHasNeutralizers)
                        .ThenByDescending(k => k.IsNPCBattlecruiser && k.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCBattlecruiser && l.IsTrackingDisruptingMe && DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsTrackable)
                        .ThenByDescending(i => i.IsNPCBattlecruiser && i.IsInOptimalRange)
                        .ThenByDescending(k => k.IsNPCBattlecruiser)
                        .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                        .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                        .ThenBy(p => p.StructurePct)
                        .ThenBy(q => q.ArmorPct)
                        .ThenBy(r => r.ShieldPct);

                    if (DebugConfig.DebugLogOrderOfKillTargets)
                        LogOrderOfKillTargets(_pickPrimaryWeaponTargetAbyssalDeadSpace);

                    return _pickPrimaryWeaponTargetAbyssalDeadSpace;
                }

                return _pickPrimaryWeaponTargetAbyssalDeadSpace;
            }
        }

        private static bool? _combatIsAppropriateHere { get; set; } = null;

        public static bool CombatIsAppropriateHere(string CalledFrom = "Combat")
        {
            try
            {
                try
                {
                    CombatisAppropriateHereStopwatch.Restart();

                    if (_combatIsAppropriateHere != null)
                        return (bool)_combatIsAppropriateHere;

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                    {
                        if (ESCache.Instance.InWormHoleSpace && ESCache.Instance.EntitiesNotSelf.Any(i => i.IsPlayer && !i.IsInMyFleet && !i.IsInMyEveSharpFleet))
                        {
                            if (DirectEve.Interval(7000))
                            {
                                DebugConfig.DebugDisableTargetCombatants = true;
                                DebugConfig.DebugDisableCombat = true;
                                try
                                {
                                    if (ESCache.Instance.EntitiesNotSelf.Any(i => i.IsPlayer && !i.IsInMyFleet && !i.IsInMyEveSharpFleet))
                                    {
                                        var thisPlayer = ESCache.Instance.EntitiesNotSelf.FirstOrDefault(i => i.IsPlayer && !i.IsInMyFleet && !i.IsInMyEveSharpFleet);
                                        string msg = "Notification!: Player found [" + thisPlayer.TypeName + "] Name [" + thisPlayer.Name + "] Distance [" + thisPlayer.Nearest1KDistance + "k] Velocity [" + thisPlayer.Velocity + "]";
                                        Log.WriteLine(msg);

                                    }
                                }
                                catch (Exception){}


                                foreach (var npc in ESCache.Instance.Targets.Where(i => i.IsNpc))
                                {
                                    Log.WriteLine("Unlocking NPC [" + npc.TypeName + "][" + npc.Nearest1KDistance + "] because PVP!");
                                    npc.UnlockTarget();
                                    continue;
                                }

                                //Util.PlayNoticeSound();
                                return false;
                            }

                            return false;
                        }
                    }

                    if (CalledFrom != "Combat" && Salvage.Salvagers.Count >= 3)
                        return true;

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 1 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    if (!ESCache.Instance.InSpace)
                    {
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 1a Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = false;
                        return (bool)_combatIsAppropriateHere;
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 2 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    if (ESCache.Instance.InStation)
                    {
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 2b Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = false;
                        return (bool)_combatIsAppropriateHere;
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 3 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]!");
                    return false;
                }

                try
                {
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 5 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = true;
                        return (bool)_combatIsAppropriateHere;
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 6 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "].");
                    return false;
                }

                try
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (ESCache.Instance.InWarp)
                        {
                            try
                            {
                                CombatisAppropriateHereStopwatch.Stop();
                                if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 7 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                                CombatisAppropriateHereStopwatch.Restart();

                                _combatIsAppropriateHere = false;
                                return (bool)_combatIsAppropriateHere;
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]..");
                                return false;
                            }
                        }

                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 8 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]!!");
                    return false;
                }

                try
                {
                    //if (ESCache.Instance.InWormHoleSpace)
                    //    return true;
                }
                catch (Exception){}

                try
                {
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) ||
                        ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                    {
                        //Citadel is close...
                        if (ESCache.Instance.Citadels.Count > 0 && 20000 > ESCache.Instance.ClosestCitadel.Distance && ESCache.Instance.Targets.Count == 0 && (PotentialCombatTargets.Count == 0 || PotentialCombatTargets.All(i => !i.IsTargetedBy)))
                        {
                            CombatisAppropriateHereStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 9 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                            CombatisAppropriateHereStopwatch.Restart();

                            _combatIsAppropriateHere = false;
                            return (bool)_combatIsAppropriateHere; //We are probably tethered
                        }

                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 9b Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        if (ESCache.Instance.Stations.Count > 0 && 20000 > ESCache.Instance.ClosestStation.Distance && ESCache.Instance.Targets.Count == 0 && (PotentialCombatTargets.Count == 0 || PotentialCombatTargets.All(i => !i.IsTargetedBy)))
                        {
                            CombatisAppropriateHereStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 10 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                            CombatisAppropriateHereStopwatch.Restart();

                            _combatIsAppropriateHere = false;
                            return (bool)_combatIsAppropriateHere;
                        }

                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 11 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        //Log.WriteLine("DebugTargetCombatantsController18: InSpace [" + ESCache.Instance.InSpace + "] InStation [" + ESCache.Instance.InStation + "]");
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 12 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = true;
                        return (bool)_combatIsAppropriateHere;
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 12b Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HydraController))
                    {
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 13 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = true;
                        return (bool)_combatIsAppropriateHere;
                    }

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.MiningController))
                    {
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 13 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = true;
                        return (bool)_combatIsAppropriateHere;
                    }

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.DeepFlowSignaturesController))
                    {
                        _combatIsAppropriateHere = false;
                        return (bool)_combatIsAppropriateHere;
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 13b Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WormHoleAnomalyController))
                    {
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 14 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = true;
                        return (bool)_combatIsAppropriateHere;
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 15 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    if ((State.CurrentCombatState != CombatState.Idle ||
                         State.CurrentCombatState != CombatState.OutOfAmmo) &&
                        (ESCache.Instance.InStation ||
                         !ESCache.Instance.InSpace ||
                         ESCache.Instance.ActiveShip.Entity == null ||
                         ESCache.Instance.ActiveShip.Entity.IsCloaked))
                    {
                        State.CurrentCombatState = CombatState.Idle;
                        if (DebugConfig.DebugCombat || DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("CombatIsAppropriateHere: False: NotIdle, NotOutOfAmmo and InStation or NotInspace or ActiveShip is null or cloaked");
                        {
                            CombatisAppropriateHereStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 16 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                            CombatisAppropriateHereStopwatch.Restart();

                            _combatIsAppropriateHere = false;
                            return (bool)_combatIsAppropriateHere;
                        }
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 17a Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]!..");
                    return false;
                }

                try
                {
                    if (ESCache.Instance.InStation)
                    {
                        State.CurrentCombatState = CombatState.Idle;
                        if (DebugConfig.DebugCombat || DebugConfig.DebugTargetCombatants) Log.WriteLine("CombatIsAppropriateHere: False: We are in station, do nothing");

                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 18 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = false;
                        return (bool)_combatIsAppropriateHere;
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 18b Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    if (ESCache.Instance.InsidePosForceField)
                    {
                        State.CurrentCombatState = CombatState.Idle;
                        if (DebugConfig.DebugCombat || DebugConfig.DebugTargetCombatants) Log.WriteLine("CombatIsAppropriateHere: False: We are in a POS ForceField, do nothing");

                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 19 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = false;
                        return (bool)_combatIsAppropriateHere;
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 20 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    if (ESCache.Instance.Targets.Count == 0)
                    {
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 21a Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        if (ESCache.Instance.ClosestDockableLocation != null && 9500 > ESCache.Instance.ClosestDockableLocation.Distance)
                        {
                            State.CurrentCombatState = CombatState.Idle;
                            if (DebugConfig.DebugCombat || DebugConfig.DebugTargetCombatants || DebugConfig.DebugKillTargets) Log.WriteLine("CombatIsAppropriateHere: False: We are probably tethered, do nothing: DockableLocation [" + ESCache.Instance.ClosestDockableLocation.Id + "] TypeName [" + ESCache.Instance.ClosestDockableLocation.TypeName + "] Distance [" + ESCache.Instance.ClosestDockableLocation.Nearest1KDistance + "]");

                            CombatisAppropriateHereStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 21b Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                            CombatisAppropriateHereStopwatch.Restart();

                            _combatIsAppropriateHere = false;
                            return (bool) _combatIsAppropriateHere;
                        }
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 21c Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]!..!");
                    return false;
                }

                try
                {
                    if (ESCache.Instance.MyShipEntity != null && ESCache.Instance.ActiveShip != null &&
                        !ESCache.Instance.MyShipEntity.IsFrigate &&
                        !ESCache.Instance.MyShipEntity.IsCruiser &&
                        ESCache.Instance.MyShipEntity.GroupId != (int)Group.Capsule &&
                        ESCache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.SalvageShipName.ToLower() &&
                        ESCache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.TransportShipName.ToLower() &&
                        ESCache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.StorylineTransportShipName.ToLower() &&
                        ESCache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.TravelShipName.ToLower() &&
                        ESCache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.MiningShipName.ToLower() &&
                        ESCache.Instance.ActiveShip.GroupId != (int)Group.Shuttle)
                    {
                        if (ESCache.Instance.Weapons.Count == 0 && ESCache.Instance.InSpace)
                        {
                            ChangeCombatState(CombatState.OutOfAmmo, "CombatIsAppropriateHere: False: Your Current ship [" + ESCache.Instance.ActiveShip.GivenName + "] has no weapons!");

                            CombatisAppropriateHereStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 22 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                            CombatisAppropriateHereStopwatch.Restart();

                            _combatIsAppropriateHere = false;
                            return (bool)_combatIsAppropriateHere;
                        }

                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 23 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        if (ESCache.Instance.MyShipEntity.GivenName.ToLower() != CombatShipName.ToLower() && !ESCache.Instance.InWormHoleSpace)
                        {
                            Log.WriteLine("CombatIsAppropriateHere: False: Your Current ship [" + ESCache.Instance.MyShipEntity.GivenName + "] GroupID [" +
                                          ESCache.Instance.MyShipEntity.GroupId + "] TypeID [" +
                                          ESCache.Instance.MyShipEntity.TypeId + "] is not the CombatShipName [" + CombatShipName + "]");
                            ChangeCombatState(CombatState.OutOfAmmo, "CombatIsAppropriateHere: False");

                            CombatisAppropriateHereStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 24 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                            CombatisAppropriateHereStopwatch.Restart();

                            _combatIsAppropriateHere = false;
                            return (bool)_combatIsAppropriateHere;
                        }

                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 25 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        if (State.CurrentCombatState == CombatState.Idle)
                        {
                            ChangeCombatState(CombatState.KillTargets);

                            CombatisAppropriateHereStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 25b Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                            CombatisAppropriateHereStopwatch.Restart();
                        }
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 26a Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    if (!ESCache.Instance.WeHaveWeapons)
                    {
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 26b Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        if (!Drones.UseDrones)
                        {
                            CombatisAppropriateHereStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 27 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                            CombatisAppropriateHereStopwatch.Restart();

                            _combatIsAppropriateHere = false;
                            return (bool)_combatIsAppropriateHere;
                        }
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 28 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    if (!ESCache.Instance.InMission)
                    {
                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 29 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        if (ESCache.Instance.EntitiesOnGrid.All(i => !i.IsTargetedBy))
                        {
                            if (DebugConfig.DebugCombat || DebugConfig.DebugTargetCombatants) Log.WriteLine("CombatIsAppropriateHere: False: if (ESCache.Instance.EntitiesOnGrid.All(i => !i.IsTargetedBy))");

                            CombatisAppropriateHereStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 30 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                            CombatisAppropriateHereStopwatch.Restart();

                            _combatIsAppropriateHere = false;
                            return (bool)_combatIsAppropriateHere;
                        }

                        if (DebugConfig.DebugCombat || DebugConfig.DebugTargetCombatants) Log.WriteLine("CombatIsAppropriateHere: True: Something has us targeted...");

                        CombatisAppropriateHereStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 35 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                        CombatisAppropriateHereStopwatch.Restart();

                        _combatIsAppropriateHere = true;
                        return (bool)_combatIsAppropriateHere;
                    }

                    CombatisAppropriateHereStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("CombatIsAppropriateHere 37 Took [" + Util.ElapsedMicroSeconds(CombatisAppropriateHereStopwatch) + "]");
                    CombatisAppropriateHereStopwatch.Restart();

                    _combatIsAppropriateHere = true;
                    return (bool)_combatIsAppropriateHere;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]!.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]!!!");
                return false;
            }
        }

        public static void InvalidateCache()
        {
            try
            {
                _activateBastion = null;
                _aggressed = null;
                _deActivateBastion = null;
                _chargeToLoadIntoWeapon = null;
                _combatIsAppropriateHere = null;
                _currentWeaponTarget = null;
                _doWeCurrentlyHaveProjectilesMounted = null;
                _doWeCurrentlyHaveTurretsMounted = null;
                _entityToUseForAmmo = null;
                _killTarget = null;
                _maxrange = null;
                _maxWeaponRange = null;
                _maxTargetRange = null;
                _maxMiningRange = null;
                _potentialCombatTargets = null;
                _primaryWeaponPriorityTargetsPerFrameCaching = null;
                _targetedBy = null;
                _pickPrimaryWeaponTarget = null;
                _pickPrimaryWeaponTargetBasedOnTargetsForAFrigate = null;
                _pickPrimaryWeaponTargetBasedOnTargetsForBurners = null;
                //_pickPrimaryWeaponTargetBasedOnTargetsForACruiser = null;
                _pickPrimaryWeaponTargetBasedOnTargetsForABattleship = null;
                _pickPrimaryWeaponTargetAbyssalDeadSpace = null;
                _pickPrimaryWeaponTargetTriglavianInvasion = null;
                _pickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard = null;
                //_pickPrimaryWeaponTargetFleetAbyssalDeadSpace = null;
                _pickPrimaryWeaponTargetWspaceDreadnaught = null;
                _pickPrimaryWeaponTargetWspace = null;
                _pickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank = null;
                _pickWhatToLockNextBasedOnTargetsForABattleship = null;
                _pickWhatToLockNextBasedOnTargetsForLowValueTargets = null;
                _primaryWeaponPriorityEntities = null;
                _preferredPrimaryWeaponTarget = null;
                _AmmoInCargoThatCanReachThisSpecificDistance = null;
                _AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance = null;
                _targetedByCount = null;
                _targetedBy = null;
                _usableAmmoInCargo = null;
                _usableSpecialAmmoInCargo = null;
                Targeting_AbyssalTargetsToLock = null;
                Targeting_TriglavianInvasionTargetsToLock = null;
                Targeting_TriglavianInvasionNestorTargetsToLock = null;
                Targeting_TriglavianInvasionLogisticsTargetsToLock = null;
                Targeting_TriglavianInvasionAntiLooterTargetsToLock = null;

                if (_primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Count > 0)
                    _primaryWeaponPriorityTargets.ForEach(pt => pt.ClearCache());
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static EntityCache EwarTarget
        {
            get
            {
                if (Drones.DronesKillHighValueTargets)
                    return Drones._cachedDroneTarget ?? _killTarget ?? null;

                return _killTarget ?? null;
            }
        }

        private static Stopwatch KillTargetsStopwatch = new Stopwatch();
        private static Stopwatch CombatisAppropriateHereStopwatch = new Stopwatch();
        public static bool BoolReloadWeaponsAsap { get; set; } = false;
        public static bool ReloadWeaponsIfIdle = false;

        /**
        public static void ChangeAmmoIfNeeded()
        {
            if (ESCache.Instance.Weapons.Any(i => i.Charge == null && !i.IsActive && !i.IsReloadingAmmo && !i.InLimboState && ReloadWeaponsIfIdle))
            {
                if (!BoolReloadWeaponsAsap)
                {
                    Log.WriteLine("ChangeAmmoIfNeeded: BoolReloadWeaponsAsap = true");
                    BoolReloadWeaponsAsap = true;
                    return;
                }

                return;
            }

            if (!Combat.AllowChangingAmmoInWspace && ESCache.Instance.InWormHoleSpace)
                return;

            if (ESCache.Instance.InAbyssalDeadspace)
                return;

            if (KillTarget.ShouldWeChangeToShorterRangeAmmo || KillTarget.ShouldWeChangeAmmo)
            {
                if (KillTarget.IsTarget && (KillTarget.IsNpcCapitalEscalation || KillTarget._directEntity.IsNPCWormHoleSpaceDrifter))
                {
                    if (DebugConfig.DebugReloadorChangeAmmo) Log.WriteLine("ChangeAmmoIfNeeded: KillTarget.IsNpcCapitalEscalation [" + KillTarget.IsNpcCapitalEscalation + "] KillTarget.IsNPCWormHoleSpaceDrifter [" + KillTarget._directEntity.IsNPCWormHoleSpaceDrifter + "]");
                    Combat.ReloadAll();
                    return;
                }

                if (KillTarget.IsNPCBattleship &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.IsNPCBattleship && i.IsAttacking) >= 2 &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.Distance > i.CurrentAmmo.Range && i.LongestRangeAmmo.Range > i.Distance && i.IsNPCBattleship && i.IsAttacking) <= 2)
                {
                    Log.WriteLine("ChangeAmmoIfNeeded: KillTarget.IsNPCBattleship [" + KillTarget.IsNPCBattleship + "] PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCBattleship) [" + PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCBattleship) + "]");
                    Combat.ReloadAll();
                    return;
                }

                if (KillTarget.IsNPCBattlecruiser &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.IsNPCBattlecruiser && i.IsAttacking) >= 2 &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.Distance > i.CurrentAmmo.Range && i.LongestRangeAmmo.Range > i.Distance && i.IsNPCBattlecruiser && i.IsAttacking) <= 2)
                {
                    Log.WriteLine("ChangeAmmoIfNeeded: KillTarget.IsNPCBattleship [" + KillTarget.IsNPCBattleship + "] PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCBattlecruiser) [" + PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCBattlecruiser) + "]");
                    Combat.ReloadAll();
                    return;
                }

                if (KillTarget.IsNPCCruiser &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.IsNPCCruiser && i.IsAttacking) >= 2 &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.Distance > i.CurrentAmmo.Range && i.LongestRangeAmmo.Range > i.Distance && i.IsNPCCruiser && i.IsAttacking) <= 2)
                {
                    Log.WriteLine("ChangeAmmoIfNeeded: KillTarget.IsNPCBattleship [" + KillTarget.IsNPCBattleship + "] PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCCruiser) [" + PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCCruiser) + "]");
                    Combat.ReloadAll();
                    return;
                }

                if (KillTarget.IsNPCDestroyer &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.IsNPCDestroyer && i.IsAttacking) >= 2 &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.Distance > i.CurrentAmmo.Range && i.LongestRangeAmmo.Range > i.Distance && i.IsNPCDestroyer && i.IsAttacking) <= 2)
                {
                    Log.WriteLine("ChangeAmmoIfNeeded: KillTarget.IsNPCBattleship [" + KillTarget.IsNPCBattleship + "] PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCDestroyer) [" + PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCDestroyer) + "]");
                    Combat.ReloadAll();
                    return;
                }

                if (KillTarget.IsNPCFrigate &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.IsNPCFrigate && i.IsAttacking) >= 2 &&
                    PotentialCombatTargets.Count(i => KillTarget.Id != i.Id && i.Distance > i.CurrentAmmo.Range && i.LongestRangeAmmo.Range > i.Distance && i.IsNPCFrigate && i.IsAttacking) <= 2)
                {
                    Log.WriteLine("ChangeAmmoIfNeeded: KillTarget.IsNPCBattleship [" + KillTarget.IsNPCBattleship + "] PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCFrigate) [" + PotentialCombatTargets.Count(i => i.ShouldWeChangeToShorterRangeAmmo && i.IsNPCFrigate) + "]");
                    Combat.ReloadAll();
                    return;
                }
            }
        }
        **/

        public static bool KillTargets()
        {
            //if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 500)
            //    return false;
            //
            //_lastPulse = DateTime.UtcNow;

            if (BoolReloadWeaponsAsap)
            {
                Log.WriteLine("KillTargets: if (BoolReloadWeaponsAsap)");
                if (!DebugConfig.DebugDisableAmmoManagement)
                {
                    AmmoManagementBehavior.ChangeAmmoManagementBehaviorState(States.AmmoManagementBehaviorState.HandleWeaponsNeedReload);
                }
                else ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
            }

            KillTargetsStopwatch.Restart();

            if (!CombatIsAppropriateHere("Combat"))
            {
                if (DebugConfig.DebugKillTargets) Log.WriteLine("KillTargets: if (!CombatIsAppropriateHere())");
                return false;
            }

            KillTargetsStopwatch.Stop();
            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 1 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
            KillTargetsStopwatch.Restart();

            if (State.CurrentHydraState == HydraState.Leader)
            {
                if (DebugConfig.DebugKillTargets) Log.WriteLine("KillTargets: if (State.CurrentHydraState == HydraState.Leader) return true");
                return true;
            }

            if (ESCache.Instance.EveAccount.BotUsesHydra && ESCache.Instance.EveAccount.IsLeader && ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.HydraController))
                HydraController.PushAggroEntityInfo();

            KillTargetsStopwatch.Stop();
            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 2 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
            KillTargetsStopwatch.Restart();

            if (ESCache.Instance.Weapons.Count == 0 && ESCache.Instance.Modules.Count(i => i.IsRemoteArmorRepairModule || i.IsRemoteShieldRepairModule || i.IsRemoteEnergyTransferModule) > 3)
            {
                //
                // this must be a logistics boat
                //
                if (DebugConfig.DebugKillTargets) Log.WriteLine("KillTargets: if (!ESCache.Instance.Weapons.Any() && ESCache.Instance.Modules.Count(i => i.IsRemoteArmorRepairModule || i.IsRemoteShieldRepairModule || i.IsRemoteEnergyTransferModule) > 3) return true");
                return true;
            }

            KillTargetsStopwatch.Stop();
            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 5 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
            KillTargetsStopwatch.Restart();

            try
            {
                List<long> ChooseNavigateOnGridTargets = NavigateOnGrid.ChooseNavigateOnGridTargetIds;
                KillTargetsStopwatch.Stop();
                if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 15 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                KillTargetsStopwatch.Restart();

                if (ChooseNavigateOnGridTargets.Count > 0)
                {
                    if (DebugConfig.DebugNavigateOnGrid)
                        Log.WriteLine("KillTargets: NavigateIntoRange: InMission [" + ESCache.Instance.InMission + "] SpeedTank [" + NavigateOnGrid.SpeedTank + "] InAbyssalDeadspace [" + ESCache.Instance.InAbyssalDeadspace + "]");

                    NavigateOnGrid.NavigateIntoRange(ChooseNavigateOnGridTargets, "Combat", ESCache.Instance.NormalNavigation);
                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 19 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }

            KillTargetsStopwatch.Stop();
            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 25 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
            KillTargetsStopwatch.Restart();

            if (!ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTarget))
            {
                if (DebugConfig.DebugKillTargets) Log.WriteLine("KillTargets: No Targets yet. waiting.");
                return false;
            }

            KillTargetsStopwatch.Stop();
            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 27 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
            KillTargetsStopwatch.Restart();

            try
            {
                if (KillTarget != null)
                {
                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 35 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();

                    if (DebugConfig.DebugKillTargets) Log.WriteLine("Overloading Weapons");
                    OverloadWeapons();
                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 38 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();

                    if (DebugConfig.DebugKillTargets) Log.WriteLine("Overloading Ecm");
                    OverloadEcm();
                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 41 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();

                    if (DebugConfig.DebugKillTargets) Log.WriteLine("Overloading Webs");
                    OverloadWeb();
                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 45 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();

                    if (DebugConfig.DebugKillTargets) Log.WriteLine("Activating Ecm");
                    ActivateEcm();
                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 47 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();

                    if (DebugConfig.DebugKillTargets) Log.WriteLine("Activating Tracking Disruptors");
                    ActivateTrackingDisruptors(EwarTarget);
                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 51 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();

                    if (DebugConfig.DebugKillTargets) Log.WriteLine("Activating Painters");
                    ActivateTargetPainters(EwarTarget);
                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 55 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();

                    if (DebugConfig.DebugKillTargets) Log.WriteLine("Activating SensorDampeners");
                    ActivateSensorDampeners(EwarTarget);
                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 61 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();

                    KillTargetsStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 65 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                    KillTargetsStopwatch.Restart();

                    if (KillTarget.IsReadyToShoot)
                    {
                        //
                        // Shorter range ammo?
                        //

                        //ChangeAmmoIfNeeded();

                        KillTargetsStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 71 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                        KillTargetsStopwatch.Restart();

                        if (DebugConfig.DebugKillTargets) Log.WriteLine("Activating BastionAndSiegeModules");
                        ActivateBastion(DateTime.UtcNow.AddMinutes(5), false);
                        KillTargetsStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 72 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                        KillTargetsStopwatch.Restart();

                        if (DebugConfig.DebugKillTargets) Log.WriteLine("Activating Webs");
                        ActivateStasisWeb(EwarTarget);
                        KillTargetsStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 73 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                        KillTargetsStopwatch.Restart();

                        if (DebugConfig.DebugKillTargets) Log.WriteLine("Activating WarpDisruptors");
                        ActivateWarpDisruptor(EwarTarget);
                        KillTargetsStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 74 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                        KillTargetsStopwatch.Restart();

                        //if (DebugConfig.DebugKillTargets) Log.WriteLine("Activating NOS/Neuts");
                        //ActivateNos(EwarTarget);
                        KillTargetsStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 75 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                        KillTargetsStopwatch.Restart();

                        if (DebugConfig.DebugKillTargets) Log.WriteLine("Activating Weapons");
                        ActivateWeapons(KillTarget);
                        KillTargetsStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("KillTargets 76 Took [" + Util.ElapsedMicroSeconds(KillTargetsStopwatch) + "]");
                        return true;
                    }
                    else
                    {
                        //
                        // Longer range ammo?
                        //
                        //if (KillTarget.IsPossibleToShoot)
                        //{
                        //    Combat.ReloadAll();
                        //}
                    }

                    if (DebugConfig.DebugKillTargets)
                        Log.WriteLine("killTarget [" + KillTarget.Name + "][" + Math.Round(KillTarget.Distance / 1000, 0) + "k][" +
                                      KillTarget.MaskedId +
                                      "] is not yet ReadyToShoot, LockedTarget [" + KillTarget.IsTarget + "] My MaxRange [" +
                                      Math.Round(MaxRange / 1000, 0) + "]");
                    return true;
                }

                if (DebugConfig.DebugKillTargets)
                    Log.WriteLine("We do not have a killTarget targeted, waiting");

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) ||
                        ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                    return true;

                //if (PrimaryWeaponPriorityTargets.Any() ||
                //    PotentialCombatTargets.Any() && ESCache.Instance.Targets.Any() && (!ESCache.Instance.InMission || NavigateOnGrid.SpeedTank))
                //{
                //    GetBestPrimaryWeaponTarget(MaxRange, false, "Combat");
                //    icount = 0;
                //}

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public static void ShootNPCWeAreAlreadyEngagingIfSameName()
        {
            if (_pickPrimaryWeaponTarget != null && PotentialCombatTargets.Count > 0)
                if (PotentialCombatTargets.Any(i => i.IsLastTargetPrimaryWeaponsWereShooting && i.IsInRangeOfWeapons && i.IsTarget && i.Name == _pickPrimaryWeaponTarget.Name && !i.WeShouldFocusFire))
                {
                    _pickPrimaryWeaponTarget = PotentialCombatTargets.Find(i => i.IsLastTargetPrimaryWeaponsWereShooting && i.IsInRangeOfWeapons && i.IsTarget && !i.WeShouldFocusFire);
                    if (DebugConfig.DebugPotentialCombatTargets)
                    {
                        Log.WriteLine("ShootNPCWeAreAlreadyEngagingIfSameName;" + _pickPrimaryWeaponTarget.Name + ";" + Math.Round(_pickPrimaryWeaponTarget.Distance / 1000, 0) + "k;IsReadyToShoot;" + _pickPrimaryWeaponTarget.IsReadyToShoot + ";IsReadyToTarget;" + _pickPrimaryWeaponTarget.IsReadyToTarget + ";IsTarget;" + _pickPrimaryWeaponTarget.IsTarget + ";BS;" + _pickPrimaryWeaponTarget.IsBattleship + ";BC;" + _pickPrimaryWeaponTarget.IsBattlecruiser + ";C;" + _pickPrimaryWeaponTarget.IsCruiser + ";F;" + _pickPrimaryWeaponTarget.IsFrigate + ";isAttacking;" + _pickPrimaryWeaponTarget.IsAttacking + ";IsTargetedBy;" + _pickPrimaryWeaponTarget.IsTargetedBy + ";IsWarpScramblingMe;" + _pickPrimaryWeaponTarget.IsWarpScramblingMe + ";IsNeutralizingMe;" + _pickPrimaryWeaponTarget.IsNeutralizingMe + ";Health;" + _pickPrimaryWeaponTarget.HealthPct + ";ShieldPct;" + _pickPrimaryWeaponTarget.ShieldPct + ";ArmorPct;" + _pickPrimaryWeaponTarget.ArmorPct + ";StructurePct;" + _pickPrimaryWeaponTarget.StructurePct);
                    }
                }
        }

        public static List<EntityCache> _listOfPickPrimaryWeaponTarget { get; set; }
        public static EntityCache _pickPrimaryWeaponTarget { get; set; }

        private static DateTime _lastPickPrimaryWeaponTarget { get; set; } = DateTime.UtcNow;

        private static EntityCache PickPrimaryWeaponTarget()
        {
            Stopwatch tempStopwatch = new Stopwatch();
            try
            {
                tempStopwatch.Restart();

                if (_pickPrimaryWeaponTarget != null)
                    return _pickPrimaryWeaponTarget;

                if (DateTime.UtcNow < _lastPickPrimaryWeaponTarget.AddMilliseconds(300))
                {
                    //if (DebugConfig.DebugCombat)
                    //    Log.WriteLine("if (DateTime.UtcNow < _lastPickPrimaryWeaponTarget.AddMilliseconds(1000))");
                    return null;
                }

                _lastPickPrimaryWeaponTarget = DateTime.UtcNow;
                if (DebugConfig.DebugKillTargets) Log.WriteLine("DebugTargetCombatants: Entering PickPrimaryWeaponTarget()");

                if (ESCache.Instance.Targets.Any(i => !i.IsContainer && !i.IsBadIdea))
                {
                    if (DebugConfig.DebugPreferredPrimaryWeaponTarget || DebugConfig.DebugKillTargets)
                    {
                        if (ESCache.Instance.Targets.Count > 0)
                        {
                            if (PreferredPrimaryWeaponTarget != null)
                            {
                                Log.WriteLine("PreferredPrimaryWeaponTarget [" + PreferredPrimaryWeaponTarget.Name + "][" +
                                              Math.Round(PreferredPrimaryWeaponTarget.Distance / 1000, 0) + "k][" +
                                              PreferredPrimaryWeaponTarget.MaskedId + "] IsTarget [" + PreferredPrimaryWeaponTarget.IsTarget + "] IsTargeting [" + PreferredPrimaryWeaponTarget.IsTargeting + "]");
                            }
                            else
                            {
                                Log.WriteLine("PreferredPrimaryWeaponTarget [ null ]");
                            }
                        }
                    }

                    if (ESCache.Instance.InWormHoleSpace)
                    {
                        if (ESCache.Instance.ActiveShip.IsDread)
                        {
                            if (ESCache.Instance.Weapons.Any(i => i.IsHighAngleWeapon))
                            {
                                if (DebugConfig.DebugTargetCombatants)
                                    Log.WriteLine("DebugTargetCombatants: Targeting_WormHoleAnomaly_Dread();");
                                _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetWSpaceDreadnaught_HighAngle.FirstOrDefault();
                                ShootNPCWeAreAlreadyEngagingIfSameName();
                                tempStopwatch.Stop();
                                if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                                return _pickPrimaryWeaponTarget;
                            }

                            if (DebugConfig.DebugTargetCombatants)
                                Log.WriteLine("DebugTargetCombatants: Targeting_WormHoleAnomaly_Dread();");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetWSpaceDreadnaught_BigGuns.FirstOrDefault();
                            ShootNPCWeAreAlreadyEngagingIfSameName();
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        if (ESCache.Instance.ActiveShip.IsMarauder || ESCache.Instance.ActiveShip.IsBattleship || ESCache.Instance.ActiveShip.IsBattleCruiser)
                        {
                            if (DebugConfig.DebugTargetCombatants)
                                Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetWSpaceMarauder();");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetWSpaceMarauder.FirstOrDefault();
                            ShootNPCWeAreAlreadyEngagingIfSameName();
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: Targeting_WormHoleAnomaly();");
                        _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetWSpace.FirstOrDefault();
                        ShootNPCWeAreAlreadyEngagingIfSameName();
                        tempStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                        return _pickPrimaryWeaponTarget;
                    }

                    if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    {
                        if (DebugConfig.DebugKillTargets) Log.WriteLine("DebugTargetCombatants: if (ESCache.Instance.InAbyssalDeadspace)");

                        if (ESCache.Instance.ActiveShip.IsFrigate)
                        {
                            if (!PotentialCombatTargets.Any())
                            {
                                if (ESCache.Instance.EntitiesOnGrid.Count > 0 && ESCache.Instance.EntitiesOnGrid.Any(i => (i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache || i.IsAbyssalDeadspaceTriglavianExtractionNode) && i.IsTarget && i.IsInRangeOfWeapons))
                                {
                                    var bioadaptivecache = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => (i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache || i.IsAbyssalDeadspaceTriglavianExtractionNode) && i.IsTarget && i.IsInRangeOfWeapons);
                                    if (DebugConfig.DebugKillTargets) Log.WriteLine("IsAbyssalDeadspaceTriglavianBioAdaptiveCache found [" + bioadaptivecache.Name + "][" + bioadaptivecache.TypeId + "][" + bioadaptivecache.GroupId + "]");
                                    _pickPrimaryWeaponTarget = bioadaptivecache;
                                    tempStopwatch.Stop();
                                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                                    return _pickPrimaryWeaponTarget;
                                }
                            }
                        }
                        else if (ESCache.Instance.EntitiesOnGrid.Count > 0 && ESCache.Instance.EntitiesOnGrid.Any(i => (i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache || i.IsAbyssalDeadspaceTriglavianExtractionNode) && i.IsTarget && i.IsInRangeOfWeapons))
                        {
                            var bioadaptivecache = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => (i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache || i.IsAbyssalDeadspaceTriglavianExtractionNode) && i.IsTarget && i.IsInRangeOfWeapons);
                            if (DebugConfig.DebugKillTargets) Log.WriteLine("IsAbyssalDeadspaceTriglavianBioAdaptiveCache found [" + bioadaptivecache.Name + "][" + bioadaptivecache.TypeId + "][" + bioadaptivecache.GroupId + "]");
                            _pickPrimaryWeaponTarget = bioadaptivecache;
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        //if (ESCache.Instance.EntitiesOnGrid.Count > 0 && ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalDeadspaceTriglavianExtractionNode && i.IsTarget && i.IsInRangeOfWeapons))
                        //{
                        //    var extractionnode = ESCache.Instance.EntitiesOnGrid.Find(i => i.IsAbyssalDeadspaceTriglavianExtractionNode && i.IsTarget && i.IsInRangeOfWeapons);
                        //    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("IsAbyssalDeadspaceTriglavianExtractionNode found [" + extractionnode.Name + "][" + extractionnode.TypeId + "][" + extractionnode.GroupId + "]");
                        //    _pickPrimaryWeaponTarget = extractionnode;
                        //    tempStopwatch.Stop();
                        //    if (DebugConfig.DebugKillTargets) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                        //    return _pickPrimaryWeaponTarget;
                        //}

                        if (Drones.DronesKillHighValueTargets)
                        {
                            if (DebugConfig.DebugKillTargets) Log.WriteLine("DebugTargetCombatants: if (Drones.DronesKillHighValueTargets)");
                            try
                            {
                                //
                                // if drone target isnt yet chosen, choose the target for drones
                                //

                                if (Drones._cachedDroneTarget == null)
                                    Drones.PickDroneTarget();

                                //
                                // continues on to choose targets for gune
                                //
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }

                            if (Drones._cachedDroneTarget != null)
                            {
                                //
                                // if we should focus fire use drone target for guns / missiles
                                //
                                if (Drones._cachedDroneTarget.IsReadyToShoot)
                                {
                                    if (DebugConfig.DebugKillTargets) Log.WriteLine("DebugTargetCombatants: if (Drones._cachedDroneTarget.IsReadyToShoot)");
                                    if (DebugConfig.DebugPickTargets && Drones._cachedDroneTarget.Name.Contains("Vedmak"))
                                        Log.WriteLine("DebugPickTargets: IsReadyToShoot [" + Drones._cachedDroneTarget.IsReadyToShoot + "]: using dronetarget [" + Drones._cachedDroneTarget.Name + "] as KillTarget");
                                    _pickPrimaryWeaponTarget = Drones._cachedDroneTarget;
                                    tempStopwatch.Stop();
                                    if (DebugConfig.DebugKillTargets) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                                    return _pickPrimaryWeaponTarget;
                                }
                            }
                        }

                        if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer || ESCache.Instance.ActiveShip.IsAssaultShip)
                        {
                            if (DebugConfig.DebugKillTargets) Log.WriteLine("PickPrimaryWeaponTarget: if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer || ESCache.Instance.ActiveShip.IsAssaultShip)");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetFleetAbyssalDeadSpace.FirstOrDefault(i => !i.IsWreck);
                            ShootNPCWeAreAlreadyEngagingIfSameName();
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        if (ESCache.Instance.Modules.Any(i => i.GroupId == (int)Group.VortonProjector))
                        {
                            if (DebugConfig.DebugKillTargets) Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetAbyssalDeadSpaceVortonProjector();");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetAbyssalDeadSpaceVortonProjector.FirstOrDefault(i => !i.IsWreck);
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        if (ESCache.Instance.ActiveShip.HasSpeedMod && NavigateOnGrid.SpeedTank && !Drones.DronesKillHighValueTargets)
                        {
                            if (DebugConfig.DebugKillTargets) Log.WriteLine("PickPrimaryWeaponTarget: PickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank();");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetAbyssalDeadSpaceWhileSpeedTank.FirstOrDefault(i => !i.IsWreck && i.IsReadyToShoot);
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs)
                        {
                            if (DebugConfig.DebugKillTargets) Log.WriteLine("PickPrimaryWeaponTarget: PickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard();");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetAbyssalDeadSpaceConstructionYard.FirstOrDefault(i => !i.IsWreck && i.IsReadyToShoot);
                            //ShootNPCWeAreAlreadyEngagingIfSameName();
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        if (DebugConfig.DebugTargetCombatants) Log.WriteLine("PickPrimaryWeaponTarget: PickPrimaryWeaponTargetAbyssalDeadSpace();");
                        //_pickPrimaryWeaponTarget = PickPrimaryWeaponTargetAbyssalDeadSpace.FirstOrDefault(i => !i.IsWreck && i.IsReadyToShoot);
                        _pickPrimaryWeaponTarget = AbyssalPotentialCombatTargets_Guns.FirstOrDefault(i => !i.IsWreck && i.IsReadyToShoot);
                        ShootNPCWeAreAlreadyEngagingIfSameName();
                        tempStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                        return _pickPrimaryWeaponTarget;
                    }

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) ||
                        ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController))
                    {
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Nestor)
                        {
                            return null;
                        }

                        if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Logistics)
                        {
                            return null;
                        }

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Augoror)
                        {
                            return null;
                        }

                        if (!ESCache.Instance.DockableLocations.All(i => i.IsOnGridWithMe) && !ESCache.Instance.Stargates.All(i => i.IsOnGridWithMe) && PotentialCombatTargets.Count > 0 && ESCache.Instance.Weapons.Count == 0 && (ESCache.Instance.ActiveShip.GroupId == (int)Group.CommandShip || ESCache.Instance.ActiveShip.GroupId == (int)Group.Battlecruiser))
                        {
                            return null;
                        }

                        //
                        // 1st or 2nd Room of Observatory Site
                        //
                        if (ESCache.Instance.AccelerationGates.Any(i => i.Name.Contains("Observatory")) || ESCache.Instance.Entities.Any(i => i.Name.Contains("Triglavian Stellar Accelerator")) || ESCache.Instance.Entities.Any(i => i.Name.Contains("Stellar Observatory")))
                        {
                            if (DebugConfig.DebugKillTargets) Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetTriglavianInvasionObeservatory();");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetTriglavianInvasionObservatory.FirstOrDefault(i => !i.IsWreck);
                            ShootNPCWeAreAlreadyEngagingIfSameName();
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        //
                        // All other Sites
                        //
                        //if (Drones.DronesKillHighValueTargets)
                        //{
                        //    if (DebugConfig.DebugTargetCombatants) Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetAbyssalDeadSpaceDronesKillHighValueTargets();");
                        //    _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetTriglavianInvasionDronesKillHighValueTargets.FirstOrDefault(i => !i.IsWreck);
                        //    ShootNPCWeAreAlreadyEngagingIfSameName();
                        //    return _pickPrimaryWeaponTarget;
                        //}
                        if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer || ESCache.Instance.ActiveShip.IsAssaultShip)
                        {
                            if (DebugConfig.DebugKillTargets || DebugConfig.DebugPreferredPrimaryWeaponTarget) Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetBasedOnTargetsForAFrigate()!.!");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForAFrigate.FirstOrDefault();
                            ShootNPCWeAreAlreadyEngagingIfSameName();
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        if (ESCache.Instance.ActiveShip.IsCruiser)
                        {
                            if (DebugConfig.DebugKillTargets || DebugConfig.DebugPreferredPrimaryWeaponTarget) Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetBasedOnTargetsForACruiser();");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForACruiser.FirstOrDefault();
                            ShootNPCWeAreAlreadyEngagingIfSameName();
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                        if (ESCache.Instance.ActiveShip.IsMarauder || ESCache.Instance.ActiveShip.IsBattleship || ESCache.Instance.ActiveShip.IsBattleCruiser)
                        {
                            if (DebugConfig.DebugKillTargets || DebugConfig.DebugPreferredPrimaryWeaponTarget) Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetBasedOnTargetsForABattleship();");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForABattleship.FirstOrDefault();
                            ShootNPCWeAreAlreadyEngagingIfSameName();
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }


                        //if (DebugConfig.DebugKillTargets || DebugConfig.DebugPreferredPrimaryWeaponTarget) Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetTriglavianInvasion();");
                        //_pickPrimaryWeaponTarget = PickPrimaryWeaponTargetTriglavianInvasion.FirstOrDefault();
                        //ShootNPCWeAreAlreadyEngagingIfSameName();
                        //tempStopwatch.Stop();
                        //if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                        //return _pickPrimaryWeaponTarget;

                        if (DebugConfig.DebugKillTargets || DebugConfig.DebugPreferredPrimaryWeaponTarget) Log.WriteLine("DebugTargetCombatants: PickPrimaryWeaponTargetBasedOnTargetsForABattleship()!!!");
                        _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForABattleship.FirstOrDefault();
                        ShootNPCWeAreAlreadyEngagingIfSameName();
                        tempStopwatch.Stop();
                        if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                        return _pickPrimaryWeaponTarget;
                    }

                    if (State.CurrentHydraState == HydraState.Combat)
                    {
                        if (ESCache.Instance.Targets.Any(i => i.Id == ESCache.Instance.EveAccount.LeaderIsAggressingTargetId))
                        {
                            _pickPrimaryWeaponTarget = ESCache.Instance.Targets.Find(i => i.Id == ESCache.Instance.EveAccount.LeaderIsAggressingTargetId);
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }
                    }
                    //
                    // Regular Behaviors: Missions / Belts / Anomalies, etc
                    //

                    if (ESCache.Instance.InMission)
                        if (ESCache.Instance.MyShipEntity != null)
                        {
                            if (ESCache.Instance.MyShipEntity.IsFrigate)
                            {
                                if (MissionSettings.MyMission != null && MissionSettings.MyMission.Name.Contains("Anomic"))
                                {
                                    if (DebugConfig.DebugKillTargets) Log.WriteLine("if (MissionSettings.MyMission != null && MissionSettings.MyMission.Name.Contains(Anomic))");
                                    _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForABurners.FirstOrDefault();
                                    tempStopwatch.Stop();
                                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                                    return _pickPrimaryWeaponTarget;
                                }

                                if (DebugConfig.DebugKillTargets) Log.WriteLine("if (ESCache.Instance.InMission) if (ESCache.Instance.MyShipEntity.IsFrigate)");
                                _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForAFrigate.FirstOrDefault();
                                tempStopwatch.Stop();
                                if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                                return _pickPrimaryWeaponTarget;
                            }

                            if (ESCache.Instance.MyShipEntity.IsCruiser)
                            {
                                if (DebugConfig.DebugKillTargets) Log.WriteLine("if (ESCache.Instance.InMission) if (ESCache.Instance.MyShipEntity.IsCruiser)");
                                _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForACruiser.FirstOrDefault();
                                tempStopwatch.Stop();
                                if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                                return _pickPrimaryWeaponTarget;
                            }

                            if (ESCache.Instance.MyShipEntity.IsBattleship)
                            {
                                if (DebugConfig.DebugKillTargets) Log.WriteLine("if (ESCache.Instance.InMission) if (ESCache.Instance.MyShipEntity.IsBattleship)");
                                _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForABattleship.FirstOrDefault();
                                ShootNPCWeAreAlreadyEngagingIfSameName();
                                tempStopwatch.Stop();
                                if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                                return _pickPrimaryWeaponTarget;
                            }

                            //
                            // Default to picking targets for a battleship sized ship
                            //
                            if (DebugConfig.DebugKillTargets) Log.WriteLine("if (ESCache.Instance.InMission) MyShipEntity class unknown");
                            _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForABattleship.FirstOrDefault();
                            ShootNPCWeAreAlreadyEngagingIfSameName();
                            tempStopwatch.Stop();
                            if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                            return _pickPrimaryWeaponTarget;
                        }

                    if (DebugConfig.DebugKillTargets) Log.WriteLine("!if (ESCache.Instance.InMission)");
                    _pickPrimaryWeaponTarget = PickPrimaryWeaponTargetBasedOnTargetsForABattleship.FirstOrDefault();
                    ShootNPCWeAreAlreadyEngagingIfSameName();
                    tempStopwatch.Stop();
                    if (DebugConfig.DebugKillTargetsPerformance) Log.WriteLine("PickPrimaryWeaponTarget Took [" + Util.ElapsedMicroSeconds(tempStopwatch) + "]");
                    return _pickPrimaryWeaponTarget;
                }

                if (DebugConfig.DebugKillTargets) Log.WriteLine("KillTarget: No Targets Yet. Waiting!.!");
                return null;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return null;
            }
        }

        public static bool ChangeCombatState(CombatState state, string reason = "")
        {
            try
            {
                switch (state)
                {
                    case CombatState.Idle:
                        State.CurrentCombatState = CombatState.Idle;
                        break;

                    case CombatState.KillTargets:
                        State.CurrentCombatState = CombatState.KillTargets;
                        break;

                    case CombatState.OutOfAmmo:
                        Log.WriteLine("OutOfAmmo: Reason [" + reason + "]");
                        State.CurrentCombatState = CombatState.OutOfAmmo;
                        break;
                }

                if (State.CurrentCombatState != state)
                {
                    if (DebugConfig.DebugCombat) Log.WriteLine("New CombatState [" + state + "]");
                    State.CurrentCombatState = state;
                    //if (wait)
                    //    _lastArmAction = DateTime.UtcNow;
                    //else
                    //    ProcessState();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void ProcessState()
        {
            try
            {
                if (DateTime.UtcNow < _lastCombatProcessState.AddMilliseconds(350))
                {
                    if (DebugConfig.DebugCombat)
                        Log.WriteLine("if (DateTime.UtcNow < _lastCombatProcessState.AddMilliseconds(350) || Logging.DebugDisableCombat)");
                    return;
                }

                _lastCombatProcessState = DateTime.UtcNow;

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

                switch (State.CurrentCombatState)
                {
                    case CombatState.KillTargets:
                        if (!KillTargets()) return;
                        break;

                    case CombatState.OutOfAmmo:
                        if (ESCache.Instance.InStation)
                        {
                            Log.WriteLine("Out of ammo. Pausing questor if in station.");
                            ControllerManager.Instance.SetPause(true);
                        }

                        break;

                    case CombatState.Idle:

                        if (ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.Entity != null && !ESCache.Instance.ActiveShip.Entity.IsCloaked &&
                            ESCache.Instance.ActiveShip.GivenName.ToLower() == CombatShipName.ToLower() && !ESCache.Instance.InWarp)
                        {
                            ChangeCombatState(CombatState.KillTargets);
                            if (DebugConfig.DebugCombat)
                                Log.WriteLine("We are in space and ActiveShip is null or Cloaked or we arent in the combatship or we are in warp");
                        }
                        break;

                    default:

                        Log.WriteLine("CurrentCombatState was not set thus ended up at default");
                        ChangeCombatState(CombatState.KillTargets);
                        break;
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void LogOrderOfKillTargets(IOrderedEnumerable<EntityCache> killTargets)
        {
            int targetnum = 0;
            Log.WriteLine("----------------[ killtargets ]------------------");
            foreach (EntityCache myKillTarget in killTargets)
            {
                targetnum++;
                Log.WriteLine(targetnum + ";" + myKillTarget.Name + ";" + Math.Round(myKillTarget.Distance / 1000, 0) + "k;IsReadyToShoot;" + myKillTarget.IsReadyToShoot + ";IsReadyToTarget;" + myKillTarget.IsReadyToTarget + ";IsTarget;" + myKillTarget.IsTarget + ";BS;" + myKillTarget.IsBattleship + ";BC;" + myKillTarget.IsBattlecruiser + ";C;" + myKillTarget.IsCruiser + ";F;" + myKillTarget.IsFrigate + ";isAttacking;" + myKillTarget.IsAttacking + ";IsTargetedBy;" + myKillTarget.IsTargetedBy + ";IsWarpScramblingMe;" + myKillTarget.IsWarpScramblingMe + ";IsNeutralizingMe;" + myKillTarget.IsNeutralizingMe + ";Health;" + myKillTarget.HealthPct + ";ShieldPct;" + myKillTarget.ShieldPct + ";ArmorPct;" + myKillTarget.ArmorPct + ";StructurePct;" + myKillTarget.StructurePct);
            }

            Log.WriteLine("----------------------------------------------");
        }

        #endregion Methods
    }
}