using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void ClearWithinWeaponsRangeOnlyAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            // Get lowest range
            int DistanceToClear;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToClear))
                DistanceToClear = (int)Combat.Combat.MaxRange - 1000;

            if (DistanceToClear == 0 || DistanceToClear == -2147483648 || DistanceToClear == 2147483647)
                DistanceToClear = (int)Distances.OnGridWithMe;

            //
            // note this WILL clear sentries within the range given... it does NOT respect the KillSentries setting. 75% of the time this wont matter as sentries will be outside the range
            //

            foreach (EntityCache combatTarget in Combat.Combat.PotentialCombatTargets.Where(i => !i.IsPrimaryWeaponPriorityTarget && DistanceToClear > i.Distance).OrderBy(t => t.Distance))
            {
                Combat.Combat.AddPrimaryWeaponPriorityTarget(combatTarget, PrimaryWeaponPriority.PriorityKillTarget);
                _clearPocketTimeout = null;
            }

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue)
                _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value)
                return;

            Log.WriteLine("is complete: no more targets in weapons range");
            NextAction(myMission, myAgent, true);

            // Reset timeout
            _clearPocketTimeout = null;
        }

        #endregion Methods
    }
}