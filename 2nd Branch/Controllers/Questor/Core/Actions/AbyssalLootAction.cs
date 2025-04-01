extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Stats;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using SC::SharedComponents.EVE;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void AbyssalLootAction_WeAreDoneLooting(string WeAreDoneLootingLogMessage)
        {
            Log.WriteLine(WeAreDoneLootingLogMessage);
            ESCache.Instance.DirectEve.ExecuteCommand(Framework.DirectCmd.CmdSetShipFullSpeed);
            // now that we are done with this action revert OpenWrecks to false

            if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }

            foreach (EntityCache wreck in ESCache.Instance.Containers)
            {
                Log.WriteLine("PocketNumber: [" + ActionControl.PocketNumber + "]  Wreck: [" + wreck.Name + "] at [" + wreck.Nearest1KDistance + "] IsWreckEmpty [" + wreck.IsWreckEmpty + "] IsTractorActive [" + wreck.IsTractorActive + "] TypeId [" + wreck.TypeId + "] GroupID [" + wreck.GroupId + "] ID [" + wreck.Id + "]");
            }

            //Salvage.MissionLoot = false;
            NextAction(null, null);
            return;
        }

        private static void AbyssalLootAction(Action action)
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
                List<EntityCache> containers = ESCache.Instance.Wrecks.Where(x => !x.IsWreckEmpty && (x.IsAbyssalCacheWreck || x.IsInTractorRange))
                    .OrderBy(e => e.Distance).ToList();

                if (DebugConfig.DebugLootWrecks)
                {
                    int i = 0;
                    foreach (EntityCache container in ESCache.Instance.Wrecks.Where(x => !x.IsWreckEmpty))
                    {
                        i++;
                        Log.WriteLine("[" + i + "] " + container.Name + "[" + Math.Round(container.Distance / 1000, 0) + "k] isWreckEmpty [" +
                                      container.IsWreckEmpty +
                                      "] IsTarget [" + container.IsTarget + "] IsInTractorRange [" + container.IsInTractorRange + "]");
                    }

                    i = 0;
                    foreach (long containerToLoot in ESCache.Instance.ListofContainersToLoot)
                    {
                        i++;
                        Log.WriteLine("_containerToLoot [" + i + "] ID[ " + containerToLoot + " ]");
                    }
                }

                foreach (EntityCache wreck in ESCache.Instance.Wrecks)
                {
                    Log.WriteLine("PocketNumber: [" + ActionControl.PocketNumber + "] Wreck: [" + wreck.Name + "] at [" + wreck.Nearest1KDistance + "] IsTarget [" + wreck.IsTarget + "] IsWreckEmpty [" + wreck.IsWreckEmpty + "] IsTractorActive [" + wreck.IsTractorActive + "] TypeId [" + wreck.TypeId + "] GroupID [" + wreck.GroupId + "] ID [" + wreck.Id + "]");
                }

                if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs && ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway)
                {
                    AbyssalLootAction_WeAreDoneLooting("LootAction: We are in the room with tons of BSs, gate should be unlocked: skip looting");
                    return;
                }

                //
                // in Abyssal Deadspace if we have no Triglavian Bioadaptive Cache Wreck yet, wait: they are indestructable, so if we dont have one we arent ready yet
                //
                if (!ESCache.Instance.Wrecks.Any())
                {
                    Log.WriteLine("LootAction: There are no wrecks on grid yet, waiting");
                    return;
                }

                if (DateTime.UtcNow > _currentActionStarted.AddSeconds(Salvage.SecondsWaitForLootAction))
                {
                    if (ESCache.Instance.ActiveShip.Entity.IsCruiser && (Salvage.TractorBeams.Any() && Salvage.TractorBeams.All(i => !i.IsActive)) || DateTime.UtcNow > _currentActionStarted.AddSeconds(Salvage.SecondsWaitForLootAction + 30))
                    {
                        try
                        {
                            Statistics.WriteWreckOutOfRangeLootSkipLog();
                        }
                        catch (Exception)
                        {
                            //ignore this exception
                        }

                        AbyssalLootAction_WeAreDoneLooting("LootAction: We must be stuck: skipping loot! secondsWaitForLootAction [" + Salvage.SecondsWaitForLootAction + "] sec have passed and all tractors are off");
                        return;
                    }
                }

                if (Salvage.TractorBeams.Any() && containers.Any(i => !i.IsTarget && !i.IsTargeting))
                {
                    if (!Salvage.TargetWrecks(containers.Where(i => !i.IsTarget && !i.IsTargeting).ToList())) return;
                }

                if (containers.Any())
                {
                    if (!Salvage.ProcessTractorBeams()) return;
                    if (!Salvage.LootWrecks()) return;
                }

                if (!containers.Any())
                {
                    AbyssalLootAction_WeAreDoneLooting("LootAction: We are done looting");
                }

                //
                // add containers that we were told to loot into the ListofContainersToLoot so that they are prioritized by the background salvage routine
                //

                foreach (EntityCache containerToLoot in containers)
                        if (!ESCache.Instance.ListofContainersToLoot.Contains(containerToLoot.Id))
                            ESCache.Instance.ListofContainersToLoot.Add(containerToLoot.Id);

                EntityCache wreckToLoot = null;
                if (ESCache.Instance.Containers == null) return;
                if (ESCache.Instance.Containers.Any(i => i.IsWreck && !i.IsWreckEmpty))
                {
                    wreckToLoot = ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty);
                    if (wreckToLoot != null)
                    {
                        if (Salvage.TractorBeams.Count > 0 && Salvage.TractorBeamRange != null)
                        {
                            if (wreckToLoot.Distance > Salvage.TractorBeamRange - 1000)
                            {
                                if (Salvage.TractorBeams.All(i => !i.IsActive) && DateTime.UtcNow > Time.Instance.NextTractorBeamAction.AddSeconds(3))
                                {
                                    NavigateOnGrid.NavigateToTarget(wreckToLoot, 500);
                                    return;
                                }
                            }
                        }
                        else if (wreckToLoot.Distance > (int)Distances.ScoopRange)
                        {
                            NavigateOnGrid.NavigateToTarget(wreckToLoot, 500);
                        }

                        Salvage.TargetWrecks(ESCache.Instance.Wrecks.Where(i => !i.IsWreckEmpty).ToList());
                        Salvage.LootWrecks();
                        if (ESCache.Instance.ActiveShip.Entity.IsCruiser && (Salvage.TractorBeams.Any(i => i.IsActive) || (!Salvage.TractorBeams.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && 15000 > i.Distance))) && ESCache.Instance.MyShipEntity.Velocity > 500 && ESCache.Instance.Wrecks.Any(i => 10000 > i.Distance))
                            ESCache.Instance.ActiveShip.SetSpeed(50);

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }
}