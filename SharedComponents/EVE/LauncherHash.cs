using System;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE
{
    public class LauncherHash
    {
        #region Fields

        private static Tuple<string, string, string, string> _getLauncherHash;
        private static int? _maxNonPrintableChars;
        private static Random random = new Random();


        #endregion Fields

        #region Methods

        public static string CCPMagic(string md5)
        {
            var ba = StringToByteArray(md5);
            var str = System.Text.Encoding.ASCII.GetString(ba) + " \"\"";
            str = str.Replace("?", "�");
            return str;
        }

        public static Tuple<string, string, string, string> GetLauncherHash()
        {
            if (_getLauncherHash == null)
            {
                var pUID = WMIQuery("Win32_Processor", "UniqueId");
                var pId = WMIQuery("Win32_Processor", "ProcessorId");
                var diskS = WMIQuery("Win32_DiskDrive", "SerialNumber");
                var biosS = WMIQuery("Win32_Bios", "SerialNumber");
                var baseS = WMIQuery("Win32_BaseBoard", "SerialNumber");
                var proc = pUID != string.Empty ? pUID : pId;
                var result = $"{proc}_{diskS}_{biosS}_{baseS}";
                var resultEs = $"{proc}_{diskS}_{biosS}_{baseS}_EveSharp";
                var md5Result = MD5Hash(result);
                _getLauncherHash = new Tuple<string, string, string, string>(result, md5Result, CCPMagic(md5Result), MD5Hash(resultEs));
            }
            return _getLauncherHash;
        }

        public static Tuple<string, string> GetRandomLauncherHash()
        {
            var linuxTime = (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            var launcherHash = string.Empty;
            var ccpMagic = string.Empty;
            do
            {
                launcherHash = MD5Hash(GetLauncherHash().Item1 + random.Next(50000, 9999999).ToString() + linuxTime.ToString());
                ccpMagic = CCPMagic(launcherHash);
            }
            while (!CheckHash(launcherHash));
            _maxNonPrintableChars = null;
            return new Tuple<string, string>(launcherHash, ccpMagic);
        }

        public static string MD5Hash(string s)
        {
            using (var provider = MD5.Create())
            {
                StringBuilder builder = new StringBuilder();

                foreach (byte b in provider.ComputeHash(Encoding.UTF8.GetBytes(s)))
                    builder.Append(b.ToString("x2").ToLower());

                return builder.ToString();
            }
        }

        private static bool CheckHash(string md5)
        {
            var notPrintableCount = 0;
            for (int i = 0; i < md5.Length; i = i + 2)
            {
                var p = md5.Substring(i, 2);
                var n = (int)Convert.ToByte(p, 16);
                if (n < 33 || n == 63)
                {
                    return false;
                }
                if (n > 126)
                    notPrintableCount++;
            }
            return notPrintableCount <= GetNumMaxNonPrintableChars();
        }

        private static int GetNumMaxNonPrintableChars()
        {
            if (_maxNonPrintableChars == null)
            {
                _maxNonPrintableChars = random.Next(5, 8);
            }
            return _maxNonPrintableChars.Value;
        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private static string WMIQuery(string className, string attributeName)
        {
            WqlObjectQuery wquery = new WqlObjectQuery($"SELECT * from {className}");
            ManagementObjectSearcher searcher1 = new ManagementObjectSearcher(wquery);
            foreach (ManagementObject mo1 in searcher1.Get())
            {
                try
                {
                    var k = mo1.GetPropertyValue(attributeName);
                    return k == null ? string.Empty : k.ToString();
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
            return string.Empty;
        }

        #endregion Methods
    }
}