/*
/*
 * ---------------------------------------
 * User: duketwo
 * Date: 24.12.2013
 * Time: 16:26
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using EasyHook;
using ServiceStack;
using ServiceStack.Text;
using SharedComponents.CurlUtil;
using SharedComponents.Events;
using SharedComponents.EVE.ClientSettings;
using SharedComponents.Extensions;
using SharedComponents.IPC;
using SharedComponents.Utility;
using SharedComponents.WinApiUtil;
using SharedComponents.SeleniumDriverHandler;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using OpenQA.Selenium.Interactions;
using SharedComponents.SharpLogLite.Model;
using SharedComponents.IPC.TCP;
using System.Threading.Tasks;
using Microsoft.Win32;
using SharedComponents.EVE.ClientSettings.Abyssal.Main;

namespace SharedComponents.EVE
{
    public class EVESharpInterface : MarshalByRefObject
    {
        public void Ping()
        {
        }
    }

    /// <summary>
    ///     Description of EveAccountData.
    /// </summary>
    [Serializable]
    public class EveAccount : ViewModelBase
    {
        [NonSerialized] private static Random rnd = new Random();

        [NonSerialized] private static DateTime lastEveInstanceKilled = DateTime.MinValue;

        [NonSerialized] private static int waitTimeBetweenEveInstancesKills = rnd.Next(15, 25);

        [NonSerialized] private IDuplexServiceCallback ClientCallback;

        [NonSerialized]
        public static ConcurrentDictionary<int, string> CharnameByPid = new ConcurrentDictionary<int, string>();

        [NonSerialized] private DateTime? _lastResponding = null;

        [NonSerialized] public readonly static double MAX_SERIALIZATION_ERRORS = GetMaxSerializationErrors();

        [NonSerialized] private DriverHandler _driverHandler;

        public EveAccount(string accountName, string characterName, string password, DateTime startTime,
            DateTime endTime, bool isActive)
        {
            AccountName = accountName;
            CharacterName = characterName;
            Password = password;
            Starts = 0;
            LastStartTime = DateTime.MinValue;
            IsActive = isActive;
            Pid = 0;
        }

        public EveAccount()
        {
        }


        public DriverHandler GetDriverHandler(bool headless = false, bool newInstance = false)
        {

            if (newInstance)
                return new DriverHandler(this, headless);

            if (_driverHandler != null && !_driverHandler.IsAlive())
            {
                _driverHandler.Close();
                _driverHandler = null;
            }


            if (_driverHandler == null)
            {
                try
                {
                    _driverHandler = new DriverHandler(this, headless);

                }
                catch (Exception ex)
                {

                    Cache.Instance.Log(ex.StackTrace);
                }
            }


            return _driverHandler;
        }

        private static int GetMaxSerializationErrors()
        {
            var env = Environment.GetEnvironmentVariable("EVESHARP_MAX_SERIALIZATION_ERRORS");
            if (!string.IsNullOrEmpty(env))
            {
                if (int.TryParse(env, out int res))
                {
                    return res;
                }
            }
            return 3 * 4 + 1;
        }

        public void DisposeDriverHandler()
        {
            try
            {
                if (_driverHandler != null)
                {
                    _driverHandler.Dispose();
                }
            }
            catch (Exception ex)
            {
                Cache.Instance.Log(ex.ToString());
            }
        }

        // --------- Pattern Manager Setttings Start

        [Browsable(false)]
        public bool PatternManagerEnabled
        {
            get { return GetValue(() => PatternManagerEnabled); }
            set { SetValue(() => PatternManagerEnabled, value); }
        }

        [Browsable(false)]
        public Guid? StartOnVirtualDesktopId { get; set; }

        [Browsable(false)]
        public int PatternManagerHoursPerWeek
        {
            get { return GetValue(() => PatternManagerHoursPerWeek); }
            set { SetValue(() => PatternManagerHoursPerWeek, value); }
        }

        [Browsable(false)]
        public int CurrentShipTypeId
        {
            get { return GetValue(() => CurrentShipTypeId); }
            set { SetValue(() => CurrentShipTypeId, value); }
        }

        [Browsable(false)]
        public int PatternManagerDaysOffPerWeek
        {
            get { return GetValue(() => PatternManagerDaysOffPerWeek); }
            set { SetValue(() => PatternManagerDaysOffPerWeek, value); }
        }

        [Browsable(false)]
        public List<int> PatternManagerExcludedHours
        {
            get { return GetValue(() => PatternManagerExcludedHours); }
            set { SetValue(() => PatternManagerExcludedHours, value); }
        }

        [Browsable(false)]
        public DateTime PatternManagerLastUpdate
        {
            get { return GetValue(() => PatternManagerLastUpdate); }
            set { SetValue(() => PatternManagerLastUpdate, value); }
        }

        // --------- Pattern Manager Setttings End


        public string AccountName
        {
            get { return GetValue(() => AccountName); }
            set
            {
                if (Cache.IsServer && EveProcessExists())
                {
                    Cache.Instance.Log("Can't change the account name until the client has been stopped.");
                    return;
                }

                if (Cache.IsServer && Cache.Instance.GetXMLFilesLoaded)
                {
                    DeleteCurlCookie();
                }

                SetValue(() => AccountName, value);
                if (Cache.IsServer && Cache.Instance.GetXMLFilesLoaded)
                {
                    UniqueID = String.Empty;
                    ClearTokens();
                }
            }
        }

        public string CharacterName
        {
            get { return GetValue(() => CharacterName); }
            set
            {
                if (Cache.IsServer && EveProcessExists())
                {
                    Cache.Instance.Log("Can't change the character name until the client has been stopped.");
                    return;
                }

                SetValue(() => CharacterName, value);
                if (Cache.IsServer && Cache.Instance.GetXMLFilesLoaded)
                {
                    UniqueID = String.Empty;
                    ClearTokens();
                }
            }
        }

        [Browsable(false)]
        public string Salt
        {
            get { return GetValue(() => Salt); }
            set { SetValue(() => Salt, value); }
        }

        [Browsable(false)]
        public bool LoggedIn
        {
            get { return GetValue(() => LoggedIn); }
            set { SetValue(() => LoggedIn, value); }
        }

        [Browsable(false)]
        public string UniqueID
        {
            get
            {
                var val = GetValue(() => UniqueID);
                if (string.IsNullOrEmpty(val))
                {

                    if (string.IsNullOrEmpty(Salt))
                    {
                        Salt = Path.GetRandomFileName().Replace(".", "");
                    }

                    val = Util.Sha256(Util.Sha256(CharacterName + AccountName) + Salt);
                    SetValue(() => UniqueID, val);
                }

                return val;
            }
            set { SetValue(() => UniqueID, value); }
        }

        public string Password
        {
            get { return GetValue(() => Password); }
            set
            {
                if (Cache.IsServer && EveProcessExists())
                {
                    Cache.Instance.Log("Can't change the password until the client has been stopped.");
                    return;
                }

                SetValue(() => Password, value);
                if (Cache.IsServer && Cache.Instance.GetXMLFilesLoaded)
                {
                    ClearTokens();
                }
            }
        }

        public string Info
        {
            get { return GetValue(() => Info); }
            set { SetValue(() => Info, value); }
        }

        public string Email
        {
            get { return GetValue(() => Email); }
            set { SetValue(() => Email, value); }
        }

        public string EmailPassword
        {
            get { return GetValue(() => EmailPassword); }
            set { SetValue(() => EmailPassword, value); }
        }

        public string Pattern
        {
            get { return GetValue(() => Pattern); }
            set
            {
                try
                {
                    var res = PatternEval.GenerateOutput(value);
                    FilledPattern = res;
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e);
                }

                SetValue(() => Pattern, value);
            }
        }

        [Browsable(false)]
        public DateTime LastPatternTimeUpdate
        {
            get { return GetValue(() => LastPatternTimeUpdate); }
            set { SetValue(() => LastPatternTimeUpdate, value); }
        }

        [Browsable(false)]
        public string FilledPattern
        {
            get
            {
                var ret = GetValue(() => FilledPattern);
                if (Cache.IsServer && SelectedGroup != "0" && string.IsNullOrEmpty(ret))
                {
                    var ea = Cache.Instance.EveAccountSerializeableSortableBindingList.List
                        .Where(e => e.SelectedGroup == SelectedGroup)
                        .FirstOrDefault(e => !string.IsNullOrEmpty(e.FilledPattern));

                    if (ea != null)
                    {
                        return ea.FilledPattern;
                    }
                }

                return ret;
            }
            set { SetValue(() => FilledPattern, value); }
        }

        [Browsable(false)]
        public DateTime SkillQueueEnd
        {
            get { return GetValue(() => SkillQueueEnd); }
            set { SetValue(() => SkillQueueEnd, value); }
        }

        [Browsable(false)]
        public string IMAPHost
        {
            get { return GetValue(() => IMAPHost); }
            set { SetValue(() => IMAPHost, value); }
        }

        public int Starts
        {
            get { return GetValue(() => Starts); }
            set { SetValue(() => Starts, value); }
        }

        [ReadOnly(true)]
        public bool SkillInTrain
        {
            get { return GetValue(() => SkillInTrain); }
            set { SetValue(() => SkillInTrain, value); }
        }

        //[Browsable(false)]
        public bool IsInAbyss
        {
            get { return GetValue(() => IsInAbyss); }
            set
            {
                if (IsInAbyss && !value)
                {
                    AbyssExitValidUntil = DateTime.UtcNow.AddSeconds(new Random().Next(90, 105)); // 60s invuln + 20 sec 0.5 concorde
                }
                SetValue(() => IsInAbyss, value);
            }
        }

        [Browsable(false)]
        public DateTime LastAbyssEntered
        {
            get { return GetValue(() => LastAbyssEntered); }
            set { SetValue(() => LastAbyssEntered, value); }
        }

        [Browsable(false)]
        public DateTime AbyssExitValidUntil
        {
            get { return GetValue(() => AbyssExitValidUntil); }
            set { SetValue(() => AbyssExitValidUntil, value); }
        }

        [Browsable(false)]
        public DateTime LastStartTime
        {
            get { return GetValue(() => LastStartTime); }
            set { SetValue(() => LastStartTime, value); }
        }

        [Browsable(false)]
        public DateTime LastSessionReady
        {
            get { return GetValue(() => LastSessionReady); }
            set { SetValue(() => LastSessionReady, value); }
        }

        public bool IsActive
        {
            get { return GetValue(() => IsActive); }
            set { SetValue(() => IsActive, value); }
        }

        public bool Force
        {
            get { return GetValue(() => Force); }
            set { SetValue(() => Force, value); }
        }

        [Browsable(false)]
        public DateTime AbyssSecondsDailyLastReset
        {
            get { return GetValue(() => AbyssSecondsDailyLastReset); }
            set { SetValue(() => AbyssSecondsDailyLastReset, value); }
        }

        [Browsable(false)]
        public int AbyssSecondsDaily
        {
            get
            {
                if (AbyssSecondsDailyLastReset.Day != DateTime.Now.Day || AbyssSecondsDailyLastReset.Month != DateTime.Now.Month || AbyssSecondsDailyLastReset.Year != DateTime.Now.Year)
                {
                    AbyssSecondsDaily = 0;
                    AbyssSecondsDailyLastReset = DateTime.UtcNow;
                    return 0;
                }
                return GetValue(() => AbyssSecondsDaily);
            }
            set
            {
                SetValue(() => AbyssSecondsDaily, value);
            }
        }

        public bool Hidden
        {
            get { return GetValue(() => Hidden); }
            set
            {
                SetValue(() => Hidden, value);
                if (value) HideWindows();
                else ShowWindows();
            }
        }

        public bool Console
        {
            get { return GetValue(() => Console); }
            set
            {
                SetValue(() => Console, value);
                if (value) ShowConsoleWindow();
                else HideConsoleWindow();
            }
        }

        [Browsable(false)]
        public bool DX11
        {
            get { return GetValue(() => DX11); }
            set { SetValue(() => DX11, value); }
        }

        [Browsable(false)]
        public DateTime LastAmmoBuy
        {
            get { return GetValue(() => LastAmmoBuy); }
            set { SetValue(() => LastAmmoBuy, value); }
        }

        [Browsable(false)]
        public int DumpLootIterations
        {
            get { return GetValue(() => DumpLootIterations); }
            set { SetValue(() => DumpLootIterations, value); }
        }

        [Browsable(false)]
        public DateTime DumpLootTimestamp
        {
            get { return GetValue(() => DumpLootTimestamp); }
            set { SetValue(() => DumpLootTimestamp, value); }
        }

        [Browsable(false)]
        [ReadOnly(true)]
        public bool IsDocked
        {
            get { return GetValue(() => IsDocked); }
            set { SetValue(() => IsDocked, value); }
        }

        [Browsable(false)]
        [ReadOnly(true)]
        public bool IsDockedInHomeStation
        {
            get { return GetValue(() => IsDockedInHomeStation); }
            set { SetValue(() => IsDockedInHomeStation, value); }
        }

        [Browsable(false)]
        public int Pid
        {
            get { return GetValue(() => Pid); }
            set { SetValue(() => Pid, value); }
        }

        [Browsable(false)]
        public int AbyssStage
        {
            get { return GetValue(() => AbyssStage); }
            set { SetValue(() => AbyssStage, value); }
        }

        [Browsable(false)]
        public HWSettings HWSettings
        {
            get { return GetValue(() => HWSettings); }
            set { SetValue(() => HWSettings, value); }
        }

        [Browsable(false)]
        public DateTime NextCacheDeletion
        {
            get { return GetValue(() => NextCacheDeletion); }
            set { SetValue(() => NextCacheDeletion, value); }
        }

        [Browsable(false)]
        public DateTime LastPlexBuy
        {
            get { return GetValue(() => LastPlexBuy); }
            set { SetValue(() => LastPlexBuy, value); }
        }

        [Browsable(false)]
        public Int64 EVESharpCoreFormHWnd
        {
            get { return GetValue(() => EVESharpCoreFormHWnd); }
            set { SetValue(() => EVESharpCoreFormHWnd, value); }
        }

        [Browsable(false)]
        public Int64 HookmanagerHWnd
        {
            get { return GetValue(() => HookmanagerHWnd); }
            set { SetValue(() => HookmanagerHWnd, value); }
        }

        [Browsable(false)]
        public Int64 EveHWnd
        {
            get { return GetValue(() => EveHWnd); }
            set { SetValue(() => EveHWnd, value); }
        }

        //[Browsable(false)]
        //public bool LastServerSISI
        //{
        //    get { return GetValue(() => LastServerSISI); }
        //    set { SetValue(() => LastServerSISI, value); }
        //}

        [Browsable(false)]
        public String EveRefreshTokenSISI
        {
            get
            {
                return GetValue(() => EveRefreshTokenSISI);
            }
            set
            {
                SetValue(() => EveRefreshTokenSISI, value);
            }
        }

        [Browsable(false)]
        public String EveRefreshToken
        {
            get
            {
                if (SISI)
                    return EveRefreshTokenSISI;
                return GetValue(() => EveRefreshToken);
            }
            set
            {
                if (SISI)
                    EveRefreshTokenSISI = value;
                else
                    SetValue(() => EveRefreshToken, value);
            }
        }

        [Browsable(false)]
        public DateTime EveAccessTokenValidTillSISI
        {
            get
            {
                return GetValue(() => EveAccessTokenValidTillSISI);
            }
            set
            {
                SetValue(() => EveAccessTokenValidTillSISI, value);
            }
        }

        [Browsable(false)]
        public DateTime EveAccessTokenValidTill
        {
            get
            {
                if (SISI)
                    return EveAccessTokenValidTillSISI;
                return GetValue(() => EveAccessTokenValidTill);
            }
            set
            {
                if (SISI)
                    EveAccessTokenValidTillSISI = value;
                else
                    SetValue(() => EveAccessTokenValidTill, value);
            }
        }

        [Browsable(false)]
        public String EveAccessTokenSISI
        {
            get { return GetValue(() => EveAccessTokenSISI); }
            set { SetValue(() => EveAccessTokenSISI, value); }
        }


        [Browsable(false)]
        public String EveAccessToken
        {
            get
            {
                if (SISI)
                    return EveAccessTokenSISI;
                return GetValue(() => EveAccessToken);
            }
            set
            {
                if (SISI)
                    EveAccessTokenSISI = value;
                else
                    SetValue(() => EveAccessToken, value);
            }
        }

        [Browsable(false)]
        public DateTime StartingTokenTime
        {
            get { return GetValue(() => StartingTokenTime); }
            set { SetValue(() => StartingTokenTime, value); }
        }

        [XmlIgnore]
        [Browsable(false)]
        public TimeSpan StartingTokenTimespan
        {
            get { return GetValue(() => StartingTokenTimespan); }
            set { SetValue(() => StartingTokenTimespan, value); }
        }

        [XmlIgnore]
        [Browsable(false)]
        public double ClientSettingsSerializationErrors => StartingTokenTimespan.TotalHours;

        [Browsable(false)]
        [XmlElement("StartingTokenTimespan")]
        public long StartingTokenTimespanWrapper
        {
            get => StartingTokenTimespan.Ticks;
            set => StartingTokenTimespan = TimeSpan.FromTicks(value);
        }

        [Browsable(false)]
        public List<String> CharsOnAccount
        {
            get { return GetValue(() => CharsOnAccount); }
            set { SetValue(() => CharsOnAccount, value); }
        }

        [Browsable(false)]
        public String CurrentFit
        {
            get { return GetValue(() => CurrentFit); }
            set { SetValue(() => CurrentFit, value); }
        }

        [Browsable(false)]
        public bool SISI => CS?.GlobalMainSetting?.SISI ?? false;

        private DateTime _lastExceptionCheck;
        private int _lastExceptionCheckValue;

        public void ResetExceptions()
        {
            SetValue(() => AmountExceptionsCurrentSession, 0);
            _lastExceptionCheckValue = 0;
        }

        [Browsable(false)]
        public int AmountExceptionsCurrentSession
        {
            get
            {
                var val = GetValue(() => AmountExceptionsCurrentSession);
                return val;
            }
            set
            {
                var interval = 30;
                var limit = 70;


                //Cache.Instance.Log($"[{_lastExceptionCheckValue}] AmountExceptionsCurrentSession [{AmountExceptionsCurrentSession}]");
                if (_lastExceptionCheck.AddSeconds(interval) < DateTime.UtcNow)
                {
                    _lastExceptionCheck = DateTime.UtcNow;
                    var lim = limit;
                    lim = this.IsInAbyss ? limit * 3 : limit;
                    if (Math.Abs(_lastExceptionCheckValue - AmountExceptionsCurrentSession) > lim && !SelectedController.Equals("None"))
                    {
                        Cache.Instance.Log($"Received more than [{lim}] exceptions the past [{interval}] seconds.");
                        Cache.Instance.Log($"Disabling this instance.");
                        this.KillEveProcess(true);
                        this.IsActive = false;
                    }

                    _lastExceptionCheckValue = AmountExceptionsCurrentSession;
                }

                SetValue(() => AmountExceptionsCurrentSession, value);
            }
        }

        [Browsable(false)]
        public ClientSetting ClientSetting
        {
            get
            {
                var ret = GetValue(() => ClientSetting);
                if (ret == null)
                {
                    ClientSetting = new ClientSetting();
                    return ClientSetting;
                }
                return ret;

            }
            set { SetValue(() => ClientSetting, value); }
        }

        [Browsable(false)] public ClientSetting CS => ClientSetting;

        [Browsable(false)]
        public string SelectedController
        {
            get
            {
                var ret = GetValue(() => SelectedController);
                if (string.IsNullOrEmpty(ret))
                {
                    SelectedController = DefaultController;
                    return DefaultController;
                }

                return ret;
            }
            set { SetValue(() => SelectedController, value); }
        }

        [Browsable(false)]
        public string SelectedGroup
        {
            get
            {
                var ret = GetValue(() => SelectedGroup);
                if (string.IsNullOrEmpty(ret))
                {
                    SelectedGroup = "0";
                    return SelectedGroup;
                }

                return ret;
            }
            set { SetValue(() => SelectedGroup, value); }
        }

        [XmlIgnore]
        [Browsable(false)]
        private DateTime RunAbyssalGuardSlaveAtLeastUntil
        {
            get { return GetValue(() => RunAbyssalGuardSlaveAtLeastUntil); }
            set { SetValue(() => RunAbyssalGuardSlaveAtLeastUntil, value); }
        }

        [Browsable(false)]
        public bool ShouldBeRunning
        {
            get
            {

                if (IsInAbyss)
                {
                    return true;
                }
                // 670 = capsule
                if (SelectedController.Equals("AbyssalController") && CurrentShipTypeId != AbyssalMainSetting.GetShipTypeId(this.ClientSetting.AbyssalMainSetting.ShipType) && CurrentShipTypeId != 670)
                {
                    return true;
                }

                if (!IsDocked && AbyssExitValidUntil > DateTime.UtcNow)
                    return true;

                if (!IsActive)
                    return false;

                if (Force)
                    return true;

                if (this.SelectedController.Equals("AbyssalGuardController") && !string.IsNullOrEmpty(CS?.AbyssalGuardMainSetting?.AbyssCharacterName))
                {

                    if (RunAbyssalGuardSlaveAtLeastUntil > DateTime.UtcNow)
                    {
                        return true;
                    }

                    var abyssRunnerChar = Cache.Instance.EveAccountSerializeableSortableBindingList.List.FirstOrDefault(e => e.CharacterName.Equals(CS?.AbyssalGuardMainSetting?.AbyssCharacterName));
                    if (abyssRunnerChar != null && abyssRunnerChar.EveProcessExists())
                    {
                        RunAbyssalGuardSlaveAtLeastUntil = DateTime.UtcNow.AddMinutes(rnd.Next(15, 25));
                        return true;
                    }

                    try
                    {

                        var task = Task.Run(() =>
                        {
                            var isRunningOnARemoteInstance = WCFClientTCP.Instance.GetPipeProxy.IsEveInstanceRunning(CS.AbyssalGuardMainSetting.AbyssCharacterName);
                            return isRunningOnARemoteInstance;
                        });

                        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(4));
                        var completedTask = Task.WhenAny(task, timeoutTask).GetAwaiter().GetResult();

                        if (completedTask == task)
                        {
                            bool result = task.GetAwaiter().GetResult();
                            if (result)
                            {
                                RunAbyssalGuardSlaveAtLeastUntil = DateTime.UtcNow.AddMinutes(rnd.Next(15, 25));
                                return true;
                            }
                        }
                        else
                        {
                            // Handle the timeout case here
                            Debug.WriteLine("Timeout occurred.");
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }

                try
                {
                    return IsDocked
                        ? PatternEval.IsAnyPatternMatchingDatetime(this.FilledPattern, DateTime.UtcNow)
                        : PatternEval.IsAnyPatternMatchingDatetime(this.FilledPattern, DateTime.UtcNow, 60);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }

                return false;
            }
        }

        [Browsable(false)]
        public double RamUsage
        {
            get
            {
                if (EveProcessExists())
                {
                    var p = Process.GetProcessById(Pid);
                    return p.WorkingSet64 / 1024 / 1024;
                }

                return 0;
            }
        }

        public IDuplexServiceCallback GetClientCallback()
        {
            return ClientCallback;
        }

        public void SetClientCallback(IDuplexServiceCallback s)
        {
            ClientCallback = s;
        }


        public void ClearTokens()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AccountName))
                    return;

                if (Cache.Instance == null)
                    return;
                Cache.Instance.Log($"Clearing tokens for accountName [{AccountName}]");
            }
            catch (Exception)
            {
            }

            EveAccessTokenValidTill = DateTime.MinValue;
            EveAccessToken = String.Empty;
            EveRefreshToken = String.Empty;
        }


        public void GenerateNewTimeSpan()
        {
            try
            {
                Pattern = Pattern;
            }
            catch (Exception e)
            {
                Cache.Instance.Log("Exception " + e.StackTrace);
            }
        }


        public bool KillEveProcess(bool force = false)
        {
            if (lastEveInstanceKilled.AddSeconds(waitTimeBetweenEveInstancesKills) < DateTime.UtcNow || force)
            {
                lastEveInstanceKilled = DateTime.UtcNow;
                waitTimeBetweenEveInstancesKills = rnd.Next(17, 35);
                if (EveProcessExists())
                {
                    try
                    {
                        var processStartInfo = new ProcessStartInfo("taskkill", "/f /t /pid " + Pid)
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        Process.Start(processStartInfo);
                        Info = string.Empty;
                        Cache.Instance.Log(String.Format(
                            $"Stopping Eve process used by character {CharacterName} on account {AccountName} with pid {Pid}"));
                    }
                    catch
                    {
                        Cache.Instance.Log("Exception: Couldn't execute taskkill.");
                    }

                    return true;
                }
            }

            return false;
        }


        public bool EveProcessExists()
        {
            var ps = Process.GetProcesses();
            return Pid != -1 && Pid != 0 && ps.Any(x => x.Id == Pid) &&
                   ps.FirstOrDefault(x => x.Id == Pid).ProcessName.ToLower().Contains("exefile");
        }


        public bool IsProcessAlive()
        {
            var p = Process.GetProcesses().FirstOrDefault(x => x.Id == Pid);
            if (p != null)
            {
                return IsProcessResponding(p) && CheckEvents() && LastSessionReady.AddSeconds(120) > DateTime.UtcNow;
            }

            return false;
        }

        [Browsable(false)]
        public bool IsNoneController => this.SelectedController.Equals("None");

        public bool CheckEvents()
        {
            foreach (DirectEvents directEvent in Enum.GetValues(typeof(DirectEvents)))
            {
                var value = (int)directEvent;

                if (value < 0)
                    continue;

                if (!IsNoneController && HasLastDirectEvent(directEvent) && GetLastDirectEvent(directEvent).Value <
                    DateTime.UtcNow.AddMinutes(-Math.Abs(value)))
                {
                    Cache.Instance.Log(String.Format(
                        "Stopping account [{0}] because DirectEvent [{1}] hasn't been received within [{2}] minutes.",
                        AccountName,
                        directEvent, value));
                    return false;
                }
            }

            return true;
        }

        public bool IsProcessResponding(Process p)
        {
            if (p != null)
            {
                if (_lastResponding == null)
                    _lastResponding = DateTime.UtcNow;

                if (p.Responding)
                {
                    _lastResponding = DateTime.UtcNow;
                    return true;
                }
                else
                {
                    if (IsInAbyss)
                        return _lastResponding.Value.AddSeconds(8) > DateTime.UtcNow;

                    return _lastResponding.Value.AddSeconds(35) > DateTime.UtcNow;
                }
            }

            return false;
        }


        public void StartExecuteable(string filename, string parameters = "")
        {
            var args = new string[] { CharacterName, WCFServer.Instance.GetPipeName };
            var processId = -1;
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var injectionFile = Path.Combine(path, "DomainHandler.dll");
            String ChannelName = null;
            RemoteHooking.IpcCreateServer<EVESharpInterface>(ref ChannelName, WellKnownObjectMode.SingleCall);


            if (!String.IsNullOrEmpty(filename) && File.Exists(filename))
            {
                RemoteHooking.CreateAndInject(filename, parameters, (int)InjectionOptions.Default, injectionFile,
                    injectionFile, out processId, ChannelName,
                    args);
                return;
            }

            var openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "exe files | *.exe";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                RemoteHooking.CreateAndInject(openFileDialog.FileName, parameters, (int)InjectionOptions.Default,
                    injectionFile, injectionFile, out processId,
                    ChannelName, args);
        }

        public void HideWindows()
        {
            if (WinApiUtil.WinApiUtil.IsValidHWnd((IntPtr)EveHWnd))
            {
                WinApiUtil.WinApiUtil.RemoveFromTaskbar((IntPtr)EveHWnd);
                WinApiUtil.WinApiUtil.SetWindowsPos((IntPtr)EveHWnd, 0, 0);
            }

            if (EveProcessExists())
                foreach (var w in Util.GetVisibleWindows(Pid))
                {
                    if (w.Key == (IntPtr)EveHWnd)
                    {
                        continue;
                    }

                    Util.ShowWindow(w.Key, Util.SW_HIDE);
                }
        }

        public void HideConsoleWindow()
        {
            if (EveProcessExists())
                foreach (var w in Util.GetVisibleWindows(Pid))
                    if (w.Value.Contains("[CCP]"))
                        Util.ShowWindow(w.Key, Util.SW_HIDE);
        }

        public void ShowConsoleWindow()
        {
            if (EveProcessExists())
                foreach (var w in Util.GetInvisibleWindows(Pid))
                    if (w.Value.Contains("[CCP]"))
                        Util.ShowWindow(w.Key, Util.SW_SHOWNOACTIVATE);
        }

        public void DeleteCurlCookie()
        {
            if (string.IsNullOrWhiteSpace(AccountName))
                return;

            using (var curlWoker = new CurlWorker(AccountName))
            {
                if (curlWoker.DeleteCurrentSessionCookie(true))
                {
                    Cache.Instance.Log($"Cookies for Account [{AccountName}] deleted.");
                }
            }
        }

        public void ShowWindows()
        {
            if (EveProcessExists())
            {

                if (WinApiUtil.WinApiUtil.IsValidHWnd((IntPtr)EveHWnd))
                {
                    WinApiUtil.WinApiUtil.AddToTaskbar((IntPtr)EveHWnd);
                    WinApiUtil.WinApiUtil.SetWindowsPos((IntPtr)EveHWnd, 0, 0);
                    if (WinApiUtil.WinApiUtil.IsValidHWnd((IntPtr)Cache.Instance.MainFormHWnd))
                    {
                        WinApiUtil.WinApiUtil.SetHWndInsertAfter((IntPtr)EveHWnd,
                            (IntPtr)Cache.Instance.MainFormHWnd);
                        WinApiUtil.WinApiUtil.SetHWndInsertAfter((IntPtr)HookmanagerHWnd,
                            (IntPtr)Cache.Instance.MainFormHWnd);
                        WinApiUtil.WinApiUtil.SetHWndInsertAfter((IntPtr)EVESharpCoreFormHWnd,
                            (IntPtr)Cache.Instance.MainFormHWnd);
                    }

                    Util.ShowWindow((IntPtr)EveHWnd, Util.SW_SHOWNOACTIVATE);
                }

                var list = Util.GetInvisibleWindows(Pid);
                foreach (var w in list)
                {
                    if ((Int64)w.Key == HookmanagerHWnd
                        || (Int64)w.Key == EVESharpCoreFormHWnd
                        || w.Value.Contains("exefile.exe")
                        || (w.Value.Contains("[CCP]") && Console))
                        Util.ShowWindow(w.Key, Util.SW_SHOWNOACTIVATE);
                }

                if (WinApiUtil.WinApiUtil.IsValidHWnd((IntPtr)EveHWnd))
                {
                    WinApiUtil.WinApiUtil.SetWindowsPos((IntPtr)EveHWnd, 0, 0);
                    Util.ShowWindow((IntPtr)EveHWnd, Util.SW_SHOWNOACTIVATE);
                    WinApiUtil.WinApiUtil.ForcePaint((IntPtr)EveHWnd);
                    WinApiUtil.WinApiUtil.ForceRedraw((IntPtr)EveHWnd);
                }
            }
        }

        public static string DefaultController => AvailableControllers[0];

        public static List<string> AvailableControllers = new List<string>()
            {"QuestorController", "PinataController", "None", "AbyssalController", "AbyssalGuardController", "AbyssalHydraController", "AbyssalHydraSlaveController", };

        public DateTime? GetLastDirectEvent(DirectEvents directEvent)
        {
            return DirectEventHandler.GetLastEventReceived(CharacterName, directEvent);
        }

        public bool HasLastDirectEvent(DirectEvents directEvent)
        {
            return DirectEventHandler.GetLastEventReceived(CharacterName, directEvent) != null;
        }


        public void CheckEveVersion()
        {
            WebClient webClient = new WebClient();
            var k = JSON.parse((webClient.DownloadString("https://c4s.de/eveServerStatus")));
            var res = k.ToStringDictionary();

            if (res.ContainsKey("server_version"))
            {

                if (res.ContainsKey("vip") && res["vip"].ToLower().Equals("true"))
                    throw new Exception($"Error: Server is still in VIP-mode.");

                var remoteVersion = res["server_version"].ToInt();
                int localVersion;

                var evePath = GetEvePath(false);
                var eveINIFile = Path.Combine(evePath, "start.ini");
                if (File.Exists(eveINIFile))
                {

                    var iniR = INIReader.Read(File.ReadAllText(eveINIFile));
                    if (iniR.ContainsKey("build"))
                    {
                        localVersion = iniR["build"].ToInt();
                    }
                    else
                    {
                        throw new Exception($"Error: Key ['build'] does not exist in [{eveINIFile}].");
                    }
                }
                else
                {
                    throw new Exception($"Error: File [{eveINIFile}] does not exist.");
                }

                Cache.Instance.Log($"Remote version: [{remoteVersion}] Local version: [{localVersion}]");

                if (remoteVersion != localVersion)
                {
                    bool _finished = false;
                    var _lastProgressChanged = DateTime.UtcNow;
                    if (Cache.Instance.EveSettings.AutoUpdateEve)
                    {
                        var tempFileName = Path.GetTempFileName();

                        WebClient client = new WebClient();
                        Uri uri = new Uri($"https://patches.c4s.de/{remoteVersion}_patch.zip");
                        Cache.Instance.Log($"Downloading patch from [{uri}] to [{tempFileName}]");
                        client.DownloadFileCompleted += delegate (object sender, AsyncCompletedEventArgs args)
                        {
                            _finished = true;
                        };
                        client.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs args)
                        {
                            if (_lastProgressChanged < DateTime.UtcNow)
                            {
                                _lastProgressChanged = DateTime.UtcNow.AddSeconds(1);
                                Cache.Instance.Log($"Downloaded [{args.ProgressPercentage}%]");
                            }
                        };
                        client.DownloadFileAsync(uri, tempFileName);

                        while (client.IsBusy || !_finished)
                        {
                            Thread.Sleep(20);
                        }

                        if (!Util.IsZipValid(tempFileName))
                            throw new Exception("Error: Zip archive is corrupted.");
                        var eveTQPath = GetEvePath(false);

                        Cache.Instance.Log($"Extracting archive to [{eveTQPath}]");
                        using (ZipArchive archive = ZipFile.Open(tempFileName, ZipArchiveMode.Read))
                        {
                            Util.ExtractZipToDirectory(archive, eveTQPath, true);
                        }

                        Cache.Instance.Log($"Successfully extracted the patch version [{remoteVersion}].");
                        Cache.Instance.Log($"Deleting temp file[{tempFileName}].");
                        File.Delete(tempFileName);

                        // Eve Resource Downloading
                        var t = Task.Run(() =>
                        {
                            return EveResourceUpdater.DownloadAppResourcesAsync(remoteVersion, "tq", GetEveRootPath(),
                                new Progress<string>(
                                    (h) => { Cache.Instance.Log(h); }));
                        });

                        t.GetAwaiter().GetResult();
                    }
                    else
                    {
                        //throw new Exception("Error: Eve version missmatch!");
                    }
                }
            }
            else

            {
                throw new Exception("JSON error: Server version could not be found.");
            }
        }

        public void CheckFirewallRule()
        {
            var ruleName = SISI ? FirewallRuleHelper.FW_RULE_NAME_SISI : FirewallRuleHelper.FW_RULE_NAME_TQ;
            var proxyList = Cache.Instance.EveSettings.Proxies.Select(p => p.Ip).Distinct().ToList();
            var proxies = String.Join(",", proxyList);
            var maskedList = IPUtil.GenerateIPMask(proxyList);
            string eveLocation = GetEveExePath(SISI);
            var match = FirewallRuleHelper.CheckIfIPBlockingRuleMatch(ruleName, maskedList, eveLocation);
            if (!match)
            {
                Cache.Instance.Log("Updating firewall rule.");
                FirewallRuleHelper.RemoveRule(ruleName);
                FirewallRuleHelper.AddIPBlockingRule(ruleName, eveLocation, maskedList);
                CheckFirewallRule();
            }
            else
            {
                Cache.Instance.Log("Firewall rule exists and matches.");
            }

            // add rules for eve_crashmon.exe on tq and sisi
            var sisiPath = Cache.Instance.EveLocation.Replace("tq", "sisi");
            var tqPath = Cache.Instance.EveLocation;
            var crashmonTQPath = Path.Combine(Path.GetDirectoryName(tqPath), "eve_crashmon.exe");
            var crashmonSisiPath = Path.Combine(Path.GetDirectoryName(sisiPath), "eve_crashmon.exe");

            if (File.Exists(crashmonTQPath))
            {
                if (!FirewallRuleHelper.CheckIfRuleNameExists(crashmonTQPath))
                {
                    Cache.Instance.Log("Updating firewall rule for eve_crashmon.exe on TQ.");
                    FirewallRuleHelper.RemoveRule(crashmonTQPath);
                    FirewallRuleHelper.AddBlockingRule(crashmonTQPath, crashmonTQPath);
                    CheckFirewallRule();
                }
                else
                {
                    Cache.Instance.Log("Firewall rule for eve_crashmon.exe on TQ exists and matches.");
                }
            }

            if (File.Exists(crashmonSisiPath))
            {
                if (!FirewallRuleHelper.CheckIfRuleNameExists(crashmonSisiPath))
                {
                    Cache.Instance.Log("Updating firewall rule for eve_crashmon.exe on SISI.");
                    FirewallRuleHelper.RemoveRule(crashmonSisiPath);
                    FirewallRuleHelper.AddBlockingRule(crashmonSisiPath, crashmonSisiPath);
                    CheckFirewallRule();
                }
                else
                {
                    Cache.Instance.Log("Firewall rule for eve_crashmon.exe on SISI exists and matches.");
                }
            }
        }

        public void StartEveInject()
        {
            using var pLock =
                (ProcessLock)CrossProcessLockFactory.CreateCrossProcessLock(nameof(StartEveInject));
            try
            {

                try
                {
                    var virtId = VirtualDesktopHelper.VirtualDesktopManager.GetWindowDesktopId(Process.GetCurrentProcess()
                        .MainWindowHandle);
                    Cache.Instance.Log($"VirtualDesktopId: {virtId}");
                    this.StartOnVirtualDesktopId = virtId;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }


                var sisi = SISI;
                //if (LastServerSISI != sisi)
                //{
                //    Cache.Instance.Log($"Seems you changed server, clearing cached tokens");
                //    LastServerSISI = sisi;
                //    ClearTokens();
                //}
                LoggedIn = false;
                CheckFirewallRule();

                try
                {
                    // Disable checking for SISI for the moment
                    if (!sisi)
                        CheckEveVersion();
                }
                catch (Exception e)
                {
                    Cache.Instance.Log(e.ToString());
                    return;
                }

                this.DX11 = true;
                if (EveProcessExists())
                {
                    Cache.Instance.Log(String.Format("There is already one eve process running. [{0}]",
                        CharacterName));
                    return;
                }

                if (ClientSettingsSerializationErrors > MAX_SERIALIZATION_ERRORS)
                {
                    Cache.Instance.Log("Client settings serialization error!");
                    return;
                }

                if (ClientSetting == null)
                {
                    Cache.Instance.Log("ClientSetting == null. Error.");
                    return;
                }

                if (string.IsNullOrEmpty(CharacterName))
                {
                    Cache.Instance.Log("A character name is required.");
                    return;
                }

                if (string.IsNullOrEmpty(AccountName))
                {
                    Cache.Instance.Log("An account name is required.");
                    return;
                }

                if (HWSettings == null || HWSettings.Proxy == null || !HWSettings.Proxy.IsValid ||
                    !HWSettings.IsValid())
                {
                    Cache.Instance.Log("HWSettings or HWSettings.Proxy == null / invalid. Error.");
                    return;
                }

                var eveAccessToken = GetEveAccessToken(sisi: sisi);

                if (eveAccessToken == String.Empty)
                {
                    Cache.Instance.Log("EveAccessToken is empty. Error.");
                    Starts++;
                    return;
                }

                if (HWSettings == null ||
                    HWSettings != null && String.IsNullOrWhiteSpace(HWSettings.Computername) ||
                    HWSettings != null && String.IsNullOrWhiteSpace(HWSettings.Computername))
                {
                    Cache.Instance.Log(
                        "Hardware profile usage is now required. Please setup a different hardware profile for each account or use the same profile for accounts you want to link together.");
                    return;
                }

                if (String.IsNullOrEmpty(HWSettings.LauncherMachineHash))
                {
                    Cache.Instance.Log(
                        "LauncherMachineHash missing. You need to open the HWProfile form once to generate a LauncherMachineHash.");
                    return;
                }

                if (string.IsNullOrEmpty(HWSettings.GpuManufacturer) || HWSettings.GpuDriverDate == null ||
                    HWSettings.GpuDriverDate == DateTime.MinValue)
                {
                    Cache.Instance.Log("You need to generate a new GPU profile for this account.");
                    return;
                }

                Util.CheckCreateDirectorys(HWSettings.WindowsUserLogin);

                if (Cache.Instance.EveSettings.AlwaysClearNonEveSharpCCPData)
                {
                    ClearNonEveSharpCCPData();
                }

                var args = new string[] { CharacterName, WCFServer.Instance.GetPipeName };
                Pid = 0;
                var processId = -1;
                EVESharpCoreFormHWnd = -1;
                EveHWnd = -1;
                HookmanagerHWnd = -1;

                var currentAppDataFolder = GetAppDataFolder();
                var eveBasePath = GetEveUnderscorePath(sisi);
                var appDataCCPEveFolder = currentAppDataFolder + "CCP\\EVE\\";
                var eveSettingFolder = currentAppDataFolder + "CCP\\EVE\\" + eveBasePath + "\\";
                var crashDumpFolder = currentAppDataFolder + "CrashDumps";
                var defaultSettingsFolder = GetDefaultSettingsFolder(); // default settings folder to copy
                var eveCacheFolder = eveSettingFolder + "cache\\";
                var questorConsoleLogFolder = Path.Combine(Util.AssemblyPath, "Logs", CharacterName, "Console");
                var sharpLogLiteFolder = Path.Combine(Util.AssemblyPath, "Logs", CharacterName, "SharpLogLite");
                var logFolder = Path.Combine(Util.AssemblyPath, "Logs", CharacterName);

                try
                {
                    if (Directory.Exists(logFolder))
                        foreach (var file in Directory.GetFiles(logFolder, "*.log")
                                     .Where(item => item.EndsWith(".log")))
                        {
                            if (File.Exists(file))
                            {
                                var fileSize = new FileInfo(file).Length;
                                //Cache.Instance.Log($"File {file} Size {fileSize}");
                                if (fileSize > 1024 * 1024 * 10)
                                    File.Delete(file);
                            }
                        }

                    if (Directory.Exists(questorConsoleLogFolder))
                        foreach (var file in Directory.GetFiles(questorConsoleLogFolder, "*.log")
                                     .Where(item => item.EndsWith(".log")))
                        {
                            var fileLastwrite = File.GetLastWriteTimeUtc(file);
                            if (fileLastwrite.AddDays(15) < DateTime.UtcNow)
                                File.Delete(file);
                        }

                    if (Directory.Exists(sharpLogLiteFolder))
                        foreach (var file in Directory.GetFiles(sharpLogLiteFolder, "*.log")
                                     .Where(item => item.EndsWith(".log")))
                        {
                            var fileLastwrite = File.GetLastWriteTimeUtc(file);
                            var fileSize = new FileInfo(file).Length;
                            if (fileLastwrite.AddDays(90) < DateTime.UtcNow || fileSize > 1024 * 1024)
                                File.Delete(file);
                        }

                    if (Directory.Exists(crashDumpFolder))
                        foreach (var file in Directory.GetFiles(crashDumpFolder, "*.dmp")
                                     .Where(item => item.EndsWith(".dmp")))
                            File.Delete(file);

                    if (Directory.Exists(crashDumpFolder))
                        foreach (var file in Directory.GetFiles(appDataCCPEveFolder, "*.crs")
                                     .Where(item => item.EndsWith(".crs")))
                            File.Delete(file);

                    foreach (var file in Directory.GetFiles(appDataCCPEveFolder, "*.dmp")
                                 .Where(item => item.EndsWith(".dmp")))
                        File.Delete(file);


                    if (Directory.Exists(eveSettingFolder))
                    {
                        Cache.Instance.Log($"Eve settings directory: {eveCacheFolder}");
                    }
                    else
                    {
                        Cache.Instance.Log($"Eve settings does not exist!");
                    }

                    if (Directory.Exists(eveCacheFolder))
                    {
                        Cache.Instance.Log($"Cache directory: {eveCacheFolder}");
                    }
                    else
                    {
                        Cache.Instance.Log($"Cache directory does not exist!");
                    }

                    if (Directory.Exists(eveCacheFolder) && NextCacheDeletion < DateTime.UtcNow)
                    {
                        NextCacheDeletion = DateTime.UtcNow.AddDays(Util.GetRandom(15, 45));
                        Cache.Instance.Log("Clearing EVE cache folder: " + eveCacheFolder +
                                           " Next cache deletion will be on: " + NextCacheDeletion.ToString());
                        Directory.Delete(eveCacheFolder, true);

                        if (Directory.Exists(GetPersonalFolder() + "Documents\\EVE\\logs\\"))
                            Directory.Delete(GetPersonalFolder() + "Documents\\EVE\\logs\\", true);
                    }
                }
                catch (Exception e)
                {
                    Cache.Instance.Log($"Couldn't clear cache, crs files, dmp files: {e}");
                }

                if (!Directory.Exists(eveSettingFolder))
                {
                    Util.DirectoryCopy(defaultSettingsFolder, eveSettingFolder, true);
                    Cache.Instance.Log("EveSettingsFolder doesn't exist. Copying default settings");
                }
                else
                {
                    Cache.Instance.Log("EveSettingsFolder already exists. Not copying default settings");
                }

                if (!File.Exists(GetEveExePath(sisi)))
                {
                    Cache.Instance.Log("Exefile.exe does not exist?");
                    return;
                }

                if (!GetEveExePath(sisi).ToLower().EndsWith("exefile.exe"))
                {
                    Cache.Instance.Log("Exefile.exe filename not properly set? It has to end with exefile.exe");
                    return;
                }

                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var injectionFile = Path.Combine(path, "DomainHandler.dll");
                String ChannelName = null;
                RemoteHooking.IpcCreateServer<EVESharpInterface>(ref ChannelName, WellKnownObjectMode.SingleCall);

                var startParams = GetEveStartParameter(eveAccessToken, sisi);

                if (startParams.Length == 0)
                {
                    Cache.Instance.Log("Error: startParams.Length == 0.");
                    return;
                }

                RemoteHooking.CreateAndInject(GetEveExePath(sisi), startParams, (int)InjectionOptions.Default,
                    injectionFile,
                    injectionFile, out processId, ChannelName, args);

                Starts++;
                LastSessionReady = DateTime.UtcNow;

                if (processId != -1 && processId != 0)
                {
                    ResetExceptions();
                    Pid = processId;
                    LastStartTime = DateTime.UtcNow;
                    Info = string.Empty;
                    DirectEventHandler.ClearEvents(CharacterName);
                    CharnameByPid.AddOrUpdate(processId, CharacterName, (key, oldValue) => CharacterName);
                }
                else
                {
                    Cache.Instance.Log("ProcessId is zero. Error.");
                }
            }
            catch (Exception e)
            {
                Cache.Instance.Log("Exception: " + e);
            }
            finally
            {
                pLock.Dispose();
                Cache.Instance.Log($"Released the [{nameof(StartEveInject)}] mutex.");
            }
        }

        public void ClearCache()
        {
            Cache.Instance.Log($"Next cache deletion for character {this.CharacterName} set to {DateTime.MinValue.ToString()}");
            this.NextCacheDeletion = DateTime.MinValue;
        }

        private string GetAccessToken(string s)
        {
            var at = s.Substring("\"access_token\":\"", "\"");
            Cache.Instance.Log($"Eve access token: {at}");
            Cache.Instance.Log($"Eve access token length: {at.Length}");
            if (at.Length < 100)
            {
                Cache.Instance.Log("Error: AccessToken length < 100.");
                return string.Empty;
            }
            return at;
        }

        private string GetTokenExpiresIn(string s)
        {
            var tei = s.Substring("\"expires_in\":", ",");
            if (tei.Length != 3)
            {
                Cache.Instance.Log("Error: TokenExpiresIn length != 3.");
                return string.Empty;
            }
            return tei;
        }

        private string GetRefreshToken(string s)
        {
            var rt = s.Substring("\"refresh_token\":\"", "\"");
            if (rt.Length != 24)
            {
                Cache.Instance.Log("Error: RefreshToken length != 24.");
                return string.Empty;
            }
            return rt;
        }

        private string GetAuthCode(string s)
        {
            var ac = s.Substring("&code=", "&state=");
            if (ac.Length != 22)
            {
                Cache.Instance.Log("Error: Authcode length != 22.");
                return string.Empty;
            }
            return ac;
        }

        private string GetVerificationToken(string s)
        {
            var vt = s.Substring("__RequestVerificationToken\" type=\"hidden\" value=\"", "\"");
            if (vt.Length != 92)
            {
                Cache.Instance.Log("Error: Verification token length != 92.");
                return string.Empty;
            }
            return vt;
        }

        public String GetSSORefreshToken(bool useCache = true, bool sisi = false)
        {
            try
            {
                using (var curlWoker = new CurlWorker(this.AccountName))
                {
                    if (useCache && !String.IsNullOrWhiteSpace(EveRefreshToken) && !String.IsNullOrEmpty(EveRefreshToken))
                    {
                        Cache.Instance.Log("Returning cached refresh token: " + EveRefreshToken);
                        return EveRefreshToken;
                    }
                    Cache.Instance.Log("Aquiring a new refresh token");

                    Guid state = Guid.NewGuid();
                    Guid challengeCodeSource = Guid.NewGuid();
                    byte[] challengeCode = System.Text.Encoding.UTF8.GetBytes(challengeCodeSource.ToString().Replace("-", ""));
                    string challengeHash = Base64.Encode(SHA256.GenerateHash(Base64.Encode(challengeCode)));

                    Cache.Instance.Log($"State: {state}");
                    Cache.Instance.Log($"ChallengeHash: {challengeHash}");

                    Cache.Instance.Log("Trying to get the SSO-Token.");
                    var url = EveUri.GetLogoffUri(sisi, state.ToString(), challengeHash).ToString();

                    //Cache.Instance.Log($"LogoffUri Url: {url}");

                    //Logoff any current character on session
                    //var resp = curlWoker.GetPostPage(url, string.Empty, HWSettings.Proxy.GetIpPort(),
                    //    HWSettings.Proxy.GetUserPassword(), true, true);

                    //using (StreamWriter w = File.AppendText(Utility.Util.AssemblyPath + "\\SSO.txt"))
                    //{
                    //    w.WriteLine(resp);

                    //}

                    bool useLegacy = Cache.Instance.EveSettings.UseLegacyLogin;
                    string authCode = String.Empty;

                    if (useLegacy)
                    {

                        url = EveUri.GetLoginUri(sisi, state.ToString(), challengeHash).ToString();

                        Cache.Instance.Log($"Open the following URI in the browser (make sure to start firefox WITH the proxy corresponding to the eve account.)");
                        Cache.Instance.Log($"EVELogin Uri: {url}");

                        var challFrm = new AuthCodeForm();
                        challFrm.ShowDialog();
                        authCode = challFrm.Challenge;
                        Cache.Instance.Log($"Submitting given auth code: {authCode}");

                    }
                    else
                    {
                        var dh = GetDriverHandler(false);
                        var driver = dh.GetDriver();

                        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(600);

                        url = EveUri.GetLoginUri(sisi, state.ToString(), challengeHash).ToString();

                        driver.GoToUrl(url);

                        dh.WaitForPage();

                        //Cache.Instance.Log($"Open the following URI in the browser (make sure to start firefox WITH the proxy corresponding to the eve account.)");
                        //Cache.Instance.Log($"EVELogin Uri: {url}");

                        // Gets the login page to extract the verfication token

                        //var resp = curlWoker.GetPostPage(url, string.Empty, HWSettings.Proxy.GetIpPort(),
                        //    HWSettings.Proxy.GetUserPassword(), true, true);


                        var response = driver.PageSource;
                        //var verificationToken = GetVerificationToken(resp);

                        //if (string.IsNullOrEmpty(verificationToken))
                        //{
                        //    Cache.Instance.Log("No verification token found in response");
                        //    return string.Empty;
                        //}


                        while (dh.IsAlive() && !dh.IsElementPresent(By.Id("UserName")))
                        {
                            Cache.Instance.Log("Waiting for the #UserName element to appear, maybe you need to solve the captcha.");
                            Thread.Sleep(2000);
                        }

                        driver.FindElement(By.Id("UserName")).SendKeys(AccountName);
                        driver.FindElement(By.Id("Password")).SendKeys(Password);
                        driver.FindElement(By.Id("submitButton")).Click();

                        dh.WaitForPage();

                        //Click(By.Id("submitButton"), driver);

                        //string postLoginData = $"__RequestVerificationToken={verificationToken}" +
                        //    $"&UserName={HttpUtility.UrlEncode(AccountName)}" +
                        //    $"&Password={HttpUtility.UrlEncode(Password)}" +
                        //    $"&RememberMe=false";

                        //resp = curlWoker.GetPostPage(url, postLoginData, HWSettings.Proxy.GetIpPort(),
                        //    HWSettings.Proxy.GetUserPassword(), true, true);

                        response = driver.PageSource;

                        //if (resp.Contains("Account/Challenge?"))
                        //{
                        //    Cache.Instance.Log("Character name challenge required. Sending challenge.");
                        //    url =
                        //        "https://login.eveonline.com/Account/Challenge?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
                        //    resp = curlWoker.GetPostPage(url, "Challenge=" + CharacterName + "&RememberCharacterChallenge=false&command=Continue",
                        //        HWSettings.Proxy.GetIpPort(), HWSettings.Proxy.GetUserPassword(), true, true);
                        //}

                        if (response.Contains("/account/verifytwofactor"))
                        {
                            Cache.Instance.Log("Email challenge required. Opening the challenge window.");
                            var challFrm = new AccountChallengeForm();
                            challFrm.ShowDialog();
                            var challenge = challFrm.Challenge;
                            Cache.Instance.Log($"Submitting given challenge: {challenge}");

                            if (String.IsNullOrEmpty(challenge))
                                throw new Exception("Challenge can't be empty.");

                            driver.FindElement(By.Id("verificationcode")).SendKeys(challenge);
                            driver.FindElement(By.Id("submitcodebutton")).Click();
                            dh.WaitForPage();
                            //url = EveUri.GetVerifyTwoFactorUri(sisi, state.ToString(), challengeHash).ToString();
                            //resp = curlWoker.GetPostPage(url, "Challenge=" + challenge + "&command=Continue&IsPasswordBreached=False&NumPasswordBreaches=0",
                            //    HWSettings.Proxy.GetIpPort(), HWSettings.Proxy.GetUserPassword(), true, true);
                        }

                        //if (resp.Contains("/oauth/eula"))
                        //{
                        //    // id="eulaHash" name="eulaHash" type="hidden" value="8FE2635FC9F85DE00542597D9D962E8D"
                        //    // https://login.eveonline.com/OAuth/Eula
                        //    // "eulaHash=8FE2635FC9F85DE00542597D9D962E8D&returnUrl=https%3A%2F%2Flogin.eveonline.com%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken&action=Accept"
                        //    var eulaHashLine = resp.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                        //        .ToList()
                        //        .FirstOrDefault(l => l.Contains("id=\"eulaHash\""));

                        //    if (eulaHashLine == null)
                        //    {
                        //        Cache.Instance.Log("if(eulaHashLine == null)");
                        //        return String.Empty;
                        //    }
                        //    else
                        //    {
                        //        var eulaHash = eulaHashLine.Substring("name=\"eulaHash\" type=\"hidden\" value=\"", "\"");
                        //        if (eulaHash.Length != 32)
                        //        {
                        //            Cache.Instance.Log("if(eulaHash.Length != 32)");
                        //            return String.Empty;
                        //        }
                        //        else
                        //        {
                        //            Cache.Instance.Log("Accepting Eula. Eula hash: " + eulaHash);
                        //            url = "https://login.eveonline.com/OAuth/Eula";
                        //            var postData = "eulaHash=" + eulaHash +
                        //                           "&returnUrl=https%3A%2F%2Flogin.eveonline.com%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken&action=Accept";
                        //            resp = curlWoker.GetPostPage(url, postData, HWSettings.Proxy.GetIpPort(), HWSettings.Proxy.GetUserPassword(), true, true);
                        //        }
                        //    }
                        //}

                        //var challFrm = new AuthCodeForm();
                        //challFrm.ShowDialog();
                        //var authCode = challFrm.Challenge;
                        //Cache.Instance.Log($"Submitting given auth code: {authCode}");

                        //string authCode = GetAuthCode(resp);

                        //if (authCode.Length == 0)
                        //{
                        //    Cache.Instance.Log("Unable to get Authcode");
                        //    return String.Empty;
                        //}

                        authCode = HttpUtility.ParseQueryString(new Uri(driver.Url).Query).Get("code");

                    }

                    if (String.IsNullOrEmpty(authCode))
                    {
                        throw new Exception("AuthCode was null or empty.");
                    }

                    url = EveUri.GetTokenUri(sisi).ToString();
                    string postDataToken = $"grant_type=authorization_code" +
                        $"&client_id=eveLauncherTQ" +
                        $"&redirect_uri=https%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ" +
                        $"&code={authCode}" +
                        $"&code_verifier={Base64.Encode(challengeCode)}";

                    var resp = curlWoker.GetPostPage(url, postDataToken, HWSettings.Proxy.GetIpPort(), HWSettings.Proxy.GetUserPassword(), true, true);

                    //using (StreamWriter w = File.AppendText(Utility.Util.AssemblyPath + "\\SSO.txt"))
                    //{
                    //    w.WriteLine(resp);
                    //}
                    string refreshToken = GetRefreshToken(resp);

                    if (string.IsNullOrEmpty(refreshToken))
                    {
                        Cache.Instance.Log("Refresh token is empty from response.");
                        return String.Empty;

                    }
                    else
                    {
                        EveRefreshToken = refreshToken;
                        Cache.Instance.Log("Refresh token request was successful: " + EveRefreshToken);
                        if (!useLegacy)
                            GetDriverHandler().Dispose();
                        return EveRefreshToken;
                    }
                }
            }
            catch (Exception ex)
            {
                Cache.Instance.Log("Exception: " + ex);
                return String.Empty;
            }
        }

        public String GetEveAccessToken(bool useCache = true, bool sisi = false, bool retryOnce = true)
        {
            if (useCache && EveAccessTokenValidTill > DateTime.UtcNow && !String.IsNullOrWhiteSpace(EveAccessToken))
            {
                Cache.Instance.Log("Returning cached EveAccessToken: " + EveAccessToken);
                return EveAccessToken;
            }

            using (var curlWoker = new CurlWorker(this.AccountName))
            {
                var refreshToken = GetSSORefreshToken(useCache, sisi);

                string postdataRefreshToken = $"client_id=eveLauncherTQ" +
                    $"&grant_type=refresh_token" +
                    $"&refresh_token={HttpUtility.UrlEncode(EveRefreshToken)}";


                Cache.Instance.Log($"Post data refresh token: {postdataRefreshToken} Uri {EveUri.GetTokenUri(sisi).ToString()}");

                var resp = curlWoker.GetPostPage(
                    EveUri.GetTokenUri(sisi).ToString(), postdataRefreshToken, HWSettings.Proxy.GetIpPort(),
                    HWSettings.Proxy.GetUserPassword(), true, true);

                using (StreamWriter w = File.AppendText(Utility.Util.AssemblyPath + "\\SSO.txt"))
                {
                    w.WriteLine(resp);
                }

                string accessToken = GetAccessToken(resp);
                string expiresIn = GetTokenExpiresIn(resp);

                var expiresInt = 600;
                int.TryParse(expiresIn, out expiresInt);
                expiresInt -= 100;

                EveAccessToken = accessToken;
                EveAccessTokenValidTill = DateTime.UtcNow.AddSeconds(expiresInt);

                Cache.Instance.Log("EveAccessToken: " + EveAccessToken);
                Cache.Instance.Log("EveAccessToken expires in " + expiresInt + " seconds. Which is at: " + EveAccessTokenValidTill.ToString());

            }
            return EveAccessToken;
        }

        // C:\****\tq\bin\exefile.exe  /noconsole /server:tranquility /triPlatform=dx9 /ssoToken=****** /settingsprofile=Default /machineHash=******

        public String GetEveStartParameter(string eveAccessToken, bool sisi)
        {

            if (HWSettings.LauncherMachineHash.Length != 36)
            {
                Cache.Instance.Log("Error: HWSettings.LauncherMachineHash.Length != 36. Please generate a new LauncherMachineHash.");
                return string.Empty;
            }

            var ccpXDHash = LauncherHash.CCPMagic(HWSettings.LauncherMachineHash.Replace("-", ""));

            if (ccpXDHash.Length != 19)
                return string.Empty;

            Cache.Instance.Log($"LauncherMachineHash: [{HWSettings.LauncherMachineHash}] CCP FailEncodedHash: [{ccpXDHash}]");

            // March 2024
            // C:\eveonline\SharedCache\tq\bin64\exefile.exe "/noconsole" "/server:tranquility.servers.eveonline.com" "/ssoToken=***" "/refreshToken=***" "/settingsprofile=Default" "/language=en" "/triplatform=dx11" "/deviceID=***" "/machineHash=***"
            // where the deviceID is a guid with "-" seperators and the machineHash is the same guid without "-"

            // March 2025
            // C:\eveonline\SharedCache\tq\bin64\exefile.exe "/noconsole" "/server:tranquility.servers.eveonline.com" "/ssoToken=***" "/refreshToken=***" "/settingsprofile=Default" "/language=en" "/triplatform=dx11" "/deviceID=***" "/machineHash=***" "/journeyID=***"
            // where the deviceID is a guid with "-" seperators and the machineHash is the same guid without "-"
            // TODO:  add journey id? what's that? Also return DeviceIdV2 from the registry hook? (Is the registry value of DeviceIdV2 the same as the /deviceID start param?)

            var startParams = $"\"{(!Console ? "/noconsole" : "")}\" \"/server:{(sisi ? "singularity" : "tranquility.servers.eveonline.com")}\" \"/ssoToken={eveAccessToken}\" \"/refreshToken={this.EveRefreshToken}\" \"/settingsprofile=Default\" \"/language=en\" \"/triPlatform={(DX11 ? "dx11" : "dx12")}\" \"/deviceID={HWSettings.LauncherMachineHash}\" \"/machineHash={HWSettings.LauncherMachineHash.Replace("-", "")}\"";
            //var startParams = $"\"/server:{(sisi ? "singularity" : "tranquility.servers.eveonline.com")}\" \"/ssoToken={eveAccessToken}\" \"/refreshToken={this.EveRefreshToken}\" \"/settingsprofile=Default\" \"/language=en\" \"/triPlatform={(DX11 ? "dx11" : "dx12")}\" \"/deviceID={HWSettings.LauncherMachineHash}\" \"/machineHash={HWSettings.LauncherMachineHash.Replace("-", "")}\"";

            Cache.Instance.Log($"Startparams: [{startParams}]");
            return startParams;
            //return string.Empty;
        }

        #region pathes

        /* personal folder example */
        // C:\Users\USERNAME\Documents\EVE
        // C:\Users\USERNAME\Documents\

        /* local appdata example */
        // C:\Users\USERNAME\AppData\Local\CCP\EVE\c_eveoffline
        // C:\Users\USERNAME\AppData\Local\

        //		public string GetEveSettingsFolder() {
        //			return "C:\\Users\\";
        //		}

        public string GetDefaultSettingsFolder()
        {
            return Cache.Instance.AssemblyPath + "\\Resources\\EveSettings\\default\\";
        }

        public string GetAppDataFolder()
        {
            return "C:\\Users\\" + HWSettings.WindowsUserLogin + "\\AppData\\Local\\";
        }


        public string GetPersonalFolder()
        {
            return "C:\\Users\\" + HWSettings.WindowsUserLogin + "\\Documents\\";
        }

        public string GetEveUnderscorePath(bool sisi = false)
        {
            var eveBasePath = GetEvePath(sisi);
            eveBasePath = eveBasePath.Replace(":\\", "_");
            eveBasePath = eveBasePath.Replace("\\", "_").ToLower() + (sisi ? "_singularity" : "_tranquility");
            return eveBasePath;
        }

        public void ClearNonEveSharpCCPData()
        {

            try
            {
                // Delete %localappdata%/CCP directory and its contents recursively
                string localAppDataCCP = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CCP");
                if (Directory.Exists(localAppDataCCP))
                {
                    Directory.Delete(localAppDataCCP, true);
                    Cache.Instance.Log("Deleted directory: " + localAppDataCCP);
                }

                // Delete %appdata%/EVE Online directory and its contents recursively
                string appDataEVEOnline = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EVE Online");
                if (Directory.Exists(appDataEVEOnline))
                {
                    Directory.Delete(appDataEVEOnline, true);
                    Cache.Instance.Log("Deleted directory: " + appDataEVEOnline);
                }

                // Delete registry key Computer\HKEY_CURRENT_USER\SOFTWARE\CCP\EVE
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\CCP", true))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree("EVE", false);
                        Cache.Instance.Log("Deleted registry key: HKEY_CURRENT_USER\\SOFTWARE\\CCP\\EVE");
                    }
                }
            }
            catch (Exception ex)
            {
                Cache.Instance.Log("An error occurred: " + ex.Message);
            }
        }

        public string GetEveExePath(bool sisi = false)
        {
            return sisi ? Cache.Instance.EveLocation.Replace("tq", "sisi") : Cache.Instance.EveLocation;
        }



        public string GetEvePath(bool sisi = false)
        {
            var eveLocation = sisi ? Cache.Instance.EveLocation.Replace("tq", "sisi") : Cache.Instance.EveLocation;
            return Directory.GetParent(Directory.GetParent(eveLocation).ToString()).ToString();
        }

        public string GetEveRootPath(bool sisi = false)
        {
            return Directory.GetParent(GetEvePath(sisi)).ToString();
        }

        public EveAccount Clone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                if (this.GetType().IsSerializable)
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, this);
                    stream.Position = 0;
                    return (EveAccount)formatter.Deserialize(stream);
                }
                return null;
            }
        }

        #endregion
    }
}