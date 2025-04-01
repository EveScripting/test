extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers;
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

        private static DateTime? _lootFactionOnlyActionTimeout = DateTime.UtcNow;

        private static void LootFactionOnlyAction(Action action)
        {
            try
            {
                // if we are not generally looting we need to re-enable the opening of wrecks to
                // find this LootItems we are looking for
                Salvage.OpenWrecks = true;

                // unlock targets count
                //Salvage.MissionLoot = true;

                //
                // sorting by distance is bad if we are moving (we'd change targets unpredictably)... sorting by ID should be better and be nearly the same(?!)
                //
                if (ESCache.Instance.Containers == null) return;
                IOrderedEnumerable<EntityCache> containers = ESCache.Instance.Containers.Where(e => e.IsPossibleToDropFactionModules && !e.IsWreckEmpty && !ESCache.Instance.LootedContainers.Contains(e.Id))
                    .OrderBy(i => i.IsWreck)
                    .ThenBy(e => e.Distance);

                if (containers == null || !containers.Any())
                {
                    WeAreDoneLooting();
                    return;
                }

                //if (Salvage.CachedUnlootedWrecksAndContainersTimeStamp != null && Salvage.CachedUnlootedWrecksAndContainersTimeStamp.Value.AddMinutes(6) > DateTime.UtcNow)
                //    if (ESCache.Instance.UnlootedWrecksAndSecureCans.Count() == Salvage.CachedUnlootedWrecksAndContainers)
                //    {
                //        //
                //        // it has been 6 minutes and we havent looted ANY wrecks / cans?
                //        //
                //        Log.WriteLine("We have been processing this loot action for over 6 minutes and the number of unlooted wrecks hasnt changed: GotoBase");
                //        WeAreDoneLooting();
                //        return;
                //    }

                //
                // add containers that we were told to loot into the ListofContainersToLoot so that they are prioritized by the background salvage routine
                //

                EntityCache container = containers.Where(i => !i.IsWreckEmpty).OrderBy(i => i.Distance).FirstOrDefault();

                if (container == null)
                {
                    if (ESCache.Instance.Entities.Any(i => i.GroupId == (int)Group.DeadSpaceOverseersBelongings && (double)Distances.ScoopRange > i.Distance))
                    {
                        container = ESCache.Instance.Entities.Find(i => i.GroupId == (int)Group.DeadSpaceOverseersBelongings && (double)Distances.ScoopRange > i.Distance);
                        if (container == null)
                        {
                            if (ESCache.Instance.Entities.Any(i => i.Name == "Cargo Container") && ESCache.Instance.Entities.Any(i => i.Name == "Supply Crate"))
                            {
                                container = ESCache.Instance.Entities.Find(i => i.Name == "Cargo Container" && ESCache.Instance.Entities.Any(x => x.Name == "Supply Crate") && (double)Distances.ScoopRange > i.Distance);
                            }
                        }
                    }
                }

                if (container != null)
                    if (container.Distance > (int)Distances.ScoopRange)
                        NavigateOnGrid.NavigateToTarget(container, 0);

                // Do we have a timeout?  No, set it to now + 3 seconds
                if (!_lootFactionOnlyActionTimeout.HasValue)
                {
                    _lootFactionOnlyActionTimeout = DateTime.UtcNow.AddSeconds(3);
                }

                // Are we in timeout?
                if (DateTime.UtcNow < _lootFactionOnlyActionTimeout.Value) return;

                if (!containers.Any() || containers.All(i => i.GroupId != (int)Group.SpawnContainer && i.IsWreckEmpty))
                {
                    WeAreDoneLooting();
                    return;
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception logged was [" + exception + "]");
            }
        }

        private static void WeAreDoneLooting()
        {
            // lock targets count
            Log.WriteLine("We are done looting");

            // now that we are done with this action revert OpenWrecks to false

            if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }

            //Salvage.MissionLoot = false;
            NextAction(null, null);
        }

        #endregion Methods
    }
}