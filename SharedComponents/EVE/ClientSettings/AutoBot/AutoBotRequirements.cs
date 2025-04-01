using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings.AutoBot
{
    [Serializable]
    public class AutoBotRequirements
    {
        public int Isk { get; set; }

        public BindingList<AutoBotSkillRequirement> SkillRequirements { get; set; } = new BindingList<AutoBotSkillRequirement>();
    }
}
