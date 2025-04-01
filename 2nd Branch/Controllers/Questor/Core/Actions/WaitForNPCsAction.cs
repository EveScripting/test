using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Linq;
using EVESharpCore.Cache;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using EVESharpCore.Framework;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void WaitForNPCsAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (Combat.Combat.PotentialCombatTargets.Count > 0)
            {
                Log.WriteLine("We have [" + Combat.Combat.PotentialCombatTargets.Count + "] PotentialCombatTargets on grid!");
                _waiting = false;
                SetHereToCurrentXYZCoord();
                NextAction(myMission, myAgent, true);
                return;
            }

            // Default timeout is 30 seconds
            int timeout;
            if (!int.TryParse(action.GetParameterValue("timeout"), out timeout))
                timeout = 30;

            if (_waiting)
            {
                if (DateTime.UtcNow < _waitingSince.AddSeconds(timeout))
                    return;

                Log.WriteLine("We found no PotentialCombatTargets within [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                NextAction(myMission, myAgent, true);
                return;
            }

            Log.WriteLine("We have no PotentialCombatTargets on grid yet: waiting up to [ " + timeout + "sec] starting now.");
            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
        }

        #endregion Methods
    }
}