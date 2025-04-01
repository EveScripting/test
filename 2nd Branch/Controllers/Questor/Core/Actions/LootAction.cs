extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Questor.BackgroundTasks;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using SC::SharedComponents.EVE;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static DateTime? _lootActionTimeout = DateTime.UtcNow;

        private static void LootAction(Action action)
        {
            try
            {
                List<string> items = action.GetParameterValues("item");
                List<string> targetNames = action.GetParameterValues("target");

                // if we are not generally looting we need to re-enable the opening of wrecks to
                // find this LootItems we are looking for
                Salvage.OpenWrecks = true;

                if (!Salvage.LootEverything)
                    if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Any(i => items.Contains(i.TypeName, StringComparer.OrdinalIgnoreCase)))
                    {
                        Log.WriteLine("LootEverything:  We are done looting");

                        // now that we are done with this action revert OpenWrecks to false

                        if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                        {
                            if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                            Salvage.OpenWrecks = false;
                        }

                        NextAction(null, null);
                        return;
                    }

                // unlock targets count

                //
                // sorting by distance is bad if we are moving (we'd change targets unpredictably)... sorting by ID should be better and be nearly the same(?!)
                //
                if (ESCache.Instance.Containers == null) return;
                IOrderedEnumerable<EntityCache> containers = ESCache.Instance.Containers.Where(e => !ESCache.Instance.LootedContainers.Contains(e.Id))
                    .OrderBy(i => i.IsWreck)
                    .ThenBy(e => e.IsWreckEmpty)
                    .ThenBy(e => e.Distance);

                if (DebugConfig.DebugLootWrecks)
                {
                    int i = 0;
                    foreach (EntityCache _container in containers)
                    {
                        i++;
                        Log.WriteLine("[" + i + "] " + _container.Name + "[" + Math.Round(_container.Distance / 1000, 0) + "k] isWreckEmpty [" +
                                      _container.IsWreckEmpty +
                                      "] IsTarget [" + _container.IsTarget + "]");
                    }

                    i = 0;
                    foreach (long _containerToLoot in ESCache.Instance.ListofContainersToLoot)
                    {
                        i++;
                        Log.WriteLine("_containerToLoot [" + i + "] ID[ " + _containerToLoot + " ]");
                    }
                }

                /**
                if (Salvage.CachedUnlootedWrecksAndContainersTimeStamp != null && Salvage.CachedUnlootedWrecksAndContainersTimeStamp.Value.AddMinutes(6) > DateTime.UtcNow)
                    if (ESCache.Instance.UnlootedWrecksAndSecureCans.Count() == Salvage.CachedUnlootedWrecksAndContainers)
                    {
                        //
                        // it has been 6 minutes and we havent looted ANY wrecks / cans?
                        //
                        Log.WriteLine("We have been processing this loot action for over 6 minutes and the number of unlooted wrecks hasnt changed: GotoBase");
                        CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                        return;
                    }
                **/

                //
                // add containers that we were told to loot into the ListofContainersToLoot so that they are prioritized by the background salvage routine
                //
                if (targetNames != null && targetNames.Count > 0)
                    foreach (EntityCache continerToLoot in containers)
                        if (continerToLoot.Name == targetNames.FirstOrDefault())
                            if (!ESCache.Instance.ListofContainersToLoot.Contains(continerToLoot.Id))
                                ESCache.Instance.ListofContainersToLoot.Add(continerToLoot.Id);

                EntityCache container = containers.Where(i => !i.IsWreckEmpty).OrderBy(i => i.Distance).FirstOrDefault(c => targetNames != null && targetNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase)) ?? containers.Where(i => !i.IsWreckEmpty).OrderBy(i => i.Distance).FirstOrDefault();
                if (container != null)
                    if (container.Distance > (int)Distances.ScoopRange)
                        NavigateOnGrid.NavigateToTarget(container, 0);

                // Do we have a timeout?  No, set it to now + 3 seconds
                if (!_lootActionTimeout.HasValue)
                {
                    _lootActionTimeout = DateTime.UtcNow.AddSeconds(3);
                }

                // Are we in timeout?
                if (DateTime.UtcNow < _lootActionTimeout.Value) return;

                if (!containers.Any() || containers.All(i => i.GroupId != (int)Group.SpawnContainer && i.IsWreckEmpty))
                {
                    // lock targets count
                    Log.WriteLine("We are done looting");

                    // now that we are done with this action revert OpenWrecks to false
                    if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                    {
                        if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                        Salvage.OpenWrecks = false;
                    }

                    NextAction(null, null);
                    return;
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception logged was [" + exception + "]");
            }
        }

        #endregion Methods
    }
}