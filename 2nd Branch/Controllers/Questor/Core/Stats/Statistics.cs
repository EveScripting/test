extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace EVESharpCore.Questor.Stats
{
    public sealed partial class Statistics
    {
        #region Constructors

        private Statistics()
        {
            PanicAttemptsThisPocket = 0;
            LowestShieldPercentageThisPocket = 100;
            LowestArmorPercentageThisPocket = 100;
            LowestCapacitorPercentageThisPocket = 100;
            PanicAttemptsThisMission = 0;
            LowestShieldPercentageThisMission = 100;
            LowestArmorPercentageThisMission = 100;
            LowestCapacitorPercentageThisMission = 100;
        }

        #endregion Constructors

        #region Fields

        public static Dictionary<long, double> BountyValues = new Dictionary<long, double>();
        public static DateTime DateTimeForLogs;
        public static bool DroneLoggingCompleted;
        public static DateTime FinishedMission = DateTime.UtcNow;

        //public static DateTime LastMissionCompletionError;

        //false
        public static bool MissionAcceptDeclineLoggingCompleted;

        public static bool MissionLoggingCompleted;
        public static bool PocketStatsUseIndividualFilesPerPocket = true;
        public static DateTime StartedMission = DateTime.UtcNow;
        public static DateTime StartedPocket { get; set; } = DateTime.UtcNow;
        public static DateTime StartedSalvaging = DateTime.UtcNow;
        public static int TimeSpentInMission_seconds;

        public static int TimeSpentInMissionInRange;

        public static int TimeSpentInMissionOutOfRange;

        public static int TimeSpentReloading_seconds;

        public static int WrecksThisMission;

        public static int WrecksThisPocket;

        public bool MissionLoggingStarted = true;

        //singleton class
        private static readonly Statistics _instance = new Statistics();

        #endregion Fields

        #region Properties

        public static int AgentLPRetrievalAttempts { get; set; }
        public static int AmmoConsumption { get; set; }
        public static int AmmoValue { get; set; }
        public static int DroneRecalls { get; set; }
        public static bool DroneStatsLog { get; set; } = true;
        public static string DroneStatslogFile { get; set; }
        public static string DroneStatsLogPath { get; set; }
        public static int ISKMissionReward { get; set; }
        public static double IskPerLP { get; set; }
        public static int LostDrones { get; set; }
        public static double LowestArmorPercentageThisMission { get; set; }
        public static double LowestArmorPercentageThisPocket { get; set; }
        public static double LowestCapacitorPercentageThisMission { get; set; }
        public static double LowestCapacitorPercentageThisPocket { get; set; }
        public static double LowestShieldPercentageThisMission { get; set; }
        public static double LowestShieldPercentageThisPocket { get; set; }
        public static int LoyaltyPointsForCurrentMission { get; set; }
        public static int LoyaltyPointsTotal { get; set; }
        public static string MissionAcceptDeclineStatsLogFile { get; set; }
        public static string MissionAcceptDeclineStatsLogPath { get; set; }
        public static string WreckOutOfRangeLootSkipLogFile { get; set; }
        public static string WreckOutOfRangeLootSkipLogPath { get; set; }
        public static string MissionDetailsHtmlPath { get; set; }
        public static bool MissionDungeonIdLog { get; set; } = true;
        public static string MissionDungeonIdLogFile { get; set; }
        public static string MissionDungeonIdLogPath { get; set; }
        public static string MissionPocketObjectivesPath { get; set; }
        public static string MissionStats3LogFile { get; set; }
        public static string MissionStats3LogPath { get; set; }
        public static int MissionsThisSession { get; set; }
        public static int OutOfDronesCount { get; set; }
        public static int PanicAttemptsThisMission { get; set; }
        public static int PanicAttemptsThisPocket { get; set; }
        public static bool PocketObjectStatisticsBool { get; set; }
        public static string PocketObjectStatisticsFile { get; set; }
        public static bool PocketObjectStatisticsLog { get; set; } = true;
        public static string PocketObjectStatisticsPath { get; set; }
        public static string AbyssalSpawnStatisticsPath { get; set; }
        public static string AbyssalSpawnStatisticsFile { get; set; }

        public static bool PocketStatistics { get; set; } = true;
        public static string PocketStatisticsFile { get; set; }
        public static string PocketStatisticsPath { get; set; }
        public static int RepairCycleTimeThisMission { get; set; }
        public static int RepairCycleTimeThisPocket { get; set; }
        public static int SessionRunningTime { get; set; }
        public static bool WindowStatsLog { get; set; } = true;
        public static string WindowStatslogFile { get; set; }
        public static string WindowStatsLogPath { get; set; }
        public static bool WreckLootStatistics { get; set; } = true;
        public static string WreckLootStatisticsFile { get; set; }
        public static string WreckLootStatisticsPath { get; set; }
        public DateTime MissionLoggingStartedTimestamp { get; set; }
        public StatisticsState State { get; set; }

        #endregion Properties

        #region Methods

        private static IEnumerable<AmmoType> CorrectAmmo1
        {
            get
            {
                if (DirectUIModule.DefinedAmmoTypes.Count > 0)
                {
                    return DirectUIModule.DefinedAmmoTypes.Where(a => a.DamageType == MissionSettings.CurrentDamageType);
                }

                return new List<AmmoType>();
            }
        }

        private static IEnumerable<DirectItem> AmmoCargo
        {
            get
            {
                if (ESCache.Instance.CurrentShipsCargo != null)
                {
                    if (ESCache.Instance.CurrentShipsCargo.Items != null && ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
                    {
                        return ESCache.Instance.CurrentShipsCargo.Items.Where(i => CorrectAmmo1.Any(a => a.TypeId == i.TypeId));
                    }
                }

                return new List<DirectItem>();
            }
        }

        public static bool AmmoConsumptionStatistics()
        {
            try
            {
                if (DirectUIModule.DefinedAmmoTypes != null && DirectUIModule.DefinedAmmoTypes.Count > 0)
                {
                    foreach (DirectItem item in AmmoCargo)
                    {
                        AmmoType ammo1 = DirectUIModule.DefinedAmmoTypes.Find(a => a.TypeId == item.TypeId);
                        if (ammo1 != null) AmmoConsumption = ammo1.Quantity - item.Quantity;
                        continue;
                    }

                    Log.WriteLine("AmmoConsumption [" + AmmoConsumption + "] units");
                    return true;
                }

                AmmoConsumption = 0;
                return true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception: " + exception);
            }

            return true;
        }

        public static bool EntityStatistics(List<EntityCache> things)
        {
            string objectline =
                "Name;Distance;TypeId;GroupId;CategoryId;IsNPC;IsNPCByGroupID;IsPlayer;TargetValue;Velocity;HaveLootRights;IsContainer;IsWreck;IsWreckEmpty;ID;\r\n";
            Log.WriteLine(";EntityStatistics;" + objectline);

            if (things.Count == 0) //if their are no entries, return
            {
                Log.WriteLine("EntityStatistics: No entries to log");
                return true;
            }

            foreach (EntityCache thing in things.OrderBy(i => i.Distance))
            // can we somehow get the X,Y,Z coord? If we could we could use this info to build some kind of grid layout...,or at least know the distances between all the NPCs... thus be able to infer which NPCs were in which 'groups'
            {
                objectline = thing.Name + ";";
                objectline += Math.Round(thing.Distance / 1000, 0) + ";";
                objectline += thing.TypeId + ";";
                objectline += thing.GroupId + ";";
                objectline += thing.CategoryId + ";";
                objectline += thing.IsNpc + ";";
                objectline += thing.IsNpcByGroupID + ";";
                objectline += thing.IsPlayer + ";";
                objectline += thing.TargetValue + ";";
                objectline += Math.Round(thing.Velocity, 0) + ";";
                objectline += thing.HaveLootRights + ";";
                objectline += thing.IsContainer + ";";
                objectline += thing.IsWreck + ";";
                objectline += thing.IsWreckEmpty + ";";
                objectline += thing.Id + ";\r\n";

                Log.WriteLine(";EntityStatistics;" + objectline);
            }
            return true;
        }

        public static void ResetInStationSettingsWhenExitingStation()
        {
            Log.WriteLine("Exiting Station: MissionLoggingCompleted set to false");
            MissionLoggingCompleted = false;
        }

        public static void SaveMissionHtmlDetails(string MissionDetailsHtml, string missionName, string factionName)
        {
            DateTimeForLogs = DateTime.UtcNow;
            missionName = Log.FilterPath(missionName);
            factionName = Log.FilterPath(factionName);

            string missionDetailsHtmlFile = Path.Combine(MissionDetailsHtmlPath, missionName + "-" + factionName + "-" + "mission-description-html.txt");

            if (!Directory.Exists(MissionDetailsHtmlPath))
                Directory.CreateDirectory(MissionDetailsHtmlPath);

            if (!File.Exists(missionDetailsHtmlFile))
            {
                Log.WriteLine("Writing mission details HTML [ " + missionDetailsHtmlFile + " ]");
                File.AppendAllText(missionDetailsHtmlFile, MissionDetailsHtml);
            }
        }

        public static void SaveMissionPocketObjectives(string MissionPocketObjectives, string missionName, int pocketNumber)
        {
            DateTimeForLogs = DateTime.UtcNow;
            missionName = Log.FilterPath(missionName);

            string missionPocketObjectivesFile = Path.Combine(MissionDetailsHtmlPath,
                missionName + " - " + "MissionPocketObjectives-Pocket[" + pocketNumber + "].txt");

            if (!Directory.Exists(MissionDetailsHtmlPath))
                Directory.CreateDirectory(MissionDetailsHtmlPath);

            if (!File.Exists(missionPocketObjectivesFile))
            {
                Log.WriteLine("Writing mission details HTML [ " + missionPocketObjectivesFile + " ]");
                File.AppendAllText(missionPocketObjectivesFile, MissionPocketObjectives);
            }
        }

        public static bool WriteDroneStatsLog()
        {
            try
            {
                DateTimeForLogs = DateTime.UtcNow;

                if (DroneStatsLog && !DroneLoggingCompleted)
                    if (Drones.UseDrones &&
                        ESCache.Instance.ActiveShip.GroupId != (int)Group.Capsule &&
                        ESCache.Instance.ActiveShip.GroupId != (int)Group.Shuttle &&
                        ESCache.Instance.ActiveShip.GroupId != (int)Group.Frigate &&
                        ESCache.Instance.ActiveShip.GroupId != (int)Group.Destroyer &&
                        ESCache.Instance.ActiveShip.GroupId != (int)Group.Industrial &&
                        ESCache.Instance.ActiveShip.GroupId != (int)Group.TransportShip &&
                        ESCache.Instance.ActiveShip.GroupId != (int)Group.Freighter)
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
                    else
                    {
                        Log.WriteLine("We do not use drones in this type of ship, skipping drone stats");
                        DroneLoggingCompleted = true;
                    }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public static bool WriteMissionAcceptDeclineStatsLog(bool Declined, bool Accepted, double Personal, double Corp, double Faction, DateTime LastDeclineTimeBeforeThisMission, string ReasonDeclined = "accepted")
        {
            DateTimeForLogs = DateTime.UtcNow;

            if (!File.Exists(MissionAcceptDeclineStatsLogFile))
                File.AppendAllText(MissionAcceptDeclineStatsLogFile, "Date;Time;Mission;Declined;Accepted;Personal;Corp;Faction;LastDeclineTimeBeforeThisMission\r\n");

            string acceptDeclineLogline = DateTime.UtcNow.ToShortDateString() + ";";
            acceptDeclineLogline = DateTime.UtcNow.ToShortTimeString() + ";";
            acceptDeclineLogline += MissionSettings.MissionNameforLogging + ";";
            acceptDeclineLogline += Declined + ";";
            acceptDeclineLogline += Accepted + ";";
            acceptDeclineLogline += Personal + ";";
            acceptDeclineLogline += Corp + ";";
            acceptDeclineLogline += Faction + ";";
            acceptDeclineLogline += LastDeclineTimeBeforeThisMission + ";";
            acceptDeclineLogline += ReasonDeclined + ";";
            acceptDeclineLogline += MissionSettings.MyMission.Type + ";";
            acceptDeclineLogline += "\r\n";
            File.AppendAllText(MissionAcceptDeclineStatsLogFile, acceptDeclineLogline);
            MissionAcceptDeclineLoggingCompleted = true;
            return true;
        }

        public static void LogMyComputersHealth()
        {
            try
            {
                return;
                /**
                DateTime LogMyComputersHealthStarted = DateTime.UtcNow;
                Process p = System.Diagnostics.Process.GetCurrentProcess();
                PerformanceCounter ramCounter = new PerformanceCounter("Processor", "Working Set",p.ProcessName);
                PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", p.ProcessName);
                double ram = ramCounter.NextValue();
                double cpu = cpuCounter.NextValue();

                while (cpu == 0 || cpu > 80 || DateTime.UtcNow > LogMyComputersHealthStarted.AddMilliseconds(750))
                {
                    System.Threading.Thread.Sleep(100);
                    ram = ramCounter.NextValue();
                    cpu = cpuCounter.NextValue();
                }

                Log.WriteLine("RAM: " + (ram / 1024 / 1024) + " MB; CPU: " + (cpu) + " %");
                **/
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void WriteWreckOutOfRangeLootSkipLog()
        {
            try
            {
                DateTimeForLogs = DateTime.UtcNow;

                if (!File.Exists(MissionAcceptDeclineStatsLogFile))
                    File.AppendAllText(MissionAcceptDeclineStatsLogFile, "Date;Time;Mission;Declined;Accepted;Personal;Corp;Faction;LastDeclineTimeBeforeThisMission\r\n");

                string wreckOutOfRangeLootSkipLogline = DateTime.UtcNow.ToShortDateString() + ";";
                wreckOutOfRangeLootSkipLogline = "Skipped Looting due to time constraints" + ";";
                wreckOutOfRangeLootSkipLogline += "\r\n";
                File.AppendAllText(WreckOutOfRangeLootSkipLogFile, wreckOutOfRangeLootSkipLogline);
                MissionAcceptDeclineLoggingCompleted = true;
                return;
            }
            catch (Exception)
            {
                //ignore this exception
            }

            return;
        }

        public static bool WriteMissionStatistics()
        {
            try
            {
                DateTimeForLogs = DateTime.UtcNow;

                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("We have started questor in space, assume we do not need to write any statistics at the moment.");
                    MissionLoggingCompleted = true; //if the mission was completed more than 10 min ago assume the logging has been done already.
                    return true;
                }

                //if (MissionSettings.AgentToPullNextRegularMissionFrom.Level == 1)
                //{
                //    Log.WriteLine("We dont need/want to track statistics for level 1 missions");
                //    MissionLoggingCompleted = true;
                //    return true;
                //}

                //if (AgentLPRetrievalAttempts > 5)
                //{
                //  Log.WriteLine("WriteMissionStatistics: We do not have loyalty points with the current agent yet, still -1, attempt # [" +
                //                  AgentLPRetrievalAttempts +
                //                  "] giving up");
                //    AgentLPRetrievalAttempts = 0;
                //    MissionLoggingCompleted = true; //if it is not true - this means we should not be trying to log mission stats atm
                //    return true;
                //}
                //
                // Seeing as we completed a mission, we will have loyalty points for this agent
                //if (MissionSettings.Agent.LoyaltyPoints == -1)
                //{
                //    AgentLPRetrievalAttempts++;
                //    Log.WriteLine("WriteMissionStatistics: We do not have loyalty points with the current agent yet, still -1, attempt # [" +
                //                  AgentLPRetrievalAttempts +
                //                  "] retrying...");
                //    return false;
                //}

                AgentLPRetrievalAttempts = 0;

                int isk = Convert.ToInt32(BountyValues.Sum(x => x.Value));
                long lootValCurrentShipInv = 0;
                long lootValItemHangar = 0;

                try
                {
                    lootValCurrentShipInv = UnloadLoot.CurrentLootValueInCurrentShipInventory();
                }
                catch (Exception)
                {
                    //ignore this exception
                }

                try
                {
                    lootValItemHangar = UnloadLoot.CurrentLootValueInItemHangar();
                    if (lootValItemHangar > 0)
                    {
                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.ItemHangarValue), lootValItemHangar);
                    }
                }
                catch (Exception)
                {
                    //ignore this exception
                }

                MissionsThisSession++;

                if (!MissionSettings.CourierMission(null) && ESCache.Instance.MissionSolarSystem != null && ESCache.Instance.MissionSolarSystem.Name != null)
                {
                    Log.WriteLine("Printing All Statistics Related Variables to the console log:");
                    Log.WriteLine("Mission Name: [" + MissionSettings.MissionNameforLogging + "]");
                    //Log.WriteLine("Faction: [" + MissionSettings.RegularMission.FactionName + "]");
                    if (ESCache.Instance.MissionSolarSystem != null) Log.WriteLine("System: [" + ESCache.Instance.MissionSolarSystem.Name + "]");
                    Log.WriteLine("Total Missions completed this session: [" + MissionsThisSession + "]");
                    Log.WriteLine("StartedMission: [ " + StartedMission + "]");
                    Log.WriteLine("FinishedMission: [ " + FinishedMission + "]");
                    Log.WriteLine("StartedSalvaging: [ " + StartedSalvaging + "]");
                    /// Log.WriteLine("FinishedSalvaging: [ " + FinishedSalvaging + "]");
                    Log.WriteLine("Wealth before mission: [ " + ESCache.Instance.Wealth ?? 0 + "]");
                    Log.WriteLine("Wealth after mission: [ " + ESCache.Instance.MyWalletBalance ?? 0 + "]");
                    Log.WriteLine("Value of Loot from the mission: [" + lootValCurrentShipInv + "]");
                    Log.WriteLine("Total LP after mission:  [" + MissionSettings.AgentToPullNextRegularMissionFrom.LoyaltyPoints + "]");
                    Log.WriteLine("Total LP before mission: [" + LoyaltyPointsTotal + "]");
                    Log.WriteLine("LP from this mission: [" + LoyaltyPointsForCurrentMission + "]");
                    Log.WriteLine("ISKBounty from this mission: [" + isk + "]");
                    Log.WriteLine("ISKMissionreward from this mission: [" + ISKMissionReward + "]");
                    Log.WriteLine("Lootvalue Itemhangar: [" + lootValItemHangar + "]");
                    Log.WriteLine("LostDrones: [" + LostDrones + "]");
                    Log.WriteLine("DroneRecalls: [" + DroneRecalls + "]");
                    Log.WriteLine("AmmoConsumption: [" + AmmoConsumption + "]");
                    Log.WriteLine("AmmoValue: [" + AmmoConsumption + "]");
                    Log.WriteLine("Panic Attempts: [" + PanicAttemptsThisMission + "]");
                    Log.WriteLine("Lowest Shield %: [" + Math.Round(LowestShieldPercentageThisMission, 0) + "]");
                    Log.WriteLine("Lowest Armor %: [" + Math.Round(LowestArmorPercentageThisMission, 0) + "]");
                    Log.WriteLine("Lowest Capacitor %: [" + Math.Round(LowestCapacitorPercentageThisMission, 0) + "]");
                    Log.WriteLine("Repair Cycle Time: [" + RepairCycleTimeThisMission + "]");
                    Log.WriteLine("MissionXMLIsAvailable: [" + MissionSettings.MissionXMLIsAvailable + "]");
                    Log.WriteLine("the stats below may not yet be correct and need some TLC");
                    int weaponNumber = 0;
                    if (ESCache.Instance.Weapons.Count > 0)
                    {
                    	foreach (ModuleCache weapon in ESCache.Instance.Weapons)
                    	{
                        	weaponNumber++;
                        	if (Time.Instance.ReloadTimePerModule != null && Time.Instance.ReloadTimePerModule.ContainsKey(weapon.ItemId))
                            	Log.WriteLine("Time Spent Reloading: [" + weaponNumber + "][" + Time.Instance.ReloadTimePerModule[weapon.ItemId] + "]");
                    	}
                    }

                    Log.WriteLine("Time Spent IN Mission: [" + TimeSpentInMission_seconds + "sec]");
                    Log.WriteLine("Time Spent In Range: [" + TimeSpentInMissionInRange + "]");
                    Log.WriteLine("Time Spent Out of Range: [" + TimeSpentInMissionOutOfRange + "]");

                    if (!Directory.Exists(MissionStats3LogPath))
                        Directory.CreateDirectory(MissionStats3LogPath);

                    if (!File.Exists(MissionStats3LogFile))
                        File.AppendAllText(MissionStats3LogFile,
                            "Date;Mission;Time;Isk;ISKReward;Loot;LP;DroneRecalls;LostDrones;AmmoConsumption;AmmoValue;Panics;LowestShield;LowestArmor;LowestCapacator;RepairCycles;AfterMissionSalvagingTime;TotalMissionTime;MissionXMLAvailable;Faction;SolarSystem;OutOfDronesCount;ISKWallet;ISKLootHangarItems;TotalLP;CorpStanding;FactionStanding;IskPerLP\r\n");

                    string line3 = DateTimeForLogs + ";"; // Date
                    line3 += MissionSettings.MissionNameforLogging + ";"; // RegularMission
                    line3 += (int)FinishedMission.Subtract(StartedMission).TotalMinutes + ";"; // TimeMission
                    line3 += isk + ";"; // Isk
                    line3 += ISKMissionReward + ";"; // ISKMissionReward
                    line3 += lootValCurrentShipInv + ";"; // Loot
                    line3 += LoyaltyPointsForCurrentMission + ";"; // LP
                    line3 += DroneRecalls + ";"; // Lost Drones
                    line3 += LostDrones + ";"; // Lost Drones
                    line3 += AmmoConsumption + ";"; // DefinedAmmoTypes Consumption
                    line3 += AmmoValue + ";"; // DefinedAmmoTypes Value
                    line3 += PanicAttemptsThisMission + ";"; // Panics
                    line3 += (int)LowestShieldPercentageThisMission + ";"; // Lowest Shield %
                    line3 += (int)LowestArmorPercentageThisMission + ";"; // Lowest Armor %
                    line3 += (int)LowestCapacitorPercentageThisMission + ";"; // Lowest Capacitor %
                    line3 += RepairCycleTimeThisMission + ";"; // repair Cycle Time
                    /// line3 += (int)FinishedSalvaging.Subtract(StartedSalvaging).TotalMinutes + ";"; // After Mission Salvaging Time
                    /// line3 += (int)FinishedSalvaging.Subtract(StartedSalvaging).TotalMinutes + (int)FinishedMission.Subtract(StartedMission).TotalMinutes + ";";
                    // Total Time, Mission + After Mission Salvaging (if any)
                    line3 += MissionSettings.MissionXMLIsAvailable.ToString(CultureInfo.InvariantCulture) + ";";
                    line3 += "" + ";"; // FactionName that the mission is against
                    if (ESCache.Instance.MissionSolarSystem != null)
                        line3 += ESCache.Instance.MissionSolarSystem.Name + ";"; // SolarSystem the mission was located in
                    else
                        line3 += "MissionSolarSystem.Name was null" + ";"; // SolarSystem the mission was located in
                    line3 += ESCache.Instance.EveAccount.IskPerLp + ";"; // ISKPerLP
                    line3 += OutOfDronesCount + ";"; // OutOfDronesCount - number of times we totally ran out of drones and had to go re-arm
                    line3 += ESCache.Instance.MyWalletBalance + ";"; // Current wallet balance
                    line3 += lootValItemHangar + ";"; // loot value in itemhangar
                    line3 += LoyaltyPointsTotal + ";"; // total LP
                    //line3 += string.Format("{0:0.00}", AgentInteraction.AgentCorpEffectiveStandingtoMe) + ";"; // AgentCorpEffectiveStandingtoMe
                    //line3 += string.Format("{0:0.00}", AgentInteraction.AgentFactionEffectiveStandingtoMe); // AgentFactionEffectiveStandingtoMe
                    line3 += "\r\n";
                    // The mission is finished
                    Log.WriteLine("writing mission log3 to  [ " + MissionStats3LogFile + " ]");
                    File.AppendAllText(MissionStats3LogFile, line3);
                }

                MissionLoggingCompleted = true;
                LoyaltyPointsTotal = MissionSettings.AgentToPullNextRegularMissionFrom.LoyaltyPoints ?? 0;
                StartedMission = DateTime.UtcNow;
                FinishedMission = DateTime.UtcNow; //this may need to be reset to DateTime.MinValue, but that was causing other issues...
                MissionSettings.MissionNameforLogging = string.Empty;
                DroneRecalls = 0;
                LostDrones = 0;
                AmmoConsumption = 0;
                AmmoValue = 0;
                DroneLoggingCompleted = false;
                OutOfDronesCount = 0;
                if (ESCache.Instance.Weapons.Count > 0)
                {
                	foreach (ModuleCache weapon in ESCache.Instance.Weapons)
                    	if (Time.Instance.ReloadTimePerModule != null && Time.Instance.ReloadTimePerModule.ContainsKey(weapon.ItemId))
                    	    Time.Instance.ReloadTimePerModule[weapon.ItemId] = 0;
                }
                BountyValues = new Dictionary<long, double>();
                PanicAttemptsThisMission = 0;
                LowestShieldPercentageThisMission = 101;
                LowestArmorPercentageThisMission = 101;
                LowestCapacitorPercentageThisMission = 101;
                RepairCycleTimeThisMission = 0;
                TimeSpentReloading_seconds = 0; // this will need to be added to whenever we reload or switch ammo
                TimeSpentInMission_seconds = 0; // from landing on grid (loading mission actions) to going to base (changing to gotobase state)
                TimeSpentInMissionInRange = 0; // time spent totally out of range, no targets
                TimeSpentInMissionOutOfRange = 0; // time spent in range - with targets to kill (or no targets?!)
                ESCache.Instance.MissionSolarSystem = null;
                ESCache.Instance.OrbitEntityNamed = null;
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void WritePocketStatistics()
        {
            DateTimeForLogs = DateTime.UtcNow;

            string currentPocketName = string.Empty;
            if (ESCache.Instance.InAnomaly)
                currentPocketName = "Anomaly-" + DateTime.UtcNow.Hour + "-" + DateTime.UtcNow.Minute;
            else if (ESCache.Instance.InAbyssalDeadspace)
                currentPocketName = "AbyssalDeadspace-" + DateTime.UtcNow.Hour + "-" + DateTime.UtcNow.Minute;
            else if (MissionSettings.MyMission != null) currentPocketName = Log.FilterPath(MissionSettings.MyMission.Name);

            // //agentID needs to change if its a storyline mission - so its assigned in storyline.cs to the various modules directly.
            if (PocketStatistics)
            {
                if (PocketStatsUseIndividualFilesPerPocket)
                    PocketStatisticsFile = Path.Combine(PocketStatisticsPath,
                        Log.FilterPath(ESCache.Instance.DirectEve.Me.Name) + " - " + currentPocketName + " - " + ActionControl.PocketNumber +
                        " - PocketStatistics.csv");
                if (!Directory.Exists(PocketStatisticsPath))
                    Directory.CreateDirectory(PocketStatisticsPath);

                if (!File.Exists(PocketStatisticsFile))
                    File.AppendAllText(PocketStatisticsFile,
                        "Date and Time;Mission Name ;Pocket;Time to complete;Isk;panics;LowestShields;LowestArmor;LowestCapacitor;RepairCycles;Wrecks\r\n");

                string pocketstatsLine = DateTimeForLogs + ";"; //Date
                pocketstatsLine += currentPocketName + ";"; //Mission Name
                pocketstatsLine += "pocket" + ActionControl.PocketNumber + ";"; //Pocket number
                pocketstatsLine += (int)DateTime.UtcNow.Subtract(StartedMission).TotalMinutes + ";"; //Time to Complete
                pocketstatsLine += ESCache.Instance.MyWalletBalance - ESCache.Instance.WealthAtStartOfPocket + ";"; //Isk
                pocketstatsLine += PanicAttemptsThisPocket + ";"; //Panics
                pocketstatsLine += (int)LowestShieldPercentageThisPocket + ";"; //LowestShields
                pocketstatsLine += (int)LowestArmorPercentageThisPocket + ";"; //LowestArmor
                pocketstatsLine += (int)LowestCapacitorPercentageThisPocket + ";"; //LowestCapacitor
                pocketstatsLine += RepairCycleTimeThisPocket + ";"; //repairCycles
                pocketstatsLine += WrecksThisPocket + ";"; //wrecksThisPocket
                pocketstatsLine += "\r\n";

                Log.WriteLine("Writing pocket statistics to [ " + PocketStatisticsFile + " ] and clearing stats for next pocket");
                File.AppendAllText(PocketStatisticsFile, pocketstatsLine);
                Log.WriteLine(pocketstatsLine);
            }

            // Update statistic values for next pocket stats
            ESCache.Instance.WealthAtStartOfPocket = ESCache.Instance.MyWalletBalance ?? 0;
            StartedPocket = DateTime.UtcNow;
            PanicAttemptsThisPocket = 0;
            LowestShieldPercentageThisPocket = 101;
            LowestArmorPercentageThisPocket = 101;
            LowestCapacitorPercentageThisPocket = 101;
            RepairCycleTimeThisPocket = 0;
            WrecksThisMission += WrecksThisPocket;
            WrecksThisPocket = 0;
            ESCache.Instance.OrbitEntityNamed = null;
        }

        public double TimeInCurrentMission()
        {
            double missiontimeMinutes = Math.Round(DateTime.UtcNow.Subtract(StartedMission).TotalMinutes, 0);
            return missiontimeMinutes;
        }

        #endregion Methods
    }
}

namespace CpuUsageCs
{
    internal class CpuUsage
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out ComTypes.FILETIME lpIdleTime, out ComTypes.FILETIME lpKernelTime, out ComTypes.FILETIME lpUserTime);

        private ComTypes.FILETIME _prevSysKernel;
        private ComTypes.FILETIME _prevSysUser;
        private TimeSpan _prevProcTotal;
        private Int16 _cpuUsage;
        private DateTime _lastRun;
        private long _runCount;

        public CpuUsage()
        {
            _cpuUsage = -1;
            _lastRun = DateTime.MinValue;
            _prevSysUser.dwHighDateTime = _prevSysUser.dwLowDateTime = 0;
            _prevSysKernel.dwHighDateTime = _prevSysKernel.dwLowDateTime = 0;
            _prevProcTotal = TimeSpan.MinValue;
            _runCount = 0;
        }

        public short GetUsage()
        {
            short cpuCopy = _cpuUsage;

            if (Interlocked.Increment(ref _runCount) == 1)
            {
                if (!EnoughTimePassed)
                {
                    Interlocked.Decrement(ref _runCount);
                    return cpuCopy;
                }

                ComTypes.FILETIME sysIdle, sysKernel, sysUser;
                TimeSpan procTime;
                Process process = Process.GetCurrentProcess();
                procTime = process.TotalProcessorTime;

                if (!GetSystemTimes(out sysIdle, out sysKernel, out sysUser))
                {
                    Interlocked.Decrement(ref _runCount);
                    return cpuCopy;
                }

                if (!IsFirstRun)
                {
                    UInt64 sysKernelDiff = SubtractTimes(sysKernel, _prevSysKernel);
                    UInt64 sysUserDiff = SubtractTimes(sysUser, _prevSysUser);
                    UInt64 sysTotal = sysKernelDiff + sysUserDiff;
                    Int64 procTotal = procTime.Ticks - _prevProcTotal.Ticks;

                    if (sysTotal > 0)
                    {
                        _cpuUsage = (short)((100.0 * procTotal) / sysTotal);
                    }
                }
                _prevProcTotal = procTime;
                _prevSysKernel = sysKernel;
                _prevSysUser = sysUser;
                _lastRun = DateTime.Now;
                cpuCopy = _cpuUsage;
            }
            Interlocked.Decrement(ref _runCount);
            return cpuCopy;
        }

        private UInt64 SubtractTimes(ComTypes.FILETIME a, ComTypes.FILETIME b)
        {
            UInt64 aInt = ((UInt64)(a.dwHighDateTime << 32)) | (UInt64)a.dwLowDateTime;
            UInt64 bInt = ((UInt64)(b.dwHighDateTime << 32)) | (UInt64)b.dwLowDateTime;
            return aInt - bInt;
        }

        private bool EnoughTimePassed
        {
            get
            {
                const int minimumElapsedMS = 250;
                TimeSpan sinceLast = DateTime.Now - _lastRun;
                return sinceLast.TotalMilliseconds > minimumElapsedMS;
            }
        }

        private bool IsFirstRun
        {
            get
            {
                return _lastRun == DateTime.MinValue;
            }
        }
    }
}

