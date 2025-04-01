using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace SharedComponents.EVE.DatabaseSchemas
{

    public class CachedWebsiteEntry
    {
        [AutoIncrement]
        public int Index { get; set; }

        [Index]
        public string Url { get; set; }

        public string Source { get; set; }

        public DateTime LastUpdate { get; set; }
    }
}
