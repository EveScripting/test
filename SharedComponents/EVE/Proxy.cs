using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text.RegularExpressions;
using System.Threading;
using EasyHook;
using SharedComponents.CurlUtil;
using SharedComponents.IPC;
using SharedComponents.Utility;

namespace SharedComponents.EVE
{
    [Serializable]
    public class Proxy : ViewModelBase
    {
        public Proxy(string ip, string port, string username, string password, ConcurrentBindingList<Proxy> list)
        {
            Id = list.Any() ? list.Max(p => p.Id) + 1 : 1;
            Ip = ip;
            Port = port;
            Username = username;
            Password = password;
            Description = Username + "@" + Ip;
        }

        public Proxy()
        {
        }

        [ReadOnly(true)]
        public int Id
        {
            get { return GetValue(() => Id); }
            set { SetValue(() => Id, value); }
        }

        public string Description
        {
            get { return GetValue(() => Description); }
            set { SetValue(() => Description, value); }
        }

        public string Ip
        {
            get { return GetValue(() => Ip); }
            set { SetValue(() => Ip, value); }
        }

        public string Port
        {
            get { return GetValue(() => Port); }
            set { SetValue(() => Port, value); }
        }

        public string Username
        {
            get { return GetValue(() => Username); }
            set { SetValue(() => Username, value); }
        }

        public string Password
        {
            get { return GetValue(() => Password); }
            set { SetValue(() => Password, value); }
        }

        [ReadOnly(true)]
        public DateTime LastFail
        {
            get { return GetValue(() => LastFail); }
            set { SetValue(() => LastFail, value); }
        }

        [ReadOnly(true)]
        public DateTime LastCheck
        {
            get { return GetValue(() => LastCheck); }
            set { SetValue(() => LastCheck, value); }
        }

        [ReadOnly(true)]
        public bool IsAlive
        {
            get { return GetValue(() => IsAlive); }
            set { SetValue(() => IsAlive, value); }
        }

        [ReadOnly(true)]
        //        public bool TorExit { get { return GetValue(() => TorExit); } set { SetValue(() => TorExit, value); } }
        //        [ReadOnly(true)]
        public string ExtIp
        {
            get { return GetValue(() => ExtIp); }
            set { SetValue(() => ExtIp, value); }
        }

        [ReadOnly(true)]
        public string LinkedAccounts
        {
            get { return GetValue(() => LinkedAccounts); }
            set { SetValue(() => LinkedAccounts, value); }
        }

        public string Notes
        {
            get { return GetValue(() => Notes); }
            set { SetValue(() => Notes, value); }
        }

        public String GetIpPort()
        {
            return Ip + ":" + Port;
        }

        public String GetUserPassword()
        {
            return Username + ":" + Password;
        }


        public bool IsValid => Regex.Match(Ip, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b").Success;


        public String GetHashcode()
        {
            return GetIpPort() + "@" + GetUserPassword();
        }

        public String GetExternalIp(long timeout = 60L)
        {

            if (!IsValid)
                return string.Empty;

            using (var cw = new CurlWorker())
            {
                return cw.GetPostPage("https://whoer.net/ip", string.Empty, GetIpPort(), GetUserPassword(), timeout: timeout);
            }
        }
        //
        //        public void CheckTorExit()
        //        {
        //            this.TorExit = CurlWorker.CheckTorExit(this.GetIpPort(), this.GetUserPassword());
        //        }

        public bool CheckInternetConnectivity()
        {
            return IsValid && CurlWorker.CheckInternetConnectivity(GetIpPort(), GetUserPassword());
        }

        public void Clear()
        {
            IsAlive = false;
            LastCheck = DateTime.MinValue;
            LastFail = DateTime.MinValue;
            ExtIp = string.Empty;
            LinkedAccounts = string.Empty;
            //            this.TorExit = false;
        }
    }
}