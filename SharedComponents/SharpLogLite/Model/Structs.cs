using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

/*
 * User: duketwo - https://github.com/duketwo/
 * Date: 10.11.2016
 */

namespace SharedComponents.SharpLogLite.Model
{
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct TextMessage
    {
        [FieldOffset(0), MarshalAs(UnmanagedType.U8)]
        public UInt64 Timestamp;
        [FieldOffset(8), MarshalAs(UnmanagedType.U4)]
        public LogSeverity Severity;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct ConnectionMessage
    {
        [FieldOffset(0)]
        public UInt32 Version;
        [FieldOffset(8)]
        public UInt64 Pid;

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Version, this.Pid);
        }
    }


    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct RawLogMessage
    {
        [FieldOffset(0)]
        public MessageType Type;
        [FieldOffset(8)]
        public TextMessage TextMessage;
        [FieldOffset(8)]
        public ConnectionMessage ConnectionMessage;
    }

    public struct SharpLogMessage
    {
        public DateTime DateTime;
        public LogSeverity Severity;
        public string Module;
        public string Channel;
        public string Message;
        public long Pid;

        public SharpLogMessage(DateTime dateTime, LogSeverity severity, string module, string channel, string message, long pid)
        {
            this.DateTime = dateTime;
            this.Severity = severity;
            this.Module = module;
            this.Channel = channel;
            this.Message = message;
            this.Pid = pid;
        }

        public SharpLogMessage Copy()
        {
            return new SharpLogMessage(this.DateTime, this.Severity, this.Module, this.Channel, this.Message, this.Pid);
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2} {3} {4}", this.DateTime, this.Severity, this.Module, this.Channel, this.Message);
        }
    }

    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 344;
        public byte[] buffer = new byte[BufferSize];
        public SharpLogMessage sharpLogMessage;
    }

}
