extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Behaviors
{
    public class DrWhoDeadspaceBehavior
    {
        #region Constructors

        private DrWhoDeadspaceBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        private static int _abyssalDeadspaceFilamentTypeId;
        private static int AbyssalSitePrerequisiteCheckRetries;
        private static DateTime LastAbyssalSitePrerequisiteCheck = DateTime.UtcNow.AddDays(-1);
        //public static bool AbyssalDeadspaceControllerEnabled = false; //defaults to off, XML setting can enable it


        #endregion Fields

        #region Properties

        public static int numAbyssalFillamentsToBring = 1;

        //ranges for clouds (and towers) afaik
        //Small = 5k
        //Medium = 15k
        //Large = 40k
        private static int AbyssalDeadspaceCloudRange = 40000;

        private static int AbyssalDeadspaceTowerRange = 15000;

        public static bool AbyssalDeadspaceAvoidBioluminescenceClouds
        {
            get
            {
                if (Combat.Combat.PotentialCombatTargets.Count == 0)
                    return false;

                if (Combat.Combat.PotentialCombatTargets.Count <= 4 && Combat.Combat.PotentialCombatTargets.All(i => !i.IsNPCBattleship))
                    return false;

                if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Count > 0 && GlobalAbyssalDeadspaceAvoidBioluminescenceClouds)
                    return GlobalAbyssalDeadspaceAvoidBioluminescenceClouds;

                return false;
            }
        }

        public static bool AbyssalDeadspaceAvoidTachyonClouds
        {
            get
            {
                if (Combat.Combat.PotentialCombatTargets.Count == 0)
                    return false;

                if (Combat.Combat.PotentialCombatTargets.Count <= 4 && Combat.Combat.PotentialCombatTargets.All(i => !i.IsNPCBattleship))
                    return false;

                if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Count > 0 && GlobalAbyssalDeadspaceAvoidTachyonClouds)
                    return GlobalAbyssalDeadspaceAvoidTachyonClouds;

                return false;
            }
        }

        public static bool AbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers
        {
            get
            {
                if (Combat.Combat.PotentialCombatTargets.Count == 0)
                    return false;

                if (Combat.Combat.PotentialCombatTargets.Count <= 4 && Combat.Combat.PotentialCombatTargets.All(i => !i.IsNPCBattleship))
                    return false;

                if (ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Count > 0 && GlobalAbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers)
                    return GlobalAbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers;

                return false;
            }
        }

        public static bool AbyssalDeadspaceAvoidFilamentClouds
        {
            get
            {
                if (Combat.Combat.PotentialCombatTargets.Count == 0)
                    return false;

                if (Combat.Combat.PotentialCombatTargets.Count <= 4 && Combat.Combat.PotentialCombatTargets.All(i => !i.IsNPCBattleship))
                    return false;

                if (ESCache.Instance.AbyssalDeadspaceFilamentCloud.Count > 0 && GlobalAbyssalDeadspaceAvoidFilamentClouds)
                    return GlobalAbyssalDeadspaceAvoidFilamentClouds;

                return false;
            }
        }

        public static bool AbyssalDeadspaceAvoidMultibodyTrackingPylonTowers
        {
            get
            {
                if (Combat.Combat.PotentialCombatTargets.Count == 0)
                    return false;

                if (Combat.Combat.PotentialCombatTargets.Count <= 4 && Combat.Combat.PotentialCombatTargets.All(i => !i.IsNPCBattleship))
                    return false;

                if (ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.Count > 0 && GlobalAbyssalDeadspaceAvoidMultibodyTrackingPylonTowers)
                    return GlobalAbyssalDeadspaceAvoidMultibodyTrackingPylonTowers;

                return false;
            }
        }

        public static string AbyssalDeadspaceBookmarkName { get; set; }
        public static string AbyssalDeadspaceFilamentName { get; set; }
        public static int? AbyssalDeadspaceFilamentsToStock { get; set; }
        public static int? AbyssalDeadspaceFilamentsToLoad
        {
            get
            {
                if (ESCache.Instance.ActiveShip == null)
                    return 1;

                if (ESCache.Instance.ActiveShip.IsFrigate)
                    return 3;

                if (ESCache.Instance.ActiveShip.IsDestroyer)
                    return 2;

                return 1;
            }
        }


        public static int? AbyssalDeadspaceFilamentTypeId
        {
            get
            {
                try
                {
                   // ESCache.Instance.DirectEve.InvTypeNames.TryGetValue(AbyssalDeadspaceFilamentName, out _abyssalDeadspaceFilamentTypeId);
                    return _abyssalDeadspaceFilamentTypeId;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        //light blue
        private static bool GlobalAbyssalDeadspaceAvoidBioluminescenceClouds { get; set; }

        //yellow
        private static bool GlobalAbyssalDeadspaceAvoidTachyonClouds { get; set; }

        //Short-Range Deviant Suppressor: 15k
        //Medium-Range Deviant Suppressor: 40k
        private static bool GlobalAbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers { get; set; }

        //https://wiki.eveuniversity.org/Abyssal_Deadspace
        //orange
        private static bool GlobalAbyssalDeadspaceAvoidFilamentClouds { get; set; }

        //boost to tracking
        private static bool GlobalAbyssalDeadspaceAvoidMultibodyTrackingPylonTowers { get; set; }

        private static string HomeBookmarkName { get; set; }

        #endregion Properties

        #region Methods

        public static bool ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentAbyssalDeadspaceBehaviorState != _StateToSet)
                {
                    if (_StateToSet == AbyssalDeadspaceBehaviorState.GotoHomeBookmark)
                    {
                        //WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, "AbyssalPocketNumber", 0);
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                    }

                    if (_StateToSet == AbyssalDeadspaceBehaviorState.GotoAbyssalBookmark)
                    {
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                    }

                    if (_StateToSet == AbyssalDeadspaceBehaviorState.ExecuteMission)
                    {
                        State.CurrentCombatMissionCtrlState = ActionControlState.Start;
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.AtDestination;
                    }

                    Log.WriteLine("New AbyssalDeadspaceBehaviorState [" + _StateToSet + "]");
                    State.CurrentAbyssalDeadspaceBehaviorState = _StateToSet;
                    if (ESCache.Instance.InStation && !wait) ProcessState();
                    if (State.CurrentAbyssalDeadspaceBehaviorState == AbyssalDeadspaceBehaviorState.GotoHomeBookmark) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static bool DoWeHave_Ship()
        {
            return false;
        }

        public static bool? ShoppingTripForAmmoNeeded()
        {
            bool? DoWeHaveAmmo = DoWeHave_Ammo();
            bool? DoWeHaveDrones = DoWeHave_Drones();
            bool? DoWeHaveFilaments = DoWeHave_Filaments();

            if (DoWeHaveAmmo == null || DoWeHaveDrones == null || DoWeHaveFilaments == null)
                return null;

            if (DoWeHaveAmmo != null && !(bool)DoWeHaveAmmo)
                return true;

            if (DoWeHaveDrones != null && !(bool)DoWeHaveDrones)
                return true;

            if (DoWeHaveFilaments != null && !(bool)DoWeHaveFilaments)
                return true;

            return false;
        }

        private static long Estimate_Market_Ship_Cost = 1000000;

        private static long Estimate_Market_Ammo_Cost = 1000000;

        private static long Estimate_Market_Drones_Cost = 1000000;

        private static long Estimate_Market_Filaments_Cost = 1000000;

        private static bool DoWeHaveIskToBuy(long ItemWeWantToBuy)
        {
            if (ESCache.Instance.DirectEve.Wallet.Wealth > ItemWeWantToBuy)
                return true;

            return false;
        }

        private static bool DoWeHaveIskToBuy_Ship()
        {
            return DoWeHaveIskToBuy(Estimate_Market_Ship_Cost);
        }

        private static bool DoWeHaveIskToBuy_Ammo()
        {
            return DoWeHaveIskToBuy(Estimate_Market_Ammo_Cost);
        }

        private static bool DoWeHaveIskToBuy_Drones()
        {
            return DoWeHaveIskToBuy(Estimate_Market_Drones_Cost);
        }

        private static bool DoWeHaveIskToBuy_Filaments()
        {
            return DoWeHaveIskToBuy(Estimate_Market_Filaments_Cost);
        }

        public static bool ShoppingTripForShipNeeded()
        {
            if (!DoWeHave_Ship())
                return true;

            return false;
        }

        public static bool? DoWeHave_Ammo()
        {
            if (ESCache.Instance.InSpace)
                return true;

            if (ESCache.Instance.InStation)
            {
                if (Time.Instance.LastDockAction.AddSeconds(8) < DateTime.UtcNow)
                    return null;

                //if (Arm.)

                return false;
            }


            return false;
        }

        public static bool? DoWeHave_Filaments()
        {
            if (ESCache.Instance.InSpace)
                return true;

            if (ESCache.Instance.InStation)
            {
                if (Time.Instance.LastDockAction.AddSeconds(8) < DateTime.UtcNow)
                    return null;

                //if (Arm.)
            }


            return false;
        }

        public static bool? DoWeHave_Drones()
        {
            if (ESCache.Instance.InSpace)
                return true;

            if (ESCache.Instance.InStation)
            {
                if (Time.Instance.LastDockAction.AddSeconds(8) < DateTime.UtcNow)
                    return null;

                //if (Arm.)
            }


            return false;
        }

        public static int NumOfYellowBoxingAbyssalBCsToPullDrones = 2;
        public static int NumOfYellowBoxingAbyssalFrigsToPullDrones = 6;
        public static int NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones = 2;
        public static int NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones = 2;
        public static int NumOfYellowBoxingAbyssalNPCsToPullDrones = 6;

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            Log.WriteLine("LoadSettings: DrWhoDeadspaceBehavior");
            //AbyssalDeadspaceControllerEnabled =
            //    (bool?)CharacterSettingsXml.Element("AbyssalDeadspaceControllerEnabled") ??
            //    (bool?)CommonSettingsXml.Element("AbyssalDeadspaceControllerEnabled") ?? true;
            NumOfYellowBoxingAbyssalBCsToPullDrones =
                (int?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalBCsToPullDrones") ??
                (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalBCsToPullDrones") ?? 2;
            Log.WriteLine("DrWhoDeadspaceBehavior: NumOfYellowBoxingAbyssalBCsToPullDrones [" + NumOfYellowBoxingAbyssalBCsToPullDrones + "]");
            NumOfYellowBoxingAbyssalFrigsToPullDrones =
                (int?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalFrigsToPullDrones") ??
                (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalFrigsToPullDrones") ?? 6;
            Log.WriteLine("DrWhoDeadspaceBehavior: NumOfYellowBoxingAbyssalBCsToPullDrones [" + NumOfYellowBoxingAbyssalBCsToPullDrones + "]");
            NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones =
                (int?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones") ??
                (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones") ?? 2;
            Log.WriteLine("DrWhoDeadspaceBehavior: NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones [" + NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones + "]");
            NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones =
                 (int ?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones") ??
                 (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones") ?? 2;
            Log.WriteLine("DrWhoDeadspaceBehavior: NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones [" + NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones + "]");
            NumOfYellowBoxingAbyssalNPCsToPullDrones =
                (int?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalNPCsToPullDrones") ??
                (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalNPCsToPullDrones") ?? 6;
            Log.WriteLine("DrWhoDeadspaceBehavior: NumOfYellowBoxingAbyssalNPCsToPullDrones [" + NumOfYellowBoxingAbyssalNPCsToPullDrones + "]");
            AbyssalDeadspaceBookmarkName =
                (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmark") ?? (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmark") ??
                (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmarks") ?? (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmarks") ??
                (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmark") ?? (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmark") ??
                (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmarks") ?? (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmarks") ?? "abyssal";
            Log.WriteLine("DrWhoDeadspaceBehavior: AbyssalDeadspaceBookmarkName [" + AbyssalDeadspaceBookmarkName + "]");
            HomeBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CharacterSettingsXml.Element("HomeBookmark") ??
                (string)CommonSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            Log.WriteLine("DrWhoDeadspaceBehavior: HomeBookmarkName [" + HomeBookmarkName + "]");
            AbyssalDeadspaceFilamentName =
                (string)CharacterSettingsXml.Element("AbyssalDeadspaceFilamentName") ??
                (string)CommonSettingsXml.Element("AbyssalDeadspaceFilamentName") ?? "Calm Firestorm Filament";
            Log.WriteLine("DrWhoDeadspaceBehavior: AbyssalDeadspaceFilamentName [" + AbyssalDeadspaceFilamentName + "] TypeId [" + AbyssalDeadspaceFilamentTypeId + "]");
            AbyssalDeadspaceFilamentsToStock =
                (int?)CharacterSettingsXml.Element("AbyssalDeadspaceFilamentsToStock") ??
                (int?)CommonSettingsXml.Element("AbyssalDeadspaceFilamentsToStock") ?? 1;
            Log.WriteLine("DrWhoDeadspaceBehavior: AbyssalDeadspaceFilamentsToStock [" + AbyssalDeadspaceFilamentsToStock + "]");
            GlobalAbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers =
                (bool?)CharacterSettingsXml.Element("AbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers") ??
                (bool?)CommonSettingsXml.Element("AbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers") ?? true;
            Log.WriteLine("DrWhoDeadspaceBehavior: GlobalAbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers [" + GlobalAbyssalDeadspaceAvoidDeviantAutomataSuppressorTowers + "]");
            GlobalAbyssalDeadspaceAvoidMultibodyTrackingPylonTowers =
                (bool?)CharacterSettingsXml.Element("AbyssalDeadspaceAvoidMultibodyTrackingPylonTowers") ??
                (bool?)CommonSettingsXml.Element("AbyssalDeadspaceAvoidMultibodyTrackingPylonTowers") ?? true;
            Log.WriteLine("DrWhoDeadspaceBehavior: GlobalAbyssalDeadspaceAvoidMultibodyTrackingPylonTowers [" + GlobalAbyssalDeadspaceAvoidMultibodyTrackingPylonTowers + "]");
            AbyssalDeadspaceCloudRange =
                (int?)CharacterSettingsXml.Element("AbyssalDeadspaceCloudRange") ??
                (int?)CommonSettingsXml.Element("AbyssalDeadspaceCloudRange") ?? 40000;
            Log.WriteLine("DrWhoDeadspaceBehavior: AbyssalDeadspaceCloudRange [" + AbyssalDeadspaceCloudRange + "]");
            AbyssalDeadspaceTowerRange =
                (int?)CharacterSettingsXml.Element("AbyssalDeadspaceTowerRange") ??
                (int?)CommonSettingsXml.Element("AbyssalDeadspaceTowerRange") ?? 15000;
            Log.WriteLine("DrWhoDeadspaceBehavior: AbyssalDeadspaceTowerRange [" + AbyssalDeadspaceTowerRange + "]");
            GlobalAbyssalDeadspaceAvoidBioluminescenceClouds =
                (bool?)CharacterSettingsXml.Element("AbyssalDeadspaceAvoidBioluminescenceClouds") ??
                (bool?)CommonSettingsXml.Element("AbyssalDeadspaceAvoidBioluminescenceClouds") ?? false;
            Log.WriteLine("DrWhoDeadspaceBehavior: GlobalAbyssalDeadspaceAvoidBioluminescenceClouds [" + GlobalAbyssalDeadspaceAvoidBioluminescenceClouds + "]");
            GlobalAbyssalDeadspaceAvoidTachyonClouds =
                (bool?)CharacterSettingsXml.Element("AbyssalDeadspaceAvoidTachyonClouds") ??
                (bool?)CommonSettingsXml.Element("AbyssalDeadspaceAvoidTachyonClouds") ?? false;
            Log.WriteLine("DrWhoDeadspaceBehavior: GlobalAbyssalDeadspaceAvoidTachyonClouds [" + GlobalAbyssalDeadspaceAvoidTachyonClouds + "]");
            GlobalAbyssalDeadspaceAvoidFilamentClouds =
                (bool?)CharacterSettingsXml.Element("AbyssalDeadspaceAvoidFilamentClouds") ??
                (bool?)CommonSettingsXml.Element("AbyssalDeadspaceAvoidFilamentClouds") ?? false;
            Log.WriteLine("DrWhoDeadspaceBehavior: GlobalAbyssalDeadspaceAvoidFilamentClouds [" + GlobalAbyssalDeadspaceAvoidFilamentClouds + "]");
            HealthCheckMinimumShieldPercentage =
                (int?)CharacterSettingsXml.Element("HealthCheckMinimumShieldPercentage") ??
                (int?)CharacterSettingsXml.Element("healthCheckMinimumShieldPercentage") ??
                (int?)CommonSettingsXml.Element("HealthCheckMinimumShieldPercentage") ??
                (int?)CommonSettingsXml.Element("healthCheckMinimumShieldPercentage") ?? 30;
            Log.WriteLine("DrWhoDeadspaceBehavior: HealthCheckMinimumShieldPercentage [" + HealthCheckMinimumShieldPercentage + "]");
            HealthCheckMinimumArmorPercentage =
                (int?)CharacterSettingsXml.Element("HealthCheckMinimumArmorPercentage") ??
                (int?)CharacterSettingsXml.Element("healthCheckMinimumArmorPercentage") ??
                (int?)CommonSettingsXml.Element("HealthCheckMinimumArmorPercentage") ??
                (int?)CommonSettingsXml.Element("healthCheckMinimumArmorPercentage") ?? 30;
            Log.WriteLine("DrWhoDeadspaceBehavior: HealthCheckMinimumArmorPercentage [" + HealthCheckMinimumArmorPercentage + "]");
            HealthCheckMinimumCapacitorPercentage =
                (int?)CharacterSettingsXml.Element("HealthCheckMinimumCapacitorPercentage") ??
                (int?)CharacterSettingsXml.Element("healthCheckMinimumCapacitorPercentage") ??
                (int?)CommonSettingsXml.Element("HealthCheckMinimumCapacitorPercentage") ??
                (int?)CommonSettingsXml.Element("healthCheckMinimumCapacitorPercentage") ?? 30;
            Log.WriteLine("DrWhoDeadspaceBehavior: HealthCheckMinimumCapacitorPercentage [" + HealthCheckMinimumCapacitorPercentage + "]");

            AbyssalPocketWarningSeconds =
                (int?)CharacterSettingsXml.Element("abyssalPocketWarningSeconds") ??
                (int?)CommonSettingsXml.Element("abyssalPocketWarningSeconds") ?? 360;
            Log.WriteLine("DrWhoDeadspaceBehavior: AbyssalPocketWarningSeconds [" + AbyssalPocketWarningSeconds + "]");

            TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway =
                (bool?)CharacterSettingsXml.Element("TriglavianConstructionSiteSpawnFoundDozenPlusBSSpawn_RunAway") ??
                (bool?)CommonSettingsXml.Element("TriglavianConstructionSiteSpawnFoundDozenPlusBSSpawn_RunAway") ??
            (bool?)CharacterSettingsXml.Element("abyssalConstructionSite14BSSpawnRunAway") ??
                (bool?)CommonSettingsXml.Element("abyssalConstructionSite14BSSpawnRunAway") ?? true;
            Log.WriteLine("DrWhoDeadspaceBehavior: abyssalConstructionSite14BSSpawnRunAway [" + TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway + "]");
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("State.CurrentAbyssalDeadspaceBehaviorState is [" + State.CurrentAbyssalDeadspaceBehaviorState + "]");

                switch (State.CurrentAbyssalDeadspaceBehaviorState)
                {
                    case AbyssalDeadspaceBehaviorState.Idle:
                        MissionSettings.MissionSafeDistanceFromStructure = 22000;
                        IdleCMBState();
                        break;

                    case AbyssalDeadspaceBehaviorState.Start:
                        StartCMBState();
                        break;

                    case AbyssalDeadspaceBehaviorState.Switch:
                        SwitchCMBState();
                        break;

                    case AbyssalDeadspaceBehaviorState.Arm:
                        ArmCMBState((int)AbyssalDeadspaceFilamentsToLoad);
                        break;

                    case AbyssalDeadspaceBehaviorState.LocalWatch:
                        LocalWatchCMBState();
                        break;

                    case AbyssalDeadspaceBehaviorState.WaitingforBadGuytoGoAway:
                        WaitingFoBadGuyToGoAway();
                        break;

                    case AbyssalDeadspaceBehaviorState.WarpOutStation:
                        WarpOutBookmarkCMBState();
                        break;

                    case AbyssalDeadspaceBehaviorState.GotoAbyssalBookmark:
                        GotoAbyssalBookmarkState();
                        break;

                    case AbyssalDeadspaceBehaviorState.ActivateAbyssalDeadspace:
                        ActivateAbyssalDeadspaceState();
                        break;

                    case AbyssalDeadspaceBehaviorState.ExecuteMission:
                        if (ESCache.Instance.InAbyssalDeadspace && !ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache) && Combat.Combat.PotentialCombatTargets.Count == 0 && ESCache.Instance.AccelerationGates.Any(i => 30000 > i.Distance))
                        {
                            if (!Combat.Combat.BoolReloadWeaponsAsap && ESCache.Instance.Weapons.Any(i => !i.IsCivilianWeapon))
                            {
                                Log.WriteLine("ExecuteMission: BoolReloadWeaponsAsap = true");
                                Combat.Combat.BoolReloadWeaponsAsap = true;
                            }
                        }
                        Salvage.LootWhileSpeedTanking = true;
                        AbyssalSitePrerequisiteCheckRetries = 0;
                        ExecuteAbyssalDeadspaceSiteState();
                        break;

                    case AbyssalDeadspaceBehaviorState.GotoHomeBookmark:
                        GotoHomeBookmarkState();
                        break;

                    case AbyssalDeadspaceBehaviorState.UnloadLoot:
                        UnloadLootCMBState();
                        break;

                    case AbyssalDeadspaceBehaviorState.Traveler:
                        TravelerCMBState();
                        break;

                    case AbyssalDeadspaceBehaviorState.Default:
                        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Idle, false);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool AbyssalSitePrerequisiteCheck()
        {
            if (DateTime.UtcNow < LastAbyssalSitePrerequisiteCheck.AddMinutes(10))
            {
                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                AbyssalSitePrerequisiteCheckRetries++;
                return false;
            }

            if (AbyssalSitePrerequisiteCheckRetries > 3)
            {
                Log.WriteLine("AbyssalSitePrerequisiteCheck: if (AbyssalSitePrerequisiteCheckRetries > 10): go home");
                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                AbyssalSitePrerequisiteCheckRetries++;
                return false;
            }

            if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
            {
                if (ESCache.Instance.CurrentShipsCargo.Items.All(i => i.GroupId != (int) Group.AbyssalDeadspaceFilament))
                {
                    Log.WriteLine("AbyssalSitePrerequisiteCheck: We have no filaments in our cargo: go home");
                    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                    AbyssalSitePrerequisiteCheckRetries++;
                    return false;
                }

                if (ESCache.Instance.Modules.Any(m => !m.IsOnline))
                {
                    Log.WriteLine("AbyssalSitePrerequisiteCheck: We have offline [" + ESCache.Instance.Modules.Count(m => !m.IsOnline) + "] modules: go home");
                    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                    AbyssalSitePrerequisiteCheckRetries++;
                    return false;
                }

                DirectItem abyssalFilament = ESCache.Instance.CurrentShipsCargo.Items.Find(i => i.GroupId == (int) Group.AbyssalDeadspaceFilament);
                if (abyssalFilament != null && !abyssalFilament.IsSafeToUseAbyssalKeyHere)
                {
                    Log.WriteLine("AbyssalSitePrerequisiteCheck: We have a filament but it is not safe to activate it here! go home and pause");
                    ESCache.Instance.PauseAfterNextDock = true;
                    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                    AbyssalSitePrerequisiteCheckRetries++;
                    return false;
                }

                if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.All(i => !i.IsEnergyWeapon))
                {
                    if (ESCache.Instance.CurrentShipsCargo.UsedCapacity > ESCache.Instance.CurrentShipsCargo.Capacity * .8)
                    {
                        Log.WriteLine("AbyssalSitePrerequisiteCheck: Less than 80% of our cargo space left: go home");
                        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                        AbyssalSitePrerequisiteCheckRetries++;
                        return false;
                    }

                    if (ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.CategoryId == (int)CategoryID.Charge))
                    {
                        foreach (DirectItem ammoItem in ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.CategoryId == (int)CategoryID.Charge))
                        {
                            Log.WriteLine("AbyssalSitePrerequisiteCheck: CargoHoldItem: [" + ammoItem.TypeName + "] TypeId [" + ammoItem.TypeId + "] Quantity [" + ammoItem.Quantity + "]");

                            if (ammoItem.Quantity < 650 && ESCache.Instance.Weapons.All(i => i.GroupId == (int)Group.ProjectileWeapon ||
                                                                                              i.GroupId == (int)Group.HybridWeapon ||
                                                                                              i.GroupId == (int)Group.VortonProjector ||
                                                                                              i.IsMissileLauncher))
                            {
                                Log.WriteLine("AbyssalSitePrerequisiteCheck: Less than 1000 units, go back to base");
                                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                                AbyssalSitePrerequisiteCheckRetries++;
                                return false;
                            }

                            if (ammoItem.Quantity < 500 && ESCache.Instance.Weapons.All(i => i.GroupId == (int)Group.PrecursorWeapon))
                            {
                                Log.WriteLine("AbyssalSitePrerequisiteCheck: Less than 500 units of Precursor weapon ammo, go back to base");
                                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                                AbyssalSitePrerequisiteCheckRetries++;
                                return false;
                            }
                        }

                        Log.WriteLine("AbyssalSitePrerequisiteCheck: We have enough ammo.");
                        AbyssalSitePrerequisiteCheckRetries = 0;
                        return true;
                        /**
                        if (Drones.UseDrones)
                        {
                            //if (Drones.DroneBay != null && Drones.DroneBay.Capacity > 0)
                            //{
                            //    if (Drones.DroneBay.Capacity - Drones.DroneBay.UsedCapacity >= 25)
                            //    {
                            //        Log.WriteLine("AbyssalSitePrerequisiteCheck: if (Drones.DroneBay.Capacity - Drones.DroneBay.UsedCapacity >= 25)");
                            //        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark);
                            //        return false;
                            //    }
                            //
                            Log.WriteLine("AbyssalSitePrerequisiteCheck: We assume we have enough drones!?!");
                            LastAbyssalSitePrerequisiteCheck = DateTime.UtcNow;
                            AbyssalSitePrerequisiteCheckRetries = 0;
                            return true;
                            //}

                            //AbyssalSitePrerequisiteCheckRetries++;
                            //return false;
                        }
                        Log.WriteLine("AbyssalSitePrerequisiteCheck: UseDrones is false");
                        AbyssalSitePrerequisiteCheckRetries = 0;
                        return true;
                        **/
                    }

                    Log.WriteLine("AbyssalSitePrerequisiteCheck: We have no ammo left in the cargo!");
                    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                    AbyssalSitePrerequisiteCheckRetries++;
                    return false;
                }

                AbyssalSitePrerequisiteCheckRetries = 0;
                return true;
            }

            Log.WriteLine("AbyssalSitePrerequisiteCheck: We have no items in our cargo!");
            ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
            return false;
        }

        private static int AbyssalFilamentsActivated { get; set; } = 0;
        private static DateTime AbyssalSiteStarted = DateTime.MinValue;
        private static DateTime AbyssalRoom1Started = DateTime.MinValue;
        private static DateTime AbyssalRoom2Started = DateTime.MinValue;
        private static DateTime AbyssalRoom3Started = DateTime.MinValue;
        private static DateTime AbyssalSiteFinished = DateTime.MinValue;
        private static Tuple<DateTime, DateTime, DateTime, DateTime> AbyssalFinishTimes = new Tuple<DateTime, DateTime, DateTime, DateTime>(DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
        public static int HealthCheckMinimumShieldPercentage = 30;
        public static int HealthCheckMinimumArmorPercentage = 30;
        public static int HealthCheckMinimumCapacitorPercentage = 30;

        private static DirectKeyActivationWindow _keyActivationWindow = null;

        public static DirectKeyActivationWindow keyActivationWindow
        {
            get
            {
                if (_keyActivationWindow == null)
                {
                    _keyActivationWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectKeyActivationWindow>().FirstOrDefault();
                    return _keyActivationWindow ?? null;
                }

                return _keyActivationWindow ?? null;
            }
        }

        private static DirectAbyssActivationWindow _abyssActivationWindow = null;

        public static DirectAbyssActivationWindow abyssActivationWindow
        {
            get
            {
                if (_abyssActivationWindow == null)
                {
                    _abyssActivationWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectAbyssActivationWindow>().FirstOrDefault();
                    return _abyssActivationWindow ?? null;
                }

                return _abyssActivationWindow ?? null;
            }
        }

        private static void ActivateAbyssalDeadspaceState()
        {
            try
            {
                if (ESCache.Instance.InStation)
                {
                    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Idle, false);
                    return;
                }

                if (!ESCache.Instance.InSpace)
                    return;

                if (ESCache.Instance.InAbyssalDeadspace)
                    return;

                if (ESCache.Instance.MyShipEntity == null)
                    return;

                if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                    return;

                if (ESCache.Instance.DockableLocations.Any(station => station.IsOnGridWithMe) || ESCache.Instance.Stargates.Any(stargate => stargate.IsOnGridWithMe))
                    return;

                if (!HandleAbyssalTrace()) return; //Used with Destroyer and Frigate Abyssals - technically exists for cruiser abyssals but you auto jump so you dont usually see it
                if (!HandleDrWhoDeadspaceActivationWindow()) return; //Used with Destroyer and Frigate Abyssals
                if (!HandleKeyActivationWindow()) return; //Used with all abyssals to activate the gate and with cruiser abyssals you auto jump - destroyer and frigate abyssals you have 2 other steps
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return;
            }
        }

        private static bool HandleDrWhoDeadspaceActivationWindow()
        {
            try
            {
                if (abyssActivationWindow != null && abyssActivationWindow.PyWindow.IsValid)
                {
                    if (Time.Instance.LastActivateAbyssalActivationWindowAttempt.AddSeconds(20) > DateTime.UtcNow)
                    {
                        Log.WriteLine("We have activated the DrWhoDeadspaceActivationWindow: Waiting to be moved to AbyssalDeadspace");
                        return false;
                    }

                    Log.WriteLine("ActivateAbyssalDeadspaceState: Found drWhoActivationWindow");
                    if (abyssActivationWindow.IsReady && !abyssActivationWindow.IsJumping && ESCache.Instance.MyShipEntity.Velocity > 0)
                    {
                        Log.WriteLine("ActivateAbyssalDeadspaceState: if (abyssActivationWindow.IsReady && !abyssActivationWindow.IsJumping && ESCache.Instance.MyShipEntity.Velocity > 0)");
                        if (DateTime.UtcNow > Time.Instance.NextActivateAccelerationGate)
                        {
                            if (abyssActivationWindow.Activate())
                            {
                                Log.WriteLine("ActivateAbyssalDeadspaceState: Activating abyssActivationWindow");
                                Time.Instance.LastActivateAbyssalActivationWindowAttempt = DateTime.UtcNow;
                                Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(13);
                                return false;
                            }

                            Log.WriteLine("ActivateAbyssalDeadspaceState: Activating abyssActivationWindow failed: waiting");
                            return false;
                        }
                    }

                    if (abyssActivationWindow.IsReady && abyssActivationWindow.IsJumping)
                    {
                        Log.WriteLine("ActivateAbyssalDeadspaceState: Found abyssActivationWindow: if (abyssActivationWindow.IsReady && abyssActivationWindow.IsJumping)");
                        return false;
                    }

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        private static bool HandleKeyActivationWindow()
        {
            try
            {
                if (keyActivationWindow != null && keyActivationWindow.PyWindow.IsValid)
                {
                    if (Time.Instance.LastActivateKeyActivationWindowAttempt.AddSeconds(20) > DateTime.UtcNow)
                    {
                        Log.WriteLine("We have activated the KeyActivationWindow: Waiting to be moved to AbyssalDeadspace");
                        return false;
                    }

                    if (ESCache.Instance.MyShipEntity.Velocity < ESCache.Instance.ActiveShip.MaxVelocity * .25)
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);

                    if (20 > ESCache.Instance.MyShipEntity.Velocity)
                        return false;

                    Log.WriteLine("ActivateAbyssalDeadspaceState: Found KeyActivationWindow");
                    if (keyActivationWindow.IsReady && !keyActivationWindow.IsJumping && ESCache.Instance.MyShipEntity.Velocity > 0)
                    {
                        Log.WriteLine("ActivateAbyssalDeadspaceState: if (keyWindow.IsReady && !keyWindow.IsJumping && ESCache.Instance.MyShipEntity.Velocity > 0)");
                        if (DateTime.UtcNow > Time.Instance.NextActivateAccelerationGate)
                        {
                            if (keyActivationWindow.Activate())
                            {
                                Log.WriteLine("ActivateAbyssalDeadspaceState: Activating Filament");
                                Time.Instance.LastActivateKeyActivationWindowAttempt = DateTime.UtcNow;
                                Time.Instance.NextActivateKeyActivationWindow = DateTime.UtcNow.AddSeconds(8);
                                AbyssalFilamentsActivated++;
                                Log.WriteLine("ActivateAbyssalDeadspaceState: AbyssalFilamentsActivated this session [" + AbyssalFilamentsActivated + "]");
                                //AbyssalSiteStarted =
                                //    Abyssal
                                return false;
                            }

                            Log.WriteLine("ActivateAbyssalDeadspaceState: Activating Filament failed: waiting");
                            return false;
                        }
                    }

                    if (keyActivationWindow.IsReady && keyActivationWindow.IsJumping)
                    {
                        Log.WriteLine("ActivateAbyssalDeadspaceState: Found KeyActivationWindow: if (keywindow.IsReady && keywindow.IsJumping)");
                        return false;
                    }

                    if (ESCache.Instance.MyShipEntity.Velocity < ESCache.Instance.ActiveShip.MaxVelocity * .25)
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdAccelerate);

                    return false;
                }

                if (keyActivationWindow == null)
                {
                    if (!AbyssalSitePrerequisiteCheck()) return false;

                    if (!HandleFilaments()) return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        private static bool HandleFilaments()
        {
            try
            {
                DirectItem fila = ESCache.Instance.CurrentShipsCargo.Items.Find(i => i.GroupId == (int)Group.AbyssalDeadspaceFilament && i.TypeId == AbyssalDeadspaceFilamentTypeId);
                if (fila != null)
                {
                    if (ESCache.Instance.MyShipEntity.Velocity < ESCache.Instance.ActiveShip.MaxVelocity * .25)
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);

                    Log.WriteLine($"Activating filament.");
                    if (fila.ActivateAbyssalKey())
                    {
                        Time.Instance.LastActivateFilamentAttempt = DateTime.UtcNow;
                        Log.WriteLine("Activated AbyssalKey [" + fila.TypeName + "] TypeId [" + fila.TypeId + "] GroupId [" + fila.GroupId + "] Quantity [" + fila.Quantity + "] Window will open next...");
                        return false;
                    }

                    Log.WriteLine("Failed to ActivateAbyssalKey [" + fila.TypeName + "] TypeId [" + fila.TypeId + "] GroupId [" + fila.GroupId + "] Quantity [" + fila.Quantity + "]");
                    return false;
                }

                Log.WriteLine("Failed to find any filaments in your cargo");
                foreach (DirectItem item in ESCache.Instance.CurrentShipsCargo.Items)
                    Log.WriteLine("Item [" + item.TypeName + "] GroupId [" + item.GroupId + "] TypeId [" + item.TypeId + "]");

                Log.WriteLine("-------------------------------------------");
                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        private static bool HandleAbyssalTrace()
        {
            try
            {
                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.GroupId == (int)Group.AbyssalTrace) && ESCache.Instance.ClosestStation != null && !ESCache.Instance.ClosestStation.IsOnGridWithMe)
                {
                    var AbyssalTrace = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.GroupId == (int)Group.AbyssalTrace);
                    if (AbyssalTrace.Distance > (double)Distances.GateActivationRange)
                    {
                        Log.WriteLine("if (AbyssalTrace.Distance > (double)Distances.CloseToGateActivationRange)");
                        AbyssalTrace.Orbit(500);
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.Velocity < 100)
                    {
                        Log.WriteLine("if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.Velocity < 100)");
                        AbyssalTrace.Orbit(500);
                        return false;
                    }
                    if (ESCache.Instance.ActiveShip.FollowingEntity == null || (ESCache.Instance.ActiveShip.FollowingEntity != null && ESCache.Instance.ActiveShip.FollowingEntity.Id == AbyssalTrace.Id))
                    {
                        AbyssalTrace.Orbit(500);
                    }

                    if (abyssActivationWindow == null)
                    {
                        Log.WriteLine("Found abyssActivationWindow: AbyssalTrace.Activate();");
                        AbyssalTrace.ActivateAccelerationGate();
                        return false;
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

        private static void ArmCMBState(int FilamentsToLoad = 1)
        {
            numAbyssalFillamentsToBring = FilamentsToLoad;

            if (!AttemptToBuyAmmo()) return;

            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin Arm");
                Arm.ChangeArmState(ArmState.Begin, false, null);
            }

            if (!ESCache.Instance.InStation) return;

            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.NotEnoughAmmo ||
                State.CurrentArmState == ArmState.NotEnoughDrones)
            {
                if (Settings.Instance.BuyAmmo)
                {
                    Log.WriteLine("Armstate [" + State.CurrentArmState + "]: BuyAmmo [" + Settings.Instance.BuyAmmo + "]");
                    ESCache.Instance.EveAccount.LastAmmoBuy.AddDays(-1);
                    Arm.ChangeArmState(ArmState.Done, false, null);
                    return;
                }

                Log.WriteLine("Armstate [" + State.CurrentArmState + "]: BuyAmmo [" + Settings.Instance.BuyAmmo + "]");
                Arm.ChangeArmState(ArmState.NotEnoughAmmo, true, null);
                return;
            }

            if (State.CurrentArmState == ArmState.Done)
            {
                Arm.ChangeArmState(ArmState.Idle, false, null);

                //if (Settings.Instance.BuyAmmo && BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.DisabledForThisSession)
                //{
                //    BuyAmmoController.CurrentBuyAmmoState = BuyAmmoState.Idle;
                //    ControllerManager.Instance.RemoveController(typeof(BuyAmmoController));
                //}

                State.CurrentDroneControllerState = DroneControllerState.WaitingForTargets;
                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.LocalWatch, false);
            }
        }

        private static bool AttemptToBuyAmmo()
        {
            if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace || ESCache.Instance.InWormHoleSpace)
                return true;

            //if (Settings.Instance.BuyAmmo)
            //    if (BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.Done && BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.DisabledForThisSession)
            //        if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastAmmoBuy.AddHours(8) && DateTime.UtcNow > ESCache.Instance.EveAccount.LastBuyLpItemAttempt.AddHours(1))
            //        {
            //            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastAmmoBuyAttempt), DateTime.UtcNow);
            //            ControllerManager.Instance.AddController(new BuyAmmoController());
            //            return false;
            //        }

            return true;
        }

        private static bool WeHaveBeenInPocketTooLong_WarningSent = false;

        public static void ClearPerPocketCache()
        {
            ESCache.Instance.OldAccelerationGateId = null;
            AbyssalSpawn.ClearPerPocketCache();
            WeHaveBeenInPocketTooLong_WarningSent = false;
            return;
        }

        private static int AbyssalPocketWarningSeconds = 360;

        public static bool TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway { get; set; } = true;

        private static bool? _assumeGatesAreLockedOrThereAreNoGates = null;

        public static bool AssumeGatesAreLockedOrThereAreNoGates
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (_assumeGatesAreLockedOrThereAreNoGates == null)
                    {
                        if (TriglavianConstructionSiteSpawnFoundDozenPlusBSs)
                        {
                            if (TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway && ESCache.Instance.AccelerationGates.Count > 0) //&& 55000 > ESCache.Instance.AccelerationGates.FirstOrDefault().Distance)
                            {
                                _assumeGatesAreLockedOrThereAreNoGates = false;
                                return _assumeGatesAreLockedOrThereAreNoGates ?? false;
                            }
                        }

                        _assumeGatesAreLockedOrThereAreNoGates = true;
                        return _assumeGatesAreLockedOrThereAreNoGates ?? true;
                    }

                    return _assumeGatesAreLockedOrThereAreNoGates ?? true;
                }

                return true;
            }
        }

        private static bool? _triglavianConstructionSiteSpawnFoundDozenPlusBSs;

        public static bool TriglavianConstructionSiteSpawnFoundDozenPlusBSs
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    //
                    // what type of filament are we using? If its not a tier 5 filament we should just assume this cant happen
                    // https://wiki.eveuniversity.org/Filaments
                    //

                    if (!AbyssalDeadspaceFilamentName.ToLower().Contains("Chaotic".ToLower()))
                        return false;

                    if (_triglavianConstructionSiteSpawnFoundDozenPlusBSs == null)
                    {
                        if (ESCache.Instance.Entities.Any(i => i.TypeId == (int)TypeID.TriglavianConstruction01a) && ESCache.Instance.Entities.Count(i => i.Name.Contains("Leshak")) >= 6)
                        {
                            _triglavianConstructionSiteSpawnFoundDozenPlusBSs = true;
                            DebugConfig.DebugAbyssalDeadspaceBehavior = true;
                            DebugConfig.DebugCombat = true;
                            DebugConfig.DebugActivateWeapons = true;
                            DebugConfig.DebugKillTargets = true;

                            DebugConfig.DebugDisableCombat = true;
                            return _triglavianConstructionSiteSpawnFoundDozenPlusBSs ?? true;
                        }

                        DebugConfig.DebugAbyssalDeadspaceBehavior = false;
                        DebugConfig.DebugCombat = false;
                        DebugConfig.DebugActivateWeapons = false;
                        DebugConfig.DebugKillTargets = false;
                        DebugConfig.DebugDisableCombat = false;
                        _triglavianConstructionSiteSpawnFoundDozenPlusBSs = false;
                        return _triglavianConstructionSiteSpawnFoundDozenPlusBSs ?? false;
                    }

                    return _triglavianConstructionSiteSpawnFoundDozenPlusBSs ?? false;
                }

                _triglavianConstructionSiteSpawnFoundDozenPlusBSs = false;
                return _triglavianConstructionSiteSpawnFoundDozenPlusBSs ?? false;
            }
        }

        //private static void ProcessAlerts()
        //{
        //    Time.ItHasBeenThisLongSinceWeStartedThePocket = Statistics.StartedPocket - DateTime.UtcNow;
        //    int minutesInPocket = Time.ItHasBeenThisLongSinceWeStartedThePocket.Minutes;
        //    if (minutesInPocket > (AbyssalPocketWarningSeconds / 60) && !WeHaveBeenInPocketTooLong_WarningSent)
        //    {
        //        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ABYSSAL_POCKET_TIME, "AbyssalDeadspace: FYI: We have been in pocket number [" + ActionControl.PocketNumber + "] for [" + minutesInPocket + "] min: You might need to check that we arnt stuck."));
        //        WeHaveBeenInPocketTooLong_WarningSent = true;
        //        Log.WriteLine("We have been in AbyssalPocket [" + ActionControl.PocketNumber + "] for more than [" + AbyssalPocketWarningSeconds + "] seconds!");
        //        return;
        //    }
        //
        //    return;
        //}

        private static DateTime WaitInStationUntil = DateTime.MinValue;

        private static bool EveryPulse()
        {
            try
            {
                if (ESCache.Instance.InStation)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("EveryPulse: if (ESCache.Instance.InStation)");
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("EveryPulse: UseScheduler [" + ESCache.Instance.EveAccount.UseScheduler + "]");
                    if (ESCache.Instance.EveAccount.UseScheduler)
                    {
                        if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("EveryPulse: UseScheduler [" + ESCache.Instance.EveAccount.UseScheduler + "] was true");
                        if (ESCache.Instance.EveAccount.ShouldBeStopped)
                        {
                            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("EveryPulse: if (ESCache.Instance.EveAccount.ShouldBeStopped) was true");
                            ESCache.Instance.CloseEveReason = "if (ESCache.Instance.EveAccount.ShouldBeStopped)";
                            ESCache.Instance.BoolCloseEve = true;
                            return false;
                        }

                        if (AbyssalFilamentsActivated >= ESCache.Instance.EveAccount.NumOfAbyssalSitesBeforeRestarting)
                        {
                            //close eve here: if the schedule is on the launcher will restart
                            ESCache.Instance.CloseEveReason = "AbyssalDeadspaceController: We have done [" + ESCache.Instance.EveAccount.NumOfAbyssalSitesBeforeRestarting + "] Abyssal sites, restarting eve";
                            ESCache.Instance.BoolRestartEve = true;
                            return false;
                        }
                    }

                    if (AbyssalFilamentsActivated == ESCache.Instance.EveAccount.NumOfAbyssalSitesBeforeWaitingInStationForRandomTime)
                    {
                        WaitInStationUntil = DateTime.UtcNow.AddMinutes(ESCache.Instance.EveAccount.BaseTimeToWaitInStationForRandomTime + ESCache.Instance.RandomNumber(0, 9));
                    }

                    if (AbyssalFilamentsActivated > ESCache.Instance.EveAccount.NumOfAbyssalSitesBeforeWaitingInStationForRandomTime)
                    {
                        if (WaitInStationUntil > DateTime.UtcNow)
                        {
                            //close eve here: if the schedule is on the launcher will restart
                            Log.WriteLine("Waiting till [" + WaitInStationUntil.ToShortTimeString() + "]... AbyssalFilamentsActivated [" + AbyssalFilamentsActivated + "] > NumOfAbyssalSitesBeforeWaitingInStationForRandomTime [" + ESCache.Instance.EveAccount.NumOfAbyssalSitesBeforeRestarting + "]");
                            return false;
                        }
                    }

                    if (AbyssalSitePrerequisiteCheckRetries > 3)
                    {
                        Log.WriteLine("AbyssalDeadspaceController: Start: AbyssalSitePrerequisiteCheckRetries > 3: Pausing");
                        ControllerManager.Instance.SetPause(true);
                    }
                }

                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: CMBEveryPulse: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)");
                    return false;
                }

                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: CMBEveryPulse: if (ESCache.Instance.InAbyssalDeadspace)");

                    if (State.CurrentAbyssalDeadspaceBehaviorState != AbyssalDeadspaceBehaviorState.ExecuteMission)
                        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.ExecuteMission);

                    //ProcessAlerts();
                    //
                    // if we are in Abyssal Deadspace we do not need to process localsafe or panic...
                    //
                    return true;
                }

                if (Time.Instance.LastActivateFilamentAttempt.AddSeconds(5) > DateTime.UtcNow)
                    return false;

                if (Time.Instance.LastActivateKeyActivationWindowAttempt.AddSeconds(5) > DateTime.UtcNow)
                    return false;

                if (ESCache.Instance.InWormHoleSpace)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: CMBEveryPulse: If (ESCache.Instance.InWormHoleSpace)");
                    return true;
                }

                if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: CMBEveryPulse: if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)");
                    return true;
                }

                if (State.CurrentAbyssalDeadspaceBehaviorState != AbyssalDeadspaceBehaviorState.GotoHomeBookmark)
                    Panic.ProcessState(HomeBookmarkName);

                if (State.CurrentPanicState == PanicState.Resume)
                {
                    if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                    {
                        State.CurrentPanicState = PanicState.Normal;
                        State.CurrentTravelerState = TravelerState.Idle;
                        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoAbyssalBookmark, false);
                        return true;
                    }

                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: CMBEveryPulse: if (State.CurrentPanicState == PanicState.Resume)");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public static void ExecuteAbyssalDeadspaceSiteState()
        {
            if (!ESCache.Instance.InSpace)
            {
                Log.WriteLine("AbyssalDeadspaceBehavior: ExecuteAbyssalDeadspaceSiteState: if (!ESCache.Instance.InSpace)");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log.WriteLine("AbyssalDeadspaceBehavior: ExecuteAbyssalDeadspaceSiteState: if (ESCache.Instance.CurrentShipsCargo == null)");
                return;
            }

            if (!ESCache.Instance.InAbyssalDeadspace && DateTime.UtcNow > Time.Instance.LastActivateFilamentAttempt.AddSeconds(60))
            {
                Log.WriteLine("AbyssalDeadspaceBehavior: ExecuteMission: InRegularSpace: Go Home");
                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                return;
            }

            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: ExecuteMission: _actionControl.ProcessState();");

            if (keyActivationWindow != null)
            {
                keyActivationWindow.Close();
            }

            if (abyssActivationWindow != null)
            {
                abyssActivationWindow.Close();
            }

            Salvage.OpenWrecks = true;

            try
            {
                ActionControl.ProcessState(null, null);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            if (NavigateOnGrid.ChooseNavigateOnGridTargetIds != null)
                NavigateOnGrid.NavigateIntoRange(NavigateOnGrid.ChooseNavigateOnGridTargetIds, "ClearPocket", true);
            else
                Log.WriteLine("if (NavigateOnGrid.ChooseNavigateOnGridTargets == null)");
        }

        private static void GotoAbyssalBookmarkState()
        {
            if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
            {
                TravelerDestination.Undock();
                return;
            }

            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoAbyssalBookmarkState: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "AbyssalDeadspaceBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoAbyssalBookmarkState: Traveler.TravelToBookmarkName([" + AbyssalDeadspaceBookmarkName +" ])");

            if (ESCache.Instance.DockableLocations.Any(i => i.IsOnGridWithMe))
                if (Time.Instance.LastDamagedModuleCheck.AddSeconds(10) < DateTime.UtcNow && ESCache.Instance.InSpace
                    && ESCache.Instance.Modules.Any(m => m.DamagePercent > 0)
                    && !ESCache.Instance.Paused
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

                    if (State.CurrentAbyssalDeadspaceBehaviorState != States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark)
                    {
                        Log.WriteLine("Damaged Modules Found! Go Home.");
                        State.CurrentAbyssalDeadspaceBehaviorState = States.AbyssalDeadspaceBehaviorState.GotoHomeBookmark;
                        return;
                    }

                    return;
                }

            Traveler.TravelToBookmarkName(AbyssalDeadspaceBookmarkName);

            if (ESCache.Instance.MyShipEntity.HasInitiatedWarp || ESCache.Instance.InWarp)
                return;

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                if (ESCache.Instance.CurrentShipsCargo == null) return;
                ESCache.Instance.CurrentShipsCargo.StackShipsCargo();
                ActionControl.ChangeCombatMissionCtrlState(ActionControlState.Start, null, null);
                Traveler.Destination = null;
                //if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.FleetAbyssalDeadspaceController))
                //{
                //    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.ActivateFleetAbyssalFilaments, true);
                //    return;
                //}

                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.ActivateAbyssalDeadspace, true);
            }
        }

        private static void GotoHomeBookmarkState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "AbyssalDeadspaceBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: Traveler.TravelToBookmarkName(" + HomeBookmarkName + ");");

            Traveler.TravelToBookmarkName(HomeBookmarkName);

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("HomeBookmark is defined as [" + HomeBookmarkName + "] and should be a bookmark of a station or citadel we can dock at: why are we still in space?!");
                    return;
                }

                Traveler.Destination = null;
                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Start, false);
            }
        }

        private static void IdleCMBState()
        {
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentDroneControllerState = DroneControllerState.Idle;
            State.CurrentSalvageState = SalvageState.Idle;
            State.CurrentStorylineState = StorylineState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;

            if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }
            else
            {
                Salvage.OpenWrecks = true;
            }

            ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
        }

        public static void InvalidateCache()
        {
            _abyssActivationWindow = null;
            _keyActivationWindow = null;
            _triglavianConstructionSiteSpawnFoundDozenPlusBSs = null;
            _assumeGatesAreLockedOrThereAreNoGates = null;
        }

        private static void LocalWatchCMBState()
        {
            if (Settings.Instance.UseLocalWatch)
            {
                Time.Instance.LastLocalWatchAction = DateTime.UtcNow;

                if (DebugConfig.DebugArm) Log.WriteLine("Starting: Is LocalSafe check...");
                if (ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    Log.WriteLine("local is clear");
                    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.WarpOutStation, false);
                    return;
                }

                Log.WriteLine("Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.WaitingforBadGuytoGoAway, false);
                return;
            }

            if (ESCache.Instance.DirectEve.Me.PVPTimerExist)
            {
                Log.WriteLine("AbyssalSitePrerequisiteCheck: We have pvp timer: waiting");
                return;
            }

            if (ESCache.Instance.DirectEve.Me.SuspectTimerExists)
            {
                Log.WriteLine("AbyssalSitePrerequisiteCheck: We have a suspect timer: waiting");
                return;
            }

            ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.WarpOutStation, false);
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("AbyssalDeadspaceBehavior.ResetStatesToDefaults: start");
            State.CurrentAbyssalDeadspaceBehaviorState = AbyssalDeadspaceBehaviorState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return;
        }

        private static void StartCMBState()
        {
            //
            // It takes 20 minutes (potentially) to do an abyssal site: if it is within 25min of Downtime (10:35 evetime) pause
            //
            if (Time.Instance.IsItDuringDowntimeNow)
            {
                Log.WriteLine("AbyssalDeadspaceController: Arm: Downtime is less than 25 minutes from now: Pausing");
                ControllerManager.Instance.SetPause(true);
                return;
            }

            ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Switch, false);
        }

        private static void SwitchCMBState()
        {
            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                Arm.SwitchShipsOnly = true;
                Arm.ChangeArmState(ArmState.ActivateCombatShip, false, null);
            }

            if (DebugConfig.DebugArm) Log.WriteLine("CombatMissionBehavior.Switch is Entering Arm.Processstate");
            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Done");
                Arm.SwitchShipsOnly = false;
                Arm.ChangeArmState(ArmState.Idle, true, null);
                ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.UnloadLoot, false);
            }
        }

        private static void TravelerCMBState()
        {
            try
            {
                if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                {
                    if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                List<long> destination = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();
                if (destination == null || destination.Count == 0)
                {
                    Log.WriteLine("No destination?");
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

                if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                    destination[0] = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.LastOrDefault())
                {
                    if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Count > 0)
                    {
                        IEnumerable<DirectBookmark> bookmarks = ESCache.Instance.CachedBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.Any())
                        {
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                            return;
                        }

                        Log.WriteLine("Destination: [" + ESCache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                        long lastSolarSystemInRoute = destination.LastOrDefault();

                        Log.WriteLine("Destination: [" + lastSolarSystemInRoute + "]");
                        Traveler.Destination = new SolarSystemDestination(destination.LastOrDefault());
                        return;
                    }

                    return;
                }

                Traveler.ProcessState();

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
                    {
                        Log.WriteLine("an error has occurred");
                        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Error, true);
                        return;
                    }

                    if (ESCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Error, false);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Idle, false);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void UnloadLootCMBState()
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return;

                if (State.CurrentUnloadLootState == UnloadLootState.Idle)
                {
                    Log.WriteLine("UnloadLoot: Begin");
                    State.CurrentUnloadLootState = UnloadLootState.Begin;
                }

                UnloadLoot.ProcessState();

                if (State.CurrentUnloadLootState == UnloadLootState.Done)
                {
                    State.CurrentUnloadLootState = UnloadLootState.Idle;

                    if (State.CurrentCombatState == CombatState.OutOfAmmo)
                        Log.WriteLine("State.CurrentCombatState == CombatState.OutOfAmmo");

                    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Arm, true);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void WaitingFoBadGuyToGoAway()
        {
            if (DateTime.UtcNow.Subtract(Time.Instance.LastLocalWatchAction).TotalMinutes <
                Time.Instance.WaitforBadGuytoGoAway_minutes + ESCache.Instance.RandomNumber(1, 3))
                return;

            ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.LocalWatch, false);
        }

        private static void WarpOutBookmarkCMBState()
        {
            if (ESCache.Instance.EveAccount.OtherToonsAreStillLoggingIn)
            {
                Log.WriteLine("WarpOutBookmarkCMBState: Waiting for other toons to finish logging in before we undock!");
                return;
            }

            if (!string.IsNullOrEmpty(Settings.Instance.UndockBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> warpOutBookmarks = ESCache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "");
                if (warpOutBookmarks != null && warpOutBookmarks.Any())
                {
                    DirectBookmark warpOutBookmark = warpOutBookmarks.FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.Distance < 10000000 && b.Distance > (int)Distances.WarptoDistance);

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Log.WriteLine("No Bookmark");
                        State.CurrentTravelerState = TravelerState.Idle;
                        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoAbyssalBookmark, false);
                    }
                    else if (warpOutBookmark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + warpOutBookmark.Title);
                            Traveler.Destination = new BookmarkDestination(warpOutBookmark);
                        }

                        Traveler.ProcessState();
                        if (State.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            State.CurrentTravelerState = TravelerState.Idle;
                            ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoAbyssalBookmark, false);
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmarks found starting with UndockBookmarkPrefix [" + Settings.Instance.UndockBookmarkPrefix + "] in local");
                        State.CurrentTravelerState = TravelerState.Idle;
                        ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoAbyssalBookmark, false);
                    }

                    return;
                }
            }

            Log.WriteLine("No Bookmark in System");
            ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoAbyssalBookmark, false);
        }

        #endregion Methods
    }
}