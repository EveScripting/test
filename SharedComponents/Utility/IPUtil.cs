using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public class IPComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return Enumerable.Zip(a.Split('.'), b.Split('.'),
                                 (x, y) => int.Parse(x).CompareTo(int.Parse(y)))
                             .FirstOrDefault(i => i != 0);
        }
    }

    public static class IPUtil
    {
        /// <summary>
        /// Generate a negated list for one or many IP-Adresses. E.g. To use for the windows firewall if a "Deny all except" functionality is required.
        /// Input 192.0.2.55
        /// Output: 1.1.1.1-192.0.2.54,192.0.2.56-255.255.255.255
        /// Win FW example:
        /// netsh advfirewall firewall add rule name="Deny from all except" dir=out action=block protocol=ANY remoteip=1.1.1.1-192.0.2.54,192.0.2.56-255.255.255.255
        /// </summary>
        /// <param name="ips">List of IPs</param> 
        /// <returns></returns>
        public static string GenerateIPMask(List<string> ips)
        {
            
            var orderedIPs = ips.Distinct().OrderBy(p => p, new IPComparer()).ToList();

            var prefix = "1.1.1.1";
            var suffix = "255.255.255.255";

            var res = string.Empty;

            
            for (int i = 0; i < orderedIPs.Count; i++)
            {
                
                var start = i;
                while (i < orderedIPs.Count - 1)
                {
                    var c = IpToUint(orderedIPs[i]);
                    var next = IpToUint(orderedIPs[i + 1]);
                    // Check if the next IP is consecutive
                    if (c + 1 != next)
                        break;
                    i++;
                }

                if (i < orderedIPs.Count - 1 && IpToUint(orderedIPs[i]) + 2 == IpToUint(orderedIPs[i + 1]))
                {
                    res += $"{UintToIp(IpToUint(orderedIPs[i]) - 1)},{UintToIp(IpToUint(orderedIPs[i + 1]) - 1)},{UintToIp(IpToUint(orderedIPs[i + 1]) + 1)}-";
                    i++;
                }
                else if (start == i)
                    
                    res += $"{UintToIp(IpToUint(orderedIPs[i]) - 1)},{UintToIp(IpToUint(orderedIPs[i]) + 1)}-";
                else
                    
                    res += $"{UintToIp(IpToUint(orderedIPs[start]) - 1)},{UintToIp(IpToUint(orderedIPs[i]) + 1)}-";
            }

            
            return $"{prefix}-{res}{suffix}";
        }


        public static uint IpToUint(string ipString)
        {
            var ipAddress = IPAddress.Parse(ipString);
            var ipBytes = ipAddress.GetAddressBytes();
            var ip = (uint)ipBytes[0] << 24;
            ip += (uint)ipBytes[1] << 16;
            ip += (uint)ipBytes[2] << 8;
            ip += (uint)ipBytes[3];
            return ip;
        }
        public static string UintToIp(uint ip)
        {
            var ipBytes = BitConverter.GetBytes(ip);
            var ipBytesRevert = new byte[4];
            ipBytesRevert[0] = ipBytes[3];
            ipBytesRevert[1] = ipBytes[2];
            ipBytesRevert[2] = ipBytes[1];
            ipBytesRevert[3] = ipBytes[0];
            return new IPAddress(ipBytesRevert).ToString();
        }

    }
}
