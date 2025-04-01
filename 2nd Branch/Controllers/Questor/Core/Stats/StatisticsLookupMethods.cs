/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 08.05.2016
 * Time: 15:15
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EVESharpCore.Questor.Stats
{
    /// <summary>
    ///     Description of StatisticsMethods.
    /// </summary>
    public partial class Statistics
    {
        #region Methods

        public static bool LogWindowActionToWindowLog(string Windowname, string Description)
        {
            try
            {
                if (string.IsNullOrEmpty(WindowStatslogFile))
                    return true;

                string textToLogToFile;
                if (!File.Exists(WindowStatslogFile))
                {
                    textToLogToFile = "WindowName;Description;Time;Seconds Since LastInSpace;Seconds Since LastInStation;Seconds Since We Started;\r\n";
                    File.AppendAllText(WindowStatslogFile, textToLogToFile);
                }

                textToLogToFile = Windowname + ";" + Description + ";" + DateTime.UtcNow.ToShortTimeString() + ";" +
                                  Time.Instance.Started_DateTime.Subtract(DateTime.UtcNow).TotalSeconds + ";";
                textToLogToFile += "\r\n";

                File.AppendAllText(WindowStatslogFile, textToLogToFile);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception while logging to file [" + ex.Message + "]");
                return false;
            }
        }

        public static bool LootStatisticsCsv(IEnumerable<ItemCache> items, EntityCache containerEntity)
        {
            DateTimeForLogs = DateTime.UtcNow;
            string currentPocketName = Log.FilterPath("Random-Grid-" + DateTime.UtcNow.DayOfWeek + "-" + DateTime.UtcNow.Month + "-" + DateTime.UtcNow.Day + DateTime.UtcNow.Hour + "-" + DateTime.UtcNow.Minute);

            string LootStatisticsFile = Path.Combine(
                PocketObjectStatisticsPath,
                Log.FilterPath(ESCache.Instance.DirectEve.Me.Name) + " - " + currentPocketName + " - " +
                ActionControl.PocketNumber + " - LootStatistics.csv");

            if (ESCache.Instance.InAbyssalDeadspace)
                if (containerEntity != null)
                {
                    // Log all items found in the wreck
                    foreach (ItemCache item in items.OrderBy(i => i.TypeId))
                    {
                        File.AppendAllText(LootStatisticsFile, string.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTimeForLogs + ";"));
                        File.AppendAllText(LootStatisticsFile, containerEntity.Name + ";");
                        File.AppendAllText(LootStatisticsFile, "TypeID;" + item.TypeId.ToString(CultureInfo.InvariantCulture) + ";");
                        File.AppendAllText(LootStatisticsFile, "Name;" + item.Name + ";");
                        File.AppendAllText(LootStatisticsFile, "Quantity;" + item.Quantity.ToString(CultureInfo.InvariantCulture) + ";");
                        File.AppendAllText(LootStatisticsFile, "Value;" + item.Value + "\n");
                    }

                    File.AppendAllText(LootStatisticsFile, "------------------------------------- \n");
                    File.AppendAllText(LootStatisticsFile, ";;;;;;;;;;Value of wreck:" + ";" + items.Sum(i => i.Value) + "\n");
                }
            return true;
        }

        public static bool PocketObjectStatistics(List<EntityCache> things, bool force = false)
        {
            if (PocketObjectStatisticsLog || force)
            {
                string currentPocketName = Log.FilterPath("Random-Grid" + DateTime.UtcNow.DayOfWeek + "-" + DateTime.UtcNow.Month + "-" + DateTime.UtcNow.Day + DateTime.UtcNow.Hour + "-" + DateTime.UtcNow.Minute);
                try
                {
                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                        if (MissionSettings.MyMission != null && !string.IsNullOrEmpty(MissionSettings.MyMission.Name))
                            currentPocketName = Log.FilterPath(MissionSettings.MyMission.Name);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("PocketObjectStatistics: is cache.Instance.MissionName null?: exception was [" + ex.Message + "]");
                }

                PocketObjectStatisticsFile = Path.Combine(
                    PocketObjectStatisticsPath,
                    Log.FilterPath(ESCache.Instance.DirectEve.Me.Name) + " - " + currentPocketName + " - " +
                    ActionControl.PocketNumber + " - ObjectStatistics.csv");

                Log.WriteLine("Logging info on the [" + things.Count + "] objects in this pocket to [" + PocketObjectStatisticsFile + "]");

                if (File.Exists(PocketObjectStatisticsFile))
                    File.Delete(PocketObjectStatisticsFile);

                string objectline = "Name;Distance;TypeId;GroupId;CategoryId;IsNPC;IsPlayer;TargetValue;Velocity;ID;IsNPCDrone;IsNPCFrigate;IsNPCCruiser;IsNPCBattleCruiser;IsNPCBattleShip;IsNPCByGroupID;Shield;Armor;Hull;ShieldResistanceEM;ShieldResistanceExplosive;ShieldResistanceKinetic;ShieldResistanceThermal;ArmorResistanceEM;ArmorResistanceExplosive;ArmorResistanceKinetic;ArmorResistanceThermal;Health;EmEHP;ExpEHP;KineticEHP;ThermalEHP;NPCRawDps;EMRawMissileDPS;NpcExplosiveRawMissileDps;KineticRawMissileDPS;NpcThermalRawMissileDps;EMRwTurretDPS;ExplosiveRawTurretDPS;KineticRawTurretDPS;ThermalRawTurretDPS;EntityAttackRange;\r\n";
                File.AppendAllText(PocketObjectStatisticsFile, objectline);

                foreach (EntityCache thing in things.OrderBy(i => i.Distance))
                // can we somehow get the X,Y,Z coord? If we could we could use this info to build some kind of grid layout...,or at least know the distances between all the NPCs... thus be able to infer which NPCs were in which 'groups'
                {
                    objectline = thing.Name + ";";
                    objectline += Math.Round(thing.Distance / 1000, 0) + ";";
                    objectline += thing.TypeId + ";";
                    objectline += thing.GroupId + ";";
                    objectline += thing.CategoryId + ";";
                    objectline += thing.IsNpc + ";";
                    objectline += thing.IsPlayer + ";";
                    objectline += thing.TargetValue + ";";
                    objectline += Math.Round(thing.Velocity, 0) + ";";
                    objectline += thing.Id + ";";
                    objectline += thing.IsNPCDrone + ";";
                    objectline += thing.IsNPCFrigate + ";";
                    objectline += thing.IsNPCCruiser + ";";
                    objectline += thing.IsNPCBattlecruiser + ";";
                    objectline += thing.IsNPCBattleship + ";";
                    objectline += thing.IsNpcByGroupID + ";";
                    objectline += thing.ShieldMaxHitPoints + ";";
                    objectline += thing.ArmorMaxHitPoints + ";";
                    objectline += thing.StructureMaxHitPoints + ";";
                    objectline += thing.ShieldResistanceEm + ";";
                    objectline += thing.ShieldResistanceExplosive + ";";
                    objectline += thing.ShieldResistanceKinetic + ";";
                    objectline += thing.ShieldResistanceThermal + ";";
                    objectline += thing.ArmorResistanceEm + ";";
                    objectline += thing.ArmorResistanceExplosive + ";";
                    objectline += thing.ArmorResistanceKinetic + ";";
                    objectline += thing.ArmorResistanceThermal + ";";
                    objectline += thing.HealthPct + ";";
                    objectline += thing._directEntity.EmEHP + ";";
                    objectline += thing._directEntity.ExpEHP + ";";
                    objectline += thing._directEntity.KinEHP + ";";
                    objectline += thing._directEntity.TrmEHP + ";";
                    objectline += thing._directEntity.NpcEmRawMissileDps + ";";
                    objectline += thing._directEntity.NpcExplosiveRawMissileDps + ";";
                    objectline += thing._directEntity.NpcKineticRawMissileDps + ";";
                    objectline += thing._directEntity.NpcThermalRawMissileDps + ";";
                    objectline += thing._directEntity.NpcEmRawTurretDps + ";";
                    objectline += thing._directEntity.NpcExplosiveRawTurretDps + ";";
                    objectline += thing._directEntity.NpcKineticRawTurretDps + ";";
                    objectline += thing._directEntity.NpcThermalRawTurretDps + ";";
                    objectline += thing.BracketType + ";";
                    objectline += thing._directEntity.GetBracketName() + ";";
                    objectline += thing._directEntity.EntityAttackRange + ";\r\n";

                    File.AppendAllText(PocketObjectStatisticsFile, objectline);
                }
            }
            return true;
        }

        public static bool WreckStatistics(IEnumerable<ItemCache> items, EntityCache containerEntity)
        {
            DateTimeForLogs = DateTime.UtcNow;

            if (WreckLootStatistics)
                if (containerEntity != null)
                {
                    // Log all items found in the wreck
                    File.AppendAllText(WreckLootStatisticsFile, "TIME: " + string.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTimeForLogs) + "\n");
                    File.AppendAllText(WreckLootStatisticsFile, "NAME: " + containerEntity.Name + "\n");
                    File.AppendAllText(WreckLootStatisticsFile, "ITEMS:" + "\n");
                    foreach (ItemCache item in items.OrderBy(i => i.TypeId))
                    {
                        File.AppendAllText(WreckLootStatisticsFile, "TypeID: " + item.TypeId.ToString(CultureInfo.InvariantCulture) + "\n");
                        File.AppendAllText(WreckLootStatisticsFile, "Name: " + item.Name + "\n");
                        File.AppendAllText(WreckLootStatisticsFile, "Quantity: " + item.Quantity.ToString(CultureInfo.InvariantCulture) + "\n");
                        File.AppendAllText(WreckLootStatisticsFile, "=\n");
                    }
                    File.AppendAllText(WreckLootStatisticsFile, ";" + "\n");
                }
            return true;
        }

        #endregion Methods
    }
}