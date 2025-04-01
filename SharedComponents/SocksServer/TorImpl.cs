/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 20.10.2016
 * Time: 19:31
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Diagnostics;
using System.Linq;
using SharedComponents.EVE;
using Tor;
using Tor.Config;

namespace EVESharpLauncher.SocksServer
{
    /// <summary>
    ///     Description of Tor.
    /// </summary>
    public class TorImpl
    {
        private static TorImpl _instance;
        private static Tor.Client _client;

        static TorImpl()
        {
            _instance = new TorImpl();
            _client = null;
        }

        public TorImpl()
        {
        }

        public static TorImpl Instance => _instance;

        public void StartTorSocksProxy()
        {
            if (_client != null && _client.IsRunning)
                return;

            Log("Starting Tor Socks Server on port 127.0.0.1:15001.");

            try
            {
                var createParams = new ClientCreateParams();
                createParams.ControlPassword = "";
                createParams.ControlPort = 9051;
                createParams.Path = @".\Resources\Tor\Tor\Tor.exe";
                createParams.DefaultConfigurationFile = @"defaults";
                createParams.SetConfig(ConfigurationNames.SocksPort, 15001);
                createParams.SetConfig(ConfigurationNames.AvoidDiskWrites, true);
                createParams.SetConfig(ConfigurationNames.GeoIPFile, @".\..\Data\Tor\geoip");
                createParams.SetConfig(ConfigurationNames.GeoIPv6File, @".\..\Data\Tor\geoip6");
                _client = Tor.Client.Create(createParams);
                _client.Logging.InfoReceived += delegate(object sender, LogEventArgs args) { Log("TorLog: " + args.Message); };
                _client.Logging.DebugReceived += delegate(object sender, LogEventArgs args) { Log("TorLog: " + args.Message); };
                _client.Logging.ErrorReceived += delegate(object sender, LogEventArgs args) { Log("TorLog: " + args.Message); };
                _client.Logging.WarnReceived += delegate(object sender, LogEventArgs args) { Log("TorLog: " + args.Message); };
                _client.Logging.NoticeReceived += delegate(object sender, LogEventArgs args) { Log("TorLog: " + args.Message); };

                var prx = Cache.Instance.EveSettings.Proxies.FirstOrDefault(p => p.Ip.Equals("127.0.0.1") && p.Port.Equals("15001"));
                if (prx == null)
                {
                    Cache.Instance.Log("Adding Tor-proxy to the list of proxies.");
                    Cache.Instance.EveSettings.Proxies.Add(new Proxy("127.0.0.1", "15001", "", "", Cache.Instance.EveSettings.Proxies));
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private void Log(string msg)
        {
            Cache.Instance.Log(msg);
        }

        public void StopTorSocksProxy()
        {
            if (_client != null)
            {
                Log("Stopping Tor Socks Server on port 127.0.0.1:15001.");
                _client.Dispose();
                _client = null;

                foreach (var p in Process.GetProcesses())
                    if (p.ProcessName.ToLower() == "tor")
                        try
                        {
                            p.Kill();
                        }
                        catch (Exception)
                        {
                        }
            }
        }
    }
}