using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions.Base;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void UseDronesAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            bool usedrones;
            if (!bool.TryParse(action.GetParameterValue("use"), out usedrones))
                usedrones = false;

            if (!usedrones)
            {
                Log.WriteLine("Disable launch of drones");
                MissionSettings.PocketUseDrones = false;
            }
            else
            {
                Log.WriteLine("Enable launch of drones");
                MissionSettings.PocketUseDrones = true;
            }
            NextAction(myMission, myAgent, true);
        }

        #endregion Methods
    }
}