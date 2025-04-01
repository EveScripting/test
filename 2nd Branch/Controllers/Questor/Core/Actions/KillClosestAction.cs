using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using System;
using System.Linq;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void KillClosestAction(DirectAgentMission myMission, DirectAgent myAgent)
        {
            //
            // the way this is currently written is will NOT stop after killing the first target as intended, it will clear all targets with the Name given, in this everything on grid
            //

            Combat.Combat.AddPrimaryWeaponPriorityTarget(Combat.Combat.PotentialCombatTargets.OrderBy(t => t.Distance).FirstOrDefault(),
                PrimaryWeaponPriority.PriorityKillTarget);

            //if (Combat.Combat.GetBestPrimaryWeaponTarget((double)Distances.OnGridWithMe, false, "combat",
            //    Combat.Combat.PotentialCombatTargets.OrderBy(t => t.Distance).Take(1).ToList()))
            //    _clearPocketTimeout = null;
            //}

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