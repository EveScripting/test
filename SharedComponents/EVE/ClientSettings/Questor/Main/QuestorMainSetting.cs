using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedComponents.EVE.ClientSettings.SharedComponents.EVE.ClientSettings;

namespace SharedComponents.EVE.ClientSettings
{
    [Serializable]
    public class QuestorMainSetting
    {
        public QuestorMainSetting()
        {

        }

        public string CombatShipName { get; set; } = "CombatShipName";
        public string TransportShipName { get; set; } = "Transportshipname";
        public string AgentName { get; set; } = "AgentName";
        public bool BuyPlex { get; set; } = true;
        public bool BuyAmmo { get; set; } = true;
        public bool DumpLoot { get; set; } = false;

        public int BuyAmmoStationID { get; set; } = 60003760;
        public int BuyAmmoDroneAmount { get; set; } = 200;
        [Description("A non empty value defines a group to synchronize questor settings between multiple chars.\nA reload is required after modifying this value.")]
        public string QuestorSettingGroup { get; set; }

        [TabPage("Debug")]
        public QuestorDebugSetting QuestorDebugSetting { get; set; } = new QuestorDebugSetting();

        public QuestorDebugSetting QDS => QuestorDebugSetting;

        [TabPage("Settings")]
        public QuestorSetting QuestorSetting { get; set; } = new QuestorSetting();

        public QuestorSetting QS => QuestorSetting;
    }
}
