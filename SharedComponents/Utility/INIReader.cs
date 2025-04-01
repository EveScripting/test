using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public static class INIReader
    {
        const string lineSep = "\r\n";
        const string kvSep = "=";

        public static Dictionary<string, string> Read(string config)
        {
            Dictionary<string, string> _s = new Dictionary<string, string>();
            string[] lines = config.Split(new string[] { lineSep }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var l in lines)
            {
                string t = l.Trim();
                if (t[0] == '#')
                {

                }
                else
                {
                    string[] kv = t.Split(new string[] { kvSep }, StringSplitOptions.None);
                    if (kv.Length == 2)
                    {
                        _s.Add(kv[0].Trim(), kv[1].Trim());
                    }
                }
            }
            return _s;
        }
    }
}
