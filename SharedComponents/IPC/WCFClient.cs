/*
 * ---------------------------------------
 * User: duketwo
 * Date: 08.04.2014
 * Time: 11:50
 * 
 * ---------------------------------------
 */

using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharedComponents.IPC

{
    /// <summary>
    ///  This runs in every eve instance and connects to the launcher (WCFServer)
    /// </summary>
    public class WCFClient
    {
        private static readonly WCFClient _instance = new WCFClient();
        private ChannelFactory<IOperationContract> pipeFactory;
        private IOperationContract pipeProxy;

        public WCFClient()
        {
        }

        public string pipeName { get; set; }
        private IDuplexServiceCallback DuplexServiceCallbackInst { get; set; }
        private bool _reconnect { get; set; }
        private string _charname { get; set; }

        

        public IOperationContract GetPipeProxy
        {
            get
            {
                try
                {

                    if (pipeFactory == null || pipeProxy == null || _reconnect)
                        CreateChannel();
                    pipeProxy.Ping();
                }
                catch (CommunicationObjectFaultedException)
                {
                    CreateChannel();
                }
                catch (CommunicationException)
                {
                    CreateChannel();
                }

                catch (PipeException)
                {
                    CreateChannel();
                }

                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e);
                }

                return pipeProxy;
            }
        }

        public static WCFClient Instance => _instance;

        public void SetCallBackService(IDuplexServiceCallback serviceCallback, string charname)
        {
            DuplexServiceCallbackInst = serviceCallback;
            _reconnect = true;
            _charname = charname;
        }

        private void CreateChannel()
        {
            try
            {
                var binding = new NetNamedPipeBinding();
                binding.MaxReceivedMessageSize = 2147483647;
                binding.MaxBufferSize = 2147483647;
                var cb = DuplexServiceCallbackInst != null ? DuplexServiceCallbackInst : new DuplexServiceCallback();


                var callbackContext = new InstanceContext(cb);

                pipeFactory = new DuplexChannelFactory<IOperationContract>(callbackContext, binding, new EndpointAddress("net.pipe://localhost/" + pipeName));

                foreach (OperationDescription op in pipeFactory.Endpoint.Contract.Operations)
                {
                    var dataContractBehavior = op.Behaviors.Find<DataContractSerializerOperationBehavior>();
                    if (dataContractBehavior != null)
                    {
                        dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
                    }
                }

                pipeProxy = pipeFactory.CreateChannel();
                ClientBase<IOperationContract>.CacheSetting = System.ServiceModel.CacheSetting.AlwaysOn;

                if (!string.IsNullOrEmpty(_charname))
                    pipeProxy.AttachCallbackHandlerToEveAccount(_charname);

                if (_reconnect)
                    _reconnect = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [CallbackBehavior(UseSynchronizationContext = false)]
        public class DuplexServiceCallback : IDuplexServiceCallback
        {
            public void OnCallback()
            {
            }

            #region Implementation of IDuplexServiceCallback

            public void GotoHomebaseAndIdle()
            {
            }

            public void RestartEveSharpCore()
            {
            }

            public void GotoJita()
            {
            }

            public void PauseAfterNextDock()
            {
            }


            public void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
            {

            }

            #endregion
        }
    }
}