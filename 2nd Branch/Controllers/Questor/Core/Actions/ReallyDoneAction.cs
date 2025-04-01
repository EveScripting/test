using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using System.Linq;
using EVESharpCore.Framework;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void ReallyDoneAction(DirectAgentMission myMission, DirectAgent myAgent)
        {
            // If we are not warp scrambled Tell the drones module to retract drones
            if (ESCache.Instance.EntitiesOnGrid.Any(t => t.IsWarpScramblingMe))
            {
                Log.WriteLine("CombatMissionCtrl: ReallyDone: Waiting for us to no longer be scrambled! Combat/Drones should handle it...");
                return;
            }

            Log.WriteLine("ReallyDoneAction");

            MissionSettings.MissionUseDrones = null;
            _doneActionAttempts = 0;

            if (Drones.ActiveDroneCount > 0)
            {
                Drones.DronesShouldBePulled = true;
                Log.WriteLine("CombatMissionCtrl: ReallyDone: We still have drones out! Wait for them to return.");
                if (Drones.WaitForDronesToReturn)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("if (WaitForDronesToReturn)");
                    return;
                }
            }

            // Add bookmark (before we're done)
            if (Salvage.CreateSalvageBookmarks)
                if (!BookmarkPocketForSalvaging(myMission))
                    if (DebugConfig.DebugDoneAction) Log.WriteLine("Wait for CreateSalvageBookmarks to return true (it just returned false!)");

            //
            // we are ready and can set the "done" State.
            //
            LogCurrentMissionActions(myMission, myAgent);
            ChangeCombatMissionCtrlState(ActionControlState.Done, myMission, myAgent);
            if (DebugConfig.DebugDoneAction) Log.WriteLine("we should be ready to complete the mission and have set [ State.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Done ]");
        }

        #endregion Methods
    }
}