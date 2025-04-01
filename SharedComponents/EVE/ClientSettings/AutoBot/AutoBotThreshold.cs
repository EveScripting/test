using SharedComponents.EVE.ClientSettings.Abyssal.Main;
using SharedComponents.EVE.ClientSettings.AbyssalGuard.Main;
using SharedComponents.EVE.ClientSettings.SharedComponents.EVE.ClientSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings.AutoBot
{
    [Copyable]
    [Serializable]
    public class AutoBotThreshold
    {
        [TabPage("Requirements")]
        public AutoBotRequirements AutoBotRequirements { get; set; } = new AutoBotRequirements();

        [TabPage("Settings")]
        public AutoBotControllerSettings ControllerSettings { get; set; } = new AutoBotControllerSettings();
    }
}
