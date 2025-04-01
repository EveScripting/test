using EVESharpCore.Cache;
using EVESharpCore.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void KeepAtRangeToBackgroundAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                return;

            Log.WriteLine("KeepAtRangeToBackground was called.");

            //we cant move in bastion mode, do not try
            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                return;

            int Distance;
            if (!int.TryParse(action.GetParameterValue("distance"), out Distance))
                Distance = 5000;

            string target = action.GetParameterValue("target");
            if (string.IsNullOrEmpty(target))
                target = action.GetParameterValue("Target");

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
                target = "Acceleration Gate";

            IEnumerable<EntityCache> targets = Combat.Combat.PotentialCombatTargets.Where(i => i.IsOnGridWithMe).OrderByDescending(i => i.Name.ToLower() == target.ToLower()).ToList();

            if (!targets.Any())
            {
                Log.WriteLine("KeepAtRange unable to find target named [" + target + "] on grid! NextAction");
                // Unlike activate, no target just means next action
                NextAction(myMission, myAgent, true);
                return;
            }

            ESCache.Instance.NormalNavigation = false;

            EntityCache closest = targets.OrderBy(t => t.Distance).FirstOrDefault();

            if (closest != null)
            {
                Log.WriteLine("Attempting to KeepAtRange @ [" + Math.Round((double)Distance / 1000, 0) + "] to [" + closest.Name + "] Current Distance [" + Math.Round(closest.Distance / 1000, 0) + "k]");
                if (closest.KeepAtRange(Distance, true, true))
                {
                    Log.WriteLine("KeepAtRange Done");
                    NextAction(myMission, myAgent, true);
                    _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(1);
                    return;
                }

                Log.WriteLine("KeepAtRange returned false");
                return;
            }

            Log.WriteLine("KeepAtRange unable to find target named [" + target + "] on grid!");
            return;
        }

        #endregion Methods
    }
}