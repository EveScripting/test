extern alias SC;

using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.IPC;
using System;
using System.Linq;
using EVESharpCore.Questor.BackgroundTasks;
using SC::SharedComponents.Extensions;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.States;
using System.Runtime.CompilerServices;

namespace EVESharpCore.Cache
{
    public class ModuleCache
    {
        #region Constructors

        public ModuleCache(DirectUIModule module)
        {
            _module = module;
        }

        #endregion Constructors

        #region Fields

        internal readonly DirectUIModule _module;

        private int ClickCountThisFrame;

        #endregion Fields

        #region Properties

        public bool AutoReload => _module.AutoReload;
        public double? CapacitorNeed => _module.CapacitorNeed ?? 0;
        public DirectItem Charge => _module.Charge;

        public string ChargeName
        {
            get
            {
                if (Charge != null)
                {
                    return Charge.TypeName;
                }

                return "None";
            }

        }
        public int ChargeQty => _module.ChargeQty;

        public double DamageHitPoints => _module.HeatDamage;
        public double DamagePercent => _module.HeatDamagePercent;

        public bool DisableAutoReload
        {
            get
            {
                if (IsActivatable && !InLimboState)
                {
                    if (_module.AutoReload)
                    {
                        if (!ESCache.Instance.OkToInteractWithEveNow)
                        {
                            if (DebugConfig.DebugInteractWithEve) Log.WriteLine("ModuleCache: DisableAutoReload: !OkToInteractWithEveNow");
                            return false;
                        }

                        if (_module.SetAutoReload(false))
                        {
                            ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                            Log.WriteLine("ModuleCache: DisableAutoReload");
                            return false;
                        }

                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        public double Duration => _module.Duration ?? 0;

        public bool EnableAutoReload
        {
            get
            {
                if (IsActivatable && !InLimboState)
                {
                    if (!_module.AutoReload)
                    {
                        if (!ESCache.Instance.OkToInteractWithEveNow)
                        {
                            if (DebugConfig.DebugInteractWithEve) Log.WriteLine("ModuleCache: DisableAutoReload: !OkToInteractWithEveNow");
                            return false;
                        }

                        if (_module.SetAutoReload(true))
                        {
                            ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                            Log.WriteLine("ModuleCache: DisableAutoReload");
                            return false;
                        }

                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        public double FallOff => _module.FallOff ?? 0;

        public double FalloffEffectiveness => _module.FalloffEffectiveness ?? 0;
        public DirectUIModule GetDirectModule => _module;

        public bool IsHighAngleWeapon
        {
            get
            {
                if (IsTurret)
                {
                    // afaik: Missiles do not have a High Angle Weapon equivalent
                    //
                    // Projectile Autocannon HAW
                    if (TypeName.Contains("Quad 800mm Repeating"))
                        return true;

                    // Hybrid Blaster HAW
                    if (TypeName.Contains("Triple Neutron Blaster"))
                        return true;

                    // Laser Pulse HAW
                    if (TypeName.Contains("Quad Mega Pulse"))
                        return true;

                    // Missile Torpedo HAW
                    if (TypeName.Contains("Rapid Torpedo Launcher"))
                        return true;

                    return false;
                }

                return false;
            }
        }

        public int GroupId => _module.GroupId;

        public double Hp => _module.Hp;

        private bool? _inLimboState { get; set; } = null;

        public bool InLimboState
        {
            get
            {
                try
                {
                    if (_inLimboState != null)
                        return _inLimboState ?? false;

                    if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    {
                        _inLimboState = true;
                        return _inLimboState ?? true;
                    }

                    if (Time.Instance.NextJumpAction > DateTime.UtcNow)
                    {
                        _inLimboState = false;
                        return _inLimboState ?? false;
                    }

                    if (Time.Instance.NextDockAction > DateTime.UtcNow)
                    {
                        _inLimboState = false;
                        return _inLimboState ?? false;
                    }

                    if (Time.Instance.NextActivateModules > DateTime.UtcNow)
                    {
                        _inLimboState = false;
                        return _inLimboState ?? false;
                    }

                    if (ESCache.Instance.InStation)
                    {
                        _inLimboState = false;
                        return _inLimboState ?? false;
                    }

                    if (!IsEnergyWeapon && Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(ItemId))
                        if (DateTime.UtcNow < Time.Instance.LastReloadedTimeStamp[ItemId].AddSeconds(2))
                        {
                            _inLimboState = true;
                            return _inLimboState ?? true;
                        }

                    if (Time.Instance.LastWeaponHasNoAmmoTimeStamp != null && Time.Instance.LastWeaponHasNoAmmoTimeStamp.ContainsKey(ItemId))
                        if (DateTime.UtcNow < Time.Instance.LastWeaponHasNoAmmoTimeStamp[ItemId].AddSeconds(2) && !IsReloadingAmmo)
                        {
                            _inLimboState = true;
                            return _inLimboState ?? true;
                        }

                    if (!IsOnline)
                    {
                        _inLimboState = true;
                        return _inLimboState ?? true;
                    }

                    if (!IsActivatable)
                    {
                        if (DebugConfig.DebugCombat) if (ESCache.Instance.Weapons.Any(i => i.ItemId == ItemId)) Log.WriteLine("InLimboState: [" + TypeName + "] if (!IsActivatable)");
                        _inLimboState = true;
                        return _inLimboState ?? true;
                    }

                    if (IsReloadingAmmo)
                    {
                        if (DebugConfig.DebugCombat) if (ESCache.Instance.Weapons.Any(i => i.ItemId == ItemId)) Log.WriteLine("InLimboState: [" + TypeName + "]if (IsReloadingAmmo)");
                        _inLimboState = true;
                        return _inLimboState ?? true;
                    }

                    if (IsDeactivating)
                    {
                        if (DebugConfig.DebugCombat) if (ESCache.Instance.Weapons.Any(i => i.ItemId == ItemId)) Log.WriteLine("InLimboState: [" + TypeName + "] if (IsDeactivating)");
                        _inLimboState = true;
                        return _inLimboState ?? true;
                    }

                    if (_module.IsCloak && _module.ReactivationDelay > 0)
                    {
                        _inLimboState = true;
                        return _inLimboState ?? true;
                    }

                    if (IsActive)
                    {
                        _inLimboState = false;
                        return _inLimboState ?? false;
                    }

                    if (WeaponNeedsAmmo &&
                        (Charge == null || ChargeQty == 0) &&
                        Time.Instance.LastWeaponHasNoAmmoTimeStamp.ContainsKey(ItemId))
                    {
                        Log.WriteLine("ActivateWeapons: deactivate: no ammo loaded? [" + TypeName + "][" + ItemId + "]");

                        AmmoType ammo = DirectUIModule.DefinedAmmoTypes.Find(a => Charge != null && a.TypeId == Charge.TypeId);

                        if (ammo == null)
                            if (DebugConfig.DebugActivateWeapons)
                                Log.WriteLine("ActivateWeapons: deactivate: ammo == null [" + TypeName + "][" + ItemId +
                                              "] someone manually loaded ammo we do not have defined as an AmmoType?");

                        //
                        // if we still have no ammo loaded and are not actively reloading after 6 seconds, refresh the timestamp so that we try to reload again
                        //
                        if (DateTime.UtcNow > Time.Instance.LastWeaponHasNoAmmoTimeStamp[ItemId].AddSeconds(6))
                            Time.Instance.LastWeaponHasNoAmmoTimeStamp[ItemId] = DateTime.UtcNow;

                        //
                        // if we still have no ammo loaded after waiting 2 seconds (above) then attempt to reload (ReloadAmmo only allows a reload every x seconds, it will not spam reload)
                        //
                        //AmmoManagementBehavior.ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWeaponsNeedReload);
                        _inLimboState = true;
                        return _inLimboState ?? true;
                    }

                    _inLimboState = false;
                    return _inLimboState ?? false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("InLimboState - Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsActivatable => _module.IsActivatable;
        public bool IsActive => _module.IsActive;

        public bool IsArmorRepairModule
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.ArmorRepairer;
                    result |= GroupId == (int)Group.AncillaryArmorBooster;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("IsEwarModule - Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsReadyToReloadAmmo
        {
            get
            {
                if (!IsOnline)
                {
                    if (DebugConfig.DebugReloadAll) Log.WriteLine("if (!IsOnline)");
                    return false;
                }

                if (InLimboState)
                {
                    if (DebugConfig.DebugReloadAll) Log.WriteLine("if (InLimboState)");
                    return false;
                }

                if (IsReloadingAmmo)
                {
                    if (DebugConfig.DebugReloadAll) Log.WriteLine("if (IsReloadingAmmo)");
                    return false;
                }

                if (!IsWeapon)
                {
                    if (DebugConfig.DebugReloadAll) Log.WriteLine("if (!IsWeapon)");
                    return false;
                }

                if (IsDeactivating)
                {
                    if (DebugConfig.DebugReloadAll) Log.WriteLine("if (IsDeactivating)");
                    return false;
                }

                if (IsActive)
                {
                    if (DebugConfig.DebugReloadAll) Log.WriteLine("if (IsActive)");
                    return false;
                }

                if (!WeaponNeedsAmmo)
                {
                    if (DebugConfig.DebugReloadAll) Log.WriteLine("if (!WeaponNeedsAmmo)");
                    return false;
                }

                if (!_module.IsMasterOrIsNotGrouped)
                {
                    if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement) Log.WriteLine("if (!_module.IsMasterOrIsNotGrouped)");
                    return false;
                }

                return true;
            }
        }

        public bool IsRemoteArmorRepairModule
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.RemoteArmorRepairer;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("IsEwarModule - Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsCivilianWeapon
        {
            get
            {
                if (TypeId == (int)TypeID.CivilianGatlingAutocannon)
                    return true;

                if (TypeId == (int)TypeID.CivilianGatlingPulseLaser)
                    return true;

                if (TypeId == (int)TypeID.CivilianGatlingRailgun)
                    return true;

                if (TypeId == (int)TypeID.CivilianLightElectronBlaster)
                    return true;

                return false;
            }
        }

        public bool IsDeactivating => _module.IsDeactivating;
        public bool IsEnergyWeapon => GroupId == (int)Group.EnergyWeapon;

        public bool IsEwarModule
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.WarpDisruptor;
                    result |= GroupId == (int)Group.StasisWeb;
                    result |= GroupId == (int)Group.TargetPainter;
                    result |= GroupId == (int)Group.TrackingDisruptor;
                    result |= GroupId == (int)Group.Neutralizer;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("IsEwarModule - Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsOreMiningModule
        {
            get
            {
                if (!IsHighSlotModule)
                    return false;

                if (TypeName.Contains("Ice "))
                    return false;

                if (GroupId == (int)Group.Miners)
                    return true;

                if (GroupId == (int)Group.StripMiners)
                    return true;

                if (GroupId == (int)Group.ModulatedStripMiners)
                    return true;

                return false;
            }
        }

        public bool IsIceMiningModule
        {
            get
            {
                if (!IsHighSlotModule)
                    return false;

                if (TypeName.Contains("Ice "))
                {
                    if (GroupId == (int)Group.Miners)
                        return true;

                    if (GroupId == (int)Group.StripMiners)
                        return true;

                    if (GroupId == (int)Group.ModulatedStripMiners)
                        return true;
                }

                return false;
            }
        }

        public bool IsHighSlotModule
        {
            get
            {
                if (GroupId == (int)Group.EnergyWeapon) return true;
                if (GroupId == (int)Group.ProjectileWeapon) return true;
                if (GroupId == (int)Group.HybridWeapon) return true;
                if (GroupId == (int)Group.PrecursorWeapon) return true;
                if (GroupId == (int)Group.VortonProjector) return true;
                if (GroupId == (int)Group.AssaultMissileLaunchers) return true;
                if (GroupId == (int)Group.CruiseMissileLaunchers) return true;
                if (GroupId == (int)Group.DefenderMissileLaunchers) return true;
                if (GroupId == (int)Group.HeavyAssaultMissileLaunchers) return true;
                if (GroupId == (int)Group.HeavyMissileLaunchers) return true;
                if (GroupId == (int)Group.LightMissileLaunchers) return true;
                if (GroupId == (int)Group.RapidHeavyMissileLaunchers) return true;
                if (GroupId == (int)Group.StandardMissileLaunchers) return true;
                if (GroupId == (int)Group.TorpedoLaunchers) return true;
                if (GroupId == (int)Group.CitadelTorpLaunchers) return true;
                if (GroupId == (int)Group.CitadelCruiseLaunchers) return true;
                if (GroupId == (int)Group.RocketLaunchers) return true;
                if (GroupId == (int)Group.Neutralizer) return true;
                if (GroupId == (int)Group.ModulatedStripMiners) return true;
                if (GroupId == (int)Group.RemoteArmorRepairer) return true;
                if (GroupId == (int)Group.RemoteHullRepairer) return true;
                if (GroupId == (int)Group.RemoteShieldRepairer) return true;
                if (GroupId == (int)Group.StripMiners) return true;
                if (GroupId == (int)Group.Miners) return true;
                if (GroupId == (int)Group.GasCloudHarvester) return true;
                if (GroupId == (int)Group.Salvager) return true;
                if (GroupId == (int)Group.TractorBeam) return true;
                if (GroupId == (int)Group.DroneControlRange) return true;
                return false;
            }
        }

        public bool IsInLimboState => InLimboState;

        public bool IsLowSlotModule
        {
            get
            {
                if (GroupId == (int)Group.ArmorRepairer) return true;
                if (GroupId == (int)Group.ArmorHardeners) return true;
                if (GroupId == (int)Group.ArmorResistanceShiftHardener) return true;
                if (GroupId == (int)Group.DamageControl) return true;
                if (GroupId == (int)Group.DroneDamageAmplifier) return true;
                //if (GroupId == (int)GroupID.TrackingLink) return true;
                return false;
            }
        }

        public bool IsMidSlotModule
        {
            get
            {
                if (GroupId == (int)Group.ShieldBoosters) return true;
                if (GroupId == (int)Group.ShieldHardeners) return true;
                if (GroupId == (int)Group.AncillaryShieldBooster) return true;
                if (GroupId == (int)Group.TrackingDisruptor) return true;
                if (GroupId == (int)Group.WarpDisruptor) return true;
                if (GroupId == (int)Group.StasisWeb) return true;
                if (GroupId == (int)Group.StasisGrappler) return true;
                if (GroupId == (int)Group.Afterburner) return true;
                if (GroupId == (int)Group.CapacitorInjector) return true;
                if (GroupId == (int)Group.SensorBooster) return true;
                if (GroupId == (int)Group.MissileGuidanceComputer) return true;
                if (GroupId == (int)Group.SensorDampener) return true;
                if (GroupId == (int)Group.TargetPainter) return true;
                if (GroupId == (int)Group.TrackingComputer) return true;
                if (GroupId == (int)Group.DroneTrackingLink) return true;
                return false;
            }
        }

        public bool IsMissileLauncher => _module.IsMissileLauncher;

        public bool IsMicroWarpDrive
        {
            get
            {
                if (GroupId == (int)Group.Afterburner)
                {
                    if (TypeName.Contains("5"))
                        return true;

                    if (TypeName.Contains("50"))
                        return true;

                    if (TypeName.Contains("500"))
                        return true;

                    return false;
                }

                return false;
            }
        }


        public bool IsOnline => _module.IsOnline;
        public bool IsOverloaded => _module.IsOverloaded;
        public bool IsPendingOverloading => _module.IsPendingOverloading;
        public bool IsPendingStopOverloading => _module.IsPendingStopOverloading;

        public bool? _isReloadingAmmo;

        public bool IsReloadingAmmo
        {
            get
            {
                if (_isReloadingAmmo != null)
                    return _isReloadingAmmo ?? false;

                if (!IsEnergyWeapon && Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(ItemId))
                    if (DateTime.UtcNow < Time.Instance.LastReloadedTimeStamp[ItemId].AddSeconds(1))
                    {
                        _isReloadingAmmo = true;
                        return _isReloadingAmmo ?? true;
                    }

                return _module.IsReloadingAmmo;
            }
        }

        public bool IsShieldRepairModule
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.ShieldBoosters;
                    result |= GroupId == (int)Group.AncillaryShieldBooster;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsRemoteShieldRepairModule
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.RemoteShieldRepairer;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsRemoteEnergyTransferModule
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.RemoteEnergyTransfer;
                    return result;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsArmorHardener
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.ArmorHardeners;
                    result |= GroupId == (int)Group.ArmorResistanceShiftHardener;
                    return result;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception: [" + ex + "]");
                    return false;
                }
            }
        }

        public bool OverloadDesirable
        {
            get
            {
                if (IsModuleTooDamagedToOverloadAgain)
                {
                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: IsModuleTooDamagedToOverloadAgain [" + IsModuleTooDamagedToOverloadAgain + "]");
                    return false;
                }

                if (_module.IsAfterburner || _module.IsMicroWarpDrive)
                {
                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: Module [" + TypeName + "] if (_module.IsAfterburner || _module.IsMicroWarpDrive)");
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: Module [" + TypeName + "] if (ESCache.Instance.InAbyssalDeadspace)");
                        //
                        // todo: add health monitoring too?
                        //

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                        {
                            if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: Module [" + TypeName + "] if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordBSSpawn)");
                            if (Combat.PotentialCombatTargets.Any() && Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                            {
                                if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: Module [" + TypeName + "] Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)) return true;");
                                return true;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                        {
                            if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: Module [" + TypeName + "] if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)");

                            if (Combat.PotentialCombatTargets.Any() && Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 10)
                            {
                                if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)");
                                return true;
                            }

                            return false;
                        }

                        return false;
                    }

                    return false;
                }

                if (IsShieldRepairModule && Defense.OverloadRepairModulesAtThisPerc > ESCache.Instance.ActiveShip.ShieldPercentage)
                {
                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: IsShieldRepairModule [" + IsShieldRepairModule + "] ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] greaterthan OverloadRepairModulesAtThisPerc [" + Defense.OverloadRepairModulesAtThisPerc + "] return true");
                    return true;
                }

                if (IsShieldHardener && Defense.OverloadHardenerModulesAtThisPerc > ESCache.Instance.ActiveShip.ShieldPercentage)
                {
                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: IsShieldHardener [" + IsShieldHardener + "] ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] greaterthan OverloadHardenerModulesAtThisPerc [" + Defense.OverloadHardenerModulesAtThisPerc + "] return true");
                    return true;
                }

                if (IsArmorRepairModule && Defense.ToggleOffOverloadRepairModulesAtThisPerc > ESCache.Instance.ActiveShip.ArmorPercentage)
                {
                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: IsArmorRepairModule [" + IsArmorRepairModule + "] ArmorPercentage [" + ESCache.Instance.ActiveShip.ArmorPercentage + "] greaterthan OverloadRepairModulesAtThisPerc [" + Defense.OverloadRepairModulesAtThisPerc + "] return true");
                    return true;
                }

                if (IsArmorHardener && Defense.OverloadHardenerModulesAtThisPerc > ESCache.Instance.ActiveShip.ArmorPercentage)
                {
                    if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("OverloadDesirable: IsArmorHardener [" + IsArmorHardener + "] ArmorPercentage [" + ESCache.Instance.ActiveShip.ArmorPercentage + "] greaterthan OverloadHardenerModulesAtThisPerc [" + Defense.OverloadHardenerModulesAtThisPerc + "] return true");
                    return true;
                }

                return false;
            }
        }

        public bool UnOverloadDesirable
        {
            get
            {
                //if ((IsShieldRepairModule || IsShieldHardener || IsArmorRepairModule || IsArmorHardener) && ESCache.Instance.InMission && MissionSettings.MyMission != null && !MissionSettings.MyMission.Name.Contains("Anomic"))
                //    return false;

                if (IsModuleTooDamagedToOverloadAgain)
                    return true;

                if ((_module.IsAfterburner || _module.IsMicroWarpDrive) && (!Combat.PotentialCombatTargets.Any() || 2 >= Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship)))
                {
                    //if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if ((_module.IsAfterburner || _module.IsMicroWarpDrive) && (!Combat.PotentialCombatTargets.Any() || 2 > Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship)))");
                    return true;
                }

                if (IsShieldHardener && ESCache.Instance.ActiveShip.ShieldPercentage > Defense.ToggleOffOverloadHardenerModulesAtThisPerc)
                {
                    //if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.ActiveShip.ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] > OverloadRepairModulesAtThisPerc [" + Defense.OverloadRepairModulesAtThisPerc + "])");
                    return true;
                }

                if (IsShieldRepairModule && ESCache.Instance.ActiveShip.ShieldPercentage > Defense.ToggleOffOverloadRepairModulesAtThisPerc)
                {
                    //if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.ActiveShip.ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] > OverloadRepairModulesAtThisPerc [" + Defense.OverloadRepairModulesAtThisPerc + "])");
                    return true;
                }

                if (IsArmorHardener && ESCache.Instance.ActiveShip.ArmorPercentage > Defense.ToggleOffOverloadHardenerModulesAtThisPerc)
                {
                    //if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.ActiveShip.ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] > OverloadRepairModulesAtThisPerc [" + Defense.OverloadRepairModulesAtThisPerc + "])");
                    return true;
                }

                if (IsArmorRepairModule && ESCache.Instance.ActiveShip.ArmorPercentage > Defense.ToggleOffOverloadRepairModulesAtThisPerc)
                {
                    //if (DebugConfig.DebugOverLoadModules || DebugConfig.DebugOverLoadReps) Log.WriteLine("if (ESCache.Instance.ActiveShip.ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] > OverloadRepairModulesAtThisPerc [" + Defense.OverloadRepairModulesAtThisPerc + "])");
                    return true;
                }

                return false;
            }
        }


        public bool IsModuleTooDamagedToOverloadAgain
        {
            get
            {
                try
                {
                    if (IsShieldRepairModule || IsArmorRepairModule)
                    {
                        if (DamagePercent > Defense.GlobalRepsOverloadDamageAllowed)
                        {
                            if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log.WriteLine("IsModuleTooDamagedToOverloadAgain: [" + TypeName + "] IsShieldRepairModule [" + IsShieldRepairModule + "] IsArmorRepairModule [" + IsArmorRepairModule + "] DamagePercent [" + DamagePercent + "] greaterthan  GlobalRepsOverloadDamageAllowed [" + Defense.GlobalRepsOverloadDamageAllowed + "] return true");
                            return true;
                        }

                        if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log.WriteLine("IsModuleTooDamagedToOverloadAgain: [" + TypeName + "] IsShieldRepairModule [" + IsShieldRepairModule + "] IsArmorRepairModule [" + IsArmorRepairModule + "] DamagePercent [" + DamagePercent + "] lessthan  GlobalRepsOverloadDamageAllowed [" + Defense.GlobalRepsOverloadDamageAllowed + "] return false");
                        return false;
                    }

                    if (IsShieldHardener || IsArmorHardener)
                    {
                        if (DamagePercent > Defense.GlobalRepsOverloadDamageAllowed)
                        {
                            if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log.WriteLine("IsModuleTooDamagedToOverloadAgain: [" + TypeName + "] IsShieldRepairModule [" + IsShieldRepairModule + "] IsArmorRepairModule [" + IsArmorRepairModule + "] DamagePercent [" + DamagePercent + "] greaterthan  GlobalRepsOverloadDamageAllowed [" + Defense.GlobalRepsOverloadDamageAllowed + "] return true");
                            return true;
                        }

                        if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log.WriteLine("IsModuleTooDamagedToOverloadAgain: [" + TypeName + "] IsShieldRepairModule [" + IsShieldRepairModule + "] IsArmorRepairModule [" + IsArmorRepairModule + "] DamagePercent [" + DamagePercent + "] lessthan  GlobalRepsOverloadDamageAllowed [" + Defense.GlobalRepsOverloadDamageAllowed + "] return false");
                        return false;
                    }

                    if (_module.IsAfterburner || _module.IsMicroWarpDrive)
                    {
                        if (DamagePercent > Combat.SpeedModOverloadDamageAllowed)
                        {
                            if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log.WriteLine("IsModuleTooDamagedToOverloadAgain: [" + TypeName + "] IsShieldRepairModule [" + IsShieldRepairModule + "] IsArmorRepairModule [" + IsArmorRepairModule + "] DamagePercent [" + DamagePercent + "] greaterthan  GlobalRepsOverloadDamageAllowed [" + Defense.GlobalRepsOverloadDamageAllowed + "] return true");
                            return true;
                        }

                        if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log.WriteLine("IsModuleTooDamagedToOverloadAgain: [" + TypeName + "] IsShieldRepairModule [" + IsShieldRepairModule + "] IsArmorRepairModule [" + IsArmorRepairModule + "] DamagePercent [" + DamagePercent + "] lessthan  GlobalRepsOverloadDamageAllowed [" + Defense.GlobalRepsOverloadDamageAllowed + "] return false");
                        return false;
                    }

                    if (DamagePercent > 75)
                    {
                        if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log.WriteLine("IsModuleTooDamagedToOverloadAgain: [" + TypeName + "] IsShieldRepairModule [" + IsShieldRepairModule + "] IsArmorRepairModule [" + IsArmorRepairModule + "] DamagePercent [" + DamagePercent + "] lessthan  GlobalRepsOverloadDamageAllowed [" + Defense.GlobalRepsOverloadDamageAllowed + "] return true");
                        return true;
                    }

                    if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log.WriteLine("IsModuleTooDamagedToOverloadAgain: [" + TypeName + "] IsShieldRepairModule [" + IsShieldRepairModule + "] IsArmorRepairModule [" + IsArmorRepairModule + "] DamagePercent [" + DamagePercent + "] lessthan  GlobalRepsOverloadDamageAllowed [" + Defense.GlobalRepsOverloadDamageAllowed + "] return false");
                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public bool IsShieldHardener
        {
            get
            {
                try
                {
                    bool result = false;
                    result |= GroupId == (int)Group.ShieldHardeners;
                    return result;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception: [" + ex + "]");
                    return false;
                }
            }
        }

        public bool IsTurret => _module.IsTurret;

        private bool? _isWeapon = null;

        public bool IsWeapon
        {
            get
            {
                if (_isWeapon != null)
                    return _isWeapon ?? false;

                Tuple<long, string> tupleToFind = new Tuple<long, string>(ItemId, nameof(IsWeapon));
                if (ESCache.Instance.DictionaryCachedPerPocket.ContainsKey(tupleToFind))
                {
                    _isWeapon = (bool)ESCache.Instance.DictionaryCachedPerPocket[tupleToFind];
                    return _isWeapon ?? false;
                }

                if (_isWeapon == null && IsMidSlotModule)
                {
                    _isWeapon = false;
                }

                if (_isWeapon == null && IsLowSlotModule)
                {
                    _isWeapon = false;
                }

                if (_isWeapon == null)
                {
                    switch (TypeId)
                    {
                        case (int)TypeID.CivilianGatlingAutocannon:
                        case (int)TypeID.CivilianGatlingPulseLaser:
                        case (int)TypeID.CivilianGatlingRailgun:
                        case (int)TypeID.CivilianLightElectronBlaster:
                        {
                            _isWeapon = true;
                            break;
                        }
                    }

                    switch (GroupId)
                    {
                        case (int)Group.ProjectileWeapon:
                        case (int)Group.EnergyWeapon:
                        case (int)Group.HybridWeapon:
                        case (int)Group.CruiseMissileLaunchers:
                        case (int)Group.RocketLaunchers:
                        case (int)Group.StandardMissileLaunchers:
                        case (int)Group.TorpedoLaunchers:
                        case (int)Group.AssaultMissileLaunchers:
                        case (int)Group.LightMissileLaunchers:
                        case (int)Group.CitadelCruiseLaunchers:
                        case (int)Group.CitadelTorpLaunchers:
                        case (int)Group.RapidHeavyMissileLaunchers:
                        case (int)Group.RapidLightMissileLaunchers:
                        case (int)Group.HeavyMissileLaunchers:
                        case (int)Group.HeavyAssaultMissileLaunchers:
                        case (int)Group.PrecursorWeapon:
                        case (int)Group.VortonProjector:
                        {
                            _isWeapon = true;
                            break;
                        }
                    }
                }

                Tuple<long, string> tupleToAdd = new Tuple<long, string>(ItemId, nameof(IsWeapon));
                if (_isWeapon == null) _isWeapon = false;
                ESCache.Instance.DictionaryCachedPerPocket.AddOrUpdate(tupleToAdd, _isWeapon);
                return _isWeapon ?? false;
            }
        }

        public long ItemId => _module.ItemId;

        public long LastTargetId
        {
            get
            {
                if (ESCache.Instance.LastModuleTargetIDs.ContainsKey(ItemId))
                    return ESCache.Instance.LastModuleTargetIDs[ItemId];

                return -1;
            }
        }

        public int MaxCharges => _module.MaxCharges;

        public double MaxRange
        {
            get
            {
                try
                {
                    double? _maxRange = null;
                    if (_maxRange == null || _maxRange == 0)
                    {
                        if (_module.GroupId == (int)Group.RemoteArmorRepairer || _module.GroupId == (int)Group.RemoteShieldRepairer ||
                            _module.GroupId == (int)Group.RemoteHullRepairer)
                            return Combat.RemoteRepairDistance;

                        if (_module.GroupId == (int)Group.NOS || _module.GroupId == (int)Group.Neutralizer)
                            return Combat.NosDistance;

                        if (_module.IsTurret)
                        {
                            //if (requiresAmmo)
                            if (Charge != null)
                            {
                                if (GroupId == (int)Group.PrecursorWeapon)
                                {
                                    if (DebugConfig.DebugReloadAll) Log.WriteLine("MaxRange: _module.OptimalRange;");
                                    return (double)_module.OptimalRange;
                                }
                            }
                        }

                        if (!_module.IsMissileLauncher)
                            return ChargeRange;

                        if (_module.OptimalRange != null && _module.OptimalRange > 0)
                            return (double)_module.OptimalRange;

                        return 0;
                    }

                    return (double)_maxRange;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [ " + ex + " ]");
                }

                return 0;
            }
        }

        public double ChargeRange => _module.ChargeRange;

        public bool OnlineModule => _module.OnlineModule();
        public double OptimalRange => _module.OptimalRange ?? 0;
        public double PowerTransferRange => _module.PowerTransferRange ?? 0;
        public long TargetId => _module.TargetId ?? -1;

        private DirectEntity _targetEntity;

        public DirectEntity TargetEntity
        {
            get
            {
                if (_targetEntity != null)
                    return _targetEntity;

                if (ESCache.Instance.Entities.Any(i => i.Id == TargetId))
                {
                    _targetEntity = ESCache.Instance.Entities.FirstOrDefault(i => i.Id == TargetId)._directEntity;
                    return _targetEntity ?? null;
                }

                return _targetEntity;
            }
        }

        public string TargetEntityName
        {
            get
            {
                if (TargetEntity != null)
                    return TargetEntity.TypeName;

                return "None";
            }
        }
        public double TrackingSpeed => _module.TrackingSpeed ?? 0;
        public int TypeId => _module.TypeId;

        public string TypeName => _module.TypeName;

        #endregion Properties

        /**
        public bool IsReloadingAmmo
        {
            get
            {
                int reloadDelayToUseForThisWeapon;
                if (IsEnergyWeapon)
                    reloadDelayToUseForThisWeapon = 1;
                else
                    reloadDelayToUseForThisWeapon = Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;

                if (Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(ItemId))
                    if (DateTime.UtcNow < Time.Instance.LastReloadedTimeStamp[ItemId].AddSeconds(reloadDelayToUseForThisWeapon))
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("TypeName: [" + _module.TypeName + "] This module is likely still reloading! Last reload was [" +
                                          Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastReloadedTimeStamp[ItemId]).TotalSeconds, 0) + "sec ago]");
                        return true;
                    }

                return false;
            }
        }
        **/
        /**
        public bool IsChangingAmmo
        {
            get
            {
                int reloadDelayToUseForThisWeapon;
                if (IsEnergyWeapon)
                    reloadDelayToUseForThisWeapon = 1;
                else
                    reloadDelayToUseForThisWeapon = Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;

                if (Time.Instance.LastChangedAmmoTimeStamp != null && Time.Instance.LastChangedAmmoTimeStamp.ContainsKey(ItemId))
                    if (DateTime.UtcNow < Time.Instance.LastChangedAmmoTimeStamp[ItemId].AddSeconds(reloadDelayToUseForThisWeapon))
                        return true;

                return false;
            }
        }
        **/

        #region Methods

        public bool Activate(EntityCache target)
        {
            try
            {
                if (IsActive)
                {
                    Log.WriteLine("Activate: Module [" + TypeName + "][" + ItemId + "] is already active. waiting...");
                    return false;
                }

                if (InLimboState) // || ActivateCountThisFrame > 0)
                {
                    if (DebugConfig.DebugDefense || DebugConfig.DebugActivateWeapons)
                        Log.WriteLine("if (InLimboState || ClickCountThisFrame > 0)");
                    return false;
                }

                if (!target.IsTarget)
                {
                    if (DebugConfig.DebugActivateWeapons)
                        Log.WriteLine("if (!target.IsTarget)");
                    return false;
                }

                if (IsEwarModule && target.IsEwarImmune)
                {
                    if (DebugConfig.DebugDefense)
                        Log.WriteLine("if (IsEwarModule && target.IsEwarImmune)");
                    return false;
                }

                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(ItemId))
                {
                    if (DateTime.UtcNow < Time.Instance.LastActivatedTimeStamp[ItemId].AddMilliseconds(Settings.Instance.EnforcedDelayBetweenModuleClicks))
                    {
                        if (DebugConfig.DebugDefense)
                            Log.WriteLine(
                                "if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(Settings.Instance.EnforcedDelayBetweenModuleClicks))");
                        return false;
                    }

                    if (_module.Duration != null)
                    {
                        double CycleTime = (double)_module.Duration + 500;
                        if (DateTime.UtcNow < Time.Instance.LastActivatedTimeStamp[ItemId].AddMilliseconds(CycleTime))
                        {
                            if (DebugConfig.DebugDefense)
                                Log.WriteLine("if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(CycleTime))");
                            return false;
                        }
                    }
                }

                if (ESCache.Instance.Weapons.Any(i => i.ItemId == ItemId))
                    if (ChargeQty == 0 && WeaponNeedsAmmo)
                    {
                        Log.WriteLine("Activate: Weapon [" + TypeName + "][" + ItemId + "] needs ammo but currently has none. waiting...");
                        return false;
                    }

                if (!target.IsValid)
                    return false;

                if (!IsEnergyWeapon && Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(ItemId))
                    if (DateTime.UtcNow < Time.Instance.LastReloadedTimeStamp[ItemId].AddSeconds(Time.Instance.ReloadWeaponDelayBeforeUsable_seconds))
                    {
                        Log.WriteLine("TypeName: [" + _module.TypeName + "] This module is likely still reloading! aborting activating this module.");
                        return false;
                    }

                if (!IsEnergyWeapon && Time.Instance.LastChangedAmmoTimeStamp != null && Time.Instance.LastChangedAmmoTimeStamp.ContainsKey(ItemId))
                    if (DateTime.UtcNow < Time.Instance.LastChangedAmmoTimeStamp[ItemId].AddSeconds(Time.Instance.ReloadWeaponDelayBeforeUsable_seconds))
                    {
                        Log.WriteLine("TypeName: [" + _module.TypeName + "] This module is likely still changing ammo! aborting activating this module.");
                        return false;
                    }

                if (!target.IsTarget)
                {
                    if (ESCache.Instance.InMission && MissionSettings.MyMission != null && MissionSettings.MyMission.Name.ToLower().Contains("Anomic".ToLower()))
                    {
                        if (!target.IsTargeting)
                        {
                            if (target.LockTarget("Activate"))
                            {
                                Log.WriteLine("Target [" + target.Name + "][" + Math.Round(target.Distance / 1000, 2) + "]IsTargeting[" + target.IsTargeting +
                                              "] was not locked, aborting activating module and attempting to lock this target.");
                                return false;
                            }

                            return false;
                        }

                        return false;
                    }

                    Log.WriteLine("Target [" + target.Name + "][" + Math.Round(target.Distance / 1000, 2) + "]IsTargeting[" + target.IsTargeting +
                                  "] was not locked, aborting activating module as we cant activate a module on something that is not locked!");
                    return false;
                }

                if (target.IsEwarImmune && IsEwarModule)
                {
                    Log.WriteLine("Target [" + target.Name + "][" + Math.Round(target.Distance / 1000, 2) + "]IsEwarImmune[" + target.IsEwarImmune +
                                  "] is EWar Immune and Module [" + _module.TypeName + "] isEwarModule [" + IsEwarModule + "]");
                    return false;
                }

                if (IsWeapon && AmmoManagementBehavior.TryingToChangeOrReloadAmmo)
                {
                    Log.WriteLine("AmmoManagementBehavior.TryingToChangeOrReloadAmmo [" + AmmoManagementBehavior.TryingToChangeOrReloadAmmo + "] CurrentAmmoManagementBehaviorState [" + State.CurrentAmmoManagementBehaviorState + "]");
                    return false;
                }

                if (!_module.Activate(target.Id))
                {
                    Log.WriteLine("Attempt to activate [" + _module.TypeName + "] on [" + target.Name + "] at [" + Math.Round(target.Distance / 1000, 0) + "k] failed. if (!_module.Activate(target.Id))");
                    return false;
                }

                Time.Instance.LastActivatedTimeStamp[ItemId] = DateTime.UtcNow;
                ESCache.Instance.LastModuleTargetIDs[ItemId] = target.Id;
                return true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Activate - Exception: [" + exception + "]");
                return false;
            }
        }

        public bool ChangeAmmo(DirectItem charge, int weaponNumber = 1, double Range = 0, EntityCache entity = null)
        {
            if (!ESCache.Instance.InSpace)
                return false;

            if (!IsOnline)
                return true;

            if (!_module.CanBeReloaded)
                return false;

            if (!IsReloadingAmmo)
            {
                if (!InLimboState)
                {
                    if (_module.Charge == null || _module.Charge.TypeId != charge.TypeId)
                    {
                        if (!ESCache.Instance.OkToInteractWithEveNow)
                        {
                            if (DebugConfig.DebugInteractWithEve) Log.WriteLine("ModuleCache: ChangeAmmo: !OkToInteractWithEveNow");
                            return false;
                        }

                        if (!IsEnergyWeapon && Time.Instance.LastChangedAmmoTimeStamp != null && Time.Instance.LastChangedAmmoTimeStamp.ContainsKey(ItemId))
                        {
                            if (Time.Instance.LastChangedAmmoTimeStamp[ItemId].AddSeconds(25) > DateTime.UtcNow)
                            {
                                if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement) Log.WriteLine("ModuleCache: ChangeAmmo: We have changed ammo recently: ignoring request to change ammo again until 65 seconds have passed since the last ammo change");
                                return true;
                            }
                        }

                        if (!IsEnergyWeapon && Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(ItemId))
                        {
                            int ReloadDelay = 25;
                            if (IsEnergyWeapon) ReloadDelay = 1;

                            if (Time.Instance.LastReloadedTimeStamp[ItemId].AddSeconds(ReloadDelay) > DateTime.UtcNow)
                            {
                                if (DebugConfig.DebugReloadAll || DebugConfig.DebugAmmoManagement) Log.WriteLine("ModuleCache: ChangeAmmo: We have reloaded ammo recently: ignoring request to change ammo again until 65 seconds have passed since the last ammo change");
                                return true;
                            }
                        }

                        if (_module.ChangeAmmo(charge))
                        {
                            ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                            Time.Instance.LastChangedAmmoTimeStamp[ItemId] = DateTime.UtcNow;

                            if (Time.Instance.ReloadTimePerModule.ContainsKey(ItemId))
                            {
                                Time.Instance.ReloadTimePerModule[ItemId] += Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                            }
                            else
                            {
                                Time.Instance.ReloadTimePerModule[ItemId] = Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                            }

                            return true;
                        }

                        return false;
                    }

                    Log.WriteLine("[" + weaponNumber + "][" + _module.TypeName + "] already has [" + charge.TypeName + "] TypeId [" + charge.TypeId + "] loaded.");
                    return true;
                }

                Log.WriteLine("[" + weaponNumber + "][" + _module.TypeName + "] is currently in a limbo state, waiting");
                return false;
            }

            Log.WriteLine("[" + weaponNumber + "][" + _module.TypeName + "] is already reloading, waiting");
            return false;
        }

        public bool Click()
        {
            try
            {
                if (InLimboState && !_module.IsCloak)
                {
                    if (DebugConfig.DebugClick || DebugConfig.DebugDefense)
                        Log.WriteLine("if (InLimboState || ClickCountThisFrame > 0)");
                    return false;
                }

                if (Time.Instance.LastClickedTimeStamp != null && Time.Instance.LastClickedTimeStamp.ContainsKey(ItemId))
                {
                    if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(Settings.Instance.EnforcedDelayBetweenModuleClicks))
                    {
                        if (DebugConfig.DebugClick || DebugConfig.DebugDefense)
                            Log.WriteLine(
                                "if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(Settings.Instance.EnforcedDelayBetweenModuleClicks))");
                        return false;
                    }

                    if (_module.Duration != null)
                    {
                        double CycleTime = (double)_module.Duration + 500;
                        if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(CycleTime))
                        {
                            if (DebugConfig.DebugClick || DebugConfig.DebugDefense)
                                Log.WriteLine("if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(CycleTime))");
                            return false;
                        }
                    }
                }

                if (IsActivatable && ESCache.Instance.ActiveShip.Capacitor > CapacitorNeed)
                {
                    if (!IsActive)
                    {
                        //if (!ESCache.Instance.OkToInteractWithEveNow && !IsCloak)
                        //{
                        //    //if (DebugConfig.DebugInteractWithEve)
                        //    Log.WriteLine("ModuleCache: Click [" + TypeName + "] IsActive [" + IsActive + "] !OkToInteractWithEveNow");
                        //    return false;
                        //}

                        if (IsWeapon && AmmoManagementBehavior.TryingToChangeOrReloadAmmo)
                        {
                            Log.WriteLine("AmmoManagementBehavior.TryingToChangeOrReloadAmmo [" + AmmoManagementBehavior.TryingToChangeOrReloadAmmo + "] CurrentAmmoManagementBehaviorState [" + State.CurrentAmmoManagementBehaviorState + "]");
                            return false;
                        }

                        if (_module.Click())
                        {
                            ClickCountThisFrame++;
                            if (DebugConfig.DebugClick) Log.WriteLine("Module [" + TypeName + "][" + ItemId + "] Clicked to Activate");
                            ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                            if (Time.Instance.LastClickedTimeStamp != null) Time.Instance.LastActivatedTimeStamp[ItemId] = DateTime.UtcNow;
                            return true;
                        }

                        return false;
                    }

                    if (IsActive)
                    {
                        if (_module.Click())
                        {
                            if (DebugConfig.DebugClick) Log.WriteLine("Module [" + TypeName + "][" + ItemId + "] Clicked to Deactivate");
                            ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                            if (Time.Instance.LastClickedTimeStamp != null) Time.Instance.LastClickedTimeStamp[ItemId] = DateTime.UtcNow;
                            return true;
                        }

                        return false;
                    }

                    if (Time.Instance.LastClickedTimeStamp != null) Time.Instance.LastClickedTimeStamp[ItemId] = DateTime.UtcNow;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("ModuleCache.Click - Exception: [" + exception + "]");
                return false;
            }
        }

        public bool ReloadAmmo(DirectItem charge, int weaponNumber, double Range)
        {
            if (!IsReloadingAmmo)
            {
                if (!InLimboState)
                {
                    if (!ESCache.Instance.OkToInteractWithEveNow)
                    {
                        if (DebugConfig.DebugInteractWithEve) Log.WriteLine("ModuleCache: ReloadAmmo: !OkToInteractWithEveNow");
                        return false;
                    }

                    if (IsCivilianWeapon) return true;

                    if (_module.ReloadAmmo(charge))
                    {
                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                        Log.WriteLine("Reloading [" + weaponNumber + "] [" + _module.TypeName + "] with [" + charge.TypeName + "][" +
                                      Math.Round(Range / 1000, 0) + "]");
                        Time.Instance.LastReloadedTimeStamp[ItemId] = DateTime.UtcNow;
                        if (Time.Instance.ReloadTimePerModule.ContainsKey(ItemId))
                        {
                            Time.Instance.ReloadTimePerModule[ItemId] += Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                        }
                        else
                        {
                            Time.Instance.ReloadTimePerModule[ItemId] = Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                        }

                        return true;
                    }

                    return true;
                }

                Log.WriteLine("[" + weaponNumber + "][" + _module.TypeName + "] is currently in a limbo state, waiting");
                return false;
            }

            Log.WriteLine("[" + weaponNumber + "][" + _module.TypeName + "] is already reloading, waiting");
            return false;
        }

        public bool ToggleOverload()
        {
            if (_module.IsPassiveModule)
                return true;

            if (Time.Instance.LastOverLoadedTimeStamp != null)
            {
                if (Time.Instance.LastOverLoadedTimeStamp.ContainsKey(ItemId))
                {
                    if (Time.Instance.LastOverLoadedTimeStamp[ItemId].AddMilliseconds(1200) > DateTime.UtcNow)
                    {
                        return false;
                    }

                    Time.Instance.LastOverLoadedTimeStamp[ItemId] = DateTime.UtcNow;
                }
                else Time.Instance.LastOverLoadedTimeStamp.AddOrUpdate(ItemId, DateTime.UtcNow);
            }

            return _module.ToggleOverload();
        }

        private bool? _weaponNeedsAmmo = null;

        public bool WeaponNeedsAmmo
        {
            get
            {
                try
                {
                    if (_weaponNeedsAmmo != null)
                        return _weaponNeedsAmmo ?? true;

                    if (TypeId == (int)TypeID.CivilianGatlingAutocannon)
                    {
                        _weaponNeedsAmmo = false;
                        return _weaponNeedsAmmo ?? false;
                    }

                    if (TypeId == (int)TypeID.CivilianGatlingPulseLaser)
                    {
                        _weaponNeedsAmmo = false;
                        return _weaponNeedsAmmo ?? false;
                    }

                    if (TypeId == (int)TypeID.CivilianGatlingRailgun)
                    {
                        _weaponNeedsAmmo = false;
                        return _weaponNeedsAmmo ?? false;
                    }

                    if (TypeId == (int)TypeID.CivilianLightElectronBlaster)
                    {
                        _weaponNeedsAmmo = false;
                        return _weaponNeedsAmmo ?? false;
                    }

                    //
                    // only weapons require ammo, non-weapons (target painters, damps, ecm, etc) may take scripts but they do not require them.
                    //
                    if (ESCache.Instance.Weapons.Any(i => i.TypeId == TypeId))
                    {
                        _weaponNeedsAmmo = true;
                        return _weaponNeedsAmmo ?? true;
                    }

                    _weaponNeedsAmmo = false;
                    return _weaponNeedsAmmo ?? false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool WeaponNeedsToBeReloadedNow
        {
            get
            {
                if (!IsWeapon) return false;
                if (!IsOnline) return false;
                if (IsInLimboState) return false;
                if (IsCivilianWeapon) return false;
                if (IsEnergyWeapon && ChargeQty > 0) return false;

                //
                // If these conditions are true... return true
                //
                if (Combat.MinimumAmmoCharges >= ChargeQty)
                    return true;

                if (ChargeQty == 0)
                    return true;

                if (Charge == null)
                    return true;

                return false;
            }
        }

        #endregion Methods
    }
}