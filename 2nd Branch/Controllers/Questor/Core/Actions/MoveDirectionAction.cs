using EVESharpCore.Cache;
using EVESharpCore.Logging;
using System;
using EVESharpCore.Framework;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void MoveDirectionAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            // Default timeout is 30 seconds
            int timeout;
            if (!int.TryParse(action.GetParameterValue("timeout"), out timeout))
                timeout = 15;

            if (_waiting)
            {
                if (DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds < timeout)
                    return;

                Log.WriteLine("Done moving up for [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                NextAction(myMission, myAgent, true);
                return;
            }

            ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdFlightControlsUp);
            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
        }

        #endregion Methods
    }
}