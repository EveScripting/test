using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using EVESharpCore.Framework;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void SalvageAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            List<string> itemsToLoot = null;
            if (action.GetParameterValues("item") != null)
                itemsToLoot = action.GetParameterValues("item");

            int quantity;
            if (!int.TryParse(action.GetParameterValue("quantity"), out quantity))
                quantity = 1;

            List<string> targetNames = action.GetParameterValues("target");

            // No parameter? Ignore salvage action
            if (targetNames.Count == 0)
            {
                Log.WriteLine("No targets defined!");
                NextAction(myMission, myAgent, true);
                return;
            }

            if (itemsToLoot == null)
            {
                Log.WriteLine(" *** No Item Was Specified In the Salvage Action! ***");
                NextAction(myMission, myAgent, true);
            }
            else if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Window.IsReady)
            {
                if (ESCache.Instance.CurrentShipsCargo.Items.Any(i => itemsToLoot.Contains(i.TypeName) && i.Quantity >= quantity))
                {
                    Log.WriteLine("We are done - we have the item(s)");

                    // now that we have completed this action revert OpenWrecks to false
                    if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                    {
                        if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                        Salvage.OpenWrecks = false;
                    }

                    NextAction(myMission, myAgent, true);
                    return;
                }
            }

            IEnumerable<EntityCache> targets = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == targetNames.FirstOrDefault().ToLower()).ToList();
            if (!targets.Any())
            {
                Log.WriteLine("no entities found named [" + targets.FirstOrDefault() + "] proceeding to next action");
                NextAction(myMission, myAgent, true);
                return;
            }

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue) _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            //
            // how do we determine success here? we assume the 'reward' for salvaging will appear in your cargo, we also assume the mission action will know what that item is called!
            //

            EntityCache closest = targets.OrderBy(t => t.Distance).FirstOrDefault();
            if (closest != null)
            {
                if (!NavigateOnGrid.NavigateToTarget(targets.FirstOrDefault(), 500)) return;

                if (Salvage.Salvagers.Count == 0)
                {
                    Log.WriteLine("this action REQUIRES at least 1 salvager! - you may need to use Mission specific fittings to accomplish this");
                    Log.WriteLine("this action REQUIRES at least 1 salvager! - disabling going to base and pausing after next dock");
                    Log.WriteLine("this action REQUIRES at least 1 salvager! - setting CombatMissionsBehaviorState to GotoBase");
                    CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                    ESCache.Instance.PauseAfterNextDock = true;
                    ESCache.Instance.DeactivateScheduleAndCloseEveAfterNextDock = true;
                }
                else if (closest.Distance < Salvage.Salvagers.Min(s => s.OptimalRange))
                {
                    if (NavigateOnGrid.SpeedTank) Salvage.OpenWrecks = true;
                    if (!Salvage.TargetWrecks(targets.ToList())) return;
                    if (!Salvage.ProcessSalvagers(targets)) return;
                }

                return;
            }

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