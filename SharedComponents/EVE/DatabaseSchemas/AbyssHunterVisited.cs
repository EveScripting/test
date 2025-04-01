using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.DatabaseSchemas
{
    public class AbyssHunterVisited
    {
        [AutoIncrement]
        public int Index { get; set; }
        public long SolarSystemId { get; set; }
        public string SolarSystemName { get; set; }
        public DateTime Viewed { get; set; }
        public string ViewedByCharacterName { get; set; }

    }
}
