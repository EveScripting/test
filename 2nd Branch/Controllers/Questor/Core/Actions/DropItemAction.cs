extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void DropItemAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                //Cache.Instance.DropMode = true;
                List<string> items = action.GetParameterValues("item");
                string targetName = action.GetParameterValue("target");

                int quantity;
                if (!int.TryParse(action.GetParameterValue("quantity"), out quantity))
                    quantity = 1;

                if (!CargoHoldHasBeenStacked)
                {
                    Log.WriteLine("Stack CargoHold");
                    if (!ESCache.Instance.CurrentShipsCargo.StackShipsCargo()) return; //.StackCargoHold("DropItem")) return;
                    CargoHoldHasBeenStacked = true;
                    return;
                }

                IEnumerable<EntityCache> targetEntities = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == targetName.ToLower()).ToList();
                if (targetEntities.Any())
                {
                    Log.WriteLine("We have [" + targetEntities.Count() + "] entities on grid that match our target by name: [" +
                                  targetName.FirstOrDefault() + "]");
                    targetEntities = targetEntities.Where(i => i.IsContainer || i.GroupId == (int)Group.LargeColidableObject);
                    //some missions (like: Onslaught - lvl1) have LCOs that can hold and take cargo, note that same mission has a LCS with the same name!

                    if (!targetEntities.Any())
                    {
                        Log.WriteLine("No entity on grid named: [" + targetEntities.FirstOrDefault() + "] that is also a container");

                        // now that we have completed this action revert OpenWrecks to false
                        //Cache.Instance.DropMode = false;
                        NextAction(myMission, myAgent, true);
                        return;
                    }

                    EntityCache closest = targetEntities.OrderBy(t => t.Distance).FirstOrDefault();

                    if (closest == null)
                    {
                        Log.WriteLine("closest: target named [" + targetName.FirstOrDefault() + "] was null" + targetEntities);

                        // now that we have completed this action revert OpenWrecks to false
                        //Cache.Instance.DropMode = false;
                        NextAction(myMission, myAgent, true);
                        return;
                    }

                    if (closest.Distance > (int)Distances.SafeScoopRange)
                    {
                        if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                            if (ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != closest.Id ||
                                ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 50)
                                if (closest.Approach())
                                    Log.WriteLine("Approaching target [" + closest.Name + "][" + closest.MaskedId + "] which is at [" +
                                                  Math.Round(closest.Distance / 1000, 0) + "k away]");
                    }
                    else if (ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.Velocity < 50) //nearly stopped
                    {
                        if (DateTime.UtcNow > Time.Instance.NextOpenContainerInSpaceAction)
                        {
                            DirectContainer containerWeWillDropInto = null;

                            containerWeWillDropInto = ESCache.Instance.DirectEve.GetContainer(closest.Id);
                            //
                            // the container we are going to drop something into must exist
                            //
                            if (containerWeWillDropInto == null)
                            {
                                Log.WriteLine("if (container == null)");
                                return;
                            }

                            //
                            // open the container so we have a window!
                            //
                            if (containerWeWillDropInto.Window == null)
                            {
                                if (closest.OpenCargo())
                                    Time.Instance.NextOpenContainerInSpaceAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);

                                return;
                            }

                            if (!containerWeWillDropInto.Window.IsReady)
                            {
                                Log.WriteLine("LootWrecks: containerWeWillDropInto.Window is not ready, waiting");
                                return;
                            }

                            if (ItemsHaveBeenMoved)
                            {
                                Log.WriteLine("We have Dropped the items: ItemsHaveBeenMoved [" + ItemsHaveBeenMoved + "]");
                                // now that we have completed this action revert OpenWrecks to false
                                //Cache.Instance.DropMode = false;
                                NextAction(myMission, myAgent, true);
                                return;
                            }

                            //
                            // if we are going to drop something into the can we MUST already have it in our cargohold
                            //
                            if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Any())
                            {
                                //int CurrentShipsCargoItemCount = 0;
                                //CurrentShipsCargoItemCount = Cache.Instance.CurrentShipsCargo.Items.Count();

                                //DirectItem itemsToMove = null;
                                //itemsToMove = Cache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeName.ToLower() == items.FirstOrDefault().ToLower());
                                //if (itemsToMove == null)
                                //{
                                //    Logging.Log("MissionController.DropItem", "CurrentShipsCargo has [" + CurrentShipsCargoItemCount + "] items. Item We are supposed to move is: [" + items.FirstOrDefault() + "]");
                                //    return;
                                //}

                                int ItemNumber = 0;
                                foreach (DirectItem CurrentShipsCargoItem in ESCache.Instance.CurrentShipsCargo.Items)
                                {
                                    ItemNumber++;
                                    Log.WriteLine("[" + ItemNumber + "] Found [" + CurrentShipsCargoItem.Quantity + "][" +
                                                  CurrentShipsCargoItem.TypeName +
                                                  "] in Current Ships Cargo: StackSize: [" + CurrentShipsCargoItem.Stacksize + "] We are looking for: [" +
                                                  items.FirstOrDefault() + "]");
                                    if (items.Any() && items.FirstOrDefault() != null)
                                    {
                                        string NameOfItemToDropIntoContainer = items.FirstOrDefault();
                                        if (NameOfItemToDropIntoContainer != null)
                                            if (CurrentShipsCargoItem.TypeName.ToLower() == NameOfItemToDropIntoContainer.ToLower())
                                            {
                                                Log.WriteLine("[" + ItemNumber + "] container.Capacity [" + containerWeWillDropInto.Capacity +
                                                              "] ItemsHaveBeenMoved [" +
                                                              ItemsHaveBeenMoved + "]");
                                                if (!ItemsHaveBeenMoved)
                                                {
                                                    Log.WriteLine("Moving Items: " + items.FirstOrDefault() + " from cargo ship to " +
                                                                  containerWeWillDropInto.TypeName);
                                                    //
                                                    // THIS IS NOT WORKING - EXCEPTION/ERROR IN CLIENT...
                                                    //
                                                    if (!containerWeWillDropInto.Add(CurrentShipsCargoItem, quantity)) return;
                                                    Time.Instance.NextOpenContainerInSpaceAction =
                                                        DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(4, 6));
                                                    ItemsHaveBeenMoved = true;
                                                    return;
                                                }

                                                return;
                                            }
                                    }
                                }
                            }
                            else
                            {
                                Log.WriteLine("No Items: Cache.Instance.CurrentShipsCargo.Items.Any()");
                            }
                        }
                    }

                    return;
                }

                Log.WriteLine("No entity on grid named: [" + targetEntities.FirstOrDefault() + "]");
                // now that we have completed this action revert OpenWrecks to false
                //Cache.Instance.DropMode = false;
                NextAction(myMission, myAgent, true);
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception: [" + exception + "]");
            }
        }

        #endregion Methods
    }
}