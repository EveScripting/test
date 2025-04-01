using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings.AbyssalHunter.Main
{
    [Serializable]
    public class AbyssalHunterMainSetting
    {
        public AbyssalHunterMainSetting()
        {

        }

        [Description("A comma seperated list of the slaves. Example: Slave1,Slave2,Slave3")]
        public string SlaveCharacterNames { get; set; }
        public int BroadcastDelayMinimum { get; set; } = 320;
        public int BroadcastDelayMaximum { get; set; } = 600;
        public bool ResetConcordeOnStation { get; set; }
    }
}