/*
 * ---------------------------------------
 * User: duketwo
 * Date: 21.06.2014
 * Time: 11:00
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;
using SharedComponents.EVEAccountCreator;
using SharedComponents.IPC;
using SharedComponents.Utility;
using SharedComponents.Extensions;

namespace SharedComponents.EVE
{
    /// <summary>
    ///     Description of HWSettings.
    /// </summary>
    [Serializable]
    public class HWSettings : ViewModelBase
    {
        public HWSettings(ulong totalPhysRam, string windowsUserLogin, string computername, string windowsKey, string networkAdapterGuid, string networkAddress,
            string macAddress, string processorIdent, string processorRev, string processorCoreAmount,
            string processorLevel, string gpuDescription, uint gpuDeviceId, uint gpuVendorId, uint gpuRevision,
            long gpuDriverversion, string gpuDriverversionInt, string gpuIdentifier, int proxyId,
            string machineGuid, ulong systemReservedMemory, string launcherMachineHash, DateTime gpuDriverDate, string gpuManufacturer, uint gpuDedicatedMemoryMB, string networkAdapterName, string monitorName, int monitorWidth, int monitorHeight, int refreshRate,
            bool redirectCoreSettings
            )
        {
            TotalPhysRam = totalPhysRam;
            WindowsUserLogin = windowsUserLogin;
            Computername = computername;
            WindowsKey = windowsKey;
            NetworkAdapterGuid = networkAdapterGuid;
            MacAddress = macAddress;
            NetworkAddress = networkAddress;
            ProcessorIdent = processorIdent;
            ProcessorRev = processorRev;
            ProcessorCoreAmount = processorCoreAmount;
            ProcessorLevel = processorLevel;
            GpuDescription = gpuDescription;
            GpuDeviceId = gpuDeviceId;
            GpuVendorId = gpuVendorId;
            GpuRevision = gpuRevision;
            GpuDriverversion = gpuDriverversion;
            GpuDriverversionInt = gpuDriverversionInt;
            GpuIdentifier = gpuIdentifier;
            ProxyId = proxyId;
            MachineGuid = machineGuid;
            SystemReservedMemory = systemReservedMemory;
            LauncherMachineHash = launcherMachineHash;
            GpuDriverDate = gpuDriverDate;
            GpuManufacturer = gpuManufacturer;
            GpuDedicatedMemoryMB = gpuDedicatedMemoryMB;
            NetworkAdapterName = networkAdapterName;
            MonitorName = monitorName;
            MonitorRefreshrate = refreshRate;
            MonitorHeight = monitorHeight;
            MonitorWidth = monitorWidth;
            RedirectCoreSettings = redirectCoreSettings;
        }


        public HWSettings()
        {
        }

        public bool IsValid()
        {
            if (String.IsNullOrEmpty(TotalPhysRam.ToString())
               || String.IsNullOrEmpty(WindowsUserLogin.ToString())
               || String.IsNullOrEmpty(Computername.ToString())
               || String.IsNullOrEmpty(WindowsKey.ToString())
               || String.IsNullOrEmpty(NetworkAdapterGuid.ToString())
               || String.IsNullOrEmpty(MacAddress.ToString())
               || String.IsNullOrEmpty(NetworkAddress.ToString())
               || String.IsNullOrEmpty(ProcessorIdent.ToString())
               || String.IsNullOrEmpty(ProcessorRev.ToString())
               || String.IsNullOrEmpty(ProcessorCoreAmount.ToString())
               || String.IsNullOrEmpty(ProcessorLevel.ToString())
               || String.IsNullOrEmpty(GpuDescription.ToString())
               || String.IsNullOrEmpty(GpuDeviceId.ToString())
               || String.IsNullOrEmpty(GpuVendorId.ToString())
               || String.IsNullOrEmpty(GpuRevision.ToString())
               || String.IsNullOrEmpty(GpuDriverversion.ToString())
               || String.IsNullOrEmpty(GpuDriverversionInt.ToString())
               || String.IsNullOrEmpty(GpuIdentifier.ToString())
               || String.IsNullOrEmpty(ProxyId.ToString())
               || String.IsNullOrEmpty(MachineGuid.ToString())
               || String.IsNullOrEmpty(SystemReservedMemory.ToString())
               || String.IsNullOrEmpty(LauncherMachineHash.ToString())
               || String.IsNullOrEmpty(GpuDriverDate.ToString())
               || String.IsNullOrEmpty(GpuManufacturer.ToString())
               || String.IsNullOrEmpty(GpuDedicatedMemoryMB.ToString())
               || String.IsNullOrEmpty(NetworkAdapterName.ToString())
               || String.IsNullOrEmpty(MonitorName.ToString())
               )
                return false;

            if (GpuDedicatedMemoryMB == 0)
                return false;

            if (SystemReservedMemory == 0)
                return false;

            if (MonitorHeight == 0 || MonitorWidth == 0 || MonitorRefreshrate == 0)
                return false;

            return true;
        }

        public ulong SystemReservedMemory
        {
            get { return GetValue(() => SystemReservedMemory); }
            set { SetValue(() => SystemReservedMemory, value); }
        }

        public string MachineGuid
        {
            get { return GetValue(() => MachineGuid); }
            set { SetValue(() => MachineGuid, value); }
        }

        public ulong TotalPhysRam
        {
            get { return GetValue(() => TotalPhysRam); }
            set { SetValue(() => TotalPhysRam, value); }
        }

        public string WindowsUserLogin
        {
            get { return GetValue(() => WindowsUserLogin); }
            set { SetValue(() => WindowsUserLogin, value); }
        }

        public string Computername
        {
            get { return GetValue(() => Computername); }
            set { SetValue(() => Computername, value); }
        }

        public string WindowsKey
        {
            get { return GetValue(() => WindowsKey); }
            set { SetValue(() => WindowsKey, value); }
        }

        public string NetworkAdapterGuid
        {
            get { return GetValue(() => NetworkAdapterGuid); }
            set { SetValue(() => NetworkAdapterGuid, value); }
        }

        public string NetworkAddress
        {
            get { return GetValue(() => NetworkAddress); }
            set { SetValue(() => NetworkAddress, value); }
        }

        public string MacAddress
        {
            get { return GetValue(() => MacAddress); }
            set { SetValue(() => MacAddress, value); }
        }

        public string NetworkAdapterName
        {
            get { return GetValue(() => NetworkAdapterName); }
            set { SetValue(() => NetworkAdapterName, value); }
        }

        public string ProcessorIdent
        {
            get { return GetValue(() => ProcessorIdent); }
            set { SetValue(() => ProcessorIdent, value); }
        }

        public string ProcessorRev
        {
            get { return GetValue(() => ProcessorRev); }
            set { SetValue(() => ProcessorRev, value); }
        }

        public string ProcessorCoreAmount
        {
            get { return GetValue(() => ProcessorCoreAmount); }
            set { SetValue(() => ProcessorCoreAmount, value); }
        }

        public string ProcessorLevel
        {
            get { return GetValue(() => ProcessorLevel); }
            set { SetValue(() => ProcessorLevel, value); }
        }

        public string GpuDescription
        {
            get { return GetValue(() => GpuDescription); }
            set { SetValue(() => GpuDescription, value); }
        }

        public uint GpuDeviceId
        {
            get { return GetValue(() => GpuDeviceId); }
            set { SetValue(() => GpuDeviceId, value); }
        }

        public uint GpuVendorId
        {
            get { return GetValue(() => GpuVendorId); }
            set { SetValue(() => GpuVendorId, value); }
        }

        public DateTime GpuDriverDate
        {
            get { return GetValue(() => GpuDriverDate); }
            set { SetValue(() => GpuDriverDate, value); }
        }

        public String GpuManufacturer
        {
            get { return GetValue(() => GpuManufacturer); }
            set { SetValue(() => GpuManufacturer, value); }
        }

        public uint GpuRevision
        {
            get { return GetValue(() => GpuRevision); }
            set { SetValue(() => GpuRevision, value); }
        }

        public ulong GpuDedicatedMemoryMB
        {
            get { return GetValue(() => GpuDedicatedMemoryMB); }
            set { SetValue(() => GpuDedicatedMemoryMB, value); }
        }

        public long GpuDriverversion
        {
            get { return GetValue(() => GpuDriverversion); }
            set { SetValue(() => GpuDriverversion, value); }
        }


        public string MonitorName
        {
            get { return GetValue(() => MonitorName); }
            set { SetValue(() => MonitorName, value); }
        }

        public int MonitorWidth
        {
            get { return GetValue(() => MonitorWidth); }
            set { SetValue(() => MonitorWidth, value); }
        }

        public int MonitorHeight
        {
            get { return GetValue(() => MonitorHeight); }
            set { SetValue(() => MonitorHeight, value); }
        }

        public int MonitorRefreshrate
        {
            get { return GetValue(() => MonitorRefreshrate); }
            set { SetValue(() => MonitorRefreshrate, value); }
        }

        public string GpuDriverversionInt
        {
            get { return GetValue(() => GpuDriverversionInt); }
            set
            {
                SetValue(() => GpuDriverversionInt, value);
                var currentVal = GpuDriverversion;
                try
                {
                    if (value.Contains("."))
                    {
                        var res = GPUDriverHelpers.ConvertGpuDriverStringToLong(value);
                        currentVal = res;
                        GpuDriverversion = currentVal;
                    }
                }
                catch (Exception ex)
                {
                    GpuDriverversion = currentVal;
                    Cache.Instance.Log("GpuDriverversionInt Exception: " + ex.ToString());
                }
            }
        }

        public string GpuIdentifier
        {
            get { return GetValue(() => GpuIdentifier); }
            set { SetValue(() => GpuIdentifier, value); }
        }

        public string LauncherMachineHash
        {
            get { return GetValue(() => LauncherMachineHash); }
            set { SetValue(() => LauncherMachineHash, value); }
        }

        public int ProxyId
        {
            get { return GetValue(() => ProxyId); }
            set { SetValue(() => ProxyId, value); }
        }

        [XmlIgnore]
        public Proxy Proxy
        {
            get
            {
                if (Cache.IsServer)
                    return Cache.Instance.EveSettings.Proxies.FirstOrDefault(p => p.Id == ProxyId);
                else
                    return WCFClient.Instance.GetPipeProxy.GetProxy(ProxyId);
            }
            set
            {
                try
                {
                    ProxyId = value.Id;
                }
                catch (Exception e)
                {
                    Cache.Instance.Log("Exception: " + e);
                }
            }
        }

        public bool RedirectCoreSettings
        {
            get { return GetValue(() => RedirectCoreSettings); }
            set { SetValue(() => RedirectCoreSettings, value); }
        }


        private string GenerateMacAddress()
        {
            var random = new Random();
            var array = new byte[6];
            random.NextBytes(array);
            var text = string.Concat((
                from byte_0 in array
                select string.Format("{0}-", byte_0.ToString("X2"))).ToArray<string>());
            return text.TrimEnd(new char[]
            {
                '-'
            });
        }

        private string GenerateWindowsKey()
        {
            var random = new Random();
            var text = string.Concat(new object[]
            {
                "00",
                random.Next(100, 1000),
                "-OEM-",
                random.Next(1000000, 9999999),
                "-00",
                random.Next(100, 999)
            });

            return text;
        }

        private string GenerateIpAddress()
        {
            var random = new Random();
            var text = string.Concat(new object[]
            {
                "192.168.",
                random.Next(0, 255),
                ".",
                random.Next(1, 255)
            });
            return text;
        }

        private string GenerateRamSize()
        {
            var random = new Random();
            var sizes = new string[] { "8192", "16384", "32768" };
            return sizes[random.Next(sizes.Length)];
        }

        public static void ParseHardwareDetailsFromDxDiagFolder()
        {
            string path = string.Empty;
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = false;
            DialogResult result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                path = folderDlg.SelectedPath;
                Environment.SpecialFolder root = folderDlg.RootFolder;


                GPUDetail.ParseGPUDetailsFromDxDiagFolder(path);
                ParseEthernetCardNamesFromDxDiagFolder(path);
                ParseMonitorInformationFromDxDiagFolder(path);
            }
        }


        public static void ParseEthernetCardNamesFromDxDiagFolder(string path)
        {
            foreach (string file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    var ret = ParseEthernetCardNameFromDxDiagFile(File.ReadAllText(file));
                    if (!String.IsNullOrEmpty(ret))
                    {
                        Debug.WriteLine(ret);
                    }
                }
                catch (Exception)
                {
                    continue;
                }

            }
        }

        public static String ParseEthernetCardNameFromDxDiagFile(string cont)
        {
            try
            {
                if (cont.Contains("System Devices"))
                {
                    GPUDetail gpuDetail = new GPUDetail();

                    var strD = @"System Devices";
                    var strA = @"DirectShow Filters";

                    var dispCont = cont.Substring(strD, strA);

                    var matches = Regex.Matches(dispCont, @"(?<=Name: ).*");

                    foreach (Match match in matches)
                    {
                        if (match.Value.ToLower().Contains("ethernet"))
                        {
                            return match.Value;
                        }
                    }
                }
                Cache.Instance.Log("Unable to find \"System Devices\" in clipboard content");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ParseEthernetCardNameFromDxDiagFile Exception: " + ex);
                Cache.Instance.Log($"Exception was thrown trying to parse the content of your clipboard: {ex.StackTrace}");
                return null;
            }
        }

        public static void ParseMonitorInformationFromDxDiagFolder(string path)
        {
            foreach (string file in Directory.EnumerateFiles(path
              , "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    var ret = ParseMonitorInformationFromDxDiagFile(File.ReadAllText(file));
                    foreach (var r in ret)
                    {
                        Debug.WriteLine($"(\"{r.Item1.Trim()}\",\"{r.Item2.Trim()}\",\"{r.Item3.Trim()}\",\"{r.Item4.Trim()}\"),");
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        // name, width, height, refresh rate
        public static List<Tuple<String, String, String, String>> ParseMonitorInformationFromDxDiagFile(string cont)
        {
            try
            {
                var ret = new List<Tuple<String, String, String, String>>();
                if (cont.Contains("Display Devices"))
                {
                    GPUDetail gpuDetail = new GPUDetail();

                    var strD = @"Display Devices";
                    var strA = @"DirectShow Filters";

                    var dispCont = cont.Substring(strD, strA);

                    var matches = Regex.Matches(dispCont, @"(?<=Monitor Model: ).*");

                    List<string> monitorNameList = new List<string>();
                    foreach (Match match in matches)
                    {
                        monitorNameList.Add(match.Value);
                    }

                    matches = Regex.Matches(dispCont, @"(?<=Current Mode: ).*");
                    List<string> modeList = new List<string>();
                    foreach (Match match in matches)
                    {
                        modeList.Add(match.Value);
                    }

                    if (modeList.Count == monitorNameList.Count)
                    {
                        for (int i = 0; i < modeList.Count; i++)
                        {
                            var monitorName = monitorNameList[i];
                            var mode = modeList[i];
                            matches = Regex.Matches(mode, @"\d+");
                            var width = matches[0].Value;
                            var height = matches[1].Value;
                            var hz = matches[3].Value;
                            ret.Add(new Tuple<string, string, string, string>(monitorName, width, height, hz));
                        }
                    }

                    return ret;
                }
                Cache.Instance.Log("Unable to find \"System Devices\" in clipboard content");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ParseEthernetCardNameFromDxDiagFile Exception: " + ex);
                Cache.Instance.Log($"Exception was thrown trying to parse the content of your clipboard: {ex.StackTrace}");
                return null;
            }
        }

        // name, width, height, refresh rate
        [NonSerialized]
        public static List<(string, string, string, string)> DisplayInformation = new List<(string, string, string, string)>()
        {
                ("G235H","1920","1080","59"),
                ("LONTIUM","1920","1080","50"),
                ("BenQ GL2460","1920","1080","60"),
                ("LG TV","1920","1080","59"),
                ("EPSON PJ","1920","1080","30"),
                ("BenQG2222HDL","1920","1080","60"),
                ("SMB2430H","1920","1080","60"),
                ("S24C200","1920","1080","60"),
                ("22EA53","1920","1080","60"),
                ("Acer G245HQ","1920","1080","60"),
                ("E2340","1920","1080","60"),
                ("DELL U2410","1920","1200","59"),
                ("VX2739 Series","1920","1080","60"),
                ("W2361","1920","1080","60"),
                ("P244W","1920","1080","60"),
                ("G215HV","1920","1080","60"),
                ("ASUS PA238QR","1920","1080","60"),
                ("HSD173PUW1","1920","1080","60"),
                ("HH281","1920","1200","59"),
                ("SMBX2250","1920","1080","60"),
                ("SMB2030","1600","900","60"),
                ("ASUS VS247","1920","1080","60"),
                ("HF237","1920","1080","60"),
                ("MW19E-ABA","1440","900","60"),
                ("VX2235wm","1680","1050","59"),
                ("SAMSUNG","1920","1080","60"),
                ("ASUS VH232H","1920","1080","59"),
                ("GDM-5510","1600","1200","75"),
                ("DELL UP3214Q","3840","2160","60"),
                ("DELL U2711","2560","1440","59"),
                ("TOSHIBA-TV","1920","1080","60"),
                ("LG TV","1920","1080","60"),
                ("SyncMaster","1600","1200","60"),
                ("LG Ultra HD","2560","1440","30"),
                ("X233H","1080","1920","60"),
                ("Acer S201HL","1600","900","60"),
                ("ASUS VH242H","1920","1080","60"),
                ("SONY TV XV","1920","1080","60"),
                ("ASUS VH232H","1920","1080","60"),
                ("ASUS VS228","1920","1080","60"),
                ("S220HQL","1920","1080","60"),
                ("M198WA","1440","900","60"),
                ("DELL U3014","2560","1600","60"),
                ("BenQ XL2720Z","1920","1080","144"),
                ("B24W-5","1920","1200","60"),
                ("VG248","1920","1080","144"),
                ("VX2457","1920","1080","75"),
                ("W2353","1920","1080","60"),
                ("W2252","1680","1050","60"),
                ("SE2717H/HX","1920","1080","60"),
                ("IPS234","1920","1080","60"),
                ("ROG PG278QR","2560","1440","59"),
                ("PLE2483H","1920","1080","60"),
                ("PLE2407HDS","1920","1080","60"),
                ("HP S2031","1600","900","60"),
                ("BenQ G2020HD","1600","900","60"),
                ("PHL 233V5","1920","1080","60"),
                ("32W_LCD_TV","1920","1080","60"),
                ("S27F350","1920","1080","60"),
                ("PHL BDM4065","2560","1440","59"),
                ("F22","1920","1080","60"),
                ("BenQ XL2420Z","1920","1080","144"),
                ("ASUS VN247","1920","1080","60"),
                ("U3477","3440","1440","59"),
                ("DELL U2311H","1920","1080","60"),
                ("ROG PG348Q","3440","1440","100"),
                ("ASUS VG236","1920","1080","120"),
                ("VE247","1920","1080","60"),
                ("BenQ EW2740L","1920","1080","60"),
                ("E2742","1920","1080","60"),
                ("DELL E2211H","1920","1080","60"),
                ("Acer XB241H","1920","1080","180"),
                ("ROG PG279Q","2560","1440","144"),
                ("ASUS PB278","2560","1440","59"),
                ("G276HL","1920","1080","60"),
                ("2367","1920","1080","60"),
                ("SAMSUNG","1680","1050","60"),
                ("SyncMaster","1920","1080","59"),
                ("SM2333T","1920","1080","60"),
                ("24MB65","1920","1200","59"),
                ("BenQ GL2760","1920","1080","60"),
                ("LG IPS FULLHD","1920","1080","60"),
                ("ZOWIE XL LCD","1920","1080","144"),
                ("BenQ XL2730Z","2560","1440","144"),
                ("SyncMaster","1400","1050","60"),
                ("PHL 223V5","1920","1080","59"),
                ("VS278","1920","1080","60"),
                ("Acer XB271H","1920","1080","144"),
                ("20WGX2","1680","1050","59"),
                ("SyncMaster","1920","1200","59"),
                ("Cintiq 13HD","1920","1080","60"),
                ("VS248","1920","1080","60"),
                ("GN246HL","1920","1080","144"),
                ("S242HL","1920","1080","60"),
                ("LG ULTRAWIDE","2560","1080","60"),
                ("L2151w","1920","1080","60"),
                ("PLE2607WS","1920","1200","59"),
                ("VW225","1680","1050","59"),
                ("HP 22cwa","1920","1080","60"),
                ("LG FULL HD","1920","1080","60"),
                ("ASUS VH198","1440","900","60"),
                ("AG352UCG","3440","1440","60"),
                ("C27HG7x","2560","1440","144"),
                ("HP ENVY 27s","3840","2160","60"),
                ("ROG PG279Q","2560","1440","59"),
                ("24MP55","1920","1080","59"),
                ("VA2431 Series","1920","1080","60"),
                ("SONY TV","1920","1080","72"),
                ("DELL S2330MX","1920","1080","60"),
                ("U28D590","3840","2160","60"),
                ("Philips 220AW","1680","1050","60"),
                ("DELL ST2410","1920","1080","60"),
                ("DellSP2008WFP","1680","1050","59"),
                ("24EN33","1920","1080","60"),
                ("ASUS VW224","1680","1050","60"),
                ("ASUS VE278","1920","1080","60"),
                ("DELL P2412H","1080","1920","60"),
                ("DELL S2440L","1920","1080","60"),
                ("HH251D","1920","1080","59"),
                ("Philips 224CL","1920","1080","60"),
                ("DELL S2409W","1920","1080","60"),
                ("PL2201W","1680","1050","59"),
                ("Acer X223W","1680","1050","59"),
                ("2450W","1920","1080","60"),
                ("W2363D","1920","1080","120"),
                ("SyncMaster","1600","900","60"),
                ("DELL E2314H","1920","1080","60"),
                ("NS-32E570A11","1920","1080","60"),
                ("Acer P221W","1680","1050","60"),
                ("DELL D1626HT","1600","1200","85"),
                ("228CLH","1920","1080","60"),
                ("2369M","1920","1080","60"),
                ("SMS24A350H","1920","1080","60"),
                ("BenQ G2420HD","1920","1080","60"),
                ("DENON-AVAMP","1920","1080","60"),
                ("W2442","1920","1080","59"),
                ("ASUS VB198","1280","1024","75"),
                ("Acer H236HL","1920","1080","60"),
                ("ASUS PB287Q","3840","2160","60"),
                ("BenQ XL2720Z","1920","1080","120"),
                ("Acer G215H","1920","1080","60"),
                ("W2353","1920","1080","59"),
                ("HP L1955","1280","1024","60"),
                ("SyncMaster","2560","1600","60"),
                ("FPD1775W","1280","720","60"),
                ("DELL 2407WFP","1920","1200","59"),
                ("HP LP2475w","1920","1200","59"),
                ("2795E","900","1600","59"),
                ("2236","1920","1080","60"),
                ("DELL E248WFP","1920","1200","59"),
                ("Acer G24","1920","1200","59"),
                ("S24D340","1920","1080","60"),
                ("L2250p Wide","1680","1050","60"),
                ("SA300/SA350","1920","1080","60"),
                ("BenQ G2420HD","1920","1080","59"),
                ("Acer GD245HQ","1920","1080","60"),
                ("Philips 272P4","2560","1440","59"),
                ("SMD ST1080","1920","1080","60"),
                ("P224W","1680","1050","60"),
                ("DELL U2414H","1920","1080","59"),
                ("ROG PG278Q","2560","1440","144"),
                ("BenQ XL2420T","1920","1080","60"),
                ("BenQ GW2270","1920","1080","60"),
                ("BenQ XL2411Z","1920","1080","144"),
                ("VG248","1920","1080","60"),
                ("HP LA2405x","1920","1200","59"),
                ("NX-VUE27","2560","1440","59"),
                ("BenQ GL2450H","1920","1080","60"),
                ("UW32S3PW","1920","1080","60"),
                ("Acer G236HL","1920","1080","60"),
                ("BenQ G2025HDA","1600","900","60"),
                ("247ELH","1920","1080","60"),
                ("22W_LCD_TV","1920","1080","60"),
                ("Acer GD245HQ","1920","1080","120"),
                ("ASUS VN248","1920","1080","60"),
                ("Acer X203W","1050","1680","60"),
                ("VG248","5760","1080","60"),
                ("Acer AL2216W","1680","1050","59"),
                ("HP x2301","1920","1080","60"),
                ("SV472XVT","1920","1080","60"),
                ("DELL U2410","1920","1200","60"),
                ("ASUS VE258","1920","1080","60"),
                ("DELL 2001FP","1600","1200","60"),
                ("BenQ FP94VW","1440","900","75"),
                ("NS-24E40SNA14","1920","1080","60"),
                ("BenQ XL2420T","1920","1080","120"),
                ("27EA53","1920","1080","60"),
                ("SMB2220N","1920","1080","60"),
                ("BenQ E2200HD","1920","1080","60"),
                ("SMT27A300","1920","1080","60"),
                ("S27B350","1920","1080","60"),
                ("S27E510","1920","1080","59"),
                ("BenQ GL2450","1920","1080","59"),
                ("DELL 2209WA","1680","1050","60"),
                ("ASUS MK241","1920","1200","60"),
                ("Acer H226HQL","1920","1080","60"),
                ("Sceptre X24WG","1920","1200","60"),
                ("DELL 2007WFP","1680","1050","60"),
                ("Acer A221HQ","1920","1080","60"),
                ("T22D390","1920","1080","60"),
                ("VSX-528","1920","1080","60"),
                ("BenQ XL2410T","1920","1080","120"),
                ("G236HL","1920","1080","60"),
                ("Acer T231H","1920","1080","60"),
                ("PL2409HD","1920","1080","59"),
                ("SAMSUNG","1360","765","60"),
                ("LCD22WV","1680","1050","59"),
                ("E2250","1920","1080","60"),
                ("Viseo243D","1920","1080","60"),
                ("SP2208WFP","1680","1050","59"),
                ("SMBX2331","1920","1080","60"),
                ("TOSHIBA-TV","1600","900","25"),
                ("VX2025wm","1680","1050","59"),
                ("LA2205","1680","1050","59"),
        };

        [NonSerialized]
        public static List<String> NetworkAdapterNames = new List<String>()
        {
                "Qualcomm Atheros AR8161/8165 PCI-E Gigabit Ethernet Controller",
                "Marvell Yukon 88E8056 PCI-E Gigabit Ethernet Controller",
                "Marvell Yukon 88E8055 PCI-E Gigabit Ethernet Controller",
                "Marvell Yukon 88E8071 PCI-E Gigabit Ethernet Controller",
                "Gigabit Ethernet Broadcom NetLink (TM)",
                "Qualcomm Atheros AR8151 PCI-E Gigabit Ethernet Controller",
                "Qualcomm Atheros AR8152 PCI-E Fast Ethernet-controller",
                "Killer e2200 Gigabit Ethernet Controller",
                "Marvell Yukon 88E8001/8003/8010 PCI Gigabit Ethernet Controller",
                "Killer e2200 Gigabit Ethernet Controller",
                "Realtek RTL8168D/8111D Family PCI-E Gigabit Ethernet NIC",
                "Realtek RTL8169/8110 Family Gigabit Ethernet NIC",
                "3Com 3C920B-EMB Integrated Fast Ethernet Controller",
                "Realtek RTL8168D/8111D Family PCI-E Gigabit Ethernet NIC",
                "Marvell Yukon 88E8057 PCI-E Gigabit Ethernet Controller",
                "Realtek RTL8168/8111 PCI-E Gigabit Ethernet NIC",
                "Marvell Yukon 88E8056 PCI-E Gigabit Ethernet Controller",
                "Intel(R) Ethernet Connection I217-V",
                "Realtek RTL8139 Family PCI Fast Ethernet NIC",
                "Atheros AR8121/AR8113/AR8114 PCI-E Ethernet Controller",
                "Killer e2200 PCI-E Gigabit Ethernet Controller",
                "Realtek RTL8168C(P)/8111C(P) Family PCI-E Gigabit Ethernet NIC",
                "Qualcomm Atheros AR8171/8175 PCI-E Gigabit Ethernet Controller",
                "Realtek RTL8168C(P)/8111C(P) Family PCI-E Gigabit Ethernet NIC",
                "Generic Marvell Yukon Chipset based Ethernet Controller",
                "Marvell Yukon 88E8057 Family PCI-E Gigabit Ethernet Controller",
                "Killer e2400 Gigabit Ethernet Controller",
                "Intel(R) Ethernet Connection I217-LM",
                "Atheros AR8151 PCI-E Gigabit Ethernet Controller",
                "Realtek RTL8168C/8111C Family PCI-E Gigabit Ethernet NIC",
                "Killer E2500 Gigabit Ethernet Controller",
                "Broadcom NetLink (TM) Gigabit Ethernet",
                "Marvell Yukon 88E8055 PCI-E-Gigabit-Ethernet-Controller",
                "Intel(R) Ethernet Connection I219-V",
                "Intel(R) Ethernet Connection I218-V",
                "Broadcom NetXtreme Gigabit Ethernet",
                "Atheros L1 Gigabit Ethernet 10/100/1000Base-T Controller",
                "Atheros AR8121/AR8113/AR8114 PCI-E Ethernet Controller",
                "Atheros AR8152/8158 PCI-E Fast Ethernet Controller",
                "Qualcomm Atheros AR8151 PCI-E Gigabit Ethernet Controller",
                "Realtek RTL8102E Family PCI-E Fast Ethernet NIC",
        };



        [NonSerialized]
        public static List<Tuple<string, string, string, string, string>> CPUProfiles = new List<Tuple<string, string, string, string, string>>()
        {
            new Tuple<string, string, string, string, string>("Intel Core i5", "Intel64 Family 6 Model 158 Stepping 9, GenuineIntel", "9e09", "4", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i7 4790K", "Intel64 Family 6 Model 60 Stepping 3, GenuineIntel", "3c03", "8", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i5 3470", "Intel64 Family 6 Model 58 Stepping 9, GenuineIntel", "3a09", "4", "6"),
            new Tuple<string, string, string, string, string>("Intel Xeon v4", "Intel64 Family 6 Model 79 Stepping 1, GenuineIntel", "4f01", "3", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i7 2600K", "Intel64 Family 6 Model 42 Stepping 7, GenuineIntel", "2a07", "8", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i5 2400", "Intel64 Family 6 Model 42 Stepping 7, GenuineIntel", "2a07", "4", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i7 3930K", "Intel64 Family 6 Model 45 Stepping 7, GenuineIntel", "2d07", "12", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i5 6600", "Intel64 Family 6 Model 94 Stepping 3, GenuineIntel", "5e03", "4", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i7 3770K", "Intel64 Family 6 Model 58 Stepping 9, GenuineIntel", "3a09", "8", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i5 4670K", "Intel64 Family 6 Model 60 Stepping 3, GenuineIntel", "3c03", "4", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i5", "Intel64 Family 6 Model 78 Stepping 3, GenuineIntel", "4e03", "4", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i7 6700HQ", "Intel64 Family 6 Model 94 Stepping 3, GenuineIntel", "5e03", "8", "6"),
            new Tuple<string, string, string, string, string>("Intel Core i7 5820K", "Intel64 Family 6 Model 63 Stepping 2, GenuineIntel", "3f02", "12", "6"),
            new Tuple<string, string, string, string, string>("Intel Processor", "Intel64 Family 6 Model 79 Stepping 1, GenuineIntel", "4f01", "12", "6"),
            new Tuple<string, string, string, string, string>("AMD A10-6800K", "AMD64 Family 21 Model 19 Stepping 1, AuthenticAMD", "1301", "4", "21"),
            new Tuple<string, string, string, string, string>("AMD FX-8350", "AMD64 Family 21 Model 2 Stepping 0, AuthenticAMD", "0200", "8", "21"),
            new Tuple<string, string, string, string, string>("AMD FX-6300", "AMD64 Family 21 Model 2 Stepping 0, AuthenticAMD", "0200", "6", "21"),
        };

        [NonSerialized]
        public static List<GPUDetail> GPUDetails = new List<GPUDetail>() {
                new GPUDetail("AMD Radeon (TM) R9 390 Series", "26545", "4098", "128", "21.19.525.0", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-24f1-11cf-fb74-29c13cc2d835", "10.03.2017 01.00.00", "8169"),
                new GPUDetail("AMD Radeon (TM) RX 570", "26591", "4098", "207", "23.20.15033.5003", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-249f-11cf-1360-bb0d74c2da35", "3/21/2018 5:00:00 PM", "4041"),
                new GPUDetail("AMD Radeon R7 Graphics", "39028", "4098", "204", "22.19.159.256", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-db34-11cf-9872-7d2770c2db35", "5/9/2017 7:00:00 PM", "487"),
                new GPUDetail("AMD Radeon R9 200 Series", "26545", "4098", "0", "21.19.519.2", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-24f1-11cf-3075-92b0bcc2d835", "10/02/2017 00:00:00", "4073"),
                new GPUDetail("NVIDIA GeForce GTX 1050", "7297", "4318", "161", "23.21.13.8843", "11", "NVIDIA", "d7b71e3e-5fc1-11cf-e551-8c3c1bc2da35", "11/28/2017 02:55:41", "1968"),
                new GPUDetail("NVIDIA GeForce GTX 1050", "7297", "4318", "161", "23.21.13.9135", "11", "NVIDIA", "d7b71e3e-5fc1-11cf-c559-59041bc2da35", "3/26/2018 01:12:06", "1958"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "22.21.13.8569", "12", "NVIDIA", "d7b71e3e-5fc2-11cf-135b-58341bc2db35", "16.09.2017 02:00:00", "4014"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "23.21.13.8813", "12", "NVIDIA", "d7b71e3e-5fc2-11cf-8555-3f171bc2da35", "27.10.2017 02:00:00", "4029"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "22.21.13.8528", "12", "NVIDIA", "d7b71e3e-5fc2-11cf-3e52-8f3c1bc2db35", "09.08.2017 02:00:00", "4016"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "22.21.13.8554", "11", "NVIDIA", "d7b71e3e-5fc2-11cf-b856-9bac1bc2db35", "9/2/2017 07:44:09", "3993"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "22.21.13.8569", "12", "NVIDIA", "d7b71e3e-5fc2-11cf-135b-58341bc2db35", "16.09.2017 02:00:00", "4014"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "22.21.13.8569", "12", "NVIDIA", "d7b71e3e-5fc2-11cf-135b-58341bc2db35", "16.09.2017 02:00:00", "4014"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "24.21.13.9907", "12", "NVIDIA", "d7b71e3e-5fc2-11cf-417d-5c421bc2d535", "8/20/2018 5:00:00 PM", "4018"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "22.21.13.8569", "12", "NVIDIA", "d7b71e3e-5fc2-11cf-135b-58341bc2db35", "16.09.2017 02:00:00", "4014"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "23.21.13.9077", "12", "NVIDIA", "d7b71e3e-5fc2-11cf-bf57-53420fc2c535", "1/22/2018 7:00:00 PM", "4018"),
                new GPUDetail("NVIDIA GeForce GTX 1050 Ti", "7298", "4318", "161", "23.21.13.9077", "11", "NVIDIA", "d7b71e3e-5fc2-11cf-a754-9bac1bc2da35", "1/23/2018 19:19:35", "3072"),
                new GPUDetail("NVIDIA GeForce GTX 1060", "7264", "4318", "161", "21.21.13.7866", "12", "Intel Corporation", "d7b71e3e-5f20-11cf-226d-cd271bc2d835", "2/8/2017 7:00:00 PM", "6081"),
                new GPUDetail("NVIDIA GeForce GTX 1060 3GB", "7170", "4318", "161", "23.21.13.8813", "12", "NVIDIA", "d7b71e3e-5f42-11cf-8555-29171bc2da35", "10/26/2017 20:00:00", "2997"),
                new GPUDetail("NVIDIA GeForce GTX 1060 3GB", "7170", "4318", "161", "23.21.13.8813", "12", "NVIDIA", "d7b71e3e-5f42-11cf-8555-2c171bc2da35", "27.10.2017 3:00:00", "2997"),
                new GPUDetail("NVIDIA GeForce GTX 1060 3GB", "7170", "4318", "161", "23.21.13.8813", "12", "NVIDIA", "d7b71e3e-5f42-11cf-9f79-6d411bc2da35", "27.10.2017 2.00.00", "2997"),
                new GPUDetail("NVIDIA GeForce GTX 1060 3GB", "7170", "4318", "161", "23.21.13.8813", "12", "NVIDIA", "d7b71e3e-5f42-11cf-0351-0f3c1bc2da35", "27/10/2017 00:00:00", "2997"),
                new GPUDetail("NVIDIA GeForce GTX 1060 6GB", "7171", "4318", "161", "22.21.13.8165", "11", "NVIDIA", "d7b71e3e-5f43-11cf-8b6c-da311bc2db35", "4/1/2017 05:20:54", "3072"),
                new GPUDetail("NVIDIA GeForce GTX 1060 6GB", "7171", "4318", "161", "22.21.13.8205", "12", "NVIDIA", "d7b71e3e-5f43-11cf-e557-1b171bc2db35", "01/05/2017 02:00:00", "6084"),
                new GPUDetail("NVIDIA GeForce GTX 1060 6GB", "7171", "4318", "161", "23.21.13.8859", "12", "NVIDIA", "d7b71e3e-5f43-11cf-4955-8c121bc2da35", "05.12.2017 02:00:00", "6061"),
                new GPUDetail("NVIDIA GeForce GTX 1060 6GB", "7171", "4318", "161", "23.21.13.9135", "12", "Intel Corporation", "d7b71e3e-5f43-11cf-3750-da311bc2da35", "3/22/2018 5:00:00 PM", "6052"),
                new GPUDetail("NVIDIA GeForce GTX 1060 6GB", "7171", "4318", "161", "24.21.13.9907", "12", "NVIDIA", "d7b71e3e-5f43-11cf-6151-8e121bc2d535", "8/21/2018 2:00:00 AM", "6052"),
                new GPUDetail("NVIDIA GeForce GTX 1060 6GB", "7171", "4318", "161", "23.21.13.9077", "12", "NVIDIA", "d7b71e3e-5f43-11cf-1b50-0e3c1bc2da35", "23.01.2018 01:00:00 Uhr", "6052"),
                new GPUDetail("NVIDIA GeForce GTX 1060 6GB", "7171", "4318", "161", "23.21.13.9101", "12", "NVIDIA", "d7b71e3e-5f43-11cf-e350-da311bc2da35", "22/02/2018 21:00:00", "6052"),
                new GPUDetail("NVIDIA GeForce GTX 1060 6GB", "7171", "4318", "161", "23.21.13.9135", "12", "NVIDIA", "d7b71e3e-5f43-11cf-5457-35041bc2da35", "23/03/2018 03:00:00", "6052"),
                new GPUDetail("NVIDIA GeForce GTX 1070", "7041", "4318", "161", "23.21.13.8871", "12", "NVIDIA", "d7b71e3e-58c1-11cf-5451-11a71bc2da35", "2017-12-15 01:00:00", "8096"),
                new GPUDetail("NVIDIA GeForce GTX 1070", "7041", "4318", "161", "23.21.13.9124", "12", "NVIDIA", "d7b71e3e-58c1-11cf-5750-94a51bc2da35", "2018. 03. 15. 1:00:00", "8088"),
                new GPUDetail("NVIDIA GeForce GTX 1070", "7041", "4318", "161", "23.21.13.9124", "12", "NVIDIA", "d7b71e3e-58c1-11cf-5750-94a51bc2da35", "2018. 03. 15. 1:00:00", "8088"),
                new GPUDetail("NVIDIA GeForce GTX 1070", "7073", "4318", "161", "23.21.13.9065", "12", "Intel Corporation", "d7b71e3e-58e1-11cf-f150-7c271bc2da35", "03.01.2018 01:00:00", "8081"),
                new GPUDetail("NVIDIA GeForce GTX 1070", "7041", "4318", "161", "21.21.13.7633", "12", "NVIDIA", "d7b71e3e-58c1-11cf-396a-0c171bc2d835", "2016. 12. 11. 1:00:00", "8145"),
                new GPUDetail("NVIDIA GeForce GTX 1070 Ti", "7042", "4318", "161", "24.21.13.9811", "12", "NVIDIA", "d7b71e3e-58c2-11cf-8151-0ee31bc2d535", "1/06/2018 2:00:00", "8080"),
                new GPUDetail("NVIDIA GeForce GTX 1070 Ti", "7042", "4318", "161", "23.21.13.9124", "12", "NVIDIA", "d7b71e3e-58c2-11cf-5750-13a61bc2da35", "2018-03-14 17:00:00", "8080"),
                new GPUDetail("NVIDIA GeForce GTX 1080", "7040", "4318", "161", "24.21.14.1163", "12", "NVIDIA", "d7b71e3e-58c0-11cf-e577-90311bc2d535", "18/9/2018 8:00:00 AM", "8079"),
                new GPUDetail("NVIDIA GeForce GTX 1080", "7040", "4318", "161", "22.21.13.8205", "11", "NVIDIA", "d7b71e3e-58c0-11cf-ff7b-85421bc2db35", "5/2/2017 08:32:51", "3072"),
                new GPUDetail("NVIDIA GeForce GTX 1080", "7040", "4318", "161", "24.21.13.9811", "12", "NVIDIA", "d7b71e3e-58c0-11cf-a055-9fa51bc2d535", "1/06/2018 2:00:00", "8079"),
                new GPUDetail("NVIDIA GeForce GTX 1080", "7040", "4318", "161", "24.21.13.9882", "11", "NVIDIA", "d7b71e3e-58c0-11cf-687d-8b421bc2d535", "8/1/2018 05:47:14", "3072"),
                new GPUDetail("NVIDIA GeForce GTX 1080", "7040", "4318", "161", "22.21.13.8253", "12", "NVIDIA", "d7b71e3e-58c0-11cf-ef57-6f131bc2db35", "07-06-2017 02:00:00", "8110"),
                new GPUDetail("NVIDIA GeForce GTX 1080", "7040", "4318", "161", "24.21.14.1163", "12", "NVIDIA", "d7b71e3e-58c0-11cf-e577-90311bc2d535", "18/9/2018 8:00:00 AM", "8079"),
                new GPUDetail("NVIDIA GeForce GTX 1080", "7040", "4318", "161", "22.21.13.8253", "12", "NVIDIA", "d7b71e3e-58c0-11cf-ef57-6f131bc2db35", "07-06-2017 02:00:00", "8110"),
                new GPUDetail("NVIDIA GeForce GTX 1080", "7040", "4318", "161", "24.21.14.1163", "12", "NVIDIA", "d7b71e3e-58c0-11cf-e577-90311bc2d535", "18/9/2018 8:00:00 AM", "8079"),
                new GPUDetail("NVIDIA GeForce GTX 1080 Ti", "6918", "4318", "161", "22.21.13.8205", "11", "NVIDIA", "d7b71e3e-5846-11cf-e557-5c171bc2db35", "5/1/2017 23:32:51", "3072"),
                new GPUDetail("NVIDIA GeForce GTX 1080 Ti", "6918", "4318", "161", "24.21.13.9731", "12", "NVIDIA", "d7b71e3e-5846-11cf-f055-e7a51bc2d535", "22/04/2018 02:00:00", "11127"),
                new GPUDetail("NVIDIA GeForce GTX 1080 Ti", "6918", "4318", "161", "24.21.13.9836", "12", "NVIDIA", "d7b71e3e-5846-11cf-9e7d-9b461bc2d535", "6/23/2018 8:00:00 PM", "11127"),
                new GPUDetail("NVIDIA GeForce GTX 1080 Ti", "6918", "4318", "161", "21.21.13.7878", "12", "NVIDIA", "d7b71e3e-5846-11cf-a86d-02321bc2d835", "23/02/2017 01:00:00", "11158"),
                new GPUDetail("NVIDIA GeForce GTX 650", "4038", "4318", "161", "23.21.13.9101", "12", "NVIDIA", "d7b71e3e-4c86-11cf-5f54-04081bc2da35", "23.02.2018 7:00:00", "2007"),
                new GPUDetail("NVIDIA GeForce GTX 745", "4994", "4318", "162", "23.21.13.8813", "12", "Intel Corporation", "d7b71e3e-50c2-11cf-0351-683018c2da35", "10/26/2017 7:00:00 PM", "4063"),
                new GPUDetail("NVIDIA GeForce GTX 745", "4994", "4318", "162", "23.21.13.8813", "12", "Intel Corporation", "d7b71e3e-50c2-11cf-0351-683018c2da35", "10/26/2017 7:00:00 PM", "4063"),
                new GPUDetail("NVIDIA GeForce GTX 760", "4487", "4318", "161", "10.18.13.6472", "12", "NVIDIA", "d7b71e3e-52c7-11cf-a06e-06161cc2c735", "2016. 03. 21. 0:00:00", "1989"),
                new GPUDetail("NVIDIA GeForce GTX 760", "4487", "4318", "161", "21.21.13.7892", "11", "NVIDIA", "d7b71e3e-52c7-11cf-647d-0d201bc2d835", "3/17/2017 02:59:25", "3994"),
                new GPUDetail("NVIDIA GeForce GTX 770", "4484", "4318", "161", "21.21.13.7849", "11", "NVIDIA", "d7b71e3e-52c4-11cf-4169-0e161bc2d835", "1/20/2017 17:36:19", "1989"),
                new GPUDetail("NVIDIA GeForce GTX 960", "5121", "4318", "161", "23.21.13.8871", "12", "NVIDIA", "d7b71e3e-5741-11cf-5451-66a51bc2da35", "15.12.2017 01:00:00", "4062"),
                new GPUDetail("NVIDIA GeForce GTX 960", "5121", "4318", "161", "23.21.13.8871", "12", "NVIDIA", "d7b71e3e-5741-11cf-5451-66a51bc2da35", "15.12.2017 01:00:00", "4062"),
                new GPUDetail("NVIDIA GeForce GTX 960", "5121", "4318", "161", "23.21.13.9135", "11", "NVIDIA", "d7b71e3e-5741-11cf-7d54-0c121bc2da35", "3/25/2018 12:12:06", "4002"),
                new GPUDetail("NVIDIA GeForce GTX 970", "5058", "4318", "161", "23.21.13.8792", "11", "NVIDIA", "d7b71e3e-5082-11cf-8a55-6d111bc2da35", "10/6/2017 14:32:47", "4007"),
                new GPUDetail("NVIDIA GeForce GTX 970", "5058", "4318", "161", "23.21.13.9135", "12", "NVIDIA", "d7b71e3e-5082-11cf-4754-74161bc2da35", "3/22/2018 8:00:00 PM", "4043"),
                new GPUDetail("NVIDIA GeForce GTX 970", "5058", "4318", "161", "23.21.13.8813", "12", "NVIDIA", "d7b71e3e-5082-11cf-0758-6b331bc2da35", "10/27/2017 1:00:00 AM", "4058"),
                new GPUDetail("NVIDIA GeForce GTX 970", "5058", "4318", "161", "10.18.13.6822", "12", "NVIDIA", "d7b71e3e-5082-11cf-c869-cf331cc2c735", "2016-05-19 ???? 12:00:00", "4007"),
                new GPUDetail("NVIDIA GeForce GTX 970", "5058", "4318", "161", "21.21.13.7878", "11", "NVIDIA", "d7b71e3e-5082-11cf-1469-6d111bc2d835", "2/23/2017 11:34:39", "3072"),
                new GPUDetail("NVIDIA GeForce GTX 980", "5056", "4318", "161", "22.21.13.8233", "12", "NVIDIA", "d7b71e3e-5080-11cf-fb57-7d111bc2db35", "5/16/2017 8:00:00 PM", "4060"),
                new GPUDetail("Radeon 500 Series", "27039", "4098", "199", "22.19.147.0", "11", "Advanced Micro Devices, Inc.", "d7b71ee2-2adf-11cf-e877-60027bc2db35", "3/7/2017 01:07:26", "1993"),
                new GPUDetail("Radeon RX 550 Series", "27039", "4098", "199", "23.20.793.0", "11", "Advanced Micro Devices, Inc.", "d7b71ee2-2adf-11cf-d277-89a97cc2da35", "11/15/2017 21:46:04", "4039"),
                new GPUDetail("Radeon RX 550 Series", "27039", "4098", "199", "24.20.11001.5003", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-2adf-11cf-b966-a99f7cc2d535", "4/24/2018 6:00:00 PM", "4042"),
                new GPUDetail("Radeon RX 550 Series", "27039", "4098", "199", "23.20.782.0", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-2adf-11cf-127e-69c07cc2da35", "19/10/2017 21:00:00", "4042"),
                new GPUDetail("Radeon RX 560 Series", "26607", "4098", "229", "23.20.15017.3010", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-24af-11cf-3178-e21f5ec2da35", "31.01.2018 1:00:00", "4041"),
                new GPUDetail("Radeon RX 560 Series", "26623", "4098", "207", "23.20.15017.3010", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-24bf-11cf-3178-981f74c2da35", "31.01.2018 3:00:00", "4041"),
                new GPUDetail("Radeon RX 560 Series", "26623", "4098", "207", "23.20.793.0", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-24bf-11cf-d277-88a974c2da35", "15.11.2017 3:00:00", "4041"),
                new GPUDetail("Radeon RX 560 Series", "26623", "4098", "207", "23.20.15007.1005", "11", "Advanced Micro Devices, Inc.", "d7b71ee2-24bf-11cf-3f74-0e9074c2da35", "12/18/2017 06:06:34", "3072"),
                new GPUDetail("Radeon RX 580 Series", "26591", "4098", "231", "24.20.11019.1004", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-249f-11cf-3e74-1c3f5cc2d535", "29.05.2018 02:00:00", "8137"),
                new GPUDetail("Radeon RX 580 Series", "26591", "4098", "231", "24.20.11021.1000", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-249f-11cf-d474-7a285cc2d535", "07.06.2018 3:00:00", "8137"),
                new GPUDetail("Radeon RX 580 Series", "26591", "4098", "231", "23.20.15007.1005", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-249f-11cf-3f74-882e5cc2da35", "16/12/2017 21:00:00", "8137"),
                new GPUDetail("Radeon RX 580 Series", "26591", "4098", "231", "23.20.15027.2002", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-249f-11cf-c079-d5f95cc2da35", "2018. 03. 08. 1:00:00", "8137"),
                new GPUDetail("Radeon(TM) RX 460 Graphics", "26607", "4098", "207", "22.19.162.4", "12", "Advanced Micro Devices, Inc.", "d7b71ee2-24af-11cf-ec77-7f0273c2db35", "24.04.2017 3:00:00", "4044"),
        };

        private static Random _rnd = new Random();

        public void GenerateRandomProfile()
        {
            var list = Cache.Instance.EveAccountSerializeableSortableBindingList.List;
            var first = true;

            while (first || list.Any(e => e != null && e.HWSettings != null && e.HWSettings != this && e.HWSettings.CheckEquality(this, true)))
            {
                first = false;

            calcNameAgain:
                var name = UserPassGen.Instance.GenerateFirstname();
                TotalPhysRam = Convert.ToUInt64(GenerateRamSize());
                WindowsUserLogin = name;
                Computername = name + "-PC";

                if (Computername.Length > 16)
                    goto calcNameAgain;

                WindowsKey = GenerateWindowsKey();
                MachineGuid = Guid.NewGuid().ToString();
                NetworkAdapterGuid = Guid.NewGuid().ToString();
                NetworkAddress = GenerateIpAddress();
                MacAddress = GenerateMacAddress();
                SystemReservedMemory = (ulong)(2 * new Random().Next(8 / 2, 100 / 2));

                var cpuProfile = CPUProfiles[_rnd.Next(CPUProfiles.Count)];

                ProcessorIdent = cpuProfile.Item2;
                ProcessorRev = cpuProfile.Item3;
                ProcessorCoreAmount = cpuProfile.Item4;
                ProcessorLevel = cpuProfile.Item5;
                //LauncherMachineHash = LauncherHash.GetRandomLauncherHash().Item1;
                GenerateNewLauncherMachineHash();

                var gpuProfile = GPUDetails[_rnd.Next(GPUDetails.Count)];
                GpuIdentifier = gpuProfile.DeviceIdentifier;
                GpuDescription = gpuProfile.CardName;
                GpuDeviceId = Convert.ToUInt32(gpuProfile.DeviceId);
                GpuVendorId = Convert.ToUInt32(gpuProfile.VendorId);
                GpuRevision = Convert.ToUInt32(gpuProfile.RevisionId);
                GpuDriverversionInt = gpuProfile.DriverVersion;
                GpuManufacturer = gpuProfile.Manufacturer;
                GpuDriverDate = gpuProfile.DriverDateTime.Value;
                GpuDedicatedMemoryMB = Convert.ToUInt64(gpuProfile.DedicatedMemoryMB);
                NetworkAdapterName = NetworkAdapterNames[_rnd.Next(NetworkAdapterNames.Count)];

                GenerateNewMonitorProfile();
            }
        }

        public void GenerateNewLauncherMachineHash()
        {
            LauncherMachineHash = Guid.NewGuid().ToString();
        }   

        public void GenerateRandomGpuProfile()
        {
            var rnd = new Random();
            var gpuProfile = GPUDetails[rnd.Next(GPUDetails.Count)];
            GpuIdentifier = gpuProfile.DeviceIdentifier;
            GpuDescription = gpuProfile.CardName;
            GpuDeviceId = Convert.ToUInt32(gpuProfile.DeviceId);
            GpuVendorId = Convert.ToUInt32(gpuProfile.VendorId);
            GpuRevision = Convert.ToUInt32(gpuProfile.RevisionId);
            GpuDriverversionInt = gpuProfile.DriverVersion;
            GpuManufacturer = gpuProfile.Manufacturer;
            GpuDriverDate = gpuProfile.DriverDateTime.Value;
            GpuDedicatedMemoryMB = Convert.ToUInt64(gpuProfile.DedicatedMemoryMB);
        }

        public void GenerateNewMonitorProfile()
        {
            var monitorProfile = DisplayInformation[_rnd.Next(DisplayInformation.Count)];
            MonitorName = monitorProfile.Item1;
            MonitorWidth = int.Parse(monitorProfile.Item2);
            MonitorHeight = int.Parse(monitorProfile.Item3);
            MonitorRefreshrate = int.Parse(monitorProfile.Item4);
        }

        public void GenerateNetworkProfile()
        {
            NetworkAdapterGuid = Guid.NewGuid().ToString();
            NetworkAddress = GenerateIpAddress();
            MacAddress = GenerateMacAddress();
            NetworkAdapterName = NetworkAdapterNames[_rnd.Next(NetworkAdapterNames.Count)];
        }

        public bool CheckEquality(object value, bool excludeProxies = false)
        {
            if (!(value is HWSettings))
                return false;

            var p = (HWSettings)value;

            if (WindowsUserLogin == p.WindowsUserLogin)
            {
                Debug.WriteLine($"{nameof(WindowsUserLogin)} equal.");
                return true;
            }

            if (Computername == p.Computername)
            {
                Debug.WriteLine($"{nameof(Computername)} equal.");
                return true;
            }

            if (WindowsKey == p.WindowsKey)
            {
                Debug.WriteLine($"{nameof(WindowsKey)} equal.");
                return true;
            }

            if (NetworkAdapterGuid == p.NetworkAdapterGuid)
            {
                Debug.WriteLine($"{nameof(NetworkAdapterGuid)} equal.");
                return true;
            }

            if (MacAddress == p.MacAddress)
            {
                Debug.WriteLine($"{nameof(MacAddress)} equal.");
                return true;
            }

            if (GpuIdentifier == p.GpuIdentifier)
            {
                Debug.WriteLine($"{nameof(GpuIdentifier)} equal.");
                return true;
            }

            if (MachineGuid == p.MachineGuid)
            {
                Debug.WriteLine($"{nameof(MachineGuid)} equal.");
                return true;
            }

            if (LauncherMachineHash == p.LauncherMachineHash)
            {
                Debug.WriteLine($"{nameof(LauncherMachineHash)} equal.");
                return true;
            }


            if (!excludeProxies && ProxyId > 0 && ProxyId == p.ProxyId)
            {
                Debug.WriteLine($"{nameof(ProxyId)} equal.");
                return true;
            }

            return false;
        }


        public void ParseDXDiagFile(string content)
        {
            try
            {
                var gpuDetail = GPUDetail.ParseDXDiagFile(content);
                if (gpuDetail != null)
                {
                    GpuDescription = gpuDetail.CardName;
                    GpuDeviceId = Convert.ToUInt32(gpuDetail.DeviceId);
                    GpuVendorId = Convert.ToUInt32(gpuDetail.VendorId);
                    GpuRevision = Convert.ToUInt32(gpuDetail.RevisionId);
                    GpuDriverversionInt = gpuDetail.DriverVersion;
                    GpuIdentifier = gpuDetail.DeviceIdentifier;
                    GpuManufacturer = gpuDetail.Manufacturer;
                    GpuDriverDate = gpuDetail.DriverDateTime.Value;
                    GpuDedicatedMemoryMB = ulong.Parse(gpuDetail.DedicatedMemoryMB);

                }
                else
                {
                    Cache.Instance.Log("ParseDXDiagFile Error.");
                }
            }
            catch (Exception ex)
            {
                Cache.Instance.Log("ParseDXDiagFile Exception: " + ex);
            }
        }

        public HWSettings Clone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                if (this.GetType().IsSerializable)
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, this);
                    stream.Position = 0;
                    return (HWSettings)formatter.Deserialize(stream);
                }
                return null;
            }
        }

        public void ParseCPUDetails(string content)
        {
            try
            {
                // google:  -inurl "piriform.com/results/"

                var PROCESSOR_LEVEL = String.Empty;
                var PROCESSOR_IDENTIFIER = String.Empty;
                var NUMBER_OF_PROCESSORS = String.Empty;
                var PROCESSOR_REVISION = String.Empty;
                var CPU_NAME = String.Empty;

                Match match;

                try
                {
                    match = Regex.Match(content, @"CPU[\r\n]+([^\r\n]+)");
                    if (match.Success)
                        CPU_NAME = match.NextMatch().Groups[1].Value;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }


                match = Regex.Match(content, @"(?<=PROCESSOR_LEVEL: ).*");
                if (match.Success)
                    PROCESSOR_LEVEL = match.Value.Trim();

                match = Regex.Match(content, @"(?<=PROCESSOR_IDENTIFIER: ).*");
                if (match.Success)
                    PROCESSOR_IDENTIFIER = match.Value.Trim();

                match = Regex.Match(content, @"(?<=NUMBER_OF_PROCESSORS: ).*");
                if (match.Success)
                    NUMBER_OF_PROCESSORS = match.Value.Trim();

                match = Regex.Match(content, @"(?<=PROCESSOR_REVISION: ).*");
                if (match.Success)
                    PROCESSOR_REVISION = match.Value.Trim();

                //Debug.WriteLine($"{CPU_NAME} - {PROCESSOR_LEVEL} - {PROCESSOR_IDENTIFIER} - {NUMBER_OF_PROCESSORS} - {PROCESSOR_REVISION}");

                //                ProcessorIdent = "Intel64 Family 6 Model 26 Stepping 5, GenuineIntel";
                //                ProcessorRev = "6661";
                //                ProcessorCoreAmount = "8";
                //                ProcessorLevel = "6";

                Debug.WriteLine($"new Tuple<string, string, string, string, string>(\"{CPU_NAME}\", \"{PROCESSOR_IDENTIFIER}\", \"{PROCESSOR_REVISION}\", \"{NUMBER_OF_PROCESSORS}\", \"{PROCESSOR_LEVEL}\"),");

            }
            catch (Exception ex)
            {
                Debug.WriteLine("ParseDXDiagFile Exception: " + ex);
            }
        }
    }
}