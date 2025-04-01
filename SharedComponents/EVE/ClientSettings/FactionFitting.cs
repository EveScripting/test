using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedComponents.EVE.ClientSettings
{
    [Serializable]
    public class FactionFitting
    {
        public FactionFitting()
        {
            
        }

        [Browsable(false)]
        public FactionType FactionType { get; set; }
        public string FittingName { get; set; }
        public int? DronetypeId { get; set; }
    }
}