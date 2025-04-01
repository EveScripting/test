using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings
{
    [Serializable]
    public class QuestorFaction
    {
        public QuestorFaction()
        {

        }
        [Browsable(false)]
        public FactionType FactionType { get; set; }
    }
}
