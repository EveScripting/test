using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework;
using EVESharpCore.Questor.Behaviors;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void MoveToBackgroundAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                return;

            Log.WriteLine("MoveToBackground was called.");

            //we cant move in bastion mode, do not try
            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                return;

            ESCache.Instance.NormalNavigation = false;

            int DistanceToApproach;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToApproach))
                DistanceToApproach = (int) Distances.GateActivationRange;

            string target = action.GetParameterValue("target");

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
                target = "Acceleration Gate";

            IEnumerable<EntityCache> targets = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == target.ToLower()).ToList();

            if (ESCache.Instance.InAbyssalDeadspace)
            {
                targets = new List<EntityCache>();
                if (!targets.Any() && ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && !ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway)
                {
                    targets = ESCache.Instance.Entities.Where(i => i.IsAbyssalDeadspaceTriglavianBioAdaptiveCache);
                }

                if (!ESCache.Instance.ActiveShip.IsFrigate && !ESCache.Instance.ActiveShip.IsAssaultShip && !ESCache.Instance.ActiveShip.IsDestroyer)
                {
                    if (!targets.Any() && ESCache.Instance.Entities.Any(i => i.Name == "Transfer Conduit (Triglavian)"))
                    {
                        targets = ESCache.Instance.Entities.Where(i => i.Name == "Transfer Conduit (Triglavian)");
                    }

                    if (!targets.Any() && ESCache.Instance.Entities.Any(i => i.Name == "Origin Conduit (Triglavian)"))
                    {
                        targets = ESCache.Instance.Entities.Where(i => i.Name == "Origin Conduit (Triglavian)");
                    }
                }
            }

            if (!targets.Any())
            {
                // Unlike activate, no target just means next action
                NextAction(myMission, myAgent, true);
                return;
            }

            EntityCache closest = targets.OrderBy(t => t.Distance).FirstOrDefault();

            if (closest != null)
            {
                if (ESCache.Instance.InAbyssalDeadspace || NavigateOnGrid.SpeedTank)
                {
                    if (closest.Orbit(500))
                    {
                        NextAction(myMission, myAgent, true);
                        _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(1);
                        return;
                    }

                    return;
                }

                if (closest.Approach())
                {
                    NextAction(myMission, myAgent, true);
                    _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(1);
                    return;
                }

                return;
            }
        }

        #endregion Methods
    }
}