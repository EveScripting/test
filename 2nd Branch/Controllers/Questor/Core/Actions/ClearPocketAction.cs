extern alias SC;
using System;
using System.Diagnostics;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using SC::SharedComponents.Utility;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        private static Stopwatch stopwatch = new Stopwatch();

        #region Methods

        private static void ClearPocketAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            stopwatch.Restart();

            try
            {
                // Get lowest range
                int DistanceToClear;
                if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToClear))
                    DistanceToClear = (int)Combat.Combat.MaxRange;

                if (DistanceToClear != 0 && DistanceToClear != -2147483648 && DistanceToClear != 2147483647)
                    DistanceToClear = (int)Distances.OnGridWithMe;

                string continueWhenWeHaveThisItemInCargo = string.Empty;
                if (!string.IsNullOrEmpty(action.GetParameterValue("continueWhenWeHaveThisItemInCargo")))
                {
                    Log.WriteLine("ClearPocket: We are looking for [" + continueWhenWeHaveThisItemInCargo + "] in our cargo");
                    continueWhenWeHaveThisItemInCargo = action.GetParameterValue("continueWhenWeHaveThisItemInCargo");
                }

                int continueWhenWeHaveThisNumberOfTheItemInCargo = 1;
                if (!int.TryParse(action.GetParameterValue("continueWhenWeHaveThisNumberOfTheItemInCargo"), out continueWhenWeHaveThisNumberOfTheItemInCargo))

                    //panic handles adding any priority targets and combat will prefer to kill any priority targets

                    //If the closest target is out side of our max range, combat cant target, which means GetBest cant return true, so we are going to try and use potentialCombatTargets instead

                    if (Combat.Combat.PotentialCombatTargets.Count > 0 && AbyssalDeadspaceBehavior.AssumeGatesAreLockedOrThereAreNoGates)
                    {
                        try
                        {
                            Salvage.DeployMobileTractor();
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                        }

                        Drones.DronesShouldBePulled = false;
                        if (NavigateOnGrid.ChooseNavigateOnGridTargetIds != null)
                            NavigateOnGrid.NavigateIntoRange(NavigateOnGrid.ChooseNavigateOnGridTargetIds, "ClearPocket", true);

                        _clearPocketTimeout = null;
                    }

                if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
                {
                    if (DebugConfig.DebugClearPocket)
                    {
                        int intItemInCargo = 0;
                        foreach (DirectItem item in ESCache.Instance.CurrentShipsCargo.Items)
                        {
                            intItemInCargo++;
                            Log.WriteLine("[" + intItemInCargo + "] ItemInCargo: [" + item.TypeName + "] Qroup [" + item.GroupName + "] Quantity [" + item.Quantity + "] Type ID [" + item.TypeId + "] GroupId [" + item.GroupId + "]");
                        }
                    }

                    if (!string.IsNullOrEmpty(continueWhenWeHaveThisItemInCargo) && ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeName.ToLower().Contains(continueWhenWeHaveThisItemInCargo.ToLower()) && i.Quantity >= continueWhenWeHaveThisNumberOfTheItemInCargo))
                    {
                        //
                        // if we have the item and the correct quantity cause clearpocket to timeout so that we move to the next action
                        //
                        Log.WriteLine("Found [" + continueWhenWeHaveThisNumberOfTheItemInCargo + "] items named [" + continueWhenWeHaveThisItemInCargo + "] in our cargo: Setting _clearPocketTimeout to allow clearpocket to finish");
                        _clearPocketTimeout = DateTime.UtcNow.AddSeconds(-10);
                    }
                }

                // Do we have a timeout?  No, set it to now + 5 seconds
                if (!_clearPocketTimeout.HasValue)
                {
                    _clearPocketTimeout = DateTime.UtcNow.AddSeconds(10);
                    if (ESCache.Instance.InAbyssalDeadspace)
                        _clearPocketTimeout = DateTime.UtcNow.AddSeconds(2);
                }

                // Are we in timeout?
                if (DateTime.UtcNow < _clearPocketTimeout.Value) return;

                if (!Salvage.PickupMobileTractor()) return;
                // We have cleared the Pocket, perform the next action \o/ - reset the timers that we had set for actions...
                NextAction(myMission, myAgent, true);

                // Reset timeout
                _clearPocketTimeout = null;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
            finally
            {
                stopwatch.Stop();
                if (DebugConfig.DebugClearPocket) Log.WriteLine("ClearPocketAction Took [" + Util.ElapsedMicroSeconds(stopwatch) + "]");
            }
        }

        #endregion Methods
    }
}