/*
/*
 * ---------------------------------------
 * User: duketwo
 * Date: 24.12.2013
 * Time: 16:26
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Generic;
using SharedComponents.SharpLogLite.Model;
using SharedComponents.Utility;

namespace SharedComponents.EVE
{
    /// <summary>
    ///     Description of EveAccountData.
    /// </summary>
    [Serializable]
    public class EveSetting : ViewModelBase
    {
        public EveSetting(string eveDirectory, DateTime last24HourTS)
        {
            Last24HourTS = last24HourTS;
            Proxies = new ConcurrentBindingList<Proxy>();
            DatagridViewHiddenColumns = new List<int>();
            EveDirectory = eveDirectory;
        }

        public EveSetting()
        {
            Proxies = new ConcurrentBindingList<Proxy>();
        }

        public string EveDirectory
        {
            get { return GetValue(() => EveDirectory); }
            set { SetValue(() => EveDirectory, value); }
        }

        public bool RecordingEnabled
        {
            get { return GetValue(() => RecordingEnabled); }
            set { SetValue(() => RecordingEnabled, value); }
        }

        public string RecordingDirectory
        {
            get { return GetValue(() => RecordingDirectory); }
            set { SetValue(() => RecordingDirectory, value); }
        }

        public int VideoRotationMaximumSizeGB
        {
            get { return GetValue(() => VideoRotationMaximumSizeGB); }
            set { SetValue(() => VideoRotationMaximumSizeGB, value); }
        }

        public DateTime LastHourTS
        {
            get { return GetValue(() => LastHourTS); }
            set { SetValue(() => LastHourTS, value); }
        }

        public DateTime Last24HourTS
        {
            get { return GetValue(() => Last24HourTS); }
            set { SetValue(() => Last24HourTS, value); }
        }

        public DateTime LastBackupXMLTS
        {
            get { return GetValue(() => LastBackupXMLTS); }
            set { SetValue(() => LastBackupXMLTS, value); }
        }


        public bool DisableTLSVerifcation
        {
            get { return GetValue(() => DisableTLSVerifcation); }
            set { SetValue(() => DisableTLSVerifcation, value); }
        }

        public string FireFoxDirectory
        {
            get { return GetValue(() => FireFoxDirectory); }
            set { SetValue(() => FireFoxDirectory, value); }
        }

        public string Pastebin
        {
            get { return GetValue(() => Pastebin); }
            set { SetValue(() => Pastebin, value); }
        }

        public string WCFPipeName
        {
            get { return GetValue(() => WCFPipeName); }
            set { SetValue(() => WCFPipeName, value); }
        }

        public string GmailUser
        {
            get { return GetValue(() => GmailUser); }
            set { SetValue(() => GmailUser, value); }
        }

        public string GmailPassword
        {
            get { return GetValue(() => GmailPassword); }
            set { SetValue(() => GmailPassword, value); }
        }

        public string ReceiverEmailAddress
        {
            get { return GetValue(() => ReceiverEmailAddress); }
            set { SetValue(() => ReceiverEmailAddress, value); }
        }

        public bool UseTorSocksProxy
        {
            get { return GetValue(() => UseTorSocksProxy); }
            set { SetValue(() => UseTorSocksProxy, value); }
        }

        public bool AutoUpdateEve
        {
            get { return GetValue(() => AutoUpdateEve); }
            set { SetValue(() => AutoUpdateEve, value); }
        }

        public bool AutoStartScheduler
        {
            get { return GetValue(() => AutoStartScheduler); }
            set { SetValue(() => AutoStartScheduler, value); }
        }

        public bool BlockEveTelemetry
        {
            get { return GetValue(() => BlockEveTelemetry); }
            set { SetValue(() => BlockEveTelemetry, value); }
        }

        public bool UseLegacyLogin
        {
            get { return GetValue(() => UseLegacyLogin); }
            set { SetValue(() => UseLegacyLogin, value); }
        }

        public bool AlwaysClearNonEveSharpCCPData
        {
            get { return GetValue(() => AlwaysClearNonEveSharpCCPData); }
            set { SetValue(() => AlwaysClearNonEveSharpCCPData, value); }
        }

        public ConcurrentBindingList<Proxy> Proxies
        {
            get { return GetValue(() => Proxies); }
            set { SetValue(() => Proxies, value); }
        }

        public List<int> DatagridViewHiddenColumns
        {
            get { return GetValue(() => DatagridViewHiddenColumns); }
            set { SetValue(() => DatagridViewHiddenColumns, value); }
        }

        public int BackgroundFPS
        {
            get { return GetValue(() => BackgroundFPS); }
            set { SetValue(() => BackgroundFPS, value); }
        }

        public int TimeBetweenEVELaunchesMin
        {
            get { return GetValue(() => TimeBetweenEVELaunchesMin); }
            set { SetValue(() => TimeBetweenEVELaunchesMin, value); }
        }
        public int TimeBetweenEVELaunchesMax
        {
            get { return GetValue(() => TimeBetweenEVELaunchesMax); }
            set { SetValue(() => TimeBetweenEVELaunchesMax, value); }
        }

        public int BackgroundFPSMin => 15;

        public int BackgroundFPSMax => 30;

        public LogSeverity SharpLogLiteLogSeverity
        {
            get { return GetValue(() => SharpLogLiteLogSeverity); }
            set { SetValue(() => SharpLogLiteLogSeverity, value); }
        }

        public RecorderEncoderSetting RecorderEncoderSetting
        {
            get { return GetValue(() => RecorderEncoderSetting); }
            set { SetValue(() => RecorderEncoderSetting, value); }
        }

        public bool RemoteWCFServer
        {
            get { return GetValue(() => RemoteWCFServer); }
            set { SetValue(() => RemoteWCFServer, value); }
        }

        public bool RemoteWCFClient
        {
            get { return GetValue(() => RemoteWCFClient); }
            set { SetValue(() => RemoteWCFClient, value); }
        }

        public string RemoteWCFIpAddress
        {
            get { return GetValue(() => RemoteWCFIpAddress); }
            set { SetValue(() => RemoteWCFIpAddress, value); }
        }

        public string RemoteWCFPort
        {
            get { return GetValue(() => RemoteWCFPort); }
            set { SetValue(() => RemoteWCFPort, value); }
        }

    }
}