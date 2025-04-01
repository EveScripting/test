using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings.Abyssal.Main
{
    [Serializable]
    public class AbyssBooster
    {
        public int TypeId { get; set; }
        public int Amount { get; set; }
    }

}
