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

        private static void WaitForWreckAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            string target = action.GetParameterValue("target");
            if (string.IsNullOrEmpty(target))
                target = "Triglavian Bioadaptive Cache Wreck"; //fix this to detect the lower tier wreck as well? Biocombinative Cache Wreck

            if (ESCache.Instance.Wrecks.Any(i => i.Name.ToLower() == target.ToLower()))
            {
                Log.WriteLine("We found a wreck named [" + target + "] on grid!");
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

                Log.WriteLine("We found no wrecks within [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                NextAction(myMission, myAgent, true);
                return;
            }

            Log.WriteLine("We have no wrecks on grid yet: waiting up to [ " + timeout + "sec] starting now.");
            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
        }

        #endregion Methods
    }
}