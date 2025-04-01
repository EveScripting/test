using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;

namespace SharedComponents.Utility
{
    public class TcpConnectionInfo
    {
        public IPAddress LocalIPAddress { get; }
        public int LocalPort { get; }
        public IPAddress RemoteIPAddress { get; }
        public int RemotePort { get; }
        public int ProcessId { get; }

        public TcpConnectionInfo(IPAddress localIPAddress, int localPort, IPAddress remoteIPAddress, int remotePort,
            int processId)
        {
            LocalIPAddress = localIPAddress;
            LocalPort = localPort;
            RemoteIPAddress = remoteIPAddress;
            RemotePort = remotePort;
            ProcessId = processId;
        }
    }

    public class TcpConnectionManager
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tcpTableLength, bool sort, int ipVersion,
            TcpTableType tcpTableType, int reserved);

        public enum TcpTableType
        {
            BasicListener,
            BasicConnections,
            BasicAll,
            OwnerPidListener,
            OwnerPidConnections,
            OwnerPidAll,
            OwnerModuleListener,
            OwnerModuleConnections,
            OwnerModuleAll,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TcpTable
        {
            public uint length;
            public TcpRow row;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TcpRow
        {
            public uint state;
            public uint localAddr;
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            public uint remoteAddr;
            public byte remotePort1;
            public byte remotePort2;
            public byte remotePort3;
            public byte remotePort4;
            public int owningPid;
        }

        public IEnumerable<TcpConnectionInfo> GetTcpConnections()
        {
            List<TcpConnectionInfo> connections = new List<TcpConnectionInfo>();

            IntPtr tcpTable = IntPtr.Zero;
            int tcpTableLength = 0;

            if (GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, 2, TcpTableType.OwnerPidAll, 0) != 0)
            {
                try
                {
                    tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                    if (GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, 2, TcpTableType.OwnerPidAll, 0) == 0)
                    {
                        TcpTable table = (TcpTable)Marshal.PtrToStructure(tcpTable, typeof(TcpTable));

                        IntPtr rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            TcpRow row = (TcpRow)Marshal.PtrToStructure(rowPtr, typeof(TcpRow));

                            IPAddress localIPAddress = new IPAddress(row.localAddr);
                            int localPort = (row.localPort1 << 8) + row.localPort2;
                            IPAddress remoteIPAddress = new IPAddress(row.remoteAddr);
                            int remotePort = (row.remotePort1 << 8) + row.remotePort2;
                            int processId = row.owningPid;

                            connections.Add(new TcpConnectionInfo(localIPAddress, localPort, remoteIPAddress,
                                remotePort, processId));

                            rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(TcpRow)));
                        }
                    }
                }
                finally
                {
                    if (tcpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tcpTable);
                    }
                }
            }

            return connections;
        }
    }
}