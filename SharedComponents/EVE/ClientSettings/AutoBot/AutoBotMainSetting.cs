using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings.AutoBot
{
    [Serializable]
    public class AutoBotMainSetting
    {
        [ControlList]
        public BindingList<AutoBotThreshold> Thresholds { get; set; } = new BindingList<AutoBotThreshold>();
    }
}
