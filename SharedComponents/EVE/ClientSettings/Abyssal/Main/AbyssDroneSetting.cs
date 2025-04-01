using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings.Abyssal.Main
{
    [Serializable]
    public class AbyssDrone
    {
        //[Browsable(false)]
        //public DroneSize Type { get; set; }

        public int TypeId { get; set; }

        public int Amount { get; set; }

        [Description("Check this if the drone type is mutated and set the TypeId to the original (non mutated) TypeId.")]
        public bool Mutated { get; set; }
    }

    [Serializable]
    public enum DroneSize
    {
        Small,
        Medium,
        Large,
    }


}
