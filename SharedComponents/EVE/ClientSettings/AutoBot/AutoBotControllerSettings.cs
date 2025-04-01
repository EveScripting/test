using SharedComponents.EVE.ClientSettings.Abyssal.Main;
using SharedComponents.EVE.ClientSettings.AbyssalGuard.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings.AutoBot
{

    [Serializable]
    public class AutoBotControllerSettings
    {

        [SelectableType(typeof(AbyssalMainSetting), typeof(AbyssalGuardMainSetting))]
        public object Settings { get; set; } = new object();
    }
}
