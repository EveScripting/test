using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using System.Runtime.InteropServices;

namespace SharedComponents.IPC.TCP
{
    [ServiceKnownType(typeof(BroadcastMessage))]
    [ServiceContract(CallbackContract = typeof(IDuplexServiceCallbackTCP))]
    public interface IOperationContractTCP
    {
        [OperationContract]
        void Ping();

        [OperationContract(IsOneWay = true)]
        void SendBroadcastMessage(BroadcastMessage broadcastMessage);

        [OperationContract(IsOneWay = true)]
        void RegisterCallbackHandler();

        [OperationContract]
        bool IsEveInstanceRunning(string charName);
    }

    public interface IDuplexServiceCallbackTCP
    {

        [OperationContract(IsOneWay = true)]
        void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage);

        [OperationContract]
        bool IsEveInstanceRunning(string charName);
    }

    [KnownType(typeof(BroadcastMessage))]


    public class OperationContract : IOperationContractTCP
    {

        private static ConcurrentDictionary<IDuplexServiceCallbackTCP, object> _callbacks;

        public OperationContract()
        {
            _callbacks = new ConcurrentDictionary<IDuplexServiceCallbackTCP, object>();
        }

        private IDuplexServiceCallbackTCP Callback => OperationContext.Current.GetCallbackChannel<IDuplexServiceCallbackTCP>();

        public void Ping()
        {
            //Debug.WriteLine("WCF TCP Server PING!");
        }

        private static bool IsCallBackStillAlive(IDuplexServiceCallbackTCP callback)
        {
            try
            {
                if (((ICommunicationObject)callback).State != CommunicationState.Opened)
                {
                    _callbacks.TryRemove(callback, out _);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
            return true;
        }

        public void SendBroadcastMessage(BroadcastMessage broadcastMessage)
        {
            try
            {
                foreach (var callback in _callbacks.ToList())
                {
                    if (callback.Key == Callback)
                        continue;

                    try
                    {
                        if (!IsCallBackStillAlive(callback.Key))
                            continue;

                        callback.Key.ReceiveBroadcastMessage(broadcastMessage);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void RegisterCallbackHandler()
        {
            if (!_callbacks.ContainsKey(Callback))
            {
                _callbacks.TryAdd(Callback, null);
                Cache.Instance.Log($"Registered [{Callback}] Amount of callbacks stored [{_callbacks.Count}]");
            }
        }

        public bool IsEveInstanceRunning(string charName)
        {

            try
            {
                foreach (var callback in _callbacks)
                {
                    if (callback.Key == Callback)
                        continue;

                    if (!IsCallBackStillAlive(callback.Key))
                        continue;

                    try
                    {
                        if (callback.Key.IsEveInstanceRunning(charName))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex}");
            }
            return false;
        }
    }
    /// <summary>
    /// EvesharpLauncher Tcp server <-> EveSharpLauncher Tcp client
    /// </summary>
    public class WCFServerTCP
    {
        private static readonly WCFServerTCP _instance = new WCFServerTCP();
        private ServiceHost host;

        public WCFServerTCP()
        {

        }

        public static WCFServerTCP Instance => _instance;

        public bool StartWCFServer()
        {
            if (host != null)
                return false;
            var thread = new Thread(() =>
            {

                try
                {
                    host = new ServiceHost(typeof(OperationContract),
                        new Uri[] { new Uri($"net.tcp://{Cache.Instance.EveSettings.RemoteWCFIpAddress}:{Cache.Instance.EveSettings.RemoteWCFPort}/EveSharp") });

                    ((ServiceBehaviorAttribute)host.Description.Behaviors[typeof(ServiceBehaviorAttribute)]).InstanceContextMode =
                        InstanceContextMode.Single;
                    ((ServiceBehaviorAttribute)host.Description.Behaviors[typeof(ServiceBehaviorAttribute)]).ConcurrencyMode = ConcurrencyMode.Multiple;
                    ((ServiceBehaviorAttribute)host.Description.Behaviors[typeof(ServiceBehaviorAttribute)]).MaxItemsInObjectGraph = int.MaxValue; // max size = 2048mb
                    host.Description.Behaviors.Remove(typeof(ServiceDebugBehavior));
                    host.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
                    var binding = new NetTcpBinding();
                    binding.Security.Mode = SecurityMode.None;
                    binding.CloseTimeout = TimeSpan.FromSeconds(5);
                    binding.OpenTimeout = TimeSpan.FromSeconds(5);
                    binding.ReceiveTimeout = TimeSpan.FromSeconds(5);
                    binding.SendTimeout = TimeSpan.FromSeconds(5);
                    //binding.TransferMode = TransferMode.Streamed;
                    binding.MaxReceivedMessageSize = 2147483647;
                    binding.MaxBufferSize = 2147483647;
                    //binding.ReliableSession.Enabled = true;
                    //binding.ReceiveTimeout = TimeSpan.MaxValue;
                    //binding.SendTimeout = TimeSpan.MaxValue;
                    //binding.ReliableSession.InactivityTimeout = TimeSpan.MaxValue;
                    host.AddServiceEndpoint(typeof(IOperationContractTCP), binding, "");
                    ServiceThrottlingBehavior stb = new ServiceThrottlingBehavior
                    {
                        MaxConcurrentSessions = 500,
                        MaxConcurrentCalls = 500,
                        MaxConcurrentInstances = 500,
                    };
                    host.Description.Behaviors.Add(stb);
                    host.Open();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            });
            thread.Start();
            return true;
        }

        public void StopWCFServer()
        {
            try
            {
                host?.Close();
                host = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}