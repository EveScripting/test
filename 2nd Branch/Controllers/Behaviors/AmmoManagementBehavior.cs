extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.EVE;
using System.Diagnostics;
using SC::SharedComponents.EVE.ClientSettings;
using System.Drawing;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace EVESharpCore.Questor.Behaviors
{
    public class AmmoManagementBehavior
    {
        #region Constructors

        private AmmoManagementBehavior()
        {
        }

        #endregion Constructors

        public static bool TryingToChangeOrReloadAmmo { get; set; } = false;

        private static DirectItem _cachedAmmoDirectItem { get; set; } = null;
        public static DirectItem CachedAmmoDirectItem
        {
            get
            {
                try
                {
                    if (_cachedAmmoDirectItem != null)
                        return _cachedAmmoDirectItem;

                    if (Combat.Combat.UsableAmmoInCargo != null && Combat.Combat.UsableAmmoInCargo.Any())
                    {
                        if (ESCache.Instance.InWarp || ESCache.Instance.DirectEve.Session.InJump)
                        {
                            if (DebugConfig.DebugAmmoManagement)
                            {
                                Log.WriteLine("if (ESCache.Instance.InWarp || ESCache.Instance.DirectEve.Session.InJump)");
                            }

                            foreach (DirectItem individualUsableAmmoInCargoAsItem in Combat.Combat.UsableAmmoInCargo)
                            {
                                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("individualUsableAmmoInCargoAsItem [" + individualUsableAmmoInCargoAsItem.TypeName + "] TypeID [" + individualUsableAmmoInCargoAsItem.TypeId + "] DamageType [" + individualUsableAmmoInCargoAsItem.DefinedDamageTypeForThisItem + "]");

                                if (individualUsableAmmoInCargoAsItem.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType))
                                {
                                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (individualUsableAmmoInCargoAsItem.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.DefaultDamageType))");
                                    foreach (var AmmoTypeThisIsDefinedAs in individualUsableAmmoInCargoAsItem.ListOfAmmoTypesThisIsDefinedAs.Where(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType).OrderByDescending(i => i.Default == true).ThenBy(x => x.Range))
                                    {
                                        //
                                        // should we be checking to make sure there is enough ammo to reload?
                                        //
                                        _cachedAmmoDirectItem = individualUsableAmmoInCargoAsItem;
                                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("_cachedAmmoDirectItem [" + _cachedAmmoDirectItem.TypeName + "] TypeID[" + _cachedAmmoDirectItem.TypeId + "] DamageType [" + _cachedAmmoDirectItem.DefinedDamageTypeForThisItem + "] default [" + _cachedAmmoDirectItem.DefinedAsAmmoType.Default + "]");
                                        return _cachedAmmoDirectItem ?? null;
                                    }
                                }

                                continue;
                            }

                            Log.WriteLine("After: foreach (DirectItem individualUsableAmmoInCargoAsItem in Combat.Combat.UsableAmmoInCargo) - none found!");
                        }

                        if (DebugConfig.DebugAmmoManagement)
                        {
                            Log.WriteLine("MissionSettings.CurrentDamageType [" + MissionSettings.CurrentDamageType + "] Combat.Combat.EntityToUseForAmmo [" + Combat.Combat.EntityToUseForAmmo.TypeName + "]");
                            foreach (var thisUsableAmmo in Combat.Combat.UsableAmmoInCargo)
                            {
                                Log.WriteLine("thisUsableAmmo [" + thisUsableAmmo.TypeName + "][" + thisUsableAmmo.TypeId + "][" + thisUsableAmmo.DefinedDamageTypeForThisItem + "]");
                            }
                        }

                        _cachedAmmoDirectItem = Combat.Combat.UsableAmmoInCargo.Where(i => i.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType && ammotype.Range > Combat.Combat.EntityToUseForAmmo.Distance)).OrderByDescending(i => i.AmmoType.DamageForThisDamageType(MissionSettings.CurrentDamageType)).ThenBy(x => x.DefinedAsAmmoType.Range).FirstOrDefault();
                        if (_cachedAmmoDirectItem == null)
                        {
                            _cachedAmmoDirectItem = Combat.Combat.UsableAmmoInCargo.Where(i => i.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType)).OrderByDescending(x => x.DefinedAsAmmoType.Range).FirstOrDefault();
                            if (_cachedAmmoDirectItem == null)
                            {
                                _cachedAmmoDirectItem = Combat.Combat.UsableAmmoInCargo.OrderByDescending(x => x.DefinedAsAmmoType.Range).FirstOrDefault();
                                return _cachedAmmoDirectItem ?? null;
                            }

                            return _cachedAmmoDirectItem ?? null;
                        }

                        return _cachedAmmoDirectItem ?? null;
                    }

                    if (DirectEve.Interval(10000)) Log.WriteLine("CachedAmmoDirectItem is null?!");
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        private static DirectItem _cachedSpecialAmmoDirectItem = null;
        public static DirectItem CachedSpecialAmmoDirectItem
        {
            get
            {
                try
                {
                    if (_cachedSpecialAmmoDirectItem != null)
                        return _cachedSpecialAmmoDirectItem;

                    if (Combat.Combat.UsableAmmoInCargo != null && Combat.Combat.UsableAmmoInCargo.Any())
                    {
                        if (ESCache.Instance.InWarp || ESCache.Instance.DirectEve.Session.InJump)
                        {
                            if (DebugConfig.DebugAmmoManagement)
                            {
                                Log.WriteLine("if (ESCache.Instance.InWarp || ESCache.Instance.DirectEve.Session.InJump)");
                            }

                            foreach (DirectItem individualUsableAmmoInCargoAsItem in Combat.Combat.UsableAmmoInCargo)
                            {
                                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("individualUsableAmmoInCargoAsItem [" + individualUsableAmmoInCargoAsItem.TypeName + "] TypeID [" + individualUsableAmmoInCargoAsItem.TypeId + "] DamageType [" + individualUsableAmmoInCargoAsItem.DefinedDamageTypeForThisItem + "]");

                                if (individualUsableAmmoInCargoAsItem.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType))
                                {
                                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (individualUsableAmmoInCargoAsItem.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.DefaultDamageType))");
                                    foreach (var AmmoTypeThisIsDefinedAs in individualUsableAmmoInCargoAsItem.ListOfAmmoTypesThisIsDefinedAs.Where(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType).OrderBy(x => x.Range))
                                    {
                                        //
                                        // should we be checking to make sure there is enough ammo to reload?
                                        //
                                        _cachedAmmoDirectItem = individualUsableAmmoInCargoAsItem;
                                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("_cachedAmmoDirectItem [" + _cachedAmmoDirectItem.TypeName + "] TypeID[" + _cachedAmmoDirectItem.TypeId + "] DamageType [" + _cachedAmmoDirectItem.DefinedDamageTypeForThisItem + "]");
                                        return _cachedAmmoDirectItem ?? null;
                                    }
                                }

                                continue;
                            }

                            Log.WriteLine("After: foreach (DirectItem individualUsableAmmoInCargoAsItem in Combat.Combat.UsableAmmoInCargo) - none found!");
                        }

                        if (DebugConfig.DebugAmmoManagement)
                        {
                            Log.WriteLine("MissionSettings.CurrentDamageType [" + MissionSettings.CurrentDamageType + "] Combat.Combat.EntityToUseForAmmo [" + Combat.Combat.EntityToUseForAmmo.TypeName + "]");
                            foreach (var thisUsableAmmo in Combat.Combat.UsableAmmoInCargo)
                            {
                                Log.WriteLine("thisUsableAmmo [" + thisUsableAmmo.TypeName + "][" + thisUsableAmmo.TypeId + "][" + thisUsableAmmo.DefinedDamageTypeForThisItem + "]");
                            }
                        }

                        _cachedSpecialAmmoDirectItem = Combat.Combat.UsableSpecialAmmoInCargo.Where(i => i.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => Combat.Combat.KillTarget != null && ammotype.OverrideTargetName == Combat.Combat.KillTarget.Name)).OrderBy(x => x.DefinedAsAmmoType.Range).FirstOrDefault();
                        if (_cachedSpecialAmmoDirectItem == null)
                        {
                            _cachedSpecialAmmoDirectItem = Combat.Combat.UsableSpecialAmmoInCargo.Where(i => i.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => Combat.Combat.KillTarget != null && ammotype.OverrideTargetName == Combat.Combat.KillTarget.Name)).OrderByDescending(x => x.DefinedAsAmmoType.Range).FirstOrDefault();
                            if (_cachedSpecialAmmoDirectItem == null)
                            {
                                _cachedAmmoDirectItem = Combat.Combat.UsableSpecialAmmoInCargo.OrderByDescending(x => x.DefinedAsAmmoType.Range).FirstOrDefault();
                                return _cachedSpecialAmmoDirectItem ?? null;
                            }

                            return _cachedSpecialAmmoDirectItem ?? null;
                        }

                        return _cachedSpecialAmmoDirectItem ?? null;
                    }

                    if (DirectEve.Interval(10000)) Log.WriteLine("CachedSpecialAmmoDirectItem is null?!");
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public static AmmoType CachedAmmoTypeCurrentlyLoaded
        {
            get
            {
                if (ESCache.Instance.Weapons.Any())
                {
                    try
                    {
                        var thisCharge = ESCache.Instance.Weapons.FirstOrDefault(i => i.Charge != null && i.ChargeQty > 0).Charge;
                        if (thisCharge != null)
                        {
                            if (thisCharge.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType))
                            {
                                var thisAmmoType = thisCharge.ListOfAmmoTypesThisIsDefinedAs.FirstOrDefault(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType);
                                return thisAmmoType ?? null;
                            }
                        }

                        return null;
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
                }

                return null;
            }
        }

        private static Dictionary<long, int> DictionaryOutOfRangeCount = new Dictionary<long, int>();


        #region Methods

        public static bool ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (_StateToSet == AmmoManagementBehaviorState.Monitor)
                {
                    Combat.Combat.BoolReloadWeaponsAsap = false;
                    TryingToChangeOrReloadAmmo = false;
                }

                if (_StateToSet.ToString().Contains("Handle"))
                    TryingToChangeOrReloadAmmo = true;

                if (State.CurrentAmmoManagementBehaviorState != _StateToSet)
                {
                    Log.WriteLine("New AmmoManagementBehaviorState [" + _StateToSet + "] TryingToChangeOrReloadAmmo [" + TryingToChangeOrReloadAmmo + "]");
                    State.CurrentAmmoManagementBehaviorState = _StateToSet;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            //Log.WriteLine("LoadSettings: AmmoManagementBehavior");

            //HomeBookmarkName =
            //    (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CharacterSettingsXml.Element("HomeBookmark") ??
            //    (string)CommonSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            //Log.WriteLine("LoadSettings: ProbeScanBehavior: HomeBookmarkName [" + HomeBookmarkName + "]");
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;
                if (DebugConfig.DebugAmmoManagement || TryingToChangeOrReloadAmmo) Log.WriteLine("State.CurrentAmmoManagementBehaviorState [" + State.CurrentAmmoManagementBehaviorState + "] TryingToChangeOrReloadAmmo [" + TryingToChangeOrReloadAmmo + "]");

                switch (State.CurrentAmmoManagementBehaviorState)
                {
                    case AmmoManagementBehaviorState.Idle:
                        IdleAMState();
                        break;

                    case AmmoManagementBehaviorState.Start:
                        StartAMState();
                        break;

                    case AmmoManagementBehaviorState.Monitor:
                        MonitorAMState();
                        break;

                    case AmmoManagementBehaviorState.HandleOverrideTargetIfFoundAndAmmoChangeNeeded:
                        //HandleOverrideTargetGroupIfFoundAndAmmoChangeNeeded();
                        break;

                    case AmmoManagementBehaviorState.HandleWrongAmmoDamageType:
                        HandleWrongAmmoDamageType();
                        break;

                    case AmmoManagementBehaviorState.HandleWrongAmmoNotEnoughRange:
                        HandleWrongAmmoNotEnoughRange();
                        break;

                    case AmmoManagementBehaviorState.HandleWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange:
                        HandleWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange();
                        break;

                    case AmmoManagementBehaviorState.HandleWrongAmmoShorterRangeAmmoStillHits:
                        HandleWrongAmmoShorterRangeAmmoStillHits();
                        break;

                    case AmmoManagementBehaviorState.HandleWrongAmmoNeedsBetterTracking:
                        HandleWrongAmmoNeedsBetterTracking();
                        break;

                    case AmmoManagementBehaviorState.HandleWrongAmmoOutOfBestAmmoUseWhatWeHave:
                        HandleWrongAmmoOutOfBestAmmoUseWhatWeHave();
                        break;

                    case AmmoManagementBehaviorState.HandleChangeToDefaultAmmo:
                        HandleChangeToDefaultAmmo();
                        break;

                    case AmmoManagementBehaviorState.HandleWeaponsNeedReload:
                        HandleWeaponsNeedReload();
                        break;

                    case AmmoManagementBehaviorState.HandleWeaponsNeedSpecialReload:
                        HandleWeaponsNeedSpecialReload();
                        break;

                    case AmmoManagementBehaviorState.Default:
                        ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        /**
        private static void ProcessAlerts()
        {
            TimeSpan ItHasBeenThisLongSinceWeStartedThePocket = Statistics.StartedPocket - DateTime.UtcNow;
            int minutesInPocket = ItHasBeenThisLongSinceWeStartedThePocket.Minutes;
            if (minutesInPocket > (AbyssalPocketWarningSeconds / 60) && !WeHaveBeenInPocketTooLong_WarningSent)
            {
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ABYSSAL_POCKET_TIME, "AbyssalDeadspace: FYI: We have been in pocket number [" + ActionControl.PocketNumber + "] for [" + minutesInPocket + "] min: You might need to check that we arnt stuck."));
                WeHaveBeenInPocketTooLong_WarningSent = true;
                Log.WriteLine("We have been in AbyssalPocket [" + ActionControl.PocketNumber + "] for more than [" + AbyssalPocketWarningSeconds + "] seconds!");
                return;
            }

            return;
        }
        **/

        private static bool EveryPulse()
        {
            if (ESCache.Instance.InStation)
            {
                ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Idle);
                return false;
            }

            if (!ESCache.Instance.InSpace)
            {
                ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Idle);
                return false;
            }

            if (!ESCache.Instance.Weapons.Any())
            {
                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("AmmoManagementBehavior: EveryPulse: if (!ESCache.Instance.Weapons.Any()) return false;");
                return false;
            }

            if (Combat.Combat.CombatShipName != ESCache.Instance.ActiveShip.GivenName)
            {
                TryingToChangeOrReloadAmmo = false;
                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("AmmoManagementBehavior: EveryPulse: if (Combat.Combat.CombatShipName != ESCache.Instance.ActiveShip.GivenName) return false;");
                return false;
            }

            if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))
            {
                TryingToChangeOrReloadAmmo = false;
                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("AmmoManagementBehavior: EveryPulse: if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon)) return false;");
                return false;
            }

            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (45 > Time.Instance.SecondsSinceLastSessionChange &&
                    AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn &&
                    AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn &&
                    AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn &&
                    AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.RodivaSpawn &&
                    AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn
                    )
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (Combat.Combat.PotentialCombatTargets.Any())
                    {
                        if (Combat.Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Lucifer Cynabal")))
                        {
                            Log.WriteLine("MonitorAMState: if (Combat.Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains(\"Lucifer Cynabal\")))");
                            if (!ESCache.Instance.Weapons.All(i => i.ChargeQty == 0))
                            {
                                if (90 > Time.Instance.SecondsSinceLastSessionChange)
                                {
                                    Log.WriteLine("MonitorAMState: if (90 > Time.Instance.SecondsSinceLastSessionChange) return; do not reload/change ammo unless we run out!");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            MonitorAMState();
            if (DebugConfig.DebugAmmoManagement) Log.WriteLine("AmmoManagementBehavior: EveryPulse: return true;");
            return true;
        }

        private static void IdleAMState()
        {
            if (ESCache.Instance.InSpace && ESCache.Instance.Weapons.Any())
            {
                ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Start);
            }
        }

        public static void InvalidateCache()
        {
            _cachedAmmoDirectItem = null;
        }

        public static void ClearPerSystemCache()
        {
            ClearPerPocketCache();
        }

        public static void ClearPerPocketCache()
        {
            if (DebugConfig.DebugAmmoManagement)
            {
                Log.WriteLine("AmmoMagementBehavior: ClearPerPocketCache: UsableAmmoInCargo is:");
                int intCount = 0;
                if (ESCache.Instance.InSpace && ESCache.Instance.Weapons.Any() && Combat.Combat.UsableAmmoInCargo != null && Combat.Combat.UsableAmmoInCargo.Any())
                {
                    try
                    {
                        foreach (var individualAmmoIncargo in Combat.Combat.UsableAmmoInCargo)
                        {
                            intCount++;
                            if (DebugConfig.DebugAmmoManagement) Log.WriteLine("ClearPerPocketCache [" + intCount + "][" + individualAmmoIncargo.TypeName + "][" + individualAmmoIncargo.DefinedAsAmmoType.DamageType + "] Range [" + individualAmmoIncargo.DefinedAsAmmoType.Range + "] MinimumQty [" + individualAmmoIncargo.DefinedAsAmmoType.Quantity + "]");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }
                }
            }

            DictionaryOutOfRangeCount = new Dictionary<long, int>();
            //ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleChangeToDefaultAmmo);
        }

        private static bool ResetStatesToDefaults()
        {
            // intentionally left empty.
            return true;
        }

        private static void StartAMState()
        {
            ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
        }

        private static void ChangeToDefaultAmmoState()
        {
            //
            // runs on every grid change (in warp?)
            //

            ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Idle);
        }

        private static void MonitorAMState()
        {
            if (Combat.Combat.CombatShipName != ESCache.Instance.ActiveShip.GivenName)
            {
                TryingToChangeOrReloadAmmo = false;
                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("MonitorAMState: if (Combat.Combat.CombatShipName != ESCache.Instance.ActiveShip.GivenName)");
                return;
            }

            if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))
            {
                TryingToChangeOrReloadAmmo = false;
                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("MonitorAMState: if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))");
                return;
            }

            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (Combat.Combat.PotentialCombatTargets.Any())
                    {
                        if (Combat.Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Lucifer Cynabal")))
                        {
                            Log.WriteLine("MonitorAMState: if (Combat.Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains(\"Lucifer Cynabal\")))");
                            if (!ESCache.Instance.Weapons.Any(i => i.ChargeQty == 0))
                            {
                                if (90 > Time.Instance.SecondsSinceLastSessionChange)
                                {
                                    Log.WriteLine("MonitorAMState: if (90 > Time.Instance.SecondsSinceLastSessionChange) return; do not reload/change ammo unless we run out!");
                                    return;
                                }
                            }
                        }
                    }
                }

                if (!Combat.Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && (ESCache.Instance.Wrecks.Any()))
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("MonitorAMState: if (!Combat.Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && ESCache.Instance.Wrecks.Any())");
                    if (ESCache.Instance.Weapons.Any(x => x.Charge != null && CachedAmmoDirectItem != null && (CachedAmmoDirectItem.TypeId != x.Charge.TypeId)))
                    {
                        Log.WriteLine("MonitorAMState: if (ESCache.Instance.Weapons.Any(x => x.Charge != null && (CachedAmmoDirectItem.TypeId != x.Charge.TypeId)))");
                        ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleChangeToDefaultAmmo);
                        return;
                    }

                    if (ESCache.Instance.Weapons.Any(x => x.Charge != null && x.MaxCharges > x.ChargeQty))
                    {
                        Log.WriteLine("MonitorAMState: if (ESCache.Instance.Weapons.Any(x => x.Charge != null && (x.MaxCharges > x.ChargeQty)))");
                        ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWeaponsNeedReload);
                        return;
                    }
                }
            }

            /**
            if (!Combat.Combat.PotentialCombatTargets.Any() && ESCache.Instance.AbyssalTrace != null && false)
            {
                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("MonitorAMState: if (!Combat.Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && ESCache.Instance.Wrecks.Any())");
                if (ESCache.Instance.Weapons.Any(x => x.Charge != null && (CachedAmmoDirectItem.TypeId != x.Charge.TypeId || x.MaxCharges > x.ChargeQty)))
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("MonitorAMState: if (ESCache.Instance.Weapons.Any(x => x.Charge != null && x.MaxCharges > x.ChargeQty))");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleChangeToDefaultAmmo);
                    return;
                }
            }

            **/

            //
            // Detect when we need to change ammo or reload!
            //
            if (Combat.Combat.PotentialCombatTargets.Any())
            {
                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (Combat.Combat.PotentialCombatTargets.Any())");
                if (ESCache.Instance.Targets.Any(i => i.GroupId != (int)Group.AssaultShip))
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (ESCache.Instance.Targets.Any())");

                    if (Combat.Combat.EntityToUseForAmmo != null)
                    {
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (Combat.Combat.EntityToUseForAmmo != null)");
                        //
                        // do not swap ammo if dealing with the drifter: use special rules for this.
                        //
                        if (DetectOverrideTargetGroupIfFoundAndAmmoChangeNeeded()) return;

                        //
                        //WrongAmmoDamageType,
                        //
                        if (DetectWrongDamageType()) return;


                        //WrongAmmoNotEnoughRange,
                        if (DetectWrongAmmoNotEnoughRange()) return;

                        //WrongAmmoOtherAmmoDoesMoreDamageAndIsInRange,
                        if (DetectWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange()) return;

                        //WrongAmmoShorterRangeAmmoStillHits,
                        if (DetectWrongAmmoShorterRangeAmmoStillHits()) return;

                        //WrongAmmoNeedsBetterTracking,
                        //WrongAmmoOutOfBestAmmoUseWhatWeHave,5
                        return;
                    }

                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (Combat.Combat.EntityToUseForRangeCalc != null)");
                    //
                    // Just because you have it targeted does not mean the target is in weapons range!
                    // do not return here
                }
                else if (DetectWrongAmmoNotEnoughRange()) return;
                //
                // Just because you have a kill target does not mean you should not check for mismatched ammo in guns or the need to reload!
                // do not return here
            }

            if (DebugConfig.DebugAmmoManagement) Log.WriteLine("!if (Combat.Combat.PotentialCombatTargets.Any())");

            if (DetectWeaponsDoNotHaveAllTheSameAmmoTypeLoaded()) return;
            if (DetectWeaponsNeedReload()) return;

            if (DetectWrongDamageType()) return;

            ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
            return;
        }

        private static bool HandleOverrideTargetGroupIfFoundAndAmmoChangeNeeded()
        {
            try
            {
                ///todo: fixme ...
                if (CachedSpecialAmmoDirectItem == null)
                {
                    Log.WriteLine("if (CachedAmmoDirectItem == null)");
                    return false;
                }

                //
                // Detect we are done changing ammo
                //
                if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == CachedSpecialAmmoDirectItem.TypeId))
                {
                    Log.WriteLine("if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == CachedSpecialAmmoDirectItem.TypeId))");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (!DeactivateAllWeapons()) return false;

                int intCount = 0;
                if (ESCache.Instance.Weapons.Any(i => i._module.IsMaster))
                {
                    foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsMaster && (i.ChargeQty == 0 || i.Charge.TypeId != CachedSpecialAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                    {
                        intCount++;
                        Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedSpecialAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                        thisWeapon.ChangeAmmo(CachedSpecialAmmoDirectItem);
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                        {
                            continue;
                        }

                        return false;
                    }

                    return false;
                }

                foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => (i.ChargeQty == 0 || i.Charge.TypeId != CachedSpecialAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                {
                    intCount++;
                    Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedSpecialAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                    thisWeapon.ChangeAmmo(CachedSpecialAmmoDirectItem);
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        continue;
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

        private static bool DetectOverrideTargetGroupIfFoundAndAmmoChangeNeeded()
        {
            if (DirectUIModule.DefinedAmmoTypes.Any(i => !string.IsNullOrEmpty(i.OverrideTargetName) && ESCache.Instance.Targets.Any(x => x.Name.ToLower().Contains(i.OverrideTargetName.ToLower())) && Combat.Combat.KillTarget != null && Combat.Combat.KillTarget.Name.ToLower().Contains(i.OverrideTargetName.ToLower())))
            {
                var thisOverrideAmmoTypeNeeded = DirectUIModule.DefinedAmmoTypes.FirstOrDefault(i => !string.IsNullOrEmpty(i.OverrideTargetName) && ESCache.Instance.Targets.Any(x => x.Name.ToLower().Contains(i.OverrideTargetName.ToLower())) && Combat.Combat.KillTarget != null && Combat.Combat.KillTarget.Name.ToLower().Contains(i.OverrideTargetName.ToLower()));
                if (thisOverrideAmmoTypeNeeded != null)
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (thisOverrideAmmoTypeNeeded != null)");
                    if (ESCache.Instance.InWormHoleSpace && 1000 > Combat.Combat.KillTarget.ShieldCurrentHitPoints)
                    {
                        //
                        // we need to detect if we have already changed ammo here
                        //
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (1000 > Combat.Combat.KillTarget.ShieldCurrentHitPoints [" + Combat.Combat.KillTarget.ShieldCurrentHitPoints + "])");

                        if (ESCache.Instance.Weapons.Any(i => i.ChargeQty != 0 && i.Charge.TypeId != thisOverrideAmmoTypeNeeded.TypeId))
                        {
                            if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (ESCache.Instance.Weapons.Any(i => i.ChargeQty != 0 && i.Charge.TypeId != thisOverrideAmmoTypeNeeded.TypeId))");
                            ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleOverrideTargetIfFoundAndAmmoChangeNeeded);
                            return true;
                        }

                        if (DetectWeaponsNeedSpecialReload()) return true;
                        return true;
                    }

                    return false;
                }

                return false;
            }

            return false;

        }

        private static DateTime LastDetectWeaponsNeedReload = DateTime.UtcNow.AddMinutes(-5);

        private static bool DetectWeaponsNeedReload()
        {
            try
            {
                if (LastDetectWeaponsNeedReload.AddSeconds(DelayBasedOnWeaponReloadTime) > DateTime.UtcNow)
                    return false;

                //if (ESCache.Instance.Weapons.Any(i => i.Charge == null && !i.IsCivilianWeapon))
                //{
                //    //when your guns havent had ammo in them (or are empty?) reload does nothing!
                //    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                //    return false;
                //}

                if (ESCache.Instance.Weapons.Any(i => i.WeaponNeedsToBeReloadedNow))
                {
                    if (Combat.Combat.UsableAmmoInCargo.Any())
                    {
                        if (CachedAmmoDirectItem != null)
                        {
                            Log.WriteLine("DetectWeaponsNeedReload: CachedAmmoDirectItem [" + CachedAmmoDirectItem.TypeName + "] Range [" + CachedAmmoDirectItem.DefinedAsAmmoType.Range + "m]");
                            ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWeaponsNeedReload, false);
                            return true;
                        }

                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWeaponsNeedReload: if (CachedAmmoDirectItem == null)");
                        return false;
                    }

                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (!Combat.Combat.UsableAmmoInCargo.Any())");
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("exception [" + ex + "]");
                return false;
            }
        }

        private static bool DetectWeaponsNeedSpecialReload()
        {
            try
            {
                if (LastDetectWeaponsNeedReload.AddSeconds(DelayBasedOnWeaponReloadTime) > DateTime.UtcNow)
                    return false;

                if (ESCache.Instance.Weapons.Any(i => i.Charge == null && !i.IsCivilianWeapon && !i.IsInLimboState))
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    return false;
                }

                if (ESCache.Instance.InWormHoleSpace)
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (ESCache.Instance.InWormHoleSpace)");
                    if (Combat.Combat.KillTarget.ShieldCurrentHitPoints > 1000)
                    {
                        DetectWeaponsNeedReload();
                        return false;
                    }

                    return false;
                }

                if (ESCache.Instance.Weapons.Any(i => i.WeaponNeedsToBeReloadedNow))
                {
                    if (Combat.Combat.UsableSpecialAmmoInCargo.Any())
                    {
                        if (CachedSpecialAmmoDirectItem != null)
                        {
                            Log.WriteLine("DetectWeaponsNeedReload: CachedSpecialAmmoDirectItem [" + CachedSpecialAmmoDirectItem.TypeName + "] Range [" + CachedSpecialAmmoDirectItem.DefinedAsAmmoType.Range + "m]");
                            ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWeaponsNeedSpecialReload, false);
                            return true;
                        }

                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWeaponsNeedReload: if (CachedSpecialAmmoDirectItem != null)");
                        return false;
                    }

                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("if (!Combat.Combat.UsableSpecialAmmoInCargo.Any())");
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("exception [" + ex + "]");
                return false;
            }
        }

        private static DateTime LastDetectWeaponsDoNotHaveAllTheSameAmmoTypeLoaded = DateTime.UtcNow.AddMinutes(-5);
        private static bool DetectWeaponsDoNotHaveAllTheSameAmmoTypeLoaded()
        {
            try
            {
                if (LastDetectWeaponsDoNotHaveAllTheSameAmmoTypeLoaded.AddSeconds(DelayBasedOnWeaponReloadTime) > DateTime.UtcNow)
                    return false;

                if (ESCache.Instance.Weapons.Any(i => i.Charge == null && !i.IsCivilianWeapon))
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    return false;
                }

                if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0 && i.Charge.TypeId != ESCache.Instance.Weapons.FirstOrDefault(x => i.GroupId == x.GroupId && x.ChargeQty > 0).Charge.TypeId))
                {
                    if (Combat.Combat.UsableAmmoInCargo.Any())
                    {
                        Log.WriteLine("DetectWeaponsDoNotHaveAllTheSameAmmoTypeLoaded: Setting CachedAmmoDirectItem to [" + CachedAmmoDirectItem.TypeName + "] Range [" + CachedAmmoDirectItem.DefinedAsAmmoType.Range + "m]");
                        ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleChangeToDefaultAmmo, false);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("exception [" + ex + "]");
                return false;
            }
        }

        private static DateTime LastDetectWrongDamageType = DateTime.UtcNow.AddMinutes(-5);

        private static bool DetectWrongDamageType()
        {
            try
            {
                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongDamageType");
                //if (LastDetectWrongDamageType.AddSeconds(DelayBasedOnWeaponReloadTime) > DateTime.UtcNow)
                //    return false;

                if (Combat.Combat.CombatShipName != ESCache.Instance.ActiveShip.GivenName)
                    return false;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    return false;

                if (ESCache.Instance.InWarp || ESCache.Instance.ActiveShip.Entity.Mode == 3) //we want not only when its mid warp but also when its just warp drive active
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("InWarp");
                    DirectItem chargeThatIsLoaded = null;
                    if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0))
                    {
                        chargeThatIsLoaded = ESCache.Instance.Weapons.FirstOrDefault(i => i.ChargeQty > 0).Charge;
                        if (chargeThatIsLoaded != null)
                        {
                            if (chargeThatIsLoaded.ListOfAmmoTypesThisIsDefinedAs != null && chargeThatIsLoaded.ListOfAmmoTypesThisIsDefinedAs.Any())
                            {
                                foreach (var AmmoTypeThisIsDefinedAs in chargeThatIsLoaded.ListOfAmmoTypesThisIsDefinedAs)
                                {
                                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("Charge [" + chargeThatIsLoaded.TypeName + "] AmmoTypeThisIsDefinedAs [" + AmmoTypeThisIsDefinedAs.Description + "][" + AmmoTypeThisIsDefinedAs.DamageType + "][" + AmmoTypeThisIsDefinedAs.Range + "] and DefaultDamageType is [" + MissionSettings.DefaultDamageType.ToString() + "]");
                                }
                            }
                            else if (DebugConfig.DebugAmmoManagement) Log.WriteLine("Charge [" + chargeThatIsLoaded.TypeName + "] Is not defined as any damagetype (not defined in ammo?) and DefaultDamageType is [" + MissionSettings.DefaultDamageType.ToString() + "]");

                        }

                        if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0 && !i.Charge.ListOfAmmoTypesThisIsDefinedAs.Any()))
                        {
                            Log.WriteLine("DetectWrongDamageType: [" + chargeThatIsLoaded.TypeName + "]!>!");
                            ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWrongAmmoDamageType, false);
                            return true;
                        }
                    }

                    if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0 && CachedAmmoDirectItem != null && i.Charge.TypeId != CachedAmmoDirectItem.TypeId))
                    {
                        Log.WriteLine("DetectWrongDamageType: [" + CachedAmmoDirectItem.TypeName + "] ListOfAmmoTypesThisIsDefinedAs [" + CachedAmmoDirectItem.stringListOfAmmoTypesThisIsDefinedAs + "] CurrentDamageType [" + MissionSettings.CurrentDamageType.ToString() + "]");
                        ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWrongAmmoDamageType, false);
                        return true;
                    }

                    //if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0 && i.Charge.ListOfAmmoTypesThisIsDefinedAs.Any() && i.Charge.ListOfAmmoTypesThisIsDefinedAs.All(i => i.DamageType != MissionSettings.DefaultDamageType)))
                    //{
                    //    var thisDamageType = chargeThatIsLoaded.ListOfAmmoTypesThisIsDefinedAs.FirstOrDefault();
                    //    Log.WriteLine("DetectWrongDamageType: [" + chargeThatIsLoaded.TypeName + "] Range [" + thisDamageType.Range + "m] DamageType [" + thisDamageType.ToString() + "] CurrentDamageType [" + MissionSettings.CurrentDamageType.ToString() + "]");
                    //    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWrongAmmoDamageType, false);
                    //    return true;
                    //}

                    return false;
                }

                /**
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                    {
                        if (Combat.Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && i.Name.Contains("Blastlance")) > 3)
                            return true;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.Contains("Lucifer Cynabal")))
                            return true;
                    }
                }
                **/

                if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0 && CachedAmmoDirectItem != null && i.Charge.TypeId != CachedAmmoDirectItem.TypeId))
                {
                    Log.WriteLine("DetectWrongDamageType: CachedAmmoDirectItem [" + CachedAmmoDirectItem.TypeName + "] ListOfAmmoTypesThisIsDefinedAs [" + CachedAmmoDirectItem.stringListOfAmmoTypesThisIsDefinedAs + "] CurrentDamageType [" + MissionSettings.CurrentDamageType.ToString() + "] CurrentLoadedAmmo [" + ESCache.Instance.Weapons.FirstOrDefault(i => i.Charge != null).ChargeName + "]");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWrongAmmoDamageType, false);
                    return true;
                }

                if (ESCache.Instance.Weapons.Any(i => i.Charge == null || i.ChargeQty == 0))
                {
                    Log.WriteLine("DetectWrongDamageType: if (ESCache.Instance.Weapons.Any(i => i.Charge == null))");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleChangeToDefaultAmmo, false);
                    return true;
                }

                //if (ESCache.Instance.Weapons.Any(i => i.Charge == null && !i.IsCivilianWeapon))
                //{
                //    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                //    return false;
                //}

                //if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0 && i.Charge.ListOfAmmoTypesThisIsDefinedAs.Any() && i.Charge.ListOfAmmoTypesThisIsDefinedAs.All(i => i.DamageType != MissionSettings.CurrentDamageType)))
                //{
                //    Log.WriteLine("DetectWrongDamageType: [" + CachedAmmoDirectItem.TypeName + "] ListOfAmmoTypesThisIsDefinedAs [" + CachedAmmoDirectItem.stringListOfAmmoTypesThisIsDefinedAs + "] CurrentDamageType [" + MissionSettings.CurrentDamageType.ToString() + "]");
                //    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWrongAmmoDamageType, false);
                //    return true;
                //}

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("exception [" + ex + "]");
                return false;
            }
        }

        private static bool IsEntityTooFarForThisAmmo(DirectItem myCharge)
        {
            try
            {
                if (myCharge == null)
                    return false;

                //Yes, this ammo can be defined multiple times for different damagetypes, but the range should be defined the same for all (because if its not it really should be!)
                if (myCharge.ListOfAmmoTypesThisIsDefinedAs.Any())
                {
                    var thisAmmoType = myCharge.ListOfAmmoTypesThisIsDefinedAs.FirstOrDefault();
                    if (thisAmmoType != null)
                    {
                        var thisAmmoType_DirectItem = new DirectItem(ESCache.Instance.DirectEve);
                        thisAmmoType_DirectItem.TypeId = thisAmmoType.TypeId;
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("IsEntityTooFarForThisAmmo [" + thisAmmoType_DirectItem.TypeName + "][" + thisAmmoType.DamageType + "] Range [" + thisAmmoType.Range + "] EntityToUseForRangeCalc [" + Combat.Combat.EntityToUseForAmmo.Name + "][" + Math.Round(Combat.Combat.EntityToUseForAmmo.Distance, 0) + "m]");
                        if (Combat.Combat.EntityToUseForAmmo.Distance > thisAmmoType.Range)
                        {
                            if (DebugConfig.DebugAmmoManagement) Log.WriteLine("IsEntityTooFarForThisAmmo [true]");
                            return true;
                        }
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("exception [" + ex + "]");
                return false;
            }
        }

        /**
        private static EntityCache _entityToUseForRangeCalc { get; set; } = null;
        public static EntityCache EntityToUseForRangeCalc
        {
            get
            {
                try
                {
                    if (_entityToUseForRangeCalc != null)
                        return _entityToUseForRangeCalc;

                    if (Combat.Combat.KillTarget != null)
                    {
                        _entityToUseForRangeCalc = Combat.Combat.KillTarget;
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForRangeCalc [" + _entityToUseForRangeCalc.Name + "][" + _entityToUseForRangeCalc.Nearest1KDistance + "k]");
                        return _entityToUseForRangeCalc ?? null;
                    }

                    if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsOnGridWithMe))
                    {
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForRangeCalc if (Combat.Combat.PotentialCombatTargets.Any())");
                        _entityToUseForRangeCalc = Combat.Combat.PotentialCombatTargets.Where(i => i.IsOnGridWithMe).OrderBy(i => i.Distance).FirstOrDefault();
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForRangeCalc [" + _entityToUseForRangeCalc.Name + "][" + _entityToUseForRangeCalc.Nearest1KDistance + "]");
                        return _entityToUseForRangeCalc ?? null;
                    }

                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForRangeCalc: KillTarget == null and !PotentialCombatTargets.Any() - using my Ship (0k!)");
                    _entityToUseForRangeCalc = ESCache.Instance.MyShipEntity;
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("EntityToUseForRangeCalc [" + _entityToUseForRangeCalc.Name + "][" + _entityToUseForRangeCalc.Nearest1KDistance + "]");
                    return _entityToUseForRangeCalc ?? null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    _entityToUseForRangeCalc = ESCache.Instance.MyShipEntity;
                    return _entityToUseForRangeCalc ?? null;
                }
            }
        }
        **/

        private static DateTime LastDetectWrongAmmoNotEnoughRange = DateTime.UtcNow.AddMinutes(-5);

        private static bool DetectWrongAmmoNotEnoughRange()
        {
            try
            {
                if (LastDetectWrongAmmoNotEnoughRange.AddSeconds(DelayBasedOnWeaponReloadTime) > DateTime.UtcNow)
                    return false;

                if (ESCache.Instance.Weapons.Any(i => i.Charge == null && !i.IsCivilianWeapon))
                {
                    if (DirectEve.Interval(10000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    //return false;
                }

                if (DebugConfig.DebugAmmoManagement && DirectEve.Interval(60000))
                {
                    if (ESCache.Instance.Weapons.Any())
                    {
                        Log.WriteLine("[" + ESCache.Instance.Weapons.Count() + "] weapons found");
                        foreach (var item in ESCache.Instance.Weapons)
                        {
                            Log.WriteLine("[" + item.TypeName + "] ItemID [" + item.ItemId + "]");
                        }
                    }
                    else Log.WriteLine("no weapons found");
                }

                var myCharge = ESCache.Instance.Weapons.FirstOrDefault(i => i.ChargeQty > 0).Charge ?? null;
                if (myCharge != null)
                {
                    if (IsEntityTooFarForThisAmmo(myCharge))
                    {
                        List<DirectItem> myAmmoInCargoThatCanReachThisSpecificDistance = Combat.Combat.AmmoInCargoThatCanReachThisSpecificDistance(Math.Round(Combat.Combat.EntityToUseForAmmo.Distance + 500, 0), false);

                        if (DebugConfig.DebugAmmoManagement)
                        {
                            int intCount = 0;
                            foreach (DirectItem thisAmmoInCargo in myAmmoInCargoThatCanReachThisSpecificDistance)
                            {
                                intCount++;
                                Log.WriteLine("--- [" + intCount + "][" + thisAmmoInCargo.TypeName + "] Range [" + thisAmmoInCargo.DefinedAsAmmoType.Range + "m] EntityToUseForRangeCalc [" + Combat.Combat.EntityToUseForAmmo.TypeName + "] Distance  [" + Math.Round(Combat.Combat.EntityToUseForAmmo.Distance, 0) + "m]");
                            }
                        }

                        if (myAmmoInCargoThatCanReachThisSpecificDistance.Any())
                        {
                            if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongAmmoNotEnoughRange: if (myAmmoInCargoThatCanReachThisSpecificDistance.Any())");

                            var closestAmmoRange = myAmmoInCargoThatCanReachThisSpecificDistance.Where(x => x.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType)).OrderBy(i => i.DefinedAsAmmoType.Range).FirstOrDefault();
                            if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongAmmoNotEnoughRange: closestAmmoRange [" + closestAmmoRange.TypeName + "] Range [" + closestAmmoRange.DefinedAsAmmoType.Range + "]");
                            int icount = 0;
                            //
                            // more logging needed here??
                            //
                            foreach (var individualAmmoInCargo in myAmmoInCargoThatCanReachThisSpecificDistance.Where(x => x.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType && ammotype.Range == closestAmmoRange.DefinedAsAmmoType.Range)))
                            {
                                icount++;
                                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongAmmoNotEnoughRange: individualAmmoInCargo [" + individualAmmoInCargo.TypeName + "][" + individualAmmoInCargo.DefinedAsAmmoType.Range + "] closestAmmoRange is [" + closestAmmoRange.DefinedAsAmmoType.Range + "]");
                                if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0 && i.Charge.TypeId != individualAmmoInCargo.TypeId))
                                {
                                    Log.WriteLine("DetectWrongAmmoNotEnoughRange [" + true + "]");
                                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWrongAmmoNotEnoughRange, false);
                                    return true;
                                }
                            }

                            //
                            // why are we hitting this?
                            //
                            if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongAmmoNotEnoughRange: ?!? !!!");
                            return false;
                        }

                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongAmmoNotEnoughRange: !if (myAmmoInCargoThatCanReachThisSpecificDistance.Any()) !-!-!");
                        return false;
                    }

                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongAmmoNotEnoughRange: Entity is in range.");
                    return false;
                }
                else Log.WriteLine("if (myCharge == null)");

                Log.WriteLine("DetectWrongAmmoNotEnoughRange [" + true + "] myCharge == null!");
                ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWrongAmmoNotEnoughRange, false);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static DateTime LastDetectWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange = DateTime.UtcNow.AddMinutes(-5);

        private static bool DetectWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange()
        {
            try
            {
                if (LastDetectWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange.AddSeconds(DelayBasedOnWeaponReloadTime) > DateTime.UtcNow)
                    return false;

                if (ESCache.Instance.Weapons.Any(i => i.Charge == null && !i.IsCivilianWeapon))
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    return false;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                {
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                        {
                            if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                            {
                                if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == (int)TypeID.ScorchS))
                                {
                                    return false;
                                }
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                        {
                            if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))
                            {
                                if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == (int)TypeID.ScorchS))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }


                if (Combat.Combat.EntityToUseForAmmo != null)
                {
                    List<DirectItem> myAmmoInCargoThatCanReachThisSpecificDistance = Combat.Combat.AmmoInCargoThatCanReachThisSpecificDistance(Math.Round(Combat.Combat.EntityToUseForAmmo.Distance + 500, 0), false);

                    if (DebugConfig.DebugAmmoManagement)
                    {
                        int intCount = 0;
                        foreach (DirectItem thisAmmoInCargo in myAmmoInCargoThatCanReachThisSpecificDistance)
                        {
                            intCount++;
                            Log.WriteLine("--- [" + intCount + "][" + thisAmmoInCargo.TypeName + "] Range [" + thisAmmoInCargo.DefinedAsAmmoType.Range + "m] EntityToUseForAmmo [" + Combat.Combat.EntityToUseForAmmo.TypeName + "] Distance  [" + Math.Round(Combat.Combat.EntityToUseForAmmo.Distance, 0) + "m]");
                        }
                    }

                    if (myAmmoInCargoThatCanReachThisSpecificDistance.Any())
                    {
                        var closestAmmoRange = myAmmoInCargoThatCanReachThisSpecificDistance.Where(x => x.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType)).OrderBy(i => i.DefinedAsAmmoType.Range).FirstOrDefault();
                        foreach (var individualAmmoInCargo in myAmmoInCargoThatCanReachThisSpecificDistance.Where(x => x.ListOfAmmoTypesThisIsDefinedAs.Any(ammotype => ammotype.DamageType == MissionSettings.CurrentDamageType && ammotype.Range == closestAmmoRange.DefinedAsAmmoType.Range)))
                        {
                            if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0 && i.Charge.TypeId != individualAmmoInCargo.TypeId))
                            {
                                Log.WriteLine("Setting CachedAmmoDirectItem to [" + CachedAmmoDirectItem.TypeName + "] Range [" + CachedAmmoDirectItem.DefinedAsAmmoType.Range + "m]");
                                ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange, false);
                                return true;
                            }
                        }
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

        private static int DelayBasedOnWeaponReloadTime
        {
            get
            {
                if (ESCache.Instance.Weapons.Any(i => i.IsEnergyWeapon))
                    return ESCache.Instance.RandomNumber(59, 61);

                if (ESCache.Instance.Weapons.Any(i => i._module.IsVortonProjector))
                    return ESCache.Instance.RandomNumber(5, 6);

                if (ESCache.Instance.Weapons.Any(i => i.IsMissileLauncher))
                    return ESCache.Instance.RandomNumber(40, 47);

                if (ESCache.Instance.Weapons.Any(i => i.IsTurret))
                    return ESCache.Instance.RandomNumber(25,32);

                return 60;
            }
        }

        private static DateTime LastDetectWrongAmmoShorterRangeAmmoStillHits = DateTime.UtcNow.AddMinutes(-5);
        private static bool DetectWrongAmmoShorterRangeAmmoStillHits()
        {
            try
            {
                if (ESCache.Instance.Weapons.Any(i => i.Charge == null && !i.IsCivilianWeapon))
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    return false;
                }

                //if (LastDetectWrongAmmoShorterRangeAmmoStillHits.AddSeconds(DelayBasedOnWeaponReloadTime + 15) > DateTime.UtcNow)
                //    return false;
                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongAmmoShorterRangeAmmoStillHits: 1");
                if (Combat.Combat.EntityToUseForAmmo != null)
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongAmmoShorterRangeAmmoStillHits: 2");
                    List<DirectItem> myAmmoInCargoThatCanReachThisSpecificDistance = Combat.Combat.AmmoInCargoThatCanReachThisSpecificDistance(Math.Round(Combat.Combat.EntityToUseForAmmo.Distance + 500, 0), false);

                    if (DebugConfig.DebugAmmoManagement)
                    {
                        int iCount = 0;
                        foreach (var thisAmmo in myAmmoInCargoThatCanReachThisSpecificDistance)
                        {
                            iCount++;
                            Log.WriteLine("[" + iCount + "][" + thisAmmo.TypeName + "] Range [" + thisAmmo.DefinedAsAmmoType.Range + "] Desired Range [" + Math.Round(Combat.Combat.EntityToUseForAmmo.Distance, 0) + "]");
                        }
                    }

                    var closestAmmoRange = myAmmoInCargoThatCanReachThisSpecificDistance.Where(x => x.ListOfAmmoTypesThisIsDefinedAs.Any(i => i.DamageType == MissionSettings.CurrentDamageType)).OrderBy(i => i.DefinedAsAmmoType.Range).FirstOrDefault();
                    if (closestAmmoRange != null)
                    {
                        if (DebugConfig.DebugAmmoManagement) Log.WriteLine("ClosestRange Ammo: [" + closestAmmoRange.TypeName + "] Range [" + closestAmmoRange.DefinedAsAmmoType.Range + "] Desired Range [" + Math.Round(Combat.Combat.EntityToUseForAmmo.Distance, 0) + "]");
                        foreach (var individualAmmoInCargo in myAmmoInCargoThatCanReachThisSpecificDistance.Where(x => x.ListOfAmmoTypesThisIsDefinedAs.Any(i => i.DamageType == MissionSettings.CurrentDamageType && i.Range == closestAmmoRange.DefinedAsAmmoType.Range)))
                        {
                            if (DebugConfig.DebugAmmoManagement) Log.WriteLine("DetectWrongAmmoShorterRangeAmmoStillHits: 3");
                            if (ESCache.Instance.Weapons.Any(i => i.ChargeQty > 0 && i.Charge.TypeId != individualAmmoInCargo.TypeId))
                            {
                                LastDetectWrongAmmoShorterRangeAmmoStillHits = DateTime.UtcNow;
                                Log.WriteLine("DetectWrongAmmoShorterRangeAmmoStillHits: ");
                                ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.HandleWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange, false);
                                return true;
                            }

                            break;
                        }

                        return false;
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

        private static bool HandleWrongAmmoDamageType()
        {
            try
            {
                if (CachedAmmoDirectItem == null)
                {
                    Log.WriteLine("if (CachedAmmoDirectItem == null)");
                    return false;
                }

                //
                // Detect we are done changing ammo
                //
                if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == CachedAmmoDirectItem.TypeId))
                {
                    Log.WriteLine("if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == CachedAmmoDirectItem.TypeId))");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (!DeactivateAllWeapons()) return false;

                int intCount = 0;
                if (ESCache.Instance.Weapons.Any(i => i._module.IsMaster))
                {
                    foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsMaster && (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                    {
                        intCount++;
                        Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                        thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                        {
                            continue;
                        }

                        return false;
                    }

                    return false;
                }

                foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                {
                    intCount++;
                    Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                    thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        continue;
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

        private static bool HandleWrongAmmoNotEnoughRange()
        {
            try
            {
                if (CachedAmmoDirectItem == null)
                {
                    Log.WriteLine("if (CachedAmmoDirectItem == null)");
                    return false;
                }

                //
                // Detect we are done changing ammo
                //
                if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == CachedAmmoDirectItem.TypeId))
                {
                    Log.WriteLine("HandleWrongAmmoNotEnoughRange: Done Changing Ammo");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (!DeactivateAllWeapons()) return false;

                int intCount = 0;
                if (ESCache.Instance.Weapons.Any(i => i._module.IsMaster))
                {
                    foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsMaster && (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                    {
                        intCount++;
                        Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                        thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                        if (ESCache.Instance.Weapons.Any(i => i.IsEnergyWeapon)) //should not be stacked!
                        {
                            continue;
                        }

                        return false;
                    }

                    return false;
                }

                foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => (i.Charge == null || i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                {
                    intCount++;
                    Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                    thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                    if (ESCache.Instance.Weapons.Any(i => i.IsEnergyWeapon)) //should not be stacked!
                    {
                        continue;
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

        private static bool HandleWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange()
        {
            try
            {
                if (CachedAmmoDirectItem == null)
                {
                    Log.WriteLine("if (CachedAmmoDirectItem == null)");
                    return false;
                }

                //
                // Detect we are done changing ammo
                //
                if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == CachedAmmoDirectItem.TypeId))
                {
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (!DeactivateAllWeapons()) return false;

                int intCount = 0;
                if (ESCache.Instance.Weapons.Any(i => i._module.IsMaster))
                {
                    foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsMaster && (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                    {
                        intCount++;
                        Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                        thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                        {
                            continue;
                        }

                        return false;
                    }

                    return false;
                }

                foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                {
                    intCount++;
                    Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                    thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        continue;
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

        private static bool HandleWrongAmmoShorterRangeAmmoStillHits()
        {
            try
            {
                if (CachedAmmoDirectItem == null)
                {
                    Log.WriteLine("if (CachedAmmoDirectItem == null)");
                    return false;
                }

                //
                // Detect we are done changing ammo
                //
                if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == CachedAmmoDirectItem.TypeId))
                {
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (!DeactivateAllWeapons()) return false;

                int intCount = 0;
                if (ESCache.Instance.Weapons.Any(i => i._module.IsMaster))
                {
                    foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsMaster && (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                    {
                        intCount++;
                        Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                        thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                        {
                            continue;
                        }

                        return false;
                    }

                    return false;
                }

                foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                {
                    intCount++;
                    Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                    thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        continue;
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

        private static bool HandleWrongAmmoNeedsBetterTracking()
        {
            try
            {
                if (CachedAmmoDirectItem == null)
                {
                    Log.WriteLine("if (CachedAmmoDirectItem == null)");
                    return false;
                }

                //
                // Detect we are done changing ammo
                //
                if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == CachedAmmoDirectItem.TypeId))
                {
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                TryingToChangeOrReloadAmmo = true;

                if (!DeactivateAllWeapons()) return false;

                int intCount = 0;
                if (ESCache.Instance.Weapons.Any(i => i._module.IsMaster))
                {
                    foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsMaster && (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                    {
                        intCount++;
                        Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                        thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                        {
                            continue;
                        }

                        return false;
                    }

                    return false;
                }

                foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                {
                    intCount++;
                    Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                    thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        continue;
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

        private static bool WeaponDeactivationNeeded = false;

        private static bool DeactivateAllWeapons()
        {
            try
            {
                if (ESCache.Instance.Weapons.All(i => !i.IsActive))
                {
                    if (WeaponDeactivationNeeded) Log.WriteLine("Finished deactivating all weapons: proceed to changing / reloading ammo");
                    WeaponDeactivationNeeded = false;
                    return true;
                }

                if (ESCache.Instance.Weapons.Any(i => (i._module.IsMaster || ESCache.Instance.Weapons.All(x => !x._module.IsMaster)) && i.IsActive && !i.IsDeactivating))
                {
                    WeaponDeactivationNeeded = true;
                    foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsMaster || ESCache.Instance.Weapons.All(x => !x._module.IsMaster) && !i.IsReloadingAmmo && !i.IsInLimboState))
                    {
                        thisWeapon.Click();
                    }

                    return false;
                }

                Log.WriteLine("Finished deactivating all weapons: proceed to changing / reloading ammo!!");
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool HandleWrongAmmoOutOfBestAmmoUseWhatWeHave()
        {
            try
            {
                if (CachedAmmoDirectItem == null)
                {
                    Log.WriteLine("if (CachedAmmoDirectItem == null)");
                    return false;
                }

                //
                // Detect we are done changing ammo
                //
                if (ESCache.Instance.Weapons.All(i => i.ChargeQty > 0 && i.Charge.TypeId == CachedAmmoDirectItem.TypeId))
                {
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                TryingToChangeOrReloadAmmo = true;

                if (!DeactivateAllWeapons()) return false;

                int intCount = 0;
                if (ESCache.Instance.Weapons.Any(i => i._module.IsMaster))
                {
                    foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsMaster && (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                    {
                        intCount++;
                        Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                        thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                        {
                            continue;
                        }

                        return false;
                    }

                    return false;
                }

                foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                {
                    intCount++;
                    Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                    thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        continue;
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

        private static bool HandleChangeToDefaultAmmo()
        {
            try
            {
                Combat.Combat.BoolReloadWeaponsAsap = false;

                if (CachedAmmoDirectItem == null)
                {
                    Log.WriteLine("if (CachedAmmoDirectItem == null)");
                    if (Combat.Combat.UsableAmmoInCargo.Any())
                    {
                        return false;
                    }

                    Log.WriteLine("Do we have no ammo in cargo?!");
                    return false;
                }

                if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))
                {
                    TryingToChangeOrReloadAmmo = false;
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleChangeToDefaultAmmo: if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                //
                // Detect we are done changing ammo
                //
                if (ESCache.Instance.Weapons.All(i => !i.IsCivilianWeapon && i.ChargeQty > 0 && i.Charge.TypeId == CachedAmmoDirectItem.TypeId))
                {
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                TryingToChangeOrReloadAmmo = true;

                if (!DeactivateAllWeapons()) return false;

                int intCount = 0;

                if (ESCache.Instance.Weapons.Any(i => i._module.IsMaster))
                {
                    foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsMaster && (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                    {
                        intCount++;
                        if (thisWeapon.ChangeAmmo(CachedAmmoDirectItem))
                        {
                            Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                        }
                        else Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "] - failed?");

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                        {
                            continue;
                        }

                        return false;
                    }

                    return false;
                }

                foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => !i.IsCivilianWeapon && (i.ChargeQty == 0 || i.Charge.TypeId != CachedAmmoDirectItem.TypeId) && !i.IsReloadingAmmo && !i.IsInLimboState))
                {
                    intCount++;
                    Log.WriteLine("Changing [" + intCount + "][" + thisWeapon.TypeName + "][" + thisWeapon.ItemId + "] to use [" + CachedAmmoDirectItem.TypeName + "] instead of [" + thisWeapon.ChargeName + "]");
                    thisWeapon.ChangeAmmo(CachedAmmoDirectItem);
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        continue;
                    }

                    return false;
                }

                /**
                 *
                 * if (ESCache.Instance.Weapons.Any(i => i._module.IsMaster && !i.IsActive && !i.IsReloadingAmmo && i.IsOnline && !i.IsInLimboState))
                {
                    bool DefaultToUseLongRangeAmmoWhenWeHaveNoTargets = true;
                    if (DefaultToUseLongRangeAmmoWhenWeHaveNoTargets)
                    {
                        DirectItem AmmoToLoadIntoWeapon = AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance(60000, MissionSettings.CurrentDamageType).OrderBy(i => i.DefinedAsAmmoType.Range).FirstOrDefault();
                        if (!ESCache.Instance.Weapons.FirstOrDefault(i => i._module.IsMaster && !i.IsActive && !i.IsReloadingAmmo && i.IsOnline && !i.IsInLimboState).ChangeAmmo(AmmoToLoadIntoWeapon))
                        {
                            Log.WriteLine("if (!ESCache.Instance.Weapons.FirstOrDefault(i => i._module.IsMaster).ChangeAmmo(AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance(60000, CurrentDamageType)))");
                        }
                    }
                    else
                    {
                        DirectItem AmmoToLoadIntoWeapon = AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance(500, MissionSettings.CurrentDamageType).OrderBy(i => i.DefinedAsAmmoType.Range).FirstOrDefault();
                        if (!ESCache.Instance.Weapons.FirstOrDefault(i => i.i._module.IsMaster && !i.IsActive && !i.IsReloadingAmmo && i.IsOnline && !i.IsInLimboState).ChangeAmmo(AmmoToLoadIntoWeapon))
                        {
                            Log.WriteLine("if (!ESCache.Instance.Weapons.FirstOrDefault(i => i._module.IsMaster).ChangeAmmo(AmmoOfThisDamageTypeInCargoThatCanReachThisSpecificDistance(60000, CurrentDamageType)))");
                        }
                    }
                }
                 *
                 * **/

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool HandleWeaponsNeedReload()
        {
            try
            {
                if (!Combat.Combat.UsableAmmoInCargo.Any())
                {
                    TryingToChangeOrReloadAmmo = false;
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: if (!Combat.Combat.UsableAmmoInCargo.Any())");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 1");
                if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))
                {
                    TryingToChangeOrReloadAmmo = false;
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 2");
                //
                // Detect we are done changing ammo
                //
                int intCount = 0;
                if (ESCache.Instance.Weapons.All(i => i.IsEnergyWeapon && i.ChargeQty != 0))
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: if (ESCache.Instance.Weapons.All(i => i.IsEnergyWeapon && i.ChargeQty != 0))");
                    intCount = 0;
                    foreach (var thisWeapon in ESCache.Instance.Weapons)
                    {
                        intCount++;
                        Log.WriteLine("HandleWeaponsNeedReload: [" + intCount + "][" + thisWeapon.TypeName + "] IsEnergyWeapon [" + thisWeapon.IsEnergyWeapon + "] ChargeQty [" + thisWeapon.ChargeQty + "] ChargeName [" + thisWeapon.ChargeName + "]");
                    }

                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 3");

                if (ESCache.Instance.Weapons.Where(i => i.IsTurret || i.IsMissileLauncher).All(i => i.Charge != null && i.ChargeQty > i.MaxCharges - 8))
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: if (ESCache.Instance.Weapons.Where(i => i.IsTurret || i.IsMissileLauncher).All(i => i.Charge != null && i.ChargeQty > i.MaxCharges - 8))");
                    intCount = 0;
                    foreach (var thisWeapon in ESCache.Instance.Weapons.Where(i => i.IsTurret || i.IsMissileLauncher))
                    {
                        intCount++;
                        Log.WriteLine("HandleWeaponsNeedReload: [" + intCount + "][" + thisWeapon.TypeName + "] MaxCharges [" + thisWeapon.MaxCharges + "] ChargeQty [" + thisWeapon.ChargeQty + "] ChargeName [" + thisWeapon.ChargeName + "]");
                    }

                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 4");

                intCount = 0;
                foreach (var thisWeapon in ESCache.Instance.Weapons.Where(i => i.IsTurret || i.IsMissileLauncher))
                {
                    intCount++;
                    Log.WriteLine("HandleWeaponsNeedReload: [" + intCount + "][" + thisWeapon.TypeName + "] MaxCharges [" + thisWeapon.MaxCharges + "] ChargeQty [" + thisWeapon.ChargeQty + "] ChargeName [" + thisWeapon.ChargeName + "]");
                }

                if (ESCache.Instance.Weapons.Where(i => i.IsTurret || i.IsMissileLauncher).All(i => !i.WeaponNeedsAmmo))
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("Civilian guns do not use ammo.");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 5");

                TryingToChangeOrReloadAmmo = true;

                if (!DeactivateAllWeapons()) return false;

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: DeactivateAllWeapons() true");
                if (ESCache.Instance.Weapons.Any(weapon => weapon.ChargeQty > 0 &&
                    (weapon.GroupId == (int)Group.RapidHeavyMissileLaunchers || weapon.GroupId == (int)Group.RapidLightMissileLaunchers) &&
                    Combat.Combat.PotentialCombatTargets.Count > 0 &&
                    !ESCache.Instance.MyShipEntity.HasInitiatedWarp && !ESCache.Instance.InWarp))
                {
                    if (DebugConfig.DebugAmmoManagement)
                        Log.WriteLine("if (weapon.ChargeQty > 0 && weapon.GroupId == Group.RapidHeavyMissileLaunchers  || weapon.GroupId == Group.RapidLightMissileLaunchers && PotentialCombatTargets.Any())");
                    return true;
                }

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 6");

                if (Time.Instance.LastInWarp.AddSeconds(5) > DateTime.UtcNow)
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("ReloadAll: if (Time.Instance.LastInWarp.AddSeconds(30) > DateTime.UtcNow)");
                    return true;
                }

                bool UseEveCommandToReload = true;
                if (ESCache.Instance.Weapons.Any(i => i.IsTurret && !i.IsEnergyWeapon && i.Charge == null))
                {
                    UseEveCommandToReload = false;
                }

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 7");

                if (DebugConfig.DebugAmmoManagement)
                {
                    int iCount = 0;
                    Log.WriteLine("-------------------------------------------");
                    Log.WriteLine("-------------------------------------------");
                    Log.WriteLine("-------------------------------------------");
                    foreach (var weapon in ESCache.Instance.Weapons)
                    {
                        iCount++;
                        Log.WriteLine("[" + iCount + "][" + weapon.TypeName + "] IsMaster [" + weapon._module.IsMaster + "] ChargeQty [" + weapon.ChargeQty + "] MaxCharges [" + weapon.MaxCharges + "] IsActive [" + weapon.IsActive + "] Limbo [" + weapon.IsInLimboState + "]");
                    }
                    Log.WriteLine("-------------------------------------------");
                    Log.WriteLine("-------------------------------------------");
                    Log.WriteLine("-------------------------------------------");
                }

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 8");

                if (ESCache.Instance.Weapons.Where(i => i.IsTurret || i.IsMissileLauncher).Any(weapon => 1 >= weapon.ChargeQty && !weapon.IsReloadingAmmo && weapon.IsOnline && !weapon.IsActive && !weapon.IsDeactivating && !weapon.IsInLimboState))
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 9");
                    if (ESCache.Instance.Weapons.All(i => i.Charge != null && i.ChargeQty > 0))
                    {
                        Log.WriteLine("UseEveCommandToReload");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    }
                    else
                    {
                        Log.WriteLine("ReloadAmmo");
                        intCount = 0;
                        foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i.IsReadyToReloadAmmo))
                        {
                            Log.WriteLine("foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i.IsReadyToReloadAmmo))");
                            intCount++;

                            int chargeTypeIDToUse = 0;
                            if (ESCache.Instance.Weapons.Where(i => i.Charge != null).Any())
                            {
                                chargeTypeIDToUse = ESCache.Instance.Weapons.Where(i => i.Charge != null).FirstOrDefault().Charge.TypeId;
                            }

                            if (chargeTypeIDToUse != 0)
                            {
                                if (Combat.Combat.UsableAmmoInCargo.Any(i => i.TypeId == chargeTypeIDToUse))
                                {
                                    thisWeapon.ChangeAmmo(Combat.Combat.UsableAmmoInCargo.FirstOrDefault(i => i.TypeId == chargeTypeIDToUse));
                                    continue;
                                }
                            }

                            thisWeapon.ChangeAmmo(Combat.Combat.UsableAmmoInCargo.FirstOrDefault());
                            continue;
                        }
                    }
                }

                if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedReload: 10");
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool HandleWeaponsNeedSpecialReload()
        {
            try
            {
                if (!Combat.Combat.UsableSpecialAmmoInCargo.Any())
                {
                    TryingToChangeOrReloadAmmo = false;
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedSpecialReload: if (!Combat.Combat.UsableAmmoInCargo.Any())");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))
                {
                    TryingToChangeOrReloadAmmo = false;
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("HandleWeaponsNeedSpecialReload: if (ESCache.Instance.Weapons.Any(i => i.IsCivilianWeapon))");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                //
                // Detect we are done changing ammo
                //
                int intCount = 0;
                if (ESCache.Instance.Weapons.Any(i => i.IsEnergyWeapon && i.ChargeQty != 0))
                {
                    intCount = 0;
                    foreach (var thisWeapon in ESCache.Instance.Weapons)
                    {
                        intCount++;
                        Log.WriteLine("HandleWeaponsNeedSpecialReload: [" + intCount + "][" + thisWeapon.TypeName + "] IsEnergyWeapon [" + thisWeapon.IsEnergyWeapon + "] ChargeQty [" + thisWeapon.ChargeQty + "] ChargeName [" + thisWeapon.ChargeName + "]");
                    }

                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (ESCache.Instance.Weapons.All(i => i.ChargeQty == i.MaxCharges || i.ChargeQty > 8))
                {
                    intCount = 0;
                    foreach (var thisWeapon in ESCache.Instance.Weapons)
                    {
                        intCount++;
                        Log.WriteLine("HandleWeaponsNeedSpecialReload: [" + intCount + "][" + thisWeapon.TypeName + "] MaxCharges [" + thisWeapon.MaxCharges + "] ChargeQty [" + thisWeapon.ChargeQty + "] ChargeName [" + thisWeapon.ChargeName + "]");
                    }

                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                if (ESCache.Instance.Weapons.All(i => !i.WeaponNeedsAmmo))
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("Civilian guns do not use ammo.");
                    ChangeAmmoManagementBehaviorState(AmmoManagementBehaviorState.Monitor);
                    return true;
                }

                TryingToChangeOrReloadAmmo = true;

                if (!DeactivateAllWeapons()) return false;

                if (ESCache.Instance.Weapons.Any(weapon => weapon.ChargeQty > 0 &&
                    (weapon.GroupId == (int)Group.RapidHeavyMissileLaunchers || weapon.GroupId == (int)Group.RapidLightMissileLaunchers) &&
                    Combat.Combat.PotentialCombatTargets.Count > 0 &&
                    !ESCache.Instance.MyShipEntity.HasInitiatedWarp && !ESCache.Instance.InWarp))
                {
                    if (DebugConfig.DebugAmmoManagement)
                        Log.WriteLine("if (weapon.ChargeQty > 0 && weapon.GroupId == Group.RapidHeavyMissileLaunchers  || weapon.GroupId == Group.RapidLightMissileLaunchers && PotentialCombatTargets.Any())");
                    return true;
                }

                if (Time.Instance.LastInWarp.AddSeconds(5) > DateTime.UtcNow)
                {
                    if (DebugConfig.DebugAmmoManagement) Log.WriteLine("ReloadAll: if (Time.Instance.LastInWarp.AddSeconds(30) > DateTime.UtcNow)");
                    return true;
                }

                bool UseEveCommandToReload = true;
                if (ESCache.Instance.Weapons.Any(i => i.IsTurret && !i.IsEnergyWeapon && i.Charge == null))
                {
                    UseEveCommandToReload = false;
                }

                if (DebugConfig.DebugAmmoManagement)
                {
                    int iCount = 0;
                    Log.WriteLine("-------------------------------------------");
                    Log.WriteLine("-------------------------------------------");
                    Log.WriteLine("-------------------------------------------");
                    foreach (var weapon in ESCache.Instance.Weapons)
                    {
                        iCount++;
                        Log.WriteLine("[" + iCount + "][" + weapon.TypeName + "] IsMaster [" + weapon._module.IsMaster + "] ChargeQty [" + weapon.ChargeQty + "] MaxCharges [" + weapon.MaxCharges + "] IsActive [" + weapon.IsActive + "] Limbo [" + weapon.IsInLimboState + "]");
                    }
                    Log.WriteLine("-------------------------------------------");
                    Log.WriteLine("-------------------------------------------");
                    Log.WriteLine("-------------------------------------------");
                }

                if (ESCache.Instance.Weapons.Any(weapon => (weapon._module.IsMaster || ESCache.Instance.Weapons.All(x => !x._module.IsMaster)) && 1 >= weapon.ChargeQty && !weapon.IsReloadingAmmo && weapon.IsOnline && !weapon.IsActive && !weapon.IsDeactivating && !weapon.IsInLimboState))
                {
                    if (UseEveCommandToReload)
                    {
                        Log.WriteLine("if (UseEveCommandToReload)");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    }
                    else
                    {
                        Log.WriteLine("!if (UseEveCommandToReload)");
                        intCount = 0;
                        foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i.IsReadyToReloadAmmo))
                        {
                            Log.WriteLine("foreach (ModuleCache thisWeapon in ESCache.Instance.Weapons.Where(i => i.IsReadyToReloadAmmo))");
                            intCount++;
                            Combat.Combat.ReloadThisSpecialAmmo(thisWeapon, intCount);
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        #endregion Methods
    }
}