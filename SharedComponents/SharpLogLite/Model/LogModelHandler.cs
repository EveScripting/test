using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SharedComponents.Utility;

/*
 * User: duketwo - https://github.com/duketwo/
 * Date: 10.11.2016
 */

namespace SharedComponents.SharpLogLite.Model
{
    public class LogModelHandler : IDisposable
    {
        public delegate void SharpLogMessageDelegate(SharpLogMessage msg);

        private static int VERSION = 2;
        private Socket handler;
        private LogSeverity LogSeverity;

        private ManualResetEvent ManualResetEvent;
        private ulong? Pid;
        private static List<Regex> FILTERED_MESSAGES = new List<Regex>()
        {
          new Regex("Warping got a None planet ball"),
          new Regex("Tried to add an item that is already there"),
          new Regex("FIXUP: godma is using an old thrown away invCacheContainer"),
          new Regex("Client is using a session bound remote object while"),
          new Regex("Discarded  \\d*  messages"),
          new Regex("self destination path 0 is own solarsystem, picking next node instead."),
          new Regex("Retrying \\(Retry\\(total=4.*renew.*token"),
          new Regex("CombatMessage::ShowMsg - Discarded \\d* combat UI messages"),
          new Regex("Exception report throttled"),
          new Regex("curl_easy_perform returned error code"),
          new Regex("Audio listener was requested to be created before audio was initialized!"),
          new Regex("Sentry send failed:*.*10057"),
        };

        public LogModelHandler(ManualResetEvent manualResetEvent, LogSeverity logSeverity)
        {
            ManualResetEvent = manualResetEvent;
            Pid = null;
            LogSeverity = logSeverity;
        }

        public void Dispose()
        {
            if (handler != null)
                handler.Close();
        }

        public static event SharpLogMessageDelegate OnMessage;

        public void AcceptCallback(IAsyncResult ar)
        {
            Debug.WriteLine("Accepting a new connection...");
            var listener = (Socket)ar.AsyncState;

            try
            {
                handler = listener.EndAccept(ar);
                ManualResetEvent.Set();
                var state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine("Socket has been closed.");
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            try
            {
                var read = handler.EndReceive(ar);
                if (read > 0)
                {
                    var msg = Util.ByteArrayToStructure<RawLogMessage>(state.buffer);

                    if (msg.Type == MessageType.CONNECTION_MESSAGE)
                    {
                        Pid = msg.ConnectionMessage.Pid;

                        Debug.WriteLine(String.Format("Accepted a new connection from PID {0}.", Pid));

                        if (msg.ConnectionMessage.Version > VERSION)
                        {
                            Debug.WriteLine(String.Format("Error: Client using a newer verison: {0}.", msg.ConnectionMessage.Version));
                            Dispose();
                        }
                    }

                    if (Pid == null && msg.Type != MessageType.CONNECTION_MESSAGE)
                    {
                        Debug.WriteLine("Error: Initial CONNECTION_MESSAGE message was not received.");
                        Dispose();
                    }

                    if (msg.Type == MessageType.SIMPLE_MESSAGE ||
                        msg.Type == MessageType.LARGE_MESSAGE)
                        state.sharpLogMessage = new SharpLogMessage(
                            Util.Unix2DateTime(msg.TextMessage.Timestamp),
                            msg.TextMessage.Severity,
                      Helper.ByteArrayToStringNullTerminated(state.buffer.Skip(20).Take(32).ToArray()),
                      Helper.ByteArrayToStringNullTerminated(state.buffer.Skip(52).Take(32).ToArray()),
                      Helper.ByteArrayToStringNullTerminated(state.buffer.Skip(84).Take(256).ToArray()),
                          (long)Pid
                        );

                    if (msg.Type == MessageType.CONTINUATION_MESSAGE)
                        state.sharpLogMessage.Message += Helper.ByteArrayToStringNullTerminated(state.buffer.Skip(84).Take(256).ToArray());

                    if (msg.Type == MessageType.CONTINUATION_END_MESSAGE)
                        state.sharpLogMessage.Message += Helper.ByteArrayToStringNullTerminated(state.buffer.Skip(84).Take(256).ToArray());

                    if ((msg.Type == MessageType.SIMPLE_MESSAGE ||
                        msg.Type == MessageType.CONTINUATION_END_MESSAGE) && state.sharpLogMessage.Severity >= LogSeverity)
                    {
                        var msgCopy = state.sharpLogMessage.Copy();
                        Task.Run(() =>
                                {
                                    try
                                    {
                                        if (!FILTERED_MESSAGES.Any(f => f.Match(msgCopy.Message).Success))
                                        {
                                            OnMessage?.Invoke(msgCopy);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine(e);
                                    }
                                });
                    }

                    state.buffer = new byte[StateObject.BufferSize];
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
                else
                {
                    Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("Exception: {0}", e));
                Dispose();
            }
        }
    }
}