using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings
{
    [Serializable]
    public class ShipFitting
    {
        public ShipFitting()
        {

        }

        public ShipFitting(string fittingName, string b64Fitting)
        {
            FittingName = fittingName;
            B64Fitting = b64Fitting;
        }

        public String FittingName { get; set; }
        public String B64Fitting { get; set; }
    }
}
