using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.EVE.ClientSettings.Global.Main
{
    [Serializable]
    public class GlobalMainSetting
    {
        public bool Disable3D { get; set; } = true;

        public bool FPSLimit { get; set; } = true;

        [Description("Attempts to reduce the working set memory during a session change.")]
        public bool ClearMemoryDuringSessionChange { get; set; } = true;

        [Description("Record a video of this instance.")]
        public bool RecordVideo { get; set; } = false;

        public bool SISI { get; set; } = false;
        [Description("A comma seperated list of system names to exclude from the traveller.")]
        public string TravellerSystemsToAvoid { get; set; } = "Ahbazon, Tama, Olettiers, Crielere";
        public bool DebugControllerManager { get; set; } = false;

        [Description("A comma seperated list of charname(s) to auto manage a fleet. You can include yourself. You need to be in any non-local chat channel with the characters. (Corp or any other)")]
        public string AutoFleetMembers { get; set; }
    }
}
