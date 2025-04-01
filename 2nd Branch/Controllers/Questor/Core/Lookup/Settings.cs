extern alias SC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using EVESharpCore.Framework;
using System.Threading.Tasks;

namespace EVESharpCore.Lookup
{
    public class Settings
    {
        #region Constructors

        private Settings()
        {
            try
            {
                Interlocked.Increment(ref SettingsInstances);
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception: [" + exception + "]");
            }
        }

        #endregion Constructors

        #region Destructors

        ~Settings()
        {
            Interlocked.Decrement(ref SettingsInstances);
        }

        #endregion Destructors

        #region Fields

        //public List<string> CharacterNamesForMasterToInviteToFleet = new List<string>();
        public static bool CharacterXmlExists = true;

        private static bool _commonXmlExists;

        private static int SettingsInstances;

        public bool DefaultSettingsLoaded = false;

        public List<InventoryItem> ListOfItemsToBuyFromLpStore = new List<InventoryItem>();

        public List<InventoryItem> ListOfItemsToKeepInStock = new List<InventoryItem>();

        private List<InventoryItem> ListOfItemsToTakeToMarket = new List<InventoryItem>();

        public int NumberOfModulesToActivateInCycle = 4;

        //
        // path information - used to load the XML and used in other modules
        //
        public readonly string Path = Util.AssemblyPath;

        /// <summary>
        ///     Singleton implementation
        /// </summary>
        private static readonly Settings _instance = new Settings();

        private static string _characterSettingsPath;
        private static string _commonSettingsPath;
        private DateTime _lastModifiedDateOfMyCommonSettingsFile;
        private DateTime _lastModifiedDateOfMySettingsFile;
        private int _settingsLoadedICount;

        #endregion Fields

        #region Properties

        public static string CharacterSettingsPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_characterSettingsPath)) return _characterSettingsPath;

                string testCharacterSettingsPath = string.Empty;
                if (ESCache.Instance.EveAccount.ConnectToTestServer)
                {
                    //CharacterName-DayOfWeek.xml: ex: JohnDoe-Sisi.xml
                    testCharacterSettingsPath = System.IO.Path.Combine(Instance.Path, "QuestorSettings", Log.FilterPath(ESCache.Instance.EveAccount.CharacterName) + "-Sisi.xml");

                    if (string.IsNullOrEmpty(testCharacterSettingsPath) || !File.Exists(testCharacterSettingsPath))
                    {
                        //CharacterName-DayOfMonth.xml: ex: JohnDoe-14.xml
                        testCharacterSettingsPath = System.IO.Path.Combine(Instance.Path, "QuestorSettings", Log.FilterPath(ESCache.Instance.EveAccount.CharacterName) + "-" + DateTime.Now.Day + "-Sisi.xml");
                    }

                    if (!File.Exists(testCharacterSettingsPath))
                    {
                        //CharacterName-DayOfWeek.xml: ex: JohnDoe-Wednesday.xml
                        if (DebugConfig.DebugLoadSettings) Log.WriteLine("DateOfMonth [" + DateTime.Now.Day + "] - [" + testCharacterSettingsPath + "] not found");
                        testCharacterSettingsPath = System.IO.Path.Combine(Instance.Path, "QuestorSettings", Log.FilterPath(ESCache.Instance.EveAccount.CharacterName) + "-" + CurrentDayOfTheWeek() + "-Sisi.xml");
                    }
                }

                if (string.IsNullOrEmpty(testCharacterSettingsPath) || !File.Exists(testCharacterSettingsPath))
                {
                    //CharacterName-DayOfMonth.xml: ex: JohnDoe-14.xml
                    testCharacterSettingsPath = System.IO.Path.Combine(Instance.Path, "QuestorSettings", Log.FilterPath(ESCache.Instance.EveAccount.CharacterName) + "-" + DateTime.Now.Day + ".xml");
                }

                if (!File.Exists(testCharacterSettingsPath))
                {
                    //CharacterName-DayOfWeek.xml: ex: JohnDoe-Wednesday.xml
                    if (DebugConfig.DebugLoadSettings) Log.WriteLine("DateOfMonth [" + DateTime.Now.Day + "] - [" + testCharacterSettingsPath + "] not found");
                    testCharacterSettingsPath = System.IO.Path.Combine(Instance.Path, "QuestorSettings", Log.FilterPath(ESCache.Instance.EveAccount.CharacterName) + "-" + CurrentDayOfTheWeek() + ".xml");
                }

                //CharacterName.xml ex: JohnDoe.xml
                if (!File.Exists(testCharacterSettingsPath))
                {
                    if (DebugConfig.DebugLoadSettings) Log.WriteLine("DayOfWeek [" + CurrentDayOfTheWeek() + "] - [" + testCharacterSettingsPath + "] not found");
                    testCharacterSettingsPath = System.IO.Path.Combine(Instance.Path, "QuestorSettings", Log.FilterPath(ESCache.Instance.EveAccount.CharacterName) + ".xml");
                }

                _characterSettingsPath = testCharacterSettingsPath;
                return _characterSettingsPath;
            }
        }

        public static string templateSettingsPath
        {
            get
            {
                string tempTemplateSettingsPath = System.IO.Path.Combine(Instance.Path, "QuestorSettings", "myCustomTemplate.xml");

                if (!File.Exists(tempTemplateSettingsPath))
                {
                    Log.WriteLine(tempTemplateSettingsPath + " not found: using RenameThisToToonName.xml");
                    return System.IO.Path.Combine(Instance.Path, "QuestorSettings", "RenameThisToToonName.xml");
                }

                return tempTemplateSettingsPath;
            }
        }


        public static XElement CharacterSettingsXml { get; set; }

        public static string CommonSettingsFileName //just the filename, no path, with file extension
        {
            get
            {
                try
                {
                    return (string) CharacterSettingsXml.Element("commonSettingsFileName") ?? "common.xml";
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return "common.xml";
                }
            }
        }

        public static string CommonSettingsPath
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(_commonSettingsPath)) return _commonSettingsPath;

                    // System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.Combine(Instance.Path, "QuestorSettings", Instance.CommonSettingsFileName));
                    string tempCommonSettingsFileName = System.IO.Path.Combine(Instance.Path, "QuestorSettings", NameOfCommonXml + "-" + DateTime.Now.Day + ".xml");

                    //ex: Does common-1.xml exist? if not try to find common-monday.xml
                    if (!File.Exists(tempCommonSettingsFileName))
                    {
                        if (DebugConfig.DebugLoadSettings) Log.WriteLine("DateOfMonth [" + DateTime.Now.Day + "] - [" + _commonSettingsPath + "] not found");
                        tempCommonSettingsFileName = System.IO.Path.Combine(Instance.Path, "QuestorSettings", NameOfCommonXml + "-" + CurrentDayOfTheWeek() + ".xml");
                    }
                    //ex: Does common-monday.xml exist? if not use common.xml
                    if (!File.Exists(tempCommonSettingsFileName))
                    {
                        if (DebugConfig.DebugLoadSettings) Log.WriteLine("DayOfWeek [" + CurrentDayOfTheWeek() + "] - [" + _commonSettingsPath + "] not found");
                        tempCommonSettingsFileName = System.IO.Path.Combine(Instance.Path, "QuestorSettings", NameOfCommonXml + ".xml");
                    }

                    _commonSettingsPath = tempCommonSettingsFileName;
                    return _commonSettingsPath;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return System.IO.Path.Combine(Instance.Path, "QuestorSettings", NameOfCommonXml + ".xml");
                }
            }
        }

        public static XElement CommonSettingsXml { get; set; }
        public static Settings Instance => _instance;

        public static string NameOfCommonXml //full path plus filename without file extension
        {
            get
            {
                try
                {
                    return System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.Combine(Instance.Path, "QuestorSettings", CommonSettingsFileName));
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return string.Empty;
                }
            }
        }

        public string AbyssalFleetMemberName1 = string.Empty;
        public string AbyssalFleetMemberName2 = string.Empty;
        public string AbyssalFleetMemberName3 = string.Empty;
        public string AbyssalFleetMemberCharacterId1 = string.Empty;
        public string AbyssalFleetMemberCharacterId2 = string.Empty;
        public string AbyssalFleetMemberCharacterId3 = string.Empty;

        public int AncillaryShieldBoosterScript { get; private set; }

        public int GlobalBackgroundFramesPerSecondLimit = 0;

        public int BackgroundFramesPerSecondLimit
        {
            get
            {
                //if ()
                return GlobalBackgroundFramesPerSecondLimit;
            }
        }

        public int GlobalFramesPerSecondLimit = 0;
        public int FramesPerSecondLimit
        {
            get
            {
                return GlobalFramesPerSecondLimit;
            }
        }
        public bool EnableFpsLimits = false;
        public string BookmarkFolder { get; set; }

        //
        // Travel and Undock Settings
        //
        public string BookmarkPrefix { get; set; }

        public bool BuyAmmo { get; private set; }
        public int BuyAmmoStationId { get; private set; }
        public bool BuyLpItems { get; private set; }
        public bool BuyPlex { get; private set; }

        public int CapacitorInjectorScript
        {
            get
            {
                try
                {
                    if (MissionSettings.MissionCapacitorInjectorScript != null && MissionSettings.MissionCapacitorInjectorScript != 0)
                        return (int) MissionSettings.MissionCapacitorInjectorScript;

                    if (GlobalCapacitorInjectorScript != 0)
                        return GlobalCapacitorInjectorScript;

                    return 0;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 0;
                }
            }
        }

        //
        // Misc Settings
        //
        private string CharacterToAcceptInvitesFrom { get; set; }
        public bool CreateCourierContracts { get; private set; }
        public bool CreateDockBookmarksAsNeeded { get; set; } = false;
        public bool CreateUndockBookmarksAsNeeded { get; set; } = false;

        private bool DetailedCurrentTargetHealthLogging { get; set; }

        public bool Disable3D
        {
            get
            {
                if (ESCache.Instance.Paused)
                {
                    if (!ESCache.Instance.EveAccount.DoneLaunchingEveInstance)
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastSessionReady), DateTime.UtcNow);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.DoneLaunchingEveInstance), true);
                        //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AllowSimultaneousLogins), false);
                    }

                    if (DebugConfig.DebugReduceGraphicsController && !ESCache.Instance.InStation) Log.WriteLine("Disable3D returning false because we are paused: this forces 3d on when paused");
                    if (!DebugConfig.DebugReduceGraphicsController)
                        return false;

                    return Disable3dGlobal;
                }

                return Disable3dGlobal;
            }
        }

        public bool DisableResourceLoad
        {
            get
            {
                if (ESCache.Instance.Paused)
                    return false;

                return DisableResourceLoadGlobal;
            }
        }

        private bool DisableResourceLoadGlobal { get; set; }

        private bool Disable3dGlobal { get; set; }

        public bool Disable3dInStation { get; set; }

        //
        // Enable / Disable Major Features that do not have categories of their own below
        //
        public bool EnableStorylines { get; set; } = false;

        public int EnforcedDelayBetweenModuleClicks { get; set; }

        public string EveServerName { get; set; }

        public bool FinishWhenNotSafe { get; set; }
        private int GlobalCapacitorInjectorScript { get; set; }
        private int GlobalNumberOfCapBoostersToLoad { get; set; }
        public string HighTierLootContainer { get; set; } = string.Empty;

        //
        // Storage location for loot, ammo, and bookmarks
        //
        public string HomeBookmarkName { get; set; }

        public bool KeepWeaponsGrouped { get; set; }
        public double LocalBadStandingLevelToConsiderBad { get; set; }

        //
        // Local Watch settings - if enabled
        //
        public int LocalBadStandingPilotsToTolerate { get; set; }

        public string LootContainerName { get; set; } = string.Empty;
        public string MiningShipName { get; set; }

        //they are not scripts, but they work the same, but are consumable for our purposes that does not matter
        public int NumberOfCapBoostersToLoad
        {
            get
            {
                try
                {
                    if (MissionSettings.MissionNumberOfCapBoostersToLoad != null && MissionSettings.MissionNumberOfCapBoostersToLoad != 0)
                        return (int) MissionSettings.MissionNumberOfCapBoostersToLoad;

                    if (GlobalNumberOfCapBoostersToLoad != 0)
                        return GlobalNumberOfCapBoostersToLoad;

                    return 0;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return 0;
                }
            }
        }

        public string SafeSpotBookmarkPrefix { get; set; }

        //
        // Ship Names
        //
        public string SalvageShipName { get; set; }

        public int SensorBoosterScript { get; private set; }
        public int SensorDampenerScript { get; private set; }
        public string StationDockBookmarkPrefix { get; set; }
        public string StoryLineBaseBookmark { get; set; }
        public bool StorylineDoNotTryToDoCourierMissions { get; set; } = false;
        public bool StorylineDoNotTryToDoEncounterMissions { get; set; } = false;
        public bool StorylineDoNotTryToDoMiningMissions { get; set; } = true;
        public bool StorylineDoNotTryToDoTradeMissions { get; set; } = false;
        public bool DoNotTryToDoEncounterMissions { get; set; } = false;
        public string StorylineTransportShipName { get; set; } = "myTransportShip";
        public int TrackingComputerScript { get; private set; }

        //
        // Script Settings - TypeIDs for the scripts you would like to use in these modules
        //
        public int TrackingDisruptorScript { get; private set; }

        public int TrackingLinkScript { get; private set; }
        public string TransportShipName { get; set; }
        public string TravelShipName { get; set; }
        public string UndockBookmarkPrefix { get; set; }
        public bool UseCorpAmmoHangar { get; set; }
        public bool UseCorpLootHangar { get; set; }
        public bool UseInvasionManager = false;

        public int LootCorpHangarDivisionNumber = 1;
        public int AmmoCorpHangarDivisionNumber = 1;
        public int HighTierLootCorpHangarDivisionNumber = 1;

        public bool UseDockBookmarks { get; set; }

        private bool GlobalUseFittingManager { get; set; }

        public bool UseFittingManager
        {
            get
            {
                if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                {
                    if (DebugConfig.DebugFittingMgr) Log.WriteLine("UseFittingManager we are using AbyssalDeadspaceController return false");
                    return false;
                }

                if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                {
                    if (DebugConfig.DebugFittingMgr) Log.WriteLine("UseFittingManager we are using CareerAgentController return false");
                    return false;
                }

                if (GlobalUseFittingManager)
                    return true;

                if (DebugConfig.DebugFittingMgr) Log.WriteLine("UseFittingManager is [" + GlobalUseFittingManager + "]");
                return false;
            }
            set
            {
                GlobalUseFittingManager = value;
                if (DebugConfig.DebugFittingMgr) Log.WriteLine("UseFittingManager is now [" + GlobalUseFittingManager + "]");
            }
        }

        public bool UseFleetManager { get; set; }
        public bool UseLocalWatch { get; set; }
        public bool UseUndockBookmarks { get; set; }
        public bool WatchForActiveWars { get; set; }
        public bool AllowPvpInWspace { get; set; }
        public bool AllowPvpInHighSecuritySpace { get; set; }
        public bool AllowPvpInLowSecuritySpace { get; set; }
        public bool AllowPvpInZeroZeroSpace { get; set; }
        public bool AllowPvpInAbyssalSpace { get; set; }
        public bool AllowBuyingItems { get; set; }


        #endregion Properties

        #region Methods

        public static void InvalidateCache()
        {
            _characterSettingsPath = null;
            _commonSettingsPath = null;
        }

        public void CreateDirectoriesForLogging()
        {
            try
            {
                Statistics.DroneStatsLogPath = Log.BotLogpath;
                Statistics.DroneStatslogFile = System.IO.Path.Combine(Statistics.DroneStatsLogPath, ESCache.Instance.EveAccount.MaskedCharacterName + "-DroneStats.csv");
                Statistics.MissionAcceptDeclineStatsLogPath = Log.BotLogpath;
                Statistics.MissionAcceptDeclineStatsLogFile = System.IO.Path.Combine(Statistics.MissionAcceptDeclineStatsLogPath, ESCache.Instance.EveAccount.MaskedCharacterName + "-MissionAcceptDecline.csv");

                Statistics.WreckOutOfRangeLootSkipLogPath = Log.BotLogpath;
                Statistics.WreckOutOfRangeLootSkipLogFile = System.IO.Path.Combine(Statistics.WreckOutOfRangeLootSkipLogPath, ESCache.Instance.EveAccount.MaskedCharacterName + "-WreckOutOfRangeLootSkipLog.csv");

                Statistics.WindowStatsLogPath = System.IO.Path.Combine(Log.BotLogpath, "WindowStats\\");
                Statistics.WindowStatslogFile = System.IO.Path.Combine(Statistics.WindowStatsLogPath,
                    ESCache.Instance.EveAccount.MaskedCharacterName + "-WindowStats-DayOfYear[" + DateTime.UtcNow.DayOfYear + "].csv");
                Statistics.WreckLootStatisticsPath = Log.BotLogpath;
                Statistics.WreckLootStatisticsFile = System.IO.Path.Combine(Statistics.WreckLootStatisticsPath,
                    ESCache.Instance.EveAccount.MaskedCharacterName + "-WreckLootStatisticsDump.csv");

                Statistics.MissionStats3LogPath = System.IO.Path.Combine(Log.BotLogpath, "MissionStats\\");
                Statistics.MissionStats3LogFile = System.IO.Path.Combine(Statistics.MissionStats3LogPath,
                    ESCache.Instance.EveAccount.MaskedCharacterName + "-CustomDatedStatistics.csv");
                Statistics.MissionDungeonIdLogPath = System.IO.Path.Combine(Log.BotLogpath, "MissionStats\\");
                Statistics.MissionDungeonIdLogFile = System.IO.Path.Combine(Statistics.MissionDungeonIdLogPath,
                    ESCache.Instance.EveAccount.MaskedCharacterName + "Mission-DungeonId-list.csv");
                Statistics.PocketStatisticsPath = System.IO.Path.Combine(Log.BotLogpath, "PocketStats\\");
                Statistics.PocketStatisticsFile = System.IO.Path.Combine(Statistics.PocketStatisticsPath,
                    ESCache.Instance.EveAccount.MaskedCharacterName + "pocketstats-combined.csv");
                Statistics.PocketObjectStatisticsPath = System.IO.Path.Combine(Log.BotLogpath, "PocketObjectStats\\");
                Statistics.PocketObjectStatisticsFile = System.IO.Path.Combine(Statistics.PocketObjectStatisticsPath,
                    ESCache.Instance.EveAccount.MaskedCharacterName + "PocketObjectStats-combined.csv");
                Statistics.MissionDetailsHtmlPath = System.IO.Path.Combine(Log.BotLogpath, "MissionDetailsHTML\\");
                Statistics.MissionPocketObjectivesPath = System.IO.Path.Combine(Log.BotLogpath, "MissionPocketObjectives\\");
                Statistics.AbyssalSpawnStatisticsPath = System.IO.Path.Combine(Log.BotLogpath, "AbyssalSpawnStats\\");
                Statistics.AbyssalSpawnStatisticsFile = System.IO.Path.Combine(Statistics.AbyssalSpawnStatisticsPath,
                    ESCache.Instance.EveAccount.MaskedCharacterName + "AbyssalSpawnStats.log");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Problem creating directory path strings for logs [" + ex + "]");
            }

            try
            {
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Log.LogPath [" + Log.BotLogpath + "]");
                Directory.CreateDirectory(Log.BotLogpath);
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Log.ConsoleLogPath [" + Log.ConsoleLogPath + "]");
                Directory.CreateDirectory(Log.ConsoleLogPath);
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Statistics.DroneStatsLogPath [" + Statistics.DroneStatsLogPath + "]");
                Directory.CreateDirectory(Statistics.DroneStatsLogPath);
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Statistics.MissionAcceptDeclineStatsLogPath [" + Statistics.MissionAcceptDeclineStatsLogPath + "]");
                Directory.CreateDirectory(Statistics.MissionAcceptDeclineStatsLogPath);
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Statistics.WreckLootStatisticsPath [" + Statistics.WreckLootStatisticsPath + "]");
                Directory.CreateDirectory(Statistics.WreckLootStatisticsPath);
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Statistics.MissionStats3LogPath [" + Statistics.MissionStats3LogPath + "]");
                Directory.CreateDirectory(Statistics.MissionStats3LogPath);
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Statistics.MissionDungeonIdLogPath [" + Statistics.MissionDungeonIdLogPath + "]");
                Directory.CreateDirectory(Statistics.MissionDungeonIdLogPath);
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Statistics.PocketStatisticsPath [" + Statistics.PocketStatisticsPath + "]");
                Directory.CreateDirectory(Statistics.PocketStatisticsPath);
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Statistics.PocketObjectStatisticsPath [" + Statistics.PocketObjectStatisticsPath + "]");
                Directory.CreateDirectory(Statistics.PocketObjectStatisticsPath);
                if (DebugConfig.DebugLoadSettings) Log.WriteLine("CreateDirectory Statistics.WindowStatsLogPath [" + Statistics.WindowStatsLogPath + "]");
                Directory.CreateDirectory(Statistics.WindowStatsLogPath);
            }
            catch (Exception exception)
            {
                Log.WriteLine("Problem creating directories for logs [" + exception + "]");
            }
            //create all the logging directories even if they are not configured to be used - we can adjust this later if it really bugs people to have some potentially empty directories.
        }

        private bool IsFileLocked(string file)
        {
            FileStream stream = null;

            try
            {
                stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }


        public void LoadSettings_Initialize(bool forceReload = false)
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.NextLoadSettings)
                    return;

                Time.Instance.NextLoadSettings = DateTime.UtcNow.AddSeconds(10);

                try
                {
                    bool reloadSettings = true;
                    if (File.Exists(CharacterSettingsPath))
                    {
                        reloadSettings = _lastModifiedDateOfMySettingsFile != File.GetLastWriteTime(CharacterSettingsPath);
                        if (!reloadSettings)
                            if (File.Exists(CommonSettingsPath)) reloadSettings = _lastModifiedDateOfMyCommonSettingsFile != File.GetLastWriteTime(CommonSettingsPath);
                        if (!reloadSettings && forceReload) reloadSettings = true;

                        if (!reloadSettings)
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                if (!File.Exists(CharacterSettingsPath) && !Instance.DefaultSettingsLoaded)
                    //if the settings file does not exist initialize these values. Should we not halt when missing the settings XML?
                {
                    Log.WriteLine("Settings: if (!File.Exists(Logging.CharacterSettingsPath) && !Settings.Instance.DefaultSettingsLoaded)");

                    File.Copy(templateSettingsPath, CharacterSettingsPath, true);
                    while (IsFileLocked(CharacterSettingsPath))
                    {
                        Log.WriteLine("Settings: waiting for [" + CharacterSettingsPath + "] to finish copying from [" + templateSettingsPath + "]");
                        Thread.Sleep(300);
                        return;
                    }

                    return;
                }
                else //if the settings file exists - load the characters settings XML
                {
                    CharacterXmlExists = true;

                    using (XmlTextReader reader = new XmlTextReader(CharacterSettingsPath))
                    {
                        reader.EntityHandling = EntityHandling.ExpandEntities;
                        CharacterSettingsXml = XDocument.Load(reader).Root;
                    }

                    if (CharacterSettingsXml == null)
                        Log.WriteLine("unable to find [" + CharacterSettingsPath + "] FATAL ERROR - use the provided settings.xml to create that file.");
                    else
                        try
                        {
                            PushInfoToEveSharpLauncher();
                            if (File.Exists(CharacterSettingsPath)) _lastModifiedDateOfMySettingsFile = File.GetLastWriteTime(CharacterSettingsPath);
                            if (File.Exists(CommonSettingsPath)) _lastModifiedDateOfMyCommonSettingsFile = File.GetLastWriteTime(CommonSettingsPath);
                            CreateDirectoriesForLogging();
                            LoadSettings();
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }
                }

                if (!Instance.DefaultSettingsLoaded)
                {
                    _settingsLoadedICount++;
                    if (_commonXmlExists)
                        Log.WriteLine("[" + _settingsLoadedICount + "] Done Loading Settings from [" + CommonSettingsPath + "] and");
                    Log.WriteLine("[" + _settingsLoadedICount + "] Done Loading Settings from [" + CharacterSettingsPath + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Problem creating directories for logs [" + ex + "]");
            }
        }

        public void PushInfoToEveSharpLauncher()
        {
            try
            {
                //if (DebugConfig.DebugLoadSettings) Log.WriteLine("Settings: myCharacterId [" + ESCache.Instance.DirectEve.Session.MyMaskedCharacterId + "]");
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MyCharacterId), ESCache.Instance.DirectEve.Session.CharacterId.ToString());
                if (ESCache.Instance.DirectEve.Session.SolarSystemId != null)
                {
                    string solarsystem = ESCache.Instance.DirectEve.GetLocationName((long) ESCache.Instance.DirectEve.Session.SolarSystemId);
                    if (DebugConfig.DebugLoadSettings) Log.WriteLine("Settings: SolarSystem [" + solarsystem + "]");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.SolarSystem), solarsystem);
                }

                if (DebugConfig.DebugLoadSettings) Log.WriteLine("Settings: IsOmegaClone [" + ESCache.Instance.DirectEve.Me.IsOmegaClone + "]");
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsOmegaClone), ESCache.Instance.DirectEve.Me.IsOmegaClone);

                //Log.WriteLine("Settings: SubEnd [" + ESCache.Instance.DirectEve.Me.SubTimeEnd + "]");
                //WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.EveAccount.CharacterName, nameof(EveAccountSubEnd", ESCache.Instance.DirectEve.Me.SubTimeEnd);

                if (DebugConfig.DebugLoadSettings) Log.WriteLine("Settings: IsAtWar [" + ESCache.Instance.DirectEve.Me.IsAtWar + "]");
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsAtWar), ESCache.Instance.DirectEve.Me.IsAtWar);

                if (ESCache.Instance.DirectEve.Me.IsAtWar)
                {
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NextWarCheck), DateTime.UtcNow.AddDays(1));
                    if (ESCache.Instance.InSpace && State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.GotoBase)
                    {
                        Log.WriteLine("Settings: IsAtWar [" + ESCache.Instance.DirectEve.Me.IsAtWar + "] and we are in space. GoToBase");
                        State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                        return;
                    }

                    Log.WriteLine("Settings: IsAtWar [" + ESCache.Instance.DirectEve.Me.IsAtWar + "] and we are in station. pausing.");
                    ControllerManager.Instance.SetPause(true);
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        public int LoadXmlStringValueAndConvertToInt(string StringXmlElementNameToFind, int IntDefaultValue)
        {
            try
            {
                string stringValueWeFound = (string)CharacterSettingsXml.Element(StringXmlElementNameToFind) ??
                                              (string)CommonSettingsXml.Element(StringXmlElementNameToFind) ?? "";
                int? IntValueToReturn = null;
                if (!string.IsNullOrEmpty(stringValueWeFound))
                {
                    IntValueToReturn = int.Parse(stringValueWeFound);
                    return IntValueToReturn ?? IntDefaultValue;
                }

                return IntDefaultValue;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return IntDefaultValue;
            }
        }

        private void LoadSettings()
        {
            try
            {
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.RestartOfEveClientNeeded), false);
                Log.WriteLine("Start reading settings from xml");

                if (File.Exists(CommonSettingsPath))
                {
                    Log.WriteLine("Loading Common Settings XML [" + CommonSettingsPath + "]");
                    _commonXmlExists = true;
                    CommonSettingsXml = XDocument.Load(CommonSettingsPath).Root;
                    if (CommonSettingsXml == null)
                        Log.WriteLine("found [" + CommonSettingsPath +
                                      "] but was unable to load it: FATAL ERROR - use the provided settings.xml to create that file.");
                }
                else
                {
                    _commonXmlExists = false;
                    //
                    // if the common XML does not exist, load the characters XML into the CommonSettingsXml just so we can simplify the XML element loading stuff.
                    //
                    Log.WriteLine("Common Settings XML [" + CommonSettingsPath + "] not found.");
                    CommonSettingsXml = XDocument.Load(CharacterSettingsPath).Root;
                }

                if (CommonSettingsXml == null)
                    return;

                // this should never happen as we load the characters xml here if the common xml is missing. adding this does quiet some warnings though

                if (_commonXmlExists)
                    Log.WriteLine("Loading Settings from [" + CommonSettingsPath + "] and");
                Log.WriteLine("Loading Settings from [" + CharacterSettingsPath + "]");
                //
                // these are listed by feature and should likely be re-ordered to reflect that
                //

                if (DirectEve.Interval(90000) && ESCache.Instance.EveAccount.LastQuestorStarted.AddSeconds(30) > DateTime.UtcNow)
                {
                    Log.WriteLine("if (DirectEve.Interval(90000) && ESCache.Instance.EveAccount.LastQuestorStarted.AddSeconds(30) > DateTime.UtcNow) Util.FlushMemIfThisProcessIsUsingTooMuchMemory(2048);");
                    Util.FlushMemIfThisProcessIsUsingTooMuchMemory(2048);
                }

                //
                // Debug Settings
                //
                DebugConfig.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                GlobalBackgroundFramesPerSecondLimit = (int?)CharacterSettingsXml.Element("backgroundFramesPerSecondLimit") ??
                                (int?)CommonSettingsXml.Element("backgroundFramesPerSecondLimit") ?? ESCache.Instance.EveSetting.BackgroundFramesPerSecondLimit;
                Log.WriteLine("Settings: backgroundFPSLimit [" + GlobalBackgroundFramesPerSecondLimit + "]");
                GlobalFramesPerSecondLimit = (int?)CharacterSettingsXml.Element("framesPerSecondLimit") ??
                                   (int?)CommonSettingsXml.Element("framesPerSecondLimit") ?? ESCache.Instance.EveSetting.FramesPerSecondLimit;
                Log.WriteLine("Settings: backgroundFPSMin [" + GlobalFramesPerSecondLimit + "]");
                EnableFpsLimits = (bool?)CharacterSettingsXml.Element("enableFPSLimits") ??
                                   (bool?)CommonSettingsXml.Element("enableFPSLimits") ?? true;
                Log.WriteLine("Settings: enableFPSLimits [" + EnableFpsLimits + "]");

                DetailedCurrentTargetHealthLogging = (bool?) CharacterSettingsXml.Element("detailedCurrentTargetHealthLogging") ??
                                                     (bool?) CommonSettingsXml.Element("detailedCurrentTargetHealthLogging") ?? false;
                Log.WriteLine("Settings: detailedCurrentTargetHealthLogging [" + DetailedCurrentTargetHealthLogging + "]");
                CreateCourierContracts = (bool?) CharacterSettingsXml.Element("createCourierContracts") ?? (bool?) CommonSettingsXml.Element("createCourierContracts") ?? false;
                Log.WriteLine("Settings: createCourierContracts [" + CreateCourierContracts + "]");
                BuyPlex = (bool?) CharacterSettingsXml.Element("buyPlex") ?? (bool?) CommonSettingsXml.Element("buyPlex") ?? false;
                Log.WriteLine("Settings: buyPlex [" + BuyPlex + "]");
                BuyAmmo = (bool?) CharacterSettingsXml.Element("buyAmmo") ?? (bool?) CommonSettingsXml.Element("buyAmmo") ?? false;
                Log.WriteLine("Settings: buyAmmo [" + BuyAmmo + "]");
                BuyLpItems = (bool?) CharacterSettingsXml.Element("buyLpItems") ?? (bool?) CommonSettingsXml.Element("buyLpItems") ?? false;
                Log.WriteLine("Settings: buyLpItems [" + BuyLpItems + "]");
                BuyAmmoStationId = (int?) CharacterSettingsXml.Element("buyAmmoStationID") ?? (int?) CommonSettingsXml.Element("buyAmmoStationID") ?? 60003760;
                Log.WriteLine("Settings: buyAmmoStationID [" + BuyAmmoStationId + "]");

                EveServerName = (string) CharacterSettingsXml.Element("eveServerName") ?? (string) CommonSettingsXml.Element("eveServerName") ?? "Tranquility";
                EnforcedDelayBetweenModuleClicks = (int?) CharacterSettingsXml.Element("enforcedDelayBetweenModuleClicks") ??
                                                   (int?) CommonSettingsXml.Element("enforcedDelayBetweenModuleClicks") ?? 3000;

                //
                // Misc Settings
                //
                Disable3dGlobal = (bool?) CharacterSettingsXml.Element("disable3D") ?? (bool?) CommonSettingsXml.Element("disable3D") ?? false;
                Log.WriteLine("Settings: disable3D [" + Disable3dGlobal + "]");

                Disable3dInStation = (bool?)CharacterSettingsXml.Element("disable3dInStation") ?? (bool?)CommonSettingsXml.Element("disable3dInStation") ?? false;
                Log.WriteLine("Settings: disable3dInStation [" + Disable3dInStation + "]");

                DisableResourceLoadGlobal = (bool?)CharacterSettingsXml.Element("disableResourceLoad") ?? (bool?)CommonSettingsXml.Element("disableResourceLoad") ?? false;
                Log.WriteLine("Settings: disableResourceLoad [" + DisableResourceLoadGlobal + "]");

                try
                {
                    UseCorpAmmoHangar =
                        (bool?)CharacterSettingsXml.Element("useCorpAmmoHangar") ??
                        (bool?)CommonSettingsXml.Element("useCorpAmmoHangar") ?? false;
                    Log.WriteLine("Settings: useCorpAmmoHangar [" + UseCorpAmmoHangar + "]");

                    UseCorpLootHangar =
                        (bool?)CharacterSettingsXml.Element("useCorpLootHangar") ??
                        (bool?)CommonSettingsXml.Element("useCorpLootHangar") ?? false;
                    Log.WriteLine("Settings: useCorpLootHangar [" + UseCorpLootHangar + "]");

                    UseFittingManager = (bool?) CharacterSettingsXml.Element("UseFittingManager") ??
                                        (bool?) CommonSettingsXml.Element("UseFittingManager") ?? true;
                    Log.WriteLine("Settings: UseFittingManager [" + GlobalUseFittingManager + "]");
                    UseFleetManager = (bool?) CharacterSettingsXml.Element("UseFleetManager") ??
                                      (bool?) CommonSettingsXml.Element("UseFleetManager") ?? true;
                    Log.WriteLine("Settings: UseFleetManager [" + UseFleetManager + "]");


                    UseLocalWatch = (bool?) CharacterSettingsXml.Element("UseLocalWatch") ?? (bool?) CommonSettingsXml.Element("UseLocalWatch") ?? true;
                    Log.WriteLine("Settings: UseLocalWatch [" + UseLocalWatch + "]");
                    WatchForActiveWars = (bool?) CharacterSettingsXml.Element("watchForActiveWars") ??
                                         (bool?) CommonSettingsXml.Element("watchForActiveWars") ?? true;
                    Log.WriteLine("Settings: watchForActiveWars [" + WatchForActiveWars + "]");
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Major Feature Settings: Exception [" + exception + "]");
                }

                try
                {
                    Log.WriteLine("CacheHangars");

                    AmmoCorpHangarDivisionNumber =
                        (int?)CharacterSettingsXml.Element("ammoCorpHangarDivisionNumber") ?? (int?)CharacterSettingsXml.Element("ammoCorpHangarDivisionNumber") ??
                        (int?)CommonSettingsXml.Element("ammoCorpHangarDivisionNumber") ?? (int?)CommonSettingsXml.Element("ammoCorpHangarDivisionNumber") ?? 1;
                    Log.WriteLine("CacheHangars: AmmoCorpHangarDivisionNumber [" + AmmoCorpHangarDivisionNumber + "]");
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    AmmoCorpHangarDivisionNumber = 1;
                    Log.WriteLine("CacheHangars: AmmoCorpHangarDivisionNumber [" + AmmoCorpHangarDivisionNumber + "]");
                }
                try
                {
                    LootCorpHangarDivisionNumber =
                        (int?) CharacterSettingsXml.Element("lootCorpHangarDivisionNumber") ?? (int?) CharacterSettingsXml.Element("lootCorpHangarDivisionNumber") ??
                        (int?) CommonSettingsXml.Element("lootCorpHangarDivisionNumber") ?? (int?) CommonSettingsXml.Element("lootCorpHangarDivisionNumber") ?? 1;
                    Log.WriteLine("CacheHangars: LootCorpHangarDivisionNumber [" + LootCorpHangarDivisionNumber + "]");
                }
                catch (Exception)
                {
                    LootCorpHangarDivisionNumber = 1;
                    Log.WriteLine("CacheHangars: LootCorpHangarDivisionNumber [" + LootCorpHangarDivisionNumber + "]");
                }
                try
                {
                    HighTierLootCorpHangarDivisionNumber =
                        (int?)CharacterSettingsXml.Element("highTierLootCorpHangarDivisionNumber") ?? (int?)CharacterSettingsXml.Element("highTierLootCorpHangarDivisionNumber") ??
                        (int?)CommonSettingsXml.Element("highTierLootCorpHangarDivisionNumber") ?? (int?)CommonSettingsXml.Element("highTierLootCorpHangarDivisionNumber") ?? 1;
                    Log.WriteLine("CacheHangars: HighTierLootCorpHangarDivisionNumber [" + HighTierLootCorpHangarDivisionNumber + "]");
                }
                catch (Exception)
                {
                    HighTierLootCorpHangarDivisionNumber = 1;
                    Log.WriteLine("CacheHangars: HighTierLootCorpHangarDivisionNumber [" + HighTierLootCorpHangarDivisionNumber + "]");
                }

                //
                // Local Watch Settings - if enabled
                //
                try
                {
                    //LocalBadStandingPilotsToTolerate = (int?) CharacterSettingsXml.Element("LocalBadStandingPilotsToTolerate") ??
                    //                                   (int?) CommonSettingsXml.Element("LocalBadStandingPilotsToTolerate") ?? 1;
                    //Log.WriteLine("Settings: LocalBadStandingPilotsToTolerate [" + LocalBadStandingPilotsToTolerate + "]");
                    //LocalBadStandingLevelToConsiderBad = (double?) CharacterSettingsXml.Element("LocalBadStandingLevelToConsiderBad") ??
                    //                                     (double?) CommonSettingsXml.Element("LocalBadStandingLevelToConsiderBad") ?? -0.1;
                    //Log.WriteLine("Settings: LocalBadStandingLevelToConsiderBad [" + LocalBadStandingLevelToConsiderBad + "]");
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Local watch Settings: Exception [" + exception + "]");
                }

                //
                // Undock settings
                //
                UseUndockBookmarks = (bool?)CharacterSettingsXml.Element("useUndockBookmarks") ?? (bool?)CharacterSettingsXml.Element("useundockbookmarks") ??
                                     (bool?)CommonSettingsXml.Element("useUndockBookmarks") ?? (bool?)CommonSettingsXml.Element("useundockbookmarks") ?? true;
                Log.WriteLine("Settings: useUndockBookmarks [" + UseUndockBookmarks + "]");

                if (UseUndockBookmarks)
                {
                    UndockBookmarkPrefix = (string)CharacterSettingsXml.Element("stationUndockPrefix") ?? (string)CharacterSettingsXml.Element("stationundockprefix") ??
                                           (string)CommonSettingsXml.Element("stationUndockPrefix") ?? (string)CommonSettingsXml.Element("stationundockprefix") ??
                                           (string)CharacterSettingsXml.Element("undockprefix") ?? (string)CharacterSettingsXml.Element("undockPrefix") ??
                                           (string)CommonSettingsXml.Element("undockprefix") ?? (string)CommonSettingsXml.Element("undockPrefix") ??
                                           (string)CharacterSettingsXml.Element("bookmarkWarpOut") ?? (string)CommonSettingsXml.Element("bookmarkWarpOut") ?? "insta";
                    Log.WriteLine("Settings: stationUndockPrefix [" + UndockBookmarkPrefix + "]");
                    CreateUndockBookmarksAsNeeded = (bool?)CharacterSettingsXml.Element("createUndockBookmarksAsNeeded") ?? (bool?)CharacterSettingsXml.Element("createundockbookmarksasneeded") ??
                                                    (bool?)CommonSettingsXml.Element("createUndockBookmarksAsNeeded") ?? (bool?)CommonSettingsXml.Element("createundockbookmarksasneeded") ?? true;
                    Log.WriteLine("Settings: CreateUndockBookmarksAsNeeded [" + CreateUndockBookmarksAsNeeded + "]");

                }

                UseDockBookmarks =
                    (bool?)CharacterSettingsXml.Element("useDockBookmarks") ?? (bool?)CharacterSettingsXml.Element("usedockbookmarks") ??
                    (bool?)CommonSettingsXml.Element("useDockBookmarks") ?? (bool?)CommonSettingsXml.Element("usedockbookmarks") ?? true;
                Log.WriteLine("Settings: useDockBookmarks [" + UseDockBookmarks + "]");

                if (UseDockBookmarks)
                {
                    CreateDockBookmarksAsNeeded = (bool?)CharacterSettingsXml.Element("createDockBookmarksAsNeeded") ?? (bool?)CharacterSettingsXml.Element("createdockbookmarksasneeded") ??
                                              (bool?)CommonSettingsXml.Element("createDockBookmarksAsNeeded") ?? (bool?)CommonSettingsXml.Element("createdockbookmarksasneeded") ?? true;
                    Log.WriteLine("Settings: createDockBookmarksAsNeeded [" + CreateDockBookmarksAsNeeded + "]");
                }

                //
                // Ship Names
                //
                try
                {
                    SalvageShipName = (string) CharacterSettingsXml.Element("salvageShipName") ??
                                      (string) CommonSettingsXml.Element("salvageShipName") ?? "My Destroyer of salvage";
                    Log.WriteLine("Settings: salvageShipName [" + SalvageShipName + "]");
                    TransportShipName = (string) CharacterSettingsXml.Element("transportShipName") ??
                                        (string) CommonSettingsXml.Element("transportShipName") ?? "My Hauler of transportation";
                    Log.WriteLine("Settings: transportShipName [" + TransportShipName + "]");

                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    {
                        StorylineTransportShipName = (string)CharacterSettingsXml.Element("storylineTransportShipName") ??
                                                 (string)CommonSettingsXml.Element("storylineTransportShipName") ?? TransportShipName;
                        Log.WriteLine("Settings: storylineTransportShipName [" + StorylineTransportShipName + "]");
                        TravelShipName = (string)CharacterSettingsXml.Element("travelShipName") ??
                                         (string)CommonSettingsXml.Element("travelShipName") ?? "My Shuttle of traveling";
                        Log.WriteLine("Settings: travelShipName [" + TravelShipName + "]");
                    }

                    MiningShipName = (string) CharacterSettingsXml.Element("miningShipName") ??
                                     (string) CommonSettingsXml.Element("miningShipName") ?? "My Exhumer of Destruction";
                    Log.WriteLine("Settings: miningShipName [" + MiningShipName + "]");
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Ship Name Settings [" + exception + "]");
                }

                //
                // Storage Location for Loot, DefinedAmmoTypes, Bookmarks
                //
                try
                {
                    HomeBookmarkName = (string) CharacterSettingsXml.Element("homeBookmarkName") ??
                                       (string) CommonSettingsXml.Element("homeBookmarkName") ?? "myHomeBookmark";
                    Log.WriteLine("Settings: homeBookmarkName [" + HomeBookmarkName + "]");
                    LootContainerName = (string) CharacterSettingsXml.Element("lootContainer") ?? (string) CommonSettingsXml.Element("lootContainer");
                    if (LootContainerName != null)
                        LootContainerName = LootContainerName.ToLower();
                    HighTierLootContainer = (string) CharacterSettingsXml.Element("highValueLootContainer") ??
                                            (string) CommonSettingsXml.Element("highValueLootContainer") ?? "FactionLoot";
                    if (HighTierLootContainer != null)
                        HighTierLootContainer = HighTierLootContainer.ToLower();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Hangar Settings [" + exception + "]");
                }

                try
                {
                    BookmarkPrefix = (string) CharacterSettingsXml.Element("bookmarkPrefix") ??
                                     (string) CommonSettingsXml.Element("bookmarkPrefix") ?? "Salvage:";
                    Log.WriteLine("Settings: bookmarkPrefix [" + BookmarkPrefix + "]");
                    StationDockBookmarkPrefix = (string) CharacterSettingsXml.Element("stationDockBookmarkPrefix") ??
                                                (string) CommonSettingsXml.Element("stationDockBookmarkPrefix") ?? "Undock @ 0";
                    Log.WriteLine("Settings: stationDockBookmarkPrefix [" + StationDockBookmarkPrefix + "]");
                    SafeSpotBookmarkPrefix = (string) CharacterSettingsXml.Element("safeSpotBookmarkPrefix") ??
                                             (string) CommonSettingsXml.Element("safeSpotBookmarkPrefix") ?? "safespot";
                    Log.WriteLine("Settings: safeSpotBookmarkPrefix [" + SafeSpotBookmarkPrefix + "]");
                    BookmarkFolder = (string) CharacterSettingsXml.Element("bookmarkFolder") ??
                                     (string) CommonSettingsXml.Element("bookmarkFolder") ?? "Salvage:";
                    Log.WriteLine("Settings: bookmarkFolder [" + BookmarkFolder + "]");
                    KeepWeaponsGrouped = (bool?) CharacterSettingsXml.Element("keepWeaponsGrouped") ??
                                         (bool?) CommonSettingsXml.Element("keepWeaponsGrouped") ??
                                         false;
                    Log.WriteLine("Settings: keepWeaponsGrouped [" + KeepWeaponsGrouped + "]");
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Hangar Settings [" + exception + "]");
                }

                //
                // Script and Shield and Armor Repair Settings - TypeIDs for the scripts you would like to use in these modules
                //
                try
                {
                    TrackingDisruptorScript = LoadXmlStringValueAndConvertToInt("trackingDisruptorScript", (int)TypeID.TrackingSpeedDisruptionScript);
                    Log.WriteLine("Settings: TrackingDisruptorScript [" + TrackingDisruptorScript + "]");

                    TrackingComputerScript = LoadXmlStringValueAndConvertToInt("trackingComputerScript", (int)TypeID.TrackingSpeedScript);
                    Log.WriteLine("Settings: TrackingComputerScript [" + TrackingComputerScript + "]");

                    TrackingLinkScript = LoadXmlStringValueAndConvertToInt("trackingLinkScript", (int)TypeID.TrackingSpeedScript);
                    Log.WriteLine("Settings: TrackingLinkScript [" + TrackingLinkScript + "]");

                    SensorBoosterScript = LoadXmlStringValueAndConvertToInt("sensorBoosterScript", (int)TypeID.TargetingRangeScript);
                    Log.WriteLine("Settings: SensorBoosterScript [" + SensorBoosterScript + "]");

                    SensorDampenerScript = LoadXmlStringValueAndConvertToInt("sensorDampenerScript", (int)TypeID.TargetingRangeDampeningScript);
                    Log.WriteLine("Settings: SensorDampenerScript [" + SensorDampenerScript + "]");

                    AncillaryShieldBoosterScript = LoadXmlStringValueAndConvertToInt("ancillaryShieldBoosterScript", (int)TypeID.AncillaryShieldBoosterScript);
                    Log.WriteLine("Settings: AncillaryShieldBoosterScript [" + AncillaryShieldBoosterScript + "]");

                    GlobalCapacitorInjectorScript = LoadXmlStringValueAndConvertToInt("capacitorInjectorScript", 0);
                    Log.WriteLine("Settings: CapacitorInjectorScript [" + GlobalCapacitorInjectorScript + "]");

                    GlobalNumberOfCapBoostersToLoad = (int?) CharacterSettingsXml.Element("capacitorInjectorToLoad") ??
                                                      (int?) CommonSettingsXml.Element("capacitorInjectorToLoad") ??
                                                      (int?) CharacterSettingsXml.Element("capBoosterToLoad") ??
                                                      (int?) CommonSettingsXml.Element("capBoosterToLoad") ?? 0;
                    Log.WriteLine("Settings: capacitorInjectorToLoad [" + GlobalNumberOfCapBoostersToLoad + "]");
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Script and Booster Settings [" + exception + "]");
                }

                try
                {
                    if (ESCache.Instance.EveAccount.UseFleetMgr)
                    {
                        Log.WriteLine("Settings: My CharacterID [" + ESCache.Instance.DirectEve.Session.CharacterId + "]");
                        CharacterToAcceptInvitesFrom = (string)CharacterSettingsXml.Element("characterToAcceptInvitesFrom") ??
                                                   (string)CommonSettingsXml.Element("characterToAcceptInvitesFrom") ?? string.Empty;

                        //AbyssalFleetMember1 = string.Empty;
                        AbyssalFleetMemberName1 =
                            (string)CharacterSettingsXml.Element("abyssalFleetMemberName1") ??
                            (string)CommonSettingsXml.Element("abyssalFleetMemberName1") ??
                            string.Empty;
                        Log.WriteLine("Settings: abyssalFleetMember1 [" + AbyssalFleetMemberName1 + "]");
                        AbyssalFleetMemberCharacterId1 =
                            (string)CharacterSettingsXml.Element("abyssalFleetMemberCharacterId1") ??
                            (string)CommonSettingsXml.Element("abyssalFleetMemberCharacterId1") ??
                            string.Empty;
                        Log.WriteLine("Settings: abyssalFleetMemberCharacterId1 [" + AbyssalFleetMemberCharacterId1 + "]");
                        //AbyssalFleetMember2 = string.Empty;
                        AbyssalFleetMemberName2 =
                            (string)CharacterSettingsXml.Element("abyssalFleetMemberName2") ??
                            (string)CommonSettingsXml.Element("abyssalFleetMemberName2") ??
                            string.Empty;
                        Log.WriteLine("Settings: abyssalFleetMember2 [" + AbyssalFleetMemberName2 + "]");
                        AbyssalFleetMemberCharacterId2 =
                            (string)CharacterSettingsXml.Element("abyssalFleetMemberCharacterId2") ??
                            (string)CommonSettingsXml.Element("abyssalFleetMemberCharacterId2") ??
                            string.Empty;
                        Log.WriteLine("Settings: abyssalFleetMemberCharacterId2 [" + AbyssalFleetMemberCharacterId2 + "]");
                        //AbyssalFleetMember3 = string.Empty;
                        AbyssalFleetMemberName3 =
                            (string)CharacterSettingsXml.Element("abyssalFleetMemberName3") ??
                            (string)CommonSettingsXml.Element("abyssalFleetMemberName3") ??
                            string.Empty;
                        Log.WriteLine("Settings: abyssalFleetMember3 [" + AbyssalFleetMemberName3 + "]");
                        AbyssalFleetMemberCharacterId3 =
                            (string)CharacterSettingsXml.Element("abyssalFleetMemberCharacterId3") ??
                            (string)CommonSettingsXml.Element("abyssalFleetMemberCharacterId3") ??
                            string.Empty;
                        Log.WriteLine("Settings: abyssalFleetMemberCharacterId2 [" + AbyssalFleetMemberCharacterId3 + "]");

                    }

                    AllowPvpInWspace = (bool?)CharacterSettingsXml.Element("allowPvpInWspace") ?? (bool?)CommonSettingsXml.Element("allowPvpInWspace") ?? true;
                    Log.WriteLine("Settings: AllowPvpInWspace [" + AllowPvpInWspace + "]");

                    AllowPvpInHighSecuritySpace = (bool?)CharacterSettingsXml.Element("allowPvpInHighSecuritySpace") ?? (bool?)CommonSettingsXml.Element("allowPvpInHighSecuritySpace") ?? false;
                    Log.WriteLine("Settings: AllowPvpInHighSecuritySpace [" + AllowPvpInHighSecuritySpace + "]");

                    AllowPvpInLowSecuritySpace = (bool?)CharacterSettingsXml.Element("allowPvpInLowSecuritySpace") ?? (bool?)CommonSettingsXml.Element("allowPvpInLowSecuritySpace") ?? false;
                    Log.WriteLine("Settings: AllowPvpInLowSecuritySpace [" + AllowPvpInLowSecuritySpace + "]");

                    AllowPvpInZeroZeroSpace = (bool?)CharacterSettingsXml.Element("allowPvpInZeroZeroSpace") ?? (bool?)CommonSettingsXml.Element("allowPvpInZeroZeroSpace") ?? false;
                    Log.WriteLine("Settings: AllowPvpInZeroZeroSpace [" + AllowPvpInZeroZeroSpace + "]");

                    AllowPvpInAbyssalSpace = (bool?)CharacterSettingsXml.Element("allowPvpInAbyssalSpace") ?? (bool?)CommonSettingsXml.Element("allowPvpInAbyssalSpace") ?? false;
                    Log.WriteLine("Settings: AllowPvpInAbyssalSpace [" + AllowPvpInAbyssalSpace + "]");

                    AllowBuyingItems = (bool?)CharacterSettingsXml.Element("allowBuyingItems") ?? (bool?)CommonSettingsXml.Element("allowBuyingItems") ?? false;
                    Log.WriteLine("Settings: allowBuyingItems [" + AllowBuyingItems + "]");
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Script and Booster Settings [" + exception + "]");
                }

                try
                {
                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    {
                        MissionSettings.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                        CourierContractController.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                        AgentInteraction.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                    }
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error SelectedControllerUsesCombatMissionsBehavior Settings [" + exception + "]");
                }

                try
                {
                    Log.WriteLine("SelectedController [" + ESCache.Instance.EveAccount.SelectedController.ToString() + "]");

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                        AbyssalDeadspaceBehavior.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                        AbyssalController.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    //if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.FleetAbyssalDeadspaceController))
                    //    AbyssalDeadspaceBehavior.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                        HighSecAnomalyBehavior.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.MarketAdjustController))
                        MarketAdjustBehavior.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.MiningController))
                        MiningBehavior.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.MonitorGridController))
                        MonitorGridController.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.WSpaceScoutController))
                        WSpaceScoutBehavior.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.IndustryController))
                        IndustryBehavior.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.TransportItemTypesController))
                        TransportItemTypesBehavior.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.ItemTransportController))
                        ItemTransportController.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (Scanner.SelectedControllerUsesScanner)
                        Scanner.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                }

                try
                {
                    Arm.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                    Salvage.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                    Combat.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                    Drones.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                    NavigateOnGrid.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                    Defense.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                    Panic.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    if (ESCache.Instance.EveAccount.AutoSkillTraining)
                        SkillQueue.LoadSettings(CharacterSettingsXml, CommonSettingsXml);

                    Traveler.LoadSettings(CharacterSettingsXml, CommonSettingsXml);
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                }

                try
                {
                    //
                    // Items to buy on market and bring to home station (if needed)
                    //
                    ListOfItemsToBuyFromLpStore = new List<InventoryItem>();
                    XElement xmlListOfItemsToBuyFromLpStore = CharacterSettingsXml.Element("itemsToBuyFromLpStore") ?? CommonSettingsXml.Element("itemsToBuyFromLpStore");
                    if (xmlListOfItemsToBuyFromLpStore != null)
                    {
                        int itemNum = 0;
                        foreach (XElement item in xmlListOfItemsToBuyFromLpStore.Elements("itemToBuyFromLpStore"))
                        {
                            itemNum++;
                            InventoryItem _itemToBuy = new InventoryItem(item);
                            Log.WriteLine("ListOfItemsToBuyFromLpStore: [" + itemNum + "][" + _itemToBuy.Name + "][" + _itemToBuy.TypeId + "][" + _itemToBuy.Quantity + "]");
                            ListOfItemsToBuyFromLpStore.Add(_itemToBuy);
                        }
                    }

                    //
                    // Items to buy on market and bring to home station (if needed)
                    //
                    ListOfItemsToKeepInStock = new List<InventoryItem>();
                    XElement xmlStuffToKeepInStock = CharacterSettingsXml.Element("itemsToKeepInStock") ?? CommonSettingsXml.Element("itemsToKeepInStock");

                    if (xmlStuffToKeepInStock != null)
                    {
                        int itemNum = 0;
                        foreach (XElement item in xmlStuffToKeepInStock.Elements("itemToKeepInStock"))
                        {
                            itemNum++;
                            InventoryItem _itemToHaul = new InventoryItem(item);
                            Log.WriteLine("ListOfItemsToKeepInStock: [" + itemNum + "][" + _itemToHaul.Name + "][" + _itemToHaul.TypeId + "][" + _itemToHaul.Quantity + "]");
                            ListOfItemsToKeepInStock.Add(_itemToHaul);
                        }
                    }

                    //
                    // Items to bring from home station to market station (if available)
                    //
                    ListOfItemsToTakeToMarket = new List<InventoryItem>();
                    XElement xmlStuffToTakeTomarket = CharacterSettingsXml.Element("itemsToTakeToMarket") ?? CommonSettingsXml.Element("itemsToTakeToMarket");

                    if (xmlStuffToTakeTomarket != null)
                    {
                        int itemNum = 0;
                        foreach (XElement item in xmlStuffToTakeTomarket.Elements("itemToTakeToMarket"))
                        {
                            itemNum++;
                            InventoryItem _itemToHaul = new InventoryItem(item);
                            Log.WriteLine("ListOfItemsToTakeToMarket: [" + itemNum + "][" + _itemToHaul.Name + "][" + _itemToHaul.TypeId + "]");
                            ListOfItemsToTakeToMarket.Add(_itemToHaul);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Settings [" + exception + "]");
                }

                //
                // Enable / Disable the different types of logging that are available
                //
                Statistics.DroneStatsLog = (bool?) CharacterSettingsXml.Element("DroneStatsLog") ?? (bool?) CommonSettingsXml.Element("DroneStatsLog") ?? true;
                Statistics.WreckLootStatistics = (bool?) CharacterSettingsXml.Element("WreckLootStatistics") ??
                                                 (bool?) CommonSettingsXml.Element("WreckLootStatistics") ?? true;
                Statistics.MissionDungeonIdLog = (bool?) CharacterSettingsXml.Element("MissionDungeonIdLog") ??
                                                 (bool?) CommonSettingsXml.Element("MissionDungeonIdLog") ?? true;
                Statistics.PocketStatistics = (bool?) CharacterSettingsXml.Element("PocketStatistics") ??
                                              (bool?) CommonSettingsXml.Element("PocketStatistics") ?? true;
                Statistics.PocketStatsUseIndividualFilesPerPocket = (bool?) CharacterSettingsXml.Element("PocketStatsUseIndividualFilesPerPocket") ??
                                                                    (bool?) CommonSettingsXml.Element("PocketStatsUseIndividualFilesPerPocket") ?? true;
                Statistics.PocketObjectStatisticsLog = (bool?) CharacterSettingsXml.Element("PocketObjectStatisticsLog") ??
                                                       (bool?) CommonSettingsXml.Element("PocketObjectStatisticsLog") ?? true;
                Statistics.WindowStatsLog = (bool?) CharacterSettingsXml.Element("WindowStatsLog") ??
                                            (bool?) CommonSettingsXml.Element("WindowStatsLog") ?? true;
                Statistics.IskPerLP = (double?) CharacterSettingsXml.Element("IskPerLP") ?? (double?) CommonSettingsXml.Element("IskPerLP") ?? 500;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }

            Log.WriteLine("Settings: IsOmegaClone [" + ESCache.Instance.DirectEve.Me.IsOmegaClone + "]");
        }

        public bool ReturnBoolSetting(bool? CharacterXMLBool, bool? CommonXMLBool, bool GuiSettingBool)
        {
            if (CharacterXMLBool != null)
                return (bool) CharacterXMLBool;

            if (CommonXMLBool != null)
                return (bool) CommonXMLBool;

            return GuiSettingBool;
        }

        private static string CurrentDayOfTheWeek()
        {
            try
            {
                int day = (int) DateTime.Now.DayOfWeek == 0 ? 7 : (int) DateTime.Now.DayOfWeek;
                switch (day)
                {
                    case 1: //Monday
                        return "monday";

                    case 2: //Tuesday
                        return "tuesday";

                    case 3: //Wednesday
                        return "wednesday";

                    case 4: //Thursday
                        return "thursday";

                    case 5: //Friday
                        return "friday";

                    case 6: //Saturday
                        return "saturday";

                    case 7: //Sunday
                        return "sunday";
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [ " + ex + " ]");
                return string.Empty;
            }
        }

        #endregion Methods
    }
}