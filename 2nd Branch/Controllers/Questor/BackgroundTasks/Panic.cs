extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Questor.Activities;
using SC::SharedComponents.Utility;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.BackgroundTasks
{
    public static class Panic
    {
        #region Fields

        private static readonly Random _random = new Random();

        private static bool _delayedResume;
        private static bool _headedToRepairStation;

        private static Vec3 _lastNormalPositionInSpace;

        private static DateTime _lastPriorityTargetLogging = DateTime.UtcNow;

        private static DateTime _nextPanicProcessState;

        private static DateTime _nextWarpScrambledWarning;

        private static int _randomDelay;

        private static DateTime _resumeTime;

        private static int BookmarkMyWreckAttempts;

        private static int icount = 1;

        public static bool HeadedToRepairStation
        {
            get
            {
                if (!ESCache.Instance.NeedRepair)
                {
                    _headedToRepairStation = false;
                    return _headedToRepairStation;
                }

                if (ESCache.Instance.InMission)
                    return false;

                return _headedToRepairStation;
            }
            set => _headedToRepairStation = value;
        }

        #endregion Fields

        #region Properties

        public static int BattlecruiserInvasionLimit { get; set; }

        //
        // Invasion Settings
        //
        public static int BattleshipInvasionLimit { get; set; }

        public static int CruiserInvasionLimit { get; set; }
        public static int FrigateInvasionLimit { get; set; }
        public static int InvasionMinimumDelay { get; set; }
        public static int InvasionRandomDelay { get; set; }

        public static int MinimumArmorPct
        {
            get
            {
                if (MissionSettings.MinimumArmorPctMissionSetting != null)
                    return (int)MissionSettings.MinimumArmorPctMissionSetting;

                if (MinimumArmorPctGlobalSetting != null)
                    return (int)MinimumArmorPctGlobalSetting;

                return 90;
            }
        }

        public static int? MinimumArmorPctGlobalSetting { get; set; }

        public static int MinimumCapacitorPct
        {
            get
            {
                if (MissionSettings.MinimumCapacitorPctMissionSetting != null)
                    return (int)MissionSettings.MinimumCapacitorPctMissionSetting;

                if (MinimumCapacitorPctGlobalSetting != null)
                    return (int)MinimumCapacitorPctGlobalSetting;

                return 90;
            }
        }

        public static int? MinimumCapacitorPctGlobalSetting { get; set; }

        public static int MinimumShieldPct
        {
            get
            {
                if (MissionSettings.MinimumShieldPctMissionSetting != null)
                    return (int)MissionSettings.MinimumShieldPctMissionSetting;

                if (MinimumShieldPctGlobalSetting != null)
                    return (int)MinimumShieldPctGlobalSetting;

                return 40;
            }
        }

        public static int? MinimumShieldPctGlobalSetting { get; set; }
        public static int SafeArmorPct { get; set; }
        public static int SafeCapacitorPct { get; set; }
        public static int SafeShieldPct { get; set; }
        public static bool UseStationRepair { get; set; }

        private static bool EnablePanic
        {
            get
            {
                if (ESCache.Instance.InSpace)
                {
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (DebugConfig.DebugPanic) Log.WriteLine("if (ESCache.Instance.InAbyssalDeadspace)");
                        return false;
                    }

                    if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                    {
                        if (DebugConfig.DebugPanic) Log.WriteLine("if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Shuttle)
                    {
                        if (DebugConfig.DebugPanic) Log.WriteLine("if (ESCache.Instance.ActiveShip.GroupId == (int) Group.Shuttle)");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule && State.CurrentPanicState == PanicState.Normal)
                    {
                        Log.WriteLine("You are in a Capsule, you must have died in a mission :(");
                        ChangePanicState(PanicState.BookmarkMyWreck);
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.Entity == null)
                    {
                        if (DebugConfig.DebugPanic) Log.WriteLine("if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.Entity == null");
                        return false;
                    }

                    if (ESCache.Instance.InMission && MissionSettings.MyMission != null && MissionSettings.MyMission.Name.ToLower().Contains("anomic".ToLower()))
                    {
                        if (DebugConfig.DebugPanic) Log.WriteLine("if (ESCache.Instance.InMission && MissionSettings.MyMission != null && MissionSettings.MyMission.Name.ToLower().Contains(anomic.ToLower()))");
                        return false;
                    }

                    if ((long)ESCache.Instance.ActiveShip.StructurePercentage == 0) //if your hull is 0 you are dead or bugged, wait.
                    {
                        if (DebugConfig.DebugPanic) Log.WriteLine("if ((long) ESCache.Instance.ActiveShip.StructurePercentage == 0)");
                        return false;
                    }
                }

                if (State.CurrentHydraState == HydraState.Combat)
                {
                    if (DebugConfig.DebugPanic) Log.WriteLine("if (State.CurrentHydraState == HydraState.Combat)");
                    return false;
                }

                if (State.CurrentHydraState == HydraState.Leader)
                {
                    if (DebugConfig.DebugPanic) Log.WriteLine("if (State.CurrentHydraState == HydraState.Leader)");
                    return false;
                }

                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                {
                    return true;
                }

                return true;
            }
        }

        #endregion Properties

        #region Methods

        public static bool ChangePanicState(PanicState state)
        {
            try
            {
                if (State.CurrentPanicState != state)
                {
                    Log.WriteLine("New PanicState [" + state + "]");
                    State.CurrentPanicState = state;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: Panic");
                MinimumShieldPctGlobalSetting = (int?)CharacterSettingsXml.Element("minimumShieldPct") ??
                                                (int?)CommonSettingsXml.Element("minimumShieldPct") ?? 100;
                Log.WriteLine("Panic: minimumShieldPct [" + MinimumShieldPctGlobalSetting + "]");
                MinimumArmorPctGlobalSetting = (int?)CharacterSettingsXml.Element("minimumArmorPct") ??
                                               (int?)CommonSettingsXml.Element("minimumArmorPct") ?? 100;
                Log.WriteLine("Panic: minimumArmorPct [" + MinimumArmorPctGlobalSetting + "]");
                MinimumCapacitorPctGlobalSetting = (int?)CharacterSettingsXml.Element("minimumCapacitorPct") ??
                                                   (int?)CommonSettingsXml.Element("minimumCapacitorPct") ?? 50;
                Log.WriteLine("Panic: minimumCapacitorPct [" + MinimumCapacitorPctGlobalSetting + "]");
                SafeShieldPct = (int?)CharacterSettingsXml.Element("safeShieldPct") ?? (int?)CommonSettingsXml.Element("safeShieldPct") ?? 90;
                Log.WriteLine("Panic: safeShieldPct [" + SafeShieldPct + "]");
                SafeArmorPct = (int?)CharacterSettingsXml.Element("safeArmorPct") ?? (int?)CommonSettingsXml.Element("safeArmorPct") ?? 90;
                Log.WriteLine("Panic: safeArmorPct [" + SafeArmorPct + "]");
                SafeCapacitorPct = (int?)CharacterSettingsXml.Element("safeCapacitorPct") ??
                                   (int?)CommonSettingsXml.Element("safeCapacitorPct") ?? 80;
                Log.WriteLine("Panic: safeCapacitorPct [" + SafeCapacitorPct + "]");
                UseStationRepair = (bool?)CharacterSettingsXml.Element("useStationRepair") ??
                                   (bool?)CommonSettingsXml.Element("useStationRepair") ?? true;
                Log.WriteLine("Panic: useStationRepair [" + UseStationRepair + "]");

                //
                // Invasion Settings
                //
                try
                {
                    BattleshipInvasionLimit = (int?)CharacterSettingsXml.Element("battleshipInvasionLimit") ??
                                              (int?)CommonSettingsXml.Element("battleshipInvasionLimit") ?? 1;
                    Log.WriteLine("Panic: battleshipInvasionLimit [" + BattleshipInvasionLimit + "]");
                    // if this number of BattleShips lands on grid while in a mission we will enter panic
                    BattlecruiserInvasionLimit = (int?)CharacterSettingsXml.Element("battlecruiserInvasionLimit") ??
                                                 (int?)CommonSettingsXml.Element("battlecruiserInvasionLimit") ?? 1;
                    Log.WriteLine("Panic: battlecruiserInvasionLimit [" + BattlecruiserInvasionLimit + "]");
                    // if this number of BattleCruisers lands on grid while in a mission we will enter panic
                    CruiserInvasionLimit = (int?)CharacterSettingsXml.Element("cruiserInvasionLimit") ??
                                           (int?)CommonSettingsXml.Element("cruiserInvasionLimit") ?? 1;
                    Log.WriteLine("Panic: cruiserInvasionLimit [" + CruiserInvasionLimit + "]");
                    // if this number of Cruisers lands on grid while in a mission we will enter panic
                    FrigateInvasionLimit = (int?)CharacterSettingsXml.Element("frigateInvasionLimit") ??
                                           (int?)CommonSettingsXml.Element("frigateInvasionLimit") ?? 1;
                    Log.WriteLine("Panic: frigateInvasionLimit [" + FrigateInvasionLimit + "]");
                    // if this number of Frigates lands on grid while in a mission we will enter panic
                    InvasionRandomDelay = (int?)CharacterSettingsXml.Element("invasionRandomDelay") ??
                                          (int?)CommonSettingsXml.Element("invasionRandomDelay") ?? 300; // random delay to stay docked
                    Log.WriteLine("Panic: invasionRandomDelay [" + InvasionRandomDelay + "]");
                    InvasionMinimumDelay = (int?)CharacterSettingsXml.Element("invasionMinimumDelay") ??
                                           (int?)CommonSettingsXml.Element("invasionMinimumDelay") ?? 15;
                    Log.WriteLine("Panic: invasionMinimumDelay [" + InvasionMinimumDelay + "]");
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Invasion Settings: Exception [" + exception + "]");
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Error Loading Panic Settings [" + exception + "]");
            }
        }

        public static void ProcessState(string HomeBookmarkName)
        {
            if (DateTime.UtcNow < _nextPanicProcessState)
                return;

            _nextPanicProcessState = DateTime.UtcNow.AddMilliseconds(500);

            if (DebugConfig.DebugPanic) Log.WriteLine("PanicState [" + State.CurrentPanicState + "]");

            RunEveryPulse();

            switch (State.CurrentPanicState)
            {
                case PanicState.Idle:
                    if (!IdlePanicState()) return;
                    break;

                case PanicState.Normal:
                    if (!NormalPanicState()) return;
                    break;

                // NOTE: The difference between Panicking and StartPanicking is that the bot will move to "Panic" state once in warp & Panicking
                //       and the bot wont go into Panic mode while still "StartPanicking"
                case PanicState.StartPanicking:
                case PanicState.Panicking:
                    PanicingPanicState(HomeBookmarkName);
                    break;

                case PanicState.BookmarkMyWreck:
                    BookmarkMyWreckPanicState();
                    break;

                case PanicState.Panic:
                    if (!PanicPanicState()) return;
                    break;

                case PanicState.DelayedResume:
                    if (DateTime.UtcNow > _resumeTime)
                        ChangePanicState(PanicState.Resume);

                    break;

                case PanicState.Resume:
                    // Don't do anything here
                    break;
            }
        }

        private static void BookmarkMyWreckPanicState()
        {
            BookmarkMyWreckAttempts++;
            if (ESCache.Instance.Wrecks.Any(i => i.Name.ToLower().Contains(Combat.Combat.CombatShipName.ToLower())))
            {
                if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdCreateBookmark))
                {
                    SetStartPanickingState();
                    return;
                }

                return;
            }

            if (BookmarkMyWreckAttempts++ > 3)
                SetStartPanickingState();
        }

        private static bool IdlePanicState()
        {
            //
            // below is the reasons we will start the panic state(s) - if the below is not met do nothing
            //
            if (ESCache.Instance.InSpace &&
                ESCache.Instance.ActiveShip.Entity != null &&
                !ESCache.Instance.ActiveShip.Entity.IsCloaked &&
                ESCache.Instance.ActiveShip.Entity.GroupId != (int)Group.Capsule)
            {
                ChangePanicState(PanicState.Normal);
                return true;
            }

            return false;
        }

        private static bool CheckPanicReasons_Timers()
        {
            if (DebugConfig.DebugPanic) Log.WriteLine("CheckPanicReasons_Timers()");

            if (!ESCache.Instance.InSpace) return false;

            if (ESCache.Instance.DirectEve.Me.SuspectTimerExists && !ESCache.Instance.InWormHoleSpace)
            {
                Log.WriteLine(" Panic: SuspectTimer is active [ " + ESCache.Instance.DirectEve.Me.SuspectTimerRemainingSeconds + " sec]");
                SetStartPanickingState();
                return true;
            }

            if (ESCache.Instance.DirectEve.Me.CriminalTimerExists && !ESCache.Instance.InWormHoleSpace)
            {
                Log.WriteLine(" Panic: CriminalTimer is active [ " + ESCache.Instance.DirectEve.Me.CriminalTimerRemainingSeconds + " sec]");
                SetStartPanickingState();
                return true;
            }

            //if (ESCache.Instance.DirectEve.Me.IsInvasionActive && !ESCache.Instance.InWormHoleSpace && ESCache.Instance.SelectedController != "AbyssalDeadspaceController" && ESCache.Instance.EveAccount.SelectedController != "SalvageGridController")
            //{
            //    Log.WriteLine(" Panic: Invasion is active!");
            //    SetStartPanickingState();
            //    return true;
            //}

            return false;
        }

        private static bool CheckPanicReasons_OutOfAmmo()
        {
            return false;

            if (DebugConfig.DebugPanic) Log.WriteLine("CheckPanicReasons_OutOfAmmo()");

            if (!ESCache.Instance.InSpace) return false;

            if (Combat.Combat.PotentialCombatTargets.Count > 0 ||
                (ESCache.Instance.DirectEve.AgentMissions.Count > 0 &&
                ESCache.Instance.DirectEve.AgentMissions.Any(i => i.State == MissionState.Accepted) &&
                MissionSettings.MyMission != null && MissionSettings.MyMission.Name != "Transaction Data Delivery" && !MissionSettings.MyMission.Type.Contains("Trade") && !MissionSettings.MyMission.Type.Contains("Courier") && ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GivenName == Combat.Combat.CombatShipName))
            {
                if (Combat.Combat.OutOfAmmo)
                {
                    if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoBase)
                        return true;

                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    {
                        if (MissionSettings.MyMission != null)
                            Log.WriteLine("Panic: We are OutOfAmmo: MissionType [" + MissionSettings.MyMission.Type + "]");

                        Log.WriteLine("Panic: We are OutOfAmmo: Set CombatMissionsBehaviorState.GotoBase");
                        State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CheckPanicReasons_LogPriorityTargets()
        {
            if (DebugConfig.DebugPanic) Log.WriteLine("CheckPanicReasons_LogPriorityTargets()");

            if (!ESCache.Instance.InSpace) return false;

            int targetedByNpcPlayer = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsPlayer && e.IsTargetedBy);

            if (targetedByNpcPlayer > 0)
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.LOCKED_BY_PLAYER, "Locked by another player."));

            if (Math.Round(DateTime.UtcNow.Subtract(_lastPriorityTargetLogging).TotalSeconds) > Combat.Combat.ListPriorityTargetsEveryXSeconds)
            {
                _lastPriorityTargetLogging = DateTime.UtcNow;

                icount = 1;
                foreach (EntityCache target in Drones.DronePriorityEntities)
                {
                    icount++;
                    Log.WriteLine("[" + icount + "][" + target.Name + "][" + target.MaskedId + "][" + Math.Round(target.Distance / 1000, 0) +
                                  "k away] WARP[" +
                                  target.IsWarpScramblingMe + "] ECM[" + target.IsTryingToJamMe + "] Damp[" + target.IsSensorDampeningMe + "] TP[" +
                                  target.IsTargetPaintingMe + "] NEUT[" + target.IsNeutralizingMe + "]");
                }

                icount = 1;
                foreach (EntityCache target in Combat.Combat.PrimaryWeaponPriorityEntities)
                {
                    icount++;
                    Log.WriteLine("[" + icount + "][" + target.Name + "][" + target.MaskedId + "][" + Math.Round(target.Distance / 1000, 0) +
                                  "k away] WARP[" +
                                  target.IsWarpScramblingMe + "] ECM[" + target.IsTryingToJamMe + "] Damp[" + target.IsSensorDampeningMe + "] TP[" +
                                  target.IsTargetPaintingMe + "] NEUT[" + target.IsNeutralizingMe + "]");
                }
            }

            return false;
        }

        private static bool CheckPanicReasons_LowCapacitor()
        {
            if (DebugConfig.DebugPanic) Log.WriteLine("CheckPanicReasons_LowCapacitor()");

            if (!ESCache.Instance.InSpace) return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                return false;

            if (ESCache.Instance.ActiveShip.CapacitorPercentage < MinimumCapacitorPct && ESCache.Instance.ActiveShip.IsActiveTanked)
            {
                if (DebugConfig.DebugPanic) Log.WriteLine("DebugPanic: Capacitor is low: capacitor [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] below [" +
                                                          MinimumCapacitorPct + "%] S[" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] A[" +
                                                          Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] C[" +
                                                          Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%]");

                if (ESCache.Instance.InMission)
                {
                    if (DebugConfig.DebugPanic) Log.WriteLine("DebugPanic: Capacitor is low: InMission [" + ESCache.Instance.InMission + "]");
                    if (DateTime.UtcNow > Time.Instance.LastInWarp.AddSeconds(30))
                    {
                        if (DebugConfig.DebugPanic) Log.WriteLine("DebugPanic: Capacitor is low: We have been out of warp for more than 30 seconds");
                        // Only check for cap-panic while in a mission, not while doing anything else
                        Log.WriteLine("Start panicking, capacitor [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] below [" +
                                      MinimumCapacitorPct + "%] S[" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] A[" +
                                      Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] C[" +
                                      Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%]");

                        Statistics.PanicAttemptsThisMission++;
                        Statistics.PanicAttemptsThisPocket++;
                        HighSecAnomalyBehavior.ClearInfoDuringPanicSoThatWeWillReturnToThisAnom();
                        SetStartPanickingState();
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CheckPanicReasons_LowShields()
        {
            if (DebugConfig.DebugPanic) Log.WriteLine("CheckPanicReasons_LowShields()");

            if (!ESCache.Instance.InSpace) return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                return false;


            if (ESCache.Instance.ActiveShip.ShieldPercentage < MinimumShieldPct && !ESCache.Instance.ActiveShip.IsArmorTanked) // && !HeadedToRepairStation)
            {
                Log.WriteLine("Start panicking, shield [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] below [" +
                              MinimumShieldPct + "%] S[" +
                              Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] A[" +
                              Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] C[" +
                              Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%]");
                Statistics.PanicAttemptsThisMission++;
                Statistics.PanicAttemptsThisPocket++;
                HighSecAnomalyBehavior.ClearInfoDuringPanicSoThatWeWillReturnToThisAnom();
                SetStartPanickingState();
                return true;
            }

            return false;
        }

        private static bool CheckPanicReasons_LowArmor()
        {
            if (DebugConfig.DebugPanic) Log.WriteLine("CheckPanicReasons_LowArmor()");

            if (!ESCache.Instance.InSpace) return false;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                return false;

            if (ESCache.Instance.ActiveShip.ArmorPercentage < MinimumArmorPct && !ESCache.Instance.ActiveShip.IsShieldTanked) // && !HeadedToRepairStation)
            {
                Log.WriteLine("Start panicking, armor [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] below [" +
                              MinimumArmorPct + "%] S[" +
                              Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] A[" +
                              Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] C[" +
                              Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%]");
                Statistics.PanicAttemptsThisMission++;
                Statistics.PanicAttemptsThisPocket++;
                HighSecAnomalyBehavior.ClearInfoDuringPanicSoThatWeWillReturnToThisAnom();
                SetStartPanickingState();
                return true;
            }

            return false;
        }

        public static DateTime LastMissionInvadedByTimeStamp = DateTime.UtcNow.AddDays(-1);

        public static void ClearPerPocketCache()
        {
            LastMissionInvadedByTimeStamp = DateTime.UtcNow.AddDays(-1);
        }

        private static bool CheckPanicReasons_InvasionLimits()
        {
            if (DebugConfig.DebugPanic) Log.WriteLine("CheckPanicReasons_InvasionLimits()");

            if (!ESCache.Instance.InSpace) return false;

            if (ESCache.Instance.InWarp) return false;

            if (ESCache.Instance.ClosestStargate != null && (double)Distances.DirectionalScannerCloseRange > ESCache.Instance.ClosestStargate.Distance) return false;

            if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer)
                return false;

            if (ESCache.Instance.InMission || (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController)))
            {
                int frigates = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsFrigate && e.IsPlayer);
                int cruisers = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsCruiser && e.IsPlayer);
                int battlecruisers = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsBattlecruiser && e.IsPlayer);
                int battleships = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsBattleship && e.IsPlayer);
                EntityCache missionInvadedBy = ESCache.Instance.EntitiesNotSelf.Find(e => e.IsPlayer && (!e.IsFrigate && !e.IsCruiser && !e.IsBattleship && !e.IsMarauder && !e.IsT2Cruiser && !e.IsT2BattleCruiser && !e.IsPod && !e.IsShuttle && !e.IsHauler && !e.IsMiningShip));

                if (missionInvadedBy != null)
                    try
                    {
                        if (DateTime.UtcNow > LastMissionInvadedByTimeStamp.AddMinutes(10))
                        {
                            LastMissionInvadedByTimeStamp = DateTime.UtcNow;
                        }

                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                        {
                            if (LastMissionInvadedByTimeStamp.AddSeconds(ESCache.Instance.RandomNumber(95, 120)) > DateTime.UtcNow)
                            {
                                return false;
                            }

                            //Do We think this user could run this anomaly? if not we should stay, not panic!
                            //return
                        }

                        //DirectEvent
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.MISSION_INVADED,
                            "Mission was invaded by another player: " + missionInvadedBy.Name + " Ship: " + missionInvadedBy.TypeName));
                    }
                    catch (Exception)
                    {
                    }

                if (missionInvadedBy != null)
                {
                    _delayedResume = true;
                    Statistics.PanicAttemptsThisMission++;
                    Statistics.PanicAttemptsThisPocket++;
                    SetStartPanickingState();
                    Log.WriteLine("Start panicking, mission invaded by [" + missionInvadedBy.Name + "] Ship [" + missionInvadedBy.TypeName + "][" + missionInvadedBy.Nearest5kDistance + "k]");
                    return true;
                }

                if (FrigateInvasionLimit > 0 && frigates >= FrigateInvasionLimit)
                {
                    _delayedResume = true;

                    Statistics.PanicAttemptsThisMission++;
                    Statistics.PanicAttemptsThisPocket++;
                    SetStartPanickingState();
                    Log.WriteLine("Start panicking, mission invaded by [" + frigates + "] Frigates");
                    return true;
                }

                if (CruiserInvasionLimit > 0 && cruisers >= CruiserInvasionLimit)
                {
                    _delayedResume = true;

                    Statistics.PanicAttemptsThisMission++;
                    Statistics.PanicAttemptsThisPocket++;
                    SetStartPanickingState();
                    Log.WriteLine("Start panicking, mission invaded by [" + cruisers + "] Cruisers");
                    return true;
                }

                if (BattlecruiserInvasionLimit > 0 && battlecruisers >= BattlecruiserInvasionLimit)
                {
                    _delayedResume = true;

                    Statistics.PanicAttemptsThisMission++;
                    Statistics.PanicAttemptsThisPocket++;
                    SetStartPanickingState();
                    Log.WriteLine("Start panicking, mission invaded by [" + battlecruisers + "] BattleCruisers");
                    return true;
                }

                if (BattleshipInvasionLimit > 0 && battleships >= BattleshipInvasionLimit)
                {
                    _delayedResume = true;

                    Statistics.PanicAttemptsThisMission++;
                    Statistics.PanicAttemptsThisPocket++;
                    SetStartPanickingState();
                    Log.WriteLine("Start panicking, mission invaded by [" + battleships + "] BattleShips");
                    return true;
                }

                if (_delayedResume)
                {
                    _randomDelay = InvasionRandomDelay > 0 ? _random.Next(InvasionRandomDelay) : 0;
                    _randomDelay += InvasionMinimumDelay;
                    foreach (EntityCache enemy in ESCache.Instance.EntitiesNotSelf.Where(e => e.IsPlayer))
                        Log.WriteLine("Invaded by: PlayerName [" + enemy.Name + "] ShipTypeID [" + enemy.TypeId + "] TypeName [" + enemy.TypeName + "] Distance [" +
                                      (Math.Round(enemy.Distance, 0) / 1000) +
                                      "k] Velocity [" + Math.Round(enemy.Velocity, 0) + "]");
                    return true;
                }

                return false;
            }

            return false;
        }

        private static bool CheckPanicReasons_Wars()
        {
            if (Settings.Instance.WatchForActiveWars && ESCache.Instance.EveAccount.IsAtWar)
            {
                Log.WriteLine("IsAtWar [" + ESCache.Instance.EveAccount.IsAtWar + "] and WatchForActiveWars [" + Settings.Instance.WatchForActiveWars + "], Starting panic!");
                SetStartPanickingState();
                return true;
            }

            return false;
        }

        private static bool NormalPanicState()
        {
            if (DebugConfig.DebugPanic) Log.WriteLine("private static bool NormalPanicState()");

            if (ESCache.Instance.InStation)
                ChangePanicState(PanicState.Idle);

            if (ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition != null)
            {
                _lastNormalPositionInSpace = (Vec3)ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.PositionInSpace;
            }

            if (CheckPanicReasons_Wars()) return false;

            if (ESCache.Instance.InSpace)
            {
                if (CheckPanicReasons_Timers()) return false;

                if (CheckPanicReasons_OutOfAmmo()) return false;

                if (CheckPanicReasons_LogPriorityTargets()) return false;

                if (CheckPanicReasons_LowCapacitor()) return false;

                if (CheckPanicReasons_LowShields()) return false;

                if (CheckPanicReasons_LowArmor()) return false;

                BookmarkMyWreckAttempts = 1; // reset to 1 when we are known to not be in a pod anymore

                _delayedResume = false;
                if (CheckPanicReasons_InvasionLimits()) return false;
            }

            return true;
        }

        public static bool NPCStationIsWhereWeWantToGo()
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                        return false;

                    EntityCache npcstation = null;

                    if (ESCache.Instance.Stations != null && ESCache.Instance.Stations.Count > 0)
                        npcstation = ESCache.Instance.Stations.FirstOrDefault();

                    if (npcstation == null)
                    {
                        Log.WriteLine("NPCStationIsWhereWeWantToGo: No NPC Stations in local");
                        return false;
                    }

                    //
                    // Warp to NPC Station
                    //
                    if (ESCache.Instance.InSpace)
                    {
                        if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp) return true;

                        if (npcstation.Distance > (int)Distances.WarptoDistance)
                        {
                            NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "Panic", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
                            if ((Drones.DronePriorityEntities != null && Drones.DronePriorityEntities.Any(pt => pt.IsWarpScramblingMe)) || (Combat.Combat.PrimaryWeaponPriorityEntities != null && Combat.Combat.PrimaryWeaponPriorityEntities.Any(pt => pt.IsWarpScramblingMe)))
                            {
                                EntityCache warpScrambledBy = Drones.DronePriorityEntities.Find(pt => pt.IsWarpScramblingMe) ?? Combat.Combat.PrimaryWeaponPriorityEntities.Find(pt => pt.IsWarpScramblingMe);
                                if (warpScrambledBy != null && DateTime.UtcNow > _nextWarpScrambledWarning)
                                {
                                    _nextWarpScrambledWarning = DateTime.UtcNow.AddSeconds(20);
                                    Log.WriteLine("We are scrambled by: [" + warpScrambledBy.Name + "][" + Math.Round(warpScrambledBy.Distance, 0) + "][" + warpScrambledBy.Id + "]");
                                    NavigateOnGrid.LastWarpScrambled = DateTime.UtcNow;
                                }
                            }

                            if (!ESCache.Instance.ActiveShip.IsWarpScrambled) //this will effectively spam warpto as soon as you are free of warp disruption if you were warp disrupted in the past 10 seconds)
                            {
                                if (!npcstation.WarpTo())
                                    Log.WriteLine("Warpto [" + npcstation.Name + "][" + Math.Round(npcstation.Distance / 1000 / 149598000, 2) + " AU away] returned false.");
                            }
                            else
                            {
                                Log.WriteLine("IsWarpScrambled [" + ESCache.Instance.ActiveShip.IsWarpScrambled + "]");
                            }

                            return true;
                        }

                        if (npcstation.Distance <= (int)Distances.DockingRange)
                        {
                            if (npcstation.Dock())
                            {
                                Log.WriteLine("Docking with [" + npcstation.Name + "][" + Math.Round(npcstation.Distance / 1000 / 149598000, 2) + " AU away]");
                                return true;
                            }

                            return true;
                        }

                        if (DateTime.UtcNow > Time.Instance.NextTravelerAction)
                        {
                            if (ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != npcstation.Id || (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 50))
                            {
                                if (npcstation.Approach())
                                {
                                    Log.WriteLine("Approaching to [" + npcstation.Name + "] which is [" + Math.Round(npcstation.Distance / 1000, 0) + "k away]");
                                    return true;
                                }

                                return true;
                            }

                            Log.WriteLine("Already Approaching to: [" + npcstation.Name + "] which is [" + Math.Round(npcstation.Distance / 1000, 0) + "k away]");
                            return true;
                        }

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

        public static bool SafeSpotBookmarkIsWhereWeWantToGo(string SafeSpotPrefix)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                        return false;

                    if ((ESCache.Instance.ClosestCitadel != null && ESCache.Instance.ClosestCitadel.IsOnGridWithMe) || (ESCache.Instance.ClosestStation != null && ESCache.Instance.ClosestStation.IsOnGridWithMe))
                        return false;

                    List<DirectBookmark> myHomeBookmarks = new List<DirectBookmark>();
                    if (ESCache.Instance.CachedBookmarks.Count > 0 && ESCache.Instance.BookmarksThatContain(SafeSpotPrefix).Any(i => i.LocationId == ESCache.Instance.DirectEve.Session.LocationId))
                    {
                        myHomeBookmarks = ESCache.Instance.BookmarksByLabel(SafeSpotPrefix).OrderByDescending(i => i.LocationId == ESCache.Instance.DirectEve.Session.LocationId).ToList();

                        if (myHomeBookmarks != null && myHomeBookmarks.Count > 0)
                        {
                            if (!Traveler.TravelToBookmarkName(SafeSpotPrefix)) return false;
                            return true;
                        }

                        Log.WriteLine("HomeBookmarkIsWhereWeWantToGo: No SafeSpot bookmarks with [" + SafeSpotPrefix + "] found.");
                        return false;
                    }

                    Log.WriteLine("HomeBookmarkIsWhereWeWantToGo: No SafeSpot bookmarks with [" + SafeSpotPrefix + "] found!");
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

        public static bool HomeBookmarkIsWhereWeWantToGo(string HomeBookmarkName)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                        return false;

                    if ((ESCache.Instance.ClosestCitadel != null && ESCache.Instance.ClosestCitadel.IsOnGridWithMe) || (ESCache.Instance.ClosestStation != null && ESCache.Instance.ClosestStation.IsOnGridWithMe))
                        return false;

                    List<DirectBookmark> myHomeBookmarks = new List<DirectBookmark>();
                    if (ESCache.Instance.CachedBookmarks.Count > 0 && ESCache.Instance.BookmarksThatContain(HomeBookmarkName).Any(i => i.LocationId == ESCache.Instance.DirectEve.Session.LocationId))
                    {
                        myHomeBookmarks = ESCache.Instance.BookmarksByLabel(HomeBookmarkName).OrderByDescending(i => i.LocationId == ESCache.Instance.DirectEve.Session.LocationId).ToList();

                        if (myHomeBookmarks != null && myHomeBookmarks.Count > 0)
                        {
                            if (!Traveler.TravelToBookmarkName(HomeBookmarkName)) return false;
                            return true;
                        }

                        Log.WriteLine("HomeBookmarkIsWhereWeWantToGo: No HomeBookmarks found.");
                        return false;
                    }

                    Log.WriteLine("HomeBookmarkIsWhereWeWantToGo: No HomeBookmarks found!");
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

        public static bool FreeportCitadelIsWhereWeWantToGo()
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                        return false;

                    EntityCache freeportCitadel = null;

                    if (ESCache.Instance.FreeportCitadels != null && ESCache.Instance.FreeportCitadels.Count > 0)
                        freeportCitadel = ESCache.Instance.FreeportCitadels.FirstOrDefault();

                    if (freeportCitadel == null)
                    {
                        if (ESCache.Instance.ClosestStation.IsOnGridWithMe)
                            return false;

                        Log.WriteLine("FreeportCitadelIsWhereWeWantToGo: No Freeport Citadels Found");
                        return false;
                    }

                    //
                    // Warp to Citadel
                    //
                    if (ESCache.Instance.InSpace)
                    {
                        if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp) return true;

                        if (freeportCitadel.Distance > (int)Distances.WarptoDistance)
                        {
                            NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "Panic", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
                            if ((Drones.DronePriorityEntities != null && Drones.DronePriorityEntities.Any(pt => pt.IsWarpScramblingMe)) || (Combat.Combat.PrimaryWeaponPriorityEntities != null && Combat.Combat.PrimaryWeaponPriorityEntities.Any(pt => pt.IsWarpScramblingMe)))
                            {
                                EntityCache warpScrambledBy = Drones.DronePriorityEntities.Find(pt => pt.IsWarpScramblingMe) ?? Combat.Combat.PrimaryWeaponPriorityEntities.Find(pt => pt.IsWarpScramblingMe);
                                if (warpScrambledBy != null && DateTime.UtcNow > _nextWarpScrambledWarning)
                                {
                                    _nextWarpScrambledWarning = DateTime.UtcNow.AddSeconds(20);
                                    Log.WriteLine("We are scrambled by: [" + warpScrambledBy.Name + "][" + Math.Round(warpScrambledBy.Distance, 0) + "][" + warpScrambledBy.Id + "]");
                                    NavigateOnGrid.LastWarpScrambled = DateTime.UtcNow;
                                }
                            }

                            if (DateTime.UtcNow > Time.Instance.NextWarpAction || DateTime.UtcNow.Subtract(NavigateOnGrid.LastWarpScrambled).TotalSeconds < Time.Instance.WarpScrambledNoDelay_seconds) //this will effectively spam warpto as soon as you are free of warp disruption if you were warp disrupted in the past 10 seconds)
                            {
                                if (!freeportCitadel.WarpTo())
                                    Log.WriteLine("Warpto [" + freeportCitadel.Name + "][" + Math.Round(freeportCitadel.Distance / 1000 / 149598000, 2) + " AU away] returned false.");
                            }
                            else
                            {
                                Log.WriteLine("Warping will be attempted again after [" + Math.Round(Time.Instance.NextWarpAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                            }

                            return true;
                        }

                        if (freeportCitadel.Distance <= 0)
                        {
                            //
                            // Purposely do not dock, stay outside the citadel for reps...
                            //

                            return true;
                        }

                        if (DateTime.UtcNow > Time.Instance.NextTravelerAction)
                        {
                            if ((freeportCitadel.Distance > 0 && ESCache.Instance.FollowingEntity == null) || ESCache.Instance.FollowingEntity.Id != freeportCitadel.Id || (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 50))
                            {
                                if (freeportCitadel.Approach())
                                {
                                    Log.WriteLine("Approaching to [" + freeportCitadel.Name + "] which is [" + Math.Round(freeportCitadel.Distance / 1000, 0) + "k away]");
                                    return true;
                                }

                                return true;
                            }

                            Log.WriteLine("Already Approaching to: [" + freeportCitadel.Name + "] which is [" + Math.Round(freeportCitadel.Distance / 1000, 0) + "k away]");
                            return true;
                        }

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

        private static void PanicingPanicState(string HomeBookmarkName)
        {
            /**
            // Failsafe, in theory would/should never happen
            if (State.CurrentPanicState == PanicState.Panicking && Combat.Combat.TargetedBy.Any(t => t.IsWarpScramblingMe))
            {
                Log.WriteLine("Panic: Currently panicking: We have something scrambling us! Setting panic to Resume to we can kill it!");
                // Resume is the only state that will make Questor revert to combat mode
                State.CurrentPanicState = PanicState.Resume;
                return;
            }
            **/

            if (Safe())
            {
                Log.WriteLine("We are Safe, lower panic mode");
                ChangePanicState(PanicState.Panic);
                return;
            }

            // Once we have warped off 500km, assume we are "safer"
            if (State.CurrentPanicState == PanicState.StartPanicking &&
                ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.GetDistance(_lastNormalPositionInSpace) > (int)Distances.PanicDistanceToConsiderSafelyWarpedOff)
            {
                Log.WriteLine("We have warped off:  My ShipType: [" + ESCache.Instance.ActiveShip.TypeName + "] My ShipName [" +
                              ESCache.Instance.ActiveShip.GivenName + "]");
                ChangePanicState(PanicState.Panicking);
            }

            // We leave the panicking state once we actually start warping off

            if (Defense.CovertOps)
                if (SafeSpotIsWhereWeWantToGo()) return;

            if (HomeBookmarkIsWhereWeWantToGo(HomeBookmarkName)) return;
            if (FreeportCitadelIsWhereWeWantToGo()) return;
            if (NPCStationIsWhereWeWantToGo()) return;
            if (SafeSpotIsWhereWeWantToGo()) return;
            if (StarIsWhereWeWantToGo()) return;
        }

        private static bool PanicPanicState()
        {
            // Do not resume until you're no longer in a capsule
            if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                return false;

            if (ESCache.Instance.InStation)
            {
                if (ESCache.Instance.EveAccount.IsAtWar && Settings.Instance.WatchForActiveWars)
                {
                    Log.WriteLine("IsAtWar [" + ESCache.Instance.EveAccount.IsAtWar + "] and WatchForActiveWars [" + Settings.Instance.WatchForActiveWars + "]: Pausing");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.DeactivateScheduleAndCloseEveAfterNextDock = true;
                    return false;
                }

                if (!Cleanup.RepairItems()) return false; //attempt to use repair facilities if avail in station

                Log.WriteLine("We are in a station, resume mission");
                if (_delayedResume)
                    ChangePanicState(PanicState.DelayedResume);
                else
                    ChangePanicState(PanicState.Resume);
            }

            bool isSafe = ESCache.Instance.ActiveShip.CapacitorPercentage >= SafeCapacitorPct;
            isSafe &= ESCache.Instance.ActiveShip.ShieldPercentage >= SafeShieldPct;
            isSafe &= ESCache.Instance.ActiveShip.ArmorPercentage >= SafeArmorPct;
            if (isSafe)
            {
                if (ESCache.Instance.InSpace)
                    ESCache.Instance.NeedRepair = true;

                Log.WriteLine("We have recovered, resume mission");
                if (_delayedResume)
                    ChangePanicState(PanicState.DelayedResume);
                else
                    ChangePanicState(PanicState.Resume);

                if (Combat.Combat.OutOfAmmo && State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.GotoBase)
                {
                    Log.WriteLine("Panic: We are OutOfAmmo");
                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    {
                        CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                        return true;
                    }

                    if (ESCache.Instance.SelectedController.Contains("Abyssal"))
                    {
                        CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                        return true;
                    }

                    Log.WriteLine("Panic: We are OutOfAmmo and not using the CombatMissionBehavior, assuming we cant just return to base, pausing");
                    ControllerManager.Instance.SetPause(true);
                    return true;
                }
            }

            if (State.CurrentPanicState == PanicState.DelayedResume)
            {
                Log.WriteLine("Delaying resume for " + _randomDelay + " seconds");
                Drones.DronesShouldBePulled = false;
                _resumeTime = DateTime.UtcNow.AddSeconds(_randomDelay);
            }

            return true;
        }

        private static DateTime _nextLogCurrentShipHealth = DateTime.UtcNow;

        private static void ReportCurrentShipHealth()
        {
            if (!ESCache.Instance.InSpace) return;
            if (ESCache.Instance.InStation) return;
            if (ESCache.Instance.ActiveShip == null) return;

            if (DateTime.UtcNow > _nextLogCurrentShipHealth)
            {
                _nextLogCurrentShipHealth = DateTime.UtcNow.AddSeconds(20);
                Log.WriteLine("My Ship: Shield% [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "] Armor% [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "] Capacitor [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "] Panic Levels @ S[" + MinimumShieldPct + "] A[" + MinimumArmorPct + "] C[" + MinimumCapacitorPct + "] InMission [" + ESCache.Instance.InMission + "]");
            }
        }

        private static void RunEveryPulse()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return;

            ReportCurrentShipHealth();

            if (!EnablePanic)
                return;

            if (Combat.Combat.PotentialCombatTargets.Count > 0)
            {
                List<EntityCache> entitiesThatAreWarpScramblingMe = Combat.Combat.PotentialCombatTargets.Where(t => t.IsWarpScramblingMe).ToList();
                if (entitiesThatAreWarpScramblingMe.Count > 0)
                    if (DebugConfig.DebugPanic)
                        Log.WriteLine("We have been warp scrambled by [" + entitiesThatAreWarpScramblingMe.Count + "] Entities");

                List<EntityCache> entitiesThatCanWarpScramble = Combat.Combat.PotentialCombatTargets.Where(t => t.WarpScrambleChance > 0).ToList();
                if (entitiesThatCanWarpScramble.Count > 0)
                    if (DebugConfig.DebugPanic)
                        Log.WriteLine("We can be warp scrambled by [" + entitiesThatCanWarpScramble.Count + "] Entities");

                List<EntityCache> entitiesThatAreWebbingMe = Combat.Combat.PotentialCombatTargets.Where(t => t.IsWebbingMe).ToList();
                if (entitiesThatAreWebbingMe.Count > 0)
                    if (DebugConfig.DebugPanic)
                        Log.WriteLine("We have been webbed by [" + entitiesThatAreWebbingMe.Count + "] Entities");

                List<EntityCache> entitiesThatAreTargetPaintingMe = Combat.Combat.PotentialCombatTargets.Where(t => t.IsTargetPaintingMe).ToList();
                if (entitiesThatAreTargetPaintingMe.Count > 0)
                    if (DebugConfig.DebugPanic)
                        Log.WriteLine("We have been target painted by [" + entitiesThatAreTargetPaintingMe.Count + "] Entities");

                List<EntityCache> entitiesThatAreNeutralizingMe = Combat.Combat.PotentialCombatTargets.Where(t => t.IsNeutralizingMe).ToList();
                if (entitiesThatAreNeutralizingMe.Count > 0)
                    if (DebugConfig.DebugPanic)
                        Log.WriteLine("We have been neuted by [" + entitiesThatAreNeutralizingMe.Count + "] Entities");

                List<EntityCache> entitiesThatAreJammingMe = Combat.Combat.PotentialCombatTargets.Where(t => t.IsTryingToJamMe).ToList();
                if (entitiesThatAreJammingMe.Count > 0)
                    if (DebugConfig.DebugPanic)
                        Log.WriteLine("We have been ECMd by [" + entitiesThatAreJammingMe.Count + "] Entities");

                List<EntityCache> entitiesThatAreSensorDampeningMe = Combat.Combat.PotentialCombatTargets.Where(t => t.IsSensorDampeningMe).ToList();
                if (entitiesThatAreSensorDampeningMe.Count > 0)
                    if (DebugConfig.DebugPanic)
                        Log.WriteLine("We have been Sensor Damped by [" + entitiesThatAreSensorDampeningMe.Count + "] Entities");

                if (ESCache.Instance.Modules.Any(m => m.IsTurret) || Combat.Combat.AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList)
                {
                    //
                    // tracking disrupting targets
                    //
                    List<EntityCache> entitiesThatAreTrackingDisruptingMe = Combat.Combat.PotentialCombatTargets.Where(t => t.IsTrackingDisruptingMe).ToList();
                    if (entitiesThatAreTrackingDisruptingMe.Count > 0)
                        if (DebugConfig.DebugPanic)
                            Log.WriteLine("We have been Tracking Disrupted by [" + entitiesThatAreTrackingDisruptingMe.Count + "] Entities");
                }
            }
        }

        private static bool Safe()
        {
            try
            {
                if (ESCache.Instance.InStation)
                    return true;

                if (ESCache.Instance.Star != null && ESCache.Instance.Star.Distance < 50000)
                    return true;

                if (ESCache.Instance.FreeportCitadels.Count > 0)
                {
                    if (ESCache.Instance.FreeportCitadels.OrderBy(i => i.Distance).FirstOrDefault().IsOnGridWithMe)
                    {
                        if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.ShieldPercentage >= SafeShieldPct && ESCache.Instance.ActiveShip.ArmorPercentage >= SafeArmorPct)
                            return true;

                        return false;
                    }

                    return false;
                }

                if (ESCache.Instance.InSpace)
                {
                    if (ESCache.Instance.SafeSpotBookmarks.Any() &&
                        ESCache.Instance.SafeSpotBookmarks.Any(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId))
                    {
                        List<DirectBookmark> safeSpotBookmarksInLocal = new List<DirectBookmark>(ESCache.Instance.SafeSpotBookmarks
                            .Where(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.X != null && b.Y != null && b.Z != null));

                        if (safeSpotBookmarksInLocal.Any(bookmark => ESCache.Instance.DistanceFromMe((double)bookmark.X, (double)bookmark.Y, (double)bookmark.Z) < 500000))
                            return true;

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

        private static bool SafeSpotIsWhereWeWantToGo()
        {
            if (ESCache.Instance.InSpace)
            {
                if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp) return false;
                if (ESCache.Instance.SafeSpotBookmarks.Any() && ESCache.Instance.SafeSpotBookmarks.Any(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId))
                {
                    List<DirectBookmark> safeSpotBookmarksInLocal = new List<DirectBookmark>(ESCache.Instance.SafeSpotBookmarks
                        .Where(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.X != null && b.Y != null && b.Z != null)
                        .OrderBy(b => b.CreatedOn));

                    if (safeSpotBookmarksInLocal.Count > 0)
                    {
                        DirectBookmark offridSafeSpotBookmark = safeSpotBookmarksInLocal.OrderBy(i => ESCache.Instance.DistanceFromMe((double)i.X, (double)i.Y, (double)i.Z)).FirstOrDefault();
                        if (offridSafeSpotBookmark != null)
                        {
                            if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp) return false;

                            if (Combat.Combat.TargetedBy.Any(t => t.IsWarpScramblingMe))
                            {
                                Log.WriteLine("We are still warp scrambled!");
                                //This runs every 'tick' so we should see it every 1.5 seconds or so
                                NavigateOnGrid.LastWarpScrambled = DateTime.UtcNow;
                                return false;
                            }

                            if (DateTime.UtcNow > Time.Instance.NextWarpAction || DateTime.UtcNow.Subtract(NavigateOnGrid.LastWarpScrambled).TotalSeconds < 10)
                            //this will effectively spam warpto as soon as you are free of warp disruption if you were warp disrupted in the past 10 seconds
                            {
                                if (!ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                                {
                                    offridSafeSpotBookmark.WarpTo();
                                    return false;
                                }

                                if (ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                                {
                                    double distanceToBm = ESCache.Instance.DistanceFromMe((double)offridSafeSpotBookmark.X, (double)offridSafeSpotBookmark.Y, (double)offridSafeSpotBookmark.Z);
                                    Log.WriteLine("Warping to safespot bookmark [" + offridSafeSpotBookmark.Title + "][" + Math.Round(distanceToBm / 1000 / 149598000, 2) + " AU away]");
                                    return false;
                                }

                                return false;
                            }

                            Log.WriteLine("Warping has been delayed for [" + Math.Round(Time.Instance.NextWarpAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                            return false;
                        }

                        return true;
                    }

                    return true;
                }

                return true;
            }

            return true;
        }

        public static void SetStartPanickingState()
        {
            ChangePanicState(PanicState.StartPanicking);

            //DirectEvent
            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PANIC, "Panicking."));
        }

        public static bool StarIsWhereWeWantToGo()
        {
            if (ESCache.Instance.InSpace)
            {
                // What is this you say?  No star?
                if (ESCache.Instance.Star == null) return false;
                if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp) return false;

                if (ESCache.Instance.Star.Distance > (int)Distances.WeCanWarpToStarFromHere)
                {
                    NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "Panic", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());

                    if (Combat.Combat.TargetedBy.Any(t => t.IsWarpScramblingMe))
                    {
                        Log.WriteLine("We are still warp scrambled!");
                        //This runs every 'tick' so we should see it every 1.5 seconds or so
                        NavigateOnGrid.LastWarpScrambled = DateTime.UtcNow;
                        return false;
                    }

                    //this will effectively spam warpto as soon as you are free of warp disruption if you were warp disrupted in the past 10 seconds
                    if (DateTime.UtcNow > Time.Instance.NextWarpAction || DateTime.UtcNow.Subtract(NavigateOnGrid.LastWarpScrambled).TotalSeconds < 10)
                    {
                        if (ESCache.Instance.Star.WarpTo())
                            return false;

                        return false;
                    }

                    Log.WriteLine("Warping has been delayed for [" + Math.Round(Time.Instance.NextWarpAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                    return false;
                }

                Log.WriteLine("We are [" + Math.Round(ESCache.Instance.Star.Distance / (double)Distances.OneAu, 0) + "AU] from the local star");
                return true;
            }

            return true;
        }

        #endregion Methods
    }
}