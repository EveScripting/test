using SharedComponents.EVE.StaticData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings.AutoBot
{
    [Serializable]
    public class AutoBotSkillRequirement
    {
        [Browsable(false)]
        [EnumOrdering(EnumOrderingAttribute.EnumOrdering.Name)]
        public Skills Skill { get; set; }

        public int Level { get; set; }
    }
}
