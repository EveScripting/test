using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void WaitUntilTargetedAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (Combat.Combat.TargetedBy.Count > 0)
            {
                Log.WriteLine("We have been targeted!");

                // We have been locked, go go go ;)
                _waiting = false;
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

                Log.WriteLine("Nothing targeted us within [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                NextAction(myMission, myAgent, true);
                return;
            }

            Log.WriteLine("Nothing has us targeted yet: waiting up to [ " + timeout + "sec] starting now.");
            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
        }

        #endregion Methods
    }
}