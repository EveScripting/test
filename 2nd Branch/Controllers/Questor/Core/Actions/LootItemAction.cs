extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework;
using EVESharpCore.Questor.BackgroundTasks;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using SC::SharedComponents.EVE;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void LootItemAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                List<string> targetContainerNames = null;
                List<string> targetContainerIds = null;
                long targetContainerId = 0;
                if (action.GetParameterValues("target") != null)
                    targetContainerNames = action.GetParameterValues("target");

                if ((targetContainerNames == null || targetContainerNames.Count == 0) && Salvage.LootItemRequiresTarget)
                    Log.WriteLine(" *** No Target Was Specified In the LootItem Action! ***");

                if (action.GetParameterValues("containerid") != null)
                    targetContainerIds = action.GetParameterValues("containerid");
                if (targetContainerIds != null && targetContainerIds.Count > 0)
                    targetContainerId = long.Parse(targetContainerIds.FirstOrDefault());

                List<string> typeIDOfItemsToLoot = null;
                string typeIDOfItemToLoot = null;
                if (action.GetParameterValues("typeIDOfItemToLoot") != null)
                    typeIDOfItemsToLoot = action.GetParameterValues("typeIDOfItemToLoot");

                DirectInvType invTypeToLoot = null;
                if (typeIDOfItemsToLoot != null && typeIDOfItemsToLoot.Count > 0)
                {
                    typeIDOfItemToLoot = typeIDOfItemsToLoot.FirstOrDefault();
                    if (typeIDOfItemToLoot != null)
                        invTypeToLoot = ESCache.Instance.DirectEve.GetInvType(int.Parse(typeIDOfItemToLoot));
                }

                List<string> itemsToLoot = null;
                if (action.GetParameterValues("item") != null)
                    itemsToLoot = action.GetParameterValues("item");

                if (itemsToLoot == null && typeIDOfItemToLoot == null)
                {
                    Log.WriteLine(" *** No Item Was Specified In the LootItem Action! ***");
                    NextAction(myMission, myAgent, true);
                }

                // if we are not generally looting we need to re-enable the opening of wrecks to
                // find this LootItems we are looking for
                Salvage.OpenWrecks = true;

                int quantity;
                if (!int.TryParse(action.GetParameterValue("quantity"), out quantity))
                    quantity = 1;

                if (ESCache.Instance.CurrentShipsCargo != null &&
                    ESCache.Instance.CurrentShipsCargo.Items.Any(i => itemsToLoot != null && itemsToLoot.Contains(i.TypeName, StringComparer.OrdinalIgnoreCase) && i.Quantity >= quantity))
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

                //
                // we re-sot by distance on every pulse. The order will be potentially different on each pulse as we move around the field. this is ok and desirable.
                //
                if (ESCache.Instance.Containers == null) return;
                List<EntityCache> containers =
                    ESCache.Instance.Containers.Where(e => !ESCache.Instance.LootedContainers.Contains(e.Id))
                        .OrderByDescending(e => e.GroupId == (int)Group.CargoContainer)
                        .ThenBy(e => e.IsWreckEmpty)
                        .ThenBy(e => e.Distance).ToList();

                if (containers.Count == 0)
                {
                    containers = ESCache.Instance.Containers.OrderByDescending(e => e.GroupId == (int)Group.CargoContainer)
                            .ThenBy(e => e.IsWreckEmpty)
                            .ThenBy(e => e.Distance).ToList();
                }

                if (DebugConfig.DebugLootWrecks)
                {
                    int i = 0;
                    foreach (EntityCache myContainer in containers)
                    {
                        i++;
                        Log.WriteLine("[" + i + "] " + myContainer.Name + "[" + Math.Round(myContainer.Distance / 1000, 0) + "k] isWreckEmpty [" +
                                      myContainer.IsWreckEmpty +
                                      "] IsTarget [" + myContainer.IsTarget + "]");
                    }

                    i = 0;
                    foreach (string targetContainer in targetContainerNames)
                    {
                        i++;
                        Log.WriteLine("TargetContainerName [" + i + "][ " + targetContainer + " ]");
                    }
                }

                if (containers.Count == 0)
                {
                    Log.WriteLine("no containers left to loot, next action");

                    if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                    {
                        if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                        Salvage.OpenWrecks = false;
                    }

                    NextAction(myMission, myAgent, true);
                    return;
                }

                if (targetContainerId != 0)
                    if (!ESCache.Instance.ListofContainersToLoot.Contains(targetContainerId))
                        ESCache.Instance.ListofContainersToLoot.Add(targetContainerId);

                //
                // add containers that we were told to loot into the ListOfContainersToLoot so that they are prioritized by the background salvage routine
                //

                foreach (EntityCache containerToLoot in containers)
                {
                    if (targetContainerNames != null && targetContainerNames.Count > 0)
                    {
                        foreach (string targetContainerName in targetContainerNames)
                        {
                            if (!containerToLoot.Name.ToLower().Contains(targetContainerName.ToLower())) continue;

                            if (!ESCache.Instance.ListofContainersToLoot.Contains(containerToLoot.Id))
                            {
                                ESCache.Instance.ListofContainersToLoot.Add(containerToLoot.Id);
                            }
                        }
                    }
                    else
                    {
                        foreach (EntityCache unlootedContainer in ESCache.Instance.UnlootedContainers)
                        {
                            if (containerToLoot.Name == unlootedContainer.Name)
                            {
                                if (!ESCache.Instance.ListofContainersToLoot.Contains(containerToLoot.Id))
                                    ESCache.Instance.ListofContainersToLoot.Add(containerToLoot.Id);
                            }
                        }
                    }
                }

                if (invTypeToLoot != null)
                    if (!ESCache.Instance.ListofMissionCompletionItemsToLoot.Contains(invTypeToLoot.TypeName))
                        ESCache.Instance.ListofMissionCompletionItemsToLoot.Add(invTypeToLoot.TypeName);

                if (itemsToLoot != null && itemsToLoot.Count > 0)
                    foreach (string itemToLoot in itemsToLoot)
                        if (!ESCache.Instance.ListofMissionCompletionItemsToLoot.Contains(itemToLoot.ToLower()))
                            ESCache.Instance.ListofMissionCompletionItemsToLoot.Add(itemToLoot.ToLower());

                EntityCache container;
                if (targetContainerNames != null && targetContainerNames.Count > 0)
                {
                    container = ESCache.Instance.Containers.OrderBy(d => d.Distance).FirstOrDefault(i => ESCache.Instance.ListofContainersToLoot.Contains(i.Id));
                    //if (container == null)
                    //    foreach (EntityCache myContainer in containers.OrderBy(u => !u.IsWreck).ThenBy(j => j.Distance).Where(i => !i.IsWreckEmpty))
                    //        container = myContainer;

                    if (container == null)
                    {
                        Log.WriteLine("no containers exist with [" + targetContainerNames.FirstOrDefault() + "] in the name, assuming it is not a container.");
                        container = ESCache.Instance.EntitiesOnGrid.Find(c => targetContainerNames.Contains(c.Name));
                        if (container == null)
                            Log.WriteLine("no entities exist with [" + targetContainerNames.FirstOrDefault() + "] in the name. failing.");
                    }
                }
                else
                {
                    container = containers.FirstOrDefault();
                    if (container == null)
                    {
                        Log.WriteLine("no containers exist with [" + targetContainerNames.FirstOrDefault() + "] in the name, assuming it is not a container.");
                        container = ESCache.Instance.Entities.Find(c => targetContainerNames.Contains(c.Name));
                        if (container == null)
                            Log.WriteLine("no entities exist with [" + targetContainerNames.FirstOrDefault() + "] in the name. failing.");
                    }
                }

                if (container != null)
                {
                    if (container.Distance > (int)Distances.SafeScoopRange)
                        NavigateOnGrid.NavigateToTarget(container, 0);
                }
                else
                {
                    Log.WriteLine("LootItem Action failed. container is null. we were trying to find [ " + targetContainerNames.FirstOrDefault() +
                                  " ] but it does not appear to be on grid");
                    NextAction(myMission, myAgent, true);
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