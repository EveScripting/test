using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SharedComponents.Extensions;

namespace SharedComponents.EVE.Models
{
    /// <summary>
    /// Has to be manually deserialized because the API returns a top level dictionary with keys being solar system IDs
    /// </summary>
    [JsonConverter(typeof(EveGateCheckResponseJsonConverter))]
    public class EveGateCheckResponse
    {
        public bool IsPremium { get; set; }
        public double RequestTimeTaken { get; set; }
        public Dictionary<int, SolarSystemEntry> SolarSystemKills { get; set; } = new Dictionary<int, SolarSystemEntry>();
    }

    public class SolarSystemEntry
    {
        [JsonProperty("kills")]
        public SolarSystemKills Kills { get; set; }
    }

    public class SolarSystemKills
    {
        [JsonProperty("killCount")] 
        public int KillCountLastHour { get; set; }
        [JsonProperty("gateKillCount")] 
        public int GateKillCountLastHour { get; set; }

        [JsonProperty("data")]
        public Dictionary<string, KillsInfo> KillInfos { get; set; } = new Dictionary<string, KillsInfo>();
        
        [JsonIgnore] 
        private Dictionary<string, KillsInfo> _gateKills;
        
        [JsonIgnore]
        public Dictionary<string, KillsInfo> GateKills => _gateKills ??= this.KillInfos
            .Where(k => k.Key != "Not on a gate")
            .ToDictionary(x => 
                x.Key, x => 
                x.Value);
        
        [JsonIgnore] 
        public KillsInfo NonGateKills => this.KillInfos.GetValueOrDefault("Not on a gate");
    }

    public class KillsInfo
    {
        [JsonProperty("killCount")]
        public int KillCountLastHour { get; set; }
        [JsonProperty("checks")]
        public Checks Checks { get; set; }
    }
    
    public class Checks
    {
        [JsonProperty("smartbombs", NullValueHandling = NullValueHandling.Ignore)]
        public bool Smartbombs { get; set; } = false;
        [JsonProperty("dictors", NullValueHandling = NullValueHandling.Ignore)]
        public bool Dictors { get; set; } = false;
        [JsonProperty("hictors", NullValueHandling = NullValueHandling.Ignore)]
        public bool Hictors { get; set; } = false;
    }

// {
// "premium": false,
// "30005195": {
//     "kills": {
//         "killCount": 3,
//         "gateKillCount": 3,
//         "data": {
//             "Cleyd": {
//                 "killCount": 2,
//                 "checks": {
//                     "smartbombs": null,
//                     "dictors": null,
//                     "hictors": true
//                 }
//             },
//             "Tarta": {
//                 "killCount": 1,
//                 "checks": {
//                     "smartbombs": null,
//                     "dictors": null,
//                     "hictors": true
//                 }
//             }
//         }
//     }
// },
// "tot_time": 0.033236026763916016,
// "esi_cache": null
// }
}