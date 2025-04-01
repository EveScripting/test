extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Questor.Combat
{
    public static partial class Combat
    {
        #region Fields

        public static bool GlobalAllowOverLoadOfEcm = false;
        public static bool GlobalAllowOverLoadOfSpeedMod = false;
        public static bool GlobalAllowOverLoadOfWeapons = false;

        public static bool GlobalAllowOverLoadOfWebs = false;

        #endregion Fields

        #region Properties

        public static int? GlobalEcmOverloadDamageAllowed { get; set; }

        public static int? GlobalSpeedModOverloadDamageAllowed { get; set; }

        public static int? GlobalWeaponOverloadDamageAllowed { get; set; }

        public static int? GlobalWebOverloadDamageAllowed { get; set; }

        private static bool AllowOverLoadOfEcm
        {
            get
            {
                if (MissionSettings.MissionAllowOverLoadOfEcm)
                    return true;

                if (GlobalAllowOverLoadOfEcm)
                    return true;

                return false;
            }
        }

        private static bool AllowOverLoadOfSpeedMod
        {
            get
            {
                if (MissionSettings.MissionAllowOverLoadOfSpeedMod)
                    return true;

                if (GlobalAllowOverLoadOfSpeedMod)
                    return true;

                return false;
            }
        }

        private static bool AllowOverLoadOfWeapons
        {
            get
            {
                if (MissionSettings.MissionAllowOverLoadOfWeapons)
                    return true;

                if (GlobalAllowOverLoadOfWeapons)
                    return true;

                return false;
            }
        }

        public static bool AllowOverLoadOfWebs
        {
            get
            {
                if (MissionSettings.MissionAllowOverLoadOfWebs)
                    return true;

                if (GlobalAllowOverLoadOfWebs)
                    return true;

                return false;
            }
        }

        private static int EcmOverloadDamageAllowed
        {
            get
            {
                if (MissionSettings.MissionEcmOverloadDamageAllowed != null)
                    return (int)MissionSettings.MissionEcmOverloadDamageAllowed;

                if (GlobalEcmOverloadDamageAllowed != null)
                    return (int)GlobalEcmOverloadDamageAllowed;

                return 30;
            }
        }

        public static int SpeedModOverloadDamageAllowed
        {
            get
            {
                if (MissionSettings.MissionSpeedModOverloadDamageAllowed != null)
                    return (int)MissionSettings.MissionSpeedModOverloadDamageAllowed;

                if (GlobalSpeedModOverloadDamageAllowed != null)
                    return (int)GlobalSpeedModOverloadDamageAllowed;

                return 60;
            }
        }

        public static int WeaponOverloadDamageAllowed
        {
            get
            {
                if (MissionSettings.MissionWeaponOverloadDamageAllowed != null)
                    return (int)MissionSettings.MissionWeaponOverloadDamageAllowed;

                if (GlobalWeaponOverloadDamageAllowed != null)
                    return (int)GlobalWeaponOverloadDamageAllowed;

                return 60;
            }
        }

        private static int WebOverloadDamageAllowed
        {
            get
            {
                if (MissionSettings.MissionWebOverloadDamageAllowed != null)
                    return (int)MissionSettings.MissionWebOverloadDamageAllowed;

                if (GlobalWebOverloadDamageAllowed != null)
                    return (int)GlobalWebOverloadDamageAllowed;

                return 30;
            }
        }

        #endregion Properties

        #region Methods

        public static void OverloadModules(bool allowOverLoadOfTheseModules, List<ModuleCache> modulesToTryToOverload, int moduleDamagePercentageAllowed, int targetDistanceToUnOverload = 99999, EntityCache myTarget = null)
        {
            try
            {
                //
                // for Anomic Team missions do not overheat until after we are out of warp for x seconds.
                // this allows us to settle in to shooting the correct target before overheating
                //
                if (ESCache.Instance.InMission && MissionSettings.MyMission != null && MissionSettings.MyMission.Name.Contains("Anomic Team"))
                    if (Time.Instance.LastInWarp.AddSeconds(45) > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugOverLoadWeapons) Log.WriteLine("DebugOverLoadWeapons: We are doing an Anomic Team mission and are within 60 sec of dropping out of warp. Do not yet overload.");
                        return;
                    }

                //
                // DeActivate Overload as needed
                //
                _weaponNumber = 0;
                foreach (ModuleCache overloadedModule in modulesToTryToOverload.Where(i => i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading))
                {
                    if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(overloadedModule.ItemId))
                        if (Time.Instance.LastActivatedTimeStamp[overloadedModule.ItemId].AddMilliseconds(Time.Instance.PainterDelay_milliseconds) > DateTime.UtcNow)
                            continue;

                    _weaponNumber++;

                    if (overloadedModule.DamagePercent > moduleDamagePercentageAllowed || (ESCache.Instance.InMission && MissionSettings.MyMission != null && !MissionSettings.MyMission.Name.Contains("Anomic") && ESCache.Instance.Weapons.Any(i => i.IsActive && i.ItemId == overloadedModule.ItemId)))
                    {
                        try
                        {
                            if (overloadedModule.ToggleOverload())
                            {
                                Log.WriteLine("OverLoadModules: deactivate overload: [" + overloadedModule.TypeName + "][" + _weaponNumber + "] has [" + overloadedModule.DamagePercent + "%] damage: allowing [" + moduleDamagePercentageAllowed + "]% damage deactivating overload");
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        return;
                    }

                    if (myTarget != null && myTarget.Distance != 0 && targetDistanceToUnOverload > myTarget.Distance)
                    {
                        try
                        {
                            if (overloadedModule.ToggleOverload())
                            {
                                Log.WriteLine("OverLoadModules: deactivate overload: [" + overloadedModule.TypeName + "][" + _weaponNumber + "] targetDistanceToUnOverload [" + Math.Round((double)targetDistanceToUnOverload / 1000, 0) + "k] > [" + Math.Round(myTarget.Distance / 1000, 0) + "]k from [" + myTarget.Name + "]: deactivating overload");
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

                if (!allowOverLoadOfTheseModules)
                    return;

                //
                // Activate Overload as needed
                //
                _weaponNumber = 0;
                foreach (ModuleCache moduleToOverload in modulesToTryToOverload.Where(i => !i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading && !i._module.IsBeingRepaired))
                {
                    if (Time.Instance.LastOverLoadedTimeStamp != null && Time.Instance.LastOverLoadedTimeStamp.ContainsKey(moduleToOverload.ItemId))
                        if (Time.Instance.LastOverLoadedTimeStamp[moduleToOverload.ItemId].AddMilliseconds(ESCache.Instance.RandomNumber(22000, 25000)) > DateTime.UtcNow)
                            continue;

                    _weaponNumber++;

                    //
                    //
                    //
                    if (moduleToOverload.IsActive) //and not already overloading
                    {
                        //
                        // only overload these modules if they arent active (these modules give range when overloaded)
                        //
                        if (moduleToOverload.GroupId == (int)Group.StasisWeb)
                            continue;

                        if (moduleToOverload.GroupId == (int)Group.WarpDisruptor)
                            continue;
                    }

                    //
                    // do not overload if we are outside range
                    //
                    if (myTarget != null && targetDistanceToUnOverload != 99999 && targetDistanceToUnOverload > myTarget.Distance)
                        continue;

                    if (moduleToOverload.DamagePercent >= moduleDamagePercentageAllowed)
                        continue;

                    try
                    {
                        if (moduleToOverload.ToggleOverload())
                        {
                            if (Time.Instance.LastOverLoadedTimeStamp != null)
                                Time.Instance.LastOverLoadedTimeStamp.AddOrUpdate(moduleToOverload.ItemId, DateTime.UtcNow);
                            Log.WriteLine("OverLoadModules: activate overload: [" + moduleToOverload.TypeName + "][" + _weaponNumber + "]: activating overload");
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

        private static void OverloadEcm()
        {
            try
            {
                if (MissionSettings.MissionAllowOverLoadOfEcm && !ESCache.Instance.InMission)
                    return;

                OverloadModules(AllowOverLoadOfEcm, ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.Ecm).ToList(), EcmOverloadDamageAllowed);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void OverloadWeapons()
        {
            try
            {
                if (ESCache.Instance.Weapons.Count == 0) return;

                if (MissionSettings.MissionAllowOverLoadOfWeapons && !ESCache.Instance.InMission)
                    return;

                OverloadModules(AllowOverLoadOfWeapons, ESCache.Instance.Weapons, WeaponOverloadDamageAllowed);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void OverloadWeb()
        {
            try
            {
                if (MissionSettings.MissionAllowOverLoadOfWebs)
                {
                    if (!ESCache.Instance.InMission)
                        return;

                    EntityCache webKillTarget = null;
                    if (KillTarget != null && ESCache.Instance.Modules.Any(m => m.GroupId == (int)Group.StasisWeb && m.IsActive && m.IsOverloaded))
                    {
                        webKillTarget = KillTarget;
                        OverloadModules(AllowOverLoadOfWebs, ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.StasisWeb).ToList(), WebOverloadDamageAllowed, 14000, webKillTarget);
                        return;
                    }

                    OverloadModules(AllowOverLoadOfWebs, ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.StasisWeb).ToList(), WebOverloadDamageAllowed);
                    return;
                }

                OverloadModules(AllowOverLoadOfWebs, ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.StasisWeb).ToList(), WebOverloadDamageAllowed);
                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }
}