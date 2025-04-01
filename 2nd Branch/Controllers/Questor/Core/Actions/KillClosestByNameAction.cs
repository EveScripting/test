using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void KillClosestByNameAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            List<string> targetNames = action.GetParameterValues("target");

            // No parameter? Ignore kill action
            if (targetNames.Count == 0)
            {
                Log.WriteLine("No targets defined!");
                NextAction(myMission, myAgent, true);
                return;
            }

            //
            // the way this is currently written is will NOT stop after killing the first target as intended, it will clear all targets with the Name given
            //

            Combat.Combat.AddPrimaryWeaponPriorityTarget(
                Combat.Combat.PotentialCombatTargets.Where(t => targetNames.Contains(t.Name)).OrderBy(t => t.Distance).Take(1).FirstOrDefault(),
                PrimaryWeaponPriority.PriorityKillTarget);

            //if (Combat.Combat.GetBestPrimaryWeaponTarget((double)Distances.OnGridWithMe, false, "combat",
            //    Combat.Combat.PotentialCombatTargets.OrderBy(t => t.Distance).Take(1).ToList()))
            //    _clearPocketTimeout = null;

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue) _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value) return;

            // We have cleared the Pocket, perform the next action \o/ - reset the timers that we had set for actions...
            NextAction(myMission, myAgent, true);

            // Reset timeout
            _clearPocketTimeout = null;
        }

        #endregion Methods
    }
}