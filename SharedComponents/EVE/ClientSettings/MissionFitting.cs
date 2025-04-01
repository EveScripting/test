using System;
using System.Xml.Serialization;

namespace SharedComponents.EVE.ClientSettings
{
    [Serializable]
    public class MissionFitting
    {
        public MissionFitting()
        {
            
        }
        public string Mission { get; set; }
        public string FittingName { get; set; }
        public int? DronetypeId { get; set; }
    }
}