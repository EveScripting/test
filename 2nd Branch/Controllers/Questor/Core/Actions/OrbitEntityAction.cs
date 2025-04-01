using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void OrbitEntityAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            ESCache.Instance.NormalNavigation = false;

            string target = action.GetParameterValue("target");

            bool notTheClosest;
            if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
                notTheClosest = false;

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
            {
                Log.WriteLine("No Entity Specified to orbit: skipping OrbitEntity Action");
                NextAction(myMission, myAgent, true);
                return;
            }

            IEnumerable<EntityCache> targets = ESCache.Instance.EntitiesByPartialName(target).ToList();
            if (!targets.Any())
            {
                // Unlike activate, no target just means next action
                NextAction(myMission, myAgent, true);
                return;
            }

            EntityCache closest = targets.OrderBy(t => t.Distance).FirstOrDefault();

            if (notTheClosest)
                closest = targets.OrderByDescending(t => t.Distance).FirstOrDefault();

            if (closest != null)
            {
                int orbitDistance = NavigateOnGrid.OrbitDistanceToUse;
                if (orbitDistance == 0) orbitDistance = 1000;
                // Move to the target
                if (closest.Orbit(orbitDistance, false, "Setting [" + closest.Name + "][" + closest.MaskedId + "][" + Math.Round(closest.Distance / 1000, 0) + "k away as the Orbit Target]"))
                {
                    NextAction(myMission, myAgent, true);
                }
            }
            else
            {
                NextAction(myMission, myAgent, true);
            }
        }

        #endregion Methods
    }
}