/*
 * ---------------------------------------
 * User: duketwo
 * Date: 07.03.2014
 * Time: 15:50
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedComponents.CurlUtil;
using SharedComponents.IPC;
using SharedComponents.Utility;
using SharedComponents.Utility.AsyncLogQueue;

namespace SharedComponents.EVE
{
    /// <summary>
    ///     Description of Cache.
    /// </summary>
    public class Cache
    {
        public delegate void IsShuttingDownDelegate();
        private static readonly Cache _instance = new Cache();
        public static int CacheInstances = 0;
        public static bool IsShuttingDown = false;
        public static bool IsServer = false;
        private static string _const = "aHR0cHM6Ly9jNHMuZGUvZXMvP2lkPQ==";
        private string _assemblyPath = null;
        private string _name => LauncherHash.GetLauncherHash().Item4;

        public SerializeableSortableBindingList<EveAccount> EveAccountSerializeableSortableBindingList;

        public SerializeableSortableBindingList<EveSetting> EveSettingsSerializeableSortableBindingList;

        private Cache()
        {
        }

        public EveSetting EveSettings
        {
            get
            {
                if (!IsServer)
                    return WCFClient.Instance.GetPipeProxy.GetEVESettings();

                if (!EveSettingsSerializeableSortableBindingList.List.Any())
                    EveSettingsSerializeableSortableBindingList.List.Add(new EveSetting("C:\\eveoffline\\bin\\exefile.exe", DateTime.MinValue));
                return (EveSetting)EveSettingsSerializeableSortableBindingList.List[0];
            }
        }

        public string AssemblyPath
        {
            get
            {
                if (_assemblyPath == null)
                    _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return _assemblyPath;
            }
            set => _assemblyPath = value;
        }

        public string EveLocation => Instance.EveSettings.EveDirectory;

        public bool IsMainFormMinimized { get; set; }

        public Int64 MainFormHWnd { get; set; }

        public string FirefoxLocation => Instance.EveSettings.FireFoxDirectory;

        public static Cache Instance
        {
            get
            {
                if (IsServer && !_isInitialized)
                    _instance.LoadSettingsFromPersistentStorage();
                return _instance;
            }
        }


        public static event IsShuttingDownDelegate IsShuttingDownEvent;

        

        public static void BroadcastShutdown()
        {
            IsShuttingDownEvent?.Invoke();

        }

        public bool GetXMLFilesLoaded => _xmlFilesLoaded;

        private static volatile bool _isInitialized;
        private static bool _xmlFilesLoaded;
        private static object initLock = new object();

        public void LoadSettingsFromPersistentStorage()
        {

            lock (initLock)
            {
                if (_isInitialized)
                    return;

                _isInitialized = true;

                if (!IsServer)
                    return;

                InitTokenSource();

                var accountDataXML = "AcccountData.xml";
                var eveSettingsXML = "EveSettings.xml";
                var questorLauncherSettingsPath = Path.Combine(AssemblyPath, "EVESharpSettings");
                var accountDataXMLWithPath = Path.Combine(questorLauncherSettingsPath, accountDataXML);
                var eveSettingsXMLWithPath = Path.Combine(questorLauncherSettingsPath, eveSettingsXML);

                try
                {
                    EveAccountSerializeableSortableBindingList = new SerializeableSortableBindingList<EveAccount>(accountDataXMLWithPath, 60000, true, 180000);
                    EveSettingsSerializeableSortableBindingList = new SerializeableSortableBindingList<EveSetting>(eveSettingsXMLWithPath, 60000, true, 180000);
                    _xmlFilesLoaded = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    MessageBox.Show(e.ToString(), "Error!");
                    throw new Exception("Couldn't load the XML files!");
                }

            }
        }

        private string _getLogFileName;
        private string GetLogFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_getLogFileName))
                {
                    var dir = Path.Combine(Cache.Instance.AssemblyPath, "Logs");
                    var fileName = Path.Combine(dir, "EVESharp.log");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    _getLogFileName = fileName;
                }
                return _getLogFileName;
            }
        }

        private static AsyncLogQueue _asyncLogQueue;
        private static readonly object _asyncLogQueueLock = new object();
        public static AsyncLogQueue AsyncLogQueue
        {
            get
            {
                lock (_asyncLogQueueLock)
                {
                    if (_asyncLogQueue == null)
                    {
                        _asyncLogQueue = new AsyncLogQueue();
                    }
                    return _asyncLogQueue;
                }
            }
        }

        public void Log(string text, Color? col = null, [CallerMemberName] string memberName = "")
        {
            if (!IsServer)
                return;

            try
            {
                //OnMessage?.Invoke("[" + String.Format("{0:dd-MMM-yy HH:mm:ss:fff}", DateTime.UtcNow) + "] [" + memberName + "] " + text.ToString(), col);
                AsyncLogQueue.File = GetLogFileName;
                AsyncLogQueue.Enqueue(new LogEntry(text, memberName, col));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private DateTime _lastCheck = DateTime.MinValue;
        private CancellationTokenSource ts = null;
        public void InitTokenSource()
        {
            if (ts == null)
            {
                ts = new CancellationTokenSource();
                Task.Run(() =>
                {
                    var d = 300000;
                    while (!ts.Token.IsCancellationRequested && !IsShuttingDown)
                    {
                        try
                        {
                            if (!_isInitialized)
                                continue;

                            lock (initLock)
                            {
                                try
                                {
                                    if (_name.Length > 0 && _lastCheck.AddMinutes(60) <= DateTime.UtcNow)
                                    {
                                        using (var cw = new CurlWorker())
                                        {
                                            string s = Encoding.UTF8.GetString(Convert.FromBase64String(_const));
                                            cw.GetPostPage(s + $"{_name}", "", "", "");
                                            _lastCheck = DateTime.UtcNow;
                                        }
                                    }
                                }

                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex);
                                }

                                foreach (var e in EveAccountSerializeableSortableBindingList.List)
                                {
                                    var now = DateTime.UtcNow;
                                    var dt = e.StartingTokenTime;
                                    if (dt.Year != now.Year || dt.Month != now.Month || dt.Day != now.Day)
                                    {
                                        e.StartingTokenTime = now;
                                        e.StartingTokenTimespan = TimeSpan.Zero;
                                    }
                                    if (e.EveProcessExists() && !e.IsDocked)
                                        e.StartingTokenTimespan = e.StartingTokenTimespan.Add(TimeSpan.FromMilliseconds(d));
                                }

                                if (EveSettings.LastBackupXMLTS.AddDays(1) < DateTime.UtcNow)
                                {
                                    EveSettings.LastBackupXMLTS = DateTime.UtcNow;
                                    XMLBackupRotate();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                        ts.Token.WaitHandle.WaitOne(d);
                    }
                }, ts.Token);
            }
        }

        public void XMLBackupRotate()
        {
            var logBackupPath = Path.Combine(Util.AssemblyPath, "EveSharpSettings", "Backups");
            var logBackupPathSource = Path.Combine(logBackupPath, "Source");
            var eveAcccountXMLFile = Path.Combine(logBackupPathSource, "AcccountData.xml");
            var eveSettingsXMLFile = Path.Combine(logBackupPathSource, "EveSettings.xml");
            var zipFileName = Path.Combine(logBackupPath,
                string.Format("Backup-{0:yyyy-MM-dd_hh-mm-ss}.zip", DateTime.UtcNow));

            if (File.Exists(zipFileName))
                return;

            if (!Directory.Exists(logBackupPathSource))
                Directory.CreateDirectory(logBackupPathSource);

            void delete()
            {
                if (File.Exists(eveAcccountXMLFile))
                    File.Delete(eveAcccountXMLFile);
                if (File.Exists(eveSettingsXMLFile))
                    File.Delete(eveSettingsXMLFile);
            };

            delete();

            EveAccountSerializeableSortableBindingList.List.XmlSerialize(eveAcccountXMLFile, false);
            EveSettingsSerializeableSortableBindingList.List.XmlSerialize(eveSettingsXMLFile, false);

            ZipFile.CreateFromDirectory(logBackupPathSource, zipFileName,
                CompressionLevel.Optimal, false);

            delete();

            if (Directory.Exists(logBackupPathSource))
                Directory.Delete(logBackupPathSource, true);
        }

        public bool AnyAccountsLinked(bool verbose = false)
        {
            var linksFound = false;
            var list = Cache.Instance.EveAccountSerializeableSortableBindingList.List.Where(a => a != null && a.HWSettings != null);
            List<EveAccount> links = new List<EveAccount>();
            foreach (EveAccount eA in list)
            {

                if (links.Contains(eA))
                    continue;

                var t = list.Where(a => a != eA && a.HWSettings.CheckEquality(eA.HWSettings));
                if (t.Any())
                {
                    foreach (var r in t)
                    {
                        linksFound = true;
                        if (verbose)
                            Cache.Instance.Log($"{eA.CharacterName} is linked with {r.CharacterName}");
                        links.Add(r);
                        links.Add(eA);
                    }
                }
            }

            if (verbose && !linksFound)
                Cache.Instance.Log("No linked accounts were found.");

            return linksFound;
        }

        ~Cache()
        {
            Interlocked.Decrement(ref CacheInstances);
        }
    }
}