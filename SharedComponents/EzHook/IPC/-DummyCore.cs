using System.Diagnostics;

namespace EasyHook.IPC
{
    /// <summary>
    ///     Represents EasyHook's (future) CoreClass/DomainManager/...
    /// </summary>
    public static class DummyCore
    {
        static DummyCore()
        {
            ConnectionManager = new ConnectionManager();
        }

        public static ConnectionManager ConnectionManager { get; set; }

        public static void StartRemoteProcess(string exe)
        {
            var channelUrl = ConnectionManager.InitializeInterDomainConnection();
            Process.Start(exe, channelUrl);
        }

        public static void InitializeAsRemoteProcess(string channelUrl)
        {
            ConnectionManager.ConnectInterDomainConnection(channelUrl);
        }
    }
}