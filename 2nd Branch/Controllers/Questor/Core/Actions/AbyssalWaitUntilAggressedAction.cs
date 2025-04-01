using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Combat;
using System;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void AbyssalWaitUntilAggressedAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            // Default timeout is 60 seconds
            int timeout;
            if (!int.TryParse(action.GetParameterValue("timeout"), out timeout))
                timeout = 60;

            int WaitUntilShieldsAreThisLow;
            if (int.TryParse(action.GetParameterValue("WaitUntilShieldsAreThisLow"), out WaitUntilShieldsAreThisLow))
                MissionSettings.MissionActivateRepairModulesAtThisPerc = WaitUntilShieldsAreThisLow;

            int WaitUntilArmorIsThisLow;
            if (int.TryParse(action.GetParameterValue("WaitUntilArmorIsThisLow"), out WaitUntilArmorIsThisLow))
                MissionSettings.MissionActivateRepairModulesAtThisPerc = WaitUntilArmorIsThisLow;

            if (_waiting)
            {
                if (DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds > timeout)
                {
                    Log.WriteLine("Done waiting [ " + timeout + "sec] Still have no aggro.");
                    DoneWaiting(myMission, myAgent);
                    return;
                }

                if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsAttacking))
                {
                    Log.WriteLine("Done: Waiting on Aggro took [ " + Math.Round(DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds, 0) + "sec]");
                    DoneWaiting(myMission, myAgent);
                    return;
                }

                if (Combat.Combat.PotentialCombatTargets.Any(i => i.IsSensorDampeningMe))
                {
                    Log.WriteLine("Done: Waiting on Aggro took [ " + Math.Round(DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds, 0) + "sec] We have no aggro but we do have dampening NPCs! proceed anyway");
                    DoneWaiting(myMission, myAgent);
                    return;
                }

                //
                // waiting on conditions above to be true
                //
                Log.WriteLine("Waiting for Aggro: for [" + Math.Round(DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds, 0) + "] up to [" + timeout + "] sec");
                return;
            }

            // Start waiting
            //Drones.DronesShouldBePulled = true;
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
        }

        private static void DoneWaiting(DirectAgentMission myMission, DirectAgent myAgent)
        {
            Drones.DronesShouldBePulled = false;
            _waiting = false;
            NextAction(myMission, myAgent, true);
            return;
        }

        #endregion Methods
    }
}