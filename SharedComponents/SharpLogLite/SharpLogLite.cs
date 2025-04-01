using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SharedComponents.SharpLogLite.Model;

/*
 * User: duketwo - https://github.com/duketwo/
 * Date: 10.11.2016
 */

namespace SharedComponents.SharpLogLite
{
    public class SharpLogLite : IDisposable
    {
        private static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
        private bool IsClosed;
        private Socket Listener;
        private LogSeverity LogSeverity;

        public SharpLogLite(LogSeverity severity)
        {
            LogSeverity = severity;
        }


        public void Dispose()
        {
            try
            {
                Listener.Close();
                IsClosed = true;
                ManualResetEvent.Set();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public void StartListening()
        {
            var localEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3273);
            Debug.WriteLine("Local address and port : {0}", localEP.ToString());

            Listener = new Socket(localEP.Address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Listener.Bind(localEP);
                Listener.Listen(20);

                while (true && !IsClosed)
                    try
                    {
                        ManualResetEvent.Reset();
                        Debug.WriteLine("Waiting for a connection...");
                        Listener.BeginAccept(
                            new AsyncCallback(new LogModelHandler(ManualResetEvent, LogSeverity).AcceptCallback),
                            Listener);
                        ManualResetEvent.WaitOne();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(String.Format("Exception: {0}", e));
                        break;
                    }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            finally
            {
                if (Listener != null)
                    Listener.Close();
            }

            Debug.WriteLine("Closing the listener...");
        }
    }
}