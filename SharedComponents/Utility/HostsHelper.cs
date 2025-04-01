using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public static class HostsHelper
    {
        private const string LocalHost = "127.0.0.1";
        private const string HostsPath = @"C:\Windows\System32\drivers\etc\hosts";

        private static Dictionary<string, string> _defaultEntries = new Dictionary<string, string>()
        {
            //["tranquility.servers.eveonline.com"] = string.Empty,
            ["logs-01.loggly.com"] = LocalHost,
            ["clientstream.launchdarkly.com"] = LocalHost,
            ["api.honeycomb.io"] = LocalHost,
            ["honeycomb.io"] = LocalHost,
            ["vault.evetech.net"] = LocalHost,
            ["api.honeycomb.io"] = LocalHost,
            ["logs-01.loggly.com"] = LocalHost,
            ["crashes.eveonline.com"] = LocalHost,
            ["slogs-01.loggly.com"] = LocalHost,
            ["sentry.tech.ccp.is"] = LocalHost,
            ["sentry.evetech.net"] = LocalHost,
            ["mobile.launchdarkly.com"] = LocalHost,
            ["sentry.io"] = LocalHost,
            //["live-public-gateway.evetech.net"] = LocalHost,
            ["test-public-gateway.evetech.netAd"] = LocalHost, // what is TLD .netAd? was this a copy paste mistake? I don't know :D
            ["test-public-gateway.evetech.net"] = LocalHost,
        };

        public static void BlockDefault()
        {
            foreach (var entry in _defaultEntries)
            {
                Set(entry.Key, entry.Value);
            }
        }

        public static void UnblockDefault()
        {
            foreach (var entry in _defaultEntries)
            {
                Unblock(entry.Key, keepInHosts: false);
            }
        }

        public static void Block(string hostname)
        {
            Set(hostname, LocalHost);
        }

        public static void Unblock(string hostname, bool keepInHosts = false)
        {
            Set(hostname, keepInHosts ? "" : null);
        }

        public static void Set(string hostname, string address) 
        {
            hostname = hostname.Trim();
            address = address?.Trim();

            var hostLines = File.ReadAllLines(HostsPath).ToList();
            var foundMatch = false;

            for (int lineNumber = 0; lineNumber < hostLines.Count; lineNumber++)
            {
                var trimmed = hostLines[lineNumber].Trim();
                if (trimmed.StartsWith("#")) continue;

                var split = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length == 0) continue; // Blank line
                if (split.Length >= 3) continue; // Unknown format

                // Extract the address for both 2 column and single hostname lines
                var lineAddress = split.Length == 2 ? split[0] : "";
                var lineHostname = split.Length == 2 ? split[1] : split[0];

                if (lineHostname.Equals(hostname, StringComparison.OrdinalIgnoreCase))
                {
                    if (lineAddress.Equals(address, StringComparison.OrdinalIgnoreCase))
                    {
                        // Nothing to edit
                        return;
                    }

                    foundMatch = true;

                    // Found an equalivent line
                    if (address == null)
                    {
                        // Remove
                        hostLines.RemoveAt(lineNumber);
                        break;
                    }
                    
                    if (string.IsNullOrWhiteSpace(address))
                    {
                        // Single column
                        hostLines[lineNumber] = $"{hostname}";
                        break;
                    }

                    // Write normally
                    hostLines[lineNumber] = $"{address} {hostname}";
                    break;
                }
            }

            if (!foundMatch)
            {
                // Not already in hosts, add new line
                if (string.IsNullOrWhiteSpace(address))
                {
                    // Single column
                    hostLines.Add($"{hostname}");
                }
                else
                {
                    // Write normally
                    hostLines.Add($"{address} {hostname}");
                }
            }

            File.WriteAllLines(HostsPath, hostLines);
        }
    }
}
