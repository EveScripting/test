using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public static class FirewallRuleHelper
    {
        public const string FW_RULE_NAME_TQ = "E#-Blocking-Rule-TQ";
        public const string FW_RULE_NAME_SISI = "E#-Blocking-Rule-SISI";

        public static bool CheckIfRuleNameExists(string name)
        {
            var res = Util.ExecCommand($"netsh advfirewall firewall show rule name=all | find \"{name}\"");
            Debug.WriteLine($"Res: [{res}] Length [{res.Length}]");
            return res.Length > 0;
        }

        public static void AddBlockingRule(string name, string pathToExe)
        {
            if (!CheckIfRuleNameExists(name))
            {
                Util.ExecCommand($"netsh advfirewall firewall add rule name=\"{name}\" dir=out action=block program=\"{pathToExe}\"");
                Debug.WriteLine($"FWRule added name: [{name}]");
            }
            else
            {
                Debug.WriteLine($"FWRule could not be added name: [{name}]");
            }
        }


        public static void AddIPBlockingRule(string name, string pathToExe, string ips)
        {
            if (!CheckIfRuleNameExists(name))
            {
                Util.ExecCommand($"netsh advfirewall firewall add rule name=\"{name}\" dir=out action=block program=\"{pathToExe}\" protocol=ANY remoteip=\"{ips}\"");
                Debug.WriteLine($"FWRule added name: [{name}]");
            }
            else
            {
                Debug.WriteLine($"FWRule could not be added name: [{name}]");
            }
        }


        public static bool CheckIfIPBlockingRuleMatch(string name, string filter, string evePath)
        {
            // this verification does not work properly with consecutive ips, the windows firewall puts single ips at the end
            var res = Util.ExecCommand($"netsh advfirewall firewall show rule name=\"{name}\" | find \"{filter}\"");
            var res2 = Util.ExecCommand($"netsh advfirewall firewall show rule name=\"{name}\" verbose | find \"{evePath}\"");
            return res.Length > 0 && res2.Length > 0;
        }


        public static void RemoveRule(string name)
        {
            if (CheckIfRuleNameExists(name))
            {
                Util.ExecCommand($"netsh advfirewall firewall del rule name=\"{name}\"");
                Debug.WriteLine($"FWRule deleted name: [{name}]");
            }
            else
            {
                Debug.WriteLine($"Could not delete FWRule name: [{name}]");
            }
        }
    }
}
