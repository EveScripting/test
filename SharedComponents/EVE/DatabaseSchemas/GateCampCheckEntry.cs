using Newtonsoft.Json;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.DatabaseSchemas
{
    public class GateCampCheckEntry
    {
        [AutoIncrement]

        [Index]
        public int Index { get; set; }
        public int SolarSystemId { get; set; }
        public string SolarSystemEntryJson { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool IsPremium { get; set; }
    }
}
