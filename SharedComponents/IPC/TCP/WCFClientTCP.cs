using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedComponents.EVE;

namespace SharedComponents.IPC.TCP

{
    /// <summary>
    ///     Description of WCFClientTCP.
    /// </summary>
    public class WCFClientTCP
    {
        private static readonly WCFClientTCP _instance = new WCFClientTCP();
        private ChannelFactory<IOperationContractTCP> pipeFactory;
        private IOperationContractTCP pipeProxy;
        /// <summary>
        /// EvesharpLauncher Tcp server <-> EveSharpLauncher Tcp client
        /// </summary>
        public WCFClientTCP()
        {
        }

        private bool _reconnect { get; set; }

        private Task _connectTask = null;

        public IOperationContractTCP GetPipeProxy
        {
            get
            {
                try
                {

                    if (_connectTask == null)
                    {
                        _connectTask = Task.Run(async () =>
                        {

                            try
                            {
                                while (true)
                                {
                                    //Debug.WriteLine("GetPipeProxy");
                                    await Task.Delay(1500);
                                    var p = this.GetPipeProxy;
                                    //p.Ping();
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                            }

                        });
                    }

                    if (pipeFactory == null || pipeProxy == null || _reconnect)
                        CreateChannel();
                    pipeProxy.Ping();
                }
                catch (CommunicationObjectFaultedException e)
                {
                    Debug.WriteLine("Exception: " + e);
                    CreateChannel();

                }
                catch (CommunicationException e)
                {
                    Debug.WriteLine("Exception: " + e);
                    CreateChannel();
                }

                catch (PipeException e)
                {
                    Debug.WriteLine("Exception: " + e);
                    CreateChannel();
                }

                catch (Exception e)
                {
                    Debug.WriteLine("Exception: " + e);
                }

                return pipeProxy;
            }
        }

        public static WCFClientTCP Instance => _instance;


        private void CreateChannel()
        {
            try
            {
                var binding = new NetTcpBinding();
                binding.Security.Mode = SecurityMode.None;
                binding.CloseTimeout = TimeSpan.FromSeconds(5);
                binding.OpenTimeout = TimeSpan.FromSeconds(5);
                binding.ReceiveTimeout = TimeSpan.FromSeconds(5);
                binding.SendTimeout = TimeSpan.FromSeconds(5);
                binding.MaxReceivedMessageSize = 2147483647;
                //binding.ReceiveTimeout = TimeSpan.MaxValue;
                binding.MaxBufferSize = 2147483647;
                //binding.ReliableSession.Enabled = true;
                //binding.SendTimeout = TimeSpan.MaxValue;
                //binding.ReliableSession.InactivityTimeout = TimeSpan.MaxValue;
                //binding.TransferMode = TransferMode.Streamed;
                var cb = new WCFClientTCPCallback();
                var callbackContext = new InstanceContext(cb);

                pipeFactory = new DuplexChannelFactory<IOperationContractTCP>(callbackContext, binding, new EndpointAddress($"net.tcp://{Cache.Instance.EveSettings.RemoteWCFIpAddress}:{Cache.Instance.EveSettings.RemoteWCFPort}/EveSharp"));

                foreach (OperationDescription op in pipeFactory.Endpoint.Contract.Operations)
                {
                    var dataContractBehavior = op.Behaviors.Find<DataContractSerializerOperationBehavior>();
                    if (dataContractBehavior != null)
                    {
                        dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
                    }
                }

                pipeProxy = pipeFactory.CreateChannel();
                pipeProxy.RegisterCallbackHandler();
                ClientBase<IOperationContractTCP>.CacheSetting = System.ServiceModel.CacheSetting.AlwaysOn;

                Cache.Instance.Log("WCF Tcp client connected to: " + $"net.tcp://{Cache.Instance.EveSettings.RemoteWCFIpAddress}:{Cache.Instance.EveSettings.RemoteWCFPort}/EveSharp");

                if (_reconnect)
                    _reconnect = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}