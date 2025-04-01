using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using System.Linq;
using EVESharpCore.Framework;
using EVESharpCore.Questor.Combat;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void DoneAction(DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (myMission != null && myAgent != null)
                if (_doneActionAttempts == 0)
                {
                    if (ChooseNextActionsBasedOnMissionObjective(myMission, myAgent))
                    {
                        if (_pocketActions.Any()) Log.WriteLine("ChooseNextActionsBasedOnMissionObjective: CurrentAction is now [" + _pocketActions[_currentActionNumber] + "]!");
                        _doneActionAttempts++;
                        return;
                    }

                    _doneActionAttempts++;
                }

            // If we are not warp scrambled Tell the drones module to retract drones
            if (ESCache.Instance.EntitiesOnGrid.Any(t => t.IsWarpScramblingMe))
            {
                EntityCache warpScrambler = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(w => w.IsWarpScramblingMe);
                Log.WriteLine("[" + warpScrambler + "] is scrambling me. waiting for combat to kill this target so we can leave the mission.");
                return;
            }

            //
            // if we are scrambled (above) we wait until we are no longer scrambled before proceeding
            //
            ReallyDoneAction(myMission, myAgent);
        }

        #endregion Methods
    }
}