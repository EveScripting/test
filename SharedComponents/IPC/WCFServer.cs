/*
 * ---------------------------------------
 * User: duketwo
 * Date: 08.04.2014
 * Time: 11:35
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using SharedComponents.Events;
using SharedComponents.EVE;
using SharedComponents.EVE.ClientSettings;
using SharedComponents.EVE.ClientSettings.Global.Main;
using SharedComponents.EVE.ClientSettings.Pinata;
using SharedComponents.EVE.ClientSettings.Pinata.Main;
using SharedComponents.SharpLogLite.Model;
using SharedComponents.Utility;
using SharedComponents.CurlUtil;
using SharedComponents.SQLite;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Dapper;
using SharedComponents.EVE.DatabaseSchemas;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedComponents.IPC.TCP;
using System.ServiceModel.Channels;
using Newtonsoft.Json;
using SharedComponents.EVE.Models;
using SharedComponents.EVE.StaticData;
using SharedComponents.EVE.ClientSettings.Abyssal.Main;
using SharedComponents.EVE.ClientSettings.AbyssalGuard.Main;

namespace SharedComponents.IPC
{
    [ServiceKnownType(typeof(MissionType))]
    [ServiceKnownType(typeof(AmmoType))]
    [ServiceKnownType(typeof(ClientSetting))]
    [ServiceKnownType(typeof(DamageType))]
    [ServiceKnownType(typeof(QuestorDebugSetting))]
    [ServiceKnownType(typeof(ShipFitting))]
    [ServiceKnownType(typeof(FactionFitting))]
    [ServiceKnownType(typeof(MissionFitting))]
    [ServiceKnownType(typeof(QuestorSetting))]
    [ServiceKnownType(typeof(QuestorMainSetting))]
    [ServiceKnownType(typeof(QuestorFaction))]
    [ServiceKnownType(typeof(QuestorMission))]
    [ServiceKnownType(typeof(EveAccount))]
    [ServiceKnownType(typeof(HWSettings))]
    [ServiceKnownType(typeof(EveSetting))]
    [ServiceKnownType(typeof(DirectEvent))]
    [ServiceKnownType(typeof(Proxy))]
    [ServiceKnownType(typeof(ConcurrentBindingList<Proxy>))]
    [ServiceKnownType(typeof(LogSeverity))]
    [ServiceKnownType(typeof(PinataMainSetting))]
    [ServiceKnownType(typeof(Region))]
    [ServiceKnownType(typeof(List<int>))]
    [ServiceKnownType(typeof(List<string>))]
    [ServiceContract(CallbackContract = typeof(IDuplexServiceCallback))]
    [ServiceKnownType(typeof(GlobalMainSetting))]
    [ServiceKnownType(typeof(TimeSpan))]
    [ServiceKnownType(typeof(FactionType))]
    [ServiceKnownType(typeof(BroadcastMessage))]
    [ServiceKnownType(typeof(RecorderEncoderSetting))]
    [ServiceKnownType(typeof(Skills))]
    [ServiceKnownType(typeof(AbyssalMainSetting))]
    [ServiceKnownType(typeof(AbyssalGuardMainSetting))]
    public interface IOperationContract
    {
        [OperationContract]
        void Ping();

        [OperationContract]
        int GetDumpLootIterations(string charName);

        [OperationContract]
        void IncreaseDumpLootIterations(string charName);

        [OperationContract(IsOneWay = true)]
        void RemoteLog(string s);

        [OperationContract(IsOneWay = true)]
        void RemoteConsoleLog(string charname, string s, bool isErr);

        [OperationContract]
        EveAccount GetEveAccount(string charName);

        [OperationContract]
        void CompileEveSharpCore(string charName);

        [OperationContract(IsOneWay = true)]
        void SetEveAccountAttributeValue(string charName, string attributeName, object val);

        [OperationContract]
        void SetEveAccountAttributeValueBlocking(string charName, string attributeName, object val);

        [OperationContract]
        bool IsMainFormMinimized();

        [OperationContract]
        EveSetting GetEVESettings();

        [OperationContract(IsOneWay = true)]
        void OnDirectEvent(string charName, DirectEvent directEvent);

        [OperationContract(IsOneWay = true)]
        void AttachCallbackHandlerToEveAccount(string charName);


        [OperationContract]
        Proxy GetProxy(int proxyId);

        [OperationContract]
        Dictionary<int, SolarSystemEntry> GetGateCampInfo(int[] solarSystemIds, TimeSpan? cacheDuration);

        [OperationContract]
        string GetPage(string url, TimeSpan cacheDuration);

        [OperationContract]
        Int64 MainFormHWnd();

        [OperationContract(IsOneWay = true)]
        void SendBroadcastMessage(BroadcastMessage broadcastMessage, bool final = false);


        [OperationContract(IsOneWay = true)]
        void SendDiscordWebhookMessage(string url, string message, string name);
    }


    public interface IDuplexServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnCallback();

        [OperationContract(IsOneWay = true)]
        void GotoHomebaseAndIdle();

        [OperationContract(IsOneWay = true)]
        void RestartEveSharpCore();

        [OperationContract(IsOneWay = true)]
        void GotoJita();

        [OperationContract(IsOneWay = true)]
        void PauseAfterNextDock();

        [OperationContract(IsOneWay = true)]
        void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage);
    }

    [KnownType(typeof(MissionType))]
    [KnownType(typeof(AmmoType))]
    [KnownType(typeof(ClientSetting))]
    [KnownType(typeof(DamageType))]
    [KnownType(typeof(QuestorDebugSetting))]
    [KnownType(typeof(ShipFitting))]
    [KnownType(typeof(FactionFitting))]
    [KnownType(typeof(MissionFitting))]
    [KnownType(typeof(QuestorSetting))]
    [KnownType(typeof(QuestorMainSetting))]
    [KnownType(typeof(QuestorFaction))]
    [KnownType(typeof(QuestorMission))]
    [KnownType(typeof(EveAccount))]
    [KnownType(typeof(HWSettings))]
    [KnownType(typeof(EveSetting))]
    [KnownType(typeof(DirectEvent))]
    [KnownType(typeof(Proxy))]
    [KnownType(typeof(ConcurrentBindingList<Proxy>))]
    [KnownType(typeof(LogSeverity))]
    [KnownType(typeof(Region))]
    [KnownType(typeof(PinataMainSetting))]
    [KnownType(typeof(List<string>))]
    [KnownType(typeof(List<int>))]
    [KnownType(typeof(GlobalMainSetting))]
    [KnownType(typeof(FactionType))]
    [KnownType(typeof(TimeSpan))]
    [KnownType(typeof(TimeSpan?))] // Nullable<TimeSpan>
    [KnownType(typeof(BroadcastMessage))]
    [KnownType(typeof(RecorderEncoderSetting))]
    [KnownType(typeof(Dictionary<int, SolarSystemEntry>))]
    [KnownType(typeof(SolarSystemEntry))]
    [KnownType(typeof(SolarSystemKills))]
    [KnownType(typeof(KillsInfo))]
    [KnownType(typeof(Checks))]
    [KnownType(typeof(Skills))]
    [KnownType(typeof(AbyssalMainSetting))]
    [KnownType(typeof(AbyssalGuardMainSetting))]
    public class OperationContract : IOperationContract
    {
        public OperationContract()
        {
            _eveAccountCache = new ConcurrentDictionary<string, EveAccount>();
            Debug.WriteLine("New OperationContract (NamedPipe WCF) instance.");
        }

        private IDuplexServiceCallback Callback => OperationContext.Current.GetCallbackChannel<IDuplexServiceCallback>();
        private ConcurrentDictionary<string, EveAccount> _eveAccountCache;

        public EveSetting GetEVESettings()
        {
            try
            {
                return Cache.Instance.EveSettings;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }


        public bool IsMainFormMinimized()
        {
            try
            {
                return Cache.Instance.IsMainFormMinimized;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public Int64 MainFormHWnd()
        {
            try
            {
                return Cache.Instance.MainFormHWnd;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return (Int64)IntPtr.Zero;
            }
        }

        public void SetEveAccountAttributeValueBlocking(string charName, string attributeName, object val)
        {
            SetEveAccountAttributeValue(charName, attributeName, val);
        }

        public void SetEveAccountAttributeValue(string charName, string attributeName, object val)
        {
            try
            {
                var eA = GetEveAccount(charName);
                if (eA != null)
                {
                    var t = eA.GetType();
                    var info = t.GetProperty(attributeName);
                    if (info == null)
                        return;
                    if (!info.CanWrite)
                        return;
                    info.SetValue(eA, val, null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Cache.Instance.Log(e.ToString());
            }
        }

        public void CompileEveSharpCore(string charName)
        {
            Util.RunInDirectory("Updater.exe", "CompileEVESharpCore");
        }

        public EveAccount GetEveAccount(string charName)
        {
            try
            {
                if (_eveAccountCache.TryGetValue(charName, out var eA) && eA != null)
                {
                    //Debug.WriteLine($"EVEAccount of {charName} was found in the cache. Returning.");
                    return eA;
                }
                else
                {
                    var ret = Cache.Instance.EveAccountSerializeableSortableBindingList.List.ToList()
                        .FirstOrDefault(s => !string.IsNullOrEmpty(s.CharacterName) && s.CharacterName.Equals(charName));
                    if (ret != null)
                        _eveAccountCache[charName] = ret; // AddOrUpdate func isn't atomic!
                    //Debug.WriteLine($"EVEAccount of {charName} wasn't found in the cache. Adding to the cache.");
                    return ret;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Cache.Instance.Log(e.ToString());
                return null;
            }
        }

        public void Ping()
        {
        }

        public int GetDumpLootIterations(string charName)
        {
            var eA = GetEveAccount(charName);
            if (eA.DumpLootTimestamp.AddHours(24) < DateTime.UtcNow)
            {
                eA.DumpLootTimestamp = DateTime.UtcNow;
                eA.DumpLootIterations = 0;
            }
            return eA.DumpLootIterations;
        }

        public Dictionary<int, SolarSystemEntry> GetGateCampInfo(int[] solarSystemIds, TimeSpan? cacheDuration)
        {
            cacheDuration ??= TimeSpan.FromMinutes(5);

            solarSystemIds = solarSystemIds.Distinct().ToArray();
            var solarSystemIdLookup = solarSystemIds.ToHashSet();
            var entries = new Dictionary<int, SolarSystemEntry>();
            // Grab existing entries if within cache duration
            using (var rc = ReadConn.Open())
            {
                var sql = @"SELECT * from GateCampCheckEntry WHERE LastUpdate > @lastUpdate";
                var param = new { solarSystemIds, lastUpdate = DateTime.UtcNow - cacheDuration };
                var query = rc.DB.Select<GateCampCheckEntry>(sql, param);

                // Only update entries that are in the lookup
                foreach (var entry in query)
                {
                    if (entry.SolarSystemEntryJson == null)
                    {
                        // System has no information
                        continue;
                    }

                    if (solarSystemIdLookup.Contains(entry.SolarSystemId))
                    {
                        try
                        {
                            entries[entry.SolarSystemId] = JsonConvert.DeserializeObject<SolarSystemEntry>(entry.SolarSystemEntryJson);
                        }
                        catch (Exception e)
                        {
                            Cache.Instance.Log(e.ToString());
                        }
                    }
                }
            }

            // Fetch missing entries from
            // https://eve-gatecheck.space/eve/get_kills.php?systems={ID1},{ID2},...
            var missingSolarSystemIds = solarSystemIds.Where(id => !entries.ContainsKey(id)).ToArray();
            if (missingSolarSystemIds.Length > 0)
            {
                var url = $"https://eve-gatecheck.space/eve/get_kills.php?systems={string.Join(",", missingSolarSystemIds)}";
                // Cache.Instance.Log(url);
                try
                {
                    using var httpClient = new HttpClient();
                    var source = httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                    // Cache.Instance.Log(source);
                    var response = JsonConvert.DeserializeObject<EveGateCheckResponse>(source);
                    // Cache.Instance.Log(response != null ? "Response is not null" : "Response is null");

                    // Update cache
                    using (var wc = WriteConn.Open())
                    {
                        var entriesToInsert = new List<GateCampCheckEntry>();
                        foreach (var missingSolarSystemId in missingSolarSystemIds)
                        {
                            if (response.SolarSystemKills.TryGetValue(missingSolarSystemId, out var entry))
                            {
                                entriesToInsert.Add(new GateCampCheckEntry
                                {
                                    SolarSystemId = missingSolarSystemId,
                                    SolarSystemEntryJson = JsonConvert.SerializeObject(entry),
                                    LastUpdate = DateTime.UtcNow,
                                    IsPremium = response.IsPremium,
                                });
                            }
                            else
                            {
                                // Its possible that the system has no information, populate with empty entry
                                entriesToInsert.Add(new GateCampCheckEntry
                                {
                                    SolarSystemId = missingSolarSystemId,
                                    SolarSystemEntryJson = null,
                                    LastUpdate = DateTime.UtcNow,
                                    IsPremium = response.IsPremium,
                                });
                            }
                        }
                        wc.DB.InsertAll(entriesToInsert);
                    }

                    // Merge with existing entries
                    foreach (var entry in response.SolarSystemKills)
                    {
                        entries[entry.Key] = entry.Value;
                    }
                }
                catch (Exception e)
                {
                    Cache.Instance.Log(e.ToString());
                }
            }

            return entries;
        }

        public string GetPage(string url, TimeSpan cacheDuration)
        {
            if (cacheDuration == default)
            {
                using (var cw = new CurlWorker())
                {
                    return cw.GetPostPage(url, "", "", "") ?? string.Empty;
                }
            }

            try
            {
                using (var rc = ReadConn.Open())
                {
                    var sql = @"SELECT * from CachedWebsiteEntry WHERE url = @url ORDER BY LastUpdate DESC LIMIT 1";
                    var paramUrl = new { url };
                    var query = rc.DB.QueryFirstOrDefault<CachedWebsiteEntry>(sql, paramUrl);
                    if (query != null && query.LastUpdate > DateTime.UtcNow - cacheDuration)
                    {
                        return query.Source ?? string.Empty;
                    }

                    //Remove 5 points from Jackson Score
                    //db.UpdateAdd(() => new Person { Score = -5 }, where: x => x.LastName == "Jackson");
                    //INSERT one row
                    //db.Insert(new Artist { Id = 1, Name = "Faith No More" });

                    Task.Run(() =>
                    {
                        using (var cw = new CurlWorker())
                        {
                            var source = cw.GetPostPage(url, "", "", "") ?? string.Empty;

                            if (string.IsNullOrEmpty(source)) return;

                            using (var wc = WriteConn.Open())
                            {
                                if (wc.DB.Exists<CachedWebsiteEntry>(x => x.Url == url))
                                {
                                    var allEntries = wc.DB.Select<CachedWebsiteEntry>(x => x.Url == url);
                                    wc.DB.Delete<CachedWebsiteEntry>(x => x.Url == url);
                                }

                                wc.DB.Insert<CachedWebsiteEntry>(new CachedWebsiteEntry()
                                {
                                    Url = url,
                                    Source = source,
                                    LastUpdate = DateTime.UtcNow,
                                });
                            }
                        }
                    });

                    if (query != null)
                    {
                        return query.Source ?? string.Empty;
                    }

                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                throw;
            }
        }

        public void IncreaseDumpLootIterations(string charName)
        {
            var eA = GetEveAccount(charName);
            eA.DumpLootIterations++;
        }

        public void RemoteLog(string s)
        {
            Cache.Instance.Log(s);
        }

        public void RemoteConsoleLog(string charName, string s, bool isErr)
        {
            try
            {
                ConsoleEventHandler.InvokeConsoleEvent(charName, s, isErr);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public void OnDirectEvent(string charName, DirectEvent directEvent)
        {
            try
            {
                DirectEventHandler.OnNewDirectEvent(charName, directEvent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public Proxy GetProxy(int proxyId)
        {
            try
            {
                return Cache.Instance.EveSettings.Proxies.FirstOrDefault(p => p.Id == proxyId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }


        public void AttachCallbackHandlerToEveAccount(string charName)
        {
            try
            {
                var eA = GetEveAccount(charName);
                if (eA != null)
                    eA.SetClientCallback(Callback);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Distribute messages to each of the eve clients on this local machine
        /// </summary>
        /// <param name="broadcastMessage"></param>
        public void SendBroadcastMessage(BroadcastMessage broadcastMessage, bool final = false)
        {
            try
            {
                // local machine handling
                var eveAccounts = Cache.Instance.EveAccountSerializeableSortableBindingList.List;
                foreach (var eA in eveAccounts)
                {
                    if (broadcastMessage.Receiver.Equals("*") == false && eA.CharacterName.Equals(broadcastMessage.Receiver) == false)
                        continue;

                    if (broadcastMessage.Sender.Equals(eA.CharacterName))
                        continue;

                    if (eA.GetClientCallback() == null)
                        continue;

                    if (((ICommunicationObject)eA.GetClientCallback()).State != CommunicationState.Opened)
                        continue;

                    eA.GetClientCallback().ReceiveBroadcastMessage(broadcastMessage);

                }

                // remote machine handling
                if (!final && (Cache.Instance.EveSettings.RemoteWCFClient || Cache.Instance.EveSettings.RemoteWCFServer))
                {
                    WCFClientTCP.Instance?.GetPipeProxy?.SendBroadcastMessage(broadcastMessage);
                }
            }
            catch (Exception ex)
            {
                Cache.Instance.Log($"Exception [{ex}]");
            }
        }

        public void SendDiscordWebhookMessage(string url, string message, string name)
        {

            Task.Run(async () =>
            {
                try
                {

                    await Discord.SendDiscordWebhook(url, message, name);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            });

        }
    }
    /// <summary>
    /// This runs on the Launcher and acts as server to communicate with all running eve clients via (WCFClient)
    /// </summary>
    public class WCFServer
    {
        private static readonly WCFServer _instance = new WCFServer();
        private ServiceHost host;
        public Thread thread;

        public WCFServer()
        {
        }


        public string GetPipeName => Cache.Instance.EveSettings.WCFPipeName;


        public static WCFServer Instance => _instance;

        public void StartWCFServer()
        {
            if (Cache.Instance.EveSettings.WCFPipeName == String.Empty ||
                Cache.Instance.EveSettings.WCFPipeName == null)
                Cache.Instance.EveSettings.WCFPipeName = Guid.NewGuid().ToString();

            thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        host = new ServiceHost(typeof(OperationContract),
                            new Uri[] { new Uri("net.pipe://localhost/" + Cache.Instance.EveSettings.WCFPipeName.ToString()) });

                        ((ServiceBehaviorAttribute)host.Description.Behaviors[typeof(ServiceBehaviorAttribute)]).InstanceContextMode =
                            InstanceContextMode.Single;
                        ((ServiceBehaviorAttribute)host.Description.Behaviors[typeof(ServiceBehaviorAttribute)]).ConcurrencyMode = ConcurrencyMode.Multiple;
                        ((ServiceBehaviorAttribute)host.Description.Behaviors[typeof(ServiceBehaviorAttribute)]).MaxItemsInObjectGraph = int.MaxValue; // max size = 2048mb
                        host.Description.Behaviors.Remove(typeof(ServiceDebugBehavior));
                        host.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
                        var binding = new NetNamedPipeBinding();
                        binding.MaxReceivedMessageSize = 2147483647;
                        binding.MaxBufferSize = 2147483647;
                        host.AddServiceEndpoint(typeof(IOperationContract), binding, "");
                        ServiceThrottlingBehavior stb = new ServiceThrottlingBehavior
                        {
                            MaxConcurrentSessions = 10000,
                            MaxConcurrentCalls = 10000,
                            MaxConcurrentInstances = 10000,
                        };
                        host.Description.Behaviors.Add(stb);

                        host.Open();
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Generating new guid..");
                        Cache.Instance.EveSettings.WCFPipeName = Guid.NewGuid().ToString();
                        continue;
                    }

                    break;
                }
            });
            thread.Start();
        }

        public void StopWCFServer()
        {
            if (host != null)
                host.Close();
        }
    }
}