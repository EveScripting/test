using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void WaitUntilAggressedAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            // Default timeout is 60 seconds
            int timeout;
            if (!int.TryParse(action.GetParameterValue("timeout"), out timeout))
                timeout = 60;

            int WaitUntilShieldsAreThisLow;
            if (int.TryParse(action.GetParameterValue("WaitUntilShieldsAreThisLow"), out WaitUntilShieldsAreThisLow))
                MissionSettings.MissionActivateRepairModulesAtThisPerc = WaitUntilShieldsAreThisLow;

            int WaitUntilArmorIsThisLow;
            if (int.TryParse(action.GetParameterValue("WaitUntilArmorIsThisLow"), out WaitUntilArmorIsThisLow))
                MissionSettings.MissionActivateRepairModulesAtThisPerc = WaitUntilArmorIsThisLow;

            if (_waiting)
            {
                if (DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds < timeout && Combat.Combat.PotentialCombatTargets.All(i => !i.IsAttacking))
                    return;

                Log.WriteLine("Nothing targeted us within [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                NextAction(myMission, myAgent, true);
                return;
            }

            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
        }

        #endregion Methods
    }
}