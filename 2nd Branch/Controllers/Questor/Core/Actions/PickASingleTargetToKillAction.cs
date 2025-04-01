using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Properties

        private static long _singleTargetToEliminate { get; set; }

        #endregion Properties

        #region Methods

        private static void PickASingleTargetToKillAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                bool notTheClosest;
                if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
                    notTheClosest = false;

                List<string> targetNames = action.GetParameterValues("target");

                // No parameter? Ignore kill action
                if (!targetNames.Any())
                {
                    Log.WriteLine("No targets defined in kill action!");
                    NextAction(myMission, myAgent, true);
                    return;
                }

                List<EntityCache> killTargets = ESCache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderBy(e => e.Distance).ToList();

                if (notTheClosest)
                    killTargets = ESCache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderByDescending(e => e.Distance).ToList();

                if (killTargets.Any())
                {
                    _singleTargetToEliminate = killTargets.FirstOrDefault().Id;
                    if (_singleTargetToEliminate != 0)
                    {
                        Log.WriteLine("PickASingleTargetToKill: [" + killTargets.FirstOrDefault().Name + "] @ [" + Math.Round(killTargets.FirstOrDefault().Distance / 1000, 0) + "k] ID [" + killTargets.FirstOrDefault().MaskedId + "]");
                        NextAction(myMission, myAgent, true);
                        return;
                    }
                }

                NextAction(myMission, myAgent, true);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }
}