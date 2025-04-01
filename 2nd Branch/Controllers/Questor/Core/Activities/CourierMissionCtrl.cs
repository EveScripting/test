extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Questor.Activities
{
    public static class CourierMissionCtrl
    {
        #region Fields

        private static readonly List<int> PossibleNumberOfItemsToMove = new List<int>
        {
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            10,
            20,
            30,
            32,
            35,
            40,
            50,
            60,
            70,
            80,
            90,
            100,
            200,
            300,
            400,
            500,
            600,
            700,
            800,
            900,
            1000,
            2000,
            3000,
            4000,
            5000,
            6000,
            7000,
            8000,
            9000,
            10000,
            20000,
            30000,
            40000,
            50000,
            60000,
            70000,
            80000,
            90000,
            100000,
            200000,
            300000,
            400000,
            500000,
            600000,
            700000,
            800000,
            900000,
            1000000
        };

        private static int _allObjectiveCompleteAttempts;

        private static int? TotalNumberOfItemsToMove(DirectAgent myAgent)
        {
            //
            // if the agent has the view mission button we will not populate the Objective
            //

            foreach (int tryTofindThisNumber in PossibleNumberOfItemsToMove)
            {
                if (string.IsNullOrEmpty(myAgent.AgentWindow.Objective))
                {
                    Log.WriteLine("_totalNumberOfItemsToMove: Objective is empty");
                    return null;
                }

                if (myAgent.AgentWindow.Objective.Contains("x " + tryTofindThisNumber) || myAgent.AgentWindow.Objective.Contains(tryTofindThisNumber + " x"))
                    return tryTofindThisNumber;
            }

            Log.WriteLine("_totalNumberOfItemsToMove: We did not find the total number of items we should be moving within the ObjectiveHtml!");
            Log.WriteLine(myAgent.AgentWindow.Objective);
            return null;
        }

        #endregion Fields

        #region Methods

        public static bool ActivateTransportShip(DirectAgentMission myMission) // check
        {
            if (ESCache.Instance.InSpace)
            {
                Log.WriteLine("ActivateTransportShip: InSpace [" + ESCache.Instance.InSpace + "] return");
                return false;
            }

            if (string.IsNullOrEmpty(Settings.Instance.TransportShipName))
            {
                Log.WriteLine("Could not find transportshipName in settings!");
                Arm.ChangeArmState(ArmState.NotEnoughAmmo, true, null);
                return false;
            }

            if (!Arm.ActivateShip(Settings.Instance.TransportShipName)) return false;

            Log.WriteLine("ActivateTransportShip: Done");
            _allObjectiveCompleteAttempts = 0;
            ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation);
            return true;
        }

        public static bool ChangeCourierMissionCtrlState(DirectAgentMission myMission, CourierMissionCtrlState courierMbStateToSet, bool wait = true)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("InSpace [" + ESCache.Instance.InSpace + "] not changing CourierMissionCtrlState from [" + myMission.CurrentCourierMissionCtrlState + "] to [" + courierMbStateToSet + "]: return");
                    return true;
                }

                //if (courierMbStateToSet == CourierMissionCtrlState.PickupItem || courierMbStateToSet == CourierMissionCtrlState.DropOffItem)
                //    myMission.Reset();

                myMission.ChangeCourierMissionCtrlState(courierMbStateToSet);
                if (!wait) ProcessState(myMission);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void CompleteMission(DirectAgent myAgent)
        {
            //
            // this state should never be reached in space. if we are in space and in this state we should switch to go to mission
            //
            if (ESCache.Instance.InSpace)
            {
                Log.WriteLine("We are in space, how did we get set to this state while in space? Changing state to: GotoDropOffLocation");
                ChangeCourierMissionCtrlState(myAgent.Mission, CourierMissionCtrlState.GotoDropOffLocation);
                return;
            }

            if (State.CurrentAgentInteractionState == AgentInteractionState.Idle)
            {
                if (!ESCache.Instance.InStation || ESCache.Instance.InSpace) //do not proceed until we have been docked for at least a few seconds
                    return;

                Log.WriteLine("Start Conversation [Complete Mission]");

                State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
            }

            AgentInteraction.ProcessState(myAgent);

            if (State.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ChangeCourierMissionCtrlState(myAgent.Mission, CourierMissionCtrlState.Statistics);
            }
        }

        public static bool DoWeHaveEnoughFreeSpaceinCargo(DirectAgentMission myMission)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("DoWeHaveEnoughFreeSpaceinCargo: InSpace [" + ESCache.Instance.InSpace + "] return");
                    return false;
                }

                if (myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.PickupItem ||
                    myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation ||
                    myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.NotEnoughCargoRoom)
                {
                    if (ESCache.Instance.CurrentShipsCargo == null) return false;
                    if (MissionSettings.M3NeededForCargo(myMission) == null) return false;
                    if (ESCache.Instance.CurrentShipsCargo.Items.Count > 0)
                    {
                        if (!ESCache.Instance.CurrentShipsCargo.FreeCapacity.HasValue)
                        {
                            Log.WriteLine("Courier:  FreeCapacity [" + Math.Round(ESCache.Instance.CurrentShipsCargo.FreeCapacity ?? 0, 0) + "] Capacity [" + ESCache.Instance.CurrentShipsCargo.Capacity + "] UsedCapacity [" + ESCache.Instance.CurrentShipsCargo.UsedCapacity + "] ItemCount [" + ESCache.Instance.CurrentShipsCargo.Items.Count + "]");
                            return false;
                        }

                        if (ESCache.Instance.CurrentShipsCargo.FreeCapacity > MissionSettings.M3NeededForCargo(myMission))
                        {
                            Log.WriteLine("Courier:  [" + Math.Round(ESCache.Instance.CurrentShipsCargo.FreeCapacity ?? 0, 0) + "] Cargo Capacity and the Courier Mission needs [" + MissionSettings.M3NeededForCargo(myMission) + "]: Pulling Items [" + myMission.Name + "]");
                            return true;
                        }

                        Log.WriteLine("Courier:  [" + Math.Round(ESCache.Instance.CurrentShipsCargo.FreeCapacity ?? 0, 0) + "] Cargo Capacity and the Courier Mission needs [" + MissionSettings.M3NeededForCargo(myMission) + "]: waiting until later to process [" + myMission.Name + "]");
                        ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.NotEnoughCargoRoom);
                        return false;
                    }

                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void DropOffItem(DirectAgentMission myMission, DirectAgent myAgent)
        {
            Log.WriteLine("DropOffItem");
            if (ESCache.Instance.InSpace)
            {
                Log.WriteLine("DropOffItem: InSpace [" + ESCache.Instance.InSpace + "] return");
                return;
            }

            if (myAgent.Mission.moveItemRetryCounter > 20)
            {
                ControllerManager.Instance.SetPause(true);
                Log.WriteLine("MoveItem has tried 20x to Drop off the mission item and failed. Pausing: please debug the cause of this error");
                ChangeCourierMissionCtrlState(myAgent.Mission, CourierMissionCtrlState.Error);
                return;
            }

            if (ManageCourierMission(myMission, myAgent, false))
            {
                Log.WriteLine("DropOffItem: ManageCourierMission returned true");
                myAgent.Mission.moveItemRetryCounter = 0;
            }
        }

        public static void GotoDropoffLocation(DirectAgentMission myMission)
        {
            if (Traveler.TravelToMissionBookmark(myMission, "Objective (Drop Off)"))
            {
                if (DebugConfig.DebugCourierMissions) Log.WriteLine("Changing State.CurrentCourierMissionCtrlState to [ CourierMissionCtrlState.DropOffItem ]");
                ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.DropOffItem);
            }
        }

        public static void GotoPickupLocation(DirectAgentMission myMission)
        {
            try
            {
                if (Traveler.TravelToMissionBookmark(myMission, "Objective (Pick Up)"))
                    ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.PickupItem);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void ItemsFoundAndBeingMoved(DirectAgentMission myMission)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("ItemsFoundAndBeingMoved: InSpace [" + ESCache.Instance.InSpace + "] return");
                    return;
                }

                if (ESCache.Instance.CurrentShipsCargo == null) return;
                if (ESCache.Instance.ItemHangar == null) return;

                if (myMission.CourierMissionItemToMove == null)
                {
                    Log.WriteLine("ItemsFoundAndBeingMoved: myMission.CourierMissionItemToMove == null");
                    return;
                }

                if (myMission.CourierMissionToContainer == null)
                {
                    Log.WriteLine("ItemsFoundAndBeingMoved: CourierMissionToContainer == null");
                    return;
                }

                if (myMission.CourierMissionItemToMove != null)
                {
                    State.CurrentArmState = ArmState.Idle;

                    int fromContainerItemCount = 0;
                    double fromContainerCapacity = 0;
                    double fromContainerFreeCapacity = 0;
                    if (myMission.CourierMissionFromContainer != null && myMission.CourierMissionFromContainer.Items != null && myMission.CourierMissionFromContainer.Items.Count > 0)
                    {
                        fromContainerItemCount = myMission.CourierMissionFromContainer.Items.Count;
                        fromContainerCapacity = myMission.CourierMissionFromContainer.Capacity;
                        fromContainerFreeCapacity = myMission.CourierMissionFromContainer.FreeCapacity ?? 0;
                    }

                    Log.WriteLine("fromContainer [" + myMission.CourierMissionFromContainerName + "] has [" + fromContainerItemCount + "] items [" + fromContainerCapacity + "] m3 Capacity [" + fromContainerFreeCapacity + "] m3 FreeCapacity");

                    int toContainerItemCount = 0;
                    double toContainerCapacity = 0;
                    double toContainerFreeCapacity = 0;
                    if (myMission.CourierMissionToContainer != null && myMission.CourierMissionToContainer.Items != null && myMission.CourierMissionToContainer.Items.Count > 0)
                    {
                        toContainerItemCount = myMission.CourierMissionToContainer.Items.Count;
                        toContainerCapacity = myMission.CourierMissionToContainer.Capacity;
                        toContainerFreeCapacity = myMission.CourierMissionToContainer.FreeCapacity ?? 0;
                    }

                    Log.WriteLine("_itemToMove [" + myMission.CourierMissionItemToMove.TypeName + "] Stacksize [" + myMission.CourierMissionItemToMove.Stacksize + "]");
                    Log.WriteLine("ToContainer [" + myMission.CourierMissionToContainerName + "] has [" + toContainerItemCount + "] items [" + toContainerCapacity + "] m3 Capacity [" + toContainerFreeCapacity + "] m3 FreeCapacity: ArmState [" + State.CurrentArmState + "]");

                    if (myMission.CourierMissionToContainer != null)
                        if (!Arm.MoveItemsToCargo(myMission.CourierMissionFromContainer, myMission.CourierMissionToContainer, myMission.CourierMissionItemToMove.TypeName, myMission.CourierMissionItemToMove.Stacksize, ArmState.Idle, false, myMission.Agent)) return;

                    if (myMission.PreviousCourierMissionCtrlState == CourierMissionCtrlState.PickupItem)
                        if (myMission.CourierMissionItemToMove != null)
                        {
                            Log.WriteLine("EveAccount.CourierItemTypeId [" + ESCache.Instance.EveAccount.CourierItemTypeId + "]");
                        }
                        else
                        {
                            Log.WriteLine("_itemToMove == null");
                        }

                    ChangeCourierMissionCtrlState(myMission, myMission.PreviousCourierMissionCtrlState);
                    return;
                }

                Log.WriteLine("_itemToMove == null.");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool ManageCourierMission(DirectAgentMission myMission, DirectAgent myAgent, bool tryToGrabPickupItemsFromHomeStation)
        {
            try
            {
                Log.WriteLine("ManageCourierMission: CurrentCourierMissionCtrlState [" + myMission.CurrentCourierMissionCtrlState + "]");

                if (Time.Instance.NextLootAction > DateTime.UtcNow)
                {
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("if (Time.Instance.NextLootAction [" + Time.Instance.NextLootAction.ToShortTimeString() + "] < DateTime.UtcNow)");
                    return false;
                }

                if (!myAgent.OpenAgentWindow(true)) return false;

                // Open the item hangar (should still be open)
                if (ESCache.Instance.ItemHangar == null)
                {
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("if (ESCache.Instance.ItemHangar == null) return;");
                    return false;
                }

                if (ESCache.Instance.CurrentShipsCargo == null)
                {
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("if (ESCache.Instance.CurrentShipsCargo == null) return;");
                    return false;
                }

                //if (myAgent.Mission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoPickupLocation)
                //{
                //    if (...)
                //}

                if (myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoDropOffLocation)
                {
                    Log.WriteLine("ManageCourierMission: GotoDropOffLocation");
                    return false;
                }

                if (myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.DropOffItem)
                {
                    // We completed all the objectives of the mission (even if the item is still in our cargo)
                    if (myMission.GetAgentMissionRawCsvHint().ToLower().Contains("AllObjectivesComplete".ToLower()))
                        if (_allObjectiveCompleteAttempts < 1)
                        {
                            _allObjectiveCompleteAttempts++;
                            Log.WriteLine("All Objectives Complete");
                            ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.CompleteMission);
                            return true;
                        }

                    if (myMission.GetAgentMissionRawCsvHint().ToLower().Contains("TransportItemsPresent".ToLower()))
                        if (_allObjectiveCompleteAttempts < 1)
                        {
                            _allObjectiveCompleteAttempts++;
                            Log.WriteLine("All Objectives Complete: TransportItemsPresent");
                            ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.CompleteMission);
                            return true;
                        }
                }

                DirectItem foundItemWithCorrectTypeId = null;

                if (myMission.CourierMissionToContainer != null && myMission.CourierMissionToContainer.Items != null)
                {
                    // Check ObjectiveHtml to determine if we moved the item
                    if (foundItemWithCorrectTypeId == null && !string.IsNullOrEmpty(myAgent.AgentWindow.Objective) && myMission.CourierMissionToContainer.Items.Any(i => myAgent.AgentWindow.Objective.Contains("x " + i.TypeName)))
                    {
                        foundItemWithCorrectTypeId = myMission.CourierMissionToContainer.Items.Find(i => myAgent.AgentWindow.Objective.Contains("x " + i.TypeName));
                        if (foundItemWithCorrectTypeId != null) Log.WriteLine("foundItemWithCorrectTypeId [" + foundItemWithCorrectTypeId.TypeName + "][" + foundItemWithCorrectTypeId.Quantity + "] found via ObjectiveHtml");
                    }

                    // Check AgentInfo to determine if we moved the item
                    if (foundItemWithCorrectTypeId == null && myMission.CourierMissionToContainer.Items.Any(i => myMission.GetAgentMissionRawCsvHint().Contains(i.TypeId.ToString())))
                    {
                        foundItemWithCorrectTypeId = myMission.CourierMissionToContainer.Items.Find(i => myMission.GetAgentMissionRawCsvHint().Contains(i.TypeId.ToString()));
                        if (foundItemWithCorrectTypeId != null) Log.WriteLine("foundItemWithCorrectTypeId [" + foundItemWithCorrectTypeId.TypeName + "][" + foundItemWithCorrectTypeId.Quantity + "] found via AgentMissionInfo");
                    }

                    // Check EveAccount.CourierItemTypeid to determine if we moved the item
                    if (foundItemWithCorrectTypeId == null && myMission.CourierMissionToContainer.Items.Any(i => i.TypeId != 0 && i.TypeId == ESCache.Instance.EveAccount.CourierItemTypeId))
                    {
                        foundItemWithCorrectTypeId = myMission.CourierMissionToContainer.Items.Find(i => i.TypeId == ESCache.Instance.EveAccount.CourierItemTypeId);
                        if (foundItemWithCorrectTypeId != null) Log.WriteLine("foundItemWithCorrectTypeId [" + foundItemWithCorrectTypeId.TypeName + "][" + foundItemWithCorrectTypeId.Quantity + "] found via EveAccount.CourierItemTypeId");
                    }

                    if (foundItemWithCorrectTypeId != null)
                    {
                        int quantityFoundinToContainer = myMission.CourierMissionToContainer.Items.Where(i => i.TypeId == foundItemWithCorrectTypeId.TypeId && i.Quantity != -1).Sum(i => i.Quantity);
                        if (quantityFoundinToContainer == 0)
                            quantityFoundinToContainer = myMission.CourierMissionToContainer.Items.Where(i => i.TypeId == foundItemWithCorrectTypeId.TypeId).Sum(i => i.Quantity);

                        if (quantityFoundinToContainer == 0)
                            Log.WriteLine("Error: We found [" + foundItemWithCorrectTypeId.TypeName + "] but quantityFoundinToContainer is 0");

                        if (quantityFoundinToContainer != 0 && TotalNumberOfItemsToMove(myAgent) >= quantityFoundinToContainer)
                        {
                            Log.WriteLine("We have located [" + foundItemWithCorrectTypeId.Quantity + "][" + foundItemWithCorrectTypeId.TypeName + "] TypeId [ " + foundItemWithCorrectTypeId.TypeId + " ] in the toContainer [" + myAgent.Mission.CourierMissionToContainerName + "]: we needed to move [" + TotalNumberOfItemsToMove(myAgent) + "] of them from [" + myAgent.Mission.CourierMissionFromContainerName + "][" + myAgent.Mission.CurrentCourierMissionCtrlState + "]");
                            myMission.moveItemRetryCounter = 0;
                            if (myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.PickupItem ||
                                myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoPickupLocation ||
                                myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation ||
                                (myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.ItemsFoundAndBeingMoved &&
                                 (myMission.PreviousCourierMissionCtrlState == CourierMissionCtrlState.PickupItem ||
                                myMission.PreviousCourierMissionCtrlState == CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation)))
                            {
                                ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.GotoDropOffLocation);
                                return true;
                            }

                            ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.CompleteMission);
                            return true;
                        }

                        if (quantityFoundinToContainer != 0 && TotalNumberOfItemsToMove(myAgent) <= foundItemWithCorrectTypeId.Quantity)
                        {
                            myMission.moveItemRetryCounter = 0;
                            if (myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.PickupItem ||
                                myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation ||
                                (myMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.ItemsFoundAndBeingMoved &&
                                 (myMission.PreviousCourierMissionCtrlState == CourierMissionCtrlState.PickupItem ||
                                  myMission.PreviousCourierMissionCtrlState == CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation)))
                            {
                                Log.WriteLine("We have located [" + foundItemWithCorrectTypeId.Quantity + "][" + foundItemWithCorrectTypeId.TypeName + "] TypeId [ " + foundItemWithCorrectTypeId.TypeId + " ] in the destination container: we need to move [" + TotalNumberOfItemsToMove(myAgent) + "] of them");
                                ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.GotoDropOffLocation);
                                return true;
                            }

                            if (TotalNumberOfItemsToMove(myAgent) == 0)
                            {
                                Log.WriteLine("We have located [" + foundItemWithCorrectTypeId.Quantity + "][" + foundItemWithCorrectTypeId.TypeName + "] TypeId [ " + foundItemWithCorrectTypeId.TypeId + " ] in the destination container: we dont know how many we need, so assume we are done.");
                                ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.CompleteMission);
                                return true;
                            }

                            Log.WriteLine("We have located [" + foundItemWithCorrectTypeId.Quantity + "][" + foundItemWithCorrectTypeId.TypeName + "] TypeId [ " + foundItemWithCorrectTypeId.TypeId + " ] in the destination container: we need to move [" + TotalNumberOfItemsToMove(myAgent) + "] of them");
                            ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.GotoPickupLocation);
                            return true;
                        }
                    }
                }

                Log.WriteLine("--------------------------Mission Objective Info----------------------------");
                Log.WriteLine("MissionSettings.RegularMission.State [" + myMission.State + "]");
                Log.WriteLine("agentMissionInfo [" + myMission.GetAgentMissionRawCsvHint() + "]");
                Log.WriteLine("EveAccount.CourierItemTypeId [" + ESCache.Instance.EveAccount.CourierItemTypeId + "] MissionHint_TransportItemMissingTypeId [" + myAgent.Mission.MissionHint_TransportItemMissingTypeId + "]");
                Log.WriteLine("---------------------------------------------------------------------------");

                if (!DoWeHaveEnoughFreeSpaceinCargo(myMission)) return false;

                // Move items
                if (myMission.CourierMissionFromContainer != null && myMission.CourierMissionFromContainer.Items != null && myMission.CourierMissionFromContainer.Items.Count > 0)
                    foreach (DirectItem item in myMission.CourierMissionFromContainer.Items)
                    {
                        Log.WriteLine("Found [" + item.TypeName + "][" + item.TypeId + "] in fromContainer");

                        if (myMission.MissionHint_TransportItemMissingTypeId == item.TypeId)
                        {
                            myMission.CourierMissionItemToMove = item;
                            if (myMission.CourierMissionItemToMove == null) Log.WriteLine("ManageCourierMission: if (myAgent.RegularMission.CourierMissionItemToMove == null).");
                            else Log.WriteLine("ManageCourierMission: myAgent.RegularMission [" + myMission.Name + "] CourierMissionItemToMove [" + myMission.CourierMissionItemToMove.TypeName + "][" + item.Quantity + "]");
                            ItemsFoundAndBeingMoved(myMission);
                            return false;
                        }

                        if (_allObjectiveCompleteAttempts >= 1 && ESCache.Instance.EveAccount.CourierItemTypeId == item.TypeId)
                        {
                            if (!Arm.MoveItemsToCargo(myMission.CourierMissionFromContainer, myMission.CourierMissionToContainer, item.TypeName, item.Quantity, ArmState.Idle, false, myAgent)) return false;
                            return false;
                        }

                        if (!string.IsNullOrEmpty(myAgent.AgentWindow.Objective) && (myAgent.AgentWindow.Objective.Contains("x " + item.TypeName) || myAgent.AgentWindow.Objective.Contains(item.TypeName + " x")))
                        {
                            if (!Arm.MoveItemsToCargo(myMission.CourierMissionFromContainer, myMission.CourierMissionToContainer, item.TypeName, item.Quantity, ArmState.Idle, false, myAgent)) return false;
                            return false;
                        }
                    }

                if (tryToGrabPickupItemsFromHomeStation)
                {
                    Log.WriteLine("MoveItemProcess: Unable to find the Item here. We have to assume it is at the pickup location");
                    ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.GotoPickupLocation);
                    return false;
                }
                //
                // we should probably decline the mission here, unless we can somehow figure out what to grab and where it is =/
                //
                Log.WriteLine("We did not find the items in our cargo, or AgentMissionInfo unexpectedly did not have the typeId of the items in it.");
                Log.WriteLine("agentMissionInfo [" + myMission.GetAgentMissionRawCsvHint() + "] MissionHint_TransportItemMissingTypeId [" + myAgent.Mission.MissionHint_TransportItemMissingTypeId + "] moveItemRetryCounter [" + myAgent.Mission.moveItemRetryCounter + "]");
                if (myMission.moveItemRetryCounter > 25)
                {
                    ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.GotoPickupLocation);
                    return false;
                }

                myMission.moveItemRetryCounter++;
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("MoveItem: exception [" + exception + "]");
                return false;
            }
        }

        /**
        public static bool MoveItem(DirectContainer fromContainer, DirectContainer toContainer, DirectItem itemToMove)
        {
            try
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("MoveItem: InSpace [" + ESCache.Instance.InSpace + "] return");
                    return false;
                }

                int itemMovedCount = 0;
                foreach (DirectItem itemInContainer in fromContainer.Items)
                {
                    if (itemInContainer.ItemId == itemToMove.ItemId)
                    {
                        itemMovedCount++;
                        Log.WriteLine("Moving [" + itemToMove.TypeName + "][" + itemToMove.TypeId + "]");
                        toContainer.Add(itemToMove, itemToMove.Stacksize);
                        continue;
                    }
                }

                if (itemMovedCount == 0)
                {
                    Log.WriteLine("MoveItem: Finished moving [" + itemToMove.TypeName + "]");
                    return true;
                }

                Log.WriteLine("Waiting a few seconds before checking progress...");
                Time.Instance.NextLootAction = DateTime.UtcNow.AddSeconds(2);
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("MoveItem: exception [" + exception + "]");
                return true;
            }
        }
        **/

        public static void PickupItem(DirectAgentMission myMission, DirectAgent myAgent)
        {
            Log.WriteLine("PickupItem");
            if (ESCache.Instance.InSpace)
            {
                Log.WriteLine("PickupItem: InSpace [" + ESCache.Instance.InSpace + "] return");
                return;
            }

            if (!ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugCourierMissions) Log.WriteLine("Waiting until we are safely docked");
                return;
            }

            if (myAgent.Mission.moveItemRetryCounter > 20)
            {
                ControllerManager.Instance.SetPause(true);
                Log.WriteLine("MoveItem has tried 20x to Pickup the Mission Item and failed. Pausing: please debug the cause of this error");
                ChangeCourierMissionCtrlState(myAgent.Mission, CourierMissionCtrlState.Error);
                return;
            }

            if (ManageCourierMission(myMission, myAgent, false))
            {
                Log.WriteLine("PickupItem: ManageCourierMission returned true");
                myAgent.Mission.moveItemRetryCounter = 0;
            }
        }

        public static void PrerequisitesForGotoDropOffLocation(DirectAgentMission myMission)
        {
            //
            // either we have the items in our cargo OR they already exist in the destination
            //
            if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items != null && ESCache.Instance.CurrentShipsCargo.Items.Count == 0)
            {
                //
                // How are we in space, headed to the dropofflocation with no items?!
                //
                Log.WriteLine("PrerequisitesForGotoDropOffLocation: We have no items in our cargo?!");
                ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.GotoPickupLocation, true);
            }
        }

        private static DateTime _nextLogCurrentCourierMissionState = DateTime.UtcNow;

        private static void ReportCurrentCourierMissionState(DirectAgentMission myMission)
        {
            try
            {
                if (DateTime.UtcNow > _nextLogCurrentCourierMissionState)
                {
                    string missionName = string.Empty;
                    CourierMissionCtrlState myCourierMissionCtrlState = CourierMissionCtrlState.Unused;

                    if (myMission != null)
                    {
                        missionName = myMission.Name;
                        myCourierMissionCtrlState = myMission.CurrentCourierMissionCtrlState;
                    }

                    _nextLogCurrentCourierMissionState = DateTime.UtcNow.AddMinutes(1);
                    Log.WriteLine("ReportCurrentCourierMissionState: myMission [" + missionName + "] CourierMissionCtrlState [" + myCourierMissionCtrlState + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void Reset()
        {
            TriedToGrabPickupItemsFromHomeStationAlready = false;
            return;
        }

        /// <summary>
        ///     Goto the pickup location
        ///     Pickup the item
        ///     Goto drop off location
        ///     Drop the item
        ///     Goto Agent
        ///     Complete mission
        /// </summary>
        /// <returns></returns>
        public static void ProcessState(DirectAgentMission myMission)
        {
            try
            {
                if (myMission == null)
                {
                    Log.WriteLine("ProcessState: MissionSettings.RegularMission == null");
                    MissionSettings.AgentToPullNextRegularMissionFrom = null;
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                    {
                        CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Start, true, null);
                        return;
                    }

                    return;
                }

                if (AgentInteraction._lastAgentWindowInteraction.AddSeconds(2) > DateTime.UtcNow)
                {
                    Log.WriteLine("ProcessState: if (AgentInteraction._lastAgentWindowInteraction.AddSeconds(2) > DateTime.UtcNow)");
                    return;
                }

                ReportCurrentCourierMissionState(myMission);

                if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
                {
                    if (DebugConfig.DebugAgentInteractionReplyToAgent) Log.WriteLine("ProcessState: if (!AgentInteraction.PressViewButtonIfItExists(IsAgentWindowReady)) return;");
                    if (!AgentInteraction.PressViewButtonIfItExists("CourierMissionCtrl.ProcessState: ViewButton", myMission.Agent)) return;
                    if (!AgentInteraction.PressAcceptButtonIfItExists("CourierMissionCtrl.ProcessState: AcceptButton", myMission.Agent)) return;
                }

                if (DebugConfig.DebugCourierMissions) Log.WriteLine("CourierMissionCtrlState [" + myMission.CurrentCourierMissionCtrlState + "]");
                if (!TryToCompleteMissions()) return;

                switch (myMission.CurrentCourierMissionCtrlState)
                {
                    case CourierMissionCtrlState.Idle:
                        break;

                    case CourierMissionCtrlState.Start:
                        Reset();
                        Log.WriteLine("CourierMissionCtrlState.Start");
                        ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.ActivateTransportShip);
                        break;

                    case CourierMissionCtrlState.ActivateTransportShip:
                        ActivateTransportShip(myMission);
                        break;

                    case CourierMissionCtrlState.ItemsFoundAndBeingMoved:
                        ItemsFoundAndBeingMoved(myMission);
                        break;

                    case CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation:
                        if (DebugConfig.DebugCourierMissions) Log.WriteLine("1CourierMissionCtrlState [" + myMission.CurrentCourierMissionCtrlState + "]1");
                        TryToGrabPickupItemsFromHomeStation(myMission, myMission.Agent);
                        if (DebugConfig.DebugCourierMissions) Log.WriteLine("2CourierMissionCtrlState [" + myMission.CurrentCourierMissionCtrlState + "]2");
                        break;

                    case CourierMissionCtrlState.GotoPickupLocation:
                        //myMission.Reset();
                        GotoPickupLocation(myMission);
                        break;

                    case CourierMissionCtrlState.PickupItem:
                        PickupItem(myMission, myMission.Agent);
                        break;

                    case CourierMissionCtrlState.GotoDropOffLocation:
                        //myMission.Reset();
                        PrerequisitesForGotoDropOffLocation(myMission);
                        GotoDropoffLocation(myMission);
                        break;

                    case CourierMissionCtrlState.DropOffItem:
                        DropOffItem(myMission, myMission.Agent);
                        break;

                    case CourierMissionCtrlState.CompleteMission:
                        CompleteMission(myMission.Agent);
                        break;

                    case CourierMissionCtrlState.Statistics:
                        StatisticsCourierMissionState(myMission);
                        break;

                    case CourierMissionCtrlState.Done:
                        Log.WriteLine("CourierMissionCtrl: Done");
                        TriedToGrabPickupItemsFromHomeStationAlready = false;
                        State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static bool TryToCompleteMissions()
        {
            if (ESCache.Instance.InStation)
            {
                foreach (DirectAgentMission myMission in ESCache.Instance.DirectEve.AgentMissions.Where(i => i.Agent.IsAgentMissionAccepted && i.WeAreDockedAtDropOffLocation && i.CurrentCourierMissionCtrlState != CourierMissionCtrlState.CompleteMission))
                {
                    if (!myMission.Agent.OpenAgentWindow(true)) return false;

                    if (!string.IsNullOrEmpty(myMission.Agent.AgentWindow.Objective) && myMission.Agent.AgentWindow.Objective.Contains("Objectives Complete") && myMission.WeAreDockedAtDropOffLocation)
                    {
                        Log.WriteLine("TryToCompleteMissions: Objectives Complete");
                        if (!AgentInteraction.PressCompleteButtonIfItExists("TryToCompleteMissions", myMission.Agent)) return false;
                        if (!AgentInteraction.CloseAgentWindowIfRequestMissionButtonExists("TryToCompleteMissions", myMission.Agent)) return false;
                    }
                }

                if (DebugConfig.DebugCourierMissions) Log.WriteLine("TryToCompleteMissions: Return true;");
                return true;
            }

            return true;
        }

        private static bool TriedToGrabPickupItemsFromHomeStationAlready = false;

        public static void TryToGrabPickupItemsFromHomeStation(DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (ESCache.Instance.InSpace)
            {
                Log.WriteLine("TryToGrabPickupItemsFromHomeStation: InSpace [" + ESCache.Instance.InSpace + "] return");
                return;
            }

            if (!ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugCourierMissions) Log.WriteLine("Waiting until we are safely docked");
                return;
            }

            if (myAgent == null)
            {
                Log.WriteLine("TryToGrabPickupItemsFromHomeStation: if (myAgent == null)");
                return;
            }

            if (myMission == null)
            {
                Log.WriteLine("TryToGrabPickupItemsFromHomeStation: myAgent [" + myAgent.Name + "] if (myMission == null)");
                return;
            }

            if (TriedToGrabPickupItemsFromHomeStationAlready)
            {
                Log.WriteLine("TryToGrabPickupItemsFromHomeStation: CurrentCourierMissionCtrlState [" + myMission.CurrentCourierMissionCtrlState + "] ");
                return;
            }

            if (myMission.moveItemRetryCounter > 20)
            {
                Log.WriteLine("TryToGrabPickupItemsFromHomeStation: moveItemRetryCounter is [" + myMission.moveItemRetryCounter + "]");
                myMission.moveItemRetryCounter = 0;
                TriedToGrabPickupItemsFromHomeStationAlready = true;
                Log.WriteLine("TryToGrabPickupItemsFromHomeStation: CurrentCourierMissionCtrlState [" + myMission.CurrentCourierMissionCtrlState + "] ");
                ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.GotoPickupLocation);
                return;
            }

            Log.WriteLine("TryToGrab: ManageCourierMission: Starting");
            if (ManageCourierMission(myMission, myAgent, true))
            {
                Log.WriteLine("TryToGrab: ManageCourierMission returned true");
                myMission.moveItemRetryCounter = 0;
                return;
            }

            myMission.moveItemRetryCounter++;
            Log.WriteLine("TryToGrab: moveItemRetryCounter [" + myAgent.Mission.moveItemRetryCounter + "]");
        }

        private static void StatisticsCourierMissionState(DirectAgentMission myMission)
        {
            Statistics.FinishedMission = DateTime.UtcNow;

            try
            {
                if (ESCache.Instance.CurrentShipsCargo.Items.Count == 0)
                {
                    Dictionary<Tuple<long, string>, CourierMissionCtrlState> tempDictCurrentCourierMissionCtrlState = DirectEve.DictCurrentCourierMissionCtrlState;
                    foreach (KeyValuePair<Tuple<long, string>, CourierMissionCtrlState> kvpState in tempDictCurrentCourierMissionCtrlState)
                        if (kvpState.Value == CourierMissionCtrlState.NotEnoughCargoRoom)
                            DirectEve.DictCurrentCourierMissionCtrlState.AddOrUpdate(kvpState.Key, CourierMissionCtrlState.Start);
                }

                if (!Statistics.MissionLoggingCompleted)
                {
                    Statistics.WriteMissionStatistics();
                    return;
                }

                ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.Done);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                ChangeCourierMissionCtrlState(myMission, CourierMissionCtrlState.Done);
            }
        }

        #endregion Methods
    }
}